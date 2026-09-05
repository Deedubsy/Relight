/** Prompt B M4 — threat, and the rifle, at tile level (run name B-M4-threat).
 *
 *  The block sim still decides *how much* rot arrives (§7's blooms, §11's 15 s arrival, the 40-arrival rule); with
 *  the tile layer on a city, every crawler it would have fed or let through is instead born on the segment ridge
 *  facing the held block and walks a per-face flow field — the nearest lit lamp, then a turret, then the substation
 *  (§7, §17) — never leaving the two blocks the edge joins. Turrets shoot what is within 9 tiles out of their own
 *  hoppers; the rifle shoots what the aim line crosses; whatever reaches the substation is an unfed arrival and the
 *  block model turns the substation off at 40 exactly as before ("tile events drive the same block transitions").
 *
 *  Retaliation only (D5): a crawler keeps its chain and turns on the engineer only when shot by them or when the
 *  engineer stands in its path; then 5 HP/s at arm's reach until it dies, the engineer is knocked down, or the
 *  engineer leaves the crawler's two blocks.
 *
 *  GAME-ASSUMPTION constants, every one a human's to move (see the M4 slice report's table):
 *   ROUND_DMG 4 (12 HP ÷ 3 rounds a crawler; 40 ÷ 10 a shade) · CONTACT_R 1.2 tiles · DANGER_R 3 tiles ·
 *   PATH_R 0.9 tiles (the engineer "stands in the path") · EAT_R 1.6 (a lamp is eaten from the next tile) ·
 *   STUCK_S 30 (a crawler that cannot move for 30 s is gone) · shades walk straight to the substation.
 *
 *  RI-04 (plan §6, §7): a body is born at the Dark block's *emergence point* for its edge (emergence.ts — one kerb
 *  frontage per block and street, stable ids, never a tile the Held block owns or faces) and remembers it (`origin`);
 *  every body records the direction of its last move (`dir`), a crawler's current target is a query
 *  (`crawlerTarget`), a shade leaves a trace of the tiles it crossed (`trail`, `shadeTraces`) so its approach can
 *  be read off unlit ground without being lit; the Stalker (stalker.ts) rides this tick when `enableStalkers` has put
 *  its candidate on the state (`stalk`), and the turrets and the rifle treat it as one more body. */
import { SimState, Engineer, HELD } from './types';
import { TURRET } from './constants';
import { edgeFrom, edgeTo } from './graph';
import { ground, inGround } from './ground';
import { passable, spendRound } from './walk';
import { emergencePoint, birthTile } from './emergence';
import { hash01 } from './prng';
import { moveTo, stepToward, NX, NY, NC, headingWord } from './move';
import { Stalker, StalkerLayer, newStalkerLayer, spawnStalkers, damageStalker, tickStalkers } from './stalker';
import { CANDIDATES, StalkerCandidate } from './candidates';
import { heartAt, cabinetAt, knockOutCabinet } from './heart';   // RI-06
import { hurt, threatHooks, RETALIATE_HP_PER_S, RIFLE_RANGE, RIFLE_HIT_RADIUS, rifleRate } from './engineer';
import { ENEMIES } from './enemies';
import { TURRET_RANGE, TURRET_ROUNDS_PER_S } from './recipes';
import { FlowState, faceSub, blockLights, litAt, TURRET_FLASH_S, machineRunning } from './flow';

export const ROUND_DMG = TURRET.roundDmg;   // constants.ts (§7: 4 HP a round)
export const CONTACT_R = 1.2, DANGER_R = 3, PATH_R = 0.9, EAT_R = 1.6, STUCK_S = 30;
const SPEED: Record<Crawler['kind'], number> = { crawler: ENEMIES[0].tilesPerSec, shade: ENEMIES[1].tilesPerSec };
const HP: Record<Crawler['kind'], number> = { crawler: ENEMIES[0].hp, shade: ENEMIES[1].hp };

