/** One experiment run: a policy bot on a generated map for N hours, with the per-run bookkeeping the Python
 *  experiments read (frontsim.py `stats`): bloom records, edge-minutes per district, hopper samples, fall delays. */
import {
  SimConfig, Command, DEFAULT_CONFIG, createState, generateMap, step, createBot, botCommands, Policy,
  HourRow, DARK, HELD, District,
} from '@relight/sim';

export type DistrictKey = District | 'well';

export interface RunOpts {
  seed: number; policy: Policy; hours: number;
  cfg?: Partial<SimConfig>;
  claimGap?: number | null;   // fixed claim gap (frontsim.py claim_gap); null = the §18 cadence
  gapAfter?: number;          // claim gap after hour one (default 300 s)
}

export interface BloomRec { t: number; key: DistrictKey; wake: boolean; cr: number; sh: number; hu: number; rounds: number; shells: number }
export interface FallRec { t: number; reason: string; x: number; y: number; key: DistrictKey; delay: number; starved: string }

export interface RunSummary {
  opts: RunOpts;
  hourly: HourRow[];
  totalMags: number; totalShells: number;
  firstInterior: number; firstFall: number; firstUnfed: number; unfedTotal: number;
  lost: number; retakes: number; claims: number;
  held: number; front: number; interior: number; nScatter: number;
  lostLog: FallRec[]; lostByHour: number[];
  ratios: Record<number, [number, number]>;   // hour → [demand mag/min, production mag/min]
  asmAt: Record<number, number>;
  hopperMean: number; minBufferAfter2h: number;
  blooms: BloomRec[];
  edgeMin: Record<string, number>; awakeMin: Record<string, number>;
  power: { demandKw: number[]; supplyKw: number[]; shedLog: number[]; shedEvents: number; firstBrownout: number;
           lostInWindow: number; lostAfterWindow: number };
  wellsDead: number;
  firstShade: number; firstHulk: number; hqFell: number;   // seconds, -1 = never
  wellDeadAt: number[];
  wellHeldAt: number[];   // seconds at which a well block itself became Held (the bot took the well head-on)
  bufferAt: Record<number, number>;                     // rounds in the buffer at each hour mark
  ticks: number;
}

/** The canonical experiment configuration: the fixture config of the regression (scattered 9 % map, wake cap on,
 *  jitter ±10 %, hash hulk roll, production on with 300 starting rounds, unfed = substation N 40). Experiments state
 *  what they change. GAME-ASSUMPTION: the doc's headline numbers refer to this map (E6 measures the open map too). */
export const CANON: SimConfig = { ...DEFAULT_CONFIG, eco: { ...DEFAULT_CONFIG.eco } };

export function runSim(o: RunOpts): RunSummary {
  const cfg: SimConfig = { ...CANON, ...(o.cfg ?? {}), eco: { ...CANON.eco } };
  const spec = generateMap(o.seed, cfg);
  const st = createState(spec, cfg, o.seed);
  const bot = createBot(o.policy, o.claimGap ?? null, false, o.gapAfter ?? 300);
  const cmds: Command[] = [];
  const ticks = Math.round(o.hours * 3600);
  const blooms: BloomRec[] = [], lostLog: FallRec[] = [], lostByHour: number[] = [];
  const edgeMin: Record<string, number> = {}, awakeMin: Record<string, number> = {};
  const ratios: Record<number, [number, number]> = {}, asmAt: Record<number, number> = {};
  let hopSum = 0, hopN = 0, minBuf = Infinity;
  let firstShade = -1, firstHulk = -1, hqFell = -1;
  const wellDeadAt: number[] = [], wellHeldAt: number[] = [], bufferAt: Record<number, number> = {};
  const wellKeys = new Set(spec.wells.map(([x, y]) => x * st.h + y));
  const keyOf = (x: number, y: number): DistrictKey => { const b = st.blocks[x * st.h + y]; return b.well ? 'well' : b.name; };
  for (let k = 0; k < ticks; k++) {
    cmds.length = 0;
    botCommands(st, bot, cmds);
    step(st, cmds);
    const t = st.t - 1;
    for (const ev of st.events) {
      if (ev.type === 'bloom') {
        blooms.push({ t: ev.t, key: keyOf(ev.x, ev.y), wake: ev.wake, cr: ev.cr, sh: ev.sh, hu: ev.hu, rounds: ev.cr * 3 + ev.sh * 10, shells: ev.hu * 10 });
        if (firstShade < 0 && ev.sh > 0) firstShade = ev.t;
        if (firstHulk < 0 && ev.hu > 0) firstHulk = ev.t;
      } else if (ev.type === 'fall') {
        lostLog.push({ t: ev.t, reason: ev.reason, x: ev.x, y: ev.y, key: keyOf(ev.x, ev.y), delay: ev.delay, starved: ev.starved });
        const h = Math.floor(ev.t / 3600); lostByHour[h] = (lostByHour[h] ?? 0) + 1;
        if (ev.x === spec.start[0] && ev.y === spec.start[1]) hqFell = ev.t;
      } else if (ev.type === 'well-dead') {
        wellDeadAt.push(ev.t);
      } else if (ev.type === 'held') {
        if (wellKeys.has(ev.x * st.h + ev.y)) wellHeldAt.push(ev.t);
      } else if (ev.type === 'hour') {
        ratios[ev.row.h] = [ev.row.mags, ev.row.production]; asmAt[ev.row.h] = ev.row.production / Math.max(1e-9, cfg.asmRate);
      }
    }
    st.events.length = 0;
    if (t % 3600 === 0) bufferAt[t / 3600] = st.buffer;
    if (t % 60 === 0) {
      if (cfg.production && st.ring.length) {
        let s = 0; for (const e of st.ring) s += e.hopper; hopSum += s / st.ring.length; hopN++;
      }
      if (t >= 7200) minBuf = Math.min(minBuf, st.buffer);
      if (t >= 3600) {
        for (let i = 0; i < st.blocks.length; i++) {
          const b = st.blocks[i];
          if (b.state === HELD) {
            for (let d = 0; d < 4; d++) {
              const ri = st.edgeAt[i * 4 + d];
              if (ri < 0) continue;
              const n = st.blocks[st.ring[ri].b];
              if (n.state === DARK) { const kk = n.well ? 'well' : n.name; edgeMin[kk] = (edgeMin[kk] ?? 0) + 1; }
            }
          } else if (b.state === DARK && b.awake) { const kk = b.well ? 'well' : b.name; awakeMin[kk] = (awakeMin[kk] ?? 0) + 1; }
        }
      }
    }
  }
  let held = 0;
  for (const b of st.blocks) { if (b.state === HELD) held++; }
  const nScatter = cfg.scatter ? spec.scatteredInert.length : 0;
  const last = st.hourly[st.hourly.length - 1];
  return {
    opts: o, hourly: st.hourly, totalMags: st.totalRounds / 10, totalShells: st.totalShells,
    firstInterior: st.stats.firstInterior, firstFall: st.stats.firstFall, firstUnfed: st.stats.firstUnfed, unfedTotal: st.stats.unfedTotal,
    lost: st.stats.lost, retakes: st.stats.retakes, claims: st.stats.claims,
    held, front: last ? last.front : 0, interior: last ? last.interior : 0, nScatter,
    lostLog, lostByHour, ratios, asmAt,
    hopperMean: hopN ? hopSum / hopN : 0, minBufferAfter2h: minBuf === Infinity ? NaN : minBuf,
    blooms, edgeMin, awakeMin,
    power: { demandKw: st.power.demandKw, supplyKw: st.power.supplyKw, shedLog: st.stats.shedLog, shedEvents: st.stats.shedEvents,
             firstBrownout: st.stats.firstBrownout, lostInWindow: st.stats.lostInWindow, lostAfterWindow: st.stats.lostAfterWindow },
    wellsDead: st.stats.wellsDead,
    firstShade, firstHulk, hqFell, wellDeadAt, wellHeldAt, bufferAt,
    ticks,
  };
}

