/** The one place the design doc's tempo constants live (guardrails task, Step 5; constitution rules 1 and 11).
 *
 *  Every number here is the doc's number with the doc sentence and the decision it came from. `packages/tools/src/docsync.ts`
 *  generates the doc's "source-of-truth constants" table from this file and, under `--check`, fails when the doc's prose,
 *  the calibration config (`PROTO_CALIBRATED` / `DEFAULT_CONFIG`), `firsthour.ts` or the hour bot (`hour.ts`) disagree
 *  with it. The sim modules read these values instead of carrying their own copies; a module that still carries a
 *  different number is a recorded disagreement (`GUARDRAILS_REPORT.md` §5), not something this file resolves.
 *
 *  This file imports nothing, so anything in the sim may read it. Changing a value here is a rules change: it moves
 *  the D6 fixtures and needs a DECISIONS.md row. */

// ------------------------------------------------------------------ §12 the ammo chain

/** §12 Shot magazine: 2 steel + 1 Cu → 1 magazine of 10 rounds in 6 s on the start Assembler (the Mk1, D-P4-4 / D-B1-1,
 *  decided 2026-09-04) and in 3 s on the Mk2 (docsync `recipes` table). */
export const SHOT_MAGAZINE = { steel: 2, copper: 1, rounds: 10, seconds: 6 } as const;
export const SHOT_MAGAZINE_MK2_SECONDS = 3;
export const ROUNDS_PER_MAG = SHOT_MAGAZINE.rounds;

/** §12 assembler rates by tier (D-P4-4 / D-B1-1, decided 2026-09-04; C9 / D-P3-7's "one tier" is superseded): the
 *  start Assembler is the Mk1 at 10 mag/min from the 6 s recipe — the map-view calibration's `startAsmRate` — and the
 *  Mk2, the purchase (unlock open), makes 20 mag/min from the 3 s recipe — the block sim's `asmRate`, which is what
 *  `DEFAULT_CONFIG` and the config hash carry (ASSEMBLER_MAG_PER_MIN), so no fixture moved. */
export interface AssemblerTier { name: string; magPerMin: number; note: string }
export const ASSEMBLER_TIERS: readonly AssemblerTier[] = [
  { name: 'Mk1', magPerMin: 60 / SHOT_MAGAZINE.seconds, note: '§12, §13: 10 mag/min from the 6 s recipe; the start Assembler (D-P4-4, D-B1-1)' },
  { name: 'Mk2', magPerMin: 60 / SHOT_MAGAZINE_MK2_SECONDS, note: '§12, §13: 20 mag/min from the 3 s recipe; the purchase, unlock open (D-B1-1); the block sim\'s asmRate' },
];
export const ASSEMBLER_MK1_MAG_PER_MIN = ASSEMBLER_TIERS[0].magPerMin;
export const ASSEMBLER_MAG_PER_MIN = ASSEMBLER_TIERS[1].magPerMin;

// ------------------------------------------------------------------ §13 rates

/** §13: Excavator mines a 5×5 at 0.5/s [sim: M2-rates]. */
export const EXCAVATOR_PER_S = 0.5;
/** §13/§14: belt 7.5/s, fast belt 15/s (D-P4-6, [sim: M2-rates]); inserter 1 item/s. */
export const BELT_PER_S = 7.5, FAST_BELT_PER_S = 15, INSERTER_PER_S = 1;

// ------------------------------------------------------------------ §13 the Gun turret

/** §13: range 9, 5 rounds/s, 50-round hopper [sim: M3-rates]; §7: 4 HP a round [sim: B-M4-threat]. */
export const TURRET: { range: number; roundsPerS: number; hopper: number; roundDmg: number } = { range: 9, roundsPerS: 5, hopper: 50, roundDmg: 4 };
/** D-B1-4-rider: the block sim's "edge hopper" is not a constant of its own — it is the turrets on the segment × the
 *  turret hopper. The prototype's edge carries two turrets (D-P1-3 rider, "two turrets per edge"); sim.ts
 *  DEFAULT_CONFIG.hopper is EDGE_TURRETS × TURRET.hopper and docsync compares the block sim to that product. */
export const EDGE_TURRETS: number = 2;

// ------------------------------------------------------------------ D-B1-4 placement rules

/** §13 pre-existing fixtures (D-B1-4): the HQ's start turrets are one per 16 tiles of each street segment's length,
 *  at least one a segment; a face's streetlights stand one every 4 tiles along each segment's kerb. */
export const TURRET_PER_TILES: number = 16, LAMP_STEP_TILES: number = 4;
/** §11 (D-P4-8, decided 2026-09-04): six start turrets, hoppers full, spread over the HQ's street segments by D-B1-4's
 *  rule (in proportion to length, at least one a segment; flow.ts `apportion`). */
export const START_TURRETS: number = 6;
/** §13 (D-B5-4, decided 2026-09-04): a kerb streetlight reaches the street's midline — radius 7 on the river city's
 *  12–14-tile streets — so a held face lights its half of the shared street; the Lamp keeps §13's radius 4. */
