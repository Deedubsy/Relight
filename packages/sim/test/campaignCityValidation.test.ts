import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createCampaign,validateCampaignCity,placementGeometryProblem,placeable,ground,makeSave,loadState,ensureFlow,applyCommands} from '../src/index';
import {citySeeds,cityValidationRun} from '../../harness/src/cityValidationRun';
import {readFileSync} from 'node:fs';

test('composed city query is repeatable, read-only and stable after load; includes every current site',()=>{
 const st=createCampaign(3),before=structuredClone(st),a=validateCampaignCity(st),loaded=loadState(makeSave(st));ensureFlow(loaded);
 assert.deepEqual(st,before);assert.deepEqual(validateCampaignCity(st),a);assert.deepEqual(validateCampaignCity(loaded),a);
 assert.equal(a.measurements.sites.length,14);assert.equal(a.measurements.origins.length,3);assert.equal(a.measurements.truck.status,'not-unlocked');
 assert.ok(a.measurements.reachableTiles>0&&a.measurements.factory);assert.equal(a.failures.some(f=>f.code==='input'),false);
});

test('permanent rail remains geometrically valid and rejects player construction without spending stock',()=>{
 const st=createCampaign(3),G=ground(st),t=st.campaign!.expansion!.route[0];ensureFlow(st);const before=structuredClone(st);
 assert.match(placeable(st,'track',t%G.tw,Math.floor(t/G.tw)),/Permanent/);
 assert.equal(placementGeometryProblem(st,'track',t%G.tw,Math.floor(t/G.tw),0,st.flow!.occ[t]),'');
 validateCampaignCity(st);assert.deepEqual(st,before);
 applyCommands(st,[{type:'construct',edits:[{action:'place',item:'track',x:t%G.tw,y:Math.floor(t/G.tw),dir:0}]}]);
 assert.deepEqual(st.flow!.machines,before.flow!.machines);assert.deepEqual(st.engineer.inv,before.engineer.inv);
});

test('bad reservation rectangles, overlapping sites, source identity and rail topology are independently reported',()=>{
 const st=createCampaign(3),c=st.campaign!;c.recruits!.sites[0].x=-1;
 assert.ok(validateCampaignCity(st).failures.some(f=>f.code==='site-footprint'));
 const next=createCampaign(3),d=next.campaign!;d.recruits!.sites[0].x=d.recruits!.sites[1].x;d.recruits!.sites[0].y=d.recruits!.sites[1].y;
 d.districts!.sources[1].item='steel';d.districts!.route[1]=d.districts!.route[0];
 const result=validateCampaignCity(next);for(const code of ['site-overlap','source-regions','rail-route'])assert.ok(result.failures.some(f=>f.code===code),code);
});

test('blocked interactions are found from the final shared collision mask, not just the site centre',()=>{
 const st=createCampaign(3),r=st.campaign!.recruits!.sites[0];
 // Deliberately malformed physical fixture, not an ordinary construction or travel scenario.
 for(let y=r.y-9;y<=r.y+9;y++)for(let x=r.x-9;x<=r.x+9;x++)st.flow!.machines.push({id:100000+st.flow!.machines.length,kind:'chest',x,y,dir:0,size:2,items:[],hold:null,timer:0,phase:0,inv:{},out:0,busy:false});
 st.flow!.rev++;
 assert.ok(validateCampaignCity(st).failures.some(f=>f.code==='site-access'&&f.subject==='recruit:'+r.kind));
});

test('runner retains failed generation and seed identity; invalid or duplicate seed requests refuse',()=>{
 assert.deepEqual(citySeeds('3-5,8,11,13'),[3,4,5,8,11,13]);
 for(const s of ['','5-3','3,3','1-10001','1.5','-1'])assert.throws(()=>citySeeds(s));
 const row=cityValidationRun(123,()=>{throw Error('fixture generation failure');});assert.equal(row.seed,123);assert.equal(row.status,'failed');assert.match(row.failure!.reason,/fixture generation failure/);
});

test('existing paid truck checkpoint reports physical queries without commissioning a fresh truck',()=>{
 const st=loadState(JSON.parse(readFileSync(new URL('../../../docs/evidence/p8-03-2026-09-08/truck-ready.json',import.meta.url),'utf8'))),before=structuredClone(st);
 const r=validateCampaignCity(st);assert.equal(r.measurements.truck.status,'queried');assert.equal(r.measurements.truck.queries.length,2);assert.deepEqual(st,before);
});

test('malformed maps and missing metadata return bounded failures; physical route blockers remain findings',()=>{
 const st=createCampaign(3);st.city!.tw=NaN;assert.equal(validateCampaignCity(st).failures[0].code,'input');
 const other=createCampaign(3);delete other.campaign!.turbine;assert.equal(validateCampaignCity(other).failures[0].code,'input');
 const blocked=createCampaign(3),G=ground(blocked),t=blocked.campaign!.expansion!.route[3];
 blocked.campaign!.recruits!.sites[0].x=t%G.tw;blocked.campaign!.recruits!.sites[0].y=Math.floor(t/G.tw);
 const r=validateCampaignCity(blocked);assert.ok(r.failures.some(f=>f.code==='rail-placement'&&/shelter/.test(f.reason)));
});
