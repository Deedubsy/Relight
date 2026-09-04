import Phaser from 'phaser';
import { SimEvent, flowSummary, canPlace, place, remove, canPickUp, rotate, queueCraft, setHandMine, Kind, Dir, handFeed, cellLights, blockLights, substationAt, poleGrid,
  hqLot, blockOfTile, ground, chestCount, chestTake, chestPut, ChestItem, invStacks, currentPath, describeGround, cityGeomOf, segBetween,
} from '@relight/sim';
import { parseUrl, createSession, setSpeed, runTicks, loadSnapshot, frame, Session, queue } from './session';
import { MapScene, MapView, SceneHooks, MAP_W, MAP_H } from './mapScene';
import { CityMapScene } from './cityMapScene';
import { WorldScene, Tool } from './worldScene';
import { View } from './view';
import { createPanel } from './panel';
import { exportJson, summarise } from './telemetry';

const params = parseUrl(location.search);
let loadError: string | null = null;
let session: Session;
try {
  session = createSession(params, params.state ? await loadSnapshot(params.state) : null);
} catch (e) {
  loadError = (e as Error).message;
  session = createSession({ ...params, state: null });
}
/** Both views share one block. M1 (D5): on foot the block is the engineer's — map → world lands the camera on the
 *  engineer, world → map marks the block they stand in. Without the flow layer (?flow=0) the old hand-off stays: the
 *  block under the map cursor, the block under the world camera's centre. */
const view: View = { mode: params.view, focus: [session.state.start[0], session.state.start[1]], switchedAt: 0 };

const panel = createPanel(session, document.getElementById('panel')!, {
  onSelectEdge(id) { mapScene.selectEdge(id); },
  onToggleView() { toggleView(); },
});
if (loadError) panel.toast(`Could not load the snapshot (${loadError}); started a fresh seed ${session.state.seed} instead`, 'bad');
else if (session.scenario === 'B') panel.toast(`Snapshot loaded at ${session.telemetry.meta.startT / 3600 | 0}:${String(Math.floor(session.telemetry.meta.startT / 60) % 60).padStart(2, '0')} — paused. Press space or a speed to begin.`, 'good');

function describe(events: SimEvent[]): void {
  for (const ev of events) {
    switch (ev.type) {
      case 'held':
        if (ev.facility) panel.toast(`Reached the ${ev.facility}`, 'good');
        if (ev.survivor) panel.toast(`${ev.survivor}: "We're in."`, 'good');
        break;
      case 'fall': panel.toast(`Block (${ev.x},${ev.y}) lost — ${ev.reason}`, 'bad'); break;
      case 'sub-off': panel.toast(`Substation (${ev.x},${ev.y}) stopped: ${session.state.config.unfedN} crawlers unfed. It falls if this goes on.`, 'bad'); break;
      case 'sub-on': panel.toast(`Substation (${ev.x},${ev.y}) back on`, 'good'); break;
      case 'claim-rejected': panel.toast(`Claim (${ev.x},${ev.y}) rejected: ${ev.reason}`, 'bad'); break;
      case 'assembler': panel.toast(`Assembler built on (${ev.x},${ev.y}) — ${ev.count} running`, 'good'); break;
      case 'assembler-rejected': panel.toast(ev.reason === 'no free interior slot' ? 'No assembler: no free machine slot (enclose a block first)' : 'No assembler: cannot afford it', 'bad'); break;
      case 'machine-lost': panel.toast(`Assembler on (${ev.x},${ev.y}) lost with its block — ${ev.count} running`, 'bad'); break;
      case 'run-dry': panel.toast(`Block (${ev.x},${ev.y}) is dug out — no more ${ev.district === 'civ' ? 'stone' : ev.district === 'res' ? 'copper' : 'steel'} from it`); break;
      case 'reorder': panel.toast('Ring order changed'); break;
      // M3 (rule 8): the hopper, Generator and §14 shed rules surface as toasts from the sim's events
      case 'hopper-empty': panel.toast(`Hopper EMPTY on block (${ev.x},${ev.y}) facing (${ev.nx},${ev.ny}) — its pip is red until it is fed`, 'bad'); break;
      case 'gen-dry': panel.toast('The Generator burned its last coal — hand-feed it (click it with the hand) or belt coal in. No power until then.', 'bad'); break;
      case 'brownout': panel.toast(`Brownout: demand ${Math.round(ev.demandKw)} kW over ${Math.round(ev.supplyKw)} kW supply — machines shed first (§14), then assemblers, then substations`, 'bad'); break;
      case 'shed': panel.toast(ev.machine ? `Brownout: a ${ev.machine} switched off` : ev.x < 0 ? 'Brownout: an assembler switched off' : `Brownout: substation (${ev.x},${ev.y}) switched off — its streetlights are out`, 'bad'); break;
      case 'restore': panel.toast(ev.machine ? `Power back: ${ev.machine} running again` : ev.x < 0 ? 'Power back: assembler running again' : `Power back: substation (${ev.x},${ev.y}) on`, 'good'); break;
      case 'claim': {
        if (!session.state.flow) break;
        const kits = session.state.engineer.inv.kit ?? 0;
        panel.toast(`Poles strung to (${ev.x},${ev.y}) — its streetlights come on when it is held${kits ? '' : '. Its edges wait for a kit: take kits from the Depot chest (I) and walk there'}`);
        break;
      }
      case 'kitted': panel.toast(`Kits laid on block (${ev.x},${ev.y}) — ${ev.edges} edge${ev.edges === 1 ? '' : 's'} armed`, 'good'); break;
      case 'engineer-down': panel.toast('The engineer is down — back at the HQ workbench in 10 s', 'bad'); break;
    }
  }
}

