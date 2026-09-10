import {machineTransfer,type MachineTransfer} from './machineInventory';
import {progressionCommand,progressionCheck} from './progression';
import {inventoryCommand,pocketSlots} from './engineer';
import { truckWorkCommand } from './truckWork';
import {navigationCommand} from './navigation';
import { blueprintLibrary, blueprintOrder } from './blueprintPlans';
import { TRUCK_RULES, boardTruck, truckTransfer } from './truck';
/** P5-01: bounded, saved construction history. Inverses use current stock and reach, never rewind the world. */
import type { Command, SimState } from './types';
import { canPlace, place, canPickUp, remove, rotate, machineAt, tramAt, isKind, isRecipeId, MACHINE_SIZE,
  type Kind, type Dir, type RecipeId, type Machine, setRecipe, queueCraft, chestTake, chestPut, isChestItem, isItem,
  pickUpItems, chestCount, depotChestOf, handFeed, setHandMine, repairLight, deliverTo, SHOT, rotateTo, type Item } from './flow';
import { dimensions, machineDimensions } from './footprint';
import { configureRouting, undergroundSpan, undergroundMate, type UndergroundMode, type OutputPriority } from './routing';
import { type StationRules, validStationRules } from './freight';
import { blueprintCopy, blueprintEdits, blueprintTransform, blueprintGate } from './blueprint';
import { inReach, distToRect } from './ground';
import { syncProjects } from './project';

export const BUILD_LIMIT = 128, HISTORY_LIMIT = 50;
export interface TilePoint { x: number; y: number }
export type BuildEdit = { action: 'place'; item: Kind; x: number; y: number; dir: Dir; underground?: UndergroundMode; recipe?: RecipeId; filter?: Item; priority?: OutputPriority; freight?: StationRules }
  | { action: 'pickUp' | 'rotate'; x: number; y: number };
export interface BuildChange { action: BuildEdit['action']; item: Kind; x: number; y: number; dir: Dir; id: number;
  recipe?: RecipeId; freight?: StationRules; beforeDir?: Dir; filter?: Item; priority?: OutputPriority; underground?: UndergroundMode }
export interface ConstructionHistory { version: 1; undo: BuildChange[][]; redo: BuildChange[][] }
export type FactoryAction = MachineTransfer | { type: 'craft'; item: string; count?: number }
  | { type: 'truckBoard' } | { type:'truckCargo'; item:string; n:number; put:boolean }
  | { type: 'routing'; x: number; y: number; filter?: Item | null; priority?: OutputPriority }
  | { type: 'chestTake' | 'chestPut'; item: string; n: number; x?: number; y?: number; expected?:{total:number;layout?:string;slot?:number} }
  | { type: 'feed' | 'mineAt' | 'repair'; x: number; y: number }
  | { type: 'setRecipe'; x: number; y: number; recipe: string }
  | { type: 'deliver'; bx: number; by: number; item: 'steel' | 'copper'; n: number; cabinet?: number };
export interface ActionResult { ok: boolean; reason: string; moved?: number }
const results = new WeakMap<SimState, ActionResult>();
export const actionResult = (st: SimState): ActionResult => results.get(st) ?? { ok: true, reason: '' };
const no = (reason: string): ActionResult => ({ ok: false, reason });
const integer = (v: unknown): v is number => Number.isSafeInteger(v);
const direction = (v: unknown): v is Dir => integer(v) && v >= 0 && v <= 3;
const clone = <T>(v: T): T => JSON.parse(JSON.stringify(v)) as T;
function trialState(st: SimState): SimState {
  const copy = clone(st);
  // Construction reads block geometry/state but never changes it. Keep the geometry cache key;
  // inventories, machines, counters and repair/project records remain isolated in the trial.
  copy.blocks = st.blocks;
  return copy;
}