/** Target class along the chain: 0 the nearest lit lamp, 1 a turret, 2 the substation. */
export type Cls = 0 | 1 | 2;
export interface Crawler {
  id: number; kind: 'crawler' | 'shade';
  x: number; y: number;      // tile space, centre of the body
  hp: number;
  edge: number;              // the engagement's edge id (held block → dark block)
  from: number; to: number;  // dark block it came from, held block it walks into
  cls: Cls;
  onPlayer: boolean;         // retaliation: chasing the engineer
  escaped: boolean;          // born past a stand-in edge's abstract turrets (D-P4-9): straight to the substation
  born: number;
  stuck: number;             // seconds without progress
  /** RI-04: the emergence point it was born at (emergence.ts id; absent on a body placed by hand or before RI-04). */
  origin?: number;
  /** RI-06: the Heart packet this body belongs to (`${attempt}:${threshold}`); such a body has no ring edge (`edge` -1). */
  packet?: string;
  /** RI-04: unit direction of its last move, for the world view's heading tick. */
  dir?: [number, number];
  /** RI-04 (shades): the last TRACE_N tiles it crossed and when — its trace on unlit ground (`shadeTraces`). */
  trail?: number[]; trailT?: number[];
}
export interface Fight { edge: number; t: number; rounds: number; kills: number; held: boolean | null; bx: number; by: number }
export interface ThreatStats {
  spawned: number; shades: number; turretKills: number; rifleKills: number; arrivals: number; shadeArrivals: number;
  lampsEaten: number; turned: number; contactS: number; lost: number;
}
export interface ThreatState {
  next: number;
  crawlers: Crawler[];
  /** Fractional arrivals per edge id waiting to become whole crawlers / shades. */
  acc: Record<number, [number, number]>;
  /** Tiles whose light a crawler ate (streetlight tile, or a Lamp's tile). M5: `repairLight` (E on it) takes a tile back out. */
  broken: number[];
  fights: Fight[];
  dangerS: number; shotAt: number;
  stats: ThreatStats;
  /** RI-04: bodies born per emergence point id (a discovered site's "local activity"). */
  born: Record<number, number>;
  /** RI-04: the Stalker candidate layer — absent on every state `enableStalkers` has not touched (D-RI-5). */
  stalk?: StalkerLayer;
}
/** RI-04: how long a shade's trace stays readable, and how many tiles of it are kept. */
export const TRACE_S = 6, TRACE_N = 8;

export function newThreat(): ThreatState {
  return { next: 1, crawlers: [], acc: {}, broken: [], fights: [], dangerS: 0, shotAt: -1, born: {},
    stats: { spawned: 0, shades: 0, turretKills: 0, rifleKills: 0, arrivals: 0, shadeArrivals: 0, lampsEaten: 0, turned: 0, contactS: 0, lost: 0 } };
}
export function threatOf(f: FlowState): ThreatState { return (f.threat ??= newThreat()); }
/** The tile threat runs on a city with the flow layer; the lattice keeps the block model's arithmetic. */
export function threatActive(st: SimState): boolean { return !!st.flow && !st.lattice; }

// ------------------------------------------------------------------ spawning at the emergence point

/** RI-04: what the block model lets arrive on an edge is born at the Dark block's emergence point for that edge
 *  (before RI-04: a seeded hash along the segment ridge). No point — no shared street — and the block model keeps
 *  its arithmetic, as before. */
function spawn(st: SimState, edgeId: number, cr: number, sh: number, escaped: boolean): boolean {
  if (!threatActive(st)) return false;
  const f = st.flow!, T = threatOf(f);
  const held = edgeFrom(st, edgeId), dark = edgeTo(st, edgeId);
  const pt = emergencePoint(st, dark, held);
  if (!pt) return false;
  const acc = (T.acc[edgeId] ??= [0, 0]);
  acc[0] += cr; acc[1] += sh;
  T.born ??= {};
  const birth = (kind: Crawler['kind']): void => {
    const [tx, ty] = birthTile(st, pt, hash01(st.seed, st.t, edgeId, T.next));   // M4's seeded hash, now along the Dark frontage's kerb
    T.crawlers.push({ id: T.next++, kind, x: tx + 0.5, y: ty + 0.5, hp: HP[kind], edge: edgeId, from: dark, to: held,
      cls: kind === 'shade' || escaped ? 2 : 0, onPlayer: false, escaped, born: st.t, stuck: 0, origin: pt.id, dir: [0, 0] });
    T.born[pt.id] = (T.born[pt.id] ?? 0) + 1;
    T.stats.spawned++; if (kind === 'shade') T.stats.shades++;
  };
  while (acc[0] >= 1 - 1e-9) { acc[0] -= 1; birth('crawler'); }
  while (acc[1] >= 1 - 1e-9) { acc[1] -= 1; birth('shade'); }
  return true;
}
/** RI-06: one Crawler of a Heart packet (heart.ts), born on the approach's frontage facing the Heart's block — its
 *  emergence point, the same seeded birth tile as a wake's — with no ring edge: it walks the encounter's targets (a
 *  standing feeder cabinet, then the installation) and is shot, counted and cleaned up like any other body. False
 *  without an emergence point that way. */
