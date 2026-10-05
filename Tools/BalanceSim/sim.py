"""Balance simulator v0 — mirrors Assets/Scripts/VillagerAI/*.cs numbers.

Run 30 in-game days with random weather, check villages neither die
instantly nor never react. Keep in sync with C# when tuning.

Usage:
    python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7
    python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7 --scenario cruel
    python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7 --scenario kind
    python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7 --json
"""
import argparse
import json
import math
import random
from dataclasses import dataclass, field

NAMES = ["Asha", "Bram", "Chiku", "Dev", "Esha", "Farid", "Gita", "Hari", "Ira", "Jai"]
JOBS = ["farmer", "fisher", "builder", "elder", "child"] * 2
TRAITS = ["optimistic", "gruff", "anxious", "devout", "mischievous"] * 2

WEATHERS = ["sun", "rain", "wind", "calm"]


def clamp(v, lo=0.0, hi=100.0):
    return max(lo, min(hi, v))


@dataclass
class Needs:
    food: float = 70.0
    water: float = 70.0
    warmth: float = 70.0
    safety: float = 70.0
    faith: float = 55.0

    def lowest(self):
        return min(self.food, self.water, self.warmth, self.safety)


@dataclass
class MemoryEvent:
    day: int
    description: str
    valence: float
    initial_salience: float
    salience: float
    cause: str


def salience_for(valence):
    a = abs(valence) / 100.0
    if a < 0.15:
        return 0.25
    if a < 0.4:
        return 0.55
    if a < 0.7:
        return 0.8
    return 1.0


@dataclass
class Villager:
    name: str
    job: str
    trait: str
    needs: Needs = field(default_factory=Needs)
    memories: list = field(default_factory=list)
    has_shrine: bool = False
    last_action: str = "idle"

    def relationship(self):
        return clamp(sum(m.valence * m.salience for m in self.memories), -100, 100)

    def remember(self, day, desc, valence, cause):
        s = salience_for(valence)
        self.memories.append(MemoryEvent(day, desc, valence, s, s, cause))
        if len(self.memories) > 30:
            self.memories.pop(0)

    def decay(self, day):
        for m in self.memories:
            age = max(0, day - m.day)
            keep = 0.95 if m.initial_salience > 0.8 else 0.85
            m.salience = m.initial_salience * (keep ** age)
        self.memories = [m for m in self.memories if m.salience >= 0.05]


def apply_weather(v: Villager, weather: str, intensity: float, island: dict):
    n = v.needs
    i = intensity
    if weather == "sun":
        n.food += 10 * i; n.water -= 6 * i; n.warmth += 15 * i; n.safety += 2 * i
        island["fire"] += 8 * i; island["river"] -= 6 * i
    elif weather == "rain":
        n.water += 22 * i; n.warmth -= 10 * i; island["river"] += 14 * i
        if i > 0.75 or island["river"] > 85:
            n.safety -= 15 * i; n.food -= 10 * i
        else:
            n.food += 8 * i; n.safety -= 3 * i
        island["fire"] -= 12 * i
    elif weather == "wind":
        n.water -= 3 * i; n.warmth -= 4 * i; n.food -= 2 * i; n.safety -= 6 * i
        island["river"] -= 2 * i
    else:  # calm
        n.warmth += 2; n.safety += 5; n.food += 2; island["river"] -= 4
    # passive drift
    n.food -= 2; n.water -= 2
    n.warmth += (50 - n.warmth) * 0.08
    n.safety += (70 - n.safety) * 0.08
    if n.lowest() > 50:
        n.faith += 1
    elif min(n.food, n.water, n.warmth, n.safety) < 30:
        n.faith -= 1.5
    else:
        n.faith += (50 - n.faith) * 0.02
    for k in ("food", "water", "warmth", "safety", "faith"):
        setattr(n, k, clamp(getattr(n, k)))
    island["river"] = clamp(island["river"]); island["fire"] = clamp(island["fire"])
    if n.water < 20:
        n.food -= 5; n.faith -= 2
    if n.warmth < 20:
        n.safety -= 5; n.faith -= 2
    if island["fire"] > 85:
        n.safety -= 6; n.food -= 4
    for k in ("food", "water", "warmth", "safety", "faith"):
        setattr(n, k, clamp(getattr(n, k)))


def choose_action(v: Villager, weather: str, intensity: float, village_faith: float, low_faith_days: int):
    n = v.needs
    rel = v.relationship()
    scores = {}
    # work
    s = 30 + (n.lowest() - 50) * 0.6
    if n.safety < 30:
        s -= 40
    if n.warmth < 20:
        s -= 30
    scores["work"] = s
    # urgency: hungry/thirsty villagers work before praying
    if n.lowest() < 30:
        scores["work"] += (30 - n.lowest()) * 2.0
    # shelter
    s = (70 - n.safety) * 1.2
    if n.warmth < 25:
        s += (25 - n.warmth) * 1.5
    storm = (weather == "rain" and intensity > 0.7) or (weather == "wind" and intensity > 0.7)
    if storm:
        s += 35
    scores["shelter"] = s
    # pray
    if n.lowest() >= 40:
        scores["pray"] = -10
    else:
        scores["pray"] = (n.faith - 40) * 1.5 + (40 - n.lowest()) * 0.8
        if n.food < 25 or n.water < 25:
            scores["pray"] = min(scores["pray"], 25)
    # complain
    if n.lowest() >= 40:
        scores["complain"] = -10
    else:
        s = (40 - n.lowest()) * 1.2 + (50 - n.faith) * 0.8
        if rel < -10:
            s += 15
        scores["complain"] = s
    # shrine
    if v.has_shrine or n.faith < 60 or rel < 25:
        scores["shrine"] = -100
    else:
        scores["shrine"] = 40 + (n.faith - 60) * 1.5 + (rel - 25)
    # protest / rebel
    if village_faith > 35 or low_faith_days < 2:
        scores["protest"] = -100
    else:
        scores["protest"] = 45 + (35 - village_faith) + low_faith_days * 8 - n.faith * 0.2
    if n.faith > 20 or low_faith_days < 4:
        scores["rebel"] = -100
    else:
        scores["rebel"] = 50 + (20 - n.faith) * 1.5 + low_faith_days * 10
    return max(scores, key=scores.get)


