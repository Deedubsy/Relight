const assert=require('node:assert/strict'),fs=require('node:fs');
const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage({viewport:{width:1366,height:768}}),errors=[],checks=[];page.on('pageerror',e=>errors.push(e.message));
try{
 for(const fixture of ['ui-fresh','ui-network']){
  await page.goto(`http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world&state=/${fixture}.json`);await page.waitForFunction(()=>window.__relight?.world);await page.waitForTimeout(300);
  assert.equal(await page.evaluate(()=>__relight.session.state.campaign.fixedTram.stops.length),4);
  await page.locator('#map > canvas').focus();if(await page.evaluate(()=>__relight.session.state.speed!==0))await page.keyboard.press('p');
  await page.keyboard.press('m');await page.waitForFunction(()=>__relight.view.mode==='map');await page.waitForTimeout(3000);
  const before=await page.evaluate(()=>{const e=__relight.session.state.engineer;return {x:e.x,y:e.y,target:e.target,moves:__relight.commandLog().filter(l=>l.c.type==='move').length};});
  const canvas=await page.locator('#map > canvas').boundingBox();
  for(const [x,y]of [[.45,.4],[.55,.7],[.7,.55]]){await page.mouse.click(canvas.x+canvas.width*x,canvas.y+canvas.height*y);await page.waitForTimeout(100);}
  const after=await page.evaluate(()=>{const e=__relight.session.state.engineer;return {x:e.x,y:e.y,target:e.target,moves:__relight.commandLog().filter(l=>l.c.type==='move').length};});assert.deepEqual(after,before);checks.push(`${fixture}: map clicks issue no movement and retain player position/target`);
  await page.screenshot({path:`E:/Factorio2/docs/evidence/ui-redesign/ui08-tram-map-${fixture}-1366.png`});
  await page.setViewportSize({width:1920,height:1080});await page.waitForTimeout(350);await page.screenshot({path:`E:/Factorio2/docs/evidence/ui-redesign/ui08-tram-map-${fixture}-1920.png`});
  await page.setViewportSize({width:1366,height:768});await page.waitForTimeout(200);
  await page.keyboard.press('m');await page.waitForFunction(()=>__relight.view.mode==='world');
  const first=await page.evaluate(()=>{const st=__relight.session.state;return st.flow.machines.find(m=>m.id===st.campaign.fixedTram.stops[0]);});
  await page.evaluate(m=>__relight.world.centreOn(m.x+1,m.y+1),first);await page.waitForTimeout(200);
  await page.screenshot({path:`E:/Factorio2/docs/evidence/ui-redesign/ui08-tram-world-${fixture}.png`});
  if(fixture==='ui-network'){const tram=await page.evaluate(()=>{const st=__relight.session.state,m=st.flow.machines.find(m=>m.id===st.campaign.fixedTram.tram);return [m.x,m.y];});await page.locator('#map > canvas').focus();await page.keyboard.press('p');await page.waitForTimeout(6500);await page.keyboard.press('p');const after=await page.evaluate(()=>{const st=__relight.session.state,m=st.flow.machines.find(m=>m.id===st.campaign.fixedTram.tram);return [m.x,m.y];});assert.notDeepEqual(after,tram);checks.push('Network tram moves automatically during normal real-time play');}
 }
 assert.deepEqual(errors,[]);fs.writeFileSync('E:/Factorio2/docs/evidence/ui-redesign/ui08-tram-map-browser.json',JSON.stringify({checks,errors},null,2));console.log(JSON.stringify({checks,errors}));
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1});
