/** E-hour (prompt B M6, run name B-M6-hour): §11's hour played by the walking bot on the tile layer (hour.ts) with
 *  the rifle off and on, seeds 3/4/5 on the river city. The timeline of §11's moments in mm:ss, every divergence from
 *  §11's prose and from the calibration timeline (docs/experiments/lattice/calibration.md) as a finding, the rework's
 *  three re-checks (claim walking under a minute, the truck outside the hour, walking vs §19's 15 %), Gate B's two new
 *  rows (first fire and did it matter — the rifle run's command log replayed with the rifle off; minutes walked and chest
 *  trips vs 15 %). The checks are the bars the prompt names; the findings are the report's. */
import { Experiment, ExperimentResult, Section, Check, within, isTrue } from '../util';
import {
  DEFAULT_CONFIG, protoCalibrated, createState, citySpec, ensureFlow, SimState, LoggedCommand,
  createHourBot, runHour, hourReport, replay, replayVerdict, mmss, HourReport, HOUR_CLAIM_AT, HOUR_END,
} from '@relight/sim';

/** The session's city (session.ts createSession with the flow layer and §14 power on, as the world view plays it). */
export function hourCity(seed: number): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true });
  Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  return st;
}

const MARKS = ['mine-done', 'craft-done', 'first-hand-feed', 'first-crawler', 'first-turret-fire', 'generator-2', 'line-excavators', 'line-assembler', 'first-line-magazine',
  'first-rounds-run', 'steel-to-chest', 'generator-3', 'claim-east', 'arrive-east', 'held-east', 'kitted-east', 'claim-west', 'held-west', 'kitted-west', 'claim-north', 'held-north', 'kitted-north',
  'turrets-picked-up', 'turrets-carried', 'enclosure', 'generator-4', 'electricians', 'first-shade', 'first-amber', 'first-red', 'first-shot', 'first-brownout', 'steel-zero', 'copper-2', 'hq-fell'];

