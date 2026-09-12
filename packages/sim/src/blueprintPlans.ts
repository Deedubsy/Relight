/** Saved plan data is inert. Only the ordinary paid constructor creates machines. */
import type { SimState } from './types';
import { parseBlueprint, type Blueprint, type BlueprintEntity } from './blueprintFormat';
import { blueprintGate, blueprintBounds } from './blueprint';
import { construct, undergroundCheck, type BuildEdit, type ActionResult } from './construction';
import { machineAt, tramAt, canPlace, isKind, MACHINE_SIZE, MACHINE_COST, type Kind } from './flow';
import { dimensions } from './footprint';
import { inReach, ground } from './ground';
import { campaignRecruited } from './campaignRecruits';
import { undergroundSpan, undergroundMate } from './routing';
export const BLUEPRINT_LIBRARY_LIMIT=32, BLUEPRINT_ORDER_LIMIT=32, BLUEPRINT_TEXT_LIMIT=262144;
export interface BlueprintEntry { id:number; name:string; folder:string; icon:Exclude<Kind,'depot'>; blueprint:Blueprint }
export interface BlueprintOrder { id:number; blueprint:Blueprint; x:number; y:number; createdAt:number; status:'waiting'|'cancelled'|'completed'; paused?:boolean; finishedAt?:number }
export interface BlueprintPlans { version:1; nextId:number; library:BlueprintEntry[]; orders:BlueprintOrder[] }
type Metadata={name:string;folder:string;icon:Exclude<Kind,'depot'>};
export type BlueprintLibraryAction = ({type:'save'}&Metadata) | ({type:'update';id:number}&Metadata) | {type:'load'|'remove';id:number} | {type:'import';text:string};
export type BlueprintOrderAction = {type:'queue';x:number;y:number} | {type:'build'|'cancel'|'remove'|'pause'|'resume';id:number};
const clone=<T>(v:T):T=>structuredClone(v);
const record=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==='object'&&!Array.isArray(v);
const keys=(v:Record<string,unknown>,allowed:string[])=>Object.keys(v).every(k=>allowed.includes(k));
function metadata(v:Metadata):void {
 if(typeof v.name!=='string'||!v.name.trim()||v.name.length>120||typeof v.folder!=='string'||v.folder.length>64||typeof v.icon!=='string'||!isKind(v.icon)||String(v.icon)==='depot')throw Error('Use a name (1–120 characters), folder (up to 64) and catalogue icon.');
}
function bounded(raw:unknown):Blueprint {
 const bp=parseBlueprint(raw),b=blueprintBounds(bp);if(b.w>128||b.h>128)throw Error('Blueprint exceeds 128 × 128 tiles.');
 return {...bp,entities:bp.entities.map(e=>({...e,x:e.x-b.x,y:e.y-b.y})).sort((a,b)=>Number(a.kind==='tram')-Number(b.kind==='tram'))};
}
function allocate(p:BlueprintPlans):number {if(!Number.isSafeInteger(p.nextId)||p.nextId>=Number.MAX_SAFE_INTEGER)throw Error('Plan identifiers exhausted.');return p.nextId++;}
function plans(st:SimState):BlueprintPlans {return st.campaign!.plans??{version:1,nextId:1,library:[],orders:[]};}
export function exportBlueprintEntry(st:SimState,id:number):string {
 const e=st.campaign?.plans?.library.find(e=>e.id===id);if(!e)throw Error('Choose a saved blueprint.');
 return JSON.stringify({format:'relight-blueprint',version:1,name:e.name,folder:e.folder,icon:e.icon,blueprint:e.blueprint},null,2);
}
export function blueprintLibrary(st:SimState,a:BlueprintLibraryAction):ActionResult {
 try{const why=blueprintGate(st);if(why)throw Error(why);const p=clone(plans(st));
  if(a.type==='save'||a.type==='import'){
   if(p.library.length>=BLUEPRINT_LIBRARY_LIMIT)throw Error('Library full (32 entries). Remove an entry first.');
   let m:Metadata,bp:Blueprint;
   if(a.type==='import'){
    if(typeof a.text!=='string'||a.text.length>BLUEPRINT_TEXT_LIMIT)throw Error('Import text exceeds 262144 characters.');
    const v:unknown=JSON.parse(a.text);
    if(!record(v)||!keys(v,['format','version','name','folder','icon','blueprint'])||v.format!=='relight-blueprint'||v.version!==1)throw Error('Invalid portable blueprint version or fields.');
    m=v as unknown as Metadata;metadata(m);bp=bounded(v.blueprint);if(bp.name!==m.name)throw Error('Blueprint names disagree.');
   }else{metadata(a);m=a;bp=bounded(st.campaign!.clipboard);bp.name=m.name;}
   p.library.push({id:allocate(p),name:m.name,folder:m.folder,icon:m.icon,blueprint:bp});st.campaign!.clipboard=clone(bp);
  }else{
   const e=p.library.find(e=>e.id===a.id);if(!e)throw Error('Choose a saved blueprint.');
   if(a.type==='update'){metadata(a);e.name=e.blueprint.name=a.name;e.folder=a.folder;e.icon=a.icon;}
   else if(a.type==='load')st.campaign!.clipboard=clone(e.blueprint);
   else if(a.type==='remove')p.library=p.library.filter(e=>e.id!==a.id);
   else throw Error('Invalid library action.');
  }
  st.campaign!.plans=p;return {ok:true,reason:'Blueprint library updated.'};
 }catch(e){return {ok:false,reason:(e as Error).message};}
}
function cells(bp:Blueprint,x:number,y:number):Set<string>{const cells=new Set<string>();for(const e of bp.entities){const [w,h]=dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind]);for(let yy=0;yy<h;yy++)for(let xx=0;xx<w;xx++)cells.add(`${x+e.x+xx},${y+e.y+yy}`);}return cells;}
function destination(st:SimState,bp:Blueprint,x:number,y:number):void {
 const b=blueprintBounds(bp);if(!Number.isSafeInteger(x)||!Number.isSafeInteger(y)||x<0||y<0||x+b.w>st.flow!.tw||y+b.h>ground(st).th)throw Error('Plan must fit inside the city map.');
}
export function blueprintQueueCheck(st:SimState,x:number,y:number):ActionResult {
 try{const why=blueprintGate(st);if(why)throw Error(why);const p=plans(st),bp=bounded(st.campaign!.clipboard);destination(st,bp,x,y);
  if(p.orders.length>=BLUEPRINT_ORDER_LIMIT)throw Error('Queue full (32 records). Remove a finished record first.');
  const occupied=cells(bp,x,y);for(const o of p.orders)if(o.status==='waiting'&&[...cells(o.blueprint,o.x,o.y)].some(k=>occupied.has(k)))throw Error(`Overlaps waiting order #${o.id}.`);
  return {ok:true,reason:''};
 }catch(e){return {ok:false,reason:(e as Error).message};}
}
const freightKey=(v:BlueprintEntity['freight'])=>JSON.stringify(Object.entries(v??{}).sort(([a],[b])=>a.localeCompare(b)).map(([k,r])=>[k,r!.request,r!.reserve,r!.export]));
export function blueprintEntityBuilt(st:SimState,o:BlueprintOrder,e:BlueprintEntity):boolean {
 const x=o.x+e.x,y=o.y+e.y,m=e.kind==='tram'?tramAt(st,x,y):machineAt(st,x,y);
 return !!m&&m.x===x&&m.y===y&&m.kind===e.kind&&m.dir===e.dir&&(m.recipe??'shot')===(e.recipe??'shot')&&m.filter===e.filter&&(m.priority??'balanced')===(e.priority??'balanced')&&m.underground===e.underground&&freightKey(m.freight)===freightKey(e.freight);
}
function edits(o:BlueprintOrder):BuildEdit[]{return o.blueprint.entities.map(({id:_id,kind,...e})=>({action:'place',item:kind,...e,x:o.x+e.x,y:o.y+e.y}));}
export function blueprintOrderInfo(st:SimState,o:BlueprintOrder){
 const remaining=o.blueprint.entities.filter(e=>!blueprintEntityBuilt(st,o,e)),stock:Record<string,number>={...st.engineer.inv},cost:Record<string,number>={},blockers=new Set<string>();
 for(const e of remaining){
  if((stock[e.kind]??0)>0)stock[e.kind]--;else for(const [k,n] of Object.entries(MACHINE_COST[e.kind]))cost[k]=(cost[k]??0)+n;
  const x=o.x+e.x,y=o.y+e.y,check=canPlace(st,e.kind,x,y,e.dir);if(!check.ok)blockers.add(check.reason);
  if(!inReach(st,x,y,...dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind])))blockers.add('Walk closer to build');
 }
 const missing=Object.fromEntries(Object.entries(cost).map(([k,n])=>[k,Math.max(0,n-(stock[k]??0))]).filter(([,n])=>Number(n)>0));
 if(!remaining.length&&!pairsMatch(st,o))blockers.add('Another underground endpoint interrupts the planned pair');
 if(st.engineer.down>=0)blockers.add('Wait until you recover');
 return {built:o.blueprint.entities.length-remaining.length,remaining,missing,blockers:[...blockers]};
}
function pairsMatch(st:SimState,o:BlueprintOrder):boolean {
 return o.blueprint.entities.filter(e=>e.underground==='input').every(e=>{
  const intended=o.blueprint.entities.find(b=>b.underground==='output'&&b.dir===e.dir&&!undergroundSpan(e,b,e.dir));
  const m=machineAt(st,o.x+e.x,o.y+e.y),mate=m&&undergroundMate(st,m);return !!intended&&mate?.x===o.x+intended.x&&mate.y===o.y+intended.y;
 });
}
export function syncBlueprintOrders(st:SimState):void {
 for(const o of st.campaign?.plans?.orders??[])if(o.status==='waiting'&&o.blueprint.entities.every(e=>blueprintEntityBuilt(st,o,e))&&pairsMatch(st,o)){o.status='completed';delete o.paused;o.finishedAt=st.t;}
}
export function blueprintOrder(st:SimState,a:BlueprintOrderAction):ActionResult {
 try{const why=blueprintGate(st);if(why)throw Error(why);const p=plans(st);
  if(a.type==='queue'){
   const check=blueprintQueueCheck(st,a.x,a.y);if(!check.ok)return check;
   const o:BlueprintOrder={id:allocate(p),blueprint:bounded(st.campaign!.clipboard),x:a.x,y:a.y,createdAt:st.t,status:'waiting'};
   p.orders.push(o);st.campaign!.plans=p;syncBlueprintOrders(st);return {ok:true,reason:`Queued plan #${o.id}; no materials spent.`};
  }
  const o=p.orders.find(o=>o.id===a.id);if(!o)throw Error('Choose a saved order.');
  if(a.type==='pause'||a.type==='resume'){
   if(o.status!=='waiting')throw Error('Order is already finished.');
   if(a.type==='pause'){if(o.paused)return {ok:true,reason:'Order is already paused.'};o.paused=true;}else{if(!o.paused)return {ok:true,reason:'Order is already waiting.'};delete o.paused;}
   const w=st.campaign?.truck?.work;if(w?.enabled&&w.orderIds.includes(o.id)){w.phase='waiting';w.reason=o.paused?'Order paused; checking other selected plans':'Order resumed; checking current conditions';w.route=[];delete w.target;w.nextActionTick=st.flow!.tick;}
  }else if(a.type==='remove'){if(o.status==='waiting')throw Error('Cancel the order before removing its record.');p.orders=p.orders.filter(v=>v.id!==o.id);}
  else if(a.type==='cancel'){if(o.status==='cancelled')return {ok:true,reason:'Order is already cancelled.'};if(o.status!=='waiting')throw Error('Order is already finished.');delete o.paused;o.status='cancelled';o.finishedAt=st.t;}
  else if(a.type==='build'){
   if(o.status!=='waiting')throw Error('Order is already finished.');
   if(o.paused)throw Error('Resume this order before building it.');
   const all=edits(o),remaining=all.filter((_,i)=>!blueprintEntityBuilt(st,o,o.blueprint.entities[i]));
   for(const e of all)if(e.action==='place'&&e.underground==='input'){
    const b=all.find(b=>b.action==='place'&&b.underground==='output'&&b.dir===e.dir&&!undergroundSpan(e,b,e.dir));
    if(b&&remaining.some(r=>r===e||r===b)){const check=undergroundCheck(st,[e,b]);if(!check.ok)return check;}
   }
   if(remaining.length){const check=construct(st,remaining);if(!check.ok)return check;}
   syncBlueprintOrders(st);return {ok:true,reason:String(o.status)==='completed'?'Order completed using ordinary construction.':'Waiting for the planned underground connection.'};
  }else throw Error('Invalid order action.');
  return {ok:true,reason:'Order updated; existing machines remain.'};
 }catch(e){return {ok:false,reason:(e as Error).message};}
}
export function blueprintPlansProblem(st:SimState):string {
 const raw:unknown=st.campaign?.plans;if(raw===undefined)return '';
 try{
  if((st.campaign?.version??0)<10||!campaignRecruited(st,'foreman'))throw Error('requires Foreman and metadata 10');
  if(!record(raw)||!keys(raw,['version','nextId','library','orders'])||raw.version!==1||!Number.isSafeInteger(raw.nextId)||Number(raw.nextId)<1||!Array.isArray(raw.library)||raw.library.length>BLUEPRINT_LIBRARY_LIMIT||!Array.isArray(raw.orders)||raw.orders.length>BLUEPRINT_ORDER_LIMIT)throw Error('invalid header or limits');
  const ids=new Set<number>();const id=(n:number)=>{if(!Number.isSafeInteger(n)||n<1||n>=Number(raw.nextId)||ids.has(n))throw Error('invalid identifier');ids.add(n);};
  for(const e of raw.library){if(!record(e)||!keys(e,['id','name','folder','icon','blueprint']))throw Error('invalid entry');metadata(e as unknown as Metadata);id(e.id as number);const bp=bounded(e.blueprint);if(bp.name!==e.name||JSON.stringify(bp)!==JSON.stringify(e.blueprint))throw Error('entry definition is not normalized');}
  const occupied=new Set<string>();
  for(const v of raw.orders){if(!record(v)||!keys(v,['id','blueprint','x','y','createdAt','status','finishedAt','paused']))throw Error('invalid order');const o=v as unknown as BlueprintOrder;id(o.id);const bp=bounded(o.blueprint);if(JSON.stringify(bp)!==JSON.stringify(o.blueprint))throw Error('order definition is not normalized');destination(st,bp,o.x,o.y);
   if(o.paused!==undefined&&(typeof o.paused!=='boolean'||o.status!=='waiting'))throw Error('invalid order pause');
   if(!['waiting','cancelled','completed'].includes(o.status)||!Number.isFinite(o.createdAt)||o.createdAt<0||o.createdAt>st.t||(o.status==='waiting'?o.finishedAt!==undefined:!Number.isFinite(o.finishedAt)||o.finishedAt!<o.createdAt||o.finishedAt!>st.t))throw Error('invalid order lifecycle');
   if(o.status==='waiting')for(const k of cells(bp,o.x,o.y)){if(occupied.has(k))throw Error('overlapping orders');occupied.add(k);}
  }return '';
 }catch(e){return `invalid blueprint plans: ${(e as Error).message}`;}
}
