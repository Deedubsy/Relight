/** Bounded CITY-E authoring pass. Refuses to overwrite subsequent source or scene edits. */
import {readFileSync,writeFileSync,existsSync} from 'node:fs';
import {RIVERFRONT as C,RIVERFRONT_BUILDINGS as buildings,RIVERFRONT_PROPS as props,createCampaign} from '@relight/sim';
import {validateRiverfront} from '../../sim/src/city/validateRiverfront';
import {doorRect,corridorRect,overlaps} from '../../sim/src/city/parcelGeometry';
import {hash,readLayout,writeLayout,type EditorScene} from './phaserCityModel';
import {connectEntrance} from './phaserCityPlots';
import {exportEditorScene} from './phaserCityExport';
import type {Parcel,MapProp} from '../../sim/src/city/riverfront';
const dir='docs/evidence/neighbourhood-density/',sourcePath='packages/sim/src/city/riverfront.ts',scenePath='packages/game/src/editor/RiverfrontCity.scene';
const read=(p:string)=>readFileSync(p,'utf8'),before=read(dir+'before-riverfront.ts.txt'),previous=JSON.parse(read(dir+'before.scene')) as EditorScene;
const last=existsSync(dir+'authored.json')?JSON.parse(read(dir+'authored.json')):null;
if(![hash(before),last?.sourceHash].includes(hash(read(sourcePath)))||![hash(read(dir+'before.scene')),last?.sceneHash].includes(hash(read(scenePath))))throw Error('Neighbourhood source/scene drift: reconcile subsequent edits before rerunning this authoring pass.');
const layout=readLayout(before),removed=layout.buildings.filter(b=>b.id.startsWith('block-2-7-')||b.id.startsWith('block-2-8-')).map(b=>b.id);
const paths=layout.buildings.filter(b=>removed.includes(b.id)).map(b=>JSON.stringify(b.path));
layout.buildings=layout.buildings.filter(b=>!removed.includes(b.id));layout.props=layout.props.filter(p=>!removed.some(id=>p.id.startsWith(id+':')));layout.city.paths=layout.city.paths.filter(p=>!paths.includes(JSON.stringify(p)));
const added:Parcel[]=[];
function add(id:string,name:string,kind:Parcel['kind'],x:number,y:number,w:number,h:number,facing:Parcel['facing']='S',margin=1){
 const b:Parcel={id,name,kind,x,y,w,h,facing,variant:added.length%3,visual:{x,y,w,h},parcel:{id:'plot:'+id,x:x-margin,y:y-margin,w:w+margin*2,h:h+margin*2},path:[]};
 const d=doorRect(b);b.door=[d.x,d.y];added.push(b);layout.buildings.push(b);return b;
}
// A south-facing shopping frontage, with a service alley and north-facing homes behind it.
for(const [i,x]of [134,144,154,164].entries()){
 add('court-shops-'+i,['Court Grocer','Founders Hardware','Corner Bakery','Repair Shop'][i],'shop',x,356,8,15);
 add('court-shops-home-'+i,'Market Lane home '+(i+1),'house',x,333,8,10,'N');
}
add('court-shops-store','Market Lane store','garage',135,347,10,6);
add('court-shops-workshop','Market Lane workshop','garage',159,347,12,6);
// The next block is residential: two close rows, deep gardens and a central pedestrian mews.
for(const [i,x]of [134,144,154,164].entries()){
 add('court-mews-north-'+i,'Founders Mews '+(i+1),'house',x,387,8,12,'N');
 add('court-mews-south-'+i,'Founders Mews '+(i+5),'house',x,413,8,12,'S');
}
// Infill the court's western edge, without moving the established homes or campaign anchors.
add('court-garden-home','Garden Cottage','house',31,393,10,9,'E');
add('court-southwest-home','Court End home','house',31,414,10,10,'S');
add('court-north-home','North Court home','house',31,333,10,9,'N');
// A modest east-side infill helps enclose the court while keeping the yard approach open.
add('court-east-garden-home','East Garden home','house',101,414,10,10,'S');
for(const b of added){b.path=connectEntrance(b,layout);layout.city.paths.push(b.path);}
layout.city.squares.push(
 {x:133,y:353,w:41,h:3,name:'Market Lane service alley'},
 {x:133,y:371,w:41,h:2,name:'Court shops forecourt'},
 {x:133,y:405,w:41,h:3,name:'Founders Mews garden walk'},
 {x:43,y:395,w:4,h:25,name:'Court garden walk'}
);
// Solid garden edges and furniture leave every entrance strip and reservation unobstructed.
const details:MapProp[]=[];
function prop(id:string,kind:MapProp['kind'],x:number,y:number,w:number,h:number){
 const p:MapProp={id:'neighbourhood:'+id,kind,x,y,w,h};
 const obstacles=[...layout.buildings,...layout.props,...details,...layout.city.yards,...layout.city.resources.map(([,x,y,w,h])=>({x,y,w,h})),...layout.city.drives.flatMap(path=>path.slice(1).map((z,i)=>corridorRect(path[i],z,2.5))),...layout.city.roads.map(([a,z])=>corridorRect(a,z,6)),...layout.city.paths.flatMap(path=>path.slice(1).map((z,i)=>corridorRect([path[i][0]+.5,path[i][1]+.5],[z[0]+.5,z[1]+.5],1)))];
 if(obstacles.some(q=>overlaps(p,q)))throw Error('Authored detail obstructs geometry: '+p.id);
 details.push(p);
}
for(const [i,x]of [135,145,155,165].entries()){
 prop('mews-tree-'+i,'tree',x,401,2,2);
 prop('mews-rear-fence-'+i,'fence',x+3,400,1,4);
 prop('mews-south-fence-'+i,'fence',x+3,409,1,3);
}
prop('mews-bench','furniture',150,409,3,1);
prop('court-tree-a','tree',44,388,2,2);
prop('court-tree-b','tree',44,410,2,2);
prop('court-west-garden-edge','fence',27,387,1,14);
prop('court-west-garden-edge-2','fence',27,406,1,18);
prop('market-tree','tree',149,348,2,2);
layout.props.push(...details);
layout.city.id='riverfront-arc-v4-editor-'+hash(JSON.stringify(layout)).slice(0,12);
Object.assign(C,layout.city);buildings.splice(0,buildings.length,...layout.buildings);props.splice(0,props.length,...layout.props);
const {reachable,...validation}=validateRiverfront(createCampaign());void reachable;
if(validation.errors.length)throw Error(validation.errors.join('\n'));
const source=writeLayout(before,layout);
writeFileSync(sourcePath,source);exportEditorScene(source,previous);
const report={sourceHash:hash(source),sceneHash:hash(read(scenePath)),mapId:layout.city.id,removed,added:added.map(b=>b.id),details:details.length,validation};
writeFileSync(dir+'authored.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));
