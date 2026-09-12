import {inFreightGate} from './city/gameplaySites';
import type {SimState} from './types';
import type {Target,ThreatState} from './threat';
import {citySight,ground,inGround} from './ground';
import {litAt,machineAt} from './flow';
import {activeWeapon} from './equipment';
import {WEAPON_PROFILES,weaponDamage,type WeaponKind} from './weaponProfiles';
import {stalkersOf} from './stalker';
export interface PlayerProjectile {kind:'plasma';x:number;y:number;ux:number;uy:number;distance:number;origin:[number,number]}
/** One resolved shot for the renderer: muzzle, end point (the body hit or where the ray stopped) and the sim time. */
export interface PlayerShot {x0:number;y0:number;x1:number;y1:number;t:number;hit:boolean}
export const SHOT_TRACE_S=.5;
function noteShot(T:ThreatState,t:number,x0:number,y0:number,x1:number,y1:number,hit:boolean):void {
 (T.playerShots??=[]).push({x0,y0,x1,y1,t,hit});if(T.playerShots.length>24)T.playerShots.splice(0,T.playerShots.length-24);
}
type Impact=(target:Target,damage:number,origin:[number,number])=>void;
function bodies(st:SimState,T:ThreatState):Target[]{return [...T.crawlers,...stalkersOf(st)];}
/** Stop each ray at solid city geometry, then select the first body along it. */
function cast(st:SimState,T:ThreatState,x:number,y:number,ux:number,uy:number,length:number,radius:number):{target:Target|null;distance:number} {
 let limit=length;const G=ground(st);
 for(let d=Math.min(.2,length);d<=length+1e-8;d=Math.min(length,d+.2)){
  const nx=x+ux*d,ny=y+uy*d,tx=Math.floor(nx),ty=Math.floor(ny),m=machineAt(st,tx,ty);if(!inGround(G,tx,ty)||st.campaign?.progression?.gameplay?.region&&!st.campaign.progression.gameplay.strongholds.freight.opened&&inFreightGate(tx,ty)||G.urban?.solid[ty*G.tw+tx]||m&&(m.kind==='wall'||m.kind==='barricade')&&m.hp!==0||!citySight(st,x,y,nx,ny)){limit=Math.max(0,d-.2);break;}if(d>=length)break;
 }
 let target:Target|null=null,best=limit;
 for(const c of bodies(st,T)){
  const dx=c.x-x,dy=c.y-y,along=dx*ux+dy*uy;
  if(along<0||along>best||Math.abs(dx*uy-dy*ux)>radius||!citySight(st,x,y,c.x,c.y)||c.kind==='shade'&&!litAt(st,Math.floor(c.x),Math.floor(c.y)))continue;
  target=c;best=along;
 }
 return {target,distance:target?best:limit};
}
export function firePlayerWeapon(st:SimState,T:ThreatState,ax:number,ay:number,impact:Impact):void {
 const e=st.engineer,kind:WeaponKind=activeWeapon(st)?.kind??(e.barrels===2?'double':'rifle'),p=WEAPON_PROFILES[kind];
 const angle=Math.atan2(ay-e.y,ax-e.x),origin:[number,number]=[e.x,e.y];
 if(Math.hypot(ax-e.x,ay-e.y)<1e-6)return;
 if(kind==='plasma'){
  (T.playerProjectiles??=[]).push({kind,x:e.x,y:e.y,ux:Math.cos(angle),uy:Math.sin(angle),distance:0,origin});return;
 }
 for(let i=0;i<p.pellets;i++){
  const a=angle+(p.pellets===1?0:(i/(p.pellets-1)-.5)*p.spread),ux=Math.cos(a),uy=Math.sin(a),ray=cast(st,T,e.x,e.y,ux,uy,p.max,p.radius);
  noteShot(T,st.t,e.x,e.y,ray.target?ray.target.x:e.x+ux*ray.distance,ray.target?ray.target.y:e.y+uy*ray.distance,!!ray.target);
  if(ray.target){const damage=weaponDamage(kind,Math.hypot(ray.target.x-e.x,ray.target.y-e.y));if(damage>0)impact(ray.target,damage,origin);}
 }
}
export function tickPlayerProjectiles(st:SimState,T:ThreatState,dt:number,impact:Impact):void {
 T.playerProjectiles=T.playerProjectiles?.filter(p=>{
  const def=WEAPON_PROFILES[p.kind],travel=Math.min(def.max-p.distance,def.speed*dt);
  // Swept segments prevent tunnelling, including a body whose centre crosses the bolt between ticks.
  const steps=Math.max(1,Math.ceil(travel/.2));
  for(let i=0;i<steps;i++){
   const length=travel/steps,ray=cast(st,T,p.x,p.y,p.ux,p.uy,length,def.radius);
   if(ray.target){noteShot(T,st.t,p.x,p.y,ray.target.x,ray.target.y,true);impact(ray.target,def.damage,p.origin);return false;}
   if(ray.distance<length-1e-8)return false;
   p.x+=p.ux*length;p.y+=p.uy*length;p.distance+=length;
  }
  return p.distance<def.max-1e-8;
 });
}
