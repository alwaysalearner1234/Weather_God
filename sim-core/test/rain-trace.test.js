'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createSession, playerAction, tick, endDay } = require('../src/session');
const { validateChronicle } = require('../src/chronicle');

// Doc 01 §4.2 worked example: rain -> crops -> flood -> shelter -> calm -> chronicle.
test('rain trace: gentle rain greens the field', () => {
  const s = createSession(7);
  const before = s.state.env.crops;
  playerAction(s, 'rain', 'field', 0.5);
  tick(s, 3);
  assert.ok(s.state.env.crops > before, 'crops should rise after gentle rain');
  const envEv = s.state.events.find(
    (e) => e.type === 'env' && e.variable === 'crops' && e.newV > e.oldV
  );
  assert.ok(envEv, 'an env event must record the crop change (no cosmetic-only weather)');
});

test('rain trace: sustained heavy rain floods, shelter follows with reason', () => {
  const s = createSession(7);
  const intBefore = s.state.env.integrity;
  playerAction(s, 'rain', 'low-hut', 0.95);
  const flood = s.state.events.find((e) => e.type === 'hazard' && e.variable === 'flood');
  assert.ok(flood, 'heavy rain must emit a flood hazard event');
  assert.ok(s.state.env.integrity < intBefore, 'flood must damage structures');
  tick(s, 6); // let decisions run
  const shelters = s.state.events.filter(
    (e) => e.type === 'decision' && e.variable === 'shelter'
  );
  assert.ok(shelters.length >= 1, 'someone must seek shelter');
  for (const sh of shelters) {
    assert.ok(['warmth', 'safety'].includes(sh.reason), 'shelter needs a reason tag');
  }
});

test('rain trace: calm stops escalation but damage remains', () => {
  const s = createSession(7);
  const preFlood = s.state.env.integrity;
  playerAction(s, 'rain', 'low-hut', 0.95);
  const damaged = s.state.env.integrity;
  assert.ok(damaged < preFlood, 'flood must damage structures first');
  playerAction(s, 'calm', 'island', 0.5);
  assert.equal(s.state.weather.mode, 'clear', 'calm clears the weather');
  tick(s, 3);
  assert.ok(
    s.state.env.integrity < preFlood,
    'calm must not erase flood damage (consequences persist)'
  );
  assert.ok(
    s.state.env.integrity <= damaged + 1.0,
    'only slow upkeep recovery allowed, no full repair: ' + s.state.env.integrity + ' vs ' + damaged
  );
});

test('rain trace: chronicle references only real events', () => {
  const s = createSession(7);
  playerAction(s, 'rain', 'field', 0.5);
  tick(s, 6);
  playerAction(s, 'calm', 'island', 0.5);
  tick(s, 3);
  const { chronicle } = endDay(s);
  assert.ok(chronicle.length > 0);
  const v = validateChronicle(chronicle, s.state.events);
  assert.ok(v.ok, 'every chronicle line must reference a real event, missing: ' + v.missing);
});
