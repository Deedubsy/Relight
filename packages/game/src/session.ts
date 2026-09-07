/** Glue between the sim and the renderer. Engine-free. The prototype holds no game state of its own:
 *  everything is in `state`; this file only forwards commands and drains events. */
import {
  SimState, SimEvent, Command, SimConfig, DEFAULT_CONFIG, generateMap, createState, advance, takeEvents,
  Bot, createBot, botCommands, Policy, POLICIES, protoCalibrated, ensureFlow, advanceFlow, botHands, citySpec, CityPreset, CITY_PRESETS,
  HourBot, createHourBot, hourCommands, LoggedCommand, replay, replayVerdict, ReplayVerdict,
  SaveFile, makeSave, loadState, isSaveFile, stateHash, enableStalkers, enableHeart,
} from '@relight/sim';
import { Telemetry, createTelemetry, recordEvent, recordMinute, recordPips } from './telemetry';
import { applyCommands, actionResult, type ActionResult } from '@relight/sim';
import { type Ruleset, isRuleset, rulesetOf, isCampaign, CAMPAIGN_RULESET, LEGACY_RULESET, createCampaign } from '@relight/sim';

/** `state` names a snapshot: a bare name resolves to /snapshots/<name>.json (shipped with the proto), a path or URL
 *  is fetched as is. A snapshot is a raw SimState, or a telemetry export (whose `finalState` is taken). */
/** `map`: a D6 city preset (default river) or `lattice` for the Phase 1–4 grid. (M1: the `walk` flag is gone — the
 *  engineer always walks, on foot in the world view, block-level under a bot.) */
/** M6: `autoplay=hour` runs §11's hour bot on the tile layer (hour.ts; a dev aid, never a player control); `rifle=1`
 *  gives it the rifle reflex. Every command of a session is logged (`Session.log`) so a played hour replays without
 *  the rifle for Gate B's "did it matter?" row (`replaySession`). */
export interface UrlParams { seed: number; economy: boolean; scatter: boolean; autoplay: Policy | 'hour' | null; player: string; state: string | null; view: 'map' | 'world'; flow: boolean; map: 'lattice' | CityPreset; rifle: boolean; stalker: boolean; heart: boolean; cityProfile?: 'riverside-v1' | 'legacy'; ruleset?: Ruleset }

export function parseUrl(search: string): UrlParams {
  const q = new URLSearchParams(search);
  const seed = Number(q.get('seed') ?? '3');
  const auto = q.get('autoplay');
  const ruleset = q.get('rules');
  if (ruleset !== null && !isRuleset(ruleset)) throw new Error('Unknown campaign rules');
  return {
    seed: Number.isFinite(seed) ? seed : 3,
    ...(ruleset ? { ruleset } : {}),
    economy: q.get('economy') !== '0',
    scatter: q.get('scatter') !== '0',
    autoplay: auto === 'hour' ? 'hour' : auto && (POLICIES as string[]).includes(auto) ? (auto as Policy) : null,
    player: q.get('player') ?? '',
    state: q.get('state') || null,
    view: q.get('view') === 'map' ? 'map' : 'world',
    cityProfile: q.get('city') === 'legacy' ? 'legacy' : 'riverside-v1',
    flow: q.get('flow') !== '0',
    map: mapParam(q.get('map')),
    rifle: q.get('rifle') === '1',
    stalker: q.get('stalker') === '1',   // RI-04 (D-RI-5): the Stalker candidate — a switch, never the benchmark's default
    heart: q.get('heart') === '1',   // RI-06 (D-RI-5): the Junction Heart candidate — the rail yard's claim as the first boss
  };
}

function mapParam(v: string | null): 'lattice' | CityPreset {
  if (v === 'lattice') return 'lattice';
  return v && (CITY_PRESETS as readonly string[]).includes(v) ? (v as CityPreset) : 'river';
}

export function shareUrl(p: UrlParams): string {
  const q = new URLSearchParams();
  if (p.ruleset) q.set('rules', p.ruleset);
  if (p.state) q.set('state', p.state);
  else {
    q.set('seed', String(p.seed));
    if (!p.economy) q.set('economy', '0');
    if (!p.scatter) q.set('scatter', '0');
  }
  if (!p.flow) q.set('flow', '0');
  if (p.map !== 'river') q.set('map', p.map);
  if (p.cityProfile === 'legacy') q.set('city', 'legacy');
  if (p.autoplay) q.set('autoplay', p.autoplay);
  if (p.rifle) q.set('rifle', '1');
  if (p.stalker) q.set('stalker', '1');
  if (p.heart) q.set('heart', '1');
  const u = new URL(location.href);
  u.search = q.toString();
  return u.toString();
}

export function snapshotUrl(ref: string): string {
  return /^(https?:)?\//.test(ref) || ref.includes('/') || ref.endsWith('.json') ? ref : `/snapshots/${ref}.json`;
}

