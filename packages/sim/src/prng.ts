/** Seeded PRNG for the sim. mulberry32: one 32-bit word of state, so it serialises as a number.
 *  Deliberately not Python-`random` compatible; parity with the Python sim is by fixtures, not by RNG. */
export interface RngHolder { rng: number }

export function seedRng(seed: number): number {
  return (Math.imul(seed | 0, 0x9E3779B1) ^ 0x2545F491) >>> 0;
}

/** Advance `holder.rng` and return a uniform float in [0, 1). */
export function rngNext(holder: RngHolder): number {
  let a = (holder.rng + 0x6D2B79F5) | 0;
  holder.rng = a;
  let t = Math.imul(a ^ (a >>> 15), 1 | a);
  t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
  return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
}

export function rngInt(holder: RngHolder, n: number): number {
  return Math.floor(rngNext(holder) * n);
}

/** Uniform in [lo, hi). Same interface as Python's random.uniform, different sequence. */
export function rngUniform(holder: RngHolder, lo: number, hi: number): number {
  return lo + (hi - lo) * rngNext(holder);
}

/** Deterministic uniform in [0,1) from integers. Bit-identical to `hash01` in frontsim.py:
 *  the hulk roll of a bloom at (t, x, y) under a seed, so shells are comparable exactly. */
export function hash01(seed: number, t: number, x: number, y: number): number {
  let h = (seed ^ 0x9E3779B9) >>> 0;
  h = Math.imul(h ^ (t >>> 0), 0x9E3779B1) >>> 0; h = (h ^ (h >>> 15)) >>> 0;
  h = Math.imul(h ^ (x >>> 0), 0x9E3779B1) >>> 0; h = (h ^ (h >>> 15)) >>> 0;
  h = Math.imul(h ^ (y >>> 0), 0x9E3779B1) >>> 0; h = (h ^ (h >>> 15)) >>> 0;
  h = (h ^ (h >>> 16)) >>> 0;
  h = Math.imul(h, 0x85EBCA6B) >>> 0;
  h = (h ^ (h >>> 13)) >>> 0;
  h = Math.imul(h, 0xC2B2AE35) >>> 0;
  h = (h ^ (h >>> 16)) >>> 0;
  return h / 4294967296;
}

/** Python's round(): half to even. The sim's crawler count uses it. */
export function pyRound(v: number): number {
  const f = Math.floor(v);
  const r = v - f;
  if (r > 0.5) return f + 1;
  if (r < 0.5) return f;
  return f % 2 === 0 ? f : f + 1;
}
