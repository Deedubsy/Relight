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

/** §11's minute list as the hour bot plays it (hour.ts, run name B-M6-hour) since the economy fix (2026-09-04, D-P4-4 /
 *  D-P4-7 / D-P4-8 decided; §11 rewritten to it): Generators at 0 / 6 / 15 / 45 (E4-doc, D-P4-7), the Excavators and
 *  their belts at 6, the Shot line at 8 ("by minute 10 ammo is automated"), the second steel Excavator into the chest
 *  at 12 (D-P4-4's "~15:00", taken at 12 under GA-EF-1), east and west claimed at 15 / 25 (D-B6-2), the second copper
 *  Excavator at 46 and the third Assembler at 50 (the doc's minute-45 machines, spread so the chest never refuses),
 *  north at 65 — outside the hour, §11's "60–75 once west's coal is real" (D-P4-10, recommended). `firsthour.ts` (the
 *  calibration) still claims north at 40; that is a recorded disagreement. */
export const HOUR_GENERATOR_MIN: readonly number[] = [0, 6, 15, 45];
export const HOUR_CLAIM_MIN = { east: 15, west: 25, north: 65 } as const;
export const HOUR_EXCAVATORS_MIN = 6, HOUR_SHOT_LINE_MIN = 8, HOUR_STEEL2_MIN = 12, HOUR_COPPER2_MIN = 46, HOUR_ASM3_MIN = 50;
export const HOUR_MINUTES: readonly { min: number; what: string; doc: string }[] = [
  { min: 0, what: 'hand-mine steel, craft ten magazines; hand-feed the turrets on the first red pip (~6 min) and on every red pip after', doc: '§11 0–10' },
  { min: HOUR_EXCAVATORS_MIN, what: 'Generator 2 (on the chest\'s coal), the coal Excavator, the steel and copper Excavators and their belts', doc: '§11 "at minute 6"' },
  { min: HOUR_SHOT_LINE_MIN, what: 'the Shot assembler (Mk1), three inserters, the ammo belt', doc: '§11 "by minute 10"' },
  { min: HOUR_STEEL2_MIN, what: 'the second steel Excavator, belted into the chest', doc: '§11 "at minute 12" (D-P4-4)' },
  { min: HOUR_CLAIM_MIN.east, what: 'claim east', doc: '§11 "about minute 15"' },
  { min: HOUR_GENERATOR_MIN[2], what: 'Generator 3', doc: '§11 10–30 (E4-doc 15)' },
  { min: HOUR_CLAIM_MIN.west, what: 'claim west', doc: '§11 "about minute 25"' },
  { min: HOUR_GENERATOR_MIN[3], what: 'Generator 4', doc: '§11 "minute 45"' },
  { min: HOUR_COPPER2_MIN, what: 'the second copper Excavator, into the chest', doc: '§11 "minute 46"' },
  { min: HOUR_ASM3_MIN, what: 'the third Assembler', doc: '§11 "minute 50"' },
  { min: HOUR_CLAIM_MIN.north, what: 'claim north; the two idle turrets carried to north (E-hour-north only)', doc: '§11 30–60 ("north at 60–75", D-P4-10)' },
];
