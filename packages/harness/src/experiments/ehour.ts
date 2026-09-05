/** E-hour (prompt B M6, run name B-M6-hour; RI-01 re-scoped it to the 75-minute hour): §11's hour played by the
 *  walking bot on the tile layer (hour.ts) with the rifle off and on, seeds 3/4/5 on the river city. The timeline of
 *  §11's moments in mm:ss, every divergence from §11's prose and from the calibration timeline
 *  (docs/experiments/lattice/calibration.md) as a finding, the rework's three re-checks (claim walking under a minute,
 *  the truck outside the hour, walking vs §19's 15 %), Gate B's two new rows (first fire and did it matter — the rifle
 *  run's command log replayed with the rifle off; minutes walked and chest trips vs 15 %), and RI-01's rows: north
 *  claimed at 65:00 and Held at 75:00 (D-HOUR-3), the rail yard's coal at the Depot ≥ 10 min before the chest's coal
 *  is gone (D-P4-12, M6's check (a)), the crawlers counted at the first red pip (M6's (b)), and the resource
 *  conservation check (ledger.ts) on every run. The checks are the bars the prompt names; the findings are the report's. */
import { Experiment, ExperimentResult, Section, Check, within, isTrue } from '../util';
import {
  DEFAULT_CONFIG, protoCalibrated, createState, citySpec, ensureFlow, SimState, LoggedCommand,
  createHourBot, runHour, hourReport, replay, replayVerdict, mmss, HourReport, HOUR_CLAIM_AT, HOUR_END, HOUR_S, conservation, Ledger,
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
  'first-rounds-run', 'steel-to-chest', 'generator-3', 'claim-east', 'arrive-east', 'held-east', 'kitted-east', 'east-line', 'claim-west', 'held-west', 'kitted-west', 'west-line', 'rail-coal-arrived',
  'claim-north', 'held-north', 'kitted-north', 'enclosure', 'generator-4', 'electricians', 'first-shade', 'first-amber', 'first-red', 'first-shot', 'first-brownout', 'generators-dry', 'steel-zero', 'copper-2', 'hq-fell'];

/** North's minute (D-P4-10 (a), D-HOUR-3: north at constants.HOUR's 65:00, inside the 75-minute hour). */
const NORTH_AT = HOUR_CLAIM_AT.north;
const MIN = HOUR_S / 60;
const chest = (x: { steel: number; copper: number; coal: number; magazines: number } | undefined) => x ? `${x.steel}/${x.copper}/${x.coal}/${x.magazines}` : '-';
const n1 = (x: number) => x.toFixed(1);

