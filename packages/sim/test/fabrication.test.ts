import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createCampaign,applyCommands,actionResult,progressionCommand,lockReason,addMachine,place,openLedger,advanceFlow,loadState,stateProblem,conservation,heldItems,regionCommand,tickFirstRegion,FABRICATION} from '../src/index';
test('optional recovered knowledge, paid M2 workbench, powered decode/craft and removable module conserve ownership',()=>{
  let s=createCampaign();const p=s.campaign!.progression!,g=p.gameplay!,source=p.sites.find(x=>x.id==='artifact:workshop')!;
  assert.match(lockReason(s,'alienworkbench'),/M2/);s.engineer.x=source.x-.5;s.engineer.y=source.y+.5;
  assert.match(progressionCommand(s,{type:'recover',id:source.id}),/2 shared Artifacts/);assert.equal(s.engineer.inv.alienartifact,2);assert.deepEqual(g.recoveredSchematics,['overclock']);assert.deepEqual(g.learnedSchematics,[]);
  assert.match(progressionCommand(s,{type:'recover',id:source.id}),/Already/);assert.equal(s.engineer.inv.alienartifact,2);
  // Supplied post-core fixture. The separately recorded ordinary route proves acquisition.
  const core=p.sites.find(x=>x.item==='core1')!,plant=p.sites.find(x=>x.id==='plant:riverside')!;
  core.recovered=true;core.enabled=false;g.strongholds.freight={keys:[],opened:true,recovered:true,inherited:true};s.engineer.inv={...s.engineer.inv,core1:1,steel:100,copper:100,frame:6,board:3,wire:4};s.engineer.x=plant.x-.5;s.engineer.y=plant.y+.5;
  progressionCommand(s,{type:'deliver',id:plant.id});progressionCommand(s,{type:'activate',id:plant.id});assert.equal(lockReason(s,'alienworkbench'),'');assert.match(lockReason(s,'assembler2'),/M3/);
  const gen=addMachine(s,'generator',94,355,0);gen.inv.coal=40;const recipient=addMachine(s,'assembler',90,363,0);s.flow!.ledger=openLedger(s);
  s.engineer.x=97.5;s.engineer.y=363.5;const m=place(s,'alienworkbench',98,363,0)!;assert.ok(m);assert.equal(s.engineer.inv.frame,2);assert.equal(s.engineer.inv.board,1);
  const command=(c:Parameters<typeof applyCommands>[1][number])=>{applyCommands(s,[c]);assert.ok(actionResult(s).ok,actionResult(s).reason);};
  command({type:'fabrication',action:{type:'decode',machine:m.id}});s.speed=1;advanceFlow(s,7,[],141);assert.ok(m.decode!.progress>6);
  const paused=loadState(s),before=paused.flow!.machines.find(x=>x.id===m.id)!.decode!.progress;paused.speed=1;paused.flow!.machines.find(x=>x.id===gen.id)!.inv.coal=0;advanceFlow(paused,3,[],61);assert.equal(paused.flow!.machines.find(x=>x.id===m.id)!.decode!.progress,before);
  s=loadState(s);s.speed=1;advanceFlow(s,9,[],181);assert.deepEqual(s.campaign!.progression!.gameplay!.learnedSchematics,['overclock']);assert.equal(s.engineer.inv.alienartifact,2);
  for(const [item,n] of [['alienartifact',2],['frame',2],['board',1],['wire',4]] as const)command({type:'factory',action:{type:'machineTransfer',id:m.id,item,n,put:true}});
  advanceFlow(s,21,[],421);assert.equal(s.flow!.machines.find(x=>x.id===m.id)!.out,1);
  command({type:'factory',action:{type:'machineTransfer',id:m.id,item:'overclock',n:1,put:false}});
  s.engineer.x=recipient.x+.5;s.engineer.y=recipient.y-.5;
  for(const type of ['attach','detach','attach'] as const)command({type:'progression',action:type==='attach'?{type,machine:recipient.id,item:'overclock'}:{type,machine:recipient.id}});
  assert.equal(heldItems(s).total.overclock,1);assert.equal(heldItems(s).total.alienartifact,0);assert.equal(stateProblem(s),'');assert.ok(conservation(s).ok,conservation(s).problems.join(';'));assert.equal(heldItems(loadState(s)).total.overclock,1);
});
test('optional reoccupation postpones while close/observed and yields replacement Artifacts without another key',()=>{
  const s=createCampaign(),g=s.campaign!.progression!.gameplay!,r=g.region!.sites['freight:camp:1'],T=s.flow!.threat!;
  T.crawlers=T.crawlers.filter(c=>c.gp?.source!=='freight:camp:1');s.engineer.x=95.5;s.engineer.y=286.5;
  regionCommand(s,{type:'claimKey',id:'freight:camp:1'});tickFirstRegion(s);s.t=FABRICATION.repeatSeconds+1;tickFirstRegion(s);assert.equal(r.cycle,undefined);
  s.engineer.x=70.5;s.engineer.y=363.5;regionCommand(s,{type:'watchCamp',id:'freight:camp:1'});tickFirstRegion(s);assert.equal(r.cycle,undefined);
  s.t+=3;tickFirstRegion(s);assert.equal(r.cycle,1);assert.equal(r.guards.length,20);
  T.crawlers=T.crawlers.filter(c=>c.gp?.source!=='freight:camp:1');s.engineer.x=95.5;s.engineer.y=286.5;
  assert.match(regionCommand(s,{type:'salvage',id:'freight:camp:1'}),/2 shared Artifacts/);assert.match(regionCommand(s,{type:'salvage',id:'freight:camp:1'}),/No unclaimed/);
  assert.equal(s.engineer.inv.alienartifact,2);assert.deepEqual(g.strongholds.freight.keys,['freight:camp:1']);assert.deepEqual(g.learnedSchematics,[]);assert.equal(stateProblem(loadState(s)),'');
});
