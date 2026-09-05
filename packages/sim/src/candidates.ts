/** RI-04 (plan §7.1, D-RI-5): the tuning candidates of the archetypes RI-04 introduces, in one data configuration —
 *  not in `SimConfig` (the benchmark's `configHash` covers SimConfig alone, so the benchmark stamp never sees them),
 *  not scattered through the AI, the renderer and the tests. Every number here is a *candidate* in plan §1's sense:
 *  reversible, not a fun claim, not a historical approval. A state switches a candidate on by carrying its own copy
 *  (`enableStalkers` puts one on `threatOf(f).stalk`); a fresh state carries none, so E-hour, E-rifle and the
 *  snapshot measure the game without it until a decided row promotes it (D-RI-5: a `*-cand-*` run, then a row). */
import { RETALIATE_HP_PER_S } from './engineer';

export interface StalkerCandidate {
  /** Tiles: deterministic activity inside this radius of the Stalker attracts it (plan §7.1: 8). */
  perception: number;
  /** Tiles from its home beyond which a pursuit ends (§7.1: 16). */
  leash: number;
  /** Health as a multiple of a Crawler's (§7.1: 2×). */
  hpMul: number;
  /** Speed as a multiple of a Crawler's (§7.1: 1.2×). */
  speedMul: number;
  /** Seconds of wind-up before the first attack of a pursuit (§7.1: 0.8). */
  windupS: number;
  /** Seconds between attacks (§7.1: one a second). */
  attackS: number;
  /** HP one attack takes — a whole attack, never multiplied by the frame rate (§7.1: the current player-damage value). */
  attackHp: number;
  /** Implementation default: seconds an investigation lasts before the Stalker gives up and goes home. */
  investigateS: number;
  /** Implementation default: seconds the engineer must stay out of perception (+ hysteresis) before a pursuit is given up. */
  lostS: number;
  /** Implementation default: tiles added to perception while pursuing, so its edge does not flicker. */
  hysteresis: number;
  /** Implementation default: seconds after a Stalker's death before its site fields another, out of the engineer's perception. */
  respawnS: number;
}

export const CANDIDATES: { readonly stalker: StalkerCandidate } = {
  stalker: { perception: 8, leash: 16, hpMul: 2, speedMul: 1.2, windupS: 0.8, attackS: 1, attackHp: RETALIATE_HP_PER_S,
             investigateS: 6, lostS: 2, hysteresis: 2, respawnS: 120 },
};
