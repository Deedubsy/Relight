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
 *   STUCK_S 30 (a crawler that cannot move for 30 s is gone) · shades walk straight to the substation. */
import { SimState, Engineer, HELD } from './types';
import { edgeFrom, edgeTo } from './graph';
import { ground, inGround, cityGeomOf } from './ground';
import { segBetween } from './city';
import { hash01 } from './prng';
import { passable, spendRound } from './walk';
import { hurt, threatHooks, RETALIATE_HP_PER_S, RIFLE_RANGE, RIFLE_HIT_RADIUS, rifleRate } from './engineer';
import { ENEMIES } from './enemies';
import { TURRET_RANGE, TURRET_ROUNDS_PER_S } from './recipes';
import { FlowState, Machine, faceSub, blockLights, litAt, TURRET_FLASH_S } from './flow';

export const ROUND_DMG = 4;
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
}

export function newThreat(): ThreatState {
  return { next: 1, crawlers: [], acc: {}, broken: [], fights: [], dangerS: 0, shotAt: -1,
    stats: { spawned: 0, shades: 0, turretKills: 0, rifleKills: 0, arrivals: 0, shadeArrivals: 0, lampsEaten: 0, turned: 0, contactS: 0, lost: 0 } };
}
export function threatOf(f: FlowState): ThreatState { return (f.threat ??= newThreat()); }
/** The tile threat runs on a city with the flow layer; the lattice keeps the block model's arithmetic. */
export function threatActive(st: SimState): boolean { return !!st.flow && !st.lattice; }

// ------------------------------------------------------------------ spawning at the ridge

function spawn(st: SimState, edgeId: number, cr: number, sh: number, escaped: boolean): boolean {
  if (!threatActive(st)) return false;
  const f = st.flow!, T = threatOf(f), cg = cityGeomOf(st);
  const held = edgeFrom(st, edgeId), dark = edgeTo(st, edgeId);
  const sg = segBetween(cg, held, dark);
  if (!sg || !sg.ridge.length) return false;
  const acc = (T.acc[edgeId] ??= [0, 0]);
  acc[0] += cr; acc[1] += sh;
  const G = ground(st);
  const birth = (kind: Crawler['kind']): void => {
    const k0 = Math.floor(hash01(st.seed, st.t, edgeId, T.next) * sg.ridge.length);   // GAME-ASSUMPTION: the birth tile is a seeded hash along the ridge, the nearest passable tile from there
    let tx = -1, ty = -1;
    for (let k = 0; k < sg.ridge.length; k++) {
      const t = sg.ridge[(k0 + k) % sg.ridge.length], x = t % G.tw, y = Math.floor(t / G.tw);
      if (passable(st, x, y)) { tx = x; ty = y; break; }
    }
    if (tx < 0) { const t = sg.ridge[k0]; tx = t % G.tw; ty = Math.floor(t / G.tw); }
    T.crawlers.push({ id: T.next++, kind, x: tx + 0.5, y: ty + 0.5, hp: HP[kind], edge: edgeId, from: dark, to: held,
      cls: kind === 'shade' || escaped ? 2 : 0, onPlayer: false, escaped, born: st.t, stuck: 0 });
    T.stats.spawned++; if (kind === 'shade') T.stats.shades++;
  };
  while (acc[0] >= 1 - 1e-9) { acc[0] -= 1; birth('crawler'); }
  while (acc[1] >= 1 - 1e-9) { acc[1] -= 1; birth('shade'); }
  return true;
}

// ------------------------------------------------------------------ the per-face flow field

interface Field { key: string; dist: Int32Array; targets: number[]; cls: Cls }
const fields = new WeakMap<FlowState, Map<string, Field>>();
const NX = [0, 1, 0, -1, 1, 1, -1, -1], NY = [-1, 0, 1, 0, -1, 1, 1, -1], NC = [10, 10, 10, 10, 14, 14, 14, 14];

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

const cool = new WeakMap<Machine, number>();
const isCrawlerHeld = (st: SimState, c: Crawler): boolean => st.blocks[c.to].state === HELD;

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
  // the turrets: 5 rounds/s each at the nearest crawler within 9 tiles, shades only where the tile is lit
  for (const m of f.machines) {
    if (m.kind !== 'turret') continue;
    let cd = (cool.get(m) ?? 0) - dt;
    if (cd < -1) cd = -1;
    const cx = m.x + m.size / 2, cy = m.y + m.size / 2;
    const bi = G.near[m.y * tw + m.x];
    while (cd <= 0 && (m.inv.rounds ?? 0) >= 1 - 1e-9 && bi >= 0 && st.blocks[bi].state === HELD) {
      const c = nearest(st, cs, cx, cy, TURRET_RANGE);
      if (!c) { cd = 0; break; }
      m.inv.rounds = Math.max(0, (m.inv.rounds ?? 0) - 1); m.out += 1; m.timer = TURRET_FLASH_S; f.stats.fired += 1;
      cd += 1 / TURRET_ROUNDS_PER_S;
      c.hp -= ROUND_DMG;
      if (c.hp <= 0) { T.stats.turretKills++; cs.splice(cs.indexOf(c), 1); }
    }
    cool.set(m, cd);
  }
  // the bots' rifle (D5): a bot standing on a red edge fires at that edge's crawlers within reach of the rifle
  if (e.firing >= 0 && !e.aim && engUp && e.dash <= 0 && e.cooldown <= 0) {
    const c = nearest(st, cs, e.x, e.y, RIFLE_RANGE, e.firing);
    if (c && spendRound(st, e)) { hit(st, T, e, c); e.cooldown = 1 / rifleRate(e); }
  }
  resolveFights(st, T);
}

