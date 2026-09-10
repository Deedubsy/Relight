import {suppliedCampaign as createCampaign} from './suppliedCampaignFixture';
// Prepared historical starting stock; current ungranted progression is checked in gameplayCorrections.test.ts.
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {ensureFlow,knownEquipment,buildStock,buildAffordability,MACHINE_COST,stateHash,makeSave,loadState} from '../src/index';
import {parseUiPreferences,DEFAULT_QUICKBAR,assignQuickbar,QUICKBAR_KEYS} from '../../game/src/uiPreferences';
import {visibleBuildCatalogue,toolForKey,buildCategory} from '../../game/src/buildCatalogue';
const fresh=()=>{const st=createCampaign(3);ensureFlow(st);return st;};
test('UI-03 preferences default safely for absent, invalid, future and malformed records',()=>{
 for(const raw of [null,'bad','{}','null','{"version":2,"quickbar":[]}',JSON.stringify({version:1,quickbar:[...DEFAULT_QUICKBAR,'rifle']})])assert.deepEqual(parseUiPreferences(raw).quickbar,DEFAULT_QUICKBAR);
 const slots=[...DEFAULT_QUICKBAR];slots[0]=null;assert.deepEqual(parseUiPreferences(JSON.stringify({version:1,quickbar:slots})).quickbar,slots);
});
test('UI-03 assignment swaps or clears without moving other slots; digit dispatch follows the assigned slot',()=>{
 const swapped=assignQuickbar(DEFAULT_QUICKBAR,0,'excavator');assert.equal(swapped[0],'excavator');assert.equal(swapped[2],'belt');assert.equal(toolForKey('1',swapped),'excavator');assert.equal(toolForKey('3',swapped),'belt');
 const cleared=assignQuickbar(swapped,0,null);assert.equal(toolForKey('1',cleared),null);assert.equal(cleared.length,10);assert.deepEqual(DEFAULT_QUICKBAR,['belt','inserter','excavator',null,null,null,null,null,null,null]);
 for(let i=0;i<10;i++)assert.equal(toolForKey(QUICKBAR_KEYS[i]),DEFAULT_QUICKBAR[i]);assert.equal(toolForKey('j',swapped),'splitter');assert.equal(toolForKey('u',swapped),'underground');
});
test('UI-03 unknown equipment is omitted; discovery exposes a locked plan, recruitment unlocks it',()=>{
 const st=fresh(),source=st.campaign!.recruits!.sites.find(s=>s.kind==='lamplighters')!;assert.equal(source.seenAt,-1);assert.equal(knownEquipment(st,'arclamp'),false);assert.ok(!visibleBuildCatalogue(st).some(b=>b.kind==='arclamp'));
 source.seenAt=st.t;assert.equal(knownEquipment(st,'arclamp'),true);source.recruitedAt=st.t;assert.equal(buildCategory('arclamp'),'Power');assert.ok(visibleBuildCatalogue(st).some(b=>b.kind==='arclamp'));
});
test('UI-03 reserved assignments survive a less explored save without exposing or reordering their content',()=>{
 const st=fresh(),slots=assignQuickbar(DEFAULT_QUICKBAR,0,'arclamp'),loaded=parseUiPreferences(JSON.stringify({version:1,quickbar:slots}));assert.equal(loaded.quickbar[0],'arclamp');assert.equal(knownEquipment(st,'arclamp'),false);assert.deepEqual(loaded.quickbar,slots);assert.deepEqual(visibleBuildCatalogue(loadState(makeSave(st))).map(x=>x.kind),visibleBuildCatalogue(st).map(x=>x.kind));
});
test('UI-03 stock facts distinguish zero, partial materials and packed-first payment',()=>{
 assert.equal(buildStock('excavator',{}).carried,0);assert.deepEqual(buildStock('excavator',{steel:4}).shortage,[{item:'steel',count:MACHINE_COST.excavator.steel-4}]);
 const material=buildStock('excavator',{steel:10});assert.equal(material.affordability.ok,true);assert.equal(material.affordability.carried,false);
 const packed=buildStock('excavator',{excavator:1});assert.equal(packed.carried,1);assert.equal(packed.affordability.carried,true);assert.deepEqual(packed.affordability,buildAffordability('excavator',{excavator:1}));
});
test('UI-03 catalogue, knowledge, affordability and shortcut edits never mutate gameplay stock or save hashes',()=>{
 const st=fresh(),before=stateHash(st),inventory={...st.engineer.inv};visibleBuildCatalogue(st);for(const k of ['excavator','mixer','barricade','generator'] as const)buildStock(k,st.engineer.inv);assignQuickbar(DEFAULT_QUICKBAR,3,'wall');assert.deepEqual(st.engineer.inv,inventory);assert.equal(stateHash(st),before);
});
