import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, ground, findPath, persistentSource, loadState, makeSave, stateHash, type SimState,
  applyCommands as rawApplyCommands, advanceFlow, canPlace, machineAt, blockOfTile, campaignSite, siteCheck, inReach, passable,
  conservation, openLedger, damageDefence, defenceHp, tickDistricts, workshopStatus, campaignThrottle,
  rubbleAt, tramRoute, routeStops, tickCampaignSchedule, MACHINE_SIZE, type Kind, type Dir, type Machine, type Command, type LoggedCommand } from '../src/index';
import { writeFileSync } from 'node:fs';
import { createSession, parseUrl, replaySession } from '../../game/src/session';
const commandLogs=new WeakMap<SimState,LoggedCommand[]>();
function applyCommands(st:SimState,commands:Command[]):void {for(const c of commands)commandLogs.get(st)?.push({tick:st.flow!.tick,c:structuredClone(c)});rawApplyCommands(st,commands);}
import { threatOf } from '../src/threat';

/** Labelled construction fixture: extra finite chest stock and deferred threats isolate logistics, not campaign balance. */
function fixture():SimState {
  const st=createCampaign();st.stock.steel+=1200;st.stock.copper+=600;st.flow!.store.coal+=500;
  st.campaign!.defence!.nextDawn=100000;st.campaign!.defence!.lastMinorSlot=100000;st.campaign!.defence!.sites=[];
  st.flow!.ledger=openLedger(st);
  applyCommands(st,[{type:'chestTake',item:'steel',n:400},{type:'chestTake',item:'copper',n:150},{type:'chestTake',item:'coal',n:100}]);return st;
}
function run(st:SimState,seconds:number):void { advanceFlow(st,seconds,[],Math.ceil(seconds*20)+1); }
function walk(st:SimState,x:number,y:number,size=1):void {
  if(inReach(st,x,y,size))return;
  let target:[number,number]|undefined;
  for(let r=0;r<6&&!target;r++)for(let yy=y-r;yy<y+size+r&&!target;yy++)for(let xx=x-r;xx<x+size+r;xx++)if(passable(st,xx,yy)&&findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),xx,yy)?.length){target=[xx,yy];break;}
  assert.ok(target,'walkable approach');applyCommands(st,[{type:'move',x:target[0]+.5,y:target[1]+.5}]);
  for(let i=0;i<150&&st.engineer.target;i++)run(st,1);
  assert.ok(inReach(st,x,y,size),`arrived at ${x},${y}`);
}
function place(st:SimState,kind:Kind,x:number,y:number,dir:Dir=0):Machine {
  walk(st,x,y,MACHINE_SIZE[kind]);assert.ok(canPlace(st,kind,x,y).ok,`${kind} ${x},${y}: ${canPlace(st,kind,x,y).reason}`);
  applyCommands(st,[{type:'place',item:kind,x,y,dir}]);const m=kind==='tram'?st.flow!.machines.find(m=>m.kind==='tram'):machineAt(st,x,y);
  assert.equal(m?.kind,kind);return m!;
}
function nearby(st:SimState,kind:Kind,x:number,y:number,block:number,radius=15):Machine {
  for(let r=1;r<=radius;r++)for(let yy=y-r;yy<=y+r;yy++)for(let xx=x-r;xx<=x+r;xx++)if(blockOfTile(st,xx,yy)===block&&canPlace(st,kind,xx,yy).ok)return place(st,kind,xx,yy);
  throw new Error(`no ${kind} pad near ${x},${y}`);
}
function restore(st:SimState,id:'station'|'northStation'|'workshop'):void {
  const s=campaignSite(st,id)!;walk(st,s.x,s.y,s.size);
  if(id==='workshop'&&process.env.EX06_REVIEW_PENDING_PATH)writeFileSync(process.env.EX06_REVIEW_PENDING_PATH,JSON.stringify(makeSave(st),null,2));
  applyCommands(st,[{type:'deliverSite',site:id},{type:'restoreSite',site:id}]);assert.ok(s.restoredAt>=0,siteCheck(st,id));
}
function power(st:SimState,block:number):Machine {
  const s=ground(st).blocks[block].sub!,m=nearby(st,'generator',s.x,s.y,block);
  applyCommands(st,[{type:'feed',x:m.x,y:m.y}]);assert.ok(campaignThrottle(st,block)>0);return m;
}
function outposts(st:SimState):void {
  power(st,st.campaign!.expansion!.station.block);restore(st,'station');
  power(st,st.campaign!.districts!.station.block);restore(st,'northStation');
}
function homeFactory(st:SimState,stop:Machine):void {
  for(const [dx,dy,dir]of [[1,0,1],[-1,0,3],[0,1,2],[0,-1,0]] as const)for(let off=0;off<2;off++) {
    const ix=dx===1?stop.x-1:dx===-1?stop.x+2:stop.x+off;
    const iy=dy===1?stop.y-1:dy===-1?stop.y+2:stop.y+off;
    const cx=ix-2*dx,cy=iy-2*dy;
    const ax=cx-1,ay=cy-1,fx=cx-2*dx,fy=cy-2*dy;
    const sx=cx-3*dx-(dx===1?1:dx===0?1:0),sy=cy-3*dy-(dy===1?1:dy===0?1:0);
    const plan:[Kind,number,number,Dir][]=[['assembler',ax,ay,dir],['inserter',ix,iy,dir],['inserter',fx,fy,dir],['chest',sx,sy,0]];
    if(plan.some(([k,x,y])=>blockOfTile(st,x,y)!==st.campaign!.homeBlock||!canPlace(st,k,x,y).ok))continue;
    for(const [k,x,y,dir]of plan)place(st,k,x,y,dir);
    walk(st,sx,sy,2);applyCommands(st,[{type:'chestPut',x:sx,y:sy,item:'steel',n:40},{type:'chestPut',x:sx,y:sy,item:'copper',n:20}]);return;
  }
  throw new Error('no home factory layout beside its station');
}
/** Ordinary inserter plus local belt branch from arrivals into the workshop supply chest. */
function connectStorage(st:SimState,from:Machine,to:Machine):void {
  const G=ground(st),tw=G.tw,dirs=[[0,-1],[1,0],[0,1],[-1,0]];
  for(let dir=0;dir<4;dir++)for(let off=0;off<from.size;off++) {
    const [dx,dy]=dirs[dir],ix=dx<0?from.x-1:dx>0?from.x+from.size:from.x+off,iy=dy<0?from.y-1:dy>0?from.y+from.size:from.y+off;
    const sx=ix+dx,sy=iy+dy,start=sy*tw+sx;
    if(!canPlace(st,'inserter',ix,iy).ok||!canPlace(st,'belt',sx,sy).ok)continue;
    const prev=new Map<number,number>([[start,start]]),queue=[start];let end=-1,target=-1;
    for(let i=0;i<queue.length&&end<0;i++) {
      const t=queue[i],x=t%tw,y=Math.floor(t/tw);
      for(const [dx,dy]of dirs) {
        const xx=x+dx,yy=y+dy,q=yy*tw+xx;
        if(machineAt(st,xx,yy)?.id===to.id){end=t;target=q;break;}
        if(Math.abs(xx-from.x)>50||Math.abs(yy-from.y)>50||xx===ix&&yy===iy||prev.has(q)||!canPlace(st,'belt',xx,yy).ok)continue;
        prev.set(q,t);queue.push(q);
      }
    }
    if(end<0)continue;
    const path=[end];while(path.at(-1)!==start)path.push(prev.get(path.at(-1)!)!);path.reverse();
    place(st,'inserter',ix,iy,dir as Dir);
    for(let i=0;i<path.length;i++){const t=path[i],next=path[i+1]??target,x=t%tw,y=Math.floor(t/tw);const dir=dirs.findIndex(([dx,dy])=>x+dx===next%tw&&y+dy===Math.floor(next/tw));place(st,'belt',x,y,dir as Dir);}return;
  }
  throw new Error('no physical local storage branch');
}

