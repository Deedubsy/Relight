import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { writeFileSync } from 'node:fs';
import { expedition, command, layKit } from './transportFixture';
import { truckRect, truckFits, truckOccupies, truckBoardCheck, boardTruck, driveTruck, truckTransfer, truckProblem, initTruck,
 invCap, invStacks, stackSize, passable, findPath, canPlace, ground, applyCommands, actionResult, advanceFlow, conservation, hurt, loadState, makeSave, stateHash,
 stationRoute, machineAt, tramRoute, type SimState, type Dir, type Machine } from '../src/index';
import { createSession, parseUrl, replaySession } from '../../game/src/session';

function beside(st:SimState):void {
 const t=st.campaign!.truck!,r=truckRect(t),e=st.engineer;
 for(let y=Math.floor(r.y)-1;y<=r.y+r.h+1;y++)for(let x=Math.floor(r.x)-1;x<=r.x+r.w+1;x++){
  if(!passable(st,x,y)||!findPath(st,Math.floor(e.x),Math.floor(e.y),x,y))continue;
  const ox=e.x,oy=e.y;e.x=x+.5;e.y=y+.5;const why=truckBoardCheck(st);e.x=ox;e.y=oy;if(why)continue;
  command(st,[{type:'move',x:x+.5,y:y+.5}]);for(let k=0;k<180&&e.target;k++)advanceFlow(st,1);
  assert.equal(truckBoardCheck(st),'');return;
 }
 throw new Error('No boarding approach');
}
let base:SimState;
function parked():SimState {base??=expedition().st;const st=loadState(makeSave(base));st.speed=1;return st;}
function send(st:SimState,action:Parameters<typeof applyCommands>[1][number]&{type:'factory'}){command(st,[action]);return actionResult(st);}
function board(st:SimState):void {beside(st);assert.equal(boardTruck(st),'');}

test('truck unlocks once at the paid restored station, keeps the kit corridor clear and replays its entire driving/cargo trip',()=>{
 const {st,log}=expedition();assert.ok(st.campaign!.truck);assert.equal(truckProblem(st),'');
 const t=st.campaign!.truck!,original={x:t.x,y:t.y};initTruck(st);assert.deepEqual({x:t.x,y:t.y},original);
 layKit(st);beside(st);assert.ok(conservation(st).ok);
 // A browser-ready checkpoint with paid track, two stops, a tram and a nearby parked truck.
 if(process.env.P5_TRANSPORT_FIXTURE)writeFileSync(process.env.P5_TRANSPORT_FIXTURE,JSON.stringify(makeSave(st,{log,logComplete:true,params:{seed:3,ruleset:'exploration-v2',map:'city',view:'world'}})));
 assert.ok(send(st,{type:'factory',action:{type:'truckCargo',item:'steel',n:10,put:true}}).ok);
 assert.ok(send(st,{type:'factory',action:{type:'truckBoard'}}).ok);assert.equal(invCap(st.engineer),40);
 command(st,[{type:'walk',dx:0,dy:1}]);advanceFlow(st,1);command(st,[{type:'walk',dx:0,dy:0}]);
 assert.ok(send(st,{type:'factory',action:{type:'truckBoard'}}).ok);assert.equal(t.cargo.steel,10);assert.ok(conservation(st).ok);
 const saved=makeSave(st);assert.equal(stateHash(loadState(saved)),stateHash(st));
 const old=Object.getOwnPropertyDescriptor(globalThis,'location');Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
 try{const s=createSession(parseUrl('?rules=exploration-v2'));s.state=st;s.log=log;const replay=replaySession(s,{rifleOff:false});assert.ok('state' in replay);assert.equal(stateHash(replay.state),stateHash(st));}finally{if(old)Object.defineProperty(globalThis,'location',old);else Reflect.deleteProperty(globalThis,'location');}
});

test('truck boarding and cargo are local; a parked footprint blocks walking, paths and construction',()=>{
 const st=parked(),t=st.campaign!.truck!,r=truckRect(t);st.engineer.x=1;st.engineer.y=1;
 const before=stateHash(st);assert.ok(boardTruck(st));assert.equal(truckTransfer(st,'steel',1,true).ok,false);assert.equal(stateHash(st),before);
 const x=Math.floor(t.x),y=Math.floor(t.y);assert.ok(truckOccupies(st,x,y));assert.equal(passable(st,x,y),false);assert.equal(findPath(st,1,1,x,y),null);assert.match(canPlace(st,'belt',x,y).reason,/truck/);
 assert.deepEqual([r.w,r.h],t.dir%2?[3,2]:[2,3]);
});

