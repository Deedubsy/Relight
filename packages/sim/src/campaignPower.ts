import {RIVERFRONT} from './city/riverfront';
import {CORRECTIONS} from './progression';
import {TURBINE} from './campaignTurbine';
import {SimState,HELD} from './types';
import {ground,blockOfTile,distToRect} from './ground';
import {MACHINE_KW,Machine,FlowState} from './flow';
import {DISTRICT_ECONOMY} from './campaignDistricts';
import {GENERATOR_KW,POLE_REACH,BIG_POLE_REACH} from './recipes';
import {defenceHp,defenceMax} from './campaignDefence';
export const CAMPAIGN_POWER={coreKw:100,radioKw:20} as const;
export interface Circuit {id:number;name:string;rated:number;turbineSupply:number;plantSupply:number;supply:number;demand:number;load:number;throttle:number;generators:number[]}
export interface CampaignGrid {turbineOutput:number;plantOutput:number;blocks:Circuit[];poles:Map<number,Circuit>;machines:Map<number,Circuit>;links:{x0:number;y0:number;x1:number;y1:number}[];generation:Map<number,number>;supply:number;demand:number;load:number}
const cache=new WeakMap<FlowState,{key:string;grid:CampaignGrid}>();
interface PowerNode {x:number;y:number;size:number;reach:number}
/** A reach node's centre lies within its reach of the other node's footprint (either direction). */
export function nodesLinked(a:PowerNode,b:PowerNode):boolean {
 const within=(p:PowerNode,q:PowerNode)=>p.reach>0&&distToRect(p.x+p.size/2,p.y+p.size/2,q.x,q.y,q.size,q.size)<=p.reach;
 return within(a,b)||within(b,a);
}
export function nodeReach(kind:string):number{return kind==='bigpole'?BIG_POLE_REACH:kind==='pole'||kind==='substation'?POLE_REACH:0;}
export function campaignGrid(st:SimState):CampaignGrid {
 const f=st.flow!,G=ground(st),disabled=new Set(st.campaign?.defence?.bases.filter(b=>b.hp===0).map(b=>b.block));
 const turbine=st.campaign?.turbine,plants=st.campaign?.progression?.sites.filter(s=>s.kind==='plant')??[];
 const key=`${f.rev}:${f.tick}:${turbine?.restoredAt}:${turbine?.enabled}:${plants.map(p=>`${p.id}:${p.installed}:${p.enabled}`).join(',')}:${[...disabled]}:${f.machines.filter(m=>m.kind==='generator').map(m=>`${m.id}:${(m.inv.coal??0)+(m.inv.fuel??0)>0}:${defenceHp(m)}`).join(',')}`;
 const old=cache.get(f);if(old?.key===key)return old.grid;
 type Node={x:number;y:number;size:number;reach:number;bi:number;machine?:Machine};
 const nodes:Node[]=[],subIndex=new Map<number,number>();
 for(const b of G.blocks){const placed=f.machines.find(m=>m.kind==='substation'&&blockOfTile(st,m.x,m.y)===b.i),s=placed??b.sub;if(s&&!disabled.has(b.i)){subIndex.set(b.i,nodes.length);nodes.push({...s,reach:POLE_REACH,bi:b.i,...(placed?{machine:placed}:{})});}}
 for(const m of f.machines)if(m.kind==='pole'||m.kind==='bigpole'||m.kind==='generator'||m.kind==='substation'&&!nodes.some(n=>n.machine?.id===m.id))nodes.push({...m,reach:m.kind==='generator'?0:m.kind==='bigpole'?BIG_POLE_REACH:POLE_REACH,bi:blockOfTile(st,m.x,m.y),machine:m});
 const parent=nodes.map((_,i)=>i),root=(i:number):number=>{while(parent[i]!==i){parent[i]=parent[parent[i]];i=parent[i];}return i;};
 const links:CampaignGrid['links']=[],link=(a:Node,b:Node)=>links.push({x0:a.x+a.size/2,y0:a.y+a.size/2,x1:b.x+b.size/2,y1:b.y+b.size/2});
 // GP-POWER-FIX (2026-09-11): a reach node links to another node when its centre is within reach of that node's
 // footprint — the same rule a machine uses — so a 2×2 Generator is not held to a shorter centre-to-centre distance.
 for(let i=0;i<nodes.length;i++)for(let j=0;j<i;j++){const a=nodes[i],b=nodes[j];if(nodesLinked(a,b)){parent[root(i)]=root(j);link(a,b);}}
 const groups=new Map<number,Circuit>(),perBlock=new Map<number,number>();
 const circuit=(id:number,bi:number)=>{let c=groups.get(id);if(!c){const n=(perBlock.get(bi)??0)+1;perBlock.set(bi,n);c={id,name:`${bi===st.campaign?.homeBlock?'Home':`District ${bi+1}`} network ${n}`,rated:0,turbineSupply:0,plantSupply:0,supply:0,demand:0,load:0,throttle:0,generators:[]};groups.set(id,c);}return c;};
 const nodeCircuit=(i:number)=>circuit(root(i),nodes[root(i)].bi);
 const blocks=st.blocks.map((_,bi)=>subIndex.has(bi)?nodeCircuit(subIndex.get(bi)!):circuit(nodes.length+bi,bi));
 const machines=new Map<number,Circuit>(),poles=new Map<number,Circuit>();
 for(let i=0;i<nodes.length;i++){const m=nodes[i].machine;if(m){const c=nodeCircuit(i);machines.set(m.id,c);if(m.kind!=='generator')poles.set(m.id,c);}}
 for(const m of f.machines){const bi=blockOfTile(st,m.x,m.y);let c=machines.get(m.id);
  if(!c){let nearest=-1,distance=Infinity;for(let i=0;i<nodes.length;i++){const p=nodes[i];if(!p.reach)continue;const d=distToRect(p.x+p.size/2,p.y+p.size/2,m.x,m.y,m.size,m.size);if(d<=p.reach&&d<distance){distance=d;nearest=i;}}
   if(nearest>=0){c=nodeCircuit(nearest);if(MACHINE_KW[m.kind]>0)link(nodes[nearest],{...m,reach:0,bi});machines.set(m.id,c);}
  }
  if(!c)continue;
  if(m.kind==='generator'){c.rated+=GENERATOR_KW;if(!disabled.has(bi)&&!(defenceMax(m)>0&&defenceHp(m)<=0)&&((m.inv.coal??0)+(m.inv.fuel??0))>0){c.supply+=GENERATOR_KW;c.generators.push(m.id);}}
  else if(!disabled.has(bi))c.demand+=MACHINE_KW[m.kind];
 }
 if(turbine&&turbine.restoredAt>=0){const c=blocks[turbine.block];c.rated+=TURBINE.kw;if(turbine.enabled&&!disabled.has(turbine.block)){c.supply+=TURBINE.kw;c.turbineSupply+=TURBINE.kw;}}
 for(const p of plants)if(p.installed){const c=blocks[p.block];c.rated+=CORRECTIONS.plantKw;if(p.enabled&&!disabled.has(p.block)){c.supply+=CORRECTIONS.plantKw;c.plantSupply+=CORRECTIONS.plantKw;}}
 for(let bi=0;bi<blocks.length;bi++)if(!disabled.has(bi)){if(st.city?.mapId?st.campaign?.defence?.bases.some(b=>b.block===bi):st.blocks[bi].state===HELD)blocks[bi].demand+=CAMPAIGN_POWER.coreKw;if(st.city?.mapId)blocks[bi].demand+=G.blocks[bi].lights.length*RIVERFRONT.lightKw;}
 for(const s of st.campaign?.progression?.sites??[])if(s.started&&s.restoredAt<0)blocks[s.block].demand+=CORRECTIONS.encounterKw;
 const radio=st.campaign?.expansion?.radio,workshop=st.campaign?.districts?.workshop;
 if(radio&&radio.restoredAt>=0&&!disabled.has(radio.block))blocks[radio.block].demand+=CAMPAIGN_POWER.radioKw;
 if(workshop&&workshop.restoredAt>=0&&!disabled.has(workshop.block))blocks[workshop.block].demand+=DISTRICT_ECONOMY.workshopKw;
 const grid:CampaignGrid={turbineOutput:0,plantOutput:0,blocks,poles,machines,links,generation:new Map(),supply:0,demand:0,load:0};
 for(const c of groups.values()){c.load=Math.min(c.supply,c.demand);c.throttle=c.supply>0?Math.min(1,c.supply/Math.max(1,c.demand)):0;grid.supply+=c.supply;grid.demand+=c.demand;grid.load+=c.load;const t=Math.min(c.load,c.turbineSupply),p=Math.min(c.load-t,c.plantSupply);grid.turbineOutput+=t;grid.plantOutput+=p;for(const id of c.generators)grid.generation.set(id,(c.load-t-p)/c.generators.length);}
 cache.set(f,{key,grid});return grid;
}
export function campaignThrottle(st:SimState,bi:number):number{return bi>=0&&st.flow?campaignGrid(st).blocks[bi]?.throttle??0:0;}
export function machineThrottle(st:SimState,m:Machine):number{return MACHINE_KW[m.kind]===0?1:campaignGrid(st).machines.get(m.id)?.throttle??0;}

