'use strict';

/**
 * Utility decisions (doc 01 §6.3). Every DECISION_EVERY ticks each villager
 * scores eligible actions and picks the best. Eligibility + min-duration +
 * cooldowns prevent jitter; ties break by fixed order (deterministic).
 * Every choice carries a reason tag for debug and UI.
 *
 * Minimum action set: tend, shelter, pray, rest, complain (+ fetchWater,
 * the documented "should"). Ambient reactions (shiver/shade/lean) are
 * presentation-only and live outside this module.
 */

const { emit } = require('./environment');

const DECISION_EVERY = 3;
const MIN_DURATION = 2; // decisions, not ticks
const ORDER = ['shelter', 'tend', 'fetchWater', 'pray', 'complain', 'rest'];

function stormNow(state) {
  const w = state.weather;
  return (w.mode === 'rain' && w.intensity > 0.7)
    || (w.mode === 'wind' && w.intensity > 0.7);
}

function score(v, action, state) {
  const n = v.needs;
  const w = v.weights;
  const storm = stormNow(state);
  switch (action) {
    case 'shelter': {
      if (n.safety >= 70 && n.warmth >= 55 && !storm) return -100;
      let s = (70 - n.safety) * 1.2 + (55 - Math.min(n.warmth, 55)) * 1.0;
      if (storm) s += 35;
      return s + w.cautious * 20;
    }
    case 'tend': {
      // Farmers/fishers work their craft: on personal need, or when the
      // field itself needs work (routine maintenance, not just deficit).
      if (storm) return -100;
      if (v.craft !== 'farmer' && v.craft !== 'fisher') return -100;
      const deficit = (70 - n.sustenance) * 1.2;
      const field = (65 - state.env.crops) * 0.9;
      const s = Math.max(deficit, field);
      if (s <= 0) return -100;
      return s - w.cautious * 5;
    }
    case 'fetchWater': {
      if (n.sustenance >= 55 || state.env.moisture >= 35) return -100;
      return (55 - n.sustenance) * 1.1 + (35 - state.env.moisture) * 0.5;
    }
    case 'pray': {
      // Routine devotion below comfort, urgent below 60; devout villagers
      // pray more often. Keeps prayer in the daily rhythm, not only crisis.
      if (n.faith >= 75) return -100;
      return (75 - n.faith) * 0.9 + w.devout * 20 + Math.max(0, v.trust) * 0.2;
    }
    case 'complain': {
      if (v.trust >= 0 && n.safety >= 40 && n.warmth >= 40) return -100;
      let s = (40 - Math.min(n.safety, n.warmth)) * 1.0 + Math.max(0, -v.trust) * 0.4;
      return s - w.stubborn * 5 + (v.trust < -20 ? 15 : 0);
    }
    case 'rest':
    default:
      return 5; // default when nothing presses
  }
}

/** Apply the chosen action's effects (needs + env + faith). Returns side-effect notes. */
function applyEffects(state, v, action) {
  const notes = [];
  if (action === 'tend') {
    v.needs.sustenance = Math.min(100, v.needs.sustenance + 6);
    state.env.crops = Math.min(100, state.env.crops + 1);
    notes.push('tended crops');
  } else if (action === 'fetchWater') {
    v.needs.sustenance = Math.min(100, v.needs.sustenance + 5);
    notes.push('fetched water');
  } else if (action === 'pray') {
    v.needs.faith = Math.min(100, v.needs.faith + 5);
    notes.push('prayed');
  } else if (action === 'shelter') {
    notes.push('sheltered');
  } else if (action === 'complain') {
    v.needs.faith = Math.max(0, v.needs.faith - 0.5);
    notes.push('complained');
  }
  return notes;
}

function reasonFor(action, v, state) {
  switch (action) {
    case 'shelter': return v.needs.safety < v.needs.warmth ? 'safety' : 'warmth';
    case 'tend':
    case 'fetchWater': return 'sustenance';
    case 'pray': return 'faith';
    case 'complain': return 'trust';
    default: return 'idle';
  }
}

/**
 * Run decisions for all villagers when due (state.clock % DECISION_EVERY === 0).
 * Respects min-duration locks and cooldowns. Emits decision events.
 * Locks/cooldowns use state.dseq, a monotonic decision counter that NEVER
 * resets (the day clock does), so a new day cannot freeze villagers.
 */
function decideAll(state) {
  if (state.clock % DECISION_EVERY !== 0) return [];
  state.dseq = (state.dseq || 0) + 1;
  const now = state.dseq;
  const out = [];
  for (const v of state.villagers) {
    const held = v.actionUntil > now;
    const cd = v.cooldowns;
    if (held) {
      applyEffects(state, v, v.action);
      continue;
    }
    let best = 'rest';
    let bestScore = -Infinity;
    for (const a of ORDER) {
      if (cd[a] != null && cd[a] > now) continue;
      const s = score(v, a, state);
      if (s > bestScore) { bestScore = s; best = a; }
    }
    if (best !== v.action) {
      v.cooldowns[v.action] = now + 1; // leaving an action cools it down
      v.action = best;
      v.actionUntil = now + MIN_DURATION;
      v.actionReason = reasonFor(best, v, state);
      const ev = emit(state, 'decision', best, v.id, 0, Math.round(bestScore * 10) / 10, 'tick');
      ev.reason = v.actionReason;
      out.push(ev);
    }
    applyEffects(state, v, v.action);
  }
  return out;
}

module.exports = { DECISION_EVERY, decideAll, score, stormNow };
