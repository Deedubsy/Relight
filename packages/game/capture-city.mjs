// RI-02A browser evidence. Uses the real Vite entry point and existing __relight QA hooks.
// Run with RELIGHT_PLAYWRIGHT pointing to a Playwright package if it is not installed locally.
import { createRequire } from 'node:module';
import { mkdirSync, writeFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
const require = createRequire(import.meta.url);
const { chromium } = require(process.env.RELIGHT_PLAYWRIGHT || 'playwright');
const out = new URL('../../docs/evidence/city-rebuild/', import.meta.url);
mkdirSync(out, { recursive: true });
const browser = await chromium.launch({ channel: 'chrome', headless: true, args: ['--disable-gpu', '--disable-background-timer-throttling', '--disable-renderer-backgrounding'] });
const page = await browser.newPage({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 1 });
page.setDefaultTimeout(60000);
const errors = [];
const source = { source_commit: execFileSync('git', ['rev-parse', 'HEAD'], { encoding: 'utf8' }).trim(), working_tree: true,
  config_hash: 'd51dfee0', config_ref: { kind: 'calibration', map: 'river', walk: true, economy: true, overrides: { power: true, supply: 'generators', draw: 'half' } } };
page.on('pageerror', e => errors.push(e.message));
const baseline = process.argv.includes('--baseline');
const prefix = baseline ? 'baseline' : 'revised';
console.log('loading Vite');
await page.goto('http://127.0.0.1:5173/?seed=3&view=world', { waitUntil: 'load' });
await page.waitForFunction(() => !!window.__relight);
console.log('game ready');
await page.evaluate(() => { window.__relight.session.state.speed = 0; });
await page.waitForTimeout(500);
await page.screenshot({ path: new URL(`${prefix}-opening.png`, out).pathname.replace(/^\/(.:)/, '$1') });
console.log('opening captured');
const info = await page.evaluate(() => ({ seed: 3, viewport: [innerWidth, innerHeight], dpr: devicePixelRatio,
  config: window.__relight.configHash, zoom: window.__relight.world.zoom, engineer: window.__relight.engineer(), state: JSON.parse(window.__relight.stateJson()) }));
writeFileSync(new URL(`${prefix}-capture.json`, out), JSON.stringify(info, null, 2));
console.log('metadata saved');
await page.evaluate(() => window.__relight.world.setZoom(1));
console.log('zoom set');
await page.waitForTimeout(200);
await page.screenshot({ path: new URL(`${prefix}-construction.png`, out).pathname.replace(/^\/(.:)/, '$1') });
console.log('construction captured');
await page.evaluate(() => window.__relight.toggleView());
await page.waitForTimeout(300);
await page.screenshot({ path: new URL(`${prefix}-overview.png`, out).pathname.replace(/^\/(.:)/, '$1') });
writeFileSync(new URL(`${prefix}-browser-errors.json`, out), JSON.stringify(errors));
if (!baseline) {
  const shots = [], perf = [];
  const capture = async (name, url, opts = {}) => {
    await page.goto(`http://127.0.0.1:5173/${url}`, { waitUntil: 'load' });
    await page.waitForFunction(() => !!window.__relight);
    const data = await page.evaluate(({ role, restored, zoom, qa }) => {
      const r = window.__relight, st = r.session.state;
      st.speed = 0;
      const city = r.city(), place = role && city.places.find(p => p.name === role || p.role === role);
      if (place) {
        // Labelled screenshot-camera QA: not movement evidence. Actual movement is in citycheck.ts.
        st.engineer.x = place.pad.x + place.pad.w / 2; st.engineer.y = place.pad.y + place.pad.h / 2;
        st.engineer.block = place.block;
        if (place.role === 'hq') {
          // The workbench is outside the real Depot's solid footprint.
          st.engineer.x = place.pad.x + 12.5; st.engineer.y = place.pad.y + 15.5;
        }
      }
      if (zoom) r.world.setZoom(zoom);
      if (qa) {
        const label = document.createElement('div'); label.textContent = qa;
        label.style.cssText = 'position:fixed;bottom:80px;left:24px;background:#13252d;padding:8px 12px;color:#e9cf99;font:14px Segoe UI;z-index:50;pointer-events:none';
        document.body.append(label);
      }
      return { name: role, profile: city.profile, seed: st.seed, tick: st.flow.tick, zoom: r.world.zoom, engineer: r.engineer(), viewport: [innerWidth, innerHeight], dpr: devicePixelRatio, restored };
    }, opts);
    await page.waitForTimeout(700);
    await page.screenshot({ path: new URL(`${name}.png`, out).pathname.replace(/^\/(.:)/, '$1') });
    shots.push({ file: `${name}.png`, ...data });
    writeFileSync(new URL(`${name}.json`, out), JSON.stringify({ ...source, ...data }, null, 2));
    console.log('captured', name);
  };
  await capture('revised-matched-opening', '?seed=3&view=world', { zoom: 0.5 });
  await capture('factory', '?view=world&state=city-factory-qa', { role: 'hq', zoom: 0.65, qa: 'QA snapshot · real command-driven factory at 20:00 · camera inspection' });
  for (const role of ['Founders Court', 'Ironworks Yard', 'Exchange Square', 'Switchyard']) {
    await capture(role.toLowerCase().replaceAll(' ', '-'), '?seed=3&view=world', { role, zoom: 0.65, qa: 'QA camera inspection · fresh city geometry' });
  }
  await capture('street-dark', '?view=world&state=city-opening-qa', { role: 'Founders Court', zoom: 0.65, qa: 'Matched street · before restoration · camera QA' });
  await capture('street-restored', '?view=world&state=city-factory-qa', { role: 'Founders Court', zoom: 0.65, qa: 'Matched street · 20:00 command-driven restoration · camera QA' });
  for (const seed of [4, 5, 6]) await capture(`seed-${seed}-neighbourhood`, `?seed=${seed}`, { zoom: 0.5 });
  for (const [width, height] of [[2560, 1440], [1280, 720]]) {
    await page.setViewportSize({ width, height });
    await capture(`opening-${width}`, '?seed=3');
    const clipping = await page.evaluate(() => ({ horizontalOverflow: document.documentElement.scrollWidth > innerWidth, panelScroll: document.getElementById('panel').scrollHeight > document.getElementById('panel').clientHeight }));
    shots.at(-1).clipping = clipping;
  }
  await page.setViewportSize({ width: 1920, height: 1080 });
  await capture('performance-neighbourhood', '?view=world&state=city-factory-qa', { zoom: 0.5 });
  const wallStart = await page.evaluate(() => { window.__relight.session.state.speed = 1; window.__relight.fps(); return performance.now(); });
  await page.waitForTimeout(3000);
  perf.push(await page.evaluate(start => ({ view: 'world', wallMs: performance.now() - start, fps: window.__relight.fps(), draw: window.__relight.world.drawMs() }), wallStart));
  const mapStart = Date.now();
  await page.evaluate(() => window.__relight.toggleView());
  await page.waitForTimeout(500);
  perf.push({ mapOpenWallMsIncluding500msWait: Date.now() - mapStart });
  await page.waitForTimeout(3000);
  perf.push(await page.evaluate(() => ({ view: 'map', fps: window.__relight.fps() })));
  writeFileSync(new URL('screenshot-manifest.json', out), JSON.stringify({ ...source, browser: 'headless Chrome --disable-gpu', shots, perf, errors }, null, 2));
}
await browser.close();
console.log(`${prefix} screenshots captured; ${errors.length} browser errors`);
process.exit(0);
