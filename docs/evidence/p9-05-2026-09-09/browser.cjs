const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),os=require('node:os');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage({viewport:{width:1366,height:900}}),errors=[],starts=[],rows=[],settings=JSON.parse(fs.readFileSync(path.join(__dirname,'settings.json'),'utf8'));page.on('pageerror',e=>errors.push(String(e)));
const load=async(name)=>{await page.goto('http://127.0.0.1:5181/?rules=exploration-v2&seed=3&view=world&state=/'+name+'.json');await page.waitForFunction(()=>window.__relight?.world);await page.waitForTimeout(400);};
try{
 for(const width of [1366,900])for(const name of ['fresh','construction','network','turbine']){
  await page.setViewportSize({width,height:900});await load(name);const expected=settings.starts.find(s=>s.name===name);
  const before=await page.evaluate(()=>{const a=window.__relight;return {hash:a.stateHash(),tick:a.session.state.flow.tick,t:a.session.state.t,speed:a.session.state.speed,log:a.session.log.length,complete:a.session.logComplete,overflow:document.documentElement.scrollWidth>innerWidth};});
  assert.equal(before.hash,expected.browserNormalizedHash);assert.equal(before.tick,expected.tick);assert.equal(before.speed,0);assert.equal(before.log,expected.logEntries);assert.ok(before.complete);assert.ok(!before.overflow);await page.waitForTimeout(700);assert.equal(await page.evaluate(()=>window.__relight.stateHash()),before.hash);
  await page.screenshot({path:path.join(__dirname,name+'-'+width+'.png')});
  await page.keyboard.press('Control+s');page.once('dialog',d=>d.accept());await page.keyboard.press('Control+o');await page.waitForURL(/state=local/);await page.waitForFunction(()=>window.__relight?.world);assert.equal(await page.evaluate(()=>window.__relight.stateHash()),before.hash);
  starts.push({width,name,...before,saveReload:true});console.log('PASS paused start/save-load',name,width,before.hash);
 }
 for(const name of ['fresh','network'])for(const width of [1366,900]){
  await page.setViewportSize({width,height:900});await load(name);
  await page.getByRole('button',{name:'Items and recipes',exact:true}).first().click();await page.getByLabel('Item guide',{exact:true}).selectOption('concrete');await page.evaluate(()=>window.scrollTo(0,0));
  await page.getByRole('button',{name:'1×',exact:true}).click();await page.waitForFunction(()=>window.__relight.session.state.speed===1);
  const setup=await page.evaluate(name=>{
   const a=window.__relight,s=a.session.state;if(name==='fresh'){a.chest.take('steel',80);a.chest.take('copper',40);a.chest.take('coal',20);}
   const gen=s.flow.machines.find(m=>m.kind==='generator');a.walkTo(gen.x,gen.y+gen.size);a.run(30);
   const e=a.engineer(),pick=kind=>{for(let y=Math.floor(e.y)-4;y<=Math.floor(e.y)+3;y++)for(let x=Math.floor(e.x)-4;x<=Math.floor(e.x)+3;x++)if(a.flow.canPlace(kind,x,y).ok)return {x,y};throw Error('no '+kind+' placement');};
   const pole=pick('pole');if(!a.flow.place('pole',pole.x,pole.y))throw Error('pole refused');const lamp=pick('lamp');if(!a.flow.place('lamp',lamp.x,lamp.y))throw Error('lamp refused');if(name==='fresh')a.flow.feed(gen.x,gen.y);a.run(1);return {pole,lamp,engineer:a.engineer(),machines:s.flow.machines.length};
  },name);
  await page.getByRole('button',{name:'1×',exact:true}).click();await page.waitForFunction(()=>window.__relight.session.state.speed===1);await page.waitForTimeout(500);
  const r=await page.evaluate(async setup=>{
   const a=window.__relight,before=a.renderTiming(),startSim=a.session.state.t,samples=[],start=performance.now();let prior=start,last=0,on=true,edits=0,successful=0;
   a.world.drawMs();await new Promise(resolve=>{function frame(t){samples.push(t-prior);prior=t;if(t-start-last>500){last=t-start;if(on){a.flow.remove(setup.lamp.x,setup.lamp.y);a.flow.remove(setup.pole.x,setup.pole.y);}else{a.flow.place('pole',setup.pole.x,setup.pole.y);a.flow.place('lamp',setup.lamp.x,setup.lamp.y);}on=!on;edits+=2;for(const [kind,p] of [['pole',setup.pole],['lamp',setup.lamp]])if(a.session.state.flow.machines.some(m=>m.kind===kind&&m.x===p.x&&m.y===p.y)===on)successful++;}if(t-start<5000)requestAnimationFrame(frame);else resolve();}requestAnimationFrame(frame);});
   if(!on){a.flow.place('pole',setup.pole.x,setup.pole.y);a.flow.place('lamp',setup.lamp.x,setup.lamp.y);}const after=a.renderTiming(),v=samples.slice(1).sort((a,b)=>a-b);
   return {width:innerWidth,edits,successful,startSim,endSim:a.session.state.t,frames:v.length,meanMs:v.reduce((a,b)=>a+b,0)/v.length,p95Ms:v[Math.floor(v.length*.95)],maxMs:v.at(-1),draw:a.world.drawMs(),lightPaints:after.lightPaints-before.lightPaints,lightPaintMeanMs:(after.lightPaintMs-before.lightPaintMs)/Math.max(1,after.lightPaints-before.lightPaints),before,after,overflow:document.documentElement.scrollWidth>innerWidth};
  },setup);
  assert.ok(r.endSim>r.startSim+3);assert.equal(r.successful,r.edits);assert.ok(r.lightPaints>0);assert.ok(r.edits>=10);assert.ok(!r.overflow);rows.push({name,setup,...r});console.log('MEASURE',JSON.stringify({name,...r}));
 }
 assert.deepEqual(errors,[]);fs.writeFileSync(path.join(__dirname,'browser-result.json'),JSON.stringify({browser:browser.version(),host:{cpu:os.cpus()[0].model,memoryGiB:os.totalmem()/2**30},starts,rows,errors,method:'Headless Chrome on frozen archive. UI save/load; paid walking/placement/pickup through existing command-recording hooks. Four live 1x lamp/pole edit samples; not human play, a full stress workload or reference-laptop/GPU certification.'},null,2)+'\n');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
