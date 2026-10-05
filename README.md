# Tabletop Weather God

Seated, hands-only god-game for Quest. One session = one in-game day (5–8 min).
Lidiya owns: villager AI (needs / memory / decisions), dialogue + chronicle,
save + time-away. Game dev owns: hands, island, builds.

## Week 1 done when

- All 4 gestures trigger in headset (game dev side).
- Needs + memory data model in C#, testable without headset (this repo).
- Balance simulator v0 runs 30 days of random weather without instant
  collapse or total apathy (see `Tools/BalanceSim/`).

## Layout

```text
Assets/Scripts/VillagerAI/   plain C# (no headset deps), Unity-ready
  Enums.cs                    WeatherType, VillagerAction, Mood, Era
  VillagerNeeds.cs            5 needs 0-100 + weather effects (+WithIsland path)
  VillagerMemory.cs           event log, salience decay, relationship, opinions
  Villager.cs                 personality + needs + memory + tick (+TickWithIsland)
  UtilityAI.cs                on-device action scoring
  VillageState.cs             faith, era, growth, low-faith tracking
  IslandState.cs              crops/river/fire/huts/temple, ticked ONCE per step
  ThoughtIcon.cs              need -> thought-icon sprite mapping for UI
  VillagerDirector.cs         action -> movement intent (wander/work/shelter/...)
  DailyPrayer.cs              session-opening request picker + fulfilment check
  GameSession.cs              composer: island+villagers+prayer+day flow
  Dialogue/FallbackLines.cs   offline line bank (game never waits on net)
  Dialogue/ChronicleGenerator.cs  offline template chronicle
  Dialogue/DialogueService.cs LLM proxy contract + fallback service + prompts
  Dialogue/ContentFilter.cs   length + blocklist guards, 1500 ms timeout rule
  Dialogue/Prompts/*.txt      LLM prompt templates (proxy only, never in APK key)
  Save/VillageSave.cs         JSON DTOs for persistentDataPath
  Save/SaveManager.cs         file store abstraction + restore (testable w/o Unity)
  Save/TimeAway.cs            resume fast-forward (cap 3 days) + away summary
Tools/BalanceSim/sim.py       30-day weather balance sim (Phase 1 gate)
Tools/BalanceSim/checks2.py   21 phase-2 checks: island, icons, prayer, save, filter
```

Core rule (take-it-away test): everything playable runs on-device.
The LLM only adds words. Wi-Fi off = fallback lines, full game.

## Quick start (simulator, no Unity needed)

```bash
python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7
python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7 --scenario cruel
python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7 --scenario kind
```

Pass criteria v0: after 30 random days, village still alive
(avg food/water > 0, not all needs pinned at 0/100) AND villagers reacted
(shrines+prays+complains+protests > 0). The script exits non-zero if not.

## Unity wiring (later weeks)

- Attach a MonoBehaviour wrapper that calls `GameSession.ApplyWeather(...)`
  every ~2–3 s (not every frame) and `GameSession.EndDay()` at day end.
- Map `ThoughtIcons.GetIcon(...)` to head sprites, `VillagerDirector.NextIntent(...)`
  slot indices to world anchors (fields/shore/shelter/shrine/square).
- Save via `SaveManager(new FileStore(Application.persistentDataPath),
  JsonUtility.ToJson, j => JsonUtility.FromJson<VillageSave>(j))` on day end
  + OnApplicationPause. On resume: `AwayDays()` → `TimeAway.Simulate(...)`.
- LLM later: POST filled prompts to Lambda proxy (key server-side only),
  1500 ms timeout → `FallbackDialogueService` + `ContentFilter`.

## Verify (no Unity / no dotnet needed)

```bash
python Tools/BalanceSim/sim.py --days 30 --villagers 8 --seed 7
python Tools/BalanceSim/checks2.py   # 21/21: island, icons, prayer, save, filter
```
