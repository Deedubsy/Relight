import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createCampaign,loadState,makeSave,stateHash,validateCampaignCity,initExpansion,EXPANSION_SURVEY_VERSION} from '../src/index';
import {loadSnapshot,createSession,parseUrl,makeSessionSave,replaySession} from '../../game/src/session';

for(const seed of [6,29])test(`current survey: seed ${seed} composes legal tracks/stops and preserves an older paid save`,()=>{
 const fresh=createCampaign(seed),report=validateCampaignCity(fresh);
 assert.equal(fresh.campaign!.expansion!.surveyVersion,EXPANSION_SURVEY_VERSION);assert.ok(report.ok,JSON.stringify(report.failures));
 assert.deepEqual(validateCampaignCity(loadState(makeSave(fresh))),report);
 const old=JSON.parse(readFileSync(new URL(`../../../docs/evidence/p9-02-2026-09-08/old-seed-${seed}.json`,import.meta.url),'utf8'));
 const loaded=loadState(old),hash=stateHash(loaded);assert.equal(hash,old.hash);
 assert.deepEqual(loaded.campaign!.expansion,old.state.campaign.expansion);assert.deepEqual(loaded.campaign!.districts,old.state.campaign.districts);
 assert.deepEqual(loaded.engineer.inv,old.state.engineer.inv);assert.deepEqual(loaded.flow!.machines,old.state.flow.machines);
 initExpansion(loaded);assert.equal(stateHash(loaded),hash,'existing surveys must never regenerate');
 assert.notDeepEqual(fresh.campaign!.expansion!.route,loaded.campaign!.expansion!.route);
});

test('old survey logs remain available but cannot claim new-factory replay; new survey saves retain complete logs',async()=>{
 const old=readFileSync(new URL('../../../docs/evidence/p9-02-2026-09-08/old-seed-6.json',import.meta.url),'utf8'),raw=JSON.parse(old);
 const descriptor=Object.getOwnPropertyDescriptor(globalThis,'localStorage');let contents=old;
 Object.defineProperty(globalThis,'localStorage',{configurable:true,value:{getItem:()=>contents}});
 try {
  const loaded=await loadSnapshot('local:survey');assert.equal(loaded.logComplete,false);assert.deepEqual(loaded.log,raw.log);assert.equal(stateHash(loaded.state),raw.hash);
  contents=JSON.stringify(makeSave(createCampaign(6),{log:[],logComplete:true}));const current=await loadSnapshot('local:survey');assert.equal(current.logComplete,true);assert.equal(current.state.campaign!.expansion!.surveyVersion,EXPANSION_SURVEY_VERSION);
 }finally{if(descriptor)Object.defineProperty(globalThis,'localStorage',descriptor);else Reflect.deleteProperty(globalThis,'localStorage');}
});

test('unknown survey revision refuses load without treating the saved route as a new survey',()=>{
 const raw=JSON.parse(JSON.stringify(makeSave(createCampaign(6))));raw.state.campaign.expansion.surveyVersion=999;
 assert.throws(()=>loadState(raw),/survey revision/);
});

for(const seed of [80,88,102,305,842,1340,2313,3610,7384,8102,9711])test(`population repair: seed ${seed} has complete legal reservations`,()=>{
 const st=createCampaign(seed),before=stateHash(st),report=validateCampaignCity(st);
 assert.ok(report.ok,JSON.stringify(report.failures));assert.equal(stateHash(st),before);
 assert.equal(stateHash(createCampaign(seed)),before,'fresh factory must repeat exactly');
 assert.equal(stateHash(loadState(makeSave(st))),before,'saved layout must remain exact');
 assert.equal(st.events.filter(e=>e.type==='stalker').length,1,'rejected candidates must not emit ghost spawns');
});

test('revision 2 paid saves retain geometry, stock and logs under revision 3',async()=>{
 const text=readFileSync(new URL('../../../docs/evidence/p9-04-2026-09-08/paid/save-3.json',import.meta.url),'utf8'),raw=JSON.parse(text);
 const st=loadState(raw);assert.equal(stateHash(st),raw.hash);assert.deepEqual(st.campaign!.expansion,raw.state.campaign.expansion);
 initExpansion(st);assert.equal(stateHash(st),raw.hash);
 const descriptor=Object.getOwnPropertyDescriptor(globalThis,'localStorage');
 Object.defineProperty(globalThis,'localStorage',{configurable:true,value:{getItem:()=>text}});
 try { const loaded=await loadSnapshot('local:old-survey');assert.equal(loaded.logComplete,false);assert.deepEqual(loaded.log,raw.log);assert.equal(stateHash(loaded.state),raw.hash); }
 finally{if(descriptor)Object.defineProperty(globalThis,'localStorage',descriptor);else Reflect.deleteProperty(globalThis,'localStorage');}
});

test('resaving a prior survey preserves its complete historical log without promising fresh-factory replay',async()=>{
 const text=readFileSync(new URL('../../../docs/evidence/p9-04-2026-09-08/paid/save-3.json',import.meta.url),'utf8'),raw=JSON.parse(text);
 const storage=Object.getOwnPropertyDescriptor(globalThis,'localStorage'),location=Object.getOwnPropertyDescriptor(globalThis,'location');
 Object.defineProperty(globalThis,'localStorage',{configurable:true,value:{getItem:()=>text}});
 Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});
 try {
  const loaded=await loadSnapshot('local:previous-survey'),session=createSession(parseUrl('?rules=exploration-v2'),loaded);
  assert.equal(stateHash(session.state),raw.hash);assert.equal(session.logComplete,false);assert.deepEqual(session.log,raw.log);
  assert.ok('error' in replaySession(session,{rifleOff:false}));
  const save=makeSessionSave(session);assert.deepEqual(save.log,raw.log);assert.equal(save.logComplete,false);assert.equal(save.hash,raw.hash);
  session.log.push({tick:session.state.flow!.tick,c:{type:'setSpeed',mult:0}});
  assert.deepEqual(loaded.log,raw.log,'session continuation must not mutate the loaded historical log');
 }finally{
  if(storage)Object.defineProperty(globalThis,'localStorage',storage);else Reflect.deleteProperty(globalThis,'localStorage');
  if(location)Object.defineProperty(globalThis,'location',location);else Reflect.deleteProperty(globalThis,'location');
 }
});
