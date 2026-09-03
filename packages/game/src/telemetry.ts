/** Session telemetry. Engine-free: reads sim state and events, writes plain JSON. */
import { SimState, SimEvent, shapeMetrics, ammoStatus, heldCount, frontage, interior, ShapeMetrics, clockOf, configHash, pipOf, slotInfo } from '@relight/sim';

export interface ClaimRecord {
  t: number; x: number; y: number; district: string; well: boolean; d: number;
  fBefore: number; fAfter: number; iBefore: number; iAfter: number; retake: boolean; wakeCrawlers: number;
  source: 'player' | 'bot';
}
export interface MinuteRecord {
  t: number; held: number; front: number; interior: number; contested: number; lost: number;
  bboxW: number; bboxH: number; aspect: number; perimeter: number; perimeterOverArea: number;
  productionMagPerMin: number; demandMagPerMin: number; stockMags: number; emptyHoppers: number; assemblers: number;
  copper: number; steel: number; magsMade: number; amber: number; red: number;
  slotsUsed: number; slotsFree: number; atRisk: number; dry: number;
}
export interface Telemetry {
  meta: { seed: number; url: string; startedAt: string; config: unknown; configHash: string; player: string;
          scenario: 'A' | 'B'; snapshot: string | null; startT: number };   // startT: sim tick the session began at
  claims: ClaimRecord[];
  rejected: { t: number; x: number; y: number; reason: string }[];
  losses: { t: number; x: number; y: number; reason: string }[];
  reorders: { t: number; ids: number[] }[];
  assemblers: { t: number; count: number; x: number; y: number; source: 'player' | 'bot' }[];
  assemblersRejected: { t: number; reason: string }[];
  machinesLost: { t: number; x: number; y: number }[];
  dry: { t: number; x: number; y: number; district: string }[];   // every block that runs dry
  speeds: { t: number; realTime: number; speed: number }[];
  minutes: MinuteRecord[];
  firstEnclosure: number | null;   // sim tick of the first interior block
  firstAmber: number | null;       // sim tick a pip first left green (sampled once per frame / per tick in run())
  firstRed: number | null;
  hours: { h: number; held: number; front: number; interior: number; mags: number; lost: number }[];
}

export function createTelemetry(st: SimState, url: string, player = '', scenario: 'A' | 'B' = 'A', snapshot: string | null = null): Telemetry {
  return {
    meta: { seed: st.seed, url, startedAt: new Date().toISOString(), config: st.config, configHash: configHash(st.config), player,
            scenario, snapshot, startT: st.t },
    claims: [], rejected: [], losses: [], reorders: [], assemblers: [], assemblersRejected: [], machinesLost: [], dry: [], speeds: [], minutes: [], firstEnclosure: null,
    firstAmber: null, firstRed: null, hours: [],
  };
}

export function recordEvent(tel: Telemetry, ev: SimEvent, source: 'player' | 'bot'): void {
  switch (ev.type) {
    case 'claim':
      tel.claims.push({ t: ev.t, x: ev.x, y: ev.y, district: ev.district, well: ev.well, d: ev.d, fBefore: ev.fBefore, fAfter: ev.fAfter,
                        iBefore: ev.iBefore, iAfter: ev.iAfter, retake: ev.retake, wakeCrawlers: ev.cr, source });
      break;
    case 'claim-rejected': tel.rejected.push({ t: ev.t, x: ev.x, y: ev.y, reason: ev.reason }); break;
    case 'fall': tel.losses.push({ t: ev.t, x: ev.x, y: ev.y, reason: ev.reason }); break;
    case 'reorder': tel.reorders.push({ t: ev.t, ids: ev.ids.slice() }); break;
    case 'assembler': tel.assemblers.push({ t: ev.t, count: ev.count, x: ev.x, y: ev.y, source }); break;
    case 'assembler-rejected': tel.assemblersRejected.push({ t: ev.t, reason: ev.reason }); break;
    case 'machine-lost': tel.machinesLost.push({ t: ev.t, x: ev.x, y: ev.y }); break;
    case 'run-dry': tel.dry.push({ t: ev.t, x: ev.x, y: ev.y, district: ev.district }); break;
    case 'hour': tel.hours.push({ h: ev.row.h, held: ev.row.held, front: ev.row.front, interior: ev.row.interior, mags: ev.row.mags, lost: ev.row.lost }); break;
  }
}

