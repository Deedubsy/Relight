import {progressionDiscoveries,progressionCheck} from './progression';
import {lockReason,chestCount,type ChestItem} from './flow';
import {take} from './engineer';
import {radioUpgradeCheck,CAMPAIGN_THREAT} from './campaignThreat';
/** P7-04: saved installation knowledge and read-only player-facing discovery information. */
import {navigationName} from './navigation';
import type { Command, SimState } from './types';
import { isCampaign } from './rules';
import { inReach } from './ground';
import { campaignSite, SITE_IDS, SITE_LABELS, EXPANSION, describeSite, siteDeliveryCheck, type SiteId } from './expansion';
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
  c.version=10;
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
export interface ProjectMaterial {item:string;delivered:number;required:number;carried:number;home:number;transfer:number}
export interface ServiceCircuit {restored:boolean;powered:boolean;materials:ProjectMaterial[];blocker:string}
export interface DiscoveryInfo {
  id:string; title:string; defaultTitle:string; status:string; detail:string; x:number; y:number; size:number;
  needs:{item:string;delivered:number;required:number}[]; actions:GuideAction[];
  circuit?:ServiceCircuit; reward?:{item:string;remaining:number;collect:number}[];
  upgrade?:{bought:boolean;steel:number;copper:number};
}
const local=(st:SimState,s:{x:number;y:number;size:number}):string=>st.engineer.down>=0||st.engineer.truckSeat||!inReach(st,s.x,s.y,s.size)?'Walk closer on foot to interact.':'';
/** Uses only carried inventory for delivery; home stock is informational and separately identified. */
function circuit(st:SimState,s:{restoredAt:number;block:number;delivered:Record<string,number>},cost:Record<string,number>):ServiceCircuit {
  const restored=s.restoredAt>=0,powered=campaignThrottle(st,s.block)>0;
  const materials=Object.entries(cost).map(([item,required])=>({item,required,delivered:s.delivered[item],carried:st.engineer.inv[item]??0,home:chestCount(st,item as ChestItem),transfer:restored?0:Math.min(Math.max(0,required-s.delivered[item]),st.engineer.inv[item]??0)}));
  return {restored,powered,materials,blocker:restored?'':!powered?'Local power is still required to restore.':materials.some(m=>m.delivered+m.transfer<m.required)?'More carried materials are required to finish.':''};
}
function restorationAction(c:ServiceCircuit,reason:string,deliver:Command,restore:Command):GuideAction {
  const transfer=c.materials.some(m=>m.transfer>0),ready=c.materials.every(m=>m.delivered+m.transfer>=m.required)&&c.powered;
  return {label:transfer?(ready?'Deliver and restore':'Deliver carried materials'):'Restore',reason:reason||(!transfer&&!ready?(c.blocker||'Deliver the missing materials.') :''),commands:transfer?(ready?[deliver,restore]:[deliver]):[restore]};
}
/** Only known locations. No hidden counters, locations, roster or attack query enters this model. */
export function campaignDiscoveries(st:SimState):DiscoveryInfo[] {
  if(!isCampaign(st))return [];
  const out:Omit<DiscoveryInfo,'defaultTitle'>[]=[];
  for(const id of SITE_IDS){if(!knownSite(st,id))continue;const s=campaignSite(st,id)!;
    const restored=s.restoredAt>=0,powered=campaignThrottle(st,s.block)>0;
    const disabled=st.campaign!.defence!.bases.some(b=>b.block===s.block&&b.hp===0);
    let status=restored?(disabled?'Restored · core disabled':powered?'Restored · powered':'Restored · no power'):'Discovered · awaiting restoration';
    // describeSite(radio) includes radio warning data: the journal deliberately avoids that query.
    const detail=id==='radio'?'Powered service names the announced target; a precision upgrade adds approach and composition. Read received intelligence in defence warnings.':id==='workshop'?(restored?workshopStatus(st):'Automates paid repairs near the workshop when supplied and powered, between attacks.'):restored?describeSite(st,id):'Deliver carried materials and provide local power. Station restoration registers a base; power the permanent tram stops. Any two powered stops start automatic transport.';
    if(id==='workshop'&&restored)status=`Restored · ${detail.replace(/^Workshop /,'').replace(/\.$/,'')}`;
    const service=circuit(st,s,EXPANSION[id]);
    const reward=id==='station'&&restored?(()=>{const e={...st.engineer,inv:{...st.engineer.inv}};return (['tram','tramstop','track'] as const).map(item=>({item,remaining:st.campaign!.expansion!.reward[item],collect:take(e,item,st.campaign!.expansion!.reward[item])}));})():undefined;
    const actions:GuideAction[]=restored?(id==='station'&&(!st.campaign?.fixedTram||reward!.some(r=>r.remaining))?[{label:'Collect tram kit',reason:local(st,s)||(reward!.every(r=>!r.remaining)?'Kit fully collected.':reward!.every(r=>!r.collect)?'Pockets full. Make room to collect.':''),commands:[{type:'collectTramKit'}]}]:id==='radio'?[{label:'Buy radio precision upgrade',reason:local(st,s)||radioUpgradeCheck(st),commands:[{type:'upgradeRadio'}]}]:[]):[restorationAction(service,local(st,s)||siteDeliveryCheck(st,id),{type:'deliverSite',site:id},{type:'restoreSite',site:id})];
    out.push({id,title:SITE_LABELS[id][0].toUpperCase()+SITE_LABELS[id].slice(1),status,detail,x:s.x,y:s.y,size:s.size,
      needs:restored?[]:(['steel','copper'] as const).map(item=>({item,delivered:s.delivered[item],required:EXPANSION[id][item]})),actions,circuit:service,reward:st.campaign?.fixedTram&&!reward?.some(r=>r.remaining)?undefined:reward,...(id==='radio'&&restored?{upgrade:{bought:!!st.campaign!.defence!.radioUpgrade,steel:CAMPAIGN_THREAT.radioUpgradeSteel,copper:CAMPAIGN_THREAT.radioUpgradeCopper}}:{})});
  }
  for(const s of st.campaign!.recruits?.sites??[]){if(s.seenAt<0&&s.recruitedAt<0)continue;
    out.push({id:s.id,title:RECRUITS[s.kind].name,status:s.recruitedAt>=0?'Recruited · plans retained':'Discovered · not recruited',detail:recruitDescription(st,s),x:s.x,y:s.y,size:1,needs:[],
      actions:s.recruitedAt>=0?[]:[{label:`${s.recruitedAt>=0?'Recruited':'Recruit'} ${RECRUITS[s.kind].name}`,reason:recruitCheck(st,s.id),commands:[{type:'recruitSurvivors',id:s.id}]}]});
  }
  const t=st.campaign!.turbine;
  if(t&&(t.seenAt>=0||t.restoredAt>=0)){const restored=t.restoredAt>=0,grid=campaignGrid(st),service=circuit(st,t,TURBINE.cost);
    out.push({id:t.id,title:'Turbine hall',status:!restored?'Discovered · awaiting restoration':!t.enabled?'Restored · switched off':grid.blocks[t.block].turbineSupply===0?'Restored · core disabled':grid.turbineOutput>0?'Restored · generating':'Restored · standby',detail:describeTurbine(st),x:t.x,y:t.y,size:t.size,
      needs:restored?[]:(['steel','copper','concrete'] as const).map(item=>({item,delivered:t.delivered[item],required:TURBINE.cost[item]})),
      circuit:service,actions:restored?[{label:`Switch Turbine ${t.enabled?'off':'on'}`,reason:turbineReachProblem(st),commands:[{type:'setTurbineEnabled',enabled:!t.enabled}]}]:[restorationAction(service,turbineReachProblem(st),{type:'deliverTurbine'},{type:'restoreTurbine'})]});
  }
  const d=st.campaign!.discovery;
  if(d&&(d.seenAt>=0||d.recoveredAt>=0))out.push({id:d.id,title:'Workshop records',status:d.recoveredAt>=0?'Recovered · field-repair tool fitted':'Discovered · records available',detail:discoveryDescription(st),x:d.x,y:d.y,size:1,needs:[],actions:d.recoveredAt>=0?[]:[{label:d.recoveredAt>=0?'Field-repair tool fitted':'Recover field-repair schematic',reason:discoveryCheck(st,d.id),commands:[{type:'recoverSchematic',id:d.id}]}]});
  for(const info of out){const recruit=st.campaign?.recruits?.sites.find(s=>s.id===info.id);if(recruit?.kind==='railcrew'&&recruit.recruitedAt>=0&&!st.campaign?.progression?.freightUpgrade)info.actions.push({label:'Upgrade freight capacity · 20 steel + 10 copper',reason:progressionCheck(st,{type:'freight'}),commands:[{type:'progression',action:{type:'freight'}}]});}
  out.push(...progressionDiscoveries(st));
  return out.map(s=>({...s,defaultTitle:s.title,title:navigationName(st,`site:${s.id}`,s.title)}));
}

/** UI catalogue knowledge follows earned plans or the existing discovered source of those plans. */
export function knownEquipment(st:SimState,kind:import('./flow').Kind):boolean {
  if(st.campaign?.fixedTram&&['track','tramstop','tram'].includes(kind))return false;
  if(!isCampaign(st)||!lockReason(st,kind))return true;
  const recruit=kind==='arclamp'?'lamplighters':kind==='mixer'||kind==='barricade'?'concrete':kind==='floodlight'||kind==='bigpole'?'electricians':null;
  if(recruit)return !!st.campaign?.recruits?.sites.some(s=>s.kind===recruit&&(s.seenAt>=0||s.recruitedAt>=0));
  return ['track','tramstop','tram'].includes(kind)&&knownSite(st,'station');
}
