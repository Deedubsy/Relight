/** E-project (RI-05, run name RI-05-project): the neighbourhood project framework on the tile layer — the rail yard's
 *  restoration as the first project and its reward used at once. The hour bot plays §11's hour (E-hour's city, clock
 *  and physical claim path, rifle off) with its west line on the tram route instead of E-hour's belt: after west is
 *  Held (the rail yard, 25:00 + the burn-off) it lays the restoration's reward — one track column on the street
 *  between the yard and the HQ, a stop at each end, one tram — and the yard's coal Excavator belts into the yard-side
 *  stop; the tram carries the coal to the HQ-side stop, whose inserter belts it into the Depot (E-hour's coal line by
 *  tram). Six minutes later the bot loads the local supply depot's materials (SUPPLY_DEPOT_NEED: ordinary coal and
 *  magazines from the Depot) on the HQ-side platform; the tram carries them back, the yard-side inserter fills the
 *  supply chest, and the bot commissions the depot at its chest (plan §5.2 / §9.1: the reward used on the next
 *  delivery). Rows: the project's moments, the route as laid and its price, the replay of the logged hour (the same
 *  hash, the same project records, the rail kit locked until the restoration's second — no circular unlock), a save
 *  at 40:00 reloaded and replayed to the hour's end (goalcheck.ts), the ledger, and the belt route (E-hour's benchmark
 *  configuration, unchanged) beside it for the price the reward puts on the hour's economy. E-hour keeps the belt. */
import { Experiment, ExperimentResult, Section, Check, isTrue } from '../util';
import {
  LoggedCommand, createHourBot, runHour, hourReport, replay, mmss, HOUR_CLAIM_AT, HOUR_S, TILE_TPS, conservation, Ledger,
  stateHash, lockReason, projectOf, RAIL_YARD_PROJECT, SUPPLY_DEPOT_PROJECT, SUPPLY_DEPOT_NEED, ProjectRecord, MACHINE_COST, TramPlan, ITEMS,
} from '@relight/sim';
import { hourCity } from './ehour';
import { goalReplay, GoalReplay } from '../goalcheck';

const MARKS = ['claim-west', 'held-west', 'rail-yard-restored', 'rail-kit-unlocked', 'tram-route', 'tram-first-delivery', 'rail-coal-arrived', 'depot-supplied', 'depot-restored',
  'first-brownout', 'generator-4', 'copper-2', 'claim-north', 'held-north'];
/** The save point: the route running and the depot restored (32:15), the north claim (65:00) still ahead. */
const SAVE_AT_S = 40 * 60;
const MIN = HOUR_S / 60;
const n1 = (x: number) => x.toFixed(1);
const chest = (x: { steel: number; copper: number; coal: number; magazines: number } | undefined) => x ? `${x.steel}/${x.copper}/${x.coal}/${x.magazines}` : '-';
const rec = (r: ProjectRecord | undefined) => r ? `${r.stage} · attempt ${r.activationAttemptId} · reward ${r.rewardId ?? '-'} @ ${mmss(r.rewardAt)} · delivered ${Object.entries(r.deliveredItems).map(([k, v]) => `${v} ${k}`).join(', ') || 'nothing'}` : '(no record)';
const routePrice = (p: TramPlan) => {
  const C = MACHINE_COST, trackN = p.yHi - p.yLo + 1, belts = p.excBelts.length + p.depotBelts.length;
  const steel = C.excavator.steel + belts * C.belt.steel + 2 * C.tramstop.steel + 2 * C.inserter.steel + C.chest.steel + trackN * C.track.steel + C.tram.steel;
  const copper = C.excavator.copper + belts * C.belt.copper + 2 * C.tramstop.copper + 2 * C.inserter.copper + C.chest.copper + trackN * C.track.copper + C.tram.copper;
  return { steel, copper, trackN, belts };
};

