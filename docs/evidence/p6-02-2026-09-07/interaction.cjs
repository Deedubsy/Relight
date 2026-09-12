const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),os=require('node:os');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage({viewport:{width:1366,height:900}}),errors=[],rows=[];page.on('pageerror',e=>errors.push(String(e)));
try{
 await page.addInitScript(s=>localStorage.setItem('relight.save.p602-enemy',JSON.stringify(s)),JSON.parse(fs.readFileSync(path.join(__dirname,'enemy.json'),'utf8')));
 for(const width of [1366,900]){
  await page.setViewportSize({width,height:900});await page.goto('http://127.0.0.1:5176/?rules=exploration-v2&view=world&state=local:p602-enemy');await page.waitForFunction(()=>window.__relight?.world);await page.waitForTimeout(450);
  const pt=await page.evaluate(()=>{const a=window.__relight,c=a.session.state.flow.threat.crawlers[0];return a.world.screenOf(c.x,c.y);}),box=await page.locator('canvas').boundingBox();await page.mouse.move(pt[0]+box.x,pt[1]+box.y);await page.waitForTimeout(200);
  assert.match(await page.locator('#tooltip').innerText(),/now attacking turret.*destination: base core/);await page.screenshot({path:path.join(__dirname,'enemy-action-'+width+'.png')});
 }
 await page.setViewportSize({width:1366,height:900});await page.goto('http://127.0.0.1:5176/?rules=exploration-v2&seed=3&view=world');await page.waitForFunction(()=>window.__relight?.world);await page.keyboard.press('p');
 const setup=await page.evaluate(()=>{
  const a=window.__relight,s=a.session.state; a.chest.take('steel',80);a.chest.take('copper',40);a.chest.take('coal',20);
  const gen=s.flow.machines.find(m=>m.kind==='generator');a.walkTo(gen.x,gen.y+gen.size);a.run(30);
  const e=a.engineer();const pick=kind=>{for(let y=Math.floor(e.y)-4;y<=Math.floor(e.y)+3;y++)for(let x=Math.floor(e.x)-4;x<=Math.floor(e.x)+3;x++)if(a.flow.canPlace(kind,x,y).ok)return {x,y};throw Error('no '+kind+' placement');};
  const pole=pick('pole');assertPlace(a.flow.place('pole',pole.x,pole.y));const lamp=pick('lamp');assertPlace(a.flow.place('lamp',lamp.x,lamp.y));
  const fed=a.flow.feed(gen.x,gen.y);a.run(1);return {gen,pole,lamp,fed,engineer:a.engineer(),summary:a.flow.summary()};
  function assertPlace(m){if(!m)throw Error('placement refused');}
 });
 console.log('SETUP',JSON.stringify(setup));
 await page.getByRole('button',{name:'1×',exact:true}).click();await page.waitForFunction(()=>window.__relight.session.state.speed===1);await page.waitForTimeout(500);
 for(const width of [1366,900]){
  await page.setViewportSize({width,height:900});await page.waitForTimeout(400);
  const r=await page.evaluate(async(lamp)=>{
   const a=window.__relight,before=a.renderTiming(),startSim=a.session.state.t,samples=[],start=performance.now();let prior=start,last=0,on=true,edits=0,successful=0;
   a.world.drawMs();await new Promise(resolve=>{function frame(t){samples.push(t-prior);prior=t;if(t-start-last>500){last=t-start;if(on)a.flow.remove(lamp.x,lamp.y);else a.flow.place('lamp',lamp.x,lamp.y);on=!on;edits++;if(a.session.state.flow.machines.some(m=>m.kind==='lamp'&&m.x===lamp.x&&m.y===lamp.y)===on)successful++;}if(t-start<4500)requestAnimationFrame(frame);else resolve();}requestAnimationFrame(frame);});
   if(!on)a.flow.place('lamp',lamp.x,lamp.y);const after=a.renderTiming(),v=samples.slice(1).sort((a,b)=>a-b);return {width:innerWidth,edits,successful,startSim,endSim:a.session.state.t,frames:v.length,meanMs:v.reduce((a,b)=>a+b,0)/v.length,p95Ms:v[Math.floor(v.length*.95)],maxMs:v.at(-1),draw:a.world.drawMs(),lightPaints:after.lightPaints-before.lightPaints,lightPaintMeanMs:(after.lightPaintMs-before.lightPaintMs)/Math.max(1,after.lightPaints-before.lightPaints),before,after,summary:a.flow.summary(),overflow:document.documentElement.scrollWidth>innerWidth};
  },setup.lamp);
  assert.ok(r.endSim>r.startSim+3);assert.equal(r.successful,r.edits);assert.ok(r.lightPaints>0);assert.ok(r.edits>=6);assert.equal(r.overflow,false);rows.push(r);console.log('MEASURE',JSON.stringify(r));
 }
 assert.deepEqual(errors,[]);fs.writeFileSync(path.join(__dirname,'interaction-result.json'),JSON.stringify({browser:browser.version(),host:{cpu:os.cpus()[0].model,memoryGiB:os.totalmem()/2**30},setup,rows,errors,method:'Headless Chrome; actual enemy hover at both viewports; normal fresh stock and ordinary chest/walk/place/feed/pickup commands through existing command-recording test hooks, live 1x lamp replacement samples. Not a human verdict or reference-laptop/GPU gate.'},null,2)+'\n');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
