/** RI-04 (plan §7, §7.1; D-RI-6; GDD §28.6): the Stalker prototype — the third archetype, introduced alone.
 *
 *  A Stalker guards a bounded territory that belongs to one occupied site. Implementation default (RI-04): the
 *  occupied sites are the map's wells while Dark (§5's established rot sources; `SimState.wells`), its home the site's
 *  substation — so there are no wandering Stalkers across every street, and none at all until `enableStalkers`
 *  puts the candidate on a state. State machine `guard → investigate → pursue → attack → return` (D-RI-6):
 *   guard        at home. Deterministic activity inside `perception` attracts it: the engineer moved, a rifle round
 *                left (threat.ts stamps `shotAt` per round), a machine was placed (a new machine id inside
 *                perception) — never the engineer merely standing there, never audio;
 *   investigate  walks to the activity's tile; a fresh cue with the engineer inside perception turns it to pursue;
 *                reaching the tile, or `investigateS` without one, turns it home;
 *   pursue       follows the engineer while they stay inside perception (+ `hysteresis`); `lostS` out of it, the
 *                engineer down, the leash (`leash` tiles from home) or no path turns it home;
 *   attack       at arm's reach (threat.ts CONTACT_R): the first attack of a pursuit winds up `windupS` first, then
 *                one every `attackS` for `attackHp` — a whole attack, never × dt. A swing during a valid dodge
 *                (`engineer.dash > 0`) misses; an engineer who gets out of reach is pursued again without a second
 *                wind-up; a downed engineer is left alone;
 *   return       walks home deaf to cues; at home it guards again and the next pursuit winds up afresh.
 *  Death: turrets and the rifle hit it like a crawler (threat.ts `damage`); at 0 HP it is gone — no drop, no reward
 *  (§6). Its site restored (the block no longer Dark) sends every survivor home, where it retires, and the site
 *  fields no more; a dead Stalker's site fields another `respawnS` later, only while still Dark and only while the
 *  engineer is beyond perception of the home (no respawn behind the engineer). Paths: walk.ts `findPath` over
 *  passable tiles, replanned as the goal tile moves — no pursuit through impassable terrain. Every number is
 *  `CANDIDATES.stalker` (candidates.ts); the layer's stats and events live beside M4's, never inside them. */
import { SimState, DARK } from './types';
import { Ground, ground, inGround } from './ground';
import { passable, findPath } from './walk';
import { hurt } from './engineer';
import { ENEMIES } from './enemies';
import { CANDIDATES, StalkerCandidate } from './candidates';
import { moveTo, NX, NY } from './move';
import type { ThreatState } from './threat';

export type StalkerMode = 'guard' | 'investigate' | 'pursue' | 'attack' | 'return';
export type StalkerWhat = StalkerMode | 'spawn' | 'hit' | 'dodged' | 'dead' | 'retired';
export interface Stalker {
  id: number; kind: 'stalker';
  /** The occupied site it belongs to (a block index) and its home tile's centre there. */
  site: number; hx: number; hy: number;
  x: number; y: number; hp: number;
  mode: StalkerMode; modeT: number;
  /** The point of interest: an investigation's tile, or where the engineer was last seen. */
  px: number; py: number;
  windup: number; atk: number; warmed: boolean;
  lost: number; stuck: number; born: number;
  dir: [number, number];
  /** The current walk: walk.ts path tiles still to step through, the goal tile it was planned for, when. */
  path: number[]; goal: number; planT: number;
}
export interface SiteMemory { restored: boolean; diedAt: number; spawned: number }
export interface StalkerStats { spawned: number; pursuits: number; attacks: number; hits: number; dodged: number; kills: number; retired: number; contactS: number }
export interface StalkerLayer {
  cand: StalkerCandidate; next: number;
  stalkers: Stalker[];
  sites: Record<number, SiteMemory>;
  /** The layer's last look at the engineer and the machine counter: a cue is a change since it. */
  ex: number; ey: number; shotAt: number; machineId: number;
  stats: StalkerStats;
}

export const stalkerHp = (c: StalkerCandidate): number => ENEMIES[0].hp * c.hpMul;
export const stalkerSpeed = (c: StalkerCandidate): number => ENEMIES[0].tilesPerSec * c.speedMul;
const STUCK_S = 30, PATH_LIMIT = 4000, REPLAN_S = 0.25, ARRIVE_R = 1;