test('district sites and specialised sources are reachable, distinct and persistent across seeds and saves',()=>{
  for(const seed of [3,4,5,8,13]) {
    const st=createCampaign(seed),d=st.campaign!.districts!,G=ground(st);
    assert.equal(new Set(d.sources.map(s=>s.block)).size,3);
    const route=[...st.campaign!.expansion!.route,...d.route.slice(1)];
    for(const t of route) {
      assert.equal(G.owner[t],-1);
      const adjacent=route.filter(q=>Math.abs(q%G.tw-t%G.tw)+Math.abs(Math.floor(q/G.tw)-Math.floor(t/G.tw))===1);
      assert.ok(adjacent.length<=2,'survey has no track junction');
    }
    for(const s of [...d.sources,d.workshop])assert.ok(findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),s.x,s.y)?.length,`reachable ${s.x},${s.y} seed ${seed}`);
    assert.equal(persistentSource(st,d.sources[0].x,d.sources[0].y)?.item,'steel');
    assert.equal(stateHash(loadState(makeSave(st))),stateHash(st));
  }
});

test('powered extraction produces into real local storage without depletion or hand mining; outages stop it',()=>{
  const st=fixture(),d=st.campaign!.districts!;
  for(const source of d.sources) {
    power(st,source.block);
    // The excavator straddles the source edge; its eastern output is outside the extraction footprint.
    const ex=place(st,'excavator',source.x+2,source.y+1,1);
    const chest=place(st,'chest',ex.x+3,ex.y+1);
    run(st,25);assert.ok((chest.inv[source.item]??0)>0,`actual ${source.item} output`);
    assert.equal(rubbleAt(st,source.x,source.y)?.units,Infinity);
    walk(st,source.x,source.y);const hand=st.flow!.stats.handMined;
    applyCommands(st,[{type:'mineAt',x:source.x,y:source.y}]);run(st,3);assert.equal(st.flow!.stats.handMined,hand);
    const gens=st.flow!.machines.filter(m=>m.kind==='generator'&&blockOfTile(st,m.x,m.y)===source.block);
    for(const gen of gens){walk(st,gen.x,gen.y,2);applyCommands(st,[{type:'pickUp',x:gen.x,y:gen.y}]);}
    const count=st.flow!.stats.minedOf[source.item];run(st,5);assert.equal(st.flow!.stats.minedOf[source.item],count);
  }
  assert.ok(conservation(st).ok,conservation(st).problems.join(','));
  const resumed=loadState(makeSave(st));resumed.speed=st.speed;run(st,5);run(resumed,5);assert.equal(stateHash(st),stateHash(resumed));
});

