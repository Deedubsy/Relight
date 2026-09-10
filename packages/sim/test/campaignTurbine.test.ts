import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {ground,findPath,passable,canPlace,applyCommands,advanceFlow,inReach,MACHINE_SIZE,machineAt,blockOfTile,conservation,openLedger,makeSave,loadState,stateHash,TURBINE,turbineCheck,turbineReachProblem,campaignGrid,surveyedDistrict,lockReason,blockLights,inspectMachine,type SimState,type Kind,type Command} from '../src/index';
import {createSession,parseUrl,replaySession} from '../../game/src/session';
const run=(s:SimState,n:number)=>advanceFlow(s,n,[],Math.ceil(n*20)+1);
function walk(s:SimState,x:number,y:number,size=1,send=(c:Command)=>applyCommands(s,[c])){
 if(inReach(s,x,y,size))return;
 let target:number[]|undefined;
 for(let r=0;r<5&&!target;r++)for(let yy=y-r;yy<y+size+r&&!target;yy++)for(let xx=x-r;xx<x+size+r;xx++)if(passable(s,xx,yy)&&findPath(s,Math.floor(s.engineer.x),Math.floor(s.engineer.y),xx,yy)){target=[xx,yy];break;}
 assert.ok(target);send({type:'move',x:target[0]+.5,y:target[1]+.5});for(let i=0;i<240&&s.engineer.target;i++)run(s,1);assert.ok(inReach(s,x,y,size),`reach ${x},${y}`);
}
function place(s:SimState,k:Kind,x:number,y:number){walk(s,x,y,MACHINE_SIZE[k]);assert.ok(canPlace(s,k,x,y).ok,canPlace(s,k,x,y).reason);applyCommands(s,[{type:'place',item:k,x,y}]);assert.equal(machineAt(s,x,y)?.kind,k);return machineAt(s,x,y)!;}
function near(s:SimState,k:Kind,x:number,y:number,bi:number){for(let r=1;r<25;r++)for(let yy=y-r;yy<=y+r;yy++)for(let xx=x-r;xx<=x+r;xx++)if(blockOfTile(s,xx,yy)===bi&&canPlace(s,k,xx,yy).ok)return place(s,k,xx,yy);throw Error(`no ${k}`);}
const conserved=(s:SimState)=>assert.ok(conservation(s).ok,conservation(s).problems.join(', '));

test('optional riverside hall and shelters preserve corridors, reach and stable identities on required seeds',()=>{
 for(const seed of [3,4,5,8,11,13]){
  const s=createCampaign(seed),g=ground(s),h=s.campaign!.turbine!,routes=[...s.campaign!.expansion!.route,...s.campaign!.districts!.route];
  assert.ok(findPath(s,Math.floor(s.engineer.x),Math.floor(s.engineer.y),h.x-1,h.y));assert.equal(passable(s,h.x,h.y),false);
  assert.match(canPlace(s,'pole',h.x,h.y).reason,/Turbine/);assert.ok(routes.every(t=>!(t%g.tw>=h.x&&t%g.tw<h.x+h.size&&Math.floor(t/g.tw)>=h.y&&Math.floor(t/g.tw)<h.y+h.size)));
  for(const r of s.campaign!.recruits!.sites)assert.ok(findPath(s,Math.floor(s.engineer.x),Math.floor(s.engineer.y),r.x,r.y));
  assert.equal(s.campaign!.defence!.bases.length,1);assert.equal(stateHash(loadState(makeSave(s))),stateHash(s));
  const old=structuredClone(s);delete old.campaign!.knowledge;old.campaign!.version=7;old.campaign!.recruits!.version=2;old.campaign!.recruits!.sites=old.campaign!.recruits!.sites.slice(0,2);delete old.campaign!.turbine;
  const migrated=loadState(old);assert.deepEqual(migrated.campaign!.recruits!.sites.slice(0,2),old.campaign!.recruits!.sites);assert.deepEqual(migrated.flow!.machines,old.flow!.machines);assert.deepEqual(migrated.campaign!.turbine,h);
 }
});

