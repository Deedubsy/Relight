import assert from 'node:assert/strict';
import {readFileSync,writeFileSync} from 'node:fs';
import {loadState,advanceFlow,turretRecommendation,turretFireRate,aimTurret,angleDifference,TURRET_TURN_SPEED,addMachine,placementGeometryProblem,baseCore,ground,citySight,passable,makeSave,stateProblem,newCombatActor,type Crawler} from '../../../packages/sim/src/index';
const s=loadState(JSON.parse(readFileSync('docs/evidence/gp-checkpoint/player-test-save.json','utf8')).state),f=s.flow!,T=f.threat!,e=s.engineer,d=s.campaign!.defence!,G=ground(s);
// Supplied finite Home fixture, not a natural campaign or a full major assault.
s.speed=1;const turrets=f.machines.filter(m=>m.kind==='turret');assert.equal(turretRecommendation(s).count,2);assert.equal(turretRecommendation(s).complete,false);
let third:typeof turrets[number]|undefined;
for(let y=368;y<381&&!third;y++)for(let x=76;x<89&&!third;x++)if(!placementGeometryProblem(s,'turret',x,y))third=addMachine(s,'turret',x,y,1);
assert.ok(third);turrets.push(third);assert.equal(turretRecommendation(s).count,3);assert.ok(turretRecommendation(s).complete);assert.match(turretRecommendation(s).detail,/not guaranteed/);assert.match(turretRecommendation(s).text,/Enemies can attack from any direction/);
// Isolate cadence: one supplied turret, a durable nearby target, no other shooters.
f.machines=f.machines.filter(m=>m.kind!=='belt'&&m.kind!=='inserter');f.rev++;
const m=turrets[0];for(const t of turrets)t.inv.rounds=0;m.inv.rounds=50;T.crawlers=[];T.playerProjectiles=[];e.aim=null;e.x=30.5;e.y=350.5;
const site=d.sites[0];let point:[number,number]|undefined;
for(let dy=-6;dy<=6&&!point;dy++)for(let dx=-6;dx<=6&&!point;dx++){const x=m.x+1+dx,y=m.y+1+dy,r=Math.hypot(dx,dy);if(r>=4&&r<=6&&passable(s,Math.floor(x),Math.floor(y))&&citySight(s,m.x+1,m.y+1,x,y))point=[x,y];}assert.ok(point);
const c:Crawler={id:T.next++,kind:'crawler',x:point[0],y:point[1],hp:600,edge:-1,from:site.block,to:site.block,cls:2,onPlayer:false,escaped:false,born:s.t,stuck:0,campaign:{layer:'site',group:site.id,origin:site.tile},gp:newCombatActor('guardian','turret-check',1,...point)};T.crawlers.push(c);
const baseDir=m.dir;aimTurret(m,c.x,c.y,1);m.cool=0;const fired=f.stats.fired,ammo=m.inv.rounds,times:number[]=[];
for(let i=0;i<200;i++){c.x=point[0];c.y=point[1];const n=f.stats.fired;advanceFlow(s,.05,[]);if(f.stats.fired>n){times.push(i*.05);assert.ok(Math.abs(angleDifference(Math.atan2(m.turret!.shot!.y-m.y-1,m.turret!.shot!.x-m.x-1),m.turret!.angle))<1e-6);}}
const count=f.stats.fired-fired;assert.ok(count>=34&&count<=36,`cadence ${count}`);assert.equal(ammo-m.inv.rounds,count);assert.equal(m.dir,baseDir);assert.equal(turretFireRate(s),3.5);
// Switching across pi takes the short arc, smoothly, without rotating the base.
m.turret!.angle=Math.PI-.05;const before=m.turret!.angle;aimTurret(m,m.x-5,m.y+.7,.01);assert.ok(Math.abs(angleDifference(m.turret!.angle,before))<=TURRET_TURN_SPEED*.01+1e-8);assert.equal(m.dir,baseDir);
// Brief actual small defence: three loaded turrets, 12 attackers and the original finite generator supply.
T.crawlers=[];for(const t of turrets)t.inv.rounds=50;
const core=baseCore(s,s.campaign!.homeBlock)!;e.x=core.x-2;e.y=core.y+core.size+2;
const id=d.nextId++,origin=Math.floor(point[1])*G.tw+Math.floor(point[0]);d.minor={id,block:core.block,origin,retreat:false};
let spawned=0;for(let y=core.y-12;y<=core.y+17&&spawned<12;y++)for(let x=core.x-12;x<=core.x+19&&spawned<12;x++)if(passable(s,x,y)&&Math.hypot(x-e.x,y-e.y)>8&&turrets.some(t=>Math.hypot(t.x+1-x,t.y+1-y)<=8&&citySight(s,t.x+1,t.y+1,x+.5,y+.5))){const kind=spawned%4===3?'spitter':'skitter';T.crawlers.push({id:T.next++,kind:'crawler',x:x+.5,y:y+.5,hp:kind==='spitter'?50:20,edge:-1,from:core.block,to:core.block,cls:2,onPlayer:false,escaped:false,born:s.t,stuck:0,campaign:{layer:'minor',group:id,origin},gp:newCombatActor(kind,'minor:'+id,0,x+.5,y+.5)});spawned++;}
assert.equal(spawned,12);const start=f.stats.fired;for(let i=0;i<600&&T.crawlers.some(c=>c.campaign?.group===id);i++)advanceFlow(s,.05,[]);
assert.ok(core.hp>0,'prepared Home survives brief early defence');const remaining=T.crawlers.filter(c=>c.campaign?.group===id).length;assert.equal(remaining,0,'small attack defeated');assert.equal(stateProblem(s),'');
s.speed=0;writeFileSync('docs/evidence/gp-checkpoint/turret-check-save.json',JSON.stringify(makeSave(s,{logComplete:false})));
console.log(JSON.stringify({tutorial:turretRecommendation(s),rate:turretFireRate(s),shotsIn10Seconds:count,intervals:[...new Set(times.slice(1).map((t,i)=>+(t-times[i]).toFixed(2)))],rotation:'shortest arc; every shot aligned; base unchanged',smallDefence:{spawned,remaining,coreHp:core.hp,shots:f.stats.fired-start}}));
