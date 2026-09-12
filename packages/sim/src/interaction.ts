import {RIVERFRONT_BUILDINGS} from './city/riverfront';
import {cityVisible} from './authoredCity';
import {cityProp,cityPropReason} from './authoredCity';
import {progressionCheck} from './progression';
/** Read-only campaign E target. The renderer displays and executes this same result. */
import type {Command,SimState} from './types';
import {isCampaign} from './rules';
import {inReach} from './ground';
import {depotRect,machineAt,KIND_LABEL,canRepair,lightAt} from './flow';
import {machineDimensions} from './footprint';
import {workbenchTile} from './walk';
import {campaignDiscoveries} from './campaignGuide';
import {coreAt,defenceMax,defenceHp,repairCheck,DEFENCE} from './campaignDefence';
import {truckOccupies,truckRect,truckBoardCheck} from './truck';
export interface Interaction {
  x:number;y:number;label:string;reason:string;detail?:string;commands:Command[];
  panel?:'home'|'chest'|'routing'|'inspection';
}
export function campaignInteraction(st:SimState, hover?:{tx:number;ty:number}|null):Interaction|null {
  if(!isCampaign(st)||!st.flow||st.engineer.down>=0)return null;
  if(st.campaign?.progression?.passenger)return {x:st.engineer.x,y:st.engineer.y,label:'Exit tram',reason:'',commands:[{type:'progression',action:{type:'exit'}}]};
  const discoveries=campaignDiscoveries(st),e=st.engineer;
  const query=(x:number,y:number):Interaction|null=>{
    const result=(label:string,commands:Command[]=[],reason='',size=1,height=size):Interaction=>({x,y,label,commands,reason:reason||(!inReach(st,x,y,size,height)?'Walk closer on foot':'')});
    const prop=cityProp(st,x,y);if(prop)return result(prop.kind==='gate'?'Open return gate':'Clear light debris',[{type:'cityProp',id:prop.id}],cityPropReason(st,prop.id),prop.w,prop.h);
    if(e.truckSeat||truckOccupies(st,x,y))return result(e.truckSeat?'Exit truck':'Board truck',[{type:'factory',action:{type:'truckBoard'}}],truckBoardCheck(st));
    const tram=st.flow!.machines.find(m=>m.kind==='tram'&&Math.abs(m.x-x)<2&&Math.abs(m.y-y)<2);
    if(tram)return result('Board tram',[{type:'progression',action:{type:'board'}}],progressionCheck(st,{type:'board'}));
    const core=coreAt(st,x,y),m=machineAt(st,x,y);
    if(((core&&core.hp<DEFENCE.coreHp)||(m&&defenceMax(m)>0&&defenceHp(m)<defenceMax(m)))&&m?.kind!=='depot')return result(   /* GP-HOME-REPAIR: the Home core is the workshop — E opens it; its Base core card repairs */core?'Repair base core':`Repair ${KIND_LABEL[m!.kind]}`,[{type:'repairDefence',x,y}],repairCheck(st,x,y),core?.size??m!.size);
    const site=discoveries.find(s=>x>=s.x&&x<s.x+s.size&&y>=s.y&&y<s.y+s.size);
    if(site){if(st.campaign?.progression?.sites.some(s=>s.id===site.id&&(s.recovered||s.kind==='core'&&!s.seen)))return null;if(!site.actions.length)return null;const a=site.actions[0];return {...result(a?.label??site.title,a?.commands??[],a?.reason??'',site.size),detail:site.status};}
    const [wx,wy]=workbenchTile(st);
    if(x>=wx&&x<wx+2&&y>=wy&&y<wy+2)return {...result('Open Home workshop',[],'',2),panel:'home'};
    const l=lightAt(st,x,y);
    if(l?.l.why)return result('Repair streetlight',[{type:'factory',action:{type:'repair',x,y}}],canRepair(st,x,y).reason);
    if(m){const [w,h]=machineDimensions(m),panel=m.kind==='depot'?'home':m.kind==='chest'||m.kind==='tramstop'?'chest':m.kind==='inserter'||m.kind==='splitter'?'routing':'inspection';
      return {...result(panel==='home'?'Open Home workshop & storage':`${panel==='chest'?'Open':'Inspect'} ${KIND_LABEL[m.kind]}`,[],'',w,h),x:m.x,y:m.y,panel,reason:inReach(st,m.x,m.y,w,h)?'':'Walk closer on foot'};}
    const house=st.city?.mapId?RIVERFRONT_BUILDINGS.find(b=>b.note&&x>=b.x+1&&x<b.x+b.w-1&&y>=b.y+1&&y<b.y+b.h-1):undefined;if(house&&cityVisible(st,x+.5,y+.5))return {...result('Read the house notes'),detail:house.note};
    return null;
  };
  if(e.truckSeat)return query(Math.floor(e.x),Math.floor(e.y));
  if(hover){const hit=query(hover.tx,hover.ty);if(hit)return hit;}
  const d=depotRect(st),[wx,wy]=workbenchTile(st),truck=st.campaign?.truck;
  const home=query(d.x,d.y);if(home?.panel==='home'&&!home.reason)return home;
  const points=[d,{x:wx,y:wy},...discoveries,...st.flow.machines];
  if(truck){const r=truckRect(truck);points.push({x:Math.ceil(r.x),y:Math.ceil(r.y)});}
  return points.map(p=>query(p.x,p.y)).filter((p):p is Interaction=>!!p&&!p.reason)
    .sort((a,b)=>Math.hypot(a.x-e.x,a.y-e.y)-Math.hypot(b.x-e.x,b.y-e.y))[0]??null;
}
