'use strict';

/**
 * Memory (doc 01 §6.5). NOT a log: only salient events that can change later
 * behaviour or the recap. Bounds: MEMORY_CAP per villager (4). Salience
 * decays daily; major events decay slower. Duplicates merge on
 * type|target|day. Memory changes the game three ways: line selection,
 * trust-weighted utility, visible behaviour (handled by consumers).
 */
const { MEMORY_CAP } = require('./schema');
const { emit } = require('./environment');

const SALIENCE_FLOOR = 0.05;

function salienceFor(impact) {
  const a = Math.abs(impact) / 100;
  if (a < 0.15) return 0.25;
  if (a < 0.4) return 0.55;
  if (a < 0.7) return 0.8;
  return 1.0;
}

/**
 * Consider a hazard/env event for a villager. impact -100..100 signed
 * (negative = harm). Records when |impact| >= 15.
 */
function observe(state, v, sourceEvent, kind, target, impact) {
  if (Math.abs(impact) < 15) return null;
  const key = kind + '|' + target + '|d' + state.day;
  const existing = v.memories.find((m) => m.key === key);
  const sal = salienceFor(impact);
  if (existing) {
    if (sal > existing.salience) {
      existing.salience = sal;
      existing.initial = sal;
      existing.impact = impact;
    }
    return existing;
  }
  const mem = {
    id: 'm' + state.day + '-' + v.id + '-' + v.memories.length,
    key,
    kind,
    target,
    impact,
    salience: sal,
    initial: sal,
    major: Math.abs(impact) >= 60,
    sourceEvent: sourceEvent.id,
    day: state.day,
  };
  v.memories.push(mem);
  // Bound: drop the lowest-salience memory.
  if (v.memories.length > MEMORY_CAP) {
    let drop = 0;
    for (let i = 1; i < v.memories.length; i++) {
      if (v.memories[i].salience < v.memories[drop].salience) drop = i;
    }
    v.memories.splice(drop, 1);
  }
  // Trust moves with remembered harm/help (long-term attitude).
  v.trust = Math.max(-100, Math.min(100, v.trust + impact * sal * 0.1));
  emit(state, 'memory', kind, v.id, 0, Math.round(sal * 100) / 100, sourceEvent.id);
  return mem;
}

/** Daily decay; drops memories below the floor. */
function decay(v) {
  for (const m of v.memories) {
    m.salience *= m.major ? 0.95 : 0.85;
  }
  v.memories = v.memories.filter((m) => m.salience >= SALIENCE_FLOOR);
}

module.exports = { observe, decay, salienceFor };
