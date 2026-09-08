const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage(),errors=[],rows=[];page.on('pageerror',e=>errors.push(String(e)));
const pointer=async(x,y)=>{await page.evaluate(()=>window.scrollTo(0,0));await page.waitForTimeout(250);const p=await page.evaluate(([x,y])=>{const a=window.__relight.world.screenOf(x,y),b=document.querySelector('canvas').getBoundingClientRect();return [a[0]+b.x,a[1]+b.y];},[x,y]);await page.mouse.move(...p);await page.waitForTimeout(200);return p;};
try{for(const width of [1366,900]){
 await page.setViewportSize({width,height:900});await page.goto('http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world');await page.waitForFunction(()=>window.__relight?.session.state.campaign?.version===8);await page.getByRole('button',{name:'1×',exact:true}).click();
 for(const kind of width===1366?['lamplighters','surveyors']:['surveyors','lamplighters']){
  const r=await page.evaluate(kind=>{const a=window.__relight,r=a.session.state.campaign.recruits.sites.find(r=>r.kind===kind);a.walkTo(r.x,r.y);a.run(60);return r;},kind);
  if(width===1366)await page.getByRole('button',{name:`Recruit ${kind==='lamplighters'?'Lamplighters':'Surveyors'}`,exact:true}).click();else{await pointer(r.x+.5,r.y+.5);await page.keyboard.press('e');}
  await page.waitForFunction(kind=>window.__relight.session.state.campaign.recruits.sites.find(r=>r.kind===kind).recruitedAt>=0,kind);
 }
 await page.getByRole('button',{name:'Pause',exact:true}).click();assert.ok((await page.evaluate(()=>window.__relight.replayHash())).same);
 await page.evaluate(()=>window.scrollTo(0,0));await page.keyboard.press('m');await page.waitForTimeout(600);
 const survey=await page.evaluate(()=>{const a=window.__relight;return {view:a.view.mode,surveyors:a.session.state.campaign.recruits.sites.find(r=>r.kind==='surveyors').recruitedAt};});
 assert.equal(survey.view,'map');assert.ok(survey.surveyors>=0);await page.screenshot({path:path.join(__dirname,`survey-${width}.png`)});
 // Full ordinary-command checkpoint produced from starting resources, before actual UI commissioning.
 await page.goto('http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world&state=/turbine-pending.json');await page.waitForFunction(()=>window.__relight?.session.state.campaign?.version===8);await page.getByRole('button',{name:'1×',exact:true}).click();
 const h=await page.evaluate(()=>window.__relight.session.state.campaign.turbine);
 if(width===1366)await page.getByRole('button',{name:'Deliver and restore Turbine hall',exact:true}).click();else{await pointer(h.x+1,h.y+1);await page.keyboard.press('e');}
 await page.waitForFunction(()=>window.__relight.session.state.campaign.turbine.restoredAt>=0);
 const spot=await page.evaluate(h=>{const a=window.__relight;for(let y=h.y-2;y<h.y+6;y++)for(let x=h.x-2;x<h.x+6;x++)if(a.flow.canPlace('arclamp',x,y).ok){a.walkTo(x,y);a.run(8);return [x,y];}throw Error('no Arc lamp');},h);
 await page.keyboard.press('b');await page.getByRole('button',{name:'arclamp',exact:true}).click();const p=await pointer(spot[0]+.5,spot[1]+.5);await page.mouse.click(...p);await page.waitForFunction(()=>window.__relight.session.state.flow.machines.some(m=>m.kind==='arclamp'));
 await page.keyboard.press('Escape');await pointer(spot[0]+.5,spot[1]+.5);await page.keyboard.press('f');await page.waitForTimeout(300);assert.match(await page.locator('body').innerText(),/Light radius: 6/);await page.screenshot({path:path.join(__dirname,`arc-${width}.png`)});await page.keyboard.press('Escape');
 await page.evaluate(h=>{const a=window.__relight;a.walkTo(h.x-1,h.y);a.run(8);},h);
 await page.getByRole('button',{name:'Switch Turbine off',exact:true}).click();await page.waitForFunction(()=>!window.__relight.session.state.campaign.turbine.enabled);await page.waitForFunction(()=>document.body.innerText.includes('switched off'));
 await page.getByRole('button',{name:'Switch Turbine on',exact:true}).click();await page.waitForFunction(()=>window.__relight.session.state.campaign.turbine.enabled);await page.getByRole('button',{name:'Pause',exact:true}).click();
 await page.getByRole('button',{name:'Switch Turbine off',exact:true}).scrollIntoViewIfNeeded();await page.screenshot({path:path.join(__dirname,`turbine-${width}.png`)});
 const result=await page.evaluate(()=>{const a=window.__relight,s=a.session.state;return {hash:a.stateHash(),replay:a.replayHash(),hall:s.campaign.turbine,bases:s.campaign.defence.bases.length,concreteSpent:s.flow.stats.placed.concrete,overflow:document.documentElement.scrollWidth>innerWidth,save:a.saveFile()};});
 assert.equal(result.bases,1);assert.equal(result.concreteSpent,40);assert.ok(result.replay.same,JSON.stringify(result.replay));assert.equal(result.overflow,false);fs.writeFileSync(path.join(__dirname,`browser-save-${width}.json`),JSON.stringify(result.save,null,2));delete result.save;
 await page.keyboard.press('Control+s');page.once('dialog',d=>d.accept());await page.keyboard.press('Control+o');await page.waitForURL(/state=local/);await page.waitForFunction(()=>window.__relight?.world);assert.equal(await page.evaluate(()=>window.__relight.stateHash()),result.hash);
 rows.push({width,survey,result,reload:true});console.log('PASS',width,'recruit order, survey isolation, paid Turbine UI, Arc build and inspection, switches, conservation counter, replay, save/load');
}assert.deepEqual(errors,[]);fs.writeFileSync(path.join(__dirname,'browser.json'),JSON.stringify({browser:browser.version(),method:'Headless Chrome; ordinary commands for travel and production checkpoint; actual buttons/E, build, map, inspection and save controls. No gameplay-state edits.',rows,errors},null,2));
}catch(e){await page.screenshot({path:path.join(__dirname,'failure.png')});fs.writeFileSync(path.join(__dirname,'failure.txt'),await page.locator('body').innerText());throw e;}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
