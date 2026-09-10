const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage(),errors=[],rows=[];
 page.on('pageerror',e=>errors.push(String(e)));
 const fixtures=[102,305,842].map(seed=>({id:String(seed),file:path.join(__dirname,`fresh-${seed}.json`),old:false}));
 fixtures.push({id:'old',file:path.resolve(__dirname,'../p9-04-2026-09-08/paid/save-3.json'),old:true});
 for(const f of fixtures)await page.route(u=>u.pathname===`/p904r-${f.id}.json`,r=>r.fulfill({contentType:'application/json',body:fs.readFileSync(f.file,'utf8')}));
 const read=()=>page.evaluate(()=>{const a=window.__relight;return {hash:a.stateHash(),survey:a.session.state.campaign.expansion.surveyVersion,logComplete:a.session.logComplete,logLength:a.session.log.length,view:a.view.mode,overflow:document.documentElement.scrollWidth>innerWidth};});
 try{
  for(const width of [1366,900])for(const f of fixtures){
   await page.setViewportSize({width,height:900});
   await page.goto(`http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world&state=/p904r-${f.id}.json`);
   await page.waitForFunction(()=>window.__relight?.world);
   const expected=JSON.parse(fs.readFileSync(f.file,'utf8')),initial=await read();
   if(initial.hash!==expected.hash){fs.writeFileSync(path.join(__dirname,'browser-state.json'),JSON.stringify(await page.evaluate(()=>window.__relight.session.state)));fs.writeFileSync(path.join(__dirname,'browser-state-meta.json'),JSON.stringify(initial));}assert.equal(initial.hash,expected.hash);assert.equal(initial.logComplete,!f.old);assert.equal(initial.logLength,expected.log.length);assert.equal(initial.survey,f.old?2:3);assert.ok(!initial.overflow);
   await page.getByRole('button',{name:/^(Map|World) view \(M\)$/}).click();
   await page.waitForFunction(()=>window.__relight.view.mode==='map');assert.equal((await read()).hash,initial.hash);
   if(width===1366&&!f.old)await page.screenshot({path:path.join(__dirname,`map-${f.id}.png`)});
   await page.getByRole('button',{name:/^(Map|World) view \(M\)$/}).click();
   await page.waitForFunction(()=>window.__relight.view.mode==='world');
   await page.locator('canvas').focus();await page.keyboard.press('Control+s');page.once('dialog',d=>d.accept());await page.keyboard.press('Control+o');
   await page.waitForURL(/state=local/);await page.waitForFunction(()=>window.__relight?.world);
   const loaded=await read();assert.equal(loaded.hash,initial.hash);assert.equal(loaded.logComplete,!f.old);assert.equal(loaded.logLength,expected.log.length);
   const replay=f.old?null:await page.evaluate(()=>window.__relight.replayHash());if(replay)assert.ok(replay.same,JSON.stringify(replay));
   if(width===1366&&f.id==='102')await page.screenshot({path:path.join(__dirname,'world-102.png')});
   rows.push({width,fixture:f.id,initial,loaded,replay});console.log('PASS',width,f.id);
  }
  assert.deepEqual(errors,[]);fs.writeFileSync(path.join(__dirname,'browser-result.json'),JSON.stringify({browser:browser.version(),rows,errors,method:'Actual UI map/world and local save/reload in headless Chrome; fresh repaired seeds replay exactly, revision-2 paid save retains its hash and incomplete fresh-factory provenance. Not human play or performance certification.'},null,2)+'\n');
 }catch(error){await page.screenshot({path:path.join(__dirname,'browser-failure.png')});throw error;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
