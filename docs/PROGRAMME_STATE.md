# Relight — programme state

Living file. One entry per phase, newest first; the current phase is the top one. Rules are in the programme constitution (the prompt that started Phase 0); the spec is `RELIGHT-design.md`; the sim is the judge.

**Current phase: 1 (built 2026-09-03; the three Phase 1 decisions are made — 24×24, spike a priced choice, gate passed on the reachable 15 — and the gate waits only on the human five-minute smoke test; `PHASE_1_REPORT.md`). Next: Phase 2 — map-view prototype on the TS sim (`packages/proto` becomes the Phase 2 artefact), Gate A.**

Gates passed: none. Gate A (`TEST_RESULTS.md` `verdict: go`) has not been run; the file is a template with `verdict: pending`.

Housekeeping the constitution assumes and the repo does not have (a human decides how, not whether):

- ~~`/mnt/e/Factorio2` is not a git repository and has no CI.~~ Done in Phase 1: `github.com/Deedubsy/Relight` (private), branch `phase-1` → PR #1 to `main` (CI green), `.github/workflows/ci.yml` and `nightly.yml`; `main` protection is refused on a free-plan private repo (D-CI, human choice). **Decided (D-CI, 2026-09-03):** GitHub private repo, GitHub Actions, npm workspaces; push CI = lint + `tsc --strict` + fixtures + E1–E9 at three seeds; nightly = 10,000-seed and 25 h runs to `docs/experiments/`; Python sim stays as fixture exporter for one phase, then retired. Linear for tasks. Phase 1's first task is to set this up.
- Reports live at the repo root, not in `docs/`. Left where they are (Phase 0 changes nothing else); Phase 1 may move them under `docs/` and leave root stubs.
- `packages/harness` and `packages/tools` exist since Phase 1; `packages/game`, `apps/steam` do not. `packages/proto` exists and is not in the constitution's layout; it becomes the Phase 2 artefact.

## Phase 1 — headless front sim (2026-09-03)

**Status: built; decisions made; gate pending the smoke test.** The canonical map is 24×24 with 200 start rounds (D-P1-1, C10; config hash `01dc5d02`). Experiments green locally (`npm run experiments`: 9 experiments, 3 seeds, 0 failing checks, ~50 s) and wired into CI (`.github/workflows/ci.yml`: lint, `tsc --strict`, fixtures, E1–E9, `docsync --check`). The human five-minute smoke test has not been run. Report: `PHASE_1_REPORT.md`.

### 1.1 What exists

- `packages/sim` — pure TypeScript port of `frontsim.py` with the power model (`firsthour.ts`), districts/enemies/recipes as data (`districts.ts`, `enemies.ts`, `recipes.ts`), six bot policies (`bots.ts`: compact, spike, balanced, cheapest, river, turtle), JSON state, fixed tick, deterministic (`prng.ts`). Fixtures from the Python sim (`fixtures/*.json`, incl. `power3–5.json`) pass under `npm test`.
- `packages/harness` — `cli.ts` (`--seeds`, `--hours`, `--out`, `--nightly --seeds-n`), `run.ts` (`runSim` → `RunSummary`), `experiments/e1–e9.ts`, `report.ts` → `docs/EXPERIMENTS.md` + `docs/experiments/E<n>.json`, `nightly.ts`.
- `packages/tools/src/docsync.ts` — regenerates the §7 district and enemy tables and the §12 recipe table between `<!-- docsync:… -->` markers from `packages/sim`; `--check` is a CI step.
- `docs/EXPERIMENTS.md` — every run named; config hash `01dc5d02` in the header (24×24, 200 start rounds; the 24×22 / 300-round pass was `7637b6e3`). Run names are now the section and row names of that file.

### 1.2 Doc pass

29 disagreements listed in `PHASE_1_REPORT.md` before editing; 27 edited into the doc with changelog lines (§4, §5, §7, §9, §11, §12, §14, §15, §16, §17, §18, §19, §23, §24, §25, §27), two left as flagged assumptions (edge hopper 100 vs turret hopper 50; assembler line 220 kW). Every `[sim: …]` tag outside the changelog now names a section or row of `docs/EXPERIMENTS.md`, except `E10-bloom-cadence` (Python only, marked as such).

