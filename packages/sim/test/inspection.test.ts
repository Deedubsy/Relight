import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import {DARK,machineConstraints,knownInputSource,chestTransferPreview,chestTake,chestPut,truckTransferPreview,truckTransfer,ASM_OUTPUT_CAP} from '../src/index';
import test from 'node:test';
import assert from 'node:assert/strict';
import { ensureFlow, applyCommands, actionResult, canPlace, inReach, machineAt, machineById, advanceFlow,
  inspectMachine, factoryStatistics, sampleInspection, machineStatus, machineCircuit, campaignGrid, makeSave, loadState, stateHash, conservation,
  MACHINE_SIZE, dimensions, type SimState, type Kind, type Machine, type Command, type Item, observeOutput, blockOfTile } from '../src/index';
import { createSession, parseUrl, dispatch, replaySession } from '../../game/src/session';

function send(st:SimState,c:Command){applyCommands(st,[c]);assert.ok(actionResult(st).ok,actionResult(st).reason);}
function fresh(){const s=createCampaign(3);ensureFlow(s);for(const [item,n] of [['steel',200],['copper',100],['coal',20]] as const)send(s,{type:'factory',action:{type:'chestTake',item,n}});return s;}
function build(st:SimState,kind:Kind):Machine{
 const e=st.engineer;
 for(let y=Math.floor(e.y)-8;y<=e.y+8;y++)for(let x=Math.floor(e.x)-8;x<=e.x+8;x++){
  if(!canPlace(st,kind,x,y,1).ok||!inReach(st,x,y,...dimensions(kind,1,MACHINE_SIZE[kind])))continue;
  send(st,{type:'construct',edits:[{action:'place',item:kind,x,y,dir:1}]});return machineAt(st,x,y)!;
 }
 throw Error('no paid reachable footprint');
}
function run(st:SimState,seconds:number){st.speed=1;for(let i=0;i<Math.round(seconds*20);i++)advanceFlow(st,.05);}
/** Labelled inventory fixture: only moves normal carried inputs into the paid machine, preserving the ledger. */
function inputs(st:SimState,m:Machine,k:Item,n:number){st.engineer.inv[k]-=n;m.inv[k]=(m.inv[k]??0)+n;}

test('inspection: paid assembler exposes nominal recipe rates, completed production and conserved saved continuation',()=>{
 const s=fresh(),m=build(s,'assembler');inputs(s,m,'steel',4);inputs(s,m,'copper',2);
 const before=stateHash(s),info=inspectMachine(s,m.id)!;assert.equal(stateHash(s),before,'inspection is read-only');
 assert.equal(info.recipe,'Shot magazine');assert.equal(info.nominal.find(r=>r.item==='magazine')!.output,10);
 assert.equal(info.nominal.find(r=>r.item==='steel')!.input,20);assert.equal(info.measured!.seconds,0);
 run(s,6);let row=inspectMachine(s,m.id)!.measured!.rows.find(r=>r.item==='magazine')!;
 assert.equal(row.produced,1);assert.equal(row.producedPerMin,10);
 const saved=loadState(makeSave(s));run(s,6);run(saved,6);assert.equal(stateHash(saved),stateHash(s));
 row=inspectMachine(s,m.id)!.measured!.rows.find(r=>r.item==='magazine')!;assert.equal(row.produced,2);
 assert.ok(conservation(s).ok,conservation(s).problems.join(', '));
});

test('inspection: recipe changes retain per-item observations; packing/rebuild resets only machine history',()=>{
 const s=fresh(),m=build(s,'assembler');send(s,{type:'factory',action:{type:'setRecipe',x:m.x,y:m.y,recipe:'wire'}});
 inputs(s,m,'copper',2);run(s,2);assert.equal(inspectMachine(s,m.id)!.measured!.rows.find(r=>r.item==='wire')!.produced,4);
 send(s,{type:'factory',action:{type:'setRecipe',x:m.x,y:m.y,recipe:'frame'}});inputs(s,m,'steel',2);run(s,2);
 const q=inspectMachine(s,m.id)!;assert.equal(q.recipe,'Frame');assert.equal(q.measured!.rows.find(r=>r.item==='wire')!.produced,4);
 assert.equal(q.measured!.rows.find(r=>r.item==='frame')!.produced,1);
 const totals=factoryStatistics(s)!.rows.map(r=>r.produced);
 send(s,{type:'construct',edits:[{action:'pickUp',x:m.x,y:m.y}]});assert.equal(inspectMachine(s,m.id),null);
 send(s,{type:'undoBuild'});assert.equal(inspectMachine(s,m.id)!.measured!.seconds,0);assert.deepEqual(factoryStatistics(s)!.rows.map(r=>r.produced),totals);
 assert.ok(conservation(s).ok);
});

