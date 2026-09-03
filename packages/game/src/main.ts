import Phaser from 'phaser';
import { SimEvent, flowSummary, canPlace, place, remove, rotate, queueCraft, setHandMine, Kind, Dir, CELL_TILES, MARGIN_TILES, handFeed, cellLights, substationAt, poleGrid, SIM_DIR_NAMES,
} from '@relight/sim';
import { parseUrl, createSession, setSpeed, runTicks, loadSnapshot, frame, Session } from './session';
import { MapScene, SceneHooks, MAP_W, MAP_H } from './mapScene';
import { WorldScene } from './worldScene';
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
/** Both views share one block: the map hands the block under the cursor to the world camera, the world hands the
 *  block under its camera centre back to the map (constitution Phase 4 M1: "E toggles map ↔ world at the same block"). */
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
      case 'hopper-empty': panel.toast(`Hopper EMPTY on block (${ev.x},${ev.y}), ${SIM_DIR_NAMES[ev.dir]} side — its pip is red until it is fed`, 'bad'); break;
      case 'gen-dry': panel.toast('The Generator burned its last coal — hand-feed it (click it with the hand) or belt coal in. No power until then.', 'bad'); break;
      case 'brownout': panel.toast(`Brownout: demand ${Math.round(ev.demandKw)} kW over ${Math.round(ev.supplyKw)} kW supply — machines shed first (§14), then assemblers, then substations`, 'bad'); break;
      case 'shed': panel.toast(ev.machine ? `Brownout: a ${ev.machine} switched off` : ev.x < 0 ? 'Brownout: an assembler switched off' : `Brownout: substation (${ev.x},${ev.y}) switched off — its streetlights are out`, 'bad'); break;
      case 'restore': panel.toast(ev.machine ? `Power back: ${ev.machine} running again` : ev.x < 0 ? 'Power back: assembler running again' : `Power back: substation (${ev.x},${ev.y}) on`, 'good'); break;
      case 'claim': if (session.state.flow) panel.toast(`Poles strung to (${ev.x},${ev.y}) — its streetlights come on when it is held`); break;
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
const mapScene = new MapScene(session, hooks);
const worldScene = new WorldScene(session, view, { onHoverText: (text, px, py) => panel.tooltipText(text, px, py), onToast: (msg, kind) => panel.toast(msg, kind) });
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

function toggleView(): void {
  if (view.mode === 'map') {
    view.focus = mapScene.hoverBlock() ?? view.focus;
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

window.addEventListener('keydown', ev => {
  if ((ev.target as HTMLElement)?.tagName === 'INPUT') return;
  if (ev.key === ' ') { ev.preventDefault(); setSpeed(session, session.state.speed === 0 ? 1 : 0); }
  else if (ev.key === '1') setSpeed(session, 1);
  else if (ev.key === '2') setSpeed(session, 4);
  else if (ev.key === '3') setSpeed(session, 16);
  else if (ev.key === 'e' || ev.key === 'E') toggleView();
  else if (view.mode === 'world' && worldScene.key(ev.key)) ev.preventDefault();
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
  world: { get zoom() { return worldScene.zoom; }, setZoom: (z: number) => worldScene.setZoom(z), centreOn: (x: number, y: number) => worldScene.centreOn(x, y), focus: () => worldScene.focusBlock(), get drawn() { return worldScene.drawn; },
           get tool() { return worldScene.tool; }, key: (k: string) => worldScene.key(k) },
  /** M2 flow layer: place/remove/rotate by city tile, `hq(lx, ly)` = city tile of a lot tile on the HQ lot. */
  flow: {
    summary: () => flowSummary(session.state),
    canPlace: (kind: Kind, tx: number, ty: number) => canPlace(session.state, kind, tx, ty),
    place: (kind: Kind, tx: number, ty: number, dir: Dir = 0) => place(session.state, kind, tx, ty, dir),
    remove: (tx: number, ty: number) => remove(session.state, tx, ty),
    rotate: (tx: number, ty: number) => rotate(session.state, tx, ty),
    craft: (n = 1) => queueCraft(session.state, n),
    mine: (at: [number, number] | null) => setHandMine(session.state, at),
    hq: (lx: number, ly: number): [number, number] => [session.state.start[0] * CELL_TILES + MARGIN_TILES + lx, session.state.start[1] * CELL_TILES + MARGIN_TILES + ly],
    // M3: hand-feed a turret or Generator, and the light/substation/pole/power queries the world view draws from
    feed: (tx: number, ty: number) => handFeed(session.state, tx, ty),
    lights: (bx: number, by: number) => cellLights(session.state, bx, by),
    substation: (bx: number, by: number) => substationAt(session.state, bx, by),
    poles: () => { const g = poleGrid(session.state); return { connected: g.connected.size, links: g.links.length, reached: g.reached.slice() }; },
    power: () => session.state.flow?.power ?? null,
  },
  /** Render-loop sample since the last call: frames, mean and worst frame time (ms), frames over 50 ms. */
  fps: () => { const n = frameMs.length, mean = frameMs.reduce((a, b) => a + b, 0) / Math.max(1, n), worst = Math.max(0, ...frameMs), slow = frameMs.filter(d => d > 50).length; const r = { frames, sampled: n, meanMs: +mean.toFixed(2), worstMs: +worst.toFixed(1), over50ms: slow, fps: +(1000 / mean).toFixed(1) }; frames = 0; frameMs = []; return r; },
};