const game = new Phaser.Game({
  type: Phaser.AUTO,
  parent: 'map',
  width: MAP_W, height: MAP_H,
  backgroundColor: '#0b0e1a',
  render: { antialias: true, pixelArt: false },
  disableContextMenu: true,   // right click removes a machine in the world view
  scene: [],
});
const hooks: SceneHooks = {
  onHover: (info, px, py) => panel.tooltip(info, px, py),
  onPipSelect: e => panel.setSelectedEdge(e),
};
// D6: the city map draws polygons; the lattice MapScene stays for ?map=lattice and the lattice-era snapshots
const mapScene: MapView = session.state.city ? new CityMapScene(session, hooks) : new MapScene(session, hooks);
const worldScene = new WorldScene(session, view, { onHoverText: (text, px, py) => panel.tooltipText(text, px, py), onToast: (msg, kind) => panel.toast(msg, kind), onChest: () => panel.togglePockets() });
panel.onPick = kind => { worldScene.setTool(kind); panel.toast(`${kind} in hand — left-click places it, R rotates, right-click clears the hand`); };
game.scene.add('map', mapScene, view.mode === 'map');
game.scene.add('world', worldScene, view.mode === 'world');
if (view.mode === 'world') worldScene.centreOn(view.focus[0], view.focus[1]);
panel.setView(view.mode);

// The sim runs once per game step whichever view is up; the map view consumes the events for its pulses.
game.events.on(Phaser.Core.Events.STEP, (time: number, delta: number) => {
  const events = frame(session, Math.min(0.1, delta / 1000));
  if (events.length) { mapScene.consume(events, time); describe(events); }
  panel.update(performance.now());
});

/** The block the engineer stands in (or last stood in), for the map's marker. */
function engineerBlock(): [number, number] {
  const st = session.state, e = st.engineer;
  const i = e.block >= 0 ? e.block : blockOfTile(st, Math.floor(e.x), Math.floor(e.y));
  const b = st.blocks[i] ?? st.blocks[0];
  return [b.x, b.y];
}

