/** RI-05 (plan §5): neighbourhood projects — a small record linked to an existing facility's block: what a site
 *  needs, what has reached it, which stage it is at, the commissioning attempt that restored it and the reward it
 *  granted. The stage is DERIVED from the site's real prerequisites every sync (the block record alone owns Dark /
 *  Contested / Held — plan §4.4: no second project-owned territory state); the only things written here are the
 *  once-only facts: the attempt id, `restoredAt`, the reward (plan §5.1: "completed stages and rewards are
 *  recorded once and survive save/load").
 *
 *  Two projects (plan §5.2, the minimal part of T16):
 *   - `rail-yard`: the rail yard's restoration (D-P4-12's coal block). Its site is the block; its requirements are
 *     the claim's own materials (§4.3's `deliverTo`, by hand or — RI-05 — off a belt into the installation); its
 *     commissioning is the explicit Activate and the normal burn-off; it is restored when the block turns Held.
 *     Reward: the transport kit — Track, Tram stop and Tram (flow.ts `PROJECT_UNLOCKS`) — an operational capability
 *     (plan §9.1), deliverable before any tram exists (no circular unlock: the kit is locked until then).
 *   - `supply-depot`: the local supply depot (plan §5.2 "a named installation using the existing chest/inventory
 *     system") on the restored rail yard: a supply chest on the site, stocked with ordinary items, commissioned by
 *     the explicit `commission` command. On a Held block that changes only the project state — no second claim, no
 *     claim materials (plan §4.4). Reward: the restored chest is the block's named depot and hands out kits like the
 *     Depot (shorter restocking trips — plan §5.2's function, nothing invented).
 *  Implementation defaults (RI-05, reversible): the catalogue's two entries and their requirements; the depot's
 *  stock is not consumed by commissioning (it is the depot's function); a lost block after restoration follows the
 *  normal retake rules and neither revokes nor re-grants the reward (plan §4.4). */
import { SimState, HELD, CONTESTED, DARK, ProjectStage } from './types';
import {
  FlowState, Item, Machine, ProjectRecord, faceSub, deliveredTo, claimNeed, activationCheck, poleGrid, RAIL_ROUTE_KINDS,
} from './flow';
import { ground, blockOfTile, inReach } from './ground';
import { blockName } from './names';
import { heartAt, describeHeart } from './heart';   // RI-06

export const RAIL_YARD_PROJECT = 'rail-yard', SUPPLY_DEPOT_PROJECT = 'supply-depot';
export const RAIL_ROUTE_REWARD = 'rail-route', LOCAL_DEPOT_REWARD = 'local-depot';
/** Implementation default (RI-05): the local depot's stock — ordinary front supplies (plan §5.2: "actual inventory;
 *  no global magic stock"); the same items the tram can carry from the HQ, so the route has a real follow-on use. */
export const SUPPLY_DEPOT_NEED: Readonly<Partial<Record<Item, number>>> = { coal: 20, magazine: 10 };
export const PROJECT_STAGES: readonly ProjectStage[] = ['discovered', 'preparing', 'ready', 'commissioning', 'restored', 'interrupted'];

function newRecord(st: SimState, projectId: string, siteId: number, requirements: Partial<Record<Item, number>>): ProjectRecord {
  return {
    projectId, siteId, neighbourhoodBlockIds: [...(st.nb[siteId] ?? [])], requirements: { ...requirements }, deliveredItems: {},
    stage: 'discovered', activationAttemptId: -1, rewardId: null, restoredAt: -1, rewardAt: -1, installId: -1,
  };
}
export function projectOf(st: SimState, id: string): ProjectRecord | undefined { return st.flow?.projects?.[id]; }
export function projectList(st: SimState): ProjectRecord[] { return Object.values(st.flow?.projects ?? {}); }
/** The catalogue's title for a record (plan §5.2). */
export function projectTitle(st: SimState, r: ProjectRecord): string {
  const name = blockName(st, r.siteId), Name = name.charAt(0).toUpperCase() + name.slice(1);   // a title: the block's name capitalised
  return r.projectId === RAIL_YARD_PROJECT ? `${Name} restoration` : r.projectId === SUPPLY_DEPOT_PROJECT ? `${Name} supply depot` : r.projectId;
}
/** The reward, once granted, is owned for good; whether the facility is OPERATIONAL is a separate, derived fact
 *  (plan §5.1: "distinguish owning an unlock from having an operational facility"). */