test('ordinary optional recruitment works in both orders, grants paid Arc coverage and district-only survey, and replays',()=>{
 for(const order of [['lamplighters','surveyors'],['surveyors','lamplighters']]){
  const previous=Object.getOwnPropertyDescriptor(globalThis,'location');Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
  const session=(()=>{try{return createSession(parseUrl('?rules=exploration-v2&seed=3&view=world'));}finally{if(previous)Object.defineProperty(globalThis,'location',previous);else Reflect.deleteProperty(globalThis,'location');}})(),s=session.state;
  const send=(c:Command)=>{session.log.push({tick:s.flow!.tick,c:structuredClone(c)});applyCommands(s,[c]);};
  const home=[Math.floor(s.engineer.x),Math.floor(s.engineer.y)];send({type:'chestTake',item:'steel',n:20});send({type:'chestTake',item:'copper',n:20});
  assert.equal(surveyedDistrict(s,0),null);assert.match(lockReason(s,'arclamp'),/Lamplighters/);
  const cache=structuredClone(s.campaign!.discovery!),bases=s.campaign!.defence!.bases.length,stock={...s.engineer.inv};
  for(const kind of order){const r=s.campaign!.recruits!.sites.find(r=>r.kind===kind)!;walk(s,r.x,r.y,1,send);send({type:'recruitSurvivors',id:r.id});const at=r.recruitedAt;send({type:'recruitSurvivors',id:r.id});assert.equal(r.recruitedAt,at);}
  assert.deepEqual(s.engineer.inv,stock);assert.equal(s.campaign!.defence!.bases.length,bases);assert.equal(s.campaign!.discovery!.recoveredAt,cache.recoveredAt);
  for(let bi=0;bi<s.blocks.length;bi++)assert.equal(surveyedDistrict(s,bi),s.blocks[bi].name);
  walk(s,home[0],home[1],1,send);let spot:number[]|undefined;
  for(let y=home[1]-5;y<home[1]+5&&!spot;y++)for(let x=home[0]-5;x<home[0]+5;x++)if(inReach(s,x,y)&&canPlace(s,'arclamp',x,y).ok&&blockOfTile(s,x,y)===s.campaign!.homeBlock){spot=[x,y];break;}
  assert.ok(spot);send({type:'construct',edits:[{action:'place',item:'arclamp',x:spot[0],y:spot[1],dir:0}]});const m=machineAt(s,...spot as [number,number])!;
  assert.equal(s.engineer.inv.steel,stock.steel-4);assert.equal(s.engineer.inv.copper,stock.copper-4);
  const light=blockLights(s,s.campaign!.homeBlock).find(l=>l.tx===m.x&&l.ty===m.y)!;assert.equal(light.r,6);assert.ok(light.lit);assert.equal(inspectMachine(s,m.id)!.draw,12);
  conserved(s);const replay=replaySession(session);assert.ok('state' in replay,JSON.stringify(replay));if('state' in replay)assert.equal(stateHash(replay.state),stateHash(s));
  const copy=loadState(makeSave(s));copy.speed=s.speed;run(s,2);run(copy,2);assert.equal(stateHash(s),stateHash(copy));
  s.campaign!.defence!.bases[0].hp=0;s.flow!.rev++;assert.equal(blockLights(s,s.campaign!.homeBlock).find(l=>l.tx===m.x&&l.ty===m.y)!.lit,false);
 }
});

