import type {SimState} from './types';
import {campaignWarning,knownCampaignThreat} from './campaignThreat';
import {campaignClock} from './rules';
import {blockName} from './names';
import {blockOfTile} from './ground';
import {cityVisible} from './authoredCity';
import {knownDistrict} from './navigation';
import {machineConstraints} from './inspection';
import {KIND_LABEL,litAt} from './flow';
import {DANGER_R} from './threat';
import {DEFENCE} from './campaignDefence';
import {campaignGrid} from './campaignPower';
export function campaignOutages(st:SimState):CampaignAlert[] {
 if(!st.flow||!st.campaign)return [];
 const grid=campaignGrid(st);
 return (st.campaign.defence?.bases??[]).filter(b=>b.hp===0||(grid.blocks[b.block]?.supply??0)<=0).map(b=>({id:`outage:${b.block}`,priority:75,title:`No power · ${b.block===st.campaign!.homeBlock?'Founders Court':blockName(st,b.block)}`,detail:b.hp===0?'Base core disabled. Visit the core to repair.':'No connected generation. Refill a Generator or restore a connected power source.',location:{x:b.x+b.size/2,y:b.y+b.size/2}}));
}
export interface CampaignAlert {id:string;priority:number;title:string;detail:string;location:{x:number;y:number}|null}
/** Read-only conditions. Never inspect a hidden attack roster, target or creature location. */
export function campaignAlerts(st:SimState):CampaignAlert[] {
 const d=st.campaign?.defence;if(!d)return [];const out:CampaignAlert[]=campaignOutages(st),e=st.engineer,target=knownCampaignThreat(st);
 const relay=st.campaign?.progression?.sites.find(s=>`relay:${s.id}`===e.lastDamageSource);
 if(e.down>=0||st.t-e.lastHit<5)out.push({id:'engineer:danger',priority:100,title:e.down>=0?'Engineer down':relay?'Alien relay draining health':'Engineer under attack',detail:e.down>=0?'Wait for recovery.':relay?'Move away from the relay or behind solid cover.':'Take cover or defend yourself.',location:{x:e.x,y:e.y}});
 for(const c of st.flow?.threat?.crawlers??[])if(c.hp>0&&c.kind==='shade'&&Math.hypot(c.x-e.x,c.y-e.y)<14&&cityVisible(st,c.x,c.y)&&!litAt(st,Math.floor(c.x),Math.floor(c.y)))out.push({id:`shade:${c.id}`,priority:86,title:'Shade · needs powered lighting',detail:'Bring a powered Lamp to make it vulnerable. Your flashlight only helps you see.',location:{x:c.x,y:c.y}});
 for(const c of st.flow?.threat?.crawlers??[])if(c.hp>0&&Math.hypot(c.x-e.x,c.y-e.y)<=DANGER_R&&(c.kind!=='shade'||litAt(st,Math.floor(c.x),Math.floor(c.y))))out.push({id:`nearby:${c.id}`,priority:85,title:`Nearby threat · ${c.kind} #${c.id}`,detail:'Hostile within close range of the engineer.',location:{x:c.x,y:c.y}});
 for(const b of d.bases)if(b.hp<DEFENCE.coreHp&&b.block!==target?.block)out.push({id:`damage:${b.block}`,priority:80,title:`Base ${b.hp===0?'disabled':'damaged'} · ${blockName(st,b.block)}`,detail:`Core ${Math.ceil(b.hp)}/${DEFENCE.coreHp} HP. Visit the core to repair.`,location:{x:b.x+b.size/2,y:b.y+b.size/2}});
 if(target){const active=target.phase==='assault'||target.phase==='minor raid',title=active?'Base under attack':target.phase==='recovery'?'Base core disabled':target.phase==='withdrawal'?'Attackers withdrawing':`Assault in ${Math.max(0,Math.ceil(((d.major?.startsAt??st.t)-st.t)/60))} min`;out.push({id:`threat:${target.block}`,priority:active?90:target.phase==='recovery'?80:60,title:`${title} · ${blockName(st,target.block)}`,detail:campaignWarning(st),location:{x:target.x,y:target.y}});}
 else if(d.major)out.push({id:'assault:unknown',priority:65,title:'Assault · location unavailable',detail:campaignWarning(st),location:null});
 out.push({id:'schedule',priority:50,title:d.major?'Assault schedule pending withdrawal':`Major assault · Night ${campaignClock({t:d.nextDawn}).day}`,detail:d.major?'Current assault in progress. The next schedule updates after withdrawal.':'Smaller raids can come sooner.',location:null});
 for(const m of st.flow?.machines??[]){if(!['assembler','mixer','generator','turret'].includes(m.kind)||!knownDistrict(st,blockOfTile(st,m.x,m.y)))continue;const blockers=machineConstraints(st,m).blockers;if(blockers.length)out.push({id:`production:${m.id}`,priority:20,title:`Production warning · ${KIND_LABEL[m.kind]} #${m.id}`,detail:blockers.map(b=>b.text).join(' · '),location:{x:m.x+.5,y:m.y+.5}});}
 return out.sort((a,b)=>b.priority-a.priority||a.id.localeCompare(b.id));
}
