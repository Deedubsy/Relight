import { driveTruck, truckOccupies } from './truck';
/** Prompt B M1 — the engineer on the tile grid. The block sim keeps its block-level engineer (engineer.ts: a walk is
 *  a distance along the streets, the harness bot's model); with the flow layer present the engineer is a sprite on
 *  the ground instead, ticked 20× a second by `stepFlow`: WASD velocity with collision (D-B1-5: plus sprint, the
 *  dodge and the rifle aimed at the cursor), the map view's walk-here and the bots' block walks as A* paths over the
 *  walkable grid (any tile that is not water and holds no machine but a belt). Walking anywhere is harmless (D5).
 *  Paths live outside SimState (a snapshot restarts them). */
import { SimState, Engineer } from './types';
import { machineDimensions } from './footprint';
import type { FlowState } from './flow';
import { Ground, ground, walkable, inGround, hqLot, cityGeomOf } from './ground';
import { segBetween } from './city';
import { threatHooks } from './engineer';
import {
  speedOf, kitBlock, hqIdx, markShot, rifleRate, ENGINEER_HP, REGEN_AFTER_S, REGEN_HP_PER_S,
  SPRINT_MULT, SPRINT_S, STAMINA_REFILL_S, DODGE_TILES, DODGE_S, DODGE_COST, RIFLE_RANGE, RIFLE_HIT_RADIUS,
} from './engineer';
import { DEPOT_LOT, DEPOT_TILES } from './tiles';
import { ROUNDS_PER_MAG } from './recipes';

/** HQ lot tile the engineer starts on: just south of the Depot's middle, the workbench side (§18). */
export const WORKBENCH_LOT: readonly [number, number] = [DEPOT_LOT + 3, DEPOT_LOT + DEPOT_TILES];
export function workbenchTile(st: SimState): [number, number] { return hqLot(st, WORKBENCH_LOT[0], WORKBENCH_LOT[1]); }

/** GAME-ASSUMPTION: the engineer walks through belts and poles (thin), never through other machines or water.
 *  RI-05: and over track (rails in the street) and a tram (it rides the track, is not in `occ`, and moves without
 *  bumping the placement revision this map is rebuilt on). */
const PASSABLE = new Set(['belt', 'pole', 'track', 'tram']);

const solids = new WeakMap<FlowState, { rev: number; solid: Uint8Array }>();
function solidMap(st: SimState, G: Ground): Uint8Array | null {
  const f = st.flow;
  if (!f) return null;
  const c = solids.get(f);
  if (c && c.rev === f.rev) return c.solid;
  const solid = new Uint8Array(G.tw * G.th);
  for (const m of f.machines) {
    if (PASSABLE.has(m.kind) || (st.campaign && (m.kind==='wall'||m.kind==='turret') && m.hp===0)) continue;
    const [width,height]=machineDimensions(m);
    for (let y = m.y; y < m.y + height; y++) for (let x = m.x; x < m.x + width; x++) if (inGround(G, x, y)) solid[y * G.tw + x] = 1;
  }
  solids.set(f, { rev: f.rev, solid });
  return solid;
}

/** Can the engineer stand on a tile? */
export function passable(st: SimState, tx: number, ty: number): boolean {
  const G = ground(st);
  if (!walkable(G, tx, ty)) return false;
  const s = solidMap(st, G);
  return !truckOccupies(st,tx,ty) && (!s || s[ty * G.tw + tx] === 0);
}

// ------------------------------------------------------------------ A*

interface Scratch { g: Float64Array; parent: Int32Array; stamp: Int32Array; heap: Int32Array; hf: Float64Array; gen: number }
const scratches = new WeakMap<Ground, Scratch>();
function scratchOf(G: Ground): Scratch {
  let s = scratches.get(G);
  if (!s) {
    const n = G.tw * G.th;
    s = { g: new Float64Array(n), parent: new Int32Array(n), stamp: new Int32Array(n), heap: new Int32Array(n * 2), hf: new Float64Array(n * 2), gen: 0 };
    scratches.set(G, s);
  }
  return s;
}
const SQRT2 = Math.SQRT2;
const NX = [0, 1, 0, -1, 1, 1, -1, -1], NY = [-1, 0, 1, 0, -1, 1, 1, -1];

