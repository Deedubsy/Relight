import {authoredProblem} from './authoredCity';
import {initProgression,progressionProblem} from './progression';
import {initFixedTram, fixedTramProblem} from './fixedTram';
import {packProblem} from './engineer';
import { truckProblem } from './truck';
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
import { routingProblem } from './routing';
import { inspectionProblem } from './inspection';
import { initExpansion } from './expansion';
import { initDefence } from './campaignDefence';
import {navigationProblem} from './navigation';
import { initKnowledge, knowledgeProblem } from './campaignGuide';
import { initTurbine, turbineProblem } from './campaignTurbine';
import { initForeman, initRecruits, recruitsProblem } from './campaignRecruits';
import { initDiscovery } from './campaignDiscovery';
import { initDistricts } from './campaignDistricts';
import { discoveryProblem } from './discoveryValidation';
import { districtsProblem } from './districtValidation';
import { defenceProblem } from './defenceValidation';
import { rulesetProblem } from './rules';
import { truckWorkProblem } from './truckWork';
import { blueprintPlansProblem } from './blueprintPlans';
import { clipboardProblem } from './blueprint';
import { constructionProblem } from './construction';
import { concreteProblem } from './concreteValidation';

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
  version: 1 | 2 | 3;
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
  return !!o && typeof o === 'object' && o.kind === 'relight-save' && [1, 2, 3].includes(o.version!) && !!o.state && typeof o.state === 'object';
}

/** Why a value is not a Relight state, or '' when it is one. */
export function stateProblem(v: unknown): string {
  const st = v as Partial<SimState> | null;
  if (!st || typeof st !== 'object') return 'not an object';
  if (st.version !== 1 && st.version !== 2 && st.version !== 3) return `version ${String(st.version)} is unsupported (expected 1, 2 or 3)`;
  if (!Array.isArray(st.blocks)) return 'no blocks';
  if (!st.config || typeof st.config !== 'object') return 'no config';
  if (!Array.isArray(st.ring)) return 'no ring';
  if (!st.engineer || typeof st.engineer !== 'object') return 'no engineer';
  if(packProblem(st.engineer))return packProblem(st.engineer);
  if(st.engineer.lastDamageSource!==undefined&&(typeof st.engineer.lastDamageSource!=='string'||!st.engineer.lastDamageSource.startsWith('relay:')))return 'Invalid damage source';
  if (st.city?.profile && st.city.profile !== 'riverside-v1') return `unsupported city profile ${st.city.profile}`;
  return authoredProblem(st as SimState) || rulesetProblem(st as SimState) || concreteProblem(st as SimState) || defenceProblem(st as SimState) || districtsProblem(st as SimState) || discoveryProblem(st as SimState) || recruitsProblem(st as SimState) || turbineProblem(st as SimState) || knowledgeProblem(st as SimState) || freightProblem(st as SimState) || blueprintPlansProblem(st as SimState) || clipboardProblem(st as SimState) || constructionProblem(st as SimState) || routingProblem(st as SimState) || inspectionProblem(st as SimState) || truckProblem(st as SimState) || truckWorkProblem(st as SimState) || navigationProblem(st as SimState) || fixedTramProblem(st as SimState) || progressionProblem(st as SimState);
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
  if (st.flow) { initExpansion(st); initDefence(st); initDistricts(st); initDiscovery(st); initRecruits(st); initTurbine(st); initForeman(st); initFixedTram(st); initProgression(st); initKnowledge(st); }
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