/** RI-02 (§11.2 save / load baseline): the browser save slots. `?state=local:<slot>` loads one; Ctrl+S / the panel's
 *  Save button writes slot 1. A slot holds a save file (save.ts `SaveFile`: the state, the session's command log and
 *  the URL parameters), so a loaded session keeps replaying from tick 0 when the log was complete. Implementation
 *  default (RI-02): one named slot in `localStorage` under `relight.save.<slot>`; the download and URL paths stay. */
export const LOCAL_PREFIX = 'local:';
export const saveKey = (slot: string): string => `relight.save.${slot}`;
export function profileSlot(slot: string, ruleset: Ruleset = LEGACY_RULESET): string { return ruleset === CAMPAIGN_RULESET ? `${CAMPAIGN_RULESET}:${slot}` : slot; }
export function hasSlot(slot: string, ruleset: Ruleset = LEGACY_RULESET): boolean {
  try { return localStorage.getItem(saveKey(profileSlot(slot, ruleset))) !== null; } catch { return false; }
}

/** What a `state` reference resolves to: the validated state (a deep copy, transients reset) and, from a save file,
 *  the command log it carried. */
export interface Loaded { state: SimState; log: LoggedCommand[]; logComplete: boolean; ref: string; saved?: SaveFile }

/** Fetch and validate a snapshot: a browser save slot (`local:<slot>`), a shipped snapshot name, or a URL to a save
 *  file, a raw state or a telemetry export. Throws with a readable message; the caller decides whether to fall back. */