export function powerConnectionAt(st:SimState,x:number,y:number,size:number):Circuit|undefined {
 const grid=campaignGrid(st);let best:Circuit|undefined,distance=Infinity;
 for(const b of ground(st).blocks){const p=st.flow!.machines.find(m=>m.kind==='substation'&&blockOfTile(st,m.x,m.y)===b.i)??b.sub;if(!p)continue;const d=distToRect(p.x+p.size/2,p.y+p.size/2,x,y,size,size);if(d<=POLE_REACH&&d<distance){distance=d;best=grid.blocks[b.i];}}
 for(const p of st.flow!.machines)if(p.kind==='pole'||p.kind==='bigpole'||p.kind==='substation'){const d=distToRect(p.x+p.size/2,p.y+p.size/2,x,y,size,size);if(d<distance&&d<=(p.kind==='bigpole'?BIG_POLE_REACH:POLE_REACH)){distance=d;best=grid.poles.get(p.id);}}
 return best;
}
/** GP-POWER-FIX (2026-09-11): the physical links a ghost would make if placed here — every Pole, Big pole, Generator
 *  and Substation a reach node would cable to, or the single nearest reach node a consuming machine would join.
 *  Read-only; the renderer draws these as preview lines and never implies a link the grid would not make. */
export interface PowerLinkPreview {kind:string;x:number;y:number;size:number;circuit:Circuit|undefined}
export function powerLinksAt(st:SimState,kind:string,x:number,y:number,size:number):PowerLinkPreview[] {
 if(!st.flow)return [];
 const grid=campaignGrid(st),ghost={x,y,size,reach:nodeReach(kind)},out:PowerLinkPreview[]=[];
 const nodes:(PowerNode&{kind:string;circuit:Circuit|undefined})[]=[];
 for(const b of ground(st).blocks){const placed=st.flow.machines.find(m=>m.kind==='substation'&&blockOfTile(st,m.x,m.y)===b.i);if(placed||!b.sub)continue;nodes.push({...b.sub,reach:POLE_REACH,kind:'substation',circuit:grid.blocks[b.i]});}
 for(const m of st.flow.machines)if(m.kind==='pole'||m.kind==='bigpole'||m.kind==='generator'||m.kind==='substation')nodes.push({x:m.x,y:m.y,size:m.size,reach:nodeReach(m.kind),kind:m.kind,circuit:grid.machines.get(m.id)});
 if(ghost.reach>0||kind==='generator'){for(const n of nodes)if(nodesLinked(ghost,n))out.push({kind:n.kind,x:n.x,y:n.y,size:n.size,circuit:n.circuit});return out;}
 let best:typeof nodes[number]|undefined,distance=Infinity;
 for(const n of nodes){if(!n.reach)continue;const d=distToRect(n.x+n.size/2,n.y+n.size/2,x,y,size,size);if(d<=n.reach&&d<distance){distance=d;best=n;}}
 return best?[{kind:best.kind,x:best.x,y:best.y,size:best.size,circuit:best.circuit}]:[];
}
