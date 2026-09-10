/** Half-open footprints: touching a reservation boundary is permitted; penetration is not. */
export interface Rect {x:number;y:number;w:number;h:number}
export type Facing='N'|'S'|'E'|'W';
export const outward:Record<Facing,readonly [number,number]>={N:[0,-1],S:[0,1],E:[1,0],W:[-1,0]};
export function overlaps(a:Rect,b:Rect):boolean{return a.x<b.x+b.w&&a.x+a.w>b.x&&a.y<b.y+b.h&&a.y+a.h>b.y;}
/** Orthogonal full segment including its junction cap; deliberately conservative square caps. */
export function corridorRect(a:readonly number[],b:readonly number[],radius:number):Rect{return{x:Math.min(a[0],b[0])-radius,y:Math.min(a[1],b[1])-radius,w:Math.abs(a[0]-b[0])+2*radius,h:Math.abs(a[1]-b[1])+2*radius};}
export function doorRect(b:Rect&{facing:Facing;door?:[number,number]}):Rect {const side=b.facing==='E'||b.facing==='W',fallback:[number,number]=side?[b.facing==='E'?b.x+b.w-1:b.x,b.y+Math.floor(b.h/2)-1]:[b.x+Math.floor(b.w/2)-1,b.facing==='S'?b.y+b.h-1:b.y];const[x,y]=b.door??fallback;return{x,y,w:side?1:3,h:side?3:1};}
export function doorOutside(b:Rect&{facing:Facing;door?:[number,number]}):{x:number;y:number}{const d=doorRect(b),[dx,dy]=outward[b.facing];return{x:d.x+d.w/2+dx,y:d.y+d.h/2+dy};}
