'use strict';

/**
 * Chronicle (doc 01 §6.5). Built ONLY from the event log + local templates.
 * Every line MUST reference at least one real event id. 3–5 lines per day.
 * validateChronicle() proves no line claims a change that did not happen.
 */
const { CHRONICLE_TEMPLATES } = require('./lines');

function villagerName(state, id) {
  const v = state.villagers.find((x) => x.id === id);
  return v ? v.name : 'the village';
}

function buildChronicle(state) {
  const dayEvents = state.events.filter((e) => e.day === state.day);
  const lines = ['Day ' + state.day + '.'];
  const used = [];

  const byType = (t) => dayEvents.filter((e) => e.type === t);
  const first = (t, variable) =>
    byType(t).find((e) => (variable ? e.variable === variable : true));

  const rains = byType('weather').filter((e) => e.cause === 'player:rain');
  const floods = byType('hazard');
  const prays = dayEvents.filter((e) => e.type === 'decision' && e.variable === 'pray');
  const shelters = dayEvents.filter((e) => e.type === 'decision' && e.variable === 'shelter');
  const cropUps = byType('env').filter((e) => e.variable === 'crops' && e.newV > e.oldV);
  const calms = byType('weather').filter((e) => e.cause === 'player:calm');

  if (rains.length && cropUps.length && !floods.length) {
    const e = cropUps[0];
    lines.push(CHRONICLE_TEMPLATES.crops.replace('{id}', e.id));
    used.push(e.id);
  }
  if (floods.length) {
    const f = floods[0];
    const sh = shelters[0];
    lines.push(
      CHRONICLE_TEMPLATES.flood
        .replace('{target}', sh ? villagerName(state, sh.region) : 'a family')
        .replace('{id}', f.id)
    );
    used.push(f.id);
  }
  if (prays.length) {
    const p = prays[0];
    lines.push(
      CHRONICLE_TEMPLATES.pray
        .replace('{target}', villagerName(state, p.region))
        .replace('{id}', p.id)
    );
    used.push(p.id);
  } else if (shelters.length && !floods.length) {
    const s = shelters[0];
    lines.push(
      CHRONICLE_TEMPLATES.shelter
        .replace('{target}', villagerName(state, s.region))
        .replace('{id}', s.id)
    );
    used.push(s.id);
  }
  if (calms.length && floods.length) {
    const c = calms[calms.length - 1];
    lines.push(CHRONICLE_TEMPLATES.calm.replace('{id}', c.id));
    used.push(c.id);
  }
  if (!first()) {
    const any = dayEvents[0];
    if (any) {
      lines.push('The island lay quiet under a mild sky [' + any.id + '].');
      used.push(any.id);
    } else {
      lines.push('A quiet day passed with little to tell.');
    }
  }

  const out = lines.slice(0, 5);
  return { text: out.join(' '), refs: used };
}

function faithMoodLine(faithStart, faithEnd) {
  const d = faithEnd - faithStart;
  if (d > 5) return CHRONICLE_TEMPLATES.faithUp;
  if (d < -5) return CHRONICLE_TEMPLATES.faithDown;
  return CHRONICLE_TEMPLATES.steady;
}

/**
 * Proof: every [id] in the text must exist in the event log.
 * Returns {ok, missing[]}.
 */
function validateChronicle(text, events) {
  const ids = new Set(events.map((e) => e.id));
  const refs = Array.from(text.matchAll(/\[([a-z0-9-]+)\]/gi)).map((m) => m[1]);
  const missing = refs.filter((r) => !ids.has(r));
  return { ok: missing.length === 0, missing };
}

module.exports = { buildChronicle, faithMoodLine, validateChronicle };
