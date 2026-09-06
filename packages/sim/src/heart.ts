/** RI-06 (plan §9, GDD §28.8, D-RI-6): the first boss — the Junction Heart. A rooted colony structure occupies the
 *  rail yard's switching installation (the yard's substation); two feeder cabinets on the yard, one toward each of
 *  its first two Dark neighbours, serve different approaches. The player supplies the installation (the claim's own
 *  materials) and both cabinets, strings power to all three and starts the commissioning at the installation (the
 *  same Activate): `productiveS` seconds of PRODUCTIVE commissioning — both feeders powered — destroy the Heart;
 *  progress pauses while either feeder is unpowered or knocked out, and `stallS` consecutive unproductive seconds
 *  interrupt the attempt (both tuning candidates, candidates.ts). Reinforcement packets are requested once per attempt
 *  and threshold (25 / 50 / 75 % of productive progress), shown on their approach for `approachS` before the births,
 *  keyed `${attempt}:${threshold}` so pausing, loading or oscillating power never duplicates one. An interruption
 *  keeps the installation's materials (charged once), the cabinet deliveries, the pole run and every machine; the
 *  retry is the same Start at the installation. Completion destroys the Heart: the block's commissioning ends at once
 *  (the block sim's ordinary Contested → Held step), the rail-yard project restores and its reward is granted exactly
 *  once by project.ts (RI-05) — the transport capability, not a chest of items (plan §9.1).
 *
 *  The Heart is a candidate layer (D-RI-5): absent on every state `enableHeart` has not touched — the benchmark, the
 *  snapshot and E-hour never call it; `?heart=1`, E-heart and heart.test.ts do. Its numbers are candidates.ts's,
 *  outside SimConfig. Everything here is a pure function of the state; the game only draws it and sends the commands
 *  (`deliver` with `cabinet`, `activate`, `repairCabinet`, `abort`).
 *
 *  Implementation defaults (RI-06, reversible — recorded on D-RI-6): the cabinets' tiles (the yard's emergence points
 *  toward its first two Dark neighbours in block order, moved MARGIN_TILES + 1 tiles into the lot); a cabinet is
 *  "connected" when a live pole reaches its tile and the grid has supply (flow.ts `fieldPowered`'s rule); a Crawler
 *  arriving at a cabinet knocks it out (no damage model — it is repaired like a lamp, for REPAIR_COPPER); the
 *  commissioning wakes no bloom (its packets are its reinforcements); packets are Crawlers only — the Stalker layer,
 *  when on, fields its own; the packet's approach alternates between the cabinets by threshold; the Heart block's
 *  commissioning has no ordinary burn-off (GDD §28.8: "a boss's commissioning replaces its block's ordinary burn-off
 *  timer"). */
import { SimState, CONTESTED, DARK, HELD } from './types';
import { CANDIDATES, HeartCandidate } from './candidates';
import { FlowState, poleReaches, poleGrid, rubbleAt, machineAt, REPAIR_COPPER } from './flow';
import { ground, inGround, inReach, substationOwner } from './ground';
import { emergencePoint } from './emergence';
import { drop as pocketDrop } from './engineer';
import { MARGIN_TILES } from './tiles';
import { passable } from './walk';
import { effectiveSupply, interruptContested } from './sim';
import { heartBirth } from './threat';
import { syncProjects } from './project';

export interface HeartCabinet {
  /** The Dark neighbour whose approach this cabinet serves; its packets are born on that neighbour's frontage facing the yard. */
  approach: number;
  /** That neighbour's emergence point facing the yard (the approach the view marks before a packet's births), -1 if none. */
  point: number;
  x: number; y: number;
  /** Restoration materials delivered so far (ledger `committed` until the Heart is destroyed, then spent — once). */
  delivered: { steel: number; copper: number };
  /** Knocked out by a Crawler's arrival: unpowered until repaired. */
  down: boolean;
}
export interface HeartPacket { attempt: number; threshold: number; at: number; cabinet: number; cr: number }
export interface HeartStats {
  attempts: number; interrupted: number; aborted: number; packets: number; born: number; knockouts: number; repairs: number;
  productiveS: number; stalledS: number;
}
export interface HeartState {
  cand: HeartCandidate;
  /** The block whose installation the Heart occupies (the rail yard). */
  site: number;
  cabinets: HeartCabinet[];
  /** The running attempt's activation id (-1: none running). */
  attempt: number;
  /** Productive seconds this attempt, and consecutive unproductive seconds this attempt. */
  progress: number; stall: number;
  /** The installation's materials, charged on the first Start and never again (plan §9.2 default 10). */
  charged: boolean; charge: { steel: number; copper: number };
  /** `${attempt}:${threshold}` → the second the packet was requested. */
  requested: Record<string, number>;
  /** Requested packets whose bodies are not all born yet. */
  pending: HeartPacket[];
  destroyed: boolean; destroyedAt: number;
  stats: HeartStats;
}

