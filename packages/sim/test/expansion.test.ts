import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, ground, findPath, passable, advanceFlow, applyCommands, canPlace, blockOfTile, campaignSite, siteCheck, EXPANSION, campaignThrottle, campaignGrid,
  conservation, loadState, makeSave, stateHash, lockReason, machineAt, tramRoute, inReach, polePlan, type SimState, type Command, type LoggedCommand } from '../src/index';
import { createSession, parseUrl, replaySession, loadSnapshot } from '../../game/src/session';
const logs = new WeakMap<SimState, LoggedCommand[]>();
function command(st:SimState,commands:Command[]):void { const log=logs.get(st)??[];for(const c of commands)log.push({tick:st.flow!.tick,c});logs.set(st,log);applyCommands(st,commands); }
function walk(st:SimState,x:number,y:number,size=1):void {
  if(inReach(st,x,y,size))return;
  let target:[number,number]|undefined;
  for(let r=0;r<=5&&!target;r++)for(let yy=y-r;yy<y+size+r&&!target;yy++)for(let xx=x-r;xx<x+size+r;xx++) {
    if(passable(st,xx,yy)&&findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),xx,yy)?.length){target=[xx,yy];break;}
  }
  assert.ok(target,'reachable approach');
  command(st,[{type:'move',x:target[0]+0.5,y:target[1]+0.5}]);
  for(let k=0;k<120&&st.engineer.target;k++)advanceFlow(st,1);
  assert.ok(inReach(st,x,y,size),'walk arrived within reach');
}
function generator(st:SimState,bi:number):[number,number] {
  const sub=ground(st).blocks[bi].sub!;
  for(let r=3;r<18;r++)for(let y=sub.y-r;y<sub.y+r;y++)for(let x=sub.x-r;x<sub.x+r;x++)if(blockOfTile(st,x,y)===bi&&canPlace(st,'generator',x,y).ok){walk(st,x,y,2);command(st,[{type:'place',item:'generator',x,y},{type:'feed',x,y}]);assert.equal(machineAt(st,x,y)?.kind,'generator');return[x,y];}
  throw new Error('no generator position');
}
function provision(st:SimState):void { command(st,[{type:'chestTake',item:'steel',n:120},{type:'chestTake',item:'copper',n:60},{type:'chestTake',item:'coal',n:20}]); }
function restoreStation(st:SimState):[number,number] {
  const s=campaignSite(st,'station')!;const gen=generator(st,s.block);walk(st,s.x,s.y,s.size);
  command(st,[{type:'deliverSite',site:'station'},{type:'restoreSite',site:'station'}]);assert.ok(s.restoredAt>=0,siteCheck(st,'station'));return gen;
}
test('second area is reachable across seeds; restoration costs, local power, radio and once-only kit survive saves',()=>{
  for(const seed of [3,4,5,8,13]) {
    const st=createCampaign(seed), e=st.campaign!.expansion!, G=ground(st);provision(st);
    assert.ok(lockReason(st,'tram'));assert.equal(campaignThrottle(st,e.station.block),0,'home cannot power a disconnected outpost');
    command(st,[{type:'deliverSite',site:'station'},{type:'restoreSite',site:'station'},{type:'collectTramKit'}]);assert.equal(e.station.delivered.steel,0,'out of reach refuses');assert.equal(e.grantedAt,-1);
    walk(st,e.station.x,e.station.y,e.station.size);command(st,[{type:'deliverSite',site:'station'},{type:'restoreSite',site:'station'}]);
    assert.equal(e.station.delivered.steel,EXPANSION.station.steel);assert.equal(e.station.restoredAt,-1,'delivery alone cannot restore an unpowered station');assert.ok(conservation(st).ok,conservation(st).problems.join(','));
    const gen=restoreStation(st);assert.equal(lockReason(st,'tram'),'');assert.equal(e.reward.track,e.route.length);assert.equal(e.reward.tram,1);assert.equal(e.reward.tramstop,2);
    const granted=structuredClone(e.reward);command(st,[{type:'restoreSite',site:'station'}]);assert.deepEqual(e.reward,granted,'repeated restoration grants nothing');
    for(const t of e.route){assert.equal(G.owner[t],-1);assert.ok(canPlace(st,'track',t%G.tw,Math.floor(t/G.tw)).ok,'every granted track tile is placeable');}
    const pre=conservation(st).unexplained;walk(st,e.radio.x,e.radio.y);command(st,[{type:'deliverSite',site:'radio'},{type:'restoreSite',site:'radio'}]);assert.ok(e.radio.restoredAt>=0);assert.deepEqual(conservation(st).unexplained,pre);
    const saved=loadState(makeSave(st));assert.equal(stateHash(saved),stateHash(st));assert.equal(saved.campaign!.expansion!.grantedAt,e.grantedAt);
    const remote=machineAt(st,...gen)!;remote.inv.coal=0;assert.equal(campaignThrottle(st,e.station.block),0,'local outage does not borrow home power');assert.ok(campaignThrottle(st,st.campaign!.homeBlock)>0);
  }
});
test('fresh paid expedition lays its generated kit and moves reserved freight between independently powered bases',()=>{
  const st=createCampaign(),e=st.campaign!.expansion!,G=ground(st);provision(st);restoreStation(st);
  const takeKit=()=>{walk(st,e.station.x,e.station.y,e.station.size);command(st,[{type:'collectTramKit'}]);};takeKit();
  for(const t of e.route){if(!(st.engineer.inv.track>0))takeKit();const x=t%G.tw,y=Math.floor(t/G.tw);walk(st,x,y);command(st,[{type:'place',item:'track',x,y}]);assert.equal(machineAt(st,x,y)?.kind,'track');}
  for(const [x,y]of e.stops){walk(st,x,y,2);command(st,[{type:'place',item:'tramstop',x,y}]);assert.equal(machineAt(st,x,y)?.kind,'tramstop');}
  const first=e.route[0],tx=first%G.tw,ty=Math.floor(first/G.tw);walk(st,tx,ty);command(st,[{type:'place',item:'tram',x:tx,y:ty}]);
  const tram=st.flow!.machines.find(m=>m.kind==='tram')!;assert.equal(tramRoute(st,tram).length,e.route.length);assert.deepEqual(e.reward,{tram:0,tramstop:0,track:0});
  const [a,b]=e.stops.map(([x,y])=>machineAt(st,x,y)!);
  walk(st,b.x,b.y,2);command(st,[{type:'setStationRules',x:b.x,y:b.y,rules:{steel:{request:5,reserve:0,export:false}}}]);
  walk(st,a.x,a.y,2);command(st,[{type:'setStationRules',x:a.x,y:a.y,rules:{steel:{request:0,reserve:0,export:true}}},{type:'chestPut',item:'steel',n:5,x:a.x,y:a.y}]);assert.equal(a.inv.steel,5);
  assert.ok(campaignGrid(st).blocks[blockOfTile(st,b.x,b.y)].supply>0);advanceFlow(st,60);
  assert.equal(b.cargo?.steel,5);assert.ok(conservation(st).ok,conservation(st).problems.join(','));
  const locationBefore=Object.getOwnPropertyDescriptor(globalThis,'location');
  Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
  try { const session=createSession(parseUrl('?rules=exploration-v2'));session.state=st;session.log=logs.get(st)!;const replayed=replaySession(session,{rifleOff:false});assert.ok('state' in replayed);assert.equal(stateHash(replayed.state),stateHash(st),'entire restoration/freight command log replays'); } finally {if(locationBefore)Object.defineProperty(globalThis,'location',locationBefore);else Reflect.deleteProperty(globalThis,'location');}
  const resumed=loadState(makeSave(st));resumed.speed=st.speed;advanceFlow(st,30);advanceFlow(resumed,30);assert.equal(stateHash(resumed),stateHash(st));
});
test('old home preview upgrades deterministically without granting a kit; malformed expansion metadata is rejected',()=>{
  const st=createCampaign();delete st.campaign!.expansion;st.campaign!.version=1;
  const a=loadState(st),b=loadState(st);assert.equal(a.campaign!.version,2);assert.equal(a.campaign!.expansion!.grantedAt,-1);assert.equal(stateHash(a),stateHash(b));
  a.campaign!.expansion!.reward.tram=2;assert.throws(()=>loadState(a),/reward/);
  b.campaign!.expansion!.stops[0][1]=ground(b).th;assert.throws(()=>loadState(b),/geometry/);
});