export const STREETLIGHT_RADIUS: number = 7;

// ------------------------------------------------------------------ §5 / §14 power

/** §5 table (D1): substation draw 100 kW on a Contested or Held block with a dark neighbour, 20 kW interior [sim: E4h]. */
export const SUBSTATION_KW = { front: 100, interior: 20 } as const;
/** §14 (D-B3-4): a brownout is proportional — every machine on the grid runs at supply ÷ demand; nothing sheds. */
export const BROWNOUT_RULE = 'proportional' as const;

// ------------------------------------------------------------------ §11 the start and the hour

/** §11: the chest opens with 200 steel, 100 copper, 50 stone, 40 coal and 20 magazines (C10, D-P2-1; D-P4-4 keeps the
 *  200 steel, D-P4-7 (b) adds the 40 coal beside Generator 1's hopper, D-P4-8 the 20 magazines on top of six full
 *  turrets — decided 2026-09-04). */
export const START_CHEST = { steel: 200, copper: 100, stone: 50, coal: 40, magazines: 20 } as const;

/** §11 the hour as one ordered list (D-HOUR-1, decided 2026-09-04): every minute the hour bot (hour.ts), the
 *  calibration timeline (firsthour.ts) and the design doc's §11 minute table (generated by docsync) read comes from
 *  here; nothing else carries a minute. `doc` names the decided row each minute came from. The §11 prose ("10 to 30",
 *  "30 to 60") stays prose; this table is the rule. */
export interface HourStep { id: string; min: number; what: string; doc: string }
export const HOUR: readonly HourStep[] = [
  { id: 'generator-1', min: 0, what: 'Generator 1 (300 kW) with its hopper coal; the chest\'s 40 coal beside it', doc: 'D-P4-7, D-B1-1' },
  { id: 'mine', min: 0, what: 'hand-mine steel at the HQ patch', doc: '§11 0–10, D-P4-8' },
  { id: 'craft', min: 0, what: 'craft magazines at the workbench; hand-feed the first empty hopper (~minute 6) with E', doc: '§11 0–10, D-P4-8' },
  { id: 'coal-excavator', min: 6, what: 'the coal Excavator and its belt, before Generator 2', doc: 'D-P4-7' },
  { id: 'generator-2', min: 6, what: 'Generator 2, on the chest\'s coal until the belt delivers', doc: 'D-P4-7, D-HOUR-1' },
  { id: 'line-excavators', min: 6, what: 'the steel and copper Excavators and their belts', doc: '§11 "at minute 6"' },
  { id: 'shot-line', min: 8, what: 'the Shot line: one Mk1 Assembler (10 mag/min), three inserters, the ammo belt', doc: 'D-HOUR-1, D-P4-4' },
  { id: 'generator-3', min: 15, what: 'Generator 3', doc: 'D-P4-7, D-HOUR-1' },
  { id: 'claim-east', min: 15, what: 'claim east (residential)', doc: 'D-HOUR-1, D-B6-2' },
  { id: 'steel-2', min: 15, what: 'the second steel Excavator, belted into the chest', doc: 'D-P4-7, D-HOUR-1' },
  { id: 'claim-west', min: 25, what: 'claim west (rail yard)', doc: 'D-HOUR-1, D-B6-2' },
  { id: 'claim-north', min: 40, what: 'claim north (civic); the two idle HQ turrets carried to north', doc: 'D-HOUR-1, D-B6-2' },
  { id: 'generator-4', min: 45, what: 'Generator 4', doc: 'D-P4-7, D-HOUR-1' },
  { id: 'copper-2', min: 46, what: 'the second copper Excavator, into the chest', doc: '§11 "at 46"' },
  { id: 'assembler-3', min: 50, what: 'the third Assembler', doc: '§11 "at 50"' },
];
const hourMin = (id: string): number => {
  const s = HOUR.find(h => h.id === id);
  if (!s) throw new Error(`constants.HOUR has no step "${id}"`);
  return s.min;
};
/** The minutes hour.ts and firsthour.ts read, each looked up in HOUR by id (D-HOUR-1). */
export const HOUR_GENERATOR_MIN: readonly number[] = ['generator-1', 'generator-2', 'generator-3', 'generator-4'].map(hourMin);
export const HOUR_CLAIM_MIN: { readonly east: number; readonly west: number; readonly north: number } =
  { east: hourMin('claim-east'), west: hourMin('claim-west'), north: hourMin('claim-north') };
export const HOUR_EXCAVATORS_MIN: number = hourMin('line-excavators'), HOUR_SHOT_LINE_MIN: number = hourMin('shot-line'), HOUR_STEEL2_MIN: number = hourMin('steel-2'),
  HOUR_COPPER2_MIN: number = hourMin('copper-2'), HOUR_ASM3_MIN: number = hourMin('assembler-3');
