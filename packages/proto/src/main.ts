import Phaser from 'phaser';
import { SimEvent } from '@relight/sim';
import { parseUrl, createSession, setSpeed, runTicks } from './session';
import { MapScene, SceneHooks, MAP_W, MAP_H } from './mapScene';
import { createPanel } from './panel';
import { exportJson, summarise } from './telemetry';

const params = parseUrl(location.search);
const session = createSession(params);
let scene: MapScene | null = null;

const panel = createPanel(session, document.getElementById('panel')!, {
  onSelectEdge(id) { scene?.selectEdge(id); },
});

function describe(events: SimEvent[]): void {
  for (const ev of events) {
    switch (ev.type) {
      case 'held': if (ev.facility) panel.toast(`Reached the ${ev.facility}`, 'good'); break;
      case 'fall': panel.toast(`Block (${ev.x},${ev.y}) lost — ${ev.reason}`, 'bad'); break;
      case 'sub-off': panel.toast(`Substation (${ev.x},${ev.y}) stopped: ${session.state.config.unfedN} crawlers unfed. It falls if this goes on.`, 'bad'); break;
      case 'sub-on': panel.toast(`Substation (${ev.x},${ev.y}) back on`, 'good'); break;
      case 'claim-rejected': panel.toast(`Claim (${ev.x},${ev.y}) rejected: ${ev.reason}`, 'bad'); break;
      case 'assembler': panel.toast(`Assembler built on (${ev.x},${ev.y}) — ${ev.count} running`, 'good'); break;
      case 'assembler-rejected': panel.toast(ev.reason === 'no free interior slot' ? 'No assembler: no free machine slot (enclose a block first)' : 'No assembler: cannot afford it', 'bad'); break;
      case 'machine-lost': panel.toast(`Assembler on (${ev.x},${ev.y}) lost with its block — ${ev.count} running`, 'bad'); break;
      case 'run-dry': panel.toast(`Block (${ev.x},${ev.y}) is dug out — no more ${ev.district === 'civ' ? 'stone' : ev.district === 'res' ? 'copper' : 'steel'} from it`); break;
      case 'reorder': panel.toast('Ring order changed'); break;
    }
  }
}

const game = new Phaser.Game({
  type: Phaser.AUTO,
  parent: 'map',
  width: MAP_W, height: MAP_H,
  backgroundColor: '#0b0e1a',
  render: { antialias: true, pixelArt: false },
  scene: [],
});
scene = new MapScene();
const hooks: SceneHooks = {
  onHover: (info, px, py) => panel.tooltip(info, px, py),
  onPipSelect: e => panel.setSelectedEdge(e),
  onEvents: events => { describe(events); panel.update(performance.now()); },
};
game.scene.add('map', scene, true, { session, hooks });

window.addEventListener('keydown', ev => {
  if ((ev.target as HTMLElement)?.tagName === 'INPUT') return;
  if (ev.key === ' ') { ev.preventDefault(); setSpeed(session, session.state.speed === 0 ? 1 : 0); }
  else if (ev.key === '1') setSpeed(session, 1);
  else if (ev.key === '2') setSpeed(session, 4);
  else if (ev.key === '3') setSpeed(session, 16);
});

// dev/test hooks (not player controls)
(window as unknown as { __relight: unknown }).__relight = {
  session,
  run: (ticks: number) => { runTicks(session, ticks); panel.update(performance.now()); return summarise(session.telemetry, session.state); },
  summary: () => summarise(session.telemetry, session.state),
  exportJson: () => exportJson(session.telemetry, session.state),
  configHash: session.telemetry.meta.configHash,
};
