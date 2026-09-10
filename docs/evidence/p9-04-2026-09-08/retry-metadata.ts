import {readFileSync,writeFileSync,mkdirSync,existsSync} from 'node:fs';
import {generateCity,CITY_ATTEMPTS} from '../../../packages/sim/src/index';
const population=new URL('./population/',import.meta.url),out=new URL('./retry-metadata/',import.meta.url);mkdirSync(out,{recursive:true});
const analysis=JSON.parse(readFileSync(new URL('./analysis.json',import.meta.url),'utf8'));
for(const seed of analysis.retryIndexNeedsGenerationDiagnostic as number[]){
 const file=`seed-${seed}.json`;
 const r=JSON.parse(readFileSync(new URL(file,population),'utf8')).data;if(r.report||existsSync(new URL(file,out)))continue;
 let generatorAttempt:number|null=null,reason:string|null=null;const start=performance.now();
 try{generatorAttempt=generateCity(r.seed,'river').attempt;}catch(e){reason=(e as Error).message;}
 const record={seed:r.seed,sourceDigest:r.sourceDigest,config_hash:r.config_hash,generatorAttempt,baseAttempts:generatorAttempt===null?CITY_ATTEMPTS:generatorAttempt+1,baseFailure:reason,wallMs:performance.now()-start,method:'Independent deterministic base-city generation replay for a campaign that threw during initialization. The original failed population row remains intact; no playable campaign or ordinary commands are inferred.'};
 writeFileSync(new URL(file,out),JSON.stringify(record,null,2)+'\n',{flag:'wx'});console.log(JSON.stringify(record));
}
