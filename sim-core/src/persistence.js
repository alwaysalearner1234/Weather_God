'use strict';

/**
 * Persistence (doc 02 §2.5, §3.2 save-snapshot contract). Two alternating
 * slots with version counter, schema version, checksum, validation on load,
 * automatic fallback to the last valid slot, and a visible reset path.
 * Storage adapters: memory (tests) or browser localStorage (game).
 */
const { SCHEMA_VERSION, APP_VERSION, validateState } = require('./schema');

function checksumOf(str) {
  let h = 5381;
  for (let i = 0; i < str.length; i++) h = ((h << 5) + h + str.charCodeAt(i)) | 0;
  return (h >>> 0).toString(16);
}

function encode(state) {
  const body = JSON.stringify(state);
  return {
    slot: 0,
    version: 1,
    schema: SCHEMA_VERSION,
    app: APP_VERSION,
    savedAt: new Date().toISOString(),
    clock: state.clock,
    day: state.day,
    body,
    checksum: checksumOf(body),
  };
}

function decode(snapshot) {
  if (!snapshot || typeof snapshot.body !== 'string') return { ok: false, reason: 'bad-shape' };
  if (snapshot.schema !== SCHEMA_VERSION) return { ok: false, reason: 'schema-mismatch' };
  if (checksumOf(snapshot.body) !== snapshot.checksum) return { ok: false, reason: 'checksum' };
  let state;
  try {
    state = JSON.parse(snapshot.body);
  } catch (e) {
    return { ok: false, reason: 'parse' };
  }
  const v = validateState(state);
  if (!v.ok) return { ok: false, reason: 'invalid-state', errors: v.errors };
  return { ok: true, state };
}

function memoryStore() {
  const slots = [null, null];
  let counter = 0;
  const api = {
    slots, // exposed so tests can emulate disk corruption
    write(snapshot) {
      counter += 1;
      const s = Object.assign({}, snapshot, { slot: counter % 2, version: counter });
      slots[s.slot] = s;
      return s;
    },
    read(i) {
      return slots[i];
    },
    /** Newest valid slot, else null. */
    loadLatest() {
      const order = counter % 2 === 0 ? [1, 0] : [0, 1];
      // newest written slot first
      const newest = slots[order[0]] && (!slots[order[1]] || slots[order[0]].version >= slots[order[1]].version)
        ? [order[0], order[1]] : [order[1], order[0]];
      for (const i of newest) {
        if (!slots[i]) continue;
        const d = decode(slots[i]);
        if (d.ok) return { ok: true, state: d.state, slot: i };
      }
      return { ok: false, reason: 'no-valid-slot' };
    },
  };
  return api;
}

function localStorageStore(key, ls) {
  const get = () => {
    try {
      return JSON.parse(ls.getItem(key) || '{"slots":[null,null],"counter":0}');
    } catch (e) {
      return { slots: [null, null], counter: 0 };
    }
  };
  const put = (d) => ls.setItem(key, JSON.stringify(d));
  const mem = memoryStore();
  const data = get();
  // rehydrate memory store shape (best effort; invalid entries fail validation on read)
  return {
    write(snapshot) {
      const d = get();
      d.counter += 1;
      const s = Object.assign({}, snapshot, { slot: d.counter % 2, version: d.counter });
      d.slots[s.slot] = s;
      put(d);
      return s;
    },
    read(i) {
      return get().slots[i];
    },
    loadLatest() {
      const d = get();
      const order = d.counter % 2 === 0 ? [1, 0] : [0, 1];
      for (const i of order) {
        if (!d.slots[i]) continue;
        const r = decode(d.slots[i]);
        if (r.ok) return { ok: true, state: r.state, slot: i };
      }
      return { ok: false, reason: 'no-valid-slot' };
    },
  };
}

module.exports = { encode, decode, checksumOf, memoryStore, localStorageStore };
