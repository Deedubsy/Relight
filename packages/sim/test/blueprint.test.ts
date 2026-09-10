import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import {loadSnapshot} from '../../game/src/session';
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {applyCommands, actionResult, blueprintCopy, blueprintTransform, blueprintBounds, blueprintCheck,
  parseBlueprint, canPlace, inReach, machineAt, stateHash, loadState, makeSave, conservation, findPath, campaignRecruited,
  type SimState, type Command, type Kind, type TilePoint, type BlueprintTransform} from '../src/index';
import {expedition, walk} from './transportFixture';
import {FactoryDriver, replayInterval} from '../../harness/src/blueprint';
function send(st:SimState,c:Command,ok=true){applyCommands(st,[c]);assert.equal(actionResult(st).ok,ok,actionResult(st).reason);}
function stocked(){const st=createCampaign();for(const [item,n] of [['steel',180],['copper',90]] as const)send(st,{type:'factory',action:{type:'chestTake',item,n}});return st;}
/** Labelled unlock fixture for isolated failure/configuration checks; the end-to-end test recruits by walking. */
function ready(){const st=stocked(),s=st.campaign!.recruits!.sites.find(s=>s.kind==='foreman')!;s.seenAt=s.recruitedAt=0;return st;}
function spot(st:SimState,kind:Kind='belt',w=1,h=1):TilePoint {
  const e=st.engineer;for(let y=Math.floor(e.y)-6;y<e.y+6;y++)for(let x=Math.floor(e.x)-6;x<e.x+6;x++){
    let good=true;for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++)if(!canPlace(st,kind,xx,yy,0).ok||!inReach(st,xx,yy,kind==='assembler'?3:1))good=false;
    if(good)return {x,y};
  }throw new Error(`No spot for ${kind}`);
}
const build=(item:Kind,p:TilePoint):Command=>({type:'construct',edits:[{action:'place',item,...p,dir:0}]});
const copy=(st:SimState,p:TilePoint,w=1,h=1)=>send(st,{type:'blueprintCopy',from:p,to:{x:p.x+w-1,y:p.y+h-1}});

test('clipboard rejects locked, empty, malformed, partial, remote and down selections without replacing stock or previous clipboard',()=>{
  const locked=stocked(),p=spot(locked);send(locked,build('belt',p));const hash=stateHash(locked);
  send(locked,{type:'blueprintCopy',from:p,to:p},false);assert.equal(stateHash(locked),hash);
  send(locked,{type:'blueprintPaste',...p},false);assert.equal(stateHash(locked),hash);
  const st=ready(),a=spot(st,'assembler');send(st,build('assembler',a));copy(st,a,3,3);const old=stateHash(st);
  for(const [from,to] of [[a,a],[{x:NaN,y:0},a],[a,{x:a.x+129,y:a.y}]]){send(st,{type:'blueprintCopy',from,to},false);assert.equal(stateHash(st),old);}
  const e=st.engineer;st.engineer.x+=100;const far=stateHash(st);send(st,{type:'blueprintCopy',from:a,to:{x:a.x+2,y:a.y+2}},false);assert.equal(stateHash(st),far);e.x-=100;
  e.down=0;const down=stateHash(st);send(st,{type:'blueprintPaste',...a},false);assert.equal(stateHash(st),down);
});

test('rotations and reflections preserve rectangular footprints, tunnel roles and settings with correct left/right priority',()=>{
  const bp=parseBlueprint({version:1,name:'asymmetric',entities:[
    {id:'split',kind:'splitter',x:0,y:0,dir:0,priority:'left'},
    {id:'in',kind:'underground',x:0,y:2,dir:1,underground:'input'},
    {id:'out',kind:'underground',x:4,y:2,dir:1,underground:'output'},
    {id:'arm',kind:'inserter',x:3,y:0,dir:3,filter:'copper'},
    {id:'stop',kind:'tramstop',x:6,y:0,dir:2,freight:{magazine:{request:20,reserve:5,export:false}}}
  ]});
  const saved=structuredClone(bp);let r=bp;for(let i=0;i<4;i++)r=blueprintTransform(r,'rotate');assert.deepEqual(r,bp);
  for(const op of ['mirrorX','mirrorY'] as BlueprintTransform[]){const m=blueprintTransform(bp,op);assert.equal(m.entities[0].priority,'right');assert.deepEqual(blueprintTransform(m,op),bp);assert.deepEqual(m.entities[4].freight,bp.entities[4].freight);}
  const turn=blueprintTransform(bp,'rotate');assert.equal(blueprintBounds(turn).w,blueprintBounds(bp).h);assert.equal(turn.entities[0].dir,1);assert.deepEqual(bp,saved);
  assert.throws(()=>blueprintTransform(bp,'wrong' as BlueprintTransform),/Invalid/);
  assert.throws(()=>parseBlueprint({...bp,entities:[{...bp.entities[0],freight:{}}]}),/freight/);
});

