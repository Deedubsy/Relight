/** Calibration harness (CALIBRATION_REPORT.md). Runs the proto's config with the three bots on the scattered map,
 *  seeds 3/4/5, economy on, 3 h sim, and scores targets T1–T7 from the sim's own state (not the Python sim).
 *
 *    npx tsx packages/sim/test/calibrate.ts '{"startAsmRate":10,"eco":{"yieldPerMin":4}}' [--hours 3] [--seeds 3,4,5]
 *                                            [--bots compact,spike,cheapest] [--economy 0] [--build 0] [--react 1] [--out file.json]
 *
 *  --build 0  the bots never build an assembler (a tester who ignores the HUD).
 *  --react 1  harness-only stand-in for a tester who builds when a pip first leaves green: implies --build 0; adds an
 *             assembler when the worst pip is amber or red, the stock affords it, and none was added in the last 10 min.
 *
 *  Overrides are applied on top of protoCalibrated(DEFAULT_CONFIG) so a lever can be tried without editing types.ts. */
import { writeFileSync } from 'node:fs';
import {
  DEFAULT_CONFIG, SimConfig, protoCalibrated, configHash, generateMap, createState, step, takeEvents,
  createBot, botCommands, Command, Policy, heldCount, frontage, interior, ammoStatus, pipOf, clockOf, slotInfo, asmCount,
} from '../src/index';

const argv = process.argv.slice(2);
const flag = (name: string, dflt: string): string => { const i = argv.indexOf(name); return i >= 0 && i + 1 < argv.length ? argv[i + 1] : dflt; };
const overridesArg = argv.find(a => a.startsWith('{')) ?? '{}';
const HOURS = Number(flag('--hours', '3'));
const SEEDS = flag('--seeds', '3,4,5').split(',').map(Number);
const BOTS = flag('--bots', 'compact,spike,cheapest').split(',') as Policy[];
const ECONOMY = flag('--economy', '1') !== '0';
const REACT = flag('--react', '0') !== '0';
const BUILD = !REACT && flag('--build', '1') !== '0';
const OUT = flag('--out', '');

function merge(base: Record<string, unknown>, over: Record<string, unknown>): Record<string, unknown> {
  const out: Record<string, unknown> = { ...base };
  for (const k of Object.keys(over)) {
    const b = base[k], o = over[k];
    out[k] = b && o && typeof b === 'object' && typeof o === 'object' && !Array.isArray(b) ? merge(b as Record<string, unknown>, o as Record<string, unknown>) : o;
  }
  return out;
}

const baseConfig = protoCalibrated({ ...DEFAULT_CONFIG, scatter: true, economy: ECONOMY });
const config = merge(baseConfig as unknown as Record<string, unknown>, JSON.parse(overridesArg)) as unknown as SimConfig;
const HASH = configHash(config);

interface Run {
  bot: Policy; seed: number;
  firstAmber: number | null; firstRed: number | null; firstAmberAfter10: number | null; firstRedAfter10: number | null;
  firstLoss: number | null; firstEnclosure: number | null;
  lost: number; stalls: number; firstStall: number | null; claims: number;
  held: Record<number, number>; front180: number; interior180: number;
  copper180: number; steel180: number; minSteel: number; minSteelAt: number; patchOutAt: number | null;
  assemblers: number[]; production180: number; peakDemand: number; peakDemandAt: number;
  mags: number; magsMade: number; magsPerHeld: number; amberTicks: number; redTicks: number;
  slots: Record<number, { used: number; free: number; dry: number }>;   // at 60/120/180 min
  maxAsmBefore120: number; machinesLost: number; distinctLost: number; dryLog: { t: number; x: number; y: number; district: string }[];
}

