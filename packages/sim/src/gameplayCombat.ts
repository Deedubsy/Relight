import {observePlayer} from './hostileAwareness';
import {WEAPON_PROFILES} from './weaponProfiles';
/** First-region combat. Attacks commit to an aim point; cover, retreat and dodge remain useful. */
import type {SimState} from './types';
import type {Crawler,ThreatState} from './threat';
import {ground,citySight} from './ground';
import {passable,canStand} from './walk';
import {stepToward} from './move';
import {hurt} from './engineer';
import {litAt,machineAt} from './flow';
import {damageDefence,defenceMax,coreAt,damageCore} from './campaignDefence';

export const GP_COMBAT={skitter:{hp:20,speed:5.4,damage:5,interval:1,windup:.3,range:1.3},
  spitter:{hp:50,speed:3.9,damage:10,interval:2,windup:.6,range:7},
  guardian:{hp:600,speed:2.7,damage:25,interval:4,windup:1.2,range:9},
  projectileSpeed:8,projectileLife:2,notice:8,escape:18,alertRadius:6,hesitate:.65} as const;
export type CombatKind='skitter'|'spitter'|'guardian';
export interface CombatActor {
  kind:CombatKind; source:string; squad:number; home:[number,number];
  phase:'idle'|'windup'|'charge'|'recover'; until:number; aim:[number,number];
  hesitate:number; patrol:number; alertUntil:number;
}
export interface SpitProjectile {x:number;y:number;vx:number;vy:number;life:number;source:number}
export function newCombatActor(kind:CombatKind,source:string,squad:number,x:number,y:number):CombatActor {
  return {kind,source,squad,home:[x,y],phase:'idle',until:0,aim:[x,y],hesitate:0,patrol:0,alertUntil:0};
}
/** Swept segments use the same solids and LOS as actors, never projectile endpoint-only collision. */
export function tickSpit(st:SimState,T:ThreatState,dt:number):void {
  if(!T.projectiles)return;
  const e=st.engineer;
  T.projectiles=T.projectiles.filter(p=>{
    const n=Math.max(1,Math.ceil(Math.hypot(p.vx,p.vy)*dt/.2));
    for(let i=0;i<n;i++){
      const x=p.x+p.vx*dt/n,y=p.y+p.vy*dt/n;
      if(!citySight(st,p.x,p.y,x,y)||!passable(st,Math.floor(x),Math.floor(y))){
        const m=machineAt(st,Math.floor(x),Math.floor(y)),core=coreAt(st,Math.floor(x),Math.floor(y));if(m&&defenceMax(m))damageDefence(st,m,10);else if(core)damageCore(st,core,10);return false;
      }
      p.x=x;p.y=y;
      const struckCore=coreAt(st,Math.floor(x),Math.floor(y));if(struckCore&&struckCore.hp>0){damageCore(st,struckCore,GP_COMBAT.spitter.damage);return false;}
      if(e.down<0&&e.dash<=0&&Math.hypot(e.x-x,e.y-y)<.5){hurt(st,10);return false;}
    }
    p.life-=dt;return p.life>0;
  });
}
/** Same saved windup/projectile contract for committed base attackers; existing breach paths remain authoritative. */
export function tickRaidAttack(st:SimState,T:ThreatState,c:Crawler,dt:number,x:number,y:number,hit:(n:number)=>void,contact:boolean):boolean {
  const a=c.gp!,def=GP_COMBAT[a.kind],sight=citySight(st,c.x,c.y,x,y);
  if(a.phase==='recover'){if(st.t<a.until)return true;a.phase='idle';}
  if(a.phase==='windup'){
    if(st.t<a.until)return true;
    if(a.kind==='spitter'){const dx=a.aim[0]-c.x,dy=a.aim[1]-c.y,len=Math.hypot(dx,dy)||1;(T.projectiles??=[]).push({x:c.x,y:c.y,vx:dx/len*8,vy:dy/len*8,life:2,source:c.id});}
    else if(contact&&sight&&Math.hypot(x-a.aim[0],y-a.aim[1])<1.5)hit(def.damage);
    a.phase='recover';a.until=st.t+def.interval-def.windup;return true;
  }
  if(sight&&(contact||a.kind==='spitter'&&Math.hypot(x-c.x,y-c.y)<def.range)){a.phase='windup';a.until=st.t+def.windup;a.aim=[x,y];return true;}
  return false;
}
/** Site groups alert only their own nearby squad; no reinforcements or global camp wake. */
export function tickCombatActor(st:SimState,T:ThreatState,c:Crawler,dt:number):void {
  const a=c.gp!,def=GP_COMBAT[a.kind],e=st.engineer,G=ground(st),d=Math.hypot(e.x-c.x,e.y-c.y);
  if(d>60&&a.phase==='idle'&&!c.onPlayer)return; // distant saved residents do not consume path searches
  const sight=e.down<0&&citySight(st,c.x,c.y,e.x,e.y);
  observePlayer(st,c,GP_COMBAT.notice,GP_COMBAT.escape);
  const known=c.lastKnown,engaged=!!known&&Math.hypot(known.x-a.home[0],known.y-a.home[1])<36;
  if(!engaged){delete c.lastKnown;c.onPlayer=false;}
  if(engaged&&sight&&d<=GP_COMBAT.escape)for(const other of T.crawlers)if(other.gp?.source===a.source&&other.gp.squad===a.squad&&Math.hypot(other.x-c.x,other.y-c.y)<=GP_COMBAT.alertRadius){other.lastKnown={...known};other.onPlayer=true;}
  if(a.phase==='charge'){
    const dx=a.aim[0]-c.x,dy=a.aim[1]-c.y,len=Math.hypot(dx,dy),travel=Math.min(len,13*dt),n=Math.max(1,Math.ceil(travel/.2));
    for(let i=0;i<n&&len>0;i++){
      const x=c.x+dx/len*travel/n,y=c.y+dy/len*travel/n;
      if(!canStand(st,x,y,.45)||!citySight(st,c.x,c.y,x,y)){a.phase='recover';a.until=st.t+2;break;}
      c.x=x;c.y=y;c.dir=[dx/len,dy/len];
      if(e.down<0&&e.dash<=0&&Math.hypot(e.x-x,e.y-y)<1){hurt(st,def.damage);a.phase='recover';a.until=st.t+2;break;}
    }
    if(st.t>=a.until||Math.hypot(c.x-a.aim[0],c.y-a.aim[1])<.2){a.phase='recover';a.until=st.t+2;}
    return;
  }
  if(a.phase==='recover'){if(st.t<a.until)return;a.phase='idle';}
  if(a.phase==='windup'){
    if(st.t<a.until)return;
    if(a.kind==='guardian'){a.phase='charge';a.until=st.t+.9;return;}
    if(a.kind==='spitter'){
      const dx=a.aim[0]-c.x,dy=a.aim[1]-c.y,len=Math.hypot(dx,dy)||1;
      (T.projectiles??=[]).push({x:c.x,y:c.y,vx:dx/len*GP_COMBAT.projectileSpeed,vy:dy/len*GP_COMBAT.projectileSpeed,life:GP_COMBAT.projectileLife,source:c.id});
    }else if(sight&&d<=def.range&&e.dash<=0)hurt(st,def.damage);
    a.phase='recover';a.until=st.t+def.interval-def.windup;return;
  }
  if(engaged){
    a.hesitate=0;
    if(sight&&d<=GP_COMBAT.escape&&d<=def.range){a.phase='windup';a.until=st.t+def.windup;a.aim=[e.x,e.y];return;}
    stepToward(st,G,c,known!.x,known!.y,def.speed*dt);return;
  }
  // A short visible pause at illuminated ground, then a bounded soft-cost route. All-lit routes still work.
  const angle=(c.id*7+a.patrol)*Math.PI/2,gx=a.home[0]+Math.cos(angle)*2,gy=a.home[1]+Math.sin(angle)*2;
  if(Math.hypot(c.x-gx,c.y-gy)<.6||c.stuck>2){a.patrol++;c.stuck=0;}
  if(litAt(st,Math.floor(gx),Math.floor(gy))&&a.hesitate<GP_COMBAT.hesitate){a.hesitate+=dt;return;}
  const before=[c.x,c.y];stepToward(st,G,c,gx,gy,def.speed*.35*dt,(x,y)=>litAt(st,x,y)?2:0);
  c.stuck=Math.hypot(c.x-before[0],c.y-before[1])<1e-8?c.stuck+dt:0;
}
export function combatProblem(st:SimState):string {
  const T=st.flow?.threat;if(!T)return '';
  const finite=(x:unknown):x is number=>typeof x==='number'&&Number.isFinite(x)&&x>=0;
  const point=(p:unknown):p is [number,number]=>Array.isArray(p)&&p.length===2&&p.every(finite)&&p[0]<864&&p[1]<576;
  for(const c of T.crawlers)if(c.lastKnown&&(!point([c.lastKnown.x,c.lastKnown.y])||!finite(c.lastKnown.until)||c.lastKnown.until>st.t+6.001))return 'Invalid hostile last-known position';
  for(const c of T.crawlers)if(c.gp){const a=c.gp;
    if(!Object.hasOwn(GP_COMBAT,a.kind)||!['skitter','spitter','guardian'].includes(a.kind)||typeof a.source!=='string'||!Number.isSafeInteger(a.squad)||a.squad<0||!point(a.home)||!point(a.aim)||
      !['idle','windup','charge','recover'].includes(a.phase)||![a.until,a.hesitate,a.patrol,a.alertUntil].every(finite)||c.hp<=0||c.hp>GP_COMBAT[a.kind].hp)return 'Invalid first-region combat state';
  }
  if(T.projectiles!==undefined&&(!Array.isArray(T.projectiles)||T.projectiles.length>256||T.projectiles.some(p=>!point([p.x,p.y])||![p.vx,p.vy].every(Number.isFinite)||Math.abs(Math.hypot(p.vx,p.vy)-GP_COMBAT.projectileSpeed)>.001||!finite(p.life)||p.life>GP_COMBAT.projectileLife||!Number.isSafeInteger(p.source)||p.source<1||p.source>=T.next)))return 'Invalid alien projectile';
  if(T.playerProjectiles!==undefined&&(!Array.isArray(T.playerProjectiles)||T.playerProjectiles.length>64||T.playerProjectiles.some(p=>p.kind!=='plasma'||!point([p.x,p.y])||!point(p.origin)||![p.ux,p.uy].every(Number.isFinite)||Math.abs(Math.hypot(p.ux,p.uy)-1)>.001||!finite(p.distance)||p.distance>=WEAPON_PROFILES.plasma.max||Math.hypot(p.x-p.origin[0]-p.ux*p.distance,p.y-p.origin[1]-p.uy*p.distance)>.001)))return 'Invalid player projectile';
  return '';
}
