/** DOM side panel: HUD, ring order (drag to reorder), stock + assembler, facilities, session summary, export. */
import { SimState, FrontEdgeView, ClaimInfo, HeldInfo, frontList, hud, facilityList, survivorList, shapeMetrics, clockOf, slotInfo, SKYLINE_RANGE, flowSummary, queueCraft, SHOT, MACHINE_COST,
  CHEST_ITEMS, ChestItem, chestCount, chestTake, chestPut, nearDepot, invStacks, INV_STACKS, KIT_STACKS, stackSize, REACH, Kind, KINDS, lockReason, survivorJoined, TURRET_RANGE, TURRET_HOPPER, LAMP_RADIUS,
  currentGoal, Goal, blockLabel, blockNameAt, edgeName, idxOf } from '@relight/sim';
import { Session, setSpeed, queue, shareUrl, record, saveSlot, slotUrl, hasSlot, makeSessionSave } from './session';
import { debugView } from './view';
import { summarise, exportJson } from './telemetry';
import { hourReport } from '@relight/sim';

/** M6: what the export carries beside the telemetry — the command log (the replay's input) and, under the hour bot, its log and report. */
export function exportExtra(session: Session): Record<string, unknown> {
  return { commands: session.log, hour: session.hour ? hourReport(session.state, session.hour) : null };
}

export interface PanelHooks { onSelectEdge(id: number | null): void; onToggleView(): void }
/** D-B1-5: the build menu (key B) picks a building into the hand; the world scene installs the pick. */
export type BuildKind = Exclude<Kind, 'depot'>;

export interface Panel {
  update(nowMs: number): void;
  setSelectedEdge(e: FrontEdgeView | null): void;
  tooltip(info: ClaimInfo | HeldInfo | null, px: number, py: number): void;
  /** Plain lines (\n-separated) at a screen point; null hides. The world view's tile tooltip. */
  tooltipText(text: string | null, px: number, py: number): void;
  setView(mode: 'map' | 'world'): void;
  /** Show/hide the debug sections (stock, line, ring order, skyline, summary); returns the new visibility. */
  toggleDebug(): boolean;
  /** M1: show/hide the pockets and the Depot chest (Tab or I, or E on the Depot); returns the new visibility. */
  togglePockets(): boolean;
  /** D-B1-5: show/hide the build menu (B); a click on a row puts that building in the hand. */
  toggleBuild(): boolean;
  /** Esc: close the pockets and the build menu. */
  closeAll(): void;
  onPick: ((kind: BuildKind) => void) | null;
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
  const flow = !!st.flow;
  root.innerHTML = '';