export function heartOf(st: SimState): HeartState | undefined { return st.flow?.heart; }
/** The Heart occupies block `bi` and still stands (its rules apply to the block's claim). */
export function heartAt(st: SimState, bi: number): HeartState | undefined {
  const H = st.flow?.heart;
  return H && H.site === bi && !H.destroyed ? H : undefined;
}

function cabinetFree(st: SimState, site: number, x: number, y: number): boolean {
  const G = ground(st);
  if (!inGround(G, x, y) || G.owner[y * G.tw + x] !== site) return false;
  if (rubbleAt(st, x, y) || machineAt(st, x, y) || substationOwner(st, x, y) >= 0) return false;
  return passable(st, x, y);
}
/** The cabinet's tile for a kerb point of the yard: MARGIN_TILES + 1 tiles into the lot along the dominant axis toward
 *  the lot's centre, then the nearest free lot tile (ring ≤ 4). */
function cabinetTile(st: SimState, site: number, kx: number, ky: number): [number, number] | null {
  const bg = ground(st).blocks[site], cx = (bg.x0 + bg.x1) / 2, cy = (bg.y0 + bg.y1) / 2;
  const dx = cx - kx, dy = cy - ky, inward = MARGIN_TILES + 1, alongX = Math.abs(dx) >= Math.abs(dy);
  const gx = alongX ? kx + Math.sign(dx) * inward : kx, gy = alongX ? ky : ky + Math.sign(dy) * inward;
  for (let ring = 0; ring <= 4; ring++) for (let oy = -ring; oy <= ring; oy++) for (let ox = -ring; ox <= ring; ox++) {
    if (Math.max(Math.abs(ox), Math.abs(oy)) !== ring) continue;
    if (cabinetFree(st, site, gx + ox, gy + oy)) return [gx + ox, gy + oy];
  }
  return null;
}

/** Switch the Heart on for this state: it occupies the rail yard's installation, with one feeder cabinet toward each
 *  of the yard's first two Dark neighbours (block order). Idempotent. Null off the tile layer, without a yard, or when
 *  two approaches cannot be found. Never called by the benchmark, the snapshot or the hour bot's default. */
export function enableHeart(st: SimState, cand: HeartCandidate = CANDIDATES.heart): HeartState | null {
  const f = st.flow;
  if (!f || st.lattice) return null;
  if (f.heart) return f.heart;
  const site = ground(st).railYard;
  if (site < 0) return null;
  const cabinets: HeartCabinet[] = [];
  for (const ni of [...st.nb[site]].sort((a, b) => a - b)) {
    if (cabinets.length >= 2) break;
    if (st.blocks[ni].state !== DARK) continue;
    const mine = emergencePoint(st, site, ni);
    if (!mine) continue;
    const tile = cabinetTile(st, site, mine.tx, mine.ty);
    if (!tile || cabinets.some(c => c.x === tile[0] && c.y === tile[1])) continue;
    cabinets.push({ approach: ni, point: emergencePoint(st, ni, site)?.id ?? -1, x: tile[0], y: tile[1], delivered: { steel: 0, copper: 0 }, down: false });
  }
  if (cabinets.length < 2) return null;
  f.heart = {
    cand: { ...cand, thresholds: [...cand.thresholds], packets: [...cand.packets], cabinet: { ...cand.cabinet } },
    site, cabinets, attempt: -1, progress: 0, stall: 0, charged: false, charge: { steel: 0, copper: 0 }, requested: {}, pending: [],
    destroyed: false, destroyedAt: -1,
    stats: { attempts: 0, interrupted: 0, aborted: 0, packets: 0, born: 0, knockouts: 0, repairs: 0, productiveS: 0, stalledS: 0 },
  };
  return f.heart;
}

