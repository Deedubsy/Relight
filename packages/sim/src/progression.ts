import {regionDiscoveries,tickFirstRegion} from './firstRegion';
import {recordCoreRecovery} from './gameplayProgress';
import {itemName} from './itemNames';
import {riverfrontTramPose} from './city/riverfrontRail';
/** Owner-authorised audit corrections. All numeric values here are initial implementation defaults. */
import type {SimState} from './types';
import {ground,blockOfTile,inReach,citySight} from './ground';
import {passable,findPath} from './walk';
import {take,drop,hurt} from './engineer';
import {placementGeometryProblem,isProcessor,recipesFor,litAt,machineRunning,type Item,type Machine} from './flow';
import {baseCore,nominateBase,DEFENCE} from './campaignDefence';
import {campaignThrottle,campaignGrid} from './campaignPower';
import type {DiscoveryInfo} from './campaignGuide';
import {campaignRecruited} from './campaignRecruits';
import {campaignOrigin} from './campaignThreat';
import {heldItems} from './ledger';
export const CORRECTIONS={version:1,openingMinorSlots:[900,1080],plants:3,plantKw:600,coreSearchRadius:28,discoverRadius:14,artifactSpeed:1.1,hopperIncrease:1.25,freightIncrease:1.25,plantCost:{steel:30,copper:15},upgradeCost:{steel:20,copper:10},resourceUnits:12000,encounterKw:100,encounterSeconds:[30,40,50],encounterCost:{steel:30,copper:15},alienEquipmentRange:7,alienEquipmentDps:2,cannon:{range:12,damage:50,seconds:2,capacity:20},breakerHp:100,conductorHp:80,shadeHp:40,reinforcementSeconds:8,reinforcementLimit:8} as const;
export interface ProgressionSite {id:string;kind:'core'|'plant'|'artifact'|'heart'|'furnace'|'crown';name:string;x:number;y:number;size:number;block:number;seen:boolean;item?:Item;recovered:boolean;enabled:boolean;installed?:Item;delivered:Record<string,number>;restoredAt:number;guards?:number[];progress:number;started:boolean;waves:number;attempt:number;searchX:number;searchY:number}
export interface ResourceSite {x:number;y:number;block:number;item:Item;remaining:number}
export interface Progression {version:1;sites:ProgressionSite[];resources:ResourceSite[];arsenal:boolean;freightUpgrade:boolean;notice:string;passenger?:{tram:number};gameplay?:import('./gameplayProgress').GameplayProgress;}
export type ProgressionAction={type:'recover'|'deliver'|'activate'|'toggle'|'start'|'abort';id:string}|{type:'attach';machine:number;item:Item}|{type:'detach'|'hopper';machine:number}|{type:'freight'}|{type:'board'}|{type:'exit'};
export const progressionAt=(st:SimState,x:number,y:number)=>st.campaign?.progression?.sites.find(s=>x>=s.x&&x<s.x+s.size&&y>=s.y&&y<s.y+s.size);
export function initProgression(st:SimState):void {
 if(!st.campaign||!st.flow||st.campaign.progression)return;
 const sites:ProgressionSite[]=[],resources:ResourceSite[]=[],stops=st.campaign.fixedTram!.stops.map(id=>st.flow!.machines.find(m=>m.id===id)!).filter(s=>blockOfTile(st,s.x,s.y)!==st.campaign!.homeBlock);
 // Deterministic safe additions to old saves: never move/remove an existing structure.
 const occupied=(x:number,y:number)=>resources.some(s=>Math.abs(s.x-x)<6&&Math.abs(s.y-y)<6)||sites.some(s=>Math.abs(s.x-x)<s.size+5&&Math.abs(s.y-y)<s.size+5);
 const locate=(cx:number,cy:number,block?:number):{x:number;y:number;block:number}=>{
  for(let radius=5;radius<130;radius++)for(let dy=-radius;dy<=radius;dy++)for(const dx of [-radius,radius]){
   const x=Math.floor(cx+dx),y=Math.floor(cy+dy),bi=blockOfTile(st,x,y);
   if(bi<0||bi===st.campaign!.homeBlock||(block!==undefined&&block!==bi)||occupied(x,y))continue;
   if(!placementGeometryProblem(st,'chest',x,y)&&!placementGeometryProblem(st,'chest',x+1,y+1)&&passable(st,x-1,y)&&findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),x-1,y))return {x,y,block:bi};
  }
  throw new Error('No safe space for campaign additions; existing save left unchanged');
 };
 const add=(kind:ProgressionSite['kind'],name:string,cx:number,cy:number,item?:Item,block?:number)=>{
  const p=locate(cx,cy,block),n=sites.length,s:ProgressionSite={...p,id:`regional:${kind}:${n}`,kind,name,size:3,seen:kind==='plant',item,recovered:false,enabled:true,delivered:{},restoredAt:-1,progress:0,started:false,waves:0,attempt:0,searchX:p.x+9,searchY:p.y-7};sites.push(s);return s;
 };
 for(let i=0;i<CORRECTIONS.plants;i++){
  const stop=stops[i%stops.length];add('plant',`Regional plant ${i+1}`,stop.x,stop.y,undefined,blockOfTile(st,stop.x,stop.y));
  const gx=stop.x+20+i*12,gy=stop.y-22-i*7;
  add('core',`Alien relay ${i+1}`,gx,gy,`core${i+1}` as Item);
  add('artifact',`Machine artifact cache ${i+1}`,stop.x+15,stop.y+15,`artifact${i+1}` as Item);
  for(const item of ['ironore','copperore','crude'] as Item[]){const p=locate(stop.x+6+resources.length*2,stop.y+9);resources.push({...p,item,remaining:CORRECTIONS.resourceUnits});}
 }
 const end=stops.at(-1)!;add('heart','Junction Heart / Arsenal',stops[0].x+15,stops[0].y+10);add('furnace','Furnace Walker',end.x+25,end.y);add('crown','Blackout Crown',end.x,end.y+25);
 st.campaign.progression={version:1,sites,resources,arsenal:false,freightUpgrade:false,notice:''};
}
export function processingMultiplier(m:Machine):number{return (m.kind==='assembler2'?2:1)*(m.artifact?CORRECTIONS.artifactSpeed:1);}
export const hopperCapacity=(m:Machine,base:number)=>Math.floor(base*(m.hopperUpgrade?CORRECTIONS.hopperIncrease:1));
export const freightCapacity=(st:SimState,base:number)=>Math.floor(base*(st.campaign?.progression?.freightUpgrade?CORRECTIONS.freightIncrease:1));
function local(st:SimState,s:{x:number;y:number;size:number}):string{return st.engineer.down>=0||st.engineer.truckSeat||st.campaign?.progression?.passenger||!inReach(st,s.x,s.y,s.size)?'Walk closer on foot':'';}
function costOf(s:ProgressionSite):Record<string,number>{return s.kind==='plant'?CORRECTIONS.plantCost:CORRECTIONS.encounterCost;}
const missing=(s:ProgressionSite)=>Object.entries(costOf(s)).filter(([k,n])=>(s.delivered[k]??0)<n);
function paid(st:SimState,cost:Record<string,number>):boolean {
 if(Object.entries(cost).some(([k,n])=>(st.engineer.inv[k]??0)<n))return false;
 for(const [k,n] of Object.entries(cost)){drop(st.engineer,k,n);st.flow!.stats.consumed[k as Item]=(st.flow!.stats.consumed[k as Item]??0)+n;}return true;
}
function passengerExit(st:SimState):string {
 const p=st.campaign!.progression!,e=st.engineer;
 if(!p.passenger)return 'Not riding the tram';
 for(let r=1;r<=8;r++)for(let dy=-r;dy<=r;dy++)for(const dx of [-r,r]){const x=Math.floor(e.x)+dx,y=Math.floor(e.y)+dy;if(passable(st,x,y)){e.x=x+.5;e.y=y+.5;e.block=blockOfTile(st,x,y);delete p.passenger;e.vel=[0,0];e.target=null;return '';}}
 return 'No safe exit beside the tram; continue to a clear stop';
}
export function progressionCheck(st:SimState,a:ProgressionAction):string {
 const p=st.campaign?.progression;if(!p)return 'Current campaign required';
 if(a.type==='exit')return p.passenger?'':'Not riding the tram';
 if(a.type==='board'){const m=st.flow!.machines.find(m=>m.id===st.campaign!.fixedTram?.tram);return !m?'Tram unavailable':p.passenger?'Already aboard':local(st,m)|| (m.phase===0?'Wait for the tram to stop':'');}
 if(a.type==='freight')return !campaignRecruited(st,'railcrew')?'Recruit the Rail crew':p.freightUpgrade?'Freight upgrade already installed':Object.entries(CORRECTIONS.upgradeCost).some(([k,n])=>(st.engineer.inv[k]??0)<n)?'Needs 20 steel + 10 copper':'';
 if('machine' in a){const m=st.flow!.machines.find(m=>m.id===a.machine);if(!m)return 'Machine unavailable';const why=local(st,m);if(why)return why;
  if(a.type==='hopper')return m.kind!=='turret'?'Select a gun turret':!campaignRecruited(st,'gunsmith')?'Recruit the Gunsmith':m.hopperUpgrade?'Hopper upgrade already installed':Object.entries(CORRECTIONS.upgradeCost).some(([k,n])=>(st.engineer.inv[k]??0)<n)?'Needs 20 steel + 10 copper':'';
  if(!isProcessor(m)&&m.kind!=='excavator'&&m.kind!=='pumpjack')return 'This machine has no module slot';
  if(a.type==='attach')return m.artifact?'Module slot occupied':!(p.gameplay?a.item==='overclock':a.item.startsWith('artifact'))||(st.engineer.inv[a.item]??0)<1?'Carry an Overclock Module':'';
  return !m.artifact?'Module slot empty':take({...st.engineer,inv:{...st.engineer.inv}},m.artifact,1)!==1?'Backpack full':'';
 }
 const s=p.sites.find(s=>s.id===a.id);if(!s)return 'Unknown site';const why=local(st,s);if(why)return why;
 if(a.type==='recover'&&s.item==='core1'&&p.gameplay?.region&&!p.gameplay.strongholds.freight.opened)return 'Unlock Freight with three distinct exterior keys first';
 if(a.type==='recover'&&p.gameplay&&s.kind==='artifact'&&!s.recovered){const trial={...st.engineer,inv:{...st.engineer.inv},pack:st.engineer.pack?.map(s=>s&&{...s})};return take(trial,'alienartifact',2)!==2?'Make room for two Artifacts; source remains unclaimed':'';}
 if(a.type==='recover')return !s.item?'No portable reward here':s.recovered?'Already recovered':s.kind==='core'&&!s.guards?'Locate the relay defenders before extracting':s.guards?.some(id=>st.flow!.threat?.crawlers.some(c=>c.id===id))?'Defeat the relay defenders first':take({...st.engineer,inv:{...st.engineer.inv}},s.item,1)!==1?'Backpack full; reward remains at the source':'';
 if(a.type==='toggle')return s.kind!=='plant'||!s.installed?'Plant is not commissioned':'';
 if(a.type==='abort')return s.started?'':'No encounter running';
 if(a.type==='deliver')return s.restoredAt>=0?'Already restored':s.kind==='core'||s.kind==='artifact'?'Nothing to deliver here':'';
 if(a.type==='activate')return s.kind!=='plant'?'Use the encounter controls':s.installed?'Already commissioned':missing(s).length?'Deliver the displayed materials':!['core1','core2','core3'].some(k=>(st.engineer.inv[k]??0)===1)?'Carry a recovered power core':'';
 if(!['heart','furnace','crown'].includes(s.kind))return 'Not an engineering encounter';if(s.restoredAt>=0)return 'Encounter already completed';if(s.started)return 'Encounter already running';if(missing(s).length)return 'Deliver the displayed materials';if(campaignThrottle(st,s.block)<=0)return 'Connect local power';if(s.kind!=='heart'&&!p.arsenal)return 'Complete the Junction Heart first';return '';
}
export function progressionCommand(st:SimState,a:ProgressionAction):string {
 const p=st.campaign?.progression;if(!p)return 'Current campaign required';const why=progressionCheck(st,a);if(why){p.notice=why;return why;}
 const e=st.engineer;
 if(a.type==='exit')return p.notice=passengerExit(st);
 if(a.type==='board'){const m=st.flow!.machines.find(m=>m.id===st.campaign!.fixedTram!.tram)!;p.passenger={tram:m.id};e.vel=[0,0];e.target=null;e.aim=null;const pose=st.city?.mapId?riverfrontTramPose(m):{x:m.x+.5,y:m.y+.5};e.x=pose.x;e.y=pose.y;return p.notice='Aboard tram · E to exit';}
 if(a.type==='freight'){paid(st,CORRECTIONS.upgradeCost);p.freightUpgrade=true;return p.notice='Freight capacity increased by 25%';}
 if('machine' in a){const m=st.flow!.machines.find(m=>m.id===a.machine)!;if(a.type==='hopper'){paid(st,CORRECTIONS.upgradeCost);m.hopperUpgrade=true;}else if(a.type==='attach'){drop(e,a.item,1);m.artifact=a.item;}else{take(e,m.artifact!,1);delete m.artifact;}return p.notice='Machine upgrade updated';}
 const s=p.sites.find(s=>s.id===a.id)!;s.seen=true;
 if(a.type==='recover'){const optional=p.gameplay&&s.kind==='artifact',reward=optional?'alienartifact':s.item!,count=optional?2:1;take(e,reward,count);if(optional&&!p.gameplay!.claimed.includes(s.id)){p.gameplay!.claimed.push(s.id);if(s.id==='artifact:workshop'&&!p.gameplay!.recoveredSchematics.includes('overclock'))p.gameplay!.recoveredSchematics.push('overclock');}s.recovered=true;s.enabled=false;recordCoreRecovery(st,s.item!);st.flow!.rev++;st.flow!.stats.minedOf[reward]=(st.flow!.stats.minedOf[reward]??0)+count;return p.notice=optional?'Recovered 2 shared Artifacts; any secured schematic stays in your quest knowledge. Decode at a powered Alien workbench after commissioning a plant.':`Recovered ${itemName(reward)}; source equipment offline`;}
 if(a.type==='deliver'){for(const [k,n] of missing(s)){const moved=Math.min(n-(s.delivered[k]??0),e.inv[k]??0);drop(e,k,moved);s.delivered[k]=(s.delivered[k]??0)+moved;}return p.notice='Carried materials delivered';}
 if(a.type==='toggle'){s.enabled=!s.enabled;st.flow!.rev++;return p.notice=s.enabled?'Plant output enabled':'Plant output off · still an attack target';}
 if(a.type==='abort'){s.started=false;return p.notice='Encounter paused; materials and progress retained';}
 if(a.type==='activate'){
  const k=['core1','core2','core3'].find(k=>(e.inv[k]??0)===1)! as Item;drop(e,k,1);s.installed=k;s.restoredAt=st.t;if(p.gameplay&&!p.gameplay.commissioned.includes(s.id))p.gameplay.commissioned.push(s.id);
  let health=baseCore(st,s.block);if(!health){health={block:s.block,x:s.x,y:s.y,size:s.size,hp:DEFENCE.coreHp,commissionedAt:st.t};st.campaign!.defence!.bases.push(health);}else{health.x=s.x;health.y=s.y;health.size=s.size;}
  nominateBase(st,s.block);st.flow!.rev++;return p.notice='Plant commissioned · finite regional power and defended factory site';
 }
 s.started=true;s.attempt++;return p.notice='Engineering encounter started · keep power and defend the installation';
}
export function spawnSiteEnemy(st:SimState,s:ProgressionSite,kind:'crawler'|'shade'='crawler',role?:'breaker'|'conductor'):number {
 const T=st.flow!.threat!,G=ground(st);let x=s.x-2,y=s.y;for(let dy=-4;dy<=4;dy++)if(passable(st,s.x-3,s.y+dy)){x=s.x-3;y=s.y+dy;break;}
 if(s.kind&&['heart','furnace','crown'].includes(s.kind)){const origin=campaignOrigin(st,{...s,hp:300,commissionedAt:st.t});if(origin<0)return -1;x=origin%G.tw;y=Math.floor(origin/G.tw);}
 if(st.city?.mapId){let found=false;for(let r=0;r<8&&!found;r++)for(let dy=-r;dy<=r&&!found;dy++)for(let dx=-r;dx<=r;dx++){const xx=x+dx,yy=y+dy;if(!passable(st,xx,yy)||T.crawlers.some(c=>Math.hypot(c.x-xx-.5,c.y-yy-.5)<1))continue;if(role==='breaker'&&[-1,0,1].some(a=>[-1,0,1].some(b=>!passable(st,xx+a,yy+b))))continue;x=xx;y=yy;found=true;break;}if(!found)return -1;}
 const id=T.next++;const group=1000+id;st.campaign!.defence!.sites.push({id:group,block:s.block,tile:y*G.tw+x,spawned:true});T.crawlers.push({id,kind,x:x+.5,y:y+.5,hp:role==='breaker'?CORRECTIONS.breakerHp:role==='conductor'?CORRECTIONS.conductorHp:kind==='shade'?CORRECTIONS.shadeHp:12,edge:-1,from:s.block,to:s.block,cls:2,onPlayer:false,escaped:false,born:st.t,stuck:0,role,campaign:{layer:'site',group:1000+id,origin:y*G.tw+x,...(s.kind&&['heart','furnace','crown'].includes(s.kind)?{encounter:s.id}:{})}});T.stats.spawned++;return id;
}
/** This exact predicate owns both relay damage and its visible ground cue. */
export function relayDangerAt(st:SimState,s:ProgressionSite,x:number,y:number):boolean {
 return s.kind==='core'&&!s.recovered&&s.enabled&&Math.hypot(x-s.x,y-s.y)<CORRECTIONS.alienEquipmentRange&&citySight(st,x,y,s.x+1.5,s.y+1.5);
}
export const encounterDuration=(s:ProgressionSite)=>CORRECTIONS.encounterSeconds[s.kind==='heart'?0:s.kind==='furnace'?1:2];
/** Productive-time conditions shared by the simulation and every project presentation. */
export function encounterBlocker(st:SimState,s:ProgressionSite):string {
 if(campaignThrottle(st,s.block)<=0)return 'Connect local power';
 if(s.guards?.some(id=>st.flow!.threat?.crawlers.some(c=>c.id===id)))return 'Defeat the remaining defenders';
 const nearby=st.flow!.machines.filter(m=>machineRunning(st,m)&&Math.hypot(m.x-s.x,m.y-s.y)<=14);
 if(s.kind==='heart'){
  const feeds=nearby.filter(m=>m.kind==='pole'&&(campaignGrid(st).poles.get(m.id)?.supply??0)>0);
  const sides=[!feeds.some(m=>m.x<s.x)?'west':'',!feeds.some(m=>m.x>=s.x+s.size)?'east':''].filter(Boolean);
  if(sides.length)return `Connect powered feeder poles on the ${sides.join(' and ')} side (within 14 tiles)`;
 }
 if(s.kind==='furnace'){const n=nearby.filter(m=>isProcessor(m)&&m.busy).length;if(n<2)return `Keep two processors producing within 14 tiles (${n}/2 working)`;}
 if(s.kind==='crown'){const n=nearby.filter(m=>['lamp','floodlight','arclamp'].includes(m.kind)).length;if(n<2)return `Power two lights within 14 tiles (${n}/2 powered)`;}
 return '';
}
export function progressionReward(s:ProgressionSite):string {
 return s.kind==='heart'?'Unlock the Arsenal: two-barrel rifle and Cannon/Shell plans.':s.kind==='furnace'?'50 Steel plates delivered to Home storage.':s.kind==='crown'?'50 Copper delivered to Home storage.':s.kind==='artifact'?'+10% processing capacity on one machine; supplies and output space still limit production.':s.kind==='plant'?'600 kW finite regional supply; this plant becomes an attack target. Any recovered power core fits.':'Recover one portable power core for a regional plant. Shades need powered lighting to become vulnerable. Bring a Lamp, Generator and fuel; connect the lamp to power. Your flashlight helps you see but does not expose Shades.';
}
export function progressionStatus(st:SimState,s:ProgressionSite):string {
 if(s.kind==='core')return s.recovered?'Core recovered · relay offline':'Powered alien relay · clear defenders';
 if(s.kind==='artifact')return s.recovered?'Optional source recovered':st.campaign?.progression?.gameplay?'Optional shared Artifacts'+(s.id==='artifact:workshop'?' + Overclock schematic':''):'Optional speed artifact';
 if(s.kind==='plant'&&s.installed)return baseCore(st,s.block)?.hp===0?'Damaged · repair preserves installed core':s.enabled?'Operating · 600 kW · attack target':'Offline · still attack target';
 if(s.restoredAt>=0)return 'Completed';
 const needs=missing(s);if(needs.length)return 'Needs materials: '+needs.map(([item,n])=>`${n-(s.delivered[item]??0)} ${itemName(item)}`).join(' + ');
 if(s.kind==='plant')return ['core1','core2','core3'].some(k=>(st.engineer.inv[k]??0)>0)?'Ready · install your carried core':'Materials delivered · carry a recovered power core';
 const progress=`${Math.floor(s.progress)} / ${encounterDuration(s)} productive seconds`;
 const blocker=s.kind!=='heart'&&!st.campaign!.progression!.arsenal?'Complete the Junction Heart first':encounterBlocker(st,s);
 return blocker?`${s.started?'Stalled':'Needs preparation'} · ${progress} · ${blocker}`:`${s.started?'Running':s.progress>0?'Paused · ready to resume':'Ready to start'} · ${progress}`;
}
export function tickProgression(st:SimState,dt:number):void {
 tickFirstRegion(st);
 const p=st.campaign?.progression;if(!p)return;const e=st.engineer;
 for(const m of st.flow!.machines)if(m.kind==='cannon'&&machineRunning(st,m)){m.timer=Math.max(0,m.timer-dt);if(m.timer<=0&&(m.inv.shell??0)>0){const target=st.flow!.threat?.crawlers.filter(c=>c.hp>0&&citySight(st,m.x+1,m.y+1,c.x,c.y)&&(c.kind!=='shade'||litAt(st,Math.floor(c.x),Math.floor(c.y)))&&Math.hypot(c.x-m.x-1,c.y-m.y-1)<=CORRECTIONS.cannon.range).sort((a,b)=>b.hp-a.hp)[0];if(target){m.inv.shell--;m.timer=CORRECTIONS.cannon.seconds;st.flow!.stats.consumed.shell=(st.flow!.stats.consumed.shell??0)+1;target.hp-=CORRECTIONS.cannon.damage;if(target.hp<=0)st.flow!.threat!.crawlers.splice(st.flow!.threat!.crawlers.indexOf(target),1);}}}

 for(const s of p.sites){if(!s.seen&&citySight(st,e.x,e.y,s.x+1.5,s.y+1.5)&&Math.hypot(e.x-s.x,e.y-s.y)<CORRECTIONS.discoverRadius)s.seen=true;
  if(s.kind==='core'&&s.seen&&!s.guards&&!(s.item==='core1'&&p.gameplay?.region)){s.guards=[spawnSiteEnemy(st,s),spawnSiteEnemy(st,s,s.item==='core1'?'crawler':'shade')].filter(id=>id>=0);}
  if(e.down<0&&relayDangerAt(st,s,e.x,e.y))hurt(st,CORRECTIONS.alienEquipmentDps*dt,`relay:${s.id}`);
  if(!s.started||s.restoredAt>=0)continue;
  const index=s.kind==='heart'?0:s.kind==='furnace'?1:2,duration=CORRECTIONS.encounterSeconds[index];
  if(encounterBlocker(st,s))continue;
  s.progress=Math.min(duration,s.progress+dt);
  const stage=Math.min(3,Math.floor(s.progress/duration*4));
  if(stage>s.waves){s.waves=stage;s.guards=[spawnSiteEnemy(st,s,'crawler',index===1?'breaker':index===2?'conductor':undefined),spawnSiteEnemy(st,s,index===2?'shade':'crawler')].filter(id=>id>=0);if(!s.guards.length){s.waves--;s.started=false;p.notice='No safe encounter approach; clear a route and resume';}}
  if(s.progress>=duration&&!s.guards?.some(id=>st.flow!.threat?.crawlers.some(c=>c.id===id))){s.started=false;s.restoredAt=st.t;if(s.kind==='heart'){p.arsenal=true;e.barrels=2;}else{const item=s.kind==='furnace'?'steel':'copper';st.stock[item]+=50;st.flow!.stats.minedOf[item]+=50;}p.notice=`${s.name} completed · ${progressionReward(s)}`;
  }
 }
 if(p.passenger){const m=st.flow!.machines.find(m=>m.id===p.passenger!.tram);if(!m){passengerExit(st);return;}const pose=st.city?.mapId?riverfrontTramPose(m):{x:m.x+.5,y:m.y+.5};e.x=pose.x;e.y=pose.y;e.block=blockOfTile(st,e.x,e.y);e.vel=[0,0];e.target=null;e.aim=null;if(e.down>=0)passengerExit(st);}
}
export function progressionDiscoveries(st:SimState):DiscoveryInfo[]{
 const p=st.campaign?.progression;if(!p)return [];
 return [...regionDiscoveries(st),...p.sites.filter(s=>s.seen||s.kind==='core').map(s=>{
  const hidden=s.kind==='core'&&!s.seen,actions:ProgressionAction[]=hidden||s.recovered?[]:s.kind==='core'||s.kind==='artifact'?[{type:'recover',id:s.id}]:s.kind==='plant'?s.installed?[{type:'toggle',id:s.id}]:[{type:'deliver',id:s.id},{type:'activate',id:s.id}]:s.started?[{type:'abort',id:s.id}]:s.restoredAt<0?[{type:'deliver',id:s.id},{type:'start',id:s.id}]:[];
  const label={recover:'Recover',deliver:'Deliver carried materials',activate:'Install carried core',toggle:'Toggle output (remains defended)',start:'Start / resume engineering encounter',abort:'Pause encounter'};
  const status=hidden?'Search within the marked area':progressionStatus(st,s);
  const direction=`${s.searchY<st.city!.th/2?'North':'South'}${s.searchX<st.flow!.tw/2?'west':'east'}`;
  const detail=p.gameplay&&s.kind==='artifact'?'Recover 2 shared Artifacts'+(s.id==='artifact:workshop'?' and a secured Overclock schematic':'')+'. Schematics are permanent knowledge; decode at a powered Alien workbench (M2: first commissioned plant), then craft removable modules with ordinary components. No optional reward is needed for Freight.':p.gameplay&&s.item==='core1'?`Freight: ${p.gameplay.strongholds.freight.keys.length}/3 distinct exterior keys · ${p.gameplay.strongholds.freight.opened?'access permanently open':'both entrances locked'}. Clear its finite defenders and charging guardian; use solid cargo cover and dodge the marked charge. Recover the core separately at the relay.`:hidden?`Scout within ${CORRECTIONS.coreSearchRadius} tiles. Exact relay remains unknown. Prepare powered lighting for relay defenders; the flashlight does not expose Shades.`:(['heart','furnace','crown','artifact'].includes(s.kind)?s.restoredAt>=0||s.recovered?'Reward received: ':'Reward: ':'')+progressionReward(s)+(s.kind==='heart'?' Connect a powered feeder pole on each side; 100 kW commissioning load.':s.kind==='furnace'?' Keep two nearby processors busy through the defence.':s.kind==='crown'?' Keep two nearby powered lights on and eliminate Conductor sources.':'');
  return {id:s.id,title:hidden?`${direction} core search area`:s.name,defaultTitle:s.name,x:hidden?s.searchX:s.x,y:hidden?s.searchY:s.y,size:hidden?1:s.size,status,detail,needs:s.kind==='core'||s.kind==='artifact'||s.restoredAt>=0?[]:Object.entries(costOf(s)).map(([item,required])=>({item,required,delivered:s.delivered[item]??0})),actions:actions.map(a=>({label:label[a.type as keyof typeof label],reason:progressionCheck(st,a),commands:[{type:'progression' as const,action:a}]}))};
 })];
}
export function progressionProblem(st:SimState):string {
 const p=st.campaign?.progression;if(!p)return '';
 const int=(n:unknown,min=0)=>typeof n==='number'&&Number.isSafeInteger(n)&&n>=min;
 const finite=(n:unknown,min=0)=>typeof n==='number'&&Number.isFinite(n)&&n>=min;
 const position=(s:{x:number;y:number;block:number},size=1)=>int(s.x)&&int(s.y)&&s.x+size<=st.flow!.tw&&s.y+size<=st.city!.th&&int(s.block)&&!!st.blocks[s.block]&&blockOfTile(st,s.x,s.y)===s.block;
 if(p.version!==1||!Array.isArray(p.sites)||!Array.isArray(p.resources)||typeof p.arsenal!=='boolean'||typeof p.freightUpgrade!=='boolean'||typeof p.notice!=='string'||new Set(p.sites.map(s=>s?.id)).size!==p.sites.length)return 'Invalid regional progression records';
 const rewards=new Set<string>();
 for(const s of p.sites){
  if(!s||typeof s.id!=='string'||typeof s.name!=='string'||!['core','plant','artifact','heart','furnace','crown'].includes(s.kind)||s.size!==3||!position(s,3)||!finite(s.progress)||s.progress>50||!finite(s.restoredAt,-1)||!int(s.attempt)||!int(s.waves)||s.waves>3||!int(s.searchX)||!int(s.searchY)||[s.seen,s.recovered,s.enabled,s.started].some(v=>typeof v!=='boolean')||!s.delivered||typeof s.delivered!=='object'||Array.isArray(s.delivered)||Object.entries(s.delivered).some(([k,n])=>!Object.keys(costOf(s)).includes(k)||!int(n)||n>costOf(s)[k]))return 'Invalid regional site';
  if(s.item){if(!/^(core|artifact)[123]$/.test(s.item)||!s.item.startsWith(s.kind)||rewards.has(s.item))return 'Invalid reward source';rewards.add(s.item);}
  else if(s.kind==='core'||s.kind==='artifact')return 'Missing reward source';
  if(s.installed&&(s.kind!=='plant'||!/^core[123]$/.test(s.installed)||s.restoredAt<0))return 'Invalid installed core';
  if(s.kind==='plant'&&(!!s.installed!==(s.restoredAt>=0))||s.recovered&&s.enabled||s.started&&(!['heart','furnace','crown'].includes(s.kind)||s.restoredAt>=0)||s.guards!==undefined&&(!Array.isArray(s.guards)||s.guards.some(id=>!int(id,1)||id>=st.flow!.threat!.next)||new Set(s.guards).size!==s.guards.length))return 'Invalid site lifecycle';
 }
 for(const r of p.resources)if(!r||!position(r,3)||!['ironore','copperore','crude',...(st.city?.mapId?['coal','stone']:[])].includes(r.item)||!int(r.remaining)||r.remaining>CORRECTIONS.resourceUnits)return 'Invalid regional resource';
 for(const m of st.flow!.machines){
  if(m.artifact&&(!(p.gameplay?m.artifact==='overclock':/^artifact[123]$/.test(m.artifact)&&rewards.has(m.artifact))||!isProcessor(m)&&m.kind!=='excavator'&&m.kind!=='pumpjack'))return 'Invalid machine artifact';
  if(m.hopperUpgrade!==undefined&&(typeof m.hopperUpgrade!=='boolean'||m.kind!=='turret'))return 'Invalid hopper upgrade';
  if(['foundry','refinery','assembler2'].includes(m.kind)&&(!m.recipe||!recipesFor(m).includes(m.recipe)))return 'Invalid processor recipe';
 }
 const total=heldItems(st).total;
 for(const s of p.sites)if(s.item&&(total[s.item]??0)!==(s.recovered&&!(p.gameplay?.upgradesMigrated&&s.kind==='artifact')?1:0))return `Invalid unique reward ownership: ${s.item}`;
 if(p.passenger&&(!int(p.passenger.tram,1)||st.engineer.truckSeat||!st.flow!.machines.some(m=>m.kind==='tram'&&m.id===p.passenger!.tram)))return 'Invalid tram passenger';
 return '';
}