def pick_weather(rng: random.Random, scenario: str):
    if scenario == "cruel":
        return rng.choices(["rain", "wind", "sun", "calm"], weights=[40, 30, 20, 10])[0], round(rng.uniform(0.6, 1.0), 2)
    if scenario == "kind":
        return rng.choices(["sun", "calm", "rain", "wind"], weights=[35, 30, 25, 10])[0], round(rng.uniform(0.3, 0.7), 2)
    return rng.choice(WEATHERS), round(rng.uniform(0.3, 0.9), 2)


def run(days=30, n_villagers=8, seed=7, scenario="random"):
    rng = random.Random(seed)
    villagers = [Villager(NAMES[i], JOBS[i], TRAITS[i]) for i in range(n_villagers)]
    island = {"river": 40.0, "fire": 20.0}
    low_faith_days = 0
    counts = {a: 0 for a in ["work", "shelter", "pray", "complain", "shrine", "protest", "rebel", "idle"]}
    shrines = 0
    faith_hist = []
    # daily prayer: villager 0 requests sun (flavour only in v0)
    for day in range(1, days + 1):
        weather, intensity = pick_weather(rng, scenario)
        faith = sum(v.needs.faith for v in villagers) / len(villagers)
        for v in villagers:
            before = dict(food=v.needs.food, water=v.needs.water, warmth=v.needs.warmth, safety=v.needs.safety)
            apply_weather(v, weather, intensity, island)
            d = {k: getattr(v.needs, k) - before[k] for k in before}
            worst = min(d.values())
            best = max(d.values())
            if worst <= -8:
                v.remember(day, f"{weather} hurt me ({worst:.0f})", worst * 3, f"god:{weather}")
            elif best >= 8:
                v.remember(day, f"{weather} helped me (+{best:.0f})", best * 3, f"god:{weather}")
            act = choose_action(v, weather, intensity, faith, low_faith_days)
            v.last_action = act
            counts[act] = counts.get(act, 0) + 1
            if act == "work":
                v.needs.food += 9; v.needs.water += 1
            elif act == "shelter":
                v.needs.safety += 10; v.needs.warmth += 5
            elif act == "pray":
                v.needs.faith += 4
            elif act == "complain":
                v.needs.faith -= 0.5
            elif act == "shrine":
                v.needs.faith += 10; v.has_shrine = True; shrines += 1
            elif act == "protest":
                v.needs.faith -= 1
            elif act == "rebel":
                v.needs.faith -= 2
            for k in ("food", "water", "warmth", "safety", "faith"):
                setattr(v.needs, k, clamp(getattr(v.needs, k)))
        for v in villagers:
            v.decay(day)
        faith = sum(v.needs.faith for v in villagers) / len(villagers)
        faith_hist.append(round(faith, 1))
        low_faith_days = low_faith_days + 1 if faith < 30 else 0
    avg = {k: round(sum(getattr(v.needs, k) for v in villagers) / len(villagers), 1)
           for k in ("food", "water", "warmth", "safety", "faith")}
    # Reaction = any non-default behaviour (pray/complain/shelter/shrine/protest/rebel).
    # Work alone is not enough — village must visibly respond to weather.
    reacted = (counts.get("pray", 0) + counts.get("complain", 0) + counts.get("protest", 0)
               + counts.get("rebel", 0) + shrines + counts.get("shelter", 0))
    distinct = sum(1 for a in ("work", "shelter", "pray", "complain", "shrine", "protest", "rebel") if counts.get(a, 0) > 0)
    # Alive = survival needs hold (faith excluded: faith 0 = rebellion, not death).
    alive = avg["food"] > 5 and avg["water"] > 5 and avg["warmth"] > 0 and avg["safety"] > 0
    ok = alive and reacted > 0 and distinct >= 2 and not all(f == 0 for f in faith_hist)
    return {"avg_needs": avg, "faith_hist": faith_hist, "actions": counts,
            "shrines": shrines, "reacted": reacted, "distinct": distinct, "alive": alive, "pass": bool(ok)}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--days", type=int, default=30)
    ap.add_argument("--villagers", type=int, default=8)
    ap.add_argument("--seed", type=int, default=7)
    ap.add_argument("--scenario", default="random", choices=["random", "cruel", "kind"])
    ap.add_argument("--json", action="store_true")
    args = ap.parse_args()
    res = run(days=args.days, n_villagers=args.villagers, seed=args.seed, scenario=args.scenario)
    if args.json:
        print(json.dumps(res, indent=2))
    else:
        print(f"scenario={args.scenario} days={args.days} villagers={args.villagers} seed={args.seed}")
        print(f"avg needs: {res['avg_needs']}")
        print(f"faith start/end: {res['faith_hist'][0]} -> {res['faith_hist'][-1]}")
        print(f"actions: {res['actions']} shrines={res['shrines']} reacted={res['reacted']}")
        print("PASS: village alive and reactive" if res["pass"] else "FAIL: tuning needed (dead or apathetic)")
    raise SystemExit(0 if res["pass"] else 1)


if __name__ == "__main__":
    main()
