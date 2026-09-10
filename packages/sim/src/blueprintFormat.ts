/** Shared blueprint data only: no bot, DOM, inventory or privileged build path. */
import { type Kind, type Dir, type RecipeId, type Item, isKind, isItem, isRecipeId, recipesFor, MACHINE_SIZE } from './flow';
import { dimensions } from './footprint';
import { type OutputPriority, type UndergroundMode, undergroundSpan } from './routing';
import { type StationRules, validStationRules } from './freight';

export interface BlueprintEntity { id:string;kind:Exclude<Kind,'depot'>;x:number;y:number;dir:Dir;recipe?:RecipeId;filter?:Item;priority?:OutputPriority;underground?:UndergroundMode;freight?:StationRules }
export interface Blueprint { version:1;name:string;entities:BlueprintEntity[] }
const record=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==='object'&&!Array.isArray(v);
const keys=(o:Record<string,unknown>,allowed:string[])=>Object.keys(o).every(k=>allowed.includes(k));
/** Validate the complete file before any command. Reject typos, collisions, mismatched settings and orphan tunnels. */
export function parseBlueprint(raw:unknown):Blueprint {
 if(!record(raw)||!keys(raw,['version','name','entities'])||raw.version!==1||typeof raw.name!=='string'||!raw.name.trim()||raw.name.length>120||!Array.isArray(raw.entities)||!raw.entities.length||raw.entities.length>128)throw new Error('Invalid blueprint header (version 1, name, 1–128 entities)');
 const ids=new Set<string>(),occupied=new Map<string,BlueprintEntity[]>();
 const entities=raw.entities.map((v,i)=>{
  if(!record(v)||!keys(v,['id','kind','x','y','dir','recipe','filter','priority','underground','freight'])||typeof v.id!=='string'||!/^[a-z][a-z0-9_-]{0,39}$/.test(v.id)||ids.has(v.id)||typeof v.kind!=='string'||!isKind(v.kind)||v.kind==='depot'||!Number.isSafeInteger(v.x)||!Number.isSafeInteger(v.y)||Math.abs(v.x as number)>128||Math.abs(v.y as number)>128||!Number.isSafeInteger(v.dir)||![0,1,2,3].includes(v.dir as number))throw new Error(`Invalid blueprint entity ${i}`);
  const e=v as unknown as BlueprintEntity;ids.add(e.id);
  if(e.recipe!==undefined&&(!['assembler','assembler2','foundry','refinery'].includes(e.kind)||!isRecipeId(e.recipe)||!recipesFor(e).includes(e.recipe)))throw new Error(`Invalid recipe on ${e.id}`);
  if(e.filter!==undefined&&(e.kind!=='inserter'||!isItem(e.filter)))throw new Error(`Invalid filter on ${e.id}`);
  if(e.priority!==undefined&&(e.kind!=='splitter'||!['balanced','left','right'].includes(e.priority)))throw new Error(`Invalid priority on ${e.id}`);
  if(e.freight!==undefined&&(e.kind!=='tramstop'||!validStationRules(e.freight)))throw new Error(`Invalid freight on ${e.id}`);
  if((e.kind==='underground')!==(e.underground!==undefined)||e.underground!==undefined&&!['input','output'].includes(e.underground))throw new Error(`Invalid underground mode on ${e.id}`);
  const [w,h]=dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind]);
  for(let y=e.y;y<e.y+h;y++)for(let x=e.x;x<e.x+w;x++){
   const key=`${x},${y}`,old=occupied.get(key)??[],collision=old.find(o=>!((o.kind==='track'&&e.kind==='tram')||(o.kind==='tram'&&e.kind==='track')));
   if(collision)throw new Error(`Blueprint footprints overlap: ${collision.id}, ${e.id}`);
   occupied.set(key,[...old,e]);
  }return JSON.parse(JSON.stringify(e)) as BlueprintEntity;
 });
 for(const e of entities.filter(e=>e.kind==='underground')){
  const mates=entities.filter(m=>m.kind==='underground'&&m.dir===e.dir&&m.underground!==e.underground&&!undergroundSpan(e.underground==='input'?e:m,e.underground==='input'?m:e,e.dir));
  if(mates.length!==1)throw new Error(`Underground ${e.id} needs one unambiguous paired endpoint`);
 }
 return {version:1,name:raw.name,entities};
}