test('physical pole links power a remote installation and picking them up disconnects it',()=>{
  const st=createCampaign(),s=campaignSite(st,'station')!;provision(st);
  const plan=polePlan(st,s.block);assert.ok(plan.length>0);
  for(const [x,y]of plan){walk(st,x,y);command(st,[{type:'place',item:'pole',x,y}]);assert.equal(machineAt(st,x,y)?.kind,'pole');}
  assert.ok(campaignThrottle(st,s.block)>0,'wire reaches the remote substation without a local generator');
  for(const [x,y]of plan){walk(st,x,y);command(st,[{type:'pickUp',x,y}]);}
  assert.equal(campaignThrottle(st,s.block),0);assert.ok(conservation(st).ok,conservation(st).problems.join(','));
});

test('upgrading a home-only save preserves the save but labels its old log as incomplete for the new campaign factory',async()=>{
  const st=createCampaign();delete st.campaign!.expansion;st.campaign!.version=1;
  const raw={kind:'relight-save',version:3,state:st,log:[],logComplete:true};
  const prior=Object.getOwnPropertyDescriptor(globalThis,'localStorage');
  Object.defineProperty(globalThis,'localStorage',{configurable:true,value:{getItem:()=>JSON.stringify(raw)}});
  try{const loaded=await loadSnapshot('local:old-preview');assert.equal(loaded.state.campaign!.version,2);assert.equal(loaded.logComplete,false);assert.equal(loaded.saved?.state.campaign?.version,1);}finally{if(prior)Object.defineProperty(globalThis,'localStorage',prior);else Reflect.deleteProperty(globalThis,'localStorage');}
});
