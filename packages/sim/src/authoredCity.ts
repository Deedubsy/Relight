import {doorOutside} from './city/parcelGeometry';
import type {SimState} from './types';
import {RIVERFRONT,RIVERFRONT_ID,RIVERFRONT_BUILDINGS,RIVERFRONT_PROPS} from './city/riverfront';
import {riverfrontRail} from './city/riverfrontRail';
import {inReach,invalidateGround,citySight} from './ground';
export function cityProp(st:SimState,x:number,y:number){return st.city?.mapId===RIVERFRONT_ID?RIVERFRONT_PROPS.find(p=>(p.clearable||p.kind==='gate')&&x>=p.x&&x<p.x+p.w&&y>=p.y&&y<p.y+p.h&&!st.campaign?.authored?.cleared.includes(p.id)&&!st.campaign?.authored?.opened.includes(p.id)):undefined;}
export function cityPropReason(st:SimState,id:string):string {const p=RIVERFRONT_PROPS.find(p=>p.id===id);if(!p||!cityProp(st,p.x,p.y))return 'Already clear';if(!inReach(st,p.x,p.y,p.w,p.h))return 'Walk closer';if(p.kind==='gate'&&st.engineer.x<p.x+2)return 'Barred from this side; reach the rear alley';return '';}
export function cityCommand(st:SimState,id:string):void {if(cityPropReason(st,id))return;const p=RIVERFRONT_PROPS.find(p=>p.id===id)!;st.campaign!.authored![p.kind==='gate'?'opened':'cleared'].push(id);invalidateGround(st);}
export function tickAuthored(st:SimState):void {const a=st.campaign?.authored;if(!a)return;for(const b of RIVERFRONT_BUILDINGS)if(b.enterable&&st.engineer.x>b.x+1&&st.engineer.x<b.x+b.w-1&&st.engineer.y>b.y+1&&st.engineer.y<b.y+b.h-1&&!a.visited.includes(b.id))a.visited.push(b.id);}
/** Interior objects are not visible through a neighbouring roof or solid walls. */
export function cityVisible(st:SimState,x:number,y:number):boolean {if(!st.city?.mapId)return true;const b=RIVERFRONT_BUILDINGS.find(b=>x>b.x&&x<b.x+b.w&&y>b.y&&y<b.y+b.h);return !b||!!b.enterable&&citySight(st,st.engineer.x,st.engineer.y,x,y);}

/** Side loading tiles accept logistics; track and the north boarding strip stay clear. */
export function cityRailReserved(st:SimState,x:number,y:number,kind=''):boolean {
 if(!st.city?.mapId)return false;const rail=riverfrontRail(),t=y*RIVERFRONT.width+x;
 if(rail.index.has(t))return true;
 const platform=RIVERFRONT.stops.find(p=>x>=p.x-1&&x<p.x+3&&y>=p.y-1&&y<p.y+3);
 if(platform&&['belt','fastbelt','inserter'].includes(kind)&&(x===platform.x-1||x===platform.x+2)&&y>=platform.y&&y<platform.y+2)return false;
 return !!platform||rail.reserved.has(t);
}
export function authoredProblem(st:SimState):string {if(!st.city?.mapId)return st.campaign?.authored?'Authored state has no map ID':'';if(st.city.mapId!==RIVERFRONT_ID)return 'This earlier authored city needs its original build. Original save preserved; choose New Game for Riverfront v4';if(st.city.tw!==RIVERFRONT.width||st.city.th!==RIVERFRONT.height||st.flow?.tw!==RIVERFRONT.width)return 'Authored city dimensions differ';const a=st.campaign?.authored;if(!a||a.version!==4)return 'Missing authored city progress';for(const k of ['cleared','opened','visited'] as const){if(!Array.isArray(a[k])||new Set(a[k]).size!==a[k].length||a[k].some(id=>k==='visited'?!RIVERFRONT_BUILDINGS.some(b=>b.id===id&&b.enterable):!RIVERFRONT_PROPS.some(p=>p.id===id&&(k==='opened'?p.kind==='gate':p.clearable))))return 'Invalid authored obstacle progress';}
 const net=st.campaign?.fixedTram,route=riverfrontRail().tiles;if(!net||net.route.length!==route.length||net.route.some((t,i)=>t!==route[i])||RIVERFRONT.stops.some((p,i)=>{const m=st.flow?.machines.find(m=>m.id===net.stops[i]);return !m||m.x!==p.x||m.y!==p.y;}))return 'Authored tram binding mismatch';
 for(const s of [...RIVERFRONT.plants,...RIVERFRONT.cores,...RIVERFRONT.artifacts]){const actual=st.campaign?.progression?.sites.find(p=>p.id===s.id);if(!actual||actual.x!==s.x||actual.y!==s.y)return 'Authored site binding mismatch';}return '';}

/** Player tracking uses the public door of an enterable parcel, never its blocked centre. */
export function cityApproach(st:SimState,x:number,y:number):{x:number;y:number}{const b=st.city?.mapId?RIVERFRONT_BUILDINGS.find(b=>x>b.x&&x<b.x+b.w&&y>b.y&&y<b.y+b.h):undefined;return b?.enterable&&b.door?doorOutside(b):{x,y};}
