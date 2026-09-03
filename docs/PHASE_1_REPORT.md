# Phase 1 report — headless front sim

Date: 2026-09-03. Branch `phase-1`. Suite: `npm run experiments` (9 experiments, three seeds each, ~45 s), results in `docs/EXPERIMENTS.md` and `docs/experiments/*.json`.

## Where the sim and the doc disagreed

Listed before any doc edit, as the constitution asks. "Sim" is the TypeScript sim, first at config hash `7637b6e3` (canonical map 24×22, 300 start rounds) and, after the human chose 24×24 (D-P1-1) and 20 magazines (C10), at `01dc5d02` (canonical map 24×24, scattered inert 9 %, §18 claim cadence, production on, unfed substation N40, D1 draws, ~700 coal patch). Each line: doc claim → sim result → run → what was done.

| # | § | Doc said | Sim says | Run | Done |
|---|---|---|---|---|---|
| 1 | §4 | standard city 24×24 cells | the canonical map, the §18 drawings and every run were 24×22 (row 21 the river) | E8-cadence | **human chose 24×24 (D-P1-1)**: the sim map is now 24×24 with row 23 the river, every run below is re-run on it, §4 says so; the §18 drawings stay the 24×22 sketches until Phase 2 |
| 2 | §5 | residential edge 1.5 mag/min, civic 0.9 over 5 h compact | 1.77 and 1.07 | E3-compact-shade0.3-hulk0.5 | doc → 1.8 / 1.1 |
| 3 | §5, §19, §23 | unfed edge loses its block 5–8 min after the pip goes red | mean 7.2 min, 4.3–19.6 across seeds | E1-starve-substation-N40 | doc → "about 7 minutes (4–20)" |
| 4 | §5, §14, §24 | 15 % shortfall cost 3 blocks (substation-first), 25 % cost 9; machines-first 0 and 2; 1 MW headroom 0 | 0–2 (mean 0.7) and 6; machines-first 0 and 0; headroom 0; knock-on draw is +80 kW under D1 | E2-matrix | doc → sim numbers |
| 5 | §5, §15 | first well killed hours 6–9 by enclosure | the enclosure rule is in the sim and never fires under any bot: the compact bot claims the well block itself at 5–18 h (mean 11.5) at the §18 cadence, before it is ever enclosed; 0 wells dead, 1–2 of 5 taken by 25 h | E8-wells | doc says wells are taken head-on at 5–18 h; enclosure rule kept and marked unexercised (GAME-ASSUMPTION: a well block is claimable like any dark block) |
| 6 | §7 | hulks in 25–47 % of well/outskirts blooms vs 4 % of deep industrial | 30–41 % over 25 h (10 % in the first five hours), industrial 17 % | E3-compact-shade0.3-hulk0.5, E9-district | doc → sim numbers |
| 7 | §7, §9, §17, §19, §27 | spike costs 3.2× compact for the same 34 blocks (14,152 vs 4,414), 1.6× on open ground; §9/§19/§27 said 2.2× | 2.6× at the §18 cadence, spike 42 blocks vs compact 52 (16,768 vs 6,458); 1.3× on open ground (17,375 vs 13,541) | E6-shape, E7 | one ratio everywhere: 2.6× / 1.3× |
| 8 | §17, §19, §25 item 7, §27 | quiet-block play costs 1.31× (or 0.97×) compact on the scattered map, 0.56× on open ground | 1.07× scattered, 0.57× open | E6-shape | doc → 1.07× / 0.57× |
| 9 | §9 | industrial edges at 2.2 mag/min, civic 0.9 | 1.8 and 0.9 at steady state | E3-block | doc → 1.8 |
| 10 | §11, §25 item 14 | HQ coal patch ~3,000 units, mines out at minute 106 | D1 sets ~700; it mines out at minute 29 at one Excavator (450 → 21 min, 1,000 → 39, 3,000 never in hour one) | E4h-patch700, E4h-patch450, E4h-patch1000, E4h-patch3000 | doc → ~700, minute 29; item 14 closed |
| 11 | §11 | no brownout, coal never runs out in hour one | no brownout; 609 of the 740 coal in reach are burned by minute 60, so west's rubble is needed from about then | E4h-gen@0,6,15,25 | doc reworded |
| 12 | §11, §15, §19 | first shade ~47 min compact / 17 min spike (tagged to Python runs); first hulk 99–154 min | 32–47 min compact, 17–18 spike; first hulk 152–172 min compact, 49–62 spike | E3-first | shade and hulk retimed, tags renamed |
| 13 | §12 | mid ammo 60–150 mag/min + 10–20 shells; late 150–250 + 30 | four assemblers meet 67 at 5 h, 52 at 8–12 h, 80–84 from 15 h and plateau at ~85 because blocks fall (111–164 lost by 25 h); eight assemblers from hour 4 meet 145–165 + ~20 shells at 25 h | E9-hourly, E9-hold | doc → 50–85 mid, 85–165 late |
| 14 | §12, §15 | power 1.0–3.1 / 3.1–5.5 / 5.5–8.6 MW; §15 said 4.5 MW at 3 h, 5.5 at 4 h, 7.6 at 10 h | 1.06 MW at 1 h, 2.0 at 3 h, 2.6 at 4 h, 3.3 at 5 h, 4.6 at 10 h, 6.0 at 15 h, 7.7 at 20 h, 9.4 at 25 h | E2-demand | doc → 1.0–2.0 / 2.0–5.2 / 5.2–9.4; §15 → 2.0, 2.6, 4.6 |
| 15 | §15 | early 12–25 blocks, mid 25–100, late 100–200; front peaks 50–70 edges; first interior ~40 min | 28 blocks at 3 h, 136 at 12 h, 292 at 25 h; front peaks at 43; compact bot's first interior 60–86 min | E8-hourly, E7 | doc → sim counts; 50–70 kept as the human-shape guess |
| 16 | §16, §25 item 5 | wake-size blooms not in the sim; 18–29 lost at parity | both surges are in the sim: 1.5× demand, peak 2.25×, 17–21 lost at 4 assemblers, 29–33 at 6, 21–30 at 8, vs 4 without the surge; at 8 assemblers a 4,000-magazine bank holds on the seed where it is full and loses 39–42 where it drained; 8 assemblers from hour 4 + uncapped bank (~13,700 magazines) lose 0 on all seeds; a 4-assembler bank never fills (80 mag/min under 84 demand), so the D4 check compares eight assemblers with and without one | E9-hold | doc → sim numbers; D4 stands; bank size open (decision 3) |
| 17 | §18 | sim holds 34/24/12 at 5 h and 184/31/155 at 25 h (8-minute cadence) | at the §18 cadence 52/19/36 and 292/43/253 | E8-cadence | captions updated; drawings untouched (24×22 sketches, redraw in Phase 2) |
| 18 | §18 | ammo demand ≈ 61 mag/min at 5 h | 67 at 5 h, 52 an hour later | E9-hourly | doc → 50–70 |
| 19 | §19 | turtle cost 528 magazines over three hours | 878 over five hours (176 an hour, the same rate) | E7 | doc → 878 / 5 h |
| 20 | §19, §24 | spike line 0–15 % (or 7–30 %) short lost 18–23 blocks (open) / 4–11 (scattered) / 4 | at ~78 % of demand by 3 h and ~96 % by 5 h with an empty buffer from 2 h the spike line loses 10 (9–12) in five hours on industrial and well edges; compact at 60 % load loses none | E1-ring-spike, E5-spike-base, E7 | doc → 10 (9–12) |
| 21 | §19 | claim cadence one per 8 minutes | §18 cadence: 15 min in hour one, then one per 5 | E8-cadence | doc → 5 |
| 22 | §24 risk 6 | cheapest used 56 % of compact's ammo, had 20 interiors, 131 % scattered | 57 % open, 107 % scattered | E6-shape | doc → sim |
| 23 | §25 item 2 | inert-as-Dark costs the river policy 21 % more ammo, first enclosure 1.5–2.5 h later | +26–28 %, front/held 0.40 vs 0.37; compact unchanged; validators 0–1 squares, identical totals | E6-river, E6-validators | doc → sim |
| 24 | §25 item 8 | outskirts/well edge cost rests on 15 blooms | 25 h: outskirts 4.4 + 2.0 shells (hulk 41 %), well 5.2 + 1.5 (30 %) | E9-district | item closed |
| 25 | §25 item 10 | cadence table 64/364, 44/244, 34/184 at 4/6/8 min | identical, plus 52/292 at 5 min | E8-cadence | 5-minute row added |
| 26 | §25 item 11 | late demand not measured | measured to 25 h | E9-hourly, E9-hold | item closed |
| 27 | §25 item 13 | spike scattered loses nothing; closed as a choice | loses 10 (9–12), holds 42 vs 52 at 2.6× the ammo | E7, E1-ring-spike | reopened (D-25-13), closed by decision 2: a priced choice |
| 28 | §12 ammo chain | turret hopper 50 rounds | the sim's edge hopper is 100 rounds | E1-ring-spike | not edited: GAME-ASSUMPTION that an edge carries two turrets (§3's HQ has two), so 2 × 50 = 100; the world view settles it |
| 29 | §13 | Assembler 100 kW | sim charges 220 kW per assembler *line* (100 kW Assembler + two 60 kW Excavators) | E2-demand | not a contradiction; noted so nobody "fixes" it |