/** The cabinet on a tile (its index), -1 if none. */
export function cabinetAt(st: SimState, tx: number, ty: number): number {
  const H = st.flow?.heart;
  return H ? H.cabinets.findIndex(c => c.x === tx && c.y === ty) : -1;
}
export function cabinetSupplied(H: HeartState, k: number): boolean {
  const c = H.cabinets[k];
  return !!c && c.delivered.steel >= H.cand.cabinet.steel && c.delivered.copper >= H.cand.cabinet.copper;
}
/** A feeder is powered: not knocked out, a live pole reaches its tile, the grid has supply (flow.ts `fieldPowered`'s rule). */
export function cabinetConnected(st: SimState, k: number): boolean {
  const H = st.flow?.heart, c = H?.cabinets[k];
  if (!H || !c || c.down) return false;
  return effectiveSupply(st) > 0 && poleReaches(st, poleGrid(st).on, c.x, c.y, 1);
}
/** A cabinet's one-line state for the view and the bot. */
export function describeCabinet(st: SimState, k: number): string {
  const H = st.flow?.heart, c = H?.cabinets[k];
  if (!H || !c) return '';
  const need = H.cand.cabinet, s: string[] = [];
  if (c.delivered.steel < need.steel) s.push(`${need.steel - c.delivered.steel} steel`);
  if (c.delivered.copper < need.copper) s.push(`${need.copper - c.delivered.copper} Cu`);
  const state = H.destroyed ? 'restored' : c.down ? `knocked out — repair it (E, ${REPAIR_COPPER} Cu)` : s.length ? `needs ${s.join(' + ')} (E delivers)` : cabinetConnected(st, k) ? 'supplied and powered' : 'supplied, not on the grid — string a pole to it';
  return `Feeder cabinet ${k + 1} (toward block ${c.approach}) · ${state}`;
}

export interface HeartCheck { ok: boolean; reason: string }
/** What still stops the commissioning at the Heart's installation ('' when nothing): the cabinets' materials, then
 *  each cabinet's power. Ok when the Heart is destroyed (the ordinary claim rules apply from then on). */
export function heartCheck(st: SimState): HeartCheck {
  const H = st.flow?.heart;
  if (!H || H.destroyed) return { ok: true, reason: '' };
  const need = H.cand.cabinet;
  for (let k = 0; k < H.cabinets.length; k++) {
    const c = H.cabinets[k], s: string[] = [];
    if (c.delivered.steel < need.steel) s.push(`${need.steel - c.delivered.steel} steel`);
    if (c.delivered.copper < need.copper) s.push(`${need.copper - c.delivered.copper} Cu`);
    if (s.length) return { ok: false, reason: `feeder cabinet ${k + 1} needs ${s.join(' + ')} delivered` };
  }
  for (let k = 0; k < H.cabinets.length; k++) {
    if (H.cabinets[k].down) return { ok: false, reason: `feeder cabinet ${k + 1} is knocked out — repair it (${REPAIR_COPPER} Cu)` };
    if (!cabinetConnected(st, k)) return { ok: false, reason: `feeder cabinet ${k + 1} is not on the live grid — string a pole to it` };
  }
  return { ok: true, reason: '' };
}

export interface CabinetCheck { ok: boolean; reason: string; moved: number }
/** Restoration materials from the pockets into feeder cabinet `k`, within reach, up to what it still needs. They sit
 *  committed there — neither in the pockets nor spent — until the Heart is destroyed; an interruption keeps them. */
export function deliverToCabinet(st: SimState, k: number, item: string, n: number): CabinetCheck {
  const H = st.flow?.heart, c = H?.cabinets[k];
  if (!H || !c) return { ok: false, reason: 'no feeder cabinet there', moved: 0 };
  if (H.destroyed) return { ok: false, reason: 'the Heart is destroyed — the cabinet needs nothing', moved: 0 };
  if (item !== 'steel' && item !== 'copper') return { ok: false, reason: 'a cabinet takes steel and copper', moved: 0 };
  if (!inReach(st, c.x, c.y, 1)) return { ok: false, reason: 'walk closer to the cabinet', moved: 0 };
  const room = H.cand.cabinet[item] - c.delivered[item];
  if (room <= 0) return { ok: false, reason: `it has its ${H.cand.cabinet[item]} ${item === 'copper' ? 'Cu' : 'steel'}`, moved: 0 };
  const moved = pocketDrop(st.engineer, item, Math.min(Math.floor(n), room));
  if (moved <= 0) return { ok: false, reason: `no ${item} in the pockets`, moved: 0 };
  c.delivered[item] += moved;
  syncProjects(st);
  return { ok: true, reason: '', moved };
}
/** Why cabinet `k` cannot be repaired now ('' when it can). */
export function cabinetRepairCheck(st: SimState, k: number): HeartCheck {
  const H = st.flow?.heart, c = H?.cabinets[k];
  if (!H || !c) return { ok: false, reason: 'no feeder cabinet there' };
  if (!c.down) return { ok: false, reason: 'the cabinet is not knocked out' };
  if (!inReach(st, c.x, c.y, 1)) return { ok: false, reason: 'walk closer to the cabinet' };
  if ((st.engineer.inv.copper ?? 0) < REPAIR_COPPER) return { ok: false, reason: `no copper in the pockets (a repair is ${REPAIR_COPPER} Cu)` };
  return { ok: true, reason: '' };
}
/** Repair a knocked-out cabinet from the pockets (REPAIR_COPPER, counted with the lamp repairs in `flow.repairs` — the
 *  ledger's sink). The commissioning, if running, resumes on its own once both feeders are powered again. */
