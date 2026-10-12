'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createSession, playerAction, tick } = require('../src/session');
const { encode, decode, memoryStore } = require('../src/persistence');

test('save roundtrip preserves state', () => {
  const s = createSession(5);
  playerAction(s, 'rain', 'field', 0.5);
  tick(s, 6);
  const snap = encode(s.state);
  const back = decode(JSON.parse(JSON.stringify(snap)));
  assert.ok(back.ok, 'valid snapshot must decode');
  assert.equal(back.state.day, s.state.day);
  assert.equal(back.state.villagers.length, s.state.villagers.length);
  assert.equal(back.state.villagers[0].needs.sustenance, s.state.villagers[0].needs.sustenance);
});

test('corrupt snapshot fails closed, two-slot store falls back', () => {
  const s = createSession(5);
  tick(s, 3);
  const store = memoryStore();
  const good = store.write(encode(s.state)); // eslint-disable-line no-unused-vars
  playerAction(s, 'sun', 'field', 0.8);
  tick(s, 3);
  store.write(encode(s.state));
  // Corrupt the newest slot in place (emulates disk corruption).
  const newestSlot = store.read(0).version > store.read(1).version ? 0 : 1;
  store.slots[newestSlot].body = store.slots[newestSlot].body.replace(/[0-9]/, 'X');
  const loaded = store.loadLatest();
  assert.ok(loaded.ok, 'must fall back to the last valid slot');
  assert.ok(loaded.slot !== newestSlot, 'fallback must not be the corrupt slot');
});

test('wrong schema version is rejected, not migrated silently', () => {
  const s = createSession(5);
  const snap = encode(s.state);
  snap.schema = 999;
  const back = decode(snap);
  assert.ok(!back.ok && back.reason === 'schema-mismatch');
});