test('workshop consumes local supplies at its powered rate, pauses during attacks/outages, and preserves repairs through saves',()=>{
  const st=fixture();outposts(st);restore(st,'workshop');
  const d=st.campaign!.districts!,w=d.workshop;
  const chest=nearby(st,'chest',w.x,w.y,w.block,3),wall=nearby(st,'wall',w.x+7,w.y+4,w.block,3);
  damageDefence(st,wall,120);assert.match(workshopStatus(st),/needs 2 steel/);
  walk(st,chest.x,chest.y,2);applyCommands(st,[{type:'chestPut',x:chest.x,y:chest.y,item:'steel',n:10},{type:'chestPut',x:chest.x,y:chest.y,item:'copper',n:5}]);
  run(st,1);assert.ok(d.repair);const resumed=loadState(makeSave(st));resumed.speed=st.speed;
  run(st,1.1);run(resumed,1.1);assert.equal(stateHash(st),stateHash(resumed));assert.equal(defenceHp(wall),40);assert.equal(chest.inv.steel,8);
  // Labelled outage fixture moves fuel back to storage; it never creates or deletes coal.
  const gen=st.flow!.machines.find(m=>m.kind==='generator'&&blockOfTile(st,m.x,m.y)===w.block)!,fuel=gen.inv.coal;
  gen.inv.coal=0;st.flow!.store.coal+=fuel;tickDistricts(st,10);assert.equal(defenceHp(wall),40);assert.match(workshopStatus(st),/power/);
  gen.inv.coal=fuel;st.flow!.store.coal-=fuel;
  // Labelled scheduler fixture holds an active major while checking service, without advancing combat.
  const def=st.campaign!.defence!;def.major={id:def.nextId++,block:w.block,dawn:st.t,startsAt:st.t,origin:0,remaining:0,nextSpawn:st.t,retreat:false};
  const before=chest.inv.steel;tickDistricts(st,10);assert.equal(defenceHp(wall),40);assert.equal(chest.inv.steel,before);assert.match(workshopStatus(st),/paused/);def.major=null;
  run(st,5);assert.equal(defenceHp(wall),120);assert.equal(chest.inv.steel,4);assert.equal(d.repairs,3);
  assert.ok(conservation(st).ok,conservation(st).problems.join(','));
});

