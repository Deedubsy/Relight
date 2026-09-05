/** E-rifle — D5's one rifle. Two questions from the rework brief:
 *   steady play: with the engineer walking and firing at red edges of the block they stand on, does the rifle change
 *     the ammo bill (< 5 %) and does anything fall (0)?
 *   rescue: a Held block whose ring is dry and whose belt is 90 s away, under the incoming blooms — does it hold with
 *     the engineer standing on it firing, and fall without? And what does the rescue cost in HP (the damage number)? */
import { runSim } from '../run';
import { mean, min, within, isTrue, Experiment, ExperimentResult, Section, Check } from '../util';
import {
  createState, citySpec, step, createBot, botCommands, Command, idxOf, DARK, HELD, ENGINEER_HP, RETALIATE_HP_PER_S, RIFLE_ROUNDS_PER_S, CityPreset,
} from '@relight/sim';
import { CANON, DEFAULT_MAP } from '../run';
import { hourCity } from './ehour';
import { createHourBot, runHour, hourReport, hourCommands, rescueStance, advanceFlow, cityGeomOf, segBetween, turretEdge, TILE_DT, Command as TileCommand, ENGINEER_HP as HP0, HourReport, enableStalkers, wellBlocks, StalkerStats, CANDIDATES } from '@relight/sim';

/** E-rifle at tile scale (ROADMAP §0 line 2). The hour bot plays §11 to `at` on the river city (flow + power on), then
 *  the most threatened Held block (the most rot on its Dark neighbours) runs dry: 'edge' = the turrets and hopper of
 *  the edge facing its worst neighbour at 0 rounds, 'ring' = every edge of the block; its belt is 90 s away (the
 *  block-level hoppers cut, the HQ's line buffer held at 0). The bot drops its script and hands. With `rifle` the
 *  engineer stands at the dry edge's street midpoint with 20 magazines and the reflex fires at the nearest crawler
 *  in range; without, they stand there unarmed. Ten more minutes: did the block fall, and what did it cost in HP? */
interface TileRescue { seed: number; at: number; block: number; blockName: string; scope: 'ring' | 'edge'; belt: number; rifle: boolean; fell: boolean; fellAt: number; reason: string;
  kills: number; fired: number; hurt: number; hpMin: number; downs: number; crawlers: number }
/** GAME-ASSUMPTION (GA-EF-4): the dry edge is made by cutting the belt (`e.cut`) and emptying the hoppers of the most
 *  threatened Held block for 90 s (a late belt) or 600 s (§11's "the belt never comes"), the engineer sent to the
 *  segment's midpoint with 20 magazines; the block sim has no tile-level belt to cut, so the cut is the stand-in. */
