/** The claim policies of frontsim.py as sim-side bots. A bot proposes commands; it holds only its own clock. */
import { SimState, Command, DARK, INERT } from './types';
import { candidates, frontage, frontageIf, interior, interiorIf, topo, freeSlot } from './sim';
import { ammoStatus } from './queries';

export type Policy = 'compact' | 'spike' | 'balanced' | 'cheapest' | 'river' | 'turtle';
export const POLICIES: Policy[] = ['compact', 'spike', 'balanced', 'cheapest', 'river', 'turtle'];

export interface Bot { policy: Policy; nextClaim: number; claimGap: number | null; build: boolean; gapAfter: number }

/** `build` turns on the assembler rule below (proto and calibration harness); the fixtures run without it.
 *  `gapAfter` is the claim gap after hour one (frontsim.py gap_after, default 300 s = the §18 cadence). */
export function createBot(policy: Policy, claimGap: number | null = null, build = false, gapAfter = 5 * 60): Bot {
  return { policy, nextClaim: 15 * 60, claimGap, build, gapAfter };
}

/** GAME-ASSUMPTION (calibration step 4): a player who watches the HUD builds an assembler when the 10-minute demand
 *  exceeds 80 % of production and the stock covers one. The bot looks once a minute, after its claim. */
export const BOT_BUILD_DEMAND_FRAC = 0.8;
/** Calibration 2: the compact bot's claim-to-enclose preference. With no free interior slot and demand above this
 *  fraction of production it claims the candidate that closes the most blocks (ties by its usual key). Compact only. */
export const BOT_ENCLOSE_DEMAND_FRAC = 0.6;

const scratch: number[] = [];
const k0: number[] = [0, 0, 0, 0], k1: number[] = [0, 0, 0, 0];

function lessKey(a: number[], b: number[], n: number): boolean {
  for (let i = 0; i < n; i++) { if (a[i] < b[i]) return true; if (a[i] > b[i]) return false; }
  return false;
}

/** The block the policy would claim now, or -1. Ties break in grid order (x-major, y inner), like Python's min(). */
export function choose(st: SimState, policy: Policy, enclose = false): number {
  const n = candidates(st, scratch);
  if (n === 0 || policy === 'turtle') return -1;
  const B = st.blocks, [sx, sy] = st.start, [tx, ty] = st.target;
  // fallen blocks first, nearest to the start
  let best = -1, bestD = 0;
  for (let q = 0; q < n; q++) {
    const i = scratch[q];
    if (!st.fallen[i]) continue;
    const d = Math.abs(B[i].x - sx) + Math.abs(B[i].y - sy);
    if (best < 0 || d < bestD) { best = i; bestD = d; }
  }
  if (best >= 0) return best;
  const F = frontage(st), I = enclose ? interior(st) : 0;
  const tp = topo(st.w, st.h);
  let nk = 0;
  const keyOf = (i: number, out: number[]) => {
    const b = B[i];
    const distT = Math.abs(b.x - tx) + Math.abs(b.y - ty);
    if (enclose) { out[0] = -(interiorIf(st, i, I) - I); out[1] = frontageIf(st, i, F); out[2] = b.d; out[3] = Math.abs(b.x - sx) + Math.abs(b.y - sy); nk = 4; return; }
    switch (policy) {
      case 'compact': out[0] = frontageIf(st, i, F); out[1] = b.d; out[2] = Math.abs(b.x - sx) + Math.abs(b.y - sy); nk = 3; break;
      case 'spike': out[0] = distT; out[1] = b.d; nk = 2; break;
      case 'balanced': out[0] = frontageIf(st, i, F) + distT; out[1] = b.d; nk = 2; break;
      case 'cheapest': out[0] = b.d; out[1] = frontageIf(st, i, F); nk = 2; break;
      case 'river': {
        let adj = 0;
        const ns = tp.nb[i];
        for (let k = 0; k < ns.length; k++) if (B[ns[k]].state === INERT) adj++;
        out[0] = -adj; out[1] = frontageIf(st, i, F); out[2] = b.d; nk = 3; break;
      }
    }
  };
  best = scratch[0]; keyOf(best, k0);
  for (let q = 1; q < n; q++) {
    const i = scratch[q];
    keyOf(i, k1);
    if (lessKey(k1, k0, nk)) { best = i; k0[0] = k1[0]; k0[1] = k1[1]; k0[2] = k1[2]; k0[3] = k1[3]; }
  }
  return best;
}

/** Push the bot's commands for this tick into `out` (usually none). Mirrors the claim cadence of frontsim.py:
 *  one claim per 15 min in hour one, then one per 5 min (what §18 is drawn at), or `claimGap` seconds. */
export function botCommands(st: SimState, bot: Bot, out: Command[]): void {
  if (bot.policy !== 'turtle' && st.t >= bot.nextClaim) {
    let enclose = false;
    if (bot.policy === 'compact' && bot.build && freeSlot(st) < 0) {
      const am = ammoStatus(st);
      enclose = am.demandMagPerMin > BOT_ENCLOSE_DEMAND_FRAC * am.productionMagPerMin;
    }
    const i = choose(st, bot.policy, enclose);
    if (i >= 0) { const b = st.blocks[i]; if (b.state === DARK) out.push({ type: 'claim', x: b.x, y: b.y }); }
    const gap = bot.claimGap ?? (st.t < 3600 ? 15 * 60 : bot.gapAfter);
    bot.nextClaim = st.t + gap;
  }
  if (bot.build && st.config.economy && st.t > 0 && st.t % 60 === 0) {
    const am = ammoStatus(st), c = st.config.eco.assemblerCost;
    if (am.demandMagPerMin > BOT_BUILD_DEMAND_FRAC * am.productionMagPerMin && st.stock.copper >= c.copper && st.stock.steel >= c.steel
        && freeSlot(st) >= 0)   // §5: an assembler needs an empty interior slot
      out.push({ type: 'addAssembler' });
  }
}