export async function loadSnapshot(ref: string): Promise<Loaded> {
  let json: unknown;
  if (ref.startsWith(LOCAL_PREFIX)) {
    const slot = ref.slice(LOCAL_PREFIX.length);
    let raw: string | null = null;
    try { raw = localStorage.getItem(saveKey(slot)); } catch { raw = null; }
    if (raw === null) throw new Error(`save slot ${slot} is empty in this browser`);
    json = JSON.parse(raw);
  } else {
    const url = snapshotUrl(ref);
    const res = await fetch(url);
    if (!res.ok) throw new Error(`snapshot ${url}: HTTP ${res.status}`);
    json = await res.json();
  }
  let state: SimState;
  try { state = loadState(json); } catch (e) { throw new Error(`${ref}: ${(e as Error).message}`); }
  const saved = isSaveFile(json) ? json : undefined;
  const original = (saved?.state ?? (json as { finalState?: SimState })?.finalState ?? json) as SimState;
  const upgradedHome = original.ruleset === CAMPAIGN_RULESET && (original.campaign?.version ?? 0) < 5;
  const upgradedTransport=original.ruleset===CAMPAIGN_RULESET&&!original.campaign?.truck&&(original.campaign?.expansion?.station.restoredAt??-1)>=0;
  // The old log predates station geometry and local circuits: resume the save, but do not claim a new-factory replay of that history.
  return { state, log: saved?.log ? saved.log.map(l => ({ tick: l.tick, c: JSON.parse(JSON.stringify(l.c)) })) : [], logComplete: !!saved?.logComplete && !upgradedHome && !upgradedTransport, ref, saved };
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
  /** RI-02: `log` runs from tick 0 (a fresh session, or one loaded from a save that carried its whole log), so the
   *  session still replays from a fresh state; false once it continues a bare snapshot. */
  logComplete: boolean;
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
export function createSession(params: UrlParams, snapshot: Loaded | null = null): Session {
  let state: SimState;
  if (snapshot) {
    state = loadState(snapshot.state);   // RI-02: the validated deep copy (save.ts), transients reset, paused
    if (params.ruleset && params.ruleset !== rulesetOf(state)) throw new Error('Saved campaign rules do not match the selected campaign');
    params = { ...params, ruleset: rulesetOf(state), seed: state.seed, economy: state.config.economy, scatter: state.config.scatter, map: state.city ? (state.city.preset as CityPreset) : 'lattice', cityProfile: state.city?.profile ?? 'legacy' };
  } else if (params.ruleset === CAMPAIGN_RULESET) {
    state = createCampaign(params.seed);
  } else {
    // D6: the map is the street-first city unless ?map=lattice; M1 put the tile layer on its faces, so flow is on there too
    const config = protoConfig(params);
    const spec = params.map === 'lattice' ? generateMap(params.seed, config) : citySpec(params.seed, params.map, config);
    if (spec.city && params.cityProfile === 'riverside-v1') spec.city.profile = 'riverside-v1';
    state = createState(spec, config, params.seed);
  }
  if (isCampaign(state)) {
    if (params.autoplay || params.heart || params.stalker || !params.flow) throw new Error('Legacy bots, encounters and block-only mode cannot run in the exploration campaign');
    params = { ...params, map: 'river', ruleset: CAMPAIGN_RULESET, cityProfile: 'riverside-v1', flow: true, economy: true, scatter: true };
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
  // RI-04 (D-RI-5): `?stalker=1` fields the Stalker candidate (candidates.ts) on this session's threat layer — a
  // candidate configuration outside SimConfig, so the config hash and the benchmark are untouched
  if (params.flow && params.stalker) enableStalkers(state);
  // RI-06 (D-RI-5): `?heart=1` puts the Junction Heart (candidates.ts) on the rail yard — the same rule: a switch beside the benchmark
  if (params.flow && params.heart) enableHeart(state);
  const scenario = snapshot ? 'B' : 'A';
  const tel = createTelemetry(state, location.href, params.player, scenario, snapshot ? params.state : null);
  tel.speeds.push({ t: state.t, realTime: 0, speed: state.speed });
  // the proto's bots build assemblers (calibration step 4); the regression's bots do not
  const hour = params.autoplay === 'hour' && state.flow ? createHourBot(params.rifle) : null;
  // RI-02: a save that carried its whole log continues it, so the loaded session replays from tick 0 like a fresh one
  const logComplete = !snapshot || snapshot.logComplete;
  return { params, state, bot: params.autoplay && params.autoplay !== 'hour' ? createBot(params.autoplay, null, true) : null, hour, telemetry: tel, pending: [], log: snapshot && snapshot.logComplete ? snapshot.log : [],
           lastMinute: Math.floor(state.t / 60) * 60, realElapsed: 0, scenario, startT: state.t, logComplete };
}

/** RI-02: the session as a save file — the state, the command log (complete or not) and the URL parameters. */
export function makeSessionSave(s: Session): SaveFile {
  const p = s.params;
  return makeSave(s.state, { log: s.log, logComplete: s.logComplete, params: { seed: p.seed, economy: p.economy, scatter: p.scatter, map: p.map, flow: p.flow, view: p.view, player: p.player, from: p.state, ruleset: rulesetOf(s.state) } });
}

/** RI-02: write the session to a browser save slot. Returns the save's hash and clock; throws when the browser
 *  refuses (private mode, quota). The state is untouched — saving is not a command and is not logged. */
export function saveSlot(s: Session, slot = '1'): SaveFile {
  const save = makeSessionSave(s);
  localStorage.setItem(saveKey(profileSlot(slot, rulesetOf(s.state))), JSON.stringify(save));
  return save;
}

/** RI-02: the URL that reloads the page from a browser save slot (`?state=local:<slot>`, the other parameters kept). */
export function slotUrl(s: Session, slot = '1'): string {
  return shareUrl({ ...s.params, state: `${LOCAL_PREFIX}${profileSlot(slot, rulesetOf(s.state))}` });
}

export { stateHash };

export function queue(s: Session, c: Command): void { s.pending.push(c); }

/** Immediate ordinary command dispatch, including while paused. Flush earlier queued inputs in order;
 * record each command once at the current tile tick. Gameplay always mutates inside the sim dispatcher. */
export function dispatch(s: Session, c: Command): ActionResult {
  const cmds = [...s.pending, c]; s.pending = [];
  for (const command of cmds) record(s, command);
  applyCommands(s.state, cmds);
  return actionResult(s.state);
}

/** Write a command at the current tile tick. `dispatch`, `frame` and `runTicks` log what
 * they apply. Only historical dev/test hooks still record direct helper calls. */
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
  const playerClaims = new Set(cmds.filter(c => c.type === 'claim' || c.type === 'activate')
    .map(c => c.type === 'activate' ? `${c.bx},${c.by}` : `${(c as { x: number }).x},${(c as { y: number }).y}`));   // RI-03: the Activate is the game's claim
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
  // RI-02: a session loaded from a save that carried its whole log replays too (the log runs from tick 0)
  if (s.scenario !== 'A' && !s.logComplete) return { error: 'a snapshot session without its command log does not replay (scenario B)' };
  if (!s.state.flow) return { error: 'no flow layer (flow=0): nothing to replay' };
  const config = protoConfig(s.params);
  const spec = s.params.map === 'lattice' ? generateMap(s.params.seed, config) : citySpec(s.params.seed, s.params.map, config);
  if (spec.city && s.state.city?.profile) spec.city.profile = s.state.city.profile;
  const st = isCampaign(s.state) ? createCampaign(s.params.seed) : createState(spec, config, s.params.seed);
  Object.assign(st.config, { power: true, supply: 'generators', draw: 'half' });
  ensureFlow(st);
  if (s.params.stalker) enableStalkers(st);   // RI-04: the candidate is part of what the log was played against
  if (s.params.heart) enableHeart(st);   // RI-06: likewise
  replay(st, s.log, s.state.flow.tick, { dropAim: opts.rifleOff ?? true });
  return { verdict: replayVerdict(s.state, st), state: st };
}
