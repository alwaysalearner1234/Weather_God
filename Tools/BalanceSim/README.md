# BalanceSim v0

Python mirror of `Assets/Scripts/VillagerAI/*.cs`. Tune here, then port
numbers to C# (keep in sync — same constants, same order of operations).

## Run

```bash
python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7            # random
python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7 --scenario kind
python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7 --scenario cruel
```

Exit 0 = PASS (alive + reactive), 1 = FAIL.

## What PASS means (v0)

- Alive: avg food > 5, water > 5, warmth/safety > 0 after 30 days
  (faith excluded — faith 0 is rebellion, not death).
- Reactive: pray/complain/shelter/shrine/protest/rebel > 0 AND ≥2 distinct
  action types used.

## Current tuning (Oct 2026, verified)

| seed | scenario | result | notes |
|------|----------|--------|-------|
| 1,2,3,7,42 | random | PASS | thriving, shrines built, faith 60–90 |
| 7 | kind | PASS | survives, shelters |
| 7 | cruel | PASS | survives via shelter, faith ~64 |
| forced sun 0.95 x30 | drought | complains/protests/rebels | negative path works |

Design intent: mild random weather = village thrives (pray/shrine/work);
sustained extreme (30-day drought) = complaints → protests → rebellion.
Too much of anything hurts someone, but random play never insta-dies.

## Tuning knobs (in order of sensitivity)

1. `apply_weather` deltas + `ApplyPassiveDrift` (-2 food/-2 water per day).
2. `choose_action` urgency: work bonus when lowest need < 30; prayer capped
   at 25 when food/water < 25 (work before prayer).
3. `ApplyActionOutcome`: work +9 food/+1 water, pray +4 faith, shrine +10.
4. Protest gate: village faith < 35 for 2+ days. Rebel gate: faith < 20 for 4+ days.
5. Memory valence x3 so relationships actually move (shrines need rel > 25).
