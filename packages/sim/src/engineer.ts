import {isWeaponItem} from './weaponTypes';
/** D5: the engineer. One body on the tile grid with pockets, a reach, a rifle and HP. The block sim moves it block
 *  to block along the streets ("teleport at walk speed"); the world view moves it tile by tile. Everything the
 *  engineer does costs walking, and walking is the tedium row of the slice.
 *
 *  GAME-ASSUMPTION constants (D5, every one a human's to move):
 *   REACH 8 tiles · INV_STACKS 40 (truck 200) · WALK 6 tiles/s (truck ×3) · HP 100, regen 5 HP/s after 5 s out of
 *   contact, RESPAWN 10 s · rifle 3 rounds a crawler at 1.5 rounds/s (≈2 s a crawler; the Arsenal's second barrel
 *   1.4 s) · a shot crawler lands 5 HP/s until it dies (10 HP a kill, 7 with the second barrel) · a claim's kit is
 *   KIT_STACKS = 10 stacks. */
import { SimState, Engineer, Command, HELD, SimEvent, Edge } from './types';
import { blockCentre, walkLen, edgeFrom, LATTICE_PITCH } from './graph';
import { ROUNDS_PER_CRAWLER } from './enemies';
import { ROUNDS_PER_MAG, TURRET_RANGE } from './recipes';

export const REACH = 8;
export const INV_STACKS = 40, TRUCK_STACKS = 200;
export const WALK_TILES_PER_S = 6, TRUCK_MULT = 3;
export const ENGINEER_HP = 100, REGEN_HP_PER_S = 5, REGEN_AFTER_S = 5, RESPAWN_S = 10;
export const RIFLE_ROUNDS_PER_S = 1.5, RIFLE2_ROUNDS_PER_S = 3 / 1.4;
export const RETALIATE_HP_PER_S = 5;
export const KIT_STACKS = 10;
/** GAME-ASSUMPTION (D-B1-5): sprint at 1.6× walk, ~4 s from a full bar, ~6 s to refill from empty. Stamina is a
 *  movement resource only — never tied to hunger, cold, dark or anything survival-shaped. */
export const SPRINT_MULT = 1.6, SPRINT_S = 4, STAMINA_REFILL_S = 6;
/** GAME-ASSUMPTION (D-B1-5): a dodge is 3 tiles in 0.25 s with invulnerability to crawlers for its duration, a 1 s
 *  cooldown and a fixed quarter of the bar; sprint never drains the bar below one dodge's worth, so one dodge is
 *  always there after a sprint into trouble. */
export const DODGE_TILES = 3, DODGE_S = 0.25, DODGE_COOLDOWN_S = 1, DODGE_COST = 0.25;
/** Legacy non-equipment rifle only (D-B1-5): reaches the turret's range (9 tiles — no range advantage) and hits anything
 *  within 1.5 tiles of the line to the cursor, so it is aim, not twitch. */
export const RIFLE_RANGE = TURRET_RANGE, RIFLE_HIT_RADIUS = 1.5;
/** Stack sizes for the pocket count (GAME-ASSUMPTION: rubble 50 a stack, magazines 20, machines one each; RI-01: the
 *  §12 intermediates the placed Assembler makes stack 50 like rubble until a rule says otherwise). */
export const STACK: Record<string, number> = { overclock:1, alienartifact:20, ironore: 50, copperore: 50, crude: 50, fuel: 50, polymer: 50, shell: 20, concrete: 50, stone: 50, copper: 50, steel: 50, coal: 50, iron: 50, magazine: 200, wire: 50, frame: 50, board: 50 };
export const stackSize = (item: string) => STACK[item] ?? 1;

