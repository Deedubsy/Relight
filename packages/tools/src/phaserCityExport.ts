import {writeFileSync,mkdirSync} from 'node:fs';
import {resolve,join} from 'node:path';
import {fileURLToPath} from 'node:url';
import {randomUUID} from 'node:crypto';
import {riverfrontRail} from '@relight/sim';
import {hash,readLayout,roof,type SceneObject,type EditorScene,type Manifest} from './phaserCityModel';
const root=resolve(fileURLToPath(new URL('../../../',import.meta.url))),dir=join(root,'maps/phaser'),scenePath=join(root,'packages/game/src/editor/RiverfrontCity.scene');
const write=(p:string,v:unknown)=>writeFileSync(p,JSON.stringify(v,null,2)+'\n');
/** Caller verifies and archives existing edits before refreshing this editable baseline. */
export function exportEditorScene(source:string,previous?:EditorScene){
 mkdirSync(dir,{recursive:true});mkdirSync(resolve(scenePath,'..'),{recursive:true});
 const base=readLayout(source),C=base.city,buildings=base.buildings,props=base.props,objects:SceneObject[]=[],bindings:Record<string,string>={};
 const add=(label:string,type:string,data:Record<string,unknown>)=>{const o={id:previous?.displayList.find(o=>o.label===label)?.id??randomUUID(),label,type,...data} as SceneObject;objects.push(o);return o;};
 // One low-resolution terrain reference keeps the full 27,648px-wide city within GPU limits.
 const svg:string[]=[`<svg xmlns="http://www.w3.org/2000/svg" width="3456" height="2304" viewBox="0 0 ${C.width} ${C.height}"><rect width="864" height="576" fill="#52614b"/>`];
 const rect=(p:{x:number;y:number;w:number;h:number},c:string)=>svg.push(`<rect x="${p.x}" y="${p.y}" width="${p.w}" height="${p.h}" fill="${c}"/>`);
 const stroke=(p:readonly (readonly number[])[],w:number,c:string)=>svg.push(`<polyline points="${p.map(([x,y])=>`${x+.5},${y+.5}`).join(' ')}" fill="none" stroke="${c}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round"/>`);
 svg.push(`<path d="M0 576V${C.riverY} ${Array.from({length:C.width+1},(_,x)=>`L${x} ${C.riverY+Math.round(6*Math.sin(x/52))}`).join(' ')}V576Z" fill="#204853"/>`);
 for(const p of C.serviceAreas)rect(p,'#70776a');for(const p of C.drives)stroke(p,4,'#858b7e');
 for(const [w,c]of [[10,'#858b7e'],[6,'#46504d']] as const){for(const p of C.roads)stroke(p,w,c);svg.push(`<circle cx="${C.court.x+.5}" cy="${C.court.y+.5}" r="${C.court.radius-(w===10?0:2)}" fill="${c}"/>`);}
 stroke(riverfrontRail().samples.map(p=>[p.x-.5,p.y-.5]),2.8,'#b5995a');
 for(const p of C.paths)stroke(p,1.4,'#929683');for(const p of [...C.yards,...C.squares])rect(p,'#7c7b68');
 for(const p of props)rect(p,p.kind==='tree'?'#324f36':'#95866b');
 for(const [item,x,y,w,h]of C.resources)rect({x,y,w,h},item.includes('copper')?'#c08854':item==='coal'?'#273338':'#a4b2b0');
 for(const [i,p]of C.yards.entries())svg.push(`<text x="${p.x+1}" y="${p.y+4}" font-size="2.5" fill="#dcddd0">${['HOME YARD','RIVERSIDE','IRONWORKS','CIVIC'][i]}</text>`);
 for(const p of [...C.stops,...C.plants,...C.cores,...C.artifacts]){rect({...p,w:2,h:2},p.id.startsWith('tram')?'#ffc94b':'#a6ded3');svg.push(`<text x="${p.x+3}" y="${p.y+1}" font-size="2" fill="#eeeecc">${p.name}</text>`);}
 svg.push('</svg>');
 const bg='art/editor/riverfront-ground.svg';mkdirSync(join(root,'packages/game/public/art/editor'),{recursive:true});writeFileSync(join(root,'packages/game/public',bg),svg.join('\n'));
 add('REFERENCE_ground_roads_paths_sites','Image',{displayName:'REFERENCE - terrain, paths, props, tram and sites',texture:{key:'editor-riverfront-ground'},x:0,y:0,originX:0,originY:0,scaleX:8,scaleY:8});
 const points=[...C.plants,...C.cores,...C.artifacts,...Object.values(C.projects).map(([x,y])=>({x,y})),...C.recruits.map(([,x,y])=>({x,y}))];
 const fixedBuildings=buildings.filter(b=>b.id==='home-workshop'||!!(b.compound||b.enterable||b.content||b.note||b.doors?.length)||points.some(p=>p.x>=b.x&&p.x<b.x+b.w&&p.y>=b.y&&p.y<b.y+b.h)).map(b=>b.id);
 for(const b of buildings){const fixed=fixedBuildings.includes(b.id);const o=add(b.id.replaceAll('-','_'),'Image',{displayName:`${fixed?'FIXED - ':''}${b.name} [${b.id}]`,texture:{key:roof(b)},x:b.x*32,y:b.y*32,originX:0,originY:0,scaleX:b.w*32/320,scaleY:b.h*32/256});bindings[o.id]=b.id;}
 for(const b of buildings){const label=b.id.replaceAll('-','_'),p=b.parcel;add('PLOT_'+label,'Rectangle',{displayName:'Plot · '+b.name,x:p.x*32,y:p.y*32,width:p.w*32,height:p.h*32,originX:0,originY:0,isFilled:false,isStroked:true,strokeColor:'#86c7bd',strokeAlpha:.5,lineWidth:2});}
 const scene={id:previous?.id??randomUUID(),sceneType:'SCENE',settings:{compilerEnabled:false,compilerOutputLanguage:'TYPE_SCRIPT',exportClass:true,autoImport:true,sceneKey:'RiverfrontCity',borderWidth:C.width*32,borderHeight:C.height*32,snapEnabled:true,snapWidth:32,snapHeight:32},displayList:objects,plainObjects:[],meta:{app:'Phaser Editor - Scene Editor',url:'https://phaser.io/editor',contentType:'phasereditor2d.core.scene.SceneContentType',version:5}};
 write(scenePath,scene);write(join(dir,'manifest.json'),{version:1,sourceHash:hash(source),base,objects:objects.filter(o=>!o.label.startsWith('PLOT_')),bindings,fixedBuildings} satisfies Manifest);writeFileSync(join(dir,'RiverfrontCity.source.txt'),source);
 write(join(root,'packages/game/public/relight-editor-pack.json'),{editor:{files:[{type:'svg',key:'editor-riverfront-ground',url:bg}]},meta:{app:'Phaser Editor',contentType:'phasereditor2d.pack.core.AssetContentType',version:2}});
 console.log(`Created ${scenePath}: ${buildings.length} buildings (${fixedBuildings.length} campaign anchors fixed).`);

}
