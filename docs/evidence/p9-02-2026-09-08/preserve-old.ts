import {readFileSync,writeFileSync} from 'node:fs';
import assert from 'node:assert/strict';
import {createCampaign,makeSave,stateHash} from '../../../packages/sim/src/index';
import {replayInterval} from '../../../packages/harness/src/blueprint';
for(const seed of [6,29]){const r=JSON.parse(readFileSync(`docs/evidence/p9-01-2026-09-08/reproduction-${seed}.json`,'utf8')),st=replayInterval(createCampaign(seed),r.log,r.seconds*20);assert.equal(stateHash(st),r.hash);writeFileSync(`docs/evidence/p9-02-2026-09-08/old-seed-${seed}.json`,JSON.stringify(makeSave(st,{log:r.log,logComplete:true}),null,1)+'\n',{flag:'wx'});console.log(seed,stateHash(st),'preserved paid pre-fix checkpoint');}