test('cargo capacity is separate, partial stacks fit, and invalid or distant transfers preserve both inventories',()=>{
 const st=parked();board(st);const e=st.engineer,t=st.campaign!.truck!;
 // Labelled capacity fixture; baseline is measured after synthetic stock assignment.
 e.inv={steel:100};t.cargo={steel:stackSize('steel')*200-5};const before=conservation(st).unexplained;
 assert.equal(truckTransfer(st,'steel',Number.MAX_SAFE_INTEGER,true).moved,5);assert.equal(invStacks(t.cargo),200);assert.equal(e.inv.steel,95);
 assert.equal(truckTransfer(st,'steel',1,true).ok,false);assert.deepEqual(conservation(st).unexplained,before);
 e.inv={belt:40};const hash=stateHash(st);assert.equal(truckTransfer(st,'steel',10,false).ok,false);assert.equal(stateHash(st),hash);
 for(const n of [-1,0,.5,NaN,Infinity])assert.equal(truckTransfer(st,'steel',n,false).ok,false);
 assert.equal(truckTransfer(st,'__proto__',1,false).ok,false);
 e.inv={belt:39,steel:49};assert.equal(truckTransfer(st,'steel',20,false).moved,1);assert.equal(invStacks(e.inv),40);
});

test('driving checks every footprint tile at speed, refuses blocked rotations, and cannot use dodge or auto-walk',()=>{
 const st=parked();board(st);const t=st.campaign!.truck!,e=st.engineer;
 // Find an open street rectangle for a deterministic collision fixture, without changing ground.
 const G=ground(st);let spot:[number,number]|undefined;
 for(let y=1;y<G.th-10&&!spot;y++)for(let x=1;x<G.tw-10;x++)if([0,1,2,3].every(d=>truckFits(st,x+1.5,y+1.5,d as Dir))&&truckFits(st,x+7.5,y+1.5,1)){spot=[x+1.5,y+1.5];break;}
 assert.ok(spot);t.x=e.x=spot[0];t.y=e.y=spot[1];t.dir=1;
 const start=t.x;e.vel=[1,0];driveTruck(st,.1);assert.ok(Math.abs(t.x-start-1.8)<1e-6,'3x six-tile walking speed');
 const obstacleX=Math.ceil(t.x+1.5)+2,obstacleY=Math.floor(t.y),f=st.flow!;
 const wall:Machine={id:f.next++,kind:'wall',x:obstacleX,y:obstacleY,dir:0,size:1,items:[],hold:null,timer:0,phase:0,inv:{},out:0,busy:false,hp:100};f.machines.push(wall);f.occ[wall.y*f.tw+wall.x]=wall.id;f.rev++;
 driveTruck(st,2);assert.ok(t.x+1.5<=wall.x+1e-6,'large time step cannot tunnel through a one-tile obstacle');assert.ok(truckFits(st,t.x,t.y,t.dir));
 command(st,[{type:'move',x:10,y:10},{type:'walkTo',block:0},{type:'dodge'},{type:'sprint',on:true}]);assert.equal(e.target,null);assert.equal(e.dest,-1);assert.equal(e.dash,0);assert.equal(e.sprint,false);
 // Add an obstacle in the N/S-only part of the rotated footprint.
 const cx=Math.floor(t.x),cy=Math.floor(t.y-1.5),rot={...wall,id:f.next++,x:cx,y:cy};f.machines.push(rot);f.occ[cy*f.tw+cx]=rot.id;f.rev++;
 const xy=[t.x,t.y];e.vel=[0,-1];driveTruck(st,.1);assert.equal(t.dir,1);assert.deepEqual([t.x,t.y],xy);
});

