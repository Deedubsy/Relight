const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage({viewport:{width:1366,height:900}}),errors=[],checks=[];
 page.on('pageerror',e=>errors.push(String(e)));
 const snapshots=Object.fromEntries(['unknown','warning','assault','withdrawal','recovery','minor','range'].map(n=>[n,JSON.parse(fs.readFileSync(path.join(__dirname,n+'.json'),'utf8'))]));
 await page.addInitScript(data=>{for(const [n,s] of Object.entries(data))localStorage.setItem('relight.save.p6-02-'+n,JSON.stringify(s));},snapshots);
 const load=async(n,view='world')=>{await page.goto('http://127.0.0.1:5176/?rules=exploration-v2&view='+view+'&state=local:p6-02-'+n);await page.waitForFunction(()=>window.__relight?.world);await page.waitForTimeout(450);};
 const check=async(label)=>{assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);assert.deepEqual(errors,[]);checks.push(label);console.log('PASS',label);};
 try{
  for(const width of [1366,900]){
   await page.setViewportSize({width,height:900});await load('warning');
   await page.keyboard.press('1');const before=await page.evaluate(()=>({e:window.__relight.engineer(),hash:window.__relight.stateHash(),tool:window.__relight.world.tool}));
   assert.match(await page.locator('#threat-direction').innerText(),/warning.*off-screen/);assert.match(await page.locator('.goal-support').innerText(),/last received warning.*offline/);
   await page.screenshot({path:path.join(__dirname,'warning-'+width+'.png')});
   await page.keyboard.press('g');await page.waitForTimeout(550);assert.match(await page.locator('#threat-direction').innerText(),/in view/);
   const after=await page.evaluate(()=>({e:window.__relight.engineer(),hash:window.__relight.stateHash(),tool:window.__relight.world.tool}));assert.deepEqual(after,before);
   await page.screenshot({path:path.join(__dirname,'remote-'+width+'.png')});
   await page.keyboard.press('k');await page.waitForTimeout(400);assert.match(await page.locator('#threat-direction').innerText(),/off-screen/);
   // Native button activation by keyboard, not a debug camera hook.
   await page.getByRole('button',{name:'View threat (G)',exact:true}).focus();await page.keyboard.press('Enter');await page.waitForTimeout(250);assert.match(await page.locator('#threat-direction').innerText(),/in view/);
   await page.getByRole('button',{name:'Return to engineer (K)',exact:true}).click();await page.waitForTimeout(250);
   await check('known offline warning, G/K and focused Enter preserve engineer/hash/tool at '+width);
   await page.keyboard.press('Control+s');page.once('dialog',d=>d.accept());await page.keyboard.press('Control+o');await page.waitForURL(/state=local/);await page.waitForTimeout(900);assert.match(await page.locator('#threat-direction').innerText(),/warning/);
   await check('ordinary save/load retains known warning at '+width);
   await load('unknown','map');assert.equal(await page.getByRole('button',{name:'View threat (G)',exact:true}).isVisible(),false);await page.keyboard.press('g');assert.equal(await page.evaluate(()=>window.__relight.view.mode),'map');await check('unknown network target has no navigation, including map start at '+width);
   for(const n of ['minor','assault','withdrawal','recovery']){await load(n);assert.match(await page.locator('#threat-direction').innerText(),new RegExp(n==='minor'?'minor raid':n));await page.screenshot({path:path.join(__dirname,n+'-'+width+'.png')});}
   await load('assault');await page.keyboard.press('1');await page.keyboard.press('p');await page.keyboard.down('w');await page.waitForTimeout(300);await page.keyboard.up('w');await page.keyboard.press('p');assert.equal(await page.evaluate(()=>window.__relight.world.tool),'belt');assert.equal(await page.getByRole('button',{name:'Return to engineer (K)',exact:true}).isVisible(),false);await check('active remote assault leaves construction tool and travelling camera under player control at '+width);
   await load('range');const turret=await page.evaluate(()=>window.__relight.session.state.flow.machines.find(m=>m.kind==='turret'));
   await page.keyboard.press('5');const pt=await page.evaluate(m=>window.__relight.world.screenOf(m.x+3,m.y+3),turret);await page.mouse.move(pt[0]+12,pt[1]+12);await page.waitForTimeout(300);await page.screenshot({path:path.join(__dirname,'placement-range-'+width+'.png')});
   await page.keyboard.press('Escape');const target=await page.evaluate(m=>window.__relight.world.screenOf(m.x+.5,m.y+.5),turret);await page.mouse.move(target[0]+12,target[1]+12);await page.waitForTimeout(200);await page.keyboard.press('f');await page.waitForTimeout(350);assert.match(await page.locator('.inspection-body').innerText(),/Weapon range: [0-9]+ tiles/);
   await page.evaluate(()=>window.scrollTo(0,0));await page.screenshot({path:path.join(__dirname,'selected-range-'+width+'.png')});await check('turret placement and F inspection display real range at '+width);
  }
  assert.deepEqual(errors,[]);fs.writeFileSync(path.join(__dirname,'browser-result.json'),JSON.stringify({browser:browser.version(),checks,errors,fixture:'Injected restored-base/clock/radio receipt checkpoints and paid turret; controls are real browser interactions. No human gameplay or balance verdict.'},null,2)+'\n');
 }catch(e){await page.screenshot({path:path.join(__dirname,'browser-failure.png')});throw e;}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
