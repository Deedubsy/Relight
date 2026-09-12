import { SurveyPlacementError, surveyCandidate, acceptSurvey, type SurveyAcceptance } from './campaignSurvey';
/** EX-06 district economy. Geometry, extraction and service defaults are provisional. */
import { DARK, HELD, type SimState } from './types';
import type { CampaignSite } from './rules';
import { ground, blockOfTile } from './ground';
import { type Machine, type Item, tramRoute, routeStops } from './flow';
import { campaignThrottle } from './campaignPower';
import { describeSite } from './expansion';
import { baseCore, defenceHp, defenceMax, DEFENCE } from './campaignDefence';

export const DISTRICT_ECONOMY = { workshopKw: 40, repairSeconds: 2, repairRadius: 14, supplyRadius: 4,
  resupplySteel: 10, resupplyCopper: 5, resupplyMagazines: 5, resupplyVisits: 2 } as const;
export interface DistrictState {
  version: 1; station: CampaignSite; workshop: CampaignSite; route: number[]; stop: [number, number];
  sources: { block: number; x: number; y: number; size: number; item: 'steel' | 'copper' | 'coal' }[];
  repair: { target: number; progress: number } | null; repairs: number;
  supplied: { steel: number; copper: number; magazine: number }; visits: number; lastVisit: number; resuppliedAt: number;
}