export function projectOperational(st: SimState, r: ProjectRecord): boolean {
  if (r.stage !== 'restored') return false;
  const b = st.blocks[r.siteId];
  if (r.projectId === SUPPLY_DEPOT_PROJECT) return b.state === HELD && !!installOf(st, r);
  return b.state === HELD;
}
/** The supply chest standing on the site (the recorded one if it still stands, else the first on the block). */
export function installOf(st: SimState, r: ProjectRecord): Machine | undefined {
  const f = st.flow;
  if (!f) return undefined;
  let best: Machine | undefined;
  for (const m of f.machines) {
    if (m.kind !== 'chest' || blockOfTile(st, m.x, m.y) !== r.siteId) continue;
    if (m.id === r.installId) return m;
    best ??= m;
  }
  return best;
}
function setStage(st: SimState, r: ProjectRecord, stage: ProjectStage): void {
  if (r.stage === stage) return;
  r.stage = stage;
  st.events.push({ type: 'project', t: st.t, id: r.projectId, stage, site: r.siteId, attempt: r.activationAttemptId });
}
function short(r: ProjectRecord): string[] {
  const out: string[] = [];
  for (const k in r.requirements) { const need = r.requirements[k as Item] ?? 0, got = r.deliveredItems[k as Item] ?? 0; if (got < need) out.push(`${need - got} ${k === 'copper' ? 'Cu' : k}`); }
  return out;
}

/** `activate` (flow.ts) records the attempt on the site's project before it consumes the delivered materials: the
 *  record keeps what was delivered (the claim's committed store is emptied by the activation). */
export function projectActivated(st: SimState, bi: number, attempt: number, got: { steel: number; copper: number }): void {
  const f = st.flow;
  if (!f) return;
  for (const r of Object.values(f.projects)) {
    if (r.siteId !== bi || r.projectId !== RAIL_YARD_PROJECT || r.stage === 'restored') continue;
    r.activationAttemptId = attempt;
    r.deliveredItems = { steel: got.steel, copper: got.copper };
  }
}

/** Recompute every record's stage from the site's real state. Called after each block tick and each hand command. */
export function syncProjects(st: SimState): void {
  const f = st.flow;
  if (!f) return;
  f.projects ??= {};
  const ry = ground(st).railYard;
  if (ry >= 0 && !f.projects[RAIL_YARD_PROJECT]) f.projects[RAIL_YARD_PROJECT] = newRecord(st, RAIL_YARD_PROJECT, ry, claimNeed(st));
  for (const r of Object.values(f.projects)) {
    if (r.projectId === RAIL_YARD_PROJECT) syncRailYard(st, f, r);
    else if (r.projectId === SUPPLY_DEPOT_PROJECT) syncDepot(st, r);
  }
}
function syncRailYard(st: SimState, f: FlowState, r: ProjectRecord): void {
  const b = st.blocks[r.siteId];
  if (r.stage === 'restored') return;   // recorded once; a later loss follows the normal retake rules (plan §4.4)
  if (b.state === HELD) {
    r.restoredAt = st.t; r.rewardId = RAIL_ROUTE_REWARD; r.rewardAt = st.t;
    setStage(st, r, 'restored');
    // the next opportunity (plan §5.3): the local depot on the restored yard, deliverable by the new route
    f.projects[SUPPLY_DEPOT_PROJECT] ??= newRecord(st, SUPPLY_DEPOT_PROJECT, r.siteId, SUPPLY_DEPOT_NEED);
    return;
  }
  if (b.state === CONTESTED) { setStage(st, r, 'commissioning'); return; }
  if (b.state !== DARK) return;   // inert / void: nothing to derive
  const have = deliveredTo(st, r.siteId);
  if (f.delivered[r.siteId]) r.deliveredItems = { steel: have.steel, copper: have.copper };   // the committed store owns it while it exists
  if (activationCheck(st, b.x, b.y, false).ok) setStage(st, r, 'ready');
  else if (r.activationAttemptId >= 0) setStage(st, r, 'interrupted');   // an attempt was made and the block is Dark again
  else if (have.steel + have.copper > 0 || heartAt(st, r.siteId)?.cabinets.some(c => c.delivered.steel + c.delivered.copper > 0)
    || poleGrid(st).reached.includes(r.siteId) || (!ground(st).blocks[r.siteId].sub && !!faceSub(st, r.siteId))) setStage(st, r, 'preparing');   // installation or cabinet delivery, a pole run, or a built substation
  else setStage(st, r, 'discovered');
}
function syncDepot(st: SimState, r: ProjectRecord): void {
  const b = st.blocks[r.siteId], chest = installOf(st, r);
  r.installId = chest?.id ?? -1;
  const got: Partial<Record<Item, number>> = {};
  for (const k in r.requirements) got[k as Item] = Math.min(r.requirements[k as Item] ?? 0, Math.floor(chest?.inv[k] ?? 0));
  r.deliveredItems = got;
  if (r.stage === 'restored') return;
  if (b.state !== HELD) { setStage(st, r, chest ? 'interrupted' : 'discovered'); return; }
  if (!chest) { setStage(st, r, 'discovered'); return; }
  setStage(st, r, short(r).length ? 'preparing' : 'ready');
}

