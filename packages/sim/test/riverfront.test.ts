import {test} from 'node:test';
import assert from 'node:assert/strict';
import {writeFileSync,readFileSync,mkdtempSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import * as S from '../src/index';
import {validateRiverfront} from '../src/city/validateRiverfront';
import {cityStep,cityNavStats} from '../src/cityNavigation';
import {campaignOrigin} from '../src/campaignThreat';
// Regression runs must never replace the historical CITY-C evidence with a newer layout.
const C=S.RIVERFRONT,W=C.width,baselineDir='../../docs/evidence/city-c/',out=mkdtempSync(join(tmpdir(),'relight-riverfront-test-'))+'/';
const fresh=()=>{const s=S.createCampaign();s.speed=1;return s;};
const run=(s:S.SimState,n:number)=>S.advanceFlow(s,n,[],Math.ceil(n*20)+1);
const near=(s:S.SimState,x:number,y:number)=>{s.engineer.x=x+.5;s.engineer.y=y+.5;s.engineer.block=S.blockOfTile(s,x,y);};
function local(s:S.SimState,o:{x:number;y:number;size?:number}){for(let y=o.y-2;y<=o.y+(o.size??1)+2;y++)for(let x=o.x-2;x<=o.x+(o.size??1)+2;x++)if(S.passable(s,x,y)){near(s,x,y);if(S.inReach(s,o.x,o.y,o.size??1))return;}throw Error('No accessible interaction');}
function roundtrip(s:S.SimState){assert.equal(S.stateProblem(s),'');assert.equal(S.stateHash(S.loadState(S.makeSave(s))),S.stateHash(s));}
function command(s:S.SimState,a:S.ProgressionAction){S.applyCommands(s,[{type:'progression',action:a}]);assert.equal(S.actionResult(s).ok,true,S.actionResult(s).reason);}
function build(s:S.SimState,k:S.Kind,x:number,y:number,dir:S.Dir=1){near(s,x-1,y);assert.equal(S.canPlace(s,k,x,y,dir).reason,'',`${k} at ${x},${y}`);const m=S.place(s,k,x,y,dir);assert.ok(m,k);return m;}
const supplied=(s:S.SimState)=>{s.engineer.inv={steel:1500,copper:1000,coal:200,polymer:100};};
const generator=(s:S.SimState,i:number)=>{const y=C.yards[i];return build(s,'generator',y.x+2,y.y+8);};

test('full city structural acceptance, real accessible area and ordinary empty new game',()=>{
 const a=fresh(),b=S.createCampaign(42),{reachable,...v}=validateRiverfront(a);assert.deepEqual(v.errors,[]);assert.ok(reachable.length);const baseline=JSON.parse(readFileSync(baselineDir+'baseline.json','utf8'));assert.ok(v.land/baseline.land>2.8&&v.land/baseline.land<3.2);writeFileSync(out+'validation.json',JSON.stringify({...v,baselineAccessible:baseline.accessible,ratio:v.accessible/baseline.accessible},null,2));
 assert.deepEqual(a.city,b.city);assert.deepEqual(a.campaign!.progression,b.campaign!.progression);assert.deepEqual(a.engineer.inv,{...S.CAMPAIGN_START_POCKETS});assert.equal(a.stock.steel+a.stock.copper+a.buffer,0); // GP-START-POCKETS: Backpack stake, Home storage emptyassert.equal(S.fixedStops(a).length,4);assert.equal(a.flow!.machines.filter(m=>m.kind==='track').length,a.campaign!.fixedTram!.route.length);assert.ok(a.campaign!.fixedTram!.route.every(t=>S.passable(a,t%W,Math.floor(t/W))));assert.ok(!a.flow!.machines.some(m=>m.kind==='generator'));roundtrip(a);
});
test('all authored versions reject safely; existing procedural saves retain their geometry',()=>{
 for(const path of [baselineDir+'before-save.json','../../docs/evidence/riverfront/bootstrap-save.json']){const raw=readFileSync(path,'utf8'),old=JSON.parse(raw);assert.throws(()=>S.loadState(old),/earlier authored city/);assert.equal(JSON.stringify(old),JSON.stringify(JSON.parse(raw)));}
 const old=S.loadState(JSON.parse(readFileSync('../../docs/evidence/gameplay-corrections/ordinary-save.json','utf8')));assert.equal(old.city?.mapId,undefined);assert.equal(S.ground(old).tw,800);
});
test('four factories fit real machinery, freight handling, defence and repair lanes',()=>{
 const layouts=[];for(const [i,yard]of C.yards.entries()){const s=fresh();supplied(s);const x=yard.x+2,y=yard.y+2,parts:[S.Kind,number,number][]=[['chest',0,0],['belt',2,0],['assembler',3,0],['belt',6,0],['chest',7,0],['belt',9,0],['turret',10,0],['generator',0,5],['generator',5,5],['chest',10,5],['turret',16,5]];for(const[k,dx,dy]of parts){const m=build(s,k,x+dx,y+dy);assert.equal(S.blockOfTile(s,m.x,m.y),i);if(k==='generator')m.inv.coal=20;}assert.ok(S.findPath(s,x-1,y-1,x+20,y+10));assert.ok(S.canPlace(s,'assembler2',x+21,y+4).ok);layouts.push({site:i,yard,placed:parts});}writeFileSync(out+'yard-layouts.json',JSON.stringify(layouts,null,2));
});
test('hand gathering, ordinary movement, construction and finite Home lighting',()=>{
 const s=fresh();for(const [i,count]of [[0,80],[1,40],[2,10]]){const[item,x,y]=C.resources[i],old=[s.engineer.x,s.engineer.y];local(s,{x,y});const to=[s.engineer.x,s.engineer.y];[s.engineer.x,s.engineer.y]=old;S.applyCommands(s,[{type:'move',x:to[0],y:to[1]}]);run(s,15);assert.ok(Math.hypot(s.engineer.x-to[0],s.engineer.y-to[1])<1);S.setHandMine(s,[x,y]);run(s,count+1);S.setHandMine(s,null);assert.ok((s.engineer.inv[item as S.Item]??0)>=count,item);}
 const g=generator(s,0);S.applyCommands(s,[{type:'factory',action:{type:'feed',x:g.x,y:g.y}}]);assert.ok(g.inv.coal!>0);const m=build(s,'assembler',C.yards[0].x+7,C.yards[0].y+8);m.inv={steel:4,copper:2};run(s,9);assert.ok(m.out>0);assert.equal(S.blockLights(s,0).filter(l=>l.lit).length,6);roundtrip(s);writeFileSync(out+'bootstrap-save.json',JSON.stringify(S.makeSave(s)));g.inv.coal=0;g.timer=0;s.flow!.rev++;s.flow!.tick++;assert.equal(S.blockLights(s,0).filter(l=>l.lit).length,0);
});
test('all deposits remain accessible and are never buried under buildings',()=>{
 const s=fresh(),G=S.ground(s),start=[Math.floor(s.engineer.x),Math.floor(s.engineer.y)];for(const[item,x,y,w,h]of C.resources){assert.ok(S.rubbleAt(s,x,y),item);local(s,{x,y});assert.ok(S.findPath(s,...start as [number,number],Math.floor(s.engineer.x),Math.floor(s.engineer.y)),item);for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++)assert.equal(G.urban!.solid[yy*W+xx],0,item);}
});
test('existing core, any-plant activation, artifact and foreman recruitment retain ownership',()=>{
 for(let i=0;i<3;i++){const s=fresh(),p=s.campaign!.progression!;s.engineer.inv={steel:30,copper:15};const core=p.sites.find(p=>p.item==='core1')!;local(s,core);core.guards=[];command(s,{type:'recover',id:core.id});const plant=p.sites.filter(p=>p.kind==='plant')[i];local(s,plant);command(s,{type:'deliver',id:plant.id});command(s,{type:'activate',id:plant.id});assert.equal(S.campaignGrid(s).blocks[plant.block].plantSupply,600);assert.ok(campaignOrigin(s,S.baseCore(s,plant.block)!)>=0);const a=p.sites.find(p=>p.item==='artifact1')!;local(s,a);command(s,{type:'recover',id:a.id});assert.equal(s.engineer.inv.artifact1,1);assert.deepEqual(S.progressionDiscoveries(s).find(p=>p.id===a.id)?.actions,[]);const crew=s.campaign!.recruits!.sites.find(r=>r.kind==='foreman')!;local(s,crew);S.applyCommands(s,[{type:'recruitSurvivors',id:crew.id}]);assert.ok(S.campaignRecruited(s,'foreman'));roundtrip(s);if(i===0)writeFileSync(out+'plant-save.json',JSON.stringify(S.makeSave(s)));}
});
test('continuous tram bends, bypass of unpowered stops, real freight and passenger travel',()=>{
 const s=fresh();supplied(s);const stops=S.fixedStops(s);generator(s,0).inv.coal=100;generator(s,3).inv.coal=100;assert.deepEqual(S.fixedPoweredStops(s).map(m=>m.id),[stops[0].id,stops[3].id]);local(s,stops[0]);assert.ok(S.setStationRules(s,stops[0].x,stops[0].y,{steel:{request:0,reserve:0,export:true}}));S.chestPut(s,'steel',20,[stops[0].x,stops[0].y]);local(s,stops[3]);assert.ok(S.setStationRules(s,stops[3].x,stops[3].y,{steel:{request:20,reserve:0,export:false}}));local(s,stops[0]);run(s,.1);command(s,{type:'board'});
 const tram=s.flow!.machines.find(m=>m.id===s.campaign!.fixedTram!.tram)!,samples=[];let prev=S.riverfrontTramPose(tram),curved=0;for(let i=0;i<740;i++){run(s,.05);const p=S.riverfrontTramPose(tram),step=Math.hypot(p.x-prev.x,p.y-prev.y);assert.ok(step<=2.01,`tram jump ${step}`);assert.ok(Math.hypot(s.engineer.x-p.x,s.engineer.y-p.y)<.001);if(Math.abs(Math.sin(p.angle*2))>.01)curved++;samples.push({t:s.t,...p,phase:tram.phase});prev=p;}
 assert.ok(curved>10);assert.equal(stops[3].cargo?.steel,20);roundtrip(s);command(s,{type:'exit'});assert.ok(!s.campaign!.progression!.passenger);writeFileSync(out+'tram-motion.json',JSON.stringify({curvedSamples:curved,samples},null,2));writeFileSync(out+'network-save.json',JSON.stringify(S.makeSave(s)));
});
test('vertical platform has two working side loading lanes, with running line and boarding protected',()=>{
 const s=fresh();supplied(s);generator(s,3).inv.coal=100;const p=S.fixedStops(s)[3],source=build(s,'chest',p.x-3,p.y-1),input=build(s,'belt',p.x-1,p.y),output=build(s,'belt',p.x-1,p.y+1,3),dest=build(s,'chest',p.x-3,p.y+1);source.inv.steel=12;run(s,15);assert.equal(dest.inv.steel,12);assert.equal(source.inv.steel??0,0);assert.equal(p.inv.steel??0,0);assert.ok(input&&output);assert.ok(S.cityRailReserved(s,p.x+2,p.y,'belt'));assert.ok(S.cityRailReserved(s,p.x,p.y-1,'wall'));roundtrip(s);
});
test('interior doors, wall sight, saved clearing and barred shortcut retain collision',()=>{
 const s=fresh(),b=S.RIVERFRONT_BUILDINGS.find(b=>b.id==='westridge-rowan')!;near(s,b.x-1,b.y+3);assert.equal(S.citySight(s,s.engineer.x,s.engineer.y,b.x+3,b.y+3),false);const[x,y]=b.door!;assert.ok(S.findPath(s,x+1,y+2,b.x+5,b.y+4));near(s,b.x+5,b.y+4);S.tickAuthored(s);assert.ok(s.campaign!.authored!.visited.includes(b.id));const brush=S.RIVERFRONT_PROPS.find(p=>p.id==='court-brush')!;local(s,brush);assert.ok(!S.passable(s,brush.x,brush.y));S.applyCommands(s,[{type:'cityProp',id:brush.id}]);assert.ok(S.passable(s,brush.x,brush.y));const gate=S.RIVERFRONT_PROPS.find(p=>p.id==='westridge-gate')!;near(s,gate.x-2,gate.y+1);assert.match(S.cityPropReason(s,gate.id),/Barred/);near(s,gate.x+3,gate.y+1);S.applyCommands(s,[{type:'cityProp',id:gate.id}]);assert.ok(S.passable(s,gate.x,gate.y+1));roundtrip(s);
});
test('bounded shared navigation responds to construction; a solid interior stays unreachable',()=>{
 const s=fresh(),c={x:142.5,y:198.5};for(let i=0;i<12;i++){s.t+=.05;s.flow!.tick++;cityStep(s,c,163.5,198.5,.3);}const obstacle=S.addMachine(s,'chest',150,198,0);for(let i=0;i<120;i++){s.t+=.05;s.flow!.tick++;cityStep(s,c,163.5,198.5,.3);assert.ok(S.passable(s,Math.floor(c.x),Math.floor(c.y)));}assert.ok(c.x>162);local(s,obstacle);assert.ok(S.remove(s,obstacle.x,obstacle.y));const closed=S.RIVERFRONT_BUILDINGS.find(b=>!b.enterable)!;assert.equal(S.findPath(s,142,198,closed.x+2,closed.y+2),null);assert.ok(cityNavStats(s).searches<6);
 const wall=S.addMachine(s,'wall',150,198,0);assert.equal(S.citySight(s,148.5,198.5,153.5,198.5),false);local(s,wall);S.remove(s,wall.x,wall.y);assert.ok(S.citySight(s,148.5,198.5,153.5,198.5));
});
test('real Home raid reaches a target without crossing any authored wall',()=>{
 const s=fresh(),core=S.baseCore(s,0)!;assert.ok(campaignOrigin(s,core)>=0);near(s,100,370);s.t=899;s.flow!.tick=17980;const samples:number[]=[];for(let i=0;i<1500&&core.hp===300;i++){const start=performance.now();run(s,.05);samples.push(performance.now()-start);for(const c of s.flow!.threat?.crawlers??[])assert.ok(!S.ground(s).urban!.solid[Math.floor(c.y)*W+Math.floor(c.x)]);}assert.ok(core.hp<300,'raid reaches Home');samples.sort((a,b)=>a-b);writeFileSync(out+'raid.json',JSON.stringify({actors:s.flow!.threat!.crawlers.length,ticks:samples.length,p50:samples[Math.floor(samples.length*.5)],p95:samples[Math.floor(samples.length*.95)],max:samples.at(-1),coreHp:core.hp},null,2));
});