test('blocked exit is refused; knockdown parks cargo, respawn does not teleport the truck, and occupied saves resume',()=>{
 const st=parked();board(st);truckTransfer(st,'steel',10,true);const t=st.campaign!.truck!,e=st.engineer;
 const resumed=loadState(makeSave(st));assert.equal(stateHash(resumed),stateHash(st));advanceFlow(st,1);resumed.speed=st.speed;advanceFlow(resumed,1);assert.equal(stateHash(resumed),stateHash(st));
 const r=truckRect(t),f=st.flow!,added:Machine[]=[];
 for(let y=Math.floor(r.y)-1;y<=Math.ceil(r.y+r.h);y++)for(let x=Math.floor(r.x)-1;x<=Math.ceil(r.x+r.w);x++)if(passable(st,x,y)){
  const m:Machine={id:f.next++,kind:'wall',x,y,dir:0,size:1,items:[],hold:null,timer:0,phase:0,inv:{},out:0,busy:false,hp:100};f.machines.push(m);f.occ[y*f.tw+x]=m.id;added.push(m);
 }f.rev++;const before=stateHash(st);assert.match(boardTruck(st),/No clear exit/);assert.equal(stateHash(st),before);
 const cargo={...t.cargo},xy=[t.x,t.y],ledger=conservation(st).unexplained;hurt(st,1000);assert.equal(e.truckSeat,undefined);assert.equal(e.truck,false);assert.deepEqual(t.cargo,cargo);
 advanceFlow(st,20);assert.deepEqual([t.x,t.y],xy);assert.deepEqual(t.cargo,cargo);assert.deepEqual(conservation(st).unexplained,ledger);assert.ok(Math.hypot(e.x-t.x,e.y-t.y)>10);assert.ok(truckBoardCheck(st));
});

test('old restored previews spawn deterministically on their next tick; malformed truck saves are rejected',()=>{
 const st=parked();delete st.campaign!.truck;st.engineer.truckFound=false;
 const a=loadState(st),b=loadState(st);assert.equal(a.campaign!.truck,undefined);a.speed=b.speed=1;advanceFlow(a,.1);advanceFlow(b,.1);assert.equal(stateHash(a),stateHash(b));assert.ok(a.campaign!.truck);
 for(const corrupt of [(s:SimState)=>s.campaign!.truck!.cargo.steel=-1,(s:SimState)=>s.campaign!.truck!.x=-10,(s:SimState)=>s.engineer.truckSeat=true,(s:SimState)=>s.campaign!.truck!.cargo.belt=201]){const bad=loadState(a);corrupt(bad);assert.throws(()=>loadState(bad),/truck/);}
 const fresh=createCampaign();command(fresh,[{type:'enterTruck'}]);assert.equal(fresh.engineer.truck,false);assert.equal(fresh.campaign!.truck,undefined);
});

test('selected permanent route is read-only, reports all four stops and pauses with cargo retained on power loss',()=>{
 const {st}=expedition();layKit(st);const [a,b]=st.campaign!.expansion!.stops.map(([x,y])=>machineAt(st,x,y)!);
 const tram=st.flow!.machines.find(m=>m.id===st.campaign!.fixedTram!.tram)!,before=stateHash(st),route=stationRoute(st,a.id)!;
 assert.equal(route.stops.length,4);assert.deepEqual(route.paths[0],tramRoute(st,tram));assert.equal(route.trams.length,1);assert.equal(stateHash(st),before);
 for(const m of st.flow!.machines)if(m.kind==='generator')m.inv.coal=0;
 tram.cargo={steel:5};tram.manifest=[{item:'steel',n:5,origin:a.id,destination:b.id,returning:false}];
 advanceFlow(st,.1);const stopped=stationRoute(st,b.id)!;assert.match(stopped.summary,/0\/4 stops powered/);assert.equal(stopped.trams[0].status,'waiting for power');assert.equal(tram.cargo.steel,5);assert.equal(stationRoute(st,-100),null);
});

test('permanent routes expose full arrivals and returning cargo while packing public stops is rejected',()=>{
 const {st}=expedition();const [a,b]=st.campaign!.expansion!.stops.map(([x,y])=>machineAt(st,x,y)!);
 const tram=st.flow!.machines.find(m=>m.id===st.campaign!.fixedTram!.tram)!;a.cargo={steel:200};tram.cargo={steel:5};tram.manifest=[{item:'steel',n:5,origin:a.id,destination:b.id,returning:true}];
 const before=stateHash(st),route=stationRoute(st,a.id)!;assert.equal(route.stops.find(s=>s.machine.id===a.id)!.status,'full');assert.match(route.summary,/Return cargo aboard/);assert.equal(stateHash(st),before);
 assert.equal(route.stops.length,4);
});