test('fresh campaign pays for both stations and the workshop through logged commands that replay identically',()=>{
  const previous=Object.getOwnPropertyDescriptor(globalThis,'location');Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
  try {
    const session=createSession(parseUrl('?rules=exploration-v2')),st=session.state,log:LoggedCommand[]=[];commandLogs.set(st,log);
    applyCommands(st,[{type:'chestTake',item:'steel',n:180},{type:'chestTake',item:'copper',n:90},{type:'chestTake',item:'coal',n:20}]);
    power(st,st.campaign!.expansion!.station.block);restore(st,'station');
    const home=st.flow!.machines.find(m=>m.kind==='depot')!;walk(st,home.x,home.y,home.size);applyCommands(st,[{type:'chestTake',item:'coal',n:20}]);
    power(st,st.campaign!.districts!.station.block);restore(st,'northStation');restore(st,'workshop');run(st,1);
    session.log=log;const replayed=replaySession(session,{rifleOff:false});assert.ok('state'in replayed);assert.equal(stateHash(replayed.state),stateHash(st));
    assert.equal(st.campaign!.defence!.bases.length,3);assert.ok(conservation(st).ok);
  }finally{if(previous)Object.defineProperty(globalThis,'location',previous);else Reflect.deleteProperty(globalThis,'location');}
});

