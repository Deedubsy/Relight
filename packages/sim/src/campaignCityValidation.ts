/** Finished campaign geometry, queried on a private copy. No unlocks, repairs or simulated travel. */
import { type SimState, configHash } from './types';
import { ground, distToRect, cityGeomOf } from './ground';
import { passable } from './walk';
import { placementGeometryProblem, rubbleAt, MACHINE_SIZE } from './flow';
import { campaignOrigin, campaignStaging } from './campaignThreat';
import { truckFits } from './truck';
import { truckWorkRoute } from './truckWork';
import { RECRUITS } from './campaignRecruits';

export const CITY_VALIDATOR_VERSION = 1;
export type CityFailureCode = 'input' | 'opening' | 'resource' | 'factory-space' | 'site-footprint' |
 'site-overlap' | 'site-access' | 'source-regions' | 'rail-route' | 'rail-placement' | 'urban-access' | 'threat-access';
export interface CityFailure { code: CityFailureCode; subject: string; reason: string }
interface Rect { x: number; y: number; w: number; h: number }
interface Site extends Rect { id: string; block: number }
export interface CampaignCityReport {
 version: number; seed: number; profile: string; configHash: string; metadata: number; surveyVersion: number;
 ok: boolean; failures: CityFailure[];
 measurements: {
  reachableTiles: number; routeTiles: number; generatorAttempt: number;
  sites: {id: string; approach: number | null; cardinalSteps: number | null}[];
  resources: {item: string; reachableTiles: number; units: number}[];
  factory: Rect | null; extraction: {item:string; footprint:Rect|null}[]; origins: {block: number; origin: number; staging: number}[];
  truck: {status: 'not-unlocked' | 'queried'; queries: {site: string; steps: number | null}[]};
 };
 scope: string;
}

