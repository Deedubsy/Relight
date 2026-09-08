import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createCampaign,applyCommands,loadState,makeSave,stateHash,machineAt,conservation,campaignGrid,TURBINE} from '../../../packages/sim/src/index';
// A labelled location fixture models a version-7 player who already built on the future hall pad.
const s=createCampaign(),original={...s.campaign!.turbine!};applyCommands(s,[{type:'chestTake',item:'steel',n:2},{type:'chestTake',item:'copper',n:2}]);
s.campaign!.version=7;s.campaign!.recruits!.version=2;s.campaign!.recruits!.sites=s.campaign!.recruits!.sites.slice(0,2);delete s.campaign!.turbine;s.flow!.rev++;
s.engineer.x=original.x-.5;s.engineer.y=original.y+.5;applyCommands(s,[{type:'place',item:'pole',x:original.x,y:original.y}]);assert.ok(machineAt(s,original.x,original.y));
const migrated=loadState(s);assert.deepEqual(migrated.flow!.machines,s.flow!.machines);assert.notDeepEqual([migrated.campaign!.turbine!.x,migrated.campaign!.turbine!.y],[original.x,original.y]);assert.ok(conservation(migrated).ok);assert.equal(stateHash(loadState(makeSave(migrated))),stateHash(migrated));
console.log('PASS: occupied old-save site relocates the new hall once without moving/deleting/refunding machines.');
const prior=JSON.parse(readFileSync('docs/evidence/p7-02-2026-09-07/concrete-line.json','utf8')),old=loadState(prior);assert.deepEqual(old.flow!.machines,prior.state.flow.machines);assert.deepEqual(old.campaign!.recruits!.sites.slice(0,2),prior.state.campaign.recruits.sites);assert.ok(conservation(old).ok);console.log('PASS: actual P7-02 production save upgrades with original factory, recruits and conserved concrete.');
const powered=loadState(JSON.parse(readFileSync('docs/evidence/p7-03-2026-09-08/turbine-complete.json','utf8'))),hall=powered.campaign!.turbine!;
// Explicit disabled-core fixture: registration here belongs only to the test, never Turbine restoration.
const core={...powered.campaign!.defence!.bases[0],block:hall.block,x:hall.x,y:hall.y,hp:0};powered.campaign!.defence!.bases.push(core);
assert.equal(campaignGrid(powered).blocks[hall.block].turbineSupply,0);assert.equal(campaignGrid(powered).turbineOutput,0);core.hp=300;assert.equal(campaignGrid(powered).blocks[hall.block].turbineSupply,TURBINE.kw);console.log('PASS: a disabled circuit core suppresses hall supply and recovery restores it.');
