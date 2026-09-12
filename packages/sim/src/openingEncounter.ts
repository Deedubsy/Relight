/** GP-OPENING (2026-09-11): the once-only introductory attack that follows the first fully loaded Home turret.
 *  Saved facts only — status, the chosen approach, the shots the turrets actually spent and the first automatic
 *  delivery. Presentation (goal.ts, campaignAlerts) reads these; scheduling and bodies stay with the single raid
 *  director in campaignThreat.ts. Constitution rule 8: no quest flags, every line is derived from state. */
import type { SimState } from './types';
import { ground, blockOfTile } from './ground';
import { machineRunning, machineAt, DX, DY, inputTile, outputTile, recipeOf, recipeOutput, type Machine } from './flow';
import { hopperCapacity } from './progression';
import { TURRET_HOPPER } from './recipes';
import { defenceHp, baseCore } from './campaignDefence';
import { conveyorDestinations } from './directConveyor';
import { headingWord } from './move';

export type OpeningStatus = 'pending' | 'scheduled' | 'active' | 'repelled' | 'lost' | 'skipped';
export interface OpeningEncounter {
  version: 1; status: OpeningStatus;
  /** Turret shots spent while the group was active (counted at the shot, never inferred from stock). */
  shots: number;
  id?: number; turretId?: number; origin?: number; scheduledAt?: number; startsAt?: number; endedAt?: number; count?: number;
  /** First moment a produced magazine reached a turret through belts/inserters from a working Assembler. */
  suppliedAt?: number;
}
/** warning: seconds between the full hopper and the first body; count: skitters (20 HP each, 10 dmg per turret round →
 *  one fifth of a full 50-round hopper at 1 round/s); recovery: active seconds before another scheduled raid;
 *  guard = ACTIVE_RAIDS.warning (300) + recovery + 60 — the minimum time the next major must still be away when the
 *  encounter is scheduled, so its warning never overlaps; maxDuration: the group withdraws after this long. */
export const OPENING_ENCOUNTER = { warning: 25, count: 5, maxDuration: 300, recovery: 300, guard: 660, ack: 60, supplyAck: 45 } as const;
export const COMPASS: Record<string, string> = { N: 'north', NE: 'north-east', E: 'east', SE: 'south-east', S: 'south', SW: 'south-west', W: 'west', NW: 'north-west' };

export const turretCapacity = (m: Machine): number => hopperCapacity(m, TURRET_HOPPER);
export const turretFull = (m: Machine): boolean => (m.inv.rounds ?? 0) >= turretCapacity(m) - 1e-9;
export function liveTurrets(st: SimState): Machine[] { return (st.flow?.machines ?? []).filter(m => m.kind === 'turret' && defenceHp(m) > 0); }
/** Turrets protecting Home: inside Founders Court or its opening bounds. */
export function homeTurrets(st: SimState): Machine[] {
  const G = ground(st), b = G.opening?.bounds, home = st.campaign?.homeBlock;
  return liveTurrets(st).filter(m => blockOfTile(st, m.x, m.y) === home || (!!b && m.x >= b.x && m.x < b.x + b.size && m.y >= b.y && m.y < b.y + b.size));
}
/** The first operational Home turret whose hopper is full — hand loading and belts both qualify. */
export function readyOpeningTurret(st: SimState): Machine | undefined {
  return homeTurrets(st).find(m => machineRunning(st, m) && turretFull(m));
}
/** Fresh campaigns wait for the first full turret; progressed saves never receive a beginner attack. */
export function initOpeningEncounter(st: SimState): void {
  const d = st.campaign?.defence; if (!d || d.opening || !d.clock) return;
  const sites = st.campaign!.progression?.sites ?? [];
  const fresh = st.t === 0 || (d.history.length === 0 && d.raidsStarted === 0 && !d.major && !d.minor && d.clock.nextStart - st.t >= OPENING_ENCOUNTER.guard
    && liveTurrets(st).length < 3 && !sites.some(s => s.recovered || (s.kind === 'plant' && s.installed)));
  d.opening = { version: 1, status: fresh ? 'pending' : 'skipped', shots: 0 };
}
export const openingBlocksRaids = (d: { opening?: OpeningEncounter } | undefined): boolean => d?.opening?.status === 'scheduled' || d?.opening?.status === 'active';

