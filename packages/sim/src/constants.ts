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

/** §12 Shot magazine: 2 steel + 1 Cu → 1 magazine of 10 rounds in 3 s (docsync `recipes` table). */
export const SHOT_MAGAZINE = { steel: 2, copper: 1, rounds: 10, seconds: 3 } as const;
export const ROUNDS_PER_MAG = SHOT_MAGAZINE.rounds;

/** §12 assembler rates by tier. The doc has one tier: "There is no Mk1/Mk2 ladder in the game: the Assembler makes
 *  20 mag/min from the 3 s recipe" (C9, D-P3-7). The prototype's 10 mag/min "Mk1" is the calibration's stand-in
 *  (`PROTO_CALIBRATED.startAsmRate`) and is reported by docsync as a disagreement, not listed here as a tier. */
export interface AssemblerTier { name: string; magPerMin: number; note: string }
export const ASSEMBLER_TIERS: readonly AssemblerTier[] = [
  { name: 'Assembler', magPerMin: 60 / SHOT_MAGAZINE.seconds, note: '§12, §13: 20 mag/min from the 3 s recipe; the only tier (C9, D-P3-7)' },
];
export const ASSEMBLER_MAG_PER_MIN = ASSEMBLER_TIERS[0].magPerMin;

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

// ------------------------------------------------------------------ §5 / §14 power

/** §5 table (D1): substation draw 100 kW on a Contested or Held block with a dark neighbour, 20 kW interior [sim: E4h]. */
export const SUBSTATION_KW = { front: 100, interior: 20 } as const;
/** §14 (D-B3-4): a brownout is proportional — every machine on the grid runs at supply ÷ demand; nothing sheds. */
export const BROWNOUT_RULE = 'proportional' as const;

// ------------------------------------------------------------------ §11 the start and the hour

/** §11: the chest opens with 200 steel, 100 copper, 50 stone and 20 magazines (C10, D-P2-1). */
export const START_CHEST = { steel: 200, copper: 100, stone: 50, magazines: 20 } as const;

/** §11's minute list as the hour bot plays it (hour.ts, run name B-M6-hour): Generators at 0 / 6 / 15 / 45 (E4-doc,
 *  D-P4-7), the Excavators and their belts at 6, the Shot line at 8 ("by minute 10 ammo is automated"), east / west /
 *  north claimed on the calibration's clock 15 / 25 / 40 (D-P3-6, D-B6-2). The doc gives windows, not minutes, for the
 *  claims (east and west 10–30, north 30–60) and no minute for the third Generator; those are recorded disagreements. */
export const HOUR_GENERATOR_MIN: readonly number[] = [0, 6, 15, 45];
export const HOUR_CLAIM_MIN = { east: 15, west: 25, north: 40 } as const;
export const HOUR_EXCAVATORS_MIN = 6, HOUR_SHOT_LINE_MIN = 8;
export const HOUR_MINUTES: readonly { min: number; what: string; doc: string }[] = [
  { min: 0, what: 'hand-mine steel, craft ten magazines, walk them to the west and east turrets twice', doc: '§11 0–10' },
  { min: HOUR_EXCAVATORS_MIN, what: 'Generator 2, the coal Excavator, the steel and copper Excavators and their belts', doc: '§11 "at minute 6"' },
  { min: HOUR_SHOT_LINE_MIN, what: 'the Shot assembler, three inserters, the ammo belt', doc: '§11 "by minute 10"' },
  { min: HOUR_CLAIM_MIN.east, what: 'claim east', doc: '§11 10–30' },
  { min: HOUR_GENERATOR_MIN[2], what: 'Generator 3', doc: '§11 10–30 (E4-doc 15)' },
  { min: HOUR_CLAIM_MIN.west, what: 'claim west', doc: '§11 10–30' },
  { min: HOUR_CLAIM_MIN.north, what: 'claim north; the two idle turrets carried to north', doc: '§11 30–60 ("about minute 40")' },
  { min: HOUR_GENERATOR_MIN[3], what: 'Generator 4, the fifth Excavator and third Assembler', doc: '§11 "minute 45"' },
];