/** frontsim.py district_report: per district after hour one, steady-state (ss) vs wake blooms. */
export interface DistrictRow {
  d: DistrictKey; nSs: number; nWake: number; shSs: number; huSs: number; shWk: number; huWk: number;
  magEdge: number; magBlock: number; shellEdge: number; wakeRounds: number; ssRounds: number;
}
export const DISTRICT_KEYS: DistrictKey[] = ['civ', 'res', 'ind', 'out', 'well'];
export function districtReport(r: RunSummary): DistrictRow[] {
  const br = r.blooms.filter(b => b.t >= 3600);
  const out: DistrictRow[] = [];
  for (const key of DISTRICT_KEYS) {
    const ss = br.filter(b => b.key === key && !b.wake), wk = br.filter(b => b.key === key && b.wake);
    const em = r.edgeMin[key] ?? 0, am = r.awakeMin[key] ?? 0;
    const mags = ss.reduce((a, b) => a + b.rounds, 0) / 10;
    const frac = (xs: BloomRec[], f: (b: BloomRec) => boolean) => xs.length ? xs.filter(f).length / xs.length : NaN;
    out.push({
      d: key, nSs: ss.length, nWake: wk.length,
      shSs: frac(ss, b => b.sh > 0), huSs: frac(ss, b => b.hu > 0), shWk: frac(wk, b => b.sh > 0), huWk: frac(wk, b => b.hu > 0),
      magEdge: em ? mags / em : NaN, magBlock: am ? mags / am : NaN,
      shellEdge: em ? ss.reduce((a, b) => a + b.shells, 0) / em : NaN,
      wakeRounds: wk.length ? wk.reduce((a, b) => a + b.rounds, 0) / wk.length : NaN,
      ssRounds: ss.length ? ss.reduce((a, b) => a + b.rounds, 0) / ss.length : NaN,
    });
  }
  return out;
}

/** frontsim.py stepped_block: one awake block in isolation stepped with the bloom model; steady-state rot,
 *  mag/min and shells/min over the last two hours of six. E3-block. */
export function steppedBlock(dmax: number, g: number, shadeThr = 0.3, hulkThr = 0.5, base = 4.0, hours = 6): { d: number; magPerMin: number; shellsPerMin: number } {
  let d = 0.5 * dmax, T = 0;
  const rec: [number, number, number, number][] = [];
  for (let t = 0; t < hours * 3600; t++) {
    d += g * (dmax - d); T++;
    if (T >= 120 / (0.5 + d)) {
      const cr = Math.round(base + 36 * d), sh = d >= shadeThr ? Math.floor(cr / 8) : 0, hu = d >= hulkThr ? 0.4 : 0;
      rec.push([t, d, cr * 3 + sh * 10, hu * 10]); d = Math.max(0.05, 0.9 * d); T = 0;
    }
  }
  const tail = rec.filter(r => r[0] > (hours - 2) * 3600);
  const span = tail.length > 1 ? (tail[tail.length - 1][0] - tail[0][0]) / 60 : 1;
  const head = tail.slice(0, -1);
  return { d: tail.reduce((a, r) => a + r[1], 0) / tail.length, magPerMin: head.reduce((a, r) => a + r[2], 0) / 10 / span,
           shellsPerMin: head.reduce((a, r) => a + r[3], 0) / span };
}
