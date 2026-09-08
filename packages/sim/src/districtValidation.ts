/** New district records must be complete and consistent; old previews upgrade explicitly. */
import type { SimState } from './types';
import { DISTRICT_ECONOMY } from './campaignDistricts';
export function districtsProblem(st:SimState):string {
  const c=st.campaign,d=c?.districts;
  if((c?.version??0)<4)return d?'districts require campaign metadata version 4':'';
  if(!d||d.version!==1||!st.flow||!st.city||!Array.isArray(d.sources)||!Array.isArray(d.route))return 'invalid campaign districts';
  const tw=st.flow.tw,th=st.city.th;
  const int=(n:unknown,min=0):n is number=>Number.isSafeInteger(n)&&(n as number)>=min;
  const rect=(s:{block:number;x:number;y:number;size:number})=>s&&int(s.block)&&!!st.blocks[s.block]&&int(s.x)&&int(s.y)&&int(s.size,1)&&s.x+s.size<=tw&&s.y+s.size<=th;
  for(const s of [d.station,d.workshop])if(!rect(s)||!int(s.restoredAt,-1)||!s.delivered||![s.delivered.steel,s.delivered.copper].every(n=>int(n)))return 'invalid district restoration site';
  if(d.station.block===c!.homeBlock||d.station.block===c!.expansion!.station.block||d.workshop.block!==d.station.block
    ||(d.station.restoredAt>=0&&c!.expansion!.station.restoredAt<0)||(d.workshop.restoredAt>=0&&d.station.restoredAt<0))return 'invalid district progression';
  if(d.sources.length!==3||new Set(d.sources.map(s=>s.item)).size!==3||new Set(d.sources.map(s=>s.block)).size!==3
    ||d.sources.some(s=>!rect(s)||s.size!==5||!['steel','copper','coal'].includes(s.item)||s.block===c!.homeBlock))return 'invalid persistent extraction sources';
  if(d.route.length<2||d.route[0]!==c!.expansion!.route.at(-1)||new Set(d.route).size!==d.route.length||d.route.some((t,i)=>!int(t)||t>=tw*th
    ||(i>0&&Math.abs(t%tw-d.route[i-1]%tw)+Math.abs(Math.floor(t/tw)-Math.floor(d.route[i-1]/tw))!==1))
    ||!Array.isArray(d.stop)||d.stop.length!==2||!rect({block:d.station.block,x:d.stop[0],y:d.stop[1],size:2}))return 'invalid district trunk geometry';
  if(!d.supplied||![d.supplied.steel,d.supplied.copper,d.supplied.magazine,d.repairs,d.visits].every(n=>int(n))||!int(d.lastVisit,-1)||!int(d.resuppliedAt,-1))return 'invalid district service history';
  if(d.resuppliedAt>=0&&(d.station.restoredAt<0||d.visits<DISTRICT_ECONOMY.resupplyVisits||d.supplied.steel<DISTRICT_ECONOMY.resupplySteel||d.supplied.copper<DISTRICT_ECONOMY.resupplyCopper||d.supplied.magazine<DISTRICT_ECONOMY.resupplyMagazines))return 'unearned district resupply milestone';
  const r=d.repair;if(r&&(!int(r.target)||!Number.isFinite(r.progress)||r.progress<0||r.progress>=DISTRICT_ECONOMY.repairSeconds||!st.flow.machines.some(m=>m.id===r.target&&(m.kind==='wall'||m.kind==='turret'||m.kind==='barricade'))))return 'invalid workshop repair';
  return '';
}
