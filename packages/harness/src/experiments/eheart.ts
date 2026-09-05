/** E-heart (RI-06, run name RI-06-cand-heart): the first boss — the Junction Heart (plan §9, GDD §28.8) — played by the
 *  hour bot as a prepared factory with the rifle off. E-hour's river city, clock and physical claim path; the state
 *  carries the candidate layer (`enableHeart`: candidates.ts, outside SimConfig — D-RI-5), so the yard's claim is the
 *  encounter: the installation and both feeder cabinets supplied and strung, a turret by each cabinet, the Start at
 *  the substation, then the patrol answering a knocked-out feeder (repair), an interruption (retry) and a low turret
 *  (feed) until the Heart is destroyed and the yard Held. Rows: the encounter's moments, the encounter's end (attempts,
 *  packets, bodies, knock-outs, repairs, the charge, the record), the cabinets, the replay of the logged hour (the same
 *  hash; the heart events counted every tick — one destruction, one restoration, each packet once per attempt), a save
 *  at 30:00 reloaded and replayed to the hour's end (goalcheck.ts), the ledger, and E-hour's benchmark (no Heart)
 *  beside it. A candidate configuration (`*-cand-*`): it runs beside the 75-minute benchmark, never in its place. */
import { Experiment, ExperimentResult, Section, Check, isTrue } from '../util';
import {
  LoggedCommand, createHourBot, runHour, hourReport, replay, mmss, HOUR_CLAIM_AT, HOUR_S, TILE_TPS, conservation, Ledger, stateHash,
  projectOf, RAIL_YARD_PROJECT, ProjectRecord, enableHeart, heartOf, describeHeart, blockName, threatOf, SimEvent, SimState, HELD,
} from '@relight/sim';
import { hourCity } from './ehour';
import { goalReplay, GoalReplay } from '../goalcheck';

const MARKS = ['claim-west', 'heart-start', 'packet-25', 'packet-50', 'packet-75', 'heart-first-body', 'cabinet-down', 'cabinet-repaired', 'heart-interrupted', 'heart-retry',
  'heart-destroyed', 'held-west', 'rail-yard-restored', 'rail-kit-unlocked', 'claim-north', 'held-north'];
/** The save point: inside or just after the encounter (the west claim at 25:00, 90 s productive + stalls). */
const SAVE_AT_S = 30 * 60;
const MIN = HOUR_S / 60;
const n1 = (x: number) => x.toFixed(1);
const chest = (x: { steel: number; copper: number; coal: number; magazines: number } | undefined) => x ? `${x.steel}/${x.copper}/${x.coal}/${x.magazines}` : '-';
const rec = (r: ProjectRecord | undefined) => r ? `${r.stage} · attempt ${r.activationAttemptId} · reward ${r.rewardId ?? '-'} @ ${mmss(r.rewardAt)}` : '(no record)';
/** The Heart's city: E-hour's with the candidate layer on. */
function heartCity(seed: number): SimState { const st = hourCity(seed); enableHeart(st); return st; }