/** Shortest walk between two tiles (8-connected, no corner cutting): the tiles to step through, ending on the goal;
 *  null when there is none. `limit` bounds the search for a stuck engineer. */
export function findPath(st: SimState, sx: number, sy: number, gx: number, gy: number, limit = 250000): Int32Array | null {
  const G = ground(st);
  if (!inGround(G, sx, sy) || !inGround(G, gx, gy)) return null;
  const solid = solidMap(st, G), tw = G.tw, th = G.th, base = G.base;
  const open = (x: number, y: number) => x >= 0 && y >= 0 && x < tw && y < th && base[y * tw + x] !== 4 /* T_RIVER */ && !G.urban?.solid[y * tw + x] && (!solid || solid[y * tw + x] === 0) && !truckOccupies(st,x,y);
  if (!open(gx, gy)) return null;
  const S = scratchOf(G), gen = ++S.gen;
  const start = sy * tw + sx, goal = gy * tw + gx;
  if (start === goal) return new Int32Array(0);
  const h = (x: number, y: number) => { const dx = Math.abs(x - gx), dy = Math.abs(y - gy); return Math.max(dx, dy) + (SQRT2 - 1) * Math.min(dx, dy); };
  let n = 0;
  const push = (t: number, f: number) => {
    let i = n++;
    S.heap[i] = t; S.hf[i] = f;
    while (i > 0) { const p = (i - 1) >> 1; if (S.hf[p] <= f) break; S.heap[i] = S.heap[p]; S.hf[i] = S.hf[p]; i = p; }
    S.heap[i] = t; S.hf[i] = f;
  };
  const pop = (): number => {
    const top = S.heap[0]; n--;
    if (n > 0) {
      const t = S.heap[n], f = S.hf[n];
      let i = 0;
      for (;;) {
        let c = 2 * i + 1;
        if (c >= n) break;
        if (c + 1 < n && S.hf[c + 1] < S.hf[c]) c++;
        if (S.hf[c] >= f) break;
        S.heap[i] = S.heap[c]; S.hf[i] = S.hf[c]; i = c;
      }
      S.heap[i] = t; S.hf[i] = f;
    }
    return top;
  };
  S.stamp[start] = gen; S.g[start] = 0; S.parent[start] = -1;
  push(start, h(sx, sy));
  let expanded = 0;
  while (n > 0) {
    const t = pop();
    if (t === goal) {
      let len = 0;
      for (let c = t; c !== start; c = S.parent[c]) len++;
      const path = new Int32Array(len);
      for (let c = t, i = len - 1; c !== start; c = S.parent[c]) path[i--] = c;
      return path;
    }
    if (S.stamp[t] === -gen) continue;   // closed
    S.stamp[t] = -gen;
    if (++expanded > limit) return null;
    const x = t % tw, y = (t - x) / tw, gt = S.g[t];
    for (let k = 0; k < 8; k++) {
      const nx = x + NX[k], ny = y + NY[k];
      if (!open(nx, ny)) continue;
      if (k >= 4 && (!open(x + NX[k], y) || !open(x, y + NY[k]))) continue;
      const nt = ny * tw + nx, ng = gt + (k >= 4 ? SQRT2 : 1);
      const seen = S.stamp[nt] === gen || S.stamp[nt] === -gen;
      if (seen && S.g[nt] <= ng) continue;
      if (S.stamp[nt] === -gen) continue;
      S.stamp[nt] = gen; S.g[nt] = ng; S.parent[nt] = t;
      push(nt, ng + h(nx, ny));
    }
  }
  return null;
}

// ------------------------------------------------------------------ the tick

interface Plan { goal: number; path: Int32Array; at: number; rev: number }
const plans = new WeakMap<Engineer, Plan>();

/** The path the engineer is on (for drawing), if any. */
export function currentPath(st: SimState): { path: Int32Array; at: number } | null {
  const p = plans.get(st.engineer);
  return p ? { path: p.path, at: p.at } : null;
}

/** The nearest passable tile to a goal (the goal itself if it is), within `r` tiles; null if none. */
function nearestOpen(st: SimState, gx: number, gy: number, r = 4): [number, number] | null {
  if (passable(st, gx, gy)) return [gx, gy];
  let best: [number, number] | null = null, bd = Infinity;
  for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) {
    const d = dx * dx + dy * dy;
    if (d >= bd || !passable(st, gx + dx, gy + dy)) continue;
    bd = d; best = [gx + dx, gy + dy];
  }
  return best;
}

