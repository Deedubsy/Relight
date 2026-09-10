/** Editable, portable Tiled snapshot of the canonical authored map. No game-state mutation/import. */
import {mkdirSync,existsSync,readFileSync,writeFileSync,copyFileSync,readdirSync} from 'node:fs';
import {resolve,join,basename} from 'node:path';
import {deflateSync} from 'node:zlib';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {RIVERFRONT as C,RIVERFRONT_BUILDINGS as buildings,RIVERFRONT_PROPS as props,createCampaign,ground,TILE_PX,riverfrontRail,doorRect} from '@relight/sim';
import {validateRiverfront} from '../../sim/src/city/validateRiverfront';

const root=resolve(fileURLToPath(new URL('../../../',import.meta.url))),out=resolve(root,process.argv[2]??'maps/tiled/riverfront-v4');
const mapPath=join(out,'riverfront.tmj');
// Re-export to a NEW directory: never overwrite the user's edited map.
if(existsSync(mapPath))throw Error(`Output already contains a map: ${mapPath}. Choose a new output folder to preserve Tiled edits.`);
const state=createCampaign(),G=ground(state),{reachable,...validation}=validateRiverfront(state);
if(validation.errors.length)throw Error(validation.errors.join('\n'));void reachable;
mkdirSync(join(out,'assets'),{recursive:true});
const P=TILE_PX,W=C.width,H=C.height,n=W*H,rail=riverfrontRail();
const write=(name:string,value:unknown)=>writeFileSync(join(out,name),JSON.stringify(value,null,2)+'\n');
const properties=(values:Record<string,unknown>)=>Object.entries(values).filter(([,v])=>v!==undefined).map(([name,v])=>({name,type:typeof v==='boolean'?'bool':typeof v==='number'?(Number.isInteger(v)?'int':'float'):'string',value:typeof v==='object'?JSON.stringify(v):v}));
type Obj={id:number;name:string;type:string;x:number;y:number;width:number;height:number;rotation:number;visible:boolean;properties:ReturnType<typeof properties>;gid?:number;point?:boolean;ellipse?:boolean;polyline?:{x:number;y:number}[]};
let nextObject=1,nextLayer=1;
const object=(name:string,type:string,x:number,y:number,w=0,h=0,data:Record<string,unknown>={}):Obj=>({id:nextObject++,name,type,x:x*P,y:y*P,width:w*P,height:h*P,rotation:0,visible:true,properties:properties(data)});
const line=(name:string,type:string,path:readonly (readonly number[])[],data:Record<string,unknown>={},offset=.5)=>{const[x,y]=path[0];return{...object(name,type,x+offset,y+offset,0,0,data),polyline:path.map(p=>({x:(p[0]-x)*P,y:(p[1]-y)*P}))};};
const layers:unknown[]=[];
function objects(name:string,items:Obj[],color:string,visible=true,locked=false){layers.push({id:nextLayer++,name,type:'objectgroup',draworder:'index',x:0,y:0,opacity:1,visible,locked,color,objects:items});}
function tiles(name:string,data:Uint32Array,visible=true,opacity=1,locked=false){const bytes=Buffer.alloc(data.length*4);data.forEach((v,i)=>bytes.writeUInt32LE(v,i*4));layers.push({id:nextLayer++,name,type:'tilelayer',x:0,y:0,width:W,height:H,visible,opacity,locked,encoding:'base64',compression:'zlib',data:deflateSync(bytes).toString('base64')});}

// Small paintable terrain palette, separate from full building art. GID is index + 1.
const colors=['#52614b','#204853','#858b7e','#46504d','#929683','#7c7b68','#70776a','#68736b','#d04e52','#55c3c6','#a4b2b0','#c08854','#273338','#92978a','#3a3b4b','#e5b961'];
const names=['Grass','River','Pavement','Road','Footpath','Factory concrete','Civic paving','Rail bed','Solid collision','Rail clearance','Steel / iron','Copper','Coal','Stone','Crude oil','Site marker'];
const palette=`<svg xmlns="http://www.w3.org/2000/svg" width="${colors.length*P}" height="${P}">${colors.map((c,i)=>`<rect x="${i*P}" width="${P}" height="${P}" fill="${c}"/>${i===5||i===6?`<path d="M${i*P} 0v${P}h${P}" fill="none" stroke="#656b61" opacity=".45"/>`:''}`).join('')}</svg>`;
writeFileSync(join(out,'assets/terrain.svg'),palette);
write('terrain.tsj',{type:'tileset',version:'1.10',name:'Relight terrain and guides',objectalignment:'topleft',tilewidth:P,tileheight:P,tilecount:colors.length,columns:colors.length,image:'assets/terrain.svg',imagewidth:P*colors.length,imageheight:P,tiles:names.map((name,id)=>({id,type:name,properties:properties({purpose:name})}))});
const artDir=join(root,'packages/game/public/art/riverfront'),art=readdirSync(artDir).filter(f=>f.endsWith('.svg')).sort(),firstArt=colors.length+1;
for(const f of art)copyFileSync(join(artDir,f),join(out,'assets',f));
const artGid=(name:string)=>{const i=art.indexOf(name+'.svg');if(i<0)throw Error('Missing building art '+name);return firstArt+i;};
write('buildings.tsj',{type:'tileset',version:'1.10',name:'Relight building artwork',tilewidth:320,tileheight:256,tilecount:art.length,columns:0,objectalignment:'topleft',grid:{orientation:'orthogonal',width:1,height:1},tiles:art.map((f,id)=>({id,image:'assets/'+f,imagewidth:320,imageheight:256,type:basename(f,'.svg')}))});

