import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, ground, advanceFlow, applyCommands, canPlace, machineAt, passable, inReach,
  damageDefence, defenceHp, damageCore, baseCore, DEFENCE, campaignOrigin, tickCampaignSchedule,
  campaignWarning, registerBase, nominateBase, repairCheck, makeSave, loadState, stateHash, conservation,
  campaignThrottle, machineRunning, type SimState, type Command, type LoggedCommand } from '../src/index';
import { threatOf, crawlerTarget } from '../src/threat';
import { createSession, parseUrl, replaySession } from '../../game/src/session';

function run(st:SimState,seconds:number):void { advanceFlow(st,seconds,[],Math.ceil(seconds*20)+1); }
function schedule(st:SimState,t:number):void { st.t=t;tickCampaignSchedule(st,threatOf(st.flow!)); }
function walk(st:SimState,x:number,y:number):void {
  applyCommands(st,[{type:'move',x:x+.5,y:y+.5}]);
  for(let i=0;i<120&&st.engineer.target;i++)run(st,1);
  assert.ok(inReach(st,x,y),'ordinary walking reached the action');
}
function supplies(st:SimState):void { applyCommands(st,[{type:'chestTake',item:'steel',n:80},{type:'chestTake',item:'copper',n:40},{type:'chestTake',item:'magazine',n:15}]); }
/** Explicit fixture for schedule tests only; restoration costs/commands are tested in expansion.test.ts. */
function stationFixture(st:SimState,radio=true):number {
  const s=st.campaign!.expansion!.station;s.restoredAt=0;st.blocks[s.block].state=2;st.blocks[s.block].subOn=true;
  st.campaign!.expansion!.grantedAt=0;registerBase(st,s.block);
  if(radio)st.campaign!.expansion!.radio.restoredAt=0;
  return s.block;
}

test('paid walls block walking, retain their footprint on defeat, and repair consumes materials once through save/load',()=>{
  const st=createCampaign();supplies(st);const G=ground(st),gate=G.opening!.gate[1],x=gate%G.tw,y=Math.floor(gate/G.tw);
  const dir=G.opening!.direction;walk(st,x-[0,1,0,-1][dir]*4,y-[-1,0,1,0][dir]*4);
  assert.ok(canPlace(st,'wall',x,y).ok);
  const stock=st.engineer.inv.steel;applyCommands(st,[{type:'place',item:'wall',x,y}]);const wall=machineAt(st,x,y)!;
  assert.equal(stock-st.engineer.inv.steel,2);assert.equal(passable(st,x,y),false);
  damageDefence(st,wall,120);assert.equal(passable(st,x,y),true);assert.equal(machineAt(st,x,y)?.id,wall.id);
  applyCommands(st,[{type:'pickUp',x,y}]);assert.equal(machineAt(st,x,y)?.id,wall.id,'cannot erase damage by packing/replacing');
  const before=conservation(st).unexplained,paid=st.engineer.inv.steel;
  applyCommands(st,[{type:'repairDefence',x,y},{type:'repairDefence',x,y}]);assert.equal(st.engineer.inv.steel,paid-2);
  run(st,2);const resumed=loadState(makeSave(st));resumed.speed=st.speed;
  run(st,2.1);run(resumed,2.1);assert.equal(stateHash(st),stateHash(resumed));assert.equal(defenceHp(wall),40);
  assert.equal(passable(st,x,y),false);assert.deepEqual(conservation(st).unexplained,before);
});