test('inspection: local circuits isolate remote supply, reflect brownout, fuel exhaustion and pole connection',()=>{
 const s=fresh(),m=build(s,'assembler'),home=blockOfTile(s,m.x,m.y),base=s.flow!.machines.find(x=>x.kind==='generator')!;
 // Explicit geometry/load fixture: a second fueled generator belongs to a distant station circuit; extra assembler loads share the home circuit.
 const remote=structuredClone(base),site=s.campaign!.expansion!.station;
 remote.id=s.flow!.next++;remote.x=site.x;remote.y=site.y;remote.inv={coal:10};s.flow!.machines.push(remote);
 for(let i=0;i<3;i++){const a=structuredClone(m);a.id=s.flow!.next++;s.flow!.machines.push(a);}s.flow!.rev++;
 const p=machineCircuit(s,m);assert.equal(p.supply,300);assert.equal(p.demand,520);assert.equal(p.throttle,300/520);
 assert.equal(campaignGrid(s).supply,600);assert.equal(inspectMachine(s,m.id)!.scale,300/520);
 assert.equal(inspectMachine(s,remote.id)!.circuit.supply,300);assert.equal(blockOfTile(s,remote.x,remote.y)===home,false);
 inputs(s,m,'steel',2);inputs(s,m,'copper',1);run(s,10);assert.equal(m.out,0,'520 kW demand has not yet completed six productive seconds');run(s,2);
 assert.equal(inspectMachine(s,m.id)!.measured!.rows.find(r=>r.item==='magazine')!.producedPerMin,5,'measured output follows the actual local throttle including the public tram stop');
 base.inv.coal=0;assert.equal(machineStatus(s,m).state,'off');assert.equal(inspectMachine(s,m.id)!.circuit.supply,0);
 base.inv.coal=10;assert.equal(inspectMachine(s,m.id)!.circuit.supply,300);
 const pole={...structuredClone(base),id:s.flow!.next++,kind:'pole' as const,size:1,x:site.x-1,y:site.y,inv:{}};s.flow!.machines.push(pole);s.flow!.rev++;
 assert.equal(machineStatus(s,pole).state,'running');assert.equal(machineCircuit(s,pole).supply,300);
});

test('inspection: belt blockage and inserter destination loss report the actual held state',()=>{
 const s=fresh(),b=build(s,'belt');b.items=[{k:'steel',p:.875}];s.engineer.inv.steel--;
 assert.equal(machineStatus(s,b).state,'blocked');b.items[0].p=.2;assert.equal(machineStatus(s,b).state,'running');
 const arm=build(s,'inserter');arm.phase=1;arm.hold='copper';arm.timer=0;s.engineer.inv.copper--;
 assert.equal(machineStatus(s,arm).state,'blocked');assert.match(machineStatus(s,arm).reason,/held item retained|cannot accept/);
 arm.phase=2;arm.hold=null;s.engineer.inv.copper++;assert.equal(machineStatus(s,arm).reason,'returning to pickup');
 assert.ok(conservation(s).ok);
});

test('inspection: saved sample window is bounded, excludes pause time and rejects malformed observations',()=>{
 const s=fresh(),b=build(s,'belt');sampleInspection(s);observeOutput(s,b,'steel',1);
 run(s,65);const q=inspectMachine(s,b.id)!.measured!;assert.ok(q.seconds<=60&&q.seconds>=59);
 assert.equal(q.rows.find(r=>r.item==='steel')!.producedPerMin,0);assert.ok(b.observation!.samples.length<=61);
 s.speed=0;const before=factoryStatistics(s);advanceFlow(s,100);assert.deepEqual(factoryStatistics(s),before);
 const loaded=loadState(makeSave(s));assert.deepEqual(factoryStatistics(loaded),before);
 const bad=structuredClone(s);bad.flow!.observation!.samples[0].tick=bad.flow!.tick+1;assert.throws(()=>loadState(bad),/observation/);
 const corrupt=structuredClone(s);corrupt.flow!.observation!.produced.steel=NaN;assert.throws(()=>loadState(corrupt),/observation/);
 delete loaded.flow!.observation;for(const m of loaded.flow!.machines)delete m.observation;
 assert.equal(factoryStatistics(loaded)!.seconds,0);run(loaded,1);assert.ok(factoryStatistics(loaded)!.seconds>0);
});

test('inspection: fresh campaign replay includes identical observations and readonly queries add no commands',()=>{
 Object.assign(globalThis,{location:{href:'http://localhost/?rules=exploration-v2'}});
 const s=createSession(parseUrl('?rules=exploration-v2&seed=3&view=world'));
 dispatch(s,{type:'setSpeed',mult:1});for(let i=0;i<40;i++)advanceFlow(s.state,.05);
 const before=stateHash(s.state),n=s.log.length;for(const m of s.state.flow!.machines)inspectMachine(s.state,m.id);factoryStatistics(s.state);
 assert.equal(stateHash(s.state),before);assert.equal(s.log.length,n);
 const resumed=loadState(makeSave(s.state,{log:s.log,logComplete:true}));assert.equal(stateHash(resumed),before);
 const replay=replaySession(s);assert.ok('state' in replay);assert.equal(stateHash(replay.state),before);
 assert.ok(machineById(s.state,s.state.flow!.machines[0].id));
});


