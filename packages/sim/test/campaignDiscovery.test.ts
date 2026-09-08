import { test } from 'node:test';
import assert from 'node:assert/strict';
import { writeFileSync } from 'node:fs';
import { createCampaign, ground, passable, findPath, applyCommands, advanceFlow, makeSave, loadState, stateHash,
  DISCOVERY, discoveryCheck, discoveryDescription, manualRepairSeconds, stalkersOf, describeStalker,
  type SimState, type Command, type LoggedCommand, canPlace, machineAt, inReach, damageCore,
  conservation, openLedger, type Machine } from '../src/index';
import { createSession, parseUrl, replaySession } from '../../game/src/session';

function run(st:SimState,s:number,commands:Command[]=[]):void { advanceFlow(st,s,commands,Math.ceil(s*20)+1); }
function walk(st:SimState,x:number,y:number):void {
  assert.ok(findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),x,y));
  run(st,.05,[{type:'move',x:x+.5,y:y+.5}]);
  for(let i=0;i<200&&st.engineer.target;i++)run(st,1);
  assert.ok(Math.hypot(st.engineer.x-x-.5,st.engineer.y-y-.5)<.1);
}
/** Labelled combat fixture: relocation and deferred other threats isolate the guardian's state machine. */
function encounter():SimState {
  const st=createCampaign(),d=st.campaign!.discovery!,s=d.guardian.stalkers[0];
  st.campaign!.defence!.sites=[];st.campaign!.defence!.nextDawn=100000;st.campaign!.defence!.lastMinorSlot=100000;
  st.engineer.x=s.hx;st.engineer.y=s.hy;run(st,.05);
  run(st,.05,[{type:'walk',dx:1,dy:0}]);run(st,.05,[{type:'walk',dx:0,dy:0}]);
  assert.equal(s.mode,'attack');
  if(process.env.EX07_REVIEW_WINDUP_PATH)writeFileSync(process.env.EX07_REVIEW_WINDUP_PATH,JSON.stringify(makeSave(st),null,2));return st;
}

test('the marked workshop side yard is seeded, reachable, optional, and clear of essential routes across seeds',()=>{
  for(const seed of [3,4,5,8,11,13,42]) {
    const st=createCampaign(seed),d=st.campaign!.discovery!,district=st.campaign!.districts!,e=st.campaign!.expansion!,G=ground(st);
    assert.ok(findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),d.x,d.y));
    assert.equal(d.guardian.stalkers.length,1);assert.equal(st.flow!.threat?.stalk,undefined);
    assert.equal(d.block,district.workshop.block);assert.equal(canPlace(st,'pole',d.x,d.y).reason,'workshop records are here');
    assert.match(discoveryDescription(st),/Optional/);
    // Flood-fill the actual walkable city while excluding the full guardian territory plus contact range.
    const start=Math.floor(st.engineer.y)*G.tw+Math.floor(st.engineer.x),seen=new Set([start]),queue=[start];
    const targets=new Set([district.workshop,e.station,e.radio,district.station,...district.sources].map(s=>s.y*G.tw+s.x));
    for(let head=0;head<queue.length&&targets.size;head++) {
      const t=queue[head];targets.delete(t);
      for(const q of [t-G.tw,t+G.tw,t%G.tw?t-1:-1,t%G.tw<G.tw-1?t+1:-1]) {
        const x=q%G.tw,y=Math.floor(q/G.tw);
        if(q<0||q>=G.base.length||seen.has(q)||Math.hypot(x-d.x,y-d.y)<=DISCOVERY.guardian.leash+1.2||!passable(st,x,y))continue;
        seen.add(q);queue.push(q);
      }
    }
    assert.equal(targets.size,0,`essential progression bypasses the encounter, seed ${seed}`);
    assert.equal(stateHash(loadState(makeSave(st))),stateHash(st));
  }
});

