# Economy-fix task report — fix the eight constant disagreements, rebuild the economy fix, make main green

Task: the user's message of 2026-09-04 "fix the eight constant disagreements, rebuild the economy fix, make main green". Five steps in order, one commit each on `phase-4`: e472a70 (Step 1), d68b293 (Step 2), ab8d80b (Step 3), 7ddc3c8 (Step 4), and the commit that carries this report (Step 5). Written 2026-09-05. Every number in this report comes from a decided row of `DECISIONS.md` or from a run named in the text.

**Outcome: main is not green.** Steps 1–4 are done. Step 5's cheap checks are green, but the verification pass is red on the decided minutes (E-hour 6 / 10, E-rifle 10 / 12), so `phase-4` was pushed and `main` was not (§7).

## 1. Rows written

Nine rows in `docs/DECISIONS.md`, each with status `decided`, `decided by` **Daniel**, `on` **2026-09-04**, `via` **message "fix the eight constant disagreements" of 2026-09-04** (commit e472a70). The recommendation cell of each, quoted:

1. **D-P4-4** — "Mk1 makes 10 magazines per minute (one every 6 s). The faster rate (one every 3 s, 20/min) is a separate machine, the Mk2. §12 gets one row for Mk1 and one for Mk2. The §11 first hour uses Mk1."
2. **D-B1-1** — "200 steel, 100 copper, 50 stone, 20 magazines, 40 coal. The 40 coal are extra; Generator 1 also starts with its own hopper coal as now. `PROTO_CALIBRATED` keeps its old numbers (80 steel, 40 copper, 0 stone, 20 magazines), frozen; docsync must not compare them to the doc."
3. **D-P4-7** — "Generators at minutes 0, 6, 15, 45. The coal Excavator and its belt are built before Generator 2. A second steel Excavator at about minute 15."
4. **D-P4-8** — "Six turrets, all hoppers start full; the chest also holds 20 magazines (D-B1-1). §11 minutes 0–10 rewritten so the first hand-feeding happens when the first hopper empties at about minute 6."
5. **D-B5-4** — "7 tiles (reaches the middle of the street; a Held block lights its own half). Broken streetlights are still repaired by hand (D-B5-3)."
6. **D-B1-4-rider** — "Not a separate constant; computed as (number of turrets on the segment) × 50 rounds."
7. **D-ENGINE-1** — "Godot."
8. **D-INSERTERS-1** — "One. §13 already says one. A faster inserter may be added later as found tech only if E10 shows a chain that cannot reach its rate."
9. **D-HOUR-1** — "In `packages/sim/src/constants.ts`, constant `HOUR`; the §11 minute table in the design doc is generated from it by docsync. Minute list: Shot assembler line at minute 8; Generator 2 at minute 6 (after the coal Excavator); Generator 3 at 15; east claim at 15; second steel Excavator at 15; west claim at 25; north claim at 40; Generator 4 at 45. §11 prose '10 to 30 minutes' / '30 to 60 minutes' stays as prose; the generated table is the rule."

## 2. The eight fixes

The eight disagreements of `GUARDRAILS_REPORT.md` §5, fixed in commit d68b293; `docsync:check` reports zero disagreements; fixtures `city3/4/5.json` byte-identical (66daff11 / 7eb7835c / 67da5753).