  // header
  const header = el('section');
  const head = el('header');
  const mapName = st.city ? `${st.city.preset} city` : 'lattice';   // D6: the street-first city is the default map
  const h1 = el('h1', undefined, `Relight · map view · ${mapName} · seed ${st.seed}`);
  head.append(h1);
  if (session.scenario === 'B') head.append(el('span', 'badge', `Scenario B · from ${clockOf(session.startT)}`));
  const btnLink = el('button', undefined, 'Copy link');
  btnLink.title = 'Share this seed and settings';
  btnLink.onclick = () => {
    const url = shareUrl(session.params);
    navigator.clipboard?.writeText(url).then(() => toast('Link copied', 'good'), () => window.prompt('Copy this link', url));
  };
  const btnView = el('button', undefined, 'World view (M)');
  btnView.title = 'Toggle map ↔ world view at the same block (M, D5)';
  btnView.onclick = () => hooks.onToggleView();
  head.append(btnView, btnLink);
  header.append(head);
  // RI-02 (§11.2): the save / load baseline — slot 1 in this browser (Ctrl+S / Ctrl+O), beside the download below
  const saveRow = el('div', 'row');
  const btnSave = el('button', undefined, 'Save (Ctrl+S)');
  btnSave.title = 'Save the game to slot 1 in this browser: the state and every command so far. Load reloads the page from it, paused.';
  btnSave.onclick = () => saveGame();
  const btnLoad = el('button', undefined, 'Load (Ctrl+O)');
  btnLoad.title = 'Reload the page from slot 1 (the last Save in this browser). Unsaved progress is lost.';
  btnLoad.onclick = () => loadGame();
  const saveNote = el('span', 'hint', '');
  saveRow.append(btnSave, btnLoad, saveNote);
  header.append(saveRow);
  header.append(el('p', 'hint', 'Click a Dark block next to your territory to claim it. Held blocks facing Dark are ammo edges (red streets); their pips go ● ▲ ✕ as the hopper empties. Interior blocks (white rim) hold a machine slot. Press ` for the debug panel (stock, line, ring order, skyline, summary) and the block coordinates.'));
  root.append(header);
  function saveGame(): void {
    try {
      const save = saveSlot(session, '1');
      saveNote.textContent = `slot 1: ${clockOf(save.t)}`;
      toast(`Saved to slot 1 at ${clockOf(save.t)} (state ${save.hash}) — Ctrl+O or Load reloads it`, 'good');
    } catch (e) { toast(`Could not save: ${(e as Error).message}`, 'bad'); }
  }
  function loadGame(): void {
    if (!hasSlot('1')) { toast('Slot 1 is empty in this browser — Ctrl+S saves to it', 'bad'); return; }
    if (!window.confirm('Reload from slot 1? Unsaved progress is lost.')) return;
    location.href = slotUrl(session, '1');
  }
  if (hasSlot('1')) saveNote.textContent = session.params.state === 'local:1' ? 'loaded from slot 1' : 'slot 1 has a save';

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
  speedRow.append(el('span', 'hint', 'keys: P pause · - / = speed · M map ↔ world · Tab/I pockets · B build menu · Esc closes · ` debug panel'));
  hudSec.append(speedRow);
  root.append(hudSec);

  // M1 (D5): the pockets (40 stacks) and the Depot chest. Transfers need the engineer within reach of the Depot; the
  // sim refuses otherwise and the reason is toasted. Kits are free to draw (engineer.ts). Hidden until I.
  // GAME-ASSUMPTION (M1): a human is not auto-restocked the way the harness bot is (`restock` on standing at the HQ):
  // kits are taken here, one a claim, and a claim made with no kit in the pockets toasts that its edges wait.
  const pocketSec = el('section');
  pocketSec.hidden = true;
  pocketSec.append(el('h2', undefined, 'Pockets and the Depot chest (Tab / I, or E on the Depot)'));
  const pocketHead = el('p', 'hint', '');
  pocketSec.append(pocketHead);
  const pocketList = el('ul', 'plain');
  const pocketRows = CHEST_ITEMS.map(item => {
    const l = el('li'), a = el('span', undefined, item === 'magazine' ? 'magazines' : item === 'kit' ? 'kits (10 stacks each)' : item), v = el('span', 'mono', '');
    const row = el('div', 'row');
    const n = item === 'kit' ? 1 : item === 'magazine' ? 5 : stackSize(item);
    const take = el('button', undefined, `Take ${n}`), put = el('button', undefined, 'Put all');
    take.onclick = () => { const r = chestTake(session.state, item, n); record(session, { type: 'chestTake', item, n }); if (r.moved) toast(`${r.moved} ${item} into the pockets`, 'good'); else toast(r.reason, 'bad'); };
    put.onclick = () => { const r = chestPut(session.state, item, 1e9); record(session, { type: 'chestPut', item, n: 1e9 }); if (r.moved) toast(`${r.moved} ${item} into the Depot`, 'good'); else toast(r.reason, 'bad'); };
    row.append(take, put);
    l.append(a, v); pocketList.append(l); pocketList.append(row);
    return { item: item as ChestItem, v, take, put };
  });
  // M2: machines picked up ride in the pockets as one stack each; they go down again from the hand (hotbar), never through the chest
  const pocketMach = el('li'); const pocketMachV = el('span', 'mono', 'none');
  pocketMach.append(el('span', undefined, 'machines carried (one stack each)'), pocketMachV); pocketList.append(pocketMach);
  pocketSec.append(pocketList);
  pocketSec.append(el('p', 'hint', `Pockets: ${INV_STACKS} stacks (a kit is ${KIT_STACKS}). The chest answers within ${REACH} tiles of the Depot. A claim's edges wait for a kit the engineer carries there. Machines are placed from the pockets: a carried one, else its price in carried steel and copper; right-click picks a machine up into the pockets with what it holds.`));
  root.append(pocketSec);

