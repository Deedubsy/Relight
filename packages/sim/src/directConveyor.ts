import type {SimState} from './types';
import {ITEMS,DX,DY,SHOT,machineAt,nextOf,accepts,giveItem,chestCount,isStoreItem,recipeOf,recipeOutput,type Machine,type FlowState,type Item} from './flow';
import {isConveyor,isRouting,routingEntry,splitterPorts,undergroundMate} from './routing';
/** Rebuilt only for topology changes, not inventory changes. Loops are bounded by visited IDs. */
const cache=new WeakMap<FlowState,{rev:number;n:number;targets:Map<number,Machine[]>}>();
export function conveyorDestinations(st:SimState):Map<number,Machine[]>{
 const f=st.flow!,old=cache.get(f);if(old?.rev===f.rev&&old.n===f.machines.length)return old.targets;
 const targets=new Map<number,Machine[]>();
 for(const start of f.machines.filter(isConveyor)){
  const seen=new Set<number>(),found=new Map<number,Machine>(),todo=[start];
  while(todo.length){const m=todo.pop()!;if(seen.has(m.id))continue;seen.add(m.id);
   if(!isConveyor(m)){found.set(m.id,m);continue;}
   if(m.kind==='splitter'){for(const [x,y] of splitterPorts(m)){const n=machineAt(st,x,y);if(n&&(!isConveyor(n)||n.dir!==(m.dir+2)%4)&&(!isRouting(n)||routingEntry(n,x-DX[m.dir],y-DY[m.dir])))todo.push(n);}}
   else if(m.kind==='underground'&&m.underground!=='output'){const n=undergroundMate(st,m);if(n)todo.push(n);}
   else {const n=nextOf(st,m);if(n)todo.push(n);}
  }
  targets.set(start.id,[...found.values()]);
 }
 cache.set(f,{rev:f.rev,n:f.machines.length,targets});return targets;
}
/** Reserve room for items already travelling to a consumer, so mixed recipe belts do not overfeed one ingredient. */
function roomAtEnd(st:SimState,belt:Machine,item:Item,targets:Map<number,Machine[]>):boolean{
 const ends=targets.get(belt.id)??[];if(!ends.length)return true; // an unfinished line can still buffer items
 return ends.some(end=>{
  const inv={...end.inv};let magazines=0;
  for(const m of st.flow!.machines)if(isConveyor(m)&&targets.get(m.id)?.some(t=>t.id===end.id))for(const carried of m.items){inv[carried.k]=(inv[carried.k]??0)+1;if(carried.k==='magazine')magazines++;}
  if(end.kind==='turret')inv.rounds=(end.inv.rounds??0)+magazines*SHOT.count;
  if(end.kind==='depot'&&item==='magazine')return st.buffer+(magazines+1)*SHOT.count<=st.config.bufferCap;
  return accepts(st,{...end,inv},item);
 });
}
function sourceCount(st:SimState,m:Machine,k:Item):number{
 if(m.kind==='depot')return chestCount(st,k);
 if(['assembler','assembler2','mixer','foundry','refinery'].includes(m.kind))return k===outputOf(m)?m.out:0;
 if((m.kind==='excavator'||m.kind==='pumpjack'))return m.hold===k?1:0;
 if(m.kind==='turret')return k==='magazine'?Math.floor((m.inv.rounds??0)/SHOT.count):0;
 if(m.kind==='tramstop')return (m.cargo?.[k]??0)+(m.inv[k]??0);
 if(m.kind==='chest'||m.kind==='generator'||m.kind==='cannon')return m.inv[k]??0;
 return 0;
}
const outputOf=(m:Machine)=>recipeOutput(recipeOf(m));
function takeSource(st:SimState,m:Machine,k:Item):void{
 if(m.kind==='depot'){if(k==='magazine')st.buffer-=SHOT.count;else if(isStoreItem(k))st.flow!.store[k]=(st.flow!.store[k]??0)-1;else st.stock[k]--;}
 else if(['assembler','assembler2','mixer','foundry','refinery'].includes(m.kind))m.out--;
 else if((m.kind==='excavator'||m.kind==='pumpjack'))m.hold=null;
 else if(m.kind==='turret')m.inv.rounds-=SHOT.count;
 else if(m.kind==='tramstop'&&(m.cargo?.[k]??0)>=1)m.cargo![k]--;
 else m.inv[k]--;
}
/** A conveyor points away from its source. Move one whole item only after reserving actual belt room. */
export function loadConveyor(st:SimState,m:Machine,targets:Map<number,Machine[]>):void{
 if(m.kind==='underground'&&m.underground==='output')return;
 const points=m.kind==='splitter'?splitterPorts(m,false):[[m.x-DX[m.dir],m.y-DY[m.dir]]];
 for(const [x,y] of points){const src=machineAt(st,x,y);if(!src||isConveyor(src)||src.kind==='inserter')continue;
  const start=m.pickupNext??0;
  for(let i=0;i<ITEMS.length;i++){const index=(start+i)%ITEMS.length,k=ITEMS[index];
   if(sourceCount(st,src,k)<1||!accepts(st,m,k)||!roomAtEnd(st,m,k,targets))continue;
   if(!giveItem(st,m,k,0))continue;takeSource(st,src,k);m.pickupNext=(index+1)%ITEMS.length;break;
  }
 }
}
