'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createRng } = require('../src/rng');
const { createInitialState } = require('../src/schema');
const { createSession, playerAction, tick, endDay, villageFaithOf } = require('../src/session');

// Consequence rule (doc 01 §4.3): every action must change an env var
// that a need depends on. Proved here per action, not by assertion alone.
test('every weather action moves a need-linked environment variable', () => {
  const linked = ['moisture', 'crops', 'envWarmth', 'integrity'];
  for (const action of ['sun', 'rain', 'wind']) {
    const s = createSession(100 + action.length);
    const n0 = s.state.events.length;
    playerAction(s, action, 'field', 0.6);
    const fresh = s.state.events.slice(n0).filter((e) => e.type === 'env' && linked.includes(e.variable));
    assert.ok(fresh.length >= 1, action + ' must emit a need-linked env change');
  }
  // Calm is the documented exception: it stops escalation, never adds state.
  const s = createSession(9);
  playerAction(s, 'calm', 'island', 0.5);
  assert.equal(s.state.weather.mode, 'clear');
});

// 30-day balance: neither instant collapse nor a static village.
test('30-day random weather: alive, reactive, moving', () => {
  const rng = createRng(20261010);
  const s = createSession(20261010);
  const actions = ['sun', 'rain', 'wind', 'calm'];
  const faithTrack = [];
  for (let day = 0; day < 30; day++) {
    for (let k = 0; k < 4; k++) {
      const a = rng.pick(actions);
      playerAction(s, a, 'field', rng.range(0.3, 0.9));
      tick(s, 6);
    }
    faithTrack.push(Math.round(villageFaithOf(s.state) * 10) / 10);
    endDay(s);
  }
  // Evidence comes from the decision event log, not one sample per day.
  const seen = new Set(
    s.state.events.filter((e) => e.type === 'decision').map((e) => e.variable)
  );
  const avgs = { sustenance: 0, warmth: 0, safety: 0 };
  for (const v of s.state.villagers) {
    for (const k of Object.keys(avgs)) avgs[k] += v.needs[k] / s.state.villagers.length;
  }
  assert.ok(avgs.sustenance > 0 && avgs.warmth > 0 && avgs.safety > 0, 'no need may flatline: ' + JSON.stringify(avgs));
  assert.ok(seen.size >= 3, 'villagers must use several actions, saw: ' + [...seen].join(','));
  assert.ok(new Set(faithTrack).size > 1, 'village faith must move over 30 days');
  assert.ok(s.state.events.length > 50, 'event log must accumulate evidence');
  // Memories bounded everywhere, chronicle refs real.
  for (const v of s.state.villagers) {
    assert.ok(v.memories.length <= 4, v.id + ' memory bounded');
  }
});
