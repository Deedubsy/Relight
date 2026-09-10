/** Add missing building assets without overwriting entries edited in Phaser Editor. */
import {readFileSync,readdirSync,existsSync,writeFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {resolve} from 'node:path';
const root=fileURLToPath(new URL('../',import.meta.url));
const publicDir=resolve(root,'packages/game/public'),file=resolve(publicDir,'relight-asset-pack.json');
const pack=existsSync(file)?JSON.parse(readFileSync(file,'utf8')):{riverfront:{files:[]},meta:{app:'Phaser Editor',contentType:'phasereditor2d.pack.core.AssetContentType',version:2}};
const keys=new Set(Object.values(pack).flatMap(section=>section.files??[]).map(entry=>entry.key));
pack.riverfront??={files:[]};let added=0;
for(const name of readdirSync(resolve(publicDir,'art/riverfront')).filter(f=>f.endsWith('.svg')).sort()){
  const key='rf-'+name.slice(0,-4);if(keys.has(key))continue;
  pack.riverfront.files.push({type:'svg',key,url:'art/riverfront/'+name});keys.add(key);added++;
}
writeFileSync(file,JSON.stringify(pack,null,2)+'\n');
console.log(`Registered ${added} new SVG assets; existing pack entries preserved.`);