const RELAYS = ['chest', 'tramstop', 'depot'];
/** True when items leaving `source` can reach `target` through belts, undergrounds, splitters, inserters and up to
 *  `depth` intermediate stores. Disconnected machines or belts pointing elsewhere never qualify. */
export function supplyChainReaches(st: SimState, source: Machine, target: Machine, depth = 4): boolean {
  const all = st.flow!.machines, dest = conveyorDestinations(st);
  const visit = (m: Machine, left: number, seen: Set<number>): boolean => {
    if (m.id === target.id) return true;
    if (left <= 0 || seen.has(m.id) || (m.id !== source.id && !RELAYS.includes(m.kind))) return false;
    seen.add(m.id);
    const next: Machine[] = [];
    for (const b of all) {
      if (b.kind === 'inserter') { const [ix, iy] = inputTile(b), [ox, oy] = outputTile(b); if (machineAt(st, ix, iy)?.id === m.id) { const o = machineAt(st, ox, oy); if (o) next.push(o); } }
      else if (machineAt(st, b.x - DX[b.dir], b.y - DY[b.dir])?.id === m.id) next.push(...(dest.get(b.id) ?? []));
    }
    return next.some(n => visit(n, left - 1, seen));
  };
  return visit(source, depth, new Set());
}
export function magazineProducers(st: SimState): Machine[] {
  return (st.flow?.machines ?? []).filter(m => (m.kind === 'assembler' || m.kind === 'assembler2') && recipeOutput(recipeOf(m)) === 'magazine');
}
/** A producing Assembler whose output route reaches this turret. */
export function turretSupplier(st: SimState, turret: Machine): Machine | undefined {
  return magazineProducers(st).find(a => ((a.observation?.produced.magazine ?? 0) > 0 || (a.out ?? 0) > 0) && supplyChainReaches(st, a, turret));
}
/** Called by the automated feed path only (belts and inserters); hand loading never records a supply chain. */
export function noteTurretSupply(st: SimState, m: Machine): void {
  const op = st.campaign?.defence?.opening; if (!op || op.suppliedAt !== undefined || m.kind !== 'turret') return;
  if (turretSupplier(st, m)) op.suppliedAt = st.t;
}
export interface OpeningView {
  status: OpeningStatus; shots: number; count: number; direction: string; secondsLeft: number;
  startsAt?: number; endedAt?: number; suppliedAt?: number; origin?: { x: number; y: number }; turret?: Machine;
}
/** Read-only view for guidance and HUD text. `direction` is the compass word of the announced approach from Home. */
export function openingEncounter(st: SimState): OpeningView | null {
  const d = st.campaign?.defence; if (!d) return null;
  const op = d.opening, status: OpeningStatus = op?.status ?? (d.clock ? 'pending' : 'skipped');
  const G = ground(st), core = baseCore(st, st.campaign!.homeBlock);
  const origin = op?.origin !== undefined ? { x: op.origin % G.tw, y: Math.floor(op.origin / G.tw) } : undefined;
  const direction = origin && core ? COMPASS[headingWord([origin.x + .5 - core.x - core.size / 2, origin.y + .5 - core.y - core.size / 2])] ?? 'outskirts' : '';
  return { status, shots: op?.shots ?? 0, count: op?.count ?? OPENING_ENCOUNTER.count, direction,
    secondsLeft: op?.startsAt !== undefined ? Math.max(0, Math.ceil(op.startsAt - st.t)) : 0,
    startsAt: op?.startsAt, endedAt: op?.endedAt, suppliedAt: op?.suppliedAt,
    origin: origin && { x: origin.x + .5, y: origin.y + .5 },
    turret: op?.turretId !== undefined ? st.flow?.machines.find(m => m.id === op.turretId) : undefined };
}