test('clipboard stamp applies recipe and routing configuration atomically, saves and undoes without copying buffers',()=>{
  for(const kind of ['assembler','inserter','splitter'] as const){
    const st=ready();
    const p=spot(st,kind),settings=kind==='assembler'?{recipe:'wire' as const}:kind==='inserter'?{filter:'copper' as const}:{priority:'left' as const};
    send(st,{type:'construct',edits:[{action:'place',item:kind,...p,dir:0,...settings}]});
    const m=machineAt(st,p.x,p.y)!,size=kind==='assembler'?3:kind==='splitter'?2:1;
    if(kind==='assembler'){// Labelled contents fixture moves one real carried input into its buffer.
      st.engineer.inv.copper--;m.inv.copper=1;}
    const before=structuredClone(m),stock={...st.engineer.inv};copy(st,p,size,kind==='assembler'?3:1);
    assert.deepEqual(machineAt(st,p.x,p.y),before);assert.deepEqual(st.engineer.inv,stock);
    const q=spot(st,kind);assert.ok(blueprintCheck(st,q.x,q.y).ok);
    send(st,{type:'blueprintPaste',...q});const pasted=machineAt(st,q.x,q.y)!;
    for(const [k,v] of Object.entries(settings))assert.equal((pasted as unknown as Record<string,unknown>)[k],v);
    assert.equal(Object.values(pasted.inv).reduce((a,b)=>a+b,0),0);
    assert.equal(st.construction!.undo.at(-1)!.length,1);const hash=stateHash(st);assert.equal(stateHash(loadState(st)),hash);
    send(st,{type:'undoBuild'});assert.equal(machineAt(st,q.x,q.y),undefined);send(st,{type:'redoBuild'});
    assert.ok(conservation(st).ok,conservation(st).problems.join(','));
  }
});

test('occupied, unaffordable and remote stamps are atomic and consume carried machines before materials',()=>{
  const st=ready(),p=spot(st,'belt',3,3);send(st,build('belt',p));send(st,build('belt',{x:p.x+1,y:p.y}));copy(st,p,2);
  send(st,build('belt',{x:p.x+1,y:p.y+2}));const hash=stateHash(st);send(st,{type:'blueprintPaste',x:p.x,y:p.y+2},false);assert.equal(stateHash(st),hash);
  send(st,{type:'blueprintPaste',x:p.x+100,y:p.y},false);assert.equal(stateHash(st),hash);
  send(st,{type:'construct',edits:[{action:'pickUp',...p},{action:'pickUp',x:p.x+1,y:p.y}]});const steel=st.engineer.inv.steel;
  send(st,{type:'blueprintPaste',...p});assert.equal(st.engineer.inv.steel,steel);assert.equal(st.engineer.inv.belt??0,0);assert.ok(conservation(st).ok);
  // Labelled stock-exhaustion fixture isolates the no-partial-spend transaction.
  st.engineer.inv.steel=1;const poor=stateHash(st);send(st,{type:'blueprintPaste',x:p.x,y:p.y+1},false);assert.equal(stateHash(st),poor);
});

test('copied underground pairs reject an intervening destination endpoint and orphan selections',()=>{
  const st=ready(),p=spot(st,'belt',5,3);
  send(st,{type:'undergroundPair',from:p,to:{x:p.x+4,y:p.y},dir:1});
  assert.throws(()=>blueprintCopy(st,p,p),/paired/);copy(st,p,5);
  send(st,{type:'construct',edits:[{action:'place',item:'underground',x:p.x+2,y:p.y+2,dir:1,underground:'output'}]});
  const hash=stateHash(st);send(st,{type:'blueprintPaste',x:p.x,y:p.y+2},false);assert.equal(stateHash(st),hash);
});

test('old post-P7 saves retain every existing site/layout/earned reward and gain an unrecruited Foreman and permanent tram infrastructure',()=>{
  for(const name of ['fresh','network','turbine']){
    const raw=JSON.parse(readFileSync(new URL(`../../../docs/evidence/ex08c-2026-09-08/campaign/${name}.json`,import.meta.url),'utf8')),old=raw.state,st=loadState(raw);
    assert.equal(st.campaign!.version,10);assert.equal(st.campaign!.recruits!.version,4);
    assert.deepEqual(st.campaign!.recruits!.sites.slice(0,4),old.campaign.recruits.sites);assert.deepEqual(st.campaign!.turbine,old.campaign.turbine);
    for(const m of old.flow.machines)assert.deepEqual(st.flow!.machines.find(n=>n.id===m.id),m);assert.equal(st.campaign!.fixedTram!.stops.length,4);assert.equal(campaignRecruited(st,'foreman'),false);assert.equal(st.campaign!.clipboard,undefined);
    assert.equal(stateHash(loadState(st)),stateHash(st));
  }
  const st=ready(),p=spot(st);send(st,build('belt',p));copy(st,p);const bad=structuredClone(st);(bad.campaign!.clipboard as unknown as {contents:number}).contents=100;assert.throws(()=>loadState(bad),/clipboard/);
  const missing=structuredClone(st);missing.campaign!.recruits!.sites.pop();assert.throws(()=>loadState(missing),/recruits/);
});

