const { chromium } = require(process.env.RELIGHT_PLAYWRIGHT || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs = require('node:fs');
const assert = require('node:assert/strict');
const path = require('node:path');
const out = __dirname;
(async () => {
  const browser = await chromium.launch({channel:'chrome',headless:true, args:['--disable-gpu']});
  const page = await browser.newPage({viewport:{width:1366,height:900}});
  const errors = []; page.on('pageerror', e => errors.push(String(e)));
  try {
    await page.goto('http://127.0.0.1:5176/?rules=exploration-v2&seed=3&view=world');
    await page.waitForFunction(() => window.__relight?.session.state.flow);
    await page.keyboard.press('p'); await page.waitForTimeout(250);
    assert.equal(await page.evaluate(() => window.__relight.session.state.speed),0);
    const state = () => page.evaluate(() => JSON.parse(window.__relight.stateJson()));
    const hash = () => page.evaluate(() => window.__relight.stateHash());
    const aim = async p => {
      const xy = await page.evaluate(p => window.__relight.world.screenOf(p.x+.5,p.y+.5),p);
      const box = await page.locator('canvas').boundingBox();
      await page.mouse.move(box.x+xy[0],box.y+xy[1]);
    };
    const findPath = async () => page.evaluate(() => {
      const r=window.__relight,e=r.engineer();
      for(let y=Math.floor(e.y)-5;y<e.y+5;y++)for(let x=Math.floor(e.x)-5;x<e.x+5;x++){
        const ps=[{x,y},{x:x+1,y},{x:x+2,y},{x:x+2,y:y+1},{x:x+2,y:y+2}];
        if(ps.every(p=>r.flow.canPlace('belt',p.x,p.y).ok&&Math.hypot(p.x+.5-e.x,p.y+.5-e.y)<7))return ps;
      } return null;
    });
    // Ordinary stock, actual Take button, no debug stock or teleportation.
    await page.keyboard.press('i');
    const pockets=page.locator('section').filter({has:page.getByRole('heading',{name:/Pockets and the chest/})});
    await pockets.getByRole('button',{name:'Take 50',exact:true}).nth(0).click();
    assert.equal((await state()).engineer.inv.steel,50);
    await pockets.getByRole('button',{name:'Take 50',exact:true}).nth(1).click();
    assert.equal((await state()).engineer.inv.copper,50);
    await page.keyboard.press('Escape');
    const ps=await findPath();assert.ok(ps);
    const start=await state();
    await page.keyboard.press('1'); await aim(ps[0]); await page.mouse.down();
    await aim(ps[2]); await aim(ps[4]); await page.waitForTimeout(150);
    assert.equal((await state()).flow.machines.length,start.flow.machines.length,'preview must not build');
    await page.screenshot({path:path.join(out,'belt-preview-1366.png')});
    await page.mouse.up(); await page.waitForTimeout(150);
    const built=await state();
    assert.equal(built.flow.machines.length,start.flow.machines.length+5);
    assert.equal(built.engineer.inv.steel,45);
    assert.equal(built.construction.undo.length,1);
    assert.equal(await page.evaluate(()=>window.__relight.commandLog().filter(e=>e.c.type==='buildPath').length),1);
    assert.deepEqual(ps.map(p=>built.flow.machines.find(m=>m.x===p.x&&m.y===p.y).dir),[1,1,2,2,2]);
    assert.equal(built.flow.tick,start.flow.tick);
    await page.keyboard.press('Control+z'); await page.waitForTimeout(100);
    assert.equal((await state()).flow.machines.length,start.flow.machines.length);
    await page.keyboard.press('Control+Shift+z'); await page.waitForTimeout(100);
    assert.equal((await state()).flow.machines.length,built.flow.machines.length);
    const cancelled=await hash();
    const next=await findPath();assert.ok(next);
    await aim(next[0]); await page.mouse.down(); await aim(next[4]);
    await page.keyboard.press('Escape'); await page.mouse.up(); await page.waitForTimeout(100);
    assert.equal(await hash(),cancelled,'Escape preview cancellation must not mutate state');
    // Occupied path and distant construction are actual pointer refusals.
    await page.keyboard.press('1'); await aim(ps[0]); await page.mouse.down(); await aim(ps[4]); await page.mouse.up();
    await page.waitForTimeout(100); assert.equal(await hash(),cancelled);
    assert.match(await page.locator('.toast').last().innerText(),/occup|already|machine/i);
    await aim({x:Math.floor(built.engineer.x)+12,y:Math.floor(built.engineer.y)});
    await page.mouse.click(...await page.evaluate(p=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=window.__relight.world.screenOf(p.x+.5,p.y+.5);return[r.x+s[0],r.y+s[1]];},{x:Math.floor(built.engineer.x)+12,y:Math.floor(built.engineer.y)}));
    await page.waitForTimeout(100);assert.equal(await hash(),cancelled);
    assert.match(await page.locator('.toast').last().innerText(),/closer/i);
    // Menu buttons remain accessible in both supported layout bands.
    await page.keyboard.press('b');
    const menu=page.locator('section').filter({has:page.getByRole('heading',{name:/Build menu/})});
    assert.ok(await menu.getByRole('button',{name:'L · track',exact:true}).isDisabled());
    for(const width of [1366,900]){
      await page.setViewportSize({width,height:900});await page.waitForTimeout(200);
      await menu.getByRole('button',{name:/Undo construction/}).click();
      assert.equal((await state()).flow.machines.length,start.flow.machines.length);
      await menu.getByRole('button',{name:/Redo construction/}).click();
      assert.equal((await state()).flow.machines.length,built.flow.machines.length);
      await page.screenshot({path:path.join(out,`build-menu-${width}.png`)});
      assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
    }
    await menu.getByRole('button',{name:'1 · belt',exact:true}).click();
    assert.equal(await page.evaluate(()=>window.__relight.world.tool),'belt');
    assert.equal(await menu.isVisible(),false);
    assert.ok((await page.locator('canvas').boundingBox()).y>=0,'selecting a tool brings the canvas back into view');
    const savedHash=await hash(); await page.keyboard.press('Control+s');await page.waitForTimeout(100);
    assert.equal(await hash(),savedHash,'Ctrl+S must not also walk south');
    const loadUrl=await page.evaluate(()=>window.__relight.loadUrl());
    await page.goto(loadUrl);await page.waitForFunction(()=>window.__relight?.session.state.flow);
    assert.equal(await hash(),savedHash);
    await page.keyboard.press('Control+z');await page.waitForTimeout(100);
    assert.equal((await state()).flow.machines.length,start.flow.machines.length);
    const replay=await page.evaluate(()=>window.__relight.replayHash());
    const log=await page.evaluate(()=>window.__relight.commandLog());
    fs.writeFileSync(path.join(out,'browser-result.json'),JSON.stringify({viewportWidths:[1366,900],setup:'Fresh seed 3, paused through P; 50 steel and 50 copper taken through normal UI. Only read-only dev hooks used.',path:ps,replay,log,errors},null,2));
    assert.equal(replay.same,true);
    await page.keyboard.press('p');await page.waitForTimeout(100);
    const beforeSave=await page.evaluate(()=>window.__relight.engineer());
    await page.keyboard.down('Control');await page.keyboard.down('s');await page.waitForTimeout(300);
    await page.keyboard.up('s');await page.keyboard.up('Control');
    const afterSave=await page.evaluate(()=>window.__relight.engineer());
    assert.equal(afterSave.x,beforeSave.x);assert.equal(afterSave.y,beforeSave.y);
    await page.goto('http://127.0.0.1:5176/?rules=exploration-v2&seed=3&view=world');
    await page.waitForFunction(()=>window.__relight?.session.state.flow);
    await page.keyboard.press('p');await page.waitForTimeout(100);
    const poorHash=await hash();await page.keyboard.press('1');await aim(ps[0]);await page.mouse.down();await page.mouse.up();
    await page.waitForTimeout(100);assert.equal(await hash(),poorHash);
    assert.match(await page.locator('.toast').last().innerText(),/steel|pockets/i);
    console.log('PASS: paid corner path preview/release; one group; keyboard and menu undo/redo; Esc cancellation; occupied/reach refusal; locks; 1366/900 layouts; Ctrl+S; reload/history; replay hash.');
    assert.deepEqual(errors, []);
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode=1; });
