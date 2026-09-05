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

/** RI-06 (plan §9, D-RI-6): the Junction Heart's candidate numbers — the first boss's timers, packets and cabinet
 *  recipe. Plan §9.2 labels the 90 s / 60 s timers and the packet roster "tuning candidates" and the cabinet amounts
 *  "candidate recipe data"; none is a decided constant and none is in SimConfig (the benchmark never enables the
 *  Heart — `enableHeart` is per state, like `enableStalkers`). */
export interface HeartCandidate {
  /** Seconds of PRODUCTIVE commissioning (both feeders powered) that destroy the Heart (plan §9.2 default 5, tuning candidate). */
  productiveS: number;
  /** Consecutive seconds without productive commissioning that interrupt the attempt (plan §9.2 default 9, tuning candidate). */
  stallS: number;
  /** The productive-progress percentages at which one reinforcement packet is requested, once each per attempt (plan §9.2 default 6). */
  thresholds: readonly number[];
  /** Implementation default: seconds the approach is shown (the packet's emergence point marked, the event logged) before its bodies are born. */
  approachS: number;
  /** Crawlers per packet, by threshold index (candidate data; the existing Crawler only — plan §9.1: no new enemy for the first boss). */
  packets: readonly number[];
  /** The encounter's own population bound: packet bodies alive at once (plan §9.2 default 6: "retain the global population budget"; default 11). */
  maxAlive: number;
  /** A feeder cabinet's restoration materials (candidate recipe data, plan §9.2 default 1). */
  cabinet: { readonly steel: number; readonly copper: number };
}

export const CANDIDATES: { readonly stalker: StalkerCandidate; readonly heart: HeartCandidate } = {
  stalker: { perception: 8, leash: 16, hpMul: 2, speedMul: 1.2, windupS: 0.8, attackS: 1, attackHp: RETALIATE_HP_PER_S,
             investigateS: 6, lostS: 2, hysteresis: 2, respawnS: 120 },
  heart: { productiveS: 90, stallS: 60, thresholds: [25, 50, 75], approachS: 5, packets: [2, 3, 4], maxAlive: 8, cabinet: { steel: 4, copper: 2 } },
};
