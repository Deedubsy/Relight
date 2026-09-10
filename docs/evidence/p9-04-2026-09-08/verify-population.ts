import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {readPopulationRows} from '../../../packages/harness/src/cityPopulation';
const out='docs/evidence/p9-04-2026-09-08/population',m=JSON.parse(readFileSync(out+'/manifest.json','utf8')),rows=readPopulationRows(out,m),summary=JSON.parse(readFileSync(out+'/summary.json','utf8'));
assert.deepEqual(m.seeds,Array.from({length:10000},(_,i)=>i+1));assert.equal(rows.size,10000);assert.ok(summary.complete&&summary.sourceUnchanged);assert.equal(summary.attempted,10000);
assert.equal(summary.failed,[...rows.values()].filter(r=>r.status==='failed').length);
console.log(`PASS checksums and identity for all ${rows.size} attempted seeds; ${summary.failed} retained failures, ${summary.passed} passes. This verifies evidence integrity, not a passing city population.`);
