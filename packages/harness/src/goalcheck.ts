/** RI-02: the current-goal line checked against the state it claims, through a logged hour, at 1 Hz.
 *
 *  `goalProblem` is an oracle independent of goal.ts's text building: one plain predicate per goal id ("this row is
 *  still unmet"), the first unmet row in §11's order is the id the line must show; the support line's ids likewise.
 *  `goalReplay` replays a run's command log from a fresh city, samples the line every simulated second, records
 *  every id transition, derives each §11 beat's time from the state (not from the bot's marks), and does the
 *  save / load baseline: the state at `saveAt` is hashed, round-tripped through `makeSave` → JSON → `loadState`,
 *  and both copies replay the rest of the log to the same hash at the end. */
import {
  SimState, LoggedCommand, HELD, DARK, CONTESTED, Goal, LegacyGoalId, SupportId, currentGoal, GOAL_ORDER, replay, TILE_TPS, isCampaign,
  generators, assemblers, shotAssemblers, excavatorsOn, patchExcavators, patchLeft, railCoalLeft, hqNeighbourToward, hqIdx, blockOfTile,
  throttle, edgeCap, stateHash, makeSave, loadState, SHOT, HOUR_MINE_STEEL, HOUR_CRAFT_MAGS, HOUR_END, HOUR, mmss,
} from '@relight/sim';

type Dir = 'east' | 'west' | 'north';
const dirBlock = (st: SimState, d: Dir) => { const i = hqNeighbourToward(st, d); return i >= 0 ? st.blocks[i] : null; };
const dirIdx = (st: SimState, d: Dir) => hqNeighbourToward(st, d);

/** "This row is still unmet" per goal id. The ids with no row of their own (front, hq-fell, hold) are handled in `goalProblem`. */
const UNMET: Record<Exclude<LegacyGoalId, 'front' | 'hq-fell' | 'hold'>, (st: SimState) => boolean> = {
  mine: st => { const f = st.flow!; return shotAssemblers(st).length === 0 && f.stats.handCrafted < HOUR_CRAFT_MAGS && f.stats.handMined < HOUR_MINE_STEEL
                  && (st.engineer.inv.steel ?? 0) < SHOT.inputs.steel * (HOUR_CRAFT_MAGS - f.stats.handCrafted); },
  craft: st => shotAssemblers(st).length === 0 && st.flow!.stats.handCrafted < HOUR_CRAFT_MAGS,
  'coal-line': st => patchExcavators(st, 'coal').length === 0 && patchLeft(st, 'coal') > 0,
  'gen-2': st => generators(st).length < 2,
  'steel-line': st => patchExcavators(st, 'steel').length === 0 && patchLeft(st, 'steel') > 0,
  'copper-line': st => patchExcavators(st, 'copper').length === 0 && patchLeft(st, 'copper') > 0,
  'shot-line': st => shotAssemblers(st).length === 0,
  'steel-2': st => patchExcavators(st, 'steel').length < 2 && patchLeft(st, 'steel') > 0,
  'gen-3': st => generators(st).length < 3,
  'claim-east': st => dirBlock(st, 'east')?.state === DARK && !st.fallen[dirIdx(st, 'east')],
  'contest-east': st => dirBlock(st, 'east')?.state === CONTESTED,
  'retake-east': st => dirBlock(st, 'east')?.state === DARK && !!st.fallen[dirIdx(st, 'east')],
  'claim-west': st => dirBlock(st, 'west')?.state === DARK && !st.fallen[dirIdx(st, 'west')],
  'contest-west': st => dirBlock(st, 'west')?.state === CONTESTED,
  'retake-west': st => dirBlock(st, 'west')?.state === DARK && !!st.fallen[dirIdx(st, 'west')],
  'rail-coal': st => { const w = dirIdx(st, 'west'); return w >= 0 && st.blocks[w].state === HELD && railCoalLeft(st) > 0 && !excavatorsOn(st, 'coal').some(m => blockOfTile(st, m.x, m.y) === w); },
  'gen-4': st => generators(st).length < HOUR_END.generators,
  'copper-2': st => patchExcavators(st, 'copper').length < 2 && patchLeft(st, 'copper') > 0,
  'assembler-3': st => assemblers(st).length < HOUR_END.assemblers,
  'claim-north': st => dirBlock(st, 'north')?.state === DARK && !st.fallen[dirIdx(st, 'north')],
  'contest-north': st => dirBlock(st, 'north')?.state === CONTESTED,
  'retake-north': st => dirBlock(st, 'north')?.state === DARK && !!st.fallen[dirIdx(st, 'north')],
};

