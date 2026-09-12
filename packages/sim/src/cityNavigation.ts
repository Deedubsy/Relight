/** Shared authored walking: 3 bounded searches/tick, 8000 nodes/search, .75s target refresh.
 * Plans are transient; f.rev invalidates machinery/cleared props. No failed-route teleport.
 * Queue admission is FIFO. Crowds reserve next tiles and retry rather than overlap.
 */
import type {SimState} from './types';
import {ground} from './ground';
import {findPath,passable} from './walk';
import type {Body} from './move';
export const CITY_NAV={searches:3,nodes:8000,replan:.75,retry:1,largeClearance:1};
interface Plan {path:Int32Array|null;at:number;rev:number;goal:number;time:number;blocked:number}
interface Queue {tick:number;used:number;waiting:Body[];reserved:Map<number,Body>;searches:number;failed:number}
const lastRequest=new WeakMap<Body,number>();
const plans=new WeakMap<Body,Plan>(),queues=new WeakMap<SimState,Queue>();
export function cityNavStats(st:SimState){const q=queues.get(st);return {searches:q?.searches??0,failed:q?.failed??0,waiting:q?.waiting.length??0};}
export function cityStep(st:SimState,c:Body,gx:number,gy:number,step:number,bias?:(x:number,y:number)=>number):boolean {
 const G=ground(st),tw=G.tw,large=(c as Body&{role?:string}).role==='breaker'?1:0,rev=st.flow?.rev??0;
 let q=queues.get(st);if(!q){q={tick:st.flow?.tick??st.t,used:0,waiting:[],reserved:new Map(),searches:0,failed:0};queues.set(st,q);}const tick=st.flow?.tick??st.t;
 if(q.tick!==tick){q.tick=tick;q.used=0;q.reserved.clear();}
 const fits=(x:number,y:number)=>{for(let dy=-large;dy<=large;dy++)for(let dx=-large;dx<=large;dx++)if(!passable(st,x+dx,y+dy))return false;return true;};
 let x=Math.floor(gx),y=Math.floor(gy);if(!fits(x,y)){let best=Infinity;for(let dy=-4;dy<=4;dy++)for(let dx=-4;dx<=4;dx++){const xx=Math.floor(gx)+dx,yy=Math.floor(gy)+dy,d=dx*dx+dy*dy;if(d<best&&fits(xx,yy)){x=xx;y=yy;best=d;}}if(best===Infinity)return false;}
 const goal=y*tw+x;let p=plans.get(c);const stale=!p||p.rev!==rev||p.goal!==goal&&st.t-p.time>=CITY_NAV.replan||!p.path&&st.t-p.time>=CITY_NAV.retry||p.blocked>1||!!bias&&st.t-p.time>=2;
 if(stale){lastRequest.set(c,st.t);q.waiting=q.waiting.filter(b=>st.t-(lastRequest.get(b)??-Infinity)<1);if(!q.waiting.includes(c))q.waiting.push(c);if(q.used>=CITY_NAV.searches||q.waiting[0]!==c)return false;q.waiting.shift();q.used++;q.searches++;const path=findPath(st,Math.floor(c.x),Math.floor(c.y),x,y,CITY_NAV.nodes,large,bias);if(!path)q.failed++;p={path,at:0,rev,goal,time:st.t,blocked:0};plans.set(c,p);}
 if(!p?.path||p.at>=p.path.length)return false;const t=p.path[p.at],tx=t%tw,ty=Math.floor(t/tw),sx=Math.floor(c.x),sy=Math.floor(c.y);
 if(!fits(tx,ty)||tx!==sx&&ty!==sy&&(!fits(tx,sy)||!fits(sx,ty))){p.rev=-1;return false;}
 const occupied=st.flow?.threat?.crawlers.some(other=>other!==c&&Math.hypot(other.x-tx-.5,other.y-ty-.5)<.65);
 if(occupied||q.reserved.has(t)&&q.reserved.get(t)!==c){p.blocked+=step/2;return false;}q.reserved.set(t,c);
 const dx=tx+.5-c.x,dy=ty+.5-c.y,L=Math.hypot(dx,dy),v=Math.min(step,L);if(L>0){c.x+=dx/L*v;c.y+=dy/L*v;if(c.dir)c.dir=[dx/L,dy/L];}if(L<=step)p.at++;p.blocked=0;return true;
}
