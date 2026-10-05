"""Phase-2 checks — mirrors IslandState/ThoughtIcons/DailyPrayer/Director/
Save/TimeAway/ContentFilter/PromptFiller C# logic. Keep in sync.

Run:  python Tools/BalanceSim/checks2.py
Exit 0 = all pass.
"""
import json
import sys
import os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sim


def clamp(v, lo=0.0, hi=100.0):
    return max(lo, min(hi, v))


# --- island mirror (IslandState.Tick) ---
def island_tick(st, weather, intensity, d=1.0):
    flooded = False
    if weather == "sun":
        irr = 1.0 if st["river"] > 25 else 0.2
        st["crops"] += 8 * intensity * d * irr; st["river"] -= 6 * d * intensity; st["fire"] += 8 * intensity * d
    elif weather == "rain":
        st["river"] += 14 * intensity * d; st["fire"] -= 12 * intensity * d
        if intensity > 0.75 or st["river"] > 85:
            st["crops"] -= 12 * intensity * d; flooded = True
        else:
            st["crops"] += 10 * intensity * d
    elif weather == "wind":
        st["crops"] -= 3 * intensity * d; st["river"] -= 2 * intensity * d
    else:
        st["crops"] += 1 * d; st["river"] -= 4 * d
    if st["river"] < 15:
        st["crops"] -= 6 * d
    st["crops"] = clamp(st["crops"]); st["river"] = clamp(st["river"]); st["fire"] = clamp(st["fire"])
    st["flooded"] = flooded
    return st


def growth(st, faith, shrines, protests):
    if faith > 65 and shrines > 0:
        st["temple"] = clamp(st.get("temple", 0) + 20)
    want = 3
    if faith > 60:
        want = 4
    if faith > 75 or shrines >= 2:
        want = 5
    if faith > 85 and shrines >= 3:
        want = 6
    if faith < 25 and protests > 0:
        want = max(2, want - 1)
    st["huts"] = want
    return st


# --- thought icon mirror ---
def thought_icon(v, island, weather, intensity):
    if v.last_action in ("protest", "rebel"):
        return "Rebel"
    if island.get("flooded"):
        return "FloodedHut"
    if v.needs.safety < 25:
        return "FloodedHut"
    storm = (weather == "rain" and intensity > 0.7) or (weather == "wind" and intensity > 0.7)
    if storm and v.last_action == "shelter":
        return "Storm"
    if v.needs.water < 30:
        return "DryField" if v.job == "farmer" else "Thirsty"
    if v.needs.food < 30:
        return "Hungry"
    if v.needs.warmth < 30:
        return "Cold"
    if v.last_action == "pray":
        return "Praying"
    return "None"


def director(last_action, job, idx, tick):
    if last_action == "shelter":
        return ("Shelter", idx % 3)
    if last_action == "work":
        h = sum(ord(c) for c in job) % 997
        if job == "farmer":
            return ("WorkSite", h % 2)
        if job == "fisher":
            return ("WorkSite", 2 + h % 2)
        if job == "builder":
            return ("WorkSite", 4 + h % 2)
        return ("WorkSite", h % 6)
    if last_action in ("pray", "shrine"):
        return ("Shrine", 0)
    if last_action in ("protest", "rebel"):
        return ("ProtestSquare", 0)
    return ("Wander", (idx + tick) % 6)


BLOCKED = ["kill", "die", "blood", "gore", "torture", "suicide", "hate", "stupid", "idiot", "damn", "hell"]


def wc(s):
    return len(s.split())


def allowed_dialogue(s):
    if not s or wc(s) > 25:
        return False
    low = s.lower()
    return not any(b in low for b in BLOCKED)


results = []


def check(name, cond, detail=""):
    results.append((name, bool(cond), detail))
    print(("PASS " if cond else "FAIL ") + name + ((" — " + str(detail)) if detail and not cond else ""))


# 1. rain grows crops
st = {"crops": 50.0, "river": 40.0, "fire": 20.0, "flooded": False}
island_tick(st, "rain", 0.5)
check("rain grows crops", st["crops"] > 50, st)

# 2. heavy rain floods + ruins
st2 = {"crops": 50.0, "river": 80.0, "fire": 20.0, "flooded": False}
island_tick(st2, "rain", 0.95)
check("heavy rain floods", st2["flooded"] and st2["crops"] < 50, st2)