export function heartBirth(st: SimState, from: number, to: number, packet: string): boolean {
  if (!threatActive(st)) return false;
  const f = st.flow!, T = threatOf(f), pt = emergencePoint(st, from, to);
  if (!pt) return false;
  T.born ??= {};
  const [tx, ty] = birthTile(st, pt, hash01(st.seed, st.t, from * 7919 + to, T.next));
  T.crawlers.push({ id: T.next++, kind: 'crawler', x: tx + 0.5, y: ty + 0.5, hp: HP.crawler, edge: -1, from, to, cls: 2, onPlayer: false, escaped: false, born: st.t, stuck: 0, origin: pt.id, dir: [0, 0], packet });
  T.born[pt.id] = (T.born[pt.id] ?? 0) + 1;
  T.stats.spawned++;
  return true;
}

// ------------------------------------------------------------------ the per-face flow field

interface Field { key: string; dist: Int32Array; targets: number[]; cls: Cls }
const fields = new WeakMap<FlowState, Map<string, Field>>();

/** The block a tile "belongs to" for the two-block rule: its lot, else the block its street faces. */
function ownerOf(G: ReturnType<typeof ground>, t: number): number { const o = G.owner[t]; return o >= 0 ? o : G.near[t]; }

/** Footprint tiles of this class's targets on block `to` (the first non-empty class from `cls` up). */
function targetsOf(st: SimState, to: number, cls: Cls): { tiles: number[]; cls: Cls } {
  const G = ground(st), f = st.flow!;
  for (let c = cls; c <= 2; c++) {
    const tiles: number[] = [];
    if (c === 0) {
      for (const l of blockLights(st, to)) if (l.lit) tiles.push(Math.floor(l.ty) * G.tw + Math.floor(l.tx));
    } else if (c === 1) {
      for (const m of f.machines) if (m.kind === 'turret' && G.near[m.y * G.tw + m.x] === to) footprint(G, m.x, m.y, m.size, tiles);
    } else {
      // RI-06: while the Heart stands its feeder cabinets are the target (a standing one — a knocked-out cabinet is passed over), then the installation
      const H = heartAt(st, to);
      if (H) for (const cb of H.cabinets) if (!cb.down && inGround(G, cb.x, cb.y)) tiles.push(cb.y * G.tw + cb.x);
      if (tiles.length) return { tiles, cls: 2 };
      const s = faceSub(st, to);
      if (s) footprint(G, s.x, s.y, s.size, tiles);
      else { const p = G.blocks[to].pole; tiles.push(p[1] * G.tw + p[0]); }   // GAME-ASSUMPTION: no substation (outskirts) → the block's pole tile
    }
    if (tiles.length) return { tiles, cls: c as Cls };
  }
  return { tiles: [], cls: 2 };
}
function footprint(G: ReturnType<typeof ground>, x: number, y: number, size: number, out: number[]): void {
  for (let yy = y; yy < y + size; yy++) for (let xx = x; xx < x + size; xx++) if (inGround(G, xx, yy)) out.push(yy * G.tw + xx);
}

/** Multi-source BFS from the targets' footprints over passable tiles of the two blocks, 8-connected without corner
 *  cutting; distances in tenths of a tile, -1 unreachable. GAME-ASSUMPTION: rebuilt at most once a block second per
 *  (face, class), so a machine placed mid-second reroutes the crawlers a moment late. */