test('one dawn lock coalesces restorations; later nominations wait and a late ending preserves two full quiet cycles',()=>{
  const st=createCampaign(),d=st.campaign!.defence!,home=d.bases[0].block;
  const remote=stationFixture(st,false);schedule(st,2400);
  assert.equal(d.major!.block,home,'without a restored radio only home can be selected');
  const id=d.major!.id;st.campaign!.expansion!.radio.restoredAt=2401;nominateBase(st,remote);schedule(st,2500);
  assert.equal(d.major!.id,id);assert.equal(d.major!.block,home);assert.equal(d.nominations.length,1);
  // Labelled scheduler fixture: resolve the roster at an overrun time, without claiming combat success.
  d.major!.remaining=0;schedule(st,4201);assert.equal(d.lastMajorEnd,4201);
  assert.ok(d.nextDawn+900>=4201+2400);assert.equal(d.nextDawn,6000);
  schedule(st,5999);assert.equal(d.major,null);schedule(st,6000);
  assert.equal(d.major!.block,remote);assert.equal(d.nominations.length,0);assert.equal(d.major!.startsAt,6900);
  const saved=loadState(makeSave(st));saved.speed=st.speed;run(st,1);run(saved,1);assert.equal(stateHash(st),stateHash(saved));
});

test('a powered radio receives the real lock; outages retain it and never reroll the target',()=>{
  const st=createCampaign(),remote=stationFixture(st),d=st.campaign!.defence!,radio=st.campaign!.expansion!.radio;
  // Labelled power fixture: relocate the existing fueled generator to the station circuit.
  const gen=st.flow!.machines.find(m=>m.kind==='generator')!;gen.x=radio.x+2;gen.y=radio.y;st.flow!.rev++;
  schedule(st,2400);assert.equal(d.major!.block,remote);assert.equal(d.warning!.block,remote);
  const warning=structuredClone(d.warning),id=d.major!.id;gen.inv.coal=0;schedule(st,2401);
  assert.deepEqual(d.warning,warning);assert.equal(d.major!.id,id);assert.match(campaignWarning(st),/last received/);
  const resumed=loadState(makeSave(st));assert.equal(resumed.campaign!.defence!.major!.id,id);
  d.radioUpgrade=true;gen.inv.coal=10;schedule(st,2402);assert.ok(d.warning?.approach);assert.equal(d.warning?.composition,'ordinary crawlers');
  assert.equal(d.major!.id,id);
});

test('minor opportunities are shared, finite and suppressed during major combat and recovery without catch-up',()=>{
  const st=createCampaign(),d=st.campaign!.defence!,T=threatOf(st.flow!);stationFixture(st);
  schedule(st,300);const first=d.minor!.id,count=T.crawlers.filter(c=>c.campaign?.layer==='minor').length;
  assert.ok(count>=8&&count<=12);schedule(st,300);assert.equal(d.raidsStarted,1);assert.equal(d.minor!.id,first);
  // The second opportunity is discarded when the first raid is still active.
  schedule(st,600);assert.equal(d.raidsStarted,1);
  T.crawlers=T.crawlers.filter(c=>c.campaign?.layer!=='minor');schedule(st,601);assert.equal(d.minor,null);assert.equal(d.raidsStarted,1);
  schedule(st,2400);schedule(st,3300);assert.equal(d.majorSpawned,1);
  const attacking=T.crawlers.find(c=>c.campaign?.layer==='major')!;
  assert.equal(crawlerTarget(st,attacking)?.what,'base core');
  d.major!.retreat=true;assert.equal(crawlerTarget(st,attacking)?.what,'exit');d.major!.retreat=false;
  schedule(st,3900);assert.equal(d.minor,null,'no minor raid while major is running');
  d.major!.remaining=0;T.crawlers=T.crawlers.filter(c=>c.campaign?.layer!=='major');schedule(st,4000);
  schedule(st,4200);assert.equal(d.minor,null,'no opportunity immediately after the actual end');
  schedule(st,4201);assert.equal(d.minor,null,'discarded opportunities do not become a backlog');
});