export function createEngineer(st: SimState, startIdx: number): Engineer {
  const [x, y] = blockCentre(st, startIdx);
  return {
    x, y, block: startIdx, hp: ENGINEER_HP, lastHit: -999, down: -1, inv: {}, reach: REACH,
    truck: false, truckFound: false, dest: -1, remaining: 0, vel: [0, 0], target: null,
    firing: -1, barrels: 1, cooldown: 0, walked: 0, walkedHour: [], fired: 0, firstShot: -1, hurt: 0, downs: 0, kills: 0,
    stamina: 1, sprint: false, dash: 0, dashDir: [1, 0], dashCooldown: 0, face: [1, 0], aim: null, shots: {}, iframes: 0,
    shootS: 0, shootHour: [], shotAt: -1, danger: 0, dangerHour: [], dangerShot: 0,
  };
}

/** A snapshot from before D-B1-5 lacks the body fields; give it the defaults. */
export function upgradeEngineer(e: Engineer): Engineer {
  e.stamina ??= 1; e.sprint ??= false; e.dash ??= 0; e.dashDir ??= [1, 0]; e.dashCooldown ??= 0; e.face ??= [1, 0];
  e.aim ??= null; e.shots ??= {}; e.iframes ??= 0; e.shootS ??= 0; e.shootHour ??= []; e.shotAt ??= -1;
  e.danger ??= 0; e.dangerHour ??= []; e.dangerShot ??= 0;
  return e;
}

/** §19's telemetry: the second `st.t` counts as shooting time (once, however many rounds). */
export function markShot(st: SimState): void {
  const e = st.engineer;
  if (e.shotAt === st.t) return;
  e.shotAt = st.t; e.shootS++;
  const h = Math.floor(st.t / 3600); e.shootHour[h] = (e.shootHour[h] ?? 0) + 1;
}

export const invStacks = (inv: Record<string, number>): number => {
  let s = 0;
  for (const k in inv) { const n = inv[k]; if (n <= 0) continue; s += k === 'kit' ? n * KIT_STACKS : Math.ceil(n / stackSize(k)); }
  return s;
};
export const invCap = (e: Engineer) => (e.truck && !e.truckSeat ? TRUCK_STACKS : INV_STACKS);
/** Optional stack allocation over inv: every view reconciles legal consumption, never invents quantities.
 * Legacy kits reserve ten cells each at the beginning; those cells cannot be split or swapped. */