function toggleView(): void {
  if (view.mode === 'map') {
    view.focus = session.state.flow ? engineerBlock() : (mapScene.hoverBlock() ?? view.focus);
    view.mode = 'world';
    panel.tooltip(null, 0, 0);
    game.scene.sleep('map');
    game.scene.run('world');
    worldScene.centreOn(view.focus[0], view.focus[1]);
  } else {
    view.focus = worldScene.focusBlock();
    view.mode = 'map';
    panel.tooltipText(null, 0, 0);
    game.scene.sleep('world');
    game.scene.run('map');
    mapScene.markFocus(view.focus, performance.now());
  }
  view.switchedAt = session.state.t;
  panel.setView(view.mode);
}

/** D-B1-5 keys: P pause, - / = speed (1×, 4×, 16×), M map ↔ world, Tab or I the pockets, B the build menu, Esc closes
 *  anything and clears the hand, ` the debug panel; Space is the dodge and the digits the hotbar, both the world
 *  scene's (Shift sprints there too). */
const SPEEDS = [1, 4, 16];
window.addEventListener('keydown', ev => {
  if ((ev.target as HTMLElement)?.tagName === 'INPUT') return;
  const k = ev.key;
  if (k === 'p' || k === 'P') setSpeed(session, session.state.speed === 0 ? 1 : 0);
  else if (k === '-' || k === '_' || k === '=' || k === '+') {
    const cur = SPEEDS.indexOf(session.state.speed), next = k === '-' || k === '_' ? Math.max(0, cur - 1) : Math.min(SPEEDS.length - 1, cur + 1);
    setSpeed(session, SPEEDS[cur < 0 ? 0 : next]);
  }
  else if (k === 'm' || k === 'M') toggleView();   // D5: M = map view
  else if (k === 'i' || k === 'I' || k === 'Tab') { ev.preventDefault(); panel.togglePockets(); }   // M1: the pockets and the Depot chest
  else if (k === 'b' || k === 'B') panel.toggleBuild();
  else if (k === '`') panel.toggleDebug();
  else if (k === 'Escape') { panel.closeAll(); if (view.mode === 'world') worldScene.key(k); }
  else if (k === ' ') { if (view.mode === 'world') ev.preventDefault(); }   // the dodge (worldScene reads the key itself)
  else if (view.mode === 'world' && worldScene.key(k)) ev.preventDefault();
});

