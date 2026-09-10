import * as S from '../../../packages/sim/src/index';
import {validateRiverfront} from '../../../packages/sim/src/city/validateRiverfront';
import {writeFileSync,readFileSync} from 'node:fs';
import assert from 'node:assert/strict';
const s=S.createCampaign();s.speed=0;const{reachable,...v}=validateRiverfront(s);assert.deepEqual(v.errors,[]);assert.ok(reachable.length);
const before=readFileSync('docs/evidence/founders-court/before-riverfront.ts','utf8').replace(/\r\n/g,'\n'),parse=(start:string,end:string)=>JSON.parse(before.split(start)[1].split(end)[0]);
const oldCity=parse('export const RIVERFRONT:Definition=',';\nexport const RIVERFRONT_BUILDINGS'),oldBuildings=parse('export const RIVERFRONT_BUILDINGS:Parcel[]=',';\nexport const RIVERFRONT_PROPS');
for(const key of ['width','height','roads','roadEdges','yards','tram','stops','resources','projects','drives'])assert.deepEqual(S.RIVERFRONT[key as keyof typeof S.RIVERFRONT],oldCity[key],key);
for(const b of oldBuildings){const now=S.RIVERFRONT_BUILDINGS.find(q=>q.id===b.id);assert.deepEqual(now,b,b.id+' retained unchanged');}
writeFileSync('docs/evidence/founders-court/validation.json',JSON.stringify({...v,retainedBuildings:oldBuildings.length,existingGeometryUnchanged:true,newHouses:S.RIVERFRONT_BUILDINGS.filter(b=>!oldBuildings.some((q:{id:string})=>q.id===b.id))},null,2));
writeFileSync('docs/evidence/founders-court/fresh-save.json',JSON.stringify(S.makeSave(s)));
