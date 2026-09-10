/** P6-03: ordinary-stock, live-clock defence workload. The sim owns every gameplay transition. */
import { createCampaign, ground, placeable, blockOfTile, rubbleAt, conservation, stateHash,
 campaignSite, siteCheck, baseCore, defenceHp, defenceMax, repairCheck, knownCampaignThreat, campaignWarning,
 stationRoute, invTotal, type SimState, type Machine, type BaseCore } from '@relight/sim';
import { FactoryDriver, buildBlueprint, parseBlueprint, replayInterval, savedContinuation } from './blueprint';
import { power, restore, configure, moveCargo, extract } from './factoryScenario';

export interface DefenceCheck {name:string;pass:boolean;detail:unknown}
export interface DefenceRun {seed:number;mode:'turrets'|'support';setup:string;checks:DefenceCheck[];measurements:Record<string,unknown>;initialState:SimState;finalState:SimState;log:FactoryDriver['log']}
interface Pod {turret:Machine;chest:Machine;arm:Machine;block:number}
export class DefenceDriver extends FactoryDriver {
 readonly timeline:Record<string,unknown>[]=[];last='';phase='opening';
 readonly raids:{id:number;block:number;start:number;end:number;coreBefore:number;coreAfter:number;nearestEngineer:number;roundsBefore:number;roundsSpent:number;rifleBefore:number;rifleShots:number}[]=[];
 awaySeconds=0;lastObservedTick=0;

