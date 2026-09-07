/** Campaign engineering workloads. Their own profile and setup replace no legacy acceptance. */
import { ground, rubbleAt, conservation, stateHash, recipeYield, recipeOutput, ASSEMBLER_RECIPES, INSERTER_PER_S,
 stationRoute, invTotal, type SimState } from '@relight/sim';
import { FactoryDriver, replayInterval, savedContinuation, type Blueprint } from './blueprint';
import { factoryScenario, factoryLine, railNetwork, configure, extract, recipe, moveCargo } from './factoryScenario';

export interface FactoryCheck { name:string;pass:boolean;detail:unknown }
export interface FactoryResult { id:string;seed:number;setup:string;checks:FactoryCheck[];measurements:unknown;log:FactoryDriver['log'];initialState:SimState;finalState:SimState }
export interface FactoryContext { seed:number;hours:number;blueprint:Blueprint;progress:(text:string)=>void }
function result(id:string,s:ReturnType<typeof factoryScenario>,measurements:unknown,checks:FactoryCheck[]):FactoryResult {
 const ledger=conservation(s.st);checks.push({name:'conserved items',pass:ledger.ok,detail:ledger.unexplained});
 const continuation=savedContinuation(s.st);checks.push({name:'save continuation',pass:continuation.same,detail:continuation});
 const replay=replayInterval(s.initial,s.d.log,s.st.flow!.tick);checks.push({name:'complete command replay from declared initial state',pass:stateHash(replay)===stateHash(s.st),detail:{replayed:stateHash(replay),actual:stateHash(s.st)}});
 return {id,seed:s.st.seed,setup:s.assistance,checks,measurements,log:s.d.log,initialState:s.initial,finalState:s.st};
}
export function chain(ctx:FactoryContext):FactoryResult {
 const s=factoryScenario(ctx.seed),{st,d}=s,line=factoryLine(d,ctx.blueprint),rows:unknown[]=[],checks:FactoryCheck[]=[];
 // Each run uses the same paid line, changes recipe by ordinary commands and recovers its real output.
 for(const id of ['wire','board','frame','shot'] as const){
  for(const [item,n] of Object.entries(line.input.inv))if(n>0)d.take(item,n,line.input);
  if(id==='board')d.take('wire',Math.min(60,line.output.inv.wire??0),line.output);
  recipe(d,line.assembler,id);
  const r=ASSEMBLER_RECIPES[id];for(const [item,n] of Object.entries(r.inputs)){
   const available=st.engineer.inv[item]??0;d.put(item,Math.min(available,n*Math.ceil(75/r.seconds)),line.input);
  }
  d.run(10);const output=recipeOutput(r),before=st.flow!.stats.made[output]??0,stored=line.output.inv[output]??0;d.run(60);
  const made=(st.flow!.stats.made[output]??0)-before,arrived=(line.output.inv[output]??0)-stored,nominal=60/r.seconds*recipeYield(r),transportCeiling=60*INSERTER_PER_S;
  rows.push({recipe:id,windowSeconds:60,nominalPerMin:nominal,outputArmCeilingPerMin:transportCeiling,completedPerMin:made,arrivedPerMin:arrived,ratioToNominal:made/nominal});
  checks.push({name:`${id}: real chain output within nominal capacity`,pass:arrived>0&&made<=nominal+recipeYield(r),detail:rows.at(-1)});
 }
 return result('E-chain',s,rows,checks);
}
export function coal(ctx:FactoryContext):FactoryResult {
 const s=factoryScenario(ctx.seed,true),{st,d}=s,G=ground(st);let patch:{x:number;y:number;units:number}|undefined;
 for(let t=0;t<G.patch.length;t++)if(G.patch[t]){const r=rubbleAt(st,t%G.tw,Math.floor(t/G.tw));if(r?.type==='coal'&&Number.isFinite(r.units)){patch={x:t%G.tw,y:Math.floor(t/G.tw),units:r.units};break;}}
 if(!patch)throw new Error('No finite home coal patch');
 d.approach(patch.x,patch.y);const minedBefore=st.flow!.stats.minedOf.coal??0;d.send({type:'factory',action:{type:'mineAt',x:patch.x,y:patch.y}});d.run(patch.units+2);d.send({type:'factory',action:{type:'mineAt',x:-1,y:-1}});
 const finiteMined=(st.flow!.stats.minedOf.coal??0)-minedBefore,regional=extract(d,'coal'),start=st.t;
 while((regional.generator.inv.coal??0)>0&&st.t-start<7200)d.run(30);
 const dryAt=st.t-start;d.take('coal',200,regional.chest);const before=regional.chest.inv.coal??0;d.run(60);const withoutFuel=regional.chest.inv.coal??0;
 d.approach(regional.generator.x,regional.generator.y,2);d.send({type:'factory',action:{type:'feed',x:regional.generator.x,y:regional.generator.y}});d.run(60);
 const after=regional.chest.inv.coal??0,persistent=rubbleAt(st,regional.source.x,regional.source.y);
 return result('E-coal',s,{finiteHomeTile:patch,finiteMined,dryAfterSeconds:dryAt,before,withoutFuel,afterRecovery:after,regionalSourcePersistent:persistent?.persistent??false,coalBurned:st.flow!.stats.coalBurned},[
  {name:'finite home coal tile depletes at whole-item mining granularity',pass:finiteMined===Math.ceil(patch.units)&&!rubbleAt(st,patch.x,patch.y),detail:{finiteMined,units:patch.units}},
  {name:'isolated coal extraction stops without fuel and restarts from produced coal',pass:withoutFuel===before&&after>withoutFuel&&!!persistent?.persistent,detail:{before,withoutFuel,after}},
 ]);
}
export function tram(ctx:FactoryContext):FactoryResult {
 const s=factoryScenario(ctx.seed,true),{st,d}=s,net=railNetwork(d),[home,middle,last]=net.stops;
 configure(d,home,{steel:{request:0,reserve:5,export:true},copper:{request:8,reserve:0,export:false}});
 configure(d,middle,{steel:{request:4,reserve:0,export:false}});configure(d,last,{steel:{request:10,reserve:0,export:false},copper:{request:0,reserve:3,export:true}});
 d.put('steel',19,home);d.put('copper',11,last);d.run(100);
 const delivered={middle:middle.cargo?.steel??0,last:last.cargo?.steel??0,returned:home.cargo?.copper??0,reservedSteel:home.inv.steel??0,reservedCopper:last.inv.copper??0};
 // A paid track removal pauses a real in-flight reservation; returning rules remain sim-owned.
 configure(d,last,{steel:{request:40,reserve:0,export:false}});d.put('steel',30,home);
 for(let n=0;n<200&&!net.tram.manifest?.length;n++)d.run(.1);
 const cutTile=net.path[Math.floor(net.path.length/2)],x=cutTile%st.flow!.tw,y=Math.floor(cutTile/st.flow!.tw);d.approach(x,y);d.send({type:'construct',edits:[{action:'pickUp',x,y}]});
 const held=invTotal(net.tram.cargo),cut=stationRoute(st,last.id)!;d.run(20);const retained=invTotal(net.tram.cargo);d.place('track',x,y);d.run(100);
 return result('E-tram',s,{delivered,cut:cut.summary,heldAtCut:held,retainedAfterCut:retained,deliveredAfterRepair:last.cargo?.steel??0},[
  {name:'three stops deliver only demanded cargo and retain export reserves',pass:delivered.middle===4&&delivered.last===10&&delivered.returned===8&&delivered.reservedSteel===5&&delivered.reservedCopper===3,detail:delivered},
  {name:'severed line retains reserved cargo and resumes after paid repair',pass:held>0&&retained===held&&cut.interrupted&&(last.cargo?.steel??0)>=40,detail:{held,retained,summary:cut.summary,after:last.cargo?.steel}},
 ]);
}