export function repairCabinet(st: SimState, k: number): HeartCheck {
  const chk = cabinetRepairCheck(st, k);
  if (!chk.ok) return chk;
  const f = st.flow as FlowState, H = f.heart as HeartState, c = H.cabinets[k], b = st.blocks[H.site];
  pocketDrop(st.engineer, 'copper', REPAIR_COPPER);
  c.down = false;
  f.repairs = (f.repairs ?? 0) + 1;
  H.stats.repairs++;
  st.events.push({ type: 'heart', t: st.t, what: 'repair', attempt: H.attempt, x: b.x, y: b.y, cabinet: k });
  syncProjects(st);
  return chk;
}
/** A Crawler's arrival at cabinet `k` (threat.ts): the feeder is knocked out until repaired. */
export function knockOutCabinet(st: SimState, k: number): void {
  const H = st.flow?.heart, c = H?.cabinets[k];
  if (!H || !c || c.down) return;
  const b = st.blocks[H.site];
  c.down = true;
  H.stats.knockouts++;
  st.events.push({ type: 'heart', t: st.t, what: 'knockout', attempt: H.attempt, x: b.x, y: b.y, cabinet: k });
}

/** `activate` (flow.ts) started an attempt on the Heart's block: the attempt is its activation id. */
export function heartStarted(st: SimState, id: number): void {
  const H = st.flow?.heart;
  if (!H || H.destroyed) return;
  const b = st.blocks[H.site];
  H.attempt = id; H.progress = 0; H.stall = 0; H.pending = [];
  H.stats.attempts++;
  st.events.push({ type: 'heart', t: st.t, what: 'start', attempt: id, x: b.x, y: b.y });
}
/** The running attempt ends without completing: the block goes back to Dark with everything on it kept
 *  (sim.ts `interruptContested`), untriggered packets are dropped, productive progress resets for the retry. Bodies
 *  already born stay (plan §9.2 default 11: never duplicated or replenished by restarting). */
export function interruptHeart(st: SimState, why: 'stalled' | 'aborted' | 'left'): boolean {
  const H = st.flow?.heart;
  if (!H || H.attempt < 0) return false;
  const b = st.blocks[H.site], attempt = H.attempt;
  H.attempt = -1; H.pending = []; H.progress = 0; H.stall = 0;
  if (why === 'aborted') H.stats.aborted++; else H.stats.interrupted++;
  interruptContested(st, H.site);
  st.events.push({ type: 'heart', t: st.t, what: why === 'aborted' ? 'aborted' : 'interrupted', attempt, x: b.x, y: b.y, why });
  syncProjects(st);
  return true;
}
/** The player's explicit abort (plan §9.2 default 9). */
export function abortHeart(st: SimState): HeartCheck {
  const H = st.flow?.heart;
  if (!H) return { ok: false, reason: 'no Heart here' };
  if (H.attempt < 0) return { ok: false, reason: 'no commissioning is running' };
  interruptHeart(st, 'aborted');
  return { ok: true, reason: '' };
}

/** Packet bodies alive (born by `heartBirth`: no ring edge, the Heart's block as their target). */
export function heartBodies(st: SimState): number {
  const f = st.flow, H = f?.heart;
  if (!f || !H || !f.threat) return 0;
  let n = 0;
  for (const c of f.threat.crawlers) if (c.edge < 0 && c.to === H.site) n++;
  return n;
}
function complete(st: SimState, H: HeartState): void {
  const b = st.blocks[H.site];
  const attempt = H.attempt;
  H.destroyed = true; H.destroyedAt = st.t; H.attempt = -1; H.pending = []; H.progress = H.cand.productiveS; H.stall = 0;
  // the cabinets' materials are spent now — once (ledger: committed → spent)
  for (const c of H.cabinets) {
    st.stats.spentSteel = (st.stats.spentSteel ?? 0) + c.delivered.steel;
    st.stats.spentCopper = (st.stats.spentCopper ?? 0) + c.delivered.copper;
    c.delivered = { steel: 0, copper: 0 };
    c.down = false;
  }
  b.contestUntil = st.t;   // the block sim's next step turns it Held: the project restores and the reward is granted there, once (RI-05)
  st.events.push({ type: 'heart', t: st.t, what: 'destroyed', attempt, x: b.x, y: b.y });
  syncProjects(st);
}
/** One tile tick of the encounter (stepFlow, after the threat's tick): productive progress or stall, the packet
 *  requests at the thresholds, the births at their time within the population bound, completion, interruption. */
