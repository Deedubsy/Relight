import {readFileSync} from 'node:fs';
import assert from 'node:assert/strict';
import {createCampaign,ground,DARK,HELD,loadState,stateHash} from '../../../packages/sim/src/index';
import {savedContinuation} from '../../../packages/harness/src/blueprint';
let tiles=0;
for(let seed=1;seed<=32;seed++){const s=createCampaign(seed),G=ground(s);for(const t of s.campaign!.districts!.route){assert.equal(G.owner[t],-1);assert.ok([DARK,HELD].includes(s.blocks[G.near[t]].state),`seed ${seed} tile ${t}`);assert.equal(G.urban!.solid[t],0);tiles++;}}
const old=JSON.parse(readFileSync('docs/evidence/p6-03-2026-09-07/campaign/E-defence-turrets-seed8.json','utf8')).finalState;
const loaded=loadState(old);loaded.speed=old.speed;assert.equal(stateHash(loaded),stateHash(old));assert.ok(savedContinuation(old).same);
console.log(`PASS: ${tiles} extension tiles across seeds 1–32 have buildable street ownership; prior seed-8 save retains its original survey and identical continuation.`);
console.log('Earlier saves keep their existing survey/layout; this fixes newly generated surveys, not an automatic existing-save terrain or route rewrite.');
