const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true,args:['--disable-gpu']});
 const page=await browser.newPage({viewport:{width:1366,height:900}}),errors=[],out=__dirname;
 page.on('pageerror',e=>errors.push(String(e)));
 const state=()=>page.evaluate(()=>JSON.parse(window.__relight.stateJson()));
 const hash=()=>page.evaluate(()=>window.__relight.stateHash());
 const aim=async p=>{await page.locator('canvas').scrollIntoViewIfNeeded();const a=await page.evaluate(p=>window.__relight.world.screenOf(p.x+.5,p.y+.5),p),box=await page.locator('canvas').boundingBox();await page.mouse.move(box.x+a[0],box.y+a[1]);};
 const pressAt=async(p,key)=>{await aim(p);await page.keyboard.press(key);await page.waitForTimeout(200);};
 const panel=page.locator('section[aria-label="Factory inspection"]');
 try{
  await page.goto('http://127.0.0.1:5176/?rules=exploration-v2&seed=3&view=world');await page.waitForFunction(()=>window.__relight?.session.state.flow);await page.keyboard.press('p');await page.waitForTimeout(150);
  const gen=(await state()).flow.machines.find(m=>m.kind==='generator');
  const old=await hash();await pressAt(gen,'f');await panel.waitFor({state:'visible'});assert.equal(await hash(),old,'F inspection does not mutate paused state');
  assert.match(await panel.innerText(),/Local circuit: 300 kW supply/);assert.match(await panel.innerText(),/Machine draw: 0 kW/);await page.keyboard.press('Escape');
  await page.keyboard.press('i');const pockets=page.locator('section').filter({has:page.getByRole('heading',{name:/Pockets and the chest/})});
  for(const n of [0,1])await pockets.getByRole('button',{name:'Take 50',exact:true}).nth(n).click();await page.keyboard.press('Escape');
  const spot=await page.evaluate(()=>{const r=window.__relight,e=r.engineer();for(let y=Math.floor(e.y)-6;y<e.y+6;y++)for(let x=Math.floor(e.x)-6;x<e.x+6;x++)if(r.flow.canPlace('assembler',x,y).ok&&Math.hypot(x+1.5-e.x,y+1.5-e.y)<7)return{x,y};});assert.ok(spot);
  await page.keyboard.press('4');await aim({x:spot.x+1,y:spot.y+1});await page.mouse.down();await page.mouse.up();await page.waitForTimeout(200);await page.keyboard.press('Escape');
  const asm=(await state()).flow.machines.find(m=>m.kind==='assembler'&&m.x===spot.x&&m.y===spot.y);assert.ok(asm,'assembler placed with normal controls');
  await pressAt(spot,'e');await panel.waitFor({state:'visible'});assert.match(await panel.innerText(),/starved.*needs steel, copper/);assert.match(await panel.innerText(),/magazine: nominal 0 in \/ 10 out per min/);
  await panel.getByRole('combobox',{name:'Assembler recipe'}).selectOption('wire');await page.waitForTimeout(180);assert.equal((await state()).flow.machines.find(m=>m.id===asm.id).recipe,'wire');assert.match(await panel.innerText(),/wire: nominal 0 in \/ 120 out per min/);
  await aim(spot);await page.waitForTimeout(300);assert.match(await page.locator('#tooltip').innerText(),/Wire|wire/);await page.waitForTimeout(5100);await page.screenshot({path:path.join(out,'inspection-1366.png')});
  await page.keyboard.press('Escape');await page.keyboard.press('p');await page.waitForTimeout(2200);await page.keyboard.press('p');
  await page.getByText('Item statistics',{exact:true}).click();await page.waitForTimeout(180);const stats=page.locator('.item-statistics');assert.match(await stats.innerText(),/Measured over the last/);assert.equal(await stats.locator('tbody tr').count(),8);assert.match(await stats.innerText(),/negative use rates mean returned recipe inputs/);
  await page.setViewportSize({width:900,height:900});await stats.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(out,'item-statistics-900.png')});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  await pressAt(spot,'f');await panel.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(out,'inspection-900.png')});
  await page.keyboard.press('Escape');await page.keyboard.press('Control+s');const saved=await hash(),url=await page.evaluate(()=>window.__relight.loadUrl());
  await page.goto(url);await page.waitForFunction(()=>window.__relight?.session.state.flow);assert.equal(await hash(),saved);
  await pressAt(spot,'f');await panel.waitFor({state:'visible'});assert.equal(await panel.getByRole('combobox',{name:'Assembler recipe'}).inputValue(),'wire');assert.match(await panel.innerText(),/Measured over the last/);
  const replay=await page.evaluate(()=>window.__relight.replayHash());assert.ok(replay.same);
  fs.writeFileSync(path.join(out,'browser-result.json'),JSON.stringify({setup:'Fresh seed 3; normal stock transfers and paid assembler; keyboard F/E/Escape, recipe DOM selector, pause/resume, item statistics, save/reload. Read-only hooks locate and inspect; no injected stock or position.',widths:[1366,900],assembler:spot,replay,errors,log:await page.evaluate(()=>window.__relight.commandLog())},null,2));
  assert.deepEqual(errors,[]);console.log('PASS: read-only F inspection, local circuit, E assembler panel, normal paid recipe command, nominal and measured labels, public item statistics, responsive layouts, saved observation window and matching replay.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
