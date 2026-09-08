/** Optional found infrastructure: real deliveries and fuel-free local generation. */
import { type SimState, HELD, DARK } from './types';
import { ground, inReach } from './ground';
import { passable } from './walk';
import { drop } from './engineer';
import { isCampaign } from './rules';
import { campaignGrid, campaignThrottle } from './campaignPower';

export const TURBINE = { size: 4, kw: 600, clueRadius: 24, cost: { steel: 60, copper: 30, concrete: 40 } } as const;
export interface TurbineSite {
  id: string; block: number; x: number; y: number; size: number;
  seenAt: number; restoredAt: number; enabled: boolean;
  delivered: { steel: number; copper: number; concrete: number };
}
const materials = ['steel','copper','concrete'] as const;
export function initTurbine(st:SimState):void {
  const c=st.campaign,f=st.flow;if(!isCampaign(st)||!c||!f)return;
  if(c.turbine){c.version=8;return;}
  const G=ground(st),n=G.base.length;
  // Static terrain, with existing construction respected on migration. Never clear a machine or a route.
  const safe=(t:number):boolean=>passable(st,t%G.tw,Math.floor(t/G.tw))
    && Math.hypot(t%G.tw-c.discovery!.x,Math.floor(t/G.tw)-c.discovery!.y)>12
    && c.defence!.sites.every(s=>Math.hypot(t%G.tw-s.tile%G.tw,Math.floor(t/G.tw)-Math.floor(s.tile/G.tw))>12);
  const river=new Int16Array(n);river.fill(-1);const water:number[]=[];
  for(let t=0;t<n;t++)if(G.owner[t]===-2){river[t]=0;water.push(t);}
  for(let h=0;h<water.length;h++){
    const t=water[h],x=t%G.tw,y=Math.floor(t/G.tw);if(river[t]>=16)continue;
    for(const [xx,yy] of [[x,y-1],[x+1,y],[x,y+1],[x-1,y]]){const q=yy*G.tw+xx;if(xx<0||yy<0||xx>=G.tw||yy>=G.th||river[q]>=0)continue;river[q]=river[t]+1;water.push(q);}
  }
  const installations=[c.expansion!.station,c.expansion!.radio,c.districts!.station,c.districts!.workshop,...c.districts!.sources];
  const reserved=new Set([...c.expansion!.route,...c.districts!.route]);
  const clear=(x:number,y:number,bi:number):boolean=>{
    // A clear ring leaves all nearby walking routes open after the hall becomes solid.
    for(let yy=y-1;yy<=y+TURBINE.size;yy++)for(let xx=x-1;xx<=x+TURBINE.size;xx++){
      const t=yy*G.tw+xx;
      if(xx<0||yy<0||xx>=G.tw||yy>=G.th||G.owner[t]!==bi||!safe(t)||G.rank[t]>=0||G.patch[t]||reserved.has(t)||f.occ[t]!==undefined)return false;
      const sub=G.blocks[bi].sub;if(sub&&xx>=sub.x&&xx<sub.x+sub.size&&yy>=sub.y&&yy<sub.y+sub.size)return false;
    }
    return installations.every(s=>Math.hypot(x-s.x,y-s.y)>s.size+TURBINE.size+4)
      && c.recruits!.sites.every(s=>Math.hypot(x-s.x,y-s.y)>TURBINE.size+4)
      && [...c.expansion!.stops,c.districts!.stop].every(([sx,sy])=>Math.hypot(x-sx,y-sy)>TURBINE.size+5);
  };
  const gate=G.opening!.gate,start=gate[Math.floor(gate.length/2)],queue=[start],seen=new Uint8Array(n);seen[start]=1;
  for(let h=0;h<queue.length;h++){
    const t=queue[h],x=t%G.tw,y=Math.floor(t/G.tw),bi=G.owner[t];
    if(bi>=0&&bi!==c.homeBlock&&[HELD,DARK].includes(st.blocks[bi].state)&&river[t]>0&&river[t]<=16&&clear(x,y,bi)){
      c.turbine={id:`${st.seed}:turbine:v1`,block:bi,x,y,size:TURBINE.size,seenAt:-1,restoredAt:-1,enabled:false,delivered:{steel:0,copper:0,concrete:0}};
      c.version=8;f.rev++;return;
    }
    for(const [xx,yy] of [[x,y-1],[x+1,y],[x,y+1],[x-1,y]]){const q=yy*G.tw+xx;if(xx<0||yy<0||xx>=G.tw||yy>=G.th||seen[q]||!safe(q))continue;seen[q]=1;queue.push(q);}
  }
  throw new Error('campaign requires an accessible riverside Turbine hall');
}
export function turbineAt(st:SimState,x:number,y:number):TurbineSite|undefined {
  const s=st.campaign?.turbine;return s&&x>=s.x&&x<s.x+s.size&&y>=s.y&&y<s.y+s.size?s:undefined;
}
export function tickTurbine(st:SimState):void {
  const s=st.campaign?.turbine;if(s&&s.seenAt<0&&Math.hypot(st.engineer.x-s.x-s.size/2,st.engineer.y-s.y-s.size/2)<=TURBINE.clueRadius)s.seenAt=st.t;
}
export function turbineReachProblem(st:SimState):string {
  const s=st.campaign?.turbine;
  return !s?'No Turbine hall.':st.engineer.down>=0||st.engineer.truckSeat||!inReach(st,s.x,s.y,s.size)?'Walk closer to the Turbine hall.':'';
}
export function turbineCheck(st:SimState):string {
  const why=turbineReachProblem(st);if(why)return why;const s=st.campaign!.turbine!;
  if(s.restoredAt>=0)return 'Turbine hall already restored.';
  if(materials.some(k=>s.delivered[k]<TURBINE.cost[k]))return 'Deliver the missing materials.';
  if(campaignThrottle(st,s.block)<=0)return 'Commissioning needs a fueled local generator or a connected powered circuit.';
  return '';
}
export function deliverTurbine(st:SimState):void {
  if(turbineReachProblem(st))return;const s=st.campaign!.turbine!;if(s.restoredAt>=0)return;
  s.seenAt=s.seenAt<0?st.t:s.seenAt;
  for(const k of materials)s.delivered[k]+=drop(st.engineer,k,TURBINE.cost[k]-s.delivered[k]);
}
export function restoreTurbine(st:SimState):string {
  const why=turbineCheck(st);if(why)return why;const s=st.campaign!.turbine!;
  st.stats.spentSteel=(st.stats.spentSteel??0)+s.delivered.steel;st.stats.spentCopper=(st.stats.spentCopper??0)+s.delivered.copper;
  st.flow!.stats.placed.concrete=(st.flow!.stats.placed.concrete??0)+s.delivered.concrete;
  s.delivered={steel:0,copper:0,concrete:0};s.seenAt=s.seenAt<0?st.t:s.seenAt;s.restoredAt=st.t;s.enabled=true;st.flow!.rev++;
  return '';
}
export function setTurbineEnabled(st:SimState,enabled:boolean):void {
  if(typeof enabled!=='boolean'||turbineReachProblem(st))return;const s=st.campaign!.turbine!;
  if(s.restoredAt>=0&&s.enabled!==enabled){s.enabled=enabled;st.flow!.rev++;}
}
export function describeTurbine(st:SimState):string {
  const s=st.campaign?.turbine;if(!s)return '';
  if(s.restoredAt<0)return `Turbine hall · ${materials.map(k=>`${s.delivered[k]}/${TURBINE.cost[k]} ${k}`).join(', ')} delivered. E delivers and commissions with local power. ${turbineCheck(st)}`;
  const grid=campaignGrid(st),c=grid.blocks[s.block];
  return `Turbine hall · ${!s.enabled?'switched off':c.turbineSupply===0?'core disabled':grid.turbineOutput>0?'generating':'standby (no demand)'} · ${Math.round(grid.turbineOutput)}/${TURBINE.kw} kW fuel-free. Circuit ${Math.round(c.load)}/${Math.round(c.demand)} kW served. Link substations with poles to supply other districts. E switches ${s.enabled?'off':'on'}.`;
}
export function turbineProblem(st:SimState):string {
  for(const m of st.flow?.machines??[])if(m.kind==='arclamp'){
    if(!isCampaign(st)||(st.campaign?.version??0)<8||m.size!==1||!Number.isInteger(m.x)||!Number.isInteger(m.y)||m.x<0||m.y<0||m.x>=st.flow!.tw||m.y>=st.city!.th||!m.inv||Object.keys(m.inv).length||!Array.isArray(m.items)||m.items.length||m.hold!==null||m.recipe!==undefined||m.busy||m.out!==0||m.timer!==0)return 'invalid Arc lamp';
  }
  const s=st.campaign?.turbine;
  if((st.campaign?.version??0)<8)return s?'Turbine requires campaign metadata version 8':'';
  if(!s||s.id!==`${st.seed}:turbine:v1`||s.size!==TURBINE.size||!Number.isInteger(s.block)||!st.blocks[s.block]
    ||!Number.isInteger(s.x)||!Number.isInteger(s.y)||s.x<0||s.y<0||s.x+s.size>st.flow!.tw||s.y+s.size>st.city!.th
    ||ground(st).owner[s.y*st.flow!.tw+s.x]!==s.block||typeof s.enabled!=='boolean'||!s.delivered
    ||!materials.every(k=>Number.isSafeInteger(s.delivered[k])&&s.delivered[k]>=0&&s.delivered[k]<=TURBINE.cost[k])
    ||Object.keys(s.delivered).length!==3||![s.seenAt,s.restoredAt].every(t=>Number.isFinite(t)&&(t===-1||(t>=0&&t<=st.t)))
    ||(s.restoredAt<0&&s.enabled)||(s.restoredAt>=0&&(s.seenAt<0||s.seenAt>s.restoredAt||materials.some(k=>s.delivered[k]!==0))))return 'invalid Turbine record';
  return '';
}
