const fs=require('node:fs'),assert=require('node:assert/strict');
const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true}),p=await browser.newPage({viewport:{width:1280,height:720}}),errors=[];
 p.on('pageerror',e=>errors.push(e.message));
 try {
  await p.route(u=>u.pathname==='/reload-fixture.json',r=>r.fulfill({contentType:'application/json',body:fs.readFileSync('docs/evidence/gp-checkpoint/player-test-save.json','utf8')}));
  await p.goto('http://127.0.0.1:5178/?state=/reload-fixture.json');await p.waitForFunction(()=>window.__relight?.world);
  // Isolated fixture only; all reload actions below use real player UI/keyboard input.
  await p.evaluate(()=>{const e=__relight.session.state.engineer;e.equipment.weapons['rifle:1'].loaded=0;e.inv.magazine=20;delete e.pack;});
  await p.keyboard.press('9');
  const toolbar=p.locator('.placement-toolbar button').filter({hasText:/^Reload/});await toolbar.click();
  assert.equal(await p.evaluate(()=>__relight.session.state.engineer.equipment.weapons['rifle:1'].reload),1.5);
  await p.waitForFunction(()=>document.querySelector('.placement-toolbar button').title.includes('paused; resume'));
  await p.keyboard.press('p');await p.waitForFunction(()=>__relight.session.state.engineer.equipment.weapons['rifle:1'].loaded===10);
  assert.equal(await p.evaluate(()=>__relight.session.state.engineer.inv.magazine),10);
  await p.waitForFunction(()=>document.querySelector('.placement-toolbar button').title.includes('Magazine is full'));
  assert.ok(await toolbar.isDisabled());
  await p.evaluate(()=>{const e=__relight.session.state.engineer;e.equipment.weapons['rifle:1'].loaded=0;e.inv.magazine=3;delete e.pack;});
  await p.getByRole('button',{name:/^Backpack/}).first().click();
  // Close owns focus: the reload shortcut still belongs to the open Backpack.
  await p.keyboard.press('r');
  assert.ok(await p.evaluate(()=>__relight.session.state.engineer.equipment.weapons['rifle:1'].reload>0));
  await p.waitForFunction(()=>__relight.session.state.engineer.equipment.weapons['rifle:1'].loaded===3);
  assert.equal(await p.evaluate(()=>__relight.session.state.engineer.inv.magazine??0),0);
  const reload=p.locator('.equipment-panel button').filter({hasText:/^Reload \(/});
  await p.waitForFunction(()=>document.querySelector('.equipment-panel button[title="Carry bullets in your Backpack"]'));
  assert.ok(await reload.isDisabled());
  await p.screenshot({path:'docs/evidence/gp-checkpoint/reload-controls.png'});
  await p.keyboard.press('Tab');
  await p.evaluate(()=>{const e=__relight.session.state.engineer;e.equipment.weapons['rifle:1'].loaded=0;e.inv.magazine=10;delete e.pack;});
  await p.keyboard.press('r');await p.waitForFunction(()=>__relight.session.state.engineer.equipment.weapons['rifle:1'].loaded===10);
  assert.equal(await p.evaluate(()=>__relight.session.state.engineer.inv.magazine??0),0);
  await p.setViewportSize({width:960,height:720});
  await p.evaluate(()=>localStorage.setItem('relight.ui.v1',JSON.stringify({version:1,scale:150})));
  await p.reload();await p.waitForFunction(()=>window.__relight?.world);await p.keyboard.press('9');
  await p.getByRole('button',{name:/^Backpack/}).first().click();
  for(const button of [toolbar,p.locator('.equipment-panel button').filter({hasText:/^Reload \(/})]){
   const box=await button.boundingBox();assert.ok(box&&box.x>=0&&box.x+box.width<=960&&box.y+box.height<=720);
  }
  await p.screenshot({path:'docs/evidence/gp-checkpoint/reload-controls-scaled.png'});
  assert.deepEqual(errors,[]);console.log('Passed: toolbar reload, paused feedback, full-magazine refusal, Backpack R with Close focused, partial reserve, no-ammo refusal, world R; ammunition conserved in each reload.');
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
