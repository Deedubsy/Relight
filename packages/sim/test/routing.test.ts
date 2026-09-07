import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createCampaign, ensureFlow, applyCommands, actionResult, canPlace, placeable, inReach, machineAt,
  dimensions, machineDimensions, undergroundMate, undergroundSpan, splitterPorts, giveItem, tickRouting,
  stepFlow, conservation, stateHash, makeSave, loadState, advanceFlow, recipeOutput, recipeOf, blockOfTile, passable,
  type SimState, type Kind, type Dir, type Machine, type Command, type Item, type BuildEdit } from '../src/index';
import { createSession, parseUrl, dispatch, replaySession, makeSessionSave } from '../../game/src/session';

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
/** Explicit transport-unit fixture: relocate ordinary carried stock into a routing buffer, preserving the ledger. */
function seed(st:SimState,m:Machine,k:Item,n:number){assert.ok(st.engineer.inv[k]>=n);st.engineer.inv[k]-=n;for(let i=0;i<n;i++)m.items.push({k,p:m.kind==='splitter'?0:i*.25});}
const ticks=(st:SimState,n:number)=>{for(let i=0;i<n;i++)stepFlow(st);};

test('routing: underground span boundaries, paired construction and atomic interference/material/reach refusals',()=>{
 const st=fresh(),[x,y]=spot(st,[['underground',0,0],['underground',5,0]]);
 assert.equal(undergroundSpan({x,y},{x:x+5,y},1),'');assert.notEqual(undergroundSpan({x,y},{x:x+6,y},1),'');
 assert.notEqual(undergroundSpan({x,y},{x:x+1,y:y+1},1),'');assert.notEqual(undergroundSpan({x,y},{x:x-1,y},1),'');
 const cmd:Command={type:'undergroundPair',from:{x,y},to:{x:x+5,y},dir:1};
 send(st,cmd);assert.equal(st.construction!.undo.at(-1)!.length,2);
 assert.equal(undergroundMate(st,machineAt(st,x,y)!)?.id,machineAt(st,x+5,y)!.id);
  send(st,{type:'undoBuild'});send(st,{type:'redoBuild'});conserved(st);
 send(st,{type:'construct',edits:[{action:'pickUp',x:x+5,y}]});
 send(st,cmd);assert.equal(st.construction!.undo.at(-1)!.length,1,'replacement reuses the surviving input and charges only the new output');conserved(st);
 const before=stateHash(st);send(st,cmd,false);assert.equal(stateHash(st),before);
 const empty=createCampaign(3);ensureFlow(empty);const poor=stateHash(empty);send(empty,cmd,false);assert.equal(stateHash(empty),poor);
 send(st,{type:'undergroundPair',from:{x:x+100,y},to:{x:x+101,y},dir:1},false);assert.equal(stateHash(st),before);
});

test('routing: buried stock survives interruption, refused rotation, load, reconnection and pickup without duplication',()=>{
 const st=fresh(),[x,y]=spot(st,[['underground',0,0],['underground',4,0],['belt',5,0]]);
 send(st,{type:'undergroundPair',from:{x,y},to:{x:x+4,y},dir:1});const a=machineAt(st,x,y)!,b=machineAt(st,x+4,y)!;
 const belt=build(st,'belt',x+5,y);seed(st,a,'steel',3);
 for(let i=0;i<15;i++)tickRouting(st,a,.05);
 const before=stateHash(st);send(st,{type:'construct',edits:[{action:'rotate',x,y}]},false);assert.equal(stateHash(st),before);
 send(st,{type:'construct',edits:[{action:'pickUp',x:b.x,y:b.y}]});assert.equal(undergroundMate(st,a),undefined);
 const pos=a.items.map(i=>i.p);for(let i=0;i<100;i++)tickRouting(st,a,.05);assert.deepEqual(a.items.map(i=>i.p),pos);
 const loaded=loadState(makeSave(st));send(st,{type:'undoBuild'});send(loaded,{type:'undoBuild'});assert.equal(stateHash(st),stateHash(loaded));
 for(let i=0;i<160;i++){tickRouting(st,a,.05);tickRouting(st,machineAt(st,b.x,b.y)!, .05);}
 assert.equal(a.items.length+machineAt(st,b.x,b.y)!.items.length+belt.items.length,3);
 send(st,{type:'construct',edits:[{action:'pickUp',x,y}]});conserved(st);
});

