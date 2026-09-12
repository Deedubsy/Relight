import {take} from './engineer';
import {FABRICATION} from './fabrication';
import {threatOf} from './threat';
import type {SimState} from './types';
import type {DiscoveryInfo} from './campaignGuide';
import {FIRST_CAMPS,FREIGHT_ARENA,FREIGHT_GATES} from './city/gameplaySites';
import {ground,blockOfTile,citySight} from './ground';
import {passable,findPath} from './walk';
import {newCombatActor,GP_COMBAT,type CombatKind} from './gameplayCombat';
import {RIVERFRONT} from './city/riverfront';

export interface RegionSite {cycle?:number;rewarded?:number;readyAt?:number;watchUntil?:number;seen:boolean; spawned:boolean; guards:number[]}
export interface FirstRegion {version:1; sites:Record<string,RegionSite>}
export type RegionAction={type:'claimKey'|'openFreight'|'salvage'|'watchCamp';id:string};
export function initFirstRegion(st:SimState,fresh=false):void {
  const g=st.campaign?.progression?.gameplay;if(!g||g.region)return;
  g.region={version:1,sites:Object.fromEntries([...FIRST_CAMPS.map(s=>s.id),'freight'].map(id=>[id,{seen:false,spawned:false,guards:[]}]))};
  const core=st.campaign!.progression!.sites.find(s=>s.item==='core1')!;
  // Existing committed encounters and occupants retain access; no body or equipment is moved.
  if(!fresh&&(core.seen||core.guards!==undefined||st.engineer.x>56&&st.engineer.x<86&&st.engineer.y>39&&st.engineer.y<62)){
    g.strongholds.freight.opened=true;g.strongholds.freight.inherited=true;
    if(!g.claimed.includes('freight:legacy-access'))g.claimed.push('freight:legacy-access');
    g.region.sites.freight={seen:true,spawned:true,guards:core.guards??[]};
    core.guards??=[];
  }
  if(core.recovered)g.region.sites.freight={seen:true,spawned:true,guards:[]};
}
function candidates(st:SimState,x:number,y:number,inside:boolean):[number,number][] {
  const out:[number,number][]=[];
  for(let dy=-4;dy<=4;dy++)for(let dx=-4;dx<=4;dx++){
    const xx=x+dx,yy=y+dy;
    if(inside&&(xx<=56||xx>=85||yy<=39||yy>=61))continue;
    if(passable(st,xx,yy)&&!RIVERFRONT.yards.some(r=>xx>=r.x-2&&xx<r.x+r.w+2&&yy>=r.y-2&&yy<r.y+r.h+2)&&
      !st.flow!.machines.some(m=>['track','tramstop','tram'].includes(m.kind)&&Math.hypot(m.x-xx,m.y-yy)<3))out.push([xx,yy]);
  }
  return out.sort((a,b)=>Math.hypot(a[0]-x,a[1]-y)-Math.hypot(b[0]-x,b[1]-y)||a[1]-b[1]||a[0]-b[0]);
}
function spawnRegion(st:SimState,id:string,groups:readonly (readonly number[])[],count:number):boolean {
  const T=threatOf(st.flow!),d=st.campaign!.defence!,r=st.campaign!.progression!.gameplay!.region!.sites[id];
  const placements:{x:number;y:number;squad:number;kind:CombatKind}[]=[];
  groups.forEach(([x,y],squad)=>{
    const points=candidates(st,x,y,id==='freight'&&squad>=4),n=Math.floor(count/groups.length)+(squad<count%groups.length?1:0);
    for(const [xx,yy] of points){if(placements.filter(p=>p.squad===squad).length>=n)break;
      if(placements.some(p=>Math.hypot(p.x-xx,p.y-yy)<1.8)||T.crawlers.some(c=>Math.hypot(c.x-xx-.5,c.y-yy-.5)<1.2))continue;
      placements.push({x:xx,y:yy,squad,kind:placements.filter(p=>p.squad===squad).length%5===4?'spitter':'skitter'});
    }
  });
  if(placements.length!==count)return false;
  if(id==='freight'){const [x,y]=FREIGHT_ARENA.guardian;if(!passable(st,x,y))return false;placements.push({x,y,squad:6,kind:'guardian'});}
  // No surprise births on the engineer. Unsafe activation is postponed, with the source still unclaimed.
  if(placements.some(p=>Math.hypot(st.engineer.x-p.x-.5,st.engineer.y-p.y-.5)<12))return false;
  for(const p of placements){
    const block=blockOfTile(st,p.x,p.y),origin=p.y*ground(st).tw+p.x,group=d.nextId++;
    d.sites.push({id:group,block,tile:origin,spawned:true});
    const actor={id:T.next++,kind:'crawler' as const,x:p.x+.5,y:p.y+.5,hp:GP_COMBAT[p.kind].hp,edge:-1,from:block,to:block,cls:2 as const,onPlayer:false,escaped:false,born:st.t,stuck:0,dir:[0,0] as [number,number],
      campaign:{layer:'site' as const,group,origin},gp:newCombatActor(p.kind,id,p.squad,p.x+.5,p.y+.5)};
    T.crawlers.push(actor);r.guards.push(actor.id);T.stats.spawned++;
  }
  r.spawned=true;
  if(id==='freight')st.campaign!.progression!.sites.find(s=>s.item==='core1')!.guards=[...r.guards];
  return true;
}
export function tickFirstRegion(st:SimState):void {
  const g=st.campaign?.progression?.gameplay;if(!g?.region)return;
  for(const s of FIRST_CAMPS){const r=g.region.sites[s.id];
    if(Math.hypot(st.engineer.x-s.x,st.engineer.y-s.y)<32)r.seen=true;
    if(!r.spawned)spawnRegion(st,s.id,s.groups,s.count);
    if(s.id==='freight:camp:1'&&g.claimed.includes(s.id)){
      r.readyAt??=st.t+FABRICATION.repeatSeconds;
      const alive=r.guards.some(id=>st.flow!.threat!.crawlers.some(c=>c.id===id));
      const safe=Math.hypot(st.engineer.x-s.x,st.engineer.y-s.y)>48&&(r.watchUntil??0)<st.t&&!st.flow!.machines.some(m=>!['track','tram','tramstop'].includes(m.kind)&&s.groups.some(([x,y])=>Math.hypot(m.x-x,m.y-y)<20));
      if(!alive&&(r.rewarded??0)===(r.cycle??0)&&st.t>=r.readyAt&&safe&&st.flow!.threat!.crawlers.length+s.count<=240){const previous=[...r.guards];r.guards=[];if(spawnRegion(st,s.id,s.groups,s.count)){r.cycle=(r.cycle??0)+1;r.readyAt=st.t+FABRICATION.repeatSeconds;st.campaign!.progression!.notice='West passage reoccupied · optional shared Artifacts; its unique key remains secured';}else r.guards=previous;}
    }
  }
  const r=g.region.sites.freight;
  if(Math.hypot(st.engineer.x-66,st.engineer.y-62)<32)r.seen=true;
  if(!r.spawned)spawnRegion(st,'freight',FREIGHT_ARENA.groups,FREIGHT_ARENA.count);
}
export function regionCheck(st:SimState,a:RegionAction):string {
  const g=st.campaign?.progression?.gameplay,e=st.engineer;if(!g?.region)return 'First-region campaign required';
  if(a.type==='watchCamp')return a.id==='freight:camp:1'?'':'Unknown optional camp';
  if(e.down>=0||e.truckSeat||st.campaign!.progression!.passenger)return 'Approach on foot';
  if(a.type==='openFreight')return a.id!=='freight'?'Unknown stronghold':g.strongholds.freight.opened?'Access already permanently open':g.strongholds.freight.keys.length<3?'Recover three distinct Freight Alien Keys':
    !FREIGHT_GATES.some(p=>Math.hypot(e.x-p.x-.5,e.y-p.y-.5)<=3)?'Walk to a Freight entrance':'';
  const s=FIRST_CAMPS.find(s=>s.id===a.id),r=g.region.sites[a.id];
  if(a.type==='salvage')return !s||s.id!=='freight:camp:1'||!r?'Unknown optional salvage camp':!r.cycle||(r.rewarded??0)>=r.cycle?'No unclaimed occupation reward':Math.hypot(e.x-s.x-.5,e.y-s.y-.5)>3||!citySight(st,e.x,e.y,s.x+.5,s.y+.5)?'Walk to the salvage marker':r.guards.some(id=>st.flow!.threat!.crawlers.some(c=>c.id===id))?'Clear the reoccupied camp':take({...e,inv:{...e.inv},pack:e.pack?.map(s=>s&&{...s})},'alienartifact',2)!==2?'Make room for two Artifacts':'';
  return !s||!r?'Unknown key source':g.claimed.includes(s.id)?'Key already claimed':!r.spawned?'Camp occupation pending — return along the marked approach':
    Math.hypot(e.x-s.x-.5,e.y-s.y-.5)>3||!citySight(st,e.x,e.y,s.x+.5,s.y+.5)?'Walk to the camp key marker':r.guards.some(id=>st.flow!.threat!.crawlers.some(c=>c.id===id))?'Clear the camp defenders':'';
}
export function regionCommand(st:SimState,a:RegionAction):string {
  const why=regionCheck(st,a);if(why)return why;const g=st.campaign!.progression!.gameplay!;
  if(a.type==='watchCamp'){g.region!.sites[a.id].watchUntil=st.t+2;return '';}
  if(a.type==='salvage'){const r=g.region!.sites[a.id];take(st.engineer,'alienartifact',2);st.flow!.stats.minedOf.alienartifact=(st.flow!.stats.minedOf.alienartifact??0)+2;r.rewarded=r.cycle;r.readyAt=st.t+FABRICATION.repeatSeconds;return 'Recovered 2 shared Artifacts · one replacement Overclock’s ingredient share · unique key and knowledge unchanged';}
  if(a.type==='claimKey'){g.claimed.push(a.id);g.strongholds.freight.keys.push(a.id);return `Alien Key secured · Freight ${g.strongholds.freight.keys.length}/3 · permanent quest pouch`;}
  g.strongholds.freight.opened=true;st.flow!.rev++;return 'Freight access permanently open · defeat the guardian and defenders, then recover the core locally';
}
export function regionDiscoveries(st:SimState):DiscoveryInfo[]{
  const g=st.campaign?.progression?.gameplay;if(!g?.region)return [];
  const sites=[...FIRST_CAMPS.map(s=>({...s,type:'claimKey' as const})),{id:'freight',name:'Freight access gate',x:66,y:62,type:'openFreight' as const}];
  return sites.map(s=>{
    const r=g.region!.sites[s.id],seen=r.seen,repeat=s.id==='freight:camp:1'&&g.claimed.includes(s.id),a:RegionAction={type:repeat?'salvage':s.type,id:s.id},done=s.type==='claimKey'?g.claimed.includes(s.id):g.strongholds.freight.opened;
    const remaining=r.guards.filter(id=>st.flow!.threat!.crawlers.some(c=>c.id===id)).length;
    return {id:`gp:${s.id}`,title:seen?s.name:`${s.type==='claimKey'?'Key camp':'Freight'} search area`,defaultTitle:s.name,x:seen?s.x:Math.round(s.x/20)*20,y:seen?s.y:Math.round(s.y/20)*20,size:1,
      status:repeat?`Key secured · ${r.cycle&&(r.rewarded??0)<r.cycle?remaining+' defenders · 2 Artifacts available after clearance':'optional reoccupation in '+Math.max(0,Math.ceil(((r.readyAt??st.t+FABRICATION.repeatSeconds)-st.t)/60))+' active min; postponed while observed or built on'}`:done?s.type==='claimKey'?'Key secured':'Access permanently open':!seen?'Scout this broad area':s.type==='claimKey'?`${remaining} defenders remain`:`${g.strongholds.freight.keys.length}/3 distinct keys`,
      detail:s.type==='claimKey'?'One Alien Key in the quest pouch. Clear separated patrol groups; use cover and retreat to reload. No repeat key rewards.':'Three exterior keys open both entrances permanently. A charging guardian and finite defenders protect the separate portable core.',needs:[],
      actions:seen&&(!done||repeat)?[{label:repeat?'Recover occupation Artifacts':s.type==='claimKey'?'Claim Alien Key':'Unlock Freight',reason:regionCheck(st,a),commands:[{type:'region',action:a}]}]:[]};
  });
}
export function firstRegionProblem(st:SimState):string {
  const g=st.campaign?.progression?.gameplay,r=g?.region;if(r===undefined)return '';
  const ids=[...FIRST_CAMPS.map(s=>s.id),'freight'];
  if(!r||r.version!==1||!r.sites||Object.keys(r.sites).length!==ids.length)return 'Invalid first-region sites';
  for(const id of ids){const s=r.sites[id];if(s&&[s.cycle,s.rewarded,s.readyAt,s.watchUntil].some(n=>n!==undefined&&(!Number.isSafeInteger(n)||n<0))||(s?.rewarded??0)>(s?.cycle??0))return 'Invalid repeatable occupation';if(!s||typeof s.seen!=='boolean'||typeof s.spawned!=='boolean'||!Array.isArray(s.guards)||new Set(s.guards).size!==s.guards.length||s.guards.some(n=>!Number.isSafeInteger(n)||n<1||n>=st.flow!.threat!.next)||!s.spawned&&s.guards.length)return 'Invalid camp occupation';}
  for(const c of st.flow!.threat!.crawlers)if(c.gp&&ids.includes(c.gp.source)&&!r.sites[c.gp.source].guards.includes(c.id))return 'Orphaned camp defender';
  return '';
}
/** Real collision and outside-gate reachability; intended for focused authored-content checks. */
export function validateFirstRegion(st:SimState):string[]{
  const errors:string[]=[];const e=st.engineer;
  for(const s of FIRST_CAMPS){if(!passable(st,s.x,s.y))errors.push(`${s.id}: key marker solid`);if(!findPath(st,Math.floor(e.x),Math.floor(e.y),s.x,s.y))errors.push(`${s.id}: no exterior Home path`);}
  for(const p of FREIGHT_GATES)for(let y=p.y;y<p.y+p.h;y++)for(let x=p.x;x<p.x+p.w;x++)if(!ground(st).urban||ground(st).urban!.solid[y*ground(st).tw+x])errors.push(`Gate ${x},${y}: not an existing door`);
  return errors;
}