export function validateCampaignCity(input: SimState): CampaignCityReport {
 const report: CampaignCityReport = {version:CITY_VALIDATOR_VERSION,seed:input?.seed??-1,
  profile:input?.city?.profile??'',configHash:input?.config?configHash(input.config):'',metadata:input?.campaign?.version??0,surveyVersion:input?.campaign?.expansion?.surveyVersion??1,
  ok:false,failures:[],measurements:{reachableTiles:0,routeTiles:0,generatorAttempt:-1,sites:[],resources:[],factory:null,extraction:[],origins:[],truck:{status:'not-unlocked',queries:[]}},
  scope:'Current physical snapshot. Cardinal connectivity is not travel time or safe combat passage. Placement geometry excludes unlocks, stock and power supply. Truck queries require an existing truck. No paid construction, survival or human fairness is established.'};
 const fail=(code:CityFailureCode,subject:string,reason:string)=>report.failures.push({code,subject,reason});
 try {
  if(input?.ruleset!=='exploration-v2'||input.campaign?.version!==10||!input.flow||!input.city||!Array.isArray(input.blocks))throw Error('requires initialized metadata-10 campaign');
  const st=structuredClone(input),c=st.campaign!,e=c.expansion!,d=c.districts!,f=st.flow!;
  if(!e||!d||!c.recruits||!c.turbine||!c.discovery||!c.defence)throw Error('missing composed campaign metadata');
  const tw=st.city!.tw,th=st.city!.th;
  if(!Number.isSafeInteger(tw)||!Number.isSafeInteger(th)||tw<1||th<1||tw*th>4_000_000||f.tw!==tw)throw Error('invalid map dimensions');
  const validRect=(r:Rect)=>[r.x,r.y,r.w,r.h].every(Number.isSafeInteger)&&r.x>=0&&r.y>=0&&r.w>0&&r.h>0&&r.x+r.w<=tw&&r.y+r.h<=th;
  const square=(id:string,s:{x:number;y:number;size?:number;block:number}):Site=>({id,x:s.x,y:s.y,w:s.size??1,h:s.size??1,block:s.block});
  const sites:Site[]=[square('station',e.station),square('radio',e.radio),square('northStation',d.station),square('workshop',d.workshop),square('turbine',c.turbine),square('discovery',c.discovery),
   ...c.recruits.sites.map(s=>square('recruit:'+s.kind,s)),...d.sources.map(s=>square('source:'+s.item,s))];
  if(c.recruits.sites.length!==Object.keys(RECRUITS).length||new Set(c.recruits.sites.map(s=>s.kind)).size!==Object.keys(RECRUITS).length||c.recruits.sites.some(s=>!(s.kind in RECRUITS)))fail('input','recruits','expected each current recruit exactly once');
  for(const s of sites)if(!validRect(s)||!Number.isSafeInteger(s.block)||!st.blocks[s.block])fail('site-footprint',s.id,'invalid rectangle or block');
  if(report.failures.some(v=>v.code==='site-footprint'))return report;
  const G=ground(st),urban=G.urban!,opening=G.opening!;
  if(!urban||!opening||!validRect({...opening.bounds,w:opening.bounds.size,h:opening.bounds.size}))throw Error('missing or invalid opening/urban geometry');
  report.measurements.generatorAttempt=cityGeomOf(st).attempt;
  const n=tw*th,validTile=(t:number)=>Number.isSafeInteger(t)&&t>=0&&t<n;
  const startX=Math.floor(st.engineer.x),startY=Math.floor(st.engineer.y);
  if(!Number.isFinite(st.engineer.reach)||st.engineer.reach<=0||st.engineer.reach>32||!passable(st,startX,startY))throw Error('engineer must stand on passable ground with valid reach');
  // Four-connected flood fill has the same connected components as movement's diagonals with no corner cutting.
  const distance=new Int32Array(n).fill(-1),queue=new Int32Array(n),start=startY*tw+startX;
  let head=0,tail=1;queue[0]=start;distance[start]=0;
  const neighbours=(t:number)=>[t%tw>0?t-1:-1,t%tw<tw-1?t+1:-1,t>=tw?t-tw:-1,t<n-tw?t+tw:-1].filter(v=>v>=0);
  while(head<tail){const t=queue[head++];for(const q of neighbours(t))if(distance[q]<0&&passable(st,q%tw,Math.floor(q/tw))){distance[q]=distance[t]+1;queue[tail++]=q;}}
  report.measurements.reachableTiles=tail;
  const approach=(r:Rect):number|null=>{
   let best:number|null=null;
   for(let y=Math.max(0,r.y-Math.ceil(st.engineer.reach));y<Math.min(th,r.y+r.h+Math.ceil(st.engineer.reach));y++)for(let x=Math.max(0,r.x-Math.ceil(st.engineer.reach));x<Math.min(tw,r.x+r.w+Math.ceil(st.engineer.reach));x++){
    const t=y*tw+x,dist=distToRect(x+.5,y+.5,r.x,r.y,r.w,r.h);
    if(distance[t]>=0&&dist>0&&dist<=st.engineer.reach&&(best===null||distance[t]<distance[best]))best=t;
   }return best;
  };
  const {x,y,size}=opening.bounds,gates=new Set(opening.gate),walls=new Set(opening.walls);
  if(gates.size!==4||opening.gate.length!==4||opening.gate.some(t=>!validTile(t))||![0,1,2,3].includes(opening.direction))fail('opening','gate','expected four distinct valid mouth tiles and a direction');
  const perimeter=new Set<number>();
  for(let yy=y;yy<y+size;yy++)for(let xx=x;xx<x+size;xx++)if(xx===x||xx===x+size-1||yy===y||yy===y+size-1){
   const t=yy*tw+xx;perimeter.add(t);
   if(gates.has(t)){if(!passable(st,xx,yy)||distance[t]<0)fail('opening','gate:'+t,'mouth is obstructed or unreachable');}
   else if(!walls.has(t)||!urban.solid[t])fail('opening','wall:'+t,'perimeter has an unreserved or non-solid opening');
  }
  if([...gates].some(t=>!perimeter.has(t))||[...walls].some(t=>!perimeter.has(t)||gates.has(t)))fail('opening','boundary','gate/wall records do not partition the perimeter');
  const sortedGate=[...gates].sort((a,b)=>a-b),stride=opening.direction%2?tw:1;
  if(sortedGate.some((t,i)=>i>0&&t-sortedGate[i-1]!==stride)||sortedGate.some(t=>opening.direction===0?Math.floor(t/tw)!==y:opening.direction===1?t%tw!==x+size-1:opening.direction===2?Math.floor(t/tw)!==y+size-1:t%tw!==x))fail('opening','mouth','gate is not one contiguous opening on its declared side');
  for(const t of gates){const q=t+[-tw,1,tw,-1][opening.direction]*2;if(!validTile(q)||distance[q]<0)fail('opening','exit:'+t,'exterior approach is unreachable');}
  for(const item of ['steel','copper','coal']){
   let count=0,units=0;
   for(let t=0;t<n;t++)if(G.owner[t]===c.homeBlock&&G.patch[t]){const r=rubbleAt(st,t%tw,Math.floor(t/tw));if(r?.type===item&&r.units>0&&approach({x:t%tw,y:Math.floor(t/tw),w:1,h:1})!==null){count++;units+=r.units;}}
   report.measurements.resources.push({item,reachableTiles:count,units});if(!count)fail('resource',item,'no remaining accessible home salvage');
  }
  for(let yy=y+1;yy<y+size-3&&!report.measurements.factory;yy++)for(let xx=x+1;xx<x+size-3;xx++){
   const r={x:xx,y:yy,w:3,h:3};if(!placementGeometryProblem(st,'assembler',xx,yy)&&approach(r)!==null){report.measurements.factory=r;break;}
  }
  if(!report.measurements.factory)fail('factory-space','home','no accessible ordinary assembler footprint in the starter frame');
  for(const s of sites){
   const t=approach(s);report.measurements.sites.push({id:s.id,approach:t,cardinalSteps:t===null?null:distance[t]});if(t===null)fail('site-access',s.id,'no reachable on-foot interaction position');
   let obstructed=false;for(let yy=s.y;yy<s.y+s.h;yy++)for(let xx=s.x;xx<s.x+s.w;xx++)if(G.owner[yy*tw+xx]===-2||urban.solid[yy*tw+xx])obstructed=true;
   if(obstructed)fail('site-footprint',s.id,'water or urban solid intersects the reservation');
  }
  for(let i=0;i<sites.length;i++)for(let j=i+1;j<sites.length;j++){const a=sites[i],b=sites[j];if(a.x<b.x+b.w&&a.x+a.w>b.x&&a.y<b.y+b.h&&a.y+a.h>b.y)fail('site-overlap',a.id+'/'+b.id,'reserved rectangles intersect');}
  if(d.sources.length!==3||new Set(d.sources.map(s=>s.item)).size!==3||!['steel','copper','coal'].every(k=>d.sources.some(s=>s.item===k))||new Set(d.sources.map(s=>s.block)).size!==3||d.sources.some(s=>s.block===c.homeBlock))fail('source-regions','districts','expected three distinct non-home steel/copper/coal regions');
  for(const s of d.sources){
   let footprint:Rect|null=null;const size=MACHINE_SIZE.excavator;
   for(let yy=s.y;yy<=s.y+s.size-size&&!footprint;yy++)for(let xx=s.x;xx<=s.x+s.size-size;xx++){
    const r={x:xx,y:yy,w:size,h:size};if(!placementGeometryProblem(st,'excavator',xx,yy)&&approach(r)!==null){footprint=r;break;}
   }
   report.measurements.extraction.push({item:s.item,footprint});if(!footprint)fail('resource','source:'+s.item,'no accessible legal extractor footprint on the source pad');
  }
  if(!Array.isArray(e.route)||!Array.isArray(d.route)||e.route.length<2||d.route.length<2||e.route.at(-1)!==d.route[0])fail('rail-route','join','missing or disconnected trunk sections');
  if(new Set(d.route).size!==d.route.length)fail('rail-route','district-survey','repeated survey tile');
  const route=c.fixedTram?.route??[...e.route,...d.route.slice(1)],set=new Set(route);report.measurements.routeTiles=route.length;
  if(set.size!==route.length||route.some(t=>!validTile(t)))fail('rail-route','tiles','invalid or repeated survey tile');
  else for(let i=0;i<route.length;i++){
   const t=route[i];if((i>0&&!neighbours(t).includes(route[i-1]))||!c.fixedTram&&neighbours(t).filter(q=>set.has(q)).length>2)fail('rail-route','tile:'+t,'non-cardinal segment or branch/shortcut');
   const why=placementGeometryProblem(st,'track',t%tw,Math.floor(t/tw),0,st.flow!.machines.find(m=>m.id===st.flow!.occ[t]&&m.kind==='track')?.id??-1);if(why)fail('rail-placement','tile:'+t,why);
  }
  const stops=c.fixedTram?c.fixedTram.stops.map(id=>{const m=st.flow!.machines.find(m=>m.id===id)!;return [m.x,m.y];}):[...e.stops,d.stop],stopRects:Rect[]=[];
  if(stops.length!==(c.fixedTram?4:3))fail('rail-route','stops','unexpected number of stop pads');
  for(const [i,stop] of stops.entries()){
   if(!Array.isArray(stop)||stop.length!==2){fail('rail-route','stop:'+i,'invalid coordinates');continue;}
   const [sx,sy]=stop,r={x:sx,y:sy,w:2,h:2};stopRects.push(r);
   if(!validRect(r)){fail('rail-route','stop:'+i,'invalid footprint');continue;}
   const why=placementGeometryProblem(st,'tramstop',sx,sy,0,st.flow!.machines.find(m=>m.id===st.flow!.occ[sy*tw+sx]&&m.kind==='tramstop')?.id??-1);if(why)fail('rail-placement','stop:'+i,why);
   if(!route.some(t=>{const tx=t%tw,ty=Math.floor(t/tw);return ((tx===sx-1||tx===sx+2)&&ty>=sy&&ty<sy+2)||((ty===sy-1||ty===sy+2)&&tx>=sx&&tx<sx+2);}))fail('rail-route','stop:'+i,'no adjacent surveyed track');
   if(approach(r)===null)fail('site-access','stop:'+i,'no reachable stop construction position');
  }
  for(const [i,a] of stopRects.entries()){
   if(route.some(t=>t%tw>=a.x&&t%tw<a.x+a.w&&Math.floor(t/tw)>=a.y&&Math.floor(t/tw)<a.y+a.h)||stopRects.slice(i+1).some(b=>a.x<b.x+b.w&&a.x+a.w>b.x&&a.y<b.y+b.h&&a.y+a.h>b.y))fail('rail-route','stop:'+i,'stop overlaps survey or another stop');
  }
  for(const [i,s] of urban.structures.entries())if(!validRect(s)||!validRect({x:s.door[0],y:s.door[1],w:1,h:1})||distance[s.door[1]*tw+s.door[0]]<0)fail('urban-access','structure:'+i,'invalid footprint or inaccessible door');
  const bases=[c.defence.bases.find(b=>b.block===c.homeBlock)!,{...e.station,hp:300,commissionedAt:-1},{...d.station,hp:300,commissionedAt:-1}];
  for(const b of bases){if(!b)throw Error('missing home core');const origin=campaignOrigin(st,b),staging=origin<0?-1:campaignStaging(st,b,origin);report.measurements.origins.push({block:b.block,origin,staging});if(origin<0||staging<0)fail('threat-access','base:'+b.block,'no current physical origin/staging; remote bases queried without commissioning');}
  if(c.truck){report.measurements.truck.status='queried';if(truckFits(st,c.truck.x,c.truck.y,c.truck.dir))for(const s of [sites[0],sites[2]]){const route=truckWorkRoute(st,[s],8);report.measurements.truck.queries.push({site:s.id,steps:route?.length??null});}}
 } catch(error){fail('input','campaign',(error as Error).message);}
 report.ok=report.failures.length===0;return report;
}
