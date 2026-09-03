# Phase 1 report — headless front sim

Date: 2026-09-03. Branch `phase-1`. Suite: `npm run experiments` (9 experiments, three seeds each, ~45 s), results in `docs/EXPERIMENTS.md` and `docs/experiments/*.json`.

## Where the sim and the doc disagreed

Listed before any doc edit, as the constitution asks. "Sim" is the TypeScript sim at config hash `7637b6e3` (canonical map 24×22, scattered inert 9 %, §18 claim cadence, production on, unfed substation N40, D1 draws, ~700 coal patch). Each line: doc claim → sim result → run → what was done.

| # | § | Doc said | Sim says | Run | Done |
|---|---|---|---|---|---|
| 1 | §4 | standard city 24×24 cells | the canonical map, the §18 drawings and every run are 24×22 (row 21 is the river) | E8-cadence | doc now says 24×22 and flags the shipped size as a human decision (decision 1 below) |
| 2 | §5 | residential edge 1.5 mag/min, civic 0.9 over 5 h compact | 1.77 and 1.07 | E3-compact-shade0.3-hulk0.5 | doc → 1.8 / 1.1 |
| 3 | §5, §19, §23 | unfed edge loses its block 5–8 min after the pip goes red | mean 7.1 min, 4.3–13.9 across seeds | E1-starve-substation-N40 | doc → "about 7 minutes (4–14)" |
| 4 | §5, §14, §24 | 15 % shortfall cost 3 blocks (substation-first), 25 % cost 9; machines-first 0 and 2; 1 MW headroom 0 | 0–2 (mean 0.7) and 3–6 (mean 5); machines-first 0 and 0; headroom 0; knock-on draw is +80 kW under D1 | E2-matrix | doc → sim numbers |
| 5 | §5, §15 | first well killed hours 6–9 by enclosure | the enclosure rule is in the sim and never fires under any bot: the compact bot claims the well block itself at 13–24 h at the §18 cadence, before it is ever enclosed; 0 wells dead, 1–2 of 5 taken by 25 h | E8-wells | doc says wells are taken head-on at 13–24 h; enclosure rule kept and marked unexercised (GAME-ASSUMPTION: a well block is claimable like any dark block) |
| 6 | §7 | hulks in 25–47 % of well/outskirts blooms vs 4 % of deep industrial | 28–39 % over 25 h (9 % in the first five hours), industrial 11 % | E3-compact-shade0.3-hulk0.5, E9-district | doc → sim numbers |
| 7 | §7, §9, §17, §19, §27 | spike costs 3.2× compact for the same 34 blocks (14,152 vs 4,414), 1.6× on open ground; §9/§19/§27 said 2.2× | 2.7× at the §18 cadence, spike 40 blocks vs compact 52 (16,792 vs 6,178); 1.3× on open ground (17,475 vs 13,542) | E6-shape, E7 | one ratio everywhere: 2.7× / 1.3× |
| 8 | §17, §19, §25 item 7, §27 | quiet-block play costs 1.31× (or 0.97×) compact on the scattered map, 0.56× on open ground | 1.17× scattered, 0.57× open | E6-shape | doc → 1.17× / 0.57× |
| 9 | §9 | industrial edges at 2.2 mag/min, civic 0.9 | 1.8 and 0.9 at steady state | E3-block | doc → 1.8 |
| 10 | §11, §25 item 14 | HQ coal patch ~3,000 units, mines out at minute 106 | D1 sets ~700; it mines out at minute 29 at one Excavator (450 → 21 min, 1,000 → 39, 3,000 never in hour one) | E4h-patch700, E4h-patch450, E4h-patch1000, E4h-patch3000 | doc → ~700, minute 29; item 14 closed |
| 11 | §11 | no brownout, coal never runs out in hour one | no brownout; 609 of the 740 coal in reach are burned by minute 60, so west's rubble is needed from about then | E4h-gen@0,6,15,25 | doc reworded |
| 12 | §11, §15, §19 | first shade ~47 min compact / 17 min spike (tagged to Python runs); first hulk 99–154 min | 47 min compact on all seeds, 17–18 spike (confirmed); first hulk 162–182 min compact, 47–62 spike | E3-first | shade confirmed, hulk retimed, tags renamed |
| 13 | §12 | mid ammo 60–150 mag/min + 10–20 shells; late 150–250 + 30 | four assemblers meet 52 at 5 h, 73 at 8 h, 84 at 10 h and plateau at ~85 because blocks fall (144–164 lost by 25 h); eight assemblers from hour 4 meet 165–180 + ~20 shells at 25 h | E9-hourly, E9-hold | doc → 50–85 mid, 85–180 late |
| 14 | §12, §15 | power 1.0–3.1 / 3.1–5.5 / 5.5–8.6 MW; §15 said 4.5 MW at 3 h, 5.5 at 4 h, 7.6 at 10 h | 1.06 MW at 1 h, 2.0 at 3 h, 2.6 at 4 h, 3.2 at 5 h, 5.0 at 10 h, 6.5 at 15 h, 8.2 at 20 h, 10.0 at 25 h | E2-demand | doc → 1.0–2.0 / 2.0–5.6 / 5.6–10.0; §15 → 2.0, 2.6, 5.0 |
| 15 | §15 | early 12–25 blocks, mid 25–100, late 100–200; front peaks 50–70 edges; first interior ~40 min | 28 blocks at 3 h, 136 at 12 h, 292 at 25 h; front peaks at 39; compact bot's first interior 65–86 min | E8-hourly, E7 | doc → sim counts; 50–70 kept as the human-shape guess |
| 16 | §16, §25 item 5 | wake-size blooms not in the sim; 18–29 lost at parity | both surges are in the sim: 1.6× demand, peak 2.25×, 20–28 lost at 4 assemblers, 24–31 at 6, 30–36 at 8, vs 4 without the surge; a 4,000-magazine bank still loses 34–38; 8 assemblers from hour 4 + uncapped bank (~15,000 magazines) lose 0 on all seeds | E9-hold | doc → sim numbers; D4 stands; bank size open (decision 3) |
| 17 | §18 | sim holds 34/24/12 at 5 h and 184/31/155 at 25 h (8-minute cadence) | at the §18 cadence 52/18/36 and 292/39/257 | E8-cadence | captions updated; drawings untouched |
| 18 | §18 | ammo demand ≈ 61 mag/min at 5 h | 52 | E9-hourly | doc → 52 |
| 19 | §19 | turtle cost 528 magazines over three hours | 878 over five hours (176 an hour, the same rate) | E7 | doc → 878 / 5 h |
| 20 | §19, §24 | spike line 0–15 % (or 7–30 %) short lost 18–23 blocks (open) / 4–11 (scattered) / 4 | at ~98 % of demand by 3 h with an empty buffer from 2 h the spike line loses 12 (12–13) in five hours on industrial and well edges; compact at 55 % load loses none | E1-ring-spike, E7 | doc → 12 |
| 21 | §19 | claim cadence one per 8 minutes | §18 cadence: 15 min in hour one, then one per 5 | E8-cadence | doc → 5 |
| 22 | §24 risk 6 | cheapest used 56 % of compact's ammo, had 20 interiors, 131 % scattered | 57 % open, 117 % scattered | E6-shape | doc → sim |
| 23 | §25 item 2 | inert-as-Dark costs the river policy 21 % more ammo, first enclosure 1.5–2.5 h later | +27–31 %, front/held 0.35 vs 0.43; compact unchanged; validators 0–1 squares, identical totals | E6-river, E6-validators | doc → sim |
| 24 | §25 item 8 | outskirts/well edge cost rests on 15 blooms | 25 h: outskirts 4.2 + 1.9 shells (hulk 39 %), well 5.2 + 1.4 (28 %) | E9-district | item closed |
| 25 | §25 item 10 | cadence table 64/364, 44/244, 34/184 at 4/6/8 min | identical, plus 52/292 at 5 min | E8-cadence | 5-minute row added |
| 26 | §25 item 11 | late demand not measured | measured to 25 h | E9-hourly, E9-hold | item closed |
| 27 | §25 item 13 | spike scattered loses nothing; closed as a choice | loses 12, holds 40 vs 52 at 2.7× the ammo | E7, E1-ring-spike | reopened (D-25-13), verdict is decision 2 |
| 28 | §12 ammo chain | turret hopper 50 rounds | the sim's edge hopper is 100 rounds | E1-ring-spike | not edited: GAME-ASSUMPTION that an edge carries two turrets (§3's HQ has two), so 2 × 50 = 100; the world view settles it |
| 29 | §13 | Assembler 100 kW | sim charges 220 kW per assembler *line* (100 kW Assembler + two 60 kW Excavators) | E2-demand | not a contradiction; noted so nobody "fixes" it |