function fieldFor(st: SimState, c: Crawler): Field {
  const f = st.flow!, T = threatOf(f);
  let m = fields.get(f); if (!m) fields.set(f, m = new Map());
  const id = `${c.to}|${c.from}|${c.cls}`, key = `${f.rev}|${T.broken.length}|${Math.floor(st.t)}|${st.blocks[c.to].subOn ? 1 : 0}`;
  const have = m.get(id);
  if (have && have.key === key) return have;
  const G = ground(st), tw = G.tw, n = tw * G.th;
  const { tiles, cls } = targetsOf(st, c.to, c.cls);
  const dist = new Int32Array(n).fill(-1);
  const q = new Int32Array(n); let qh = 0, qt = 0;
  const inRegion = (t: number): boolean => { const o = ownerOf(G, t); return o === c.to || o === c.from; };
  const seed = new Set<number>();
  for (const t of tiles) {
    const x = t % tw, y = Math.floor(t / tw);
    for (let k = 0; k < 8; k++) {
      const xx = x + NX[k], yy = y + NY[k];
      if (inGround(G, xx, yy) && passable(st, xx, yy)) seed.add(yy * tw + xx);
    }
    if (passable(st, x, y)) seed.add(t);   // a streetlight stands on a street tile
  }
  for (const t of seed) { dist[t] = 0; q[qt++] = t; }
  while (qh < qt) {
    const t = q[qh++], x = t % tw, y = Math.floor(t / tw), d = dist[t];
    for (let k = 0; k < 8; k++) {
      const xx = x + NX[k], yy = y + NY[k];
      if (!inGround(G, xx, yy)) continue;
      const u = yy * tw + xx;
      if (dist[u] >= 0 && dist[u] <= d + NC[k]) continue;
      if (!passable(st, xx, yy) || !inRegion(u)) continue;
      if (k >= 4 && !(passable(st, x + NX[k], y) && passable(st, x, y + NY[k]))) continue;
      if (dist[u] < 0) { dist[u] = d + NC[k]; q[qt++] = u; }
      else dist[u] = d + NC[k];
    }
  }
  const fld: Field = { key, dist, targets: tiles, cls };
  m.set(id, fld);
  return fld;
}

// ------------------------------------------------------------------ the tick

// RI-02: the turret's firing cooldown lives on the Machine (`cool`), so a saved state reloads mid-cooldown and replays
// identically; before RI-02 it sat in a WeakMap outside the state and a load reset it to 0.
const isCrawlerHeld = (st: SimState, c: Crawler): boolean =>
  // RI-06: a packet body (no ring edge) lives while the Heart stands — Dark between attempts too — or the block is Held
  c.edge < 0 ? !!heartAt(st, c.to) || st.blocks[c.to].state === HELD : st.blocks[c.to].state === HELD;