export function heartTick(st: SimState, dt: number): void {
  const f = st.flow, H = f?.heart;
  if (!f || !H || H.destroyed || H.attempt < 0) return;
  const b = st.blocks[H.site], cand = H.cand;
  if (b.state !== CONTESTED) { interruptHeart(st, 'left'); return; }   // a load or a fall took the block out of commissioning
  let productive = true;
  for (let k = 0; k < H.cabinets.length; k++) if (!cabinetConnected(st, k)) { productive = false; break; }
  if (productive) {
    H.progress += dt; H.stall = 0; H.stats.productiveS += dt;
    const pct = 100 * H.progress / cand.productiveS;
    for (let i = 0; i < cand.thresholds.length; i++) {
      const th = cand.thresholds[i], key = `${H.attempt}:${th}`;
      if (pct < th || H.requested[key] !== undefined) continue;
      H.requested[key] = st.t;
      const k = i % H.cabinets.length, cr = cand.packets[Math.min(i, cand.packets.length - 1)];
      H.pending.push({ attempt: H.attempt, threshold: th, at: st.t + cand.approachS, cabinet: k, cr });
      H.stats.packets++;
      st.events.push({ type: 'heart', t: st.t, what: 'packet', attempt: H.attempt, x: b.x, y: b.y, threshold: th, cabinet: k, n: cr });
    }
  } else { H.stall += dt; H.stats.stalledS += dt; }
  if (H.pending.length) {
    let alive = heartBodies(st);
    for (const p of H.pending) {
      if (p.at > st.t) continue;
      let born = 0;
      while (p.cr > 0 && alive < cand.maxAlive && heartBirth(st, H.cabinets[p.cabinet].approach, H.site, `${p.attempt}:${p.threshold}`)) { p.cr--; alive++; born++; }
      if (born > 0) { H.stats.born += born; st.events.push({ type: 'heart', t: st.t, what: 'born', attempt: p.attempt, x: b.x, y: b.y, threshold: p.threshold, cabinet: p.cabinet, n: born }); }
      if (p.cr > 0 && alive < cand.maxAlive) { p.cr = 0; }   // no birth tile (no emergence point that way): the packet is spent, never retried
    }
    H.pending = H.pending.filter(p => p.cr > 0);
  }
  if (H.progress >= cand.productiveS - 1e-9) { complete(st, H); return; }
  if (H.stall >= cand.stallS - 1e-9) interruptHeart(st, 'stalled');
}

/** One line for the UI, the bot and the harness: the objective, the active failure condition, the next corrective
 *  action (plan §9.4: "the player can identify objective, active failure condition and next corrective action"). */
export function describeHeart(st: SimState): string {
  const H = st.flow?.heart;
  if (!H) return '';
  const b = st.blocks[H.site], cand = H.cand, pct = Math.round(100 * H.progress / cand.productiveS);
  if (H.destroyed) return `Junction Heart · destroyed after ${H.stats.attempts} attempt(s) · the rail yard is ${b.state === HELD ? 'operational' : 'restored'}`;
  if (H.attempt >= 0) {
    for (let k = 0; k < H.cabinets.length; k++) {
      if (cabinetConnected(st, k)) continue;
      const c = H.cabinets[k], why = c.down ? `feeder cabinet ${k + 1} knocked out — repair it (E on it, ${REPAIR_COPPER} Cu)` : `feeder cabinet ${k + 1} unpowered — restore its pole run or the grid's supply`;
      return `Junction Heart · commissioning ${pct} % · PAUSED: ${why} · interrupted in ${Math.ceil(cand.stallS - H.stall)} s`;
    }
    const next = cand.thresholds.find(th => H.requested[`${H.attempt}:${th}`] === undefined);
    const packets = next !== undefined ? `reinforcements at ${next} %` : 'no more reinforcements';
    return `Junction Heart · commissioning ${pct} % · both feeders powered · ${packets} · ${Math.ceil(cand.productiveS - H.progress)} s of productive commissioning left · X aborts`;
  }
  const chk = heartCheck(st), stage = H.stats.attempts ? 'interrupted' : 'preparing';
  if (!chk.ok) return `Junction Heart · ${stage} · ${chk.reason}`;
  return `Junction Heart · ${H.stats.attempts ? 'ready to retry' : 'ready'} · supply the installation and Start commissioning at the substation (E)`;
}
