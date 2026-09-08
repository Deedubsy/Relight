/** Campaign threats own their schedule and paths; legacy bloom/arrival rules never run here. */
import type { SimState } from './types';
import { ground, inGround, walkable } from './ground';
import { passable } from './walk';
import { moveTo, stepToward, headingWord } from './move';
import { hurt } from './engineer';
import { machineAt, type FlowState } from './flow';
import type { Crawler, ThreatState } from './threat';
import { campaignThrottle } from './campaignPower';
import { blockName } from './names';
import { drop } from './engineer';
import { inReach } from './ground';
import { baseCore, initDefence, damageCore, damageDefence, defenceHp, defenceMax, tickRepair, type BaseCore } from './campaignDefence';

/** Provisional roster/rates; the adopted clock and two raid opportunities stay fixed. */
export const CAMPAIGN_THREAT = { majorCount: 60, spawnEvery: 4, minorMin: 8, minorMax: 12,
  contactDps: 5, structureDps: 8, speed: 2, minorAfterMajor: 300, guardLeash: 12, guardNotice: 8,
  radioUpgradeSteel: 15, radioUpgradeCopper: 10 } as const;
const DAY=1200, DUSK=900, DX=[0,1,0,-1], DY=[-1,0,1,0];
interface Field { dist:Int32Array; targets:number[] }
// Protected production equipment is traversable to creatures; it cannot substitute for paid walls.
function hostileOpen(st:SimState,x:number,y:number):boolean {
  if(!walkable(ground(st),x,y))return false;
  const m=machineAt(st,x,y);return !m||!defenceMax(m)||defenceHp(m)<=0;
}
const fields=new WeakMap<FlowState,{rev:number;map:Map<string,Field>}>();
function field(st:SimState,x:number,y:number,size:number,breach:boolean):Field {
  const f=st.flow!,G=ground(st),tw=G.tw,key=`${x},${y},${size},${breach}`;
  let cache=fields.get(f);if(!cache||cache.rev!==f.rev){cache={rev:f.rev,map:new Map()};fields.set(f,cache);}
  const old=cache.map.get(key);if(old)return old;
  const dist=new Int32Array(G.base.length);dist.fill(-1);const queue=new Int32Array(dist.length),targets:number[]=[];let tail=0;
  const open=(xx:number,yy:number):boolean=>{
    if(!inGround(G,xx,yy)||Math.abs(xx-x)>70||Math.abs(yy-y)>70||!walkable(G,xx,yy))return false;
    if(hostileOpen(st,xx,yy))return true;
    const m=machineAt(st,xx,yy);return breach&&!!m&&defenceMax(m)>0;
  };
  for(let yy=y-1;yy<=y+size;yy++)for(let xx=x-1;xx<=x+size;xx++) {
    if(size>0&&xx>=x&&xx<x+size&&yy>=y&&yy<y+size)continue;
    if(size===0&&(xx!==x||yy!==y))continue;
    if(!open(xx,yy))continue;
    const t=yy*tw+xx;dist[t]=0;queue[tail++]=t;targets.push(t);
  }
  for(let head=0;head<tail;head++) {
    const t=queue[head],tx=t%tw,ty=Math.floor(t/tw);
    for(let k=0;k<4;k++){const xx=tx+DX[k],yy=ty+DY[k],q=yy*tw+xx;if(!open(xx,yy)||dist[q]>=0)continue;dist[q]=dist[t]+1;queue[tail++]=q;}
  }
  const result={dist,targets};if(cache.map.size>32)cache.map.clear();cache.map.set(key,result);return result;
}
function route(st:SimState,c:Crawler,x:number,y:number,size:number):Field {
  const tw=ground(st).tw,t=Math.floor(c.y)*tw+Math.floor(c.x),normal=field(st,x,y,size,false);
  return normal.dist[t]>=0?normal:field(st,x,y,size,true);
}
/** Spawn only outside a base, on reachable, empty ground; Home Court uses its sole mouth. */
export function campaignOrigin(st:SimState,base:BaseCore):number {
  const G=ground(st),tw=G.tw,fld=field(st,base.x,base.y,base.size,true),home=base.block===st.campaign!.homeBlock;
  let best=-1,score=Infinity;
  const gate=G.opening!.gate[1],ox=home?gate%tw:base.x,oy=home?Math.floor(gate/tw):base.y;
  const bounds=G.opening!.bounds;
  for(let y=Math.max(0,oy-30);y<Math.min(G.th,oy+31);y++)for(let x=Math.max(0,ox-30);x<Math.min(tw,ox+31);x++) {
    const t=y*tw+x,d=fld.dist[t];if(d<20||d>60||!passable(st,x,y)||st.flow!.occ[t]!==undefined)continue;
    if(home&&x>=bounds.x&&x<bounds.x+bounds.size&&y>=bounds.y&&y<bounds.y+bounds.size)continue;
    if(Math.hypot(st.engineer.x-x-.5,st.engineer.y-y-.5)<8)continue;
    const s=Math.abs(d-24)*100+Math.hypot(x-ox,y-oy);if(s<score){score=s;best=t;}
  }
  return best;
}
/** Keep the locked compass approach. Occupied entry tiles never require spawning inside a wall.
 * Provisional local search: at most 12 ground-path steps, stable path-distance/tile tie-break. */
