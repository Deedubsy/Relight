import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {writeFileSync} from 'node:fs';
import {applyCommands,actionResult,canPlace,inReach,machineAt,stateHash,loadState,conservation,removalPreview,dimensions,MACHINE_SIZE,invCap,stackSize,type SimState,type Command,type Kind,type RemovalSelection} from '../src/index';
function send(st:SimState,c:Command,ok=true){applyCommands(st,[c]);assert.equal(actionResult(st).ok,ok,actionResult(st).reason);}
function ready(){const st=createCampaign(3);send(st,{type:'factory',action:{type:'chestTake',item:'steel',n:100}});send(st,{type:'factory',action:{type:'chestTake',item:'copper',n:60}});const r=st.campaign!.recruits!.sites.find(r=>r.kind==='foreman')!;r.seenAt=r.recruitedAt=0;return st;}
function spot(st:SimState,kind:Kind='belt',width=1){const [w,h]=dimensions(kind,1,MACHINE_SIZE[kind]);for(let y=Math.floor(st.engineer.y)-6;y<st.engineer.y+6;y++)for(let x=Math.floor(st.engineer.x)-6;x<st.engineer.x+6;x++)if(Array.from({length:width},(_,i)=>canPlace(st,kind,x+i*w,y,1).ok&&inReach(st,x+i*w,y,w,h)).every(Boolean))return {x,y};throw Error('No reachable site');}
function put(st:SimState,kind:Kind='belt'){const p=spot(st,kind);send(st,{type:'construct',edits:[{action:'place',item:kind,...p,dir:1}]});return machineAt(st,p.x,p.y)!;}
function selection(st:SimState,m:ReturnType<typeof put>){const [w,h]=dimensions(m.kind,m.dir,MACHINE_SIZE[m.kind]);return removalPreview(st,{x:m.x,y:m.y},{x:m.x+w-1,y:m.y+h-1});}
function pack(st:SimState,s:RemovalSelection,ok=true){send(st,{type:'removeArea',selection:s},ok);}

test('area removal: read-only whole-footprint preview, atomic paid packing, settings undo and saved redo',()=>{
 const st=ready(),m=put(st,'assembler');send(st,{type:'factory',action:{type:'setRecipe',x:m.x,y:m.y,recipe:'wire'}});const before=stateHash(st),p=selection(st,m);assert.ok(p.ok,p.reason);assert.equal(stateHash(st),before);assert.deepEqual(p.items,{assembler:1});
 assert.ok(!removalPreview(st,{x:m.x,y:m.y},{x:m.x,y:m.y}).ok);pack(st,p.selection!);assert.equal(st.engineer.inv.assembler,1);assert.ok(!machineAt(st,m.x,m.y));
 send(st,{type:'undoBuild'});assert.equal(machineAt(st,m.x,m.y)!.recipe,'wire');assert.equal(machineAt(st,m.x,m.y)!.id,m.id);const saved=loadState(st);send(st,{type:'redoBuild'});send(saved,{type:'redoBuild'});assert.equal(stateHash(saved),stateHash(st));assert.ok(conservation(st).ok);
 const hash=stateHash(st);pack(st,p.selection!,false);assert.equal(stateHash(st),hash);
});