/** Pip watch: the first sim time a front pip is amber or red. Called after every advance. */
export function recordPips(tel: Telemetry, st: SimState): void {
  if (tel.firstAmber !== null && tel.firstRed !== null) return;
  const cap = st.config.hopper;
  for (const e of st.ring) {
    const p = pipOf(e.hopper / cap);
    if (p !== 'green' && tel.firstAmber === null) tel.firstAmber = st.t;
    if (p === 'red' && tel.firstRed === null) tel.firstRed = st.t;
  }
}

export function recordMinute(tel: Telemetry, st: SimState): void {
  const sm = shapeMetrics(st), am = ammoStatus(st), si = slotInfo(st);
  const cap = st.config.hopper;
  let amber = 0, red = 0;
  for (const e of st.ring) { const p = pipOf(e.hopper / cap); if (p === 'amber') amber++; else if (p === 'red') red++; }
  tel.minutes.push({
    t: st.t, held: sm.held, front: sm.front, interior: sm.interior, contested: sm.contested, lost: sm.lost,
    bboxW: sm.bbox.w, bboxH: sm.bbox.h, aspect: sm.bbox.aspect, perimeter: sm.perimeter, perimeterOverArea: sm.perimeterOverArea,
    productionMagPerMin: am.productionMagPerMin, demandMagPerMin: am.demandMagPerMin, stockMags: am.stockMags,
    emptyHoppers: am.emptyHoppers, assemblers: am.assemblers, copper: st.stock.copper, steel: st.stock.steel,
    magsMade: st.stats.magsMade, amber, red,
    slotsUsed: si.used, slotsFree: si.free, atRisk: si.atRisk, dry: si.dry,
  });
  if (tel.firstEnclosure === null && st.stats.firstInterior >= 0) tel.firstEnclosure = st.stats.firstInterior;
}

/** Session numbers count from the session's start (a snapshot's history is not the tester's); `*Total` fields
 *  carry the whole run's counters so scenario B can be reconciled against the bot's 3 h. */
export interface Summary {
  simTime: string; hours: number; startTime: string; scenario: 'A' | 'B';
  claims: number; claimsPerHour: number; playerClaims: number; claimsTotal: number;
  held: number; front: number; interior: number; lost: number; lostTotal: number;
  shape: ShapeMetrics;
  ammoSpentMags: number; shellsSpent: number;
  reorders: number; assemblersAdded: number; botAssemblers: number;
  firstEnclosure: string; firstAmber: string; firstRed: string; firstLoss: string;
  stock: { copper: number; steel: number; stone: number }; magsMade: number; configHash: string;
  slotsUsed: number; slotsFree: number; machinesLost: number; ranDry: number;
}

export function summarise(tel: Telemetry, st: SimState): Summary {
  const startT = tel.meta.startT;
  const hours = (st.t - startT) / 3600;
  const sm = shapeMetrics(st);
  return {
    simTime: clockOf(st.t), hours, startTime: clockOf(startT), scenario: tel.meta.scenario,
    claims: tel.claims.length, claimsPerHour: hours > 0 ? tel.claims.length / hours : 0, claimsTotal: st.stats.claims,
    playerClaims: tel.claims.filter(c => c.source === 'player').length,
    held: heldCount(st), front: frontage(st), interior: interior(st), lost: tel.losses.length, lostTotal: st.stats.lost,
    shape: sm,
    ammoSpentMags: st.totalRounds / 10, shellsSpent: st.totalShells,
    reorders: tel.reorders.length, assemblersAdded: tel.assemblers.length,
    botAssemblers: tel.assemblers.filter(a => a.source === 'bot').length,
    firstEnclosure: st.stats.firstInterior >= 0 ? clockOf(st.stats.firstInterior) : 'never',
    firstAmber: tel.firstAmber !== null ? clockOf(tel.firstAmber) : 'never',
    firstRed: tel.firstRed !== null ? clockOf(tel.firstRed) : 'never',
    firstLoss: tel.losses.length ? clockOf(tel.losses[0].t) : 'never',
    stock: { copper: Math.floor(st.stock.copper), steel: Math.floor(st.stock.steel), stone: Math.floor(st.stock.stone) },
    magsMade: Math.round(st.stats.magsMade), configHash: tel.meta.configHash,
    slotsUsed: slotInfo(st).used, slotsFree: slotInfo(st).free, machinesLost: st.stats.machinesLost, ranDry: st.stats.ranDry,
  };
}

export function exportJson(tel: Telemetry, st: SimState): string {
  return JSON.stringify({ ...tel, summary: summarise(tel, st), finalState: st }, null, 1);
}
