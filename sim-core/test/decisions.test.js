'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createSession, tick, playerAction } = require('../src/session');
const { score } = require('../src/decisions');

// Doc 01 §6.4: the same event yields different sensible actions from different states.
test('same flood, different reactions: devout prays, cautious shelters', () => {
  const s = createSession(11);
  playerAction(s, 'rain', 'low-hut', 0.95); // shared event
  const asha = s.state.villagers.find((v) => v.id === 'v-asha');
  const bram = s.state.villagers.find((v) => v.id === 'v-bram');
  // Arrange distinct stored state: devout + trusting vs cautious + harmed.
  asha.needs.faith = 30;
  asha.trust = 40;
  asha.needs.safety = 75;
  asha.needs.warmth = 70;
  bram.trust = -40;
  bram.needs.safety = 35;
  bram.needs.faith = 50;
  // Storm just passed; clear it so shelter scores come from state, not weather.
  s.state.weather = { mode: 'clear', intensity: 0, direction: 0 };
  const ashaScores = { pray: score(asha, 'pray', s.state), shelter: score(asha, 'shelter', s.state) };
  const bramScores = { pray: score(bram, 'pray', s.state), shelter: score(bram, 'shelter', s.state) };
  assert.ok(ashaScores.pray > ashaScores.shelter, 'devout trusting Asha should favour prayer');
  assert.ok(bramScores.shelter > bramScores.pray, 'harmed cautious Bram should favour shelter');
});

test('no per-tick jitter: chosen action holds for its minimum duration', () => {
  const s = createSession(21);
  playerAction(s, 'rain', 'low-hut', 0.95);
  tick(s, 3); // first decision point
  const first = s.state.villagers.map((v) => v.action);
  tick(s, 3); // second decision point: locks should still hold
  const second = s.state.villagers.map((v) => v.action);
  assert.deepEqual(second, first, 'actions must not flip every decision point');
});