const terrain=new Uint32Array(n),streets=new Uint32Array(n),yards=new Uint32Array(n),paths=new Uint32Array(n),tracks=new Uint32Array(n),solids=new Uint32Array(n),railClearance=new Uint32Array(n);
for(let y=0;y<H;y++)for(let x=0;x<W;x++)terrain[y*W+x]=y>=C.riverY+Math.round(6*Math.sin(x/52))?2:1;
const rect=(data:Uint32Array,x:number,y:number,w:number,h:number,gid:number)=>{for(let yy=Math.max(0,y);yy<Math.min(H,y+h);yy++)for(let xx=Math.max(0,x);xx<Math.min(W,x+w);xx++)data[yy*W+xx]=gid;};
function stroke(data:Uint32Array,path:readonly (readonly number[])[],radius:number,gid:number){
 for(let i=1;i<path.length;i++){const a=path[i-1],b=path[i],dx=b[0]-a[0],dy=b[1]-a[1],len=dx*dx+dy*dy;
  for(let y=Math.max(0,Math.floor(Math.min(a[1],b[1])-radius));y<=Math.min(H-1,Math.ceil(Math.max(a[1],b[1])+radius));y++)for(let x=Math.max(0,Math.floor(Math.min(a[0],b[0])-radius));x<=Math.min(W-1,Math.ceil(Math.max(a[0],b[0])+radius));x++){const t=len?Math.max(0,Math.min(1,((x-a[0])*dx+(y-a[1])*dy)/len)):0;if(Math.hypot(x-a[0]-t*dx,y-a[1]-t*dy)<=radius&&terrain[y*W+x]!==2)data[y*W+x]=gid;}
 }
}
for(const a of C.serviceAreas)rect(terrain,a.x,a.y,a.w,a.h,7);
for(const p of C.drives)stroke(streets,p,2,3);
for(const p of C.roads)stroke(streets,p,C.roadHalf,3);
stroke(streets,[[C.court.x,C.court.y],[C.court.x,C.court.y]],C.court.radius,3);
for(const p of C.roads)stroke(streets,p,C.roadHalf-C.pavement,4);
stroke(streets,[[C.court.x,C.court.y],[C.court.x,C.court.y]],C.court.radius-C.pavement,4);
for(const p of C.paths)stroke(paths,p,.7,5);
for(const p of [...C.yards,...C.squares])rect(yards,p.x,p.y,p.w,p.h,6);
// Render samples use world centres; stroke() uses cell-centred coordinates.
stroke(tracks,rail.samples.map(p=>[p.x-.5,p.y-.5]),1.4,8);
for(let i=0;i<n;i++){if(G.urban?.solid[i])solids[i]=9;if(rail.reserved.has(i))railClearance[i]=10;}
tiles('Terrain — paintable grass, river and civic paving',terrain);
tiles('Road and pavement surface — paintable',streets);
tiles('Footpath surface — paintable',paths);
tiles('Factory yards and squares — open ground',yards);
tiles('Tram bed — visual reference',tracks);