test('guardian telegraphs every strike, accepts a dodge, leaves a downed engineer, and saves mid-windup',()=>{
  const st=encounter(),d=st.campaign!.discovery!,s=stalkersOf(st)[0],hp=st.engineer.hp;
  assert.match(describeStalker(st,s),/winding up/);assert.match(discoveryCheck(st,d.id),/draw it away/);
  const copy=loadState(makeSave(st));copy.speed=st.speed;run(st,.5);run(copy,.5);assert.equal(stateHash(copy),stateHash(st));
  assert.equal(st.engineer.hp,hp,'no damage before the visible windup');
  while(s.windup>.051)run(st,.05);
  run(st,.05,[{type:'dodge'}]);assert.equal(d.guardian.stats.dodged,1);assert.equal(st.engineer.hp,hp);
  assert.equal(s.warmed,false);assert.ok(s.windup>1,'next strike also has a visible warning');
  // Controlled knockdown fixture, leaving the ordinary damage/knockdown path intact.
  st.engineer.dash=0;st.engineer.hp=5;st.engineer.lastHit=st.t;st.engineer.x=s.x;st.engineer.y=s.y;
  run(st,1.3);assert.ok(st.engineer.down>=0);const hits=d.guardian.stats.hits;
  run(st,1);assert.equal(d.guardian.stats.hits,hits);assert.ok(['return','guard'].includes(s.mode));
});

test('the guardian returns at its bounded leash and workshop restoration does not retire it',()=>{
  const st=encounter(),d=st.campaign!.discovery!,s=stalkersOf(st)[0];
  // Controlled location probe, as in the legacy archetype tests; each move remains on passable ground.
  for(let i=0;i<200;i++) {
    const x=s.hx,y=s.hy+DISCOVERY.guardian.leash+2;
    if(passable(st,Math.floor(x),Math.floor(y))){st.engineer.x=x;st.engineer.y=y;}
    else {st.engineer.x=s.hx-DISCOVERY.guardian.leash-2;st.engineer.y=s.hy;}
    run(st,.05);assert.ok(Math.hypot(s.x-s.hx,s.y-s.hy)<=DISCOVERY.guardian.leash+1e-8);
  }
  assert.equal(s.mode,'guard');assert.equal(d.guardian.stats.retired,0);
  st.blocks[d.block].state=2; // HELD: only the legacy lifecycle retires on this change.
  run(st,2);assert.equal(stalkersOf(st).length,1);assert.equal(s.mode,'guard');
});

test('a supplied paid turret defeats the guardian without reflex combat; the site reward is once-only, not a drop',()=>{
  const st=createCampaign(),d=st.campaign!.discovery!,s=stalkersOf(st)[0];
  // Labelled construction fixture: finite materials and relocation, no injected weapon or damage.
  st.engineer.inv.steel=100;st.engineer.inv.copper=50;st.engineer.inv.magazine=5;st.flow!.ledger=openLedger(st);
  let turret:Machine|undefined;
  for(let r=6;r<=8&&!turret;r++)for(let y=d.y-r;y<=d.y+r&&!turret;y++)for(let x=d.x-r;x<=d.x+r;x++) {
    if(Math.hypot(x+1-s.x,y+1-s.y)>9||Math.hypot(x+1-s.x,y+1-s.y)<6||!canPlace(st,'turret',x,y).ok)continue;
    for(let yy=y-1;yy<=y+2&&!turret;yy++)for(let xx=x-1;xx<=x+2;xx++) {
      if(!passable(st,xx,yy)||Math.hypot(xx+.5-s.hx,yy+.5-s.hy)<=5)continue;
      st.engineer.x=xx+.5;st.engineer.y=yy+.5;
      if(!inReach(st,x,y,2))continue;
      applyCommands(st,[{type:'place',item:'turret',x,y},{type:'feed',x,y}]);turret=machineAt(st,x,y);if(turret)break;
    }
    if(turret)break;
  }
  assert.equal(turret?.kind,'turret');const rounds=turret!.inv.rounds,hp=st.engineer.hp;
  run(st,3);assert.equal(stalkersOf(st).length,0);assert.equal(d.guardian.stats.kills,1);
  assert.equal(rounds-turret!.inv.rounds,6);assert.equal(st.engineer.hp,hp);assert.equal(d.recoveredAt,-1,'death does not auto-award');
  walk(st,d.x,d.y);assert.equal(discoveryCheck(st,d.id),'');
  if(process.env.EX07_REVIEW_PENDING_PATH)writeFileSync(process.env.EX07_REVIEW_PENDING_PATH,JSON.stringify(makeSave(st),null,2));
  const inv=structuredClone(st.engineer.inv);applyCommands(st,[{type:'recoverSchematic',id:d.id},{type:'recoverSchematic',id:d.id}]);
  assert.ok(d.recoveredAt>=0);assert.deepEqual(st.engineer.inv,inv);assert.equal(manualRepairSeconds(st),2);assert.equal(manualRepairSeconds(st,true),6);
  const recovered=d.recoveredAt,loaded=loadState(makeSave(st));loaded.speed=st.speed;run(loaded,130);
  applyCommands(loaded,[{type:'recoverSchematic',id:d.id}]);assert.equal(loaded.campaign!.discovery!.recoveredAt,recovered);assert.equal(stalkersOf(loaded).length,0);
  assert.ok(conservation(st).ok,conservation(st).problems.join(','));
  if(process.env.EX07_REVIEW_PATH)writeFileSync(process.env.EX07_REVIEW_PATH,JSON.stringify(makeSave(st),null,2));
});

