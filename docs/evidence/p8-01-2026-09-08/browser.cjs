const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage(),errors=[],rows=[];page.on('pageerror',e=>errors.push(String(e)));
const canvas=page.locator('canvas');
async function point(x,y){const p=await page.evaluate(({x,y})=>window.__relight.world.screenOf(x,y),{x,y}),box=await canvas.boundingBox();return {x:p[0]+box.x,y:p[1]+box.y};}
async function clickTile(p){const q=await point(p.x+.5,p.y+.5);await page.mouse.click(q.x,q.y);await page.waitForTimeout(220);}
try{for(const width of [1366,900]){
 await page.setViewportSize({width,height:900});await page.goto('http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world');await page.waitForFunction(()=>window.__relight?.session.state.campaign?.version===10);
 await page.getByRole('button',{name:'Pause',exact:true}).click();await page.waitForTimeout(300);
 const locked=await page.evaluate(()=>window.__relight.stateHash());await page.keyboard.press('Control+c');assert.equal(await page.evaluate(()=>window.__relight.stateHash()),locked);
 const home=await page.evaluate(()=>{const a=window.__relight,e=a.engineer();a.chest.take('steel',80);a.chest.take('copper',40);return {x:e.x,y:e.y};});
 await page.getByRole('button',{name:'1×',exact:true}).click();const crew=await page.evaluate(()=>{const a=window.__relight,r=a.session.state.campaign.recruits.sites.find(r=>r.kind==='foreman');a.walkTo(r.x,r.y);a.run(40);return r;});
 await page.getByRole('button',{name:'Pause',exact:true}).click();await page.waitForTimeout(400);
 if(width===1366){await page.getByRole('button',{name:'Discoveries',exact:true}).first().click();await page.getByRole('button',{name:'Recruit Foreman',exact:true}).click();}
 else{await page.evaluate(()=>window.scrollTo(0,0));await clickTile(crew);await page.keyboard.press('e');}
 await page.waitForFunction(()=>window.__relight.session.state.campaign.recruits.sites.find(r=>r.kind==='foreman').recruitedAt>=0);
 await page.getByRole('button',{name:'1×',exact:true}).click();await page.evaluate(home=>{const a=window.__relight;a.walkTo(Math.floor(home.x),Math.floor(home.y));a.run(40);},home);await page.getByRole('button',{name:'Pause',exact:true}).click();
 const layout=await page.evaluate(()=>{const a=window.__relight,e=a.engineer();let p;
  for(let y=Math.floor(e.y)-3;y<e.y+3&&!p;y++)for(let x=Math.floor(e.x)-3;x<e.x+2;x++){let good=true;for(let yy=y;yy<y+4;yy++)for(let xx=x;xx<x+2;xx++)if(!a.flow.canPlace('belt',xx,yy).ok)good=false;if(good){p={x,y};break;}}
  if(!p)throw Error('no source/destination');if(!a.flow.place('belt',p.x,p.y,1)||!a.flow.place('belt',p.x+1,p.y,1))throw Error('paid source failed');a.world.setZoom(1);return {p,q:{x:p.x,y:p.y+2},steel:a.engineer().inv.steel};});
 const panel=page.getByLabel('Blueprint clipboard',{exact:true});await panel.scrollIntoViewIfNeeded();await page.waitForTimeout(300);
 if(width===1366)await panel.getByRole('button',{name:'Copy area (Ctrl+C)',exact:true}).click();else await page.keyboard.press('Control+c');
 const start=await point(layout.p.x+.1,layout.p.y+.1),end=await point(layout.p.x+1.9,layout.p.y+.9);await page.mouse.move(start.x,start.y);await page.mouse.down();await page.mouse.move(end.x,end.y,{steps:5});await page.mouse.up();
 await page.waitForFunction(()=>window.__relight.session.state.campaign.clipboard?.entities.length===2);
 await page.keyboard.press('r');await page.keyboard.press('h');await panel.getByRole('button',{name:'Mirror V',exact:true}).click();
 const transformed=await page.evaluate(()=>window.__relight.session.state.campaign.clipboard);assert.deepEqual(transformed.entities.map(e=>e.dir),[0,0]);
 const before=await page.evaluate(()=>window.__relight.stateHash());await clickTile(layout.p);assert.equal(await page.evaluate(()=>window.__relight.stateHash()),before,'occupied stamp changed state');
 await page.keyboard.press('Escape');assert.equal(await page.evaluate(()=>window.__relight.stateHash()),before);await page.keyboard.press('Control+v');await clickTile(layout.q);
 const placed=await page.evaluate(q=>{const a=window.__relight,s=a.session.state;return {steel:a.engineer().inv.steel,m:s.flow.machines.filter(m=>m.x===q.x&&(m.y===q.y||m.y===q.y+1)),group:s.construction.undo.at(-1)};},layout.q);
 assert.equal(placed.steel,layout.steel-2);assert.equal(placed.m.length,2);assert.equal(placed.group.length,2);
 await page.keyboard.press('Escape');await page.keyboard.press('Control+z');await page.keyboard.press('Control+y');await page.waitForTimeout(350);
 assert.equal(await page.evaluate(()=>window.__relight.engineer().inv.steel),placed.steel);
 await panel.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(__dirname,`clipboard-${width}.png`)});
 const final=await page.evaluate(()=>({hash:window.__relight.stateHash(),replay:window.__relight.replayHash(),clipboard:window.__relight.session.state.campaign.clipboard,overflow:document.documentElement.scrollWidth>innerWidth}));assert.ok(final.replay.same,JSON.stringify(final.replay));assert.ok(!final.overflow);
 await page.keyboard.press('Control+s');page.once('dialog',d=>d.accept());await page.keyboard.press('Control+o');await page.waitForURL(/state=local/);await page.waitForFunction(()=>window.__relight?.session.state.campaign.clipboard);assert.equal(await page.evaluate(()=>window.__relight.stateHash()),final.hash);
 // Ordinary live light/power edits with the persistent clipboard panel open.
 await page.getByRole('button',{name:'1×',exact:true}).click();const edit=await page.evaluate(()=>{const a=window.__relight,e=a.engineer(),find=kind=>{for(let y=Math.floor(e.y)-3;y<e.y+4;y++)for(let x=Math.floor(e.x)-3;x<e.x+4;x++)if(a.flow.canPlace(kind,x,y).ok)return {x,y};throw Error('no '+kind);};const pole=find('pole');a.flow.place('pole',pole.x,pole.y);const lamp=find('lamp');a.flow.place('lamp',lamp.x,lamp.y);return {pole,lamp};});
 const perf=await page.evaluate(async plan=>{const a=window.__relight,b=a.renderTiming(),start=performance.now(),sim=a.session.state.t,frames=[];let last=start,change=start,on=true,edits=0,success=0;
  await new Promise(resolve=>{function step(t){frames.push(t-last);last=t;if(t-change>500){change=t;for(const kind of ['lamp','pole']){const p=plan[kind];if(on)a.flow.remove(p.x,p.y);else a.flow.place(kind,p.x,p.y);edits++;if(a.session.state.flow.machines.some(m=>m.kind===kind&&m.x===p.x&&m.y===p.y)!==on)success++;}on=!on;}if(t-start<5000)requestAnimationFrame(step);else resolve();}requestAnimationFrame(step);});
  const after=a.renderTiming();return {edits,success,simSeconds:a.session.state.t-sim,meanMs:frames.slice(1).reduce((a,b)=>a+b,0)/(frames.length-1),maxMs:Math.max(...frames.slice(1)),lightPaints:after.lightPaints-b.lightPaints,meanLightPaintMs:(after.lightPaintMs-b.lightPaintMs)/Math.max(1,after.lightPaints-b.lightPaints)};},edit);
 assert.equal(perf.edits,perf.success);assert.ok(perf.edits>=10&&perf.simSeconds>3&&perf.lightPaints>0);rows.push({width,crew,layout,transformed,placed,final,reloaded:true,perf});console.log('PASS',width,'Foreman, clipboard, transforms, atomic refusal, paid stamp, undo/redo, save/replay',JSON.stringify(perf));
}assert.deepEqual(errors,[]);fs.writeFileSync(path.join(__dirname,'browser-result.json'),JSON.stringify({browser:browser.version(),rows,errors,method:'Headless Chrome; ordinary stock/travel/source-build helpers and real DOM/keyboard/pointer recruitment and clipboard controls. No gameplay-state edits. Five-second live lamp/pole samples; not human play or reference-machine certification.'},null,2));
}catch(e){await page.screenshot({path:path.join(__dirname,'browser-failure.png')});fs.writeFileSync(path.join(__dirname,'browser-failure.txt'),await page.locator('body').innerText());throw e;}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