test('routing: splitter footprint, both occupied tiles, collision-safe rotation and history/settings survive load',()=>{
 const st=fresh(),[x,y]=spot(st,[['splitter',0,0,0],['belt',0,1]]),m=build(st,'splitter',x,y,0);
 assert.deepEqual(machineDimensions(m),[2,1]);assert.equal(machineAt(st,x+1,y)?.id,m.id);
 build(st,'belt',x,y+1);let hash=stateHash(st);send(st,{type:'construct',edits:[{action:'rotate',x,y}]},false);assert.equal(stateHash(st),hash);
 send(st,{type:'undoBuild'});send(st,{type:'construct',edits:[{action:'rotate',x,y}]});assert.deepEqual(machineDimensions(m),[1,2]);
 assert.equal(machineAt(st,x,y+1)?.id,m.id);assert.equal(machineAt(st,x+1,y),undefined);
 send(st,{type:'factory',action:{type:'routing',x,y,priority:'right'}});
 send(st,{type:'construct',edits:[{action:'pickUp',x,y:y+1}]});send(st,{type:'undoBuild'});
 assert.equal(machineAt(st,x,y)!.priority,'right');const loaded=loadState(makeSave(st));assert.equal(stateHash(st),stateHash(loaded));
 hash=stateHash(st);send(st,{type:'undoBuild'},false);assert.equal(stateHash(st),hash);conserved(st);
});

test('routing: left/right priority, balanced alternation, fallback, backpressure and FIFO retain every item',()=>{
 for(const priority of ['left','right','balanced'] as const){
  const st=fresh(),[m,left,right]=planBuild(st,[['splitter',0,0,1],['belt',1,0,1],['belt',1,1,1]]);
  send(st,{type:'factory',action:{type:'routing',x:m.x,y:m.y,priority}});seed(st,m,'steel',2);
  tickRouting(st,m,.1);assert.equal(priority==='right'?right.items.length:left.items.length,1);
  if(priority==='balanced'){tickRouting(st,m,.1);assert.equal(right.items.length,1);}else{
   const preferred=priority==='left'?left:right,other=priority==='left'?right:left;
   // The first output's item is at its entry, so it refuses another until it advances.
   tickRouting(st,m,.1);assert.equal(other.items.length,1);assert.equal(preferred.items.length,1);
  }
  seed(st,m,'copper',1);tickRouting(st,m,.1);assert.equal(m.items.length,1);assert.equal(m.items[0].k,'copper');conserved(st);
  assert.equal(splitterPorts(m).length,2);
 }
});

test('routing: inserter filtering selects from mixed chests and belts and preserves a held item on filter change',()=>{
 const st=fresh(),[chest,arm,belt]=planBuild(st,[['chest',0,0],['inserter',2,0,1],['belt',3,0,1]]);
 for(const item of ['steel','copper'] as const)send(st,{type:'factory',action:{type:'chestPut',x:chest.x,y:chest.y,item,n:2}});
 send(st,{type:'factory',action:{type:'routing',x:arm.x,y:arm.y,filter:'copper'}});ticks(st,1);assert.equal(arm.hold,'copper');
 send(st,{type:'factory',action:{type:'routing',x:arm.x,y:arm.y,filter:'steel'}});assert.equal(arm.hold,'copper');ticks(st,11);
 assert.ok(belt.items.some(i=>i.k==='copper'));assert.equal(chest.inv.steel,2);conserved(st);
 const saved=loadState(makeSave(st));assert.equal(machineAt(saved,arm.x,arm.y)!.filter,'steel');
 const bad=structuredClone(st);bad.flow!.machines.find(m=>m.id===arm.id)!.filter='bogus' as Item;assert.throws(()=>loadState(bad),/filter/);
});

test('routing: paired commands and settings replay at a paused boundary and survive saved history',()=>{
 Object.assign(globalThis,{location:{href:'http://localhost/?rules=exploration-v2'}});
 const s=createSession(parseUrl('?rules=exploration-v2&seed=3&view=world'));
 for(const item of ['steel','copper'])assert.ok(dispatch(s,{type:'factory',action:{type:'chestTake',item,n:50}}).ok);
 const [x,y]=spot(s.state,[['underground',0,0],['underground',3,0]]);
 assert.ok(dispatch(s,{type:'undergroundPair',from:{x,y},to:{x:x+3,y},dir:1}).ok);
 dispatch(s,{type:'undoBuild'});dispatch(s,{type:'redoBuild'});
 const replay=replaySession(s,{rifleOff:false});assert.ok('state' in replay);assert.equal(stateHash(s.state),stateHash(replay.state));
 assert.equal(stateHash(s.state),stateHash(loadState(makeSessionSave(s))));conserved(s.state);
});

