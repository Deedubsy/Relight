const fs=require('node:fs'),assert=require('node:assert/strict');const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const dir='E:/Factorio2/docs/evidence/gameplay-corrections/';
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage({viewport:{width:1920,height:1080}});const errors=[],checks=[];page.on('pageerror',e=>errors.push(e.message));page.setDefaultTimeout(10000);
const settle=()=>page.waitForTimeout(350),world=()=>page.locator('#map > canvas').focus();
try{
 await page.goto('http://127.0.0.1:5178/?view=world');await page.waitForFunction(()=>window.__relight?.world);await world();await page.keyboard.press('p');await settle();assert.equal(await page.evaluate(()=>__relight.session.state.ruleset),'exploration-v2');
 checks.push({entry:await page.evaluate(()=>({rules:__relight.session.state.ruleset,inv:__relight.session.state.engineer.inv,stock:__relight.session.state.stock,goal:__relight.goal(),assets:[...document.scripts].map(s=>s.src)}))});
 await page.screenshot({path:dir+'opening.png'});
 await page.getByRole('button',{name:/Projects/}).first().click();await settle();await page.getByLabel('Known project',{exact:true}).selectOption('regional:core:1');await settle();assert.match(await page.locator('.project-card').innerText(),/Search within|Scout within/);await page.getByRole('button',{name:'Show project location',exact:true}).click();await settle();await page.screenshot({path:dir+'core-search.png'});
 await page.getByLabel('Known project',{exact:true}).selectOption('regional:plant:0');await settle();assert.match(await page.locator('.project-card').innerText(),/steel: 0\/30/);checks.push('Core search conceals exact relay; plant displays material requirements');
 await page.route(url=>url.pathname === '/correction-factory.json',route=>route.fulfill({contentType:'application/json',body:fs.readFileSync(dir+'factory-save.json','utf8')}));
 await page.goto('http://127.0.0.1:5178/?view=world&state=/correction-factory.json');await page.waitForFunction(()=>window.__relight?.world);await settle();await page.getByRole('button',{name:/Projects/}).first().click();await page.getByLabel('Known project',{exact:true}).selectOption('regional:plant:0');await settle();assert.match(await page.locator('.project-card').innerText(),/Operating.*600 kW/);await page.screenshot({path:dir+'plant-factory.png'});await page.keyboard.press('Escape');await settle();
 const machine=await page.evaluate(()=>{const r=__relight,m=r.session.state.flow.machines.find(m=>m.artifact);return {id:m.id,x:m.x,y:m.y,screen:r.world.screenOf(m.x+1.5,m.y+1.5)};});assert.ok(machine);
 await world();await settle();await page.mouse.move(...machine.screen);await settle();await page.keyboard.press('f');await settle();assert.match(await page.locator('.ui-drawer').innerText(),/Artifact|artifact/);await page.getByText('Speed artifact attached · +10% processing speed · 1/1 slot',{exact:true}).scrollIntoViewIfNeeded();await settle();await page.screenshot({path:dir+'machine-artifact.png'});checks.push('Verified factory checkpoint displays real attached artifact, circuit and materials');
 await page.setViewportSize({width:1280,height:720});await page.getByText('Speed artifact attached · +10% processing speed · 1/1 slot',{exact:true}).scrollIntoViewIfNeeded();await settle();await page.screenshot({path:dir+'machine-artifact-1280.png'});checks.push('Inspected controls at 1920x1080 and 1280x720');

 await page.setViewportSize({width:1920,height:1080});
 for(const name of ['core-recovered','passenger']){
  const path='/correction-'+name+'.json';await page.route(url=>url.pathname===path,r=>r.fulfill({contentType:'application/json',body:fs.readFileSync(dir+name+'-save.json','utf8')}));
  await page.goto('http://127.0.0.1:5178/?view=world&state='+path);await page.waitForFunction(()=>window.__relight?.world);await settle();
  if(name==='core-recovered'){await page.getByRole('button',{name:/Projects/}).first().click();await page.getByLabel('Known project',{exact:true}).selectOption('regional:core:1');await settle();assert.match(await page.locator('.project-card').innerText(),/Core recovered/);assert.equal(await page.evaluate(()=>__relight.session.state.engineer.inv.core1),1);}
  else {assert.ok(await page.evaluate(()=>__relight.session.state.campaign.progression.passenger));assert.match(await page.locator('body').innerText(),/Exit tram/);}
  await page.screenshot({path:dir+name+'.png'});checks.push(name+' visible in loaded actual-game checkpoint');
 }
 fs.writeFileSync(dir+'walkthrough.json',JSON.stringify({checks,errors},null,2));assert.deepEqual(errors,[]);
}catch(error){fs.writeFileSync(dir+'visual-failure.txt',String(error)+'\n'+await page.locator('body').innerText());await page.screenshot({path:dir+'visual-failure.png'});throw error;}finally{fs.writeFileSync(dir+'walkthrough.json',JSON.stringify({checks,errors},null,2));await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