function run(bot: Policy, seed: number): Run {
  const spec = generateMap(seed, config);
  const st = createState(spec, config, seed);
  const b = createBot(bot, null, BUILD);
  const cmds: Command[] = [];
  const r: Run = {
    bot, seed, firstAmber: null, firstRed: null, firstAmberAfter10: null, firstRedAfter10: null, firstLoss: null, firstEnclosure: null,
    lost: 0, stalls: 0, firstStall: null, claims: 0, held: {}, front180: 0, interior180: 0, copper180: 0, steel180: 0,
    minSteel: Infinity, minSteelAt: 0, patchOutAt: null, assemblers: [], production180: 0, peakDemand: 0, peakDemandAt: 0,
    mags: 0, magsMade: 0, magsPerHeld: 0, amberTicks: 0, redTicks: 0,
    slots: {}, maxAsmBefore120: 0, machinesLost: 0, distinctLost: 0, dryLog: [],
  };
  const ticks = Math.round(HOURS * 3600);
  const cap = config.hopper;
  let lastReact = -Infinity;
  for (let k = 0; k < ticks; k++) {
    cmds.length = 0;
    botCommands(st, b, cmds);
    if (REACT && config.economy && st.t > 0 && st.t % 60 === 0 && st.t - lastReact >= 600) {
      let w = 1;
      for (const e of st.ring) { const l = e.hopper / cap; if (l < w) w = l; }
      const ac = config.eco.assemblerCost;
      if (pipOf(w) !== 'green' && st.stock.copper >= ac.copper && st.stock.steel >= ac.steel) { cmds.push({ type: 'addAssembler' }); lastReact = st.t; }
    }
    step(st, cmds);
    for (const ev of takeEvents(st)) {
      if (ev.type === 'claim-rejected' && ev.reason.startsWith('cannot afford')) { r.stalls++; if (r.firstStall === null) r.firstStall = ev.t; }
      if (ev.type === 'assembler') r.assemblers.push(ev.t);
    }
    // pips
    let worst = 1;
    for (const e of st.ring) { const l = e.hopper / cap; if (l < worst) worst = l; }
    const p = pipOf(worst);
    if (p !== 'green') { r.amberTicks++; if (r.firstAmber === null) r.firstAmber = st.t; if (st.t >= 600 && r.firstAmberAfter10 === null) r.firstAmberAfter10 = st.t; }
    if (p === 'red') { r.redTicks++; if (r.firstRed === null) r.firstRed = st.t; if (st.t >= 600 && r.firstRedAfter10 === null) r.firstRedAfter10 = st.t; }
    if (st.stock.steel < r.minSteel) { r.minSteel = st.stock.steel; r.minSteelAt = st.t; }
    if (r.patchOutAt === null && st.patch.steel <= 0 && config.economy) r.patchOutAt = st.t;
    if (st.t % 60 === 0) {
      const am = ammoStatus(st);
      if (am.demandMagPerMin > r.peakDemand) { r.peakDemand = am.demandMagPerMin; r.peakDemandAt = st.t; }
      if (st.t % 1800 === 0) r.held[st.t / 60] = heldCount(st);
      if (st.t % 3600 === 0) { const si = slotInfo(st); r.slots[st.t / 60] = { used: si.used, free: si.free, dry: si.dry }; }
      if (st.t < 7200) r.maxAsmBefore120 = Math.max(r.maxAsmBefore120, asmCount(st, st.t));
    }
  }
  r.firstLoss = st.stats.firstFall >= 0 ? st.stats.firstFall : null;
  r.firstEnclosure = st.stats.firstInterior >= 0 ? st.stats.firstInterior : null;
  r.lost = st.stats.lost; r.claims = st.stats.claims;
  r.front180 = frontage(st); r.interior180 = interior(st);
  r.copper180 = st.stock.copper; r.steel180 = st.stock.steel;
  r.production180 = ammoStatus(st).productionMagPerMin;
  r.mags = st.totalRounds / 10; r.magsMade = st.stats.magsMade;
  const h = heldCount(st); r.magsPerHeld = h ? r.mags / h : 0;
  r.machinesLost = st.stats.machinesLost; r.dryLog = st.stats.dryLog.slice();
  r.distinctLost = new Set(st.stats.lostLog.map(l => l.x * 1000 + l.y)).size;
  return r;
}

const m = (t: number | null) => t === null ? 'never' : (t / 60).toFixed(0) + 'm';
const runs: Run[] = [];
for (const bot of BOTS) for (const seed of SEEDS) runs.push(run(bot, seed));

