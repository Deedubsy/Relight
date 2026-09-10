import {writeFileSync} from 'node:fs';
import {createCampaign,ensureFlow,makeSave,stateHash} from '../../../packages/sim/src/index';
const records=[];
for(const seed of [102,305,842]){const st=createCampaign(seed);ensureFlow(st);writeFileSync(new URL(`fresh-${seed}.json`,import.meta.url),JSON.stringify(makeSave(st,{log:[],logComplete:true})));records.push({seed,hash:stateHash(st),survey:st.campaign!.expansion!.surveyVersion});}
writeFileSync(new URL('prepared.json',import.meta.url),JSON.stringify(records,null,2)+'\n');
