/** D5: the engineer. One body on the tile grid with pockets, a reach, a rifle and HP. The block sim moves it block
 *  to block along the streets ("teleport at walk speed"); the world view moves it tile by tile. Everything the
 *  engineer does costs walking, and walking is the tedium row of the slice.
 *
 *  GAME-ASSUMPTION constants (D5, every one a human's to move):
 *   REACH 8 tiles · INV_STACKS 40 (truck 200) · WALK 6 tiles/s (truck ×3) · HP 100, regen 5 HP/s after 5 s out of
 *   contact, RESPAWN 10 s · rifle 3 rounds a crawler at 1.5 rounds/s (≈2 s a crawler; the Arsenal's second barrel
 *   1.4 s) · a shot crawler lands 5 HP/s until it dies (10 HP a kill, 7 with the second barrel) · a claim's kit is
 *   KIT_STACKS = 10 stacks. */
import { SimState, Engineer, Command, HELD, SimEvent } from './types';
import { blockCentre, walkLen, edgeFrom, LATTICE_PITCH } from './graph';
import { ROUNDS_PER_CRAWLER } from './enemies';
import { ROUNDS_PER_MAG } from './recipes';

export const REACH = 8;
export const INV_STACKS = 40, TRUCK_STACKS = 200;
export const WALK_TILES_PER_S = 6, TRUCK_MULT = 3;
export const ENGINEER_HP = 100, REGEN_HP_PER_S = 5, REGEN_AFTER_S = 5, RESPAWN_S = 10;
export const RIFLE_ROUNDS_PER_S = 1.5, RIFLE2_ROUNDS_PER_S = 3 / 1.4;
export const RETALIATE_HP_PER_S = 5;
export const KIT_STACKS = 10;
/** Stack sizes for the pocket count (GAME-ASSUMPTION: rubble 50 a stack, magazines 20, machines one each). */
export const STACK: Record<string, number> = { stone: 50, copper: 50, steel: 50, coal: 50, iron: 50, magazine: 20 };
export const stackSize = (item: string) => STACK[item] ?? 1;

export function createEngineer(st: SimState, startIdx: number): Engineer {
  const [x, y] = blockCentre(st, startIdx);
  return {
    x, y, block: startIdx, hp: ENGINEER_HP, lastHit: -999, down: -1, inv: {}, reach: REACH,
    truck: false, truckFound: false, dest: -1, remaining: 0, vel: [0, 0], target: null,
    firing: -1, barrels: 1, cooldown: 0, walked: 0, walkedHour: [], fired: 0, firstShot: -1, hurt: 0, downs: 0, kills: 0,
  };
}

export const invStacks = (inv: Record<string, number>): number => {
  let s = 0;
  for (const k in inv) { const n = inv[k]; if (n <= 0) continue; s += k === 'kit' ? n * KIT_STACKS : Math.ceil(n / stackSize(k)); }
  return s;
};
export const invCap = (e: Engineer) => (e.truck ? TRUCK_STACKS : INV_STACKS);
export const speedOf = (e: Engineer) => WALK_TILES_PER_S * (e.truck ? TRUCK_MULT : 1);
export const rifleRate = (e: Engineer) => (e.barrels === 2 ? RIFLE2_ROUNDS_PER_S : RIFLE_ROUNDS_PER_S);
/** HP a shot crawler lands before it dies: 5 HP/s for the kill time. */
export const hpPerKill = (e: Engineer) => RETALIATE_HP_PER_S * ROUNDS_PER_CRAWLER / rifleRate(e);

/** Take `n` of `item` into the pockets, as many as fit. Returns the number taken. */
export function take(e: Engineer, item: string, n: number): number {
  const room = invCap(e) - invStacks(e.inv);
  if (room <= 0 || n <= 0) return 0;
  const per = item === 'kit' ? KIT_STACKS : 1 / stackSize(item);
  const have = e.inv[item] ?? 0;
  // stacks already partly filled take no new room: fit = what the free stacks hold plus the open stack's slack
  const slack = item === 'kit' ? 0 : (Math.ceil(have / stackSize(item)) * stackSize(item) - have);
  const fit = Math.min(n, slack + Math.floor(room / per));
  if (fit <= 0) return 0;
  e.inv[item] = have + fit;
  return fit;
}
export function drop(e: Engineer, item: string, n: number): number {
  const have = e.inv[item] ?? 0, d = Math.min(have, n);
  e.inv[item] = have - d;
  if (e.inv[item] <= 0) delete e.inv[item];
  return d;
}