export interface PackStack {item:string;count:number;reserved?:true}
export type InventoryAction = {type:'sort'} | {type:'move'|'split';from:number;to:number;item:string;count:number;n?:number;layout:string};
export function pocketSlots(e:Engineer): (PackStack|null)[] {
 const slots:(PackStack|null)[]=Array.from({length:invCap(e)},()=>null),left={...e.inv};
 const kits=Math.floor(left.kit??0);delete left.kit;
 for(let i=0;i<kits*KIT_STACKS&&i<slots.length;i++)slots[i]={item:'kit',count:i%KIT_STACKS===0?1:0,...(i%KIT_STACKS?{reserved:true as const}:{})};
 for(let i=0;i<slots.length;i++) {const old=e.pack?.[i];if(slots[i]||!old||old.item==='kit'||old.reserved)continue;
  const n=Math.min(old.count,Math.max(0,left[old.item]??0),stackSize(old.item));if(n>0){slots[i]={item:old.item,count:n};left[old.item]-=n;}}
 for(const item of Object.keys(left).sort()){
  let n=Math.max(0,left[item]);for(const cell of slots){if(!n)break;if(cell?.item===item&&!cell.reserved){const add=Math.min(n,stackSize(item)-cell.count);cell.count+=add;n-=add;}}
  for(let i=0;i<slots.length&&n>0;i++)if(!slots[i]){const add=Math.min(n,stackSize(item));slots[i]={item,count:add};n-=add;}
 }
 return slots;
}
export const pocketUsed=(e:Engineer)=>e.pack?pocketSlots(e).filter(Boolean).length:invStacks(e.inv);
export function inventoryCommand(e:Engineer,a:InventoryAction):{ok:boolean;reason:string}{
 const no=(reason:string)=>({ok:false,reason});const slots=pocketSlots(e);
 if(a.type==='sort'){e.pack=pocketSlots({...e,pack:undefined});return {ok:true,reason:'Backpack sorted; quantities unchanged.'};}
 if(a.type!=='move'&&a.type!=='split')return no('Unknown inventory action');
 if(a.layout!==JSON.stringify(slots))return no('Stacks changed. Select the stack again.');
 if(!Number.isInteger(a.from)||!Number.isInteger(a.to)||a.from<0||a.to<0||a.from>=slots.length||a.to>=slots.length||a.from===a.to)return no('Choose a different backpack slot.');
 const src=slots[a.from],dst=slots[a.to];if(!src||src.item!==a.item||src.count!==a.count)return no('Source stack changed.');
 if(src.item==='kit'||dst?.item==='kit')return no('Kits reserve ten slots; transfer them through storage.');
 if(a.type==='split'){
  const n=a.n??Math.floor(src.count/2);if(!Number.isSafeInteger(n)||n<1||n>=src.count)return no('Choose fewer than the stack contains.');
  if(dst)return no('Splitting needs an empty slot.');slots[a.to]={item:src.item,count:n};src.count-=n;
 }else if(!dst){slots[a.to]=src;slots[a.from]=null;}
 else if(dst.item===src.item){const n=Math.min(src.count,stackSize(src.item)-dst.count);if(n<=0){[slots[a.from],slots[a.to]]=[dst,src];}else{dst.count+=n;src.count-=n;if(src.count<=0)slots[a.from]=null;}}
 else {[slots[a.from],slots[a.to]]=[dst,src];}
 e.pack=slots;return {ok:true,reason:a.type==='split'?'Stack split.':'Stack moved.'};
}
/** A destination snapshot is checked before either inventory is changed. Storage pools each item, so a dropped-on
 * storage stack only names the item; the Backpack keeps the player's explicit slot allocation, and a drop fills the
 * chosen slot first, then other stacks of the same item, then empty slots. Only what truly has no room stays behind. */
export interface TransferTarget { total?:number; slot?:number; layout?:string; count?:number; item?:string; offset?:number }
/** Room for `item` across the whole Backpack: slack in its stacks plus empty slots (kits are counted by `take`). */
export function packRoom(slots:(PackStack|null)[],item:string):number {
 const cap=isWeaponItem(item)?1:stackSize(item);let room=0;
 for(const c of slots){if(!c)room+=cap;else if(c.item===item&&!c.reserved&&!isWeaponItem(item))room+=cap-c.count;}
 return room;
}
export function transferTarget(e:Engineer,item:string,n:number,target:TransferTarget|undefined,put:boolean):{n:number;reason:string}{
 if(!target)return {n,reason:''};
 if(put){
  if(target.item!==undefined&&target.item!==item)return {n:0,reason:'Choose a compatible stack or empty storage space.'};
  return {n,reason:''};   // the store's own capacity rule decides how much fits (chestPut / machineTransfer)
 }
 const slots=pocketSlots(e),i=target.slot;
 if(target.layout!==JSON.stringify(slots)||!Number.isInteger(i)||i!<0||i!>=slots.length)return {n:0,reason:'Backpack changed; try the drop again.'};
 const cell=slots[i!];
 if(cell&&(cell.reserved||cell.item!==item||isWeaponItem(item)))return {n:0,reason:'Choose a compatible stack or empty Backpack slot.'};
 if(item==='kit')return {n,reason:''};
 return {n:Math.min(n,packRoom(slots,item)),reason:'Backpack is full.'};
}
/** Place `n` taken items: the dropped-on slot first, then matching stacks with room, then empty slots. */
export function allocateTransfer(e:Engineer,item:string,n:number,target:TransferTarget|undefined,before:(PackStack|null)[]):void{
 if(!target||target.slot===undefined||n<=0||item==='kit')return;
 const cap=isWeaponItem(item)?1:stackSize(item);let left=n;
 const fill=(i:number,allowEmpty:boolean)=>{const c=before[i];if(left<=0||c&&(c.item!==item||c.reserved)||!c&&!allowEmpty)return;const add=Math.min(left,cap-(c?.count??0));if(add<=0)return;before[i]=c?{...c,count:c.count+add}:{item,count:add};left-=add;};
 fill(target.slot,true);
 for(let i=0;i<before.length;i++)if(i!==target.slot)fill(i,false);
 for(let i=0;i<before.length;i++)if(i!==target.slot)fill(i,true);
 e.pack=before;
}
export function packProblem(e:Engineer):string {
 if(e.pack===undefined)return '';
 return !Array.isArray(e.pack)||e.pack.length>TRUCK_STACKS||e.pack.some(s=>s!==null&&(!s||typeof s.item!=='string'||!Number.isFinite(s.count)||s.count<0||s.count>stackSize(s.item)||(!s.count&&!s.reserved)||s.reserved!==undefined&&(s.reserved!==true||s.item!=='kit')))?'Invalid backpack allocation':'';
}
export const speedOf = (e: Engineer) => WALK_TILES_PER_S * (e.truck ? TRUCK_MULT : 1);
export const rifleRate = (e: Engineer) => (e.barrels === 2 ? RIFLE2_ROUNDS_PER_S : RIFLE_ROUNDS_PER_S);
/** HP a shot crawler lands before it dies: 5 HP/s for the kill time. */
export const hpPerKill = (e: Engineer) => RETALIATE_HP_PER_S * ROUNDS_PER_CRAWLER / rifleRate(e);

