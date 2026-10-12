'use strict';

/**
 * Needs follow the environment (doc 01 §4.4, §6.1). Four needs:
 * sustenance <- crops (+ moisture), warmth <- envWarmth (+ shelter),
 * safety <- integrity (+ flood/wind), faith <- prayer/answered requests.
 * Fixed-step, clamped 0..100. No death in the MVP.
 */
const { clamp } = require('./schema');

function stepNeeds(v, env, sheltered, dt) {
  const n = v.needs;
  // Sustenance tracks crop condition; dry soil drags it down.
  n.sustenance += ((env.crops - 50) * 0.12 - 3.5) * dt;
  if (env.moisture < 20) n.sustenance -= 4 * dt;
  // Warmth tracks local warmth; shelter halves exposure.
  const exposure = sheltered ? 0.5 : 1;
  n.warmth += ((env.envWarmth - 50) * 0.15) * exposure * dt;
  n.warmth += (50 - n.warmth) * 0.03 * dt;
  if (sheltered) n.warmth += 3 * dt;
  // Safety tracks structure integrity; floods/wind hurt until repaired.
  n.safety += ((env.integrity - 60) * 0.08 - 0.5) * dt;
  if (sheltered) n.safety += 4 * dt;
  // Faith drifts to 50; prayer and answered requests lift it (decisions.js).
  n.faith += (50 - n.faith) * 0.02 * dt;
  if (n.sustenance < 30 || n.warmth < 30 || n.safety < 30) n.faith -= 1.2 * dt;

  for (const k of ['sustenance', 'warmth', 'safety', 'faith']) n[k] = clamp(n[k]);
  return n;
}

/** Lowest need name + value (for requests and icons). */
function lowestNeed(v) {
  let name = 'sustenance';
  let val = v.needs.sustenance;
  for (const k of ['warmth', 'safety']) {
    if (v.needs[k] < val) { name = k; val = v.needs[k]; }
  }
  if (v.needs.faith < val && v.needs.faith < 40) { name = 'faith'; val = v.needs.faith; }
  return { name, value: val };
}

module.exports = { stepNeeds, lowestNeed };
