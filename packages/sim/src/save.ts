/** RI-02 — the save / load baseline (§11.2, plan line "early save/load support sufficient for repeatable testing").
 *  The state is plain JSON (types.ts), so a save is the state plus the session's command log; loading is a validated
 *  deep copy with the transients reset. `stateHash` is the check: a save at minute N reloads to the same hash and
 *  replays to the same hash as the unbroken run (E-hour, RI-02). One load path serves the game and the harness.
 *
 *  Implementation default (RI-02): the hash is FNV-1a (32-bit, 8 hex digits) over the canonical JSON (sorted keys)
 *  of the state without `events`, `acc` and `speed` — the three fields a load resets, none of which the sim reads
 *  to decide anything. A pre-RI-02 raw state file and a telemetry export (`finalState`) still load. */
import { SimState } from './types';
import type { LoggedCommand } from './hour';
import { freightProblem } from './freight';

export const SAVE_TRANSIENT: readonly (keyof SimState)[] = ['events', 'acc', 'speed'];

/** JSON with object keys sorted, so two structurally equal states hash alike whatever order they were built in. */
export function canonicalJson(v: unknown): string {
  if (v === null || typeof v !== 'object') {
    if (typeof v === 'number') return Number.isFinite(v) ? String(v) : 'null';
    if (typeof v === 'undefined' || typeof v === 'function' || typeof v === 'symbol') return 'null';
    return JSON.stringify(v);
  }
  if (Array.isArray(v)) return `[${v.map(canonicalJson).join(',')}]`;
  const o = v as Record<string, unknown>, keys = Object.keys(o).filter(k => o[k] !== undefined && typeof o[k] !== 'function').sort();
  return `{${keys.map(k => `${JSON.stringify(k)}:${canonicalJson(o[k])}`).join(',')}}`;
}

/** FNV-1a over a string's UTF-16 code units, 8 hex digits. */
export function fnv1a(s: string): string {
  let h = 0x811c9dc5;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 0x01000193) >>> 0; }
  return h.toString(16).padStart(8, '0');
}

/** The state's hash: everything the sim decides on, none of what a load resets. */
export function stateHash(st: SimState): string {
  const o: Record<string, unknown> = {};
  for (const k of Object.keys(st) as (keyof SimState)[]) if (!SAVE_TRANSIENT.includes(k)) o[k] = st[k];
  return fnv1a(canonicalJson(o));
}

export interface SaveFile {
  version: 1 | 2;
  kind: 'relight-save';
  savedAt: string;
  seed: number;
  tick: number;   // tile tick (-1 without a flow layer)
  t: number;      // sim second
  hash: string;   // stateHash(state) when saved
  /** The URL parameters the session was built from (the game); absent for a harness save. */
  params?: Record<string, string | number | boolean | null>;
  state: SimState;
  /** The session's command log (hour.ts `LoggedCommand`), complete from tick 0 when `logComplete` is true — then a
   *  loaded session can still replay from a fresh state; otherwise the log is evidence only. */
  log?: LoggedCommand[];
  logComplete?: boolean;
}

export function isSaveFile(v: unknown): v is SaveFile {
  const o = v as Partial<SaveFile> | null;
  return !!o && typeof o === 'object' && o.kind === 'relight-save' && (o.version === 1 || o.version === 2) && !!o.state && typeof o.state === 'object';
}

/** Why a value is not a Relight state, or '' when it is one. */
export function stateProblem(v: unknown): string {
  const st = v as Partial<SimState> | null;
  if (!st || typeof st !== 'object') return 'not an object';
  if (st.version !== 1 && st.version !== 2) return `version ${String(st.version)} is unsupported (expected 1 or 2)`;
  if (!Array.isArray(st.blocks)) return 'no blocks';
  if (!st.config || typeof st.config !== 'object') return 'no config';
  if (!Array.isArray(st.ring)) return 'no ring';
  if (!st.engineer || typeof st.engineer !== 'object') return 'no engineer';
  if (st.city?.profile && st.city.profile !== 'riverside-v1') return `unsupported city profile ${st.city.profile}`;
  return freightProblem(st as SimState);
}

/** A validated deep copy of a saved state — a SaveFile, a telemetry export (its `finalState`) or a raw SimState —
 *  with the transients reset (no events, no accumulated fraction, paused). Throws with the reason otherwise. */
export function loadState(raw: unknown): SimState {
  if (isSaveFile(raw) && raw.version !== raw.state.version) throw new Error('not a Relight state: save and state versions differ');
  const src = isSaveFile(raw) ? raw.state : ((raw as { finalState?: unknown } | null)?.finalState ?? raw);
  const bad = stateProblem(src);
  if (bad) throw new Error(`not a Relight state: ${bad}`);
  const st = JSON.parse(JSON.stringify(src)) as SimState;
  st.events = []; st.acc = 0; st.speed = 0;
  st.survivors ??= [];
  return st;
}

/** A save of `st` now. The state is deep-copied with its transients reset so the file equals what a load gives. */
export function makeSave(st: SimState, opts: { log?: LoggedCommand[]; logComplete?: boolean; params?: SaveFile['params']; savedAt?: string } = {}): SaveFile {
  const state = loadState(st);
  const out: SaveFile = { version: st.version, kind: 'relight-save', savedAt: opts.savedAt ?? new Date().toISOString(), seed: st.seed, tick: st.flow?.tick ?? -1, t: st.t, hash: stateHash(state), state };
  if (opts.params) out.params = opts.params;
  if (opts.log) { out.log = opts.log.map(l => ({ tick: l.tick, c: JSON.parse(JSON.stringify(l.c)) })); out.logComplete = opts.logComplete ?? false; }
  return out;
}