test('paid three-base tram route reserves onward supply, returns district goods and requires repeated actual home deliveries for shorter rest',()=>{
  const st=fixture();outposts(st);const e=st.campaign!.expansion!,d=st.campaign!.districts!,G=ground(st);
  walk(st,e.station.x,e.station.y,e.station.size);applyCommands(st,[{type:'collectTramKit'}]);
  const path=[...e.route,...d.route.slice(1)];
  for(const t of path)place(st,'track',t%G.tw,Math.floor(t/G.tw));
  const stops=[...e.stops,d.stop].map(([x,y])=>place(st,'tramstop',x,y));
  const [home,middle,last]=stops,tram=place(st,'tram',path[0]%G.tw,Math.floor(path[0]/G.tw));
  assert.equal(tramRoute(st,tram).length,path.length);assert.equal(routeStops(st,path).length,3);
  const set=(m:Machine,c:Command)=>{walk(st,m.x,m.y,m.size);applyCommands(st,[c]);};
  set(home,{type:'setStationRules',x:home.x,y:home.y,rules:{steel:{request:0,reserve:0,export:true},copper:{request:5,reserve:0,export:true},magazine:{request:0,reserve:0,export:true}}});
  set(middle,{type:'setStationRules',x:middle.x,y:middle.y,rules:{steel:{request:4,reserve:0,export:false}}});
  set(last,{type:'setStationRules',x:last.x,y:last.y,rules:{steel:{request:10,reserve:0,export:false},copper:{request:5,reserve:0,export:true},magazine:{request:1,reserve:0,export:false}}});
  const manual=nearby(st,'chest',d.workshop.x,d.workshop.y,d.station.block,4);
  set(manual,{type:'chestPut',x:manual.x,y:manual.y,item:'steel',n:10});assert.equal(d.supplied.steel,0);
  set(manual,{type:'chestTake',x:manual.x,y:manual.y,item:'steel',n:10});
  walk(st,home.x,home.y,2);applyCommands(st,[{type:'chestPut',x:home.x,y:home.y,item:'steel',n:14},{type:'chestPut',x:home.x,y:home.y,item:'copper',n:5}]);
  // A supplied home assembler sends newly produced magazines through an inserter onto the tram platform.
  homeFactory(st,home);
  run(st,120);assert.ok((middle.cargo?.steel??0)>=4);assert.ok(d.supplied.steel>=10);assert.ok(d.supplied.copper>=5);assert.equal(d.visits,1);assert.equal(d.resuppliedAt,-1);
  set(last,{type:'chestTake',x:last.x,y:last.y,item:'magazine',n:1});
  set(last,{type:'setStationRules',x:last.x,y:last.y,rules:{...last.freight,magazine:{request:5,reserve:0,export:false}}});
  run(st,120);
  assert.ok(d.visits>=2);assert.ok(st.flow!.stats.made.magazine>=5,'home factory supplies the expansion');assert.ok(d.resuppliedAt>=0);
  // Copper produced at the later district returns to the old base through the same freight contracts.
  const copper=d.sources.find(s=>s.item==='copper')!,excavator=place(st,'excavator',copper.x+2,copper.y+1,1),output=place(st,'chest',excavator.x+3,excavator.y+1);
  run(st,20);assert.ok((output.inv.copper??0)>=5);
  set(output,{type:'chestTake',x:output.x,y:output.y,item:'copper',n:5});set(last,{type:'chestPut',x:last.x,y:last.y,item:'copper',n:5});run(st,100);
  assert.ok((home.cargo?.copper??0)>=5,'extracted copper returns home');
  restore(st,'workshop');const w=d.workshop,supplyChest=nearby(st,'chest',w.x,w.y,w.block,3);
  connectStorage(st,last,supplyChest);
  const wall=nearby(st,'wall',w.x+7,w.y+4,w.block,3);damageDefence(st,wall,120);run(st,120);
  assert.ok(d.repairs>0,'tram-delivered repair materials actually serve the workshop');assert.equal(defenceHp(wall),120);
  assert.ok(conservation(st).ok,conservation(st).problems.join(','));
  if(process.env.EX06_REVIEW_PATH)writeFileSync(process.env.EX06_REVIEW_PATH,JSON.stringify(makeSave(st),null,2));
  // Completing an existing assault is the only point that shortens the next interval.
  const def=st.campaign!.defence!,promised=def.nextDawn;assert.equal(promised,100000);
  def.major={id:def.nextId++,block:def.bases[0].block,dawn:st.t,startsAt:st.t,origin:0,remaining:0,nextSpawn:st.t,retreat:false};
  tickCampaignSchedule(st,threatOf(st.flow!));assert.ok(def.nextDawn+900>=st.t+1200);assert.ok(def.nextDawn+900<st.t+2400);
  const saved=loadState(makeSave(st));saved.speed=st.speed;run(st,4);run(saved,4);assert.equal(stateHash(st),stateHash(saved));
});

test('old district-free previews upgrade once without moving the promised assault or duplicating sources',()=>{
  const st=createCampaign();delete st.campaign!.districts;st.campaign!.version=3;st.t=4000;
  const dawn=st.campaign!.defence!.nextDawn,a=loadState(st),b=loadState(st);
  assert.equal(a.campaign!.version,4);assert.equal(a.campaign!.defence!.nextDawn,dawn);assert.equal(stateHash(a),stateHash(b));
  assert.equal(stateHash(loadState(makeSave(a))),stateHash(a));
  for(const mutate of [
    (s:SimState)=>{delete s.campaign!.districts;},
    (s:SimState)=>{s.campaign!.districts!.resuppliedAt=0;},
    (s:SimState)=>{s.campaign!.districts!.sources[0].item='unknown' as 'steel';},
    (s:SimState)=>{s.campaign!.districts!.route[1]=-1;},
  ]){const bad=structuredClone(a);mutate(bad);assert.throws(()=>loadState(bad));}
});