### 1.3 Untagged recount

Phase 0 counted 70. After the Phase 1 pass: **44** (DoD asked for ≤ 23 — missed). D-P1-3 passes the gate on the 15 a block sim can reach and carries the 29 tile-scale numbers as Phase 0 routed them; later DoDs count only what that phase's instrument can reach.

| § | Phase 0 | Now untagged | Tagged in Phase 1 | Still untagged |
|---|---|---|---|---|
| §5 | 24 | 14 | 5.6, 5.10, 5.12, 5.13, 5.14, 5.15, 5.19, 5.20, 5.22, 5.23 | 5.1, 5.2, 5.3, 5.4, 5.5, 5.7, 5.8, 5.9, 5.11, 5.16, 5.17, 5.18, 5.21, 5.24 |
| §7 | 7 | 3 | 7.3, 7.5, 7.6, 7.7 | 7.1, 7.2, 7.4 (design inputs) |
| §12 | 9 | 8 | 12.8 | 12.1, 12.2, 12.3, 12.4 (generated from code, no run varies them), 12.5, 12.6, 12.7, 12.9 |
| §13 | 16 | 16 | — | all: tile-scale machine rows, Phase 4–9 |
| §14 | 5 | 4 | 14.2 | 14.1, 14.3, 14.4, 14.5 |
| §15 | 9 | 1 | 15.1–15.8 | 15.9 |

Of the 44, **29 are tile- or world-view numbers** (all of §13, §14.1/3/4/5, §5.1/5/8/9/18, §12.1/2/3/7) that no block sim can evidence — they belong to Phases 4–9 as Phase 0 already routed them. The remaining **15** are design inputs the sim takes as given (bloom timer and drop, claim cost, burn-off, district dmax/g, recipes, hopper, assembler rate, wells per map, Relight 40 MW, endgame hours) — evidenced only by `[play]` at Gate A or by a run that varies them.

### 1.4 Open constants after Phase 1

C1 edges per assembler: E9-hourly gives 67 mag/min over 20 front edges at 5 h and 52 over 21 an hour later (≈ 2.5–3.4 mag/edge-min in play, wake tails included) → one 20 mag/min assembler feeds ~6–8 edges, not ~15; open. C2 cadence: §18 cadence encoded (15 min in hour one, then 5); Gate A telemetry decides. C4 buffer cap: E9-hold shows a 4,000-round cap loses the hold and a 20,000-magazine bank wins it; a game object is needed (Phase 10). C5: made (D1; doc says ~700). C8: E10 not re-run (Python only). C10 start ammo: **made** — 20 magazines, sim default 200 rounds; holds under the 40-arrival rule with 32 accrued, falls at minute 8 under a 10- or 20-arrival rule (§11 states it).

### 1.5 §26 recount at Phase 1

Three systems: the front, found tech, automated combat. Phase 1 added tooling and data, no system. Complexity **5/10**, unchanged.

### 1.6 Gate status

- Experiments green in CI: green locally at 24×24; the PR run is the CI proof.
- Untagged ≤ 23: **missed** (44; 29 unreachable by a block sim) — **passed by D-P1-3** on the 15 reachable.
- Three decisions: **made** (D-P1-1 24×24, D-P1-2 priced choice, D-P1-3 pass with riders C10 = 20 magazines, hopper 100, `main` protection still needs Pro or public).
- `PHASE_1_REPORT.md` names the contradictions and the runs: yes.
- Human five-minute smoke test: **pending** (`npm run experiments`, read `docs/EXPERIMENTS.md`, then `npm run dev` for the proto).

---

## Phase 0 — inventory, untagged set, open constants

### 0.1 Inventory: what exists, and whether it matches the doc today

"Verified today" means run in this session (2026-09-03). "Matches the doc" is against `RELIGHT-design.md` as it reads today, changelog included.