test('area removal: changed identities/settings, new neighbours and changed reach refuse without partial effects',()=>{
 const st=ready(),p=spot(st,'belt',3);send(st,{type:'construct',edits:[{action:'place',item:'belt',...p,dir:1}]});const preview=removalPreview(st,p,{x:p.x+2,y:p.y});assert.ok(preview.ok);
 send(st,{type:'construct',edits:[{action:'place',item:'belt',x:p.x+2,y:p.y,dir:1}]});let hash=stateHash(st);pack(st,preview.selection!,false);assert.equal(stateHash(st),hash);
 const next=removalPreview(st,p,{x:p.x+2,y:p.y});send(st,{type:'construct',edits:[{action:'rotate',...p}]});hash=stateHash(st);pack(st,next.selection!,false);assert.equal(stateHash(st),hash);
 const replaced=removalPreview(st,p,{x:p.x+2,y:p.y});send(st,{type:'construct',edits:[{action:'pickUp',...p},{action:'place',item:'belt',...p,dir:2}]});hash=stateHash(st);pack(st,replaced.selection!,false);assert.equal(stateHash(st),hash);
 const current=removalPreview(st,p,{x:p.x+2,y:p.y});st.engineer.x+=100;hash=stateHash(st);pack(st,current.selection!,false);assert.equal(stateHash(st),hash); // labelled reach fixture
});

test('area removal: cumulative pocket capacity refuses the whole group then recovers',()=>{
 const st=ready(),p=spot(st,'belt',2);send(st,{type:'construct',edits:[{action:'place',item:'belt',...p,dir:1},{action:'place',item:'belt',x:p.x+1,y:p.y,dir:1}]});const selection=removalPreview(st,p,{x:p.x+1,y:p.y}).selection!;
 // Labelled stock/capacity fixture: exactly one slot left in the last belt stack.
 st.engineer.inv={belt:invCap(st.engineer)*stackSize('belt')-1};let hash=stateHash(st);pack(st,selection,false);assert.equal(stateHash(st),hash);st.engineer.inv.belt--;pack(st,selection);assert.equal(st.engineer.inv.belt,invCap(st.engineer)*stackSize('belt'));
});

test('area removal: loaded belts and hidden tunnel items return once; undo does not refill machines',()=>{
 for(const kind of ['belt','underground'] as const){const st=ready(),m=put(st,kind);if(kind==='underground')m.underground='input';
 // Labelled loaded transport fixture; exact item counts checked independently of its injected contents.
 m.items=[{k:'steel',p:kind==='underground'?2.5:.4},{k:'copper',p:.8}];const before={...st.engineer.inv},p=selection(st,m);assert.ok(p.ok,p.reason);pack(st,p.selection!);assert.equal(st.engineer.inv.steel,before.steel+1);assert.equal(st.engineer.inv.copper,before.copper+1);send(st,{type:'undoBuild'});assert.deepEqual(machineAt(st,m.x,m.y)!.items,[]);assert.equal(st.engineer.inv.steel,before.steel+1);send(st,{type:'redoBuild'});assert.equal(st.engineer.inv.copper,before.copper+1);}
});

test('area removal: loose turret rounds require buffer space and never disappear',()=>{
 const st=ready(),m=put(st,'turret');m.inv.rounds=13;st.buffer=st.config.bufferCap;const p=selection(st,m);assert.ok(!p.ok);assert.equal(p.looseRounds,3);const hash=stateHash(st);pack(st,p.selection!,false);assert.equal(stateHash(st),hash);st.buffer-=3;const lost=st.stats.roundsLost??0;pack(st,p.selection!);assert.equal(st.buffer,st.config.bufferCap);assert.equal(st.engineer.inv.magazine,1);assert.equal(st.stats.roundsLost??0,lost);send(st,{type:'undoBuild'});assert.equal(machineAt(st,m.x,m.y)!.inv.rounds??0,0);
});

test('area removal: busy machines, fractional contents, damaged defences and installed facilities stay',()=>{
 for(const kind of ['assembler','mixer','wall','chest'] as const){const st=ready();if(kind==='mixer'){const r=st.campaign!.recruits!.sites.find(r=>r.kind==='concrete')!;r.seenAt=r.recruitedAt=0;}const m=put(st,kind);
 if(kind==='assembler'||kind==='mixer')m.busy=true;else if(kind==='wall')m.hp=1;else st.flow!.projects['supply-depot']={projectId:'supply-depot',stage:'restored',installId:m.id} as never;
 const hash=stateHash(st),p=selection(st,m);assert.ok(!p.ok,kind);pack(st,p.selection!,false);assert.equal(stateHash(st),hash);}
 const st=ready(),m=put(st,'chest');m.inv.copper=.5;assert.ok(!selection(st,m).ok);delete m.inv.copper;m.cargo={steel:.5};assert.ok(!selection(st,m).ok);
 const depot=st.flow!.machines.find(m=>m.kind==='depot')!;assert.ok(!selection(st,depot).ok);const locked=createCampaign();assert.ok(!removalPreview(locked,{x:0,y:0},{x:1,y:1}).ok);assert.ok(!removalPreview(st,{x:0,y:0},{x:128,y:1}).ok);
});