function tick(st: SimState, dt: number): void {
  if (!threatActive(st)) return;
  const f = st.flow!, T = threatOf(f), G = ground(st), tw = G.tw, e = st.engineer;
  const cs = T.crawlers;
  // crawlers whose block already fell (or was lost) have nothing left to walk to
  let w = 0;
  for (const c of cs) { if (isCrawlerHeld(st, c)) cs[w++] = c; else T.stats.lost++; }
  cs.length = w;
  const engUp = e.down < 0;
  const etx = Math.floor(e.x), ety = Math.floor(e.y), eOwner = inGround(G, etx, ety) ? ownerOf(G, ety * tw + etx) : -1;
  let danger = false;
  for (let i = 0; i < cs.length; i++) {
    const c = cs[i];
    if (c.kind === 'shade') trace(st, c);
    const dxE = e.x - c.x, dyE = e.y - c.y, dE = Math.hypot(dxE, dyE);
    if (engUp && dE <= DANGER_R) danger = true;
    // GAME-ASSUMPTION: a crawler already turned keeps chasing while the engineer is up and within its two blocks; leaving them ends it
    if (c.onPlayer && (!engUp || (eOwner !== c.to && eOwner !== c.from))) c.onPlayer = false;
    if (c.onPlayer) {
      if (dE <= CONTACT_R) {
        if (e.dash <= 0) { hurt(st, RETALIATE_HP_PER_S * dt); T.stats.contactS += dt; }   // GAME-ASSUMPTION: the dash's cover (D5) holds at tile level too
        continue;
      }
      stepToward(st, G, c, e.x, e.y, SPEED[c.kind] * dt);
      continue;
    }
    // the chain
    const fld = fieldFor(st, c);
    c.cls = fld.cls;
    const ctx = Math.floor(c.x), cty = Math.floor(c.y), ct = cty * tw + ctx;
    if (!inGround(G, ctx, cty)) { c.stuck += dt; if (c.stuck >= STUCK_S) { cs.splice(i--, 1); T.stats.lost++; } continue; }
    if (fld.dist[ct] === 0) {   // on a tile beside the target: finish the step to its centre, then arrive
      if (Math.hypot(ctx + 0.5 - c.x, cty + 0.5 - c.y) > 0.05) { moveTo(c, ctx + 0.5, cty + 0.5, SPEED[c.kind] * dt); continue; }
      arrive(st, T, c, fld); if (c.hp <= 0) cs.splice(i--, 1); continue;
    }
    let best = -1, bd = fld.dist[ct] >= 0 ? fld.dist[ct] : Infinity;
    for (let k = 0; k < 8; k++) {
      const xx = ctx + NX[k], yy = cty + NY[k];
      if (!inGround(G, xx, yy)) continue;
      const d = fld.dist[yy * tw + xx];
      if (d < 0 || d >= bd) continue;
      if (k >= 4 && !(passable(st, ctx + NX[k], cty) && passable(st, ctx, cty + NY[k]))) continue;
      bd = d; best = k;
    }
    if (best < 0) {
      // off the field (a tile a machine just took, or an unreachable target): try to step straight at the target
      const tt = fld.targets.length ? nearestTarget(fld.targets, tw, c.x, c.y) : -1;
      if (tt >= 0 && stepToward(st, G, c, tt % tw + 0.5, Math.floor(tt / tw) + 0.5, SPEED[c.kind] * dt)) c.stuck = 0;
      else c.stuck += dt;
      if (c.stuck >= STUCK_S) { cs.splice(i--, 1); T.stats.lost++; }
      continue;
    }
    const gx = ctx + NX[best] + 0.5, gy = cty + NY[best] + 0.5;
    // the engineer standing in the path: the crawler turns on them (D5 retaliation, second trigger)
    if (engUp && c.kind === 'crawler' && Math.hypot(e.x - gx, e.y - gy) <= PATH_R && dE <= 1.6) { turn(st, T, c, 'path'); continue; }
    c.stuck = 0;
    moveTo(c, gx, gy, SPEED[c.kind] * dt);
  }
  T.dangerS += danger ? dt : 0;
  if (T.stalk) tickStalkers(st, T, dt, CONTACT_R);   // RI-04: the Stalker candidate, when a state carries it
  // the turrets: 5 rounds/s each at the nearest crawler within 9 tiles, shades only where the tile is lit
  for (const m of f.machines) {
    if (m.kind !== 'turret') continue;
    let cd = (m.cool ?? 0) - dt;
    if (cd < -1) cd = -1;
    const cx = m.x + m.size / 2, cy = m.y + m.size / 2;
    const bi = G.near[m.y * tw + m.x];
    // RI-03: a turret fires where it runs — on its Held block, or on the claim front as a field device (flow.ts `running`)
    while (cd <= 0 && (m.inv.rounds ?? 0) >= 1 - 1e-9 && bi >= 0 && machineRunning(st, m)) {
      const c = nearest(st, T, cx, cy, TURRET_RANGE);
      if (!c) { cd = 0; break; }
      m.inv.rounds = Math.max(0, (m.inv.rounds ?? 0) - 1); m.out += 1; m.timer = TURRET_FLASH_S; f.stats.fired += 1;
      cd += 1 / TURRET_ROUNDS_PER_S;
      if (damage(st, T, c) && c.kind !== 'stalker') T.stats.turretKills++;
    }
    m.cool = cd;
  }
  // the bots' rifle (D5): a bot standing on a red edge fires at that edge's crawlers within reach of the rifle
  if (e.firing >= 0 && !e.aim && engUp && e.dash <= 0 && e.cooldown <= 0) {
    const c = nearest(st, T, e.x, e.y, RIFLE_RANGE, e.firing);
    if (c && spendRound(st, e)) { hit(st, T, e, c); e.cooldown = 1 / rifleRate(e); }
  }
  resolveFights(st, T);
}

/** A body a round can hit: M4's crawlers and shades, RI-04's Stalkers. */
export type Target = Crawler | Stalker;
/** The nearest hittable body within `r`; `edge` restricts the crawlers to one engagement (the bots' rifle on a red
 *  edge) — a Stalker, which belongs to no edge, is in reach of that rifle regardless (RI-04: it is on the bot). */