/** Fill skipped pointer tiles, x then y; backtracking trims the preview instead of building overlapping belts. */
export function extendBuildPath(path: readonly TilePoint[], target: TilePoint): TilePoint[] {
  const out = path.map(p => ({ ...p }));
  if (!integer(target.x) || !integer(target.y)) return out;
  if (!out.length) return [{ ...target }];
  let { x, y } = out[out.length - 1];
  let steps = 0;
  while ((x !== target.x || y !== target.y) && steps++ < BUILD_LIMIT) {
    if (x !== target.x) x += Math.sign(target.x - x); else y += Math.sign(target.y - y);
    const prior = out.findIndex(p => p.x === x && p.y === y);
    if (prior >= 0) out.splice(prior + 1); else if (out.length < BUILD_LIMIT) out.push({ x, y }); else break;
  }
  return out;
}
export function pathEdits(path: readonly TilePoint[], endDir: Dir): BuildEdit[] {
  return path.map((p, i) => {
    const next = path[i + 1], prev = path[i - 1];
    const dx = next ? next.x - p.x : prev ? p.x - prev.x : 0, dy = next ? next.y - p.y : prev ? p.y - prev.y : 0;
    return { action: 'place', item: 'belt', ...p, dir: next || prev ? (dx > 0 ? 1 : dx < 0 ? 3 : dy > 0 ? 2 : 0) : endDir };
  });
}
function describe(m: Machine, action: BuildEdit['action']): BuildChange {
  return { action, item: m.kind, x: m.x, y: m.y, dir: m.dir, id: m.id,
    ...(m.recipe ? { recipe: m.recipe } : {}), ...(m.freight ? { freight: clone(m.freight) } : {}),
    ...(m.filter ? {filter:m.filter}:{}), ...(m.priority ? {priority:m.priority}:{}), ...(m.underground ? {underground:m.underground}:{}) };
}
function same(m: Machine, c: BuildChange): boolean {
  return m.id === c.id && m.kind === c.item && m.x === c.x && m.y === c.y && m.dir === c.dir
    && (m.recipe ?? 'shot') === (c.recipe ?? 'shot') && JSON.stringify(m.freight ?? {}) === JSON.stringify(c.freight ?? {})
    && m.filter === c.filter && (m.priority??'balanced') === (c.priority??'balanced') && m.underground === c.underground;
}
export type Builder = 'engineer'|'truck';
function buildReach(st:SimState,e:BuildEdit,builder:Builder):boolean {
 if(e.action!=='place'||builder==='engineer')return inReach(st,e.x,e.y,...(e.action==='place'?dimensions(e.item,e.dir,MACHINE_SIZE[e.item]):[1] as [number]));
 const t=st.campaign?.truck;return !!t&&!st.engineer.truckSeat&&distToRect(t.x,t.y,e.x,e.y,...dimensions(e.item,e.dir,MACHINE_SIZE[e.item]))<=TRUCK_RULES.serviceReach;
}
function edit(st: SimState, e: BuildEdit, builder:Builder='engineer'): BuildChange | string {
  if (!integer(e.x) || !integer(e.y)) return 'Invalid construction coordinates';
  if (e.action === 'place') {
    if (!isKind(e.item) || e.item === 'depot' || !direction(e.dir)) return 'Invalid building';
    if(e.underground!==undefined&&(e.item!=='underground'||!['input','output'].includes(e.underground)))return 'Invalid underground endpoint';
    if(e.recipe!==undefined&&(!['assembler','assembler2','foundry','refinery'].includes(e.item)||!isRecipeId(e.recipe)))return 'Invalid recipe';
    if(e.filter!==undefined&&(e.item!=='inserter'||!isItem(e.filter)))return 'Invalid filter';
    if(e.priority!==undefined&&(e.item!=='splitter'||!['balanced','left','right'].includes(e.priority)))return 'Invalid priority';
    if(e.freight!==undefined&&(e.item!=='tramstop'||!validStationRules(e.freight)))return 'Invalid station freight settings';
    if (!buildReach(st,e,builder)) return builder==='truck'?'Truck needs a nearer street service position':'Walk closer to build';
    if(builder==='engineer'&&!['belt','fastbelt','pole','bigpole','lamp','track','underground','splitter'].includes(e.item)){const [w,h]=dimensions(e.item,e.dir,MACHINE_SIZE[e.item]);if(st.engineer.x>=e.x&&st.engineer.x<e.x+w&&st.engineer.y>=e.y&&st.engineer.y<e.y+h)return 'Step outside the machine footprint before building';}
    const stock=builder==='truck'?st.campaign!.truck!.cargo:st.engineer.inv;
    const check = canPlace(st, e.item, e.x, e.y,e.dir,stock); if (!check.ok) return check.reason;
    const built=place(st, e.item, e.x, e.y, e.dir, stock)!;
    if(e.underground)built.underground=e.underground;
    if(e.recipe)built.recipe=e.recipe;
    if(e.filter)built.filter=e.filter;
    if(e.priority)built.priority=e.priority;
    if(e.freight)built.freight=clone(e.freight);
    return describe(built, 'place');
  }
  if(builder==='truck')return 'Truck construction only places planned machines';
  const m = tramAt(st, e.x, e.y) ?? machineAt(st, e.x, e.y);
  if (!m) return 'Nothing there';
  if (!inReach(st, m.x, m.y, ...machineDimensions(m))) return 'Walk closer to the machine';
  if (e.action === 'pickUp') {
    if(depotChestOf(st,m))return 'The restored supply installation stays';
    const check = canPickUp(st, e.x, e.y); if (!check.ok) return check.reason;
    // Old packing rounds fractional contents down. Refuse rather than silently lose them through history.
    if (Object.entries(m.inv).some(([k, n]) => k !== 'rounds' && !integer(n)) || Object.values(m.cargo ?? {}).some(n => !integer(n))) return 'Empty fractional contents before packing this machine';
    if (m.kind === 'turret' && (m.inv.rounds ?? 0) % SHOT.count > Math.max(0, st.config.bufferCap - st.buffer)) return 'Make room in the ammunition buffer for this turret’s loose rounds';
    if (['assembler','assembler2','foundry','refinery','mixer'].includes(m.kind) && m.busy) return 'Wait for this assembler to finish its current recipe before packing it';
    const c = describe(m, 'pickUp'); remove(st, m.x, m.y); return c;
  }
  if (e.action !== 'rotate') return 'Invalid construction action';
  const before = m.dir;
  if(m.kind==='underground'||m.kind==='splitter'){const why=rotateTo(st,m,((m.dir+1)%4) as Dir);if(why)return why;}
  else if (!rotate(st, m.x, m.y)) return 'This machine does not rotate';
  return { ...describe(m, 'rotate'), beforeDir: before };
}
function runEdits(st: SimState, edits: readonly BuildEdit[], builder:Builder='engineer'): BuildChange[] | string {
  const changes: BuildChange[] = [];
  for (const e of edits) { const r = edit(st, e, builder); if (typeof r === 'string') return r; changes.push(r); }
  return changes;
}
function invert(st: SimState, changes: BuildChange[], undo: boolean): string {
  const list = undo ? [...changes].reverse() : changes;
  for (const c of list) {
    if (c.action === 'rotate') {
      const m = machineAt(st, c.x, c.y), expected = undo ? c.dir : c.beforeDir!;
      if (!m || !same(m, { ...c, dir: expected })) return 'The machine changed since this action';
      if (!inReach(st, m.x, m.y, ...machineDimensions(m))) return 'Walk closer to undo or redo';
      const why=rotateTo(st,m,undo ? c.beforeDir! : c.dir);if(why)return why;continue;
    }
    const packing = undo ? c.action === 'place' : c.action === 'pickUp';
    if (packing) {
      const m = st.flow!.machines.find(m => m.id === c.id);
      if (!m || !same(m, c)) return 'The machine moved, was removed, or its settings changed';
      const r = edit(st, { action: 'pickUp', x: c.x, y: c.y }); if (typeof r === 'string') return r;
    } else {
      const r = edit(st, { action: 'place', item: c.item, x: c.x, y: c.y, dir: c.dir, ...(c.underground?{underground:c.underground}:{}) });
      if (typeof r === 'string') return r;
      const m = st.flow!.machines.find(m => m.id === r.id)!;
      // Rebuilding keeps its identity so adjacent placement/rotation/pickup history still refers to it.
      // The allocator remains monotonic; no living entity is replaced.
      if (!st.flow!.machines.some(other => other !== m && other.id === c.id)) {
        m.id = c.id;
        const [w,h]=machineDimensions(m);
        if (m.kind !== 'tram') for (let y = m.y; y < m.y + h; y++) for (let x = m.x; x < m.x + w; x++) st.flow!.occ[y * st.flow!.tw + x] = m.id;
      } else c.id = r.id;
      if (c.recipe) m.recipe = c.recipe;
      if (c.freight) m.freight = clone(c.freight);
      if(c.filter)m.filter=c.filter;
      if(c.priority)m.priority=c.priority;
    }
  }
  return '';
}
export function constructionCheck(st: SimState, edits: readonly BuildEdit[], builder:Builder='engineer'): ActionResult {
  if (!st.flow || (builder==='engineer'&&st.engineer.down >= 0)) return no('Construction is unavailable while down');
  if (!Array.isArray(edits) || edits.length < 1 || edits.length > BUILD_LIMIT || edits.some(e => !e || typeof e !== 'object')) return no('Invalid construction group');
  const trial=trialState(st), result = runEdits(trial, edits,builder);
  // Clipboard endpoint roles must still pair as intended in the destination world.
  if(typeof result!=='string')for(const e of edits){
    if(e.action!=='place'||e.item!=='underground'||e.underground!=='input')continue;
    const intended=edits.filter(b=>b.action==='place'&&b.item==='underground'&&b.underground==='output'&&b.dir===e.dir&&!undergroundSpan(e,b,e.dir));
    if(intended.length===1){const actual=undergroundMate(trial,machineAt(trial,e.x,e.y)!);
      if(!actual||actual.x!==intended[0].x||actual.y!==intended[0].y)return no('Another underground endpoint interrupts the copied pair');}
  }
  return typeof result === 'string' ? no(result) : { ok: true, reason: '' };
}
export function construct(st: SimState, edits: readonly BuildEdit[], builder:Builder='engineer'): ActionResult {
  const check = constructionCheck(st, edits,builder); if (!check.ok) return check;
  const changes = runEdits(st, edits,builder) as BuildChange[];
  if(builder==='truck'){syncProjects(st);return {ok:true,reason:'Built from truck cargo'};}
  const h = st.construction ??= { version: 1, undo: [], redo: [] };
  h.undo.push(changes); if (h.undo.length > HISTORY_LIMIT) h.undo.shift(); h.redo = [];
  syncProjects(st); return { ok: true, reason: `${changes.length} construction action${changes.length === 1 ? '' : 's'} completed` };
}
/** A preview captures identities/settings, never inventories. Apply rechecks the whole current transaction. */
export interface RemovalSelection {from:TilePoint;to:TilePoint;machines:BuildChange[]}
export interface RemovalPreview extends ActionResult {selection?:RemovalSelection;items:Record<string,number>;looseRounds:number}
export function removalPreview(st:SimState,from:TilePoint,to:TilePoint):RemovalPreview {
 const result:RemovalPreview={ok:false,reason:'',items:{},looseRounds:0};
 try {
  const why=blueprintGate(st);if(why)throw Error(why);
  if(!st.flow||!from||!to||![from.x,from.y,to.x,to.y].every(integer))throw Error('Invalid removal coordinates');
  const x=Math.min(from.x,to.x),y=Math.min(from.y,to.y),w=Math.abs(from.x-to.x)+1,h=Math.abs(from.y-to.y)+1;
  if(x<0||y<0||w>128||h>128)throw Error('Select an area no larger than 128 × 128 tiles');
  const machines=st.flow.machines.filter(m=>{const [mw,mh]=machineDimensions(m);return m.x<x+w&&m.x+mw>x&&m.y<y+h&&m.y+mh>y;}).sort((a,b)=>Number(b.kind==='tram')-Number(a.kind==='tram')||a.id-b.id);
  if(!machines.length||machines.length>BUILD_LIMIT)throw Error('Select 1–128 complete machines');
  for(const m of machines){const [mw,mh]=machineDimensions(m);if(m.x<x||m.y<y||m.x+mw>x+w||m.y+mh>y+h)throw Error('Select the whole footprint of every machine');}
  result.selection={from:{...from},to:{...to},machines:machines.map(m=>describe(m,'pickUp'))};
  for(const m of machines){for(const [k,n] of Object.entries(pickUpItems(m)))result.items[k]=(result.items[k]??0)+n;if(m.kind==='turret')result.looseRounds+=(m.inv.rounds??0)%SHOT.count;}
  const check=constructionCheck(st,machines.map(m=>({action:'pickUp',x:m.x,y:m.y})));
  result.ok=check.ok;result.reason=check.ok?`Pack all ${machines.length} machines in one action. Contents go to pockets; loose rounds go to the line buffer.`:check.reason;
 }catch(e){result.reason=(e as Error).message;}return result;
}
export function removeArea(st:SimState,selection:RemovalSelection):ActionResult {
 if(!selection||!Array.isArray(selection.machines)||selection.machines.some(m=>!m||m.action!=='pickUp'))return no('Select and preview machines first');
 const preview=removalPreview(st,selection.from,selection.to),current=preview.selection;
 if(!current||current.machines.length!==selection.machines.length||current.machines.some((c,i)=>{const m=st.flow!.machines.find(m=>m.id===c.id);return !m||!same(m,selection.machines[i]);}))return no('Selection changed; select and preview the area again');
 if(!preview.ok)return no(preview.reason);
 return construct(st,current.machines.map(m=>({action:'pickUp',x:m.x,y:m.y})));
}
function newUndergroundEdits(st:SimState,edits:BuildEdit[]):BuildEdit[]{
  return edits.filter(e=>{const m=machineAt(st,e.x,e.y);return e.action!=='place'||!m||m.kind!==e.item||m.dir!==e.dir||m.underground!==e.underground;});
}
export function undergroundCheck(st:SimState,edits:BuildEdit[],builder:Builder='engineer'):ActionResult {
  if(edits.length!==2||edits.some(e=>e.action!=='place'||e.item!=='underground'))return no('Choose two underground endpoints');
  const a=edits[0],b=edits[1];
  if(a.action!=='place'||b.action!=='place'||a.underground!=='input'||b.underground!=='output'||a.dir!==b.dir)return no('Choose an input and a same-facing output');
  const why=undergroundSpan(a,b,a.dir);if(why)return no(why);
  const additions=newUndergroundEdits(st,edits);if(!additions.length)return no('These underground endpoints are already built');
  const check=constructionCheck(st,additions,builder);if(!check.ok)return check;
  if(!buildReach(st,a,builder)||!buildReach(st,b,builder))return no('Move the builder closer to both underground endpoints');
  const trial=trialState(st);runEdits(trial,additions,builder);
  const m=machineAt(trial,a.x,a.y)!,mate=undergroundMate(trial,m);
  return mate?.x===b.x&&mate.y===b.y?{ok:true,reason:''}:no('Another underground endpoint interrupts this pair');
}
function history(st: SimState, undo: boolean): ActionResult {
  if (!st.flow || st.engineer.down >= 0) return no('Construction is unavailable while down');
  const h = st.construction, from = h?.[undo ? 'undo' : 'redo'];
  if (!h || !from?.length) return no(undo ? 'Nothing to undo' : 'Nothing to redo');
  const changes = clone(from[from.length - 1]);
  const reason = invert(trialState(st), clone(changes), undo); if (reason) return no(reason);
  invert(st, changes, undo); from.pop(); h[undo ? 'redo' : 'undo'].push(changes);
  syncProjects(st); return { ok: true, reason: undo ? 'Construction undone; packed contents stay in your pockets' : 'Construction redone' };
}
/** Central command feedback is transient and never part of a save or gameplay hash. */
function factory(st: SimState, c: FactoryAction): ActionResult {
  if (!st.flow) return no('No factory is available');
  if (st.engineer.down >= 0 && c.type !== 'mineAt') return no('Wait until you recover');
  if ('x' in c && c.x !== undefined && (!integer(c.x) || !integer(c.y))) return no('Invalid coordinates');
  switch (c.type) {
    case 'machineTransfer': return machineTransfer(st,c);
    case 'truckBoard': {const seated=st.engineer.truckSeat,why=boardTruck(st);return {ok:!why,reason:why||(seated?'Exited truck; cargo stays aboard':'Driving truck: WASD, E to exit')};}
    case 'truckCargo': return truckTransfer(st,c.item,c.n,c.put);
    case 'routing': {const why=configureRouting(st,c.x,c.y,c);return {ok:!why,reason:why||'Routing setting changed; held items are preserved'};}
    case 'mineAt': setHandMine(st, c.x < 0 || c.y < 0 ? null : [c.x, c.y]); return { ok: true, reason: '' };
    case 'craft': { if (!integer(c.count ?? 1) || (c.count ?? 1) < 1) return no('Invalid craft count'); const why = queueCraft(st, c.count ?? 1); return { ok: !why, reason: why || 'Hand craft queued' }; }
    case 'chestTake': case 'chestPut': {
      if (!isChestItem(c.item) || !Number.isFinite(c.n) || c.n < 0) return no('Invalid item transfer');
      const at: [number, number] | undefined = c.x === undefined ? undefined : [c.x, c.y!];
      const before=pocketSlots(st.engineer);
      if(c.expected){
       const m=at?machineAt(st,at[0],at[1]):null,total=c.type==='chestPut'?(st.engineer.inv[c.item]??0):m?(m.inv[c.item]??0)+(m.kind==='tramstop'?(m.cargo?.[c.item]??0):0):chestCount(st,c.item);
       if(total!==c.expected.total)return no('Source changed; select the stack again.');
       if(c.type==='chestPut'&&(c.expected.layout!==JSON.stringify(before)||!Number.isInteger(c.expected.slot)||before[c.expected.slot!]?.item!==c.item||before[c.expected.slot!]!.count<c.n))return no('Source stack changed; select it again.');
      }
      const r = c.type === 'chestTake' ? chestTake(st, c.item, c.n, at) : chestPut(st, c.item, c.n, at);
      if(r.moved>0&&c.type==='chestPut'&&c.expected&&c.item!=='kit'){
       const slot=c.expected.slot!;before[slot]!.count-=r.moved;if(before[slot]!.count<=0)before[slot]=null;st.engineer.pack=before;
      }
      return { ok: r.moved > 0, moved: r.moved, reason: r.reason || `${r.moved} ${c.item} transferred` };
    }
    case 'deliver': { const r = deliverTo(st, c.bx, c.by, c.item, c.n, c.cabinet ?? -1); return { ...r, reason: r.reason || `${r.moved} ${c.item} delivered` }; }
    case 'repair': { if (!inReach(st, c.x, c.y, 1)) return no('Walk closer to repair'); const r = repairLight(st, c.x, c.y); return { ok: r.ok, reason: r.reason || 'Light repaired' }; }
    case 'feed': case 'setRecipe': {
      const m = machineAt(st, c.x, c.y); if (!m || !inReach(st, m.x, m.y, m.size)) return no('Walk closer to the machine');
      if (c.type === 'setRecipe') { if (!isRecipeId(c.recipe)) return no('Unknown recipe'); const why = setRecipe(st, c.x, c.y, c.recipe); return { ok: !why, reason: why || 'Recipe changed' }; }
      const r = handFeed(st, c.x, c.y); return r ? { ok: r.moved > 0, reason: r.reason || `${r.moved} supplies added`, moved: r.moved } : no('This machine cannot be hand-fed');
    }
  }
}
export function constructionCommand(st: SimState, c: Command): boolean {
  let r: ActionResult;
  switch (c.type) {
    case 'navigation': r=navigationCommand(st,c.action);break;
    case 'removeArea': r=removeArea(st,c.selection);break;
    case 'truckWork': r=truckWorkCommand(st,c.action);break;
    case 'blueprintLibrary': r=blueprintLibrary(st,c.action);break;
    case 'blueprintOrder': r=blueprintOrder(st,c.action);break;
    case 'blueprintCopy':
      try {const bp=blueprintCopy(st,c.from,c.to);st.campaign!.clipboard=bp;r={ok:true,reason:`Copied ${st.campaign!.clipboard.entities.length} machines and settings; Ctrl+V previews a paid stamp.`};}
      catch(e){r=no((e as Error).message);}break;
    case 'blueprintTransform':
      try {const why=blueprintGate(st);if(why)throw new Error(why);
        if(!st.campaign!.clipboard)throw new Error('Copy a layout first (Ctrl+C).');
        st.campaign!.clipboard=blueprintTransform(st.campaign!.clipboard,c.operation);r={ok:true,reason:'Clipboard transformed; source machines unchanged.'};}
      catch(e){r=no((e as Error).message);}break;
    case 'blueprintPaste':
      try {r=construct(st,blueprintEdits(st,c.x,c.y));}
      catch(e){r=no((e as Error).message);}break;
    case 'undergroundPair': {
      const why=!c.from||!c.to||!direction(c.dir)?'Invalid underground direction':undergroundSpan(c.from,c.to,c.dir);
      if(why){r=no(why);break;}
      const edits:BuildEdit[]=[{action:'place',item:'underground',...c.from,dir:c.dir,underground:'input'},{action:'place',item:'underground',...c.to,dir:c.dir,underground:'output'}];
      r=undergroundCheck(st,edits);if(r.ok)r=construct(st,newUndergroundEdits(st,edits));break;
    }
    case 'construct': r = construct(st, c.edits); break;
    case 'buildPath': {
      if (!Array.isArray(c.path) || !direction(c.dir) || c.path.length < 1 || c.path.length > BUILD_LIMIT || c.path.some((p, i) => !p || !integer(p.x) || !integer(p.y) || (i > 0 && Math.abs(p.x - c.path[i - 1].x) + Math.abs(p.y - c.path[i - 1].y) !== 1)) || new Set(c.path.map(p => `${p.x},${p.y}`)).size !== c.path.length) r = no('Invalid belt path');
      else r = construct(st, pathEdits(c.path, c.dir)); break;
    }
    case 'undoBuild': r = history(st, true); break;
    case 'redoBuild': r = history(st, false); break;
    case 'inventory': r = inventoryCommand(st.engineer,c.action); break;
    case 'progression': {const why=progressionCheck(st,c.action);r=why?no(why):{ok:true,reason:progressionCommand(st,c.action)};break;}
    case 'factory': r = factory(st, c.action); syncProjects(st); break;
    default: return false;
  }
  results.set(st, r); return true;
}
/** Optional history has its own version; older profiles stay byte-for-byte unchanged until a new UI command. */
export function constructionProblem(st: SimState): string {
  const h = st.construction;
  if (h === undefined) return '';
  if (!h || h.version !== 1 || !Array.isArray(h.undo) || !Array.isArray(h.redo) || h.undo.length + h.redo.length > HISTORY_LIMIT) return 'invalid construction history';
  for (const group of [...h.undo, ...h.redo]) {
    if (!Array.isArray(group) || group.length < 1 || group.length > BUILD_LIMIT) return 'invalid construction group';
    for (const c of group) {
      if (!c || !['place', 'pickUp', 'rotate'].includes(c.action) || !isKind(c.item) || c.item === 'depot' || !integer(c.x) || !integer(c.y) || !integer(c.id) || c.id < 0 || !direction(c.dir) || (c.action === 'rotate' && !direction(c.beforeDir)) || (c.recipe !== undefined && (!['assembler','assembler2','foundry','refinery'].includes(c.item) || !isRecipeId(c.recipe)))) return 'invalid construction change';
      if(c.filter!==undefined&&(c.item!=='inserter'||!isItem(c.filter)))return 'invalid saved filter';
      if(c.priority!==undefined&&(c.item!=='splitter'||!['balanced','left','right'].includes(c.priority)))return 'invalid saved priority';
      if(c.underground!==undefined&&(c.item!=='underground'||!['input','output'].includes(c.underground)))return 'invalid saved underground';
      if (c.freight !== undefined) {
        if (c.item !== 'tramstop' || !c.freight || typeof c.freight !== 'object' || Array.isArray(c.freight)) return 'invalid saved station settings';
        for (const [item, rule] of Object.entries(c.freight)) if (!isChestItem(item) || item === 'kit' || !rule || !integer(rule.request) || rule.request < 0 || !integer(rule.reserve) || rule.reserve < 0 || typeof rule.export !== 'boolean') return 'invalid saved station settings';
      }
    }
  }
  return '';
}
