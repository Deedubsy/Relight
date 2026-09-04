/** Session telemetry. Engine-free: reads sim state and events, writes plain JSON. */
import { SimState, SimEvent, shapeMetrics, ammoStatus, heldCount, frontage, interior, ShapeMetrics, clockOf, configHash, pipOf, slotInfo, flowSummary, FlowSummary, edgeCap } from '@relight/sim';

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
  /** M2 tile layer: magazines the tile assemblers and hands made and the Depot took, magazines the ring consumed
   *  (cumulative, from totalRounds), the machines standing and the items on belts. Zero without the flow layer. */
  tileMagsMade: number; tileMagsDelivered: number; handMined: number; handCrafted: number; magsConsumed: number;
  tileMagPerMin: number; excavators: number; belts: number; inserters: number; tileAssemblers: number; beltItems: number; mined: number; coal: number;
  /** M3 defence and power: turret hopper rounds against their capacity, magazines on belts, lamps lit, brownout
   *  seconds so far, Generators burning, coal in the Generators, the grid's numbers and the D-B3-4 throttle. */
  hopperRounds: number; hopperCap: number; beltAmmo: number; lampsLit: number; lamps: number; brownoutS: number;
  generators: number; generatorsBurning: number; genCoal: number; supplyKw: number; demandKw: number; loadKw: number; throttle: number;
  turrets: number; fired: number; coalBurned: number; polesConnected: number;
  /** M4 threat: crawlers alive on the tile layer, unshot arrivals and lamps eaten so far, kills by hand, HP lost. */
  crawlers: number; arrivals: number; lampsEaten: number; rifleKills: number; hpLost: number;
}
export interface Telemetry {
  meta: { seed: number; url: string; startedAt: string; config: unknown; configHash: string; player: string;
          scenario: 'A' | 'B'; snapshot: string | null; startT: number;   // startT: sim tick the session began at
          startShootS: number; startDanger: number; startDangerShot: number };   // D-B1-5: the body counters at the session's start
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
            scenario, snapshot, startT: st.t, startShootS: st.engineer.shootS ?? 0, startDanger: st.engineer.danger ?? 0, startDangerShot: st.engineer.dangerShot ?? 0 },
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

/** Pip watch: the first sim time a front pip is amber or red. Called after every advance. Never reads a pip on its
 *  birth tick (a kitted edge is born fed; an unkitted one has no supply to be low on). */
export function recordPips(tel: Telemetry, st: SimState): void {
  if (tel.firstAmber !== null && tel.firstRed !== null) return;
  for (const e of st.ring) {
    if (e.born === st.t || e.kit === false) continue;
    const p = pipOf(e.hopper / edgeCap(st, e));
    if (p !== 'green' && tel.firstAmber === null) tel.firstAmber = st.t;
    if (p === 'red' && tel.firstRed === null) tel.firstRed = st.t;
  }
}

