# Guardrails report — 2026-09-04

The six-step guardrails task, done in order on `phase-4`, one commit per step: 15eca71 (Step 1), e58d0c5 (Step 2), fa66e51 (Step 3), ddf36f4 (Step 4), 4c066b2 (Step 5), 6aea2f7 (Step 6). Documents and CI scripts only: no game rule, no behaviour-affecting sim code and no design-doc number changed (Step 5's refactor moves values into `packages/sim/src/constants.ts` and the D6 fixtures are byte-identical before and after). Nothing a human should decide was decided.

## 1. Rules added

Rules 11–14 in `CONSTITUTION.md`, as written:

> 11. **Provenance.** A decision counts as decided only when its row in `DECISIONS.md` has three things filled in: `decided by` (a person's name), `on` (a date), and `via` (a link or a quote of the message, the commit, or the report section where they said it). If any of the three is missing, the row is *recommended*, and the assistant must not act as if it were decided. Every number in the design doc must say where it came from: a run name, a play session, or a commit. Every generated file must contain the git commit and the config hash it was made from, and CI must fail if either does not match the current code.

> 12. **Prompts do not carry constants or rules.** A task prompt may point at sections of the design doc and say what work to do. If a task prompt contains a number, a rate, a size, a cost, or a rule that is not already in the design doc or in a decided row of `DECISIONS.md`, the assistant's first and only action is to write a list of every such number or rule, say where it conflicts, and stop. The human resolves the list before any of the work starts.

> 13. **"Built" means measured.** A milestone report may use the word "built" in its heading only when its Measured table contains numbers that came from running the code. Until then the heading must say "coded (unverified)". The cheap checks (test, typecheck, lint, docsync) run at the end of every milestone, before the report is written; a milestone is never marked coded while any of them is red. The full verification pass runs when the user says "verify".

> 14. **Provisional decisions.** When the user says only "go", the assistant may act on a recommended decision only if all three are true: (a) the row is marked `provisional` in `DECISIONS.md`; (b) reversing it later would not change any game rule or any regression fixture; (c) the next report lists it first, under a heading "Taken provisionally". If a row is marked `blocking`, the assistant stops and asks the human to write the row before continuing.

Rule 9 changed from

> 9. **Every phase ends the same way.** Experiments green; a five-minute smoke test a human ran; `PHASE_N_REPORT.md` (built, assumed, deferred, measured, three decisions for a human); `PROGRAMME_STATE.md` updated; `DEFERRED.md` re-read and every item given a phase or deleted with a reason; §26 recounted; **the `STANDARDS.md` rows for this phase are checked (one minute each, by a human), and no new gap was introduced without a row** (added 2026-09-04 by the standards audit).

to

> 9. **Every phase ends the same way.** The cheap checks green at every milestone; the full verification pass green at every phase end; a five-minute smoke test a human ran; `PHASE_N_REPORT.md` (built, assumed, deferred, measured, three decisions for a human); `PROGRAMME_STATE.md` updated; `DEFERRED.md` re-read and every item given a phase or deleted with a reason; §26 recounted; **the `STANDARDS.md` rows for this phase are checked (one minute each, by a human), and no new gap was introduced without a row** (added 2026-09-04 by the standards audit).

Also in `CONSTITUTION.md` (Engine and layout): "the renderer to Unity" → "the renderer to Godot (recommended — not yet decided, see DECISIONS.md row D-ENGINE-1)".

## 2. Sentences moved out of CONSTITUTION.md

Everything below the rules, Engine and layout, and Cross-cutting moved to `PHASES.md` (Step 1), text unchanged except where §3 says otherwise:

| moved block | now at |
|---|---|
| Phase 0 — Inventory and doc lock (heading, five bullets, DoD) | `PHASES.md` lines 9–17 |
| Phase 1 — Headless front sim (paragraph, the port bullet, E1–E9, the report bullet, DoD) | lines 19–36 |
| Phase 2 — Map-view prototype → Human gate A (paragraph, bullets, calibration targets, scenarios, test plan, results template, Gate A) | lines 38–54 |
| Phase 3 — Absorb gate A (three bullets, DoD) | lines 56–62 |
| Phase 4 — Vertical slice (paragraph, M1–M6, tick rule, telemetry, SLICE_REPORT, Gate B) | lines 64–78 |
| Phase 5 — The factory, complete | lines 80–86 |
| Phase 6 — Threat, complete | lines 88–94 |
| Phase 7 — Found tech | lines 96–102 |
| Phase 8 — Territory tools | lines 104–110 |
| Phase 9 — The city | lines 112–118 |
| Phase 10 — Progression and endgame | lines 120–126 |
| Phase 11 — Performance and scale (incl. the Engine gate) | lines 128–134 |
| Phase 12 — Interface, onboarding, art, sound | lines 136–145 |
| Phase 13 — Balance and Steam | lines 147–153 |
| Phase 14 — Launch | lines 155–159 |
| "First session — Read the constitution. Create `PROGRAMME_STATE.md` if it doesn't exist. Do Phase 0. Write `PHASE_0_REPORT.md`. Stop." | lines 161–163 |
| The standards-audit footnote ("*Standards audit, 2026-09-04 … reported, not edited …*") | line 167 |

One sentence was not moved but dropped: "Read `RELIGHT-design.md` in full before anything else. Then read every `*_REPORT.md`, `TEST_RESULTS*.md` and `DEFERRED.md` that exists. Then inventory `packages/`." Its job is done by the six-file read order that opens `CLAUDE.md` since Step 2 (`CONSTITUTION.md` → `ROADMAP.md` §0 → `PROGRAMME_STATE.md` → the phase prompt → `DECISIONS.md` (not decided) → `DEFERRED.md`). `CONSTITUTION.md` line 5 points at it. If the human wants the sentence kept verbatim, `PHASES.md` line 3 is where it belongs.

## 3. Stale sentences corrected in PHASES.md

The three the standards audit reported, plus two the move found. Old → new:

- **Phase 5, line 82** — "belts, undergrounds, splitters, three inserters, chests" → "belts, undergrounds, splitters, one inserter (§13) — [open: D-INSERTERS-1], chests".
- **Phase 5, line 82** — "power complete with the shed order and the dimming on the map view" → "power complete with proportional brownout (D-B3-4) and the dimming on the map view". (Phase 6's bullet and DoD were checked and carry no shed-order sentence; Phase 1's E2 line at 26 keeps "the shed order" as the history of the experiment that replaced it.)
- **Phase 6, line 90** — "Wells with the four-neighbour kill and the Relight surge flag" → "Wells with the all-neighbours kill (D6) and the Relight surge flag".
- **Phase 12, line 140** — "Save/load from the sim's JSON with versioning, autosave hourly, saves inspectable in tools" → "Save/load from the sim's JSON with versioning, autosave every 10 minutes or less, three rotating slots (STANDARDS row 8.1), saves inspectable in tools".
- **Phase 4, lines 68–73 (M1–M6)** — the six prompt-era lines ("M1 Ground — 32×32-tile blocks, 8-wide streets …", "M2 Flow — Excavator (3×3, 0.5/s onto an adjacent belt) …", "M3 Defence — turret … power as one number with the shed order …", "M4 Threat — rot as tile presence … no hulks", "M5 Light — a single-channel 768×768 light texture …", "M6 The hour — a bot follows §11's minute list …") → what `SLICE_REPORT.md` says was built, each marked "(built, "Prompt B Mn")": M1 Ground and the engineer on the street-first city (D6); M2 Flow on the faces; M3 Defence on the segment (kits per segment, D-B1-4); M4 Threat, and the rifle (turrets have no HP, D-B4-3); M5 Light as a one-byte-per-tile light map with the streetlight sequence; M6 The hour with the command log and the rifle-off replay (`E-hour`). The full old text is `git show 2286b7d:docs/CONSTITUTION.md` lines 95–100.

The footnote at `PHASES.md` line 169 records the same list.

## 4. DECISIONS.md status changes

Step 4 (ddf36f4) rewrote `DECISIONS.md` as one eight-column table (`id | question | recommendation | status | decided by | on | via | doc sentence`, 78 rows) under rule 11. **No row is `decided`**: no row named a person, a date and a message together. The earlier free text of each row is kept in `doc sentence`; the pre-rewrite file is `git show 2286b7d:docs/DECISIONS.md`. Status per row (old wording → new status):

- **→ provisional** (acted on by the assistant's recommendation on a `go`, or a programme lock, with no human word on the row; reversible under rule 14): C1, C2, C3, C4, C8, C9, D-CI-nightly, D-P2-1, D-P2-2, D-P2-3, D-P3-1 … D-P3-11, D-P4-1, D-P4-2, D-P4-3, D-P4-5, D-P4-6, D-R1, D-R2, D-R3, D-R4, D-B4-1, D-B4-2, D-B4-3, D-B5-1, D-B5-2, D-B5-3 (36 rows).
- **→ recommended** (a human's words are quoted or "human"/"the human"/"the owner" is written, but no name, or no message/commit cited; or an open row that has a recommendation): C5, C6, C7, C10, D1, D2, D3, D4, D-CI, Gate A, D-P1-1, D-P1-2, D-P1-3, D-P4-4, D-P4-7, D-P4-9, D5, D6, D-B1-1 … D-B1-5, D-B3-1 … D-B3-4, D-B6-1, D-B6-2, D-B6-3, D-SA-1 … D-SA-5 (39 rows). D-B3-4 is recommended, not provisional: reversing it would move the fixtures (rule 14 b), so the build stands as is until a human writes the row.
- **→ superseded**: D-25-13 (by D-P1-2), D-P4-8 (by D-B1-4, unchanged).
- **new**: D-ENGINE-1 (recommended: Godot over Unity for the Phase 11 fallback), D-INSERTERS-1 (open: one inserter or three in Phase 5).
- **unchanged**: D-B2-1, D-B2-2, D-B2-3.

The rows most in need of the human's name, date and message are the ones the build already stands on: D-P1-2, D-P1-3, D-B1-4, D-B3-4, D5, D6, Gate A, D1, D-CI. The `DECISIONS.md` intro names "D-B5-4" because `ROADMAP.md` §0 line 3 (verbatim from the task) names it; no such row exists — a human should say which row was meant.

## 5. Constants that disagree

Step 5 (4c066b2) made `packages/sim/src/constants.ts` the one source for the tempo constants and rewired the readers that already agreed (`recipes.ts`, `flow.ts`, `ground.ts`, `threat.ts`, `hour.ts`, `firsthour.ts`, `sim.ts`, `types.ts`) without changing a value. `docs/RELIGHT-design.md` §13 gained "The constants the sim is built from" and a docsync-generated table (no number changed). Where the doc, the calibration config (`PROTO_CALIBRATED` / `FIRST_HOUR_DEFAULTS`) and the code disagree, `npm run docsync:check` now prints the list and exits 1 — **so CI's docsync step is red on `main` by design until a human resolves each line**. The eight, as printed:

1. Assembler rate: doc says 20 mag/min (one tier), calibration says the start "Mk1" makes 10 mag/min (PROTO_CALIBRATED.startAsmRate), code says 20 mag/min (the tile assembler's 3 s recipe).
2. Turret hopper: doc says 50 rounds a turret, calibration says 100 rounds an edge (the block sim's edge hopper, "two turrets per edge", D-P1-3 rider), code says 50 a turret.
3. Start chest: doc says 200 steel / 100 copper / 50 stone / 20 magazines, calibration says 80 / 40 / 0 / 20 (PROTO_CALIBRATED.eco.startStock, startRounds), code (the game's chest, flow.ts START_CHEST) says the doc's numbers (D-B1-1, D-P4-4).
4. Substation draw: doc says 100/20 kW (D1), calibration says 200/40 (draw "doc", the pre-D1 numbers; power is off in the calibration so nothing draws it), code (the tile sessions, ehour, replay) says draw "half" = 100/20.
5. Substation draw: doc says 100/20 kW (D1), firsthour.ts FIRST_HOUR_DEFAULTS says 200/40 (the doc-literal E4 variant; FIRST_HOUR_D1 says 100/20).
6. Streetlight spacing: doc says every 4 tiles (D-B1-4), code says 3 in tiles.ts STREETLIGHT_STEP (the lattice's eight a side, superseded by ground.ts FACE_LIGHT_STEP = 4).
7. Hour Generators: doc (§11 / E4-doc) says minutes 0/6/15/45, calibration (firsthour.ts FIRST_HOUR_DEFAULTS.gens, the doc-literal E4 variant) says 0/30, code (hour.ts HOUR_GEN_AT) says 0/6/15/45.
8. Hour minute list: doc says east in the 10–30 window, west in the same window and north 30–60 ("about minute 40"), with no minute for the third Generator and the Shot line "by minute 10"; calibration (firsthour.ts) says east 15 / west 25 / north 40; code (hour.ts) says the same claims, Generators 0/6/15/45, the Shot line at 8.

## 6. What freshness:check covers and does not cover

`npm run freshness:check` (`packages/tools/src/freshness.ts`, in CI after `npm test`, with `fetch-depth: 0` so ancestry can be tested) reads every generated file and fails if a stamp is missing, if `source_commit` is not an ancestor of HEAD (`git merge-base --is-ancestor`), or if `config_hash` differs from the hash of the same config recipe rebuilt from the checked-out code. The stamp (`packages/harness/src/provenance.ts`) names its config by recipe, not by value — `experiments` (the harness CANON), `calibration` (map, walk, economy, overrides), `snapshot`, `section18`, `seeds` — so a change in `DEFAULT_CONFIG`, `protoCalibrated` or the recipe itself makes the committed hash stale.

Covered (49 files on 6aea2f7, all fresh):

- `docs/EXPERIMENTS.md` and `docs/experiments/E1–E9, E-hour, E-rifle, E-variance, E-walk.json` (first line / top-level keys; nightly's `nightly-<date>.json` and `nightly.md` are stamped the same way when they are written).
- `docs/experiments/calibration.{md,json}` and `calibration-nowalk.{md,json}`; the JSON's own `hash` is the same value.
- `packages/game/public/snapshots/b-compact-seed3.json` (the stamp keys come first, the state follows; `snapshot:check` compares the state with the committed stamp kept, so a moved HEAD is not a moved state; the game's loader ignores the extra keys).
- `docs/section18-{10min,5h,25h}.png` and `docs/seeds/<preset>-<seed>.png` (fifteen), through a `.json` sidecar of the same name.

Covered for ancestry only: `docs/experiments/lattice/` (E1–E9.json, EXPERIMENTS.md, calibration.{md,json}) is the archive of the pre-D6 lattice results that `REWORK_REPORT.md` and `SLICE_REPORT.md` cite as the lattice's record. The files are stamped by hand with the commit that archived them (27cc9b0, verified: no commit since touched them) and their original config hashes (01dc5d02, ee23bb1c); the check prints them as `archive` with the current hash beside them and does not compare. Regenerating them would erase the record; deleting them is a human's call.

Not covered:

- `packages/sim/fixtures/city{3,4,5}.json` (written by `packages/sim/test/_exportCity.ts`): their bytes are the D6 regression set and the tests compare them; adding keys would move them. The fixtures are byte-identical after Step 5 (md5 66daff11…, 7eb7835c…, 67da5753…).
- A dirty working tree at generation time: the stamp records HEAD, not the uncommitted diff.
- A code change that leaves the config hash unchanged (a rule change inside `step()` with the same `SimConfig`): `source_commit` is an ancestor either way. `snapshot:check` and the experiment suite catch that class.
- Two files inside one commit disagreeing with each other: `EXPERIMENTS.md` was older than `E-hour.json` at b666c68 (the `--only E-hour` re-run writes the JSON, not the md), and the regeneration on 4c066b2 moved the E-hour-gate seed-5 row (fights saved/fell 4/3 → 0/7, the JSON's numbers). The check judges each file against the code, not the files against each other.

What the regeneration found: `calibration-nowalk.{md,json}` had last been run at 27cc9b0; on 4c066b2 the columns (amber/red kitted) and numbers moved. Targets unchanged: C1, C2, C4, C5, C6 met; C3, C7, i1, i2 missed, walking and not. `PROGRAMME_STATE.md` line 152's "C1/C2 met without" walking still holds. Everything else (E*.json, `calibration.md`, the snapshot, the eighteen PNGs) is byte-identical apart from the stamp. `DEFERRED.md` line 251 ("Generated-report freshness in full") describes what is now built and awaits the human's re-read.

## 7. Merged PRs

In order, into `main`, each retargeted from its stack parent to `main` after the parent merged; no conflicts:

| PR | branch | merge commit |
|---|---|---|
| #1 Phase 1: headless front sim, E1–E9 harness, docsync, CI | phase-1 | b1810672c006505f3d9fb1487c97a5f898ad2bc7 |
| #2 Phase 2: map-view prototype for Gate A | phase-2 | 7278dd9d7fd718a29b4e86ed8468de3260630415 |
| #3 Phase 3: absorb Gate A | phase-3 | 055d5cbd49fbb2064e6e03c012390446ef3576b7 |
| #4 Phase 4: vertical slice | phase-4 | bb1385c4a4a548927fb78be00bc63a8343f44c1c |

`main` contains 6aea2f7 (PR #4's last commit). Branch protection is not available on the repository's plan, so the merges were not gated on CI; on `main` the docsync step is red by design (§5) and every other step is green locally (`npm test` 121 pass, typecheck, lint, `snapshot:check`, `freshness:check`, experiments 13 with 0 failing). This report's commit lands on `phase-4` after the merge; `git push origin phase-4:main` fast-forwards it if wanted.

## 8. Anything that blocked a step

Nothing. Two notes for the human, neither a block: ROADMAP Risks keeps "Until Step 5 of the guardrails task lands, the same constant can exist …" — Step 5 landed but the eight disagreements stand, so the line is true in substance and stale in wording; and the git stash `pre-guardrails: uncommitted economy-fix work (D-P4-4/7/8)` is restored at the end of the task (its result is in the session's final message).
