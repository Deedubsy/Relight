/** Optional M2 fabrication; neither Schematics nor Artifacts are stronghold access keys. */
import type {SimState} from './types';
import type {DiscoveryInfo} from './campaignGuide';
import {inReach} from './ground';
import {machineRunning,type Machine} from './flow';
export const FABRICATION={decodeSeconds:15,artifactYield:2,repeatSeconds:900} as const;
export const MILESTONE_EQUIPMENT=['pumpjack','refinery','assembler2','alienworkbench','substation'] as const;
export type FabricationAction={type:'decode';machine:number};
export function initFabrication(st:SimState,fresh=false):void {
  const g=st.campaign?.progression?.gameplay;if(!g||g.legacyEquipment)return;
  g.legacyEquipment=fresh?[]:MILESTONE_EQUIPMENT.filter(k=>st.flow?.machines.some(m=>m.kind===k)||
    (st.engineer.inv[k]??0)>0||st.flow?.machines.some(m=>(m.inv[k]??0)+((m.cargo as Record<string,number>|undefined)?.[k]??0)>0));
}
export function fabricationBlocker(st:SimState):string {
  const g=st.campaign?.progression?.gameplay;
  return !g?'Authored campaign required':!g.learnedSchematics.includes('overclock')?g.recoveredSchematics.includes('overclock')?'Decode the secured Overclock schematic at a powered Alien workbench':'Recover the optional Workshop salvage schematic':'';
}
export function fabricationCheck(st:SimState,a:FabricationAction):string {
  const g=st.campaign?.progression?.gameplay,m=st.flow?.machines.find(m=>m.id===a.machine);
  return !g||!m||m.kind!=='alienworkbench'?'Select an Alien workbench':st.engineer.down>=0||st.engineer.truckSeat||!inReach(st,m.x,m.y,m.size)?'Walk to the workbench':
    g.learnedSchematics.includes('overclock')?'Overclock knowledge already learned':!g.recoveredSchematics.includes('overclock')?'Recover the Workshop salvage schematic first':m.decode?'Decoding already in progress':m.busy?'Wait for the current craft':!machineRunning(st,m)?'Connect actual local power':'';
}
export function fabricationCommand(st:SimState,a:FabricationAction):string {
  const why=fabricationCheck(st,a);if(why)return why;
  st.flow!.machines.find(m=>m.id===a.machine)!.decode={schematic:'overclock',progress:0};return 'Decoding secured schematic · 15 powered seconds · no Artifacts consumed';
}
export function tickDecode(st:SimState,m:Machine,dt:number):void {
  if(!m.decode)return;m.decode.progress+=dt;
  if(m.decode.progress>=FABRICATION.decodeSeconds){const g=st.campaign!.progression!.gameplay!;if(!g.learnedSchematics.includes('overclock'))g.learnedSchematics.push('overclock');delete m.decode;st.campaign!.progression!.notice='Overclock learned permanently · supply 2 Artifacts, 2 Frames, 1 Board and 4 Wire';}
}
export function fabricationDiscoveries(st:SimState):DiscoveryInfo[]{
  const g=st.campaign?.progression?.gameplay;if(!g?.recoveredSchematics.includes('overclock'))return [];
  const m=st.flow!.machines.find(m=>m.kind==='alienworkbench'),plant=st.campaign!.progression!.sites.find(s=>s.installed),learned=g.learnedSchematics.includes('overclock');
  const needs=[...(g.commissioned.length<1&&!g.legacyEquipment?.includes('alienworkbench')?['M2: commission a first plant']:[]),...(!m?['Build Alien workbench (30 Steel, 10 Copper, 4 Frames, 2 Boards)']:!machineRunning(st,m)?['Power workbench (80 kW)']:[]),...(!learned?['Decode the secured schematic (15 powered seconds, no Artifacts)']:[]), 'Craft: 2 Artifacts + 2 Frames + 1 Board + 4 Wire; 20 powered seconds'];
  return [{id:'gp:overclock',title:'Optional Overclock fabrication',defaultTitle:'Overclock fabrication',x:m?.x??plant?.x??70,y:m?.y??plant?.y??357,size:1,status:learned?'Schematic learned permanently':m?.decode?`Decoding ${Math.ceil(m.decode.progress)}/${FABRICATION.decodeSeconds} s`:'Schematic secured · not yet decoded',detail:needs.join(' · ')+'. Transfer ingredients into the workbench and take its finished output. One removable module gives +10% processing capacity; power, ingredients and output room still matter.',needs:[],
    actions:m&&!learned?[{label:'Decode Overclock schematic',reason:fabricationCheck(st,{type:'decode',machine:m.id}),commands:[{type:'fabrication',action:{type:'decode',machine:m.id}}]}]:[]}];
}
export function fabricationProblem(st:SimState):string {
  const g=st.campaign?.progression?.gameplay;if(!g)return '';
  if(g.legacyEquipment!==undefined&&(!Array.isArray(g.legacyEquipment)||new Set(g.legacyEquipment).size!==g.legacyEquipment.length||g.legacyEquipment.some(k=>!(MILESTONE_EQUIPMENT as readonly string[]).includes(k))))return 'Invalid inherited equipment gates';
  for(const m of st.flow!.machines){if(m.decode&&(m.kind!=='alienworkbench'||m.decode.schematic!=='overclock'||!Number.isFinite(m.decode.progress)||m.decode.progress<0||m.decode.progress>=FABRICATION.decodeSeconds||!g.recoveredSchematics.includes('overclock')||g.learnedSchematics.includes('overclock')||m.busy))return 'Invalid schematic decoding';
    if(m.kind==='alienworkbench'&&m.busy&&!g.learnedSchematics.includes('overclock'))return 'Fabrication requires learned knowledge';}
  return '';
}