| Artefact | What it is | Verified today | Matches the doc today | Programme phase it belongs to |
|---|---|---|---|---|
| `RELIGHT-design.md` (608 lines, §1–§27 + appendix + changelog) | The spec | read in full | is the doc; internal contradictions listed in 0.2 | all |
| `frontsim.py` (Python, ~1,000 lines) | Reference block sim: front rules, ammo ring, **power model with shedding**, five claim policies, `--experiments` (E1–E8, E4h, E2-demand-half), `first_hour()` | runs: one sim-hour, seed 3, compact, in 0.23 s; `--experiments` suite **not** re-run | yes for §5/§7 rules it models (see gaps below); every `[sim: E*]` tag in the doc traces to a run of this file or `phase5*.py` | Phase 1 reference; the TS sim is the judge from Phase 1 on |
| `frontsim_legacy_backup.py` | Pre-`FRONT_FIX_REPORT` copy | no | no (pre-fix rules) | delete or archive in Phase 1 (`DEFERRED.md` D-13) |
| `phase5.py`, `phase5_results.json`, `phase5b.py`, `phase5b_results.json` | Doc runs E9–E13 (claim cadence, bloom cadence, Relight hold, spike-scattered, inert-as-solid) and calibration-2 reruns | no | tags in §16, §18, §25 trace to these | Phase 1 (E8, E9 of the programme reproduce E9/E11) |
| `export_fixtures.py` → `packages/sim/fixtures/seed{3,4,5}.json` | Python → TS parity fixtures (mags, hourly rows, first interior, losses, shells) | via `npm test` | n/a | Phase 1 |
| `packages/sim` (TS, 1,423 lines: `types.ts`, `sim.ts`, `bots.ts`, `map.ts`, `queries.ts`, `index.ts`) | Pure `step(state, commands) → state'`, 1 s tick, JSON state `version: 1`, `configHash()`; six bots (compact, spike, balanced, cheapest, river, turtle); economy (slots 1/block, finite rubble 3,840/block, claim 5 Cu + 10 steel, assembler 20 Cu + 40 steel, magazine 2 steel + 1 Cu) | **`npm test` 28/28 green** (24 fixture comparisons + 4 unit tests); `tsc --strict` clean | partial — see "TS sim gaps" | Phase 1 (the headless sim) — exists, needs the gaps closed |
| `packages/sim/test/calibrate.ts`, `bench.ts`, `regression.test.ts` | Calibration harness CLI (`--hours --seeds --bots --economy --build --react --out` + JSON overrides), benchmark, fixture regression | regression yes; calibrate/bench not run | n/a | Phase 1 → becomes `packages/harness` |
| `packages/proto` (Phaser 3.90 + Vite 6.4, 7 files) | Map-view prototype: 24 px block map, claim tooltip `front +N · closes N`, ring order list, pips, build button with reason, slots, rubble strip, telemetry export with config hash, seed and `economy=0` in URL, speed keys, facility silhouettes, `autoplay` bots | **`npm run build` clean**, 1.53 MB bundle (chunk-size warning only); not played | Phase 2 spec: has everything except `?state=` snapshot loading (Scenario B), survivor markers and a state export button; verified against the sim only, never against a human | Phase 2 — built, **not gated** |
| `FRONT_FIX_REPORT.md` | 13 sim/doc disagreements → E1–E8; doc numbers changed; 8 unsettled; 10 rejected systems; E9–E13 | read | is the provenance of most `[sim]` tags | Phase 1 evidence |
| `CALIBRATION_REPORT.md` | Cadence 8 → 5 min; targets T1–T7; `PROTO_CALIBRATED`; flags §25 item 13 as no longer true | read | §7/§17/§24/§25 still carry the 8-min-cadence numbers it superseded (0.2) | Phase 1 evidence |
| `CALIBRATION_REPORT_2.md` | Slots + finite rubble; T1–T9 (T1, T5, T6, T7 missed, T1 structural); D1–D4 applied to the doc; coal patch size unsettled | read | D1–D4 are in the doc without a human's name on them (`DECISIONS.md`) | Phase 1/2 evidence |
| `PROTOTYPE_BUILD_REPORT.md` | What the proto is, 14 `PROTO-ASSUMPTION`s, three human decisions | read | assumptions not yet tagged `GAME-ASSUMPTION` (rule 6; retag in Phase 2's report) | Phase 2 |
| `PROTOTYPE_TEST_PLAN.md` | 5 testers + control, seed 3, 2.5 h at 4×, criteria, rework/kill triggers | read | is the Gate A protocol | Phase 2/3 |
| `TEST_RESULTS.md` | Template, `verdict: pending`, hashes `6e74fbfd` / `91bad3aa` | read | empty | Gate A |
| `DEFERRED.md` | 12 items | read; every item now has a phase | — | all |
| Not present | `docs/`, `DECISIONS.md` (created now), `EXPERIMENTS.md`, `PHASE_*_REPORT.md`, `test_results/`, `.git`, CI, `packages/harness|tools|game`, `apps/` | — | — | Phase 1 onward |

**Not rebuilt (exists and passes):** the TS sim and its fixture regression; the proto build; the calibration harness. **Not trusted (exists, never verified):** the Python `--experiments` suite as a whole (last run by a prior session, results only in the reports); the proto's feel (no human has played it); every `PROTO-ASSUMPTION`; D1–D4.

**TS sim gaps against §5/§7 (rules the doc states that `packages/sim` does not carry):**

- Power: no substation draw, Generators, brownout or shed order (Python only, `--power`). Programme Phase 1 E4 needs it in TS.
- Wells: `+0.3 dmax`, `×4 g`, 3-block influence are in; "a well dies after five minutes with no Dark neighbour" is in neither sim.
- Blooms: "adjacent blooms interleave 10 s apart" is in neither sim.
- Grid is **24×22** (`map.ts` `W = 24, H = 22`, row 21 = river) with five hard-coded wells and hard-coded district bands; §4 says 24×24 cells and §18's 25 h drawing says "full 24×22 city". No generator, no validator (Phase 9).
- Hopper is 100 rounds per edge (two turrets × 50) — matches §7's "100-round hopper" and §13's 50-round turret if read per edge; §12's "turret hopper (50 rounds = 5 magazines)" is per turret. Wording, not a rule gap.

### 0.2 Contradictions inside the doc (found while reading; none fixed in Phase 0, all for Phase 1's doc pass)

1. **§7 "What threat scaling feels like" and §17 "Strategy space"** still say 3.2× (14,152 vs 4,414 magazines) and 1.6×/1.31× with tag `[sim: E6-scatter, E6-noscatter]`; §9 item 1, §19 and §27 say 2.2× and 0.97× with the same tag "at the 5-minute cadence". The changelog entry that updated §9 is labelled "§7". One tag, two numbers.
2. **§24 risk 6 and §25 item 7** carry the 8-minute-cadence figures (56 % / 131 % / 1.3× / 0.56×); `CALIBRATION_REPORT.md` measured 0.97× / 0.57× at 5 min.
3. **§25 item 13 "Closed"** says spike on the scattered map "loses nothing"; `CALIBRATION_REPORT.md` measured 4–11 blocks lost at the 5-minute cadence and said "needs a human, not a retag". Reopened as D-25-13 in `DECISIONS.md`.
4. **§15 early phase** says 4.5 MW at 3 h `[sim: E2-demand]` (measured at the old 200/40 kW draw); §12's table says 1.0–3.1 MW `[sim: E2-demand-half]` at the D1 draw. §15 mid ("5.5 at 4 h, 7.6 at 10 h") is likewise pre-D1.
5. **Map size**: §4 24×24 cells; §18 and both sims 24×22.
6. **Big pole** "reach 12, supplies 3×3" is smaller than the pole's 7×7; almost certainly a typo for a larger area.
7. **Constitution vs doc** (the constitution is not the spec; the doc wins, but the constitution's Phase 4 text should be corrected before Phase 4): belts "7.5/s" (doc 8/s and 16/s); "Mk1 3 s recipe" (doc: Shot 3 s — agrees); "24×24" (doc agrees; sims 24×22).

### 0.3 The untagged set

Every number in §5, §7, §12, §13, §14, §15 that carries neither `[sim: run]` nor `[play: session]`. Grouped by the experiment or phase that can evidence it. Phases 1–4 aim to empty this list; a number leaves it only by gaining a tag or by being deleted from the doc with a changelog line. Numbers already tagged are not listed. "Derived" means arithmetic on other numbers and needs no run of its own once its inputs are tagged; it is listed so the count is honest.

**§5 The front**

| # | Number | Where | Evidence route |
|---|---|---|---|
| 5.1 | Lit tile burns rot at 0.05/s | Rot | Phase 4 M4 (tile rot); until then the block sim's burn-off stands in |
| 5.2 | Bloom timer `T = 120/(0.5+d)` (240 s at d=0, 96 s at 0.75) | Bloom | E10 varied it (§25 item 1); the *value* is a feel decision — Gate A, `[play]` |
| 5.3 | Bloom drop to `max(0.05, 0.9·d)` | Bloom | same as 5.2 |
| 5.4 | Claim cost 10 wire + 5 frames | Claiming | Phase 5 E13 (does the claim cost bite?); proto uses 5 Cu + 10 steel |
| 5.5 | Pole reach 8 | Claiming | Phase 4 (world view: does reach 8 cross a street margin of 8?) |
| 5.6 | Wake bloom "twice normal size" | Waking | the cap is tagged; the 2× is not — Phase 1 E3 (per-district wake cost) |
| 5.7 | Burn-off `20 + 60·d` s | Held | Phase 1 E8 (claim cadence: the burn-off sets the floor) |
| 5.8 | 2 turrets per edge, range 9, "one per 16 tiles" | Cost table | Phase 4 M3 (turret ring geometry) |
| 5.9 | 3 lamps × 5 kW per edge | Cost table | Phase 4 M5 (light map) |
| 5.10 | Outskirts ammo 4.3 mag/min + 2 shells/min | Cost table | Phase 1 E3 (10 h compact run reaching the outskirts) — residential/industrial rows are tagged, this one is not |
| 5.11 | Per-tile ammo 0.04 / 0.06 / 0.13 mag/min | Cost table | derived from tagged per-edge rows ÷ tiles |
| 5.12 | Substation draw 100 kW front / 20 kW interior | Cost table | D1 (`CALIBRATION_REPORT_2.md`, E4h-*) — evidence exists, **tag missing**, human not on record |
| 5.13 | Bloom size `4 + 36·d` crawlers over 15 s | Bloom | Phase 1 E3; Phase 2 feel |
| 5.14 | Adjacent blooms interleave 10 s | Bloom | not in either sim (0.1) — Phase 1 adds it or deletes it |
| 5.15 | Brownout: 20 s unpowered → turrets stop | Falls | Phase 1 E4 (TS power model) |
| 5.16 | Shade disables a turret 30 s, stacking | Falls / §7 | Phase 1 E3 |
| 5.17 | Hulk 600 HP | Falls / §7 | Phase 1 E3; Phase 4 M3 |
| 5.18 | Creep 1 tile / 3 s during a fall | Falls | Phase 4 M4 (tile rot) |
| 5.19 | Refeed within 60 s resets the arrival counter (D3) | Falls | Phase 1 E1 (starve-and-refeed) — decided, tag missing |
| 5.20 | Knock-on 80 kW per fallen front block (2 × 40) | Falls | derived from 5.12 |
| 5.21 | Wells: 3–6 per map | Wells | Phase 9 generator |
| 5.22 | Well `dmax + 0.3`, growth ×4, within 3 blocks | Wells / §7 | Phase 1 E3 (well row of the district table) |
| 5.23 | Well dies after 5 min with no Dark neighbour | Wells | not in either sim — Phase 1 adds it or Phase 6 |
| 5.24 | Outskirts cap 1.0 | Wells | with 7.1 |

**§7 Threat**

| # | Number | Where | Evidence route |
|---|---|---|---|
| 7.1 | District `dmax` 0.30 / 0.45 / 0.45 / 0.60 / 1.00 | table | design inputs; steady-state outputs are tagged E3-block; the inputs stay untagged until Gate A says the spread reads — `[play]` |
| 7.2 | District `g` 0.0004 / 0.0005 / 0.0005 / 0.0006 / 0.0008 | table | as 7.1 |
| 7.3 | Depth multiplier `1 + 0.3·distance/20`, capped 1.0 | Depth | Phase 1 E3 (E7 tags the *result* 0.24–0.34 only) |
| 7.4 | Crawler: 1 tile, 3 t/s, 12 HP (3 rounds) | Enemies | Phase 4 M3/M4 |
| 7.5 | Shade: 1 tile, 2 t/s, 40 HP (10 rounds); 1 per 8 crawlers at d ≥ 0.3; 30 s disable | Enemies | threshold tested in E3 (0.25 vs 0.3) and kept — tag missing; rate and stats Phase 4 M3 |
| 7.6 | Hulk: 3×3, 0.8 t/s, 600 HP, 60 dmg/shell → 10 shells; 40 % of blooms at d ≥ 0.5; barricade 1 per 4 s | Enemies | as 7.5 (0.45 vs 0.5 tested) |
| 7.7 | "3–4× a residential edge" (outskirts) | table text | with 5.10 |

**§12 Resources**

| # | Number | Where | Evidence route |
|---|---|---|---|
| 12.1 | Rubble 300 units/tile; 250–350 tiles/block (75–105 k) | Rubble | Phase 5 E11/E13 (finite rubble in world view); proto 3,840/block at 32/min |
| 12.2 | Coal ~30 k per block, 4 MJ each | Coal | Phase 5 E11 (coal depletion) |
| 12.3 | Iron deposit ~2 M, coal ~1.5 M | Deposits | Phase 5 E11 |
| 12.4 | All eight recipes (wire 1 Cu → 2 / 1 s; frame 2 steel / 2 s; concrete 2 stone / 2 s; board 3 wire + 1 steel / 4 s; shot 2 steel + 1 Cu → 10 rounds / 3 s; shell 2 steel + 1 coal / 3 s; fuel 1 crude → 4; polymer 2 crude → 1) | Intermediates | Phase 5 E10 (chain throughput) |
| 12.5 | Turret hopper 50 rounds = 5 magazines | Ammo chain | wording (5.8) |
| 12.6 | One assembler = 20 mag/min = 40 steel + 20 Cu/min = 1.3 / 0.7 excavators | Ammo chain | 20/min follows from 12.4's 3 s; the "edges per assembler" it implies is open constant C1 |
| 12.7 | Cannon hopper 20 shells | Ammo chain | Phase 6 (Arsenal) |
| 12.8 | Mid-game 60–150 mag/min + 10–20 shells; late 150–250 + 30 | Counts table | Phase 1 E5 extended past 5 h; Phase 10 E20 |
| 12.9 | 40 MW at the Relight | Counts | Phase 10 E20 (D4 banked-stock hold) |

**§13 Machines** — every row is untagged; they are design inputs, evidenced by the milestone that places them.

| # | Number | Evidence route |
|---|---|---|
| 13.1 | Excavator 3×3, 60 kW, 5×5 area, 0.5/s | Phase 4 M1 |
| 13.2 | Assembler 3×3, 100 kW | Phase 4 M2 |
| 13.3 | Generator 2×2, 300 kW, coal-fired | Phase 4 M3 |
| 13.4 | Mixer 2×2, 150 kW | Phase 5 |
| 13.5 | Gun turret 2×2, range 9, 5 rounds/s, 50-round hopper | Phase 4 M3 |
| 13.6 | Cannon 3×3, range 12, 1 shell / 2 s, 20-shell hopper | Phase 6 |
| 13.7 | Lamp 1×1, 5 kW, radius 4; Floodlight 2×2, 40 kW, 12-tile cone | Phase 4 M5 |
| 13.8 | Barricade 200 HP | Phase 4 M3 |
| 13.9 | Pole reach 8, supplies 7×7; Big pole reach 12, supplies 3×3 (typo, 0.2 #6) | Phase 4 M1 |
| 13.10 | Belt 8/s and 16/s; Inserter 10 kW, 1/s; Splitter 1×2; Underground span 4; Chest 400 | Phase 4 M2, Phase 5 |
| 13.11 | Depot input 2×2 | Phase 5 |
| 13.12 | Tram stop 2×3, 20 kW, 6 inserters; Tram 1×3 / 1×9, 200 / 600 items, 8 t/s | Phase 7 |
| 13.13 | Line truck garage 4×4, 50 kW, holds 4 kits | Phase 8 |
| 13.14 | Substation recipe 20 frames + 20 wire + 10 boards | Phase 5 |
| 13.15 | Pumpjack 3×3, 200 kW, 0.5/s | Phase 6 |
| 13.16 | Facility footprints (HQ 8×8, Foundry 10×10, etc.) and "twenty-six placeable things" | Phase 9 (generator places them) |

**§14 Logistics**

| # | Number | Evidence route |
|---|---|---|
| 14.1 | Street 8 tiles wide (4 + 4 margins); lot 24×24 | Phase 4 M1 |
| 14.2 | Assembler line 220 kW (100 + 2 × 60) | derived from §13 |
| 14.3 | Hand-collecting one stack per 2 s | Phase 5 (Depot) |
| 14.4 | Tram 200 items at 8 t/s; freight 600; 100 s load (6 inserters), 34 s | Phase 7 E16 |
| 14.5 | Front kit 32×4 strip, 2 turrets; truck carries 4 kits | Phase 8 E17 |

**§15 Progression**

| # | Number | Evidence route |
|---|---|---|
| 15.1 | Early 0–3 h, 12–25 blocks | Phase 1 E3/E8 (bot territory at 3 h), then `[play]` Gate A |
| 15.2 | First interior ~40 min (sim says ~47 [E8]) | reconcile in Phase 1 doc pass |
| 15.3 | 15–20 edges by hour 3 | Phase 1 E3 |
| 15.4 | 4.5 MW at 3 h (stale, 0.2 #4) | replace with E2-demand-half |
| 15.5 | Mid 3–12 h, 25–100 blocks; F passes 30 | Phase 1 E3 at 10 h; Phase 6 |
| 15.6 | Turbine 5 MW; "5.5 at 4 h, 7.6 at 10 h" pre-D1 | replace with E2-demand-half-25h |
| 15.7 | Wells killed hours 6–9 | Phase 6 |
| 15.8 | Late 12–25 h, 100–200 blocks, ≥ 15 blocks between wells, 50–70 edges | Phase 1 E3 at 25 h (Python E2-demand-25h holds 184 at 25 h, untagged here); Phase 10 |
| 15.9 | Endgame 20–40 h; first Relight ~25 h | Phase 10 E20; `[play]` Phase 13 |

Count: **§5 24 · §7 7 · §12 9 · §13 16 rows · §14 5 · §15 9 = 70 entries** (a §13 row bundles a machine's numbers). Of these, four have evidence in a report but no tag (5.12, 5.19, 7.5/7.6 thresholds) and two are stale tagged numbers (15.4, 15.6) that need replacing rather than evidencing.

### 0.4 Open constants (tempo-setting, not yet evidenced)

Each has an "open" line in `DECISIONS.md` with its doc value. The seven the constitution names first, then three more the reading turned up.

| ID | Constant | Doc value | Proto / sim value | Where evidence would come from |
|---|---|---|---|---|
| C1 | Edges fed per assembler | ~15 (20 mag/min ÷ 1.37 mag/edge-min in play; §12) | Mk1 10 mag/min, Mk2 20 (proto `PROTO_CALIBRATED`) | Phase 1 E5, Gate A `[play]` |
| C2 | Claim cadence the game is drawn for | §18 drawn at ~5 min after hour one `[sim: E9-claim-cadence]` (a run tag on a drawing, not a decision) | bots: 15 min hour one, then 5 min (was 8) | Gate A telemetry `summary.claimsPerHour` |
| C3 | Machine slots per block | "machine slots on interior blocks" (§5), lot 24×24 tiles (§4) | 1 (PROTO-ASSUMPTION) | Phase 4 M1 (what fits on a lot); Gate A |
| C4 | Magazine buffer cap | none stated | 400 magazines (4,000 rounds) | Phase 1 E1/E5; Phase 4 M2 (a chest is 400) |
| C5 | HQ start patch sizes | §11: "a small steel patch"; coal **~700 (D1 ratified 2026-09-03; doc still says ~3,000 until the Phase 1 doc pass)** | proto steel 7,680 at 64/min (empties at 2:00); coal not in proto | Phase 1 E4 (hour-one power), Phase 5 E11 |
| C6 | Fall time after the substation stops | 90 s (D2, was 5 min) `[sim: E8-fall16]` | 90 s | D2 ratified 2026-09-03; Phase 3 checks a human can act in the window `[play]` |
| C7 | Substation draw | 100 kW front / 20 kW interior (D1, was 200/40) | Python only | D1 ratified 2026-09-03; Phase 1 E4 encodes it in TS |
| C8 | Bloom timer base and drop | `120/(0.5+d)`, 10 % (§5) | same | Gate A `[play]` (E10 measured the alternatives) |
| C9 | Assembler rate | 20 mag/min (§12) | proto Mk1 10 / Mk2 20 | with C1 |
| C10 | Start ammo | 20 magazines (§11; sim `startRounds` 300 = 30 magazines) | 300 rounds | Phase 1 doc pass (reconcile), Phase 3 |

### 0.5 §25 open questions → phase

| §25 | Question | Phase that answers it |
|---|---|---|
| 1 | Bloom cadence (feel) | Gate A (Phase 2 → 3), `[play]`; C8 |
| 2 | Diagonal leaks (closed, E6/E13) | Phase 9 — generator validator asserts no diagonal-only Dark pocket |
| 3 | Global stock too easy | Phase 5 (Depot radius rule is the test) and Phase 13 human rounds |
| 4 | Well visibility | Phase 2 shows wells; decision at Gate A |
| 5 | Endgame hold | Phase 1 E9 (banked), Phase 10 E20 (full) |
| 6 | Density farming | Phase 6 (enemy purpose) and Phase 13 (degenerate-strategy bots) |
| 7 | Quiet-block problem | Gate A (cheapest-matched testers), Phase 13 |
| 8 | Outskirts / well edge cost | Phase 1 E3 (10 h compact run) — also 5.10 |
| 9 | Coal depletion | Phase 5 E11 |
| 10 | §18 drawings vs cadence | Phase 1 E8 + Gate A telemetry; §18 redrawn in Phase 3 |
| 11 | Late-game demand | Phase 1 E5 extended to 25 h in the TS sim; Phase 10 E20 |
| 12 | Fall distance (closed, D2) | Phase 3 confirms the rescue window `[play]`; C6 |
| 13 | Spike on the scattered map (marked closed; evidence says otherwise) | **reopened by the human 2026-09-03** — Phase 1 E7 reruns at the 5-min cadence; D-25-13 |
| 14 | Power in hour one (closed on draw; patch open) | Phase 1 E4, Phase 3; C5, C7 |

No §25 item lacks a phase; none deleted.

### 0.6 §24 risks → phase

| §24 | Risk | Phase |
|---|---|---|
| D1 | Re-fronting tedium | Phase 8 E17; Phase 13 |
| D2 | Ammo tuning window | Phase 1 E5; Gate A; Phase 13 |
| D3 | Rot unreadable | Phase 2 (mottle), Phase 12 |
| D4 | Block fall illegible | Phase 4 M3/M4, Phase 12 |
| D5 | Endgame haul | Phase 10 |
| D6 | Quiet-block greed | Gate A, Phase 13 |
| D7 | Early brownout sheds HQ | Phase 1 E4, Phase 4 M3 |
| D8 | Straight push outruns ammo | Phase 1 E5/E7, Gate A |
| D9 | Cascade | Phase 1 E2 |
| T1 | Per-tile rot cost | Phase 4 M4, Phase 11 |
| T2 | Pathfinding | Phase 4 M4 (flow fields), Phase 6 |
| T3 | Belt sim cost | Phase 5, Phase 11 |
| T4 | Procedural validity | Phase 9 |
| T5 | Light map | Phase 4 M5 |

### 0.7 §26 recount at Phase 0

Three systems: the front, found tech, automated combat. Nothing was added. Complexity **5/10** as the doc says; the count is unchanged because Phase 0 built nothing.

### 0.8 Experiment naming

The doc's run tags (E1-starve … E13-inert-solid, E4h-*, E2-demand-half) and the constitution's experiment numbers (E1–E9 in Phase 1, E10–E20 later) **collide**. Phase 1's `EXPERIMENTS.md` must carry both names per row (constitution number, doc tag) so a `[sim: …]` tag stays traceable. Proposed map for Phase 1: E1 ↔ E1-starve · E2 ↔ E2-h500 · E3 ↔ E3/E3-block/E7 · E4 ↔ E4h-* · E5 ↔ E5 · E6 ↔ E6-scatter/E6-noscatter · E7 ↔ E12-spike-scattered · E8 ↔ E9-claim-cadence · E9 ↔ E11-relight-hold.
