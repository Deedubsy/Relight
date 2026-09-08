import Phaser from 'phaser';
import { rulesetOf, knownCampaignThreat, blockName } from '@relight/sim';
import { SimEvent, flowSummary, canPlace, place, remove, canPickUp, rotate, queueCraft, setHandMine, Kind, Dir, handFeed, cellLights, blockLights, substationAt, poleGrid,
  hqLot, blockOfTile, ground, chestCount, chestTake, chestPut, ChestItem, invStacks, currentPath, describeGround, cityGeomOf, segBetween,
  projectOf, projectTitle, describeProject, RAIL_ROUTE_REWARD, LOCAL_DEPOT_REWARD,   // RI-05
  idxOf, LIGHT_SEQ_PER_S, REPAIR_COPPER, hourReport, blockLabel, stateHash, currentGoal, clockOf,
  heartAt, heartOf, CANDIDATES,   // RI-06
} from '@relight/sim';
import { parseUrl, createSession, setSpeed, runTicks, loadSnapshot, frame, Session, queue, record, dispatch, replaySession, saveSlot, slotUrl, hasSlot, makeSessionSave } from './session';
import { MapScene, MapView, SceneHooks, MAP_W, MAP_H } from './mapScene';
import { CityMapScene } from './cityMapScene';
import { WorldScene, Tool } from './worldScene';
import { View, debugView, hudInset } from './view';
import { bound, shortcut } from './controls';
import { FACTORY_TEXT } from './factoryStrings';
import { createPanel, exportExtra } from './panel';
import { exportJson, summarise } from './telemetry';

let session: Session;
try {
  const params = parseUrl(location.search);
  session = createSession(params, params.state ? await loadSnapshot(params.state) : null);
} catch (e) {
  // Fail closed: a rejected save/profile must never silently become a different campaign.
  const panel = document.getElementById('panel')!;
  const title = document.createElement('h2'), reason = document.createElement('p');
  title.textContent = 'This session could not be opened';
  reason.textContent = `${(e as Error).message}. Your saved games have not been changed.`;
  panel.replaceChildren(title, reason);
  for (const [label, rules] of [['Start Home Court preview', 'exploration-v2'], ['Start legacy game', 'legacy-v1']]) {
    const line = document.createElement('p'), link = document.createElement('a');
    link.textContent = label; link.href = `?rules=${rules}&view=world`; line.append(link); panel.append(line);
  }
  throw e;
}
const params = session.params;
/** Both views share one block. M1 (D5): on foot the block is the engineer's — map → world lands the camera on the
 *  engineer, world → map marks the block they stand in. Without the flow layer (?flow=0) the old hand-off stays: the
 *  block under the map cursor, the block under the world camera's centre. */
const view: View = { mode: params.view, focus: [session.state.start[0], session.state.start[1]], switchedAt: 0 };

const panel = createPanel(session, document.getElementById('panel')!, {
  onSelectEdge(id) { mapScene.selectEdge(id); },
  onToggleView() { toggleView(); },
});
if (session.scenario === 'B') panel.toast(`${params.state?.startsWith('local:') ? 'Save' : 'Snapshot'} loaded at ${clockOf(session.startT)} (state ${stateHash(session.state)}) — paused. Press P or a speed to begin.`, 'good');

/** RI-02: a block by its stable name (names.ts), with the coordinates only behind the ` toggle. */
const at = (x: number, y: number): string => blockLabel(session.state, idxOf(session.state, x, y), debugView.coords);
const at2 = (bi: number): string => blockLabel(session.state, bi, debugView.coords);   // RI-04: a block by index (a Stalker's site)