test('routing: crossing tunnels pair independently and an intervening endpoint refuses a new pair atomically',()=>{
 const st=fresh(),[x,y]=spot(st,[['underground',0,2],['underground',4,2],['underground',2,0],['underground',2,4]]);
 send(st,{type:'undergroundPair',from:{x,y:y+2},to:{x:x+4,y:y+2},dir:1});
 send(st,{type:'undergroundPair',from:{x:x+2,y},to:{x:x+2,y:y+4},dir:2});
 assert.equal(undergroundMate(st,machineAt(st,x,y+2)!)!.x,x+4);
 assert.equal(undergroundMate(st,machineAt(st,x+2,y)!)!.y,y+4);conserved(st);
 const other=fresh(),[a,b]=spot(other,[['underground',0,0],['underground',2,0],['underground',5,0]]);
 build(other,'underground',a+2,b);const before=stateHash(other);
 send(other,{type:'undergroundPair',from:{x:a,y:b},to:{x:a+5,y:b},dir:1},false);assert.equal(stateHash(other),before);
});

test('routing: filter applies to mixed belt and station-arrival source fixtures',()=>{
 for(const source of ['belt','arrivals'] as const){
  const st=fresh(),width=source==='belt'?1:2;
  const [src,arm,dst]=planBuild(st,[[source==='belt'?'belt':'chest',0,0,1],['inserter',width,0,1],['belt',width+1,0,1]]);
  if(source==='belt'){
   st.engineer.inv.steel-=2;st.engineer.inv.copper--;src.items=[{k:'steel',p:0},{k:'copper',p:.25},{k:'steel',p:.5}];
  }else{
   // Explicit arrivals fixture, not station-restoration/freight progression evidence. Stock is transferred from pockets.
   st.engineer.inv.steel-=2;st.engineer.inv.copper--;src.kind='tramstop';src.cargo={steel:2,copper:1};
  }
  send(st,{type:'factory',action:{type:'routing',x:arm.x,y:arm.y,filter:'copper'}});ticks(st,1);assert.equal(arm.hold,'copper');
  ticks(st,12);assert.ok(dst.items.some(it=>it.k==='copper'));assert.ok(!dst.items.some(it=>it.k==='steel'));conserved(st);
 }
});

test('routing: rectangular occupancy follows all four rotations, including the ordinary command path',()=>{
 const st=fresh(),[x,y]=spot(st,[['splitter',0,0,0],['splitter',0,0,1]]),m=build(st,'splitter',x,y,0);
 for(let i=1;i<=4;i++){
  applyCommands(st,[{type:'rotate',x,y}]);assert.equal(m.dir,i%4);
  const [w,h]=machineDimensions(m);assert.equal(st.flow!.occ[(y+h-1)*st.flow!.tw+x+w-1],m.id);
  assert.equal(machineAt(st,x+(w===1?1:0),y+(h===1?1:0)),undefined);
  assert.equal(stateHash(loadState(makeSave(st))),stateHash(st));
 }
 conserved(st);
});

test('routing: saturated splitter and underground throughput stay bounded and conserve their circulating fixture stock',()=>{
 const st=fresh(),[m,left,right]=planBuild(st,[['splitter',0,0,1],['belt',1,0,1],['belt',1,1,1]]);
 let sent=0;
 for(let i=0;i<200;i++){
  if(m.items.length<8)seed(st,m,'steel',1);tickRouting(st,m,.05);
  for(const dst of [left,right]){sent+=dst.items.length;st.engineer.inv.steel+=dst.items.length;dst.items=[];}
 }
 assert.ok(sent>=148&&sent<=150,`${sent} splitter items/10s`);conserved(st);
 const ug=fresh(),[x,y]=spot(ug,[['underground',0,0],['underground',5,0],['belt',6,0]]);
 send(ug,{type:'undergroundPair',from:{x,y},to:{x:x+5,y},dir:1});const a=machineAt(ug,x,y)!,b=machineAt(ug,x+5,y)!;const dst=build(ug,'belt',x+6,y);
 let afterWarmup=0;
 for(let i=0;i<400;i++){
  if(giveItem(ug,a,'steel'))ug.engineer.inv.steel--;
  tickRouting(ug,a,.05);tickRouting(ug,b,.05);
  if(i>=200)afterWarmup+=dst.items.length;
  ug.engineer.inv.steel+=dst.items.length;dst.items=[];
 }
 assert.ok(afterWarmup>=70&&afterWarmup<=76,`${afterWarmup} underground items/10s after warmup`);conserved(ug);
});

