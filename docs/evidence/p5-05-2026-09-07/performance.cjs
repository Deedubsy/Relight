const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),os=require('node:os'),assert=require('node:assert/strict'),crypto=require('node:crypto');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true}),page=await browser.newPage({viewport:{width:1366,height:900}}),errors=[],rows=[];
 page.on('pageerror',e=>errors.push(String(e)));
 const workload=JSON.parse(fs.readFileSync(path.resolve(__dirname,'../../experiments/campaign/E-logistics-seed3.json'),'utf8'));
 assert.equal(workload.measurements.hours,5);assert.ok(workload.checks.every(c=>c.pass));
 await page.addInitScript(s=>localStorage.setItem('relight.save.p5-05-perf',JSON.stringify(s)),workload.finalState);
 const sample=async(label,preview=false)=>{
  const data=await page.evaluate(async({preview})=>{
   const api=window.__relight,before=api.renderTiming(),samples=[],start=performance.now();let prior=start,lastToggle=0,on=false;
   api.world.drawMs();
   await new Promise(resolve=>{const frame=t=>{samples.push(t-prior);prior=t;if(preview&&t-lastToggle>400){on=!on;api.handLamp(on);lastToggle=t;}if(t-start<5000)requestAnimationFrame(frame);else resolve();};requestAnimationFrame(frame);});
   api.handLamp(false);const after=api.renderTiming(),sorted=samples.slice(1).sort((a,b)=>a-b),n=sorted.length;
   return {viewport:{width:innerWidth,height:innerHeight},frames:n,frameMeanMs:sorted.reduce((a,b)=>a+b,0)/n,frameP95Ms:sorted[Math.floor(n*.95)],frameMaxMs:sorted.at(-1),draw:api.world.drawMs(),lightBefore:before,lightAfter:after,lightChecks:after.lightChecks-before.lightChecks,lightPaints:after.lightPaints-before.lightPaints,lightMeanMs:(after.lightMs-before.lightMs)/(after.lightChecks-before.lightChecks),lightPaintMeanMs:(after.lightPaintMs-before.lightPaintMs)/Math.max(1,after.lightPaints-before.lightPaints),horizontalOverflow:document.documentElement.scrollWidth>innerWidth,machines:JSON.parse(api.stateJson()).flow.machines.length};
  },{preview});
  assert.ok(data.frames>10);assert.ok(data.lightChecks>0);assert.equal(data.horizontalOverflow,false);if(preview)assert.ok(data.lightPaints>0);
  rows.push({label,preview,...data});console.log(label,JSON.stringify({width:data.viewport.width,frames:data.frames,p95:data.frameP95Ms,light:data.lightMeanMs,paints:data.lightPaints}));
 };
 try{
  for(const mode of ['fresh','five-hour-assisted']){
   await page.goto('http://127.0.0.1:5176/?rules=exploration-v2&seed=3&view=world'+(mode==='fresh'?'':'&state=local:p5-05-perf'));
   await page.waitForFunction(()=>window.__relight?.renderTiming);await page.waitForTimeout(1000);
   const speed=await page.evaluate(()=>JSON.parse(window.__relight.stateJson()).speed);if(speed===0)await page.keyboard.press('p');
   for(const width of [1366,900,1920]){await page.setViewportSize({width,height:900});await page.waitForTimeout(400);await sample(mode);await page.keyboard.press('m');await page.waitForTimeout(200);await page.keyboard.press('m');await page.waitForTimeout(300);}
   await sample(mode+'-changed-light-mask',true);
  }
  await page.screenshot({path:path.join(__dirname,'performance-1920.png')});assert.deepEqual(errors,[]);
  const gl=await page.evaluate(()=>{const c=document.createElement('canvas'),g=c.getContext('webgl');if(!g)return null;const e=g.getExtension('WEBGL_debug_renderer_info');return e?{vendor:g.getParameter(e.UNMASKED_VENDOR_WEBGL),renderer:g.getParameter(e.UNMASKED_RENDERER_WEBGL)}:null;});
  fs.writeFileSync(path.join(__dirname,'performance-result.json'),JSON.stringify({date:new Date().toISOString(),fixtureStateSha256:crypto.createHash('sha256').update(JSON.stringify(workload.finalState)).digest('hex'),host:{platform:os.platform(),release:os.release(),cpu:os.cpus()[0].model,logicalCpus:os.cpus().length,memoryGiB:os.totalmem()/2**30,browser:browser.version(),gl},setup:'Installed Chrome, headless, five-second RAF samples after warmup; live 1x fresh seed 3 and loaded declared five-hour assisted logistics save. Real viewport resize and M map/world controls. Changed-mask samples toggle the existing rendering-only hand-lamp preview every 400 ms, not a gameplay light unlock. CPU timings include light-mask checks and changed texture paints; RAF is browser scheduling, not measured monitor presentation. No simulation workload runs concurrently.',rows,errors,gap:'No owner-designated 2019 reference laptop or controlled visible GPU/display measurement. These local headless measurements do not pass the reference-machine 60 FPS gate or human visual readability.'},null,2)+'\n');
  console.log('PASS: resize, map/world return, live renderer and changed light-mask checks; reference-machine performance remains a named gap.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
