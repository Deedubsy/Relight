// Temporary ordinary importer apply -> real game -> importer restore. No user scene edits.
const fs=require('fs'),assert=require('assert/strict'),{spawnSync}=require('child_process');
const{chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const source='packages/sim/src/city/riverfront.ts',initial=fs.readFileSync(source,'utf8');
function tool(mode,file=''){const args=['-d','Ubuntu-24.04','--','bash','-lc',`export PATH=/home/deedub/.nvm/versions/node/v22.18.0/bin:/usr/bin:/bin; cd /mnt/e/Factorio2 && node --import tsx packages/tools/src/phaserCity.ts ${mode} ${file}`];const r=spawnSync('wsl.exe',args,{encoding:'utf8',windowsHide:true});return{status:r.status,output:r.stdout+r.stderr};}
(async()=>{let browser;const result={};try{
 const invalid=tool('apply','docs/evidence/phaser-scene/invalid.scene');assert.notEqual(invalid.status,0);assert.equal(fs.readFileSync(source,'utf8'),initial);result.invalidRejected=invalid;
 const applied=tool('apply','docs/evidence/phaser-scene/moved.scene');assert.equal(applied.status,0,applied.output);result.applied=applied;
 // Confirm the blueprint generator's overwrite guard fires before any output is changed.
 const guard=spawnSync('C:/Users/Admin/AppData/Local/Programs/Python/Python310/python.exe',['docs/evidence/city-c/authorFullCity.py'],{encoding:'utf8',windowsHide:true});assert.notEqual(guard.status,0);assert.match(guard.stderr,/Phaser Editor layout is applied/);result.generatorGuard=true;
 browser=await chromium.launch({channel:'chrome',headless:true});const page=await browser.newPage({viewport:{width:1440,height:900}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:5190/?view=world');await page.waitForFunction(()=>window.__relight?.session?.state.city?.mapId?.includes('-editor-'));
 result.game=await page.evaluate(async()=>{const main=await(await fetch('/src/main.ts')).text(),url=[...main.matchAll(/from "([^"]+)"/g)].map(m=>m[1]).find(p=>p.includes('/sim/src/index.ts'));if(!url)throw Error('Simulation module URL not found');const S=await import(url),st=__relight.session.state,b=S.RIVERFRONT_BUILDINGS.find(b=>b.id==='court-northwest-home');
  const loaded=S.loadState(S.makeSave(st)),old=S.makeSave(st);old.state.city.mapId='riverfront-arc-v4';
  const evidence={mapId:st.city.mapId,building:[b.x,b.y,b.variant],door:b.door,oldWallPassable:S.passable(st,31,351),newWallPassable:S.passable(st,32,351),stateProblem:S.stateProblem(st),saveHashMatches:S.stateHash(loaded)===S.stateHash(st),oldSaveProblem:S.stateProblem(old.state)};
  // Explicit visual setup only: camera follows the engineer, so place them beside the tested house.
  st.engineer.x=47;st.engineer.y=355;st.speed=0;__relight.world.setZoom(.8);
  return evidence;
 });
 assert.deepEqual(result.game.building,[32,351,1]);assert.deepEqual(result.game.door,[41,354]);assert.equal(result.game.oldWallPassable,true);assert.equal(result.game.newWallPassable,false);assert.equal(result.game.stateProblem,'');assert.equal(result.game.saveHashMatches,true);assert.match(result.game.oldSaveProblem,/original build/);assert.deepEqual(errors,[]);result.pageErrors=errors;
 await page.waitForTimeout(500);await page.screenshot({path:'docs/evidence/phaser-scene/applied-game.png'});
}finally{if(browser)await browser.close();const restored=tool('apply');result.restored=restored;assert.equal(restored.status,0,restored.output);assert.equal(fs.readFileSync(source,'utf8'),initial);result.originalSourceRestored=true;fs.writeFileSync('docs/evidence/phaser-scene/roundtrip.json',JSON.stringify(result,null,2));}
console.log(JSON.stringify(result.game));})().catch(e=>{console.error(e);process.exitCode=1});
