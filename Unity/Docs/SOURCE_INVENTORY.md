# Source inventory — reference game (Relight, TypeScript/Phaser)

Shared starting map for the Unity migration documentation. Paths are relative to the repository root. This inventory names where things live; the specialist documents own the extracted facts. Status labels used across the package: **Implemented and retained**, **Approved but not implemented**, **Implemented but needs correction**, **Unresolved**, **Retired**.

## Checkout state (captured 2026-09-11)

| Item | Value |
|---|---|
| Branch / HEAD | `main` at `586d4525` ("merge: preserve latest main nightly reports", 2026-09-10) |
| Working tree | Uncommitted: 286 files. Most churn is CRLF→LF line-ending normalisation; ignoring that, 72 files changed (+1558/−435) covering GP-PLAYTEST-FIX/-2, GP-TURRET-POWER, GP-POWER-FIX, GP-START-POCKETS, GP-HOME-REPAIR, GP-HUD-STRIP, GP-OPENING, GP-TURRET, GP-RANGE and their docs. Nothing has been committed, reset or cleaned by this task. Re-counted 2026-09-11 during the snapshot below: **287 porcelain entries — 255 modified, 32 untracked** (the difference from 286 is this migration package's own `Unity/` directory). |
| Untracked, pre-existing | ~~`docs.zip`, `.serena/`~~ — **corrected 2026-09-11:** both are now **tracked**, committed in `aff9e7e8`. `git ls-files --error-unmatch docs.zip` → `docs.zip`; `.serena/.gitignore`, `.serena/project.yml`. Neither appears in `git status --porcelain` output. The untracked set is the 32 entries listed under "Reference snapshot" below. |
| Toolchain | Node v22.18.0 (WSL Ubuntu), npm workspaces, TypeScript 5.6, Vite 6, Phaser 3.90, node:test |
| Preview | Mutable dev preview on port 5178 (`vite preview` of `packages/game/dist`); frozen historical checkpoints 5177/5179/5180/5181/5182; Phaser Editor play 5190 |

## Reference snapshot (baseline for the migration) — captured 2026-09-11

The port is written against a *working tree*, not a commit: the GP checkpoint work is uncommitted, and this package must not commit on the reference project's behalf. The snapshot below pins exactly which bytes were read, without writing anything to git. It is the answer to "which version of the reference does this documentation describe?"

### What is pinned

| Item | Value | How obtained |
|---|---|---|
| Branch | `main` | `git rev-parse --abbrev-ref HEAD` |
| HEAD | `586d4525cd3bfb4b3b180887a5adf77f14838660` ("merge: preserve latest main nightly reports") | `git rev-parse HEAD` |
| Working-tree entries not at HEAD | **287** — 255 modified (` M`), 32 untracked (`??`) | `git status --porcelain \| wc -l`, and `grep -c` for each prefix |
| Files hashed | **254** | `wc -l < Unity/Docs/reference-snapshot.sha256` |
| Per-file hashes | `Unity/Docs/reference-snapshot.sha256` — plain `sha256sum` output, `LC_ALL=C` sorted, repo-root-relative paths, no header or comments so that `sha256sum -c` consumes it directly | see "How it was built" |
| **Aggregate hash** | **`ebcb418aea3b203a5724d19ce3192213842cbea63c94158bb564361d5aac5d5c`** | `sha256sum < Unity/Docs/reference-snapshot.sha256` — one hash over the sorted per-file list; quote *this* when a document needs to name the baseline in one string |

### Untracked entries at capture (all 32)

`Unity/`, `docs/Design/`, `docs/Implementation/GP_CHECKPOINT.md`, `docs/evidence/gp-checkpoint/`, `docs/evidence/p5-04-2026-09-07/sim-build\r.log`, `docs/evidence/phaser-terrain/`, `packages/game/public/art/editor/terrain/`, and **24 individual source files under `packages/sim`** — `src/ammunitionMigration.ts`, `src/city/gameplaySites.ts`, `src/equipment.ts`, `src/fabrication.ts`, `src/firstRegion.ts`, `src/gameplayCombat.ts`, `src/gameplayProgress.ts`, `src/hostileAwareness.ts`, `src/openingEncounter.ts`, `src/overclockMigration.ts`, `src/playerBallistics.ts`, `src/turretTracking.ts`, `src/weaponProfiles.ts`, `src/weaponTypes.ts`, plus `test/activeRaids.test.ts`, `test/equipment.test.ts`, `test/fabrication.test.ts`, `test/firstRegion.test.ts`, `test/gameplayProgress.test.ts`, `test/gpFactory.test.ts`, `test/homeRepair.test.ts`, `test/openingEncounter.test.ts`, `test/playerTestFixes.test.ts`, `test/playtestFix2.test.ts`, `test/powerFix.test.ts`.

**Re-verified 2026-09-12 (B-01):** `sha256sum -c Unity/Docs/reference-snapshot.sha256` — 254 OK, 0 failed; the reference is byte-identical to the snapshot the port is written against. The Unity project that ports it was established the same day under `Unity/` with editor `6000.6.0f1 (f7f8ed4d1e24)` (DECISIONS.md U-M-27).

**Read that list twice before trusting any git-based comparison.** Fourteen `packages/sim/src` modules that this migration ports — weapons, equipment, fabrication, the opening encounter, turret tracking, ballistics, two save migrations — **exist only in the working tree**. `git show HEAD:packages/sim/src/weaponProfiles.ts` fails. A future session that checks out `586d4525` cleanly gets a *different and smaller* game than the one documented here. That is precisely why this snapshot hashes files rather than naming a commit. (One entry, `docs/evidence/p5-04-2026-09-07/sim-build\r.log`, has a carriage return in its filename; it is not hashed, being outside the hashed paths, and is noted only so nobody treats it as corruption.)

### What was hashed

Everything the migration documentation actually reads for behaviour:

| Path set | Files |
|---|---|
| `packages/sim/src` (incl. `src/city`, 11) | 84 |
| `packages/sim/test` | 75 |
| `packages/game/src` (incl. `src/editor`, 1) | 32 |
| `packages/harness/src` (incl. `src/experiments`, 16) | 22 |
| `packages/tools/src` | 12 |
| `docs/Implementation/GP_CHECKPOINT.md` | 1 |
| **Total** | **254** |

**Path correction, recorded so it is not re-introduced:** the brief named `packages/game/editor/**` as a separate path set. **That directory does not exist.** The editor sources live at `packages/game/src/editor/` (1 file), which is already inside `packages/game/src` and therefore already hashed. `packages/game/src/style.css` is likewise already covered by `packages/game/src` and needs no separate entry.

Not hashed, deliberately: `packages/game/public/**` (art and fixture saves — large, and covered by `WORLD_AND_ASSETS.md` at the source level), `maps/**`, `docs/**` other than the GP checkpoint, and anything generated (`dist/`, `node_modules/`).

### How it was built

```sh
cd /mnt/e/Factorio2
{ find packages/sim/src packages/game/src packages/harness/src packages/tools/src \
       packages/sim/test -type f -print
  echo docs/Implementation/GP_CHECKPOINT.md
} | LC_ALL=C sort -u | xargs -d '\n' sha256sum > Unity/Docs/reference-snapshot.sha256
sha256sum < Unity/Docs/reference-snapshot.sha256   # the aggregate quoted above
```

### How a future session re-verifies it

```sh
cd /mnt/e/Factorio2
sha256sum -c Unity/Docs/reference-snapshot.sha256
```

- **Every line `OK`** → the reference is byte-identical to what this package was written against. Verified on 2026-09-11: 254 of 254 OK, zero non-`OK` lines.
- **Any line `FAILED`** → that file changed. The named document for that subsystem may now be describing behaviour that no longer exists; re-read the file before relying on it. This is a prompt to check, not a blocker.
- **Any line `No such file or directory`** → the file was deleted or renamed, *or* the working tree was reset and the file was one of the 24 untracked ones above. Distinguish the two before concluding anything.
- **A quick equivalence check** without re-reading 254 lines: `sha256sum < Unity/Docs/reference-snapshot.sha256` must print `ebcb418a…d5ac5d5c`. If it does, the snapshot file itself is unmodified; if it does not, someone edited the snapshot, which is a different problem from the reference changing.

**This snapshot is not a commit and does not ask for one.** Nothing in Phase B requires the reference working tree to be committed first (see `TECHNICAL_ARCHITECTURE.md` §13 R4 — a commit would simply make this snapshot easier to reproduce, not more correct).

**Confirmed by Owner decisions 2026-09-11 (Q12).** What the migration must preserve is an *identifiable* reference snapshot: HEAD, the uncommitted and untracked files, and content hashes. All three are pinned above — HEAD `586d4525cd3bfb4b3b180887a5adf77f14838660`, 287 working-tree entries (255 modified, 32 untracked, listed in full) and 254 per-file hashes with the aggregate `ebcb418a…d5ac5d5c`. **A commit is not a prerequisite**, and whether to commit the GP checkpoint is the owner's option rather than a migration gate. *(Correction 2026-09-11: the parenthesis above previously called this "the owner's open question"; the owner has answered it. R4 remains a **risk** — an uncommitted tree can still drift — but is no longer an open question.)* Nothing in this migration package has reset, discarded or committed the owner's changes, and nothing in it may: the reference project is read-only to the migration, and `sha256sum -c` above is how a future session proves it.

## Startup and check commands (root `package.json`)

```sh
npm install
npm run dev                      # Vite dev server for packages/game (open with ?view=world for a fresh opening)
npm run build -w packages/game   # production build → packages/game/dist
npm run preview -w packages/game -- --host 0.0.0.0 --port 5178 --strictPort
npm test                         # node:test suite in packages/sim (75 test files)
npm run typecheck                # builds sim, harness, tools, game
npm run lint
npm run docsync:check            # regenerated doc blocks
npm run freshness:check          # evidence freshness
npm run city:validate | factory | defence | experiments | nightly | replay | seeds | section18
npm run map:export-tiled         # Tiled export of the authored city
npm run editor:dev | editor:assets | editor:scene | editor:check | editor:apply   # Phaser Editor workflow
```

## Packages

| Package | Role | Size | Key entry points |
|---|---|---|---|
| `packages/sim` | Pure TypeScript simulation. Sole owner of gameplay mutation through commands. | ~50,000 lines in `src/` + `src/city/`; 75 test files in `test/` | `src/index.ts` (public API), `src/sim.ts` (tick), `src/types.ts`, `src/rules.ts`, `src/recipes.ts`, `src/save.ts` |
| `packages/game` | Phaser 3 renderer, DOM/CSS UI, input, session/save slots. | ~5,700 lines | `src/main.ts`, `src/session.ts`, `src/worldScene.ts`, `src/uiShell.ts`, `src/hud.ts`, `src/style.css` |
| `packages/harness` | Headless experiments, bots, scenario CLIs, nightly reports. | ~1,600 lines | `src/run.ts`, `src/defenceCli.ts`, `src/factoryCli.ts`, `src/experiments/` |
| `packages/tools` | Documentation sync, freshness, Tiled export, Phaser Editor city import/export, seeds. | ~1,100 lines | `src/docsync.ts`, `src/exportTiled.ts`, `src/phaserCity*.ts` |

### `packages/sim/src` — file groups (all files listed)

| Group | Files |
|---|---|
| Core state, tick, save | `types.ts`, `sim.ts`, `index.ts`, `save.ts`, `rules.ts`, `constants.ts`, `prng.ts`, `queries.ts`, `ledger.ts`, `names.ts`, `itemNames.ts`, `itemGuide.ts`, `ammunitionMigration.ts`, `overclockMigration.ts` |
| Map, tiles, city, navigation | `map.ts`, `tiles.ts`, `ground.ts`, `graph.ts`, `footprint.ts`, `navigation.ts`, `cityNavigation.ts`, `walk.ts`, `move.ts`, `light.ts`, `authoredCity.ts`, `riverfrontCampaign.ts`, `city/riverfront.ts`, `city/riverfrontRail.ts`, `city/gameplaySites.ts`, `city/opening.ts`, `city/parcelGeometry.ts`, `city/geom.ts`, `city/spec.ts`, `city/generate.ts`, `city/urban.ts`, `city/validateRiverfront.ts`, `city/index.ts`, `districts.ts`, `districtValidation.ts`, `firstRegion.ts` |
| Engineer, inventory, equipment, crafting | `engineer.ts`, `equipment.ts`, `weaponProfiles.ts`, `weaponTypes.ts`, `playerBallistics.ts`, `interaction.ts`, `machineInventory.ts`, `recipes.ts`, `fabrication.ts` |
| Production, logistics, power | `flow.ts`, `directConveyor.ts`, `routing.ts`, `freight.ts`, `transport.ts`, `fixedTram.ts`, `truck.ts`, `truckWork.ts`, `campaignPower.ts`, `campaignTurbine.ts`, `concreteValidation.ts` |
| Construction, blueprints | `construction.ts`, `blueprint.ts`, `blueprintFormat.ts`, `blueprintPlans.ts`, `bots.ts`, `candidates.ts`, `expansion.ts`, `project.ts` |
| Combat, enemies, raids | `threat.ts`, `campaignThreat.ts`, `campaignDefence.ts`, `defenceValidation.ts`, `enemies.ts`, `stalker.ts`, `hostileAwareness.ts`, `turretTracking.ts`, `openingEncounter.ts`, `gameplayCombat.ts`, `emergence.ts`, `heart.ts` |
| Campaign, progression, guidance | `campaign.ts`, `campaignAlerts.ts`, `campaignGuide.ts`, `campaignDiscovery.ts`, `campaignDistricts.ts`, `campaignRecruits.ts`, `campaignSurvey.ts`, `campaignCityValidation.ts`, `progression.ts`, `gameplayProgress.ts`, `goal.ts`, `discoveryValidation.ts`, `inspection.ts`, `hour.ts`, `firsthour.ts` |

### `packages/game/src` — files

`main.ts`, `session.ts`, `view.ts`, `controls.ts`, `worldScene.ts`, `mapScene.ts`, `cityMapScene.ts`, `uiShell.ts`, `hud.ts`, `panel.ts`, `buildPanel.ts`, `buildCatalogue.ts`, `inventoryPanel.ts`, `inspectionPanel.ts`, `campaignGuidePanel.ts`, `navigationPanel.ts`, `clipboardPanel.ts`, `blueprintLibraryPanel.ts`, `truckWorkPanel.ts`, `alertInbox.ts`, `settingsPanel.ts`, `uiDrag.ts`, `uiPreferences.ts`, `itemIcons.ts`, `factoryStrings.ts`, `machineConnections.ts`, `urbanDraw.ts`, `riverfrontDraw.ts`, `riverfrontLighting.ts`, `riverfrontRailDraw.ts`, `telemetry.ts`, `style.css`, `editor/`.

## Authored world and asset sources

| Source | Path | Notes |
|---|---|---|
| Canonical city geometry (roads, parcels, buildings, tram, yards) | `packages/sim/src/city/riverfront.ts`, `riverfrontRail.ts` | `riverfront-arc-v4`, 864 × 576 tiles; validator `city/validateRiverfront.ts` |
| Canonical gameplay sites (camps, gates, arenas, rewards, plants, cores) | `packages/sim/src/city/gameplaySites.ts`, `firstRegion.ts`, `riverfrontCampaign.ts` | Stable IDs; Tiled gameplay layer is reference-only |
| Phaser Editor scene/manifest (visual plots, background buildings) | `maps/phaser/` (`manifest.json`, `RiverfrontCity.source.txt`, `last-apply.json`), `packages/game/public/relight-editor-pack.json`, `phasereditor2d.config.json`, `scripts/phaser-editor-*.mjs`, `packages/tools/src/phaserCity*.ts` | CITY-F `riverfront-arc-v4-editor-ac16d9188c05`, 479 buildings |
| Tiled export (reference) | `maps/tiled/riverfront-v4/` (`riverfront.tmj`, `buildings.tsj`, `terrain.tsj`, `verification.json`) | 32 px/tile; export-only, no importer |
| Game art | `packages/game/public/art/riverfront/` (65 SVG + 3 `.py` generators), `packages/game/public/art/editor/` (2), `relight-asset-pack.json` | Vector, generated in-repo |
| Third-party tileset | `Assets/urban-decay/48px+MVMZ/` (15 PNG: `Modern_Outside_A1–A5`, `B/C/D_Sheet`, `!doors`) | RPG Maker MV/MZ-format tileset; licence/provenance must be checked before Unity reuse |
| Audio | **None — the reference project has no audio at all** | Verified 2026-09-11: a `find` across `packages/` and `docs/evidence/` for `*.mp3`, `*.ogg`, `*.wav`, `*.m4a`, `*.flac` and `*.aac` returns nothing, and a `grep -rn` across `packages/game/src` and `packages/sim/src` for `load.audio`, `new Audio(`, `AudioContext`, `playSound` and `sound.play` returns nothing. The game is silent end to end. **Owner decisions 2026-09-11 (Q09):** audio is in scope for the Unity port, so every clip is **new** externally-sourced content needing its own licence and provenance record — there is nothing here to port (`TECHNICAL_ARCHITECTURE.md` §15; `MIGRATION_MAP.md` S-A5) |
| Fixture saves | `packages/game/public/snapshots/*.json`, `docs/evidence/**/*.json` | Reference saves; `SaveFile` versions 1–3, campaign sub-versions. **Owner decisions 2026-09-11 (Q01/Q07):** the Unity port starts fresh and these saves stay with the original game, which still runs — no converter is built and the Unity schema starts clean at v1. They are preserved unmodified as **provenance**, not as required Unity fixtures (`TECHNICAL_ARCHITECTURE.md` §9.3; `MIGRATION_MAP.md` S-05, S-A4) |
| Icons/UI tokens | `packages/game/src/itemIcons.ts`, `packages/game/src/style.css` | |

## Documents (authority order for this migration)

| Document | Role |
|---|---|
| `CLAUDE.md`, `AGENTS.md` | Agent working rules; read order |
| `docs/PROGRAMME_STATE.md` | Actual checkout/build state and handoffs |
| `docs/PROGRESS.md` | The reference game's only task list (GP-15–25 held; human gates open) |
| `docs/CONSTITUTION.md`, `docs/DECISIONS.md` | Authority, verification, decision provenance (D-GP-START, D-GP-PLAYTEST*, D-UI-*, D-PE-*, D-EX-*) |
| `docs/RELIGHT_CONFIRMED_GAMEPLAY.md` | Current gameplay authority (intended game) |
| `docs/Design/RELIGHT_PROGRESSION_AND_WEAPONS_DRAFT.md` | Accepted keys/schematics/artifacts, five weapons, five enemies, guardians, raid direction; labelled proposals |
| `docs/EXPLORATION_DEFENCE_PLAN.md` §6 | Gap matrix G01–21, decision register D-GP-01–15, tasks GP-01–25 |
| `docs/Implementation/GP_CHECKPOINT.md` | Latest implemented state, provisional values, owner corrections 2026-09-11 |
| `docs/CURRENT_GAMEPLAY_CATALOGUE.md`, `docs/CAMPAIGN_RULES.md` | Items, recipes, machines, rates (docsync-generated blocks) |
| `docs/RI-02B_UI_SPEC.md` | UI contracts (D-UI-10 onward) |
| `docs/Implementation/RIVERFRONT_CITY.md`, `docs/PHASER_EDITOR.md`, `maps/*/README.md` | World conventions, editor workflow |
| `docs/Implementation/GAMEPLAY_CORRECTIONS.md`, `PLAYER_EXPERIENCE_CORRECTIONS.md`, `PLAYER_PLAYTEST_FOLLOWUP_02.md`, `PLAYER_CLARITY_AND_FUN_AUDIT_2026-09-10.md`, `PRE_PHASE10_GAMEPLAY_AUDIT.md` | Playtest feedback and corrections |
| `docs/Implementation/PLAYER_EXPERIENCE_CORRECTION_PLAN.md` | Planning record for the corrections above; superseded by `PLAYER_EXPERIENCE_CORRECTIONS.md` (the implemented record). Read only for provenance |
| `docs/PHASES.md` | Reference programme phase list (0–14); historical structure only. Migration phases A–F in `Unity/Docs/TASKS.md` do not map onto it |
| `docs/RELIGHT-design.md` | Legacy GDD; generated blocks; legacy reference only |
| `docs/REVISED_DEVELOPMENT_PLAN.md`, `docs/P*_REPORT.md`, `docs/EX08*`, `docs/archive/`, `docs/evidence/` | Historical reports and evidence; left in the reference project |