/** The occupied sites: the map's wells (`SimState.wells`, §5 — `Block.well` marks a well's whole influence zone, not the well). */
export function wellBlocks(st: SimState): number[] {
  return st.wells.map(([x, y]) => st.blocks.findIndex(b => b.x === x && b.y === y)).filter(i => i >= 0);
}
export function newStalkerLayer(st: SimState, cand: StalkerCandidate = CANDIDATES.stalker): StalkerLayer {
  const S: StalkerLayer = { cand: { ...cand }, next: 1, stalkers: [], sites: {}, ex: st.engineer.x, ey: st.engineer.y, shotAt: -1,
    machineId: st.flow ? st.flow.next : 0, stats: { spawned: 0, pursuits: 0, attacks: 0, hits: 0, dodged: 0, kills: 0, retired: 0, contactS: 0 } };
  for (const i of wellBlocks(st)) if (st.blocks[i].state === DARK) S.sites[i] = { restored: false, diedAt: -1, spawned: 0 };
  return S;
}

/** The home of a site's Stalker: the nearest passable tile to the substation's centre, else the block's pole. */
function homeOf(st: SimState, G: Ground, site: number): [number, number] {
  const bg = G.blocks[site], sub = bg.sub;
  const cx = sub ? sub.x + (sub.size >> 1) : bg.pole[0], cy = sub ? sub.y + (sub.size >> 1) : bg.pole[1];
  for (let r = 0; r <= 6; r++) {
    for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) {
      if (Math.max(Math.abs(dx), Math.abs(dy)) !== r) continue;
      if (inGround(G, cx + dx, cy + dy) && passable(st, cx + dx, cy + dy)) return [cx + dx + 0.5, cy + dy + 0.5];
    }
  }
  return [bg.pole[0] + 0.5, bg.pole[1] + 0.5];
}

function emit(st: SimState, s: Stalker, what: StalkerWhat): void {
  st.events.push({ type: 'stalker', t: st.t, id: s.id, what, x: Math.floor(s.x), y: Math.floor(s.y), site: s.site });
}
function go(st: SimState, s: Stalker, mode: StalkerMode): void {
  s.mode = mode; s.modeT = st.t; s.path = []; s.goal = -1;
  emit(st, s, mode);
}

/** Field a Stalker at a site's home — unless the engineer is within perception of it (no spawn behind them). */
export function spawnStalker(st: SimState, S: StalkerLayer, site: number): Stalker | null {
  const G = ground(st), [hx, hy] = homeOf(st, G, site), e = st.engineer;
  if (e.down < 0 && Math.hypot(e.x - hx, e.y - hy) <= S.cand.perception) return null;
  const s: Stalker = { id: S.next++, kind: 'stalker', site, hx, hy, x: hx, y: hy, hp: stalkerHp(S.cand), mode: 'guard', modeT: st.t,
    px: hx, py: hy, windup: 0, atk: 0, warmed: false, lost: 0, stuck: 0, born: st.t, dir: [0, 0], path: [], goal: -1, planT: -1 };
  S.stalkers.push(s);
  (S.sites[site] ??= { restored: false, diedAt: -1, spawned: 0 }).spawned++;
  S.stats.spawned++;
  emit(st, s, 'spawn');
  return s;
}
/** Field one Stalker at every Dark well block that has none (the switch, and the respawn sweep). */
export function spawnStalkers(st: SimState, S: StalkerLayer, respawn = false): number {
  let n = 0;
  for (const k of Object.keys(S.sites)) {
    const site = Number(k), mem = S.sites[site];
    if (mem.restored || st.blocks[site].state !== DARK) continue;
    if (S.stalkers.some(s => s.site === site)) continue;
    // the sweep: a site never fielded (the engineer stood within perception at the switch) fills once they leave;
    // a dead one refills `respawnS` later — never a survivor's site, never a restored one
    if (respawn && mem.spawned > 0 && (mem.diedAt < 0 || st.t - mem.diedAt < S.cand.respawnS)) continue;
    if (spawnStalker(st, S, site)) n++;
  }
  return n;
}