1. **Assembler rate** — `packages/sim/src/recipes.ts` / `constants.ts`: Mk1 = 6 s = 10 mag/min (`SHOT_MAGAZINE`, `ASSEMBLER_TIERS` Mk1 10), Mk2 = 3 s = 20 mag/min (`SHOT_MAGAZINE_MK2_SECONDS`); §12 gets a generated `assemblers` block with one row each (D-P4-4).
2. **Turret hopper** — `packages/sim/src/constants.ts` `EDGE_TURRETS` = 2; `sim.ts` `DEFAULT_CONFIG.hopper` = `EDGE_TURRETS × TURRET.hopper` (2 × 50), never a constant of its own; docsync compares the block sim to that product (D-B1-4-rider).
3. **Start chest** — `packages/sim/src/types.ts` `PROTO_CALIBRATED` marked prototype-era, frozen at Gate A, excluded from docsync; the game's `START_CHEST` stays 200 / 100 / 50, 20 magazines, 40 coal (D-B1-1).
4. **Substation draw, calibration** — `packages/sim/src/types.ts` `PROTO_CALIBRATED` draw = `'half'` = 100 / 20 kW (D1); only the calibration and snapshot config hashes moved (68d07000 → 0176f61d, 03328db7 → 184f7bc4), every number identical.
5. **Substation draw, firsthour** — `packages/sim/src/firsthour.ts` `FIRST_HOUR_DEFAULTS` draws 100 / 20 (D1); the pre-D1 200 / 40 lives only in `FIRST_HOUR_PRE_D1`, read by one E4 comparison row (`E4-pre-D1`).
6. **Streetlight spacing** — `packages/sim/src/tiles.ts` `STREETLIGHT_STEP` reads `LAMP_STEP_TILES` = 4 (D-B1-4); the lattice side takes floor(24 / 4) = 6 lights (was 8 at 3; reported, not decided).
7. **Hour Generators** — `packages/sim/src/firsthour.ts` reads its Generator minutes from `constants.HOUR`: 0 / 6 / 15 / 45 (D-P4-7).
8. **Hour minute list** — `packages/sim/src/constants.ts` `HOUR`, one ordered table (D-HOUR-1): Shot line 8, Generator 2 at 6 after the coal Excavator, Generator 3 at 15, east 15, steel-2 15, west 25, north 40, Generator 4 at 45; `hour.ts`, `firsthour.ts` and docsync's §11 `hour` block read it; docsync's "north at 60–75" and PROTO-chest comparisons dropped.

## 3. What the stash contained and why it was dropped

The stash held the economy fix's working tree from the M6 verification session: five hunks — `flow.ts` (six start turrets with full hoppers, 40 chest coal, streetlight radius 7 in the light record), `hour.ts` (the hand-feed beat and steel-to-chest), `recipes.ts` (Mk1 6 s and Mk2 3 s), `light.ts` (radius comments), `ehour.ts` (the `E-hour-stock` / `E-hour-coal` sections). By the time this task ran, the parallel session (`session_01GFnpcztBCdSbXdjYj46qPW`, commit 6796d13) had already consumed the stash and built the same fix, so `git stash list` was empty and nothing was popped. Step 3 confirmed every hunk on HEAD by reading the code, and moved the minutes onto `constants.HOUR` (D-HOUR-1, D-P4-7).

The stash was dropped because its changes were re-created on top of constants.ts.

Step 3 also brought §11's 0–10 and 10–30 prose to the decided minutes (three changelog lines: the coal Excavator before Generator 2 at 6 and the Shot line at 8; the first empty hopper fed by hand with E; the second steel Excavator at 15) and set `light.test.ts`'s thresholds to the values measured at radius 7 (commit ab8d80b).

## 4. E-hour result

Run name `B-M6-hour-2` (commit 7ddc3c8; `docs/experiments/E-hour.json`, stamp ab8d80b / config b95922d2; `SLICE_REPORT.md` "M6 re-run after the economy fix" has the full timeline).

**No-fall hour: no.**

| seed | north claimed → Held | brownout | chest steel zero | north fell | Held at 60:00 |
|---|---|---|---|---|---|
| 3 | 40:00 → 40:29 | 40:01 → 45:00 (299 s) | 15:02 | 52:43 (shade) | 3 |
| 4 | 40:01 → 40:34 | 40:01 → 45:00 (300 s) | 15:04 | 48:54 (unfed) | 3 |
| 5 | 40:00 → 40:30 | 40:01 → 45:00 (300 s) | 15:02 | 49:55 (unfed) | 3 |

Both no-fall conditions fail on every seed: north (claimed at 40, D-HOUR-1) falls inside the hour, and chest steel reaches zero at the minute-15 cluster (the second steel Excavator's kit, Generator 3 and east's kits, all at 15:00–15:05). East at 15 and west at 25 hold. The rest of the hour: first crawler 2:44–3:05, first red pip 5:33–6:18 with the hand-feed 3 s later, Generator 2 at 6:01–6:02, the Shot line at 8:01–8:10, Generator 4 at 45:00.