Confirmed unchanged by the runs: first shade 17 min on a push; "nothing cascades once supply is restored"; the §18 cadence table; validators irrelevant; the wells' ×3 surge shape; D2's 90 s fall.

Stale run names outside the changelog were renamed to the sections and rows of `docs/EXPERIMENTS.md`. Python-only runs that were not re-run stay tagged with their Python name and are listed under Deferred: E10-bloom-cadence.

## Built

- **`packages/sim`** — the Python front sim ported to pure TypeScript, plus the power model (`firsthour.ts`) that was only in the Python phase-5 scripts. Fixed tick, deterministic PRNG, JSON state, no I/O. Districts, enemies and recipes are exported data (`districts.ts`, `enemies.ts`, `recipes.ts`) — the doc's tables are generated from them. Six bot policies: compact, spike, balanced, cheapest, river, turtle.
- **Fixtures** — Python-exported traces (`packages/sim/fixtures/*.json`, including three power fixtures) replayed under `npm test`; the TS sim matches the Python sim tick for tick.
- **`packages/harness`** — E1–E9 as named runs, three seeds each (`docs/EXPERIMENTS.md`, `docs/experiments/E<n>.json`), every check machine-evaluated, `npm run experiments` exits non-zero on a red check. A nightly mode (`--nightly --seeds-n`) for the seed distribution and the 25 h runs.
- **`packages/tools/src/docsync.ts`** — regenerates the §7 district and enemy tables and the §12 recipe table in `RELIGHT-design.md` from the sim; `--check` fails CI on drift.
- **Lint and CI** — ESLint 9 flat config; `.github/workflows/ci.yml` on every push and PR (lint, `tsc --strict`, fixtures, E1–E9 at three seeds, docsync check, experiment artefacts); `.github/workflows/nightly.yml` at 03:00 UTC committing `docs/experiments/nightly*` to `main`.
- **Repo** — `github.com/Deedubsy/Relight`, private, npm workspaces, this work on branch `phase-1` as PR #1 to `main` (CI green on the PR in 1 min 45 s). **`main` is not yet protected:** GitHub refuses rulesets and branch protection on a private repo under the free plan (HTTP 403 "Upgrade to GitHub Pro or make this repository public"). Either is a human choice; the ruleset to apply (PR + `ci` check, Actions app bypass for the nightly commit) is written up in `DECISIONS.md` D-CI.
- **Doc pass** — 27 edits with changelog lines, every stale run name retagged, `[sim: …]` tags now resolve to `docs/EXPERIMENTS.md`.