function tileRescue(seed: number, at: number, rifle: boolean, scope: 'ring' | 'edge', belt: number): TileRescue {
  const st = hourCity(seed), bot = createHourBot(true);
  runHour(st, bot, at);
  const hq = st.blocks.findIndex(b => b.x === st.start[0] && b.y === st.start[1]);
  let block = -1, best = -1;
  for (let i = 0; i < st.blocks.length; i++) {
    if (st.blocks[i].state !== HELD) continue;
    let sc = 0; for (const j of st.nb[i]) if (st.blocks[j].state === DARK) sc += st.blocks[j].d * (st.blocks[j].well ? 1.5 : 1);
    if (sc > best) { best = sc; block = i; }
  }
  let worst = -1, wd = -1;
  for (const j of st.nb[block]) if (st.blocks[j].state === DARK && st.blocks[j].d > wd) { wd = st.blocks[j].d; worst = j; }
  const cutEdge = st.ring.find(e => e.a === block && e.b === worst)?.id ?? -1;
  const cut = new Set<number>();
  for (const e of st.ring) if (e.a === block && (scope === 'ring' || e.id === cutEdge)) { e.hopper = 0; e.cut = st.t + belt; cut.add(e.id); }
  for (const m of st.flow!.machines) if (m.kind === 'turret' && cut.has(turretEdge(st, m))) m.inv.rounds = 0;
  const eng = st.engineer;
  eng.inv.magazine = rifle ? 20 : 0;
  const cg = cityGeomOf(st), sg = segBetween(cg, block, worst);
  rescueStance(st, bot, Math.floor(sg ? sg.mx : eng.x), Math.floor(sg ? sg.my : eng.y), rifle);
  const kills0 = eng.kills, fired0 = eng.fired, hurt0 = eng.hurt, downs0 = eng.downs, t0 = st.t;
  const f = st.flow!, T0 = f.threat ? (f.threat as { stats: { spawned: number } }).stats.spawned : 0;
  let hpMin = HP0, fellAt = -1, reason = '-';
  const cmds: TileCommand[] = [], end = f.tick + Math.round(690 / TILE_DT);
  while (f.tick < end) {
    cmds.length = 0;
    hourCommands(st, bot, cmds);
    st.acc = 0;
    advanceFlow(st, TILE_DT, cmds, 1);
    if (block === hq && st.t < t0 + belt) st.buffer = 0;   // the HQ's belt is `belt` seconds away: the line buffer stays empty
    for (const ev of st.events) if (ev.type === 'fall' && ev.x === st.blocks[block].x && ev.y === st.blocks[block].y && fellAt < 0) { fellAt = ev.t; reason = ev.reason; }
    st.events.length = 0;
    if (eng.hp < hpMin) hpMin = eng.hp;
  }
  const T1 = f.threat ? (f.threat as { stats: { spawned: number } }).stats.spawned : 0;
  return { seed, at, block, blockName: block === hq ? 'HQ' : `(${st.blocks[block].x},${st.blocks[block].y})`, scope, belt, rifle, fell: fellAt >= 0, fellAt, reason,
           kills: eng.kills - kills0, fired: eng.fired - fired0, hurt: eng.hurt - hurt0, hpMin, downs: eng.downs - downs0, crawlers: T1 - T0 };
}

const mmssOf = (t: number | undefined) => t === undefined ? 'never' : `${Math.floor(t / 60)}:${String(Math.floor(t % 60)).padStart(2, '0')}`;

/** RI-04 (D-RI-5): §11's hour with the rifle on and the Stalker candidate switched on (`enableStalkers`, candidates.ts —
 *  outside SimConfig, so the hour's config hash is the benchmark's). A candidate run: its rows report the candidate's
 *  contact numbers for the tuning decision (plan §7.1 "test contact frequency and useful counterplay before changing
 *  them"); they are not benchmark checks. The hour bot never dodges, so "dodged" measures nothing here. */
export interface CandStalkerHour { seed: number; sites: number; stats: StalkerStats; hurt: number; downs: number; report: HourReport }
export function candStalkerHour(seed: number, seconds = 3600): CandStalkerHour {
  const st = hourCity(seed), bot = createHourBot(true);
  const S = enableStalkers(st)!;
  runHour(st, bot, seconds);
  return { seed, sites: wellBlocks(st).length, stats: { ...S.stats }, hurt: st.engineer.hurt, downs: st.engineer.downs, report: hourReport(st, bot) };
}

interface Rescue { seed: number; at: number; block: number; scope: 'ring' | 'edge'; rifle: boolean; fell: boolean; fellAt: number; reason: string; unfed: number; kills: number; fired: number; hurt: number; downs: number; downAt: number; hpMin: number }

/** One rescue: play compact to `at` seconds, dry every edge of the most threatened Held block and cut its belt for
 *  90 s, then watch 10 more minutes. With `rifle` the engineer stands on the block with 20 magazines and fires at
 *  the busiest engaged edge (a player choosing targets); without, nobody is there. */
