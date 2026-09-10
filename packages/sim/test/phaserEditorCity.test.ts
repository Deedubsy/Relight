import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {hash,draftLayout,readLayout,writeLayout,type EditorScene,type Manifest} from '../../tools/src/phaserCityModel';
import {RIVERFRONT,RIVERFRONT_BUILDINGS,RIVERFRONT_PROPS,createCampaign,passable} from '../src/index';
import {validateRiverfront} from '../src/city/validateRiverfront';
const dir=new URL('../../../maps/phaser/',import.meta.url);
const manifest=JSON.parse(readFileSync(new URL('manifest.json',dir),'utf8')) as Manifest;
const scene=()=>({displayList:structuredClone(manifest.objects)}) as EditorScene;
const object=(s:EditorScene,id:string)=>s.displayList.find(o=>manifest.bindings[o.id]===id)!;

test('editor no-op round trip preserves every authored map record',()=>{
 assert.deepEqual(draftLayout(scene(),manifest),manifest.base);
 const source=readFileSync(new URL('RiverfrontCity.source.txt',dir),'utf8');
 assert.equal(hash(source.replace(/\r\n/g,'\n')),hash(source.replace(/\r?\n/g,'\r\n')),'Windows Git line endings are not source drift');
 assert.deepEqual(readLayout(writeLayout(source,manifest.base)),manifest.base);
});

function validateDraft(s:EditorScene){
 const d=draftLayout(s,manifest),saved=structuredClone({city:RIVERFRONT,buildings:RIVERFRONT_BUILDINGS,props:RIVERFRONT_PROPS});
 try{Object.assign(RIVERFRONT,d.city);RIVERFRONT_BUILDINGS.splice(0,RIVERFRONT_BUILDINGS.length,...d.buildings);RIVERFRONT_PROPS.splice(0,RIVERFRONT_PROPS.length,...d.props);
  const st=createCampaign();assert.deepEqual(validateRiverfront(st).errors,[]);return {d,st};
 }finally{Object.assign(RIVERFRONT,saved.city);RIVERFRONT_BUILDINGS.splice(0,RIVERFRONT_BUILDINGS.length,...saved.buildings);RIVERFRONT_PROPS.splice(0,RIVERFRONT_PROPS.length,...saved.props);}
}
test('a duplicated roof becomes a new solid background building with a reachable entrance',()=>{
 const s=scene(),old=object(s,'court-southwest-home');s.displayList=s.displayList.filter(o=>o.id!==old.id);
 s.displayList.push({...old,id:'test-infill',label:'new_infill'});
 const {d,st}=validateDraft(s),b=d.buildings.find(b=>b.id==='editor:test-infill')!;
 assert.equal(b.enterable,undefined);assert.equal(passable(st,b.x,b.y),false);assert.ok(b.path.length>=2);assert.ok(!d.buildings.some(b=>b.id==='court-southwest-home'));
 const orphan=scene();orphan.displayList.push({id:'plot-orphan',type:'Rectangle',label:'PLOT_missing'});assert.throws(()=>draftLayout(orphan,manifest),/no matching building/);
});
test('moving a plot and its building updates authoritative collision and access beyond the old plot',()=>{
 const s=scene(),o=object(s,'court-southwest-home'),b=manifest.base.buildings.find(b=>b.id==='court-southwest-home')!;
 o.x!+=64;s.displayList.push({id:'test-plot',type:'Rectangle',label:'PLOT_'+o.label,x:(b.parcel.x+2)*32,y:b.parcel.y*32,width:b.parcel.w*32,height:b.parcel.h*32,originX:0,originY:0});
 const {d}=validateDraft(s),moved=d.buildings.find(q=>q.id===b.id)!;assert.equal(moved.x,b.x+2);assert.equal(moved.parcel.x,b.parcel.x+2);assert.equal(moved.door![0],b.door![0]+2);
 const invalid=structuredClone(s);invalid.displayList.at(-1)!.x=126*32;assert.throws(()=>draftLayout(invalid,manifest),/parcel|plot/);
});
test('new images cannot introduce unvalidated transforms, duplicate labels or campaign plot edits',()=>{
 for(const change of [
  (o:Record<string,unknown>)=>{o.scaleX=.83;},
  (o:Record<string,unknown>)=>{o.angle=90;},
  (o:Record<string,unknown>)=>{o.physics={};},
  (o:Record<string,unknown>)=>{o.x=83*32;o.y=350*32;},
 ]){const s=scene(),o={...object(s,'court-southwest-home'),id:'new',label:'new'};change(o);s.displayList.push(o);assert.throws(()=>draftLayout(s,manifest));}
 const s=scene(),o=object(s,'home-workshop');s.displayList.push({id:'fixed-plot',type:'Rectangle',label:'PLOT_'+o.label,x:100,y:100,width:100,height:100,originX:0,originY:0});assert.throws(()=>draftLayout(s,manifest));
});
test('building move carries the door, visual and owned props and rejoins the original street path',()=>{
 const s=scene(),o=object(s,'court-west-home');o.x!+=32;o.y!+=32;
 const d=draftLayout(s,manifest),b=d.buildings.find(b=>b.id==='court-west-home')!;
 assert.deepEqual([b.x,b.y,b.visual.x,b.visual.y,b.door],[32,375,32,375,[41,378]]);
 assert.deepEqual(b.path,[[42,379],[69,379],[69,378]]);
 assert.ok(d.city.paths.some(p=>JSON.stringify(p)===JSON.stringify(b.path)));
 const p=d.props.find(p=>p.id==='court-west-home:garden-tree')!;assert.deepEqual([p.x,p.y],[30,373]);
 assert.deepEqual(b.parcel,manifest.base.buildings.find(b=>b.id==='court-west-home')!.parcel);
 const n=scene();object(n,'court-northwest-home').x!+=32;object(n,'court-northwest-home').y!+=32;
 assert.deepEqual(draftLayout(n,manifest).buildings.find(b=>b.id==='court-northwest-home')!.path,[[42,356],[46,356],[46,370],[72,370],[72,371]],'remove a backtrack along the old approach');
});
test('roof variants import while scaling, rotation, off-grid moves, missing objects and campaign moves fail',()=>{
 const s=scene();object(s,'court-west-home').texture={key:'rf-house-E-roof-2'};
 assert.equal(draftLayout(s,manifest).buildings.find(b=>b.id==='court-west-home')!.variant,2);
 for(const change of [
  (s:EditorScene)=>{object(s,'court-west-home').x!+=1;},
  (s:EditorScene)=>{object(s,'court-west-home').scaleX=2;},
  (s:EditorScene)=>{object(s,'court-west-home').angle=90;},
  (s:EditorScene)=>{object(s,'court-west-home').x!+=3200;},
  (s:EditorScene)=>{object(s,'court-west-home').texture={key:'rf-warehouse-roof-0'};},
  (s:EditorScene)=>{object(s,'home-workshop').x!+=32;},
  (s:EditorScene)=>{s.displayList=s.displayList.filter(o=>manifest.bindings[o.id]!=='home-workshop');},
  (s:EditorScene)=>{s.displayList[0].x=32;},
  (s:EditorScene)=>{s.displayList[0].id=s.displayList[1].id;}
 ]){const trial=scene();change(trial);assert.throws(()=>draftLayout(trial,manifest));}
});
