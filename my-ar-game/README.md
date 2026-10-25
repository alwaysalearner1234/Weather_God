# IWSDK App — Tabletop Weather God

This project uses `iwsdk.config.json` for declarative scene, asset, component,
XR, and emulator configuration. Application systems remain explicit in
`src/index.ts`.

```sh
npm install
npm run dev
```

Use the Runtime and Editor controls in the managed browser to switch between
the running experience and its authored scene.

## Weather Gesture System

The app includes a hand-tracking gesture system for four weather actions:

| Gesture | Action | Detection |
|---------|--------|-----------|
| **Sun** | Rub palms together | Both palms facing, <8cm apart, oscillating for 0.5s |
| **Rain** | Pinch + flick down | Pinch held 0.1-2s, then fast downward release |
| **Wind** | Sweep flat hand | Flat palm, fingers extended, lateral velocity >0.3 m/s for 0.5s |
| **Calm** | Palm down, slow down | Palm facing down, slow downward motion for 0.5s |

All gestures work within a 60cm seated radius. Cooldown: 0.8s between triggers.

### Architecture

- `src/gesture-component.ts` — ECS components for gesture state and per-hand tracking
- `src/gesture-system.ts` — `WeatherGestureSystem` detects gestures from hand tracking data
- `src/simulation-integration.ts` — Connects gestures to sim-core (deterministic simulation)

### Integration with sim-core

The gesture system emits `weather-gesture` events that `SimulationIntegrationSystem` consumes to drive the authoritative `sim-core` simulation (see `../sim-core/`). The simulation runs headless and is fully deterministic given a seed + action list.

### Testing

```bash
# Run sim-core tests (18 tests, all green)
cd ../sim-core && npm test

# 30-day balance report
cd ../sim-core && node scripts/balance.js

# Per-action scenario evidence
cd ../sim-core && node scripts/scenarios.js
```

## Starter content

The robot and welcome panel are small examples of authored scene content plus
runtime systems. The robot turns toward the player's head and plays a sound
when pressed. To remove the robot, delete its scene node, its `RobotSystem`
registration from `src/index.ts` or `src/index.js`, and its `Robot` registration
from `src/components.ts` or `src/components.js`; you can then delete the unused
robot component and system files. To remove the welcome panel, delete its scene
node and its `PanelSystem` registration from the application entry point.

- Minimal scene walkthrough: https://iwsdk.dev/guides/01b-minimal-scene.html
- XR-enabled projects — IWER emulator controls: https://iwsdk.dev/guides/02-testing-experience.html#iwer-controls