Confirmed unchanged by the runs: first shade 47 / 17 min; "nothing cascades once supply is restored"; the §18 cadence table; validators irrelevant; the wells' ×3 surge shape; D2's 90 s fall.

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
- **Start ammo 300 rounds.** The sim default is 30 magazines against the doc's 20; E1-start-min shows 20 is enough. Left for the human (C10) rather than changed silently, because it moves the config hash.
- **Map 24×22.** Every run and drawing; the doc and constitution said 24×24 in one place each. The doc now says 24×22 with the human decision flagged.

## Deferred

- **E10-bloom-cadence** — the one Python-only number left; not ported (Gate A feel decides, DEFERRED.md).
- **Python E11/E12/E13** — superseded by E9-hold, E7 and E6-river; not re-run as such.
- **Python sim** — kept as the fixture exporter for one more phase per D-CI; `frontsim_legacy_backup.py` deleted.
- **Open-ground distribution, nightly at 10,000 seeds** — the harness supports both; the hosted cron runs 1,000 seeds (see Measured), 10,000 is a dispatch or self-hosted run.
- **Linear GitHub integration** — a GitHub UI step; not done.
- The 29 tile-scale untagged numbers stay routed to Phases 4–9 (see DoD).

## Measured

Every number is at config hash `7637b6e3`, three seeds, 5 h unless stated; ranges are min–max across seeds. The runs are sections of `docs/EXPERIMENTS.md`.

