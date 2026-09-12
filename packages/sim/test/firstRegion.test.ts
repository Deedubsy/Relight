import {test} from 'node:test';
import assert from 'node:assert/strict';
import {baseCore,createCampaign,validateFirstRegion,FIRST_CAMPS,FREIGHT_GATES,regionCommand,regionCheck,passable,citySight,loadState,stateProblem,progressionCheck,tickCombatActor,tickSpit} from '../src/index';

test('authored exterior routes, finite populations and both permanent gates survive distinct claims/load',()=>{
  let s=createCampaign();assert.deepEqual(validateFirstRegion(s),[]);
  const g=s.campaign!.progression!.gameplay!,r=g.region!;
  assert.deepEqual(FIRST_CAMPS.map(c=>r.sites[c.id].guards.length),[20,22,20]);assert.equal(r.sites.freight.guards.length,61);
  for(const p of FREIGHT_GATES)assert.equal(passable(s,p.x,p.y),false);
  assert.equal(citySight(s,66.5,63.5,66.5,59.5),false);
  assert.match(regionCheck(s,{type:'claimKey',id:FIRST_CAMPS[0].id}),/Walk/);
  // Focused defeated-roster setup, not an ordinary-equipment playthrough.
  for(const c of FIRST_CAMPS){
    s.engineer.x=c.x+.5;s.engineer.y=c.y+.5;
    assert.match(regionCheck(s,{type:'claimKey',id:c.id}),/defenders/);
    s.flow!.threat!.crawlers=s.flow!.threat!.crawlers.filter(a=>a.gp?.source!==c.id);
    assert.match(regionCommand(s,{type:'claimKey',id:c.id}),/secured/);
    assert.match(regionCommand(s,{type:'claimKey',id:c.id}),/already/);
    s=loadState(s);
  }
  s.engineer.x=66.5;s.engineer.y=63.5;
  assert.match(regionCommand(s,{type:'openFreight',id:'freight'}),/permanently/);
  assert.equal(citySight(s,66.5,63.5,66.5,59.5),true);
  s=loadState(s);for(const p of FREIGHT_GATES)assert.equal(passable(s,p.x,p.y),true);
  s.engineer.x=62.5;s.engineer.y=49.5;assert.match(progressionCheck(s,{type:'recover',id:'core:freight'}),/defenders/);
  assert.equal(stateProblem(s),'');
});
test('saved committed Spitter aim, swept cover collision and guardian recovery retain counterplay',()=>{
  const s=createCampaign(),T=s.flow!.threat!,c=T.crawlers.find(c=>c.gp?.kind==='spitter')!;
  // Explicit isolated combat fixture on clear street; geometry remains canonical.
  c.x=72.5;c.y=270.5;c.gp!.home=[c.x,c.y];s.engineer.x=77.5;s.engineer.y=270.5;
  tickCombatActor(s,T,c,.05);assert.equal(c.gp!.phase,'windup');
  const loaded=loadState(s).flow!.threat!.crawlers.find(a=>a.id===c.id)!;assert.deepEqual(loaded.gp,c.gp);
  s.engineer.y=268.5;s.t=1;tickCombatActor(s,T,c,.05);assert.equal(T.projectiles!.length,1);assert.equal(T.projectiles![0].vy,0);
  T.projectiles=[{x:79.5,y:280.5,vx:8,vy:0,life:2,source:c.id}];tickSpit(s,T,.2);assert.equal(T.projectiles.length,0);
  const core=baseCore(s,s.campaign!.homeBlock)!;T.projectiles=[{x:core.x+.1,y:core.y+.5,vx:8,vy:0,life:2,source:c.id}];const hp=core.hp;tickSpit(s,T,.05);assert.equal(core.hp,hp-10);assert.equal(T.projectiles.length,0);
  const boss=T.crawlers.find(a=>a.gp?.kind==='guardian')!;boss.gp!.phase='charge';boss.gp!.aim=[80,43];boss.gp!.until=3;s.t=1;
  boss.x=75.5;boss.y=43.5;tickCombatActor(s,T,boss,.2);assert.equal(boss.gp!.phase,'recover');assert.ok(boss.x<76);
});
