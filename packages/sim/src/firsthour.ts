/** §11 first hour as a closed timeline (frontsim.py first_hour(), ported line for line): one 300 kW Generator, the
 *  starting coal, the HQ substation, machines placed at the minutes §11 states, three starting neighbours, coal from
 *  the west block's rubble once it is Held. Deterministic, no map. E4. A shortfall throttles (D-B3-4). */
import { COAL_MJ, GENERATOR_KW } from './recipes';
import { HOUR_CLAIM_MIN, HOUR_GENERATOR_MIN, SUBSTATION_KW } from './constants';

export interface FirstHourOptions {
  startCoal: number;        // units in the HQ's starting stock (§11: 40)
  hqDraw: number;           // kW; the HQ substation
  coalRubble: boolean;      // coal rubble available from t = 0 (instead of after the west block is Held)
  drawFlat: boolean;        // every substation 120 kW
  gens: number[];           // ticks at which a Generator is placed (index 0 is the starting one)
  subKw: number;            // frontage substation draw (D1 100; the pre-D1 200 is FIRST_HOUR_PRE_D1)
  interiorKw: number;       // interior substation draw (D1 20; the pre-D1 40 is FIRST_HOUR_PRE_D1)
  patch: number | null;     // units in the HQ-lot coal patch, mined by an Excavator from `patchExcAt`
  patchExcAt: number;
}

/** The doc's numbers (economy-fix task Step 2, items 5 and 7): D1's draws (SUBSTATION_KW) and the Generators at
 *  constants.HOUR's minutes (D-P4-7: 0 / 6 / 15 / 45). */
export const FIRST_HOUR_DEFAULTS: FirstHourOptions = {
  startCoal: 40, hqDraw: SUBSTATION_KW.front, coalRubble: false, drawFlat: false, gens: HOUR_GENERATOR_MIN.map(m => m * 60),
  subKw: SUBSTATION_KW.front, interiorKw: SUBSTATION_KW.interior, patch: null, patchExcAt: 6 * 60,
};

/** The pre-D1 draws (the doc before D1: 200 kW front, 40 kW interior, HQ 200). E4 prints one comparison row with
 *  them and nothing else reads this; no live rule carries 200 / 40. */
export const FIRST_HOUR_PRE_D1: Partial<FirstHourOptions> = { subKw: 200, interiorKw: 40, hqDraw: 200 };

/** D1 variant: the coal patch on the HQ lot (D1: ~700 units); its draws are the defaults now. */
export const FIRST_HOUR_D1: Partial<FirstHourOptions> = { subKw: SUBSTATION_KW.front, interiorKw: SUBSTATION_KW.interior, hqDraw: SUBSTATION_KW.front, patch: 700 };

export interface FirstHourResult {
  firstBrownout: number; coalOut: number; patchOut: number;   // ticks, -1 = never
  throttleMin: number; brownoutS: number; coalArrives: number; peakKw: number;   // D-B3-4: worst supply ÷ demand and seconds short
  gensNeeded: number[];   // per 10-minute slot: ceil(demand / 300 kW)
  coalBurned: number; burn30: number; patchLeft: number;
  demandKw: number[];     // per minute
}

/** §11 timeline: east claim at 15 min, west at 25, north at 65 (D-P4-10 (a) — past the hour, so the 3,600 s loop
 *  below never adds north's substation and the HQ never turns interior); burn-off 20 s + 60 s × rot. */
export const FIRST_HOUR_CLAIMS = { east: HOUR_CLAIM_MIN.east * 60, west: HOUR_CLAIM_MIN.west * 60, north: HOUR_CLAIM_MIN.north * 60 };

export function firstHour(over: Partial<FirstHourOptions> = {}): FirstHourResult {
  const o: FirstHourOptions = { ...FIRST_HOUR_DEFAULTS, ...over };
  let E = o.startCoal * COAL_MJ;   // MJ
  const genKw = GENERATOR_KW;
  let patchLeft = o.patch !== null ? o.patch : 0;
  let patchOut = -1;
  const claimE = FIRST_HOUR_CLAIMS.east, claimW = FIRST_HOUR_CLAIMS.west, claimN = FIRST_HOUR_CLAIMS.north;
  const burnE = 20 + 60 * 0.22, burnW = 20 + 60 * 0.22, burnN = 20 + 60 * 0.15;
  const machines: [number, number][] = [[6 * 60, 120], [8 * 60, 100]];              // 2 excavators, shot assembler
  machines.push([claimE + burnE + 60, 60], [claimE + burnE + 120, 100]);              // copper exc, wire asm
  const coalExcAt = claimW + burnW + 60;
  machines.push([coalExcAt, 60]);
  machines.push([45 * 60, 60], [45 * 60, 100]);                                       // 5th excavator, 3rd assembler
  if (o.patch !== null) machines.push([o.patchExcAt, 60]);                            // excavator on the HQ coal patch
  let firstBrownout = -1, coalOut = -1, throttleMin = 1.0, brownoutS = 0, peak = 0, coalBurned = 0, burn30 = 0;
  const gensNeeded: number[] = [], demandKw: number[] = [];
  for (let t = 0; t < 3600; t++) {
    let d = o.drawFlat ? 120 : o.hqDraw;
    if (t >= claimE) d += o.drawFlat ? 120 : o.subKw;
    if (t >= claimW) d += o.drawFlat ? 120 : o.subKw;
    if (t >= claimN) d += o.drawFlat ? 120 : o.subKw;
    // HQ becomes interior when N is Held (E, W, N Held + river)
    if (t >= claimN + burnN && !o.drawFlat && o.hqDraw === o.subKw) d -= o.subKw - o.interiorKw;
    for (const [at, kw] of machines) if (t >= at) d += kw;
    let cap = 0;
    for (const g of o.gens) if (t >= g) cap += genKw;
    if (E <= 0 && !(o.coalRubble || t >= coalExcAt)) cap = 0;
    peak = Math.max(peak, d);
    const slot = Math.floor(t / 600);
    gensNeeded[slot] = Math.max(gensNeeded[slot] ?? 0, Math.ceil(d / genKw - 1e-9));
    if (t % 60 === 0) demandKw.push(d);
    if (d > cap) {
      // D-B3-4: nothing is shed; every machine (and the HQ) runs at cap ÷ demand
      if (firstBrownout < 0) firstBrownout = t;
      brownoutS++;
      throttleMin = Math.min(throttleMin, d ? cap / d : 1);
    }
    const burn = Math.min(d, cap);
    coalBurned += burn / 1000 / COAL_MJ;
    if (t === 30 * 60) burn30 = coalBurned;
    if (o.coalRubble || t >= coalExcAt) E += 0.5 * COAL_MJ;   // one excavator on coal rubble: 0.5/s
    if (o.patch !== null && t >= o.patchExcAt && patchLeft > 0) {
      const mined = Math.min(0.5, patchLeft); patchLeft -= mined; E += mined * COAL_MJ;
      if (patchLeft <= 0 && patchOut < 0) patchOut = t;
    }
    E -= burn / 1000;
    if (E <= 0 && coalOut < 0 && !o.coalRubble && t < coalExcAt) coalOut = t;
    E = !(o.coalRubble || t >= coalExcAt) ? Math.max(E, 0) : Math.min(E, 1e9);
  }
  return { firstBrownout, coalOut, patchOut, throttleMin, brownoutS, coalArrives: coalExcAt, peakKw: peak,
           gensNeeded, coalBurned, burn30, patchLeft, demandKw };
}
