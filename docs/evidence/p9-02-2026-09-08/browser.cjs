const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage(),errors=[],rows=[];page.on('pageerror',e=>errors.push(String(e)));
try{for(const width of [1366,900])for(const name of ['old','new']){
 const expected=JSON.parse(fs.readFileSync(path.join(__dirname,name==='old'?'old-seed-6.json':'paid-save-6.json'),'utf8'));
 await page.setViewportSize({width,height:900});await page.goto(`http://127.0.0.1:5178/?rules=exploration-v2&seed=6&view=world&state=/p902-${name}-6.json`);await page.waitForFunction(()=>window.__relight?.world);
 const read=()=>page.evaluate(()=>{const a=window.__relight,s=a.session.state;return {hash:a.stateHash(),speed:s.speed,complete:a.session.logComplete,route:s.campaign.expansion.route,surveyVersion:s.campaign.expansion.surveyVersion??1,inventory:s.engineer.inv,machines:s.flow.machines.length};});
 const before=await read();assert.equal(before.speed,0);assert.equal(before.complete,name==='new');assert.equal(before.surveyVersion,name==='new'?2:1);assert.deepEqual(before.route,expected.state.campaign.expansion.route);assert.deepEqual(before.inventory,expected.state.engineer.inv);assert.equal(before.machines,expected.state.flow.machines.length);
 await page.waitForTimeout(300);assert.deepEqual(await read(),before);await page.screenshot({path:path.join(__dirname,`${name}-${width}.png`)});
 await page.keyboard.press('Control+s');page.once('dialog',d=>d.accept());await page.keyboard.press('Control+o');await page.waitForURL(/state=local/);await page.waitForFunction(()=>window.__relight?.world);assert.deepEqual(await read(),before);
 const replay=await page.evaluate(()=>window.__relight.replayHash());if(name==='new')assert.ok(replay.same,JSON.stringify(replay));else assert.ok(replay.error);
 rows.push({width,name,...before,replay,saveReload:true});console.log('PASS',width,name,before.hash);
 }assert.deepEqual(errors,[]);fs.writeFileSync(path.join(__dirname,'browser-result.json'),JSON.stringify({browser:browser.version(),rows,errors,method:'Headless Chrome, isolated storage, actual Ctrl+S/Ctrl+O; no gameplay state injection. Automated compatibility evidence, not human play.'},null,2)+'\n');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
