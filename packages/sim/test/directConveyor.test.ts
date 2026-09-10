import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import {writeFileSync} from 'node:fs';
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {ensureFlow,applyCommands,actionResult,canPlace,placeable,inReach,machineAt,dimensions,stepFlow,conservation,stateHash,makeSave,loadState,advanceFlow,blockOfTile,passable,type SimState,type Kind,type Dir,type Machine,type Command} from '../src/index';

function send(st:SimState,c:Command,ok=true){applyCommands(st,[c]);assert.equal(actionResult(st).ok,ok,actionResult(st).reason);}
function fresh(){const st=createCampaign(3);ensureFlow(st);for(const [item,n] of [['steel',200],['copper',100],['coal',20]] as const)send(st,{type:'factory',action:{type:'chestTake',item,n}});return st;}
type Plan=[Kind,number,number,Dir?][];
function spot(st:SimState,plan:Plan):[number,number]{
 const e=st.engineer;
 for(let y=Math.floor(e.y)-7;y<e.y+7;y++)for(let x=Math.floor(e.x)-7;x<e.x+7;x++){
  if(plan.every(([k,dx,dy,dir=0])=>canPlace(st,k,x+dx,y+dy,dir).ok&&inReach(st,x+dx,y+dy,...dimensions(k,dir,k==='chest'?2:1))))return [x,y];
 }
 throw new Error('No reachable fixture footprint');
}
function build(st:SimState,kind:Kind,x:number,y:number,dir:Dir=1):Machine{
 send(st,{type:'construct',edits:[{action:'place',item:kind,x,y,dir}]});return machineAt(st,x,y)!;
}
function planBuild(st:SimState,plan:Plan){const [x,y]=spot(st,plan);return plan.map(([k,dx,dy,d])=>build(st,k,x+dx,y+dy,d));}
const conserved=(st:SimState)=>assert.ok(conservation(st).ok,conservation(st).problems.join(', '));
const ticks=(st:SimState,n:number)=>{for(let i=0;i<n;i++)stepFlow(st);};

test('direct belts: the complete mixed chest-to-assembler-to-chest-to-turret line runs without inserters',()=>{
 const st=fresh(),plan:Plan=[['chest',0,0],['belt',2,1,1],['assembler',3,0],['belt',6,1,1],['chest',7,1],['belt',9,1,1],['turret',10,0],['generator',4,4]];
 const e=st.engineer;let origin:[number,number]|undefined;
 outer:for(let radius=10;radius<=70;radius+=10)for(let y=Math.floor(e.y)-radius;y<=e.y+radius;y++)for(let x=Math.floor(e.x)-radius;x<=e.x+radius;x++){
  const bi=blockOfTile(st,x+4,y+4);if(!passable(st,x+6,y+4)||plan.some(([,dx,dy])=>blockOfTile(st,x+dx,y+dy)!==bi))continue;
  if(plan.every(([k,dx,dy,d=0])=>!placeable(st,k,x+dx,y+dy,d))){origin=[x,y];break outer;}
 }
 assert.ok(origin);const [x,y]=origin;applyCommands(st,[{type:'move',x:x+6.5,y:y+4.5}]);st.speed=1;
 for(let i=0;i<6000&&Math.hypot(e.x-x-6.5,e.y-y-4.5)>.8;i++)advanceFlow(st,.05,[]);
 assert.ok(Math.hypot(e.x-x-6.5,e.y-y-4.5)<.8,'walk to the real site');
 const machines=plan.map(([k,dx,dy,d])=>build(st,k,x+dx,y+dy,d)),source=machines[0],asm=machines[2],store=machines[4],turret=machines[6],gen=machines[7];
 send(st,{type:'factory',action:{type:'feed',x:gen.x,y:gen.y}});
 for(const [item,n] of [['steel',30],['copper',15]] as const)send(st,{type:'factory',action:{type:'chestPut',x:source.x,y:source.y,item,n}});
 if(process.env.RELIGHT_DIRECT_FIXTURE)writeFileSync(process.env.RELIGHT_DIRECT_FIXTURE,JSON.stringify(makeSave(st)));
 ticks(st,800);assert.ok(turret.inv.rounds>0,'magazines arrive directly in turret');assert.ok(asm.inv.steel>=0&&asm.inv.copper>=0);conserved(st);
 const loaded=loadState(makeSave(st));for(let i=0;i<800;i++){stepFlow(st);stepFlow(loaded);}assert.equal(stateHash(st),stateHash(loaded),'pickup fairness survives save/load');
 assert.equal(turret.inv.rounds,50,'full turret stops loading');assert.ok((store.inv.magazine??0)>0,'finished magazines buffer in the intermediate chest');conserved(st);
});

test('direct belts: any storage side can unload, and pointing inward never steals from the destination',()=>{
 for(const dir of [0,1,2,3] as Dir[]){
  const plan:Plan=dir===0?[['chest',0,3],['belt',0,2,0],['chest',0,0]]:dir===1?[['chest',0,0],['belt',2,0,1],['chest',3,0]]:dir===2?[['chest',0,0],['belt',0,2,2],['chest',0,3]]:[['chest',3,0],['belt',2,0,3],['chest',0,0]];
  const st=fresh(),[source,belt,dest]=planBuild(st,plan);send(st,{type:'factory',action:{type:'chestPut',x:source.x,y:source.y,item:'steel',n:5}});
  ticks(st,100);assert.equal(dest.inv.steel,5);assert.equal(source.inv.steel,0);assert.equal(belt.items.length,0);conserved(st);
 }
});

test('direct belts: incompatible mixed stock stays at its source and full destinations conserve items',()=>{
 const st=fresh(),[source,belt,turret]=planBuild(st,[['chest',0,0],['belt',2,0,1],['turret',3,0]]);
 send(st,{type:'factory',action:{type:'chestTake',item:'magazine',n:10}});
 for(const [item,n] of [['steel',5],['magazine',10]] as const)send(st,{type:'factory',action:{type:'chestPut',x:source.x,y:source.y,item,n}});
 ticks(st,150);assert.equal(turret.inv.rounds,50);assert.equal(source.inv.steel,5);assert.equal(source.inv.magazine,5);assert.equal(belt.items.length,0);conserved(st);
});


test('direct belts: Home supplies and turret hoppers can unload without creating stock',()=>{
 const st=fresh(),home=st.flow!.machines.find(m=>m.kind==='depot')!;
 let belt:Machine|undefined;
 for(const [dx,dy,d] of [[home.size,1,1],[-1,1,3],[1,home.size,2],[1,-1,0]] as const){if(canPlace(st,'belt',home.x+dx,home.y+dy,d).ok&&inReach(st,home.x+dx,home.y+dy)){belt=build(st,'belt',home.x+dx,home.y+dy,d);break;}}
 assert.ok(belt,'reachable Home output');ticks(st,20);assert.ok(belt.items.length>0);conserved(st);
 const second=fresh(),[turret,out,dest]=planBuild(second,[['turret',0,0],['belt',2,0,1],['chest',3,0]]);
 send(second,{type:'factory',action:{type:'chestTake',item:'magazine',n:5}});send(second,{type:'factory',action:{type:'feed',x:turret.x,y:turret.y}});
 ticks(second,100);assert.equal(turret.inv.rounds,0);assert.equal(dest.inv.magazine,5);assert.equal(out.items.length,0);conserved(second);
});
