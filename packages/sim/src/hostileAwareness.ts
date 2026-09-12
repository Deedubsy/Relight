import type {SimState} from './types';
import type {Crawler,ThreatState} from './threat';
import {citySight} from './ground';
export const HOSTILE_MEMORY_SECONDS=6;
/** A hit supplies the firing position once, never the shooter's subsequent position. */
export function rememberShot(st:SimState,T:ThreatState,c:Crawler,origin:[number,number]):void {
 c.lastKnown={x:origin[0],y:origin[1],until:st.t+HOSTILE_MEMORY_SECONDS};
 c.onPlayer=true;
 if(c.gp)for(const other of T.crawlers)if(other!==c&&other.gp?.source===c.gp.source&&other.gp.squad===c.gp.squad&&Math.hypot(other.x-c.x,other.y-c.y)<=6){other.lastKnown={...c.lastKnown};other.onPlayer=true;}
}
/** Refresh only from actual sight within perception range; otherwise investigate a saved point briefly. */
export function observePlayer(st:SimState,c:Crawler,notice:number,escape:number):void {
 const e=st.engineer;
 if(e.down>=0){delete c.lastKnown;c.onPlayer=false;return;}
 const distance=Math.hypot(e.x-c.x,e.y-c.y),aware=!!c.lastKnown&&c.lastKnown.until>st.t;
 if(distance<=(aware?escape:notice)&&citySight(st,c.x,c.y,e.x,e.y))c.lastKnown={x:e.x,y:e.y,until:st.t+HOSTILE_MEMORY_SECONDS};
 else if(c.lastKnown&&(st.t>=c.lastKnown.until||Math.hypot(c.x-c.lastKnown.x,c.y-c.lastKnown.y)<.5))delete c.lastKnown;
 c.onPlayer=!!c.lastKnown;
}