  // D-B1-5: the build menu (B). The hotbar (1–8) is its shortcut; 9 is the rifle. Prompt B M3: the Electricians'
  // three (0, [, ]) are listed locked, with who unlocks them, until the group's block turns Held (rule 8).
  const buildSec = el('section');
  buildSec.hidden = true;
  buildSec.append(el('h2', undefined, 'Build menu (B)'));
  const buildList = el('ul', 'plain');
  const BUILD: { kind: BuildKind; key: string; what: string }[] = [
    { kind: 'belt', key: '1', what: '7.5 items/s' }, { kind: 'inserter', key: '2', what: 'one item a second across a tile' },
    { kind: 'excavator', key: '3', what: '3×3, 0.5/s onto the belt it faces' }, { kind: 'assembler', key: '4', what: `Assembler (3×3): Shot magazine ${SHOT.seconds} s (${Math.round(60 / SHOT.seconds)}/min), or Wire / Frame / Board — T on it sets the recipe; the Mk2 (3 s) is the purchase` },
    { kind: 'turret', key: '5', what: `2×2, range ${TURRET_RANGE}, ${TURRET_HOPPER}-round hopper` }, { kind: 'lamp', key: '6', what: `lights a ${LAMP_RADIUS}-tile radius` },
    { kind: 'pole', key: '7', what: 'carries power, claims across the street' }, { kind: 'generator', key: '8', what: '2×2, burns coal for power' },
    { kind: 'floodlight', key: '0', what: '2×2, 40 kW, a 12-tile cone along its facing (R rotates)' }, { kind: 'bigpole', key: '[', what: '2×2, reach 12' },
    { kind: 'substation', key: ']', what: '3×3, gives a face that has none (the outskirts) its substation' },
  ];
  const buildCarried: { kind: BuildKind; v: HTMLElement; btn: HTMLButtonElement; lock: HTMLElement }[] = [];
  for (const b of BUILD) {
    const li = el('li'), btn = el('button', undefined, `${b.key} · ${b.kind}`) as HTMLButtonElement;
    const c = MACHINE_COST[b.kind];
    btn.title = `Put a ${b.kind} in the hand (hotbar ${b.key})`;
    btn.onclick = () => panelRef.onPick?.(b.kind);
    const carried = el('span', 'mono', ''), lock = el('span', 'hint', '');
    li.append(btn, el('span', 'hint', ` ${c.steel} steel${c.copper ? ` + ${c.copper} Cu` : ''} from the pockets · ${b.what} `), carried, lock);
    buildCarried.push({ kind: b.kind, v: carried, btn, lock });
    buildList.append(li);
  }
  // GAME-ASSUMPTION (M4): the Arsenal's rifle upgrade (§8, 1.4 s a crawler) is a toolbar entry only — the upgrade itself is outside the hour
  const mk2Lock = el('span', 'hint', '');
  const mk2 = el('li');
  mk2.append(el('span', 'mono', 'Rifle Mk2'), el('span', 'hint', ' · twin barrels, 1.4 s a crawler (the Mk1 takes 3 rounds, 2 s) '), mk2Lock);
  buildList.append(mk2);
  buildSec.append(buildList);
  buildSec.append(el('p', 'hint', 'In the world view: left-click places what the hand holds from the pockets (a carried machine first, else its price in carried steel and copper — take them from the chest), R rotates, Q pipettes the machine under the cursor or clears the hand, right-click with an empty hand picks a machine up into the pockets. 9 is the rifle: while it is in hand, holding left-click fires toward the cursor and nothing else happens until you clear it.'));
  root.append(buildSec);

