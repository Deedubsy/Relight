/** Glue between the sim and the renderer. Engine-free. The prototype holds no game state of its own:
 *  everything is in `state`; this file only forwards commands and drains events. */
import {
  SimState, SimEvent, Command, SimConfig, DEFAULT_CONFIG, generateMap, createState, advance, takeEvents,
  Bot, createBot, botCommands, Policy, POLICIES, protoCalibrated, ensureFlow, advanceFlow, botHands, citySpec, CityPreset, CITY_PRESETS,
  HourBot, createHourBot, hourCommands, LoggedCommand, replay, replayVerdict, ReplayVerdict,
} from '@relight/sim';
import { Telemetry, createTelemetry, recordEvent, recordMinute, recordPips } from './telemetry';

/** `state` names a snapshot: a bare name resolves to /snapshots/<name>.json (shipped with the proto), a path or URL
 *  is fetched as is. A snapshot is a raw SimState, or a telemetry export (whose `finalState` is taken). */
/** `map`: a D6 city preset (default river) or `lattice` for the Phase 1–4 grid. (M1: the `walk` flag is gone — the
 *  engineer always walks, on foot in the world view, block-level under a bot.) */
/** M6: `autoplay=hour` runs §11's hour bot on the tile layer (hour.ts; a dev aid, never a player control); `rifle=1`
 *  gives it the rifle reflex. Every command of a session is logged (`Session.log`) so a played hour replays without
 *  the rifle for Gate B's "did it matter?" row (`replaySession`). */
export interface UrlParams { seed: number; economy: boolean; scatter: boolean; autoplay: Policy | 'hour' | null; player: string; state: string | null; view: 'map' | 'world'; flow: boolean; map: 'lattice' | CityPreset; rifle: boolean }

export function parseUrl(search: string): UrlParams {
  const q = new URLSearchParams(search);
  const seed = Number(q.get('seed') ?? '3');
  const auto = q.get('autoplay');
  return {
    seed: Number.isFinite(seed) ? seed : 3,
    economy: q.get('economy') !== '0',
    scatter: q.get('scatter') !== '0',
    autoplay: auto === 'hour' ? 'hour' : auto && (POLICIES as string[]).includes(auto) ? (auto as Policy) : null,
    player: q.get('player') ?? '',
    state: q.get('state') || null,
    view: q.get('view') === 'world' ? 'world' : 'map',
    flow: q.get('flow') !== '0',
    map: mapParam(q.get('map')),
    rifle: q.get('rifle') === '1',
  };
}

function mapParam(v: string | null): 'lattice' | CityPreset {
  if (v === 'lattice') return 'lattice';
  return v && (CITY_PRESETS as readonly string[]).includes(v) ? (v as CityPreset) : 'river';
}

export function shareUrl(p: UrlParams): string {
  const q = new URLSearchParams();
  if (p.state) q.set('state', p.state);
  else {
    q.set('seed', String(p.seed));
    if (!p.economy) q.set('economy', '0');
    if (!p.scatter) q.set('scatter', '0');
  }
  if (!p.flow) q.set('flow', '0');
  if (p.map !== 'river') q.set('map', p.map);
  if (p.autoplay) q.set('autoplay', p.autoplay);
  if (p.rifle) q.set('rifle', '1');
  const u = new URL(location.href);
  u.search = q.toString();
  return u.toString();
}

export function snapshotUrl(ref: string): string {
  return /^(https?:)?\//.test(ref) || ref.includes('/') || ref.endsWith('.json') ? ref : `/snapshots/${ref}.json`;
}

/** Fetch and validate a snapshot. Throws with a readable message; the caller decides whether to fall back. */
export async function loadSnapshot(ref: string): Promise<SimState> {
  const url = snapshotUrl(ref);
  const res = await fetch(url);
  if (!res.ok) throw new Error(`snapshot ${url}: HTTP ${res.status}`);
  const json = await res.json() as { finalState?: SimState } & Partial<SimState>;
  const st = (json.finalState ?? json) as SimState;
  if (st.version !== 1 || !Array.isArray(st.blocks) || !st.config || !Array.isArray(st.ring)) throw new Error(`snapshot ${url}: not a Relight state`);
  return st;
}

