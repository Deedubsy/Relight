const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage(),errors=[],rows=[];
page.on('pageerror',e=>errors.push(String(e)));
try{for(const width of [1366,900]){
 await page.setViewportSize({width,height:900});await page.goto('http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world');
 await page.waitForFunction(()=>window.__relight?.session.state.campaign?.recruits);
 await page.getByRole('button',{name:'1×',exact:true}).click();
 const before=await page.evaluate(()=>{const a=window.__relight,s=a.session.state,r=s.campaign.recruits.sites[0];a.chest.take('steel',70);a.chest.take('copper',40);const home={x:s.engineer.x,y:s.engineer.y};a.walkTo(r.x,r.y);a.run(40);return {home,site:{...r},inv:{...s.engineer.inv}};});
 if(width===1366)await page.getByRole('button',{name:'Recruit Electricians',exact:true}).click();
 else {await page.evaluate(()=>window.scrollTo(0,0));await page.waitForTimeout(200);const xy=await page.evaluate(()=>{const a=window.__relight,r=a.session.state.campaign.recruits.sites[0],p=a.world.screenOf(r.x+.5,r.y+.5),b=document.querySelector('canvas').getBoundingClientRect();return [p[0]+b.x,p[1]+b.y];});await page.mouse.move(xy[0],xy[1]);await page.waitForTimeout(200);await page.keyboard.press('e');}
 await page.waitForFunction(()=>window.__relight.session.state.campaign.recruits.sites[0].recruitedAt>=0);
 await page.getByRole('button',{name:'Pause',exact:true}).click();
 const acquired=await page.evaluate(()=>{const a=window.__relight,s=a.session.state;return {site:s.campaign.recruits.sites[0],inv:s.engineer.inv,hash:a.stateHash(),replay:a.replayHash(),overflow:document.documentElement.scrollWidth>innerWidth};});
 assert.deepEqual(acquired.inv,before.inv);assert.ok(acquired.replay.same);assert.ok(!acquired.overflow);
 await page.getByRole('button',{name:'Electricians recruited',exact:true}).scrollIntoViewIfNeeded();
 await page.screenshot({path:path.join(__dirname,'recruited-'+width+'.png')});
 await page.keyboard.press('Control+s');page.once('dialog',d=>d.accept());await page.keyboard.press('Control+o');
 await page.waitForURL(/state=local/);await page.waitForFunction(()=>window.__relight?.world);
 assert.equal(await page.evaluate(()=>window.__relight.stateHash()),acquired.hash);
 assert.ok(await page.getByRole('button',{name:'Electricians recruited',exact:true}).isDisabled());
 await page.getByRole('button',{name:'1×',exact:true}).click();
 const built=await page.evaluate(home=>{const a=window.__relight,s=a.session.state;a.walkTo(Math.floor(home.x),Math.floor(home.y));a.run(40);const made=[];
  for(const kind of ['floodlight','bigpole']){let done=false;for(let y=Math.floor(s.engineer.y)-5;y<=s.engineer.y+5&&!done;y++)for(let x=Math.floor(s.engineer.x)-5;x<=s.engineer.x+5;x++)if(a.flow.canPlace(kind,x,y).ok){const m=a.flow.place(kind,x,y);if(m){made.push({kind,x,y});done=true;break;}}if(!done)throw Error('cannot build '+kind);}
  a.run(1);return {made,replay:a.replayHash(),render:a.renderTiming()};},before.home);
 assert.equal(built.made.length,2);assert.ok(built.replay.same);rows.push({width,before,acquired,built,saveReload:true});
 console.log('PASS',width,'recruit UI, paid machines, replay, save/load');
 }assert.deepEqual(errors,[]);fs.writeFileSync(path.join(__dirname,'browser.json'),JSON.stringify({browser:browser.version(),rows,errors,method:'Isolated headless Chrome; ordinary command hooks for walking/stock/placement, actual recruitment button and Ctrl+S/O. No human play verdict.'},null,2));
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
