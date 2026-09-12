import {cityStep} from './cityNavigation';
/** RI-04: the tile-step helpers M4's crawlers use (threat.ts), shared with the Stalker (stalker.ts). Moved here
 *  unchanged but for `dir`: a body that carries a `dir` records the unit direction of its last move, so the world
 *  view can show where it is heading (plan §7: "direction and current target are inspectable"). */
import { SimState } from './types';
import { Ground, inGround } from './ground';
import { passable } from './walk';

export const NX = [0, 1, 0, -1, 1, 1, -1, -1], NY = [-1, 0, 1, 0, -1, 1, 1, -1], NC = [10, 10, 10, 10, 14, 14, 14, 14];
/** Anything that walks the tiles: a crawler, a shade, a Stalker. */
export interface Body { x: number; y: number; dir?: [number, number] }

/** Move `step` tiles toward (gx, gy), arriving exactly when it is within a step. */
export function moveTo(c: Body, gx: number, gy: number, step: number): void {
  const dx = gx - c.x, dy = gy - c.y, L = Math.hypot(dx, dy);
  if (L > 1e-9 && c.dir) { c.dir[0] = dx / L; c.dir[1] = dy / L; }
  if (L <= step) { c.x = gx; c.y = gy; } else { c.x += dx / L * step; c.y += dy / L * step; }
}
/** Greedy step toward a point over passable tiles, 8-connected without corner cutting (the chase, and the
 *  off-field fallback); false when no neighbouring tile is closer. */
export function stepToward(st: SimState, G: Ground, c: Body, gx: number, gy: number, step: number, bias?:(x:number,y:number)=>number): boolean {
  if(st.city?.mapId)return cityStep(st,c,gx,gy,step,bias);
  const ctx = Math.floor(c.x), cty = Math.floor(c.y);
  const initial=Math.hypot(gx-c.x,gy-c.y);let best=-1,bd=bias?Infinity:initial;
  if (Math.floor(gx) === ctx && Math.floor(gy) === cty) { moveTo(c, gx, gy, step); return true; }
  for (let k = 0; k < 8; k++) {
    const xx = ctx + NX[k], yy = cty + NY[k];
    if (!inGround(G, xx, yy) || !passable(st, xx, yy)) continue;
    if (k >= 4 && !(passable(st, ctx + NX[k], cty) && passable(st, ctx, cty + NY[k]))) continue;
    const d = Math.hypot(gx - xx - 0.5, gy - yy - 0.5);
    const score=d+(bias?.(xx,yy)??0);if(d<initial&&score<bd){bd=score;best=k;}
  }
  if (best < 0) return false;
  moveTo(c, ctx + NX[best] + 0.5, cty + NY[best] + 0.5, step);
  return true;
}
/** The 8-point compass word for a direction (screen axes: y grows south), '·' for none. */
export function headingWord(d: [number, number] | undefined): string {
  if (!d || Math.hypot(d[0], d[1]) < 1e-6) return '·';
  const k = Math.round(Math.atan2(d[1], d[0]) / (Math.PI / 4)) & 7;   // 0 E, 1 SE, 2 S, 3 SW, 4 W, 5 NW, 6 N, 7 NE
  return ['E', 'SE', 'S', 'SW', 'W', 'NW', 'N', 'NE'][k];
}