export function recordMinute(tel: Telemetry, st: SimState): void {
  const sm = shapeMetrics(st), am = ammoStatus(st), si = slotInfo(st), fs = flowSummary(st);
  let amber = 0, red = 0;
  for (const e of st.ring) { const p = pipOf(e.hopper / edgeCap(st, e)); if (p === 'amber') amber++; else if (p === 'red') red++; }
  tel.minutes.push({
    t: st.t, held: sm.held, front: sm.front, interior: sm.interior, contested: sm.contested, lost: sm.lost,
    bboxW: sm.bbox.w, bboxH: sm.bbox.h, aspect: sm.bbox.aspect, perimeter: sm.perimeter, perimeterOverArea: sm.perimeterOverArea,
    productionMagPerMin: am.productionMagPerMin, demandMagPerMin: am.demandMagPerMin, stockMags: am.stockMags,
    emptyHoppers: am.emptyHoppers, assemblers: am.assemblers, copper: st.stock.copper, steel: st.stock.steel,
    magsMade: st.stats.magsMade, amber, red,
    slotsUsed: si.used, slotsFree: si.free, atRisk: si.atRisk, dry: si.dry,
    tileMagsMade: fs.magsMade, tileMagsDelivered: fs.magsDelivered, handMined: st.flow?.stats.handMined ?? 0, handCrafted: st.flow?.stats.handCrafted ?? 0,
    magsConsumed: st.totalRounds / 10, tileMagPerMin: fs.magsMade - (tel.minutes[tel.minutes.length - 1]?.tileMagsMade ?? 0),   // made in the last minute
    excavators: fs.excavators, belts: fs.belts, inserters: fs.inserters, tileAssemblers: fs.assemblers, beltItems: fs.beltItems, mined: fs.mined, coal: fs.coal,
    hopperRounds: fs.turretRounds, hopperCap: fs.turretCap, beltAmmo: fs.beltAmmo, lampsLit: fs.lampsLit, lamps: fs.lamps, brownoutS: fs.brownoutS,
    generators: fs.generators, generatorsBurning: fs.generatorsBurning, genCoal: fs.genCoal, supplyKw: fs.supplyKw, demandKw: fs.demandKw, loadKw: fs.loadKw, throttle: fs.throttle,
    turrets: fs.turrets, fired: fs.fired, coalBurned: fs.coalBurned, polesConnected: fs.polesConnected,
    crawlers: fs.crawlers, arrivals: fs.arrivals, lampsEaten: fs.lampsEaten, rifleKills: fs.rifleKills, hpLost: st.engineer.hurt,
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
  /** M2 tile layer (zeros without it). */
  flow: FlowSummary; magsConsumed: number; handMined: number; handCrafted: number;
  /** M3 (zeros without the flow layer): brownout seconds, rounds the turrets fired, coal burned, hand-feeds. */
  brownoutSeconds: number; fired: number; coalBurned: number; handFed: number;
  /** D-B1-5 (§19 guards): the session's shooting time and time in danger as a share of its sim time — shooting is
   *  capped at 10 %, danger at 5 %; `dangerShotShare` says whether danger came from the rifle (retaliation) or from
   *  crawlers loose on the player's block. */
  shootPct: number; dangerPct: number; dangerShotShare: number; rifleRounds: number; rifleKills: number;
  /** Prompt B M4 telemetry: minutes walked in each sim hour (index = hour), trips to the chest, placements refused
   *  for reach, the clock of the first rifle shot, HP lost and knockdowns over the whole run, and every hand-fired
   *  engagement with whether its edge held (null: still open). `walkedPct` is the walking share of the session
   *  against §19's 15 %. Zeros / 'never' / [] without the flow layer. */
  walkedMinPerHour: number[]; walkedPct: number; chestTrips: number; reachRefused: number; firstRifleShot: string;
  hpLost: number; knockdowns: number; lampsEaten: number; arrivals: number; turretKills: number;
  handFights: { edge: number; t: string; rounds: number; kills: number; held: boolean | null }[]; handFightsHeld: number; handFightsFell: number;
}

export function summarise(tel: Telemetry, st: SimState): Summary {
  const startT = tel.meta.startT;
  const hours = (st.t - startT) / 3600, elapsed = st.t - startT;
  const shootS = (st.engineer.shootS ?? 0) - tel.meta.startShootS, danger = (st.engineer.danger ?? 0) - tel.meta.startDanger, dangerShot = (st.engineer.dangerShot ?? 0) - tel.meta.startDangerShot;
  const sm = shapeMetrics(st), fs = flowSummary(st), fights = st.flow?.threat?.fights ?? [];
  // walking seconds inside the session: the per-hour buckets from the session's start hour on (a snapshot's hours are the bot's)
  const walkedS = st.engineer.walkedHour.reduce((a, s, h) => a + ((h + 1) * 3600 > startT ? (s ?? 0) : 0), 0);
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
    flow: fs, magsConsumed: st.totalRounds / 10, handMined: st.flow?.stats.handMined ?? 0, handCrafted: st.flow?.stats.handCrafted ?? 0,
    brownoutSeconds: st.flow?.power.overS ?? 0, fired: st.flow?.stats.fired ?? 0, coalBurned: st.flow?.stats.coalBurned ?? 0, handFed: st.flow?.stats.handFed ?? 0,
    shootPct: elapsed > 0 ? 100 * shootS / elapsed : 0, dangerPct: elapsed > 0 ? 100 * danger / elapsed : 0,
    dangerShotShare: danger > 0 ? dangerShot / danger : 0, rifleRounds: st.engineer.fired, rifleKills: st.engineer.kills,
    walkedMinPerHour: Array.from(st.engineer.walkedHour, s => Math.round((s ?? 0) / 6) / 10), walkedPct: elapsed > 0 ? 100 * walkedS / elapsed : 0,
    chestTrips: st.flow?.stats.chestTrips ?? 0, reachRefused: st.flow?.stats.reachRefused ?? 0,
    firstRifleShot: st.engineer.firstShot >= 0 ? clockOf(st.engineer.firstShot) : 'never',
    hpLost: Math.round(st.engineer.hurt), knockdowns: st.engineer.downs, lampsEaten: fs.lampsEaten, arrivals: fs.arrivals, turretKills: fs.turretKills,
    handFights: fights.map(f => ({ edge: f.edge, t: clockOf(f.t), rounds: f.rounds, kills: f.kills, held: f.held })),
    handFightsHeld: fights.filter(f => f.held === true).length, handFightsFell: fights.filter(f => f.held === false).length,
  };
}

export function exportJson(tel: Telemetry, st: SimState): string {
  return JSON.stringify({ ...tel, summary: summarise(tel, st), finalState: st }, null, 1);
}
