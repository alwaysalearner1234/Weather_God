'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createSession, playerAction, tick, endDay, stateHash } = require('../src/session');

function script(seed) {
  const s = createSession(seed);
  const actions = [
    ['rain', 'field', 0.5], ['sun', 'field', 0.6], ['wind', 'island', 0.4],
    ['rain', 'low-hut', 0.9], ['calm', 'island', 0.5], ['sun', 'field', 0.7],
  ];
  for (const [a, r, i] of actions) {
    playerAction(s, a, r, i);
    tick(s, 6);
  }
  endDay(s);
  playerAction(s, 'rain', 'field', 0.4);
  tick(s, 6);
  endDay(s);
  return stateHash(s.state);
}

test('same seed + same actions = identical state (deterministic)', () => {
  assert.equal(script(42), script(42));
});

test('different seeds diverge', () => {
  assert.notEqual(script(42), script(43));
});