objects('Buildings — select and move these',buildings.map(b=>({...object(b.name,'Building',b.x,b.y,b.w,b.h,{stableId:b.id,kind:b.kind,facing:b.facing,variant:b.variant??0,enterable:!!b.enterable,compound:b.compound,content:b.content,note:b.note,sourceRecord:b}),gid:artGid(`${b.kind}${b.kind==='house'&&b.facing!=='S'?'-'+b.facing:''}-roof-${b.variant??0}`)})),'#e8cc8b');
objects('Ground-floor artwork — hidden reference',buildings.map(b=>({...object(b.name+' floor','BuildingFloor',b.x,b.y,b.w,b.h,{buildingId:b.id}),gid:artGid(b.kind+'-floor')})),'#b0a48b',false,true);
objects('Props — solid scenery and interior furniture',props.map(p=>object(p.id,p.kind,p.x,p.y,p.w,p.h,{stableId:p.id,clearable:!!p.clearable,sourceRecord:p})),'#91b376');
objects('Resources',C.resources.map(([item,x,y,w,h,units],i)=>({...object(item,'Resource',x,y,w,h,{stableId:'resource:'+i,item,units,sourceIndex:i}),gid:item.includes('copper')?12:item==='coal'?13:item==='stone'?14:item==='crude'?15:11})),'#db9f5e');
objects('Plants, cores and artifacts',[...C.plants.map(p=>({...p,kind:'Plant'})),...C.cores.map(p=>({...p,kind:'Core'})),...C.artifacts.map(p=>({...p,kind:'Artifact'}))].map(p=>object(p.name,p.kind,p.x,p.y,3,3,{stableId:p.id})),'#e16b68');
objects('Permanent tram stops',C.stops.map(p=>object(p.name,'TramStop',p.x,p.y,2,2,{stableId:p.id})),'#f5bb48');
objects('Projects and recruits',[...Object.entries(C.projects).map(([id,[x,y]])=>({...object(id,'Project',x,y,0,0,{stableId:id}),point:true})),...C.recruits.map(([kind,x,y])=>({...object(kind,'Recruit',x,y,0,0,{kind}),point:true}))],'#c8a3e6');
objects('Lights and substations',[...C.lights.map(([x,y],i)=>({...object('Street light '+i,'Light',x,y,0,0,{kw:C.lightKw}),point:true})),...C.substations.map(([x,y],i)=>object('Substation '+i,'Substation',x,y,3,3,{block:i}))],'#eee09c');
objects('Spawn and district labels',[{...object('Player start','Spawn',state.engineer.x,state.engineer.y),point:true},...C.regions.map(([name,x,y],i)=>({...object(name,'Region',x,y,0,0,{region:i}),point:true}))],'#ffffff');
objects('Doorways — facing and building ID',buildings.flatMap(b=>{const d=doorRect(b);return[object(b.id+' entry','Door',d.x,d.y,d.w,d.h,{buildingId:b.id,facing:b.facing,enterable:!!b.enterable}),...(b.doors??[]).map(([x,y,w,h],i)=>object(b.id+' side '+i,'Door',x,y,w,h,{buildingId:b.id,enterable:!!b.enterable}))];}),'#f3d68b',false);
objects('Parcels — reserved building plots',buildings.map(b=>{const p=b.parcel;return object(p.id,'Parcel',p.x,p.y,p.w,p.h,{buildingId:b.id});}),'#6ad8d2',false);
objects('Factory reservations',C.yards.map((p,i)=>object(['Home','Riverside','Ironworks','Civic'][i]+' yard','FactoryYard',p.x,p.y,p.w,p.h,{block:i})),'#62dbcb',false);
objects('Road centre lines — authored graph',C.roads.map((p,i)=>line('Road '+C.roadEdges[i].join(' → '),'Road',p,{from:C.roadEdges[i][0],to:C.roadEdges[i][1],halfWidth:C.roadHalf,pavement:C.pavement})),'#f6d283',false);
objects('Drives and entrance paths',[...C.drives.map((p,i)=>line('Drive '+i,'Drive',p,{width:4})),...buildings.map(b=>line(b.id+' path','Footpath',b.path,{buildingId:b.id,width:1.4}))],'#aad2a1',false);
objects('Tram controls — edit with clearance in mind',[line('Tram control points','TramRoute',C.tram,{radius:C.tramRadius,speed:C.tramSpeed,dwell:C.tramDwell}),{...object('Founders Court turnaround','Turnaround',C.court.x+.5-C.court.radius,C.court.y+.5-C.court.radius,C.court.radius*2,C.court.radius*2),ellipse:true}],'#ffbc42',false);
objects('Rounded tram centreline — derived reference',[line('Running tram','TramCurve',rail.samples.map(p=>[p.x,p.y]),{derived:true},0)],'#ffbc42',true,true);
tiles('Collision — snapshot, not editable gameplay',solids,false,.5,true);
tiles('Tram clearance — snapshot guide',railClearance,false,.4,true);

write('riverfront.tmj',{type:'map',version:'1.10',orientation:'orthogonal',renderorder:'right-down',infinite:false,width:W,height:H,tilewidth:P,tileheight:P,backgroundcolor:'#142329',nextlayerid:nextLayer,nextobjectid:nextObject,properties:properties({mapId:C.id,exportVersion:1,coordinateUnit:'pixels; divide by '+P+' for simulation tiles',gameImportSupported:false,source:'packages/sim/src/city/riverfront.ts',editingNote:'Save As before editing. Objects and painted terrain are separate; moving objects does not regenerate paths, collision or surfaces.'}),tilesets:[{firstgid:1,source:'terrain.tsj'},{firstgid:firstArt,source:'buildings.tsj'}],layers});
write('source-layout.json',{city:C,buildings,props});
write('export-report.json',{map:C.id,width:W,height:H,tilePixels:P,buildings:buildings.length,props:props.length,layers:layers.length,objects:nextObject-1,artFiles:art.length,sourceSha256:createHash('sha256').update(readFileSync(join(root,'packages/sim/src/city/riverfront.ts'))).digest('hex'),validation,importSupported:false});
console.log(`Exported ${mapPath}: ${buildings.length} buildings, ${layers.length} layers; game data unchanged.`);