export interface Session {
  params: UrlParams;
  state: SimState;
  bot: Bot | null;
  /** M6: §11's hour bot (`?autoplay=hour`). */
  hour: HourBot | null;
  telemetry: Telemetry;
  pending: Command[];
  /** M6: every command applied to the state, with the tile tick it landed before (the replay's input). */
  log: LoggedCommand[];
  lastMinute: number;
  realElapsed: number;
  /** A = fresh start; B = a loaded snapshot (the test plan's scenarios). */
  scenario: 'A' | 'B';
  startT: number;   // sim tick the session started at (0 for A)
}

/** GAME-ASSUMPTION: the prototype's config. Production on with the doc's ring and unfed rule; the assembler
 *  schedule is replaced by one assembler at the start plus whatever the player builds. The numbers come from
 *  PROTO_CALIBRATED (types.ts, proto section), the same block the calibration harness runs. */
export function protoConfig(p: UrlParams): SimConfig {
  // M1 (D5): the engineer always walks. On foot in the world view (walk.ts: WASD with sprint and the dodge, the map's
  // walk-here click on a Held or street tile — D-B1-5), block-level under a harness bot (engineer.ts). A claim's edges
  // wait for a kit the engineer carries there; the pockets panel (Tab / I) draws kits from the Depot chest.
  return protoCalibrated({ ...DEFAULT_CONFIG, scatter: p.scatter, economy: p.economy, walk: true });
}

/** A fresh session (scenario A) or one continuing from `snapshot` (scenario B). A snapshot starts paused so the
 *  tester can read the map; its seed, economy and scatter come from the snapshot, not the URL. */
export function createSession(params: UrlParams, snapshot: SimState | null = null): Session {
  let state: SimState;
  if (snapshot) {
    state = JSON.parse(JSON.stringify(snapshot)) as SimState;
    state.events = []; state.acc = 0; state.speed = 0;
    state.survivors ??= [];
    params = { ...params, seed: state.seed, economy: state.config.economy, scatter: state.config.scatter, map: state.city ? (state.city.preset as CityPreset) : 'lattice' };
  } else {
    // D6: the map is the street-first city unless ?map=lattice; M1 put the tile layer on its faces, so flow is on there too
    const config = protoConfig(params);
    const spec = params.map === 'lattice' ? generateMap(params.seed, config) : citySpec(params.seed, params.map, config);
    state = createState(spec, config, params.seed);
  }
  // GAME-ASSUMPTION (M2): the tile flow layer is on for every session unless ?flow=0 (bot comparisons against the
  // block-only calibration runs). Turning it on retires the HQ's Mk1 stand-in: hour one's magazines come from the
  // line the player builds on the HQ lot, or from hand-crafting (D-P4-5). A block-only snapshot gets its flow here.
  // GAME-ASSUMPTION (M3): with the flow layer the §14 power model is on, supplied by the tile layer's Generators
  // (supply 'generators'), at the D1 half draw (100 kW exposed / 20 kW interior per substation). D-B3-4: a shortfall
  // slows every machine to supply ÷ demand; nothing is shed. One 300 kW Generator on 40 coal is the whole grid at
  // the start; the tester builds the rest. Without the flow layer nothing changes.
  if (params.flow) Object.assign(state.config, { power: true, supply: 'generators', draw: 'half' });
  if (params.flow) ensureFlow(state);
  const scenario = snapshot ? 'B' : 'A';
  const tel = createTelemetry(state, location.href, params.player, scenario, snapshot ? params.state : null);
  tel.speeds.push({ t: state.t, realTime: 0, speed: state.speed });
  // the proto's bots build assemblers (calibration step 4); the regression's bots do not
  const hour = params.autoplay === 'hour' && state.flow ? createHourBot(params.rifle) : null;
  return { params, state, bot: params.autoplay && params.autoplay !== 'hour' ? createBot(params.autoplay, null, true) : null, hour, telemetry: tel, pending: [], log: [],
           lastMinute: Math.floor(state.t / 60) * 60, realElapsed: 0, scenario, startT: state.t };
}

export function queue(s: Session, c: Command): void { s.pending.push(c); }

/** M6: write a command to the session's log at the current tile tick. `frame`/`runTicks` log what they apply; the
 *  scene's direct calls (E, R, the chest panel, the workbench) log the command they stand for after they run. */
export function record(s: Session, c: Command): void {
  if (c.type === 'setSpeed') return;
  s.log.push({ tick: s.state.flow?.tick ?? -1, c });
}

