/** Authoring helpers. Every imported footprint still goes through validateRiverfront. */
import {doorOutside,doorRect,corridorRect,overlaps,outward} from '../../sim/src/city/parcelGeometry';
import type {Parcel,BuildingKind} from '../../sim/src/city/riverfront';
import type {Layout,SceneObject} from './phaserCityModel';

export const protectedBuilding=(b:Parcel,fixed:string[])=>fixed.includes(b.id)||!!(b.enterable||b.compound||b.content||b.note||b.doors?.length);
export function plainImage(o:SceneObject){
 const allowed=new Set(['id','type','label','displayName','scope','useGameObjectName','x','y','scaleX','scaleY','originX','originY','angle','rotation','flipX','flipY','texture','alpha','visible']);
 if(Object.keys(o).some(k=>!allowed.has(k))||o.type!=='Image'||(o.originX??.5)!==0||(o.originY??.5)!==0||(o.angle??0)!==0||(o.rotation??0)!==0||o.flipX||o.flipY||(o.alpha??1)!==1||o.visible===false)throw Error(`${o.label}: use a plain, visible, unrotated roof Image with origin 0,0.`);
}
export function newBuilding(o:SceneObject):Parcel {
 plainImage(o);
 const match=/^rf-(house|terrace|garage|workshop|warehouse|shop|utility|civic|townhall|apartment|office|department|arcade|parking)(?:-([NEW]))?-roof-([012])$/.exec(o.texture?.key??'');
 if(!match||match[2]&&match[1]!=='house')throw Error(`${o.label}: select an existing building roof texture.`);
 const x=Number(o.x??0)/32,y=Number(o.y??0)/32,w=10*Number(o.scaleX??1),h=8*Number(o.scaleY??1);
 if(![x,y,w,h].every(Number.isInteger)||w<4||h<4||w>64||h>64)throw Error(`${o.label}: use whole 32 px tiles, with a building footprint between 4 and 64 tiles per side.`);
 const id='editor:'+o.id,b:Parcel={id,name:o.displayName||o.label,x,y,w,h,kind:match[1] as BuildingKind,facing:(match[2]??'S') as Parcel['facing'],variant:Number(match[3]),visual:{x,y,w,h},parcel:{id:'plot:'+id,x:x-2,y:y-2,w:w+4,h:h+4},path:[]};
 const d=doorRect(b);b.door=[d.x,d.y];return b;
}
export function readPlot(o:SceneObject,b:Parcel):Parcel['parcel'] {
 const allowed=new Set(['id','type','label','displayName','scope','useGameObjectName','x','y','width','height','scaleX','scaleY','originX','originY','angle','rotation','flipX','flipY','fillColor','fillAlpha','isFilled','isStroked','strokeColor','strokeAlpha','lineWidth','visible','alpha']);
 if(Object.keys(o).some(k=>!allowed.has(k))||o.type!=='Rectangle'||(o.angle??0)!==0||(o.rotation??0)!==0||o.flipX||o.flipY)throw Error(`${o.label}: plots must be plain unrotated Rectangles.`);
 const w=Number(o.width??128)*Number(o.scaleX??1)/32,h=Number(o.height??128)*Number(o.scaleY??1)/32;
 const x=Number(o.x??0)/32-w*Number(o.originX??.5),y=Number(o.y??0)/32-h*Number(o.originY??.5);
 if(![x,y,w,h].every(Number.isInteger)||w<=0||h<=0)throw Error(`${o.label}: plot edges must align to the 32 px grid.`);
 return {id:b.parcel.id,x,y,w,h};
}
export function validatePlot(b:Parcel,layout:Layout){
 const p=b.parcel,C=layout.city;
 if(p.x<0||p.y<0||p.x+p.w>C.width||p.y+p.h>C.riverY-6)throw Error(`${b.id}: plot outside dry city bounds.`);
 if(b.x<p.x||b.y<p.y||b.x+b.w>p.x+p.w||b.y+b.h>p.y+p.h)throw Error(`${b.id}: building outside its plot.`);
 if(layout.buildings.some(q=>q.id!==b.id&&overlaps(p,q.parcel)))throw Error(`${b.id}: plot overlaps a neighbouring plot.`);
 if(C.yards.some(q=>overlaps(p,q))||C.roads.some(([a,z])=>overlaps(p,corridorRect(a,z,C.roadHalf))))throw Error(`${b.id}: plot overlaps a road or factory yard.`);
 const reservations=[...C.resources.map(([,x,y,w,h])=>({x,y,w,h})),...C.drives.flatMap(path=>path.slice(1).map((z,i)=>corridorRect(path[i],z,2.5))),...[...C.plants,...C.cores,...C.artifacts,...C.stops].map(q=>({...q,w:3,h:3})),...Object.values(C.projects).map(([x,y])=>({x,y,w:3,h:3})),...C.recruits.map(([,x,y])=>({x,y,w:1,h:1})),...C.substations.map(([x,y])=>({x,y,w:3,h:3}))];
 if(reservations.some(q=>overlaps(p,q)))throw Error(`${b.id}: plot overlaps a resource, service drive or campaign reservation.`);
}
export function connectEntrance(b:Parcel,layout:Layout):[number,number][] {
 const p=doorOutside(b),start:[number,number]=[Math.floor(p.x),Math.floor(p.y)];
 const obstacles=[...layout.buildings.filter(q=>q.id!==b.id),...layout.props,...layout.city.yards,...layout.city.resources.map(([,x,y,w,h])=>({x,y,w,h}))];
 const candidates:{path:[number,number][];length:number}[]=[];
 const [dx,dy]=outward[b.facing];
 // An outward apron lets a side-facing entrance turn into a shared court without clipping its neighbours.
 for(const lead of [start,[start[0]+dx*2,start[1]+dy*2] as [number,number]])for(const[a,z]of layout.city.roads){
  const x=Math.max(Math.min(lead[0],Math.max(a[0],z[0])),Math.min(a[0],z[0])),y=Math.max(Math.min(lead[1],Math.max(a[1],z[1])),Math.min(a[1],z[1]));
  for(const elbow of [[x,lead[1]],[lead[0],y]] as [number,number][]){
   const path=[start,lead,elbow,[x,y] as [number,number]].filter((v,i,arr)=>!i||v[0]!==arr[i-1][0]||v[1]!==arr[i-1][1]);
   if(path.length<2)continue;
   const a=path.at(-2)!,z=path.at(-1)!,length=Math.abs(z[0]-a[0])+Math.abs(z[1]-a[1]);
   if(length>3)path[path.length-1]=[z[0]-Math.sign(z[0]-a[0])*3,z[1]-Math.sign(z[1]-a[1])*3];
   // The .7-wide cap at an adjacent entrance overlaps the outer wall by .2 tiles.
   const interior={x:b.x+.3,y:b.y+.3,w:b.w-.6,h:b.h-.6};
   if(path.slice(1).some((z,i)=>{const a=path[i],r=corridorRect([a[0]+.5,a[1]+.5],[z[0]+.5,z[1]+.5],.7);return overlaps(r,interior)||obstacles.some(q=>overlaps(r,q));}))continue;
   candidates.push({path,length:path.slice(1).reduce((n,z,i)=>n+Math.abs(z[0]-path[i][0])+Math.abs(z[1]-path[i][1]),0)});
  }
 }
 candidates.sort((a,b)=>a.length-b.length);
 if(!candidates.length)throw Error(`${b.id}: no clear entrance route to a public pavement. Move the building or leave an alley.`);
 return candidates[0].path;
}