/** Take `n` of `item` into the pockets, as many as fit. Returns the number taken. */
export function take(e: Engineer, item: string, n: number): number {
  const slots=pocketSlots(e),room=Math.max(0,invCap(e)-pocketUsed(e));
  if(!Number.isFinite(n)||n<=0||isWeaponItem(item)&&(!Number.isSafeInteger(n)||n!==1))return 0;
  const per=item==='kit'?KIT_STACKS:1/stackSize(item),have=e.inv[item]??0;
  const slack=item==='kit'?0:slots.reduce((sum,c)=>sum+(c?.item===item?stackSize(item)-c.count:0),0);
  const fit=Math.min(n,slack+Math.floor(room/per));if(fit<=0)return 0;
  e.inv[item]=have+fit;if(e.pack)e.pack=pocketSlots(e);
  return fit;
}
export function drop(e: Engineer, item: string, n: number): number {
  if(isWeaponItem(item)&&(!Number.isSafeInteger(n)||n!==1))return 0;
  const have = e.inv[item] ?? 0, d = Math.min(have, n);
  e.inv[item] = have - d;
  if (e.inv[item] <= 0) delete e.inv[item];
  if(e.pack)e.pack=pocketSlots(e);
  return d;
}

/** GAME-ASSUMPTION (C1/C2 birth artefact, 2026-09-04): a kitted edge is born fed — its hopper is filled from the ring's buffer in the tick it is created or kitted, subject to
 *  the rounds the buffer holds (the ring fill later in the same tick tops it up from production). A stand-in edge only —
 *  a turret edge reads its turrets. Telemetry never reads a pip on an edge's birth tick (`Edge.born`). */
export function bornFed(st: SimState, e: Edge): void {
  if (!st.config.production || !st.config.walk || e.kit !== true || e.turrets) return;   // D5 kits only: the Gate A lattice fixtures fill in ring order
  const give = Math.min(st.config.hopper - e.hopper, st.buffer);
  if (give > 0) { e.hopper += give; st.buffer -= give; }
}