## Assumed (GAME-ASSUMPTION)

- **Edge hopper.** The sim's edge holds 100 rounds; §12 says a turret hopper holds 50. Read as two turrets per edge. Phase 4 M3 decides turrets per edge; the sim constant follows.
- **Wells are claimable.** The sim lets a bot claim a well block and the doc's "well within three blocks" bonus is modelled as +0.30 cap, ×4 g for the neighbours; a *dead* well (§5) has no rule in the sim beyond the block itself. Nothing in E8-wells exercises well death; the numbers there are for taking wells head-on.
- **Assembler line = 220 kW.** The sim charges Assembler + two Excavators per ammo line; §13 lists the Assembler at 100 kW. Noted in §14, not a contradiction.
- **Start ammo 200 rounds (C10 made: 20 magazines).** The sim default followed the doc once the human left the rider to the report's recommendation. The margin is thin and now stated in §11: with 200 rounds spread over the HQ's edges the civic-facing edge is empty at the first bloom (~3 min), and 32 unfed arrivals accrue before the ammo line refills at minute 10; the canonical 40-arrival rule holds, a 10- or 20-arrival rule loses the HQ at minute 8 (`E1-ring-substation-N10`, `-N20`, which are sensitivity rows, not the rule). At 300 rounds the first unfed arrival was at 8.5 min and fewer than 5 accrued.
- **Map 24×24 (D-P1-1 made).** Row 23 is the river, so 23 rows are claimable; the HQ, the target and the three southern wells moved down two rows with it and the two northern corner wells stayed (GAME-ASSUMPTION: the generator in Phase 9 owns well placement; the sim's five are hand-placed). The district bands stretch by one row each for industrial and the mixed band (`zoneOf(x, y, h)`); the fixtures keep their own 24×22 geometry and pass bit for bit. Scattered inert cells are 9 % of the larger map: 50, not 45.

## Deferred

- **E10-bloom-cadence** — the one Python-only number left; not ported (Gate A feel decides, DEFERRED.md).
- **Python E11/E12/E13** — superseded by E9-hold, E7 and E6-river; not re-run as such.
- **Python sim** — kept as the fixture exporter for one more phase per D-CI; `frontsim_legacy_backup.py` deleted.
- **Open-ground distribution, nightly at 10,000 seeds** — the harness supports both; the hosted cron runs 1,000 seeds (see Measured), 10,000 is a dispatch or self-hosted run.
- **Linear GitHub integration** — a GitHub UI step; not done.
- The 29 tile-scale untagged numbers stay routed to Phases 4–9 (see DoD).

## Measured

Every number is at config hash `01dc5d02` (24×24, 200 start rounds), three seeds, 5 h unless stated; ranges are min–max across seeds. The runs are sections of `docs/EXPERIMENTS.md`. The 24×22 / 300-round numbers this report first carried are in the PR history (commit `f94a7a1`); what moved when the map grew is listed under "Re-run at 24×24" below.

| What | Result | Run |
|---|---|---|
| Unfed substation fall | 7 min typical (4–20) | E1-starve-substation-N40 |
| Minimum start ammo | 200 rounds never loses the HQ under the 40-arrival rule (32 arrivals accrue); 10 or 20 loses it at minute 8 | E1-start-min, E1-ring-substation-N10 |
| Spike ring, creation-order feed | loses 10 (9–12), holds 42 vs compact's 52 | E1-ring-spike, E7 |
| Wake-bloom rounds | 62/34 · 84/49 · 120/71 (civ/res/ind, first/steady) | E3-block |
| Steady-state edge draw | res 1.8, civ 1.1 mag/min | E3-compact |
| Cascade on a 15 % shed | 0–2 blocks (0.7); 6 at 25 % substations-first, 0 machines-first | E2-matrix |
| Power demand | 1.0–2.0 / 2.0–5.2 / 5.2–9.4 MW early/mid/late | E2-demand |
| Coal patch ~700 | mines out at minute 29 (450 → 21, 1000 → 39, 3000 never in 60 min) | E4h-patch700 |
| Hulk share of pressure | 30–41 % of well/outskirts blooms (doc said 11 %) | E9-district |
| Spike vs compact ammo | 2.6× scattered (16,768 vs 6,458), 1.3× open | E6-shape, E7 |
| River policy | +26–28 % over compact | E6-river |
| Turtle (HQ only) | 878 magazines in 5 h, holds 1 | E7 |
| Claim cadence at §18 | 52 / 19 / 36 at 5 h; 292 / 43 / 253 at 25 h (held / front / interior) | E8-cadence |
| Wells head-on | first neutralised at 5–18 h (mean 11.5) | E8-wells |
| First interior block | minute 60–86 | E7 |
| Relight hold | window 1.5× the hour before; 8 assemblers + ~13,700 bank loses 0, a 4,000 bank 0 or 39–42 by seed | E9-hold |
| Demand at 5 h | ≈ 67 mag/min over 20 edges (52 at 8 h) | E9-hourly |

### Re-run at 24×24

The map grew by two rows and the start ammo fell to 200 rounds; both changes are in one config hash so nothing separates them except the diagnostic in "Assumed". What moved, old → new: unfed fall range 4–14 → 4–20 min; spike 2.7× → 2.6×, 40 → 42 blocks, losses 12 → 10; cheapest 1.17× → 1.07× compact; first well 13–24 h → 5–18 h (the southern wells moved with the HQ); first shade 47 → 32–47 min; first hulk 162–182 → 152–172 min; Relight window 1.6× → 1.5×, losses at four assemblers 20–28 → 17–21; late power 10.0 → 9.4 MW at 25 h; late demand at four assemblers 84 → 52 at 10 h with the plateau moving from hour 10 to hour 15; 25 h front 39 → 43. Unchanged: turtle 878, held 52 / 292, residential and civic edge draw, 15 % cascade, coal at minute 29, the §18 cadence table. Two harness checks moved with the numbers: the fixture geometry test now uses the fixtures' own 24×22 MapSpec, and the D4 check compares eight assemblers with and without a bank, because a four-assembler bank never fills.

Nightly smoke on the earlier 24×22 map (20 seeds; the file is dropped from the tree so no stale map lingers, the numbers stay here): spike/compact 2.73× mean, p5 2.47, p95 3.19; cheapest/compact 1.14× (1.00–1.30). The first 24×24 distribution comes from the cron. Runtime 74 s for 20 seeds plus the ten-seed 25 h runs → a 5 h run costs ~0.3 s, so 10,000 seeds × 4 policies is ~3.5 h of runner time and ~6,000 hosted minutes a month. The cron therefore runs 1,000 seeds (~20 min); 10,000 is a `workflow_dispatch` or the self-hosted runner from D-CI. **This is a change to D-CI's letter, not its intent** — logged in `DECISIONS.md`.

Speed: `npm run experiments` ~45 s locally; CI ~2 min including install.

## Definition of done

| DoD | Status |
|---|---|
| Experiments green in CI | green locally (0 failing checks); the PR's CI run is the proof |
| Untagged set ≤ ⅓ of Phase 0 (70 → ≤ 23) | **missed: 44.** 29 are tile-scale rows (§13 all sixteen, §14 four, §5 five, §12 four) that no block sim can evidence and Phase 0 already routed to Phases 4–9; the reachable 15 are the design inputs the sim takes as constants |
| Report names contradictions and runs | above, 29 rows |
| Human five-minute smoke test | **pending** — `npm run experiments`, read `docs/EXPERIMENTS.md`, `npm run dev` for the proto (now drawing the 24×24 grid) |
| `PROGRAMME_STATE.md`, `DEFERRED.md`, §26 recount | done; three systems, complexity 5/10, nothing added |

## Three decisions (put to the human 2026-09-03, answered the same day)

1. **Map size (D-P1-1).** Asked: ship 24×22 (every run, every drawing) or 24×24 (§4, the constitution)? **Human: 24×24.** Done: sim map 24×24, every experiment re-run, doc retagged (changelog lines per section), §4 rewritten; the §18 drawings are marked as 24×22 sketches and go to Phase 2's map view (DEFERRED.md).
2. **§25 item 13 (D-P1-2, D-25-13).** Asked: with the ring fed in creation order spike loses 10 blocks (9–12) and pays 2.6× — a choice, a trap, or a rule change? **Human: as recommended.** Recommendation, now applied: a choice the doc prices. §19's "clearly worse but valid" stands, §12 states the demand, §24 risk 8 the price, and §25 item 13 closes with the numbers. The losses are the far end of the ring starving under creation-order feeding; a feed-order rule (nearest the enemy first) is a Phase 4 logistics question, not a Phase 1 rule change (DEFERRED.md).
3. **The gate against a missed DoD (D-P1-3).** Asked: 44 untagged against ≤ 23 — pass on the 15 reachable, or hold Phase 2? **Human: as recommended.** Recommendation, now applied: pass the Phase 1 gate on the 15 numbers a block sim can reach, carry the 29 tile-scale numbers as Phase 0 routed them (Phases 4–9), and restate every later DoD to count only the numbers that phase's instrument can reach (the constitution's counts are otherwise unmeetable by construction). Riders: **C10 = 20 magazines** (sim `startRounds` 200; margin stated in §11); **edge hopper stays 100** = two turrets per edge until Phase 4 M3 draws the ring; **`main` protection** still needs GitHub Pro or a public repo — a human, money-or-visibility call that no commit can make. The gate is otherwise held only by the human five-minute smoke test.