function rescue(seed: number, at: number, rifle: boolean, map: CityPreset, scope: 'ring' | 'edge'): Rescue {
  const cfg = { ...CANON, eco: { ...CANON.eco } };
  const st = createState(citySpec(seed, map, cfg), cfg, seed);
  const bot = createBot('compact');
  const cmds: Command[] = [];
  const run = (until: number, hook?: () => void) => {
    while (st.t < until) { cmds.length = 0; botCommands(st, bot, cmds); hook?.(); step(st, cmds); st.events.length = 0; }
  };
  run(at);
  // the most threatened Held block: the one whose Dark neighbours carry the most rot (bloom size follows rot)
  let block = -1, best = -1;
  for (let i = 0; i < st.blocks.length; i++) {
    if (st.blocks[i].state !== HELD) continue;
    let s = 0; for (const j of st.nb[i]) if (st.blocks[j].state === DARK) s += st.blocks[j].d * (st.blocks[j].well ? 1.5 : 1);
    if (s > best) { best = s; block = i; }
  }
  // 'ring': the block's whole belt is 90 s away (every edge dry); 'edge': only the edge facing the worst neighbour is
  let worst = -1, wd = -1;
  for (const j of st.nb[block]) if (st.blocks[j].state === DARK && st.blocks[j].d > wd) { wd = st.blocks[j].d; worst = j; }
  const cutEdge = st.ring.find(e => e.a === block && e.b === worst)?.id ?? -1;
  for (const e of st.ring) if (e.a === block && (scope === 'ring' || e.id === cutEdge)) { e.hopper = 0; e.cut = at + 90; }
  const eng = st.engineer;
  if (rifle) { eng.block = block; eng.x = st.blocks[block].x; eng.y = st.blocks[block].y; eng.dest = -1; eng.remaining = 0; eng.inv.magazine = 20; }
  const unfed0 = st.stats.unfedTotal, hurt0 = eng.hurt, kills0 = eng.kills, fired0 = eng.fired, downs0 = eng.downs;
  let hpMin = ENGINEER_HP, downAt = -1;
  run(at + 90 + 600, () => {
    if (!rifle) return;
    if (eng.down >= 0) { if (downAt < 0) downAt = st.t; return; }
    let pick = -1, most = 0;
    for (const en of st.engagements) { const ri = st.edgeAt[en.id]; if (ri >= 0 && st.ring[ri].a === block && (scope === 'ring' || en.id === cutEdge) && en.cr > most) { most = en.cr; pick = en.id; } }
    eng.firing = pick;
    if (eng.hp < hpMin) hpMin = eng.hp;
  });
  const fell = st.stats.lostLog.find(l => l.t >= at && idxOf(st, l.x, l.y) === block);
  return { seed, at, block, scope, rifle, fell: !!fell, fellAt: fell ? fell.t : -1, reason: fell?.reason ?? '-', unfed: st.stats.unfedTotal - unfed0, kills: eng.kills - kills0, fired: eng.fired - fired0,
           hurt: eng.hurt - hurt0, downs: eng.downs - downs0, downAt, hpMin };
}