test('Foreman shelters are reachable, separated from earlier sites and unchanged across save/load on 16 seeds',()=>{
  for(const seed of [1,2,3,4,5,6,7,8,9,10,11,12,13,17,23,31]){const st=createCampaign(seed),s=st.campaign!.recruits!.sites.at(-1)!;
    assert.equal(s.kind,'foreman');assert.ok(findPath(st,Math.floor(st.engineer.x),Math.floor(st.engineer.y),s.x,s.y),`seed ${seed}`);
    const t=st.campaign!.turbine!;assert.ok(Math.hypot(s.x-t.x,s.y-t.y)>t.size+4);assert.match(canPlace(st,'belt',s.x,s.y).reason,/shelter/);
    assert.deepEqual(loadState(st).campaign!.recruits!.sites.at(-1),s);
  }
});

test('ordinary stock, walking, Foreman recruitment, copy, transform, paid stamp and undo/redo replay from the opening',()=>{
  const st=createCampaign(3),initial=structuredClone(st),d=new FactoryDriver(st),home={x:st.engineer.x,y:st.engineer.y};
  d.take('steel',30);const s=st.campaign!.recruits!.sites.find(s=>s.kind==='foreman')!;
  d.approach(s.x,s.y);d.send({type:'recruitSurvivors',id:s.id});assert.ok(campaignRecruited(st,'foreman'));
  const earned=s.recruitedAt;d.send({type:'recruitSurvivors',id:s.id});assert.equal(s.recruitedAt,earned);d.walk(home.x,home.y);
  const p=spot(st,'belt',3,3);d.send(build('belt',p));d.send(build('belt',{x:p.x+1,y:p.y}));
  d.send({type:'blueprintCopy',from:p,to:{x:p.x+1,y:p.y}});assert.equal(st.campaign!.clipboard!.entities.length,2);d.send({type:'blueprintTransform',operation:'mirrorX'});
  d.send({type:'blueprintPaste',x:p.x,y:p.y+2});assert.ok(actionResult(st).ok,actionResult(st).reason);
  d.send({type:'undoBuild'});d.send({type:'redoBuild'});assert.ok(conservation(st).ok);
  assert.equal(stateHash(replayInterval(initial,d.log,st.flow!.tick)),stateHash(st));assert.equal(stateHash(loadState(st)),stateHash(st));
});


test('permanent stations retain editable freight rules but copying cannot build another station',()=>{
  const {st}=expedition(),s=st.campaign!.recruits!.sites.find(s=>s.kind==='foreman')!;
  s.seenAt=s.recruitedAt=st.t;const [a,b]=st.campaign!.expansion!.stops;
  const rules={magazine:{request:20,reserve:5,export:false},steel:{request:0,reserve:10,export:true}};
  walk(st,a[0],a[1],2);send(st,{type:'setStationRules',x:a[0],y:a[1],rules});
  copy(st,{x:a[0],y:a[1]},2,2);assert.deepEqual(st.campaign!.clipboard!.entities[0].freight,rules);
  walk(st,b[0],b[1],2);const before=stateHash(st);send(st,{type:'blueprintPaste',x:b[0]+5,y:b[1]},false);assert.equal(stateHash(st),before);assert.ok(conservation(st).ok);
});


test('old checkpoint logs remain incomplete after upgrading; current campaign saves remain complete',async()=>{
  const old=JSON.parse(readFileSync(new URL('../../../docs/evidence/ex08c-2026-09-08/campaign/network.json',import.meta.url),'utf8'));
  const before=Object.getOwnPropertyDescriptor(globalThis,'localStorage');let raw=JSON.stringify(old);
  Object.defineProperty(globalThis,'localStorage',{configurable:true,value:{getItem:()=>raw}});
  try{const loaded=await loadSnapshot('local:old');assert.equal(loaded.logComplete,false);assert.deepEqual(loaded.log,old.log);
    raw=JSON.stringify({...old,state:loaded.state,logComplete:true});assert.equal((await loadSnapshot('local:upgraded')).logComplete,false,'upgrading metadata cannot change the saved survey revision');
    raw=JSON.stringify(makeSave(createCampaign(3),{log:[],logComplete:true}));assert.equal((await loadSnapshot('local:current')).logComplete,true);
  }finally{if(before)Object.defineProperty(globalThis,'localStorage',before);else Reflect.deleteProperty(globalThis,'localStorage');}
});
