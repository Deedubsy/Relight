/** DOM side panel: HUD, ring order (drag to reorder), stock + assembler, facilities, session summary, export. */
import { SimState, FrontEdgeView, ClaimInfo, HeldInfo, frontList, hud, facilityList, survivorList, shapeMetrics, clockOf, slotInfo, SKYLINE_RANGE } from '@relight/sim';
import { Session, setSpeed, queue, shareUrl } from './session';
import { summarise, exportJson } from './telemetry';

export interface PanelHooks { onSelectEdge(id: number | null): void; onToggleView(): void }

export interface Panel {
  update(nowMs: number): void;
  setSelectedEdge(e: FrontEdgeView | null): void;
  tooltip(info: ClaimInfo | HeldInfo | null, px: number, py: number): void;
  /** Plain lines (\n-separated) at a screen point; null hides. The world view's tile tooltip. */
  tooltipText(text: string | null, px: number, py: number): void;
  setView(mode: 'map' | 'world'): void;
  toast(msg: string, kind?: 'info' | 'bad' | 'good'): void;
}

function el<K extends keyof HTMLElementTagNameMap>(tag: K, cls?: string, text?: string): HTMLElementTagNameMap[K] {
  const e = document.createElement(tag);
  if (cls) e.className = cls;
  if (text !== undefined) e.textContent = text;
  return e;
}
const pct = (v: number) => `${Math.round(v * 100)} %`;
/** Pips are shape-coded as well as coloured (constitution, Phase 2): ● green, ▲ amber, ✕ red. Same shapes on the map. */
const PIP_GLYPH: Record<string, string> = { green: '●', amber: '▲', red: '✕' };
const f1 = (v: number) => (Math.round(v * 10) / 10).toFixed(1);
const f2 = (v: number) => (Math.round(v * 100) / 100).toFixed(2);