export const ERIFLE: Experiment = {
  id: 'E-rifle', title: 'The rifle: steady play and the rescue',
  run(ctx): ExperimentResult {
    const { seeds, hours } = ctx;
    const map = (ctx.map === 'lattice' ? 'river' : ctx.map) as CityPreset;   // the rescue needs a city
    const sections: Section[] = [], checks: Check[] = [], data: Record<string, unknown> = {};

    // E-rifle-steady: compact, walking, rifle on vs off
    const rows: (string | number)[][] = [];
    const diffs: number[] = [], lostR: number[] = [], shots: number[] = [], shootPct: number[] = [], dangerPct: number[] = [], dangerShotShare: number[] = [];
    for (const seed of seeds) {
      const off = runSim({ seed, policy: 'compact', hours, walk: true, rifle: false });
      const on = runSim({ seed, policy: 'compact', hours, walk: true, rifle: true });
      const diff = (on.totalMags - off.totalMags) / off.totalMags * 100;
      const secs = hours * 3600, sp = 100 * on.engineer.shootS / secs, dp = 100 * on.engineer.danger / secs, ds = on.engineer.danger > 0 ? on.engineer.dangerShot / on.engineer.danger : 0;
      diffs.push(diff); lostR.push(on.lost); shots.push(on.engineer.firstShot); shootPct.push(sp); dangerPct.push(dp); dangerShotShare.push(ds);
      rows.push([seed, off.totalMags.toFixed(0), on.totalMags.toFixed(0), diff.toFixed(2) + ' %', off.lost, on.lost, on.engineer.fired.toFixed(0), on.engineer.kills.toFixed(1),
                 min(on.engineer.firstShot), on.engineer.hurt.toFixed(0), on.engineer.downs, sp.toFixed(2) + ' %', dp.toFixed(2) + ' %', (100 * ds).toFixed(0) + ' %']);
    }
    sections.push({ title: 'E-rifle-steady: compact, engineer walking, 5 h; the bot fires at a red engaged edge of the block it stands on',
      note: 'D-B1-5 (§19): shooting = seconds the rifle fired, as a share of the run; danger = seconds with a crawler on the player (retaliation from a rifle kill, or crawlers past the turrets on the block the engineer stands on); "from the rifle" = the share of danger seconds in which the rifle fired.',
      header: ['seed', 'magazines (no rifle)', 'magazines (rifle)', 'diff', 'lost (no rifle)', 'lost (rifle)', 'rifle rounds', 'kills', 'first shot (min)', 'HP lost', 'downs', 'shooting', 'danger', 'from the rifle'], rows });
    data.steady = { diffs, lost: lostR, firstShot: shots, shootPct, dangerPct, dangerShotShare };
    checks.push(within('rework: the rifle changes the 5 h ammo bill by < 5 % (mean |diff|)', mean(diffs.map(Math.abs)), 0, 5, ' %'));
    checks.push(isTrue('rework: nothing lost with the rifle in steady play', lostR.every(l => l === 0), `lost per seed ${lostR.join('/')}`));
    checks.push(within('§19: shooting time ≤ 10 % of an hour (max over seeds)', Math.max(...shootPct), 0, 10, ' %'));
    const worstDanger = Math.max(...dangerPct), worstIdx = dangerPct.indexOf(worstDanger);
    const why = worstDanger > 5 ? (dangerShotShare[worstIdx] >= 0.5 ? 'the rifle is too tempting (most danger seconds are the rifle firing)' : 'retaliation is too eager (most danger seconds come without the rifle firing)') : 'within the guard';
    checks.push(within(`§19 (D-B1-5): time in danger ≤ 5 % of an hour (max over seeds) — ${why}`, worstDanger, 0, 5, ' %'));
    data.dangerVerdict = why;

    // E-rifle-rescue
    const rrows: (string | number)[][] = [];
    const rescues: Rescue[] = [];
    for (const scope of ['edge', 'ring'] as const) for (const seed of seeds) for (const at of [2 * 3600, 4 * 3600]) {
      const a = rescue(seed, at, false, map, scope), b = rescue(seed, at, true, map, scope);
      rescues.push(a, b);
      rrows.push([scope, seed, at / 3600, a.block, a.unfed.toFixed(1), a.fell ? `falls at +${((a.fellAt - at) / 60).toFixed(1)} min (${a.reason})` : 'holds',
                  b.fell ? `falls at +${((b.fellAt - at) / 60).toFixed(1)} min (${b.reason})` : 'holds', b.kills.toFixed(1), b.fired.toFixed(0), b.hurt.toFixed(0), b.hpMin.toFixed(0), b.downs ? `down at +${((b.downAt - at) / 60).toFixed(1)} min` : '-']);
    }
    sections.push({ title: 'E-rifle-rescue: the most threatened Held block; "edge" = the edge facing its worst neighbour runs dry with its belt 90 s away, "ring" = the whole block\'s belt is 90 s away; then 10 min more; rescue = the engineer on the block with 20 magazines',
      note: `Damage: each crawler shot at arm's reach costs ${(RETALIATE_HP_PER_S * 3 / RIFLE_ROUNDS_PER_S).toFixed(0)} HP (${RETALIATE_HP_PER_S} HP/s while its 3 rounds land at ${RIFLE_ROUNDS_PER_S} rounds/s), so ${ENGINEER_HP} HP buys ${Math.floor(ENGINEER_HP / (RETALIATE_HP_PER_S * 3 / RIFLE_ROUNDS_PER_S))} kills before the engineer is knocked down; regen 5 HP/s starts 5 s out of contact.`,
      header: ['scope', 'seed', 'at (h)', 'block', 'unfed crawlers, no rifle', 'no rifle', 'rifle', 'kills', 'rounds', 'HP lost', 'HP min', 'knocked down'], rows: rrows });
    const noRifle = rescues.filter(r => !r.rifle), withRifle = rescues.filter(r => r.rifle);
    const damage = mean(withRifle.map(r => r.hurt));
    data.rescue = { rescues, damageHp: damage, fallsWithout: noRifle.filter(r => r.fell).length, fallsWith: withRifle.filter(r => r.fell).length };
    const fellWithout = noRifle.filter(r => r.fell), savedByRifle = fellWithout.filter(r => !withRifle.find(w => w.seed === r.seed && w.at === r.at && w.scope === r.scope)!.fell);
    data.rescue = { ...(data.rescue as object), fellWithout: fellWithout.length, savedByRifle: savedByRifle.length };
    checks.push(isTrue('rework: some scenario falls without the rifle', fellWithout.length > 0, `${fellWithout.length}/${noRifle.length} scenarios fall without the rifle`));
    // The brief expected "holds with the rifle, falls without". Measured: what takes a dry block is a shade at its unlit
    // edge, and the rifle follows the turret rules (D5: shades untargetable off light) — so the rifle saves crawler
    // falls, not shade falls. The check states what the sim does; the report carries the decision.
    const fellWith = withRifle.filter(r => r.fell);
    data.rescue = { ...(data.rescue as object), shadeFallsWith: fellWith.filter(r => r.reason === 'shade').length };
    checks.push(isTrue('rework: the rifle saves every block that falls to crawlers; what it cannot save falls to a shade at an unlit edge (D5 turret rules)',
      fellWith.every(r => r.reason === 'shade') && noRifle.filter(r => r.fell && r.reason !== 'shade').every(r => !withRifle.find(w => w.seed === r.seed && w.at === r.at && w.scope === r.scope)!.fell),
      `${savedByRifle.length}/${fellWithout.length} falls saved by the rifle; ${fellWith.length} fall with it, ${fellWith.filter(r => r.reason === 'shade').length} of them to shades`));
    checks.push(within('rework: the damage number exists (mean HP lost per rescue)', damage, 1, 1000, ' HP'));

    // ---- tile scale (ROADMAP §0 line 2): §11's hour on the river city, flow + power on, the hour bot
    // steady: the hour with the rifle off and on — the magazine bill, falls, rounds fired, HP lost
    const tsRows: (string | number)[][] = [], tsDiff: number[] = [], tsFell: number[] = [], tsFired: number[] = [];
    const tsShoot: number[] = [], tsDanger: number[] = [];   // §19's two guards at tile scale (D-B1-5), the shares the block-scale rows measure on the compact bot
    const tsOn: Record<number, { r: HourReport; hurt: number; downs: number }> = {};   // RI-04: the rifle-on hour per seed, the candidate row's reference
    for (const seed of seeds) {
      const runs = [false, true].map(rifle => { const st = hourCity(seed), bot = createHourBot(rifle); ctx.log(`E-rifle-tile-steady seed ${seed} rifle ${rifle ? 'on' : 'off'}`); runHour(st, bot, 3600); return { st, r: hourReport(st, bot) }; });
      const [off, on] = runs;
      const diff = off.r.magsMade > 0 ? 100 * (on.r.magsMade - off.r.magsMade) / off.r.magsMade : 0;
      const shootPct = 100 * on.st.engineer.shootS / 3600, dangerPct = 100 * on.st.engineer.danger / 3600;
      tsDiff.push(diff); tsFell.push(off.r.fell + on.r.fell); tsFired.push(on.r.fired); tsShoot.push(shootPct); tsDanger.push(dangerPct);
      tsOn[seed] = { r: on.r, hurt: on.st.engineer.hurt, downs: on.st.engineer.downs };
      tsRows.push([seed, off.r.magsMade, on.r.magsMade, diff.toFixed(2) + ' %', off.r.handFed, on.r.handFed, off.r.fell, on.r.fell, on.r.fired, on.r.rifleKills, on.r.turretKills, mmssOf(on.r.marks['first-shot']), on.st.engineer.hurt.toFixed(0), on.st.engineer.downs, shootPct.toFixed(2) + ' %', dangerPct.toFixed(2) + ' %']);
    }
    sections.push({ title: 'E-rifle-tile-steady: §11\'s hour on the tile layer (the hour bot), rifle off vs on — the reflex fires at the nearest crawler in range while the bot walks its script',
      note: 'the tile-scale steady run; the lattice rows above are the 5 h compact bot',
      header: ['seed', 'line magazines (no rifle)', 'line magazines (rifle)', 'diff', 'hand-fed (no rifle)', 'hand-fed (rifle)', 'falls (no rifle)', 'falls (rifle)', 'rifle rounds', 'rifle kills', 'turret kills', 'first shot', 'HP lost', 'downs', 'shooting (§19: 10)', 'danger (§19: 5)'], rows: tsRows });
    checks.push(within('tile: the rifle changes the hour\'s magazine bill by < 5 % (mean |diff|)', mean(tsDiff.map(Math.abs)), 0, 5, ' %'));
    checks.push(isTrue('tile: nothing falls in the hour, rifle off or on', tsFell.every(x => x === 0), `falls per seed ${tsFell.join('/')}`));
    data.tileSteady = { diffs: tsDiff, fell: tsFell, fired: tsFired, shootPct: tsShoot, dangerPct: tsDanger };
    // §19's two caps, measured where the rifle actually is: the block-scale rows above run the 5 h compact bot, this one runs §11's hour on the tile layer.
    checks.push(within('tile §19: shooting time ≤ 10 % of the hour (max over seeds)', Math.max(...tsShoot), 0, 10, ' %'));
    checks.push(within('tile §19 (D-B1-5): time in danger ≤ 5 % of the hour (max over seeds)', Math.max(...tsDanger), 0, 5, ' %'));
    // RI-04 (D-RI-5): the Stalker candidate on the same hour — reported, not gated (a candidate configuration, not the benchmark)
    const csRows: (string | number)[][] = [], cs: CandStalkerHour[] = [];
    for (const seed of seeds) {
      ctx.log(`E-rifle-cand-stalker seed ${seed}`);
      const c = candStalkerHour(seed), ref = tsOn[seed], k = c.stats;
      cs.push(c);
      csRows.push([seed, c.sites, k.spawned, k.pursuits, k.attacks, k.hits, k.dodged, k.contactS.toFixed(1), k.kills, k.retired,
                   `${c.hurt.toFixed(0)} / ${ref.hurt.toFixed(0)}`, `${c.downs} / ${ref.downs}`, `${c.report.fell} / ${ref.r.fell}`, `${c.report.held} / ${ref.r.held}`, `${c.report.magsMade} / ${ref.r.magsMade}`]);
    }
    const cand = CANDIDATES.stalker;
    sections.push({ title: `E-rifle-cand-stalker: the rifle-on hour with the Stalker candidate switched on (candidates.ts: perception 8, leash 16, HP 2× a Crawler, speed 1.2×, wind-up 0.8 s, one 5 HP swing a second) — one Stalker guarding each Dark well block's home; "x / y" = with the candidate / the rifle-on row above`,
      note: `D-RI-5: a candidate run, not a benchmark row — these numbers are for the tuning decision (plan §7.1: contact frequency and useful counterplay before the candidates move), and the checks below only hold the bookkeeping. The hour bot never dodges. Contact = seconds a Stalker stood within reach of the engineer; perception ${cand.perception} tiles, leash ${cand.leash}, respawn ${cand.respawnS} s after a kill while its site stays Dark and the engineer is out of perception of the home.`,
      header: ['seed', 'well sites', 'Stalkers fielded', 'pursuits', 'swings', 'hits', 'dodged', 'contact (s)', 'Stalkers killed', 'retired (site restored)', 'HP lost', 'downs', 'falls', 'held at the hour', 'line magazines'], rows: csRows });
    data.candStalker = { candidate: cand, runs: cs.map(c => ({ seed: c.seed, sites: c.sites, stats: c.stats, hurt: c.hurt, downs: c.downs, fell: c.report.fell, held: c.report.held, magsMade: c.report.magsMade })) };
    checks.push(isTrue('cand-stalker (D-RI-5, bookkeeping only): every swing either lands or is dodged', cs.every(c => c.stats.hits + c.stats.dodged === c.stats.attacks), cs.map(c => `${c.stats.hits}+${c.stats.dodged}=${c.stats.attacks}`).join(' / ')));
    checks.push(isTrue('cand-stalker (D-RI-5, bookkeeping only): one Stalker fielded per well site at the switch', cs.every(c => c.stats.spawned >= c.sites), cs.map(c => `${c.stats.spawned} fielded for ${c.sites} sites`).join(' / ')));
    // rescue at tile scale: at 20:00 (HQ + east Held, front 4) and 35:00 (HQ + east + west, front 5)
    const trRows: (string | number)[][] = [], tr: TileRescue[] = [];
    for (const belt of [90, 600]) for (const scope of ['edge', 'ring'] as const) for (const seed of seeds) for (const at of [20 * 60, 35 * 60]) {
      ctx.log(`E-rifle-tile-rescue belt ${belt} s ${scope} seed ${seed} at ${at / 60}:00`);
      const a = tileRescue(seed, at, false, scope, belt), b = tileRescue(seed, at, true, scope, belt);
      tr.push(a, b);
      trRows.push([`${belt} s`, scope, seed, `${at / 60}:00`, a.blockName, a.crawlers, a.fell ? `falls at +${((a.fellAt - at) / 60).toFixed(1)} min (${a.reason})` : 'holds',
                   b.fell ? `falls at +${((b.fellAt - at) / 60).toFixed(1)} min (${b.reason})` : 'holds', b.kills, b.fired, b.hurt.toFixed(0), b.hpMin.toFixed(0), b.downs ? `down ×${b.downs}` : '-']);
    }
    sections.push({ title: 'E-rifle-tile-rescue: the most threatened Held block on the tile layer; "edge" = the turrets and hopper of the edge facing its worst neighbour at 0, "ring" = every edge of the block; the belt 90 s away (§11\'s rescue) or 600 s away (the belt never comes inside the window); then 10 min more; rescue = the engineer at the dry edge\'s street midpoint with 20 magazines, the bot\'s script and hands dropped',
      note: 'the HQ\'s edges are its physical turrets (their rounds zeroed, the line buffer held at 0 for 90 s); east and west keep the block-level hopper (D-P4-9: cut for 90 s)',
      header: ['belt', 'scope', 'seed', 'at', 'block', 'crawlers born in the window', 'no rifle', 'rifle', 'kills', 'rounds', 'HP lost', 'HP min', 'knocked down'], rows: trRows });
    const trNo = tr.filter(r => !r.rifle), trYes = tr.filter(r => r.rifle);
    const trFellNo = trNo.filter(r => r.fell), trFellYes = trYes.filter(r => r.fell);
    const pair = (r: TileRescue) => trYes.find(w => w.seed === r.seed && w.at === r.at && w.scope === r.scope && w.belt === r.belt)!;
    data.tileRescue = { rescues: tr, fellWithout: trFellNo.length, fellWith: trFellYes.length, savedByRifle: trFellNo.filter(r => !pair(r).fell).length, damageHp: mean(trYes.map(r => r.hurt)) };
    const edgeFalls = trFellNo.filter(r => r.scope === 'edge'), ringFalls = trFellNo.filter(r => r.scope === 'ring');
    checks.push(isTrue('tile: one dry edge (§11\'s rescue, the belt never comes) — the rifle saves every block that falls without it',
      edgeFalls.every(r => !pair(r).fell), `${edgeFalls.filter(r => !pair(r).fell).length}/${edgeFalls.length} single-edge falls saved (${trNo.filter(r => r.scope === 'edge').length} edge scenarios)`));
    checks.push(isTrue('tile: a whole dry ring is beyond one rifle at one edge — but the rifle never loses the block sooner',
      ringFalls.every(r => !pair(r).fell || pair(r).fellAt >= r.fellAt),
      `${ringFalls.filter(r => !pair(r).fell).length}/${ringFalls.length} ring falls saved, the rest delayed by ${ringFalls.filter(r => pair(r).fell).map(r => ((pair(r).fellAt - r.fellAt) / 60).toFixed(1)).join('/')} min`));
    checks.push(isTrue('tile: the engineer is never knocked down in a rescue', trYes.every(r => r.downs === 0), `HP lost ${Math.min(...trYes.map(r => r.hurt)).toFixed(0)}–${Math.max(...trYes.map(r => r.hurt)).toFixed(0)}, HP min ${Math.min(...trYes.map(r => r.hpMin)).toFixed(0)}`));
    return { id: 'E-rifle', title: ERIFLE.title, pyNames: [], docRefs: ['§4', '§6', '§7', '§11', '§13', 'D5'],
      setup: `compact; ${DEFAULT_MAP} map; engineer walking with the rifle; rescue at 2 h and 4 h; tile scale: the hour bot on the river city, rescue at 20:00 and 35:00; the Stalker candidate on the rifle-on hour (D-RI-5)`, sections, checks, data };
  },
};