function nearest(st: SimState, T: ThreatState, x: number, y: number, r: number, edge = -1): Target | null {
  let best: Target | null = null, bd = r * r;
  for (const c of T.crawlers) {
    if (edge >= 0 && c.edge !== edge) continue;
    const dx = c.x - x, dy = c.y - y, d2 = dx * dx + dy * dy;
    if (d2 > bd) continue;
    if (c.kind === 'shade' && !litAt(st, Math.floor(c.x), Math.floor(c.y))) continue;   // §7: untargetable off lit tiles
    bd = d2; best = c;
  }
  if (T.stalk) for (const s of T.stalk.stalkers) {
    const dx = s.x - x, dy = s.y - y, d2 = dx * dx + dy * dy;
    if (d2 > bd) continue;
    bd = d2; best = s;
  }
  return best;
}
/** ROUND_DMG off a body; true when it died (a crawler leaves the list here, a Stalker through its own layer). */
function damage(st: SimState, T: ThreatState, c: Target): boolean {
  if (c.kind === 'stalker') return damageStalker(st, T.stalk!, c, ROUND_DMG);
  c.hp -= ROUND_DMG;
  if (c.hp > 0) return false;
  const i = T.crawlers.indexOf(c); if (i >= 0) T.crawlers.splice(i, 1);
  return true;
}
/** RI-04: a shade's trace — the tile it just entered, kept TRACE_N deep and TRACE_S long. */
function trace(st: SimState, c: Crawler): void {
  const tw = ground(st).tw, t = Math.floor(c.y) * tw + Math.floor(c.x);
  const tr = (c.trail ??= []), tt = (c.trailT ??= []);
  if (tr.length && tr[tr.length - 1] === t) return;
  tr.push(t); tt.push(st.t);
  if (tr.length > TRACE_N) { tr.shift(); tt.shift(); }
}
function nearestTarget(tiles: number[], tw: number, x: number, y: number): number {
  let best = -1, bd = Infinity;
  for (const t of tiles) { const d = Math.hypot(t % tw + 0.5 - x, Math.floor(t / tw) + 0.5 - y); if (d < bd) { bd = d; best = t; } }
  return best;
}

function turn(st: SimState, T: ThreatState, c: Crawler, cause: 'shot' | 'path'): void {
  if (c.onPlayer) return;
  c.onPlayer = true; T.stats.turned++;
  st.events.push({ type: 'retaliate', t: st.t, tx: Math.floor(c.x), ty: Math.floor(c.y), cause });
}

/** A crawler at the end of its current chain link: eat the lamp, pass the turret, or reach the substation. */
function arrive(st: SimState, T: ThreatState, c: Crawler, fld: Field): void {
  const G = ground(st), tw = G.tw, b = st.blocks[c.to], t = st.t;
  if (fld.cls === 0) {
    const tt = nearestTarget(fld.targets, tw, c.x, c.y);
    if (tt >= 0 && Math.hypot(tt % tw + 0.5 - c.x, Math.floor(tt / tw) + 0.5 - c.y) <= EAT_R) {
      if (!T.broken.includes(tt)) T.broken.push(tt);
      T.stats.lampsEaten++;
      st.events.push({ type: 'lamp-eaten', t, x: b.x, y: b.y, tx: tt % tw, ty: Math.floor(tt / tw) });
    }
    return;   // the field rebuilds without that light; the next lit lamp, then the turrets
  }
  if (fld.cls === 1) { c.cls = 2; return; }   // GAME-ASSUMPTION: crawlers do not harm a turret (§7 gives them no such attack); it is the chain's waypoint
  // RI-06: at a feeder cabinet the arrival knocks the feeder out (repaired for REPAIR_COPPER); at the Heart's installation
  // before it is destroyed the body is spent — a block that is not Held has nothing to starve
  if (heartAt(st, c.to)) {
    const tt = nearestTarget(fld.targets, tw, c.x, c.y), k = tt >= 0 ? cabinetAt(st, tt % tw, Math.floor(tt / tw)) : -1;
    if (k >= 0) knockOutCabinet(st, k);
    if (k >= 0 || b.state !== HELD) { c.hp = 0; return; }
  }
  // the substation: an unfed arrival, counted by the block model's 40-arrival rule (§11)
  T.stats.arrivals++;
  if (c.kind === 'shade') T.stats.shadeArrivals++;
  st.stats.unfedTotal += 1;
  if (st.stats.firstUnfed < 0) st.stats.firstUnfed = t;
  if (b.unfedSince < 0) b.unfedSince = t;
  if (st.config.unfed === 'substation') {
    b.unfed += 1;
    if (c.kind === 'shade') b.shadeOff = Math.max(b.shadeOff, t) + 30;
  }
  const n = Math.round(b.unfed);
  if (n === 1 || n % 10 === 0) st.events.push({ type: 'arrival', t, x: b.x, y: b.y, n, of: st.config.unfedN, shade: c.kind === 'shade' });
  c.hp = 0;
}

// ------------------------------------------------------------------ the rifle

