# Economy-fix task report — fix the eight constant disagreements, rebuild the economy fix, make main green

Task: the user's message of 2026-09-04 "fix the eight constant disagreements, rebuild the economy fix, make main green". Five steps in order, one commit each on `phase-4`: e472a70 (Step 1), d68b293 (Step 2), ab8d80b (Step 3), 7ddc3c8 (Step 4), and the commit that carries this report (Step 5). Written 2026-09-05. Every number in this report comes from a decided row of `DECISIONS.md` or from a run named in the text.

**Outcome: main is not green.** Steps 1–4 are done. Step 5's cheap checks are green, but the verification pass is red on the decided minutes (E-hour 6 / 10, E-rifle 10 / 12), so `phase-4` was pushed and `main` was not (§7).

**Step 6 (2026-09-05, the `/next` run of task T1).** Three defects in the hour bot were found and fixed (§8). They are not constants: the hands were never told to stop, six kits could never fit in the pockets, and the hand-feed beat watched the HQ alone. North is now kitted on every seed and falls on three runs of six instead of six of six. **Main is still not green**, and what is left is not a defect: each remaining red check is decided-constant arithmetic that only the human can settle — D-P4-9 (the falls), D-HOUR-2 (the chest's steel) and D-P4-10 (the brownout). T1 is therefore `blocked` in `docs/PROGRESS.md` on those three rows.

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

## 8. Step 6 — the three hour-bot defects, and what main is still red on (2026-09-05)

Run on `phase-4` at 4dfe9ca plus this step's change to `packages/sim/src/hour.ts`. Every number below is from `npm run experiments` (river city, seeds 3 / 4 / 5, rifle off and on) written to a scratch directory, not to the repo; nothing in `docs/experiments/` was regenerated by this step.

### 8.1 The three defects

**(1) The hands were never told to stop.** §11's minute 0 is "hand-mine 20 steel". The step's `until` ended at 0:20 with 20 steel in the pockets, but nothing cleared `flow.hand.mine`, so the engineer kept digging the same tile while it stood at the workbench and dug it out: **288 steel in the pockets by 5:50 and carried there for the rest of the hour** (seed 3; measured every second). The pockets are 40 stacks and rubble is 50 a stack, so the hoard alone was 6 stacks, with coal and magazines on top — 8 to 11 stacks of 40 gone at every claim. The step now ends with the hands off, sending the same `mineAt (-1,-1)` the game sends on mouse-up (`worldScene.ts:484`). Pocket steel after minute 1 is now 0–2 for the rest of the hour.

**(2) Six kits could never fit in the pockets.** A kit costs `KIT_STACKS` = 10 of `INV_STACKS` = 40 (D5), so **four is every kit one trip can carry**, and with the hoard above there was room for two. `chestCount(st, 'kit')` is `Infinity` and `take` fits silently by free stacks, so the shortfall never reached the log. North has three to four ring edges and was born part-unkitted on every seed — edge 3009 (seed 3), 3135 (seed 4), 2918 and 2920 (seed 5). A kit is spent at the instant an edge is born (D-B1-4), so a second trip cannot repair it: those edges stayed `kit === false`, fired nothing, and north starved. `HOUR_KITS` is now `floor(INV_STACKS / KIT_STACKS)` = 4, the claim's chest stop empties the pockets into the chest before taking kits, and both the kits carried and any shortfall are written to the log. **`kitted-north` is now 40:29 / 40:34 / 40:30 on every seed; before this step it was `never` on all three.**

**(3) The hand-feed beat watched the HQ alone.** A turret standing on a claimed block takes that edge off the ring feed — `hookSyncEdges`: "an edge with physical turrets fires only through them" — so the hands become the only thing that fills it, and a turret hopper is 50 rounds where the ring stand-in is 100. The beat (D-P4-8) filtered `hqTurrets`, so the two turrets §11 carries to north were never in it: on seed 3 north's three edges read 0 rounds from 42:40 to 46:00, waiting for the five-minute rounds run. The beat now covers every turret standing on a Held block, worst pip first and nearest first, and takes a full hopper's magazines per turret.

### 8.2 What the three fixes moved

| | before (4dfe9ca) | after |
|---|---|---|
| E-hour checks | 6 / 10 | 6 / 10 |
| north kitted | never, on every seed | 40:29 / 40:34 / 40:30 |
| falls | 6 of 6 runs | 3 of 6 runs (seed 3 both, seed 4 rifle off) |
| §11's end state | 0 of 6 runs | 3 of 6 runs |
| north falls at | 48:54–52:43 | 47:42–48:03, all `shade` |
| E-rifle "nothing falls" | falls 2 / 2 / 2 by seed | falls 1 / 1 / 0 by seed |
| pocket steel, minute 6 to 60 | 288 | 0–2 |

End state is otherwise right on all six runs: 4 Generators, 7 Excavators, 3 Assemblers, 6 turrets, HQ Held; the check fails only where north fell. Walking is 5.3–7.1 % of the hour (§19's limit is 15), claim walk-overs 13–20 s, reach refusals 0, refusals 0.

### 8.3 The three red checks that are not defects

**(a) North falls — D-P4-9.** All three falls are `shade`: a shade arrives at an edge with an empty hopper, the substation goes off 30 s per unfed shade, and 30 tiles of creep is a fall. The cause is §11's own instruction. **Measured:** with the two carried turrets removed and nothing else changed (a scratch run, not committed), **no block falls on any run, four blocks Held on all six runs, zero shades, E-hour 8 / 10.** Carrying a turret onto north swaps that edge's ring-fed 100-round hopper — filled automatically from a line buffer that had 100–300 rounds spare — for the turret's hand-fed 50-round one, and one engineer on foot cannot keep three of them fed at 40 rounds a minute each. This is exactly D-P4-9's question ("keep the split, or make every front edge physical"), and the row now has a clean measurement on both sides. Removing §11's carried turrets is a design change, so it was not made.

**(b) The chest's steel reaches zero — D-HOUR-2.** 200 start steel (D-B1-1) − 110 at 6:00 (three Excavators, 50 belts, Generator 2) − 48 at 8:00 (the Shot assembler, three inserters, five belts) − 12 at 15:00 (the second steel Excavator and two belts) − 30 (Generator 3, same minute) = **0 exactly**, at 15:02–15:04 on every seed. The decided start stock is precisely the decided minute list's bill through minute 15, with no margin; the second steel Excavator's belt puts the chest back to 1 steel two seconds later. Nothing in the bot's behaviour changes this: pocket steel at 15:00 is 2. Every way out (more start steel, the Excavator earlier, Generator 3 later) changes a decided number, which is D-HOUR-2's own list of options.

**(c) The brownout — D-P4-10.** North's claim stands a 100 kW front substation at 40:00 (D-HOUR-1) and Generator 4 arrives at 45:00 (D-P4-7): 262–315 s of brownout from 40:01 on every run. In the scratch run of (a), where nothing falls to end the deficit early, it is **exactly 300 s on all six runs** — the five minutes between the two decided minutes, to the second. The HQ never becomes interior inside the hour (seed 3) or does so at 40:29–40:33 (seeds 4, 5), so its substation is drawing 100 kW front, not 20 kW interior, for most of the hour.

**(d) E-rifle.** "tile: nothing falls in the hour, rifle off or on" runs the same hour bot, so it is (a) again — falls 1 / 1 / 0 by seed. The other red check, "the engineer is never knocked down in a rescue" (HP min 0 in the 600 s edge rescue), is the rescue scenario and belongs to task **T4**, not to this task.

### 8.4 Checks

| check | result |
|---|---|
| `npm test` | green — 121 / 121 |
| `npm run typecheck` | green (includes the game build) |
| `npm run lint` | green |
| `npm run docsync:check` | green — doc tables match `packages/sim` |
| `npm run snapshot:check` | green — compact seed 3 at 3:00:00 matches, config 0176f61d |
| `npm run experiments` | **red**, 6 failing checks of 13 experiments in 244 s: E-hour 6 / 10 and E-rifle 10 / 12, all six accounted for in §8.3. The other eleven experiments are green |

### 8.5 What this leaves for the human

Three rows of `docs/DECISIONS.md`, each now carrying this step's measurement, and each one a decided number that only the human may move:

1. **D-P4-9** — turrets on claimed blocks. Removing §11's two carried turrets is measured green (no falls, four Held, every run).
2. **D-HOUR-2** — the minute-15 steel cluster. The 200 start steel is the bill to minute 15 exactly.
3. **D-P4-10** — north at 40:00 against Generator 4 at 45:00: 300 s of brownout, by construction.

Until they are settled, "make main green" cannot be finished, so T1 is `blocked` on them in `docs/PROGRESS.md`. The `docs/experiments/` files and `EXPERIMENTS.md` still carry the 7ddc3c8 stamp: they are regenerated by a verification pass, which the user schedules, not by this step.