function describe(events: SimEvent[]): void {
  for (const ev of events) {
    switch (ev.type) {
      case 'held':
        // M5 (rule 8): the burn-off's end is the payoff — say so
        if (session.state.flow) panel.toast(`${at(ev.x, ev.y)} held — burn-off done, its streets are lit; the lot stays dark until you put Lamps on it`, 'good');
        if (ev.facility) panel.toast(`Reached the ${ev.facility}`, 'good');
        if (ev.survivor) panel.toast(`${ev.survivor}: "We're in."${ev.unlocks.length ? ` — ${ev.unlocks.join(', ')} are on the build menu (B; keys 0, [ and ])` : ''}`, 'good');
        break;
      case 'project': {   // RI-05 (plan §5.3): the project's stage, what it still needs, and its reward once restored
        const r = projectOf(session.state, ev.id);
        if (!r) break;
        const title = projectTitle(session.state, r);
        if (ev.stage === 'restored') panel.toast(`${title} restored — ${r.rewardId === RAIL_ROUTE_REWARD ? 'Track, Tram stop and Tram are on the build menu (B; keys L, H, V): a line of track on the street, a stop at each end, a tram on it; an inserter or E loads a stop\'s platform, the tram carries it to the other stop' : r.rewardId === LOCAL_DEPOT_REWARD ? 'its Supply chest hands out kits (E on it)' : describeProject(session.state, r)}`, 'good');
        else if (ev.stage === 'ready') panel.toast(describeProject(session.state, r), 'good');
        else if (ev.stage === 'interrupted') panel.toast(`${title} interrupted — ${describeProject(session.state, r)}`, 'bad');
        else if (ev.stage !== 'discovered') panel.toast(describeProject(session.state, r));
        break;
      }
      case 'fall': panel.toast(`${at(ev.x, ev.y)} lost — ${ev.reason}`, 'bad'); break;
      case 'sub-off': panel.toast(`${at(ev.x, ev.y)}'s substation stopped: ${session.state.config.unfedN} crawlers unfed. It falls if this goes on.`, 'bad'); break;
      case 'sub-on': panel.toast(`${at(ev.x, ev.y)}'s substation back on`, 'good'); break;
      case 'claim-rejected': panel.toast(`Claim on ${at(ev.x, ev.y)} rejected: ${ev.reason}`, 'bad'); break;
      // RI-03 (plan §4.1): the installation names the prerequisite an Activate lacked
      case 'activate-rejected': panel.toast(`${at(ev.x, ev.y)} not activated: ${ev.reason}`, 'bad'); break;
      case 'assembler': panel.toast(`Assembler built on ${at(ev.x, ev.y)} — ${ev.count} running`, 'good'); break;
      case 'assembler-rejected': panel.toast(ev.reason === 'no free interior slot' ? 'No assembler: no free machine slot (enclose a block first)' : 'No assembler: cannot afford it', 'bad'); break;
      case 'machine-lost': panel.toast(`Assembler on ${at(ev.x, ev.y)} lost with its block — ${ev.count} running`, 'bad'); break;
      case 'run-dry': panel.toast(`${at(ev.x, ev.y)} is dug out — no more ${ev.district === 'civ' ? 'stone' : ev.district === 'res' ? 'copper' : 'steel'} from it`); break;
      case 'reorder': panel.toast('Ring order changed'); break;
      // M3 (rule 8): the hopper, Generator and §14 brownout rules surface as toasts from the sim's events
      case 'hopper-empty': panel.toast(`Hopper EMPTY on ${at(ev.x, ev.y)} facing ${at(ev.nx, ev.ny)} — its pip is red until it is fed`, 'bad'); break;
      case 'gen-dry': panel.toast('The Generator burned its last coal — hand-feed it (click it with the hand) or belt coal in. No power until then.', 'bad'); break;
      case 'brownout': panel.toast(`Brownout: demand ${Math.round(ev.demandKw)} kW over ${Math.round(ev.supplyKw)} kW supply — every machine runs at ${Math.round(100 * Math.max(0, ev.supplyKw) / ev.demandKw)} % until a Generator is added or fed (§14). Nothing switches off.`, 'bad'); break;
      case 'power-ok': panel.toast('Power back: supply covers demand, every machine at full speed', 'good'); break;
      case 'claim': {
        if (!session.state.flow) break;
        const kits = session.state.engineer.inv.kit ?? 0;
        const bi = idxOf(session.state, ev.x, ev.y), H = heartAt(session.state, bi);
        // RI-06: the Heart's block has no burn-off timer — its commissioning is the encounter (§28.8)
        const burn = Math.round(session.state.blocks[bi].contestUntil - ev.t), burnTxt = H && H.attempt >= 0 ? `${H.cand.productiveS} s of productive commissioning (both feeders powered)` : `${burn} s`;
        // M5 (§5 step 3–4, §6): the streetlights come on now, in sequence; the rot burns off over 20 + 60·d s.
        // RI-03: the game's claim is the Activate at the substation (materials spent once, there); the map path's
        // wording stays for the legacy bot / a replay of an old log
        panel.toast(`${ev.via === 'activate' ? `${at(ev.x, ev.y)} activated — its claim materials spent at the substation` : `Poles strung to ${at(ev.x, ev.y)}`} — its streetlights come on now, ${LIGHT_SEQ_PER_S} a second from the substation out; the rot burns off in ${burnTxt}${kits ? '' : '. Its edges wait for a kit: take kits from the Depot chest (I) and walk there'}`);
        break;
      }
      case 'kitted': panel.toast(`Kits laid on ${at(ev.x, ev.y)} — ${ev.edges} edge${ev.edges === 1 ? '' : 's'} armed`, 'good'); break;
      case 'engineer-down': panel.toast('The engineer is down — back at the HQ workbench in 10 s', 'bad'); break;
      // RI-04 (rule 8): the Stalker candidate's rules surface as toasts — on you (a wind-up before its first strike, a
      // dodge breaks one), dead, withdrawn when its site is restored; guard / investigate / return stay on the world view
      case 'stalker':
        if (ev.what === 'pursue') panel.toast(`A Stalker from ${at2(ev.site)} is on you — it winds up 0.8 s before its first strike; a dodge (Space) breaks a strike; it gives up 16 tiles from its home`, 'bad');
        else if (ev.what === 'dead') panel.toast(`Stalker from ${at2(ev.site)} down`, 'good');
        else if (ev.what === 'retired') panel.toast(`The Stalker from ${at2(ev.site)} withdrew — its site is restored`, 'good');
        else if (ev.what === 'spawn' && ev.t > 0) panel.toast(`A Stalker guards ${at2(ev.site)} again`);
        break;
      // RI-06 (rule 8, plan §9.4): the Junction Heart's rules surface as toasts — the Start, each reinforcement packet's
      // approach, a feeder knocked out / repaired, the interruption, the abort, the destruction
      case 'heart': {
        const hc = heartOf(session.state)?.cand ?? CANDIDATES.heart, cab = ev.cabinet !== undefined ? `feeder cabinet ${ev.cabinet + 1}` : 'a feeder cabinet';
        if (ev.what === 'start') panel.toast(`Commissioning the Junction Heart at ${at(ev.x, ev.y)} (attempt ${ev.attempt}): ${hc.productiveS} s with both feeder cabinets powered; ${hc.stallS} s without and it is interrupted. Reinforcements at ${hc.thresholds.join(' / ')} %. X aborts.`);
        else if (ev.what === 'packet') panel.toast(`${ev.threshold} % — the Heart calls a packet toward ${cab}: ${ev.n ?? ''} crawler${ev.n === 1 ? '' : 's'} in ${hc.approachS} s from the far kerb`, 'bad');
        else if (ev.what === 'born') panel.toast(`Reinforcements on the kerb toward ${cab}`, 'bad');
        else if (ev.what === 'knockout') panel.toast(`${cab} knocked out — commissioning pauses until it is repaired (E on it, ${REPAIR_COPPER} Cu) — ${hc.stallS} s to interruption`, 'bad');
        else if (ev.what === 'repair') panel.toast(`${cab} repaired`, 'good');
        else if (ev.what === 'interrupted') panel.toast(`Commissioning interrupted (${ev.why ?? 'stalled'}): the block is Dark again; the installation keeps its materials and the cabinets their deliveries — repair, then Start again at the substation`, 'bad');
        else if (ev.what === 'aborted') panel.toast('Commissioning aborted — the installation keeps its materials; Start again at the substation when ready');
        else if (ev.what === 'destroyed') panel.toast(`The Junction Heart at ${at(ev.x, ev.y)} is destroyed — the switching installation is operational; the rail kit unlocks with the yard`, 'good');
        break;
      }
      // M4 (rule 8): the tile threat's rules surface as toasts — retaliation only (D5), lamps eaten, the 40-arrival count
      case 'engineer-up': panel.toast('Back on your feet at the HQ workbench — pockets intact, no other penalty', 'good'); break;
      case 'retaliate': panel.toast(ev.cause === 'shot' ? 'A crawler turned on you: you shot it. It bites at arm\'s reach (5 HP/s) — finish it (3 rounds) or step back' : 'A crawler turned on you: you are standing in its path. Step aside, or shoot it', 'bad'); break;
      case 'lamp-eaten': panel.toast(`A crawler put out a lamp on ${at(ev.x, ev.y)}${debugView.coords ? ` (tile (${ev.tx},${ev.ty}))` : ''} — the block is darker; the next ones head for its turrets, then the substation. E on the lamp repairs it (${REPAIR_COPPER} Cu)`, 'bad'); break;
      case 'arrival': panel.toast(`${ev.shade ? 'A shade' : 'A crawler'} reached the substation on ${at(ev.x, ev.y)} unshot — ${ev.n} of ${ev.of}${ev.shade ? ' (the substation is off 30 s)' : ''}`, 'bad'); break;
      case 'bloom': {
        // GAME-ASSUMPTION: only the blooms on or next to the engineer's block toast; the rest are the map view's pulses
        const st = session.state, e = st.engineer;
        if (!st.flow || st.lattice) break;
        const eb = blockOfTile(st, Math.floor(e.x), Math.floor(e.y));
        if (eb < 0 || Math.abs(st.blocks[eb].x - ev.x) > 1 || Math.abs(st.blocks[eb].y - ev.y) > 1) break;
        panel.toast(`Bloom on ${at(ev.x, ev.y)} beside you: ${Math.round(ev.cr)} crawler${Math.round(ev.cr) === 1 ? '' : 's'}${ev.sh >= 0.5 ? ` and ${Math.round(ev.sh)} shade${Math.round(ev.sh) === 1 ? '' : 's'}` : ''} at the ridge, coming for the lit lamps`, 'bad');
        break;
      }
    }
  }
}