/** A round hit it: ROUND_DMG off; at 0 HP it is gone and its site remembers when. */
export function damageStalker(st: SimState, S: StalkerLayer, s: Stalker, hp: number): boolean {
  s.hp -= hp;
  if (s.hp > 0) return false;
  s.hp = 0;
  const i = S.stalkers.indexOf(s); if (i >= 0) S.stalkers.splice(i, 1);
  S.stats.kills++;
  (S.sites[s.site] ??= { restored: false, diedAt: -1, spawned: 0 }).diedAt = st.t;
  emit(st, s, 'dead');
  return true;
}
function retire(st: SimState, S: StalkerLayer, s: Stalker): void {
  const i = S.stalkers.indexOf(s); if (i >= 0) S.stalkers.splice(i, 1);
  S.stats.retired++;
  emit(st, s, 'retired');
}

/** The nearest passable tile to a goal (its own, else its 8 neighbours in a fixed order), or -1. */
function openNear(st: SimState, G: Ground, gx: number, gy: number): number {
  const tx = Math.floor(gx), ty = Math.floor(gy);
  if (inGround(G, tx, ty) && passable(st, tx, ty)) return ty * G.tw + tx;
  for (let k = 0; k < 8; k++) { const x = tx + NX[k], y = ty + NY[k]; if (inGround(G, x, y) && passable(st, x, y)) return y * G.tw + x; }
  return -1;
}
/** One step along a planned path toward (gx, gy); false when there is no way there. */
function walk(st: SimState, G: Ground, s: Stalker, gx: number, gy: number, step: number): boolean {
  const tw = G.tw, sx = Math.floor(s.x), sy = Math.floor(s.y), here = sy * tw + sx;
  const gt = openNear(st, G, gx, gy);
  if (gt < 0) return false;
  if (gt === here) { moveTo(s, gx, gy, step); return true; }
  const next = s.path.length ? s.path[0] : -1;
  const adjacent = next >= 0 && Math.abs(next % tw - sx) <= 1 && Math.abs(Math.floor(next / tw) - sy) <= 1;
  if (s.goal !== gt && st.t - s.planT >= REPLAN_S || next < 0 || !adjacent || !passable(st, next % tw, Math.floor(next / tw))) {
    const p = findPath(st, sx, sy, gt % tw, Math.floor(gt / tw), PATH_LIMIT);
    s.goal = gt; s.planT = st.t;
    if (!p) { s.path = []; return false; }
    s.path = Array.from(p);
  }
  if (!s.path.length) { moveTo(s, gx, gy, step); return true; }
  const t = s.path[0], cx = t % tw + 0.5, cy = Math.floor(t / tw) + 0.5;
  moveTo(s, cx, cy, step);
  if (s.x === cx && s.y === cy) s.path.shift();
  return true;
}

/** The activity that attracts a guarding or investigating Stalker this tick: the engineer's move or shot inside
 *  perception, or a machine placed inside it. */
function cueFor(st: SimState, S: StalkerLayer, s: Stalker, moved: boolean, shot: boolean, built: { x: number; y: number; size: number }[]): [number, number] | null {
  const e = st.engineer, c = S.cand;
  if (e.down < 0 && (moved || shot) && Math.hypot(e.x - s.x, e.y - s.y) <= c.perception) return [e.x, e.y];
  for (const m of built) { const mx = m.x + m.size / 2, my = m.y + m.size / 2; if (Math.hypot(mx - s.x, my - s.y) <= c.perception) return [mx, my]; }
  return null;
}

