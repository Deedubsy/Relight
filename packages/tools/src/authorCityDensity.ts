/** CITY-F: deterministic district blocks, compiled through the canonical city validator. */
import {readFileSync,writeFileSync,existsSync} from 'node:fs';
import {RIVERFRONT as C,RIVERFRONT_BUILDINGS as buildings,RIVERFRONT_PROPS as props,createCampaign,riverfrontRail} from '@relight/sim';
import {validateRiverfront} from '../../sim/src/city/validateRiverfront';
import {doorRect,corridorRect,overlaps,type Rect} from '../../sim/src/city/parcelGeometry';
import {hash,readLayout,writeLayout,type EditorScene} from './phaserCityModel';
import {connectEntrance} from './phaserCityPlots';
import {exportEditorScene} from './phaserCityExport';
import type {Parcel,MapProp} from '../../sim/src/city/riverfront';
const dir='docs/evidence/city-density/',sourcePath='packages/sim/src/city/riverfront.ts',scenePath='packages/game/src/editor/RiverfrontCity.scene';
const read=(p:string)=>readFileSync(p,'utf8'),before=read(dir+'before-riverfront.ts.txt'),previous=JSON.parse(read(dir+'before.scene')) as EditorScene;
const last=existsSync(dir+'authored.json')?JSON.parse(read(dir+'authored.json')):null;
if(![hash(before),last?.sourceHash].includes(hash(read(sourcePath)))||![hash(read(dir+'before.scene')),last?.sceneHash].includes(hash(read(scenePath))))throw Error('City density source/scene drift: reconcile subsequent edits before rerunning.');
const base=readLayout(before),layout=structuredClone(base),xs=[18,72,126,180,234,288,342,378,432,486,540,594,648,666,702,756,792,810,846],ys=[18,72,126,144,198,252,306,324,378,432,450];
const groups=new Map<string,Parcel[]>();
for(const b of base.buildings)if(b.id.startsWith('block-')){const key=b.id.split('-').slice(0,3).join('-');groups.set(key,[...groups.get(key)??[],b]);}
const large=new Set(['office','department','arcade','parking','apartment']);
const replace=[...groups.values()].filter(bs=>!bs.some(b=>large.has(b.kind))).flat(),remove=new Set(replace.map(b=>b.id));
layout.buildings=layout.buildings.filter(b=>!remove.has(b.id));layout.props=layout.props.filter(p=>!replace.some(b=>p.id.startsWith(b.id+':')));
const oldPaths=new Set(replace.map(b=>JSON.stringify(b.path)));layout.city.paths=layout.city.paths.filter(p=>!oldPaths.has(JSON.stringify(p)));
const roadRects=C.roads.map(([a,z])=>corridorRect(a,z,7)),rail=riverfrontRail();
// Read reservations from the archived input, never a previous compiler output.
const reservations:Rect[]=[...base.city.yards,...base.city.resources.map(([,x,y,w,h])=>({x:x-1,y:y-1,w:w+2,h:h+2})),...base.city.drives.flatMap(p=>p.slice(1).map((z,i)=>corridorRect(p[i],z,2.5))),...Object.values(base.city.projects).map(([x,y])=>({x:x-1,y:y-1,w:5,h:5})),...base.city.substations.map(([x,y])=>({x,y,w:3,h:3})),...base.city.stops.map(p=>({...p,w:3,h:3})),...base.city.squares];
const retainedPaths=layout.city.paths.flatMap(p=>p.slice(1).map((z,i)=>corridorRect([p[i][0]+.5,p[i][1]+.5],[z[0]+.5,z[1]+.5],.7)));
const added:Parcel[]=[],report:{key:string;district:string;before:number;after:number;beforeArea:number;afterArea:number;retained:boolean}[]=[];
type Plan={kind:Parcel['kind'];x:number;y:number;w:number;h:number;facing:Parcel['facing'];name:string};
const plans:Plan[]=[];
function plan(kind:Plan['kind'],x:number,y:number,w:number,h:number,facing:Plan['facing']='S',name=''){plans.push({kind,x,y,w,h,facing,name});}
function fits(p:Rect){
 if(roadRects.some(r=>overlaps(p,r))||reservations.some(r=>overlaps(p,r))||retainedPaths.some(r=>overlaps(p,r))||layout.buildings.some(b=>overlaps(p,b.parcel))||layout.props.some(r=>overlaps(p,r)))return false;
 for(let y=p.y;y<p.y+p.h;y++)for(let x=p.x;x<p.x+p.w;x++)if(rail.reserved.has(y*C.width+x))return false;
 return true;
}
for(const[key,old]of groups){
 const [,ix,iy]=key.split('-').map(Number),x=xs[ix],y=ys[iy],w=xs[ix+1]-x,h=ys[iy+1]-y;
 const central=old.some(b=>large.has(b.kind)),industrial=y<126&&x<342,commercial=x>=234&&x<342&&y>=144||x>=378&&x<648&&y>=324,transition=x===378&&y>=144&&y<324;
 const district=central?'Civic Centre':industrial?'Northwood Industry':transition?'Civic Approach':commercial?'Old Town':x>=792?'East Wharf':x>=666?'East Gardens':y<126?'North Gardens':'Westridge';
 if(central){report.push({key,district,before:old.length,after:old.length,beforeArea:old.reduce((n,b)=>n+b.w*b.h,0),afterArea:old.reduce((n,b)=>n+b.w*b.h,0),retained:true});continue;}
 plans.length=0;
 if(industrial){
  plan('warehouse',x+8,y+8,24,18,'S','Freight hall');plan('workshop',x+35,y+8,11,18,'S','Service workshop');
  plan('garage',x+8,y+35,17,12,'S','Loading garage');plan('garage',x+29,y+35,17,12,'S','Fleet garage');
 }else if(transition){
  plan('apartment',x+8,y+8,26,16,'S','Boulevard apartments');plan('office',x+37,y+8,9,16,'S','Neighbourhood office');
  for(let i=0;i<3;i++)plan('shop',x+8+i*14,y+33,12,14,'S','Boulevard shop');
 }else if(commercial){
  for(let i=0;i<2;i++)plan('terrace',x+8+i*21,y+9,18,13,'S','Old Town terrace');
  for(let i=0;i<4;i++)plan('shop',x+8+i*10,y+33,8,14,'S','High Street shop');
 }else if(w>=54&&(ix+iy)%3===0){
  // Two inward-facing sides frame a shared garden instead of repeating the same eight-home row.
  for(let i=0;i<3;i++){plan('house',x+8,y+8+i*15,10,8,'E','Garden Court home');plan('house',x+36,y+8+i*15,10,8,'W','Garden Court home');}
 }else{
  const columns=w>=54?4:2,step=w>=54?10:11;
  for(let i=0;i<columns;i++){plan('house',x+8+i*step,y+9,8,12,'N','Garden Street home');plan('house',x+8+i*step,y+h-19,8,12,'S','Garden Street home');}
 }
 const accepted:Parcel[]=[];
 for(const [i,p]of plans.entries()){
  if(!fits(p))continue;
  const id=`density-${ix}-${iy}-${i}`,b:Parcel={...p,id,name:`${p.name} ${i+1}`,variant:(ix+iy+i)%3,visual:{x:p.x,y:p.y,w:p.w,h:p.h},parcel:{id:'plot:'+id,x:p.x-1,y:p.y-1,w:p.w+2,h:p.h+2},path:[]};
  const d=doorRect(b);b.door=[d.x,d.y];layout.buildings.push(b);accepted.push(b);
 }
 const beforeArea=old.reduce((n,b)=>n+b.w*b.h,0),afterArea=accepted.reduce((n,b)=>n+b.w*b.h,0);
 // Constrained story parcels keep their previous buildings rather than losing density/content.
 const retained=afterArea<=beforeArea;
 if(retained){layout.buildings=layout.buildings.filter(b=>!accepted.includes(b));layout.buildings.push(...structuredClone(old));for(const b of old){layout.city.paths.push(structuredClone(b.path));layout.props.push(...structuredClone(base.props.filter(p=>p.id.startsWith(b.id+':'))));remove.delete(b.id);}}
 else added.push(...accepted);
 report.push({key,district,before:old.length,after:retained?old.length:accepted.length,beforeArea,afterArea:retained?beforeArea:afterArea,retained});
}
for(const b of added){b.path=connectEntrance(b,layout);layout.city.paths.push(b.path);}
// Each block's remaining ground has a purpose; solids are checked against every entrance strip.
const allPaths=layout.city.paths.flatMap(p=>p.slice(1).map((z,i)=>corridorRect([p[i][0]+.5,p[i][1]+.5],[z[0]+.5,z[1]+.5],1)));
const detailObstacles=[...layout.buildings,...layout.props,...reservations,...allPaths,...C.roads.map(([a,z])=>corridorRect(a,z,6))];
const details:MapProp[]=[];
function detail(id:string,kind:MapProp['kind'],x:number,y:number,w:number,h:number){
 const p:MapProp={id,kind,x,y,w,h};if(detailObstacles.some(r=>overlaps(p,r))||details.some(r=>overlaps(p,r)))return;
 for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++)if(rail.reserved.has(yy*C.width+xx))return;
 details.push(p);
}
for(const r of report){
 const[,ix,iy]=r.key.split('-').map(Number),x=xs[ix],y=ys[iy],w=xs[ix+1]-x,h=ys[iy+1]-y;
 const industrial=r.district==='Northwood Industry',commercial=['Old Town','Civic Approach'].includes(r.district),central=r.district==='Civic Centre';
 if(!r.retained){
  if(industrial)layout.city.squares.push({x:x+7,y:y+7,w:w-14,h:h-14,name:r.key+' loading court'});
  else if(commercial){layout.city.squares.push({x:x+7,y:y+24,w:w-14,h:5,name:r.key+' service alley'},{x:x+7,y:y+h-7,w:w-14,h:2,name:r.key+' shop frontage'});}
  else if(w>=54&&(ix+iy)%3===0)layout.city.squares.push({x:x+24,y:y+8,w:6,h:h-16,name:r.key+' garden court walk'});
  else layout.city.squares.push({x:x+7,y:y+27,w:w-14,h:3,name:r.key+' garden mews'});
 }
 if(central){
  // Small side plazas finish the existing large footprints without crowding the civic entrances.
  const b=groups.get(r.key)![0];layout.city.squares.push({x:x+7,y:y+7,w:w-14,h:h-14,name:r.key+' civic forecourt'});
  detail(r.key+':civic-tree-a','tree',b.x-3,y+9,2,2);detail(r.key+':civic-tree-b','tree',b.x+b.w+1,y+9,2,2);
  detail(r.key+':civic-bench','furniture',x+9,y+h-10,3,1);continue;
 }
 if(industrial){detail(r.key+':pallets','container',x+35,y+28,6,3);detail(r.key+':yard-bench','furniture',x+10,y+29,3,1);}
 else{
  for(let i=0;i<(w>=54?3:1);i++){detail(r.key+':tree-'+i,'tree',x+11+i*12,y+23,2,2);detail(r.key+':garden-edge-'+i,'fence',x+15+i*12,y+31,1,3);}
  detail(r.key+':bench','furniture',x+Math.floor(w/2)-1,y+31,3,1);
 }
}
layout.props.push(...details);
layout.city.id='riverfront-arc-v4-editor-'+hash(JSON.stringify(layout)).slice(0,12);
Object.assign(C,layout.city);buildings.splice(0,buildings.length,...layout.buildings);props.splice(0,props.length,...layout.props);
const {reachable,...validation}=validateRiverfront(createCampaign());void reachable;
if(validation.errors.length)throw Error(validation.errors.join('\n'));
const source=writeLayout(before,layout);writeFileSync(sourcePath,source);exportEditorScene(source,previous);
const result={sourceHash:hash(source),sceneHash:hash(read(scenePath)),mapId:layout.city.id,removed:[...remove],added:added.map(b=>b.id),details:details.length,blocks:report,validation};
writeFileSync(dir+'authored.json',JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({mapId:result.mapId,added:added.length,removed:remove.size,details:details.length,validation,retainedBlocks:report.filter(r=>r.retained).map(r=>r.key)},null,2));
