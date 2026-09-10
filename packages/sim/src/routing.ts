/** P5-02 routing queries and simulation. All queues live on their owning machine and in the normal ledger. */
import type { SimState } from './types';
import { observeOutput } from './inspection';
import { machineAt, giveItem, accepts, beltRoom, beltInsert, DX, DY, BELT_SPACING, BELT_SPEED, MACHINE_COST, isItem, type Machine, type Item, type Dir } from './flow';
import { machineDimensions } from './footprint';
import { ground, inReach } from './ground';

export const UNDERGROUND_HIDDEN = 4, SPLITTER_CAPACITY = 8, SPLITTER_PER_S = 15;
/** Fingerprint the task defaults without eagerly reading the mutually importing flow module. */
export function routingConfig(){return {undergroundHidden:UNDERGROUND_HIDDEN,splitterCapacity:SPLITTER_CAPACITY,splitterPerSecond:SPLITTER_PER_S,undergroundCost:MACHINE_COST.underground,splitterCost:MACHINE_COST.splitter};}
export type OutputPriority = 'balanced' | 'left' | 'right';
export type UndergroundMode = 'input' | 'output';
export const isRouting = (m: Machine): boolean => m.kind === 'underground' || m.kind === 'splitter';
export const isConveyor = (m: Machine): boolean => m.kind === 'belt' || m.kind==='fastbelt' || isRouting(m);
export function undergroundMate(st: SimState, m: Machine): Machine | undefined {
  if (m.kind !== 'underground') return;
  const sign = m.underground === 'output' ? -1 : 1;
  for (let d = 1; d <= UNDERGROUND_HIDDEN + 1; d++) {
    const n = machineAt(st, m.x + DX[m.dir] * d * sign, m.y + DY[m.dir] * d * sign);
    if (n?.kind === 'underground' && n.dir === m.dir) return n.underground !== m.underground ? n : undefined;
  }
}
export function undergroundSpan(from: {x:number;y:number}, to: {x:number;y:number}, dir: Dir): string {
  const dx = to.x - from.x, dy = to.y - from.y, d = dx * DX[dir] + dy * DY[dir];
  return Number.isSafeInteger(d) && d >= 1 && d <= UNDERGROUND_HIDDEN + 1 && dx === DX[dir] * d && dy === DY[dir] * d ? '' : 'Face the output along the input: at most four hidden tiles';
}
/** The two front/back adjacent tiles, ordered left/right relative to facing. */
export function splitterPorts(m: Machine, front = true): [number, number][] {
  const {x,y,dir:d} = m, sign = front ? 1 : -1;
  if (d === 0) return [[x, y-sign], [x+1,y-sign]];
  if (d === 1) return [[x+sign,y], [x+sign,y+1]];
  if (d === 2) return [[x+1,y+sign], [x,y+sign]];
  return [[x-sign,y+1], [x-sign,y]];
}
export function routingAccepts(st: SimState, m: Machine): boolean {
  if (m.kind === 'splitter') return m.items.length < SPLITTER_CAPACITY;
  if (m.underground !== 'input') return false;
  const mate = undergroundMate(st, m), length = mate ? Math.abs(m.x-mate.x)+Math.abs(m.y-mate.y) : 1;
  return m.items.length < length / BELT_SPACING && (!m.items.length || m.items[0].p >= BELT_SPACING - 1e-9);
}
export function routingInsert(m: Machine, k: Item): void {
  if (m.kind === 'splitter') m.items.push({k,p:0}); else beltInsert(m,k,0);
}
/** Belts may enter routing devices only through their rear ports. Inserter placement still defines its own source/target. */
export function routingEntry(m: Machine, x: number, y: number): boolean {
  return m.kind === 'splitter' ? splitterPorts(m,false).some(p=>p[0]===x&&p[1]===y)
    : m.kind !== 'underground' || (m.underground === 'input' && x === m.x-DX[m.dir] && y === m.y-DY[m.dir]);
}
function forwardTarget(st: SimState, m: Machine, x: number, y: number): Machine | undefined {
  const n = machineAt(st,x,y);
  if (!n || (isConveyor(n) && (n.dir+2)%4===m.dir)) return;
  if (isRouting(n) && !routingEntry(n,x-DX[m.dir],y-DY[m.dir])) return;
  return n;
}
function forward(st: SimState, m: Machine, x: number, y: number, k: Item): boolean {
  const n=forwardTarget(st,m,x,y);return !!n&&giveItem(st,n,k);
}
export function routingStatus(st:SimState,m:Machine):'starved'|'blocked'|'running'|'idle'{
  const mate=undergroundMate(st,m),leading=m.items[m.kind==='splitter'?0:m.items.length-1];
  if(m.kind==='underground'&&m.underground==='input'&&!mate)return 'starved';
  if(!leading)return 'idle';
  if(m.kind==='underground'&&m.underground==='input'){
    return leading.p>=Math.abs(m.x-mate!.x)+Math.abs(m.y-mate!.y)-BELT_SPACING/2&&!beltRoom(mate!,0)?'blocked':'running';
  }
  const ports=m.kind==='splitter'?splitterPorts(m):[[m.x+DX[m.dir],m.y+DY[m.dir]]];
  const blocked=ports.every(([x,y])=>{const n=forwardTarget(st,m,x,y);return !n||!accepts(st,n,leading.k);});
  return blocked&&(m.kind==='splitter'||leading.p>=1-BELT_SPACING/2)?'blocked':'running';
}
export function tickRouting(st: SimState, m: Machine, dt: number): void {
  if (m.kind === 'splitter') {
    m.timer = Math.min(2/SPLITTER_PER_S, m.timer+dt);
    if (m.timer+1e-9 < 1/SPLITTER_PER_S || !m.items.length) return;
    const ports=splitterPorts(m), first=m.priority==='left'?0:m.priority==='right'?1:(m.routingNext??0);
    for (const side of [first,1-first]) {
      if (!forward(st,m,...ports[side] as [number,number],m.items[0].k)) continue;
      observeOutput(st,m,m.items[0].k);m.items.shift();m.routingNext=(1-side) as 0|1;m.timer-=1/SPLITTER_PER_S;return;
    }
    return;
  }
  if (m.kind !== 'underground' || !m.items.length) return;
  if (m.underground === 'output') {
    for(let i=m.items.length-1;i>=0;i--){
      const it=m.items[i];let np=it.p+BELT_SPEED*dt;
      if(i===m.items.length-1){if(np>=1){if(forward(st,m,m.x+DX[m.dir],m.y+DY[m.dir],it.k)){observeOutput(st,m,it.k);m.items.pop();continue;}np=1-BELT_SPACING/2;}}
      else np=Math.min(np,m.items[i+1].p-BELT_SPACING);
      it.p=Math.max(it.p,np);
    }
    return;
  }
  const mate=undergroundMate(st,m);if(!mate)return;
  const length=Math.abs(m.x-mate.x)+Math.abs(m.y-mate.y);
  for(let i=m.items.length-1;i>=0;i--){
    const it=m.items[i];let np=it.p+BELT_SPEED*dt;
    if(i===m.items.length-1){if(np>=length){if(beltRoom(mate,0)){beltInsert(mate,it.k,0);observeOutput(st,m,it.k);m.items.pop();continue;}np=length-BELT_SPACING/2;}}
    else np=Math.min(np,m.items[i+1].p-BELT_SPACING);
    it.p=Math.max(it.p,np);
  }
}
export function configureRouting(st: SimState,x:number,y:number,setting:{filter?:Item|null;priority?:OutputPriority}):string {
  const m=machineAt(st,x,y);if(!m||!inReach(st,m.x,m.y,...machineDimensions(m)))return 'Walk closer to configure the machine';
  if(setting.filter!==undefined){
    if(m.kind!=='inserter'||(setting.filter!==null&&!isItem(setting.filter)))return 'Choose an item filter on an inserter';
    if(setting.filter===null)delete m.filter;else m.filter=setting.filter;
  } else if(setting.priority!==undefined){
    if(m.kind!=='splitter'||!['balanced','left','right'].includes(setting.priority))return 'Choose balanced, left or right splitter output';
    m.priority=setting.priority;
  } else return 'No routing setting supplied';
  st.flow!.rev++;return '';
}
export function routingDescription(st:SimState,m:Machine):string {
  if(m.kind==='inserter')return `Filter: ${m.filter??'any item'} · held: ${m.hold??'none'} · T cycles filter`;
  if(m.kind==='splitter')return `Splitter → ${['north','east','south','west'][m.dir]} · priority: ${m.priority??'balanced'} · ${m.items.length}/${SPLITTER_CAPACITY} buffered · blocked priority falls back · T cycles priority`;
  const mate=undergroundMate(st,m);
  return `Underground ${m.underground} · ${mate?`${Math.abs(m.x-mate.x)+Math.abs(m.y-mate.y)-1} hidden tiles; paired`:'unpaired: stock waits'} · ${m.items.length} buffered`;
}
export function routingProblem(st:SimState):string {
  for(const m of st.flow?.machines??[]){
    if(m.pickupNext!==undefined&&(!isConveyor(m)||!Number.isInteger(m.pickupNext)||m.pickupNext<0||m.pickupNext>=32))return 'invalid conveyor pickup cursor';
    if(m.filter!==undefined&&(m.kind!=='inserter'||!isItem(m.filter)))return 'invalid inserter filter';
    if(m.priority!==undefined&&(m.kind!=='splitter'||!['balanced','left','right'].includes(m.priority)))return 'invalid splitter priority';
    if(m.routingNext!==undefined&&(m.kind!=='splitter'||![0,1].includes(m.routingNext)))return 'invalid splitter alternation';
    if(m.underground!==undefined&&(m.kind!=='underground'||!['input','output'].includes(m.underground)))return 'invalid underground mode';
    if(!isRouting(m))continue;
    if(!st.campaign||m.size!==1||!Number.isSafeInteger(m.id)||m.id<0||!Number.isSafeInteger(m.x)||!Number.isSafeInteger(m.y)||![0,1,2,3].includes(m.dir))return 'invalid routing footprint';
    if(m.kind==='underground'&&!m.underground)return 'missing underground mode';
    const capacity=m.kind==='splitter'?SPLITTER_CAPACITY:m.underground==='output'?4:20,maxPosition=m.kind==='splitter'?0:m.underground==='output'?1:5;
    if(!Array.isArray(m.items)||m.items.length>capacity||m.items.some(it=>!it||!isItem(it.k)||!Number.isFinite(it.p)||it.p<0||it.p>maxPosition))return 'invalid routing buffer';
    const [w,h]=machineDimensions(m);
    if(m.x<0||m.y<0||m.x+w>st.flow!.tw||m.y+h>ground(st).th)return 'invalid routing bounds';
    for(let y=m.y;y<m.y+h;y++)for(let x=m.x;x<m.x+w;x++)if(st.flow!.occ[y*st.flow!.tw+x]!==m.id)return 'invalid routing occupancy';
  }
  return '';
}