function nearest(st: SimState, cs: Crawler[], x: number, y: number, r: number, edge = -1): Crawler | null {
  let best: Crawler | null = null, bd = r * r;
  for (const c of cs) {
    if (edge >= 0 && c.edge !== edge) continue;
    const dx = c.x - x, dy = c.y - y, d2 = dx * dx + dy * dy;
    if (d2 > bd) continue;
    if (c.kind === 'shade' && !litAt(st, Math.floor(c.x), Math.floor(c.y))) continue;   // §7: untargetable off lit tiles
    bd = d2; best = c;
  }
  return best;
}
function nearestTarget(tiles: number[], tw: number, x: number, y: number): number {
  let best = -1, bd = Infinity;
  for (const t of tiles) { const d = Math.hypot(t % tw + 0.5 - x, Math.floor(t / tw) + 0.5 - y); if (d < bd) { bd = d; best = t; } }
  return best;
}
function moveTo(c: Crawler, gx: number, gy: number, step: number): void {
  const dx = gx - c.x, dy = gy - c.y, L = Math.hypot(dx, dy);
  if (L <= step) { c.x = gx; c.y = gy; } else { c.x += dx / L * step; c.y += dy / L * step; }
}
/** Greedy step toward a point over passable tiles (the chase, and the off-field fallback). */
function stepToward(st: SimState, G: ReturnType<typeof ground>, c: Crawler, gx: number, gy: number, step: number): boolean {
  const ctx = Math.floor(c.x), cty = Math.floor(c.y);
  let best = -1, bd = Math.hypot(gx - c.x, gy - c.y);
  if (Math.floor(gx) === ctx && Math.floor(gy) === cty) { moveTo(c, gx, gy, step); return true; }
  for (let k = 0; k < 8; k++) {
    const xx = ctx + NX[k], yy = cty + NY[k];
    if (!inGround(G, xx, yy) || !passable(st, xx, yy)) continue;
    if (k >= 4 && !(passable(st, ctx + NX[k], cty) && passable(st, ctx, cty + NY[k]))) continue;
    const d = Math.hypot(gx - xx - 0.5, gy - yy - 0.5);
    if (d < bd) { bd = d; best = k; }
  }
  if (best < 0) return false;
  moveTo(c, ctx + NX[best] + 0.5, cty + NY[best] + 0.5, step);
  return true;
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
function hit(st: SimState, T: ThreatState, e: Engineer, c: Crawler): void {
  const fg = fightFor(T, st, c.edge);
  fg.rounds++;
  T.shotAt = st.t;
  c.hp -= ROUND_DMG;
  turn(st, T, c, 'shot');
  if (c.hp <= 0) { fg.kills++; e.kills++; T.stats.rifleKills++; T.crawlers.splice(T.crawlers.indexOf(c), 1); }
}
/** An aimed round from (e.x, e.y) toward (ax, ay): the nearest crawler along the line within RIFLE_RANGE and
 *  RIFLE_HIT_RADIUS takes ROUND_DMG; a shade only on a lit tile. A miss is charged to the nearest engaged edge. */
function fire(st: SimState, e: Engineer, ax: number, ay: number): boolean {
  if (!threatActive(st)) return false;
  const T = threatOf(st.flow!);
  const dx = ax - e.x, dy = ay - e.y, L = Math.hypot(dx, dy);
  if (L < 1e-6) return true;
  const ux = dx / L, uy = dy / L;
  let best: Crawler | null = null, bestAlong = Infinity;
  for (const c of T.crawlers) {
    const px = c.x - e.x, py = c.y - e.y, along = px * ux + py * uy;
    if (along < 0 || along > RIFLE_RANGE || along >= bestAlong) continue;
    if (Math.abs(px * uy - py * ux) > RIFLE_HIT_RADIUS) continue;
    if (c.kind === 'shade' && !litAt(st, Math.floor(c.x), Math.floor(c.y))) continue;
    bestAlong = along; best = c;
  }
  if (best) hit(st, T, e, best);
  else { const c = nearest(st, T.crawlers, e.x, e.y, RIFLE_RANGE); if (c) { fightFor(T, st, c.edge).rounds++; T.shotAt = st.t; } }
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
export function describeCrawler(st: SimState, c: Crawler): string {
  const b = st.blocks[c.to];
  const goal = c.onPlayer ? 'turned on you' : c.cls === 0 ? 'toward the nearest lit lamp' : c.cls === 1 ? 'toward a turret' : 'toward the substation';
  return `${c.kind === 'shade' ? 'Shade' : 'Crawler'} · ${Math.max(0, c.hp)}/${HP[c.kind]} HP · ${goal} on (${b.x},${b.y})`;
}
/** A light eaten by a crawler at this tile (`repairLight` in flow.ts, E on it, repairs it). */
export function lightEaten(st: SimState, tx: number, ty: number): boolean {
  const T = st.flow?.threat; if (!T) return false;
  return T.broken.includes(ty * ground(st).tw + tx);
}
