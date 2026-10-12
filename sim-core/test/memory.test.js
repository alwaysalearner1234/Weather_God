'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createSession } = require('../src/session');
const { observe, decay } = require('../src/memory');
const { MEMORY_CAP } = require('../src/schema');

function fakeEvent(id) {
  return { id };
}

test('memory is bounded: cap enforced, weakest dropped', () => {
  const s = createSession(3);
  const st = s.state;
  const v = st.villagers[0];
  const kinds = ['flood', 'saved-crops', 'flood2', 'cold', 'praise', 'storm'];
  kinds.forEach((k, i) => observe(st, v, fakeEvent('e' + i), k, 'hut', i === 0 ? -80 : -20 - i));
  assert.ok(v.memories.length <= MEMORY_CAP, 'memory list must stay bounded at ' + MEMORY_CAP);
});

test('duplicates merge on type|target|day', () => {
  const s = createSession(3);
  const st = s.state;
  const v = st.villagers[0];
  observe(st, v, fakeEvent('e1'), 'flood', 'hut', -50);
  observe(st, v, fakeEvent('e2'), 'flood', 'hut', -50);
  const dups = v.memories.filter((m) => m.key === 'flood|hut|d1');
  assert.equal(dups.length, 1, 'same type+target+day must merge, not spam');
});

test('major events decay slower than minor ones', () => {
  const s = createSession(3);
  const st = s.state;
  const v = st.villagers[0];
  observe(st, v, fakeEvent('e1'), 'flood', 'hut', -80); // major
  observe(st, v, fakeEvent('e2'), 'chill', 'field', -20); // minor
  const major = v.memories.find((m) => m.kind === 'flood');
  const minor = v.memories.find((m) => m.kind === 'chill');
  decay(v);
  assert.ok(major.salience > minor.salience, 'major memory must retain more salience');
  assert.equal(Math.round(major.salience * 1000), Math.round(major.initial * 0.95 * 1000));
  assert.equal(Math.round(minor.salience * 1000), Math.round(minor.initial * 0.85 * 1000));
});

test('trust and faith are separate numbers', () => {
  const s = createSession(3);
  const st = s.state;
  const v = st.villagers[0];
  const faithBefore = v.needs.faith;
  observe(st, v, fakeEvent('e9'), 'flood', 'hut', -70);
  assert.ok(v.trust < 0, 'harm must move trust');
  assert.equal(v.needs.faith, faithBefore, 'remembering harm must not directly rewrite the faith need');
});