export const EPROJECT: Experiment = {
  id: 'E-project', title: 'RI-05: the rail-yard project and its reward — one track, two stops, one tram carrying the yard\'s coal to the Depot and the local depot\'s materials back (the hour bot, rifle off)',
  run(ctx): ExperimentResult {
    const { seeds } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const timeline: (string | number)[][] = [], ends: (string | number)[][] = [], routes: (string | number)[][] = [], replays: (string | number)[][] = [], saves: (string | number)[][] = [];
    const ledgerRows: (string | number)[][] = [], beside: (string | number)[][] = [], stageRows: (string | number)[][] = [];
    const restored: boolean[] = [], unlockOrder: boolean[] = [], routeLaid: boolean[] = [], delivered: boolean[] = [], depotDone: boolean[] = [], replaySame: boolean[] = [], saveSame: boolean[] = [];
    const conserved: boolean[] = [], hqHeld: boolean[] = [], falls: number[] = [], ordinary: boolean[] = [], refusals: string[] = [];
    const goalRuns: GoalReplay[] = [], ledgers: Record<string, Ledger> = {};
    for (const seed of seeds) {
      const st = hourCity(seed), bot = createHourBot(false, 'chest', HOUR_CLAIM_AT.north, 'physical', 'tram'), log: LoggedCommand[] = [];
      const lockedAtStart = lockReason(st, 'track') !== '';
      ctx.log(`E-project seed ${seed}: the tram route`);
      runHour(st, bot, HOUR_S, log);
      const r = hourReport(st, bot), f = st.flow!, ry = projectOf(st, RAIL_YARD_PROJECT), dp = projectOf(st, SUPPLY_DEPOT_PROJECT);
      timeline.push([seed, ...MARKS.map(m => mmss(r.marks[m]))]);
      const last = r.stock[r.stock.length - 1];
      ends.push([seed, rec(ry), rec(dp), f.stats.tramMoved ?? 0, r.railCoal, mmss(r.railArrival < 0 ? undefined : r.railArrival), r.held, r.hqHeld ? 'yes' : 'no', r.fell, `${r.steelMin} @ ${mmss(r.steelMinAt)}`, r.copperMin, r.coalMin, chest(last),
                 r.brownoutS.toFixed(0), n1(r.walkedPct) + ' %', r.refused.length, r.refused.map(x => `${mmss(x.t)} ${x.what}: ${x.reason}`).join(' | ') || '-']);
      for (const x of r.refused) refusals.push(`seed ${seed} ${mmss(x.t)} ${x.what}: ${x.reason}`);
      restored.push(ry?.stage === 'restored' && ry.activationAttemptId >= 0 && ry.rewardId === 'rail-route' && ry.rewardAt === ry.restoredAt);
      routeLaid.push(r.marks['tram-route'] !== undefined && r.marks['rail-kit-unlocked'] !== undefined && r.marks['tram-route'] >= r.marks['rail-kit-unlocked']);
      delivered.push(r.marks['tram-first-delivery'] !== undefined && r.railArrival >= 0 && (f.stats.tramMoved ?? 0) > 0);
      depotDone.push(dp?.stage === 'restored' && Object.entries(SUPPLY_DEPOT_NEED).every(([k, v]) => (dp.deliveredItems[k as keyof typeof dp.deliveredItems] ?? 0) >= (v ?? 0)) && dp.rewardId === 'local-depot');
      hqHeld.push(r.hqHeld); falls.push(r.fell);
      // ordinary items only: what the bot loaded on the platform and what the projects asked for are Depot items
      const loads = log.filter(c => c.c.type === 'chestPut' && 'x' in c.c && c.c.x !== undefined).map(c => (c.c as { item: string }).item);
      ordinary.push(loads.length > 0 && loads.every(i => (ITEMS as readonly string[]).includes(i)) && Object.keys(SUPPLY_DEPOT_NEED).every(k => (ITEMS as readonly string[]).includes(k)) && Object.keys(ry?.requirements ?? {}).every(k => (ITEMS as readonly string[]).includes(k)));
      if (bot.tram) {
        const p = bot.tram, price = routePrice(p);
        routes.push([seed, `column ${p.xT}, rows ${p.yLo}–${p.yHi} (${price.trackN} tiles)`, `(${p.stopA[0]},${p.stopA[1]})`, `(${p.stopB[0]},${p.stopB[1]})`, `(${p.chest[0]},${p.chest[1]})`, `(${p.exc.x},${p.exc.y})`, p.excBelts.length, p.depotBelts.length, `${price.steel} steel + ${price.copper} Cu`]);
        data[`route-${seed}`] = p;
      } else routes.push([seed, 'no route laid', '-', '-', '-', '-', '-', '-', '-']);
      // the logged hour replayed on a fresh city: the same hash and records; the rail kit's lock sampled every tick
      ctx.log(`E-project seed ${seed}: the replay`);
      const re = hourCity(seed);
      let unlockTick = -1, restoredTick = -1; const stages: string[] = [];
      replay(re, log, f.tick, { every: s => {
        const ff = s.flow!;
        if (unlockTick < 0 && lockReason(s, 'track') === '') unlockTick = ff.tick;
        const stage = projectOf(s, RAIL_YARD_PROJECT)?.stage ?? '-';
        if (stages.length === 0 || stages[stages.length - 1].split(' ')[0] !== stage) stages.push(`${stage} ${mmss(ff.tick / TILE_TPS)}`);
        if (restoredTick < 0 && stage === 'restored') restoredTick = ff.tick;
      } });
      const hPlayed = stateHash(st), hReplay = stateHash(re);
      const recSame = JSON.stringify(re.flow!.projects) === JSON.stringify(f.projects);
      const same = hPlayed === hReplay && recSame && (re.flow!.stats.tramMoved ?? 0) === (f.stats.tramMoved ?? 0);
      replaySame.push(same);
      const order = lockedAtStart && unlockTick >= 0 && restoredTick >= 0 && unlockTick === restoredTick && (r.marks['tram-route'] ?? -1) * TILE_TPS >= unlockTick;
      unlockOrder.push(order);
      replays.push([seed, hPlayed, hReplay, hPlayed === hReplay ? 'yes' : 'NO', recSame ? 'yes' : 'NO', re.flow!.stats.tramMoved ?? 0, lockedAtStart ? 'locked' : 'UNLOCKED', mmss(restoredTick < 0 ? undefined : restoredTick / TILE_TPS), mmss(unlockTick < 0 ? undefined : unlockTick / TILE_TPS), mmss(r.marks['tram-route']), order ? 'yes' : 'NO']);
      stageRows.push([seed, stages.join(' → ')]);
      // the save at 40:00 reloaded and replayed to the end (goalcheck.ts, RI-02's baseline); the goal line's disagreements reported, not scored
      ctx.log(`E-project seed ${seed}: the save / load replay`);
      const g = goalReplay(seed, () => hourCity(seed), log, f.tick, SAVE_AT_S * TILE_TPS, st);
      goalRuns.push(g);
      saveSame.push(g.hashAtSave === g.hashAfterLoad && g.hashEndUnbroken === g.hashEndLoaded && g.hashEndUnbroken === g.hashEndPlayed);
      saves.push([seed, mmss(g.saveAtT), g.hashAtSave, g.hashAfterLoad, g.hashAtSave === g.hashAfterLoad ? 'yes' : 'NO', g.hashEndUnbroken, g.hashEndLoaded, g.hashEndUnbroken === g.hashEndLoaded ? 'yes' : 'NO', g.hashEndPlayed, g.hashEndUnbroken === g.hashEndPlayed ? 'yes' : 'NO', g.wrong, g.problems.join(' | ') || '-']);
      const L = conservation(st); ledgers[`${seed}`] = L; conserved.push(L.ok);
      const col = (k: keyof Ledger['held']) => `${n1(L.opening[k])} + ${n1(L.sources[k])} → ${n1(L.held[k])} + ${n1(L.sinks[k])}`;
      ledgerRows.push([seed, L.ok ? 'yes' : 'NO', col('steel'), col('copper'), col('coal'), col('magazine'), L.problems.join('; ') || '-']);
      // the belt route beside it: E-hour's benchmark configuration, the same seed and clock
      ctx.log(`E-project seed ${seed}: the belt route beside it`);
      const sb = hourCity(seed), bb = createHourBot(false, 'chest', HOUR_CLAIM_AT.north, 'physical', 'belt');
      runHour(sb, bb, HOUR_S, []);
      const rb = hourReport(sb, bb), lb = rb.stock[rb.stock.length - 1];
      for (const [name, rr, ll] of [['tram', r, last], ['belt', rb, lb]] as const)
        beside.push([seed, name, mmss(rr.marks['west-line']), mmss(rr.railArrival < 0 ? undefined : rr.railArrival), rr.railCoal, mmss(rr.marks['generator-4']), mmss(rr.marks['first-brownout']), rr.brownoutS.toFixed(0), `${rr.steelMin} @ ${mmss(rr.steelMinAt)}`, rr.copperMin, rr.coalMin, chest(ll), rr.held, rr.fell, rr.refused.length]);
      data[`report-${seed}`] = { tram: r, belt: rb };
    }
    sections.push({ title: 'E-project-timeline: the project\'s moments (mm:ss) — the hour bot on the tram route, rifle off, river city',
      note: 'rail-yard-restored: the yard\'s block Held after the physical claim (the project\'s commissioning is the claim\'s Activate); rail-kit-unlocked: the first second Track may be placed; tram-route: the tram on its track; tram-first-delivery: the tram\'s first unload; rail-coal-arrived: the yard\'s first coal unit at the Depot (by tram, then the HQ-side belt); depot-supplied / depot-restored: the local depot\'s materials in its chest, then commissioned at the chest',
      header: ['seed', ...MARKS], rows: timeline });
    sections.push({ title: `E-project-end: the project records and the hour's end at ${MIN}:00`,
      header: ['seed', 'rail-yard record', 'supply-depot record', 'tram moved (items)', 'rail coal dug', 'rail coal at the Depot', 'held', 'HQ held', 'falls', 'steel min', 'copper min', 'coal min', `chest at ${MIN} (St/Cu/coal/mag)`, 'brownout (s)', 'walked %', 'refusals', 'refusal list'], rows: ends });
    sections.push({ title: 'E-project-route: the reward as laid — the plan read off the state (hour.ts tramPlan) and its price from the Depot',
      note: 'the column is the street between the yard and the HQ nearest the HQ lot that takes it all; stop B (the HQ side) on the row nearest the Depot\'s, stop A (the yard side) nearest the heap\'s; the belts by breadth-first search; MACHINE_COST prices (a stop 10 steel, the tram 20 steel + 5 Cu, track 1 steel a tile — implementation defaults, RI-05)',
      header: ['seed', 'track', 'stop A (yard)', 'stop B (HQ)', 'supply chest', 'coal Excavator', 'belts to A', 'belts to the Depot', 'price'], rows: routes });
    sections.push({ title: 'E-project-replay: the logged hour replayed on a fresh city — the same state hash, the same project records, the rail kit\'s lock sampled every tick',
      note: 'unlocked at = the first tick Track may be placed; it must equal the restoration\'s second (the reward is granted once, at the restoration, never before: no circular unlock) and the route must be laid after it; the hash is save.ts stateHash',
      header: ['seed', 'hash: played', 'hash: replayed', 'same', 'project records same', 'tram moved (replay)', 'rail kit at 0:00', 'restored at (replay)', 'unlocked at (replay)', 'route laid', 'order holds'], rows: replays });
    sections.push({ title: 'E-project-stages: the rail-yard record\'s stages through the replay, with the second each began', header: ['seed', 'stages'], rows: stageRows });
    sections.push({ title: `E-project-save: the replay saved at ${SAVE_AT_S / 60}:00 (save.ts makeSave → JSON → loadState), both copies replayed to ${MIN}:00 (goalcheck.ts)`,
      note: 'the goal line\'s disagreements with its oracle (RI-02\'s check) are reported here for the tram route, not scored — the oracle knows §11\'s belt line',
      header: ['seed', 'saved at', 'hash at the save', 'hash after the load', 'same', 'hash: unbroken replay', 'hash: loaded copy', 'same', 'hash: played run', 'replay = played', 'goal line wrong (samples)', 'goal problems'], rows: saves });
    sections.push({ title: 'E-project-ledger: resource conservation (ledger.ts) at the hour\'s end — opening + sources → held + sinks',
      note: 'the tram\'s cargo and the stops\' platforms and arrivals count as held items; the route\'s kit is a machine price',
      header: ['seed', 'conserved', 'steel', 'copper', 'coal', 'magazine', 'unexplained'], rows: ledgerRows });
    sections.push({ title: 'E-project-beside: the tram route against E-hour\'s belt route (the benchmark configuration, unchanged), rifle off',
      note: 'the same city, clock and claims; only the west line differs — the reward\'s price is what the tram route takes from the Depot that the belt line does not (the belt line: the Excavator and ~15 belts), paid at 26:00 when the chest holds ~18 Cu and no copper comes until the second copper Excavator at 46:00',
      header: ['seed', 'route', 'west line', 'rail coal at the Depot', 'rail coal dug', 'Generator 4', 'first brownout', 'brownout (s)', 'steel min', 'copper min', 'coal min', `chest at ${MIN}`, 'held', 'falls', 'refusals'], rows: beside });
    data.goal = goalRuns; data.ledgers = ledgers; data.refusals = refusals;
    const all = (xs: boolean[]) => xs.every(Boolean), count = (xs: boolean[]) => `${xs.filter(Boolean).length}/${xs.length} seeds`;
    checks.push(isTrue('RI-05: the rail yard restored on every seed by the physical claim (an activation attempt id ≥ 0), its reward granted at the restoration\'s second', all(restored), count(restored)));
    checks.push(isTrue('RI-05: no circular unlock — the rail kit locked at 0:00, unlocked at the restoration\'s tick and not before (the replay, every tick), the route laid after it', all(unlockOrder), count(unlockOrder)));
    checks.push(isTrue('RI-05: the reward laid and used — one track, two stops, one tram, and the yard\'s coal at the Depot by tram on every seed', all(routeLaid) && all(delivered), `${count(routeLaid)} laid, ${count(delivered)} delivered`));
    checks.push(isTrue('RI-05 (plan §5.2, §9.1): the local depot\'s materials rode the tram the other way and the depot was commissioned at its chest on every seed', all(depotDone), count(depotDone)));
    checks.push(isTrue('RI-05: ordinary items only — the platform loads and both projects\' requirements are Depot items', all(ordinary), count(ordinary)));
    checks.push(isTrue('RI-05: the logged hour replays to the played run\'s hash with the same project records and the same tram total (every seed)', all(replaySame), count(replaySame)));
    checks.push(isTrue(`RI-05: a save at ${SAVE_AT_S / 60}:00 reloads to the same hash and replays to the unbroken run's hash at ${MIN}:00 (every seed)`, all(saveSame), count(saveSame)));
    checks.push(isTrue('RI-05: every item conserved with the tram\'s cargo and the stops in the ledger (ledger.ts, tolerance 0.01)', all(conserved), all(conserved) ? `${conserved.length} runs` : Object.entries(ledgers).filter(([, L]) => !L.ok).map(([k, L]) => `${k}: ${L.problems.join('; ')}`).join(' | ')));
    checks.push(isTrue(`the hour still stands on the tram route: the HQ Held at ${MIN}:00 and no block falls (every seed)`, all(hqHeld) && falls.every(x => x === 0), `${count(hqHeld)} HQ held, ${falls.reduce((a, b) => a + b, 0)} falls`));
    checks.push(isTrue('the bot\'s route step is refused nowhere (every placement, take and load went through)', refusals.length === 0, refusals.join(' | ') || 'no refusals'));
    return { id: 'E-project', title: EPROJECT.title, pyNames: [], docRefs: ['§5 (plan)', '§9 (plan)', '§13', '§14', '§28.4'],
      setup: `river city, seeds ${seeds.join('/')}, the hour bot on the physical claim path with its west line by tram, rifle off, ${MIN} min; the replay, the ${SAVE_AT_S / 60}:00 save, the ledger; the belt route beside it`, sections, checks, data };
  },
};
