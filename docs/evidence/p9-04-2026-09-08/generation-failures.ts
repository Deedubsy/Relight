import assert from 'node:assert/strict';
import {readFileSync,writeFileSync,existsSync,mkdirSync} from 'node:fs';
import {factoryScenario} from '../../../packages/harness/src/factoryScenario';
import {generateCity} from '../../../packages/sim/src/index';
const out=new URL('./generation-failures/',import.meta.url);mkdirSync(out,{recursive:true});
const analysis=JSON.parse(readFileSync(new URL('./analysis.json',import.meta.url),'utf8'));
for(const [key,value] of Object.entries(analysis.failureClasses) as [string,{representative:{seed:number;reason:string}}][]){
 if(!key.startsWith('generation-or-run:'))continue;
 const {seed,reason}=value.representative,url=new URL(`seed-${seed}.json`,out);if(existsSync(url))continue;
 const attempts=[];for(let i=0;i<2;i++){let failure='',stack='';try{factoryScenario(seed,false);}catch(e){failure=(e as Error).message;stack=(e as Error).stack??'';}
 assert.equal(failure,reason);attempts.push({failure,stack});}
 let generatorAttempt:number|null=null,baseFailure:string|null=null;try{generatorAttempt=generateCity(seed,'river').attempt;}catch(e){baseFailure=(e as Error).message;}
 const record={seed,key,attempts,generatorAttempt,baseFailure,ordinaryCommandsExecuted:0,outcome:'Fresh campaign initialization fails before the ordinary command driver can start. No usable save, bypass or replacement seed was supplied.',human_play:'not_run'};
 writeFileSync(url,JSON.stringify(record,null,2)+'\n',{flag:'wx'});console.log(`Reproduced seed ${seed} twice: ${reason}`);
}