test('field repair retains material costs, paid pauses and existing job duration, including core recovery',()=>{
  const st=createCampaign(),d=st.campaign!.discovery!,core=st.campaign!.defence!.bases[0];
  st.engineer.inv.steel=100;st.engineer.inv.copper=50;st.flow!.ledger=openLedger(st);
  assert.ok(inReach(st,core.x,core.y,core.size));damageCore(st,core,80);
  applyCommands(st,[{type:'repairDefence',x:core.x,y:core.y}]);assert.equal(st.campaign!.defence!.repair!.remaining,4);
  // Acquired-tool fixture isolates its effect on a job already paid for.
  d.recoveredAt=0;d.seenAt=0;run(st,2);assert.equal(core.hp,220);run(st,2);assert.equal(core.hp,260);
  const steel=st.engineer.inv.steel,copper=st.engineer.inv.copper;
  applyCommands(st,[{type:'repairDefence',x:core.x,y:core.y}]);assert.equal(st.campaign!.defence!.repair!.remaining,2);
  const [x,y]=[st.engineer.x,st.engineer.y];st.engineer.x=core.x+20;run(st,3);assert.equal(core.hp,260);
  st.engineer.x=x;st.engineer.y=y;run(st,2);assert.equal(core.hp,300);assert.equal(steel-st.engineer.inv.steel,2);assert.equal(copper-st.engineer.inv.copper,1);
  damageCore(st,core,300);applyCommands(st,[{type:'repairDefence',x:core.x,y:core.y}]);run(st,5);assert.equal(core.hp,0);run(st,1);assert.equal(core.hp,300);
  st.engineer.inv.steel=0;damageCore(st,core,40);applyCommands(st,[{type:'repairDefence',x:core.x,y:core.y}]);assert.equal(st.campaign!.defence!.repair,null);
});

test('old previews add one deferred guardian; malformed or missing current reward state is rejected',()=>{
  const st=createCampaign(),d=st.campaign!.discovery!;st.engineer.x=d.x+.5;st.engineer.y=d.y+.5;
  delete st.campaign!.discovery;delete st.campaign!.recruits;delete st.campaign!.turbine;delete st.campaign!.knowledge;st.campaign!.version=4;
  const a=loadState(st),b=loadState(st);a.speed=1;assert.equal(stateHash(a),stateHash(b));assert.equal(stalkersOf(a).length,0);
  run(a,2);assert.equal(stalkersOf(a).length,0,'no spawn on the engineer');
  a.engineer.x+=20;run(a,.05);assert.equal(stalkersOf(a).length,1);
  assert.equal(stateHash(loadState(makeSave(a))),stateHash(a));
  for(const change of [
    (s:SimState)=>{delete s.campaign!.discovery;},
    (s:SimState)=>{s.campaign!.discovery!.id='duplicate';},
    (s:SimState)=>{s.campaign!.discovery!.guardian.stalkers=[];},
    (s:SimState)=>{s.campaign!.discovery!.guardian.stats.kills=1;},
    (s:SimState)=>{s.campaign!.discovery!.recoveredAt=NaN;},
    (s:SimState)=>{s.campaign!.discovery!.guardian.stalkers[0].path=[-1];},
  ]){const bad=structuredClone(a);change(bad);assert.throws(()=>loadState(bad));}
});