test('routing: all four recipes run through a paid chest/inserter/belt/underground/splitter line reached on foot',()=>{
 const st=fresh(),e=st.engineer;
 const plan:Plan=[['chest',0,0],['inserter',2,0,1],['assembler',3,0],['inserter',6,1,1],['belt',7,1,1],['underground',8,1,1],['underground',11,1,1],['splitter',12,1,1],['chest',13,1],['generator',5,4]];
 let origin:[number,number]|undefined;
 outer:for(let radius=10;radius<=70;radius+=10)for(let y=Math.floor(e.y)-radius;y<=e.y+radius;y++)for(let x=Math.floor(e.x)-radius;x<=e.x+radius;x++){
  const bi=blockOfTile(st,x+5,y+4);
  if(!passable(st,x+7,y+4)||[2,3,6].some(dx=>blockOfTile(st,x+dx,y+1)!==bi))continue;
  if(plan.every(([k,dx,dy,d=0])=>!placeable(st,k,x+dx,y+dy,d))){origin=[x,y];break outer;}
 }
 assert.ok(origin,'a real campaign site fits the line');const [x,y]=origin;
 applyCommands(st,[{type:'move',x:x+7.5,y:y+4.5}]);st.speed=1;
 for(let i=0;i<6000&&Math.hypot(e.x-x-7.5,e.y-y-4.5)>.8;i++)advanceFlow(st,.05,[]);
 assert.ok(Math.hypot(e.x-x-7.5,e.y-y-4.5)<.8,'engineer walks to the construction site');
 const edits:BuildEdit[]=plan.filter(([k])=>k!=='underground').map(([item,dx,dy,dir=0])=>({action:'place',item,x:x+dx,y:y+dy,dir}));
 send(st,{type:'construct',edits});send(st,{type:'undergroundPair',from:{x:x+8,y:y+1},to:{x:x+11,y:y+1},dir:1});
 send(st,{type:'factory',action:{type:'feed',x:x+5,y:y+4}});
 const asm=machineAt(st,x+3,y)!,output=machineAt(st,x+13,y+1)!;
 for(const recipe of ['shot','wire','frame','board'] as const){
  send(st,{type:'factory',action:{type:'setRecipe',x:asm.x,y:asm.y,recipe}});
  const inputs=recipe==='shot'?{steel:4,copper:2}:recipe==='wire'?{copper:4}:recipe==='frame'?{steel:4}:{wire:6,steel:2};
  if(recipe==='board')send(st,{type:'factory',action:{type:'chestTake',x:output.x,y:output.y,item:'wire',n:6}});
  for(const [item,n] of Object.entries(inputs))send(st,{type:'factory',action:{type:'chestPut',x,y,item,n}});
  const key=recipeOutput(recipeOf(asm)),before=output.inv[key]??0;
  send(st,{type:'factory',action:{type:'routing',x:x+6,y:y+1,filter:key}});
  for(let i=0;i<800;i++)advanceFlow(st,.05,[]);
  assert.ok((output.inv[key]??0)>before,`${recipe} output reaches storage through the complete line: ${JSON.stringify(st.flow!.machines.filter(m=>m.x>=x&&m.x<x+15&&m.y>=y&&m.y<y+6).map(m=>({kind:m.kind,x:m.x,y:m.y,dir:m.dir,inv:m.inv,items:m.items,hold:m.hold,out:m.out,busy:m.busy})))}`);conserved(st);
  const loaded=loadState(makeSave(st));for(let i=0;i<20;i++){advanceFlow(st,.05,[]);advanceFlow(loaded,.05,[{type:'setSpeed',mult:1}]);}
  assert.equal(stateHash(st),stateHash(loaded),`${recipe} save continuation`);
 }
});
