'use strict';

/**
 * Weather + environment (doc 01 §4). Consequence rule: every weather action
 * MUST change at least one environment variable that at least one need
 * depends on. A visual effect alone is never proof the sim updated.
 *
 * Environment variables: moisture, crops, envWarmth, integrity, cloud.
 * Fire is EXCLUDED from the MVP (decision D-17): wind steers clouds and
 * stresses fragile structures instead.
 */

const { clamp } = require('./schema');

function emit(state, type, variable, region, oldV, newV, cause) {
  state.eventSeq += 1;
  const ev = {
    id: 'e' + state.eventSeq,
    type, // 'env' | 'hazard' | 'weather' | 'decision' | 'memory' | 'ritual'
    variable,
    region,
    oldV: Math.round(oldV * 10) / 10,
    newV: Math.round(newV * 10) / 10,
    cause, // id of causing event, or 'player:sun' etc.
    day: state.day,
    clock: state.clock,
  };
  state.events.push(ev);
  return ev;
}

/**
 * Apply one player weather action. Returns the weather event.
 * intensity 0..1. Region is a free label ('field', 'low-hut', ...).
 */
function applyAction(state, action, region, intensity) {
  const env = state.env;
  const before = Object.assign({}, env);
  const i = clamp(intensity == null ? 0.6 : intensity, 0, 1);
  state.weather = { mode: action, intensity: i, direction: state.weather.direction };

  if (action === 'sun') {
    const irrigation = env.moisture > 25 ? 1 : 0.2; // dry soil drinks poorly
    env.crops += 8 * i * irrigation;
    env.moisture -= 6 * i;
    env.envWarmth += 10 * i;
    env.cloud -= 5 * i;
  } else if (action === 'rain') {
    env.moisture += 14 * i;
    env.cloud += 10 * i;
    env.envWarmth -= 6 * i;
    if (i > 0.75 || env.moisture > 85) {
      env.crops -= 8 * i; // flood ruins fields
      env.integrity -= 7 * i; // flood damages structures
    } else {
      env.crops += 10 * i; // gentle rain grows crops
    }
  } else if (action === 'wind') {
    state.weather.direction = (state.weather.direction + 45 * i) % 360; // steers clouds
    env.cloud = Math.max(0, env.cloud - 8 * i);
    env.moisture -= 2 * i;
    env.envWarmth -= 4 * i;
    if (i > 0.7) env.integrity -= 4 * i; // strong wind stresses huts
  } else if (action === 'calm') {
    // Calm reduces escalation but NEVER undoes damage.
    state.weather = { mode: 'clear', intensity: 0, direction: state.weather.direction };
  }

  for (const k of ['moisture', 'crops', 'envWarmth', 'integrity', 'cloud']) {
    env[k] = clamp(env[k]);
  }

  const wEv = emit(state, 'weather', 'mode', region || 'island', 0, i, 'player:' + action);
  for (const k of ['moisture', 'crops', 'envWarmth', 'integrity', 'cloud']) {
    if (Math.abs(env[k] - before[k]) >= 0.05) {
      emit(state, 'env', k, region || 'island', before[k], env[k], wEv.id);
    }
  }
  // Flood hazard event (deduplicated: one per action, not per tick).
  if ((action === 'rain' && (i > 0.75 || env.moisture > 85))) {
    emit(state, 'hazard', 'flood', region || 'low-hut', before.integrity, env.integrity, wEv.id);
  }
  return wEv;
}

/** Passive environment drift per fixed step (dt in day-fractions). */
function driftEnv(state, dt) {
  const env = state.env;
  env.moisture -= (1 + env.moisture * 0.12) * dt; // drainage: ~1 rain/day holds ~50
  env.cloud += (20 - env.cloud) * 0.05 * dt;
  if (env.moisture < 15) env.crops -= 6 * dt; // drought withers
  // Upkeep: villagers patch huts in fair weather (never full restore).
  const w = state.weather;
  const storm = (w.mode === 'rain' && w.intensity > 0.7) || (w.mode === 'wind' && w.intensity > 0.7);
  if (!storm && env.integrity < 85) env.integrity = Math.min(85, env.integrity + 3 * dt);
  env.crops = clamp(env.crops);
  env.moisture = clamp(env.moisture);
  env.cloud = clamp(env.cloud);
  env.integrity = clamp(env.integrity);
}

module.exports = { emit, applyAction, driftEnv };
