/** Append-only population evidence. A resume must agree with the full declared population and source identity. */
import {createHash,randomUUID} from 'node:crypto';
import {readFileSync,writeFileSync,renameSync,existsSync,readdirSync} from 'node:fs';
import {join} from 'node:path';
import type {cityValidationRun} from './cityValidationRun';
export type PopulationRow=ReturnType<typeof cityValidationRun>&{processSample:number;pid:number};
export interface PopulationIdentity {sourceDigest:string;config_hash:string;seeds:number[];runtime:string;version:number}
const digest=(value:unknown)=>createHash('sha256').update(JSON.stringify(value)).digest('hex');
export function checkPopulationIdentity(actual:PopulationIdentity,expected:PopulationIdentity):void {
 for(const k of ['sourceDigest','config_hash','seeds','runtime','version'] as const)if(JSON.stringify(actual[k])!==JSON.stringify(expected[k]))throw Error(`resume identity differs: ${k}`);
}
export function writePopulationRow(out:string,identity:PopulationIdentity,row:PopulationRow):void {
 const name=join(out,`seed-${row.seed}.json`);if(existsSync(name))throw Error(`seed already recorded: ${row.seed}`);
 const data={sourceDigest:identity.sourceDigest,config_hash:identity.config_hash,...row},pending=name+'.'+randomUUID()+'.pending';
 writeFileSync(pending,JSON.stringify({digest:digest(data),data},null,2)+'\n',{flag:'wx'});renameSync(pending,name);
}
export function readPopulationRows(out:string,identity:PopulationIdentity):Map<number,PopulationRow>{
 const rows=new Map<number,PopulationRow>(),seeds=new Set(identity.seeds);
 for(const name of readdirSync(out).filter(n=>/^seed-.*\.json$/.test(n))){
  const envelope=JSON.parse(readFileSync(join(out,name),'utf8')),r=envelope.data;
  if(!r||envelope.digest!==digest(r)||r.sourceDigest!==identity.sourceDigest||r.config_hash!==identity.config_hash||!seeds.has(r.seed)||name!==`seed-${r.seed}.json`||rows.has(r.seed))throw Error(`invalid retained seed: ${name}`);
  if(!['passed','failed'].includes(r.status)||!Number.isFinite(r.wallMs)||r.wallMs<0||!Number.isInteger(r.processSample)||r.processSample<0||!r.timings)throw Error(`invalid result: ${name}`);
  if(r.status==='passed'&&(!r.report?.ok||r.report.failures.length||r.report.seed!==r.seed||!r.unchanged||!r.deterministic||!r.loadSame))throw Error(`invalid pass: ${name}`);
  if(r.status==='failed'&&!r.failure&&!r.report)throw Error(`missing failure: ${name}`);
  rows.set(r.seed,r);
 }return rows;
}