| What | Result | Run |
|---|---|---|
| Unfed substation fall | 7 min typical (4–14) | E1-starve-substation-N40 |
| Minimum start ammo | 200 rounds never loses the HQ | E1-start-min |
| Spike ring, creation-order feed | loses 12 (12–13), holds 40 vs compact's 52 | E1-ring-spike, E7 |
| Wake-bloom rounds | 63/34 · 84/49 · 110/82 (res/civ/ind, first/steady) | E3-block |
| Steady-state edge draw | res 1.8, civ 1.1 mag/min | E3-compact |
| Cascade on a 15 % shed | 0–2 blocks (0.7), 3–6 substations-first, 0 machines-first | E2-matrix |
| Power demand | 1.0–2.0 / 2.0–5.6 / 5.6–10.0 MW early/mid/late | E2-demand |
| Coal patch ~700 | mines out at minute 29 (450 → 21, 1000 → 39, 3000 never in 60 min) | E4h-patch700 |
| Hulk share of pressure | 28–39 % (doc said 11 %) | E3-block |
| Spike vs compact ammo | 2.7× scattered (16,792 vs 6,178), 1.3× open | E6-shape, E7 |
| River policy | +27–31 % over compact | E6-river |
| Turtle (HQ only) | 878 magazines in 5 h, holds 1 | E7 |
| Claim cadence at §18 | 52 / 18 / 36 at 5 h; 292 / 39 / 257 at 25 h (held / front / interior) | E8-cadence |
| Wells head-on | neutralised at 13–24 h | E8-wells |
| First interior block | minute 65–86 | E9-hourly |
| Relight hold | 4,000-round cap loses 34–38; 8 assemblers + ~15,000 bank loses 0 | E9-hold |
| Demand at 5 h | ≈ 52 mag/min over 14–19 edges | E9-hourly |

Nightly smoke (20 seeds, `docs/experiments/nightly.md`): spike/compact 2.73× mean, p5 2.47, p95 3.19; cheapest/compact 1.14× (1.00–1.30). Runtime 74 s for 20 seeds plus the ten-seed 25 h runs → a 5 h run costs ~0.3 s, so 10,000 seeds × 4 policies is ~3.5 h of runner time and ~6,000 hosted minutes a month. The cron therefore runs 1,000 seeds (~20 min); 10,000 is a `workflow_dispatch` or the self-hosted runner from D-CI. **This is a change to D-CI's letter, not its intent** — logged in `DECISIONS.md`.

Speed: `npm run experiments` ~45 s locally; CI ~2 min including install.

## Definition of done

| DoD | Status |
|---|---|
| Experiments green in CI | green locally (0 failing checks); the PR's CI run is the proof |
| Untagged set ≤ ⅓ of Phase 0 (70 → ≤ 23) | **missed: 44.** 29 are tile-scale rows (§13 all sixteen, §14 four, §5 five, §12 four) that no block sim can evidence and Phase 0 already routed to Phases 4–9; the reachable 15 are the design inputs the sim takes as constants |
| Report names contradictions and runs | above, 29 rows |
| Human five-minute smoke test | **pending** — `npm run experiments`, read `docs/EXPERIMENTS.md`, `npm run dev` for the proto |
| `PROGRAMME_STATE.md`, `DEFERRED.md`, §26 recount | done; three systems, complexity 5/10, nothing added |

## Three decisions for the human

1. **Map size (D-P1-1).** Every run, every drawing and now §4 say 24×22. The constitution says 24×24. Ship 24×22, or re-run E8/E9 at 24×24 before Phase 9's generator?
2. **§25 item 13 (D-P1-2, D-25-13).** With the ring fed in creation order, spike loses 12 blocks and pays 2.7×. Is that "clearly worse but valid" (a choice), a trap the doc should warn about, or a rule to change (feed the front nearest the enemy first)?
3. **The gate against a missed DoD (D-P1-3).** 44 untagged against ≤ 23. Pass Phase 1 on the 15 the sim can reach and carry the 29 as routed, or hold Phase 2 until the count is met? Riders: C10 start ammo (20 or 30 magazines — the sim follows), the edge hopper (two turrets per edge or the sim's 100 → 50), and **protecting `main`** (GitHub Pro, or make the repo public — D-CI's "protected main" is otherwise unenforceable).