  // Rework Step 5: the side panel is held / front / interior / ammo made vs demanded / stock / blocks lost;
  // the rest sits behind the debug key (backquote) so a tester reads the map, not the panel.
  const debug = el('div', 'debug');
  debug.hidden = true;
  root.append(debug);

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
  // RI-01 (D-P4-5): under the tile line every Assembler is placed by hand; the block-level purchase is refused by the sim
  asmRow.hidden = flow;
  stockSec.append(asmRow);
  if (eco) stockSec.append(el('p', 'hint', `A claim costs ${st.config.eco.claimCost.copper} Cu (10 wire) and ${st.config.eco.claimCost.steel} steel (5 frames). A magazine costs ${st.config.eco.magazineCost.steel} steel + ${st.config.eco.magazineCost.copper} Cu (§12); assemblers stop when the stock runs out. Held blocks yield their district's rubble: civic stone, residential copper, industrial steel.`));
  debug.append(stockSec);

  // M2: the tile line on the HQ lot (world view). Counts and rates come from flowSummary; the Craft button queues a
  // hand craft (§11) at the workbench, from the pockets into the pockets (prompt B M2).
  const lineSec = el('section');
  let vMach: HTMLElement | null = null, vBeltItems: HTMLElement | null = null, vLineRate: HTMLElement | null = null, vLineMade: HTMLElement | null = null,
      vHand: HTMLElement | null = null, vBuffer: HTMLElement | null = null, vCoal: HTMLElement | null = null, vCrafts: HTMLElement | null = null,
      vPower: HTMLElement | null = null, vGens: HTMLElement | null = null, vTurrets: HTMLElement | null = null, vLamps: HTMLElement | null = null, vBrown: HTMLElement | null = null;
  let btnCraft: HTMLButtonElement | null = null;
  if (flow) {
    lineSec.append(el('h2', undefined, 'Line on the HQ lot (world view)'));
    const lineList = el('ul', 'plain');
    const li2 = (label: string) => { const l = el('li'); const a = el('span', undefined, label); const v = el('span', 'mono', '0'); l.append(a, v); lineList.append(l); return v; };
    vMach = li2('Excavators / belts / inserters / Assemblers'); vBeltItems = li2('Items on belts'); vLineRate = li2('Line mag/min (assemblers crafting now)');
    vLineMade = li2('Magazines made by the line'); vHand = li2('Mined / crafted by hand'); vBuffer = li2('Magazines in the line buffer'); vCoal = li2('Coal (Depot + Generators)'); vCrafts = li2('Hand crafts queued');
    // M3: power as one number (§14), the Generators, the turrets' hoppers, the lights
    vPower = li2('Power: load / supply kW (demand)'); vGens = li2('Generators burning / built'); vTurrets = li2('Turret rounds / capacity · on belts');
    vLamps = li2('Lamps lit / built · poles connected'); vBrown = li2('Brownout seconds · machines running at %');
    lineSec.append(lineList);
    const craftRow = el('div', 'row');
    btnCraft = el('button', undefined, `Craft a magazine by hand (${SHOT.inputs.steel} steel + ${SHOT.inputs.copper} Cu, ${SHOT.seconds} s)`);
    btnCraft.title = 'E on the workbench in the world view. A hand craft takes its steel and copper from the pockets and puts the magazine in the pockets; the engineer stays within reach of the Depot while it runs.';
    btnCraft.onclick = () => { const why = queueCraft(session.state, 1); record(session, { type: 'craft', item: 'magazine', count: 1 }); if (why) toast(why, 'bad'); };
    craftRow.append(btnCraft);
    lineSec.append(craftRow);
    const mc = MACHINE_COST;
    lineSec.append(el('p', 'hint', `World view: the hotbar 1–8 is the build menu's shortcut (B lists the buildings and their costs), 9 the rifle. R rotate, Q pipette / clear the hand, right-click picks a machine up into the pockets with an empty hand. WASD moves (Shift sprints, Space dodges); hand actions reach ${REACH} tiles. Hold the left button on rubble with an empty hand to mine it a unit a second into the pockets; E on the workbench crafts a magazine (${SHOT.inputs.steel} steel + ${SHOT.inputs.copper} Cu, ${SHOT.seconds} s). Magazines reach the ring only through the Depot: belt or inserter them into it, or carry them (Tab / I).`));
    debug.append(lineSec);
  }