export const EHOUR: Experiment = {
  id: 'E-hour', title: '§11\'s hour on the tile layer (M6)',
  run(ctx): ExperimentResult {
    const { seeds } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const timeline: (string | number)[][] = [], rows: (string | number)[][] = [], gate: (string | number)[][] = [];
    const findings: (string | number)[][] = [];
    const walkedPct: number[] = [], claimWalk: number[] = [], hqHeld: boolean[] = [], reports: Record<string, HourReport> = {};
    const stock: (string | number)[][] = [], coalRows: (string | number)[][] = [];
    const steelMin: number[] = [], fell: number[] = [], brownout: number[] = [], endOk: boolean[] = [];
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
        steelMin.push(r.steelMin); fell.push(r.fell); brownout.push(r.brownoutS);
        endOk.push(r.generators === HOUR_END.generators && r.assemblers === HOUR_END.assemblers && r.excavators === HOUR_END.excavators && r.held === HOUR_END.held);
        stock.push([seed, rifle ? 'on' : 'off', `${r.steelMin} @ ${mmss(r.steelMinAt)}`, r.copperMin, r.coalMin, ...r.stock.filter((_, i) => i % 5 === 0).map(x => `${x.steel}/${x.copper}/${x.coal}/${x.magazines}`)]);
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
    sections.push({ title: 'E-hour-stock: the chest by five minutes (steel/copper/coal/magazines) and the steel curve\'s minimum (D-P4-4)',
      note: 'the second steel Excavator feeds the chest from 12:00, the second copper Excavator from 46:00 (after Generator 4 at 45:00); the 200-steel start chest stays (D-P4-4)',
      header: ['seed', 'rifle', 'steel min', 'copper min', 'coal min', ...Array.from({ length: 13 }, (_, i) => `${i * 5}:00`)], rows: stock });
    // D-P4-7: Generator 2's coal — (a) the coal Excavator's first 40 units waited for, chest coal zeroed; (b) 40 in the chest at start
    for (const seed of seeds) for (const plan of ['wait', 'chest'] as const) {
      const st = hourCity(seed); if (plan === 'wait') st.flow!.store.coal = 0;
      const bot = createHourBot(false, plan);
      ctx.log(`E-hour-coal seed ${seed} plan ${plan}`);
      runHour(st, bot, 20 * 60, []);
      const r = hourReport(st, bot);
      coalRows.push([seed, plan === 'wait' ? '(a) coal Excavator first, chest coal 0' : '(b) 40 coal in the chest', mmss(r.marks['generator-2']), mmss(r.marks['first-brownout']), r.brownoutS.toFixed(0), r.coalMin, r.refused.filter(x => /coal/.test(x.what)).length]);
    }
    sections.push({ title: 'E-hour-coal: D-P4-7 — Generator 2\'s coal, the first 20 minutes, rifle off',
      note: 'the game ships (b); (a) is measured with the chest\'s coal zeroed so Generator 2 waits for the coal Excavator\'s first 40 units',
      header: ['seed', 'plan', 'Generator 2', 'first brownout', 'brownout (s)', 'coal min', 'coal refusals'], rows: coalRows });
    // §11 rewritten (D-P4-4): north at 60–75. The hour's script run on to 75:00, rifle off: does north, claimed at
    // HOUR_CLAIM_AT.north with the two carried turrets, stand to 75:00 on the one Mk1 line?
    const northRows: (string | number)[][] = [], northOk: boolean[] = [];
    for (const seed of seeds) {
      const st = hourCity(seed), bot = createHourBot(false);
      ctx.log(`E-hour-north seed ${seed}`);
      runHour(st, bot, 75 * 60, []);
      const r = hourReport(st, bot), last = r.stock[r.stock.length - 1];
      const ok = r.marks['held-north'] !== undefined && r.fell === 0;
      northOk.push(ok);
      northRows.push([seed, mmss(r.marks['claim-north']), mmss(r.marks['held-north']), mmss(r.marks['turrets-carried']), mmss(r.marks['fell-north']), r.fellWhy.north ?? '-', r.fell, r.held,
                      mmss(r.marks['generators-dry']), mmss(r.marks['first-brownout']), r.brownoutS.toFixed(0), `${r.steelMin} @ ${mmss(r.steelMinAt)}`, last ? `${last.steel}/${last.copper}/${last.coal}/${last.magazines}` : '-', r.refused.length, ok ? 'stands' : 'fell or never Held']);
    }
    sections.push({ title: 'E-hour-north: §11 rewritten — north claimed at 65:00, the run carried on to 75:00 (rifle off)',
      note: 'the hour affords two claims on one Mk1 Shot line (10 magazines/min); north is the third, at 60–75, with the two idle HQ turrets carried over; the HQ coal patch (~700) is dug out by ~36:00 and the west-coal stand-in makes nothing (GA-B6-3), so the coal column is the Generators\' clock',
      header: ['seed', 'claim north', 'held north', 'turrets carried', 'north fell', 'why', 'falls', 'held at 75', 'Generators dry', 'first brownout', 'brownout (s)', 'steel min', 'chest at 75 (St/Cu/coal/mag)', 'refusals', 'verdict'], rows: northRows });
    data.reports = reports;
    data.passVerdict = { steelNeverZero: Math.min(...steelMin) >= 1, noFall: fell.every(x => x === 0), noBrownout: brownout.every(x => x === 0), endState: endOk.every(Boolean) };
    checks.push(isTrue('D-P4-4: the chest\'s steel never at zero (worst run)', Math.min(...steelMin) >= 1, `minimum ${Math.min(...steelMin)} steel`));
    checks.push(isTrue('E-hour pass: no block falls on any run', fell.every(x => x === 0), `${fell.reduce((a, b) => a + b, 0)} falls across ${fell.length} runs`));
    checks.push(isTrue('§11: no brownout in the hour (every run)', brownout.every(x => x === 0), `worst ${Math.max(...brownout).toFixed(0)} s`));
    checks.push(isTrue(`§11's end state on every run (${HOUR_END.generators} Generators, ${HOUR_END.excavators} Excavators, ${HOUR_END.assemblers} Assemblers, ${HOUR_END.held} Held — §11 rewritten to two claims)`, endOk.every(Boolean), `${endOk.filter(Boolean).length}/${endOk.length} runs`));
    // north at 60–75 is measured, not scored: the section above is the evidence for D-P4-10 (the coal after the HQ patch)
    data.northAt65 = { held: northOk.filter(Boolean).length, of: northOk.length };
    checks.push(within('§19: walking at most 15 % of the hour (worst seed)', Math.max(...walkedPct), 0, 15, ' %'));
    checks.push(within('rework: the claim walk-overs under a minute of hour one (worst seed)', Math.max(...claimWalk), 0, 60, ' s'));
    checks.push(isTrue('the HQ stands at the hour on every seed', hqHeld.every(Boolean), `${hqHeld.filter(Boolean).length}/${hqHeld.length} runs`));
    return { id: 'E-hour', title: EHOUR.title, pyNames: [], docRefs: ['§11', '§19', 'calibration'], setup: 'river city, seeds ' + seeds.join('/') + ', hour bot, rifle off and on, 1 h', sections, checks, data };
  },
};