export function createPanel(session: Session, root: HTMLElement, hooks: PanelHooks): Panel {
  const st = session.state;
  const eco = st.config.economy;
  root.innerHTML = '';

  // header
  const header = el('section');
  const head = el('header');
  const h1 = el('h1', undefined, `Relight · map view · seed ${st.seed}`);
  head.append(h1);
  if (session.scenario === 'B') head.append(el('span', 'badge', `Scenario B · from ${clockOf(session.startT)}`));
  const btnLink = el('button', undefined, 'Copy link');
  btnLink.title = 'Share this seed and settings';
  btnLink.onclick = () => {
    const url = shareUrl(session.params);
    navigator.clipboard?.writeText(url).then(() => toast('Link copied', 'good'), () => window.prompt('Copy this link', url));
  };
  const btnView = el('button', undefined, 'World view (E)');
  btnView.title = 'Toggle map ↔ world view at the same block (E)';
  btnView.onclick = () => hooks.onToggleView();
  head.append(btnView, btnLink);
  header.append(head);
  header.append(el('p', 'hint', 'Click a Dark block next to your territory to claim it. Every Held block facing Dark is an ammo edge; each edge pulls magazines from the ring in the order below. Substations that go 40 crawlers unfed fall. Only interior blocks (white outline) have a free machine slot for an assembler; the HQ holds the Mk1. A block\'s rubble is finite: the strip at its top fades as it is dug out.'));
  root.append(header);

  // HUD
  const hudSec = el('section');
  hudSec.append(el('h2', undefined, 'Territory'));
  const stats = el('div', 'stats');
  const mk = (label: string) => { const s = el('div', 'stat'); const b = el('b', 'mono', '0'); s.append(b, el('span', undefined, label)); stats.append(s); return { s, b }; };
  const sHeld = mk('Held'), sFront = mk('Front edges'), sInt = mk('Interior');
  const sProd = mk('mag/min made'), sDem = mk('mag/min demanded'), sStock = mk('magazines in stock');
  const sLost = mk('blocks lost'), sEmpty = mk('empty hoppers'), sClock = mk('sim clock');
  hudSec.append(stats);
  const speedRow = el('div', 'row');
  const speeds: [number, string][] = [[0, 'Pause'], [1, '1×'], [4, '4×'], [16, '16×']];
  const speedBtns = speeds.map(([m, label]) => { const b = el('button', undefined, label); b.onclick = () => setSpeed(session, m); speedRow.append(b); return { m, b }; });
  speedRow.append(el('span', 'hint', 'keys: space, 1, 2, 3 · E world view'));
  hudSec.append(speedRow);
  root.append(hudSec);

  // stock + assembler
  const stockSec = el('section');
  stockSec.append(el('h2', undefined, eco ? 'Rubble & production' : 'Production'));
  const stockList = el('ul', 'plain');
  const li = (label: string) => { const l = el('li'); const a = el('span', undefined, label); const v = el('span', 'mono', '0'); l.append(a, v); stockList.append(l); return v; };
  const vCu = eco ? li('Copper (wire)') : null, vSteel = eco ? li('Steel (frames)') : null, vStone = eco ? li('Stone (no use yet)') : null, vPatch = eco ? li('HQ steel patch left') : null;
  const vAsm = li('Assemblers');
  const vSlots = li('Machine slots used / free'), vRisk = li('Machines at risk (block back on the front)'), vDry = eco ? li('Blocks dug out (rubble gone)') : null;
  stockSec.append(stockList);
  const asmRow = el('div', 'row');
  const btnAsm = el('button', undefined, eco ? `Build assembler (${st.config.eco.assemblerCost.copper} Cu, ${st.config.eco.assemblerCost.steel} steel)` : 'Build assembler');
  btnAsm.title = st.config.startAsmRate !== null
    ? `The starting assembler makes ${st.config.startAsmRate} magazines per minute; each one you build makes ${st.config.asmRate}`
    : `Each assembler adds ${st.config.asmRate} magazines per minute`;
  btnAsm.onclick = () => queue(session, { type: 'addAssembler' });
  asmRow.append(btnAsm);
  stockSec.append(asmRow);
  if (eco) stockSec.append(el('p', 'hint', `A claim costs ${st.config.eco.claimCost.copper} Cu (10 wire) and ${st.config.eco.claimCost.steel} steel (5 frames). A magazine costs ${st.config.eco.magazineCost.steel} steel + ${st.config.eco.magazineCost.copper} Cu (§12); assemblers stop when the stock runs out. Held blocks yield their district's rubble: civic stone, residential copper, industrial steel.`));
  root.append(stockSec);

  // ring order
  const ringSec = el('section');
  ringSec.append(el('h2', undefined, 'Ammo ring order — drag to reorder'));
  const ring = el('ol', 'ring');
  ringSec.append(ring);
  const ringHint = el('p', 'hint', 'Edges fill from the top. The last edges starve first when production falls short. Pips: ● hopper at least half full · ▲ running low · ✕ empty, blinking.');
  ringSec.append(ringHint);
  root.append(ringSec);

  // facilities (§8: silhouettes within SKYLINE_RANGE blocks of a Held block) and survivors (§8: a block's contents
  // show once a neighbour is Held)
  const facSec = el('section');
  facSec.append(el('h2', undefined, 'Skyline'));
  const facList = el('ul', 'plain');
  facSec.append(facList);
  facSec.append(el('p', 'hint', `A facility shows once it is within ${SKYLINE_RANGE} blocks of a Held block; survivors show once a block next to them is Held, and join when their block is.`));
  const survList = el('ul', 'plain');
  facSec.append(el('h2', undefined, 'Survivors'), survList);
  root.append(facSec);

  // summary
  const sumSec = el('section', 'summary');
  sumSec.append(el('h2', undefined, 'Session summary'));
  const dl = el('dl');
  const dd = (label: string) => { dl.append(el('dt', undefined, label)); const d = el('dd', undefined, '–'); dl.append(d); return d; };
  const dTime = dd(session.scenario === 'B' ? 'Sim time (session from ' + clockOf(session.startT) + ')' : 'Sim time'), dClaims = dd('Claims this session (per hour)'), dShape = dd('Bounding box'), dRatio = dd('Perimeter / area'),
        dAmmo = dd('Ammo spent'), dLost = dd('Blocks lost'), dReorder = dd('Ring reorders'), dEncl = dd('First enclosure');
  sumSec.append(dl);
  const sumRow = el('div', 'row');
  const btnExport = el('button', undefined, 'Export telemetry JSON');
  btnExport.onclick = () => {
    const json = exportJson(session.telemetry, session.state);
    const blob = new Blob([json], { type: 'application/json' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `relight-seed${st.seed}-${clockOf(st.t).replace(/:/g, '')}.json`;
    document.body.append(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 2000);
    toast('Telemetry exported', 'good');
  };
  sumRow.append(btnExport);
  const btnState = el('button', undefined, 'Save snapshot');
  btnState.title = 'Download the raw sim state; load it later with ?state=<file url>';
  btnState.onclick = () => {
    const blob = new Blob([JSON.stringify(session.state)], { type: 'application/json' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `relight-state-seed${st.seed}-${clockOf(session.state.t).replace(/:/g, '')}.json`;
    document.body.append(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 2000);
    toast('Snapshot saved', 'good');
  };
  sumRow.append(btnState);
  sumSec.append(sumRow);
  root.append(sumSec);

  // tooltip + toasts
  const tip = document.getElementById('tooltip')!;
  const toasts = document.getElementById('toasts')!;
  function toast(msg: string, kind: 'info' | 'bad' | 'good' = 'info'): void {
    const t = el('div', `toast ${kind === 'info' ? '' : kind}`, msg);
    toasts.append(t);
    while (toasts.children.length > 6) toasts.firstElementChild?.remove();
    setTimeout(() => t.remove(), 5000);
  }

  // ring list rendering with drag/drop
  let dragging: number | null = null;
  let ringKey = '';
  let selected: number | null = null;
  function renderRing(edges: FrontEdgeView[]): void {
    const key = edges.map(e => `${e.id}:${e.pip}:${Math.round(e.level * 20)}`).join(',') + `|${selected}`;
    if (key === ringKey || dragging !== null) return;
    ringKey = key;
    ring.innerHTML = '';
    for (const e of edges) {
      const l = el('li');
      l.draggable = true;
      l.dataset.id = String(e.id);
      if (e.id === selected) l.classList.add('selected');
      l.append(el('span', 'pos', String(e.ringPos + 1)), el('span', `pip ${e.pip}`, PIP_GLYPH[e.pip]), el('span', 'mono', `(${e.from.x},${e.from.y}) → (${e.to.x},${e.to.y})  ${e.darkDistrict}`), el('span', 'lvl', pct(e.level)));
      l.title = `Dark side rot ${pct(e.darkRot)}${e.darkWell ? ' · well' : ''}`;
      l.onclick = () => { hooks.onSelectEdge(selected === e.id ? null : e.id); };
      l.ondragstart = ev => { dragging = e.id; ev.dataTransfer?.setData('text/plain', String(e.id)); };
      l.ondragend = () => { dragging = null; clearMarks(); ringKey = ''; };
      l.ondragover = ev => {
        ev.preventDefault();
        const r = l.getBoundingClientRect();
        clearMarks();
        l.classList.add(ev.clientY < r.top + r.height / 2 ? 'drop-before' : 'drop-after');
      };
      l.ondrop = ev => {
        ev.preventDefault();
        if (dragging === null) return;
        const r = l.getBoundingClientRect();
        const before = ev.clientY < r.top + r.height / 2;
        const ids = frontList(session.state).map(x => x.id).filter(id => id !== dragging);
        let at = ids.indexOf(e.id);
        if (at < 0) at = ids.length; else if (!before) at += 1;
        ids.splice(at, 0, dragging);
        queue(session, { type: 'ringOrder', ids });
        dragging = null; clearMarks(); ringKey = '';
      };
      ring.append(l);
    }
    if (!edges.length) ring.append(el('li', undefined, 'no front'));
  }
  function clearMarks(): void { for (const c of Array.from(ring.children)) c.classList.remove('drop-before', 'drop-after'); }

  let lastUpdate = -1e9;
  let lastFacKey = '';
  function update(nowMs: number): void {
    if (nowMs - lastUpdate < 150) return;
    lastUpdate = nowMs;
    const s: SimState = session.state;
    const h = hud(s);
    sHeld.b.textContent = String(h.held); sFront.b.textContent = String(h.front); sInt.b.textContent = String(h.interior);
    sProd.b.textContent = f1(h.ammo.productionMagPerMin); sDem.b.textContent = f1(h.ammo.demandMagPerMin1);
    sDem.s.classList.toggle('warn', h.ammo.demandMagPerMin1 > h.ammo.productionMagPerMin + 1e-9);
    sStock.b.textContent = f1(h.ammo.stockMags); sLost.b.textContent = String(h.lost);
    sEmpty.b.textContent = String(h.ammo.emptyHoppers); sEmpty.s.classList.toggle('warn', h.ammo.emptyHoppers > 0);
    sClock.b.textContent = h.clock;
    for (const { m, b } of speedBtns) b.classList.toggle('active', s.speed === m);
    if (vCu) { vCu.textContent = String(Math.floor(s.stock.copper)); vSteel!.textContent = String(Math.floor(s.stock.steel)); vStone!.textContent = String(Math.floor(s.stock.stone)); vPatch!.textContent = String(Math.floor(s.patch.steel)); }
    vAsm.textContent = String(h.ammo.assemblers);
    const si = slotInfo(s);
    vSlots.textContent = `${si.used} / ${si.free}`; vRisk.textContent = String(si.atRisk); if (vDry) vDry.textContent = String(si.dry);
    const broke = eco && (s.stock.copper < s.config.eco.assemblerCost.copper || s.stock.steel < s.config.eco.assemblerCost.steel);
    btnAsm.disabled = broke || si.free === 0;
    btnAsm.title = si.free === 0 ? 'No free machine slot: an assembler needs an Interior block (every neighbour Held or inert). Enclose a block to get one.'
      : broke ? 'Not enough rubble' : `Goes on block (${si.next!.x},${si.next!.y}); makes ${s.config.asmRate} magazines per minute`;
    renderRing(frontList(s));
    const facs = facilityList(s).filter(f => f.visible);
    const survs = survivorList(s).filter(f => f.revealed);
    const fk = facs.map(f => `${f.name}${f.held ? 1 : 0}`).join() + '|' + survs.map(f => `${f.tag}${f.held ? 1 : 0}`).join();
    if (fk !== lastFacKey) {
      lastFacKey = fk; facList.innerHTML = ''; survList.innerHTML = '';
      for (const f of facs) { const l = el('li'); l.append(el('span', undefined, `${f.name} (${f.x},${f.y})`), el('span', f.held ? '' : 'muted', f.held ? 'reached' : `${f.dist} blocks from HQ`)); facList.append(l); }
      if (!facs.length) facList.append(el('li', 'muted', 'nothing on the skyline yet'));
      for (const f of survs) { const l = el('li'); l.append(el('span', undefined, `${f.tag} · ${f.name} (${f.x},${f.y})`), el('span', f.held ? '' : 'muted', f.held ? 'with us' : 'seen')); survList.append(l); }
      if (!survs.length) survList.append(el('li', 'muted', 'none found yet'));
    }
    const sum = summarise(session.telemetry, s);
    dTime.textContent = sum.simTime;
    dClaims.textContent = `${sum.claims} (${f1(sum.claimsPerHour)}/h)`;
    dShape.textContent = `${sum.shape.bbox.w}×${sum.shape.bbox.h}, aspect ${f2(sum.shape.bbox.aspect)}`;
    dRatio.textContent = f2(sum.shape.perimeterOverArea);
    dAmmo.textContent = `${Math.round(sum.ammoSpentMags)} mags, ${Math.round(sum.shellsSpent)} shells`;
    dLost.textContent = String(sum.lost);
    dReorder.textContent = String(sum.reorders);
    dEncl.textContent = sum.firstEnclosure;
  }

  function tooltipText(text: string | null, px: number, py: number): void {
    if (!text) { tip.hidden = true; return; }
    tip.innerHTML = '';
    text.split('\n').forEach((line, i) => tip.append(el('div', i ? 'muted' : undefined, line)));
    tip.hidden = false;
    const w = tip.offsetWidth, h = tip.offsetHeight;
    tip.style.left = `${Math.min(px + 14, window.innerWidth - w - 8)}px`; tip.style.top = `${Math.min(py + 14, window.innerHeight - h - 8)}px`;
  }
  function setView(mode: 'map' | 'world'): void {
    h1.textContent = `Relight · ${mode} view · seed ${st.seed}`;
    btnView.textContent = mode === 'map' ? 'World view (E)' : 'Map view (E)';
  }

  function tooltip(info: ClaimInfo | HeldInfo | null, px: number, py: number): void {
    if (!info) { tip.hidden = true; return; }
    if ('held' in info) {
      const slot = info.slot === 'free' ? 'machine slot free' : info.slot === 'assembler' ? 'assembler here' : info.slot === 'Mk1' ? 'HQ: Mk1 assembler'
        : info.slot === 'at risk' ? 'assembler AT RISK: block is back on the front' : 'front block: slot taken by the defence ring';
      const rubble = info.district === 'out' ? 'no rubble (outskirts)' : info.poolLeft > 0 ? `rubble left ${Math.floor(info.poolLeft)} (${pct(info.poolFrac)})` : 'dug out: no rubble left';
      tip.innerHTML = '';
      tip.append(el('div', undefined, `Held (${info.x},${info.y}) · ${slot}`), el('div', info.poolLeft > 0 ? 'muted' : 'bad', `${info.district} · ${rubble}`));
      tip.hidden = false;
      const w0 = tip.offsetWidth, h0 = tip.offsetHeight;
      tip.style.left = `${Math.min(px + 14, window.innerWidth - w0 - 8)}px`; tip.style.top = `${Math.min(py + 14, window.innerHeight - h0 - 8)}px`;
      return;
    }
    const sign = info.frontDelta >= 0 ? '+' : '−';
    const line1 = `Claim — rot ${pct(info.rot)} · front ${sign}${Math.abs(info.frontDelta)} · closes ${info.closes}`;
    const cost = info.cost ? ` · ${info.cost.copper} Cu ${info.cost.steel} steel` : '';
    const line2 = `${info.district}${info.well ? ' · well' : ''}${cost} · wake bloom ≈ ${info.wakeBloomCrawlers} crawlers`;
    tip.innerHTML = '';
    tip.append(el('div', undefined, line1), el('div', 'muted', line2));
    if (!info.ok) tip.append(el('div', 'bad', info.reason ?? ''));
    tip.hidden = false;
    const w = tip.offsetWidth, hgt = tip.offsetHeight;
    tip.style.left = `${Math.min(px + 14, window.innerWidth - w - 8)}px`;
    tip.style.top = `${Math.min(py + 14, window.innerHeight - hgt - 8)}px`;
  }

  return {
    update, tooltip, tooltipText, toast, setView,
    setSelectedEdge(e) {
      selected = e ? e.id : null; ringKey = '';
      if (e) toast(`Edge (${e.from.x},${e.from.y}) → (${e.to.x},${e.to.y}) is ring position ${e.ringPos + 1} of ${session.state.ring.length}; hopper ${pct(e.level)}`);
    },
  };
}
