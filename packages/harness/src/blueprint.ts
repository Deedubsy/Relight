/** Harness plans are data, not a player unlock or a privileged construction path. */
import { type SimState, type Command, type LoggedCommand, type Kind, type Dir, type RecipeId, type Item,
 type OutputPriority, type UndergroundMode, isKind, isItem, isRecipeId, dimensions, MACHINE_SIZE,
 machineAt, tramAt, applyCommands, advanceFlow, actionResult, inReach, passable, findPath, stateHash, loadState,
 undergroundSpan, distToRect } from '@relight/sim';

export interface BlueprintEntity { id:string;kind:Exclude<Kind,'depot'>;x:number;y:number;dir:Dir;recipe?:RecipeId;filter?:Item;priority?:OutputPriority;underground?:UndergroundMode }
export interface Blueprint { version:1;name:string;entities:BlueprintEntity[] }
const record=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==='object'&&!Array.isArray(v);
const keys=(o:Record<string,unknown>,allowed:string[])=>Object.keys(o).every(k=>allowed.includes(k));
/** Validate the complete file before any command. Reject typos, collisions, mismatched settings and orphan tunnels. */
export function parseBlueprint(raw:unknown):Blueprint {
 if(!record(raw)||!keys(raw,['version','name','entities'])||raw.version!==1||typeof raw.name!=='string'||!raw.name.trim()||raw.name.length>120||!Array.isArray(raw.entities)||!raw.entities.length||raw.entities.length>128)throw new Error('Invalid blueprint header (version 1, name, 1–128 entities)');
 const ids=new Set<string>(),occupied=new Map<string,BlueprintEntity[]>();
 const entities=raw.entities.map((v,i)=>{
  if(!record(v)||!keys(v,['id','kind','x','y','dir','recipe','filter','priority','underground'])||typeof v.id!=='string'||!/^[a-z][a-z0-9_-]{0,39}$/.test(v.id)||ids.has(v.id)||typeof v.kind!=='string'||!isKind(v.kind)||v.kind==='depot'||!Number.isSafeInteger(v.x)||!Number.isSafeInteger(v.y)||Math.abs(v.x as number)>128||Math.abs(v.y as number)>128||!Number.isSafeInteger(v.dir)||![0,1,2,3].includes(v.dir as number))throw new Error(`Invalid blueprint entity ${i}`);
  const e=v as unknown as BlueprintEntity;ids.add(e.id);
  if(e.recipe!==undefined&&(e.kind!=='assembler'||!isRecipeId(e.recipe)))throw new Error(`Invalid recipe on ${e.id}`);
  if(e.filter!==undefined&&(e.kind!=='inserter'||!isItem(e.filter)))throw new Error(`Invalid filter on ${e.id}`);
  if(e.priority!==undefined&&(e.kind!=='splitter'||!['balanced','left','right'].includes(e.priority)))throw new Error(`Invalid priority on ${e.id}`);
  if((e.kind==='underground')!==(e.underground!==undefined)||e.underground!==undefined&&!['input','output'].includes(e.underground))throw new Error(`Invalid underground mode on ${e.id}`);
  const [w,h]=dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind]);
  for(let y=e.y;y<e.y+h;y++)for(let x=e.x;x<e.x+w;x++){
   const key=`${x},${y}`,old=occupied.get(key)??[],collision=old.find(o=>!((o.kind==='track'&&e.kind==='tram')||(o.kind==='tram'&&e.kind==='track')));
   if(collision)throw new Error(`Blueprint footprints overlap: ${collision.id}, ${e.id}`);
   occupied.set(key,[...old,e]);
  }return {...e};
 });
 for(const e of entities.filter(e=>e.kind==='underground')){
  const mates=entities.filter(m=>m.kind==='underground'&&m.dir===e.dir&&m.underground!==e.underground&&!undergroundSpan(e.underground==='input'?e:m,e.underground==='input'?m:e,e.dir));
  if(mates.length!==1)throw new Error(`Underground ${e.id} needs one unambiguous paired endpoint`);
 }
 return {version:1,name:raw.name,entities};
}