/** The support ids in the order the line prefers them, each with its state predicate. */
const SUPPORT: [SupportId, (st: SimState) => boolean][] = [
  ['down', st => st.engineer.down >= 0],
  ['gen-dry', st => generators(st).length > 0 && generators(st).every(m => (m.inv.coal ?? 0) <= 0)],
  ['kit', st => st.ring.some(e => st.blocks[e.a].state === HELD && e.kit === false)],
  ['feed', st => st.ring.some(e => st.blocks[e.a].state === HELD && e.kit !== false && e.hopper <= 1e-9 && edgeCap(st, e) > 0)],
  ['brownout', st => throttle(st) < 1 - 1e-9],
];

/** The id the state must show: the first unmet §11 row, `hold` when none is. */
export function expectedGoal(st: SimState): LegacyGoalId {
  if (isCampaign(st)) throw new Error('The legacy goal oracle cannot evaluate an exploration campaign');
  if (st.blocks[hqIdx(st)].state !== HELD) return 'hq-fell';
  if (!st.flow) return 'front';
  for (const id of GOAL_ORDER) if (id !== 'front' && id !== 'hq-fell' && id !== 'hold' && UNMET[id](st)) return id;
  return 'hold';
}
export function expectedSupport(st: SimState): SupportId | null {
  for (const [id, p] of SUPPORT) if (p(st)) return id;
  return null;
}

/** '' when the line agrees with the state; otherwise what is wrong. */
export function goalProblem(st: SimState, g: Goal): string {
  const want = expectedGoal(st), sup = expectedSupport(st);
  const problems: string[] = [];
  if (g.goal.id !== want) problems.push(`goal ${g.goal.id}, state says ${want}`);
  if ((g.support?.id ?? null) !== sup) problems.push(`support ${g.support?.id ?? 'none'}, state says ${sup ?? 'none'}`);
  if (!g.goal.text || !g.goal.why) problems.push('goal line without text or reason');
  if (g.support && (!g.support.text || !g.support.why)) problems.push('support line without text or reason');
  return problems.join('; ');
}

/** §11's rows with a state-derived beat: the first second the state meets the row (patch rows count the Excavators placed on the HQ patch). */
const BEAT: Record<string, (st: SimState) => boolean> = {
  mine: st => st.flow!.stats.handMined >= HOUR_MINE_STEEL,
  craft: st => st.flow!.stats.handCrafted >= HOUR_CRAFT_MAGS,
  'coal-excavator': st => patchExcavators(st, 'coal').length >= 1,
  'generator-2': st => generators(st).length >= 2,
  'line-excavators': st => patchExcavators(st, 'steel').length >= 1 && patchExcavators(st, 'copper').length >= 1,
  'shot-line': st => shotAssemblers(st).length >= 1,
  'steel-2': st => patchExcavators(st, 'steel').length >= 2,
  'generator-3': st => generators(st).length >= 3,
  'claim-east': st => dirBlock(st, 'east')?.state !== DARK,
  'claim-west': st => dirBlock(st, 'west')?.state !== DARK,
  'generator-4': st => generators(st).length >= 4,
  'copper-2': st => patchExcavators(st, 'copper').length >= 2,
  'assembler-3': st => assemblers(st).length >= 3,
  'claim-north': st => dirBlock(st, 'north')?.state !== DARK,
};

