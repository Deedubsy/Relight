/** P7-04: saved installation knowledge and read-only player-facing discovery information. */
import type { Command, SimState } from './types';
import { isCampaign } from './rules';
import { inReach } from './ground';
import { campaignSite, SITE_IDS, SITE_LABELS, EXPANSION, describeSite, siteCheck, type SiteId } from './expansion';
import { RECRUITS, recruitDescription, recruitCheck } from './campaignRecruits';
import { discoveryDescription, discoveryCheck } from './campaignDiscovery';
import { describeTurbine, turbineReachProblem, TURBINE } from './campaignTurbine';
import { campaignGrid, campaignThrottle } from './campaignPower';
import { workshopStatus } from './campaignDistricts';

export const SITE_CLUE_RADIUS = 24;
export interface CampaignKnowledge { version: 1; sites: Partial<Record<SiteId,number>> }
export function initKnowledge(st:SimState, fresh=false):void {
  const c=st.campaign;if(!isCampaign(st)||!c)return;
  // Old panels exposed every installation already. Retain that knowledge during migration.
  c.knowledge??={version:1,sites:Object.fromEntries((fresh?['station']:SITE_IDS).map(id=>[id,st.t]))};
  c.version=9;
}
export function knownSite(st:SimState,id:SiteId):boolean {
  const s=campaignSite(st,id);
  return !!s && (s.restoredAt>=0 || st.campaign?.knowledge?.sites[id]!==undefined);
}
export function tickKnowledge(st:SimState):void {
  const k=st.campaign?.knowledge;if(!k)return;
  if(campaignSite(st,'station')!.restoredAt>=0)k.sites.radio??=st.t;
  if(campaignSite(st,'radio')!.restoredAt>=0)k.sites.northStation??=st.t;
  if(campaignSite(st,'northStation')!.restoredAt>=0)k.sites.workshop??=st.t;
  for(const id of SITE_IDS){const s=campaignSite(st,id)!;
    if(k.sites[id]===undefined && (s.restoredAt>=0 || Math.hypot(st.engineer.x-s.x-s.size/2,st.engineer.y-s.y-s.size/2)<=SITE_CLUE_RADIUS))k.sites[id]=st.t;
  }
}
export function knowledgeProblem(st:SimState):string {
  const c=st.campaign,k=c?.knowledge;
  if((c?.version??0)<9)return k?'installation knowledge requires campaign metadata version 9':'';
  if(!k||k.version!==1||!k.sites||typeof k.sites!=='object'||Array.isArray(k.sites)||k.sites.station===undefined
    ||Object.entries(k.sites).some(([id,t])=>!SITE_IDS.includes(id as SiteId)||!Number.isFinite(t)||t<0||t>st.t))return 'invalid installation knowledge';
  return '';
}
export interface GuideAction { label:string; reason:string; commands:Command[] }
export interface DiscoveryInfo {
  id:string; title:string; status:string; detail:string; x:number; y:number; size:number;
  needs:{item:string;delivered:number;required:number}[]; actions:GuideAction[];
}
const local=(st:SimState,s:{x:number;y:number;size:number}):string=>st.engineer.down>=0||st.engineer.truckSeat||!inReach(st,s.x,s.y,s.size)?'Walk closer on foot to interact.':'';
/** Only known locations. No hidden counters, locations, roster or attack query enters this model. */
export function campaignDiscoveries(st:SimState):DiscoveryInfo[] {
  if(!isCampaign(st))return [];
  const out:DiscoveryInfo[]=[];
  for(const id of SITE_IDS){if(!knownSite(st,id))continue;const s=campaignSite(st,id)!;
    const restored=s.restoredAt>=0,powered=campaignThrottle(st,s.block)>0;
    const disabled=st.campaign!.defence!.bases.some(b=>b.block===s.block&&b.hp===0);
    let status=restored?(disabled?'Restored · core disabled':powered?'Restored · powered':'Restored · no power'):'Discovered · awaiting restoration';
    // describeSite(radio) includes radio warning data: the journal deliberately avoids that query.
    let detail=id==='radio'?'Powered service names the announced target; a precision upgrade adds approach and composition. Read received intelligence in defence warnings.':id==='workshop'?(restored?workshopStatus(st):'Automates paid repairs near the workshop when supplied and powered, between attacks.'):restored?describeSite(st,id):'Deliver carried materials and provide local power. Station restoration registers a base; build and power actual track, platforms and trams for transport.';
    if(id==='workshop'&&restored)status=`Restored · ${detail.replace(/^Workshop /,'').replace(/\.$/,'')}`;
    if(!restored)detail+=` ${siteCheck(st,id)}`;
    const actions:GuideAction[]=restored?(id==='station'?[{label:'Collect tram kit',reason:local(st,s),commands:[{type:'collectTramKit'}]}]:[]):[{label:`Deliver and restore ${SITE_LABELS[id]}`,reason:local(st,s),commands:[{type:'deliverSite',site:id},{type:'restoreSite',site:id}]}];
    out.push({id,title:SITE_LABELS[id][0].toUpperCase()+SITE_LABELS[id].slice(1),status,detail,x:s.x,y:s.y,size:s.size,
      needs:restored?[]:(['steel','copper'] as const).map(item=>({item,delivered:s.delivered[item],required:EXPANSION[id][item]})),actions});
  }
  for(const s of st.campaign!.recruits?.sites??[]){if(s.seenAt<0&&s.recruitedAt<0)continue;
    out.push({id:s.id,title:RECRUITS[s.kind].name,status:s.recruitedAt>=0?'Recruited · plans retained':'Discovered · not recruited',detail:recruitDescription(st,s),x:s.x,y:s.y,size:1,needs:[],
      actions:[{label:`${s.recruitedAt>=0?'Recruited':'Recruit'} ${RECRUITS[s.kind].name}`,reason:recruitCheck(st,s.id),commands:[{type:'recruitSurvivors',id:s.id}]}]});
  }
  const t=st.campaign!.turbine;
  if(t&&(t.seenAt>=0||t.restoredAt>=0)){const restored=t.restoredAt>=0,grid=campaignGrid(st);
    out.push({id:t.id,title:'Turbine hall',status:!restored?'Discovered · awaiting restoration':!t.enabled?'Restored · switched off':grid.blocks[t.block].turbineSupply===0?'Restored · core disabled':grid.turbineOutput>0?'Restored · generating':'Restored · standby',detail:describeTurbine(st),x:t.x,y:t.y,size:t.size,
      needs:restored?[]:(['steel','copper','concrete'] as const).map(item=>({item,delivered:t.delivered[item],required:TURBINE.cost[item]})),
      actions:[{label:restored?`Switch Turbine ${t.enabled?'off':'on'}`:'Deliver and restore Turbine hall',reason:turbineReachProblem(st),commands:restored?[{type:'setTurbineEnabled',enabled:!t.enabled}]:[{type:'deliverTurbine'},{type:'restoreTurbine'}]}]});
  }
  const d=st.campaign!.discovery;
  if(d&&(d.seenAt>=0||d.recoveredAt>=0))out.push({id:d.id,title:'Workshop records',status:d.recoveredAt>=0?'Recovered · field-repair tool fitted':'Discovered · records available',detail:discoveryDescription(st),x:d.x,y:d.y,size:1,needs:[],actions:[{label:d.recoveredAt>=0?'Field-repair tool fitted':'Recover field-repair schematic',reason:discoveryCheck(st,d.id),commands:[{type:'recoverSchematic',id:d.id}]}]});
  return out;
}