export const EHEART: Experiment = {
  id: 'E-heart', title: 'RI-06 (cand-heart): the Junction Heart — the rail yard\'s claim as the first boss: two feeder cabinets, a 90 s commissioning, three reinforcement packets, the hour bot rifle off (candidate layer beside the benchmark)',
  run(ctx): ExperimentResult {
    const { seeds } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const timeline: (string | number)[][] = [], ends: (string | number)[][] = [], cabinets: (string | number)[][] = [], replays: (string | number)[][] = [], saves: (string | number)[][] = [];
    const ledgerRows: (string | number)[][] = [], beside: (string | number)[][] = [];
    const destroyed: boolean[] = [], noRifle: boolean[] = [], packetsOnce: boolean[] = [], chargedOnce: boolean[] = [], rewardOnce: boolean[] = [], readable: boolean[] = [];
    const replaySame: boolean[] = [], saveSame: boolean[] = [], conserved: boolean[] = [], hqHeld: boolean[] = [], falls: number[] = [], refusals: string[] = [], preserved: boolean[] = [];
    const goalRuns: GoalReplay[] = [], ledgers: Record<string, Ledger> = {};
    for (const seed of seeds) {
      const st = hourCity(seed), H0 = enableHeart(st);
      if (!H0) { ends.push([seed, 'no Heart: the yard has fewer than two Dark neighbours', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-']); destroyed.push(false); continue; }
      const bot = createHourBot(false, 'chest', HOUR_CLAIM_AT.north, 'physical', 'belt', true), log: LoggedCommand[] = [];
      ctx.log(`E-heart seed ${seed}: the Heart's hour`);
      runHour(st, bot, HOUR_S, log);
      const r = hourReport(st, bot), f = st.flow!, H = heartOf(st)!, ry = projectOf(st, RAIL_YARD_PROJECT), T = threatOf(f);
      timeline.push([seed, ...MARKS.map(m => mmss(r.marks[m]))]);
      const last = r.stock[r.stock.length - 1], keys = Object.keys(H.requested).sort();
      ends.push([seed, describeHeart(st), H.stats.attempts, H.stats.interrupted, H.stats.aborted, keys.join(' ') || '-', H.stats.born, H.stats.knockouts, H.stats.repairs, `${H.stats.productiveS.toFixed(0)} / ${H.stats.stalledS.toFixed(0)}`,
                 H.charged ? `${H.charge.steel} steel + ${H.charge.copper} Cu` : 'not charged', rec(ry), r.held, r.hqHeld ? 'yes' : 'no', r.fell, chest(last), r.refused.length, r.refused.map(x => `${mmss(x.t)} ${x.what}: ${x.reason}`).join(' | ') || '-']);
      for (const x of r.refused) refusals.push(`seed ${seed} ${mmss(x.t)} ${x.what}: ${x.reason}`);
      H.cabinets.forEach((c, k) => cabinets.push([seed, k + 1, `(${c.x},${c.y})`, `${blockName(st, c.approach)} (block ${c.approach})`, c.point, `${c.delivered.steel} steel + ${c.delivered.copper} Cu`, c.down ? 'down' : 'up']));
      destroyed.push(H.destroyed && st.blocks[H.site].state === HELD);
      noRifle.push(!bot.rifle && T.stats.rifleKills === 0 && st.engineer.firstShot < 0);
      // once-only: every requested packet is one (attempt, threshold) pair, at most the thresholds' count per attempt
      packetsOnce.push(keys.every(k => H.cand.thresholds.includes(Number(k.split(':')[1]))) && keys.length <= H.stats.attempts * H.cand.thresholds.length && H.stats.packets === keys.length);
      chargedOnce.push(H.charged && H.charge.steel > 0);
      rewardOnce.push(ry?.stage === 'restored' && ry.rewardId === 'rail-route' && ry.rewardAt === ry.restoredAt);
      // failure preserves infrastructure and deliveries: after an interruption the cabinets' deliveries stood and the charge held (read off the end: the cabinets were not re-supplied — one delivery each)
      preserved.push(H.stats.interrupted === 0 || (H.charged && H.cabinets.every(c => c.delivered.steel <= H.cand.cabinet.steel)));
      readable.push(describeHeart(st).length > 0);
      hqHeld.push(r.hqHeld); falls.push(r.fell);
      // the logged hour replayed on a fresh city with the layer: the same hash; the heart and project events counted every tick
      ctx.log(`E-heart seed ${seed}: the replay`);
      const re = heartCity(seed);
      let starts = 0, destroyedN = 0, restoredN = 0, packetN = 0; const seen = new Set<string>(); let dup = 0;
      replay(re, log, f.tick, { every: s => {
        for (const ev of s.events as SimEvent[]) {
          if (ev.type === 'heart') {
            if (ev.what === 'start') starts++;
            else if (ev.what === 'destroyed') destroyedN++;
            else if (ev.what === 'packet') { packetN++; const key = `${ev.attempt}:${ev.threshold}`; if (seen.has(key)) dup++; seen.add(key); }
          } else if (ev.type === 'project' && ev.id === RAIL_YARD_PROJECT && ev.stage === 'restored') restoredN++;
        }
      } });
      const hPlayed = stateHash(st), hReplay = stateHash(re), Hr = heartOf(re)!;
      const same = hPlayed === hReplay && JSON.stringify(Hr) === JSON.stringify(H);
      replaySame.push(same);
      const once = destroyedN === 1 && restoredN === 1 && dup === 0 && starts === H.stats.attempts && packetN === keys.length;
      packetsOnce[packetsOnce.length - 1] = packetsOnce[packetsOnce.length - 1] && once;
      replays.push([seed, hPlayed, hReplay, hPlayed === hReplay ? 'yes' : 'NO', same ? 'yes' : 'NO', starts, packetN, dup, destroyedN, restoredN, once ? 'yes' : 'NO']);
      // the save inside / after the encounter reloaded and replayed to the end (goalcheck.ts)
      ctx.log(`E-heart seed ${seed}: the save / load replay`);
      const g = goalReplay(seed, () => heartCity(seed), log, f.tick, SAVE_AT_S * TILE_TPS, st);
      goalRuns.push(g);
      saveSame.push(g.hashAtSave === g.hashAfterLoad && g.hashEndUnbroken === g.hashEndLoaded && g.hashEndUnbroken === g.hashEndPlayed);
      saves.push([seed, mmss(g.saveAtT), g.hashAtSave, g.hashAfterLoad, g.hashAtSave === g.hashAfterLoad ? 'yes' : 'NO', g.hashEndUnbroken, g.hashEndLoaded, g.hashEndUnbroken === g.hashEndLoaded ? 'yes' : 'NO', g.hashEndPlayed, g.hashEndUnbroken === g.hashEndPlayed ? 'yes' : 'NO', g.wrong, g.problems.join(' | ') || '-']);
      const L = conservation(st); ledgers[`${seed}`] = L; conserved.push(L.ok);
      const col = (k: keyof Ledger['held']) => `${n1(L.opening[k])} + ${n1(L.sources[k])} → ${n1(L.held[k])} + ${n1(L.sinks[k])}`;
      ledgerRows.push([seed, L.ok ? 'yes' : 'NO', col('steel'), col('copper'), col('coal'), col('magazine'), L.problems.join('; ') || '-']);
      // the benchmark beside it: E-hour's configuration without the layer, the same seed and clock
      ctx.log(`E-heart seed ${seed}: the benchmark beside it`);
      const sb = hourCity(seed), bb = createHourBot(false, 'chest', HOUR_CLAIM_AT.north, 'physical', 'belt');
      runHour(sb, bb, HOUR_S, []);
      const rb = hourReport(sb, bb), lb = rb.stock[rb.stock.length - 1];
      for (const [name, rr, ll] of [['cand-heart', r, last], ['benchmark', rb, lb]] as const)
        beside.push([seed, name, mmss(rr.marks['claim-west']), mmss(rr.marks['held-west']), mmss(rr.marks['west-line']), mmss(rr.railArrival < 0 ? undefined : rr.railArrival), mmss(rr.marks['first-brownout']), rr.brownoutS.toFixed(0), `${rr.steelMin} @ ${mmss(rr.steelMinAt)}`, rr.copperMin, rr.coalMin, chest(ll), rr.held, rr.fell, rr.refused.length]);
      data[`report-${seed}`] = { heart: r, benchmark: rb, heartState: H };
    }
    const all = (xs: boolean[]) => xs.length > 0 && xs.every(Boolean), count = (xs: boolean[]) => `${xs.filter(Boolean).length}/${xs.length} seeds`;
    sections.push({ title: 'E-heart-timeline: the encounter\'s moments (mm:ss) — the hour bot on the physical claim path, the candidate layer on, rifle off, river city',
      note: 'packet-N: the reinforcement packet requested at N % of the first attempt that crossed it; heart-retry: a Start after an interruption.', header: ['seed', ...MARKS], rows: timeline });
    sections.push({ title: `E-heart-end: the encounter and the hour's end at ${MIN}:00`,
      header: ['seed', 'the Heart', 'attempts', 'interrupted', 'aborted', 'packets (attempt:threshold)', 'bodies born', 'knock-outs', 'repairs', 'productive / stalled s', 'charge', 'rail-yard record', 'held', 'HQ held', 'fell', 'chest S/Cu/coal/mag', 'refused', 'refusals'], rows: ends });
    sections.push({ title: 'E-heart-cabinets: the two feeder cabinets — the tile, the approach they face, the emergence point the packets use, what reached them',
      header: ['seed', 'cabinet', 'tile', 'approach', 'point', 'delivered', 'state'], rows: cabinets });
    sections.push({ title: 'E-heart-replay: the logged hour replayed on a fresh city with the layer — the same state hash and Heart record; the heart / project events counted every tick',
      header: ['seed', 'played hash', 'replay hash', 'same hash', 'same Heart', 'starts', 'packets', 'duplicate packets', 'destroyed events', 'restored events', 'once-only'], rows: replays });
    sections.push({ title: `E-heart-save: the replay saved at ${SAVE_AT_S / 60}:00 (save.ts makeSave → JSON → loadState), both copies replayed to ${MIN}:00 (goalcheck.ts)`,
      header: ['seed', 'saved at', 'hash at save', 'hash after load', 'same', 'end unbroken', 'end loaded', 'same', 'end played', 'same', 'goal-line disagreements', 'problems'], rows: saves });
    sections.push({ title: 'E-heart-ledger: resource conservation (ledger.ts) at the hour\'s end — opening + sources → held + sinks (the cabinets\' materials as committed stock, then spent)',
      header: ['seed', 'conserved', 'steel', 'copper', 'coal', 'magazines', 'problems'], rows: ledgerRows });
    sections.push({ title: 'E-heart-beside: the candidate layer against E-hour\'s benchmark configuration (no Heart), the same seed and clock, rifle off',
      header: ['seed', 'run', 'claim-west', 'held-west', 'west-line', 'rail coal at Depot', 'first brownout', 'brownout s', 'steel min', 'Cu min', 'coal min', 'chest S/Cu/coal/mag', 'held', 'fell', 'refused'], rows: beside });
    checks.push(isTrue('RI-06 (plan §9.4): a prepared factory completes the encounter without compulsory rifle use — the Heart destroyed and the yard Held on every seed, rifle off, no rifle kill, no shot', all(destroyed) && all(noRifle), `${count(destroyed)} destroyed, ${count(noRifle)} without a shot`));
    checks.push(isTrue('RI-06 (§9.2 default 7): every reinforcement packet keyed to (attempt, threshold) and requested once — no duplicate in the replay\'s events, at most one per threshold per attempt', all(packetsOnce), count(packetsOnce)));
    checks.push(isTrue('RI-06 (§9.2 defaults 10, 12): the installation\'s materials charged once and the reward granted once at the restoration\'s second (one destroyed event, one restored event)', all(chargedOnce) && all(rewardOnce), `${count(chargedOnce)} charged, ${count(rewardOnce)} rewarded`));
    checks.push(isTrue('RI-06 (§9.4): an interruption preserves the cabinets\' deliveries and the installation\'s charge (every interrupted seed; trivially true where none was interrupted)', all(preserved), count(preserved)));
    checks.push(isTrue('RI-06 (§9.4): the objective, the active failure condition and the next action are one readable line (describeHeart) on every seed', all(readable), count(readable)));
    checks.push(isTrue('RI-06: the logged hour replays to the played run\'s hash with the same Heart record (every seed)', all(replaySame), count(replaySame)));
    checks.push(isTrue(`RI-06: a save at ${SAVE_AT_S / 60}:00 reloads to the same hash and replays to the unbroken run's hash at ${MIN}:00 (every seed)`, all(saveSame), count(saveSame)));
    checks.push(isTrue('RI-06: every item conserved with the cabinets\' materials in the ledger (ledger.ts, tolerance 0.01)', all(conserved), all(conserved) ? `${conserved.length} runs` : Object.entries(ledgers).filter(([, L]) => !L.ok).map(([k, L]) => `${k}: ${L.problems.join('; ')}`).join(' | ')));
    checks.push(isTrue(`the hour still stands with the layer on: the HQ Held at ${MIN}:00 and no block falls (every seed)`, all(hqHeld) && falls.every(x => x === 0), `${count(hqHeld)} HQ held, ${falls.reduce((a, b) => a + b, 0)} falls`));
    checks.push(isTrue('the bot\'s Heart step is refused nowhere (every placement, delivery, repair and Start went through)', refusals.length === 0, refusals.join(' | ') || 'no refusals'));
    data.goalRuns = goalRuns.map(g => ({ seed: g.seed, wrong: g.wrong, problems: g.problems, shown: g.shown }));
    return { id: 'E-heart', title: EHEART.title, pyNames: [], docRefs: ['§9 (plan)', '§28.8', '§13', '§14'],
      setup: `river city, seeds ${seeds.join('/')}, the candidate Heart layer (candidates.ts, outside SimConfig), the hour bot on the physical claim path, rifle off, ${MIN} min; the replay, the ${SAVE_AT_S / 60}:00 save, the ledger; E-hour's benchmark beside it`, sections, checks, data };
  },
};