/** Walk, wait and apply the same commands as the UI. No teleports, stock writes or helper placements. */
export class FactoryDriver {
 readonly log:LoggedCommand[]=[];
 constructor(readonly state:SimState){}
 send(c:Command):void {this.log.push({tick:this.state.flow!.tick,c:structuredClone(c)});applyCommands(this.state,[c]);if(['factory','construct','undergroundPair'].includes(c.type)&&!actionResult(this.state).ok)throw new Error(actionResult(this.state).reason);}
 run(seconds:number):void {if(!Number.isFinite(seconds)||seconds<0||seconds>18000)throw new Error('Invalid simulation duration');advanceFlow(this.state,seconds,[],Math.ceil(seconds)+1);}
 walk(x:number,y:number):void {const e=this.state.engineer;this.send({type:'move',x,y});for(let k=0;k<240&&e.target&&e.down<0;k++)this.run(1);if(e.down>=0||Math.hypot(e.x-x,e.y-y)>.8)throw new Error(`Could not walk to ${x},${y}`);}
 approach(x:number,y:number,w=1,h=w,outside=false):void {
  const st=this.state,e=st.engineer;if(e.down>=0||e.truckSeat)throw new Error('Builder needs an engineer on foot');
  if(inReach(st,x,y,w,h)&&(!outside||distToRect(e.x,e.y,x,y,w,h)>0))return;
  for(let r=1;r<=7;r++)for(let yy=y-r;yy<y+h+r;yy++)for(let xx=x-r;xx<x+w+r;xx++){
   if(outside&&xx>=x&&xx<x+w&&yy>=y&&yy<y+h)continue;
   if(!passable(st,xx,yy)||!findPath(st,Math.floor(e.x),Math.floor(e.y),xx,yy))continue;
   this.walk(xx+.5,yy+.5);
   if(!inReach(st,x,y,w,h)||e.down>=0)throw new Error(`Could not walk within reach of ${x},${y}`);return;
  }throw new Error(`No physical approach to ${x},${y}`);
 }
 take(item:string,n:number,at?:{x:number;y:number;size:number}):number {
  if(at)this.approach(at.x,at.y,at.size);this.send({type:'factory',action:{type:'chestTake',item,n,...(at?{x:at.x,y:at.y}:{})}});return actionResult(this.state).moved??0;
 }
 put(item:string,n:number,at:{x:number;y:number;size:number}):number {this.approach(at.x,at.y,at.size);this.send({type:'factory',action:{type:'chestPut',item,n,x:at.x,y:at.y}});return actionResult(this.state).moved??0;}
 place(kind:Exclude<Kind,'depot'>,x:number,y:number,dir:Dir=0,underground?:UndergroundMode){
  const [w,h]=dimensions(kind,dir,MACHINE_SIZE[kind]);this.approach(x,y,w,h,true);this.send({type:'construct',edits:[{action:'place',item:kind,x,y,dir,...(underground?{underground}:{})}]});
  const m=kind==='tram'?tramAt(this.state,x,y):machineAt(this.state,x,y);if(!m||m.kind!==kind)throw new Error(`Placement failed: ${kind}`);return m;
 }
}
export function buildBlueprint(d:FactoryDriver,raw:unknown,x:number,y:number):Record<string,number> {
 const plan=parseBlueprint(raw);if(!Number.isSafeInteger(x)||!Number.isSafeInteger(y))throw new Error('Invalid blueprint origin');const built:Record<string,number>={};
 const rects=plan.entities.map(e=>{const [w,h]=dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind]);return {x:x+e.x,y:y+e.y,w,h};});
 const candidates:{x:number;y:number;distance:number}[]=[],engineer=d.state.engineer;
 for(let yy=Math.min(...rects.map(r=>r.y))-1;yy<=Math.max(...rects.map(r=>r.y+r.h));yy++)for(let xx=Math.min(...rects.map(r=>r.x))-1;xx<=Math.max(...rects.map(r=>r.x+r.w));xx++){
  if(passable(d.state,xx,yy)&&rects.every(r=>{const distance=distToRect(xx+.5,yy+.5,r.x,r.y,r.w,r.h);return distance>0&&distance<=engineer.reach;}))candidates.push({x:xx,y:yy,distance:Math.hypot(xx+.5-engineer.x,yy+.5-engineer.y)});
 }
 const stand=candidates.sort((a,b)=>a.distance-b.distance).find(p=>findPath(d.state,Math.floor(engineer.x),Math.floor(engineer.y),p.x,p.y));
 if(stand)d.walk(stand.x+.5,stand.y+.5);
 for(const e of plan.entities){try{
  const m=d.place(e.kind,x+e.x,y+e.y,e.dir,e.underground);built[e.id]=m.id;
  if(e.recipe)d.send({type:'factory',action:{type:'setRecipe',x:m.x,y:m.y,recipe:e.recipe}});
  if(e.filter!==undefined||e.priority!==undefined)d.send({type:'factory',action:{type:'routing',x:m.x,y:m.y,...(e.filter?{filter:e.filter}:{}),...(e.priority?{priority:e.priority}:{})}});
 }catch(error){throw new Error(`Blueprint stopped at ${e.id}; ${Object.keys(built).length} placements retained: ${(error as Error).message}`,{cause:error});}}
 return built;
}
/** Replay a recorded interval from a save; final-tick commands are included, and pause speed is irrelevant. */
export function replayInterval(start:SimState,log:readonly LoggedCommand[],endTick:number):SimState {
 if(!Number.isSafeInteger(endTick)||endTick<(start.flow?.tick??0)||log.some((l,i)=>!Number.isSafeInteger(l.tick)||l.tick<0||(i>0&&l.tick<log[i-1].tick)))throw new Error('Invalid or unordered replay interval');
 const st=loadState(start);st.speed=1;let i=0;const entries=log.filter(l=>l.tick>=st.flow!.tick&&l.tick<=endTick);
 while(st.flow!.tick<=endTick){const tick=st.flow!.tick,commands:Command[]=[];while(i<entries.length&&entries[i].tick===tick)commands.push(entries[i++].c);if(commands.length)applyCommands(st,commands);if(tick===endTick)break;
  const next=Math.min(endTick,entries[i]?.tick??endTick),ticks=next-tick;st.speed=1;advanceFlow(st,ticks/20,[],Math.ceil(ticks/20)+1);
 }if(i!==entries.length)throw new Error('Unordered replay log');return st;
}
export function savedContinuation(st:SimState,seconds=2):{before:string;after:string;same:boolean} {
 const a=structuredClone(st),b=loadState(st);a.speed=b.speed=1;const before=stateHash(st);advanceFlow(a,seconds,[],Math.ceil(seconds)+1);advanceFlow(b,seconds,[],Math.ceil(seconds)+1);return {before,after:stateHash(a),same:stateHash(a)===stateHash(b)};
}