/** Lay kits on the block's unkitted edges from the pockets. Called on arrival and each tick while standing there. */
export function kitBlock(st: SimState, i: number): number {
  const e = st.engineer;
  if (i < 0 || st.blocks[i].state !== HELD) return 0;
  let n = 0;
  for (const ed of st.ring) {
    if (ed.a !== i || ed.kit !== false) continue;
    if ((e.inv.kit ?? 0) < 1) break;
    drop(e, 'kit', 1); ed.kit = true; bornFed(st, ed); n++;   // kitted this second, fed this second
  }
  if (n) st.events.push({ type: 'kitted', t: st.t, x: st.blocks[i].x, y: st.blocks[i].y, edges: n });
  return n;
}

/** Restock at the Depot (must be standing on the HQ block): fill the pockets with kits and, if the line can spare
 *  them, `mags` magazines drawn from the buffer. GAME-ASSUMPTION: kits are free to draw (the claim paid for them). */
export function restock(st: SimState, mags = 0): void {
  const e = st.engineer;
  if (e.block !== hqIdx(st)) return;
  const kits = Math.floor((invCap(e) - pocketUsed(e)) / KIT_STACKS);
  if (kits > 0) take(e, 'kit', kits);
  if (mags > 0) {
    const can = Math.min(mags, Math.floor(st.buffer / ROUNDS_PER_MAG));
    const got = take(e, 'magazine', can);
    st.buffer -= got * ROUNDS_PER_MAG;
  }
}
export const hqIdx = (st: SimState) => (st.lattice ? st.start[0] * st.h + st.start[1] : st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1]));

/** Block-level walk to block `i` — the bots' and the dev hooks' walk only (D-B1-5): the player moves with WASD and
 *  the map view's walk-here; no click walks the engineer in the world view. */
export function walkTo(st: SimState, i: number): void {
  const e = st.engineer;
  if (e.down >= 0 || i < 0 || i >= st.blocks.length) return;
  if (i === e.block && e.dest < 0) return;
  const from = e.dest >= 0 ? e.dest : e.block;   // GAME-ASSUMPTION: a redirected walk restarts from its old destination's distance
  e.remaining = walkLen(st, from, i) + (e.dest >= 0 ? e.remaining : 0);
  e.dest = i; e.firing = -1;
}

/** Retaliation: `kills` crawlers shot this tick each land their HP before dying. */
export function hurt(st: SimState, hp: number, source?: string): void {
  const e = st.engineer;
  if (e.down >= 0 || hp <= 0) return;
  e.hp -= hp; e.hurt += hp; e.lastHit = st.t; e.lastDamageSource = source;
  if (e.hp <= 0) {
    if(st.campaign&&e.truckSeat){e.truck=false;delete e.truckSeat;st.flow!.rev++;}
    e.hp = 0; e.down = st.t + RESPAWN_S; e.downs++; e.firing = -1; e.dest = -1; e.remaining = 0;
    e.aim = null; e.sprint = false; e.dash = 0; e.target = null; e.vel = [0, 0];
    st.events.push({ type: 'engineer-down', t: st.t, x: e.x, y: e.y });
  }
}

/** Prompt B M4: the tile threat layer (threat.ts) installs itself here; the block sim and walk.ts call through it so
 *  neither imports the tile layer. `spawn` returns false where there is no tile threat (the lattice), and the block
 *  model's arithmetic carries on as before. */
export interface ThreatHooks {
  tick(st: SimState, dt: number): void;
  spawn(st: SimState, edgeId: number, crawlers: number, shades: number, escaped: boolean): boolean;
  /** Danger this block second (a crawler within reach of the engineer) and whether the rifle fired in it. */
  second(st: SimState): { danger: boolean; shot: boolean };
  /** An aimed round toward (ax, ay); false where there is no tile threat to hit. */
  fire(st: SimState, e: Engineer, ax: number, ay: number): boolean;
}
export const threatHooks: { current: ThreatHooks | null } = { current: null };

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
  markShot(st);
  hurt(st, kills * hpPerKill(e));
  return kills;
}