export const EHOUR: Experiment = {
  id: 'E-hour', title: `§11's ${MIN}-minute hour on the tile layer (M6, RI-01)`,
  run(ctx): ExperimentResult {
    const { seeds } = ctx;
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};
    const timeline: (string | number)[][] = [], rows: (string | number)[][] = [], gate: (string | number)[][] = [];
    const findings: (string | number)[][] = [];
    const walkedPct: number[] = [], claimWalk: number[] = [], hqHeld: boolean[] = [], reports: Record<string, HourReport> = {};
    const stock: (string | number)[][] = [], coalRows: (string | number)[][] = [], northRows: (string | number)[][] = [], ledgerRows: (string | number)[][] = [];
    const steelMin: number[] = [], fell: number[] = [], brownout: number[] = [], endOk: boolean[] = [], northOk: boolean[] = [], railMargins: number[] = [];
    const ledgers: Record<string, Ledger> = {}, conserved: boolean[] = [];
    for (const seed of seeds) {
      for (const rifle of [false, true]) {
        const st = hourCity(seed), bot = createHourBot(rifle, 'chest', NORTH_AT), log: LoggedCommand[] = [];
        ctx.log(`E-hour seed ${seed} rifle ${rifle ? 'on' : 'off'}`);
        runHour(st, bot, HOUR_S, log);
        const r = hourReport(st, bot), key = `${seed}/${rifle ? 'rifle' : 'no-rifle'}`, on = rifle ? 'on' : 'off';
        reports[key] = r;
        timeline.push([seed, on, ...MARKS.map(m => mmss(r.marks[m]))]);
        rows.push([seed, on, r.held, r.hqHeld ? 'yes' : 'no', r.turrets, r.generators, r.excavators, r.assemblers, r.magsMade, n1(r.walkedS / 60), n1(r.walkedPct) + ' %',
                   r.claimWalkS.toFixed(0), r.chestTrips, r.handFed, r.reachRefused, r.crawlers, r.shades, r.turretKills, r.rifleKills, r.brownoutS.toFixed(0), r.refused.length]);
        for (const f of r.findings) findings.push([seed, on, f]);
        walkedPct.push(r.walkedPct); claimWalk.push(r.claimWalkS); hqHeld.push(r.hqHeld);
        steelMin.push(r.steelMin); fell.push(r.fell); brownout.push(r.brownoutS);
        endOk.push(r.generators === r.endWant.generators && r.assemblers === r.endWant.assemblers && r.excavators === r.endWant.excavators && r.held === r.endWant.held && r.turrets === r.endWant.turrets);
        stock.push([seed, on, `${r.steelMin} @ ${mmss(r.steelMinAt)}`, r.copperMin, r.coalMin, ...r.stock.filter((_, i) => i % 5 === 0).map(chest)]);
        // RI-01 (D-HOUR-3): north claimed at 65:00 stands to 75:00 on the rail yard's real coal — scored, every run
        const last = r.stock[r.stock.length - 1], nOk = r.marks['held-north'] !== undefined && r.fellWhy.north === undefined && r.fell === 0;
        northOk.push(nOk);
        northRows.push([seed, on, mmss(r.marks['claim-north']), mmss(r.marks['held-north']), mmss(r.marks['fell-north']), r.fellWhy.north ?? '-', r.fell, r.held,
                        mmss(r.marks['generators-dry']), mmss(r.marks['first-brownout']), r.brownoutS.toFixed(0), `${r.steelMin} @ ${mmss(r.steelMinAt)}`, chest(last), r.refused.length, nOk ? 'stands' : 'fell or never Held']);
        if (!rifle) railMargins.push(r.railMargin);
        // RI-01: the conservation check — production, transfer, delivery commitment and consumption reconcile
        const L = conservation(st); ledgers[key] = L; conserved.push(L.ok);
        const col = (k: keyof Ledger['held']) => `${n1(L.opening[k])} + ${n1(L.sources[k])} → ${n1(L.held[k])} + ${n1(L.sinks[k])}`;
        ledgerRows.push([seed, on, L.ok ? 'yes' : 'NO', st.stats.roundsLost ?? 0, col('steel'), col('copper'), col('stone'), col('coal'), col('magazine'), col('wire'),
                         r.handFedMags, r.handFedCoal, L.problems.join('; ') || '-']);
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
    sections.push({ title: `E-hour-timeline: §11's moments (mm:ss) — bot on the tile layer, river city, flow + power on, ${MIN} minutes`,
      note: `claims on the calibration's clock (${Object.entries(HOUR_CLAIM_AT).map(([k, v]) => `${k} ${v / 60}`).join(', ')} min — three claims inside the ${MIN}-minute hour, D-HOUR-3); Generators at E4-doc's 0/6/15/45; east-line / west-line: the district's Excavator and belt reach the Depot (RI-01); rail-coal-arrived: the rail yard's first coal unit at the Depot`,
      header: ['seed', 'rifle', ...MARKS], rows: timeline });
    sections.push({ title: `E-hour-end: the hour's end state at ${MIN}:00, walking and the hands`,
      header: ['seed', 'rifle', 'held', 'HQ held', 'turrets', 'Generators', 'Excavators', 'Assemblers', 'line magazines', 'walked (min)', 'walked % (§19: 15)', 'claim walk-overs (s)', 'chest trips', 'hand-fed', 'reach refusals', 'crawlers', 'shades', 'turret kills', 'rifle kills', 'brownout (s)', 'refusals'], rows });
    sections.push({ title: 'E-hour-gate: Gate B\'s rifle row — the rifle run replayed with the rifle off',
      note: 'per hand-fired fight: held anyway / saved it (stood only with the rifle) / fell anyway; the run\'s verdict is the worst of them (the HQ\'s fate when no shot was fired)',
      header: ['seed', 'first shot', 'rounds fired', 'rifle kills', 'fights', 'saved it', 'fell anyway', 'held (played)', 'held (rifle off)', 'verdict'], rows: gate });
    sections.push({ title: 'E-hour-findings: every divergence from §11 and from the calibration timeline', header: ['seed', 'rifle', 'finding'], rows: findings });
    const stockTs = (reports[`${seeds[0]}/no-rifle`]?.stock ?? []).filter((_, i) => i % 5 === 0).map(x => mmss(x.t));
    sections.push({ title: 'E-hour-stock: the chest by five minutes (steel/copper/coal/magazines) and the steel curve\'s minimum (D-P4-4)',
      note: 'the second steel Excavator feeds the chest from 12:00 (D-HOUR-2 (a)), the second copper Excavator from 46:00 (after Generator 4 at 45:00); the 200-steel start chest stays (D-P4-4); the HQ coal patch is ~700 units (D-P4-12) and the rail yard\'s coal takes over on the west line',
      // the chest is sampled once a minute, so the five-minute columns are the sampled minutes themselves: a fixed
      // 0:00–60:00 header was one column longer than the data and read every value a step late (T1's light review)
      header: ['seed', 'rifle', 'steel min', 'copper min', 'coal min', ...stockTs], rows: stock });
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
    // RI-01 (D-HOUR-3, D-P4-10): north is the third claim, at constants.HOUR's 65:00 inside the 75-minute hour, on the
    // block-level hopper alone (D-P4-9: no turret is carried over); it is Held at 75:00 or the run is red. Before RI-01
    // this was a separate rifle-off run, measured and not scored (T1: "north at 60–75").
    sections.push({ title: `E-hour-north: north at ${NORTH_AT / 60}:00, Held at ${MIN}:00 (D-HOUR-3) — east 15, west 25, the rail yard's coal on the west line`,
      note: 'scored on every run: north Held at the end and no block fell; the Generators\' coal is the HQ patch (~700, D-P4-12) until the rail yard\'s line delivers (E-hour-m6checks)',
      header: ['seed', 'rifle', 'claim north', 'held north', 'north fell', 'why', 'falls', `held at ${MIN}`, 'Generators dry', 'first brownout', 'brownout (s)', 'steel min', `chest at ${MIN} (St/Cu/coal/mag)`, 'refusals', 'verdict'], rows: northRows });
    // M6's two checks (economy-fix task Step 4; RI-01 closes them), rifle off: (a) the rail yard's coal at the Depot ≥ 10
    // min before the chest's coal runs out for good; (b) the crawlers the HQ had counted at the first red pip (~minute 6).
    const m6: (string | number)[][] = [];
    for (const seed of seeds) {
      const r = reports[`${seed}/no-rifle`], last = r.stock[r.stock.length - 1];
      const margin = r.railArrival < 0 ? 'never arrived' : r.coalOut < 0 ? `${n1(r.railMargin / 60)} min to the run's end (the chest's coal never ran out)` : `${n1(r.railMargin / 60)} min (out for good at ${mmss(r.coalOut)})`;
      m6.push([seed, mmss(r.marks['west-line']), mmss(r.railArrival < 0 ? undefined : r.railArrival), r.railCoal, mmss(r.coalZeroAt >= 0 ? r.coalZeroAt : undefined), mmss(r.coalOut < 0 ? undefined : r.coalOut), mmss(r.marks['generators-dry']), last?.coal ?? '-', margin,
               r.atFirstRed ? mmss(r.atFirstRed.t) : '-', r.atFirstRed?.spawned ?? '-', r.atFirstRed?.arrivals ?? '-']);
    }
    sections.push({ title: 'E-hour-m6checks: the rail yard\'s coal against the chest\'s (a) and the crawlers counted at the first red pip (b) — rifle off',
      note: '(a) margin = minutes from the rail yard\'s first coal unit at the Depot to the chest\'s coal running out for good (the run\'s end when it never does); RI-01\'s bar is 10 min; the chest\'s first zero is Generator 2\'s take of the start chest\'s 40 at ~6:00, not a shortage; (b) spawned and arrived at a Held edge by the first red pip',
      header: ['seed', 'west line built', 'rail coal at the Depot', 'rail coal dug', 'chest coal first zero', 'chest coal out for good', 'Generators dry', `chest coal at ${MIN}:00`, 'margin', 'first red pip', 'crawlers spawned by then', 'crawler arrivals by then'], rows: m6 });
    sections.push({ title: 'E-hour-ledger: resource conservation (RI-01, ledger.ts) — opening + sources → held + sinks, by item, at the run\'s end',
      note: 'sources: rubble units mined and recipe output; sinks: recipe inputs, machine and claim prices, repairs, coal burned, rounds fired or lost to a full buffer; transfers (belts, the chest, hand-feeds, the ring\'s draw) are on neither side; the hand-fed totals are transfers the Depot\'s buffer and coal explain (D-P4-11)',
      header: ['seed', 'rifle', 'conserved', 'rounds lost', 'steel', 'copper', 'stone', 'coal', 'magazine', 'wire', 'hand-fed magazines', 'hand-fed coal', 'unexplained'], rows: ledgerRows });
    data.reports = reports;
    data.ledgers = ledgers;
    data.north = { held: northOk.filter(Boolean).length, of: northOk.length, northAt: NORTH_AT };
    const worstMargin = Math.min(...railMargins);
    data.passVerdict = { steelNeverZero: Math.min(...steelMin) >= 1, noFall: fell.every(x => x === 0), noBrownout: brownout.every(x => x === 0), endState: endOk.every(Boolean),
                         northHeld: northOk.every(Boolean), railMargin: worstMargin >= 600, conserved: conserved.every(Boolean) };
    checks.push(isTrue('D-P4-4: the chest\'s steel never at zero (worst run)', Math.min(...steelMin) >= 1, `minimum ${Math.min(...steelMin)} steel`));
    checks.push(isTrue('E-hour pass: no block falls on any run', fell.every(x => x === 0), `${fell.reduce((a, b) => a + b, 0)} falls across ${fell.length} runs`));
    checks.push(isTrue('§11: no brownout in the hour (every run)', brownout.every(x => x === 0), `worst ${Math.max(...brownout).toFixed(0)} s`));
    checks.push(isTrue(`§11's end state on every run (${HOUR_END.generators} Generators, ${HOUR_END.excavators} Excavators — one fewer where east has nothing to dig — ${HOUR_END.assemblers} Assemblers, ${HOUR_END.turrets} turrets, ${HOUR_END.held} Held — three claims, D-HOUR-3)`, endOk.every(Boolean), `${endOk.filter(Boolean).length}/${endOk.length} runs`));
    checks.push(isTrue(`D-HOUR-3: north claimed at ${NORTH_AT / 60}:00 is Held at ${MIN}:00 on every run`, northOk.every(Boolean), `${northOk.filter(Boolean).length}/${northOk.length} runs`));
    checks.push(isTrue('RI-01 (D-P4-12, M6 (a)): the rail yard\'s coal at the Depot ≥ 10 min before the chest\'s coal is gone (worst rifle-off seed)', worstMargin >= 600, worstMargin === -Infinity ? 'never arrived' : `${n1(worstMargin / 60)} min`));
    const worstOff = Math.max(...Object.values(ledgers).flatMap(L => Object.values(L.unexplained).map(Math.abs)));
    checks.push(isTrue('RI-01: every item conserved on every run (ledger.ts, tolerance 0.01)', conserved.every(Boolean),
      conserved.every(Boolean) ? `${conserved.length} runs, worst unexplained ${worstOff.toFixed(3)} of an item` : Object.entries(ledgers).filter(([, L]) => !L.ok).map(([k, L]) => `${k}: ${L.problems.join('; ')}`).join(' | ')));
    checks.push(within('§19: walking at most 15 % of the hour (worst seed)', Math.max(...walkedPct), 0, 15, ' %'));
    checks.push(within('rework: the claim walk-overs under a minute of hour one (worst seed)', Math.max(...claimWalk), 0, 60, ' s'));
    checks.push(isTrue(`the HQ stands at ${MIN}:00 on every seed`, hqHeld.every(Boolean), `${hqHeld.filter(Boolean).length}/${hqHeld.length} runs`));
    return { id: 'E-hour', title: EHOUR.title, pyNames: [], docRefs: ['§11', '§19', 'calibration'], setup: `river city, seeds ${seeds.join('/')}, hour bot, rifle off and on, ${MIN} min (constants.HOUR)`, sections, checks, data };
  },
};
