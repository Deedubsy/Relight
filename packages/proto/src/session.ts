/** Glue between the sim and the renderer. Engine-free. The prototype holds no game state of its own:
 *  everything is in `state`; this file only forwards commands and drains events. */
import {
  SimState, SimEvent, Command, SimConfig, DEFAULT_CONFIG, generateMap, createState, advance, takeEvents,
  Bot, createBot, botCommands, Policy, POLICIES, protoCalibrated,
} from '@relight/sim';
import { Telemetry, createTelemetry, recordEvent, recordMinute, recordPips } from './telemetry';

export interface UrlParams { seed: number; economy: boolean; scatter: boolean; autoplay: Policy | null; player: string }

export function parseUrl(search: string): UrlParams {
  const q = new URLSearchParams(search);
  const seed = Number(q.get('seed') ?? '3');
  const auto = q.get('autoplay');
  return {
    seed: Number.isFinite(seed) ? seed : 3,
    economy: q.get('economy') !== '0',
    scatter: q.get('scatter') !== '0',
    autoplay: auto && (POLICIES as string[]).includes(auto) ? (auto as Policy) : null,
    player: q.get('player') ?? '',
  };
}

export function shareUrl(p: UrlParams): string {
  const q = new URLSearchParams();
  q.set('seed', String(p.seed));
  if (!p.economy) q.set('economy', '0');
  if (!p.scatter) q.set('scatter', '0');
  const u = new URL(location.href);
  u.search = q.toString();
  return u.toString();
}

export interface Session {
  params: UrlParams;
  state: SimState;
  bot: Bot | null;
  telemetry: Telemetry;
  pending: Command[];
  lastMinute: number;
  realElapsed: number;
}

/** PROTO-ASSUMPTION: the prototype's config. Production on with the doc's ring and unfed rule; the assembler
 *  schedule is replaced by one assembler at the start plus whatever the player builds. The numbers come from
 *  PROTO_CALIBRATED (types.ts, proto section), the same block the calibration harness runs. */
export function protoConfig(p: UrlParams): SimConfig {
  return protoCalibrated({ ...DEFAULT_CONFIG, scatter: p.scatter, economy: p.economy });
}

export function createSession(params: UrlParams): Session {
  const config = protoConfig(params);
  const spec = generateMap(params.seed, config);
  const state = createState(spec, config, params.seed);
  const tel = createTelemetry(state, location.href, params.player);
  tel.speeds.push({ t: 0, realTime: 0, speed: state.speed });
  // the proto's bots build assemblers (calibration step 4); the regression's bots do not
  return { params, state, bot: params.autoplay ? createBot(params.autoplay, null, true) : null, telemetry: tel, pending: [], lastMinute: 0, realElapsed: 0 };
}

export function queue(s: Session, c: Command): void { s.pending.push(c); }

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
  advance(s.state, realDt, cmds);
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
    advance(s.state, 1 / Math.max(1, s.state.speed), cmds);
    for (const ev of takeEvents(s.state)) { recordEvent(s.telemetry, ev, s.bot ? 'bot' : 'player'); all.push(ev); }
    recordPips(s.telemetry, s.state);
    while (s.state.t >= s.lastMinute + 60) { s.lastMinute += 60; recordMinute(s.telemetry, s.state); }
  }
  return all;
}
