'use strict';

/**
 * Return visit (doc 01 §3.2, decision D-04: positive-only and bounded).
 * The MVP never destroys or removes anything because the player was absent:
 * summarise prior events and allow limited benign developments (a crop
 * recovers, a repair is made). Capped at 3 days. Judges get a labelled
 * sample state + in-game "next day" step instead of real elapsed time.
 */
const { clamp } = require('./schema');

const MAX_AWAY_DAYS = 3;

function awayDays(realDaysElapsed) {
  const d = Math.floor(realDaysElapsed);
  if (d <= 0) return 0;
  return Math.min(MAX_AWAY_DAYS, d);
}

/** Benign-only fast-forward: crops recover slightly, integrity repaired. */
function fastForward(state, days) {
  const n = Math.min(MAX_AWAY_DAYS, Math.max(0, days));
  const lines = [];
  if (n === 0) return { days: 0, lines };
  const cropsBefore = state.env.crops;
  const intBefore = state.env.integrity;
  state.env.crops = clamp(state.env.crops + 4 * n);
  state.env.integrity = clamp(state.env.integrity + 5 * n);
  for (const v of state.villagers) {
    v.needs.faith = clamp(v.needs.faith + 1 * n);
  }
  lines.push('While you were away (' + n + ' day' + (n > 1 ? 's' : '') + '):');
  if (state.env.crops > cropsBefore) lines.push('The fields recovered a little.');
  if (state.env.integrity > intBefore) lines.push('A neighbour patched the low hut.');
  lines.push('The village endures.');
  return { days: n, lines };
}

/** Clearly labelled sample island for judges (never presented as earned play). */
function sampleStateLabel() {
  return 'SAMPLE ISLAND — a pre-made demonstration state, not earned play.';
}

module.exports = { MAX_AWAY_DAYS, awayDays, fastForward, sampleStateLabel };