**The two-claim variant** (`E-hour-north`: north at 65:00, the run carried to 75:00, the value of D-P4-10's recommendation): no fall and no brownout inside 60 min; north Held at 65:29–65:34, the Generators dry at 67:57–68:06, north fell at 72:45–72:47 (shade) and 74:38 (unfed). Chest steel still reaches zero at 15:02, so this variant is not a no-fall hour either. No design-doc number was changed; the variant is evidence for the human's D-P4-10 and the new recommended row D-HOUR-2.

## 5. The two M6 checks

| check | seed 3 | seed 4 | seed 5 |
|---|---|---|---|
| West's coal reaches a Generator | never (GA-B6-3: west's coal is the block-level stand-in) | never | never |
| Chest coal first zero (Generator 2's take) | 6:01 | 6:02 | 6:01 |
| Chest coal out for good | 54:00 | 54:00 | 54:00 |
| Coal margin to the hour | −6.0 min | −6.0 min | −6.0 min |
| First red pip | 6:09 | 5:33 | 6:18 |
| Crawlers spawned by the first red pip | 60 | 44 | 44 |
| Crawler arrivals by the first red pip | 0 | 0 | 0 |

Both are in `EXPERIMENTS.md` as `E-hour-m6checks` (`B-M6-hour-2`).

## 6. Verification pass

Run on commit 7ddc3c8 plus the doc edits of this step.

| check | result |
|---|---|
| `npm test` | green — 121 / 121 |
| `npm run typecheck` | green (includes the game build) |
| `npm run lint` | green |
| `npm run docsync:check` | green — zero disagreements |
| `npm run snapshot:check` | green (config 0176f61d) |
| `npm run freshness:check` | green |
| `npm run experiments` | **red**, exit 1 after 4 m 23 s — E-hour 6 / 10 (steel never zero, no fall, no brownout, the §11 end state all red); E-rifle 10 / 12 ("nothing falls in the hour, rifle off or on": north falls 2 / 2 / 2; "the engineer is never knocked down in a rescue": HP min 0, down × 1 in the 600 s edge rescue on seed 5 at 35:00); the other eleven experiments green |
| `npm run calibrate` | green — walk (184f7bc4) and nowalk (0176f61d) identical to the committed files except the stamp (source commit e472a70 → 7ddc3c8) |
| 900 s headless soak | green as a run — 0 page errors, 0 console errors, 19,467 frames, sim 0:00 → 59:48 at 4× on seed 3 with the rifle on (`?autoplay=hour&rifle=1&view=world&seed=3`, Playwright chromium, headless swiftshader — for regressions only): mean 45.8–50.0 ms a frame (20.0–25.6 fps) by minute, worst frame 93.3 ms, the engineer never down. The played hour in the browser shows the minute-15 cluster harder than E-hour: chest steel zero at 15:04, **the east claim refused at 15:06 (2 steel in the chest) and never retried**, so east is never Held, north Held at 40:29 and fell at 47:57; the bot's hand fights 17 / 17 held. The two extra findings against E-hour (east never Held; the coal takes at 54:01 and 59:02 refused, the chest empty) are evidence for D-HOUR-2 and D-P4-10, not a number change |
| `git push origin phase-4:main` | **not done** — main would be red (§7); `phase-4` pushed, PR #5 updated |

`docs/experiments/*.json` and `EXPERIMENTS.md` were regenerated by the full run and are committed with this report (stamp 7ddc3c8).

## 7. Anything that blocked a step

1. **The parallel session.** While this task's rows were being written, a second session (`session_01GFnpcztBCdSbXdjYj46qPW`) had already built the economy fix on `phase-4` (commit 6796d13) with north at 65 (D-P4-10's recommendation) and the second steel Excavator at 12 (GA-EF-1, provisional), consumed the stash, ticked ROADMAP §0 lines 1–4 and measured a green hour on those numbers. This task's decided rows (D-HOUR-1: north 40, steel-2 15) override them, and on the decided minutes the hour is not no-fall and E-hour / E-rifle are red. Nothing was changed to make them pass (the task's rule); ROADMAP §0 lines 1, 2 and 4 were un-ticked with the reason on each line, line 3 stays ticked (its evidence is §1–§3), no human line was ticked.
2. **Step 5 (3), "make main green", cannot be done.** `npm run experiments` is red on the decided minutes, so `git push origin phase-4:main` was not run; `phase-4` was pushed and PR #5 carries the branch. Main stays at bb1385c (PRs #1–#4). Two recommended rows wait for the human: D-P4-10 (north's minute and the coal after the HQ patch) and D-HOUR-2 (the minute-15 steel cluster). Each changes a decided minute, so neither is taken provisionally.
3. **Steps 1–4**: nothing blocked them. The soak script exists only in the session scratchpad (`soak.cjs`), not in the repo — recorded in `DEFERRED.md`.