/** D-B1-5: the rounds the aimed rifle put on an engaged edge this second (walk.ts `fireRound`) become kills here,
 *  up to the crawlers the turrets missed. Rounds that flew past are gone. The dodge's invulnerability takes its
 *  share of the second's retaliation. Returns the crawlers killed. */
export function rifleHits(st: SimState, edgeId: number, un: number): number {
  const e = st.engineer, p = e.shots[edgeId] ?? 0;
  if (p <= 0 || un <= 1e-9 || e.down >= 0) return 0;
  e.shots[edgeId] = 0;
  const kills = Math.min(un, p / ROUNDS_PER_CRAWLER);
  e.kills += kills;
  hurt(st, kills * hpPerKill(e) * Math.max(0, 1 - e.iframes));
  return kills;
}

/** Hook for the tile-level hand actions (mine, craft, place, pick up); flow.ts installs it. */
export const handHook: { current: ((st: SimState, c: Command) => void) | null } = { current: null };

export function engineerCommand(st: SimState, c: Command): void {
  const e = st.engineer;
  if(st.campaign?.progression?.passenger&&c.type!=='progression'&&c.type!=='inventory')return;
  switch (c.type) {
    case 'move': if (e.down < 0 && !e.truckSeat) { e.target = [c.x, c.y]; e.vel = [0, 0]; e.dest = -1; } break;
    case 'walk': if (e.down < 0) { e.vel = [c.dx, c.dy]; if (c.dx || c.dy) e.target = null; e.dest = -1; } break;
    case 'walkTo': if(!e.truckSeat)walkTo(st, c.block); break;   // bots and dev hooks only (D-B1-5): the player has no click-to-walk in the world
    case 'fire': e.firing = c.edge; break;
    case 'sprint': e.sprint = c.on && e.down < 0 && !e.truckSeat; break;
    case 'dodge':
      if (e.down < 0 && !e.truckSeat && e.dash <= 0 && e.dashCooldown <= 0 && e.stamina >= DODGE_COST - 1e-9) {
        const L = Math.hypot(e.vel[0], e.vel[1]);
        e.dashDir = L > 0 ? [e.vel[0] / L, e.vel[1] / L] : [e.face[0], e.face[1]];
        e.dash = DODGE_S; e.dashCooldown = DODGE_COOLDOWN_S; e.stamina -= DODGE_COST; e.target = null; e.dest = -1;
      }
      break;
    case 'aim': e.aim = e.down < 0 ? c.at : null; break;
    case 'enterTruck': if(st.campaign){handHook.current?.(st,{type:'factory',action:{type:'truckBoard'}});break;} if (e.truckFound && e.down < 0) e.truck = !e.truck; break;
    case 'navigation': case 'removeArea': case 'truckWork': case 'blueprintLibrary': case 'blueprintOrder': case 'blueprintCopy': case 'blueprintTransform': case 'blueprintPaste':
    case 'fabrication': case 'equipment': case 'region': case 'cityProp': case 'progression': case 'inventory': case 'construct': case 'buildPath': case 'undergroundPair': case 'undoBuild': case 'redoBuild': case 'factory':
    case 'mineAt': case 'craft': case 'place': case 'pickUp': case 'chestTake': case 'chestPut': case 'feed': case 'repair': case 'rotate':
    case 'deliverSite': case 'restoreSite': case 'collectTramKit':
    case 'deliver': case 'activate': case 'commission':   // RI-03: the commissioning commands are hand commands too; RI-05: a project's too
    case 'setStationRules':
    case 'deliverTurbine': case 'restoreTurbine': case 'setTurbineEnabled': case 'recruitSurvivors': case 'recoverSchematic': case 'repairDefence': case 'upgradeRadio': case 'repairCabinet': case 'abort': handHook.current?.(st, c); break;   // RI-06: the Heart's feeder repair and the explicit abort
  }
}

export type EngineerEvent = Extract<SimEvent, { type: 'engineer-down' | 'engineer-up' | 'kitted' | 'truck' | 'rifle' }>;