test('unopposed major crawlers follow the home mouth, defeat its core and physically withdraw before paid recovery',()=>{
  const st=createCampaign(),d=st.campaign!.defence!,core=d.bases[0],G=ground(st);supplies(st);
  const inventory={...st.engineer.inv},layout=st.flow!.machines.map(m=>({id:m.id,kind:m.kind,x:m.x,y:m.y}));
  schedule(st,2400);const origin=d.major!.origin;assert.ok(passable(st,origin%G.tw,Math.floor(origin/G.tw)));
  const bounds=G.opening!.bounds;assert.ok(origin%G.tw>=bounds.x+bounds.size||origin%G.tw<bounds.x||Math.floor(origin/G.tw)>=bounds.y+bounds.size||Math.floor(origin/G.tw)<bounds.y);
  st.t=3300;run(st,180);
  assert.equal(core.hp,0);assert.equal(st.blocks[core.block].state,2,'base remains registered');assert.equal(st.blocks[core.block].subOn,false);
  assert.equal(campaignThrottle(st,core.block),0);assert.equal(d.major,null,'survivors returned to the entry point');
  assert.equal(d.history.at(-1)?.defeated,true);assert.ok(d.history.at(-1)!.spawned<60,'defeat cancels the remaining roster');
  assert.deepEqual(st.flow!.machines.map(m=>({id:m.id,kind:m.kind,x:m.x,y:m.y})),layout);assert.deepEqual(st.engineer.inv,inventory);
  assert.ok(inReach(st,core.x,core.y,core.size));const before=conservation(st).unexplained;
  applyCommands(st,[{type:'repairDefence',x:core.x,y:core.y}]);run(st,12.1);
  assert.equal(core.hp,300);assert.ok(campaignThrottle(st,core.block)>0);assert.equal(d.nominations.length,1);assert.deepEqual(conservation(st).unexplained,before);
});

test('a sealed court is breached through a player wall instead of losing pathfinding; turret defeat preserves its ammo',()=>{
  const st=createCampaign(),G=ground(st),gate=G.opening!.gate,dir=G.opening!.direction;supplies(st);
  const x=gate[1]%G.tw,y=Math.floor(gate[1]/G.tw);walk(st,x-[0,1,0,-1][dir]*4,y-[-1,0,1,0][dir]*4);
  for(const t of gate)applyCommands(st,[{type:'place',item:'wall',x:t%G.tw,y:Math.floor(t/G.tw)}]);
  assert.ok(gate.every(t=>machineAt(st,t%G.tw,Math.floor(t/G.tw))?.kind==='wall'));
  assert.ok(campaignOrigin(st,st.campaign!.defence!.bases[0])>=0,'wall is a breakable path');
  schedule(st,2400);st.t=3300;run(st,70);
  assert.ok(gate.some(t=>machineAt(st,t%G.tw,Math.floor(t/G.tw))?.hp===0),'attacking creatures made a breach');
  const fresh=createCampaign();supplies(fresh);
  let turret: ReturnType<typeof machineAt>;
  const home=ground(fresh).opening!.bounds;
  for(let yy=home.y+1;yy<home.y+home.size-2&&!turret;yy++)for(let xx=home.x+1;xx<home.x+home.size-2;xx++)if(inReach(fresh,xx,yy,2)&&canPlace(fresh,'turret',xx,yy).ok){applyCommands(fresh,[{type:'place',item:'turret',x:xx,y:yy},{type:'feed',x:xx,y:yy}]);turret=machineAt(fresh,xx,yy);break;}
  assert.ok(turret);const rounds=turret.inv.rounds;damageDefence(fresh,turret,100);assert.equal(machineRunning(fresh,turret),false);assert.equal(turret.inv.rounds,rounds);
});

test('ruin guards stay at their own sites and never join the base assault',()=>{
  const st=createCampaign();run(st,20);const d=st.campaign!.defence!,T=threatOf(st.flow!);assert.ok(d.sites.length>0);
  for(const c of T.crawlers){assert.equal(c.campaign?.layer,'site');const t=c.campaign!.origin;assert.ok(Math.hypot(c.x-t%st.flow!.tw-.5,c.y-Math.floor(t/st.flow!.tw)-.5)<1);}
  const resumed=loadState(makeSave(st));resumed.speed=st.speed;run(st,3);run(resumed,3);assert.equal(stateHash(st),stateHash(resumed));
});