test('fresh campaign reaches and recovers the discovery through ordinary commands, then replays and reloads identically',()=>{
  const st=createCampaign(),d=st.campaign!.discovery!,log:LoggedCommand[]=[];
  const send=(commands:Command[])=>{for(const c of commands)log.push({tick:st.flow!.tick,c:structuredClone(c)});applyCommands(st,commands);};
  send([{type:'chestTake',item:'magazine',n:5}]);
  assert.ok((st.engineer.inv.magazine??0)>0);
  send([{type:'move',x:d.x+.5,y:d.y+.5}]);
  for(let i=0;i<4000&&(st.engineer.target||stalkersOf(st).length);i++) {
    const s=stalkersOf(st)[0];
    if(s&&Math.hypot(st.engineer.x-s.x,st.engineer.y-s.y)<9)send([{type:'aim',at:[s.x,s.y]}]);
    run(st,.05);
  }
  send([{type:'aim',at:null}]);assert.equal(stalkersOf(st).length,0);assert.ok(inReach(st,d.x,d.y,1));
  send([{type:'recoverSchematic',id:d.id}]);run(st,.05);assert.ok(d.recoveredAt>=0);
  const prior=Object.getOwnPropertyDescriptor(globalThis,'location');Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/?rules=exploration-v2'}});
  try {
    const session=createSession(parseUrl('http://localhost/?rules=exploration-v2'));session.state=st;session.log=log;
    const replay=replaySession(session,{rifleOff:false});assert.ok('state' in replay);assert.equal(stateHash(replay.state),stateHash(st));
  } finally {if(prior)Object.defineProperty(globalThis,'location',prior);else Reflect.deleteProperty(globalThis,'location');}
  const loaded=loadState(makeSave(st));loaded.speed=st.speed;run(st,2);run(loaded,2);assert.equal(stateHash(st),stateHash(loaded));
  console.log(JSON.stringify({seed:st.seed,ticks:st.flow!.tick,commands:log.length,recoveredAt:d.recoveredAt,hash:stateHash(st)}));
});

test('the records can be recovered by drawing the living guardian away, with no kill requirement',()=>{
  const st=createCampaign(),d=st.campaign!.discovery!,s=stalkersOf(st)[0];
  assert.match(discoveryCheck(st,d.id),/Walk closer/);
  applyCommands(st,[{type:'recoverSchematic',id:d.id},{type:'recoverSchematic',id:'another-site'}]);assert.equal(d.recoveredAt,-1);
  // Labelled starting-position fixture; ordinary walking supplies the activity cue and detour.
  st.engineer.x=s.hx+4;st.engineer.y=s.hy;
  run(st,.5,[{type:'walk',dx:1,dy:0}]);run(st,.05,[{type:'walk',dx:0,dy:0}]);
  for(let i=0;i<150&&Math.hypot(s.x-s.hx,s.y-s.hy)<=3;i++)run(st,.05);
  assert.ok(Math.hypot(s.x-s.hx,s.y-s.hy)>3,'guardian followed the distraction');
  assert.equal(discoveryCheck(st,d.id),'');applyCommands(st,[{type:'recoverSchematic',id:d.id}]);
  assert.ok(d.recoveredAt>=0);assert.equal(d.guardian.stats.kills,0);assert.equal(stalkersOf(st).length,1);
  assert.equal(stateHash(loadState(makeSave(st))),stateHash(st));
});

test('a paid missed rifle round attracts the workshop guardian even when the engineer stands still',()=>{
  const st=createCampaign(),d=st.campaign!.discovery!,s=stalkersOf(st)[0];
  applyCommands(st,[{type:'chestTake',item:'magazine',n:2}]);
  // Controlled stationary starting position, with an aim line facing away from the guardian.
  st.engineer.x=s.hx+4;st.engineer.y=s.hy;d.guardian.ex=st.engineer.x;d.guardian.ey=st.engineer.y;
  run(st,.05,[{type:'aim',at:[st.engineer.x+6,st.engineer.y]}]);
  assert.equal(s.hp,24);assert.equal(s.mode,'investigate');assert.equal(st.engineer.fired,1);
});