export function logistics(ctx:FactoryContext):FactoryResult {
 const s=factoryScenario(ctx.seed,true),{st,d}=s,net=railNetwork(d),[home,middle,last]=net.stops;
 const homeChest=st.flow!.machines.find(m=>m.kind==='depot')!;d.approach(homeChest.x,homeChest.y,homeChest.size);d.take('steel',300);d.take('copper',150);d.take('coal',150);
 const line=factoryLine(d,ctx.blueprint),regional=extract(d,'coal'),steel=extract(d,'steel'),copper=extract(d,'copper');
 s.assistance+=' Soak: persistent steel/copper/coal are collected by scripted hand trips; the engineer fires delivered magazines toward an empty point to maintain measured demand, not to demonstrate defence.';
 configure(d,home,{magazine:{request:0,reserve:2,export:true},coal:{request:100,reserve:0,export:false}});
 configure(d,middle,{magazine:{request:10,reserve:0,export:false},coal:{request:0,reserve:10,export:true}});
 configure(d,last,{magazine:{request:20,reserve:0,export:false}});
 // Margin mining can leave stone/iron in the pockets. Store that real surplus before the timed workload.
 for(const item of ['stone','iron','steel','copper','coal']){const keep=['stone','iron'].includes(item)?0:150,n=(st.engineer.inv[item]??0)-keep;if(n>0){d.approach(homeChest.x,homeChest.y,homeChest.size);d.send({type:'factory',action:{type:'chestPut',item,n}});}}
 const gens=[line.generator,regional.generator,steel.generator,copper.generator,...net.generators],startTick=st.flow!.tick,checkpoints:unknown[]=[],checks:FactoryCheck[]=[];
 let checkpoint=structuredClone(st),logAt=d.log.length,maxError=0,maxMachines=0,handCoal=0,magsSupplied=0,coalShipped=0,services=0,lastMade=st.flow!.stats.made.magazine??0;
 const endTick=startTick+Math.round(ctx.hours*3600*20),started=performance.now();
 while(st.flow!.tick<endTick){
  // Persistent regional output moves through ordinary chests and pockets, without restocking state.
  for(const item of ['steel','copper'] as const){const need=Math.max(0,(item==='steel'?100:50)-(line.input.inv[item]??0)),chest=item==='steel'?steel.chest:copper.chest;
   if(need&&(st.engineer.inv[item]??0)<need&&(chest.inv[item]??0)>0)d.take(item,Math.min(100,chest.inv[item]),chest);
   const n=Math.min(need,st.engineer.inv[item]??0);if(n>0)d.put(item,n,line.input);
  }
  const extracted=Math.min(regional.chest.inv.coal??0,Math.max(0,200-(st.engineer.inv.coal??0)));if(extracted>0)d.take('coal',Math.min(100,extracted),regional.chest);
  for(const gen of gens)if((gen.inv.coal??0)<20&&(st.engineer.inv.coal??0)>0){d.approach(gen.x,gen.y,2);const before=st.flow!.stats.handFedCoal??0;d.send({type:'factory',action:{type:'feed',x:gen.x,y:gen.y}});handCoal+=(st.flow!.stats.handFedCoal??0)-before;}
  magsSupplied+=moveCargo(d,'magazine',40,line.output,home);
  const ship=Math.min(30,Math.max(0,(st.engineer.inv.coal??0)-60),Math.max(0,100-(middle.inv.coal??0)));if(ship){d.put('coal',ship,middle);coalShipped+=ship;}
  // A declared ordinary-command rifle sink sustains demand; every spent round is in the ledger.
  for(const stop of [middle,last])if((stop.cargo?.magazine??0)>0)d.take('magazine',stop.cargo!.magazine,stop);
  if(!st.engineer.aim)d.send({type:'aim',at:[st.engineer.x+8,st.engineer.y]});
  if((home.cargo?.coal??0)>0)d.take('coal',home.cargo!.coal,home);
  services++;d.run(Math.min(120,Math.max(0,(endTick-st.flow!.tick)/20)));
  const ledger=conservation(st);maxError=Math.max(maxError,...Object.values(ledger.unexplained).map(Math.abs));maxMachines=Math.max(maxMachines,st.flow!.machines.length);
  const elapsed=(st.flow!.tick-startTick)/20;
  if(elapsed>=(checkpoints.length+1)*3600||st.flow!.tick>=endTick){
   const replay=replayInterval(checkpoint,d.log.slice(logAt),st.flow!.tick),hash=stateHash(st),same=stateHash(replay)===hash,save=savedContinuation(st);
   checkpoints.push({elapsedSeconds:elapsed,tick:st.flow!.tick,hash,replayed:stateHash(replay),same,save,ledger:ledger.unexplained,made:{...st.flow!.stats.made},coalBurned:st.flow!.stats.coalBurned,freightMoved:st.flow!.stats.tramMoved});
   const produced=(st.flow!.stats.made.magazine??0)-lastMade;lastMade=st.flow!.stats.made.magazine??0;
   checks.push({name:`checkpoint ${checkpoints.length}: live production, conserved save and interval replay`,pass:same&&save.same&&ledger.ok&&produced>0,detail:{elapsed,same,save,produced}});
   checkpoint=structuredClone(st);logAt=d.log.length;ctx.progress(`seed ${ctx.seed}: ${(elapsed/3600).toFixed(2)} h, ${st.flow!.machines.length} machines, ${st.flow!.stats.tramMoved} freight items, save/replay ${same?'match':'FAIL'}`);
  }
 }
 const seconds=(performance.now()-started)/1000;checks.push({name:'declared duration and live production/freight',pass:st.flow!.tick>=endTick&&(st.flow!.stats.made.magazine??0)>0&&(st.flow!.stats.tramMoved??0)>0&&maxError<=.01,detail:{duration:(st.flow!.tick-startTick)/20,maxError}});
 return result('E-logistics',s,{hours:ctx.hours,elapsedSeconds:(st.flow!.tick-startTick)/20,checkpoints,maxError,maxMachines,services,handCoal,magsSupplied,coalShipped,wallSecondsIncludingCheckpointReplays:seconds,limitations:'Threats deferred; finite opening stock assistance; scripted hand supply and collection. Not an unattended factory, survival test, reference-machine FPS result or human playtest.'},checks);
}
export const CAMPAIGN_EXPERIMENTS={ 'E-chain':chain,'E-coal':coal,'E-tram':tram,'E-logistics':logistics } as const;