test('ordinary paid and hand-supplied turrets hold the complete finite major roster without the rifle',()=>{
  const st=createCampaign(),G=ground(st),gate=G.opening!.gate,dir=G.opening!.direction;
  applyCommands(st,[{type:'chestTake',item:'steel',n:100},{type:'chestTake',item:'copper',n:40},{type:'chestTake',item:'magazine',n:20}]);
  const gx=gate[1]%G.tw,gy=Math.floor(gate[1]/G.tw),bounds=G.opening!.bounds;
  walk(st,gx-[0,1,0,-1][dir]*5,gy-[-1,0,1,0][dir]*5);
  let placed=0;
  for(let y=bounds.y+1;y<bounds.y+bounds.size-2&&placed<4;y++)for(let x=bounds.x+1;x<bounds.x+bounds.size-2&&placed<4;x++) {
    if(Math.hypot(x+1-gx,y+1-gy)>6||!inReach(st,x,y,2)||!canPlace(st,'turret',x,y).ok)continue;
    applyCommands(st,[{type:'place',item:'turret',x,y},{type:'feed',x,y}]);placed++;
  }
  assert.equal(placed,4);assert.equal(st.flow!.machines.filter(m=>m.kind==='turret').reduce((n,m)=>n+(m.inv.rounds??0),0),200);
  const baseline=conservation(st).unexplained;schedule(st,2400);st.t=3300;run(st,300);
  const d=st.campaign!.defence!;
  assert.equal(d.history.length,1);assert.equal(d.history[0].defeated,false);assert.equal(d.history[0].spawned,60);
  assert.ok(d.history[0].ended-d.history[0].started>=180&&d.history[0].ended-d.history[0].started<=300);
  assert.deepEqual(st.engineer.shots,{});assert.ok(d.bases[0].hp>0);assert.equal(threatOf(st.flow!).stats.turretKills,60);
  assert.deepEqual(conservation(st).unexplained,baseline);
});

test('old previews gain a preparation interval; malformed schedules, health and orphaned bodies are refused',()=>{
  const st=createCampaign();st.t=4000;delete st.campaign!.defence;delete st.campaign!.districts;delete st.campaign!.discovery;delete st.campaign!.recruits;delete st.campaign!.turbine;delete st.campaign!.knowledge;st.campaign!.version=2;
  const upgraded=loadState(st),d=upgraded.campaign!.defence!;assert.ok(d.nextDawn>=6400);assert.equal(d.major,null);assert.equal(d.history.length,0);
  for(const mutate of [
    (s:SimState)=>{s.campaign!.defence!.nextDawn=NaN;},
    (s:SimState)=>{s.campaign!.defence!.bases[0].hp=-1;},
    (s:SimState)=>{s.campaign!.defence!.bases.push({...s.campaign!.defence!.bases[0]});},
  ]){const bad=structuredClone(upgraded);mutate(bad);assert.throws(()=>loadState(bad));}
  const partial=createCampaign();delete partial.campaign!.defence;assert.throws(()=>loadState(partial),/defence/);
});

test('ordinary paid defence commands replay from the same fresh campaign factory',()=>{
  const previous=Object.getOwnPropertyDescriptor(globalThis,'location');Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
  try {
    const session=createSession(parseUrl('?rules=exploration-v2')),st=session.state,log:LoggedCommand[]=[];
    const command=(c:Command)=>{log.push({tick:st.flow!.tick,c});applyCommands(st,[c]);};
    command({type:'chestTake',item:'steel',n:20});command({type:'chestTake',item:'copper',n:10});
    const e=st.engineer;
    let tile:[number,number]|undefined;
    for(let y=Math.floor(e.y)-4;y<=e.y+4&&!tile;y++)for(let x=Math.floor(e.x)-4;x<=e.x+4;x++)if(canPlace(st,'wall',x,y).ok){tile=[x,y];break;}
    assert.ok(tile);command({type:'place',item:'wall',x:tile[0],y:tile[1]});run(st,30);session.log=log;
    const replayed=replaySession(session,{rifleOff:false});assert.ok('state'in replayed);assert.equal(stateHash(replayed.state),stateHash(st));
    assert.ok(conservation(st).ok);
  }finally{if(previous)Object.defineProperty(globalThis,'location',previous);else Reflect.deleteProperty(globalThis,'location');}
});
