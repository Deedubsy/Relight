/** P5-04: one physical campaign truck, separate cargo and ordinary command operations. */
import type { SimState } from './types';
import { ground, walkable, distToRect, blockOfTile } from './ground';
import { machineAt, isItem, isKind, type Dir } from './flow';
import { INV_STACKS, TRUCK_STACKS, TRUCK_MULT, WALK_TILES_PER_S, invStacks } from './engineer';
import { T_STREET } from './tiles';
import { passable } from './walk';
export const TRUCK_RULES={width:2,length:3,stacks:TRUCK_STACKS,speedMultiplier:TRUCK_MULT,boardReach:1.5,parkingRadius:24,substep:.2} as const;
export interface TruckState { x:number;y:number;dir:Dir;cargo:Record<string,number>;unlockedAt:number }
export function truckRect(t:Pick<TruckState,'x'|'y'|'dir'>){const w=t.dir%2?3:2,h=t.dir%2?2:3;return {x:t.x-w/2,y:t.y-h/2,w,h};}
export function truckOccupies(st:SimState,x:number,y:number):boolean {
 const t=st.campaign?.truck;if(!t)return false;const r=truckRect(t);
 return x+1>r.x+1e-8&&y+1>r.y+1e-8&&x<r.x+r.w-1e-8&&y<r.y+r.h-1e-8;
}
export function truckFits(st:SimState,x:number,y:number,dir:Dir):boolean {
 const G=ground(st),r=truckRect({x,y,dir});
 for(let ty=Math.floor(r.y+1e-8);ty<Math.ceil(r.y+r.h-1e-8);ty++)for(let tx=Math.floor(r.x+1e-8);tx<Math.ceil(r.x+r.w-1e-8);tx++){
  if(!walkable(G,tx,ty)||G.base[ty*G.tw+tx]!==T_STREET)return false;
  const m=machineAt(st,tx,ty);if(m&&!['belt','track','tram'].includes(m.kind)&&!(m.kind==='wall'&&m.hp===0))return false;
 }
 return true;
}
const parkingAttempts=new WeakMap<SimState,{rev:number;x:number;y:number}>();
export function initTruck(st:SimState):void {
 const c=st.campaign,s=c?.expansion?.station;if(!c||!s||s.restoredAt<0||c.truck)return;
 const prior=parkingAttempts.get(st),rev=st.flow!.rev,{x,y}=st.engineer;
 if(prior?.rev===rev&&prior.x===x&&prior.y===y)return;parkingAttempts.set(st,{rev,x,y});
 let best:TruckState|undefined,bd=Infinity;
 for(let dy=-TRUCK_RULES.parkingRadius;dy<=TRUCK_RULES.parkingRadius;dy++)for(let dx=-TRUCK_RULES.parkingRadius;dx<=TRUCK_RULES.parkingRadius;dx++)for(const dir of [0,1] as const){
  const x=s.x+dx+(dir?1.5:1),y=s.y+dy+(dir?1:1.5),d=(x-s.x-s.size/2)**2+(y-s.y-s.size/2)**2;
  if(d>=bd||!truckFits(st,x,y,dir))continue;
  const t={x,y,dir,cargo:{},unlockedAt:st.t};
  const r=truckRect(t);if(distToRect(st.engineer.x,st.engineer.y,r.x,r.y,r.w,r.h)===0)continue;
  // Keep the surveyed kit corridor and its two platforms available when the reward is collected.
  if(c.expansion!.route.some(tile=>{const tx=tile%st.flow!.tw,ty=Math.floor(tile/st.flow!.tw);return tx+1>r.x&&ty+1>r.y&&tx<r.x+r.w&&ty<r.y+r.h;})||c.expansion!.stops.some(([tx,ty])=>tx+2>r.x&&ty+2>r.y&&tx<r.x+r.w&&ty<r.y+r.h))continue;
  if(!exitTile(st,t))continue;best=t;bd=d;
 }
 if(best){c.truck=best;const e=st.engineer;if(e.truck){best.cargo=e.inv;e.inv={};e.truck=false;}e.truckFound=true;st.flow!.rev++;}
}
function exitTile(st:SimState,t:TruckState):[number,number]|null {
 const r=truckRect(t);let best:[number,number]|null=null,bd=Infinity;
 for(let y=Math.floor(r.y)-1;y<=Math.ceil(r.y+r.h);y++)for(let x=Math.floor(r.x)-1;x<=Math.ceil(r.x+r.w);x++){
  const cx=x+.5,cy=y+.5;if(cx>=r.x&&cx<=r.x+r.w&&cy>=r.y&&cy<=r.y+r.h)continue;
  if(!passable(st,x,y))continue;const d=(cx-t.x)**2+(cy-t.y)**2;if(d<bd){bd=d;best=[cx,cy];}
 }
 return best;
}
export function truckBoardCheck(st:SimState):string {
 const t=st.campaign?.truck,e=st.engineer;if(!t)return 'Restore the second-area station; clear nearby street space for the truck';
 if(e.down>=0)return 'Wait until you recover';
 if(e.truckSeat)return exitTile(st,t)?'':'No clear exit beside the truck';
 const r=truckRect(t);return distToRect(e.x,e.y,r.x,r.y,r.w,r.h)>TRUCK_RULES.boardReach?'Walk beside the truck to board':'';
}
export function boardTruck(st:SimState):string {
 const why=truckBoardCheck(st);if(why)return why;const e=st.engineer,t=st.campaign!.truck!;
 if(e.truckSeat){const p=exitTile(st,t)!;e.x=p[0];e.y=p[1];e.truck=false;delete e.truckSeat;}
 else{e.x=t.x;e.y=t.y;e.truck=true;e.truckSeat=true;}
 e.vel=[0,0];e.target=null;e.dest=-1;e.remaining=0;e.dash=0;e.sprint=false;e.block=blockOfTile(st,e.x,e.y);st.flow!.rev++;return '';
}
/** A vehicle never delegates to the engineer's point-sized auto-walk path or teleport fallback. */
export function driveTruck(st:SimState,dt:number):void {
 const e=st.engineer,t=st.campaign?.truck;if(!t||!e.truckSeat)return;
 e.target=null;e.dest=-1;e.dash=0;e.sprint=false;
 const [vx,vy]=e.vel,L=Math.hypot(vx,vy);if(!L)return;
 const dir:Dir=Math.abs(vx)>=Math.abs(vy)?(vx>0?1:3):(vy>0?2:0);
 if(!truckFits(st,t.x,t.y,dir))return;
 t.dir=dir;const distance=WALK_TILES_PER_S*TRUCK_MULT*dt,n=Math.ceil(distance/TRUCK_RULES.substep);let moved=false;
 for(let i=0;i<n;i++){
  const dx=vx/L*distance/n,dy=vy/L*distance/n;
  if(truckFits(st,t.x+dx,t.y,dir)){t.x+=dx;moved||=dx!==0;}
  if(truckFits(st,t.x,t.y+dy,dir)){t.y+=dy;moved||=dy!==0;}
 }
 e.x=t.x;e.y=t.y;e.face=[vx/L,vy/L];e.block=blockOfTile(st,e.x,e.y);
 if(moved){e.walked+=dt;const h=Math.floor(st.t/3600);e.walkedHour[h]=(e.walkedHour[h]??0)+dt;}
}
const cargoItem=(k:string)=>isItem(k)||(isKind(k)&&k!=='depot')||k==='kit';
export function truckTransfer(st:SimState,item:string,n:number,put:boolean):{ok:boolean;reason:string;moved?:number}{
 const t=st.campaign?.truck,e=st.engineer;
 if(!t||e.down>=0||!cargoItem(item)||!Number.isSafeInteger(n)||n<=0||typeof put!=='boolean')return {ok:false,reason:'Invalid truck cargo transfer'};
 const r=truckRect(t);if(distToRect(e.x,e.y,r.x,r.y,r.w,r.h)>e.reach)return {ok:false,reason:'Walk closer to the truck cargo'};
 const src=put?e.inv:t.cargo,dst=put?t.cargo:e.inv,cap=put?TRUCK_STACKS:INV_STACKS;
 const amount=Math.min(n,src[item]??0);let moved=0,hi=Math.floor(amount);
 // Monotonic capacity search includes partial stacks and keeps oversized requests bounded.
 while(moved<hi){const mid=Math.ceil((moved+hi)/2);if(invStacks({...dst,[item]:(dst[item]??0)+mid})<=cap)moved=mid;else hi=mid-1;}
 if(invStacks({...dst,[item]:(dst[item]??0)+amount})<=cap)moved=amount;
 if(!moved)return {ok:false,reason:amount?'Destination inventory is full':'No cargo of that item to transfer'};
 src[item]-=moved;if(src[item]<=0)delete src[item];dst[item]=(dst[item]??0)+moved;return {ok:true,reason:`${moved} ${item} moved ${put?'into truck cargo':'to pockets'}`,moved};
}
export function truckDescription(st:SimState):string {
 const t=st.campaign?.truck;return !t?'Truck: restore the second-area station; parking requires clear street space':`${st.engineer.truckSeat?'Driving':'Parked'} truck · ${invStacks(t.cargo)}/${TRUCK_STACKS} cargo stacks · WASD drives, E boards/exits; cargo stays with the truck`;
}
export function truckProblem(st:SimState):string {
 const t=st.campaign?.truck,e=st.engineer;if(!t)return e.truckSeat?'missing occupied truck':'';
 if(!st.campaign?.expansion||st.campaign.expansion.station.restoredAt<0)return 'truck before station restoration';
 if(e.truckSeat!==undefined&&e.truckSeat!==true)return 'invalid truck occupant';
 if(!Number.isFinite(t.x)||!Number.isFinite(t.y)||![0,1,2,3].includes(t.dir)||!Number.isFinite(t.unlockedAt)||t.unlockedAt<0||!t.cargo||typeof t.cargo!=='object'||Array.isArray(t.cargo)||Object.entries(t.cargo).some(([k,n])=>!cargoItem(k)||!Number.isFinite(n)||n<0||(!isItem(k)&&!Number.isSafeInteger(n)))||invStacks(t.cargo)>TRUCK_STACKS)return 'invalid truck state or cargo';
 if(!truckFits(st,t.x,t.y,t.dir))return 'invalid truck footprint';
 if(e.truckSeat&&(!e.truck||e.down>=0||Math.hypot(e.x-t.x,e.y-t.y)>1e-6))return 'invalid truck occupant';
 if(e.truck&&!e.truckSeat)return 'invalid truck occupant';
 return '';
}
