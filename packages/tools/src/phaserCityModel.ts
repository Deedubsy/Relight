/** Explicit editor-to-simulation boundary. Sprite transforms never become collision implicitly. */
import {createHash} from 'node:crypto';
import type {Parcel,MapProp,RIVERFRONT} from '../../sim/src/city/riverfront';
import {newBuilding,readPlot,validatePlot,connectEntrance,protectedBuilding} from './phaserCityPlots';
export type Layout={city:typeof RIVERFRONT;buildings:Parcel[];props:MapProp[]};
export type SceneObject={id:string;type:string;label:string;displayName?:string;x?:number;y?:number;texture?:{key:string};[key:string]:unknown};
export type EditorScene={displayList:SceneObject[];[key:string]:unknown};
export type Manifest={version:1;sourceHash:string;base:Layout;objects:SceneObject[];bindings:Record<string,string>;fixedBuildings:string[]};
// Git may convert this Windows checkout between CRLF and LF; that is not map drift.
export const hash=(text:string)=>createHash('sha256').update(text.replace(/\r\n/g,'\n')).digest('hex');
const markers=['export const RIVERFRONT:Definition=','export const RIVERFRONT_BUILDINGS:Parcel[]=','export const RIVERFRONT_PROPS:MapProp[]=','export function lineTiles'];
export function readLayout(source:string):Layout {
 const values=markers.slice(0,3).map((m,i)=>JSON.parse(source.slice(source.indexOf(m)+m.length,source.indexOf(markers[i+1])).trim().replace(/;$/,'')));
 return {city:values[0],buildings:values[1],props:values[2]};
}
export function writeLayout(source:string,layout:Layout):string {
 const head=source.slice(0,source.indexOf(markers[0])).replace(/export const RIVERFRONT_ID='[^']+';/,`export const RIVERFRONT_ID='${layout.city.id}';`);
 return head+[layout.city,layout.buildings,layout.props].map((v,i)=>markers[i]+JSON.stringify(v,null,1)+';\n').join('')+source.slice(source.indexOf(markers[3]));
}
export const roof=(b:Parcel)=>`rf-${b.kind}${b.kind==='house'&&b.facing!=='S'?'-'+b.facing:''}-roof-${b.variant??0}`;
function equal(a:unknown,b:unknown){return JSON.stringify(a)===JSON.stringify(b);}
function position(o:SceneObject,k:'x'|'y'){const n=o[k]??0;if(typeof n!=='number'||!Number.isFinite(n))throw Error(`${o.label}: invalid ${k}`);return n;}
function transform(o:SceneObject){return {type:o.type,x:position(o,'x'),y:position(o,'y'),scaleX:o.scaleX??1,scaleY:o.scaleY??1,originX:o.originX??.5,originY:o.originY??.5,angle:o.angle??0,rotation:o.rotation??0,flipX:o.flipX??false,flipY:o.flipY??false,texture:o.texture,alpha:o.alpha??1,visible:o.visible??true};}
export function draftLayout(scene:EditorScene,manifest:Manifest):Layout {
 if(!Array.isArray(scene.displayList))throw Error('Missing scene display list.');
 const byId=new Map(scene.displayList.map(o=>[o.id,o]));
 if(byId.size!==scene.displayList.length)throw Error('Duplicate editor object IDs.');
 if(new Set(scene.displayList.map(o=>o.label)).size!==scene.displayList.length)throw Error('Use unique object labels, including duplicated buildings.');
 const result=structuredClone(manifest.base);
 const reroute=new Set<string>(),plots=new Map<string,SceneObject>(),labels=new Map<string,Parcel>();
 for(const o of scene.displayList)if(o.label.startsWith('PLOT_'))plots.set(o.label.slice(5),o);
 for(const original of manifest.objects){
  const o=byId.get(original.id);
  if(!o){const id=manifest.bindings[original.id],b=result.buildings.find(b=>b.id===id);
   if(!b||protectedBuilding(b,manifest.fixedBuildings))throw Error('Missing protected editor object '+original.label);
   result.buildings=result.buildings.filter(q=>q.id!==id);result.props=result.props.filter(p=>!p.id.startsWith(id+':'));
   const index=result.city.paths.findIndex(p=>equal(p,b.path));if(index>=0)result.city.paths.splice(index,1);continue;
  }
  const id=manifest.bindings[o.id],before=transform(original),after=transform(o);
  const known=new Set([...Object.keys(original),'label','displayName','scope','useGameObjectName',...Object.keys(after)]);
  const unsupported=Object.keys(o).filter(k=>!known.has(k));
  if(unsupported.length)throw Error(`${original.label}: unsupported editor properties ${unsupported.join(', ')}. Keep this a plain building image.`);
  if(!id){
   // Visibility is an editor aid; reference geometry itself cannot be imported.
   after.visible=before.visible;
   if(!equal(after,before))throw Error(`${original.label}: reference layer changed. Undo its transform; this pass imports buildings only.`);
   continue;
  }
  const b=result.buildings.find(b=>b.id===id)!,old=manifest.base.buildings.find(b=>b.id===id)!;
  labels.set(o.label,b);
  const dx=(after.x-before.x)/32,dy=(after.y-before.y)/32;
  if(!Number.isInteger(dx)||!Number.isInteger(dy))throw Error(`${id}: use the 32 px tile grid for X/Y.`);
  after.x=before.x;after.y=before.y;after.texture=before.texture;
  if(!equal(after,before)||o.list||o.prefabId)throw Error(`${id}: only X/Y and a matching roof variant are supported; keep size, origin, rotation and visibility unchanged.`);
  const variants=[0,1,2].map(variant=>roof({...old,variant})),variant=variants.indexOf(o.texture?.key??'');
  if(variant<0)throw Error(`${id}: choose a roof of the same building kind and facing (${variants.join(', ')}).`);
  if((dx||dy)&&protectedBuilding(old,manifest.fixedBuildings))throw Error(`${id}: contains an interior or campaign binding and must stay in place.`);
  const plot=plots.get(o.label);
  if(plot){b.parcel=readPlot(plot,b);if(protectedBuilding(old,manifest.fixedBuildings)&&!equal(b.parcel,old.parcel))throw Error(`${id}: protected plot must stay in place.`);if(!equal(b.parcel,old.parcel)||dx||dy)reroute.add(id);}
  if(variant!==(old.variant??0))b.variant=variant;
  if(!dx&&!dy)continue;
  b.x+=dx;b.y+=dy;b.visual.x+=dx;b.visual.y+=dy;
  if(b.x<b.parcel.x||b.y<b.parcel.y||b.x+b.w>b.parcel.x+b.parcel.w||b.y+b.h>b.parcel.y+b.parcel.h)throw Error(`${id}: keep the building inside its reserved parcel, or edit its PLOT_ rectangle.`);
  if(b.door){b.door[0]+=dx;b.door[1]+=dy;}
  for(const d of b.doors??[]){d[0]+=dx;d[1]+=dy;}
  // Move the entrance and join the unchanged street end with an orthogonal elbow.
  const first:[number,number]=[old.path[0][0]+dx,old.path[0][1]+dy],next=old.path[1];
  const elbow:[number,number]=b.facing==='E'||b.facing==='W'?[next[0],first[1]]:[first[0],next[1]];
  b.path=[first,elbow,...old.path.slice(1)].filter((p,i,a)=>!i||!equal(p,a[i-1]));
  // Remove redundant elbows, including a short backtrack along the old approach.
  for(let i=1;i<b.path.length-1;){const a=b.path[i-1],p=b.path[i],z=b.path[i+1];if((a[0]===p[0]&&p[0]===z[0])||(a[1]===p[1]&&p[1]===z[1]))b.path.splice(i,1);else i++;}
  const pathIndex=result.city.paths.findIndex(p=>equal(p,old.path));if(pathIndex<0)throw Error(id+': missing canonical path');result.city.paths[pathIndex]=b.path;
  // Furniture and named garden props belong to their building; keep those relationships.
  for(const p of result.props)if(p.id.startsWith(id+':')){p.x+=dx;p.y+=dy;}
 }
 const originals=new Set(manifest.objects.map(o=>o.id));
 for(const o of scene.displayList)if(!originals.has(o.id)&&!o.label.startsWith('PLOT_')){
  const b=newBuilding(o),plot=plots.get(o.label);if(plot)b.parcel=readPlot(plot,b);
  result.buildings.push(b);labels.set(o.label,b);reroute.add(b.id);
 }
 for(const [label]of plots){const b=labels.get(label);if(!b)throw Error(`PLOT_${label}: no matching building image. Rename or remove the orphan plot.`);const old=manifest.base.buildings.find(q=>q.id===b.id);if(!old||!equal(b.parcel,old.parcel))validatePlot(b,result);}
 for(const id of reroute){const b=result.buildings.find(b=>b.id===id)!,old=manifest.base.buildings.find(q=>q.id===id);if(!old||!equal(b.parcel,old.parcel))validatePlot(b,result);const index=result.city.paths.findIndex(p=>equal(p,b.path));b.path=connectEntrance(b,result);if(index>=0)result.city.paths[index]=b.path;else result.city.paths.push(b.path);}
 return result;
}