 override run(seconds:number):void {
  for(let left=seconds;left>1e-8;left-=Math.min(1,left)) {super.run(Math.min(1,left));this.observe();}
 }
 observe():void {
  const s=this.state,d=s.campaign!.defence!,a=d.minor;
  if(blockOfTile(s,Math.floor(s.engineer.x),Math.floor(s.engineer.y))!==s.campaign!.homeBlock)this.awaySeconds+=(s.flow!.tick-this.lastObservedTick)/20;this.lastObservedTick=s.flow!.tick;
  for(const row of this.raids)if(row.end<0&&row.id!==a?.id){row.end=s.t;row.coreAfter=baseCore(s,row.block)!.hp;row.roundsSpent=s.flow!.stats.fired-row.roundsBefore;row.rifleShots=s.engineer.fired-row.rifleBefore;}
  if(a){const core=baseCore(s,a.block)!,distance=Math.hypot(s.engineer.x-core.x-core.size/2,s.engineer.y-core.y-core.size/2);let row=this.raids.find(r=>r.id===a.id);
   if(!row){row={id:a.id,block:a.block,start:s.t,end:-1,coreBefore:core.hp,coreAfter:core.hp,nearestEngineer:distance,roundsBefore:s.flow!.stats.fired,roundsSpent:0,rifleBefore:s.engineer.fired,rifleShots:0};this.raids.push(row);}row.nearestEngineer=Math.min(row.nearestEngineer,distance);
  }
  const key=JSON.stringify([d.major?.id,d.major?.retreat,d.major?.remaining===0,d.minor?.id,d.minor?.retreat,d.bases.map(b=>b.hp===0),d.warning?.assault,s.campaign!.districts!.resuppliedAt>=0]);
  if(key===this.last)return;this.last=key;this.timeline.push({t:s.t,phase:this.phase,engineerBlock:s.engineer.block,major:d.major?{...d.major}:null,minor:d.minor?{...d.minor}:null,bases:d.bases.map(b=>({block:b.block,hp:b.hp})),history:d.history.map(h=>({...h})),warning:campaignWarning(s),resuppliedAt:s.campaign!.districts!.resuppliedAt});
 }
}
function pod(d:DefenceDriver,core:BaseCore):Pod {
 const s=d.state,points:{x:number;y:number;distance:number}[]=[];
 for(let y=core.y-8;y<=core.y+core.size+6;y++)for(let x=core.x-8;x<=core.x+core.size+6;x++){
  if(blockOfTile(s,x,y)!==core.block||blockOfTile(s,x-3,y)!==core.block)continue;
  if(!placeable(s,'turret',x,y)&&!placeable(s,'inserter',x-1,y,1)&&!placeable(s,'chest',x-3,y))points.push({x,y,distance:Math.hypot(x+1-core.x-core.size/2,y+1-core.y-core.size/2)});
 }
 const p=points.sort((a,b)=>a.distance-b.distance||a.y-b.y||a.x-b.x)[0];if(!p)throw Error('No supply pod at base '+core.block);
 const chest=d.place('chest',p.x-3,p.y),arm=d.place('inserter',p.x-1,p.y,1),turret=d.place('turret',p.x,p.y);
 d.send({type:'factory',action:{type:'routing',x:arm.x,y:arm.y,filter:'magazine'}});return {chest,arm,turret,block:core.block};
}
function mine(d:DefenceDriver,item:'steel'|'copper'|'coal',target:number):void {
 const s=d.state,G=ground(s),home=s.campaign!.homeBlock;
 while((s.engineer.inv[item]??0)<target){
  let at:{x:number;y:number;units:number}|undefined;
  for(let t=0;t<G.patch.length;t++)if(G.patch[t]&&G.owner[t]===home){const r=rubbleAt(s,t%G.tw,Math.floor(t/G.tw));if(r?.type===item){at={x:t%G.tw,y:Math.floor(t/G.tw),units:r.units};break;}}
  if(!at)throw Error('No remaining home '+item);d.approach(at.x,at.y);d.send({type:'factory',action:{type:'mineAt',x:at.x,y:at.y}});
  const before=s.engineer.inv[item]??0;d.run(Math.min(30,target-before,at.units)+1);d.send({type:'factory',action:{type:'mineAt',x:-1,y:-1}});
  if((s.engineer.inv[item]??0)<=before)throw Error('No mining progress '+item);
 }
}
function line(d:DefenceDriver){
 const bp=parseBlueprint({version:1,name:'Home mixed input ammunition line',entities:[
 {id:'input',kind:'chest',x:0,y:0,dir:0},{id:'feed',kind:'inserter',x:2,y:0,dir:1},
 {id:'assembler',kind:'assembler',x:3,y:0,dir:0,recipe:'shot'},{id:'arm',kind:'inserter',x:6,y:1,dir:1},
 {id:'output',kind:'chest',x:7,y:1,dir:0}]});
 const s=d.state,b=ground(s).opening!.bounds;
 for(let y=b.y+1;y<b.y+b.size-3;y++)for(let x=b.x+1;x<b.x+b.size-9;x++){
  if(bp.entities.every(e=>!placeable(s,e.kind,x+e.x,y+e.y,e.dir))){const ids=buildBlueprint(d,bp,x,y);return {input:s.flow!.machines.find(m=>m.id===ids.input)!,output:s.flow!.machines.find(m=>m.id===ids.output)!};}
 }throw Error('No home factory footprint');
}
function feed(d:DefenceDriver,m:Machine):void {d.approach(m.x,m.y,m.size);d.send({type:'factory',action:{type:'feed',x:m.x,y:m.y}});}
export function runDefence(seed:number,mode:'turrets'|'support',progress:(s:string)=>void=()=>{}):DefenceRun {
 const st=createCampaign(seed),initial=structuredClone(st),d=new DefenceDriver(st),checks:DefenceCheck[]=[],measurements:Record<string,unknown>={};
 const home=st.flow!.machines.find(m=>m.kind==='depot')!,homeCore=baseCore(st,st.campaign!.homeBlock)!,gens=[st.flow!.machines.find(m=>m.kind==='generator')!],pods:Pod[]=[];
 let intervalStart=structuredClone(st),logAt=0;
 const checkpoint=(name:string)=>{const save=savedContinuation(st),interval=replayInterval(intervalStart,d.log.slice(logAt),st.flow!.tick),same=stateHash(interval)===stateHash(st);checks.push({name:name+' conserved/save/interval replay',pass:conservation(st).ok&&save.same&&same,detail:{t:st.t,save,intervalSame:same,ledger:conservation(st).unexplained}});intervalStart=structuredClone(st);logAt=d.log.length;progress(seed+' '+mode+' '+name+' at '+st.t+'s');};
 const stock=(item:string,n:number)=>{d.approach(home.x,home.y,home.size);d.take(item,n);};
 try {
  stock('steel',200);stock('copper',100);stock('coal',40);stock('magazine',20);
  for(let i=0;i<2;i++){const p=pod(d,homeCore);pods.push(p);d.put('magazine',10,p.chest);}
  d.phase='paid mining';mine(d,'steel',700);mine(d,'copper',300);mine(d,'coal',300);feed(d,gens[0]);checkpoint('mined ordinary home resources');
  const factory=line(d);d.put('steel',180,factory.input);d.put('copper',90,factory.input);
  const e=st.campaign!.expansion!,district=st.campaign!.districts!;
  d.phase='restoration';
  for(const id of ['station','northStation'] as const){const site=campaignSite(st,id)!;gens.push(power(d,site.block));restore(d,id);for(let i=0;i<2;i++)pods.push(pod(d,baseCore(st,site.block)!));}
  d.approach(e.radio.x,e.radio.y,e.radio.size);d.send({type:'deliverSite',site:'radio'});d.send({type:'restoreSite',site:'radio'});if(e.radio.restoredAt<0)throw Error(siteCheck(st,'radio'));
  d.phase='permanent rail service';const path=st.campaign!.fixedTram!.route;
  const stops=[...e.stops,district.stop].map(([x,y])=>st.flow!.machines.find(m=>m.kind==='tramstop'&&m.x===x&&m.y===y)!),tram=st.flow!.machines.find(m=>m.id===st.campaign!.fixedTram!.tram)!;
  configure(d,stops[0],{magazine:{request:0,reserve:0,export:true},steel:{request:0,reserve:0,export:true},copper:{request:0,reserve:0,export:true}});
  for(const stop of stops.slice(1))configure(d,stop,{magazine:{request:10,reserve:0,export:false},steel:{request:15,reserve:0,export:false},copper:{request:10,reserve:0,export:false}});
  d.put('steel',40,stops[0]);d.put('copper',25,stops[0]);
  const coalSource=extract(d,'coal');gens.push(coalSource.generator);
  const replenishCoal=()=>{const available=coalSource.chest.inv.coal??0;if(available>0)d.take('coal',Math.min(200,available),coalSource.chest);else mine(d,'coal',100);};
  const services:{at:number;end:number;mined:boolean}[]=[];
  const service=()=>{
   const at=st.t;let mined=false;
   if((st.engineer.inv.steel??0)<50&&(factory.input.inv.steel??0)<100){mine(d,'steel',200);mine(d,'copper',100);mined=true;}
   if((st.engineer.inv.coal??0)<50){replenishCoal();mined=true;}
   for(const gen of gens)if((gen.inv.coal??0)<20){if((st.engineer.inv.coal??0)<50)replenishCoal();feed(d,gen);}
   if((factory.input.inv.steel??0)<60&&(st.engineer.inv.steel??0)>40)d.put('steel',Math.min(100,(st.engineer.inv.steel??0)-40),factory.input);
   if((factory.input.inv.copper??0)<30&&(st.engineer.inv.copper??0)>20)d.put('copper',Math.min(50,(st.engineer.inv.copper??0)-20),factory.input);
   for(const p of pods.filter(p=>p.block===homeCore.block))if((p.chest.inv.magazine??0)<8)moveCargo(d,'magazine',12-(p.chest.inv.magazine??0),factory.output,p.chest);
   moveCargo(d,'magazine',Math.max(0,80-(stops[0].inv.magazine??0)),factory.output,stops[0]);
   for(const p of pods){const stock=p.chest.inv.magazine??0;if(stock>=8)continue;
    if(p.block===homeCore.block)moveCargo(d,'magazine',12-stock,factory.output,p.chest);
    else {const stop=stops.find(m=>blockOfTile(st,m.x,m.y)===p.block)!;moveCargo(d,'magazine',12-stock,stop,p.chest);}
   }
   services.push({at,end:st.t,mined});
  };
  d.phase='network service';for(let i=0;i<6;i++){service();d.run(30);}checkpoint('network established');
  measurements.setupSeconds=st.t;measurements.network={pathLength:path.length,stops:stops.map(m=>m.id),pods:pods.map(p=>({block:p.block,turret:p.turret.id,chest:p.chest.id})),resuppliedAt:district.resuppliedAt};
  // Wait on the received dawn warning, without reading an unreceived target for navigation.
  d.phase='waiting for warning';while(!st.campaign!.defence!.major&&st.t<4800){service();d.run(30);}
  const target=knownCampaignThreat(st),major=st.campaign!.defence!.major;if(!target||!major)throw Error('No received actionable major warning');
  const warnedAt=st.t,from=st.engineer.block;d.approach(Math.floor(target.x),Math.floor(target.y),1);measurements.warningTravel={warnedAt,from,target:target.block,arrivedAt:st.t,leadSeconds:major.startsAt-st.t,warning:campaignWarning(st)};
  while(st.t<major.startsAt-180){service();d.run(Math.min(30,Math.max(0,major.startsAt-180-st.t)));}
  if(mode==='support')d.take('magazine',Math.min(15,factory.output.inv.magazine??0),factory.output);
  d.approach(Math.floor(target.x),Math.floor(target.y),1);measurements.preparedAt=st.t;d.run(Math.max(0,major.startsAt-st.t));
  d.phase='major defence';const start=st.t,before={rounds:st.flow!.stats.fired,rifle:st.engineer.fired,spentSteel:st.stats.spentSteel,spentCopper:st.stats.spentCopper};
  while(st.campaign!.defence!.major&&st.t<start+700){
   if(mode==='support'){const c=st.flow!.threat?.crawlers.find(c=>c.campaign?.layer==='major'&&Math.hypot(c.x-st.engineer.x,c.y-st.engineer.y)<=9);d.send({type:'aim',at:c?[c.x,c.y]:null});}
   d.run(1);
  }
  d.send({type:'aim',at:null});measurements.major={before,start,end:st.t,history:st.campaign!.defence!.history.map(h=>({...h})),turretRounds:st.flow!.stats.fired-before.rounds,rifleShots:st.engineer.fired-before.rifle,coreHp:baseCore(st,target.block)!.hp,nextDawn:st.campaign!.defence!.nextDawn};checkpoint('major completed');
  checks.push({name:'normally supplied major holds finite roster',pass:st.campaign!.defence!.history.some(h=>h.spawned===60&&!h.defeated),detail:st.campaign!.defence!.history});
  // Interrupt a remote station through ordinary generator pickup; public track cannot be removed.
  d.phase='supply interruption';service();const repairSteelBefore=st.stats.spentSteel??0,repairCopperBefore=st.stats.spentCopper??0;configure(d,stops[2],{magazine:{request:200,reserve:0,export:false},steel:{request:25,reserve:0,export:false},copper:{request:15,reserve:0,export:false}});
  // Stage beside the remote generator before waiting for a real freight reservation.
  moveCargo(d,'magazine',20,factory.output,stops[0]);
  const interruptedGen=gens.find(m=>blockOfTile(st,m.x,m.y)===district.station.block)!;const x=interruptedGen.x,y=interruptedGen.y;d.approach(x,y,2);
  for(let i=0;i<600&&invTotal(tram.cargo)===0;i++)d.run(.1);
  if(invTotal(tram.cargo)===0)throw Error('No loaded freight reached the prepared cut');
  d.send({type:'construct',edits:[{action:'pickUp',x,y}]});gens.splice(gens.indexOf(interruptedGen),1);
  const cutAt=st.t,cargo=invTotal(tram.cargo),received=(st.flow!.stats.tramMoved??0);
  // Declared exhaustion drill: remove feeder arms, pick up/refund loaded turrets and replace them empty.
  // Refunds stay in the engineer/remote supply chest. No inventory, HP or schedule writes.
  const drained=pods.filter(p=>p.block===district.station.block),drainAt=st.t;
  for(const p of drained){d.approach(p.arm.x,p.arm.y);d.send({type:'construct',edits:[{action:'pickUp',x:p.arm.x,y:p.arm.y}]});d.approach(p.turret.x,p.turret.y,2);while(defenceHp(p.turret)<defenceMax(p.turret)){const why=repairCheck(st,p.turret.x,p.turret.y);if(why)throw Error(why);d.send({type:'repairDefence',x:p.turret.x,y:p.turret.y});d.run(4);}d.send({type:'construct',edits:[{action:'pickUp',x:p.turret.x,y:p.turret.y}]});p.turret=d.place('turret',p.turret.x,p.turret.y);}
  const recoveredMagazines=st.engineer.inv.magazine??0;if(recoveredMagazines)d.put('magazine',recoveredMagazines,drained[0].chest);
  d.approach(home.x,home.y,home.size);
  while(baseCore(st,district.station.block)!.hp>0&&st.t<cutAt+1900){for(const gen of gens)if((gen.inv.coal??0)<15){if((st.engineer.inv.coal??0)<30)replenishCoal();feed(d,gen);}d.approach(home.x,home.y,home.size);d.run(20);}
  const disabledAt=st.t;while(st.campaign!.defence!.minor?.block===district.station.block&&st.t<disabledAt+120)d.run(1);
  measurements.exhaustionDrill={drainAt,disabledAt,refundedMagazines:recoveredMagazines,coreHp:baseCore(st,district.station.block)!.hp};
  const interruption={cutAt,cargo,afterCargo:invTotal(tram.cargo),beforeMoved:received,afterMoved:(st.flow!.stats.tramMoved??0),route:stationRoute(st,stops[2].id)!.summary};checkpoint('station power interrupted');
  const restoredGen=d.place('generator',x,y);gens.push(restoredGen);feed(d,restoredGen);
  // Repair stock was actually shipped from home before the cut. Withdraw at the disabled outpost.
  const recoveryCore=baseCore(st,district.station.block)!;const repairDelivery={steel:d.take('steel',10,stops[2]),copper:d.take('copper',5,stops[2])};measurements.repairDelivery=repairDelivery;if(repairDelivery.steel!==10||repairDelivery.copper!==5)throw Error('Shipped repair stock shortfall');
  if(recoveryCore.hp===0){d.approach(recoveryCore.x,recoveryCore.y,recoveryCore.size);const why=repairCheck(st,recoveryCore.x,recoveryCore.y);if(why)throw Error(why);d.send({type:'repairDefence',x:recoveryCore.x,y:recoveryCore.y});d.run(12);}
  for(const p of drained){p.arm=d.place('inserter',p.arm.x,p.arm.y,1);d.send({type:'factory',action:{type:'routing',x:p.arm.x,y:p.arm.y,filter:'magazine'}});}
  service();d.run(90);measurements.interruption={...interruption,repairedAt:st.t,afterRepairMoved:(st.flow!.stats.tramMoved??0)};
  checks.push({name:'loaded cargo is retained or delivered without loss and repaired freight resumes',pass:cargo>0&&conservation(st).ok&&(st.flow!.stats.tramMoved??0)>interruption.afterMoved,detail:measurements.interruption});
  d.phase='recovery';for(const core of st.campaign!.defence!.bases)if(core.hp<300){
   d.approach(core.x,core.y,core.size);for(let k=0;k<12&&core.hp<300;k++){const why=repairCheck(st,core.x,core.y);if(why)throw Error(why);d.send({type:'repairDefence',x:core.x,y:core.y});d.run(12);}
  }
  for(const p of pods)while(defenceHp(p.turret)<defenceMax(p.turret)){d.approach(p.turret.x,p.turret.y,2);const why=repairCheck(st,p.turret.x,p.turret.y);if(why)throw Error(why);d.send({type:'repairDefence',x:p.turret.x,y:p.turret.y});d.run(4);}
  measurements.services=services;measurements.recovery={coreHp:recoveryCore.hp,spentSteel:(st.stats.spentSteel??0)-repairSteelBefore,spentCopper:(st.stats.spentCopper??0)-repairCopperBefore};
  checks.push({name:'exhausted outpost disabled and recovered using shipped repair stock',pass:disabledAt>drainAt&&(measurements.exhaustionDrill as {coreHp:number}).coreHp===0&&recoveryCore.hp===300,detail:measurements.exhaustionDrill});
  measurements.freight={moved:st.flow!.stats.tramMoved,supplied:{...district.supplied},visits:district.visits,receivedStocks:stops.slice(1).map(m=>({id:m.id,cargo:{...m.cargo}}))};
  checkpoint('recovered');checks.push({name:'earned three-base resupply milestone',pass:district.resuppliedAt>=0,detail:{...district.supplied,visits:district.visits,at:district.resuppliedAt}});
 } catch(error){measurements.failure={phase:d.phase,t:st.t,message:(error as Error).message};checks.push({name:'workload completed',pass:false,detail:measurements.failure});progress(JSON.stringify(measurements.failure));}
 measurements.raids=d.raids;measurements.awayFromHomeSeconds=d.awaySeconds;
 checks.push({name:'unattended supplied remote minor defence',pass:d.raids.some(r=>r.block!==homeCore.block&&r.end>=0&&r.nearestEngineer>20&&r.rifleShots===0&&r.coreAfter>0),detail:d.raids});
 measurements.timeline=d.timeline;measurements.durationSeconds=st.t;measurements.manualCommands=d.log.length;measurements.engineer={walked:st.engineer.walked,downs:st.engineer.downs,fired:st.engineer.fired,hurt:st.engineer.hurt};measurements.stats={...st.flow!.stats};measurements.bases=st.campaign!.defence!.bases.map(b=>({...b}));
 const ledger=conservation(st),save=savedContinuation(st),replayed=replayInterval(initial,d.log,st.flow!.tick);
 checks.push({name:'conservation',pass:ledger.ok,detail:ledger.unexplained},{name:'saved continuation',pass:save.same,detail:save},{name:'full normal-start command replay',pass:stateHash(replayed)===stateHash(st),detail:{actual:stateHash(st),replayed:stateHash(replayed)}});
 return {seed,mode,setup:'Fresh campaign, ordinary starting stock; no clock/position/HP/inventory/schedule injection. Paid hand mining, construction and scripted supply trips; live threats. Not human play or an unattended entire factory.',checks,measurements,initialState:initial,finalState:st,log:d.log};
}
