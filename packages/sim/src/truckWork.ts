/** P8-03: physical chest -> truck cargo -> paid blueprint construction. */
import type { SimState } from './types';
import { truckFits, truckRect, TRUCK_RULES } from './truck';
import { distToRect, ground } from './ground';
import { MACHINE_COST, MACHINE_SIZE, DX, DY, canPickUp,remove,invTotal,SUPPLY_CHEST_CAP, type Dir } from './flow';
import { dimensions } from './footprint';
import { invStacks, TRUCK_STACKS, WALK_TILES_PER_S, TRUCK_MULT } from './engineer';
import { construct, undergroundCheck, type BuildEdit, type ActionResult } from './construction';
import { blueprintEntityBuilt, syncBlueprintOrders, type BlueprintOrder } from './blueprintPlans';
import { campaignRecruited } from './campaignRecruits';
import { undergroundSpan } from './routing';
import type { BlueprintEntity } from './blueprintFormat';
export const TRUCK_WORK_RULES={loadReach:3,loadPerSecond:20,actionTicks:20,retryTicks:40,searchNodes:12000,pathLimit:2048,authoredHeuristicWeight:2} as const;
export interface TruckPose {x:number;y:number;dir:Dir}
interface Rect {x:number;y:number;w:number;h:number}
export interface TruckWork {recovery?:{ids:number[];allIds:number[]};version:1;sourceId:number;orderIds:number[];enabled:boolean;phase:'loading'|'travelling'|'building'|'waiting'|'paused'|'complete';reason:string;route:TruckPose[];target?:{kind:'source'|'build';orderId:number;entityId:string};nextActionTick:number}
export type TruckWorkAction={type:'recover';sourceId:number;ids:number[]} | {type:'start';sourceId:number;orderIds:number[]} | {type:'pause'|'resume'|'stop'|'retry'} | {type:'source';sourceId:number};
const ok=(reason:string):ActionResult=>({ok:true,reason});
const no=(reason:string):ActionResult=>({ok:false,reason});
function source(st:SimState,id:number){return st.flow?.machines.find(m=>m.id===id&&m.kind==='chest');}
export function truckWorkGate(st:SimState):string {return !campaignRecruited(st,'foreman')?'Recruit the Foreman for truck construction':!st.campaign?.truck?'Restore the second-area station for the truck':st.engineer.truckSeat?'Exit the truck before starting automatic work':st.engineer.down>=0?'Wait until you recover':'';}
export function truckWorkCommand(st:SimState,a:TruckWorkAction):ActionResult {
 const t=st.campaign?.truck;if(!t)return no('No truck is available');
 if(a.type==='pause'||a.type==='stop'){
  if(!t.work)return no('No truck work assigned');if(!t.work.enabled)return ok(t.work.reason);t.work.enabled=false;t.work.phase='paused';t.work.reason=a.type==='stop'?'Job stopped; cargo and plans retained':'Paused; cargo and plans retained';t.work.route=[];delete t.work.target;return ok(t.work.reason);
 }
 const why=truckWorkGate(st);if(why)return no(why);
 if(a.type==='recover'){if(!source(st,a.sourceId)||!Array.isArray(a.ids)||!a.ids.length||a.ids.length>32||new Set(a.ids).size!==a.ids.length||a.ids.some(id=>id===a.sourceId||!st.flow!.machines.some(m=>m.id===id&&!['depot','tram','tramstop','track'].includes(m.kind))))return no('Select existing equipment and a different destination chest');t.work={version:1,sourceId:a.sourceId,orderIds:[],enabled:true,phase:'waiting',reason:'Recovery job requested',route:[],nextActionTick:st.flow!.tick,recovery:{ids:[...a.ids],allIds:[...a.ids]}};return ok('Recovery requested; cargo, contents and artifacts use real capacity');}
 if(a.type==='start'){
  if(!Number.isSafeInteger(a.sourceId)||!source(st,a.sourceId))return no('Choose an existing physical supply chest');
  if(!Array.isArray(a.orderIds)||!a.orderIds.length||a.orderIds.length>32||new Set(a.orderIds).size!==a.orderIds.length||a.orderIds.some(id=>!Number.isSafeInteger(id)||!st.campaign?.plans?.orders.some(o=>o.id===id&&o.status==='waiting')))return no('Choose waiting construction orders');
  if(t.work?.sourceId===a.sourceId&&JSON.stringify(t.work.orderIds)===JSON.stringify(a.orderIds))return ok('This job is already assigned; use Resume or Retry to continue');
  t.work={version:1,sourceId:a.sourceId,orderIds:[...a.orderIds],enabled:true,phase:'waiting',reason:'Finding a supply route',route:[],nextActionTick:st.flow!.tick};return ok('Truck assigned; real cargo will supply the selected plans');
 }
 if(a.type==='source'&&t.work){
  if(!Number.isSafeInteger(a.sourceId)||!source(st,a.sourceId))return no('Choose an existing physical supply chest');
  if(t.work.recovery?.allIds.includes(a.sourceId))return no('Choose a destination outside the recovery targets');
  if(t.work.sourceId===a.sourceId)return ok('Supply chest unchanged');
  t.work.sourceId=a.sourceId;t.work.route=[];delete t.work.target;if(t.work.enabled)t.work.phase='waiting';t.work.reason='Supply chest changed; cargo and orders retained';t.work.nextActionTick=st.flow!.tick;return ok(t.work.reason);
 }
 if(a.type==='retry'&&t.work){if(!t.work.enabled)return no('Resume the truck before retrying');if(t.work.phase!=='waiting'||t.work.nextActionTick<=st.flow!.tick)return ok(t.work.reason);t.work.nextActionTick=st.flow!.tick;return ok('Retry scheduled now');}
 if(a.type==='resume'&&t.work){if(t.work.enabled||t.work.phase==='complete')return ok(t.work.reason);t.work.enabled=true;t.work.phase='waiting';t.work.reason='Retrying current conditions';t.work.route=[];delete t.work.target;t.work.nextActionTick=st.flow!.tick;return ok(t.work.reason);}
 return no('No truck work to resume');
}
function clearOfEngineer(st:SimState,p:TruckPose):boolean {const r=truckRect(p),e=st.engineer;return e.x<=r.x||e.x>=r.x+r.w||e.y<=r.y||e.y>=r.y+r.h;}
function fits(st:SimState,p:TruckPose):boolean{return truckFits(st,p.x,p.y,p.dir)&&clearOfEngineer(st,p);}
/** Swept footprint checks are shared by route search and actual movement. */
export function truckSegmentFits(st:SimState,a:TruckPose,b:TruckPose):boolean {
 if(a.dir!==b.dir&&Math.hypot(a.x-b.x,a.y-b.y)>1e-7||Math.abs(a.x-b.x)>1e-7&&b.dir%2===0||Math.abs(a.y-b.y)>1e-7&&b.dir%2===1)return false;
 const n=Math.max(1,Math.ceil(Math.hypot(b.x-a.x,b.y-a.y)/TRUCK_RULES.substep));
 for(let i=0;i<=n;i++)if(!fits(st,{x:a.x+(b.x-a.x)*i/n,y:a.y+(b.y-a.y)*i/n,dir:b.dir}))return false;return true;
}
function serves(p:TruckPose,rects:Rect[],reach:number):boolean {const t=truckRect(p);return rects.every(r=>distToRect(p.x,p.y,r.x,r.y,r.w,r.h)<=reach&&!(t.x<r.x+r.w&&t.x+t.w>r.x&&t.y<r.y+r.h&&t.y+t.h>r.y));}
/** Deterministic bounded A*: forward/reverse one tile and footprint-valid quarter turns. No point-path fallback. */
export function truckWorkRoute(st:SimState,rects:Rect[],reach:number):TruckPose[]|null {
 const t=st.campaign!.truck!;if(serves(t,rects,reach))return [];
 interface Node {ix:number;iy:number;dir:Dir;g:number;score:number;parent:number}
 const nodes:Node[]=[],heap:number[]=[],best=new Map<string,number>();
 const heuristic=(x:number,y:number)=>Math.max(0,...rects.map(r=>distToRect(x,y,r.x,r.y,r.w,r.h)-reach));
 const pose=(n:Node):TruckPose=>({x:t.x+n.ix,y:t.y+n.iy,dir:n.dir});
 const less=(a:number,b:number)=>nodes[a].score<nodes[b].score||(nodes[a].score===nodes[b].score&&a<b);
 const push=(n:Node)=>{const id=nodes.push(n)-1;heap.push(id);let i=heap.length-1;while(i>0){const p=(i-1)>>1;if(!less(id,heap[p]))break;heap[i]=heap[p];i=p;}heap[i]=id;};
 const pop=()=>{const out=heap[0],last=heap.pop()!;if(heap.length){let i=0;while(i*2+1<heap.length){let c=i*2+1;if(c+1<heap.length&&less(heap[c+1],heap[c]))c++;if(!less(heap[c],last))break;heap[i]=heap[c];i=c;}heap[i]=last;}return out;};
 push({ix:0,iy:0,dir:t.dir,g:0,score:heuristic(t.x,t.y),parent:-1});best.set(`0,0,${t.dir}`,0);
 let visited=0;
 while(heap.length&&visited++<TRUCK_WORK_RULES.searchNodes){const id=pop(),n=nodes[id],p=pose(n);if(n.g!==(best.get(`${n.ix},${n.iy},${n.dir}`)))continue;
  if(serves(p,rects,reach)){const route:TruckPose[]=[];let at=id;while(nodes[at].parent>=0){route.push(pose(nodes[at]));at=nodes[at].parent;}return route.length<=TRUCK_WORK_RULES.pathLimit?route.reverse():null;}
  for(const [dx,dy,dir,cost] of [[DX[n.dir],DY[n.dir],n.dir,1],[-DX[n.dir],-DY[n.dir],n.dir,1],[0,0,(n.dir+1)%4,.4],[0,0,(n.dir+3)%4,.4]]){
   const ix=n.ix+dx,iy=n.iy+dy,d=dir as Dir,g=n.g+cost,key=`${ix},${iy},${d}`;if(g>=(best.get(key)??Infinity))continue;
   const q={x:t.x+ix,y:t.y+iy,dir:d};if(!truckSegmentFits(st,p,q))continue;best.set(key,g);push({ix,iy,dir:d,g,score:g+heuristic(q.x,q.y)*(st.city?.mapId?TRUCK_WORK_RULES.authoredHeuristicWeight:1),parent:id});
  }
 }
 return null;
}
interface Job {order:BlueprintOrder;entities:BlueprintEntity[];remaining:BlueprintEntity[];rects:Rect[]}
function nextJob(st:SimState,w:TruckWork):Job|null {
 for(const id of w.orderIds){const o=st.campaign?.plans?.orders.find(o=>o.id===id);if(!o||o.status!=='waiting'||o.paused)continue;
  const e=o.blueprint.entities.find(e=>!blueprintEntityBuilt(st,o,e));if(!e)return {order:o,entities:[],remaining:[],rects:[]};
  const entities=e.kind==='underground'?o.blueprint.entities.filter(b=>b.kind==='underground'&&b.dir===e.dir&&(b===e||b.underground!==e.underground&&!undergroundSpan(e.underground==='input'?e:b,e.underground==='input'?b:e,e.dir))).sort((a,b)=>Number(a.underground==='output')-Number(b.underground==='output')):[e];
  return {order:o,entities,remaining:entities.filter(e=>!blueprintEntityBuilt(st,o,e)),rects:entities.map(e=>{const [w,h]=dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind]);return {x:o.x+e.x,y:o.y+e.y,w,h};})};
 }return null;
}
function price(entities:BlueprintEntity[],stock:Record<string,number>):Record<string,number>{const have={...stock},missing:Record<string,number>={};for(const e of entities){if((have[e.kind]??0)>=1){have[e.kind]--;continue;}for(const [k,n] of Object.entries(MACHINE_COST[e.kind])){const used=Math.min(have[k]??0,n);have[k]=(have[k]??0)-used;missing[k]=(missing[k]??0)+n-used;}}return Object.fromEntries(Object.entries(missing).filter(([,n])=>n>0));}
function loadNeeds(st:SimState,w:TruckWork,available:Record<string,number>):Record<string,number>{
 const cargo={...st.campaign!.truck!.cargo},supply={...available},need:Record<string,number>={};
 const request=(k:string,n:number)=>{const used=Math.min(cargo[k]??0,n);cargo[k]=(cargo[k]??0)-used;const take=n-used;if(take>0){need[k]=(need[k]??0)+take;supply[k]=Math.max(0,(supply[k]??0)-take);}};
 for(const id of w.orderIds){const o=st.campaign!.plans?.orders.find(o=>o.id===id);if(!o||o.status!=='waiting'||o.paused)continue;for(const e of o.blueprint.entities){if(blueprintEntityBuilt(st,o,e))continue;if((cargo[e.kind]??0)>=1||(supply[e.kind]??0)>=1)request(e.kind,1);else for(const [k,n] of Object.entries(MACHINE_COST[e.kind]))request(k,n);}}
 return need;
}
function waiting(st:SimState,w:TruckWork,reason:string):void {w.phase='waiting';w.reason=reason;w.route=[];delete w.target;w.nextActionTick=st.flow!.tick+TRUCK_WORK_RULES.retryTicks;}
function routeTo(st:SimState,w:TruckWork,j:Job,kind:'source'|'build',rects:Rect[],reach:number):void {
 const route=truckWorkRoute(st,rects,reach);if(route===null){waiting(st,w,`Street route blocked or search budget reached for ${kind==='source'?'supply chest':'order #'+j.order.id}; clear access or choose nearer work`);return;}
 w.target={kind,orderId:j.order.id,entityId:j.entities[0]?.id??''};w.route=route;w.phase=route.length?'travelling':kind==='source'?'loading':'building';w.reason=route.length?`Travelling to ${kind==='source'?'supply chest':'order #'+j.order.id}`:kind==='source'?'Loading construction supplies':`Building order #${j.order.id}`;w.nextActionTick=st.flow!.tick;
}
export function tickTruckWork(st:SimState,dt:number):void {
 const t=st.campaign?.truck,w=t?.work;if(!t||!w?.enabled)return;
 if(st.engineer.truckSeat){w.enabled=false;w.phase='paused';w.reason='Manual takeover';w.route=[];delete w.target;return;}
 if(w.recovery){tickRecovery(st,dt);return;}
 syncBlueprintOrders(st);const j=nextJob(st,w);if(!j){if(w.orderIds.some(id=>st.campaign?.plans?.orders.some(o=>o.id===id&&o.status==='waiting'&&o.paused))){if(st.flow!.tick>=w.nextActionTick)waiting(st,w,'Selected unfinished orders are paused; resume an order');return;}w.enabled=false;w.phase='complete';w.reason='Selected orders finished or cancelled; cargo retained';w.route=[];delete w.target;return;}
 if(!j.entities.length){if(st.flow!.tick>=w.nextActionTick)waiting(st,w,'Planned underground connection is interrupted');return;}
 const chest=source(st,w.sourceId);if(!chest){if(st.flow!.tick>=w.nextActionTick)waiting(st,w,'Supply chest is missing; select a new source');return;}
 if(w.phase==='travelling'){
  if(w.target?.orderId!==j.order.id||w.target.entityId!==j.entities[0].id){w.route=[];w.phase='waiting';w.nextActionTick=st.flow!.tick;delete w.target;}
  else {let budget=WALK_TILES_PER_S*TRUCK_MULT*dt;while(w.route.length&&budget>1e-9){const p=w.route[0],d=Math.hypot(p.x-t.x,p.y-t.y),move=Math.min(budget,d),q={x:d?t.x+(p.x-t.x)*move/d:t.x,y:d?t.y+(p.y-t.y)*move/d:t.y,dir:p.dir};
    if(!truckSegmentFits(st,t,q)){waiting(st,w,'Street route obstructed; waiting to retry');return;}t.x=q.x;t.y=q.y;t.dir=q.dir;budget-=move;if(d<=move+1e-8)w.route.shift();else break;
   }
   if(!w.route.length){w.phase=w.target?.kind==='source'?'loading':'building';w.nextActionTick=st.flow!.tick;}return;
  }
 }
 if(st.flow!.tick<w.nextActionTick)return;
 const sourceRect={x:chest.x,y:chest.y,w:chest.size,h:chest.size},missing=price(j.remaining,t.cargo);
 if(w.phase==='loading'&&serves(t,[sourceRect],TRUCK_WORK_RULES.loadReach)){
  const need=loadNeeds(st,w,chest.inv);let moved=0;for(const [k,n] of Object.entries(need)){
   let amount=Math.min(n,Math.floor(chest.inv[k]??0),TRUCK_WORK_RULES.loadPerSecond-moved);while(amount>0&&invStacks({...t.cargo,[k]:(t.cargo[k]??0)+amount})>TRUCK_STACKS)amount--;
   if(amount){chest.inv[k]-=amount;if(chest.inv[k]<=0)delete chest.inv[k];t.cargo[k]=(t.cargo[k]??0)+amount;moved+=amount;}if(moved>=TRUCK_WORK_RULES.loadPerSecond)break;
  }
  if(moved){w.reason=`Loading: ${moved} items from chest #${chest.id}`;w.nextActionTick=st.flow!.tick+TRUCK_WORK_RULES.actionTicks;return;}
  const lacking=price(j.remaining,t.cargo);if(Object.keys(lacking).length){waiting(st,w,invStacks(t.cargo)>=TRUCK_STACKS?'Truck cargo is full; unload unrelated cargo':`Supply shortage: ${Object.entries(lacking).map(([k,n])=>`${n} ${k}`).join(', ')}`);return;}
  routeTo(st,w,j,'build',j.rects,TRUCK_RULES.serviceReach);return;
 }
 if(Object.keys(missing).length){routeTo(st,w,j,'source',[sourceRect],TRUCK_WORK_RULES.loadReach);return;}
 if(!serves(t,j.rects,TRUCK_RULES.serviceReach)){routeTo(st,w,j,'build',j.rects,TRUCK_RULES.serviceReach);return;}
 const toEdit=(e:BlueprintEntity):BuildEdit=>{const {id:_id,kind,...settings}=e;return {action:'place',item:kind,...settings,x:j.order.x+e.x,y:j.order.y+e.y};};
 if(j.entities[0].kind==='underground'){const check=undergroundCheck(st,j.entities.map(toEdit),'truck');if(!check.ok){waiting(st,w,check.reason);return;}}
 const result=construct(st,j.remaining.map(toEdit),'truck');if(!result.ok){waiting(st,w,result.reason.replace('pockets','truck cargo'));return;}
 syncBlueprintOrders(st);w.phase='building';w.reason=`Built ${j.remaining.length} part${j.remaining.length===1?'':'s'} of order #${j.order.id} from cargo`;w.route=[];delete w.target;w.nextActionTick=st.flow!.tick+TRUCK_WORK_RULES.actionTicks;
}
export function truckWorkDescription(st:SimState):string {const w=st.campaign?.truck?.work;return w?.recovery?`${w.phase} · ${w.reason} · recovery targets ${w.recovery.ids.join(', ')||'collected'} → chest #${w.sourceId}`:w?`${w.phase} · ${w.reason} · source chest #${w.sourceId} · orders ${w.orderIds.map(id=>'#'+id).join(', ')}`:'No automatic construction assigned';}
export function truckWorkProblem(st:SimState):string {
 const t=st.campaign?.truck,w=t?.work;if(w===undefined)return '';

 const record=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==='object'&&!Array.isArray(v);
 const keys=(v:Record<string,unknown>,list:string[])=>Object.keys(v).every(k=>list.includes(k));
 if(!record(w)||!keys(w,['version','sourceId','orderIds','enabled','phase','reason','route','target','nextActionTick','recovery'])||w.version!==1||!campaignRecruited(st,'foreman')||!Number.isSafeInteger(w.sourceId)||w.sourceId<1||w.sourceId>=st.flow!.next||(!w.recovery&&!st.campaign?.plans)||!Array.isArray(w.orderIds)||(!w.recovery&&!w.orderIds.length)||w.orderIds.length>32||w.orderIds.some(id=>!Number.isSafeInteger(id)||id<1||id>=st.campaign!.plans!.nextId)||new Set(w.orderIds).size!==w.orderIds.length||typeof w.enabled!=='boolean'||!['loading','travelling','building','waiting','paused','complete'].includes(w.phase)||typeof w.reason!=='string'||w.reason.length>1024||!Number.isSafeInteger(w.nextActionTick)||w.nextActionTick<0||w.nextActionTick>st.flow!.tick+TRUCK_WORK_RULES.retryTicks||!Array.isArray(w.route)||w.route.length>TRUCK_WORK_RULES.pathLimit)return 'invalid truck work state';
 if(w.recovery&&(!record(w.recovery)||!keys(w.recovery,['ids','allIds'])||!Array.isArray(w.recovery.ids)||!Array.isArray(w.recovery.allIds)||!w.recovery.allIds.length||w.recovery.allIds.length>32||new Set(w.recovery.allIds).size!==w.recovery.allIds.length||new Set(w.recovery.ids).size!==w.recovery.ids.length||w.recovery.allIds.some(id=>!Number.isSafeInteger(id)||id<1||id>=st.flow!.next||id===w.sourceId)||w.recovery.ids.some(id=>!w.recovery!.allIds.includes(id))||w.orderIds.length||w.target!==undefined))return 'invalid truck recovery job';
 if((w.phase==='paused'||w.phase==='complete')===w.enabled||st.engineer.truckSeat&&w.enabled)return 'invalid truck work control';
 if(w.target!==undefined&&(!record(w.target)||!keys(w.target,['kind','orderId','entityId'])||!['source','build'].includes(w.target.kind)||!w.orderIds.includes(w.target.orderId)||typeof w.target.entityId!=='string'||!/^[a-z][a-z0-9_-]{0,39}$/.test(w.target.entityId)))return 'invalid truck work target';
 if(w.phase==='travelling'&&(!w.recovery&&!w.target||!w.route.length)||w.phase!=='travelling'&&w.route.length)return 'invalid truck work route phase';
 let previous:TruckPose=t!;const G=ground(st);for(const p of w.route){if(!record(p)||!keys(p,['x','y','dir'])||!Number.isFinite(p.x)||!Number.isFinite(p.y)||p.x<0||p.y<0||p.x>G.tw||p.y>G.th||![0,1,2,3].includes(p.dir))return 'invalid truck route pose';const dx=Math.abs(p.x-previous.x),dy=Math.abs(p.y-previous.y);if(dx>1e-7&&dy>1e-7||dx+dy>1+1e-7||p.dir!==previous.dir&&dx+dy>1e-7||dx>1e-7&&p.dir%2===0||dy>1e-7&&p.dir%2===1)return 'invalid truck route segment';previous=p;}
 return '';
}