function fightFor(T: ThreatState, st: SimState, edge: number): Fight {
  for (let i = T.fights.length - 1; i >= 0; i--) if (T.fights[i].edge === edge && T.fights[i].held === null) return T.fights[i];
  const b = st.blocks[edgeFrom(st, edge)];
  const fg: Fight = { edge, t: st.t, rounds: 0, kills: 0, held: null, bx: b.x, by: b.y };
  T.fights.push(fg);
  return fg;
}
function hit(st: SimState, T: ThreatState, e: Engineer, c: Target): void {
  T.shotAt = st.t;
  if (c.kind === 'stalker') { if (damage(st, T, c)) e.kills++; return; }   // RI-04: no engagement, no retaliation — it is already on you
  if (c.edge < 0) { turn(st, T, c, 'shot'); if (damage(st, T, c)) { e.kills++; T.stats.rifleKills++; } return; }   // RI-06: a packet body has no edge engagement to charge
  const fg = fightFor(T, st, c.edge);
  fg.rounds++;
  turn(st, T, c, 'shot');
  if (damage(st, T, c)) { fg.kills++; e.kills++; T.stats.rifleKills++; }
}
/** An aimed round from (e.x, e.y) toward (ax, ay): the nearest crawler along the line within RIFLE_RANGE and
 *  RIFLE_HIT_RADIUS takes ROUND_DMG; a shade only on a lit tile. A miss is charged to the nearest engaged edge. */
function fire(st: SimState, e: Engineer, ax: number, ay: number): boolean {
  if (!threatActive(st)) return false;
  const T = threatOf(st.flow!);
  const dx = ax - e.x, dy = ay - e.y, L = Math.hypot(dx, dy);
  if (L < 1e-6) return true;
  const ux = dx / L, uy = dy / L;
  let best: Target | null = null, bestAlong = Infinity;
  const bodies: Target[] = T.stalk ? [...T.crawlers, ...T.stalk.stalkers] : T.crawlers;
  for (const c of bodies) {
    const px = c.x - e.x, py = c.y - e.y, along = px * ux + py * uy;
    if (along < 0 || along > RIFLE_RANGE || along >= bestAlong) continue;
    if (Math.abs(px * uy - py * ux) > RIFLE_HIT_RADIUS) continue;
    if (c.kind === 'shade' && !litAt(st, Math.floor(c.x), Math.floor(c.y))) continue;
    bestAlong = along; best = c;
  }
  if (best) hit(st, T, e, best);
  else { const c = nearest(st, T, e.x, e.y, RIFLE_RANGE); if (c && c.kind !== 'stalker' && c.edge >= 0) { fightFor(T, st, c.edge).rounds++; T.shotAt = st.t; } }
  return true;
}

/** A hand-fired engagement is over when its edge's held block fell or its substation went off on the 40-arrival
 *  rule (held = false), or no crawler of that edge is left and the block model has no more arrivals for it while
 *  the substation still runs (held = true). */
function resolveFights(st: SimState, T: ThreatState): void {
  for (const fg of T.fights) {
    if (fg.held !== null) continue;
    const b = st.blocks[edgeFrom(st, fg.edge)];
    if (b.state !== HELD || !b.subOn) { fg.held = false; continue; }
    if (T.crawlers.some(c => c.edge === fg.edge)) continue;
    if (st.engagements.some(en => en.id === fg.edge && (en.cr > 1e-9 || en.sh > 1e-9))) continue;
    if (st.t - fg.t >= 1) fg.held = true;
  }
}

function second(st: SimState): { danger: boolean; shot: boolean } {
  if (!threatActive(st)) return { danger: false, shot: false };
  const T = threatOf(st.flow!);
  const r = { danger: T.dangerS >= 0.5, shot: T.shotAt > st.t - 1 - 1e-9 };
  T.dangerS = 0;
  return r;
}

threatHooks.current = { tick, spawn, second, fire };

// ------------------------------------------------------------------ queries for the views

/** Crawlers walking into (or already on) block `bi`. */
export function crawlersOn(st: SimState, bi: number): Crawler[] {
  const T = st.flow?.threat; if (!T) return [];
  return T.crawlers.filter(c => c.to === bi);
}
export function crawlerAt(st: SimState, x: number, y: number, r = 0.8): Crawler | null {
  const T = st.flow?.threat; if (!T) return null;
  let best: Crawler | null = null, bd = r;
  for (const c of T.crawlers) {
    if (c.kind === 'shade' && !litAt(st, Math.floor(c.x), Math.floor(c.y))) continue;
    const d = Math.hypot(c.x - x, c.y - y); if (d < bd) { bd = d; best = c; }
  }
  return best;
}
/** RI-04: a crawler's current target as a tile — the engineer once it has turned, else the nearest footprint tile
 *  of its chain link on the block it walks into — and the word for it. */
