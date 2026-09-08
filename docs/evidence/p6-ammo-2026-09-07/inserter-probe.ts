import assert from 'node:assert/strict';
import {createCampaign,ground,placeable,MACHINE_SIZE,conservation,stateHash,machineAt,describeMachine,type Dir,type Kind} from '../../../packages/sim/src/index';
import {FactoryDriver,buildBlueprint,replayInterval,savedContinuation} from '../../../packages/harness/src/blueprint';
for(const turn of [0,1,2,3])for(const lane of [0,1]){
 const st=createCampaign(3),initial=structuredClone(st),d=new FactoryDriver(st);d.take('steel',100);d.take('copper',30);d.take('magazine',12);
 const raw:[string,Kind,number,number,Dir][]=[['source','chest',0,0,1],['arm','inserter',2,lane,1],['belt','belt',3,lane,1],['loader','inserter',4,lane,1],['turret','turret',5,0,0]];
 const entities=raw.map(([id,kind,x,y,dir])=>{let points=[[x,y],[x+MACHINE_SIZE[kind]-1,y+MACHINE_SIZE[kind]-1]];
  for(let i=0;i<turn;i++)points=points.map(([a,b])=>[-b,a]);
  return {id,kind,x:Math.min(...points.map(p=>p[0])),y:Math.min(...points.map(p=>p[1])),dir:(dir+turn)%4 as Dir};});
 const minX=Math.min(...entities.map(e=>e.x)),minY=Math.min(...entities.map(e=>e.y));for(const e of entities){e.x-=minX;e.y-=minY;}
 const bounds=ground(st).opening!.bounds;let at:[number,number]|undefined;
 for(let y=bounds.y;y<bounds.y+bounds.size&&!at;y++)for(let x=bounds.x;x<bounds.x+bounds.size&&!at;x++)if(entities.every(e=>!placeable(st,e.kind,x+e.x,y+e.y,e.dir)))at=[x,y];
 assert.ok(at);const ids=buildBlueprint(d,{version:1,name:'Ammo edge probe',entities},...at);
 const get=(id:string)=>st.flow!.machines.find(m=>m.id===ids[id])!;
 d.put('magazine',12,get('source'));d.run(12);
 console.log(JSON.stringify({turn,lane,rounds:get('turret').inv.rounds,belt:get('belt').items,status:describeMachine(st,get('turret')),fed:st.flow!.stats.turretFed}));
 assert.equal(get('turret').inv.rounds,50);assert.ok(get('belt').items.length>0);assert.ok(conservation(st).ok);assert.ok(savedContinuation(st).same);
 const turret=get('turret');d.approach(turret.x,turret.y,2);d.send({type:'construct',edits:[{action:'pickUp',x:turret.x,y:turret.y}]});d.place('turret',turret.x,turret.y);d.run(10);
 assert.equal(machineAt(st,turret.x,turret.y)!.inv.rounds,50);assert.ok(conservation(st).ok);assert.equal(stateHash(replayInterval(initial,d.log,st.flow!.tick)),stateHash(st));
}
console.log('PASS: 8 turret perimeter tiles load from a conveyor through a powered inserter, back up at 50 rounds, resume for an empty replacement, conserve and save/replay.');