/** One tile tick of `dt` seconds. */
export function tickEngineerTiles(st: SimState, dt: number): void {
  const e = st.engineer, G = ground(st), f = st.flow;
  if (e.down >= 0) {
    if (st.t >= e.down) {
      const [wx, wy] = workbenchTile(st);
      e.x = wx + 0.5; e.y = wy + 0.5; e.block = hqIdx(st); e.hp = ENGINEER_HP; e.down = -1; e.lastHit = st.t;
      plans.delete(e);
      st.events.push({ type: 'engineer-up', t: st.t });
    }
    return;
  }
  if(e.truckSeat){plans.delete(e);driveTruck(st,dt);}
  else {
  let v = speedOf(e) * dt, moved = false;
  const rev = f ? f.rev : 0;
  // D-B1-5: the dodge, then sprint and the stamina bar. Keys held (not a walk-here, not a bot walk) are what sprints.
  if (e.dashCooldown > 0) e.dashCooldown = Math.max(0, e.dashCooldown - dt);
  const keys = e.dest < 0 && !e.target && (e.vel[0] !== 0 || e.vel[1] !== 0);
  if (keys) { const L = Math.hypot(e.vel[0], e.vel[1]); e.face = [e.vel[0] / L, e.vel[1] / L]; }
  if (e.dash > 0) {
    const s = Math.min(dt, e.dash), d = DODGE_TILES / DODGE_S * s;
    e.dash -= s; e.iframes += s;
    const nx = e.x + e.dashDir[0] * d, ny = e.y + e.dashDir[1] * d;
    if (passable(st, Math.floor(nx), Math.floor(e.y))) e.x = nx;
    if (passable(st, Math.floor(e.x), Math.floor(ny))) e.y = ny;
    moved = true; v = 0;   // the dash is the whole of this tick's movement
  } else if (e.sprint && keys && !e.truck) {
    // sprint never takes the bar below one dodge; at the floor Shift just walks (release it to refill)
    if (e.stamina > DODGE_COST + 1e-9) { e.stamina = Math.max(DODGE_COST, e.stamina - dt / SPRINT_S); v *= SPRINT_MULT; }
  } else e.stamina = Math.min(1, e.stamina + dt / STAMINA_REFILL_S);
  // a goal: the bots' block walk (dest) or the map view's walk-here (target)
  let gx = -1, gy = -1;
  if (e.dest >= 0) { const p = G.blocks[e.dest].pole; gx = p[0]; gy = p[1]; }
  else if (e.target) { gx = Math.floor(e.target[0]); gy = Math.floor(e.target[1]); }
  if (gx >= 0) {
    const near = nearestOpen(st, gx, gy);
    const goal = near ? near[1] * G.tw + near[0] : -1;
    let plan = plans.get(e);
    if (!plan || plan.goal !== goal || plan.rev !== rev) {
      const path = goal >= 0 ? findPath(st, Math.floor(e.x), Math.floor(e.y), near![0], near![1]) : null;
      if (!path) {
        // no way there: a block walk falls back to the block-level model (distance, then arrival), a click is dropped
        plans.delete(e);
        if (e.dest >= 0) {
          e.remaining -= v; moved = true;
          if (e.remaining <= 0) { e.remaining = 0; e.block = e.dest; e.dest = -1; const p = G.blocks[e.block].pole; e.x = p[0] + 0.5; e.y = p[1] + 0.5; }
        } else e.target = null;
        v = 0;
      } else { plan = { goal, path, at: 0, rev }; plans.set(e, plan); }
    }
    if (plan && v > 0) {
      while (v > 0 && plan.at < plan.path.length) {
        const t = plan.path[plan.at], tx = t % G.tw, ty = (t - tx) / G.tw, cx = tx + 0.5, cy = ty + 0.5;
        const dx = cx - e.x, dy = cy - e.y, L = Math.hypot(dx, dy);
        if (L <= v) { e.x = cx; e.y = cy; v -= L; plan.at++; }
        else { e.x += dx / L * v; e.y += dy / L * v; v = 0; }
        moved = true;
      }
      if (e.dest >= 0) e.remaining = Math.max(0, plan.path.length - plan.at);
      if (plan.at >= plan.path.length) {
        plans.delete(e);
        if (e.dest >= 0) { e.block = e.dest; e.dest = -1; e.remaining = 0; }
        else e.target = null;
      } else if (e.dest >= 0) e.block = -1;   // between blocks: kits stay in the pockets until arrival (engineer.ts)
    }
  } else if (e.vel[0] || e.vel[1]) {
    plans.delete(e);
    const L = Math.hypot(e.vel[0], e.vel[1]), ux = e.vel[0] / L * v, uy = e.vel[1] / L * v;
    const nx = e.x + ux, ny = e.y + uy;
    if (passable(st, Math.floor(nx), Math.floor(e.y))) { e.x = nx; moved = true; }
    if (passable(st, Math.floor(e.x), Math.floor(ny))) { e.y = ny; moved = true; }
  }
  if (moved) {
    e.walked += dt;
    const h = Math.floor(st.t / 3600); e.walkedHour[h] = (e.walkedHour[h] ?? 0) + dt;
  }
  const tx = Math.floor(e.x), ty = Math.floor(e.y);
  if (e.dest < 0 && inGround(G, tx, ty)) {
    const o = G.owner[ty * G.tw + tx];
    if (o >= 0) { e.block = o; kitBlock(st, o); }
  }
  }
  if (e.hp < ENGINEER_HP && st.t - e.lastHit >= REGEN_AFTER_S) e.hp = Math.min(ENGINEER_HP, e.hp + REGEN_HP_PER_S * dt);
  // D-B1-5: the rifle in hand — a round toward the cursor every 1/rate s while the mouse is held (not mid-dodge)
  if (e.aim && e.dash <= 0 && e.cooldown <= 0 && fireRound(st, e)) e.cooldown = 1 / rifleRate(e);
  if (e.cooldown > 0) e.cooldown = Math.max(0, e.cooldown - dt);
}

