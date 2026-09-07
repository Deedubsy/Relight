const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true,args:['--disable-gpu']});
 const page=await browser.newPage({viewport:{width:1366,height:900}}),errors=[],out=__dirname;
 page.on('pageerror',e=>errors.push(String(e)));
 const fixture=fs.readFileSync(path.join(out,'paid-expedition.json'),'utf8');
 await page.addInitScript(f=>localStorage.setItem('relight.save.p5-04-paid',f),fixture);
 const state=()=>page.evaluate(()=>JSON.parse(window.__relight.stateJson())),hash=()=>page.evaluate(()=>window.__relight.stateHash());
 const aim=async(x,y)=>{await page.locator('canvas').scrollIntoViewIfNeeded();const a=await page.evaluate(([x,y])=>window.__relight.world.screenOf(x,y),[x,y]),box=await page.locator('canvas').boundingBox();await page.mouse.move(box.x+a[0],box.y+a[1]);};
 const cargo=page.locator('.truck-cargo'),route=page.locator('.station-route');
 try{
  await page.goto('http://127.0.0.1:5176/?rules=exploration-v2&seed=3&view=world&state=local:p5-04-paid');await page.waitForFunction(()=>window.__relight?.session.state.campaign?.truck);await page.waitForTimeout(400);
  assert.equal((await state()).speed,0);const t=(await state()).campaign.truck;
  await aim(t.x,t.y);const initial=await hash();await page.keyboard.press('f');await cargo.waitFor({state:'visible'});assert.equal(await hash(),initial,'cargo inspection is read-only');
  await cargo.getByRole('combobox',{name:'Truck cargo item'}).selectOption('steel');await cargo.getByRole('spinbutton',{name:'Truck cargo amount'}).fill('10');await cargo.getByRole('button',{name:'Load cargo',exact:true}).click();assert.equal((await state()).campaign.truck.cargo.steel,10);
  await page.keyboard.press('Escape');await aim(t.x,t.y);await page.keyboard.press('e');await page.waitForTimeout(200);assert.equal((await state()).engineer.truckSeat,true);
  await page.keyboard.press('p');await page.keyboard.down('s');await page.waitForTimeout(600);await page.keyboard.up('s');await page.keyboard.press('p');await page.waitForTimeout(150);
  let s=await state();assert.equal(s.campaign.truck.cargo.steel,10);assert.ok(Math.hypot(s.campaign.truck.x-t.x,s.campaign.truck.y-t.y)>.2,'WASD physically drove the truck');
  await page.keyboard.press('e');await page.waitForTimeout(150);s=await state();assert.equal(s.engineer.truckSeat,undefined);assert.equal(s.campaign.truck.cargo.steel,10);
  await aim(s.campaign.truck.x,s.campaign.truck.y);await page.keyboard.press('f');await cargo.waitFor({state:'visible'});await cargo.getByRole('spinbutton',{name:'Truck cargo amount'}).fill('3');await cargo.getByRole('button',{name:'Unload cargo',exact:true}).click();assert.equal((await state()).campaign.truck.cargo.steel,7);
  await page.waitForTimeout(5100);await page.screenshot({path:path.join(out,'truck-1366.png')});await page.keyboard.press('Escape');
  await page.keyboard.press('m');await page.waitForTimeout(300);
  const stops=(await state()).flow.machines.filter(m=>m.kind==='tramstop');
  const clickStop=async stop=>{
   const geo=await page.evaluate(()=>window.__relight.ground()),box=await page.locator('canvas').boundingBox();
   const ppt=Math.min(Math.max(64,box.width-8)/geo.tw,Math.max(64,box.height-8)/geo.th),ox=Math.round((box.width-Math.floor(geo.tw*ppt))/2),oy=Math.round((box.height-Math.floor(geo.th*ppt))/2);
   await page.mouse.click(box.x+ox+(stop.x+stop.size/2)*ppt,box.y+oy+(stop.y+stop.size/2)*ppt);await page.waitForTimeout(250);
  };
  const mapHash=await hash();await clickStop(stops[1]);await route.waitFor({state:'visible'});assert.match(await route.innerText(),/2 stops.*1 tram.*Service connected/);assert.equal(await hash(),mapHash,'route selection sends no gameplay command');
  await page.waitForTimeout(1000);await page.screenshot({path:path.join(out,'route-1366.png')});await route.getByRole('button',{name:'Clear route',exact:true}).click();await route.waitFor({state:'hidden'});assert.equal(await hash(),mapHash);
  await clickStop(stops[0]);await route.waitFor({state:'visible'});assert.match(await route.innerText(),new RegExp(`Selected stop ${stops[0].id}`,'i'));
  await page.setViewportSize({width:900,height:900});await route.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(out,'route-900.png')});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  await page.keyboard.press('Escape');await page.keyboard.press('Control+s');const saved=await hash(),url=await page.evaluate(()=>window.__relight.loadUrl());
  await page.goto(url);await page.waitForFunction(()=>window.__relight?.session.state.campaign?.truck);assert.equal(await hash(),saved);assert.equal((await state()).campaign.truck.cargo.steel,7);
  const replay=await page.evaluate(()=>window.__relight.replayHash());assert.ok(replay.same,JSON.stringify(replay));assert.deepEqual(errors,[]);
  fs.writeFileSync(path.join(out,'browser-result.json'),JSON.stringify({setup:'Paid expedition checkpoint generated from fresh seed 3 via ordinary sim commands, complete log: station restoration, kit track, two stops and tram, walk beside truck. Browser loads that save; F/E, cargo DOM controls, WASD, M/map station clicks, Escape, Ctrl+S and reload are real controls. No injected stock, position or gameplay mutations in browser.',widths:[1366,900],replay,errors,log:await page.evaluate(()=>window.__relight.commandLog())},null,2));
  console.log('PASS: truck F inspection, local E boarding/exit, separate paid cargo, WASD footprint driving, two-stop map selection without mutation, deselection, responsive layout, saved cargo and full fresh-state replay.');
 }catch(e){await page.screenshot({path:path.join(out,'browser-failure.png')});throw e;}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
