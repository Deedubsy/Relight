import {placementGeometryProblem} from '../src/flow';
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {applyCommands,actionResult,canPlace,campaignOrigin,tickCampaignSchedule,RIVERFRONT_BUILDINGS,doorOutside,createCampaign,addMachine,fixedStops,advanceFlow,ground,blockOfTile,loadState,progressionCommand,damageCore,baseCore,conservation,openLedger,stateProblem} from '../src/index';
test('supplied integration fixture transports real ammunition and preserves chosen commissioning through outage/load',()=>{
  const s=createCampaign(),p=s.campaign!.progression!,g=p.gameplay!,core=p.sites.find(s=>s.item==='core1')!,plant=p.sites.find(s=>s.id==='plant:ironworks')!;
  // Explicit post-encounter fixture. Ordinary acquisition is exercised by the recorded command route.
  core.recovered=true;core.enabled=false;g.strongholds.freight={keys:[],opened:true,recovered:true,inherited:true};s.engineer.inv={steel:30,copper:15,core1:1};s.engineer.x=plant.x-.5;s.engineer.y=plant.y+.5;
  progressionCommand(s,{type:'deliver',id:plant.id});progressionCommand(s,{type:'activate',id:plant.id});assert.equal(plant.installed,'core1');
  const stops=fixedStops(s),source=stops[0],dest=stops[2];
  const generator=addMachine(s,'generator',94,355,0);generator.inv.coal=40;
  source.inv.magazine=7;source.freight={magazine:{request:0,reserve:0,export:true}};dest.freight={magazine:{request:7,reserve:0,export:false}};
  s.flow!.ledger=openLedger(s);s.speed=1;advanceFlow(s,120,[],2401);
  assert.equal(dest.cargo?.magazine,7);assert.equal(source.inv.magazine??0,0);assert.ok(conservation(s).ok);
  // Supplied structure placement; ammunition below is the actual freight delivery.
  const coreBody=baseCore(s,plant.block)!,origin=campaignOrigin(s,coreBody),G=ground(s),ox=origin%G.tw,oy=Math.floor(origin/G.tw);
  const entrance=doorOutside(RIVERFRONT_BUILDINGS.find(b=>b.content===plant.id)!);
  const choices:{x:number;y:number;score:number}[]=[];for(let y=plant.y-10;y<=plant.y+10;y++)for(let x=plant.x-10;x<=plant.x+10;x++)if(!placementGeometryProblem(s,'turret',x,y))choices.push({x,y,score:Math.hypot(x-entrance.x,y-entrance.y)+Math.hypot(x-plant.x,y-plant.y)*.1});choices.sort((a,b)=>a.score-b.score);
  assert.ok(choices.length);const tower=addMachine(s,'turret',choices[0].x,choices[0].y,0);
  s.engineer.x=dest.x-.5;s.engineer.y=dest.y+.5;applyCommands(s,[{type:'factory',action:{type:'chestTake',x:dest.x,y:dest.y,item:'magazine',n:7}}]);assert.ok(actionResult(s).ok,actionResult(s).reason);
  s.engineer.x=tower.x-.5;s.engineer.y=tower.y+.5;applyCommands(s,[{type:'factory',action:{type:'feed',x:tower.x,y:tower.y}}]);assert.ok(actionResult(s).ok,actionResult(s).reason);
  s.engineer.x=70.5;s.engineer.y=363.5;const d=s.campaign!.defence!;s.t=780;d.raidsStarted=1;tickCampaignSchedule(s,s.flow!.threat!);assert.equal(d.minor?.block,plant.block);
  advanceFlow(s,120,[],2401);assert.ok(coreBody.hp>0,'freight-supplied plant survived harassment');assert.equal(d.minor,null);assert.ok((tower.inv.rounds??0)<70,'supplied ammunition was fired');assert.ok(conservation(s).ok,conservation(s).problems.join(';'));
  const installed=plant.installed;damageCore(s,baseCore(s,plant.block)!,1000);
  const next=loadState(s);assert.equal(next.campaign!.progression!.sites.find(x=>x.id===plant.id)!.installed,installed);assert.deepEqual(next.campaign!.progression!.gameplay!.commissioned,[plant.id]);assert.equal(stateProblem(next),'');
});
