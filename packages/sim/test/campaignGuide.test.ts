import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createCampaign,campaignDiscoveries,itemGuide,ITEMS,tickKnowledge,knownSite,loadState,makeSave,stateHash,applyCommands,advanceFlow,inReach,conservation,type SimState,type Command} from '../src/index';
import {createSession,parseUrl,replaySession,loadSnapshot} from '../../game/src/session';
const run=(s:SimState,n:number)=>advanceFlow(s,n,[],Math.ceil(n*20)+1);

test('knowledge starts with the opening clue, records exploration and survives leaving and reloading',()=>{
 const s=createCampaign();assert.deepEqual(campaignDiscoveries(s).filter(r=>!r.id.startsWith('regional:')).map(r=>r.id),['station']);assert.equal(campaignDiscoveries(s).filter(r=>r.title==='Power core search area').length,3);assert.equal(knownSite(s,'workshop'),false);
 const text=JSON.stringify(campaignDiscoveries(s));assert.ok(!text.includes('Turbine')&&!text.includes('Surveyors')&&!text.includes('Stalker'));
 const w=s.campaign!.districts!.workshop; // Labelled position fixture isolates the exploration threshold.
 s.engineer.x=w.x+.5;s.engineer.y=w.y+.5;tickKnowledge(s);assert.ok(knownSite(s,'workshop'));
 s.engineer.x=0;s.engineer.y=0;tickKnowledge(s);assert.ok(knownSite(s,'workshop'));assert.equal(stateHash(loadState(makeSave(s))),stateHash(s));
 for(const bad of [(x:SimState)=>{delete x.campaign!.knowledge;},(x:SimState)=>{x.campaign!.knowledge!.sites.radio=-1;},(x:SimState)=>{x.campaign!.knowledge!.sites.radio=x.t+1;},(x:SimState)=>{(x.campaign!.knowledge!.sites as Record<string,number>).unknown=0;}]){const copy=structuredClone(s);bad(copy);assert.throws(()=>loadState(copy),/knowledge/);}
});

test('item guide contains only implemented recipes and true current consumers, unlock provenance and no secret locations',()=>{
 const s=createCampaign(),before=stateHash(s);for(const item of ITEMS){const g=itemGuide(s,item)!;assert.equal(g.item,item);assert.ok(g.uses.length);assert.ok(!JSON.stringify(g).includes('Starting house supplies'));}
 assert.equal(itemGuide(s,'magazine')!.recipes[0].count,1);assert.equal(itemGuide(s,'magazine')!.recipes[0].seconds,6);
 assert.equal(itemGuide(s,'concrete')!.recipes[0].available,false);assert.match(itemGuide(s,'concrete')!.recipes[0].provenance,/Concrete crew/);
 assert.match(itemGuide(s,'board')!.uses.join(','),/No implemented consumer/);assert.match(itemGuide(s,'frame')!.uses.join(','),/No implemented consumer/);
 assert.ok(itemGuide(s,'wire')!.uses.some(x=>x.startsWith('Board: 3')));assert.equal(stateHash(s),before);
 const r=s.campaign!.recruits!.sites.find(r=>r.kind==='concrete')!;s.engineer.x=r.x+.5;s.engineer.y=r.y+.5;applyCommands(s,[{type:'recruitSurvivors',id:r.id}]);
 assert.ok(itemGuide(s,'concrete')!.recipes[0].available);assert.match(campaignDiscoveries(s).find(x=>x.id===r.id)!.status,/Recruited/);
});

test('discovery service state follows actual power, switch, partial materials and empty or earned rewards',()=>{
 // Real prior paid checkpoint; load upgrades knowledge without changing its earned factory/recruit history.
 const old=JSON.parse(readFileSync(new URL('../../../docs/evidence/p7-03-2026-09-08/turbine-complete.json',import.meta.url),'utf8')),s=loadState(old),t=s.campaign!.turbine!;
 assert.equal(s.campaign!.version,10);for(const m of old.state.flow.machines)assert.deepEqual(s.flow!.machines.find(n=>n.id===m.id),m);assert.deepEqual(s.campaign!.recruits!.sites.slice(0,4),old.state.campaign.recruits!.sites);assert.ok(conservation(s).ok);
 assert.equal(Object.keys(s.campaign!.knowledge!.sites).length,4,'old panels already exposed all installation locations');
 let row=campaignDiscoveries(s).find(r=>r.id===t.id)!;assert.match(row.status,/generating/);assert.ok(row.detail.includes('12/600'));const before=stateHash(s);campaignDiscoveries(s);assert.equal(stateHash(s),before);
 s.engineer.x=t.x-.5;s.engineer.y=t.y+.5;applyCommands(s,[{type:'setTurbineEnabled',enabled:false}]);row=campaignDiscoveries(s).find(r=>r.id===t.id)!;assert.match(row.status,/switched off/);
 const station=campaignDiscoveries(s).find(r=>r.id==='station')!;assert.equal(station.needs[0].required,30);assert.equal(station.needs[0].delivered,0);
 const fresh=createCampaign();fresh.campaign!.expansion!.station.delivered.steel=12;assert.equal(campaignDiscoveries(fresh)[0].needs[0].delivered,12);
});

test('ordinary exploration and journal actions replay; read-only browsing does not change simulation state',()=>{
 Object.defineProperty(globalThis,'location',{configurable:true,value:{href:'http://localhost/'}});const session=createSession(parseUrl('?rules=exploration-v2&seed=3&view=world'));Reflect.deleteProperty(globalThis,'location');const s=session.state;
 const send=(c:Command)=>{session.log.push({tick:s.flow!.tick,c:structuredClone(c)});applyCommands(s,[c]);};
 const r=s.campaign!.recruits!.sites.find(r=>r.kind==='concrete')!;send({type:'move',x:r.x+.5,y:r.y+.5});for(let i=0;i<160&&s.engineer.target;i++)run(s,1);assert.ok(inReach(s,r.x,r.y));
 let row=campaignDiscoveries(s).find(x=>x.id===r.id)!;assert.ok(row);assert.equal(row.actions[0].reason,'');for(const c of row.actions[0].commands)send(c);
 row=campaignDiscoveries(s).find(x=>x.id===r.id)!;assert.match(row.status,/Recruited/);const hash=stateHash(s);
 for(let i=0;i<5;i++){campaignDiscoveries(s);for(const item of ITEMS)itemGuide(s,item);}assert.equal(stateHash(s),hash);
 const replay=replaySession(session);assert.ok('state' in replay,JSON.stringify(replay));if('state' in replay)assert.equal(stateHash(s),stateHash(replay.state));assert.equal(stateHash(loadState(makeSave(s))),stateHash(s));
});

test('historical P7-03 checkpoint preserves its turbine and log without falsely replaying a supplied opening as the empty campaign',async()=>{
 const old=JSON.parse(readFileSync(new URL('../../../docs/evidence/p7-03-2026-09-08/turbine-complete.json',import.meta.url),'utf8')),prior=globalThis.fetch;
 try{globalThis.fetch=async()=>new Response(JSON.stringify(old));const loaded=await loadSnapshot('/p703.json');assert.equal(loaded.state.campaign!.turbine!.restoredAt,old.state.campaign.turbine.restoredAt);assert.deepEqual(loaded.log,old.log);assert.equal(loaded.logComplete,false);assert.ok(conservation(loaded.state).ok);assert.equal(stateHash(loadState(makeSave(loaded.state))),stateHash(loaded.state));}finally{globalThis.fetch=prior;}
});