/** Lay kits on the block's unkitted edges from the pockets. Called on arrival and each tick while standing there. */
export function kitBlock(st: SimState, i: number): number {
  const e = st.engineer;
  if (i < 0 || st.blocks[i].state !== HELD) return 0;
  let n = 0;
  for (const ed of st.ring) {
    if (ed.a !== i || ed.kit !== false) continue;
    if ((e.inv.kit ?? 0) < 1) break;
    drop(e, 'kit', 1); ed.kit = true; n++;
  }
  if (n) st.events.push({ type: 'kitted', t: st.t, x: st.blocks[i].x, y: st.blocks[i].y, edges: n });
  return n;
}

/** Restock at the Depot (must be standing on the HQ block): fill the pockets with kits and, if the line can spare
 *  them, `mags` magazines drawn from the buffer. GAME-ASSUMPTION: kits are free to draw (the claim paid for them). */
export function restock(st: SimState, mags = 0): void {
  const e = st.engineer;
  if (e.block !== hqIdx(st)) return;
  const kits = Math.floor((invCap(e) - invStacks(e.inv)) / KIT_STACKS);
  if (kits > 0) take(e, 'kit', kits);
  if (mags > 0) {
    const can = Math.min(mags, Math.floor(st.buffer / ROUNDS_PER_MAG));
    const got = take(e, 'magazine', can);
    st.buffer -= got * ROUNDS_PER_MAG;
  }
}
export const hqIdx = (st: SimState) => (st.lattice ? st.start[0] * st.h + st.start[1] : st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1]));

export function walkTo(st: SimState, i: number): void {
  const e = st.engineer;
  if (e.down >= 0 || i < 0 || i >= st.blocks.length) return;
  if (i === e.block && e.dest < 0) return;
  const from = e.dest >= 0 ? e.dest : e.block;   // GAME-ASSUMPTION: a redirected walk restarts from its old destination's distance
  e.remaining = walkLen(st, from, i) + (e.dest >= 0 ? e.remaining : 0);
  e.dest = i; e.firing = -1;
}

/** Retaliation: `kills` crawlers shot this tick each land their HP before dying. */
export function hurt(st: SimState, hp: number): void {
  const e = st.engineer;
  if (e.down >= 0 || hp <= 0) return;
  e.hp -= hp; e.hurt += hp; e.lastHit = st.t;
  if (e.hp <= 0) {
    e.hp = 0; e.down = st.t + RESPAWN_S; e.downs++; e.firing = -1; e.dest = -1; e.remaining = 0;
    st.events.push({ type: 'engineer-down', t: st.t, x: e.x, y: e.y });
  }
}

/** Where the engineer stands: the lattice cell under a tile, or the resolver the tile layer installs for a city. */
export const blockAtHook: { current: ((st: SimState, x: number, y: number) => number) | null } = { current: null };
export function blockAt(st: SimState, x: number, y: number): number {
  if (st.lattice) {
    const bx = Math.floor(x / LATTICE_PITCH), by = Math.floor(y / LATTICE_PITCH);
    return bx >= 0 && by >= 0 && bx < st.w && by < st.h ? bx * st.h + by : -1;
  }
  return blockAtHook.current ? blockAtHook.current(st, x, y) : -1;
}

