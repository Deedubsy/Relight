/** The claim policies of frontsim.py as sim-side bots. A bot proposes commands; it holds only its own clock. */
import { SimState, Command, DARK, INERT, CONTESTED } from './types';
import { candidates, frontage, frontageIf, interior, interiorIf, freeSlot, idxOf, isCandidate } from './sim';
import { restock, hqIdx } from './engineer';
import { walkFrom } from './graph';
import { ammoStatus } from './queries';

export type Policy = 'compact' | 'spike' | 'balanced' | 'cheapest' | 'river' | 'turtle';
export const POLICIES: Policy[] = ['compact', 'spike', 'balanced', 'cheapest', 'river', 'turtle'];

export interface Bot {
  policy: Policy; nextClaim: number; claimGap: number | null; build: boolean; gapAfter: number;
  /** D5 (config.walk): the bot walks the engineer. `rifle` = it fires at a red edge it stands on; `pending` = the block
   *  it claimed and is walking to kit; `walks` = every walk it started, for E-walk. */
  rifle: boolean; pending: number; walks: BotWalk[];
}
export interface BotWalk { t: number; reason: 'restock' | 'kit' | 'claim'; tiles: number }

/** `build` turns on the assembler rule below (proto and calibration harness); the fixtures run without it.
 *  `gapAfter` is the claim gap after hour one (frontsim.py gap_after, default 300 s = the §18 cadence). */
export function createBot(policy: Policy, claimGap: number | null = null, build = false, gapAfter = 5 * 60, rifle = false): Bot {
  return { policy, nextClaim: 15 * 60, claimGap, build, gapAfter, rifle, pending: -1, walks: [] };
}

/** D5 walking (config.walk): the engineer carries every claim's kit. Order of business each tick, standing still:
 *  no kit in the pockets → walk to the Depot and restock; an unkitted edge somewhere → walk to it; a claim pending →
 *  stand on it so its edges are kitted the second it turns Held; otherwise wait for the claim clock.
 *  GAME-ASSUMPTION: the bot claims from the map the moment its clock says so and walks afterwards (a player does
 *  the same: the claim is a map click, the kit rides out on foot). The rifle bot fires at any red edge of the block
 *  it stands on. A walking bot only claims when it holds a kit, so the claim clock slips while it restocks. */
function walkingBot(st: SimState, bot: Bot, out: Command[]): boolean {
  const e = st.engineer;
  if (e.down >= 0 || e.dest >= 0) return true;   // walking or down: nothing to decide
  if (e.truckFound && !e.truck) out.push({ type: 'enterTruck' });
  const hq = hqIdx(st), here = e.block;
  const kits = e.inv.kit ?? 0;
  const go = (i: number, reason: BotWalk['reason']) => { bot.walks.push({ t: st.t, reason, tiles: walkFrom(st, here)[i] }); out.push({ type: 'walkTo', block: i }); };
  if (bot.rifle) {
    let target = -1;
    for (const ed of st.ring) if (ed.a === here && ed.hopper <= 1e-9 && st.engagements.some(en => en.id === ed.id)) { target = ed.id; break; }
    if ((e.inv.magazine ?? 0) <= 0) target = -1;
    if (target !== e.firing) out.push({ type: 'fire', edge: target });
  }
  if (kits < 1) {
    if (here === hq) { restock(st, bot.rifle ? 5 : 0); return true; }
    go(hq, 'restock'); return true;
  }
  if (bot.rifle && (e.inv.magazine ?? 0) <= 0 && here === hq) restock(st, 5);
  // an unkitted edge: nearest first
  let best = -1, bd = Infinity;
  const d = walkFrom(st, here);
  for (const ed of st.ring) if (ed.kit === false && d[ed.a] < bd) { bd = d[ed.a]; best = ed.a; }
  if (best >= 0) { if (best !== here) go(best, 'kit'); return true; }
  if (bot.pending >= 0) {
    const b = st.blocks[bot.pending];
    if (b.state === CONTESTED) { if (here !== bot.pending) go(bot.pending, 'claim'); return true; }
    bot.pending = -1;
  }
  return false;   // free to claim
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

/** The block the policy would claim now, or -1. Distances are street hops (D6; Manhattan on the lattice). Ties break in
 *  index order (the lattice's x-major, y inner), like Python's min(). */
export function choose(st: SimState, policy: Policy, enclose = false): number {
  const n = candidates(st, scratch);
  if (n === 0 || policy === 'turtle') return -1;
  const B = st.blocks;
  // fallen blocks first, nearest to the start
  let best = -1, bestD = 0;
  for (let q = 0; q < n; q++) {
    const i = scratch[q];
    if (!st.fallen[i]) continue;
    const d = st.hops[i];
    if (best < 0 || d < bestD) { best = i; bestD = d; }
  }
  if (best >= 0) return best;
  const F = frontage(st), I = enclose ? interior(st) : 0;
  let nk = 0;
  const keyOf = (i: number, out: number[]) => {
    const b = B[i];
    const distT = st.hopsT[i], distS = st.hops[i];
    if (enclose) { out[0] = -(interiorIf(st, i, I) - I); out[1] = frontageIf(st, i, F); out[2] = b.d; out[3] = distS; nk = 4; return; }
    switch (policy) {
      case 'compact': out[0] = frontageIf(st, i, F); out[1] = b.d; out[2] = distS; nk = 3; break;
      case 'spike': out[0] = distT; out[1] = b.d; nk = 2; break;
      case 'balanced': out[0] = frontageIf(st, i, F) + distT; out[1] = b.d; nk = 2; break;
      case 'cheapest': out[0] = b.d; out[1] = frontageIf(st, i, F); nk = 2; break;
      case 'river': {
        let adj = 0;
        const ns = st.nb[i];
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
  const busy = st.config.walk ? walkingBot(st, bot, out) : false;
  if (bot.policy !== 'turtle' && st.t >= bot.nextClaim && !busy) {
    let enclose = false;
    if (bot.policy === 'compact' && bot.build && freeSlot(st) < 0) {
      const am = ammoStatus(st);
      enclose = am.demandMagPerMin > BOT_ENCLOSE_DEMAND_FRAC * am.productionMagPerMin;
    }
    let i = choose(st, bot.policy, enclose);
    // D5 (walk on): the truck is worth a detour — the Tram depot is claimed as soon as it is a candidate (GAME-ASSUMPTION)
    if (st.config.walk && !st.engineer.truckFound) {
      const depot = st.facilities.find(f => f.name === 'Tram depot');
      if (depot) { const di = idxOf(st, depot.x, depot.y); if (di >= 0 && isCandidate(st, di)) i = di; }
    }
    if (i >= 0) {
      const b = st.blocks[i];
      if (b.state === DARK) {
        out.push({ type: 'claim', x: b.x, y: b.y });
        if (st.config.walk) { bot.pending = i; bot.walks.push({ t: st.t, reason: 'claim', tiles: walkFrom(st, st.engineer.block)[i] }); out.push({ type: 'walkTo', block: i }); }
      }
    }
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
