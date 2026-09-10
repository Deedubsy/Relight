import {test} from 'node:test';
import assert from 'node:assert/strict';
import {mkdtempSync,readFileSync,writeFileSync,rmSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {checkPopulationIdentity,readPopulationRows,writePopulationRow} from '../../harness/src/cityPopulation';
import {cityValidationRun} from '../../harness/src/cityValidationRun';
const identity={sourceDigest:'source-a',config_hash:'config-a',seeds:[3,4],runtime:'test-runtime',version:2};

test('resumption retains a failed attempted seed byte-for-byte and ignores incomplete temporary writes',t=>{
 const out=mkdtempSync(join(tmpdir(),'relight-population-'));t.after(()=>rmSync(out,{recursive:true}));
 const row={...cityValidationRun(3,()=>{throw Error('retained generation failure');}),processSample:0,pid:1};
 writePopulationRow(out,identity,row);const before=readFileSync(join(out,'seed-3.json'));
 writeFileSync(join(out,'seed-4.json.interrupted.pending'),'partial');
 const rows=readPopulationRows(out,identity);assert.equal(rows.size,1);assert.equal(rows.get(3)!.status,'failed');
 assert.deepEqual(identity.seeds.filter(s=>!rows.has(s)),[4]);assert.throws(()=>writePopulationRow(out,identity,row),/already recorded/);
 assert.deepEqual(readFileSync(join(out,'seed-3.json')),before);
});
test('resumption refuses source, population, runtime and version changes and corrupted retained results',t=>{
 for(const altered of [{sourceDigest:'source-b'},{config_hash:'config-b'},{seeds:[4,3]},{runtime:'other'},{version:3}])assert.throws(()=>checkPopulationIdentity({...identity,...altered},identity),/identity differs/);
 const out=mkdtempSync(join(tmpdir(),'relight-population-'));t.after(()=>rmSync(out,{recursive:true}));
 const row={...cityValidationRun(3,()=>{throw Error('fixture');}),processSample:0,pid:1};writePopulationRow(out,identity,row);
 assert.throws(()=>readPopulationRows(out,{...identity,seeds:[4]}),/invalid retained seed/);
 const path=join(out,'seed-3.json'),data=JSON.parse(readFileSync(path,'utf8'));data.data.status='passed';writeFileSync(path,JSON.stringify(data));
 assert.throws(()=>readPopulationRows(out,identity),/invalid retained seed/);
});
