'use strict';

/**
 * Seeded RNG (mulberry32). Same seed + same call order = same stream.
 * The whole core consumes randomness only through this module.
 */
function createRng(seed) {
  let a = (seed >>> 0) || 1;
  return {
    next() {
      a |= 0; a = (a + 0x6D2B79F5) | 0;
      let t = Math.imul(a ^ (a >>> 15), 1 | a);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    },
    range(lo, hi) {
      return lo + this.next() * (hi - lo);
    },
    pick(arr) {
      return arr[Math.floor(this.next() * arr.length)];
    },
  };
}

module.exports = { createRng };