const game = new Phaser.Game({
  type: Phaser.AUTO,
  parent: 'map',
  width: MAP_W, height: MAP_H,
  // layout pass: the canvas fills the viewport beside the panel (the map view keeps its 648 px square at the top-left)
  scale: { mode: Phaser.Scale.RESIZE, width: MAP_W, height: MAP_H },
  backgroundColor: '#0b0e1a',
  render: { antialias: true, pixelArt: false },
  disableContextMenu: true,   // right click removes a machine in the world view
  scene: [],
});
const hooks: SceneHooks = {
  onHover: (info, px, py) => panel.tooltip(info, px, py),
  onPipSelect: e => panel.setSelectedEdge(e),
  onStationSelect: (x,y) => panel.openStationRoute(x,y),
  onToast: (msg, kind) => panel.toast(msg, kind),
};
// D6: the city map draws polygons; the lattice MapScene stays for ?map=lattice and the lattice-era snapshots
const mapScene: MapView = session.state.city ? new CityMapScene(session, hooks) : new MapScene(session, hooks);
const worldScene = new WorldScene(session, view, { onHoverText: (text, px, py) => panel.tooltipText(text, px, py), onToast: (msg, kind) => panel.toast(msg, kind), onChest: () => panel.togglePockets(), onChestAt: (x, y) => panel.openPocketsAt(x, y), onRoutingAt: (x,y) => panel.openRoutingAt(x,y), onInspectAt: (x,y) => panel.openInspectionAt(x,y), onTruck: () => panel.openTruck() });
worldScene.handLamp = new URLSearchParams(location.search).get('handlamp') === '1';   // D-B5-1 preview only
panel.onPick = kind => { worldScene.setTool(kind); panel.closeAll(); panel.toast(FACTORY_TEXT.picked(kind)); };
game.scene.add('map', mapScene, view.mode === 'map');
game.scene.add('world', worldScene, view.mode === 'world');
if (view.mode === 'world') worldScene.centreOn(view.focus[0], view.focus[1]);
panel.setView(view.mode);