export function setSpeed(s: Session, mult: number): void {
  if (s.state.speed === mult) return;
  queue(s, { type: 'setSpeed', mult });
  s.telemetry.speeds.push({ t: s.state.t, realTime: s.realElapsed, speed: mult });
}

/** One render frame: apply queued commands, run the ticks the speed allows, return the events for the renderer. */
export function frame(s: Session, realDt: number): SimEvent[] {
  s.realElapsed += realDt;
  const cmds = s.pending;
  s.pending = [];
  const playerClaims = new Set(cmds.filter(c => c.type === 'claim').map(c => `${(c as { x: number }).x},${(c as { y: number }).y}`));
  let playerBuilds = cmds.filter(c => c.type === 'addAssembler').length;   // player commands are applied first, in order
  if (s.bot) botCommands(s.state, s.bot, cmds);   // player commands first, then the bot's (dev aid only)
  if (s.bot && s.state.flow) botHands(s.state);   // M3: the bot hand-feeds turrets and Generators from the Depot
  if (s.hour) hourCommands(s.state, s.hour, cmds);   // M6: §11's hour from the pockets, through the same commands a player sends
  for (const c of cmds) record(s, c);
  if (s.state.flow) advanceFlow(s.state, realDt, cmds); else advance(s.state, realDt, cmds);
  const events = takeEvents(s.state);
  for (const ev of events) {
    let src: 'player' | 'bot' = 'player';
    if (ev.type === 'claim' && s.bot && !playerClaims.has(`${ev.x},${ev.y}`)) src = 'bot';
    if (ev.type === 'assembler') { if (playerBuilds > 0) playerBuilds--; else if (s.bot) src = 'bot'; }
    recordEvent(s.telemetry, ev, src);
  }
  recordPips(s.telemetry, s.state);
  while (s.state.t >= s.lastMinute + 60) { s.lastMinute += 60; recordMinute(s.telemetry, s.state); }
  return events;
}

/** Fast-forward N ticks with the bot (if any) acting each tick. Used by the smoke test; not a player control. */
export function runTicks(s: Session, ticks: number): SimEvent[] {
  const all: SimEvent[] = [];
  const cmds: Command[] = [];
  for (let k = 0; k < ticks; k++) {
    cmds.length = 0;
    if (s.pending.length) { cmds.push(...s.pending); s.pending = []; }
    if (s.bot) botCommands(s.state, s.bot, cmds);
    if (s.hour) hourCommands(s.state, s.hour, cmds);
    for (const c of cmds) record(s, c);
    if (s.state.flow) advanceFlow(s.state, 1 / Math.max(1, s.state.speed), cmds); else advance(s.state, 1 / Math.max(1, s.state.speed), cmds);
    for (const ev of takeEvents(s.state)) { recordEvent(s.telemetry, ev, s.bot ? 'bot' : 'player'); all.push(ev); }
    recordPips(s.telemetry, s.state);
    while (s.state.t >= s.lastMinute + 60) { s.lastMinute += 60; recordMinute(s.telemetry, s.state); }
  }
  return all;
}

/** M6 / Gate B: re-run this session's command log from a fresh state built the same way, with the rifle's aim
 *  commands dropped (`rifleOff`, the default) or kept (a determinism check: the replayed state should match), and
 *  judge every hand-fired engagement. Only a fresh-start session (scenario A) with the flow layer replays; a
 *  snapshot session's base state is not rebuilt here. GAME-ASSUMPTION (M6): see hour.ts `replay`. */
export function replaySession(s: Session, opts: { rifleOff?: boolean } = {}): { verdict: ReplayVerdict; state: SimState } | { error: string } {
  if (s.scenario !== 'A') return { error: 'a snapshot session does not replay (scenario B)' };
  if (!s.state.flow) return { error: 'no flow layer (flow=0): nothing to replay' };
  const config = protoConfig(s.params);
  const spec = s.params.map === 'lattice' ? generateMap(s.params.seed, config) : citySpec(s.params.seed, s.params.map, config);
  const st = createState(spec, config, s.params.seed);
  Object.assign(st.config, { power: true, supply: 'generators', draw: 'half' });
  ensureFlow(st);
  replay(st, s.log, s.state.flow.tick, { dropAim: opts.rifleOff ?? true });
  return { verdict: replayVerdict(s.state, st), state: st };
}
