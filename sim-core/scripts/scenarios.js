'use strict';

/**
 * Scripted per-action scenarios (Person C test evidence, doc 03 §5).
 * One scenario per weather action showing env change + villager response.
 * Usage: node scripts/scenarios.js
 */
const { createSession, playerAction, tick, endDay } = require('../src/session');
const { validateChronicle } = require('../src/chronicle');

function run(name, steps) {
  const s = createSession(name.length * 77 + 3);
  for (const [a, r, i] of steps) {
    playerAction(s, a, r, i);
    tick(s, 6);
  }
  const { chronicle } = endDay(s);
  const v = validateChronicle(chronicle, s.state.events);
  const decisions = [...new Set(s.state.events.filter((e) => e.type === 'decision').map((e) => e.variable))];
  console.log(
    (v.ok ? 'PASS' : 'FAIL') + ' ' + name +
    ' | decisions: ' + decisions.join(',') +
    ' | ' + chronicle
  );
  return v.ok;
}

let ok = true;
ok = run('sun-recovers-cold', [['sun', 'field', 0.7], ['sun', 'field', 0.6]]) && ok;
ok = run('rain-greens-then-calm', [['rain', 'field', 0.5], ['calm', 'island', 0.5]]) && ok;
ok = run('heavy-rain-floods-then-calm', [['rain', 'low-hut', 0.95], ['calm', 'island', 0.5]]) && ok;
ok = run('wind-steers-clouds', [['wind', 'island', 0.8], ['calm', 'island', 0.5]]) && ok;
process.exit(ok ? 0 : 1);