/** Extend the existing trunk, never invent a second disconnected route or a junction. */
export function initDistricts(st: SimState, accept?: SurveyAcceptance): void {
  const c = st.campaign, e = c?.expansion;
  if (!c || !e || c.districts || !st.flow) return;
  const G = ground(st), tw = G.tw, n = G.base.length, old = new Set(e.route), start = e.route.at(-1)!;
  const neighbours = (t: number) => [t-tw, t%tw<tw-1?t+1:-1, t+tw, t%tw>0?t-1:-1].filter(q=>q>=0&&q<n);
  const clear = (x: number, y: number, size: number, block: number) => {
    for(let yy=y;yy<y+size;yy++)for(let xx=x;xx<x+size;xx++) {
      const t=yy*tw+xx;
      if(xx<0||yy<0||xx>=tw||yy>=G.th||blockOfTile(st,xx,yy)!==block||G.urban!.solid[t]||G.owner[t]===-2||G.patch[t]||st.flow!.occ[t]!==undefined)return false;
      if([e.station,e.radio].some(s=>xx>=s.x&&xx<s.x+s.size&&yy>=s.y&&yy<s.y+s.size))return false;
      const sub=G.blocks[block].sub;if(sub&&xx>=sub.x&&xx<sub.x+sub.size&&yy>=sub.y&&yy<sub.y+sub.size)return false;
    }return true;
  };
  const prev=new Int32Array(n);prev.fill(-1);prev[start]=start;const queue=[start];
  const compose=(end:number,stop:[number,number],path:Set<number>):boolean=>{
    const route=[end];while(route.at(-1)!==start)route.push(prev[route.at(-1)!]);route.reverse();
    const bi=G.near[end],sub=G.blocks[bi].sub!;
    const site=(x:number,y:number,size:number,block=bi):CampaignSite=>({block,x,y,size,delivered:{steel:0,copper:0},restoredAt:-1});
    const occupied=new Set([...old,...path]);
    for(const [x,y]of [...e.stops,stop])for(let yy=y;yy<y+2;yy++)for(let xx=x;xx<x+2;xx++)occupied.add(yy*tw+xx);
    const pad=(block:number,size:number):[number,number]|undefined=>{
      const p=G.urban!.places.find(p=>p.block===block)!.pad,b=G.blocks[block];
      // The preferred urban pad can be narrower than a source. Search the rest of this
      // same district before rejecting it; every tile still passes the full footprint rules.
      for(const bounds of [p,{x:b.x0,y:b.y0,w:b.x1-b.x0+1,h:b.y1-b.y0+1}])
      for(let y=bounds.y;y<=bounds.y+bounds.h-size;y++)for(let x=bounds.x;x<=bounds.x+bounds.w-size;x++) {
        if(!clear(x,y,size,block))continue;
        const tiles=Array.from({length:size*size},(_,i)=>(y+Math.floor(i/size))*tw+x+i%size);
        if(tiles.some(t=>occupied.has(t)||G.owner[t]!==block))continue;
        for(const t of tiles)occupied.add(t);return[x,y];
      }
    };
    const workshop=pad(bi,1),steel=pad(e.station.block,5),copper=pad(bi,5);
    if(!workshop||!steel||!copper)return false;
    // A nearby district is eligible only when its complete source footprint fits.
    const coalPlaces=G.urban!.places.filter(p=>p.block!==bi&&p.block!==e.station.block&&p.block!==c.homeBlock)
      .sort((a,b)=>Math.hypot(a.pad.x-sub.x,a.pad.y-sub.y)-Math.hypot(b.pad.x-sub.x,b.pad.y-sub.y)||a.block-b.block);
    for(const p of coalPlaces) {
      const coal=pad(p.block,5);if(!coal)continue;
      const candidate=surveyCandidate(st);
      candidate.campaign!.districts={version:1,station:site(sub.x,sub.y,sub.size),workshop:site(...workshop,1),route,stop,
        sources:[{block:e.station.block,x:steel[0],y:steel[1],size:5,item:'steel'},
          {block:bi,x:copper[0],y:copper[1],size:5,item:'copper'},
          {block:p.block,x:coal[0],y:coal[1],size:5,item:'coal'}],
        repair:null,repairs:0,supplied:{steel:0,copper:0,magazine:0},visits:0,lastVisit:-1,resuppliedAt:-1};
      candidate.campaign!.version=4;
      if(acceptSurvey(st,candidate,accept))return true;
      for(let y=coal[1];y<coal[1]+5;y++)for(let x=coal[0];x<coal[0]+5;x++)occupied.delete(y*tw+x);
      // Prefer another trunk endpoint after reserving the nearest usable coal district.
      break;
    }return false;
  };
  for(let head=0;head<queue.length;head++) {
    const t=queue[head],x=t%tw,y=Math.floor(t/tw),bi=G.near[t];
    if(bi>=0&&bi!==c.homeBlock&&bi!==e.station.block&&G.blocks[bi].sub&&head>8) {
      const path=new Set<number>();let q=t;while(q!==start){path.add(q);q=prev[q];}path.add(start);
      for(const [xx,yy]of [[x-2,y],[x+1,y],[x,y-2],[x,y+1],[x-2,y-1],[x+1,y-1],[x-1,y-2],[x-1,y+1]]) {
        if(clear(xx,yy,2,bi)&&![yy*tw+xx,yy*tw+xx+1,(yy+1)*tw+xx,(yy+1)*tw+xx+1].some(q=>path.has(q)||old.has(q)||G.rank[q]>=0)
          &&compose(t,[xx,yy],path))return;
      }
    }
    for(const q of neighbours(t)) {
      if(prev[q]!==-1||G.owner[q]!==-1||G.urban!.solid[q]||st.flow!.occ[q]!==undefined||old.has(q))continue;
      // Survey only streets accepted by campaign construction; an INERT neighbour is not buildable.
      const state=st.blocks[G.near[q]]?.state;if(state!==DARK&&state!==HELD)continue;
      if(neighbours(q).some(v=>old.has(v)&&v!==start))continue;
      if(e.stops.some(([x,y])=>q%tw>=x&&q%tw<x+2&&Math.floor(q/tw)>=y&&Math.floor(q/tw)<y+2))continue;
      prev[q]=t;queue.push(q);
    }
  }
  throw new SurveyPlacementError('district expansion requires complete reachable reservations');
}
export function persistentSource(st:SimState,x:number,y:number) {
  return st.campaign?.districts?.sources.find(s=>x>=s.x&&x<s.x+s.size&&y>=s.y&&y<s.y+s.size);
}
/** Actual reserved delivery only: manual transfers and returned cargo cannot satisfy this milestone. */
export function recordDistrictDelivery(st:SimState,origin:Machine,destination:Machine,item:Item,n:number):void {
  const c=st.campaign,d=c?.districts;
  if(!d||n<=0||d.station.restoredAt<0||blockOfTile(st,origin.x,origin.y)!==c!.homeBlock||blockOfTile(st,destination.x,destination.y)!==d.station.block)return;
  if(item!=='steel'&&item!=='copper'&&item!=='magazine')return;
  d.supplied[item]+=n;
  if(item==='magazine'&&d.lastVisit!==st.flow!.tick){d.lastVisit=st.flow!.tick;d.visits++;}
}
function repairTarget(st:SimState):Machine|undefined {
  const d=st.campaign?.districts;if(!d)return;
  const w=d.workshop,manual=st.campaign?.defence?.repair;
  return st.flow!.machines.filter(m=>defenceMax(m)>0&&defenceHp(m)<defenceMax(m)&&blockOfTile(st,m.x,m.y)===w.block
    &&Math.hypot(m.x+m.size/2-w.x-.5,m.y+m.size/2-w.y-.5)<=DISTRICT_ECONOMY.repairRadius
    &&!(manual?.kind==='machine'&&manual.id===m.id)).sort((a,b)=>a.id-b.id)[0];
}
function supply(st:SimState):Record<string,number>|undefined {
  const w=st.campaign!.districts!.workshop;
  for(const m of st.flow!.machines) {
    if(m.kind!=='chest'&&m.kind!=='tramstop'||blockOfTile(st,m.x,m.y)!==w.block||Math.hypot(m.x+m.size/2-w.x-.5,m.y+m.size/2-w.y-.5)>DISTRICT_ECONOMY.supplyRadius)continue;
    const inv=m.kind==='tramstop'?m.cargo:m.inv;
    if(inv&&(inv.steel??0)>=DEFENCE.repairSteel&&(inv.copper??0)>=DEFENCE.repairCopper)return inv;
  }
}
export function workshopStatus(st:SimState):string {
  const d=st.campaign?.districts,w=d?.workshop;if(!w||w.restoredAt<0)return 'Restore the workshop to automate nearby wall, Barricade and turret repairs.';
  const defence=st.campaign!.defence!;
  if(baseCore(st,w.block)?.hp===0)return 'Workshop offline: repair the disabled base core.';
  if((defence.major&&st.t>=defence.major.startsAt)||defence.minor?.block===w.block)return 'Workshop paused while attackers are active.';
  if(campaignThrottle(st,w.block)<=0)return 'Workshop needs power.';
  if(!repairTarget(st))return 'Workshop ready: nearby defences are repaired (manual repairs have priority).';
  if(!supply(st))return 'Workshop needs 2 steel + 1 copper in a supply chest or arrivals buffer within 4 tiles.';
  return `Workshop repairing: 40 HP every 2 powered seconds. Repairs completed: ${d!.repairs}.`;
}
export function tickDistricts(st:SimState,dt:number):void {
  const c=st.campaign,d=c?.districts;if(!d)return;
  const defence=c!.defence!,w=d.workshop;
  if(d.resuppliedAt<0&&d.visits>=DISTRICT_ECONOMY.resupplyVisits&&d.supplied.steel>=DISTRICT_ECONOMY.resupplySteel&&d.supplied.copper>=DISTRICT_ECONOMY.resupplyCopper&&d.supplied.magazine>=DISTRICT_ECONOMY.resupplyMagazines*(st.flow?.ammoVersion===1?10:1)) {
    const blocks=[c!.homeBlock,c!.expansion!.station.block,d.station.block];
    if(blocks.every(b=>baseCore(st,b)&&baseCore(st,b)!.hp>0)&&st.flow!.machines.some(m=>m.kind==='tram'&&blocks.every(b=>routeStops(st,tramRoute(st,m)).some(s=>blockOfTile(st,s.x,s.y)===b&&campaignThrottle(st,b)>0))))d.resuppliedAt=st.t;
    // Existing promises and target locks remain unchanged; the next actual completion uses one quiet cycle.
  }
  if(w.restoredAt<0||baseCore(st,w.block)?.hp===0||(defence.major&&st.t>=defence.major.startsAt)||defence.minor?.block===w.block)return;
  const power=campaignThrottle(st,w.block);if(power<=0)return;
  const target=repairTarget(st),inv=supply(st);if(!target){d.repair=null;return;}if(!inv)return;
  if(d.repair?.target!==target.id)d.repair={target:target.id,progress:0};
  d.repair.progress+=dt*power;
  if(d.repair.progress+1e-8<DISTRICT_ECONOMY.repairSeconds)return;
  inv.steel-=DEFENCE.repairSteel;inv.copper-=DEFENCE.repairCopper;
  st.stats.spentSteel=(st.stats.spentSteel??0)+DEFENCE.repairSteel;st.stats.spentCopper=(st.stats.spentCopper??0)+DEFENCE.repairCopper;
  target.hp=Math.min(defenceMax(target),defenceHp(target)+DEFENCE.repairHp);st.flow!.rev++;d.repairs++;d.repair=null;
}
export function districtGuidance(st:SimState):string {
  const d=st.campaign?.districts;if(!d)return '';
  return `${d.workshop.restoredAt<0?describeSite(st,'workshop'):workshopStatus(st)} Later station resupply from home: ${d.supplied.steel}/10 steel, ${d.supplied.copper}/5 copper, ${d.supplied.magazine}/${st.flow?.ammoVersion===1?50:5} ammunition items; ${d.visits}/2 ammunition deliveries. ${d.resuppliedAt>=0?'One quiet cycle applies after the next major completion.':'Two quiet cycles remain until three operational bases are connected and resupplied.'}`;
}