export function campaignStaging(st:SimState,base:BaseCore,origin:number):number {
  const G=ground(st),tw=G.tw,ox=origin%tw,oy=Math.floor(origin/tw);
  const toCore=field(st,base.x,base.y,base.size,true),bounds=G.opening!.bounds;
  const approach=headingWord([ox-base.x,oy-base.y]);
  const valid=(x:number,y:number):boolean=>{
    if(!inGround(G,x,y))return false;
    const t=y*tw+x,d=toCore.dist[t];
    if(d<20||d>60||!passable(st,x,y)||st.flow!.occ[t]!==undefined)return false;
    if(base.block===st.campaign!.homeBlock&&x>=bounds.x&&x<bounds.x+bounds.size&&y>=bounds.y&&y<bounds.y+bounds.size)return false;
    return Math.hypot(st.engineer.x-x-.5,st.engineer.y-y-.5)>=8&&headingWord([x-base.x,y-base.y])===approach;
  };
  if(valid(ox,oy))return origin;
  // Only the original tile may be obstructed. Do not choose a new birth point through another wall,
  // or closer to the core than the advertised entry; defence construction must not be bypassed.
  const queue=[{tile:origin,distance:0}],seen=new Set([origin]);let best=-1,distance=Infinity;
  for(let head=0;head<queue.length;head++) {
    const {tile:t,distance:d}=queue[head],x=t%tw,y=Math.floor(t/tw);
    if(d>distance)break;
    if(toCore.dist[t]>=toCore.dist[origin]&&valid(x,y)&&(d<distance||t<best)){best=t;distance=d;}
    if(d>=12)continue;
    for(let k=0;k<4;k++) {
      const xx=x+DX[k],yy=y+DY[k],q=yy*tw+xx;
      if(!inGround(G,xx,yy)||seen.has(q)||!hostileOpen(st,xx,yy))continue;
      seen.add(q);queue.push({tile:q,distance:d+1});
    }
  }
  return best;
}
function eligible(st:SimState,major:boolean):BaseCore[] {
  const d=st.campaign!.defence!,network=(st.campaign!.expansion?.radio.restoredAt??-1)>=0;
  return d.bases.filter(b=>b.hp>0&&(!major||network||b.block===st.campaign!.homeBlock));
}
function birth(st:SimState,T:ThreatState,block:number,origin:number,layer:'major'|'minor'|'site',group:number):void {
  const tw=ground(st).tw;
  T.crawlers.push({id:T.next++,kind:'crawler',x:origin%tw+.5,y:Math.floor(origin/tw)+.5,hp:12,
    edge:-1,from:block,to:block,cls:2,onPlayer:false,escaped:false,born:st.t,stuck:0,dir:[0,0],
    campaign:{layer,group,origin}});
  T.stats.spawned++;
}
export function radioPowered(st:SimState):boolean {
  const radio=st.campaign?.expansion?.radio;
  return !!radio&&radio.restoredAt>=0&&baseCore(st,radio.block)?.hp!==0&&campaignThrottle(st,radio.block)>0;
}
function receiveWarning(st:SimState):void {
  const d=st.campaign!.defence!,a=d.major;
  if(!a||!radioPowered(st))return;
  const core=baseCore(st,a.block)!,tw=ground(st).tw;
  const existing=d.warning?.assault===a.id?d.warning:undefined;
  d.warning={assault:a.id,block:a.block,startsAt:a.startsAt,receivedAt:existing?.receivedAt??st.t,
    ...(d.radioUpgrade?{approach:headingWord([a.origin%tw-core.x,Math.floor(a.origin/tw)-core.y]),composition:'ordinary crawlers'}:{})};
}
export function radioUpgradeCheck(st:SimState):string {
  const d=st.campaign?.defence,r=st.campaign?.expansion?.radio;
  if(!d||!r||r.restoredAt<0)return 'restore the radio first';
  if(d.radioUpgrade)return 'radio already upgraded';
  if(st.engineer.down>=0||!inReach(st,r.x,r.y,r.size))return 'walk closer to the radio';
  if(!radioPowered(st))return 'radio needs power';
  const steel=CAMPAIGN_THREAT.radioUpgradeSteel,copper=CAMPAIGN_THREAT.radioUpgradeCopper;
  if((st.engineer.inv.steel??0)<steel||(st.engineer.inv.copper??0)<copper)return `upgrade needs ${steel} steel and ${copper} copper`;
  return '';
}
export function upgradeRadio(st:SimState):string {
  const why=radioUpgradeCheck(st);if(why)return why;
  const d=st.campaign!.defence!,steel=CAMPAIGN_THREAT.radioUpgradeSteel,copper=CAMPAIGN_THREAT.radioUpgradeCopper;
  drop(st.engineer,'steel',steel);drop(st.engineer,'copper',copper);
  st.stats.spentSteel=(st.stats.spentSteel??0)+steel;st.stats.spentCopper=(st.stats.spentCopper??0)+copper;
  d.radioUpgrade=true;receiveWarning(st);return '';
}
export function restorationWindow(st:SimState):string {
  const d=st.campaign?.defence;if(!d)return '';
  return d.major?'Today’s major target is locked; restoration can nominate a later assault.':`Restoration can nominate Night ${Math.floor(d.nextDawn/DAY)+1}; protected rest days remain.`;
}
/** Presentation may navigate only received intelligence, the guaranteed home target, raids or recovery. */
export function knownCampaignThreat(st:SimState):{block:number;phase:'warning'|'assault'|'withdrawal'|'minor raid'|'recovery';x:number;y:number}|null {
  const d=st.campaign?.defence;if(!d)return null;
  const a=d.major,w=d.warning;
  let block:number|undefined,phase:'warning'|'assault'|'withdrawal'|'minor raid'|'recovery';
  // A current local raid takes priority over a future dusk warning.
  if(d.minor){block=d.minor.block;phase=d.minor.retreat?'withdrawal':'minor raid';}
  else if(a){
    block=w?.assault===a.id?w.block:(st.campaign?.expansion?.radio.restoredAt??-1)<0?st.campaign!.homeBlock:undefined;
    phase=a.retreat?'withdrawal':st.t>=a.startsAt?'assault':'warning';
  }else {block=d.bases.find(b=>b.hp===0)?.block;phase='recovery';}
  const core=block===undefined?undefined:baseCore(st,block);
  return core?{block:core.block,phase,x:core.x+core.size/2,y:core.y+core.size/2}:null;
}
export function campaignWarning(st:SimState):string {
  const d=st.campaign?.defence;if(!d)return '';
  const a=d.major,w=d.warning;
  if(a) {
    const known=w?.assault===a.id,homeOnly=(st.campaign?.expansion?.radio.restoredAt??-1)<0;
    const target=known?blockName(st,w.block):homeOnly?'Home Court':'target unknown — radio offline';
    const timing=a.retreat?'attackers withdrawing':a.waiting==='minor'?'assault delayed until the minor raid withdraws':a.waiting==='approach'?'assault delayed — no clear staging ground on the locked approach; waiting to resume':st.t>=a.startsAt?'assault underway':`assault at dusk in ${Math.ceil((a.startsAt-st.t)/60)} min`;
    const homeApproach=['north','east','south','west'][ground(st).opening!.direction];
    return `${d.minor?`Minor raid at ${blockName(st,d.minor.block)}${d.minor.retreat?' · withdrawing':''}. `:''}${target}: ${timing}${homeOnly?` · ${homeApproach} entrance`:known&&w.approach?` · approach ${w.approach} · ${w.composition}`:''}${known&&!radioPowered(st)?' · last received warning (radio offline)':''}`;
  }
  if(d.minor)return `Minor raid at ${blockName(st,d.minor.block)}${d.minor.retreat?' · withdrawing':''}`;
  const disabled=d.bases.find(b=>b.hp===0);
  if(disabled)return `${blockName(st,disabled.block)} core disabled — E at its core repairs for 10 steel + 5 copper. Supplies and layout remain.`;
  return `Next major assault: Night ${Math.floor(d.nextDawn/DAY)+1}. Two minor raid opportunities per day; ruins remain dangerous.`;
}
/** One timestamp owner; subsecond ticks cannot repeat a dawn, opportunity or finite roster entry. */
export function tickCampaignSchedule(st:SimState,T:ThreatState):void {
  const d=st.campaign!.defence!;
  for(const site of d.sites)if(!site.spawned) {
    site.spawned=true;birth(st,T,site.block,site.tile,'site',site.id);
  }
  if(d.minor&&!T.crawlers.some(c=>c.campaign?.layer==='minor'&&c.campaign.group===d.minor!.id))d.minor=null;
  if(d.major&&!d.minor&&st.t>=d.major.startsAt&&d.major.remaining===0&&!T.crawlers.some(c=>c.campaign?.layer==='major'&&c.campaign.group===d.major!.id)) {
    const a=d.major;
    d.history.push({id:a.id,block:a.block,started:a.startsAt,ended:st.t,defeated:a.retreat,spawned:d.majorSpawned});
    if(d.history.length>32)d.history.shift();
    d.lastMajorEnd=st.t;
    const quietCycles=(st.campaign!.districts?.resuppliedAt??-1)>=0?1:2;
    d.nextDawn=Math.ceil((st.t+quietCycles*DAY-DUSK)/DAY)*DAY;
    d.major=null;d.majorSpawned=0;
  }
  if(!d.major&&st.t>=d.nextDawn) {
    const choices=eligible(st,true);
    const nominations=d.nominations.filter(n=>choices.some(b=>b.block===n.block)).sort((a,b)=>b.at-a.at||a.block-b.block);
    const base=nominations.length?choices.find(b=>b.block===nominations[0].block):choices[d.history.length%Math.max(1,choices.length)];
    const origin=base?campaignOrigin(st,base):-1;
    if(base&&origin>=0) {
      d.major={id:d.nextId++,block:base.block,dawn:st.t,startsAt:st.t+DUSK,origin,remaining:CAMPAIGN_THREAT.majorCount,nextSpawn:st.t+DUSK,retreat:false};
      d.nominations=[];
    }else {d.nextDawn=Math.ceil((st.t+1)/DAY)*DAY;d.notice='Assault deferred: no operational base with a reachable approach.';}
  }
  receiveWarning(st);
  const a=d.major;
  if(a&&st.t>=a.startsAt) {
    if(d.minor){d.minor.retreat=true;if(!a.retreat)a.waiting='minor';}
    else if(a.waiting==='minor')delete a.waiting;
    // A departing small raid must finish before the major roster appears; no overlapping base targets.
    if(!d.minor&&!a.retreat&&a.remaining>0&&st.t>=a.nextSpawn) {
      const core=baseCore(st,a.block)!;
      if(core.hp===0){a.retreat=true;a.remaining=0;delete a.waiting;}
      else {
        const staging=campaignStaging(st,core,a.origin);
        if(staging>=0) {
          birth(st,T,a.block,staging,'major',a.id);a.remaining--;d.majorSpawned++;a.nextSpawn=st.t+CAMPAIGN_THREAT.spawnEvery;
          delete a.waiting;
        }else {a.waiting='approach';a.nextSpawn=st.t+1;}
      }
    }
  }
  const day=Math.floor(st.t/DAY),elapsed=st.t%DAY;
  for(let k=0;k<2;k++)if(elapsed>=[300,600][k]&&day*2+k>d.lastMinorSlot) {
    d.lastMinorSlot=day*2+k;
    if(d.minor||(a&&st.t>=a.startsAt)||st.t-d.lastMajorEnd<CAMPAIGN_THREAT.minorAfterMajor&&d.lastMajorEnd>=0)continue;
    const choices=eligible(st,false),base=choices[(day*2+k)%Math.max(1,choices.length)],origin=base?campaignOrigin(st,base):-1;
    if(!base||origin<0)continue;
    const id=d.nextId++;d.minor={id,block:base.block,origin,retreat:false};d.raidsStarted++;
    const count=8+(st.seed+day*2+k)%5;
    for(let n=0;n<count;n++)birth(st,T,base.block,origin,'minor',id);
  }
}
function fieldStep(st:SimState,c:Crawler,fld:Field,ignoreWaypoint=false):number {
  const G=ground(st),tw=G.tw,t=Math.floor(c.y)*tw+Math.floor(c.x);
  let next=ignoreWaypoint?-1:c.campaign!.waypoint??-1;
  if(next<0) {
    let best=fld.dist[t]<0?Infinity:fld.dist[t];
    for(let k=0;k<4;k++){const xx=Math.floor(c.x)+DX[k],yy=Math.floor(c.y)+DY[k],q=yy*tw+xx;if(!inGround(G,xx,yy)||fld.dist[q]<0||fld.dist[q]>=best)continue;best=fld.dist[q];next=q;}
  }
  return next;
}
export interface CampaignAction {
  action:'attack'|'breach'|'pursue'|'advance'|'withdraw'|'guard'|'blocked';
  tx:number;ty:number;what:'you'|'wall'|'barricade'|'turret'|'base core'|'exit'|'ruin';
  destination:'base core'|'exit'|'ruin';
}
/** Read-only immediate decision, also consumed by the damage/movement tick. No saved UI state. */
export function campaignCrawlerAction(st:SimState,c:Crawler):CampaignAction {
  const meta=c.campaign!,G=ground(st),e=st.engineer,d=st.campaign!.defence!;
  const origin={tx:meta.origin%G.tw,ty:Math.floor(meta.origin/G.tw)};
  const player={tx:Math.floor(e.x),ty:Math.floor(e.y),what:'you' as const};
  if(meta.layer==='site') {
    const near=e.down<0&&Math.hypot(e.x-origin.tx-.5,e.y-origin.ty-.5)<=CAMPAIGN_THREAT.guardLeash&&Math.hypot(e.x-c.x,e.y-c.y)<=CAMPAIGN_THREAT.guardNotice;
    return near?{...player,action:Math.hypot(e.x-c.x,e.y-c.y)<=1.2?'attack':'pursue',destination:'ruin'}:{...origin,what:'ruin',action:'guard',destination:'ruin'};
  }
  const group=meta.layer==='major'?d.major:d.minor,core=baseCore(st,c.to);
  const retreat=!group||group.retreat||!core||core.hp===0;
  const destination=retreat?'exit':'base core';
  if(!retreat) {
    if(e.down<0&&e.dash<=0&&Math.hypot(e.x-c.x,e.y-c.y)<=1.2)return {...player,action:'attack',destination};
    if(c.onPlayer&&e.down<0&&Math.hypot(e.x-core.x,e.y-core.y)<25&&Math.hypot(e.x-c.x,e.y-c.y)<8)return {...player,action:'pursue',destination};
    const nearby=st.flow!.machines.find(m=>m.kind==='turret'&&defenceHp(m)>0&&Math.hypot(c.x-m.x-m.size/2,c.y-m.y-m.size/2)<2);
    if(nearby)return {tx:nearby.x,ty:nearby.y,what:'turret',action:'attack',destination};
  }
  const x=retreat?origin.tx:core.x,y=retreat?origin.ty:core.y,size=retreat?0:core.size;
  const fld=route(st,c,x,y,size),t=Math.floor(c.y)*G.tw+Math.floor(c.x),reset=retreat&&!meta.withdrawing;
  if(fld.dist[t]===0&&(reset||meta.waypoint===undefined))return {tx:x,ty:y,what:destination,action:retreat?'withdraw':'attack',destination};
  const next=fieldStep(st,c,fld,reset),tx=next%G.tw,ty=Math.floor(next/G.tw),m=next<0?undefined:machineAt(st,tx,ty);
  if(m&&defenceMax(m)>0&&defenceHp(m)>0)return {tx:m.x,ty:m.y,what:m.kind as 'wall'|'barricade'|'turret',action:'breach',destination};
  return {tx:x,ty:y,what:destination,action:next<0||!hostileOpen(st,tx,ty)?'blocked':retreat?'withdraw':'advance',destination};
}
function walkField(st:SimState,c:Crawler,fld:Field,dt:number):boolean {
  const G=ground(st),tw=G.tw,t=Math.floor(c.y)*tw+Math.floor(c.x),meta=c.campaign!;
  if(fld.dist[t]===0&&meta.waypoint===undefined)return true;
  const next=fieldStep(st,c,fld);
  if(next<0){c.stuck+=dt;return false;}
  const x=next%tw,y=Math.floor(next/tw),m=machineAt(st,x,y);
  if(m&&defenceMax(m)>0&&defenceHp(m)>0){damageDefence(st,m,CAMPAIGN_THREAT.structureDps*dt);delete meta.waypoint;return false;}
  if(!hostileOpen(st,x,y)){delete meta.waypoint;c.stuck+=dt;return false;}
  meta.waypoint=next;moveTo(c,x+.5,y+.5,CAMPAIGN_THREAT.speed*dt);c.stuck=0;
  if(Math.hypot(c.x-x-.5,c.y-y-.5)<1e-8)delete meta.waypoint;
  return false;
}
/** Called before the shared turret/rifle tick. Site bodies never become base attackers. */
export function tickCampaignThreat(st:SimState,T:ThreatState,dt:number):void {
  initDefence(st);tickRepair(st,dt);tickCampaignSchedule(st,T);
  const G=ground(st),e=st.engineer;
  for(const c of [...T.crawlers]) {
    const meta=c.campaign;if(!meta)continue;
    const action=campaignCrawlerAction(st,c);
    if(meta.layer==='site') {
      c.onPlayer=action.what==='you';
      if(action.action==='attack'){if(e.dash<=0)hurt(st,CAMPAIGN_THREAT.contactDps*dt);T.dangerS+=dt;}
      else stepToward(st,G,c,c.onPlayer?e.x:action.tx+.5,c.onPlayer?e.y:action.ty+.5,CAMPAIGN_THREAT.speed*dt);
      continue;
    }
    if(action.destination==='exit') {
      if(!meta.withdrawing){delete meta.waypoint;meta.withdrawing=true;}
      c.onPlayer=false;const x=meta.origin%G.tw,y=Math.floor(meta.origin/G.tw);
      if(walkField(st,c,route(st,c,x,y,0),dt))T.crawlers.splice(T.crawlers.indexOf(c),1);
      continue;
    }
    if(action.what==='you') {
      if(action.action==='attack'){hurt(st,CAMPAIGN_THREAT.contactDps*dt);T.dangerS+=dt;}
      else {delete meta.waypoint;stepToward(st,G,c,e.x,e.y,CAMPAIGN_THREAT.speed*dt);}
      continue;
    }
    c.onPlayer=false;
    if(action.action==='attack'&&action.what==='turret'){damageDefence(st,machineAt(st,action.tx,action.ty)!,CAMPAIGN_THREAT.structureDps*dt);continue;}
    const core=baseCore(st,c.to)!;
    if(walkField(st,c,route(st,c,core.x,core.y,core.size),dt))damageCore(st,core,CAMPAIGN_THREAT.structureDps*dt);
  }
}
export function describeCampaignCrawler(st:SimState,c:Crawler):string {
  const meta=c.campaign!,a=campaignCrawlerAction(st,c);
  const verb={attack:'attacking',breach:'breaching',pursue:'pursuing',advance:'advancing toward',withdraw:'withdrawing toward',guard:'guarding',blocked:'route blocked toward'}[a.action];
  return `Crawler · ${Math.ceil(c.hp)}/12 HP · ${meta.layer==='site'?'ruin guardian (leashed)':`${meta.layer==='major'?'major assault':'minor raid'} at ${blockName(st,c.to)}`} · now ${verb} ${a.what} · destination: ${a.destination}${c.stuck>1?' · approach obstructed':''}`;
}
