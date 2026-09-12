import type {SimState} from '../types';
import {ground} from '../ground';
import {passable} from '../walk';
import {authoredProblem} from '../authoredCity';
import {RIVERFRONT as C,RIVERFRONT_BUILDINGS as buildings,RIVERFRONT_PROPS as props,lineTiles} from './riverfront';
import {riverfrontRail} from './riverfrontRail';
import {overlaps,corridorRect,doorRect,doorOutside} from './parcelGeometry';
/** One structural acceptance check for the canonical new game, never player-built barriers. */
export function validateRiverfront(source:SimState){
 // Structural binding checks consider the permanently opened route. GP validation separately
 // proves exterior key reachability and closed-gate collision; never alter the supplied save.
 const p=source.campaign?.progression,g=p?.gameplay;
 const st:SimState=g?{...source,campaign:{...source.campaign!,progression:{...p!,gameplay:{...g,strongholds:{...g.strongholds,freight:{...g.strongholds.freight,opened:true}}}}}}:source;
 const errors:string[]=[],G=ground(st),rail=riverfrontRail(),overlap=(a:{x:number;y:number;w:number;h:number},b:{x:number;y:number;w:number;h:number})=>a.x<b.x+b.w&&a.x+a.w>b.x&&a.y<b.y+b.h&&a.y+a.h>b.y;
 const ids=new Set<string>();
 for(const b of buildings){
  const tag=`${b.id} (${b.x},${b.y})`;if(ids.has(b.id))errors.push(tag+' duplicate');ids.add(b.id);
  for(const shape of [b,b.visual])if(shape.x<b.parcel.x||shape.y<b.parcel.y||shape.x+shape.w>b.parcel.x+b.parcel.w||shape.y+shape.h>b.parcel.y+b.parcel.h)errors.push(tag+' outside parcel');
  const margin=C.roadHalf+C.setback;
  if(Math.hypot(Math.max(b.x-C.court.x,0,C.court.x-b.x-b.w),Math.max(b.y-C.court.y,0,C.court.y-b.y-b.h))<C.court.radius+C.setback)errors.push(tag+' turning bulb setback');
  if(C.roads.some(([a,z])=>overlap(b,{x:Math.min(a[0],z[0])-margin,y:Math.min(a[1],z[1])-margin,w:Math.abs(a[0]-z[0])+2*margin,h:Math.abs(a[1]-z[1])+2*margin})))errors.push(tag+' road/pavement setback');
  if(C.yards.some(p=>overlap(b,p)))errors.push(tag+' factory yard');
  if(C.drives.some(path=>path.slice(1).some((z,i)=>{const a=path[i];return overlap(b,{x:Math.min(a[0],z[0])-2,y:Math.min(a[1],z[1])-2,w:Math.abs(a[0]-z[0])+5,h:Math.abs(a[1]-z[1])+5});})))errors.push(tag+' service drive');
  if(buildings.some(a=>a.id<b.id&&overlap(a,b)))errors.push(tag+' neighbouring building');
  for(const[item,x,y,w,h]of C.resources)if(overlaps(b,{x,y,w,h}))errors.push(tag+' resource '+item+' at '+x+','+y);
  for(const p of props)if(overlaps(b,p)&&!(b.enterable&&p.x>=b.x&&p.y>=b.y&&p.x+p.w<=b.x+b.w&&p.y+p.h<=b.y+b.h))errors.push(tag+' solid prop '+p.id);
  const d=doorRect(b),outside=doorOutside(b);
  if(d.x<b.x||d.y<b.y||d.x+d.w>b.x+b.w||d.y+d.h>b.y+b.h||(b.facing==='N'?d.y!==b.y:b.facing==='S'?d.y+d.h!==b.y+b.h:b.facing==='E'?d.x+d.w!==b.x+b.w:d.x!==b.x))errors.push(tag+' door on wrong wall');
  if(b.path[0][0]!==Math.floor(outside.x)||b.path[0][1]!==Math.floor(outside.y))errors.push(tag+' entrance path faces wrong way');
  const obstacles=[...buildings.filter(q=>q.id!==b.id),...props,...C.yards.map((q,i)=>({...q,id:'yard:'+i})),...C.resources.map(([item,x,y,w,h])=>({x,y,w,h,id:'resource:'+item}))];
  for(let i=1;i<b.path.length;i++){const a=b.path[i-1],z=b.path[i],strip=corridorRect([a[0]+.5,a[1]+.5],[z[0]+.5,z[1]+.5],.7);for(const q of obstacles)if(overlaps(strip,q))errors.push(tag+' path width obstructed by '+q.id);}
  let water=false,track=false;for(let y=b.y;y<b.y+b.h;y++)for(let x=b.x;x<b.x+b.w;x++){water||=G.owner[y*G.tw+x]===-2;track||=rail.reserved.has(y*G.tw+x);}if(water||track)errors.push(tag+(water?' water':' swept tram corridor'));
  for(const t of lineTiles(b.path))if(!passable(st,t%G.tw,Math.floor(t/G.tw))){errors.push(tag+' obstructed entrance path at '+t%G.tw+','+Math.floor(t/G.tw));break;}
 }
 for(const id of ['home-workshop','riverside-pump','ironworks-hall','civic-utility','freight-stronghold','quarry-stronghold','wharf-stronghold','civic-dome'])if(!ids.has(id))errors.push('Missing required parcel '+id);
 for(const [kind,min]of [['townhall',1],['apartment',2],['office',2],['department',1],['arcade',1],['parking',1]] as const)if(buildings.filter(b=>b.kind===kind).length<min)errors.push('Missing downtown archetype '+kind);
 for(const [actual,required]of [[C.plants,['plant:riverside','plant:ironworks','plant:civic']],[C.cores,['core:freight','core:quarry','core:wharf']],[C.artifacts,['artifact:workshop','artifact:quarry','artifact:wharf']],[C.stops,['tram:home','tram:riverside','tram:ironworks','tram:civic']]] as const){if(actual.length!==required.length||new Set(actual.map(s=>s.id)).size!==actual.length||required.some(id=>!actual.some(s=>s.id===id)))errors.push('Missing/duplicate required sites '+required.join(', '));}
 if(C.yards.length!==4)errors.push('Four factory yards required');
 for(const [i,yard]of C.yards.entries()){
  if(C.roads.some(([a,b])=>overlaps(yard,corridorRect(a,b,C.roadHalf))))errors.push('Yard '+i+' overlaps public pavement');
  for(let y=yard.y;y<yard.y+yard.h;y++)for(let x=yard.x;x<yard.x+yard.w;x++)if(rail.reserved.has(y*G.tw+x))errors.push('Yard '+i+' overlaps swept rail at '+x+','+y);
 }
 const links=new Map(C.roadNodes.map(n=>[n.id,[] as string[]]));
 for(const[a,b]of C.roadEdges){if(!links.has(a)||!links.has(b))errors.push('Missing road node '+a+'/'+b);else{links.get(a)!.push(b);links.get(b)!.push(a);}}
 const reached=new Set<string>(),todo=[C.roadNodes[0].id];for(let i=0;i<todo.length;i++){const id=todo[i];if(reached.has(id))continue;reached.add(id);todo.push(...links.get(id)!);}if(reached.size!==C.roadNodes.length)errors.push('Disconnected public road graph');
 for(const n of C.roadNodes)if(links.get(n.id)!.length===1&&!n.end)errors.push('Unintended road end '+n.id);
 for(const [i,path]of C.drives.entries()){const [x,y]=path[0];if(!C.roads.some(([a,b])=>x>=Math.min(a[0],b[0])&&x<=Math.max(a[0],b[0])&&y>=Math.min(a[1],b[1])&&y<=Math.max(a[1],b[1])))errors.push('Disconnected factory service drive '+i);}
 const visited=new Uint8Array(G.tw*G.th),queue=new Int32Array(visited.length),start=Math.floor(st.engineer.y)*G.tw+Math.floor(st.engineer.x);let count=0,end=1;queue[0]=start;visited[start]=1;
 for(let at=0;at<end;at++){const t=queue[at],x=t%G.tw,y=Math.floor(t/G.tw);count++;for(const[xx,yy]of[[x-1,y],[x+1,y],[x,y-1],[x,y+1]]){const k=yy*G.tw+xx;if(xx<0||yy<0||xx>=G.tw||yy>=G.th||visited[k]||!passable(st,xx,yy))continue;visited[k]=1;queue[end++]=k;}}
 for(const b of buildings){const p=doorOutside(b);if(!visited[Math.floor(p.y)*G.tw+Math.floor(p.x)])errors.push(b.id+' unreachable entrance approach');}
 for(const s of [...C.plants,...C.cores,...C.artifacts,...C.stops]){let access=false;for(let y=s.y-1;y<s.y+4;y++)for(let x=s.x-1;x<s.x+4;x++)access||=!!visited[y*G.tw+x];if(!access)errors.push(s.id+' inaccessible binding');}
 for(const p of props)for(let y=p.y;y<p.y+p.h;y++)for(let x=p.x;x<p.x+p.w;x++)if(rail.reserved.has(y*G.tw+x)){errors.push(p.id+' prop on tram corridor');y=p.y+p.h;break;}
 for(const p of props)if(C.roads.some(([a,b])=>overlap(p,{x:Math.min(a[0],b[0])-C.roadHalf,y:Math.min(a[1],b[1])-C.roadHalf,w:Math.abs(a[0]-b[0])+2*C.roadHalf+1,h:Math.abs(a[1]-b[1])+2*C.roadHalf+1})))errors.push(p.id+' prop on public road/pavement');
 for(let i=1;i<rail.tiles.length;i++){const a=rail.tiles[i-1],b=rail.tiles[i];if(Math.abs(a%G.tw-b%G.tw)+Math.abs(Math.floor(a/G.tw)-Math.floor(b/G.tw))!==1||rail.distances[i]<=rail.distances[i-1])errors.push('Rail discontinuity '+i);}
 if(new Set(rail.tiles).size!==rail.tiles.length)errors.push('Duplicate rail cell');
 for(let i=1;i<rail.samples.length;i++){const a=rail.samples[i-1],b=rail.samples[i];if(b.d-a.d>.251||Math.abs(Math.atan2(Math.sin(b.angle-a.angle),Math.cos(b.angle-a.angle)))>.03)errors.push('Rail tangent/position discontinuity '+i);}
 for(const s of C.stops){const hits=rail.samples.filter(p=>Math.hypot(p.x-s.x-1,p.y-s.y-1)<4);if(!hits.length||hits.some(p=>Math.abs(Math.sin(p.angle*2))>.001))errors.push(s.id+' not on a straight');}
 const binding=authoredProblem(st);if(binding)errors.push(binding);
 let land=0;for(const o of G.owner)if(o!==-2)land++;
 return {errors,width:G.tw,height:G.th,land,accessible:count,buildings:buildings.length,roads:C.roadEdges.length,railLength:rail.length,reachable:visited};
}