export function tickStalkers(st: SimState, T: ThreatState, dt: number, contactR: number): void {
  const S = T.stalk; if (!S) return;
  const f = st.flow!, G = ground(st), e = st.engineer, c = S.cand, engUp = e.down < 0, step = stalkerSpeed(c) * dt;
  const moved = e.x !== S.ex || e.y !== S.ey, shot = T.shotAt !== S.shotAt;
  const built = f.machines.filter(m => m.id >= S.machineId);
  for (const k of Object.keys(S.sites)) { const site = Number(k); if (!S.sites[site].restored && st.blocks[site].state !== DARK) S.sites[site].restored = true; }
  const ss = S.stalkers;
  for (let i = 0; i < ss.length; i++) {
    const s = ss[i];
    if (s.hp <= 0) { ss.splice(i--, 1); continue; }
    const mem = S.sites[s.site];
    if (mem?.restored && s.mode !== 'return') go(st, s, 'return');
    const dE = Math.hypot(e.x - s.x, e.y - s.y), dH = Math.hypot(s.hx - s.x, s.hy - s.y);
    const cue = s.mode === 'guard' || s.mode === 'investigate' ? cueFor(st, S, s, moved, shot, built) : null;
    const walked = (gx: number, gy: number): boolean => {
      if (walk(st, G, s, gx, gy, step)) { s.stuck = 0; return true; }
      s.stuck += dt; return false;
    };
    switch (s.mode) {
      case 'guard':
        if (cue) { s.px = cue[0]; s.py = cue[1]; go(st, s, 'investigate'); }
        break;
      case 'investigate':
        if (cue && engUp && dE <= c.perception) { s.px = e.x; s.py = e.y; s.lost = 0; S.stats.pursuits++; go(st, s, 'pursue'); break; }
        if (cue) { s.px = cue[0]; s.py = cue[1]; }
        if (Math.hypot(s.px - s.x, s.py - s.y) <= ARRIVE_R || st.t - s.modeT >= c.investigateS) { go(st, s, 'return'); break; }
        if (!walked(s.px, s.py) && s.stuck >= 1) go(st, s, 'return');
        break;
      case 'pursue':
      case 'attack': {
        if (!engUp || dH > c.leash) { go(st, s, 'return'); break; }
        if (dE <= c.perception + c.hysteresis) { s.px = e.x; s.py = e.y; s.lost = 0; }
        else { s.lost += dt; if (s.lost >= c.lostS) { go(st, s, 'return'); break; } }
        if (dE <= contactR) {
          if (s.mode !== 'attack') { go(st, s, 'attack'); if (!s.warmed) s.windup = c.windupS; }
          if (dE > 1e-9) { s.dir[0] = (e.x - s.x) / dE; s.dir[1] = (e.y - s.y) / dE; }
          S.stats.contactS += dt;
          let swing = false;
          if (!s.warmed) { s.windup -= dt; if (s.windup <= 1e-9) { swing = true; s.warmed = true; s.atk = c.attackS; } }
          else { s.atk -= dt; if (s.atk <= 1e-9) { swing = true; s.atk = c.attackS; } }
          if (swing) {
            S.stats.attacks++;
            if (e.dash > 0) { S.stats.dodged++; emit(st, s, 'dodged'); }   // a valid dodge: the swing misses (§7.1)
            else { hurt(st, c.attackHp); S.stats.hits++; emit(st, s, 'hit'); }
          }
        } else {
          if (s.mode === 'attack') go(st, s, 'pursue');
          if (!walked(s.px, s.py) && s.stuck >= 1) go(st, s, 'return');
        }
        break;
      }
      case 'return':
        if (dH <= 0.05) {
          if (mem?.restored) { retire(st, S, s); i--; break; }
          s.warmed = false; s.lost = 0; s.windup = 0; s.atk = 0;
          go(st, s, 'guard');
          break;
        }
        if (!walked(s.hx, s.hy) && s.stuck >= STUCK_S) { retire(st, S, s); i--; }
        break;
    }
  }
  S.ex = e.x; S.ey = e.y; S.shotAt = T.shotAt; S.machineId = f.next;
  spawnStalkers(st, S, true);
}

// ------------------------------------------------------------------ queries for the views

export function stalkersOf(st: SimState): Stalker[] { return st.flow?.threat?.stalk?.stalkers ?? []; }
export function stalkerCandidate(st: SimState): StalkerCandidate | null { return st.flow?.threat?.stalk?.cand ?? null; }
export function stalkerAt(st: SimState, x: number, y: number, r = 0.9): Stalker | null {
  let best: Stalker | null = null, bd = r;
  for (const s of stalkersOf(st)) { const d = Math.hypot(s.x - x, s.y - y); if (d < bd) { bd = d; best = s; } }
  return best;
}
export function describeStalker(st: SimState, s: Stalker): string {
  const S = st.flow!.threat!.stalk!, b = st.blocks[s.site];
  const what = s.mode === 'attack' ? (s.warmed ? 'attacking' : `winding up (${s.windup.toFixed(1)} s)`) : s.mode === 'pursue' ? 'pursuing you' : s.mode === 'investigate' ? 'investigating' : s.mode === 'return' ? 'returning home' : 'guarding';
  return `Stalker · ${what} · ${Math.max(0, Math.round(s.hp))}/${stalkerHp(S.cand)} HP · site (${b.x},${b.y}) · leash ${S.cand.leash} tiles from home`;
}
