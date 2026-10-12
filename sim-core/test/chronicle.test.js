'use strict';

const { test } = require('node:test');
const assert = require('node:assert/strict');
const { validateChronicle } = require('../src/chronicle');

test('chronicle validator catches invented events', () => {
  const events = [{ id: 'e1' }, { id: 'e2' }];
  assert.ok(validateChronicle('Rain fell [e1] and all was well [e2].', events).ok);
  const bad = validateChronicle('A dragon burned the hut [e99].', events);
  assert.ok(!bad.ok, 'a line referencing a non-event must fail validation');
  assert.deepEqual(bad.missing, ['e99']);
});