// The sim runs once per game step whichever view is up; the map view consumes the events for its pulses.
game.events.on(Phaser.Core.Events.STEP, (time: number, delta: number) => {
  const events = frame(session, Math.min(0.1, delta / 1000));
  if (events.length) { mapScene.consume(events, time); describe(events); }
  panel.update(performance.now());
  updateThreatControls();
  // RI-02: the world view's top HUD corners sit under the goal overlay (measured here, not per frame — the panel throttles)
  const goalEl = document.getElementById('goal');
  hudInset.top = goalEl && !goalEl.hidden ? goalEl.offsetHeight + 4 : 0;
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
    worldScene.returnToEngineer();
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

function viewKnownThreat():void {
  if(!knownCampaignThreat(session.state))return;
  if(view.mode==='map')toggleView();
  worldScene.viewThreat();
  document.querySelector('canvas')?.scrollIntoView({block:'nearest'});
}
const threatControls=document.createElement('div');threatControls.id='threat-controls';
const threatDirection=document.createElement('span'),threatButton=document.createElement('button'),returnButton=document.createElement('button');
threatDirection.id='threat-direction';
threatButton.textContent=`View threat (${shortcut('threat')})`;threatButton.onclick=viewKnownThreat;
returnButton.textContent=`Return to engineer (${shortcut('engineer')})`;returnButton.onclick=()=>worldScene.returnToEngineer();
threatControls.append(threatDirection,threatButton,returnButton);document.getElementById('goal')!.append(threatControls);
function updateThreatControls():void {
  const target=knownCampaignThreat(session.state);
  threatControls.hidden=!session.state.campaign;
  threatButton.hidden=!target;returnButton.hidden=!worldScene.viewingThreat;
  let text='';
  if(target){
    const [x,y]=view.mode==='world'?worldScene.screenOf(target.x,target.y):[0,0],canvas=document.querySelector('canvas')!,w=canvas.clientWidth,h=canvas.clientHeight;
    const off=x<0||y<0||x>w||y>h;
    const bearing=(Math.round(Math.atan2(y-h/2,x-w/2)/(Math.PI/4))+8)%8;
    const arrows=['→ east','↘ southeast','↓ south','↙ southwest','← west','↖ northwest','↑ north','↗ northeast'];
    text=`${target.phase} · ${blockName(session.state,target.block)} · ${view.mode==='map'?'view in world':off?`off-screen ${arrows[bearing]}`:'in view'}`;
  }else if(session.state.campaign?.defence?.major)text='Target unknown — radio offline';
  if(threatDirection.textContent!==text)threatDirection.textContent=text;
}

/** D-B1-5 keys: P pause, - / = speed (1×, 4×, 16×), M map ↔ world, Tab or I the pockets, B the build menu, Esc closes
 *  anything and clears the hand, ` the debug panel; Space is the dodge and the digits the hotbar, both the world
 *  scene's (Shift sprints there too). */
const SPEEDS = [1, 4, 16];
window.addEventListener('keydown', ev => {
  if ((ev.target as HTMLElement)?.closest('input, textarea, select, [contenteditable=true]')) return;
  const k = ev.key;
  if ((ev.ctrlKey || ev.metaKey) && (bound('undo', k) || bound('redo', k))) {
    ev.preventDefault(); if (ev.repeat) return;
    const type = bound('redo', k) || ev.shiftKey ? 'redoBuild' : 'undoBuild';
    const r = dispatch(session, { type }); panel.toast(r.reason, r.ok ? 'good' : 'bad'); return;
  }
  // RI-02: Ctrl+S saves to slot 1 in this browser, Ctrl+O reloads the page from it (the panel's Save / Load buttons)
  if ((ev.ctrlKey || ev.metaKey) && bound('save', k)) {
    ev.preventDefault();
    try { const save = saveSlot(session, '1'); panel.toast(`Saved to slot 1 at ${clockOf(save.t)} (state ${save.hash}) — Ctrl+O or Load reloads it`, 'good'); }
    catch (e) { panel.toast(`Could not save: ${(e as Error).message}`, 'bad'); }
    return;
  }
  if ((ev.ctrlKey || ev.metaKey) && bound('load', k)) {
    ev.preventDefault();
    if (!hasSlot('1', rulesetOf(session.state))) { panel.toast('This campaign has no save yet — Ctrl+S saves it', 'bad'); return; }
    if (window.confirm('Reload from slot 1? Unsaved progress is lost.')) location.href = slotUrl(session, '1');
    return;
  }
  if (ev.ctrlKey || ev.metaKey || ev.altKey) return;
  if (bound('pause', k)) setSpeed(session, session.state.speed === 0 ? 1 : 0);
  else if (bound('slower', k) || bound('faster', k)) {
    const cur = SPEEDS.indexOf(session.state.speed), next = bound('slower', k) ? Math.max(0, cur - 1) : Math.min(SPEEDS.length - 1, cur + 1);
    setSpeed(session, SPEEDS[cur < 0 ? 0 : next]);
  }
  else if (bound('threat', k)) {ev.preventDefault();viewKnownThreat();}
  else if (bound('engineer', k)) worldScene.returnToEngineer();
  else if (bound('map', k)) toggleView();   // D5: M = map view
  else if (bound('pockets', k)) { ev.preventDefault(); panel.togglePockets(); }   // M1: the pockets and the Depot chest
  else if (bound('build', k)) panel.toggleBuild();
  else if (bound('debug', k)) panel.toggleDebug();
  else if (bound('cancel', k)) { panel.closeAll(); if (view.mode === 'world') worldScene.key(k); }
  else if (bound('dodge', k)) { if (view.mode === 'world') ev.preventDefault(); }   // the dodge (worldScene reads the key itself)
  else if (view.mode === 'world' && worldScene.key(k)) ev.preventDefault();
});

// dev/test hooks (not player controls)
let frames = 0, frameMs: number[] = [];
game.events.on(Phaser.Core.Events.POST_STEP, (_t: number, delta: number) => { frames++; if (frameMs.length < 100000) frameMs.push(delta); });
(window as unknown as { __relight: unknown }).__relight = {
  session, view,
  run: (ticks: number) => { runTicks(session, ticks); panel.update(performance.now()); return summarise(session.telemetry, session.state); },
  summary: () => summarise(session.telemetry, session.state),
  exportJson: () => exportJson(session.telemetry, session.state, exportExtra(session)),
  /** M6: the hour bot's log and findings (`?autoplay=hour`), the session's command log, and the Gate B replay
   *  (rifle off unless `rifleOff: false`): every hand-fired fight judged `held anyway` / `saved it` / `fell anyway`. */
  hour: () => session.hour ? hourReport(session.state, session.hour) : null,
  commandLog: () => session.log,
  replay: (opts: { rifleOff?: boolean } = {}) => { const r = replaySession(session, opts); return 'error' in r ? r : r.verdict; },
  /** RI-02: replay the session's whole log (aim kept) from a fresh state and compare hashes with the played state. */
  replayHash: () => { const r = replaySession(session, { rifleOff: false }); return 'error' in r ? r : { same: stateHash(r.state) === stateHash(session.state), replayed: stateHash(r.state), played: stateHash(session.state), tick: r.state.flow?.tick ?? -1 }; },
  stateJson: () => JSON.stringify(session.state),
  renderTiming: () => ({...worldScene.renderTiming}),
  /** RI-02: the state hash (save.ts), the current goal line, the save file, and the browser slot (the Ctrl+S / Ctrl+O path). */
  stateHash: () => stateHash(session.state),
  goal: () => currentGoal(session.state),
  saveFile: () => makeSessionSave(session),
  save: (slot = '1') => saveSlot(session, slot).hash,
  loadUrl: (slot = '1') => slotUrl(session, slot),
  debugCoords: (on?: boolean) => { if (on !== undefined) debugView.coords = on; return debugView.coords; },
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
  /** M5: D-B5-1's hand lamp preview (a 2-tile disc on the sprite in the light map; nothing else changes). */
  handLamp: (on: boolean) => { worldScene.handLamp = on; },
  engineer: () => {
    const e = session.state.engineer, p = currentPath(session.state);
    return { x: e.x, y: e.y, block: e.block, dest: e.dest, target: e.target, hp: e.hp, down: e.down, inv: { ...e.inv }, stacks: invStacks(e.inv), walked: e.walked, pathLeft: p ? p.path.length - p.at : 0,
             stamina: e.stamina, dash: e.dash, fired: e.fired, kills: e.kills, hurt: e.hurt, shootS: e.shootS, danger: e.danger, dangerShot: e.dangerShot };
  },
  ground: () => { const G = ground(session.state); return { tw: G.tw, th: G.th, blocks: G.blocks.length }; },
  city: () => { const G = ground(session.state), u = G.urban; return u ? { profile: u.profile, places: u.places, structures: u.structures, rail: u.railReserve } : null; },
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
    take: (item: ChestItem, n: number) => { const r = chestTake(session.state, item, n); record(session, { type: 'chestTake', item, n }); return r; },
    put: (item: ChestItem, n: number) => { const r = chestPut(session.state, item, n); record(session, { type: 'chestPut', item, n }); return r; },
  },
  togglePockets: () => panel.togglePockets(),
  world: { get zoom() { return worldScene.zoom; }, drawMs: () => { const r = { ema: +worldScene.drawMs.toFixed(2), worst: +worldScene.drawWorstMs.toFixed(1) }; worldScene.drawWorstMs = 0; return r; }, setZoom: (z: number) => worldScene.setZoom(z), centreOn: (x: number, y: number) => worldScene.centreOn(x, y), focus: () => worldScene.focusBlock(), get drawn() { return worldScene.drawn; },
           get tool() { return worldScene.tool; }, key: (k: string) => worldScene.key(k), screenOf: (x: number, y: number) => worldScene.screenOf(x, y) },
  /** M2 flow layer: place/remove/rotate by city tile, `hq(lx, ly)` = city tile of a lot tile on the HQ lot. */
  flow: {
    summary: () => flowSummary(session.state),
    canPlace: (kind: Kind, tx: number, ty: number) => canPlace(session.state, kind, tx, ty),
    place: (kind: Kind, tx: number, ty: number, dir: Dir = 0) => { const m = place(session.state, kind, tx, ty, dir); record(session, { type: 'place', item: kind, x: tx, y: ty, dir }); return m; },
    remove: (tx: number, ty: number) => { const m = remove(session.state, tx, ty); record(session, { type: 'pickUp', x: tx, y: ty }); return m; },
    canPickUp: (tx: number, ty: number) => canPickUp(session.state, tx, ty),   // M2: the pick-up check the right-click makes
    rotate: (tx: number, ty: number) => { const m = rotate(session.state, tx, ty); record(session, { type: 'rotate', x: tx, y: ty }); return m; },
    craft: (n = 1) => { const r = queueCraft(session.state, n); record(session, { type: 'craft', item: 'magazine', count: n }); return r; },
    mine: (at: [number, number] | null) => { setHandMine(session.state, at); record(session, { type: 'mineAt', x: at ? at[0] : -1, y: at ? at[1] : -1 }); },
    hq: (lx: number, ly: number): [number, number] => hqLot(session.state, lx, ly),
    // M3: hand-feed a turret or Generator, and the light/substation/pole/power queries the world view draws from
    feed: (tx: number, ty: number) => { const r = handFeed(session.state, tx, ty); record(session, { type: 'feed', x: tx, y: ty }); return r; },
    lights: (bx: number, by: number) => cellLights(session.state, bx, by),
    blockLights: (i: number) => blockLights(session.state, i),
    substation: (bx: number, by: number) => substationAt(session.state, bx, by),
    poles: () => { const g = poleGrid(session.state); return { connected: g.connected.size, links: g.links.length, reached: g.reached.slice() }; },
    power: () => session.state.flow?.power ?? null,
  },
  /** Render-loop sample since the last call: frames, mean and worst frame time (ms), frames over 50 ms. */
  fps: () => { const n = frameMs.length, mean = frameMs.reduce((a, b) => a + b, 0) / Math.max(1, n), worst = Math.max(0, ...frameMs), slow = frameMs.filter(d => d > 50).length; const r = { frames, sampled: n, meanMs: +mean.toFixed(2), worstMs: +worst.toFixed(1), over50ms: slow, fps: +(1000 / mean).toFixed(1) }; frames = 0; frameMs = []; return r; },
};
