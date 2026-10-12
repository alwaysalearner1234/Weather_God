'use strict';

/**
 * Session composer (Person C integration surface for Person B).
 * One session = one in-game day. Fixed-step ticks; environment steps every
 * tick, villagers decide every DECISION_EVERY ticks. Deterministic given
 * seed + action list. No rendering, no network, no model.
 */
const { createRng } = require('./rng');
const { createInitialState, clamp } = require('./schema');
const { applyAction, driftEnv } = require('./environment');
const { stepNeeds, lowestNeed } = require('./needs');
const { decideAll } = require('./decisions');
const { observe, decay } = require('./memory');
const { buildChronicle, faithMoodLine } = require('./chronicle');

const STEP_DT = 1 / 24; // one in-game hour per tick; a day = 24 ticks

function createSession(seed) {
  const rng = createRng(seed);
  const state = createInitialState(seed);
  issueRequest(state);
  return { rng, state, faithAtDawn: villageFaithOf(state) };
}

function villageFaithOf(state) {
  if (!state.villagers.length) return 50;
  const sum = state.villagers.reduce((a, v) => a + v.needs.faith, 0);
  return sum / state.villagers.length;
}

/** Daily request: neediest villager's lowest need (short goal for the session). */
function issueRequest(state) {
  let worst = state.villagers[0];
  for (const v of state.villagers) {
    if (lowestNeed(v).value < lowestNeed(worst).value) worst = v;
  }
  const ln = lowestNeed(worst);
  const verb = { sustenance: 'feed', warmth: 'warm', safety: 'shelter', faith: 'comfort' }[ln.name];
  state.request = {
    villagerId: worst.id,
    need: ln.name,
    text: worst.name + ' asks: ' + verb + ' us — our ' + ln.name + ' is low.',
  };
  return state.request;
}

/** One player action + its immediate consequence chain. */
function playerAction(session, action, region, intensity) {
  const { state } = session;
  const wEv = applyAction(state, action, region, intensity);
  // Villagers observe salient harm/help from the resulting hazard/env.
  const hazards = state.events.filter((e) => e.cause === wEv.id && e.type === 'hazard');
  const envDrops = state.events.filter(
    (e) => e.cause === wEv.id && e.type === 'env' && e.newV < e.oldV && (e.oldV - e.newV) >= 5
  );
  for (const v of state.villagers) {
    for (const h of hazards) {
      const impact = v.craft === 'farmer' || v.id === (region || '') ? -70 : -45;
      observe(state, v, h, 'flood', h.region, impact);
    }
    for (const d of envDrops) {
      observe(state, v, d, d.variable + '-drop', d.region, -(d.oldV - d.newV) * 2);
    }
    if (action === 'rain' && state.env.crops > 55) {
      const up = state.events.find(
        (e) => e.cause === wEv.id && e.type === 'env' && e.variable === 'crops' && e.newV > e.oldV
      );
      if (up) {
        observe(state, v, up, 'saved-crops', up.region, +40);
        if (v.needs.faith < 75) v.needs.faith = clamp(v.needs.faith + 1);
      }
    }
  }
  return wEv;
}

/** Advance the fixed-step clock by n ticks. */
function tick(session, n) {
  const { state } = session;
  for (let k = 0; k < (n == null ? 1 : n); k++) {
    state.clock += 1;
    driftEnv(state, STEP_DT);
    for (const v of state.villagers) {
      const sheltered = v.action === 'shelter';
      stepNeeds(v, state.env, sheltered, STEP_DT);
    }
    decideAll(state);
  }
}

/** End the day: decay memory, chronicle, milestones, derived faith, new request. */
function endDay(session) {
  const { state } = session;
  for (const v of state.villagers) decay(v);
  const built = buildChronicle(state);
  const faithEnd = villageFaithOf(state);
  const mood = faithMoodLine(session.faithAtDawn, faithEnd);
  state.villageFaith = faithEnd;
  // One visible improvement per threshold (doc 01 §6.6).
  if (faithEnd > 65) state.milestones.shrine = true;
  if (faithEnd > 60) state.milestones.hut4 = true;
  if (faithEnd > 75) state.milestones.hut5 = true;
  const chronicle = built.text + ' ' + mood;
  state.day += 1;
  state.clock = 0;
  session.faithAtDawn = faithEnd;
  issueRequest(state);
  return { chronicle, refs: built.refs, mood, faithEnd };
}

function stateHash(state) {
  const s = JSON.stringify(state);
  let h = 5381;
  for (let i = 0; i < s.length; i++) h = ((h << 5) + h + s.charCodeAt(i)) | 0;
  return (h >>> 0).toString(16);
}

module.exports = {
  STEP_DT,
  createSession,
  issueRequest,
  playerAction,
  tick,
  endDay,
  villageFaithOf,
  stateHash,
};