/** D-B1-5: one round from the rifle in hand, toward `aim`. Hitscan along the line from the engineer to the cursor,
 *  out to RIFLE_RANGE: the nearest engaged ring edge whose street comes within RIFLE_HIT_RADIUS of the line takes the
 *  round, and the block step turns those rounds into kills (`rifleHits`). No auto-target, no lock-on: a shot at empty
 *  street hits nothing and still costs a round. Returns false when the pockets hold no round. */
/** Take one round out of the pockets' magazines (false with none left); counts the shot and the first-shot minute. */
export function spendRound(st: SimState, e: Engineer): boolean {
  const mags = e.inv.magazine ?? 0;
  if (mags * ROUNDS_PER_MAG < 1 - 1e-9) return false;
  e.inv.magazine = Math.max(0, mags - 1 / ROUNDS_PER_MAG);
  e.fired += 1;
  if (e.firstShot < 0) { e.firstShot = st.t; st.events.push({ type: 'rifle', t: st.t, x: e.x, y: e.y }); }
  markShot(st);
  return true;
}
function fireRound(st: SimState, e: Engineer): boolean {
  if (!spendRound(st, e)) return false;
  // M4: on a city the round meets the tile crawlers (threat.ts); the lattice keeps the block-level ridge hitscan
  if (threatHooks.current?.fire(st, e, e.aim![0], e.aim![1])) return true;
  const dx = e.aim![0] - e.x, dy = e.aim![1] - e.y, L = Math.hypot(dx, dy);
  if (L < 1e-6) return true;
  const ux = dx / L, uy = dy / L, cg = cityGeomOf(st), tw = cg.tw;
  let bestId = -1, bestAlong = Infinity;
  for (const en of st.engagements) {
    if (en.cr <= 1e-9 || en.id === bestId) continue;
    const ri = st.edgeAt[en.id]; if (ri < 0) continue;
    const ed = st.ring[ri], sg = segBetween(cg, ed.a, ed.b); if (!sg) continue;
    for (const t of sg.ridge) {
      const px = t % tw + 0.5 - e.x, py = Math.floor(t / tw) + 0.5 - e.y, along = px * ux + py * uy;
      if (along < 0 || along > RIFLE_RANGE || along >= bestAlong) continue;
      if (Math.abs(px * uy - py * ux) <= RIFLE_HIT_RADIUS) { bestAlong = along; bestId = en.id; }
    }
  }
  if (bestId >= 0) e.shots[bestId] = (e.shots[bestId] ?? 0) + 1;
  return true;
}