test('UI-04 simultaneous constraints are readonly and clear independently with real input and power predicates',()=>{
 const s=fresh(),m=build(s,'assembler'),g=s.flow!.machines.find(m=>m.kind==='generator')!;
 // Labelled fuel outage / filled output fixture; no production transition is run by the query.
 g.inv.coal=0;m.out=ASM_OUTPUT_CAP;const before=stateHash(s),q=machineConstraints(s,m);
 assert.deepEqual(q.blockers.map(b=>b.code),['power','input','input','output']);assert.equal(stateHash(s),before);
 g.inv.coal=10;assert.ok(!machineConstraints(s,m).blockers.some(b=>b.code==='power'));
 inputs(s,m,'steel',4);inputs(s,m,'copper',2);m.out=0;run(s,.05);
 const running=inspectMachine(s,m.id)!;assert.equal(running.status.state,'running');assert.deepEqual(running.blockers,[]);
 // Remaining materials insufficient for a future batch must not claim the running batch is stalled.
 m.inv={};assert.equal(machineConstraints(s,m).blockers.length,0);assert.equal(machineConstraints(s,m).nextInputs.length,2);
});
test('UI-04 no local load is neutral; disabled core retains the actual primary statement',()=>{
 const s=fresh(),g=s.flow!.machines.find(m=>m.kind==='generator')!;
 const base=s.campaign!.defence!.bases.find(b=>b.block===blockOfTile(s,g.x,g.y))!;base.hp=0;
 assert.equal(machineConstraints(s,g).blockers[0].code,'disabled');assert.match(inspectMachine(s,g.id)!.status.reason,/disabled/);
 const neutral=structuredClone(s);neutral.campaign!.defence!.bases=[];neutral.blocks[blockOfTile(neutral,g.x,g.y)].state=DARK;neutral.flow!.machines=neutral.flow!.machines.filter(m=>m.kind==='generator');neutral.flow!.rev++;
 assert.equal(machineStatus(neutral,neutral.flow!.machines[0]).state,'idle');assert.deepEqual(machineConstraints(neutral,neutral.flow!.machines[0]).blockers,[]);
});
test('UI-04 hand transfer previews match actual capped moves and never allocate arrivals or mutate state',()=>{
 const s=fresh(),chest=build(s,'chest');const at:[number,number]=[chest.x,chest.y];
 for(const put of [true,false]){const before=stateHash(s),q=chestTransferPreview(s,'steel',27,put,at);assert.equal(stateHash(s),before);assert.deepEqual(put?chestPut(s,'steel',27,at):chestTake(s,'steel',27,at),q);}
 // Labelled stop pool fixture: taking drains arrivals before platform, and reads do not create missing cargo.
 chest.kind='tramstop';chest.inv={steel:3};delete chest.cargo;const before=stateHash(s);assert.equal(chestTransferPreview(s,'steel',9,false,at).moved,3);assert.equal(stateHash(s),before);assert.equal(chest.cargo,undefined);
 chest.cargo={steel:2};assert.equal(chestTransferPreview(s,'steel',9,false,at).moved,5);assert.equal(chestTake(s,'steel',9,at).moved,5);assert.equal(chest.cargo.steel,undefined);assert.equal(chest.inv.steel,undefined);
 const e=s.engineer;[e.x,e.y]=[e.x+40,e.y+40];const inv=structuredClone(e.inv),q=chestTransferPreview(s,'steel',9,true,at);assert.equal(q.moved,0);assert.deepEqual(chestPut(s,'steel',9,at),q);assert.deepEqual(e.inv,inv);
 e.down=1;assert.match(chestTransferPreview(s,'steel',9,false).reason,/recover/);
});
test('UI-04 truck preview shares command capacity/reach rules and leaves source stock untouched',()=>{
 const s=fresh();s.campaign!.truck={x:s.engineer.x,y:s.engineer.y,dir:1,unlockedAt:0,cargo:{}};
 for(const put of [true,false]){const before=stateHash(s),q=truckTransferPreview(s,'steel',25,put);assert.equal(stateHash(s),before);assert.deepEqual(truckTransfer(s,'steel',25,put),q);}
 s.campaign!.truck.cargo={belt:200};const before=stateHash(s),q=truckTransferPreview(s,'steel',25,true);assert.equal(q.ok,false);assert.equal(stateHash(s),before);assert.deepEqual(truckTransfer(s,'steel',25,true),q);
 s.campaign!.truck.x+=40;assert.match(truckTransferPreview(s,'steel',1,true).reason,/closer/);
});
test('UI-04 source locations only name genuine known stock or finite home patches',()=>{
 const s=fresh(),before=stateHash(s);assert.match(knownInputSource(s,'steel')!.label,/Home/);assert.equal(knownInputSource(s,'board'),null);assert.equal(stateHash(s),before);
});
