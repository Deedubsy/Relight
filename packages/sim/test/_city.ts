/** Shared by city.test.ts and _exportCity.ts: one city run on the sim's own bot, and the generator's summary
 *  numbers for a seed. Not a test file (no `.test.` in the name). */
import {
  createState, step, createBot, botCommands, Command, Policy, citySpec, generateCity, DEFAULT_CONFIG, SimConfig,
  CityPreset, CityGeom, DISTRICT_NAMES,
} from '../src/index';

export interface CityHour { h: number; held: number; front: number; interior: number; mags: number; shells: number; lost: number }
export interface CityRun {
  hourly: CityHour[];
  totalMags: number; totalShells: number; firstInterior: number; lost: number; retakes: number; claims: number;
  held: number; front: number; interior: number;
  lostLog: { t: number; reason: string; x: number; y: number }[];
  walkedMin: number[];   // engineer minutes walked per hour (walk on; zeros otherwise)
  truckAt: number;       // tick the Tram depot was taken, -1 = never
}

export interface CityStats {
  seed: number; preset: CityPreset; attempt: number; valid: boolean; reasons: string[];
  blocks: number; segs: number; inert: number; maxHops: number;
  hq: number; foundry: number; wells: number[];
  facilities: { name: string; block: number; hops: number }[];
  degrees: Record<string, number>;
  districts: Record<string, number>;
  area: { p10: number; p50: number; p90: number; total: number };
}

export function cityStats(seed: number, preset: CityPreset = 'river'): CityStats {
  const g: CityGeom = generateCity(seed, preset);
  const live = g.blocks.filter(b => !b.inert);
  const degrees: Record<string, number> = {}, districts: Record<string, number> = {};
  for (const b of live) degrees[b.nb.filter(j => !g.blocks[j].inert).length] = (degrees[b.nb.filter(j => !g.blocks[j].inert).length] ?? 0) + 1;
  for (const b of live) districts[DISTRICT_NAMES[g.district[b.id]]] = (districts[DISTRICT_NAMES[g.district[b.id]]] ?? 0) + 1;
  const areas = live.map(b => b.area).sort((a, b) => a - b);
  const q = (p: number) => areas[Math.min(areas.length - 1, Math.floor(p * areas.length))];
  let maxHops = 0; for (let i = 0; i < g.blocks.length; i++) if (g.hops[i] > maxHops) maxHops = g.hops[i];
  return {
    seed, preset, attempt: g.attempt, valid: g.valid, reasons: g.reasons.slice(),
    blocks: g.blocks.length, segs: g.segs.length, inert: g.blocks.length - live.length, maxHops,
    hq: g.hq, foundry: g.foundry, wells: g.wells.slice(),
    facilities: g.facilities.map(f => ({ name: f.name, block: f.block, hops: g.hops[f.block] })),
    degrees, districts, area: { p10: q(0.1), p50: q(0.5), p90: q(0.9), total: areas.reduce((a, b) => a + b, 0) },
  };
}

export function runCity(seed: number, preset: CityPreset, policy: Policy, hours: number, walk = false, cfgIn?: Partial<SimConfig>): CityRun {
  const cfg: SimConfig = { ...DEFAULT_CONFIG, ...(cfgIn ?? {}), eco: { ...DEFAULT_CONFIG.eco }, walk };
  const st = createState(citySpec(seed, preset, cfg), cfg, seed);
  const bot = createBot(policy);
  const cmds: Command[] = [];
  const ticks = hours * 3600;
  let truckAt = -1;
  for (let k = 0; k < ticks; k++) {
    cmds.length = 0;
    botCommands(st, bot, cmds);
    step(st, cmds);
    for (const ev of st.events) if (ev.type === 'truck' && truckAt < 0) truckAt = ev.t;
    st.events.length = 0;
  }
  const last = st.hourly[st.hourly.length - 1];
  return {
    hourly: st.hourly.map(r => ({ h: r.h, held: r.held, front: r.front, interior: r.interior, mags: r.mags, shells: r.shells, lost: r.lost })),
    totalMags: st.totalRounds / 10, totalShells: st.totalShells, firstInterior: st.stats.firstInterior,
    lost: st.stats.lost, retakes: st.stats.retakes, claims: st.stats.claims,
    held: last.held, front: last.front, interior: last.interior,
    lostLog: st.stats.lostLog.map(l => ({ t: l.t, reason: l.reason, x: l.x, y: l.y })),
    walkedMin: Array.from({ length: hours }, (_, h) => Math.round((st.engineer.walkedHour[h] ?? 0) / 60 * 100) / 100),
    truckAt,
  };
}