test('area removal: cargo, held items and freight/filter/priority settings survive conserved packing history',()=>{
 for(const kind of ['chest','inserter','splitter'] as const){const st=ready();const foreman=st.campaign!.recruits!.sites.find(r=>r.kind==='foreman')!;foreman.seenAt=foreman.recruitedAt=st.t;const m=put(st,kind);if(kind==='chest')m.cargo={steel:3};if(kind==='inserter'){m.filter='copper';m.hold='copper';}if(kind==='splitter')m.priority='right';
 const initial={...st.engineer.inv},p=selection(st,m);assert.ok(p.ok,p.reason);pack(st,p.selection!);send(st,{type:'undoBuild'});const restored=machineAt(st,m.x,m.y)!;assert.equal(restored.filter,m.filter);assert.equal(restored.priority,m.priority);assert.deepEqual(restored.freight,m.freight);if(m.cargo){assert.equal(st.engineer.inv.steel,initial.steel+3);assert.equal(restored.cargo?.steel??0,0);}if(m.hold){assert.equal(st.engineer.inv.copper,initial.copper+1);assert.equal(restored.hold,null);}}
});

test('area removal: ordinary paid command comparison records equal layouts/materials without a human tedium claim',()=>{
 const manual=ready(),tools=loadState(manual),p=spot(manual,'belt',6),layout=Array.from({length:6},(_,i)=>({action:'place' as const,item:'belt' as const,x:p.x+i,y:p.y,dir:1 as const})),rows=[];
 for(const [name,st] of [['manual',manual],['tools',tools]] as const){const initial={...st.engineer.inv},start=performance.now();let commands=0;for(const e of layout){send(st,{type:'construct',edits:[e]});commands++;}if(name==='tools'){send(st,{type:'blueprintCopy',from:p,to:{x:p.x+5,y:p.y}});commands++;}
 const paid={...st.engineer.inv};if(name==='manual'){for(const e of layout){send(st,{type:'construct',edits:[{action:'pickUp',x:e.x,y:e.y}]});commands++;}for(const e of layout){send(st,{type:'construct',edits:[e]});commands++;}for(const e of layout){send(st,{type:'construct',edits:[{action:'pickUp',x:e.x,y:e.y}]});commands++;}}else{pack(st,removalPreview(st,p,{x:p.x+5,y:p.y}).selection!);commands++;send(st,{type:'blueprintPaste',...p});commands++;pack(st,removalPreview(st,p,{x:p.x+5,y:p.y}).selection!);commands++;}
 assert.ok(conservation(st).ok);rows.push({name,commands,simulationSeconds:st.t,wallMs:performance.now()-start,initial,afterPaidPlacement:paid,final:{...st.engineer.inv},failures:0});}
 assert.deepEqual(manual.engineer.inv,tools.engineer.inv);if(process.env.P8_COMPARISON)writeFileSync(process.env.P8_COMPARISON,JSON.stringify({rows,assistance:'Foreman recruited by labelled fixture; opening supplies withdrawn with ordinary commands; shared reachable six-belt layout; both paths build, pack, rebuild and pack the same six-belt layout; tool path includes copying its ordinarily built source. Timings are headless command execution, not human task time or tedium evidence.'},null,2));
});