/** Explicit recovery reuses truck routing, the normal packing validator and real cargo. */
function tickRecovery(st:SimState,dt:number):void {
 const t=st.campaign!.truck!,w=t.work!,job=w.recovery!,chest=source(st,w.sourceId);
 if(!chest){waiting(st,w,'Recovery destination chest missing; choose another');return;}
 if(w.route.length){let budget=WALK_TILES_PER_S*TRUCK_MULT*dt;while(w.route.length&&budget>1e-9){const p=w.route[0],d=Math.hypot(p.x-t.x,p.y-t.y),move=Math.min(budget,d),q={x:d?t.x+(p.x-t.x)*move/d:t.x,y:d?t.y+(p.y-t.y)*move/d:t.y,dir:p.dir};if(!truckSegmentFits(st,t,q)){waiting(st,w,'Recovery street route blocked');return;}Object.assign(t,q);budget-=move;if(d<=move+1e-8)w.route.shift();else break;}if(!w.route.length)w.phase='loading';return;}
 if(st.flow!.tick<w.nextActionTick)return;
 const m=st.flow!.machines.find(m=>m.id===job.ids[0]);
 if(job.ids.length&&!m){job.ids.shift();w.reason='Previously selected equipment no longer present';return;}
 const route=(rect:Rect,reach:number,label:string)=>{const path=truckWorkRoute(st,[rect],reach);if(!path){waiting(st,w,`Street access blocked or route search budget reached for ${label}`);return false;}w.route=path;w.phase=path.length?'travelling':'loading';w.reason=label;return !path.length;};
 // Unload before every pickup, so unrelated cargo cannot silently crowd out recovered artifacts/contents.
 if(Object.values(t.cargo).some(n=>n>0)){
  if(!route({x:chest.x,y:chest.y,w:chest.size,h:chest.size},TRUCK_WORK_RULES.loadReach,'Delivering recovery cargo'))return;
  let room=SUPPLY_CHEST_CAP-invTotal(chest.inv);for(const [k,n] of Object.entries(t.cargo)){const move=Math.min(n,room);if(move>0){chest.inv[k]=(chest.inv[k]??0)+move;t.cargo[k]-=move;if(t.cargo[k]<=0)delete t.cargo[k];room-=move;}}
  if(Object.values(t.cargo).some(n=>n>0))waiting(st,w,'Destination chest full; cargo retained');return;
 }
 if(!m){w.enabled=false;w.phase='complete';w.reason='Recovery complete; equipment delivered to chosen chest';return;}
 if(!route({x:m.x,y:m.y,w:m.size,h:m.size},TRUCK_RULES.serviceReach,`Recovering ${m.kind} #${m.id}`))return;
 if(m.busy&&['assembler','assembler2','foundry','refinery','mixer'].includes(m.kind)){waiting(st,w,'Processor busy; clear inputs and let the craft finish');return;}
 if(m.kind==='turret'&&(m.inv.rounds??0)%10){waiting(st,w,'Unload loose turret rounds before automatic recovery');return;}
 const engineer=st.engineer;
 try{st.engineer={...engineer,truck:true,pack:undefined,inv:t.cargo,x:t.x,y:t.y};const chk=canPickUp(st,m.x,m.y);if(!chk.ok){waiting(st,w,chk.reason.replace('pockets','truck cargo'));return;}if(!remove(st,m.x,m.y)){waiting(st,w,'Equipment changed; retry recovery');return;}t.cargo=st.engineer.inv;job.ids.shift();w.reason='Equipment, contents and artifact safely in cargo';w.nextActionTick=st.flow!.tick+TRUCK_WORK_RULES.actionTicks;}finally{st.engineer=engineer;}
}
