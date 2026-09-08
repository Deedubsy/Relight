const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage({viewport:{width:1366,height:900}}),rows=[],errors=[];page.on('pageerror',e=>errors.push(String(e)));
try{await page.addInitScript(s=>localStorage.setItem('relight.save.p602-arrival',JSON.stringify(s)),JSON.parse(fs.readFileSync(path.join(__dirname,'arrival.json'),'utf8')));
for(const width of [1366,900]){
await page.setViewportSize({width,height:900});await page.goto('http://127.0.0.1:5176/?rules=exploration-v2&view=world&state=local:p602-arrival');await page.waitForFunction(()=>window.__relight?.world);await page.waitForTimeout(400);
const e=await page.evaluate(()=>window.__relight.engineer()),box=await page.locator('canvas').boundingBox(),pt=await page.evaluate(e=>window.__relight.world.screenOf(e.x+1,e.y+2),e);
await page.keyboard.press('1');await page.mouse.move(pt[0]+box.x,pt[1]+box.y);await page.mouse.down();await page.keyboard.press('p');await page.waitForFunction(()=>window.__relight.session.state.t>=3301);
assert.match(await page.locator('#threat-direction').innerText(),/assault.*off-screen/);assert.equal(await page.evaluate(()=>window.__relight.world.tool),'belt');assert.equal(await page.getByRole('button',{name:'Return to engineer (K)',exact:true}).isVisible(),false);
const stationary=await page.evaluate(()=>window.__relight.engineer());assert.equal(stationary.x,e.x);assert.equal(stationary.y,e.y);
await page.screenshot({path:path.join(__dirname,'arrival-during-build-'+width+'.png')});await page.mouse.up();await page.keyboard.down('d');await page.waitForTimeout(700);await page.keyboard.up('d');const travelled=await page.evaluate(()=>window.__relight.engineer());assert.ok(travelled.walked>e.walked);assert.equal(await page.evaluate(()=>window.__relight.world.tool),'belt');
rows.push({width,before:e,after:travelled,phase:await page.locator('#threat-direction').innerText()});console.log('PASS natural dusk arrival during held belt placement and ordinary movement',width);
}
assert.deepEqual(errors,[]);fs.writeFileSync(path.join(__dirname,'arrival-result.json'),JSON.stringify({rows,errors,fixture:'Declared restored-base/radio/clock checkpoint two seconds before dusk; paid stock. Dusk transition, held construction and travel run through normal live simulation and actual mouse/keyboard.'},null,2)+'\n');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
