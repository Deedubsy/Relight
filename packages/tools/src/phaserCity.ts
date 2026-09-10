import {readFileSync,writeFileSync,existsSync,renameSync} from 'node:fs';
import {resolve,join} from 'node:path';
import {fileURLToPath} from 'node:url';
import {exportEditorScene} from './phaserCityExport';
import {RIVERFRONT as C,RIVERFRONT_BUILDINGS as buildings,RIVERFRONT_PROPS as props,createCampaign} from '@relight/sim';
import {validateRiverfront} from '../../sim/src/city/validateRiverfront';
import {hash,writeLayout,draftLayout,type EditorScene,type Manifest} from './phaserCityModel';
const root=resolve(fileURLToPath(new URL('../../../',import.meta.url))),dir=join(root,'maps/phaser'),scenePath=join(root,'packages/game/src/editor/RiverfrontCity.scene'),sourcePath=join(root,'packages/sim/src/city/riverfront.ts');
const read=(p:string)=>readFileSync(p,'utf8'),json=(p:string)=>JSON.parse(read(p));
const write=(p:string,v:unknown)=>writeFileSync(p,JSON.stringify(v,null,2)+'\n');
const mode=process.argv[2]??'check';
if(mode==='export'){
 if(existsSync(scenePath)||existsSync(join(dir,'manifest.json')))throw Error('The editor scene already exists. Export refuses to overwrite your edits.');
 exportEditorScene(read(sourcePath));
}else if(mode==='check'||mode==='apply'){
 const manifest=json(join(dir,'manifest.json')) as Manifest,source=read(sourcePath),baseline=read(join(dir,'RiverfrontCity.source.txt'));
 if(hash(baseline)!==manifest.sourceHash)throw Error('Editor baseline changed. Restore the baseline before importing.');
 const reportPath=join(dir,'last-apply.json'),last=existsSync(reportPath)?json(reportPath):null;
 if(![manifest.sourceHash,last?.sourceHash,last?.previousSourceHash].includes(hash(source)))throw Error('Canonical map changed outside the editor. Reconcile it before applying this scene; no files changed.');
 const scene=json(process.argv[3]?resolve(root,process.argv[3]):scenePath) as EditorScene,draft=draftLayout(scene,manifest);
 // This CLI is a fresh process: change the canonical in-memory inputs BEFORE constructing any state/cache.
 Object.assign(C,draft.city);buildings.splice(0,buildings.length,...draft.buildings);props.splice(0,props.length,...draft.props);
 const state=createCampaign(),{reachable,...validation}=validateRiverfront(state);void reachable;
 if(validation.errors.length)throw Error('City validation failed; source unchanged:\n'+validation.errors.join('\n'));
 const changed=JSON.stringify(draft)!==JSON.stringify(manifest.base);
 draft.city.id=changed?`riverfront-arc-v4-editor-${hash(JSON.stringify(draft)).slice(0,12)}`:manifest.base.city.id;
 const output=changed?writeLayout(baseline,draft):baseline;
 const summary={mode,changed,mapId:draft.city.id,sourceHash:hash(output),previousSourceHash:hash(source),validation};
 if(mode==='apply'&&output!==source){
  if(read(sourcePath)!==source)throw Error('Source changed during validation. Retry after reconciling.');
  write(reportPath,summary);writeFileSync(sourcePath+'.editor-tmp',output);renameSync(sourcePath+'.editor-tmp',sourcePath);
 }
 console.log(JSON.stringify(summary,null,2));
 if(mode==='apply')console.log('Applied. Reload the live preview and choose New Game. Existing saves require their matching layout.');
}else throw Error('Use export, check or apply.');
