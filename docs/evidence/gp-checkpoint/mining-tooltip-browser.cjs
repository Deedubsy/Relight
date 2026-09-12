const fs=require('node:fs'),assert=require('node:assert/strict'),{chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async()=>{const b=await chromium.launch({channel:'chrome',headless:true}),p=await b.newPage({viewport:{width:1280,height:720}}),errors=[];p.on('pageerror',e=>errors.push(e.message));try{
 await p.route(u=>u.pathname==='/mining.json',r=>r.fulfill({contentType:'application/json',body:fs.readFileSync('docs/evidence/gp-checkpoint/mining-visual-save.json','utf8')}));await p.goto('http://127.0.0.1:5178/?state=/mining.json');await p.waitForFunction(()=>window.__relight?.world);
 const node=JSON.parse(fs.readFileSync('docs/evidence/gp-checkpoint/mining-visual-node.json','utf8'));await p.keyboard.press('p');const xy=await p.evaluate(n=>__relight.world.screenOf(n.x+.5,n.y+.5),node);await p.mouse.move(...xy);
 const tooltip=p.locator('#tooltip');await p.waitForFunction(()=>/\d+ remaining/.test(document.querySelector('#tooltip').textContent));const initial=Number((await tooltip.innerText()).match(/(\d+) remaining/)[1]);
 await p.mouse.down();await p.waitForFunction(()=>__relight.session.state.flow.hand.prog>.2);const commands=await p.evaluate(()=>__relight.session.log.length);
 await p.waitForFunction(n=>Number(document.querySelector('#tooltip').textContent.match(/(\d+) remaining/)?.[1])<=n-2,initial);
 const after=Number((await tooltip.innerText()).match(/(\d+) remaining/)[1]);assert.ok(after<=initial-2);assert.equal(await p.evaluate(()=>__relight.session.log.length),commands,'tooltip refresh dispatches no commands');
 await p.keyboard.press('p');await p.mouse.up();assert.deepEqual(errors,[]);console.log(`Stationary held pointer: tooltip ${initial} → ${after}; no extra commands or browser errors.`);
}finally{await b.close()}})().catch(e=>{console.error(e);process.exitCode=1});
