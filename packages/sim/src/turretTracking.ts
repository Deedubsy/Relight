import type {SimState} from './types';
import type {Machine} from './flow';
import {TURRET_ROUNDS_PER_S} from './recipes';
export const CAMPAIGN_TURRET_RATE=1;   // GP-PLAYTEST-FIX 2 (2026-09-11): one bullet per second, down from 3.5, so each shot reads
export const TURRET_TURN_SPEED=2*Math.PI;
export const TURRET_MUZZLE=1;
export const TURRET_SHOT_FLASH=.08;
/** `shot` is the last round's real target and muzzle angle; `t` (sim seconds) lets the renderer trace it briefly. */
export interface TurretTracking {angle:number;target?:number;shot?:{x:number;y:number;angle:number;t?:number}}
export const turretFireRate=(st:SimState)=>st.campaign?CAMPAIGN_TURRET_RATE:TURRET_ROUNDS_PER_S;
export const angleDifference=(to:number,from:number)=>Math.atan2(Math.sin(to-from),Math.cos(to-from));
/** Shortest-path rotation; the base's placement direction never changes. */
export function aimTurret(m:Machine,x:number,y:number,dt:number):boolean {
 const a=m.turret??={angle:(m.dir-1)*Math.PI/2},desired=Math.atan2(y-m.y-m.size/2,x-m.x-m.size/2);
 const delta=angleDifference(desired,a.angle),step=Math.min(Math.abs(delta),TURRET_TURN_SPEED*dt);
 a.angle=Math.atan2(Math.sin(a.angle+Math.sign(delta)*step),Math.cos(a.angle+Math.sign(delta)*step));
 return Math.abs(angleDifference(desired,a.angle))<1e-6;
}
