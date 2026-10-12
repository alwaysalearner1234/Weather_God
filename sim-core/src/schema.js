'use strict';

/**
 * State schema (v1). Plain data only — no engine imports.
 * Needs model (doc 01 §6.1, open decision D-09, recommended option):
 *   four needs per villager: sustenance, warmth, safety, faith.
 *   Water lives in the ENVIRONMENT (moisture/waterLevel), not as a need.
 * Per villager: needs, one trust scalar (toward player, long-term),
 * up to three personality weights, current action, bounded memory list.
 * No villager death in the MVP: needs clamp at 0, never mortality.
 */

const SCHEMA_VERSION = 1;
const APP_VERSION = '0.1.0';
const MEMORY_CAP = 4;

const NEEDS = ['sustenance', 'warmth', 'safety', 'faith'];
const ACTIONS = ['sun', 'rain', 'wind', 'calm']; // weather action identifiers (frozen spelling)

function clamp(v, lo = 0, hi = 100) {
  return v < lo ? lo : v > hi ? hi : v;
}

function createVillager(id, name, craft, marker, hope, fear, weights) {
  return {
    id,
    name,
    craft,
    marker, // visual signature, e.g. 'straw hat'
    hope,
    fear,
    weights: Object.assign({ cautious: 0, devout: 0, stubborn: 0 }, weights),
    needs: { sustenance: 70, warmth: 70, safety: 70, faith: 55 },
    trust: 0, // -100..100, long-term attitude to the player
    action: 'rest',
    actionReason: 'idle',
    actionUntil: 0, // min-duration lock (clock ticks)
    cooldowns: {}, // action -> clock tick when eligible again
    memories: [], // bounded, see memory.js
  };
}

function defaultVillagers() {
  return [
    createVillager('v-asha', 'Asha', 'farmer', 'straw hat',
      'a good harvest', 'cold nights', { cautious: 0.2, devout: 0.6, stubborn: 0.1 }),
    createVillager('v-bram', 'Bram', 'fisher', 'blue scarf',
      'full nets', 'flooded hut', { cautious: 0.7, devout: 0.1, stubborn: 0.2 }),
    createVillager('v-sona', 'Sona', 'weaver', 'red bead',
      'a warm hearth', 'storms', { cautious: 0.4, devout: 0.3, stubborn: 0.8 }),
  ];
}

function createInitialState(seed) {
  return {
    schema: SCHEMA_VERSION,
    app: APP_VERSION,
    seed,
    day: 1,
    clock: 0, // fixed-step ticks since day start
    weather: { mode: 'clear', intensity: 0, direction: 0 },
    env: {
      moisture: 50, // soil moisture / water level
      crops: 50, // crop condition
      envWarmth: 60, // local warmth
      integrity: 80, // structure integrity
      cloud: 20, // cloud cover
    },
    villagers: defaultVillagers(),
    villageFaith: 55, // derived aggregate, see session.js
    milestones: { hut4: false, shrine: false, hut5: false },
    request: null, // daily request {villagerId, need, text}
    events: [], // append-only event log with ids e1, e2, ...
    eventSeq: 0,
  };
}

function validateState(s) {
  const errors = [];
  if (!s || typeof s !== 'object') return { ok: false, errors: ['not-an-object'] };
  if (s.schema !== SCHEMA_VERSION) errors.push('schema-mismatch');
  for (const k of ['moisture', 'crops', 'envWarmth', 'integrity', 'cloud']) {
    const v = s.env && s.env[k];
    if (typeof v !== 'number' || v < 0 || v > 100) errors.push('env-' + k);
  }
  if (!Array.isArray(s.villagers) || s.villagers.length < 1) errors.push('villagers');
  for (const v of s.villagers || []) {
    for (const n of NEEDS) {
      const x = v.needs && v.needs[n];
      if (typeof x !== 'number' || x < 0 || x > 100) errors.push('need-' + v.id + '-' + n);
    }
    if (!Array.isArray(v.memories) || v.memories.length > MEMORY_CAP) errors.push('memory-' + v.id);
  }
  return { ok: errors.length === 0, errors };
}

module.exports = {
  SCHEMA_VERSION,
  APP_VERSION,
  MEMORY_CAP,
  NEEDS,
  ACTIONS,
  clamp,
  createVillager,
  defaultVillagers,
  createInitialState,
  validateState,
};