console.log(`config ${HASH}  economy=${ECONOMY ? 1 : 0} build=${BUILD ? 1 : 0} react=${REACT ? 1 : 0} hours=${HOURS}  overrides ${overridesArg}`);
console.log('bot      seed amber  amber10 red    red10  loss   encl   lost stalls(first) held60/120/180  front Cu180 steel180 minSteel(at) patchOut asm(built at)          prod180 peakDem(at)   mags  mags/held pressure% slots used/free@60,120,180  dry@60/120/180 mLost');
for (const r of runs) {
  const asm = r.assemblers.length ? r.assemblers.map(t => m(t)).join(',') : '-';
  console.log(
    `${r.bot.padEnd(8)} ${String(r.seed).padEnd(4)} ${m(r.firstAmber).padEnd(6)} ${m(r.firstAmberAfter10).padEnd(7)} ${m(r.firstRed).padEnd(6)} ${m(r.firstRedAfter10).padEnd(6)} ${m(r.firstLoss).padEnd(6)} ${m(r.firstEnclosure).padEnd(6)} ` +
    `${String(r.lost).padEnd(4)} ${(r.stalls + (r.firstStall !== null ? '(' + m(r.firstStall) + ')' : '')).padEnd(13)} ` +
    `${String(r.held[60] ?? 0)}/${String(r.held[120] ?? 0)}/${String(r.held[180] ?? 0)}`.padEnd(15) +
    ` ${String(r.front180).padEnd(5)} ${r.copper180.toFixed(0).padEnd(5)} ${r.steel180.toFixed(0).padEnd(8)} ${(r.minSteel.toFixed(0) + '(' + m(r.minSteelAt) + ')').padEnd(12)} ${m(r.patchOutAt).padEnd(8)} ` +
    `${asm.padEnd(22)} ${r.production180.toFixed(0).padEnd(7)} ${(r.peakDemand.toFixed(1) + '(' + m(r.peakDemandAt) + ')').padEnd(13)} ${r.mags.toFixed(0).padEnd(5)} ${r.magsPerHeld.toFixed(0).padEnd(9)} ${(100 * r.amberTicks / (HOURS * 3600)).toFixed(1).padEnd(9)} ` +
    `${[60, 120, 180].map(k => r.slots[k] ? `${r.slots[k].used}/${r.slots[k].free}` : '-').join(' ').padEnd(27)} ${[60, 120, 180].map(k => r.slots[k] ? r.slots[k].dry : '-').join('/').padEnd(14)} ${r.machinesLost}`);
}

// ---- targets
const by = (bot: Policy) => runs.filter(r => r.bot === bot);
const min = (t: number | null) => t === null ? Infinity : t / 60;
const all = (rs: Run[], f: (r: Run) => boolean) => rs.length > 0 && rs.every(f);
const fmt = (rs: Run[], f: (r: Run) => string) => rs.map(r => `s${r.seed}:${f(r)}`).join(' ');
const compact = by('compact'), spike = by('spike'), cheapest = by('cheapest');
const compactOf = (seed: number) => compact.find(r => r.seed === seed);
const targets: [string, string, boolean, string][] = [
  ['T1', 'compact: first amber 60–120 min', all(compact, r => min(r.firstAmber) >= 60 && min(r.firstAmber) <= 120), fmt(compact, r => m(r.firstAmber))],
  ['T2', 'compact: no red before 120 min', all(compact, r => min(r.firstRed) >= 120), fmt(compact, r => m(r.firstRed))],
  ['T3', 'compact: 0 lost, 0 stalls to 180', all(compact, r => r.lost === 0 && r.stalls === 0), fmt(compact, r => `lost ${r.lost} stalls ${r.stalls}`)],
  ['T4', 'spike: red by 90 min', all(spike, r => min(r.firstRed) <= 90), fmt(spike, r => m(r.firstRed))],
  ['T5', 'spike: 1–4 lost by 180', all(spike, r => r.lost >= 1 && r.lost <= 4), fmt(spike, r => `${r.lost} (${r.distinctLost} distinct)`)],
  ['T6', 'cheapest: no stalls; mags/held 0.7–1.3× compact', all(cheapest, r => { const c = compactOf(r.seed); return r.stalls === 0 && !!c && c.magsPerHeld > 0 && r.magsPerHeld / c.magsPerHeld >= 0.7 && r.magsPerHeld / c.magsPerHeld <= 1.3; }),
    fmt(cheapest, r => { const c = compactOf(r.seed); return `stalls ${r.stalls} ratio ${c && c.magsPerHeld ? (r.magsPerHeld / c.magsPerHeld).toFixed(2) : '?'}`; })],
  ['T7', 'all: held at 120 min in 20–30', all(runs, r => (r.held[120] ?? 0) >= 20 && (r.held[120] ?? 0) <= 30), runs.map(r => `${r.bot[0]}${r.seed}:${r.held[120] ?? 0}`).join(' ')],
  ['T8', 'compact: first enclosure before first amber, ≥ 2 of 3 seeds', compact.length > 0 && compact.filter(r => min(r.firstEnclosure) < min(r.firstAmber)).length >= Math.min(2, compact.length),
    fmt(compact, r => `encl ${m(r.firstEnclosure)} amber ${m(r.firstAmber)}`)],
  ['T9', 'spike: never more than 2 assemblers before 120 min', all(spike, r => r.maxAsmBefore120 <= 2), fmt(spike, r => `max ${r.maxAsmBefore120}`)],
];
console.log('');
for (const [id, what, ok, val] of targets) console.log(`${id} ${ok ? 'MET   ' : 'MISSED'} ${what.padEnd(48)} ${val}`);
if (OUT) writeFileSync(OUT, JSON.stringify({ hash: HASH, overrides: JSON.parse(overridesArg), economy: ECONOMY, build: BUILD, react: REACT, hours: HOURS, config, runs, targets: targets.map(([id, what, ok, val]) => ({ id, what, ok, val })) }, null, 1));
