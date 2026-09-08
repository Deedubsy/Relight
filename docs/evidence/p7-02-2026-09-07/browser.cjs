const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage(),errors=[],rows=[];
page.on('pageerror',e=>errors.push(String(e)));
const pointer=async(x,y)=>{await page.evaluate(()=>window.scrollTo(0,0));await page.waitForTimeout(250);const xy=await page.evaluate(([x,y])=>{const p=window.__relight.world.screenOf(x,y),b=document.querySelector('canvas').getBoundingClientRect();return [p[0]+b.x,p[1]+b.y];},[x,y]);await page.mouse.move(...xy);await page.waitForTimeout(150);return xy;};
try{for(const width of [1366,900]){
 await page.setViewportSize({width,height:900});await page.goto('http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world');
 await page.waitForFunction(()=>window.__relight?.session.state.campaign?.version===7);
 await page.getByRole('button',{name:'1×',exact:true}).click();
 const before=await page.evaluate(()=>{const a=window.__relight,s=a.session.state,r=s.campaign.recruits.sites.find(s=>s.kind==='concrete');a.chest.take('steel',120);a.chest.take('copper',50);a.chest.take('stone',40);const home={x:Math.floor(s.engineer.x),y:Math.floor(s.engineer.y)};a.walkTo(r.x,r.y);a.run(40);return {home,site:{...r},inv:{...s.engineer.inv}};});
 if(width===1366)await page.getByRole('button',{name:'Recruit Concrete crew',exact:true}).click();
 else {await pointer(before.site.x+.5,before.site.y+.5);await page.keyboard.press('e');}
 await page.waitForFunction(()=>window.__relight.session.state.campaign.recruits.sites.find(s=>s.kind==='concrete').recruitedAt>=0);
 const inv=await page.evaluate(()=>window.__relight.session.state.engineer.inv);assert.deepEqual(inv,before.inv);
 await page.getByRole('button',{name:'Recruited Concrete crew',exact:true}).scrollIntoViewIfNeeded();await page.screenshot({path:path.join(__dirname,`crew-${width}.png`)});
 const plan=await page.evaluate(home=>{const a=window.__relight,s=a.session.state;a.walkTo(home.x,home.y);a.run(40);const [bx,by]=a.flow.hq(0,0);
  for(let y=by+2;y<by+20;y++)for(let x=bx+5;x<bx+18;x++) {const plan=[['mixer',x,y],['chest',x-4,y+1],['inserter',x-2,y+1],['belt',x-1,y+1],['inserter',x+3,y+1],['chest',x+4,y+1]];if(plan.every(([k,x,y])=>a.flow.canPlace(k,x,y).ok)){a.walkTo(x,y);a.run(10);return plan;}}throw Error('no concrete line');},before.home);
 await page.keyboard.press('b');await page.getByRole('button',{name:'mixer',exact:true}).click();
 await page.waitForTimeout(250);
 const xy=await pointer(plan[0][1]+1.5,plan[0][2]+1.5);await page.mouse.click(...xy);
 await page.waitForFunction(()=>window.__relight.session.state.flow.machines.some(m=>m.kind==='mixer'));
 const produced=await page.evaluate(plan=>{const a=window.__relight,s=a.session.state;for(const [item,x,y] of plan.slice(1)){a.walkTo(x,y);a.run(8);a.session.pending.push({type:'construct',edits:[{action:'place',item,x,y,dir:1}]});a.run(1);if(!s.flow.machines.some(m=>m.kind===item&&m.x===x&&m.y===y))throw Error('failed '+item);}
  const [x,y]=plan[1].slice(1);a.walkTo(x,y);a.run(8);a.session.pending.push({type:'factory',action:{type:'chestPut',x,y,item:'stone',n:40}});a.run(50);
  const [ox,oy]=plan[5].slice(1),out=s.flow.machines.find(m=>m.x===ox&&m.y===oy);a.walkTo(ox,oy);a.run(8);a.session.pending.push({type:'factory',action:{type:'chestTake',x:ox,y:oy,item:'concrete',n:20}});a.run(1);
  return {made:s.flow.stats.made.concrete,carried:s.engineer.inv.concrete,output:out.inv.concrete??0,replay:a.replayHash()};},plan);
 assert.equal(produced.made,20);assert.equal(produced.carried,20);assert.ok(produced.replay.same);
 // Actual inspector key shows the concrete recipe and local draw.
 await page.keyboard.press('Escape');await page.waitForTimeout(250);
 await pointer(plan[0][1]+1.5,plan[0][2]+1.5);await page.keyboard.press('f');await page.waitForTimeout(300);
 assert.ok((await page.locator('body').innerText()).includes('Concrete'));await page.screenshot({path:path.join(__dirname,`mixer-${width}.png`)});
 await page.keyboard.press('Escape');
 const spot=await page.evaluate(()=>{const a=window.__relight,s=a.session.state;for(let y=Math.floor(s.engineer.y)-5;y<=s.engineer.y+5;y++)for(let x=Math.floor(s.engineer.x)-5;x<=s.engineer.x+5;x++)if(a.flow.canPlace('barricade',x,y).ok)return [x,y];throw Error('no barricade');});
 await page.keyboard.press('b');await page.getByRole('button',{name:'barricade',exact:true}).click();await page.waitForTimeout(250);
 const bp=await pointer(spot[0]+.5,spot[1]+.5);await page.mouse.click(...bp);await page.waitForFunction(()=>window.__relight.session.state.flow.machines.some(m=>m.kind==='barricade'));
 await page.getByRole('button',{name:'Pause',exact:true}).click();
 const final=await page.evaluate(()=>{const a=window.__relight,s=a.session.state;return {hash:a.stateHash(),replay:a.replayHash(),barricade:s.flow.machines.find(m=>m.kind==='barricade'),concrete:s.engineer.inv.concrete,overflow:document.documentElement.scrollWidth>innerWidth,save:a.saveFile()};});
 assert.equal(final.concrete,16);assert.ok(final.replay.same);assert.ok(!final.overflow);await page.keyboard.press('Escape');await pointer(spot[0]+.5,spot[1]+.5);await page.screenshot({path:path.join(__dirname,`barricade-${width}.png`)});
 if(width===1366)fs.writeFileSync(path.join(__dirname,'concrete-line.json'),JSON.stringify(final.save,null,2));delete final.save;
 await page.keyboard.press('Control+s');page.once('dialog',d=>d.accept());await page.keyboard.press('Control+o');await page.waitForURL(/state=local/);await page.waitForFunction(()=>window.__relight?.world);
 assert.equal(await page.evaluate(()=>window.__relight.stateHash()),final.hash);rows.push({width,before,plan,produced,final,saveReload:true});console.log('PASS',width,'crew, paid Mixer, conveyor production, inspector, paid Barricade, replay and save');
 }assert.deepEqual(errors,[]);fs.writeFileSync(path.join(__dirname,'browser.json'),JSON.stringify({browser:browser.version(),rows,errors,method:'Isolated Chrome: ordinary queued commands for logistics; actual recruitment/build/inspection/save controls. No gameplay-state edits or human play verdict.'},null,2));
}catch(e){await page.screenshot({path:path.join(__dirname,'failure.png')});fs.writeFileSync(path.join(__dirname,'failure.txt'),await page.locator('body').innerText());console.log(await page.evaluate(()=>({tool:window.__relight.world.tool,engineer:window.__relight.engineer()})));throw e;}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