/** One engineer tick of `dt` seconds. The block sim calls it once a second; the tile layer 20 times. */
export function tickEngineer(st: SimState, dt: number): void {
  const e = st.engineer;
  if (e.down >= 0) {
    if (st.t >= e.down) {
      const hq = hqIdx(st);
      const [x, y] = blockCentre(st, hq);
      e.x = x; e.y = y; e.block = hq; e.hp = ENGINEER_HP; e.down = -1; e.lastHit = st.t;
      st.events.push({ type: 'engineer-up', t: st.t });
    }
    return;
  }
  const v = speedOf(e) * dt;
  if (e.dest >= 0) {
    e.remaining -= v; e.walked += dt;
    const h = Math.floor(st.t / 3600); e.walkedHour[h] = (e.walkedHour[h] ?? 0) + dt;
    if (e.remaining <= 0) {
      e.remaining = 0; e.block = e.dest; e.dest = -1;
      const [x, y] = blockCentre(st, e.block); e.x = x; e.y = y;
    } else e.block = -1;
  } else if (e.target || e.vel[0] || e.vel[1]) {
    let dx = e.vel[0], dy = e.vel[1];
    if (e.target) { dx = e.target[0] - e.x; dy = e.target[1] - e.y; }
    const L = Math.hypot(dx, dy);
    if (L <= v) { if (e.target) { e.x = e.target[0]; e.y = e.target[1]; e.target = null; } else { e.x += dx; e.y += dy; } }
    else { e.x += dx / L * v; e.y += dy / L * v; }
    e.walked += dt;
    const h = Math.floor(st.t / 3600); e.walkedHour[h] = (e.walkedHour[h] ?? 0) + dt;
    e.block = blockAt(st, e.x, e.y);
  }
  if (e.block >= 0) kitBlock(st, e.block);
  if (e.hp < ENGINEER_HP && st.t - e.lastHit >= REGEN_AFTER_S) e.hp = Math.min(ENGINEER_HP, e.hp + REGEN_HP_PER_S * dt);
  if (e.cooldown > 0) e.cooldown = Math.max(0, e.cooldown - dt);
}

/** The rifle's share of one engagement tick: shoots up to `un` unfed crawlers from the pockets' magazines.
 *  Returns the crawlers killed (fractional, like the turret model). */
export function rifle(st: SimState, edgeId: number, un: number, dt: number): number {
  const e = st.engineer;
  if (e.down >= 0 || e.firing !== edgeId || un <= 1e-9) return 0;
  if (!st.flow && e.block !== edgeFrom(st, edgeId)) return 0;   // the harness engineer must stand on the edge's block
  const rounds = (e.inv.magazine ?? 0) * ROUNDS_PER_MAG;
  if (rounds <= 1e-9) return 0;
  const budget = Math.min(rounds, rifleRate(e) * dt);
  const kills = Math.min(un, budget / ROUNDS_PER_CRAWLER);
  if (kills <= 1e-9) return 0;
  const spent = kills * ROUNDS_PER_CRAWLER;
  e.inv.magazine = Math.max(0, (e.inv.magazine ?? 0) - spent / ROUNDS_PER_MAG);
  e.fired += spent; e.kills += kills;
  if (e.firstShot < 0) { e.firstShot = st.t; st.events.push({ type: 'rifle', t: st.t, x: e.x, y: e.y }); }
  hurt(st, kills * hpPerKill(e));
  return kills;
}

/** Hook for the tile-level hand actions (mine, craft, place, pick up); flow.ts installs it. */
export const handHook: { current: ((st: SimState, c: Command) => void) | null } = { current: null };

export function engineerCommand(st: SimState, c: Command): void {
  const e = st.engineer;
  switch (c.type) {
    case 'move': if (e.down < 0) { e.target = [c.x, c.y]; e.vel = [0, 0]; e.dest = -1; } break;
    case 'walk': if (e.down < 0) { e.vel = [c.dx, c.dy]; e.target = null; e.dest = -1; } break;
    case 'walkTo': walkTo(st, c.block); break;
    case 'fire': e.firing = c.edge; break;
    case 'enterTruck': if (e.truckFound && e.down < 0) e.truck = !e.truck; break;
    case 'mineAt': case 'craft': case 'place': case 'pickUp': case 'chestTake': case 'chestPut': handHook.current?.(st, c); break;
  }
}

export type EngineerEvent = Extract<SimEvent, { type: 'engineer-down' | 'engineer-up' | 'kitted' | 'truck' | 'rifle' }>;
