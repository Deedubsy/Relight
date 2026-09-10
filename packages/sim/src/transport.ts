/** Read-only presentation of the same components and stop ownership used by freight movement. */
import {fixedTramStatus} from './fixedTram';
import type { SimState } from './types';
import { machineAt, tramRoute, routeStops, stopAt, machineRunning, invTotal, STOP_CAP, type Machine } from './flow';
export interface StationRoute {
 stop:Machine;paths:number[][];stops:{machine:Machine;status:'ready'|'unpowered'|'full'}[];
 trams:{machine:Machine;status:'running'|'dwelling'|'disconnected'|'no route'|'waiting for power'}[];
 interrupted:boolean;summary:string;
}
export function stationRoute(st:SimState,id:number):StationRoute|null {
 const f=st.flow,stop=f?.machines.find(m=>m.id===id&&m.kind==='tramstop');if(!f||!stop)return null;
 const paths:number[][]=[],seen=new Set<number>();let branched=false;
 for(let y=stop.y-1;y<=stop.y+stop.size;y++)for(let x=stop.x-1;x<=stop.x+stop.size;x++){
  const track=machineAt(st,x,y);if(track?.kind!=='track'||stopAt(st,x,y)?.id!==id||seen.has(y*f.tw+x))continue;
  const path=tramRoute(st,track);if(!path.length){branched=true;continue;}for(const t of path)seen.add(t);paths.push(path);
 }
 if(st.campaign?.fixedTram?.stops.includes(id)){paths.splice(0,paths.length,st.campaign.fixedTram.route);seen.clear();for(const t of st.campaign.fixedTram.route)seen.add(t);branched=false;}
 const connected=new Map<number,Machine>([[id,stop]]);for(const path of paths)for(const s of routeStops(st,path))connected.set(s.id,s);
 const stops=[...connected.values()].map(machine=>({machine,status:!machineRunning(st,machine)?'unpowered' as const:invTotal(machine.cargo)>=STOP_CAP?'full' as const:'ready' as const}));
 const trams=f.machines.filter(m=>m.kind==='tram'&&(seen.has(m.y*f.tw+m.x)||m.manifest?.some(r=>(r.returning?r.origin:r.destination)===id))).map(machine=>{
  const path=tramRoute(st,machine),ids=new Set(routeStops(st,path).map(s=>s.id));
  const disconnected=machine.manifest?.some(r=>{const target=r.returning?r.origin:r.destination;return !ids.has(target)&&f.machines.some(s=>s.id===target&&s.kind==='tramstop');});
  return {machine,status:st.campaign?.fixedTram&&machine.phase===2?'waiting for power' as const:disconnected?'disconnected' as const:path.length<2?'no route' as const:machine.phase===1?'dwelling' as const:'running' as const};
 });
 const issues:string[]=[];
 if(branched)issues.push('Branched track: tram parks');
 if(!paths.some(p=>p.length>=2))issues.push('No connected track route');
 if(paths.length&&connected.size<2)issues.push('No other stop on this track component');
 if(!trams.length)issues.push('No tram serving this stop');
 for(const s of stops)if(s.status!=='ready'&&!(st.campaign?.fixedTram&&s.status==='unpowered'&&stops.filter(s=>s.status!=='unpowered').length>=2))issues.push(`Stop ${s.machine.id}: ${s.status==='full'?'arrivals full; refused cargo returns':'unpowered; transfers paused'}`);
 for(const t of trams)if(t.status==='disconnected'||t.status==='no route')issues.push(`Tram ${t.machine.id}: ${t.status}; cargo retained`);
 const summary=`${st.campaign?.fixedTram?.stops.includes(id)?fixedTramStatus(st)+' · ':''}${connected.size} stop${connected.size===1?'':'s'} · ${trams.length} tram${trams.length===1?'':'s'} · ${issues.length?issues.join(' · '):'Service connected'}${trams.some(t=>t.machine.manifest?.some(r=>r.returning))?' · Return cargo aboard':''}`;
 return {stop,paths,stops,trams,interrupted:issues.length>0,summary};
}
