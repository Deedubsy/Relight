import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createCampaign, stateHash, conservation, ground, canPlace, MACHINE_COST, loadState } from '../src/index';
import { FactoryDriver, parseBlueprint, buildBlueprint, replayInterval } from '../../harness/src/blueprint';
import { factoryScenario, factoryLine } from '../../harness/src/factoryScenario';
import { logistics } from '../../harness/src/campaignExperiments';
const raw=JSON.parse(readFileSync(new URL('../../harness/blueprints/factory-line.json',import.meta.url),'utf8'));

test('factory blueprint validates structure, rectangular occupancy, settings, paired endpoints and bounded input before issuing commands',()=>{
 const bp=parseBlueprint(raw);assert.equal(bp.entities.length,10);
 const invalid:unknown[]=[null,{...raw,version:2},{...raw,entities:[]},{...raw,debugStock:1000},{...raw,entities:Array(129).fill(raw.entities[0])}];
 for(const change of [(e:Record<string,unknown>)=>e.x=Infinity,(e:Record<string,unknown>)=>e.x=.5,(e:Record<string,unknown>)=>e.kind='depot',(e:Record<string,unknown>)=>e.dir=4,(e:Record<string,unknown>)=>e.command='teleport',(e:Record<string,unknown>)=>e.filter='steel']){const copy=structuredClone(raw);change(copy.entities[0]);invalid.push(copy);}
 const duplicate=structuredClone(raw);duplicate.entities[1].id=duplicate.entities[0].id;invalid.push(duplicate);
 const overlap=structuredClone(raw);overlap.entities[1].x=1;invalid.push(overlap);
 invalid.push({version:1,name:'triple overlay',entities:['track','tram','track'].map((kind,i)=>({id:`rail${i}`,kind,x:0,y:0,dir:0}))});
 const span=structuredClone(raw);span.entities.find((e:{id:string})=>e.id==='tunnel-out').x=20;invalid.push(span);
 const d=new FactoryDriver(createCampaign()),before=stateHash(d.state);for(const file of invalid)assert.throws(()=>buildBlueprint(d,file,0,0),/Invalid|overlap|paired/);
 assert.equal(d.log.length,0);assert.equal(stateHash(d.state),before);
 assert.throws(()=>factoryLine(d,parseBlueprint({version:1,name:'missing roles',entities:[{id:'belt',kind:'belt',x:0,y:0,dir:0}]})),/needs input/);assert.equal(d.log.length,0);
 for(const dir of [0,1,2,3]){const wide=dir%2===0;
  const file={version:1,name:'rectangle',entities:[{id:'split',kind:'splitter',x:0,y:0,dir},{id:'blocker',kind:'belt',x:wide?1:0,y:wide?0:1,dir:0}]};assert.throws(()=>parseBlueprint(file),/overlap/);
 }
});

test('a blueprint walks and pays from ordinary stock, keeps the engineer clear, and replays all construction/settings from a fresh campaign',()=>{
 const s=factoryScenario(3),line=factoryLine(s.d,parseBlueprint(raw));assert.equal(Object.keys(line.ids).length,10);assert.ok(s.d.log.some(l=>l.c.type==='move'));
 assert.ok(conservation(s.st).ok);assert.equal(s.st.engineer.down,-1);
 const replay=replayInterval(s.initial,s.d.log,s.st.flow!.tick);assert.equal(stateHash(replay),stateHash(s.st));
 const loaded=loadState(s.st);assert.equal(stateHash(loaded),stateHash(s.st));
 assert.equal(loaded.flow!.machines.find(m=>m.id===line.ids.splitter)?.priority,'left');
});

test('failed blueprint retains only paid completed work and reports the failing entity; invalid replay intervals are rejected',()=>{
 const st=createCampaign(),d=new FactoryDriver(st);d.take('steel',MACHINE_COST.belt.steel);const e=st.engineer,G=ground(st);let at:[number,number]|undefined;
 for(let y=Math.floor(e.y)-5;y<e.y+5&&!at;y++)for(let x=Math.floor(e.x)-5;x<e.x+5;x++)if(canPlace(st,'belt',x,y).ok&&canPlace(st,'belt',x+1,y).ok){at=[x,y];break;}
 assert.ok(at);const file={version:1,name:'short stock',entities:[{id:'first',kind:'belt',x:0,y:0,dir:1},{id:'second',kind:'belt',x:1,y:0,dir:1}]};
 assert.throws(()=>buildBlueprint(d,file,...at),/second; 1 placements retained/);assert.ok(st.flow!.occ[at[1]*G.tw+at[0]]!==undefined);assert.equal(st.flow!.occ[at[1]*G.tw+at[0]+1],undefined);assert.ok(conservation(st).ok);
 assert.throws(()=>replayInterval(st,[],st.flow!.tick-1),/interval/);
 assert.throws(()=>replayInterval(st,[{tick:5,c:{type:'move',x:1,y:1}},{tick:2,c:{type:'move',x:2,y:2}}],100),/interval/);
 const resumed=replayInterval(st,[{tick:st.flow!.tick,c:{type:'setSpeed',mult:0}}],st.flow!.tick+20);assert.equal(resumed.flow!.tick,st.flow!.tick+20);
});

test('the seeded copper margin is mined and stored by ordinary commands before a paid extraction layout is built',()=>{
 const r=logistics({seed:4,hours:.2,blueprint:parseBlueprint(raw),progress:()=>{}});
 assert.ok(r.log.some(l=>l.c.type==='factory'&&l.c.action.type==='mineAt'&&l.c.action.x>=0));
 assert.ok(r.checks.every(c=>c.pass),JSON.stringify(r.checks));assert.ok(conservation(r.finalState).ok);
});