test('paid Turbine commissioning supplies a physical multi-district network, offsets fuel, isolates and resumes after saving',()=>{
 const s=createCampaign(),h=s.campaign!.turbine!,g=ground(s);
 // Labelled finite construction fixture: finished concrete and extra wiring/fuel isolate network mechanics.
 // Real stone-to-concrete production and ordinary-stock travel/replay have separate end-to-end coverage.
 s.engineer.inv.steel=600;s.engineer.inv.copper=400;s.engineer.inv.concrete=TURBINE.cost.concrete;s.engineer.inv.coal=50;
 s.campaign!.defence!.nextDawn=100000;s.campaign!.defence!.lastMinorSlot=100000;s.flow!.ledger=openLedger(s);
 applyCommands(s,[{type:'deliverTurbine'},{type:'restoreTurbine'},{type:'setTurbineEnabled',enabled:true}]);assert.equal(h.restoredAt,-1);assert.equal(h.delivered.concrete,0);
 walk(s,h.x,h.y,h.size);applyCommands(s,[{type:'deliverTurbine'}]);assert.equal(h.delivered.concrete,40);assert.equal(s.engineer.inv.concrete??0,0);conserved(s);
 const partial=loadState(makeSave(s));assert.deepEqual(partial.campaign!.turbine!.delivered,h.delivered);assert.match(turbineCheck(s),/Commissioning/);
 const gen=near(s,'generator',h.x,h.y,h.block);walk(s,gen.x,gen.y,2);applyCommands(s,[{type:'feed',x:gen.x,y:gen.y}]);
 walk(s,h.x,h.y,h.size);assert.equal(turbineCheck(s),'');applyCommands(s,[{type:'restoreTurbine'},{type:'restoreTurbine'}]);assert.equal(h.enabled,true);assert.equal(s.flow!.stats.placed.concrete,40);assert.equal(s.campaign!.defence!.bases.length,1);conserved(s);
 assert.notEqual(campaignGrid(s).blocks[h.block],campaignGrid(s).blocks[s.campaign!.homeBlock]);
 s.campaign!.recruits!.sites.find(r=>r.kind==='concrete')!.recruitedAt=0;s.campaign!.recruits!.sites.find(r=>r.kind==='concrete')!.seenAt=0;
 const load=near(s,'mixer',h.x,h.y,h.block); // Unlock below is set before this call in the fixture.
 assert.equal(campaignGrid(s).blocks[h.block].turbineSupply,600);assert.equal(campaignGrid(s).turbineOutput,60);const coal=gen.inv.coal;run(s,5);assert.equal(gen.inv.coal,coal);
 // Use real paid poles along a traversable route between existing substation service areas.
 const homeSub=g.blocks[s.campaign!.homeBlock].sub!,remote=g.blocks[h.block].sub!;
 const path=findPath(s,homeSub.x-1,homeSub.y-1,remote.x-1,remote.y-1);assert.ok(path);
 const poles=[];for(const t of path){const x=t%g.tw,y=Math.floor(t/g.tw);if(canPlace(s,'pole',x,y).ok)poles.push(place(s,'pole',x,y));}
 assert.equal(campaignGrid(s).blocks[h.block],campaignGrid(s).blocks[s.campaign!.homeBlock]);assert.ok(campaignGrid(s).turbineOutput>=160);
 walk(s,h.x,h.y,h.size);applyCommands(s,[{type:'setTurbineEnabled',enabled:false}]);assert.equal(campaignGrid(s).turbineOutput,0);assert.equal(h.enabled,false);
 const copy=loadState(makeSave(s));copy.speed=s.speed;applyCommands(copy,[{type:'setTurbineEnabled',enabled:true}]);applyCommands(s,[{type:'setTurbineEnabled',enabled:true}]);run(s,2);run(copy,2);assert.equal(stateHash(s),stateHash(copy));
 for(const p of poles){walk(s,p.x,p.y);applyCommands(s,[{type:'pickUp',x:p.x,y:p.y}]);}
 assert.notEqual(campaignGrid(s).blocks[h.block],campaignGrid(s).blocks[s.campaign!.homeBlock]);assert.equal(campaignGrid(s).turbineOutput,60);assert.ok(load);conserved(s);
});

test('Turbine commands reject incapacitated/vehicle interaction and corrupt saved records',()=>{
 const s=createCampaign(),h=s.campaign!.turbine!;s.engineer.x=h.x-.5;s.engineer.y=h.y+.5;
 s.engineer.down=0;assert.notEqual(turbineReachProblem(s),'');s.engineer.down=-1;s.engineer.truckSeat=true;assert.notEqual(turbineReachProblem(s),'');delete s.engineer.truckSeat;
 for(const change of [(x:SimState)=>{x.campaign!.turbine!.enabled=true;},(x:SimState)=>{x.campaign!.turbine!.delivered.concrete=-1;},(x:SimState)=>{x.campaign!.turbine!.restoredAt=1;},(x:SimState)=>{delete x.campaign!.turbine;}]){const c=structuredClone(s);change(c);assert.throws(()=>loadState(c),/Turbine/);}
});