// dev/test hooks (not player controls)
let frames = 0, frameMs: number[] = [];
game.events.on(Phaser.Core.Events.POST_STEP, (_t: number, delta: number) => { frames++; if (frameMs.length < 100000) frameMs.push(delta); });
(window as unknown as { __relight: unknown }).__relight = {
  session, view,
  run: (ticks: number) => { runTicks(session, ticks); panel.update(performance.now()); return summarise(session.telemetry, session.state); },
  summary: () => summarise(session.telemetry, session.state),
  exportJson: () => exportJson(session.telemetry, session.state),
  stateJson: () => JSON.stringify(session.state),
  configHash: session.telemetry.meta.configHash,
  toggleView,
  /** M1 (prompt B): the bots' and dev hooks — not player controls (D-B1-5). `walkTo(x, y)` sets a walk-here target on
   *  a tile (the map view's click does the same for the player); `engineer()` reads the engineer (tile position,
   *  block, pockets, HP, stamina, path length left); `sprint`, `dodge` and `aim` drive the body the way the keys do. */
  walkTo: (x: number, y: number) => queue(session, { type: 'move', x: x + 0.5, y: y + 0.5 }),
  sprint: (on: boolean) => queue(session, { type: 'sprint', on }),
  dodge: () => queue(session, { type: 'dodge' }),
  aim: (at: [number, number] | null) => queue(session, { type: 'aim', at }),
  setTool: (t: Tool) => worldScene.setTool(t),
  engineer: () => {
    const e = session.state.engineer, p = currentPath(session.state);
    return { x: e.x, y: e.y, block: e.block, dest: e.dest, target: e.target, hp: e.hp, down: e.down, inv: { ...e.inv }, stacks: invStacks(e.inv), walked: e.walked, pathLeft: p ? p.path.length - p.at : 0,
             stamina: e.stamina, dash: e.dash, fired: e.fired, kills: e.kills, hurt: e.hurt, shootS: e.shootS, danger: e.danger, dangerShot: e.dangerShot };
  },
  ground: () => { const G = ground(session.state); return { tw: G.tw, th: G.th, blocks: G.blocks.length }; },
  /** Dev hook (D-B1-5 check): a street segment's ridge midpoint and the lot tile of block `a` nearest it. */
  edgeGeom: (a: number, b: number) => {
    const st = session.state, cg = cityGeomOf(st), G = ground(st), sg = segBetween(cg, a, b);
    if (!sg) return null;
    let best = -1, bd = Infinity;
    for (const t of cg.blocks[a].tiles) { const tx = t % G.tw, ty = Math.floor(t / G.tw), d = Math.hypot(tx - sg.mx, ty - sg.my); if (d < bd && !st.flow?.occ[t]) { bd = d; best = t; } }
    return { mx: sg.mx, my: sg.my, len: sg.len, standX: best % G.tw + 0.5, standY: Math.floor(best / G.tw) + 0.5, dist: bd };
  },
  describe: (tx: number, ty: number) => describeGround(session.state, tx, ty),
  chest: {
    count: (item: ChestItem) => chestCount(session.state, item),
    take: (item: ChestItem, n: number) => chestTake(session.state, item, n),
    put: (item: ChestItem, n: number) => chestPut(session.state, item, n),
  },
  togglePockets: () => panel.togglePockets(),
  world: { get zoom() { return worldScene.zoom; }, drawMs: () => { const r = { ema: +worldScene.drawMs.toFixed(2), worst: +worldScene.drawWorstMs.toFixed(1) }; worldScene.drawWorstMs = 0; return r; }, setZoom: (z: number) => worldScene.setZoom(z), centreOn: (x: number, y: number) => worldScene.centreOn(x, y), focus: () => worldScene.focusBlock(), get drawn() { return worldScene.drawn; },
           get tool() { return worldScene.tool; }, key: (k: string) => worldScene.key(k), screenOf: (x: number, y: number) => worldScene.screenOf(x, y) },
  /** M2 flow layer: place/remove/rotate by city tile, `hq(lx, ly)` = city tile of a lot tile on the HQ lot. */
  flow: {
    summary: () => flowSummary(session.state),
    canPlace: (kind: Kind, tx: number, ty: number) => canPlace(session.state, kind, tx, ty),
    place: (kind: Kind, tx: number, ty: number, dir: Dir = 0) => place(session.state, kind, tx, ty, dir),
    remove: (tx: number, ty: number) => remove(session.state, tx, ty),
    canPickUp: (tx: number, ty: number) => canPickUp(session.state, tx, ty),   // M2: the pick-up check the right-click makes
    rotate: (tx: number, ty: number) => rotate(session.state, tx, ty),
    craft: (n = 1) => queueCraft(session.state, n),
    mine: (at: [number, number] | null) => setHandMine(session.state, at),
    hq: (lx: number, ly: number): [number, number] => hqLot(session.state, lx, ly),
    // M3: hand-feed a turret or Generator, and the light/substation/pole/power queries the world view draws from
    feed: (tx: number, ty: number) => handFeed(session.state, tx, ty),
    lights: (bx: number, by: number) => cellLights(session.state, bx, by),
    blockLights: (i: number) => blockLights(session.state, i),
    substation: (bx: number, by: number) => substationAt(session.state, bx, by),
    poles: () => { const g = poleGrid(session.state); return { connected: g.connected.size, links: g.links.length, reached: g.reached.slice() }; },
    power: () => session.state.flow?.power ?? null,
  },
  /** Render-loop sample since the last call: frames, mean and worst frame time (ms), frames over 50 ms. */
  fps: () => { const n = frameMs.length, mean = frameMs.reduce((a, b) => a + b, 0) / Math.max(1, n), worst = Math.max(0, ...frameMs), slow = frameMs.filter(d => d > 50).length; const r = { frames, sampled: n, meanMs: +mean.toFixed(2), worstMs: +worst.toFixed(1), over50ms: slow, fps: +(1000 / mean).toFixed(1) }; frames = 0; frameMs = []; return r; },
};