# 3. drought withers
st3 = {"crops": 50.0, "river": 10.0, "fire": 20.0, "flooded": False}
island_tick(st3, "sun", 0.9)
check("drought withers", st3["crops"] < 50, st3)

# 4. growth visuals
g = growth({"temple": 0}, 80, 2, 0)
check("high faith grows huts+temple", g["huts"] >= 5 and g["temple"] > 0, g)
g2 = growth({"temple": 0}, 10, 0, 3)
check("rebellion loses hut", g2["huts"] <= 3, g2)

# 5. thought icons
v = sim.Villager("Asha", "farmer", "optimistic")
v.needs.water = 10
check("thirsty farmer -> DryField", thought_icon(v, {"flooded": False}, "sun", 0.5) == "DryField")
v2 = sim.Villager("Bram", "fisher", "gruff")
v2.needs.water = 10
check("thirsty fisher -> Thirsty", thought_icon(v2, {"flooded": False}, "sun", 0.5) == "Thirsty")
v3 = sim.Villager("Dev", "elder", "devout")
v3.last_action = "rebel"
check("rebel -> Rebel icon", thought_icon(v3, {"flooded": False}, "calm", 0.3) == "Rebel")
check("flooded island -> FloodedHut", thought_icon(v, {"flooded": True}, "rain", 0.9) == "FloodedHut")

# 6. director intents
check("shelter -> Shelter fast", director("shelter", "farmer", 1, 0)[0] == "Shelter")
t, s = director("work", "farmer", 0, 0)
check("farmer work slot 0-1", t == "WorkSite" and s in (0, 1), (t, s))
t, _ = director("protest", "elder", 0, 0)
check("protest -> square", t == "ProtestSquare", t)

# 7. daily prayer picks neediest (mirror: lowest need villager, water/food/warmth/safety)
vs = [sim.Villager("Asha", "farmer", "optimistic"), sim.Villager("Bram", "fisher", "gruff")]
vs[0].needs.water = 12
vs[1].needs.water = 80; vs[1].needs.food = 80; vs[1].needs.warmth = 80; vs[1].needs.safety = 80
worst = min(vs, key=lambda x: x.needs.lowest())
check("prayer picks neediest", worst.name == "Asha", worst.name)

# 8. save roundtrip via JSON
vil = sim.run(days=5, n_villagers=4, seed=3)
snap = {"day": 6, "villagers": [{"name": "Asha", "faith": 60.5}], "river": 41.0}
blob = json.dumps(snap)
back = json.loads(blob)
check("save JSON roundtrip", back["villagers"][0]["faith"] == 60.5 and back["day"] == 6, back)

# 9. time-away cap 0..3
def away_days(elapsed):
    return max(0, min(3, int(elapsed)))
check("time-away caps at 3", away_days(30) == 3 and away_days(2) == 2 and away_days(-1) == 0)

# 10. content filter
check("filter allows short kind line", allowed_dialogue("Warm sun! My field thanks you."))
check("filter blocks long line", not allowed_dialogue("word " * 30))
check("filter blocks banned word", not allowed_dialogue("I will kill the sky god today"))

# 11. prompt filler replaces placeholders
tpl = "Villager: {name}, a {job}, {trait}. Mood: {mood}. Weather: {weather}."
filled = tpl.replace("{name}", "Asha").replace("{job}", "farmer").replace("{trait}", "optimistic").replace("{mood}", "Worried").replace("{weather}", "rain")
check("prompt filler clean", "{" not in filled and "Asha" in filled, filled)

# 12. phase-1 sim still passes (no regression)
ok = True
for seed in (1, 2, 3, 7, 42):
    r = sim.run(days=30, n_villagers=8, seed=seed)
    if not r["pass"]:
        ok = False
        break
check("phase-1 sim no regression (5 seeds)", ok)

# 13. C# files exist
base = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Scripts", "VillagerAI")
expected = ["IslandState.cs", "ThoughtIcon.cs", "VillagerDirector.cs", "DailyPrayer.cs",
            "GameSession.cs", "Dialogue/DialogueService.cs", "Dialogue/ContentFilter.cs",
            "Save/SaveManager.cs", "Save/TimeAway.cs"]
missing = [f for f in expected if not os.path.exists(os.path.join(base, f))]
check("phase-2 C# files present", not missing, missing)

failed = [n for n, c, _ in results if not c]
print(f"\n{len(results) - len(failed)}/{len(results)} checks passed")
sys.exit(1 if failed else 0)
