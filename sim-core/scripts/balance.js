'use strict';

/**
 * 30-day balance report (Person C deliverable). Headless, seeded.
 * Usage: node scripts/balance.js [seed]
 */
const { createRng } = require('../src/rng');
const { createSession, playerAction, tick, endDay, villageFaithOf } = require('../src/session');

const seed = parseInt(process.argv[2] || '20261010', 10);
const rng = createRng(seed);
const s = createSession(seed);
const actions = ['sun', 'rain', 'wind', 'calm'];
const counts = {};
const faithTrack = [];

for (let day = 0; day < 30; day++) {
  for (let k = 0; k < 4; k++) {
    const a = rng.pick(actions);
    counts[a] = (counts[a] || 0) + 1;
    playerAction(s, a, 'field', rng.range(0.3, 0.9));
    tick(s, 6);
  }
  const decisions = {};
  for (const v of s.state.villagers) decisions[v.action] = (decisions[v.action] || 0) + 1;
  faithTrack.push(Math.round(villageFaithOf(s.state) * 10) / 10);
  const { chronicle } = endDay(s);
  if (day % 10 === 9) console.log('day ' + (day + 1) + ' faith=' + faithTrack[faithTrack.length - 1] + ' | ' + chronicle);
}

console.log('\nweather actions:', JSON.stringify(counts));
const acts = {};
for (const v of s.state.villagers) acts[v.action] = (acts[v.action] || 0) + 1;
console.log('final villager actions:', JSON.stringify(acts));
console.log('faith start/end:', faithTrack[0], '->', faithTrack[faithTrack.length - 1]);
console.log('milestones:', JSON.stringify(s.state.milestones));
console.log('events logged:', s.state.events.length);
for (const v of s.state.villagers) {
  console.log(
    v.name + ' needs=' + JSON.stringify(v.needs, (k, x) => (typeof x === 'number' ? Math.round(x) : x)) +
    ' trust=' + Math.round(v.trust) + ' memories=' + v.memories.length
  );
}
