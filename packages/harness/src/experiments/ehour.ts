/** E-hour (prompt B M6, run name B-M6-hour): §11's hour played by the walking bot on the tile layer (hour.ts) with
 *  the rifle off and on, seeds 3/4/5 on the river city. The timeline of §11's moments in mm:ss, every divergence from
 *  §11's prose and from the calibration timeline (docs/experiments/lattice/calibration.md) as a finding, the rework's
 *  three re-checks (claim walking under a minute, the truck outside the hour, walking vs §19's 15 %), Gate B's two new
 *  rows (first fire and did it matter — the rifle run's command log replayed with the rifle off; minutes walked and chest
 *  trips vs 15 %). The checks are the bars the prompt names; the findings are the report's. */
import { Experiment, ExperimentResult, Section, Check, within, isTrue } from '../util';
import {
  DEFAULT_CONFIG, protoCalibrated, createState, citySpec, ensureFlow, SimState, LoggedCommand,
  createHourBot, runHour, hourReport, replay, replayVerdict, mmss, HourReport, HOUR_CLAIM_AT,
} from '@relight/sim';

/** The session's city (session.ts createSession with the flow layer and §14 power on, as the world view plays it). */
export function hourCity(seed: number): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true });
  Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  return st;
}

const MARKS = ['mine-done', 'craft-done', 'feed-done', 'first-crawler', 'first-turret-fire', 'generator-2', 'line-excavators', 'line-assembler', 'first-line-magazine',
  'first-rounds-run', 'generator-3', 'claim-east', 'arrive-east', 'held-east', 'kitted-east', 'claim-west', 'held-west', 'kitted-west', 'claim-north', 'held-north', 'kitted-north',
  'turrets-picked-up', 'turrets-carried', 'enclosure', 'generator-4', 'electricians', 'first-shade', 'first-amber', 'first-red', 'first-shot', 'first-brownout', 'hq-fell'];

export const EHOUR: Experiment = {
  id: 'E-hour', title: '§11\'s hour on the tile layer (M6)',
  run(ctx): ExperimentResult {
    const { seeds } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const timeline: (string | number)[][] = [], rows: (string | number)[][] = [], gate: (string | number)[][] = [];
    const findings: (string | number)[][] = [];
    const walkedPct: number[] = [], claimWalk: number[] = [], hqHeld: boolean[] = [], reports: Record<string, HourReport> = {};
    for (const seed of seeds) {
      for (const rifle of [false, true]) {
        const st = hourCity(seed), bot = createHourBot(rifle), log: LoggedCommand[] = [];
        ctx.log(`E-hour seed ${seed} rifle ${rifle ? 'on' : 'off'}`);
        runHour(st, bot, 3600, log);
        const r = hourReport(st, bot);
        const key = `${seed}/${rifle ? 'rifle' : 'no-rifle'}`;
        reports[key] = r;
        timeline.push([seed, rifle ? 'on' : 'off', ...MARKS.map(m => mmss(r.marks[m]))]);
        rows.push([seed, rifle ? 'on' : 'off', r.held, r.hqHeld ? 'yes' : 'no', r.turrets, r.generators, r.excavators, r.assemblers, r.magsMade, (r.walkedS / 60).toFixed(1), r.walkedPct.toFixed(1) + ' %',
                   r.claimWalkS.toFixed(0), r.chestTrips, r.handFed, r.reachRefused, r.crawlers, r.shades, r.turretKills, r.rifleKills, r.brownoutS.toFixed(0), r.refused.length]);
        for (const f of r.findings) findings.push([seed, rifle ? 'on' : 'off', f]);
        walkedPct.push(r.walkedPct); claimWalk.push(r.claimWalkS); hqHeld.push(r.hqHeld);
        if (rifle) {
          // Gate B's row: the rifle run replayed with its aim commands dropped
          const re = hourCity(seed);
          replay(re, log, st.flow!.tick, { dropAim: true });
          const v = replayVerdict(st, re);
          gate.push([seed, mmss(r.marks['first-shot']), r.fired, r.rifleKills, v.fights.length, v.fights.filter(f => f.verdict === 'saved it').length, v.fights.filter(f => f.verdict === 'fell anyway').length,
                     v.heldPlayed, v.heldReplayed, v.verdict]);
          data[`verdict-${seed}`] = v;
          // determinism: the same log replayed with the aim kept must reproduce the run
          const same = hourCity(seed);
          replay(same, log, st.flow!.tick);
          const eq = same.blocks.every((b, i) => b.state === st.blocks[i].state) && same.flow!.machines.length === st.flow!.machines.length && same.engineer.fired === st.engineer.fired;
          checks.push(isTrue(`seed ${seed}: the logged hour replays to the same state (aim kept)`, eq, eq ? 'same Held set, machine count and shots' : 'the replay diverged — the tile sim is not frame-independent here'));
        }
      }
    }
    sections.push({ title: 'E-hour-timeline: §11\'s moments (mm:ss) — bot on the tile layer, river city, flow + power on',
      note: `claims on the calibration's clock (${Object.entries(HOUR_CLAIM_AT).map(([k, v]) => `${k} ${v / 60}`).join(', ')} min); Generators at E4-doc's 0/6/15/45`,
      header: ['seed', 'rifle', ...MARKS], rows: timeline });
    sections.push({ title: 'E-hour-end: the hour\'s end state, walking and the hands',
      header: ['seed', 'rifle', 'held', 'HQ held', 'turrets', 'Generators', 'Excavators', 'Assemblers', 'line magazines', 'walked (min)', 'walked % (§19: 15)', 'claim walk-overs (s)', 'chest trips', 'hand-fed', 'reach refusals', 'crawlers', 'shades', 'turret kills', 'rifle kills', 'brownout (s)', 'refusals'], rows });
    sections.push({ title: 'E-hour-gate: Gate B\'s rifle row — the rifle run replayed with the rifle off',
      note: 'per hand-fired fight: held anyway / saved it (stood only with the rifle) / fell anyway; the run\'s verdict is the worst of them (the HQ\'s fate when no shot was fired)',
      header: ['seed', 'first shot', 'rounds fired', 'rifle kills', 'fights', 'saved it', 'fell anyway', 'held (played)', 'held (rifle off)', 'verdict'], rows: gate });
    sections.push({ title: 'E-hour-findings: every divergence from §11 and from the calibration timeline', header: ['seed', 'rifle', 'finding'], rows: findings });
    data.reports = reports;
    checks.push(within('§19: walking at most 15 % of the hour (worst seed)', Math.max(...walkedPct), 0, 15, ' %'));
    checks.push(within('rework: the claim walk-overs under a minute of hour one (worst seed)', Math.max(...claimWalk), 0, 60, ' s'));
    checks.push(isTrue('the HQ stands at the hour on every seed', hqHeld.every(Boolean), `${hqHeld.filter(Boolean).length}/${hqHeld.length} runs`));
    return { id: 'E-hour', title: EHOUR.title, pyNames: [], docRefs: ['§11', '§19', 'calibration'], setup: 'river city, seeds ' + seeds.join('/') + ', hour bot, rifle off and on, 1 h', sections, checks, data };
  },
};