export interface CommissionCheck { ok: boolean; reason: string }
/** Why a non-claim project cannot be commissioned now ('' when it can): the record, its stage, reach of the chest. */
export function commissionCheck(st: SimState, id: string): CommissionCheck {
  const r = projectOf(st, id);
  if (!r) return { ok: false, reason: 'no such project' };
  if (r.projectId === RAIL_YARD_PROJECT) return { ok: false, reason: 'the rail yard commissions through Activate at its substation' };
  if (r.stage === 'restored') return { ok: false, reason: 'already restored' };
  const chest = installOf(st, r);
  if (!chest) return { ok: false, reason: 'no supply chest on the site' };
  if (st.blocks[r.siteId].state !== HELD) return { ok: false, reason: 'the site is not Held' };
  const s = short(r);
  if (s.length) return { ok: false, reason: `needs ${s.join(', ')} in the chest` };
  if (!inReach(st, chest.x, chest.y, chest.size)) return { ok: false, reason: 'walk closer to the chest' };
  return { ok: true, reason: '' };
}
/** The explicit commissioning of a ready non-claim project: changes the record only — no block state, no claim
 *  charge, the stock stays in the chest (plan §4.4). Idempotent: a repeat is refused. */
export function commission(st: SimState, id: string): CommissionCheck {
  syncProjects(st);
  const chk = commissionCheck(st, id);
  if (!chk.ok) return chk;
  const r = projectOf(st, id)!;
  r.restoredAt = st.t; r.rewardId = LOCAL_DEPOT_REWARD; r.rewardAt = st.t; r.activationAttemptId = -1;
  setStage(st, r, 'restored');
  return chk;
}

/** One line for the UI and the harness: title, stage, what is still short, what to do next. */
export function describeProject(st: SimState, r: ProjectRecord): string {
  const s = short(r), need = s.length ? ` · needs ${s.join(', ')}` : '';
  // RI-06: while the Heart stands, the yard's restoration is its encounter — the objective, the active failure condition, the next action
  const next = r.projectId === RAIL_YARD_PROJECT && heartAt(st, r.siteId) ? ` · ${describeHeart(st)}`
    : r.stage === 'ready' ? (r.projectId === RAIL_YARD_PROJECT ? ' · Activate at the substation' : ' · commission it at the chest')
    : r.stage === 'restored' ? (projectOperational(st, r) ? ' · operational' : ' · restored, not operational (block lost or chest gone)')
    : r.stage === 'commissioning' ? ' · burning off' : '';
  const reward = r.rewardId === RAIL_ROUTE_REWARD ? ` · unlocked ${RAIL_ROUTE_KINDS.join(', ')}` : r.rewardId === LOCAL_DEPOT_REWARD ? ' · hands out kits' : '';
  return `${projectTitle(st, r)} · ${r.stage}${need}${next}${reward}`;
}
