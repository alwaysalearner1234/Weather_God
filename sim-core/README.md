# sim-core — Person C deliverable: deterministic simulation core

Plain JavaScript. **Zero engine imports** (no Three.js, no browser APIs), so it
runs headless under `node --test` and inside any IWSDK adapter. The simulation
is authoritative; presentation subscribes and never writes back.

## Run

```bash
npm test            # 18 headless tests (scenarios, memory, persistence, balance)
node scripts/scenarios.js   # per-action scenario evidence (Rain/Calm/Wind/Sun)
node scripts/balance.js [seed]  # 30-day balance report
```

## Layout

```text
src/
  rng.js          seeded randomness (sole source)
  schema.js       state schema v1, 4 needs, trust, bounds, validation
  environment.js  weather actions -> env vars + hazard events (fire excluded)
  needs.js        env -> sustenance/warmth/safety/faith (no death in MVP)
  decisions.js    utility scoring, reason tags, cooldowns, deterministic ties
  memory.js       bounded cap-4 salience memory, dup merge, trust updates
  chronicle.js    event-log-only recap, every line cites a real event id
  lines.js        authored line bank + chronicle templates
  persistence.js  two-slot snapshot codec, checksum, fallback, reset
  timeAway.js     positive-only bounded return summary (cap 3 days)
  session.js      composer: actions, fixed-step ticks, day end, requests
```

## Contracts for Person B (doc 02 §3, proposed v0)

- **Input action event**: `{action: sun|rain|wind|calm, region?, intensity 0..1}` →
  `playerAction(session, action, region, intensity)`. Inspect is non-weather
  and never touches state.
- **Decision event**: `{type:'decision', variable, region: villagerId, reason}` —
  subscribe for animation/VFX. Reasons: `sustenance|warmth|safety|faith|trust|idle`.
- **Save snapshot**: `encode(state)` / `decode(snapshot)` / `memoryStore()` or
  `localStorageStore(key, localStorage)`. Schema v1, checksum, fallback slot.
- **Time away**: `awayDays(elapsed)` + `fastForward(state, days)` — benign only.

## Verified balance (6 seeds, 30 random-weather days each)

Alive in all runs; 4–5 distinct actions per run (tend/shelter/pray/rest, plus
fetchWater and complain where earned); faith moves; milestones unlock.
Failure modes found and fixed by the suite: day-clock cooldown freeze,
moisture drainage, damage-vs-upkeep ratio, deficit-only work model.

## Deliberately deferred (doc 01 §8)

Fire system, rebellion/eras/extra powers, villager opinion graph, generative
text (line bank + templates only), cold-start-offline claim.