export interface GoalTransition { t: number; from: string; to: string; dwellS: number }
export interface GoalBeat { row: string; min: number; t: number; before: string; after: string; changed: boolean }
export interface GoalReplay {
  seed: number; samples: number;
  /** Samples whose line disagreed with the state, with the first few problems. */
  wrong: number; problems: string[];
  transitions: GoalTransition[];
  /** Ids shown for under `flickerS` seconds, and ids that came back after leaving. */
  flicker: GoalTransition[]; revisits: string[];
  /** Every id shown, in order of first appearance. */
  shown: string[];
  supportSeconds: Record<string, number>;
  beats: GoalBeat[];
  /** The save / load baseline at `saveAt`. */
  saveAtT: number; hashAtSave: string; hashAfterLoad: string; endTick: number; hashEndUnbroken: string; hashEndLoaded: string; hashEndPlayed: string;
}

/** Replay `log` from `fresh()` with the goal line sampled at 1 Hz; save at `saveAtTick`, load, and replay both to `endTick`. */
export function goalReplay(seed: number, fresh: () => SimState, log: LoggedCommand[], endTick: number, saveAtTick: number, played: SimState, flickerS = 5): GoalReplay {
  const A = fresh();
  const transitions: GoalTransition[] = [], problems: string[] = [], beats: GoalBeat[] = [], shown: string[] = [];
  const supportSeconds: Record<string, number> = {};
  const beatDone = new Set<string>();
  let samples = 0, wrong = 0, last = '', lastAt = 0;
  const sample = (st: SimState) => {
    const f = st.flow!;
    if (f.tick % TILE_TPS !== 0) return;
    samples++;
    const g = currentGoal(st), id = g.goal.id;
    const p = goalProblem(st, g);
    if (p) { wrong++; if (problems.length < 8) problems.push(`${mmss(st.t)}: ${p}`); }
    if (g.support) supportSeconds[g.support.id] = (supportSeconds[g.support.id] ?? 0) + 1;
    if (id !== last) {
      if (last) transitions.push({ t: st.t, from: last, to: id, dwellS: st.t - lastAt });
      if (!shown.includes(id)) shown.push(id);
      last = id; lastAt = st.t;
    }
    for (const row of HOUR) {
      const pred = BEAT[row.id];
      if (!pred || beatDone.has(row.id) || !pred(st)) continue;
      beatDone.add(row.id);
      const before = transitions.length && transitions[transitions.length - 1].t === st.t ? transitions[transitions.length - 1].from : id;
      beats.push({ row: row.id, min: row.min, t: st.t, before, after: id, changed: before !== id });
    }
  };
  sample(A);
  replay(A, log, saveAtTick, { every: sample });
  const saveAtT = A.t, hashAtSave = stateHash(A);
  const save = makeSave(A, { log: log.filter(l => l.tick < saveAtTick), logComplete: true });
  const B = loadState(JSON.parse(JSON.stringify(save)));
  const hashAfterLoad = stateHash(B);
  const from = log.findIndex(l => l.tick >= saveAtTick), rest = from < 0 ? [] : log.slice(from);
  replay(A, rest, endTick, { every: sample });
  replay(B, rest, endTick);
  const flicker = transitions.filter(tr => tr.dwellS < flickerS);
  const seen = new Set<string>(), revisits: string[] = [];
  let prev = '';
  for (const tr of [{ to: transitions.length ? transitions[0].from : last } as GoalTransition, ...transitions]) {
    if (seen.has(tr.to) && tr.to !== prev) revisits.push(tr.to);
    seen.add(tr.to); prev = tr.to;
  }
  return { seed, samples, wrong, problems, transitions, flicker, revisits, shown, supportSeconds, beats,
           saveAtT, hashAtSave, hashAfterLoad, endTick, hashEndUnbroken: stateHash(A), hashEndLoaded: stateHash(B), hashEndPlayed: stateHash(played) };
}