export function crawlerTarget(st: SimState, c: Crawler): { tx: number; ty: number; what: 'you' | 'lamp' | 'turret' | 'substation' | 'cabinet' } | null {
  if (c.onPlayer) { const e = st.engineer; return { tx: Math.floor(e.x), ty: Math.floor(e.y), what: 'you' }; }
  if (!threatActive(st)) return null;
  const tw = ground(st).tw, { tiles, cls } = targetsOf(st, c.to, c.cls);
  const t = tiles.length ? nearestTarget(tiles, tw, c.x, c.y) : -1;
  if (t < 0) return null;
  return { tx: t % tw, ty: Math.floor(t / tw), what: cls === 0 ? 'lamp' : cls === 1 ? 'turret' : cabinetAt(st, t % tw, Math.floor(t / tw)) >= 0 ? 'cabinet' : 'substation' };   // RI-06: a feeder cabinet
}
export function describeCrawler(st: SimState, c: Crawler): string {
  const b = st.blocks[c.to], tg = crawlerTarget(st, c);
  const goal = c.onPlayer ? 'turned on you' : c.cls === 0 ? 'toward the nearest lit lamp' : c.cls === 1 ? 'toward a turret' : tg?.what === 'cabinet' ? 'toward a feeder cabinet' : 'toward the substation';
  // RI-04: heading, the target's tile and the emergence point it came from (plan §7: "direction and current target are inspectable")
  const heading = c.dir ? ` · heading ${headingWord(c.dir)}` : '';
  const at = tg && !c.onPlayer ? ` at (${tg.tx},${tg.ty})` : '';
  const from = (c.origin !== undefined ? ` · from emergence point ${c.origin}` : '') + (c.packet !== undefined ? ` · Heart reinforcement ${c.packet}` : '');   // RI-06
  return `${c.kind === 'shade' ? 'Shade' : 'Crawler'} · ${Math.max(0, c.hp)}/${HP[c.kind]} HP${heading} · ${goal}${at} on (${b.x},${b.y})${from}`;
}
/** RI-04: every shade's trace — the tiles it crossed within TRACE_S, with their age — the world view draws these on
 *  unlit ground as a trace, not as light (a lit tile is one the light layer says is lit, never a render effect). */
export function shadeTraces(st: SimState): { tx: number; ty: number; age: number }[] {
  const T = st.flow?.threat; if (!T) return [];
  const tw = ground(st).tw, out: { tx: number; ty: number; age: number }[] = [];
  for (const c of T.crawlers) {
    if (c.kind !== 'shade' || !c.trail || !c.trailT) continue;
    for (let k = 0; k < c.trail.length; k++) {
      const age = st.t - c.trailT[k];
      if (age <= TRACE_S) out.push({ tx: c.trail[k] % tw, ty: Math.floor(c.trail[k] / tw), age });
    }
  }
  return out;
}
/** RI-04: a shade within `r` tiles of a tile (the view flickers a lit lamp's glyph on it; the lamp stays lit). */
export function shadeNear(st: SimState, tx: number, ty: number, r = 3): boolean {
  const T = st.flow?.threat; if (!T) return false;
  return T.crawlers.some(c => c.kind === 'shade' && Math.hypot(c.x - tx - 0.5, c.y - ty - 0.5) <= r);
}
/** RI-04 (D-RI-5): switch the Stalker candidate on for this state — a copy of the candidate goes on the threat
 *  state, one Stalker is fielded at every Dark well block. Never called by the benchmark, the snapshot or the hour
 *  bot; `?stalker=1` and E-rifle-cand-stalker call it. Returns the layer, or null off the tile threat. */
export function enableStalkers(st: SimState, cand: StalkerCandidate = CANDIDATES.stalker): StalkerLayer | null {
  if (!threatActive(st)) return null;
  const T = threatOf(st.flow!);
  T.stalk ??= newStalkerLayer(st, cand);
  spawnStalkers(st, T.stalk);
  return T.stalk;
}
/** A light eaten by a crawler at this tile (`repairLight` in flow.ts, E on it, repairs it). */
export function lightEaten(st: SimState, tx: number, ty: number): boolean {
  const T = st.flow?.threat; if (!T) return false;
  return T.broken.includes(ty * ground(st).tw + tx);
}