  // ring order
  const ringSec = el('section');
  ringSec.append(el('h2', undefined, 'Ammo ring order — drag to reorder'));
  const ring = el('ol', 'ring');
  ringSec.append(ring);
  const ringHint = el('p', 'hint', 'Edges fill from the top. The last edges starve first when production falls short. Pips: ● hopper at least half full · ▲ running low · ✕ empty, blinking.');
  ringSec.append(ringHint);
  debug.append(ringSec);

  // facilities (§8: silhouettes within SKYLINE_RANGE blocks of a Held block) and survivors (§8: a block's contents
  // show once a neighbour is Held)
  const facSec = el('section');
  facSec.append(el('h2', undefined, 'Skyline'));
  const facList = el('ul', 'plain');
  facSec.append(facList);
  facSec.append(el('p', 'hint', `A facility shows once it is within ${SKYLINE_RANGE} blocks of a Held block; survivors show once a block next to them is Held, and join when their block is.`));
  const survList = el('ul', 'plain');
  facSec.append(el('h2', undefined, 'Survivors'), survList);
  debug.append(facSec);

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
    const json = exportJson(session.telemetry, session.state, exportExtra(session));
    const blob = new Blob([json], { type: 'application/json' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `relight-seed${st.seed}-${clockOf(st.t).replace(/:/g, '')}.json`;
    document.body.append(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 2000);
    toast('Telemetry exported', 'good');
  };
  sumRow.append(btnExport);
  const btnState = el('button', undefined, 'Download save');
  btnState.title = 'Download a save file (the state and the command log); load it later with ?state=<file url>';
  btnState.onclick = () => {
    const blob = new Blob([JSON.stringify(makeSessionSave(session))], { type: 'application/json' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `relight-state-seed${st.seed}-${clockOf(session.state.t).replace(/:/g, '')}.json`;
    document.body.append(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 2000);
    toast('Save file downloaded', 'good');
  };
  sumRow.append(btnState);
  sumSec.append(sumRow);
  debug.append(sumSec);

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
    const key = edges.map(e => `${e.id}:${e.pip}:${Math.round(e.level * 20)}`).join(',') + `|${selected}|${debugView.coords}`;
    if (key === ringKey || dragging !== null) return;
    ringKey = key;
    ring.innerHTML = '';
    for (const e of edges) {
      const l = el('li');
      l.draggable = true;
      l.dataset.id = String(e.id);
      if (e.id === selected) l.classList.add('selected');
      // RI-02: the street's name (names.ts); the block coordinates only behind the ` toggle
      const where = debugView.coords ? `(${e.from.x},${e.from.y}) → (${e.to.x},${e.to.y})  ${e.darkDistrict}` : edgeName(session.state, e.id);
      l.append(el('span', 'pos', String(e.ringPos + 1)), el('span', `pip ${e.pip}`, PIP_GLYPH[e.pip]), el('span', 'mono', where), el('span', 'lvl', pct(e.level)));
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

  // RI-02 (§11.2, D-GB-2 (a), rule 8): the current-goal line over the canvas — goal.ts `currentGoal` read off the
  // state once a sim second; the goal text and its reason, and the shortage line under it while something is short.
  const goalEl = document.getElementById('goal')!;
  const goalText = goalEl.querySelector('.goal-text')!, goalWhy = goalEl.querySelector('.goal-why')!, goalSupport = goalEl.querySelector('.goal-support') as HTMLElement;
  let goalSecond = -1, goalKey = '';
  function updateGoal(s: SimState): void {
    const sec = Math.floor(s.t);
    if (sec === goalSecond) return;
    goalSecond = sec;
    const g: Goal = currentGoal(s);
    const key = `${g.goal.id}|${g.goal.text}|${g.goal.why}|${g.support ? `${g.support.id}|${g.support.text}` : ''}`;
    if (key === goalKey) return;
    goalKey = key;
    goalText.textContent = g.goal.text; goalWhy.textContent = g.goal.why;
    goalSupport.textContent = g.support ? `${g.support.text} — ${g.support.why}` : '';
    goalSupport.hidden = !g.support;
    goalEl.hidden = false;
  }
  let lastUpdate = -1e9;
  let lastFacKey = '';
  function update(nowMs: number): void {
    if (nowMs - lastUpdate < 150) return;
    lastUpdate = nowMs;
    const s: SimState = session.state;
    updateGoal(s);
    const h = hud(s);
    sHeld.b.textContent = String(h.held); sFront.b.textContent = String(h.front); sInt.b.textContent = String(h.interior);
    const fs = flowSummary(s);
    const prod = h.ammo.productionMagPerMin + fs.productionMagPerMin;   // block-level assemblers plus the tile line
    sProd.b.textContent = f1(prod); sDem.b.textContent = f1(h.ammo.demandMagPerMin1);
    sDem.s.classList.toggle('warn', h.ammo.demandMagPerMin1 > prod + 1e-9);
    sStock.b.textContent = f1(h.ammo.stockMags); sLost.b.textContent = String(h.lost);
    sEmpty.b.textContent = String(h.ammo.emptyHoppers); sEmpty.s.classList.toggle('warn', h.ammo.emptyHoppers > 0);
    sClock.b.textContent = h.clock;
    for (const { m, b } of speedBtns) b.classList.toggle('active', s.speed === m);
    if (!pocketSec.hidden) {
      const e = s.engineer, near = nearDepot(s);
      pocketHead.textContent = `${invStacks(e.inv)} / ${INV_STACKS} stacks · ${near ? 'at the Depot' : `walk to the Depot to transfer (${REACH} tiles)`} · HP ${Math.round(e.hp)}`;
      for (const r of pocketRows) {
        const c = chestCount(s, r.item);
        r.v.textContent = `pockets ${e.inv[r.item] ?? 0} · chest ${c === Infinity ? '∞' : c}`;
        r.take.disabled = !near || c <= 0; r.put.disabled = !near || !(e.inv[r.item] > 0);
      }
      const mach = KINDS.filter(k => (e.inv[k] ?? 0) > 0).map(k => `${e.inv[k]} ${k}`);
      pocketMachV.textContent = mach.length ? mach.join(', ') : 'none';
    }
    if (!buildSec.hidden) for (const b of buildCarried) {
      const n = s.engineer.inv[b.kind] ?? 0; b.v.textContent = n > 0 ? `· ${n} carried` : '';
      const lk = lockReason(s, b.kind); b.btn.disabled = !!lk; b.lock.textContent = lk ? ` · locked: ${lk}` : '';
    }
    if (!buildSec.hidden) mk2Lock.textContent = survivorJoined(s, 'Arsenal') ? '· the Arsenal are in — the upgrade itself is outside the hour (prompt B M4)' : '· locked: the Arsenal unlock it — hold their block';
    if (vCu) { vCu.textContent = String(Math.floor(s.stock.copper)); vSteel!.textContent = String(Math.floor(s.stock.steel)); vStone!.textContent = String(Math.floor(s.stock.stone)); vPatch!.textContent = String(Math.floor(s.patch.steel)); }
    vAsm.textContent = String(h.ammo.assemblers);
    const si = slotInfo(s);
    vSlots.textContent = `${si.used} / ${si.free}`; vRisk.textContent = String(si.atRisk); if (vDry) vDry.textContent = String(si.dry);
    const broke = eco && (s.stock.copper < s.config.eco.assemblerCost.copper || s.stock.steel < s.config.eco.assemblerCost.steel);
    btnAsm.disabled = broke || si.free === 0;
    btnAsm.title = si.free === 0 ? 'No free machine slot: an assembler needs an Interior block (every neighbour Held or inert). Enclose a block to get one.'
      : broke ? 'Not enough rubble' : `Goes on ${blockLabel(s, idxOf(s, si.next!.x, si.next!.y), debugView.coords)}; makes ${s.config.asmRate} magazines per minute`;
    if (vMach && s.flow) {
      vMach.textContent = `${fs.excavators} / ${fs.belts} / ${fs.inserters} / ${fs.assemblers}`; vBeltItems!.textContent = String(fs.beltItems);
      vLineRate!.textContent = f1(fs.productionMagPerMin); vLineMade!.textContent = String(fs.magsMade);
      vHand!.textContent = `${s.flow.stats.handMined} / ${s.flow.stats.handCrafted}`;
      vBuffer!.textContent = `${Math.floor(s.buffer / SHOT.count)} / ${Math.floor(s.config.bufferCap / SHOT.count)}`; vCoal!.textContent = String(Math.floor(fs.coal)); vCrafts!.textContent = String(fs.craftsQueued);
      btnCraft!.disabled = s.stock.steel < SHOT.inputs.steel || s.stock.copper < SHOT.inputs.copper;
      vPower!.textContent = `${Math.round(fs.loadKw)} / ${Math.round(fs.supplyKw)} (${Math.round(fs.demandKw)})`; vPower!.parentElement!.classList.toggle('warn', fs.demandKw > fs.supplyKw + 1e-9);
      vGens!.textContent = `${fs.generatorsBurning} / ${fs.generators}`; vGens!.parentElement!.classList.toggle('warn', fs.generators > 0 && fs.generatorsBurning === 0);
      vTurrets!.textContent = `${Math.round(fs.turretRounds)} / ${fs.turretCap} · ${fs.beltAmmo}`; vTurrets!.parentElement!.classList.toggle('warn', fs.turrets > 0 && fs.turretRounds === 0);
      vLamps!.textContent = `${fs.lampsLit} / ${fs.lamps} · ${fs.polesConnected} / ${fs.poles}`;
      vBrown!.textContent = `${Math.round(fs.brownoutS)} · ${Math.round(fs.throttle * 100)} %`; vBrown!.parentElement!.classList.toggle('warn', fs.throttle < 1 - 1e-9);
    }
    renderRing(frontList(s));
    const facs = facilityList(s).filter(f => f.visible);
    const survs = survivorList(s).filter(f => f.revealed);
    const fk = facs.map(f => `${f.name}${f.held ? 1 : 0}`).join() + '|' + survs.map(f => `${f.tag}${f.held ? 1 : 0}`).join() + `|${debugView.coords}`;
    if (fk !== lastFacKey) {
      lastFacKey = fk; facList.innerHTML = ''; survList.innerHTML = '';
      // RI-02: facilities and survivors by their block's name (names.ts); the coordinates only behind the ` toggle
      const at = (x: number, y: number) => blockLabel(s, idxOf(s, x, y), debugView.coords);
      for (const f of facs) { const l = el('li'); l.append(el('span', undefined, `${f.name} · ${at(f.x, f.y)}`), el('span', f.held ? '' : 'muted', f.held ? 'reached' : `${f.dist} blocks from HQ`)); facList.append(l); }
      if (!facs.length) facList.append(el('li', 'muted', 'nothing on the skyline yet'));
      // Phase 7 (STANDARDS dealbreaker 1): from minute one the panel says where blueprints and copy-paste will come from.
      // GAME-ASSUMPTION (GA-EF-3): the row is a fixed line of text ("not yet found"), not a survivor the sim knows; Phase 7
      // replaces it with the real survivor's row and the §8 gift
      { const l = el('li'); l.append(el('span', undefined, 'Blueprints and copy-paste · a survivor\'s gift (Phase 7)'), el('span', 'muted', 'not yet found')); survList.append(l); }
      for (const f of survs) { const l = el('li'); l.append(el('span', undefined, `${f.tag} · ${f.name} · ${at(f.x, f.y)}`), el('span', f.held ? '' : 'muted', f.held ? 'with us' : 'seen')); survList.append(l); }
      if (!survs.length) survList.append(el('li', 'muted', 'no one else found yet'));
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
    h1.textContent = `Relight · ${mode} view · ${mapName} · seed ${st.seed}`;
    btnView.textContent = mode === 'map' ? 'World view (M)' : 'Map view (M)';
  }

  function tooltip(info: ClaimInfo | HeldInfo | null, px: number, py: number): void {
    if (!info) { tip.hidden = true; return; }
    if ('held' in info) {
      const slot = info.slot === 'free' ? 'machine slot free' : info.slot === 'assembler' ? 'assembler here' : info.slot === 'Mk1' ? 'HQ: Mk1 assembler'
        : info.slot === 'at risk' ? 'assembler AT RISK: block is back on the front' : 'front block: slot taken by the defence ring';
      const rubble = info.district === 'out' ? 'no rubble (outskirts)' : info.poolLeft > 0 ? `rubble left ${Math.floor(info.poolLeft)} (${pct(info.poolFrac)})` : 'dug out: no rubble left';
      tip.innerHTML = '';
      tip.append(el('div', undefined, `${blockLabel(session.state, idxOf(session.state, info.x, info.y), debugView.coords)} · Held · ${slot}`), el('div', info.poolLeft > 0 ? 'muted' : 'bad', `${info.district} · ${rubble}`));
      tip.hidden = false;
      const w0 = tip.offsetWidth, h0 = tip.offsetHeight;
      tip.style.left = `${Math.min(px + 14, window.innerWidth - w0 - 8)}px`; tip.style.top = `${Math.min(py + 14, window.innerHeight - h0 - 8)}px`;
      return;
    }
    const sign = info.frontDelta >= 0 ? '+' : '−';
    const line1 = `Claim ${blockLabel(session.state, idxOf(session.state, info.x, info.y), debugView.coords)} — rot ${pct(info.rot)} · front ${sign}${Math.abs(info.frontDelta)} · closes ${info.closes}`;
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

  const panelRef: Panel = {
    update, tooltip, tooltipText, toast, setView, onPick: null,
    toggleDebug() { debug.hidden = !debug.hidden; debugView.coords = !debug.hidden; ringKey = ''; lastFacKey = ''; return !debug.hidden; },
    togglePockets() { pocketSec.hidden = !pocketSec.hidden; lastUpdate = -1e9; return !pocketSec.hidden; },
    toggleBuild() { buildSec.hidden = !buildSec.hidden; return !buildSec.hidden; },
    closeAll() { pocketSec.hidden = true; buildSec.hidden = true; },
    setSelectedEdge(e) {
      selected = e ? e.id : null; ringKey = '';
      if (e) toast(`${edgeName(session.state, e.id)}${debugView.coords ? ` (${e.from.x},${e.from.y}) → (${e.to.x},${e.to.y})` : ''} is ring position ${e.ringPos + 1} of ${session.state.ring.length}; hopper ${pct(e.level)}`);
    },
  };
  return panelRef;
}
