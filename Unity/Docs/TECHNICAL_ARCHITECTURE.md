# Technical architecture — Relight on Unity

**Status:** Phase A architecture proposal, now partly implemented. The Unity project exists at `Unity/Relight/` (U-M-32) and Phase B implemented the foundation described in §§2–4, 5, 8–9 and 12 (see TASKS.md B-01–B-14 and DECISIONS.md U-M-27–U-M-37 for what was built and where it departs from this text); the remaining sections stay proposals until their phase runs. The sentence "no Unity project exists in this repository" that stood here until 2026-09-12 was Phase A history. Every claim about the *reference* game carries a source reference. Every claim about the *target* is marked as a proposal and, where it depends on external facts (Unity versions, package maturity), marked **subject to verification**.

**Owner decisions applied 2026-09-11.** The owner answered the open questions this document carried. Recorded here, each once, in its own section: **Q01/Q07** start fresh, no old-save converter (§9.3); **Q05** Unity 6.6 with UI Toolkit stands, only justified packages, blockers recorded not worked around (§4.0.2.1); **Q09** audio is in scope, reversing the silence default (**§15**, new); **Q10** telemetry retired, local diagnostics only (§11); **Q11** manual saves plus a rotating autosave ring (**§9.4**, new); **Q12** the reference baseline is a preserved snapshot, no commit required (§13); **Q19** desktop keyboard and mouse, gamepad out of initial scope (§8.4.1, new). Where a recorded default is reversed, a dated correction note sits with the text it replaces.

**Owner boundaries.** This document owns the technical architecture, the import-pipeline architecture, data-asset shapes, the command/event contracts, and the audio *architecture* — where audio sits relative to the sim, the bus layout, pooling and limits (§15) — but not which sounds are chosen or how they are mixed by ear. It does **not** own gameplay rules or content values (see `GAME_DESIGN.md` and `CONTENT_CATALOGUE.md`), the authored world data format or the art asset formats (see `WORLD_AND_ASSETS.md`), or the UI screen inventory and onboarding flow (see `UI_AND_ONBOARDING.md`). Where those topics are touched here it is only to describe the wiring.

---

## 1. Purpose, status, and relationship to the reference project

### 1.1 Purpose

Describe how the existing TypeScript/Phaser game ("Relight", the *reference project*) is structured as built, size the problem honestly, and propose a Unity architecture that preserves the reference project's single most important structural property — **the simulation is the sole owner of gameplay mutation, and it is engine-free** — while making the game editable by a human in the Unity Editor.

### 1.2 Reference checkout

| Item | Value | Source |
|---|---|---|
| Branch / HEAD | `main` at `586d4525` ("merge: preserve latest main nightly reports") | `SOURCE_INVENTORY.md` § Checkout state |
| Working tree | Uncommitted GP checkpoint work (GP-PLAYTEST-FIX/-2, GP-TURRET-POWER, GP-POWER-FIX, GP-START-POCKETS, GP-HOME-REPAIR, GP-HUD-STRIP, GP-OPENING, GP-TURRET, GP-RANGE) | `SOURCE_INVENTORY.md`; `docs/PROGRAMME_STATE.md` lines 3–11 |
| Toolchain | Node 22, npm workspaces, TypeScript 5.6, Vite 6, Phaser 3.90, `node:test` | `package.json`, `packages/game/package.json`, `packages/sim/package.json` |
| Authority | Sim alone mutates gameplay through commands; player time fixed at 1× with pause; no speed controls | `CLAUDE.md` § Work and authority, § UI implementation; `docs/CONSTITUTION.md:3`, `:57` |

The reference project is **not** being retired by this proposal. `docs/CONSTITUTION.md:57` records "No engine migration is implied" for the reference programme's own obligations; this migration is a separate, owner-directed exercise. The reference project remains the behavioural authority and the source of reference saves.

### 1.3 Status labels used

**Implemented and retained** · **Approved but not implemented** · **Implemented but needs correction** · **Unresolved** · **Retired**.

---

## 2. Reference architecture as built

### 2.1 Packages

Four npm workspaces (`package.json` `workspaces`):

| Package | Role | Size | Engine dependency |
|---|---|---|---|
| `packages/sim` | Pure TypeScript simulation; sole owner of gameplay mutation | 50,155 lines across 95 `.ts` files in `src/` + `src/city/`; 75 files in `test/` | **None.** No Phaser, no DOM. |
| `packages/game` | Phaser 3 renderer, DOM/CSS UI, input, session and save slots | ~6,200 lines across 31 `.ts` files + `style.css` + `editor/RiverfrontCity.scene` | Phaser 3.90, DOM |
| `packages/harness` | Headless experiments, bots, scenario CLIs, nightly reports | 39 files | None |
| `packages/tools` | docsync, freshness, Tiled export, Phaser Editor city import/export, seeds | 12 files | None |

`packages/sim/package.json` exports `./src/index.ts` directly (no build step); `packages/game/tsconfig.json` `include` pulls `../sim/src/**/*.ts` straight into the game's typecheck. There is no compiled sim artifact — the boundary is a source-level import boundary, enforced by discipline and review rather than by a build wall. **Status: Implemented and retained.** (A Unity assembly definition would make the same boundary machine-enforced; see §4.1.)

### 2.2 The sim public API surface

`packages/sim/src/index.ts` (115 lines) is almost entirely `export *` re-exports of 60+ modules, plus a small set of named exports. The practical public surface is therefore *everything exported by the sim*, not a curated façade. **Status: Implemented but needs correction** for the Unity port — the target should expose a deliberate façade (§4.3).

The meaningful contract is three-part:

**(a) Commands — the only way to mutate gameplay.** `Command` is a discriminated union in `packages/sim/src/types.ts:155–236`, ~50 variants. Representative groups:

- Construction/blueprint: `construct`, `buildPath`, `undergroundPair`, `undoBuild`/`redoBuild`, `blueprintCopy`, `blueprintTransform`, `blueprintPaste`, `blueprintOrder`, `blueprintLibrary`, `removeArea`, `factory`
- Engineer/body: `move`, `walk`, `walkTo`, `sprint`, `dodge`, `aim`, `mineAt`, `craft`, `place`, `pickUp`, `fire`, `enterTruck`
- Inventory/interaction: `inventory`, `chestTake`, `chestPut`, `feed`, `repair`, `rotate`, `setRecipe`, `setStationRules`, `equipment`, `fabrication`
- Campaign/progression: `progression`, `region`, `deliverSite`/`restoreSite`, `collectTramKit`, `deliverTurbine`/`restoreTurbine`, `setTurbineEnabled`, `recruitSurvivors`, `recoverSchematic`, `repairDefence`, `upgradeRadio`, `navigation`, `truckWork`, `cityProp`
- Legacy (block-sim era): `claim`, `ringOrder`, `addAssembler`, `deliver`, `activate`, `commission`, `repairCabinet`, `abort`
- Clock: `setSpeed` — **effectively retired for players**; `session.ts:238` clamps every player `setSpeed` to 0 or 1 via `playerSpeed`.

Dispatch: `applyCommands(st, commands)` at `packages/sim/src/sim.ts:776–789`. It handles four legacy cases inline and delegates everything else to `engineerCommand`, then calls `tiles(st)?.afterCommand?.(st)` per command.

**(b) Queries — read-only views for presentation.** `packages/sim/src/queries.ts` (270 lines) provides `frontList`, `claimInfo`, `ammoStatus`, `shapeMetrics`, `slotInfo`, `heldInfo`, `hud`, `clockOf`, `facilityList`, `survivorList`, `nearestHeld`, `renderMap`. Many further per-subsystem selectors live beside their subsystems (`campaignGuide.ts`, `inspection.ts`, `navigation.ts`, `progression.ts`, `itemGuide.ts`, `machineInventory.ts`, `campaignAlerts.ts`).

**(c) Events — what happened this tick.** `SimEvent` is a discriminated union at `types.ts:241–304` (~30 variants: `claim`, `bloom`, `fall`, `sub-off`/`sub-on`, `brownout`/`power-ok`, `hopper-empty`, `engineer-down`/`-up`, `kitted`, `truck`, `rifle`, `gen-dry`, `retaliate`, `lamp-eaten`, `arrival`, `well-dead`, `project`, `stalker`, `hour`, `heart`, …). `takeEvents(st)` (`sim.ts:1114`) drains `st.events` and resets it to `[]`.

### 2.3 State shape

`SimState` — `packages/sim/src/types.ts:333–417`. Header comment: *"Plain-data types. The whole sim state is JSON-serialisable: save/load is JSON.stringify/parse."* Top-level fields:

`construction?`, `version` (1|2|3), `ruleset?`, `campaign?`, `seed`, `t` (sim seconds), `rng` (one 32-bit word), `speed`, `acc`, `w`, `h`, `start`, `target`, `wells`, `facilities`, `survivors`, `config`, `blocks[]`, `nb[][]`, `deg`, `len[][]`, `lattice`, `tileScale`, `hops[]`, `hopsT[]`, `wellHops[][]`, `city?`, `engineer`, `ring[]`, `edgeAt[]`, `engagements[]`, `buffer`, `asmManual`, `dirty`, `fallen[]`, `recentRounds[]`, `recentShells[]`, `recentRoundsSum`, `recentShellsSum`, `totalRounds`, `totalShells`, `hourly[]`, `stats`, `stock`, `patch`, `power`, `asmTrack`, `wellDead[]`, `wellEnclosedSince[]`, `events[]`, `flow?`.

Two layers coexist:

- **Block layer** (`blocks`, `ring`, `engagements`, `stock`, `power`) — the Phase 1–4 abstract territory sim. Largely superseded in the campaign: `rules.ts:47` rejects any campaign state with `st.ring.length || st.engagements.length || blocks.some(b => b.state === CONTESTED)`. **Status: Retired for the campaign, Implemented and retained for `legacy-v1` fixtures.**
- **Tile layer** (`flow`, `FlowState` at `flow.ts:304–360`) — the real game. Holds `machines[]`, an `occ` tile→machine index, `dug`/`units` mining state, a `store`, the engineer's `hand` state, and a large `stats` block that the conservation ledger reads.

`SimConfig` (`types.ts:70–115`) is the tuning bundle (production, economy, power model, validators, thresholds). `PROTO_CALIBRATED` (`types.ts:363–390`) is the frozen prototype calibration block; `configHash()` (`types.ts:399`) is an FNV-1a hash over key-sorted config JSON used by the freshness tooling.

### 2.4 Tick pipeline

Two nested clocks.

**Tile tick — `stepFlow(st, dt)` (`flow.ts:1061–1100`), 20 Hz** (`TILE_TPS = 20`, `flow.ts:61`; header comment at `flow.ts:18` says "ticking at a fixed 20 ticks/s"). Order:

1. `tickEquipment`
2. `initTruck`, `sampleInspection`
3. campaign power grid resolve (`campaignGrid`, `campaignPower.ts:21`) → writes `f.power.{supply,demand,load,throttle}`
4. `tickEngineerTiles`, `tickAuthored` — *"the engineer moves (and lays kits) ahead of the machines and the block tick"*
5. `tickTruckWork`
6. `threatHooks.current?.tick` — crawlers walk, turrets and rifle shoot, arrivals count
7. `heartTick`
8. belts in `beltOrder`, then routing machines (splitters/undergrounds), each optionally `loadConveyor`
9. generator share computation
10. per-machine loop: turrets (timer only), generators, trams, then inserters / excavators+pumpjacks / assemblers at `dt × throttle`
11. `tickHand` (hand mining and hand crafting)
12. `tickDistricts`, `tickProgression`

**Block tick — `step(st, commands)` (`sim.ts:793–1085`), once per 20 tile ticks** (1 Hz). Order: apply commands → contested→held transitions → rot growth and blooms (skipped when `isCampaign`) → exposure/edge bookkeeping when `dirty` → well death → engineer (only when there is no flow layer) → ammo production and ring distribution → …→ `hourRow` sampling.

**Driver — `advanceFlow(st, realSeconds, commands, maxTicks = 256)` (`flow.ts:1104–1120`):**

```
apply commands (plus any f.pending)
st.acc += realSeconds * st.speed * TILE_TPS
n = floor(st.acc + 1e-6)              // the epsilon is deliberate: "never lose a tick to rounding"
clamp n to maxTicks * TILE_TPS
repeat n times:
    stepFlow(st, TILE_DT)
    f.tick++
    if f.tick % TILE_TPS == 0: step(st); syncProjects(st)
```

`advance()` (`sim.ts:1105`) is the block-only equivalent used when there is no flow layer.

**`tileHooks` — an inversion of control.** `sim.ts:105–116` defines a `TileHooks` interface (`afterCommand`, `syncEdges`, `covered`, `drainEdges`, `supplyKw`, `demandKw`, `setLoad`) held in a module-level mutable singleton `tileHooks.current`. `flow.ts` installs itself into it. This exists to keep the frozen block-layer fixtures byte-identical while the tile layer took over. **Status: Implemented but needs correction** — a module-level mutable global is a determinism and testability hazard, and in C# it should become an explicit interface passed to the sim (§4.2). `threatHooks.current` (`threat.ts`) and `candidates.ts`'s `enableStalkers`/`enableHeart` follow the same pattern.

### 2.5 Determinism

- **PRNG:** mulberry32, one 32-bit word of state serialised as a plain number. `packages/sim/src/prng.ts` — `seedRng(seed)`, `rngNext(holder)`, `rngInt`, `rngUniform`. Also `hash01(seed,t,x,y)` (a position/time hash, documented as bit-identical to `hash01` in the original `frontsim.py`) and `pyRound` (banker's rounding, matching Python's `round()`). The full algorithm is reproduced in §10.2.
- **Fixed step:** the sim never sees a variable `dt` for its own logic; `TILE_DT` is constant. Wall-clock only decides *how many* ticks run.
- **Frame-gap guard:** `session.ts:258` — `if (!Number.isFinite(realDt) || realDt < 0 || realDt > 0.25) { realDt = 0; s.state.acc = 0; }`. Background-tab gaps are discarded rather than fast-forwarded.
- **State hashing:** `stateHash(st)` (`save.ts:74`) = FNV-1a over canonical (key-sorted) JSON of the state minus `SAVE_TRANSIENT` (`events`, `acc`, `speed`). `canonicalJson` (`save.ts:58`) drops `undefined` and functions and normalises non-finite numbers to `null`.
- **Replay:** `replay(st, log, untilTick, {dropAim})` (`hour.ts`) re-runs a logged command stream from a freshly constructed state; `replaySession` (`session.ts:308`) wires it up and `replayVerdict` compares.

**Known non-deterministic or environment-coupled paths (verified):**

| Path | Source | Note |
|---|---|---|
| `routeCache` `WeakMap` keyed on `FlowState` | `flow.ts:1123` | Cache only; invalidated on `f.rev`/`machines.length`. Behaviour-neutral, but a GC-visible structure that must not be serialised. |
| `campaignGrid` `WeakMap` cache | `campaignPower.ts:13` | Keyed on a composed string of `f.rev`, `f.tick`, turbine/plant/defence/generator state. Behaviour-neutral. |
| `new Date().toISOString()` in `makeSave` | `save.ts:129` | `savedAt` is metadata only, excluded from `stateHash`. |
| Object key iteration order | throughout (`for (const k in f.store)`) | JS insertion order is deterministic *within* JS but does **not** transfer to C# `Dictionary`. See §10.5 — this is the single largest determinism risk in the port. |

### 2.6 The command pattern in practice

`packages/game/src/session.ts` is the whole glue layer (319 lines). Its header: *"Glue between the sim and the renderer. Engine-free. The prototype holds no game state of its own: everything is in `state`; this file only forwards commands and drains events."*

- `queue(s, c)` pushes onto `s.pending`, normalising `setSpeed` through `playerSpeed`.
- `dispatch(s, c)` flushes pending + the new command immediately (works while paused), logs each, calls `applyCommands`, returns an `ActionResult`.
- `frame(s, realDt)` — per render frame: drain `pending`, let bots/hour-bot append (dev aids only), `clockPolicy` (forces `speed ∈ {0,1}`, zeroes `acc` on any change), log, then `advanceFlow`/`advance`, then `takeEvents` and telemetry.

**Verified: the renderer does not mutate sim state.** A grep across `packages/game/src/*.ts` for assignments into `state.*` found only `session.ts`'s own handling of the three transient fields (`speed`, `acc`) plus `loadState`'s resets. Renderer-only view state is deliberately segregated in `packages/game/src/view.ts`, whose header reads *"Renderer-only view state (never in SimState)"* and which holds `debugView`, `hudInset`, `transportView`, `inspectionView`, `navigationView`, `objectiveView` — each commented "presentation only". This is a strong, clean boundary and the port should preserve it exactly.

### 2.7 The conservation ledger

`packages/sim/src/ledger.ts` (114 lines). `conservation(st, tolerance = 0.01)` computes, per item:

```
unexplained[k] = held[k] + sinks[k] − sources[k] − opening[k]
```

`held` is gathered by place (`chest`, `buffer`, `ring`, `pockets`, `machines`, `belts`, `committed`), `sources` from `stats.minedOf` + `stats.made`, `sinks` from `stats.consumed` + `placed` + `spentSteel`/`spentCopper` + repairs + `coalBurned` + fired/lost rounds. `openLedger` records the opening stock on the flow state so a pre-ledger save is checked from its upgrade point rather than from a history it never kept.

This is a genuine invariant test, not a display. It is exercised by `packages/sim/test/ledger.test.ts` and by the harness experiments. **It must be ported, and it is the single best acceptance test for the C# port** — a ported sim that conserves items across a long run against the same command log is very likely correct.

### 2.8 Renderer loop and rendering approach

- Phaser game constructed at `packages/game/src/main.ts:160–170`: `Phaser.AUTO`, parent `#map`, `Scale.RESIZE`, `antialias: true, pixelArt: false`, `disableContextMenu: true`, `scene: []` (scenes added imperatively).
- Scenes: `worldScene.ts` (1,946 lines — the tile-level world), `mapScene.ts` (379, lattice map) and `cityMapScene.ts` (454, city polygon map). Exactly one runs; `toggleView()` (`main.ts:214`) sleeps one and runs the other, handing over a shared `view.focus` block.
- The sim is driven from `game.events.on(Phaser.Core.Events.STEP, …)` (`main.ts:197`) → `frame(session, Math.min(0.1, delta/1000))`. So **Phaser's loop is the host clock and the sim runs inside it**, not the other way round.
- Draw modules: `riverfrontDraw.ts`, `riverfrontRailDraw.ts`, `riverfrontLighting.ts`, `urbanDraw.ts`, `machineConnections.ts`, `itemIcons.ts`. Art is 68 generated SVGs in `packages/game/public/art/riverfront/` plus a manifest `relight-asset-pack.json`.
- **No interpolation was found in the reference renderer.** The world scene draws from current sim values. At 20 Hz with a 60 Hz display this means up to 3 identical frames per sim tick for moving bodies. Worth confirming against `worldScene.ts` during Phase B; the Unity target proposes explicit interpolation (§4.2).

### 2.9 UI

**DOM/CSS, not Phaser UI.** `uiShell.ts` (161 lines) builds `<nav>`, a drawer `<section>`, and a modal pause dialog directly with `document.createElement`. `packages/game/src/style.css` (531 lines) holds the shared tokens. Panels are `panel.ts` (774 lines) plus per-domain modules (`buildPanel`, `inventoryPanel`, `inspectionPanel`, `campaignGuidePanel`, `navigationPanel`, `clipboardPanel`, `blueprintLibraryPanel`, `truckWorkPanel`, `settingsPanel`, `alertInbox`, `hud`).

Input rules, verified in `uiShell.ts`:

- `uiShell` owns input capture: `keydown`, `keyup`, `pointerdown`, `pointerup`, `focusin`, `blur`, `visibilitychange` are all registered in the **capture** phase (`, true`), explicitly *"Capture before Phaser's window handlers"*.
- A `held`/`suppressed` key-set prevents a key held when a panel opens from re-firing into the world on release.
- Escape order (`uiShell.ts` keydown handler): cancel UI drag → close slot menu → close nav menu → close modal child → unpause → close drawer → `hooks.cancelSelection()` → pause. **Escape cancels drag first**, as `CLAUDE.md` requires.
- `uiInput.blocked` / `uiInput.modal` are the flags the world scene reads.
- Menus overlay the world; `hudInset` reserves *layout* space and the comment at `uiShell.ts` states *"the world camera ignores these insets"* — opening or closing a panel does not move the camera.
- One panel at a time: `open(id)` calls `closeDrawer(false)` first ("panel replacement").

Bindings: `controls.ts` (40 lines) holds `DEFAULT_BINDINGS`, a live `BINDINGS` override map, `FIXED` (unrebindable: Escape, numbered quickbar slots, and the retired `slower`/`faster`) and `MODIFIED` (Ctrl-combos). `bindingProblem()` validates against key-class and per-context collision. `uiPreferences.ts` persists them; `CLAUDE.md` states shortcuts are *browser preferences, separate from inventory*. **Note: `DEFAULT_BINDINGS.slower` and `.faster` are `[]` — the speed controls are retired but their binding slots remain.**

### 2.10 Persistence

- `SaveFile` (`save.ts:82–95`): `{ version: 1|2|3, kind: 'relight-save', savedAt, seed, tick, t, hash, params?, state, log?, logComplete? }`.
- `SimState.version` is also 1|2|3 and must equal `SaveFile.version` (`loadState`, `save.ts:114`).
- `SAVE_TRANSIENT = ['events','acc','speed']` (`save.ts:53`) — reset on every load.
- `loadState(raw)` accepts a `SaveFile`, a telemetry export (takes `.finalState`), or a raw `SimState`. It validates via `stateProblem()`, deep-copies with `JSON.parse(JSON.stringify(...))`, resets transients, then runs **18 `init*`/`migrate*` upgraders in a fixed order** when `st.flow` is present: `initExpansion, initDefence, initDistricts, initDiscovery, initRecruits, initTurbine, initForeman, initFixedTram, initProgression, initGameplayProgress, migrateOverclocks, initEquipment, migrateAmmunition, initFirstRegion, initFabrication, initActiveRaidClock, initOpeningEncounter, initKnowledge`. It then re-validates and throws `Save migration rejected: …; original preserved` on failure.
- `stateProblem()` chains **31 per-subsystem validators** (`ammunitionProblem || authoredProblem || rulesetProblem || … || fabricationProblem`). This is unusually thorough and is the reason old saves survive at all.
- Campaign has its **own** version axis: `CampaignState.version: 1…10` (`rules.ts:13`), plus sub-versions (`expansion.surveyVersion: 2|3`, `defence.version`, `authored.version: 4`, `flow.ammoVersion`).
- Slots: browser `localStorage`, key `relight.save.<slot>`, with a campaign profile prefix — `profileSlot()` yields `exploration-v2:1` for campaign saves and bare `1` for legacy (`session.ts:78–80`).
- Renderer-side migration gates in `loadSnapshot` (`session.ts:106–118`) compute `upgradedHome` (campaign version < 7), `upgradedDefence` (defence version < 2), `upgradedConstruction` (campaign version < 10), `upgradedGameplay`, `upgradedProgression`, `olderSurvey`, `upgradedFixedTram`, `upgradedTransport`. These do **not** block loading — they only clear `logComplete`, i.e. the save loads and plays but no longer claims a from-tick-0 replay.
- **No autosave was found.** Saving is explicit (Ctrl+S / panel button, `saveSlot`). `session.ts:210` notes *"saving is not a command and is not logged"*.

### 2.11 Rulesets

`packages/sim/src/rules.ts` (49 lines):

- `LEGACY_RULESET = 'legacy-v1'` — absent `st.ruleset` means legacy. **Status: Retired for new play**, retained for fixtures and old saves.
- `CAMPAIGN_RULESET = 'exploration-v2'` — the real game. `session.ts:31` makes it the default for a new session with no `?state=`.
- `CAMPAIGN_RULES = { daySeconds: 1200, daylightSeconds: 900, firstAssaultNight: 3, opening: 'culdesac-v1' }`.
- `rulesetProblem()` enforces the separation hard: a campaign state must be save schema 3, must have a valid `campaign` block, must not carry `ring`/`engagements`/`CONTESTED` blocks, and must not carry a legacy `heart`, `threat.stalk` or `projects`.
- `createCampaign(seed)` (`campaign.ts:22`) **ignores the seed** and returns `createRiverfrontCampaign()` — the authored city. `createProceduralCampaign` is explicitly *"Preserved procedural constructor for historical fixtures, never normal New Game."*

### 2.12 Harness and tools

- **Harness** (`packages/harness/src`): `run.ts` (experiment runner), `cli.ts`, `bench.ts`, `calibrate.ts`, `defenceCli.ts`/`defenceScenario.ts`, `factoryCli.ts`/`factoryScenario.ts`, `cityValidationCli/Run/Worker.ts`, `cityPopulation.ts`, `citycheck.ts`, `goalcheck.ts`, `nightly.ts`, `provenance.ts`, `replay.ts`, `report.ts`, `snapshot.ts`, `blueprint.ts`, `campaignExperiments.ts`, `util.ts`, and `experiments/` (E1–E9, E-heart, E-hour, E-project, E-rifle, E-variance, E-walk).
- **Tools** (`packages/tools/src`): `docsync.ts` + `campaign-docsync.ts` (regenerate doc tables *from code*, and fail CI when a prose number disagrees with `constants.ts`), `freshness.ts` (every generated file carries a `source_commit` and `config_hash` stamp; fails if the commit is not an HEAD ancestor or the hash drifted), `exportTiled.ts`, `phaserCity*.ts` (4 files — Phaser Editor manifest import/export), `authorCityDensity.ts`, `authorNeighbourhood.ts`, `section18.ts`, `seeds.ts`.

### 2.13 Data flow (as built)

```
┌──────────────────────────── packages/game (Phaser + DOM) ─────────────────────────────┐
│                                                                                        │
│  DOM events ──► uiShell.ts (capture-phase listeners, uiInput.blocked/modal)             │
│                    │                                                                    │
│                    ├──► panels (*Panel.ts) ──┐                                          │
│  canvas input ──► worldScene.ts ─────────────┤                                          │
│                                              ▼                                          │
│                                      session.queue / session.dispatch                   │
│                                              │  Command[]                               │
│  Phaser STEP (variable dt) ──► session.frame(realDt)                                    │
│                                              │                                          │
└──────────────────────────────────────────────┼──────────────────────────────────────────┘
                                               ▼
┌──────────────────────────── packages/sim (engine-free) ───────────────────────────────┐
│  advanceFlow(st, realDt, cmds)                                                          │
│    applyCommands ──► engineerCommand / claim / setRingOrder / addAssembler              │
│    acc += realDt * speed * 20                                                           │
│    ┌── repeat floor(acc) times ─────────────────────────────────────────────────┐       │
│    │  stepFlow(st, 0.05)   equipment ▸ power grid ▸ engineer ▸ truckWork ▸       │       │
│    │                       threat ▸ heart ▸ belts ▸ routing ▸ machines ▸ hand    │       │
│    │  every 20th tick: step(st)  territory ▸ blooms ▸ edges ▸ ring ▸ hour        │       │
│    └───────────────────────────────────────────────────────────────────────────┘       │
│  st.events[] appended throughout                                                        │
└──────────────────────────────────────────┬─────────────────────────────────────────────┘
                                           │  takeEvents(st)  +  queries.ts selectors
                                           ▼
                       telemetry.ts   ·   mapScene.consume(events)   ·   panel.update()
                                           │
                                           ▼
                                 localStorage `relight.save.<slot>`  ◄── makeSave / loadState
```

---

## 3. Sizing evidence

### 3.1 Code size (verified by `wc -l`)

| Group | Lines | Notes |
|---|---:|---|
| `packages/sim/src` total | **50,155** | 95 `.ts` files incl. `city/` |
| — `city/riverfront.ts` | 33,879 | **Authored data, not logic** — one 864×576 city definition, 479 building parcels, 370 props, all as TypeScript object literals |
| — sim logic excluding `riverfront.ts` | **≈16,276** | The actual simulation |
| — `flow.ts` | 2,418 | The tile layer: machines, belts, power, trams, hand work |
| — `hour.ts` | 1,548 | Hour-bot script + `replay` |
| — `sim.ts` | 1,118 | Block tick |
| — `ground.ts` / `threat.ts` / `city/generate.ts` / `campaignThreat.ts` / `goal.ts` | 646 / 584 / 582 / 497 / 435 | |
| `packages/game/src` `.ts` | **≈6,200** | + `style.css` 531 + `editor/RiverfrontCity.scene` 14,760 (generated) |
| — `worldScene.ts` / `panel.ts` / `cityMapScene.ts` / `main.ts` / `mapScene.ts` / `session.ts` | 1,946 / 774 / 454 / 393 / 379 / 319 | |
| `packages/harness/src` | ≈1,600 | 39 files |
| `packages/tools/src` | ≈1,100 | 12 files |
| `packages/sim/test` | 75 files | |

**The load-bearing number: the simulation you must port is about 16,000 lines of plain TypeScript.** The other 34,000 lines of `packages/sim` are a data file.

### 3.2 Entity counts (verified)

| Entity | Count | Source |
|---|---:|---|
| City tiles | 864 × 576 = **497,664** | `city/riverfront.ts:13–14` (`width`, `height`) |
| Authored buildings (parcels) | **479** | `RIVERFRONT_BUILDINGS` — 958 `"id":` keys ÷ 2 (each parcel has `id` and `parcel.id`) |
| Authored props (trees, debris, gates, fences, tanks, cranes, containers, rocks, statues, …) | **370** | `RIVERFRONT_PROPS` |
| Phaser Editor objects | 959 | `CLAUDE.md` CITY-F handoff (479 buildings/plots + reference) |
| Fixed campaign/interior buildings | 22 | `CLAUDE.md` CITY-F handoff |
| Active-raid attacker budget | **48** | `campaignThreat.ts:28` `activeRaidBudget: 48` |
| Total living actor budget | **240** | `campaignThreat.ts:28` `livingBudget: 240` |
| Raid total per assault | 60 | `campaignThreat.ts:28` `total: 60` |
| Observed living actors, night-time supplied fixture | **127** (peak 5 engaged) | `docs/Implementation/GP_CHECKPOINT.md:45` |
| Machine kinds | 16 in `KINDS`, 28 in the `Kind` union | `flow.ts:70`, `flow.ts:72` |
| Item types | **23** | `flow.ts:159` `ITEMS` |
| Enemy types (legacy table) | 3 (Crawler, Shade, Hulk) | `enemies.ts:8` |
| Belt throughput | 7.5 items/s (fast 15) | `constants.ts:37` |
| Tram capacity / stop capacity / supply chest | 200 each | `flow.ts:244` |
| Turret hopper | 50 rounds | `constants.ts:42` |
| Art assets | 65 SVG + 3 `.py` generators (riverfront); editor art per WORLD_AND_ASSETS.md §8 | `packages/game/public/art/` |

**Placed-machine count is unbounded by design** (the player builds a factory) but is not tracked by any cap in `flow.ts`. The realistic ceiling is set by the belt tick (`beltOrder` iterates every belt every tick at 20 Hz) and by the conveyor item lists. **UNCERTAIN:** no measured maximum machine or belt-item count exists in the evidence; Phase B must measure one before sizing the Unity machine loop.

### 3.3 Tick cost and frame evidence (verified, with correct provenance)

The brief cited "headless median 16.7 ms / max 166.8 ms from PROGRAMME_STATE". Those numbers are real but the source is **`docs/Implementation/GP_CHECKPOINT.md:45`**, not `PROGRAMME_STATE.md`:

> "Its 200-tick sample measured median **2.43 ms**, p95 **5.50 ms**, max **10.32 ms**. The final 10-second Chrome headless sample at 1600×1000 recorded 516 frames: median **16.7 ms**, p95 **33.4 ms**, p99 **50.1 ms**, max **166.8 ms** … Intel i7-11700K, Windows, Chrome 153 headless. This is a local sample, not the reference laptop or full four-factory workload."

Reading that correctly:

- **Simulation cost: median 2.43 ms, p95 5.50 ms, max 10.32 ms per 200-tick sample** with 127 living actors. That is the number that matters for the port.
- The 16.7 / 166.8 ms figures are **browser frame intervals** — rendering plus browser, not sim.
- Other recorded frame stalls: 116.8 ms (`docs/PROGRAMME_STATE.md:49`), 266.8 ms frame / 266.4 ms light paint (`:93`), 300.2 ms (`docs/PROGRESS.md:407`). `docs/P5_05_INTEGRATION_REPORT.md:76` attributes the worst of these to **changed light-mask repaints averaging ~24 ms** — a *renderer* problem in the Phaser light implementation, not a sim problem.
- **The 60 fps gate is explicitly not certified.** `docs/PROGRESS.md:5`: "the local frame sample does not certify 60 fps." `docs/EXPLORATION_DEFENCE_PLAN.md:69` (D-GP-15) records that 16.7 ms/frame is *a derived frame allowance, not a measured pass*, and that "Flow is 20 Hz (50 ms sim interval), not a 50 ms CPU allowance."

### 3.4 Conclusion for proportionate Unity choices

1. **The simulation fits comfortably in plain C# on the main thread.** A ~16k-line sim whose measured worst tick is ~10.32 ms in *interpreted JavaScript* should be materially cheaper in C#. Stated precisely (see §3.5): a 20 Hz sim produces one tick every **50 ms of wall time**; the reference's measured **tick CPU cost** is 2.43 ms median / 5.50 ms p95 / 10.32 ms max; that cost is paid inside whichever **rendered frame** the tick lands in, and at 60 fps a frame has ~16.7 ms total for everything. So the honest reading is "a tick costs a single-digit millisecond slice of one frame in twenty", not "the sim has 50 ms of CPU".
2. **ECS/DOTS is not justified by any evidence available.** The actor budget is 240 living bodies; buildings are 479 static authored parcels; items on belts are the only potentially large collection and no count has ever been measured. DOTS would cost a full rewrite of the sim in a non-portable idiom, destroy the line-by-line correspondence with the reference that makes verification possible, and buy headroom nobody has shown is needed. **Recommendation: plain C# classes/structs, single-threaded, with `List<T>`/arrays. Revisit only if Phase B measurement shows a tick over ~15 ms.**
3. **The performance risk in the reference is rendering, not simulation** — light-mask repaints are the one measured contributor (~24 ms in `P5_05_INTEGRATION_REPORT.md:76`); the GP checkpoint's 166.8 ms tail is unattributed. The Unity port must treat lighting as a designed subsystem with its own budget (Unity 2D lights / a custom shader / a baked-plus-dynamic split), not as a port of the Phaser approach. This is the highest-value performance decision in the migration.
4. **Tilemap scale:** 497,664 logical tiles. Unity's `Tilemap` handles this via chunked rendering, but a single `Tilemap` component with half a million set tiles is worth measuring rather than assuming. Chunking the world into regions loaded around the player is a likely requirement. **Subject to verification in Phase B.**

### 3.5 How to talk about tick cost (normative for this package)

Earlier drafts of this document reasoned "p95 sim cost 5.5 ms against a 50 ms budget". That is wrong and it must not be repeated: a 20 Hz tick rate does **not** grant the main thread 50 ms of CPU per tick while holding 60 fps. Three distinct quantities are involved and every performance sentence in this package must name which one it means.

| Quantity | Value in the reference | What it is | What it is **not** |
|---|---|---|---|
| **Tick interval** | **50 ms** (`TILE_TPS = 20`, `flow.ts:61`) | The wall-clock spacing between simulation ticks. It sets how *often* the sim runs. | Not a CPU allowance. Nothing is permitted to take 50 ms. |
| **Tick CPU cost** | **2.43 ms median, 5.50 ms p95, 10.32 ms max** (200-tick sample, 127 living actors, JS; `docs/Implementation/GP_CHECKPOINT.md:45`) | How long one `stepFlow` call actually occupies the thread. This is the number the port must keep low. | Not a frame time. Not measured in C#. |
| **Rendered-frame budget** | **16.7 ms** at 60 fps (a derived allowance, not a measured pass — `docs/EXPLORATION_DEFENCE_PLAN.md:69`, D-GP-15) | The total time available for everything in one displayed frame: input, sim ticks that fall due, animation, culling, draw, present. | Not per-subsystem. The sim shares it with rendering and lighting. |

How they compose: at 60 fps and 20 Hz, roughly **one frame in three contains a sim tick**. That frame pays the full tick cost on top of its rendering work; the other two pay none. A 10.32 ms tick inside a 16.7 ms frame leaves ~6 ms for everything else in that frame — which is why the max, not the median, is the number that decides whether ticks must be spread or moved off the render thread. The reference has never certified 60 fps (`docs/PROGRESS.md:5`), so no figure here is a pass.

The same correction applies to `DECISIONS.md` U-M-03: its justification is "the measured tick cost is single-digit milliseconds in interpreted JavaScript and the actor counts are small", not "5.5 ms of a 50 ms budget". See the proposed replacement row in the Phase B proposals.

---

## 4. Target Unity architecture (proposal)

### 4.0 Platform proposal

**Target: Unity 6.6 (`6000.6`) with UI Toolkit (UXML/USS authored in UI Builder), Universal Render Pipeline (2D Renderer), the Input System package, and Addressables.**

The version target is Unity 6.6 by owner direction and does not change. What follows records what "6.6" actually *is*, so this package stops calling it something it is not.

#### 4.0.1 Unity 6.6 release category — verified 2026-09-11

| Claim | Verified value | Source (fetched 2026-09-11) |
|---|---|---|
| Unity 6.6 exists and is released | Yes. Version string **`6000.6.0f1`**, "Released on Aug 31, 2026". | <https://unity.com/releases/editor/whats-new/6000.6.0> |
| Unity 6.6's release category | **Supported (Update) release — not LTS.** The manual's version banner reads "Version: **Unity 6.6** (6000.6)" and its selector categorises it as **"Supported"** (as opposed to "Legacy"); the page opens "Unity 6.6 is the latest release of the next generation of the Unity Engine." No LTS wording appears. | <https://docs.unity3d.com/6000.6/Documentation/Manual/index.html> |
| Which Unity 6 release is the current LTS | **Unity 6.3 LTS** — presented as the "Latest LTS release" and "supported with two-year LTS until December 2027". Unity 6.0 LTS support ends October 2026. Unity 6.6 is not named in any category on that page. | <https://unity.com/releases/unity-6/support>, <https://unity.com/releases/unity-6> |
| Unity's release model | Two kinds of Unity 6 release: Update releases, several per year, "supported with bug fixes and critical platform updates until the next release (update or LTS) is published"; LTS releases, once a year, supported two years. | <https://unity.com/releases/unity-6/support> |

**"Unity 6.6 LTS" is therefore a false string and must not appear anywhere in this package.** Use one of these exactly:

- Short form — **`Unity 6.6 (6000.6), a Supported (Update) release — not LTS`**
- Long form — **`Unity 6.6 (6000.6.0f1, released 2026-08-31). Unity classifies 6.6 as a Supported/Update release; the current Unity 6 LTS is 6.3 LTS, supported to December 2027. The target is 6.6 by owner direction; the support trade-off is recorded in DECISIONS.md U-M-01.`**

**The trade-off, stated so it is accepted knowingly and not by accident:** a Supported release receives fixes only until the next release ships, so a project pinned to `6000.6.x` will face a deliberate upgrade decision sooner than one on 6.3 LTS (fixes to December 2027). This is a maintenance-schedule risk, not a capability gap — UI Toolkit runtime, URP 2D Renderer, the Input System and 2D Tilemap are all present in both. B-01 records the exact pinned patch version and the trigger for an upgrade review; it does not re-open the version choice.

#### 4.0.2 Editors installed on this development machine — verified 2026-09-11

| Item | State | How checked |
|---|---|---|
| `Unity 6000.4.5f1` | Installed | `ls -d "/mnt/c/Program Files/"Unity*` → `Unity 6000.4.5f1/Editor` |
| `Unity 6000.5.4f1` | Installed | same command → `Unity 6000.5.4f1/Editor` |
| Legacy editor at `C:\Program Files\Unity\Editor` | Installed; 2018-era package payload | `ls "/mnt/c/Program Files/Unity/Editor"` |
| Unity Hub | Installed (`C:\Program Files\Unity Hub\Unity Hub.exe`) | `ls "/mnt/c/Program Files/Unity Hub"` |
| **Unity 6.6 (`6000.6.x`)** | **Not installed.** No `6000.6` install directory exists under `C:\Program Files\`, and no Hub editor root (the Windows path C:\Program Files\Unity\Hub\Editor) was found there. | `ls -d "/mnt/c/Program Files/"Unity*`; `ls "/mnt/c/Program Files/Unity/Hub/Editor/"` → no such directory |

Consequences for B-01: the target editor **must be installed first** through Unity Hub; neither installed editor is the target, and `6000.4` / `6000.5` are themselves Supported releases, not LTS. Nothing in this package may assume a Unity install exists.

**Observed later on 2026-09-11 (coordinator, read-only):** Unity **6000.6.0f1** is now installed at `D:\Unity\Editor` and the owner created a project shell at `Unity/Relight/` through Unity Hub (2D URP template: `productName: Relight`, `ProjectVersion.txt` = `6000.6.0f1 (f7f8ed4d1e24)`, Input System 1.20.0, 2D Tilemap, URP 17.6.0, UI Toolkit built in; the Vector Graphics package is not in the manifest; `com.unity.ai.assistant 2.19.0-pre.2` is present). The shell has its own nested git repository (`Unity/Relight/.git`, one commit "Initial check-in") and a `SampleScene`. No migration task created it and nothing in it has been touched. B-01's remaining work is to record these versions in U-M-01 and to log the two deviations (project lives under `Unity/Relight/` rather than `Unity/`; nested repository) for the owner's decision in B-02.

**B-02 integration (2026-09-12, coordinator):** the shell's `ProjectSettings/` and a trimmed `Packages/manifest.json` were integrated directly into `Unity/` (no `Unity/Unity` nesting; `README.md` and `Docs/` untouched); the shell itself stays at `Unity/Relight/` as the pre-integration snapshot. The project was opened once in batchmode (`-executeMethod Relight.Editor.ProjectBootstrap.CreateScenes`, `Logs/bootstrap.log`): packages resolved, all assemblies compiled with no `error CS`, and the four scene shells were written with build settings. Exact versions, the package trim with its justification, and the deviations are in DECISIONS.md U-M-27.

**Relocation (2026-09-12, owner decision, U-M-32):** the owner corrected the location — the Unity project is `Unity/Relight/`. After Phase B wave 2 the coordinator moved `Assets/`, the trimmed `Packages/manifest.json` + lock, `ProjectSettings/` (EditorBuildSettings, SceneTemplateSettings, `ProjectSettings/Packages/`), `.gitignore`/`.gitattributes`, `Tools/`, `Logs/` and `TestResults/` from `Unity/` into `Unity/Relight/` and removed the copies at `Unity/`, keeping `Unity/README.md` and `Unity/Docs/`. The template's `Assets/Scenes/SampleScene` and `Assets/Welcome/` remain untouched in the shell. Consequences: batchmode paths are `-projectPath "E:\Factorio2\Unity\Relight"`; the editor must be closed while batchmode runs; the nested `Unity/Relight/.git` keeps the port outside the main repository's tracking until the owner removes it.

##### 4.0.2.1 The shell manifest, read exactly — Owner decisions 2026-09-11 (Q05)

**Owner decision (Q05):** the target stays **Unity 6.6 with UI Toolkit**, and only packages the project can justify are added. A concrete compatibility blocker is **recorded, never worked around by changing the engine version or the UI system**. B-01 reports such a blocker and stops for the owner; it does not substitute a different editor or a different UI toolkit.

Re-read of `Unity/Relight/Packages/manifest.json` on 2026-09-11 (read-only, `cat`), so the Vector Graphics line above is not misused:

| Item | Exact manifest state | Consequence for B-01 |
|---|---|---|
| SVG import (Vector Graphics) | `com.unity.vectorgraphics` — **absent**. What *is* listed is `com.unity.modules.vectorgraphics: 1.0.0`, which is the **built-in module**, not the SVG-importer package. The two are different things and the built-in module does not import `.svg` assets. | **The concrete setup item.** The 65 placeholder riverfront SVGs (WORLD_AND_ASSETS.md §8) need either `com.unity.vectorgraphics` added, or a rasterise-on-export step in the world pipeline (§6.2). B-01 records which, with the version it installed. This is a package gap, not a version problem: it is not a reason to move off 6.6. |
| UI Toolkit | Built in (`com.unity.modules.uielements: 1.0.0`); UI Builder ships with the editor. | Nothing to install (U-M-01, §8.1). |
| Input System | `com.unity.inputsystem: 1.20.0` | Present; §8.4 can proceed. |
| 2D Tilemap | `com.unity.2d.tilemap: 1.0.0`, `com.unity.2d.tilemap.extras: 9.0.0` | Present; the 864 × 576 spike (B-14) has what it needs. |
| URP | `com.unity.render-pipelines.universal: 17.6.0` | Present; the 2D Renderer lighting spike (B-14, R2) has what it needs. |
| Audio | `com.unity.modules.audio: 1.0.0` (built in), plus `com.unity.modules.unitywebrequestaudio` | **No package is needed for audio** (§15). Unity's built-in audio satisfies the whole of the Q09 scope; FMOD/Wwise are explicitly not adopted. |
| Analytics | `com.unity.modules.unityanalytics: 1.0.0` is present because it is a default built-in module. | **Nothing enables it and nothing may.** Telemetry is retired (Q10, §11, MIGRATION_MAP.md S-50): no analytics service is configured, no Analytics API is called, and the module's presence in a default manifest is not a decision to use it. |
| Template extras | `com.unity.ai.assistant 2.19.0-pre.2`, `com.unity.ai.inference`, `com.unity.learn.iet-framework`, `com.unity.visualscripting`, `com.unity.timeline`, `com.unity.collab-proxy`, `com.unity.2d.animation/aseprite/psdimporter/spriteshape` | Template defaults, none of them justified by this project yet. B-01 lists them; removing unjustified packages is an owner call, not a silent edit. A pre-release (`-pre.2`) dependency in particular should not be inherited by accident. |

The first deviation is now settled the other way: the project **is** at `Unity/Relight/` (owner decision 2026-09-12, U-M-32). The nested `Unity/Relight/.git` remains an owner action.

#### 4.0.3 Remaining platform checks

**These have not been verified.** What must be checked before anything depends on them, and how:

| To verify | How | Blocking? |
|---|---|---|
| That `6000.6.x` installs, opens a project and reports the exact patch string | Unity Hub → Installs → Install Editor → 6000.6.x; then read `ProjectSettings/ProjectVersion.txt` | Yes — pin the exact patch (e.g. `6000.6.0f1`) in DECISIONS.md U-M-01 and SOURCE_INVENTORY.md |
| That the Unity MCP integration attaches to this project and reports editor state | §14 — the integration is configured but **unverified**; it failed to connect in the authoring session | No for the port; yes before any workflow assumes AI-driven editor operations |
| 2D Tilemap feature set at that version: chunked rendering, `Tilemap.SetTilesBlock`, rule tiles, tilemap colliders, and performance at ~500k tiles | Build a throwaway scene with an 864×576 tilemap, measure fill time and frame cost | Yes |
| UI Toolkit **runtime** (not just editor) maturity: `UIDocument`, runtime data binding, world-space UI if needed, controls available (list view, scroll view, text field, drag-and-drop) | Prototype one real panel (the inventory slot grid — the hardest one) in UI Builder and run it | Yes — if runtime UI Toolkit cannot do the slot-grid drag interaction, fall back to UGUI and record the decision |
| Input System: action assets, rebinding API (`InputActionRebindingExtensions`), and per-device binding persistence | Prototype the rebind flow against `controls.ts` `bindingProblem` semantics | Yes |
| Addressables vs. direct `Resources`/scene references for the authored city art | Decide after the art package arrives (art is owner-pending per `CLAUDE.md`) | No — can start with direct references |
| URP 2D lights at the required density, vs. the reference's light-mask cost | Measure against §3.3's 24 ms repaint figure | Yes |
| Determinism of `System.Math` / `float` across target platforms | See §10.5 | Yes |

### 4.1 Assembly layout

Assembly definitions (`.asmdef`) are the mechanism that makes the sim/presentation boundary **enforced by the compiler** rather than by review. Proposed:

| Assembly | References | Contains | Rule |
|---|---|---|---|
| `Relight.Sim` | *(none — no `UnityEngine`)* | The port of `packages/sim/src`: state, commands, events, tick pipeline, PRNG, ledger, save serialisation, all subsystem logic, **and every plain simulation input/data type** — item, recipe, machine, weapon, enemy and tuning records, and the world-geometry records (`CityGeometry`, `GameplaySites`, parcels, road/rail tile sets) | **No `UnityEngine` reference. Enforced by asmdef.** Must compile and run in a plain .NET console app. **Never references `Relight.Data`.** |
| `Relight.Data` | `UnityEngine`, **`Relight.Sim`** | `ScriptableObject` definitions (items, recipes, machines, weapons, enemies, tuning) and the **converters** that turn them into the `Relight.Sim` records above; `GameDataRegistry` | Unity-side authoring shell over engine-independent types. The dependency points **`Data` → `Sim`**, one way, always. |
| `Relight.World` | `UnityEngine`, `Relight.Sim`, `Relight.Data` | Imported world *assets* (`CityDefinitionAsset`, `GameplaySitesAsset`) and their converters to the `Relight.Sim` geometry records; runtime loaders | Generated content, read-only at runtime. Same one-way rule: the asset wraps the record, never the reverse. |
| `Relight.Presentation` | `UnityEngine`, `Relight.Sim`, `Relight.Data`, `Relight.World` | `SimHost` MonoBehaviour, view binders, prefab controllers, camera, lighting, audio | Reads sim state, never writes it |
| `Relight.UI` | `UnityEngine`, `Relight.Sim`, `Relight.Data`, `Relight.Presentation` | UI Toolkit documents, view-models, input routing shell | Sends commands only |
| `Relight.Editor` | everything + `UnityEditor` | Importers, validators, inspector tooling, balance-data checks | `Editor`-platform-only asmdef |
| `Relight.Sim.Tests` | **`Relight.Sim` only** | Edit-mode tests, ported from `packages/sim/test`; fixtures built from JSON, never from `ScriptableObject`s | Must run without entering play mode and **without `Relight.Data`** — if a sim test needs `Relight.Data`, the type it needs is in the wrong assembly. |
| `Relight.Tests.Play` | all | A small set of play-mode smoke tests | |

**The `Relight.Sim` asmdef with no `UnityEngine` reference is the single most important structural decision in this proposal.** It reproduces the property that makes the reference project verifiable: the sim can be run headless, replayed, hashed and fuzzed with no engine in the loop.

**Where data types live (corrected).** An earlier draft put the plain-C# records in `Relight.Data` alongside the ScriptableObjects, and let `Relight.Sim.Tests` reference `Relight.Data`. That is wrong in two ways and is corrected throughout this document:

1. `Relight.Sim` cannot reference `Relight.Data` (that would drag `UnityEngine` in), so records defined there would be invisible to the sim — the sim's own `Recipe`/`MachineSpec`/`CityGeometry` parameter types would live in an assembly it cannot see.
2. If tests reach for `Relight.Data`, the headless `dotnet` run of the same tests (§10.5) cannot compile, and the "sim runs with no engine" property quietly stops being tested.

**The rule, stated once:**

> Plain simulation input/data types — item, recipe, machine, weapon, enemy and tuning records, and world-geometry records — are declared in **`Relight.Sim`**. `ScriptableObject` definitions and the converters that produce those records are declared in **`Relight.Data`** (content) and **`Relight.World`** (world geometry), both of which **reference `Relight.Sim`**. `Relight.Sim` never references `Relight.Data` or `Relight.World`, and `Relight.Sim.Tests` references only `Relight.Sim`.

Why `Relight.Sim` rather than a third engine-independent assembly (e.g. `Relight.Contracts`): the records are the sim's own parameter vocabulary — `Recipe` is an argument to the crafting tick, `CityGeometry` is an argument to construction and navigation — and the reference declares exactly these types inside `packages/sim/src` (`recipes.ts`, `types.ts`, `city/riverfront.ts`). Splitting them out would add an assembly boundary that carries no rule the `Relight.Sim` asmdef does not already carry, and would break the file-for-file correspondence with the reference that makes review possible. If a later need appears (for example a tools-only assembly that wants the records without the tick), extracting `Relight.Contracts` from `Relight.Sim` is a mechanical move and can be done then — **UNVERIFIED that it will ever be needed; do not pre-build it.**

Suggested folder layout (mirrors the reference's module grouping so the correspondence stays visible):

```
Assets/Relight/
  Sim/            (Relight.Sim.asmdef)
    Core/         State, SimConfig, Command, SimEvent, Prng, Ledger, Save, Rules
    World/        Map, Tiles, Ground, Graph, Navigation, Walk, Light
    Actor/        Engineer, Equipment, Weapons, Interaction, Inventory
    Production/   Flow, Belts, Routing, Freight, Tram, Truck, Power
    Building/     Construction, Blueprints, Bots
    Combat/       Threat, Enemies, Turrets, Encounters
    Campaign/     Campaign, Progression, Discovery, Districts, Recruits, Goal
                  Data/         Plain records the sim consumes: ItemDef, Recipe, MachineSpec,
                                WeaponSpec, EnemySpec, tuning records, GameData
                  Geometry/     Plain world records: CityGeometry, Parcel, GameplaySites
  Data/           (Relight.Data.asmdef)   ScriptableObject definitions + registries + converters
                                          → references Relight.Sim; contains no sim record types
  World/          (Relight.World.asmdef)  Generated city asset + converters + loaders
                                          → references Relight.Sim; contains no geometry record types
  Presentation/   (Relight.Presentation.asmdef)
  UI/             (Relight.UI.asmdef)     UXML, USS, view-models
  Editor/         (Relight.Editor.asmdef) Importers + validators
  Tests/
```

### 4.2 The simulation host

A single `SimHost : MonoBehaviour` owns the clock. Nothing else ticks the sim.

*Illustrative — not compiled against the selected Unity version.*

```csharp
// Relight.Presentation
public sealed class SimHost : MonoBehaviour
{
    public const int TicksPerSecond = 20;              // mirrors flow.ts:61 TILE_TPS
    public const float TickSeconds  = 1f / TicksPerSecond;
    const int MaxTicksPerFrame = 256 * TicksPerSecond; // mirrors advanceFlow maxTicks

    SimState _state;
    ISimDriver _driver;                 // wraps advanceFlow/advance equivalents
    readonly List<Command> _pending = new();
    double _accumulator;

    public bool Paused { get; private set; }             // the ONLY clock control
    public float Alpha => (float)(_accumulator);         // 0..1 interpolation factor
    public IReadOnlyList<SimEvent> LastTickEvents { get; private set; }

    void Update()
    {
        var dt = Time.unscaledDeltaTime;
        // Mirrors session.ts:258 — discard background gaps rather than fast-forwarding.
        if (!float.IsFinite(dt) || dt < 0f || dt > 0.25f) { dt = 0f; _accumulator = 0; }

        _driver.ApplyCommands(_state, _pending);          // commands apply before ticks, in order
        _pending.Clear();

        if (!Paused) _accumulator += dt * TicksPerSecond;
        var n = (int)System.Math.Floor(_accumulator + 1e-6);   // the epsilon is from flow.ts:1111
        if (n > MaxTicksPerFrame) { n = MaxTicksPerFrame; _accumulator = 0; }
        else _accumulator -= n;

        for (var k = 0; k < n; k++) _driver.Tick(_state);
        LastTickEvents = _driver.DrainEvents(_state);
    }
}
```

**Clock rules (non-negotiable, from `CLAUDE.md` § UI implementation and `docs/CONSTITUTION.md:3`):**

- Player time is **1× with pause only**. There is no speed multiplier, no fast-forward, no interactive time-scale hook, and no `Time.timeScale` manipulation for gameplay. `Time.timeScale` must stay at 1; pause is a `bool` on `SimHost`, not an engine time-scale change (so UI animation and camera continue to work while paused, matching `uiShell.ts`'s live-panel behaviour).
- `slower`/`faster` bindings do not exist in the Unity input asset. (Note the reference still *declares* empty `slower`/`faster` slots in `controls.ts`; the port should drop them entirely.)
- Headless deterministic runners may run ticks as fast as they like — that is `ISimDriver` called from a test, with no `SimHost` involved.

**Interpolation.** Presentation interpolates between the previous and current sim tick using `SimHost.Alpha`. Each moving body's view component keeps `previousPosition`/`currentPosition` snapshots, updated on tick, and lerps in `LateUpdate`. Only *presentation* interpolates; the sim never sees a fractional tick. This is a proposed improvement over the reference (§2.8), not a port of it.

**Threading.** Main thread only. Justification (stated in the terms of §3.5, not as a "50 ms budget"): the reference's measured **tick CPU cost** is 2.43 ms median / 5.50 ms p95 / 10.32 ms max in interpreted JavaScript, and at 20 Hz only about one rendered frame in three carries a tick at all — so on a 16.7 ms frame the sim is a single-digit-millisecond visitor, not the frame's occupant. Any future move to a job must keep the sim deterministic, which means no parallelism inside a tick. If Phase B measurement shows the C# tick cost approaching a significant fraction of the 16.7 ms frame, the only safe pattern is running the whole tick on a single worker thread with a fence before presentation reads — not parallelising within the tick.

### 4.3 Commands and events contracts

Commands are the sole mutation channel; events are the sole "what happened" channel. Both are plain C# in `Relight.Sim`.

**Commands.** The reference's ~50-variant discriminated union should become a sealed class hierarchy (C# has no discriminated unions; records with a sealed base give exhaustive `switch` via pattern matching). All of these types are declared in `Relight.Sim`:

*Illustrative — not compiled against the selected Unity version.*

```csharp
public abstract record Command;

// Body
public sealed record MoveCommand(int X, int Y)                      : Command;
public sealed record WalkCommand(int Dx, int Dy)                    : Command;
public sealed record SprintCommand(bool On)                         : Command;
public sealed record DodgeCommand                                   : Command;
public sealed record AimCommand(TilePoint? At)                      : Command;
// Interaction
public sealed record MineAtCommand(int X, int Y)                    : Command;
public sealed record PlaceCommand(ItemId Item, int X, int Y, Dir Dir): Command;
public sealed record PickUpCommand(int X, int Y)                    : Command;
public sealed record FeedCommand(int X, int Y)                      : Command;
// Inventory / storage
public sealed record ChestTakeCommand(ItemId Item, int N, int? X, int? Y) : Command;
public sealed record ChestPutCommand (ItemId Item, int N, int? X, int? Y) : Command;
public sealed record InventoryCommand(InventoryAction Action)       : Command;
// Construction
public sealed record ConstructCommand(IReadOnlyList<BuildEdit> Edits): Command;
public sealed record BuildPathCommand(IReadOnlyList<TilePoint> Path, Dir Dir): Command;
public sealed record UndoBuildCommand : Command;  public sealed record RedoBuildCommand : Command;
// … one record per reference Command variant; see MIGRATION_MAP.md S-01 for the type vocabulary
```

Dispatch is a single `CommandDispatcher.Apply(SimState, Command)` with an exhaustive `switch` expression, mirroring `applyCommands`. **Rule: no gameplay mutation may exist outside a `Command` handler or a tick function.** A Roslyn analyser or a test that reflects over `Relight.Presentation`/`Relight.UI` for writes to sim types is a cheap way to enforce this and is recommended.

**Command results.** The reference returns `ActionResult` from `dispatch` (`construction.ts` `actionResult`). The port keeps this: `CommandResult { bool Accepted; string Problem; }` — refusals are data the UI shows, never exceptions.

**Events.** Also `Relight.Sim` types.

*Illustrative — not compiled against the selected Unity version.*

```csharp
public abstract record SimEvent(double T);
public sealed record BloomEvent (double T, int X, int Y, double Cr, double Sh, double Hu, bool Wake, int? Id) : SimEvent(T);
public sealed record FallEvent  (double T, int X, int Y, string Reason, double Delay, string Starved)        : SimEvent(T);
public sealed record BrownoutEvent(double T, double DemandKw, double SupplyKw)                               : SimEvent(T);
// …
```

The sim appends to `state.Events`; `SimHost` drains them once per frame (`DrainEvents`) and dispatches to presentation subscribers. Events are **excluded from the save** (see `SAVE_TRANSIENT`, §4.6/§9) and must never be used as a persistence channel.

**A note on `tileHooks`.** The reference's module-global `tileHooks.current` / `threatHooks.current` (§2.4) should **not** be ported as globals. Replace with explicit interfaces (`ITileLayer`, `IThreatLayer`) constructed once and held on the sim's own context object. This removes a class of cross-test contamination and makes the two layers testable in isolation.

### 4.4 Data ownership

| Data | Owner | Lives in | Mutable at runtime? |
|---|---|---|---|
| Gameplay state (engineer, machines, inventories, power, threat, campaign progress) | `Relight.Sim` | `SimState` | Yes — only through commands and tick functions |
| Balance constants and tables (items, recipes, machine specs, weapon profiles, enemy stats, raid tuning, power constants, encounter tuning) | **Record type: `Relight.Sim`. Authoring asset: `Relight.Data`.** | `ScriptableObject` assets in `Relight.Data`, converted once at boot into the immutable `Relight.Sim` records (`Recipe`, `MachineSpec`, …) that the tick functions take as parameters | **No.** Immutable after boot. |
| Authored world geometry (roads, parcels, props, tram, yards, resources) | **Record type: `Relight.Sim` (`CityGeometry`). Authoring asset: `Relight.World`.** | One generated `CityDefinitionAsset` (+ tilemaps/prefabs), converted at load into the `CityGeometry` record injected into `GameData` | No — read-only |
| Gameplay site anchors (camps, gates, arenas, plants, cores, artifacts, recruits) | **Record type: `Relight.Sim` (`GameplaySites`). Authoring asset: `Relight.World`.** | One generated `GameplaySitesAsset`, converted the same way | No — read-only; the *progress* against them lives in `SimState` |
| View state (selected machine, pinned inspection, navigation target, debug coords, camera focus, HUD insets) | `Relight.Presentation` / `Relight.UI` | Plain C# view-model objects | Yes — never serialised into the save |
| Player preferences (key bindings, UI scale) | `Relight.UI` | `PlayerPrefs` or a preferences JSON file | Yes — **separate from inventory and from the save**, per `CLAUDE.md` |

The reference's `view.ts` is the model for the fourth row and should be ported as literally as possible — its "presentation only" comments are a contract.

### 4.5 How presentation reads state

Two mechanisms, in this order of preference:

1. **Selectors.** Port `packages/sim/src/queries.ts` and the per-subsystem selectors as pure static methods on `Relight.Sim` returning small immutable structs/records (`HudView`, `AmmoStatus`, `ClaimInfo`, `MachineInspection`, `NavigationTargets`, …). Presentation calls these; it never walks `SimState` fields itself. This is exactly what the reference does and it is the reason the boundary has held.
2. **Read-only interface views.** Where a selector would allocate per frame (e.g. iterating machines to draw them), expose `IReadOnlyList<T>` over the sim's own collections plus a change counter (`FlowState.rev` in the reference — `flow.ts:313`, bumped on every placement/removal/rotation). Presentation rebuilds its visual index only when `rev` changes.

**Rule: presentation types hold sim *identifiers* (machine id, site id, block index), never sim object references that could be mutated.** Machines already carry a stable `id` (`flow.ts:252`) and the reference's `inspectionView.machineId` demonstrates the pattern.

Snapshotting (deep-copying state for the renderer) is **not** proposed: it would cost a full state copy at 20 Hz for no measured benefit, and the reference proves direct read works when discipline holds.

---

## 5. Data assets

### 5.1 What becomes a ScriptableObject

The rule: *anything a designer would want to change without recompiling, and which the sim reads but never writes.*

**Each row below is two types, not one.** The `ScriptableObject` (`ItemDefinition`, `RecipeDefinition`, …) is the *authoring* type and lives in `Relight.Data`. The immutable record it converts into (`ItemDef`, `Recipe`, `MachineSpec`, …) is the *simulation* type and lives in `Relight.Sim/Data/` (§4.1). The sim only ever sees the record; it has no way to name the asset. From the reference's constants and tables:

| Reference source | Proposed asset type | Proposed asset path |
|---|---|---|
| `flow.ts` `ITEMS` / `Item` union (23 items) + `itemNames.ts` + `itemIcons.ts` | `ItemDefinition` (id, display name, icon, stack size, category) | `Assets/Relight/Data/Items/*.asset` |
| `flow.ts` `RECIPES` / `ASSEMBLER_RECIPES`, `recipes.ts` | `RecipeDefinition` (id, inputs, output, count, seconds, machine) | `Assets/Relight/Data/Recipes/*.asset` |
| `flow.ts` `Kind`/`KINDS`, `MACHINE_COST`, `MACHINE_KW`, `constants.ts` rates (`BELT_PER_S`, `FAST_BELT_PER_S`, `INSERTER_PER_S`, `EXCAVATOR_PER_S`, `ASSEMBLER_TIERS`) | `MachineDefinition` (kind, footprint, cost, kW draw, rate, prefab reference, valid recipes) | `Assets/Relight/Data/Machines/*.asset` |
| `weaponProfiles.ts`, `weaponTypes.ts`, `playerBallistics.ts` | `WeaponDefinition` (range, spread, damage, cooldown, projectile) | `Assets/Relight/Data/Weapons/*.asset` |
| `enemies.ts` `ENEMIES` + thresholds (`SHADE_THRESHOLD`, `HULK_THRESHOLD`, `SHADE_PER_CRAWLERS`, `HULK_BLOOM_FRACTION`) | `EnemyDefinition` + `BloomCompositionAsset` | `Assets/Relight/Data/Enemies/*.asset` |
| `campaignThreat.ts` `ACTIVE_RAIDS` (firstMin, firstRange, intervalMin, intervalRange, warning, grace, minorMin, minorRange, recovery, window, total, activeRaidBudget, livingBudget) | `RaidDirectorTuning` (single asset) | `Assets/Relight/Data/Tuning/RaidDirector.asset` |
| `constants.ts` `SUBSTATION_KW`, `BROWNOUT_RULE`, `campaignPower.ts` `CAMPAIGN_POWER`, `recipes.ts` `GENERATOR_KW`, `POLE_REACH`, `BIG_POLE_REACH` | `PowerTuning` (single asset) | `Assets/Relight/Data/Tuning/Power.asset` |
| `openingEncounter.ts` constants | `OpeningEncounterTuning` (single asset) | `Assets/Relight/Data/Tuning/OpeningEncounter.asset` |
| `constants.ts` `TURRET`, `TURRET_PER_TILES`, `LAMP_STEP_TILES`, `STREETLIGHT_RADIUS`, `EDGE_TURRETS`, `START_TURRETS` | `DefenceTuning` | `Assets/Relight/Data/Tuning/Defence.asset` |
| `rules.ts` `CAMPAIGN_RULES`, `CAMPAIGN_START_POCKETS`, `constants.ts` `START_CHEST` | `CampaignRulesAsset` | `Assets/Relight/Data/Tuning/CampaignRules.asset` |
| `flow.ts` `TRAM_CAP`, `TRAM_TPS`, `TRAM_DWELL_S`, `STOP_CAP`, `SUPPLY_CHEST_CAP` | `TransportTuning` | `Assets/Relight/Data/Tuning/Transport.asset` |
| `types.ts` `SimConfig` / `PROTO_CALIBRATED` | `SimConfigAsset` — with the legacy calibration preset kept as a **separate, clearly-labelled asset** | `Assets/Relight/Data/Config/*.asset` |

The *values* in these assets are Worker A's territory (`CONTENT_CATALOGUE.md`). This document specifies only the shapes and the wiring.

### 5.2 Registry and boot conversion

A `GameDataRegistry : ScriptableObject` (in `Relight.Data`) holds arrays of every definition and is the single asset `Boot` loads. At boot it converts every ScriptableObject into its **immutable `Relight.Sim` record** and hands the sim one `GameData` object — `GameData` itself is a `Relight.Sim` type:

```
Relight.Data                                      Relight.Sim
  ScriptableObject assets ──► GameDataRegistry.Build() ──► GameData (immutable records)
  Relight.World                                              │
  CityDefinitionAsset ────────► ToGeometry() ────────────────┤
  GameplaySitesAsset  ────────► ToSites()    ────────────────┘
                                                             │
                                       held by SimContext for the session's life
```

Note the direction of every arrow: **assembly `Data`/`World` depends on `Sim`, never the reverse.** The converters are the only code that names both a `ScriptableObject` and a sim record, and they all live on the Unity side of the line.

This matters for four reasons: (a) `Relight.Sim` stays free of `UnityEngine` (§4.1); (b) a headless test constructs `GameData` from a JSON fixture with **no Unity and no `Relight.Data` reference at all** (§10.5); (c) the sim's own signatures (`Tick(SimState, GameData)`) can name their parameter types, which is impossible if the records sit in an assembly the sim cannot reference; (d) hot-reloading a ScriptableObject mid-session cannot silently change the rules under a running deterministic simulation — the conversion is a hard boundary. A "reload data" editor command that restarts the session is the safe way to iterate.

### 5.3 Replacing docsync-generated tables

The reference's `packages/tools/src/docsync.ts` regenerates doc tables *from code* and fails CI when prose disagrees with `constants.ts`. That inversion (code is truth, docs are generated) is worth keeping, but the mechanism changes: in Unity, **the ScriptableObjects become the truth**, and an editor tool exports them to Markdown.

Proposal: an `Relight.Editor` menu command `Relight/Export Data Tables` writing Markdown into a generated docs folder (`Unity/Docs/generated/`, written by `Relight/Export Data Tables` since B-05) from `GameDataRegistry`, plus a CI check that re-runs the export and fails on a diff. This preserves the reference's discipline (one owner per number) with the direction reversed from "code → doc" to "asset → doc".

**`freshness.ts` (`source_commit` + `config_hash` stamps on generated evidence) has no obvious Unity analogue for gameplay data** and is proposed for retirement; the equivalent guarantee for the world import is the import manifest in §6.4.

### 5.4 Editor-side validation

The reference validates hard at load time — `stateProblem()` chains 31 validators (§2.10) and `city/validateRiverfront.ts` enforces road/pavement/rail clearance, entrances and factory reservations. Port both directions:

- `ScriptableObject.OnValidate()` on each definition (non-negative costs, recipe inputs/outputs are known items, machine kW matches its kind, prefab reference present).
- A `Relight/Validate Game Data` editor command that runs registry-wide checks (no duplicate ids, every recipe's machine exists, every item has an icon, every machine has a prefab, every tuning number in range) and prints a report. Wire it into CI.
- The world-geometry validator (`validateRiverfront`) becomes an importer-time check (§6.3), failing the import rather than the build.

---

## 6. World data pipeline

The reference's world lives in three places: `packages/sim/src/city/riverfront.ts` (33,879 lines: the canonical geometry — roads, parcels, buildings, props, tram, yards, resources, plants, cores, artifacts, recruits, projects), `city/gameplaySites.ts` (camps, gates, arenas), and `maps/phaser/manifest.json` (810 KB, the Phaser Editor visual layer). `maps/tiled/riverfront-v4/` is an **export-only** reference with no importer.

**Worker C owns the data format's contents.** This section defines the pipeline and its boundaries only.

### 6.1 Shape

```
  ┌─────────────────── TypeScript reference project (unchanged) ───────────────────┐
  │  city/riverfront.ts  ·  city/gameplaySites.ts  ·  maps/phaser/manifest.json    │
  └───────────────────────────────┬────────────────────────────────────────────────┘
                                  │  (A) export step — run once, or on demand
                                  │      new script: packages/tools/src/exportUnity.ts
                                  ▼
                   Unity/World/Source/riverfront-v4.json   (+ a manifest with hashes)
                                  │
                                  │  (B) editor importer — Relight.Editor
                                  ▼
   ┌────────────────── GENERATED, read-only (regenerated on re-import) ──────────────┐
   │  CityDefinitionAsset  ·  GameplaySitesAsset  ·  Tilemap layers (ground, road,   │
   │  pavement, rail, water)  ·  building prefab instances in a generated subscene   │
   └──────────────────────────────┬──────────────────────────────────────────────────┘
                                  │
   ┌──────────────────────────────▼──────────────────────────────────────────────────┐
   │  HAND-EDITABLE, never overwritten: prefab definitions, materials, lighting       │
   │  settings, camera rig, gameplay-site overrides asset, scene-level decoration     │
   │  placed in the "Manual" root                                                     │
   └─────────────────────────────────────────────────────────────────────────────────┘
```

### 6.2 (A) The export step

A new script in the **reference project** (`packages/tools/src/exportUnity.ts`) that imports `RIVERFRONT`, `RIVERFRONT_BUILDINGS`, `RIVERFRONT_PROPS`, `riverfrontRail()` and the gameplay-sites tables and writes one JSON file plus a manifest recording the source commit, `RIVERFRONT_ID` (`riverfront-arc-v4-editor-ac16d9188c05`), building/prop counts and a content hash.

Why an export step rather than reading the `.ts` directly: the `.ts` file is a TypeScript module with computed values (`riverfrontRail()` derives rail geometry procedurally), so it must be *executed*, not parsed. Running it in Node and emitting JSON is the only honest way to get the same numbers the game uses.

This step runs **on demand**, not on every Unity build. Its output is committed into the Unity project so Unity does not depend on a Node toolchain at build time.

**Cost estimate:** small — one script, a few hundred lines, reusing the existing `exportTiled.ts` as a model.

### 6.3 (B) The Unity importer

**Coordinate convention (U-M-14).** The sim keeps the reference tile space unchanged (origin top-left, +y south; WORLD_AND_ASSETS.md §2.2). The importer flips Y once when it writes Tilemap cells and prefab positions, and the presentation layer applies the same flip when it reads actor positions from the query layer. Nothing inside `Relight.Sim` knows about the flip.

A `ScriptedImporter` (or an editor menu command; the `ScriptedImporter` route gives automatic re-import on file change and is preferred — **subject to verification** that it can generate scene content, which it may not be able to do; the fallback is a menu command) that:

1. Deserialises the JSON.
2. **Validates** — port `city/validateRiverfront.ts`'s checks (roads/pavements connectivity, swept rail clearance, building entrances, factory yard reservations) and fail the import loudly on violation, naming the offending id. Also assert the dimensions (864 × 576) and `RIVERFRONT_ID`.
3. Writes `CityDefinitionAsset` — a `ScriptableObject` in `Relight.World` whose serialised fields mirror, one for one, the engine-independent `CityGeometry` record declared in `Relight.Sim/Geometry/` (dimensions, road/pavement/rail tile sets, parcel rectangles, door positions, resource patches, stop positions, yard rectangles). The asset is the authoring/serialisation shell; `CityDefinitionAsset.ToGeometry()` produces the record the sim actually consumes. The sim never sees the asset, and a headless test builds the same `CityGeometry` from the exported JSON with no Unity in the loop.
4. Paints `Tilemap` layers via `SetTilesBlock` for ground/road/pavement/rail/water.
5. Instantiates building prefabs into a **generated** scene root (`World/Generated`), one GameObject per parcel, named by its stable `id` (e.g. `bld:civic:townhall:01`), with a `BuildingView` component carrying the id and a reference to its `BuildingKindDefinition`.
6. Writes `GameplaySitesAsset` from the sites tables, in the same shape relationship: asset in `Relight.World`, `GameplaySites` record in `Relight.Sim`, `ToSites()` between them.
7. Writes an **import manifest asset** recording: source JSON hash, `RIVERFRONT_ID`, import timestamp, counts, importer version.

### 6.4 Generated vs. hand-editable, and re-import safety

The boundary is enforced by **scene structure and asset location**, not by convention:

| | Generated (`World/Generated/…`, `Assets/Relight/World/Generated/…`) | Hand-editable (`World/Manual/…`, `Assets/Relight/World/Authoring/…`) |
|---|---|---|
| Contents | Tilemaps, building instances, `CityDefinitionAsset`, `GameplaySitesAsset`, import manifest | Prefab definitions, materials, lighting volumes, camera rig, audio emitters, manual decoration, a `SiteOverridesAsset` |
| Re-import behaviour | **Deleted and rebuilt in full.** Never hand-edit. | Untouched. |
| Marked how | A `[GeneratedContent]` marker component on the root, a README, and an importer that refuses to run if the generated root contains an object without the marker (i.e. someone hand-edited inside it) | — |

**Re-import preserving manual edits.** Manual edits never live inside the generated root, so nothing to preserve there. For the cases where a designer genuinely needs to tweak a *generated* object (this building's variant, this prop's rotation), provide a `SiteOverridesAsset` in the manual area: a list of `{ generatedId, overrides }` the importer applies as a final pass after regeneration. Overrides referencing an id that no longer exists are reported as warnings, never silently dropped. This is the standard pattern and avoids the unsolvable "merge hand edits into regenerated content" problem.

**Boundary with the Phaser Editor.** `maps/phaser/` and the `editor:*` npm scripts are the reference project's authoring loop. Once the Unity importer exists, the Unity Editor becomes the authoring surface and the Phaser Editor pack (`packages/game/public/relight-editor-pack.json`, `packages/game/src/editor/RiverfrontCity.scene`, `scripts/phaser-editor-*.mjs`, `packages/tools/src/phaserCity*.ts`) is **Retired** for new work — but the manifest stays as the historical source for the one-time export. See §11.

---

## 7. Scenes and prefabs

### 7.1 Scenes

| Scene | Role | Contents |
|---|---|---|
| `Boot` | Entry point. Loads `GameDataRegistry`, builds `GameData`, resolves preferences, then loads `MainMenu`. | A single `BootController`. No gameplay. |
| `MainMenu` | New game / continue / load slot / settings. | A `UIDocument` with the menu UXML. No sim. |
| `World` | The playable world. | `SimHost`, camera rig, lighting, `World/Generated` root (tilemaps + buildings), `World/Manual` root, presentation controllers |
| `GameUI` (additive) | All in-game UI. | One `UIDocument` per panel family, or one root document with panel sub-trees. Loaded additively over `World` so UI can be iterated without touching the world scene. |

`World` is loaded once and never reloaded on save-load: loading a save replaces `SimState` and rebuilds the dynamic views, exactly as `loadState` + `createSession` do in the reference. The authored city geometry is identical for every save (`createCampaign` ignores the seed — §2.11), so there is nothing to regenerate.

**No monolithic runtime constructor.** There is no `GameBootstrap` that instantiates the world from code. The world is a scene a human can open, inspect and understand; the importer writes into it offline. This is an explicit requirement from the brief and is the main reason the pipeline in §6 is an *editor* importer rather than a runtime loader.

### 7.2 Prefab families

| Family | Prefabs | Data link |
|---|---|---|
| Machines | one per `Kind` (belt, fastbelt, underground, splitter, inserter, excavator, pumpjack, assembler, assembler2, mixer, foundry, refinery, alienworkbench, turret, cannon, lamp, arclamp, floodlight, pole, bigpole, substation, generator, chest, depot, wall, barricade, track, tramstop, tram) | `MachineView` holds a `MachineDefinition` reference; the definition holds the prefab reference (bidirectional, validated by §5.4) |
| Turrets | Base + rotating cannon child (the reference has stationary bases with rotating muzzle-aligned cannons — `docs/PROGRAMME_STATE.md` turret follow-up) | `MachineDefinition` + `TurretTracking` state from `turretTracking.ts` |
| Enemies | one per enemy type | `EnemyDefinition` |
| Engineer | one | `EngineerView`; equipment slots as child attachment points |
| Items / conveyor visuals | a pooled item sprite; belts draw their `BeltItem` list | `ItemDefinition` icon/sprite |
| Vehicles | Tram, Truck | `MachineDefinition` / `TransportTuning` |
| Buildings | one per `BuildingKind` (house, terrace, garage, workshop, warehouse, shop, utility, civic, townhall, apartment, office, department, arcade, parking — `riverfront.ts:6`), with variants | `BuildingKindDefinition`; instantiated by the importer (§6.3) |
| Props | one per prop kind (tree, debris, gate, furniture, fence, tank, crane, container, converter, excavator, rock, statue — `riverfront.ts:8`) | `PropKindDefinition` |

**How a prefab links to its data asset:** the prefab's view component holds a serialised reference to its `ScriptableObject` definition; the definition holds an `AssetReference`/direct reference back to the prefab. The registry validator (§5.4) asserts the two agree and that every `Kind`/`BuildingKind`/`ItemId` has exactly one prefab. Spawning goes through a `PrefabRegistry.Get(kind)` lookup built from the registry at boot — never `Resources.Load` by string.

**Pooling.** Items on belts, projectiles and enemy bodies are pooled (`ObjectPool<T>`). Machines and buildings are not — they are long-lived and bounded (479 buildings; player machines change only on `rev` bumps).

---

## 8. UI technical approach

### 8.1 Toolkit

**UI Toolkit at runtime, UXML + USS per panel, authored in UI Builder.** Subject to the verification in §4.0 — specifically that runtime UI Toolkit supports the slot-grid drag-and-drop the inventory needs (`inventoryPanel.ts` + `uiDrag.ts`). Prototype that panel first; if it cannot be done cleanly, fall back to UGUI and record the decision.

The reference's UI is DOM/CSS, and UXML/USS is the closest available analogue — the port is structurally a translation, not a redesign. `style.css`'s shared tokens (`--ui-secondary`, `--ui-bg`, `--ui-top-inset`, `--ui-nav-height`, `--ui-drawer-inset`) become USS custom properties on the root.

The panel inventory and the onboarding flow belong to Worker C (`UI_AND_ONBOARDING.md`). Technically, one `UIDocument` + one UXML + one USS per panel family, all children of a single root `UIShell`.

### 8.2 View-model layer

```
SimState ──► Relight.Sim selectors (queries) ──► immutable view records
                                                        │
                                                        ▼
                                        Relight.UI view-models (INotifyPropertyChanged
                                        or explicit Refresh(), throttled)
                                                        │
                                                        ▼
                                              UXML elements (bound)
                                                        │
                                                        ▼
                                          user action ──► Command ──► SimHost.Queue
```

Panels never read `SimState` directly and never construct sim objects. Refresh is **throttled**, mirroring the reference: `panel.update(performance.now())` is called every frame but the panel throttles internally, and the navigation panel refreshes at most every 150 ms (`main.ts:203`). Proposal: a `UiRefreshScheduler` with per-panel intervals, plus immediate refresh on relevant `SimEvent`s.

### 8.3 Input capture

Port the `uiShell.ts` contract exactly (§2.9):

| Reference rule | Unity implementation |
|---|---|
| uiShell owns input capture, before the world | An `InputRouter` that switches Input System **action maps**: `World` map enabled only when no panel and no modal is open. This is cleaner than the reference's capture-phase `stopImmediatePropagation` and achieves the same result. |
| Held keys suppressed until re-pressed after a panel opens | Disabling the `World` action map cancels in-progress actions; verify that a held movement key does not re-fire on map re-enable, and add an explicit "consume until release" guard if it does. **Subject to verification.** |
| Escape order: cancel drag → close slot menu → close nav menu → close modal child → unpause → close drawer → cancel world selection → pause | A single ordered `EscapeChain` of `IEscapeHandler`s, first one that returns `true` wins. Keep the order literally. |
| One panel at a time (panel replacement) | `UiShell.Open(id)` closes the active panel first. |
| Menus overlay the world; opening/closing must not move the camera | The camera controller never reads UI insets. Insets affect only UI layout. Add a play-mode test that records camera transform across an open/close cycle and asserts equality — the reference had to fix this twice (`docs/PROGRAMME_STATE.md` D-UI-10 note). |
| Panels stay live (the sim keeps running while a drawer is open; only the pause modal pauses) | `SimHost.Paused` is set only by the pause modal. |

### 8.4 Input actions and rebinding

An `InputActionAsset` with maps: `World` (move, sprint, dodge, aim, fire, interact, inspect, rotate, pipette, quickbar 1–0), `UI` (navigate, submit, cancel), `Global` (pause, map, build, backpack, projects, debug, save, load, undo, redo).

Rebinding uses `InputActionRebindingExtensions.PerformInteractiveRebinding`, with the reference's validation ported from `controls.ts` `bindingProblem()`:

- Reserved/fixed: Escape, Tab, and the quickbar number slots are not rebindable (`FIXED` in `controls.ts`).
- Collision detection is **context-aware** — the reference allows the same key in different contexts (`modified` / `blueprint` / `world`) and rejects overlaps within a context. Port this logic rather than using the Input System's flat duplicate check.
- The retired `slower`/`faster` actions do not exist.

**Storage: `PlayerPrefs` (or a `preferences.json` beside the saves) — explicitly separate from the save file and from inventory**, per `CLAUDE.md` ("Customisable shortcuts are browser preferences, separate from inventory"). Rebinds must survive a save-file deletion.

#### 8.4.1 Devices in scope — Owner decisions 2026-09-11 (Q19)

**Decision.** The initial port targets **desktop keyboard and mouse only. Gamepad support is out of initial scope** (TASKS.md DEF-04 keeps the deferral visible). The input architecture stays *extensible*, which is a bounded statement, not an instruction to build for a device nobody has asked for:

**What "extensible" requires (build this):**

- One `InputActionAsset` with the maps and **action names** of §8.4 (`World`, `UI`, `Global`), named for the *intent* (`Fire`, `Interact`, `Inspect`, `Rotate`, `Pipette`, `Pause`, `QuickbarSlot`) and never for the key (`KeyE`, `RightClick`). A later control scheme is then a set of bindings added to existing actions.
- A single **Keyboard&Mouse control scheme** declared explicitly in the asset. Declaring one scheme now is what makes adding a second scheme later a data change rather than a code change.
- Every gameplay read goes through an `InputAction` — `InputAction.ReadValue`/`performed` — never `Input.GetKey`, `Keyboard.current[...]` or a raw device poll. A direct device read is the thing that makes a second device expensive later.
- Pointer-position input is read as an action with a `Vector2` value, so a future stick-driven cursor binds to the same action.

**What "extensible" does not authorise (do not build this):**

- No gamepad control scheme, no `Gamepad`-typed code path, and no `if (Gamepad.current != null)` branch anywhere.
- No **button-glyph system** — no glyph atlas, no device-aware prompt strings, no "press Ⓐ / press E" substitution layer. Prompts are plain key text resolved from the active binding.
- No **rebinding UI for unsupported devices**: the Settings screen (C-10) offers rebinding for keyboard and mouse only, and `PerformInteractiveRebinding` is constrained with `WithControlsExcluding("<Gamepad>")` so a connected pad cannot capture a binding by accident.
- No interface, abstract class or plugin seam invented "for controllers". The action asset *is* the seam.
- No stick-deadzone, vibration, cursor-emulation or focus-navigation work.

**Consequence for verification.** An acceptance check for controller support does not exist and must not be written. A connected gamepad is allowed to do nothing at all; that is the intended behaviour, not a defect.

## 9. Saving, loading and old saves

*(Repair 2026-09-11: this top-level heading was missing — the document jumped from §8.4 straight to §9.1. Nothing is renumbered; the heading simply names the section other documents already cite as §9.)*

### 9.1 Save file design (proposal)

Keep the reference's shape — it is well-designed and battle-tested:

*Illustrative — not compiled against the selected Unity version.*

```jsonc
{
  "version": 2,                 // Unity save schema version, restarts at 1; v2 since the 2026-09-12 repair pass (state.hand.refundSteel/refundCopper, U-M-37)
  "kind": "relight-save-unity",
  "savedAt": "…",               // metadata only, excluded from hash
  "seed": 3,
  "tick": 12345,                // tile tick
  "t": 617.0,                   // sim seconds
  "hash": "a1b2c3d4",           // FNV-1a over canonical JSON of state minus transients
  "dataVersion": "…",           // NEW: GameDataRegistry content hash, so a save knows its balance data
  "state": { … },               // the whole SimState
  "log": [ { "tick": 0, "c": {…} }, … ],
  "logComplete": true
}
```

- **Format: JSON**, written by a serialiser that produces **canonical output** (sorted keys, invariant culture, round-trippable doubles). Do **not** use `JsonUtility` — it cannot handle dictionaries, nullables or polymorphism, all of which `SimState` needs. Use `System.Text.Json` with a custom canonical writer, or Newtonsoft (`com.unity.nuget.newtonsoft-json`) with a sorted contract resolver. **Subject to verification** that the chosen serialiser is available and AOT-safe on the target platforms.
- **Transient fields excluded** exactly as `SAVE_TRANSIENT` does: `events`, `acc`, `speed`. These are reset on load. The port should mark them `[NotSaved]` and have one test asserting the attribute set equals the reference's three fields.
- **Hash:** port the `fnv1a` + `canonicalJson` *technique* from `save.ts:58–75` (§10.3), producing a **Unity-canonical** hash — not a value that matches the TypeScript hash (U-M-13, §10.4 H4). Its job is contract H1: save → load → re-hash → must equal.
- **`dataVersion`** is new. The reference has no equivalent and does not need one because constants are compiled in; in Unity, balance data is an asset that can change independently of the save. Recording its hash lets the loader warn "this save was made with different balance data".
- **Slots:** `{persistentDataPath}/saves/{profile}/{slot}.json`, where `profile` mirrors `profileSlot()` (`exploration-v2` / `legacy-v1`). Keep the profile separation — it is what stops a legacy save loading into a campaign session.
- **Autosave: in scope, decided.** *(Correction 2026-09-11: this bullet previously read "flag for owner decision", and the recorded default was manual save plus save-on-quit with no interval autosave. The owner has since decided — manual saves **plus** rotating autosaves. The old default is superseded; see §9.4.)* Manual slots and the autosave ring are two separate stores and an autosave **never** writes a manual slot. Interval, rotation count, the write protocol and the failure behaviour are specified in **§9.4**.
- **Save schema version:** started clean at **v1**; **v2 since 2026-09-12** (a pending hand-craft refund is saved state, U-M-37; a v1 file is refused by number, not migrated). There is no version 0, no compatibility branch and no reader for reference `SaveFile` v1–3 (Q01/Q07, §9.3).

### 9.2 Reference-save compatibility: facts

| Fact | Source |
|---|---|
| `SaveFile.version` ∈ {1, 2, 3}; must equal `state.version` | `save.ts:75`, `save.ts:114` |
| Campaign has its own version axis, `CampaignState.version` ∈ 1…10 | `rules.ts:13` |
| Further sub-versions: `expansion.surveyVersion` (2\|3), `defence.version`, `authored.version` (4), `flow.ammoVersion`, `flow.version` (1) | `rules.ts:12`, `campaignDefence.ts`, `authoredCity.ts`, `flow.ts:305`, `flow.ts:308` |
| Loading runs 18 `init*`/`migrate*` upgraders in a fixed order, then re-validates | `save.ts:113-124` |
| Validation is 31 chained per-subsystem problem functions | `save.ts:108` |
| Renderer-side gates mark 8 upgrade conditions that clear `logComplete` (replay no longer claimed) but still load | `session.ts:106–118` |
| An authored city save from an earlier map id is **rejected** with "This earlier authored city needs its original build… choose New Game for Riverfront v4" | `authoredCity.ts` `authoredProblem` |
| Reference saves live at `packages/game/public/snapshots/*.json` and `docs/evidence/**/*.json` | `SOURCE_INVENTORY.md` |

### 9.3 Old-save import: decided — no converter

**Owner decisions 2026-09-11 (Q01, Q07): start fresh in Unity.** Existing saves are preserved with the original game and stay loadable *there*. **No old-save converter is required in Unity.** Conversion work is marked **not required** and is removed from every completion dependency: F-06 closes as `done (not required)` with no work, and nothing — F-ACC included — waits on it. The Unity save schema starts clean at **v1** (§9.1).

*(Correction 2026-09-11: what follows was written as a recommendation with the owner's answer outstanding. It is now the decision. The one reversal below is the trigger table: the row "the owner answers U-Q-01 yes" is **struck** — that question is answered **no**, so that trigger can never fire and must not be cited as a reason to build a reader.)*

**Recommendation, now decided: do not import old saves into Unity. Preserve every reference save, byte-for-byte, as read-only test fixtures and as the reference project's own assets, and start Unity players on a new game.**

Reasons:

1. **Cost is high and recurring.** A faithful importer must reproduce all 31 validators and all 18 migrators in C#, then keep reproducing them for every campaign sub-version (currently 10 campaign versions × several orthogonal sub-versions). That is a substantial fraction of the sim port's cost, for content nobody has shipped to players.
2. **There are no players.** Every save in the repository is either a test fixture or an evidence artifact from an internal playtest. `SOURCE_INVENTORY.md` lists them as "Fixture saves". No external user has a save to lose.
3. **The reference project already rejects its own older saves** when the authored city id changes (`authoredProblem`), and `CLAUDE.md` records "Earlier authored v1/v2/v3 saves are preserved but need their original builds; choose New Game for v4." The project's own precedent is preservation-plus-new-game, not migration.
4. **Those saves remain valuable as preserved provenance**, and — *if and only if a concrete need appears* — as narrow verification fixtures.

**What this document does NOT propose (corrected).** An earlier draft proposed a general "test-only reference-save reader" that loads any reference save's `state` JSON into the C# sim and compares `stateHash` against the TypeScript run. That contradicts U-M-13 (no cross-language state-hash identity) and it smuggles the rejected old-save importer back in under a test label: reading an arbitrary reference save faithfully still requires the 31 validators and 18 migrators, just in a test assembly. **It is removed.** Reference saves are **preserved provenance, not required fixtures**, and no build task depends on reading one.

**The concrete trigger for building a reader — now exactly one.** Build a narrow reader for *one named save* only when this has actually happened:

| Trigger | What it justifies |
|---|---|
| A specific ported subsystem disagrees with the reference and static comparison has not found why — **and no other evidence settles it** (a focused reference run under H3, §10.4, is cheaper and is tried first) | A one-off reader for **that one save**, extracting **only the fields that subsystem needs**, used to reproduce the disagreement. Deleted or left as a single focused regression test afterwards. It is never generalised, never given a second save, and never becomes a loader. |
| ~~The owner answers U-Q-01 "yes" and names a save to carry over~~ | **Struck 2026-09-11 (Q01/Q07 answered: no).** The owner starts fresh; no save is carried over. This trigger cannot fire and must not be cited. F-06 closes as `done (not required)`. |

Absent a trigger, the reference saves are read by the *reference project*, which already loads them correctly, and any cross-language comparison is done at the level §10.4 defines: selected gameplay outcomes, inventory totals, or event sequences — never a hash and never a byte comparison.

**Preservation is unconditional:** the reference saves stay where they are, unmodified, and are never regenerated, cleaned or "upgraded". The coordinator records the decision.

### 9.4 Saving: manual slots plus a rotating autosave ring — Owner decisions 2026-09-11 (Q11)

**Decision.** The Unity build has **manual saves and rotating autosaves**. *(Correction 2026-09-11: this supersedes the earlier recorded default of "manual save plus save-on-quit, no interval autosave" — §9.1, R11, MIGRATION_MAP.md S-05/S-37. Save-on-quit stays; interval autosave is added.)* The reference has no autosave at all (§2.10, `session.ts:210`), so every rule below is **new behaviour for the port**, not a translation.

**The rule that governs everything else: an autosave never writes a manual slot.** They are different directories with different name spaces, and the writer for one physically cannot address the other.

#### 9.4.1 Layout

```
{persistentDataPath}/saves/{profile}/
    slot-<name>.json          manual slots (player-named; written only by an explicit Save)
    auto/
        auto-1.json … auto-<N>.json     the rotating ring (implemented 1-based, `AutosaveStore.cs`; U-M-37)
        auto-quit.json                  save-on-quit (its own file, outside the ring)
        auto-index.json                 ring bookkeeping: per-slot tick/timestamp — a cache, reconciled against the files on every read (§9.4.4 step 4)
```

`profile` mirrors `profileSlot()` as in §9.1. Two mechanical guards, both testable:

1. The autosave writer is constructed with the `auto/` directory and rejects any path that resolves outside it.
2. Manual slot names are validated to reject the reserved `auto` prefix, so no manual save can collide with a ring file even if the player types the name.

#### 9.4.2 Recorded defaults (configurable)

| Setting | **Recorded default** | Allowed range | Why this value |
|---|---|---|---|
| `autosaveIntervalMinutes` | **5 minutes of unpaused sim time** | 1–30, or off | A campaign day is `daySeconds: 1200` = 20 minutes with `daylightSeconds: 900` (`rules.ts`, CONTENT_CATALOGUE.md §16). Five minutes loses at most a quarter of a day-cycle — less than one raid warning window (`ACTIVE_RAIDS.warning: 300` s, `campaignThreat.ts:27`) — and at 20 Hz that is 6,000 tile ticks between writes, so the write cost is negligible against the tick budget (§3.3). |
| `autosaveSlots` | **3** | 1–10 | Three files give ~15 minutes of recoverable history: enough to step back *behind* a raid that went wrong, or behind a mistaken area-removal, without keeping a large set of whole-`SimState` JSON files on disk. |
| `autosaveOnEvents` | **on**: raid start, raid resolved, site restored/plant commissioned | on / off | These are the moments the player would most want to return to, and each is already a discrete sim-side state change. An event autosave consumes a ring slot like any other and resets the interval timer. |
| Save-on-quit | **on**, to `auto-quit.json` | on / off | Kept from the earlier default; it is not part of the ring and is never rotated out. |

Interval is measured in **unpaused sim time**, not wall clock: a game paused in a menu for an hour does not accumulate autosaves. Settings live with the other preferences (§4.4, §8.4) — **not in the save file**.

Rotation is round-robin over `auto-1 … auto-N` recorded in `auto-index.json`: the writer always targets the **oldest** slot, so the most recent good autosave is never the file being overwritten. "Oldest" is decided from the reconciled slot metadata (saved-at, then tick, then previous sequence, then file name — U-M-37), never from the index alone.

#### 9.4.3 When the state is captured (never mid-tick)

The snapshot is taken at a **tick boundary**, in `SimHost` (§4.2), after the frame's `for (k < n) Tick(state)` loop has completed and before the next frame's commands are applied. It is never taken inside `Tick`, inside a command handler, or from a presentation callback that might run between subsystems of `stepFlow`.

Sequence, in order:

1. At the tick boundary, if an autosave is due, **serialise to a canonical JSON string on the main thread** (§9.1's canonical writer). Serialisation is synchronous and deterministic; it must see exactly one consistent `SimState`.
2. Hand the finished **string/byte buffer** to a background writer. The writer never touches `SimState` — it only has bytes. This keeps the file I/O off the frame without introducing a data race, and no lock is ever taken on sim state.
3. The `tick` recorded in the file is the tick that had just completed; `savedAt` is metadata and excluded from the hash, as §9.1 already specifies.

Because the buffer is produced at a tick boundary, an autosave always satisfies contract **H1** (§10.4) — the same round-trip test covers it; no separate hash contract is added.

#### 9.4.4 Write protocol (every save, manual and automatic)

| Step | What | Why |
|---|---|---|
| 1 | Write the bytes to `<target>.tmp` **in the same directory** as `<target>` | A cross-directory temp file cannot be renamed atomically if the directories are on different volumes. |
| 2 | **Flush to disk**: `FileStream.Flush(flushToDisk: true)` before closing | Without the fsync, a power loss can leave a renamed-but-empty file — the worst outcome, because it looks valid. |
| 3 | **Atomic replace, previous file retained**: `File.Replace(tmp, target, destinationBackupFileName: <target>.bak)` when `<target>` exists; plain `File.Move(tmp, target)` when it does not | One call does the swap and keeps the superseded file as `.bak`. The rename is atomic within a volume, so a reader never sees a half-written save. |
| 4 | Only after step 3 reports success, update `auto-index.json` (itself written by the same protocol). **The index is bookkeeping, not authority:** every read reconciles it against the ring files' own headers (files win, a slot the index does not know is added, a slot the index names but the file contradicts takes the file's values), so a slot write that succeeded before a failed index update is still the newest save for Continue and is never mistaken for the oldest at the next rotation (repair pass 2026-09-12, audit F4). | The index must never name a slot whose write failed, and a stale or damaged index must never hide a valid save. |
| 5 | Directory-level durability is **best-effort**: .NET exposes no portable directory fsync, so a crash in the window between rename and directory flush can lose the *rename* (leaving the previous good file in place). That is the safe direction of failure and is accepted; recorded here so nobody claims a guarantee the platform does not give. | Honest limit. |

**A recoverable previous save always exists.** After any successful write there are at least: the newly written file, its `.bak` predecessor, and the other N−1 ring slots. The loader, given a file that fails to parse or fails its `hash` check, offers the `.bak` and then the next-newest ring slot — it never deletes a bad file automatically, it renames it `<target>.corrupt` so it can be inspected.

#### 9.4.5 When the write fails

Failures are **data the UI shows**, exactly as command refusals are (§4.3's `CommandResult` rule). No exception escapes to a crash dialog, and **the game is never paused, blocked or stopped by a failed save**.

| Failure | Behaviour |
|---|---|
| **Disk full** (`IOException`, `ERROR_DISK_FULL` / `ENOSPC`) | Abort at step 1 or 2, delete `<target>.tmp`, leave `<target>` and its `.bak` untouched. Show a non-blocking HUD notice naming the last good autosave and its in-game time. Retry at the next interval. |
| **File locked / access denied** (another instance, antivirus, cloud-sync agent) | Treated as transient. Delete the temp if it exists, retry at the next interval with a fresh temp name (`<target>.tmp-<n>`). **Never** spin, sleep or block the main thread waiting for a lock. |
| **Repeated failure** — 3 consecutive failed autosaves | Stop the ring, raise one persistent warning in the HUD ("Autosaving is off: <reason>"), and keep manual saving available (the player may be able to choose a different location). Resume automatically only after a manual save succeeds. |
| **Manual save fails** | Reported inline in the save UI as a refusal with the reason. The player's chosen slot keeps its previous contents; nothing is lost by trying. |
| **Partial/corrupt file found at load** | Never overwritten silently: renamed `<target>.corrupt`, and the loader offers `.bak`, then the next-newest ring slot. When the `.bak` loads, the primary is **restored from it** by the same write protocol so the next load is ordinary; a primary that is missing altogether (not merely damaged) is likewise served from its `.bak` and restored. If the restore itself fails the `.bak` is left in place and the load still succeeds with a warning naming the reason (repair pass 2026-09-12, audit F3). |

#### 9.4.6 What this does not add

No cloud saves, no save compression, no background *serialisation*, no incremental/delta saves, no separate autosave thread touching sim state. If whole-state JSON at 5-minute intervals ever proves too large or too slow, that is a measurement (Phase F) and a new decision — not something to pre-solve here.

---

## 10. Timing, determinism and tests

### 10.1 Fixed tick

20 Hz, `TickSeconds = 0.05`, accumulator in `SimHost` (§4.2), `+1e-6` epsilon preserved from `flow.ts:1111`, gap guard at 0.25 s preserved from `session.ts:258`, max 256 block ticks (5,120 tile ticks) per frame preserved from `advanceFlow`'s `maxTicks`.

### 10.2 PRNG port (exact algorithm)

From `packages/sim/src/prng.ts`. All arithmetic is 32-bit; `Math.imul` is a 32-bit multiply with wraparound, which in C# is `unchecked((int)(a * b))` on `int`, or better, do everything in `uint` with `unchecked`.

*Illustrative — not compiled against the selected Unity version.*

```csharp
public static class Prng
{
    // seedRng
    public static uint Seed(int seed) =>
        unchecked((uint)((int)((uint)seed * 0x9E3779B1u) ^ 0x2545F491));

    // rngNext — mulberry32. `state` is the serialised 32-bit word (SimState.rng).
    public static double Next(ref uint state)
    {
        unchecked
        {
            var a = (uint)((int)state + 0x6D2B79F5);       // (holder.rng + 0x6D2B79F5) | 0
            state = a;
            var t = (uint)((int)(a ^ (a >> 15)) * (int)(1u | a));
            t = ((t + (uint)((int)(t ^ (t >> 7)) * (int)(61u | t))) ^ t);
            return ((t ^ (t >> 14))) / 4294967296.0;
        }
    }

    public static int  Int(ref uint state, int n)                => (int)System.Math.Floor(Next(ref state) * n);
    public static double Uniform(ref uint s, double lo, double hi) => lo + (hi - lo) * Next(ref s);

    // hash01(seed, t, x, y) — position/time hash, documented as bit-identical to frontsim.py's hash01
    public static double Hash01(int seed, int t, int x, int y)
    {
        unchecked
        {
            var h = (uint)seed ^ 0x9E3779B9u;
            h = (uint)((int)(h ^ (uint)t) * (int)0x9E3779B1u); h ^= h >> 15;
            h = (uint)((int)(h ^ (uint)x) * (int)0x9E3779B1u); h ^= h >> 15;
            h = (uint)((int)(h ^ (uint)y) * (int)0x9E3779B1u); h ^= h >> 15;
            h ^= h >> 16;
            h = (uint)((int)h * (int)0x85EBCA6Bu); h ^= h >> 13;
            h = (uint)((int)h * (int)0xC2B2AE35u); h ^= h >> 16;
            return h / 4294967296.0;
        }
    }

    // pyRound — Python's round(): half to even. Used by the crawler count.
    public static double PyRound(double v)
    {
        var f = System.Math.Floor(v); var r = v - f;
        if (r > 0.5) return f + 1;
        if (r < 0.5) return f;
        return f % 2 == 0 ? f : f + 1;
    }
}
```

**Acceptance test:** generate the first 10,000 values from `seedRng(3)` in Node and assert the C# port produces bit-identical doubles. Same for `hash01` over a grid of `(seed,t,x,y)` and for `pyRound` over a range of halves. This test is cheap and catches the whole class of 32-bit-wraparound mistakes.

### 10.3 State hash port

Port `canonicalJson` + `fnv1a` (`save.ts:58–75`) exactly:

- `canonicalJson`: object keys **sorted ordinally**; `undefined`/functions dropped; non-finite numbers → `null`; arrays in order.
- `fnv1a`: `h = 0x811c9dc5`; for each UTF-16 code unit `h ^= c; h = imul(h, 0x01000193) >>> 0`; format as 8 lowercase hex digits.

Note the UTF-16 code-unit detail: C# `string` is also UTF-16, so iterating `char` matches, but only if the canonical JSON string is produced identically (escaping, number formatting). Number formatting is the trap — JS `String(number)` and C# `double.ToString("R")` differ.

**Decided (U-M-13): byte-identity with the TypeScript hash is not pursued.** The algorithm above is ported because the port needs *a* canonical hash, not because the two languages must agree. What the algorithm shape buys is cheapness and a familiar canonicalisation; its output is a **Unity-canonical hash**, defined on the C# state's own terms.

Read §10.3 as: "port the hashing *technique*", not "reproduce the reference's hash *values*". Everything the hash is used for is defined in §10.4.

### 10.4 What the hash and the saves actually guarantee (contracts)

This subsection replaces every earlier suggestion that the port compares hashes with the reference. Four contracts, in descending order of how much they are worth:

| # | Contract | Required? | Statement | Where it is checked |
|---|---|---|---|---|
| H1 | **Unity save round-trip** | **Required** | `UnityHash(Load(Save(state))) == UnityHash(state)` for a state produced by a real headless run. This is what catches a field dropped by the serialiser. | B-11 |
| H2 | **Unity replay consistency** | **Required where a replay path exists** | Two Unity runs of the same seed and the same command log produce the same `UnityHash` at the same tick. Same binary, same data assets, same platform — this is a *self*-consistency claim, not a portability claim. | Alongside the subsystem that owns the tick loop |
| H3 | **Cross-language behavioural agreement** | **Selective, where behaviour should match** | For a named scenario: compare **selected gameplay outcomes** (a site restored, a raid repelled, a recipe completed), **inventory totals per item**, or an **ordered event sequence** — not state, not hashes. Any divergence is a finding to investigate, not automatically a defect: some divergence is *intended* (retired systems, corrected defects, provisional values the owner has changed). | Where a ported subsystem is expected to match, and only there |
| H4 | **Byte-identical TS/C# saves or hashes** | **Explicitly not required, and not to be attempted** (deferred as TASKS.md **DEF-02**, which is not expected to be taken up) | The two save schemas are different by design (U-M-06: Unity schema v1 is fresh; `dataVersion` is new; `ammunitionMigration`/`overclockMigration` are already applied). Byte identity is unreachable and would couple the C# schema to JS number formatting for no player benefit. | — |

Two rules follow that apply to every task in this package:

- **Do not write "compare the hash against the reference".** Write which of H1–H3 is meant.
- **Do not build a general reference-save reader** (§9.3). A cross-language comparison under H3 is set up by running the *reference project* to produce the outcome/total/event list and comparing that list — the C# side never parses a TypeScript save.

### 10.5 Determinism risks specific to the C# port

| Risk | Why | Mitigation |
|---|---|---|
| **Dictionary iteration order** | The reference iterates plain objects (`for (const k in f.store)`, `Object.keys(m.inv)`) and JS guarantees insertion order for string keys. C# `Dictionary<K,V>` guarantees **no** order and it changes with resize history. Any tick logic that iterates a dictionary and short-circuits (`if (n <= 0) break;` in `tramTransfer`, `flow.ts:1196`) is order-sensitive. | Replace every gameplay-relevant dictionary with an **ordered structure**: a `SortedDictionary` on a stable key, or an enum-indexed array. For `Record<Item, number>` inventories, use a fixed-length array indexed by the `ItemId` enum — 23 items, so this is also faster. **This is the highest-priority porting rule.** |
| **Floating-point** | `double` arithmetic is IEEE-754 and deterministic within a platform, but `Math.Pow`/`Math.Sin`/etc. are not guaranteed identical across runtimes. | Audit the sim for transcendental functions; the reference uses `Math.hypot`, `Math.atan2` (presentation only, `main.ts:203`), `Math.floor`, `Math.min/max` — mostly safe. Use `double` throughout the sim (not `float`), and never `Mathf.*` (Unity's float helpers) inside `Relight.Sim`. |
| **`float` vs `double`** | Unity's idiom is `float`. The reference is all `double` (JS numbers). Mixing will drift. | `Relight.Sim` uses `double` exclusively. Conversion to `float` happens only at the presentation boundary. |
| **Iteration over sets** | `new Set(...)` in `ledgerFlows` and `conservation` | Same fix: ordered collections. |
| **Multi-threading** | Any parallelism inside a tick | Forbidden. §4.2. |

### 10.6 Test strategy for Phase B (deliberately small)

Phase B is a foundation, not a test programme. The tests that exist in Phase B are the ones that would otherwise let a silent porting error survive to Phase C. Everything else is **deferred behind a named trigger**, listed at the end of this subsection.

**Where tests live.** `Relight.Sim` has no `UnityEngine` reference, so:

- **Edit-mode tests (Unity Test Framework)** in `Relight.Sim.Tests` are the primary and, in Phase B, the *only* suite. They exercise the sim without entering play mode. `GameData` is built from JSON fixtures in the test assembly, never from `ScriptableObject`s, and the assembly references **only `Relight.Sim`** (§4.1).
- **Play-mode tests** (`Relight.Tests.Play`) cover only what genuinely needs the engine, and in Phase B that is a handful: `SimHost` accumulator behaviour and pause (B-04), camera stability across a panel open/close (B-13).

**When tests are written.** *Alongside the task that ports the subsystem, in the same task* — not in a separate testing task at the end. B-06 ports inventory and ships inventory tests; B-09 ports power and ships the link-rule, no-outage-at-start and turret-holds-fire tests; B-10 ports flow and ships the flow tests. The per-task acceptance rows in `TASKS.md` already name these; they are the test plan.

**The one shared piece: a small deterministic check.** One compact edit-mode fixture (the `Relight.Sim.Tests` foundation is **B-12**, whose own deliverable is the **replay-consistency check** — check 2 below — and nothing more), asserting four things and nothing more:

| # | Check | Owning task |
|---|---|---|
| 1 | PRNG vectors: 10,000 values from `seedRng(3)`, plus seeds 1–10, `hash01` over a small grid and `pyRound` over halves, all bit-identical to Node-generated vectors (§10.2) | B-03 |
| 2 | Two-run equality: the same seed and command list stepped twice in one process gives the same Unity-canonical hash at the same tick (contract H2, §10.4) | B-03 |
| 3 | Save round-trip: `UnityHash(Load(Save(state))) == UnityHash(state)` after a short headless run (contract H1) | B-11 |
| 4 | Conservation: `conservation(state).ok` at the end of that same short run (§2.7) | B-06 |

Checks 1–2 need no save layer and no data assets; 3–4 reuse the same short run. That is the whole of Phase B's cross-cutting test surface.

**Deferred, each behind a concrete trigger — do not build these in Phase B:**

| Deferred | Trigger that would justify building it |
|---|---|
| A separate Unity-free `dotnet test` / console runner sharing `Relight.Sim` sources — **optional, never required** (TASKS.md **DEF-01**) | Unity Test Framework runs become a CI bottleneck (licence, wall time), or a CI machine without a Unity licence is actually needed. **UNVERIFIED** that the shared-compilation setup works; verify before promising it, and do not write it into any acceptance row. |
| Porting the hour bot (`hour.ts`, 1,548 lines) — **DEF-01** | A long-horizon behaviour needs driving that no shorter fixture can reach. |
| Replay *infrastructure* (`replay`, `replayVerdict`, logged command streams) — **DEF-01**. B-12 itself is the edit-mode **replay-consistency check only**: same seed and command list stepped twice in one process → equal Unity-side hash. | The port has a suspected drift that a short two-run check does not reproduce; or Phase E needs a repeatable long scenario. |
| Nightly scheduled runs and a report format — **DEF-01** | There is something to watch nightly — i.e. after Phase C, when a playable opening exists to regress. |
| Multi-hour soak with golden hashes — **DEF-01** | A drift has actually been observed, or Phase F performance/balance work needs a long baseline. |
| The full 75-file reference suite, and any bulk triage of the reference's reported failures — **DEF-01** | Phase-by-phase, as each subsystem lands (§10.7), not as a Phase B batch. |

Recording the trigger, rather than the date, is the point: none of these are "later", they are "when X happens".

### 10.7 Reference tests worth porting

From `packages/sim/test` (75 files). **This is a porting order, not a Phase B deliverable.** Each group is ported by the task that ports its subsystem, in the phase that owns it; the table below says which groups are worth the effort at all and in what order, so no task has to re-decide. Priority order:

| Priority | Test files | Why |
|---|---|---|
| **1 — invariants** | `ledger.test.ts`, `regression.test.ts`, `flow.test.ts`, `tiles.test.ts` | Conservation and the tile layer are the core. `ledger.test.ts` is the single best acceptance test for the port (§2.7). |
| **1 — determinism** | `hour.test.ts` (replay), `walk.test.ts`, `body.test.ts` | Replay and body movement are where drift shows first. |
| **2 — production/logistics** | `routing.test.ts`, `directConveyor.test.ts`, `freight.test.ts`, `transport.test.ts`, `fixedTram.test.ts`, `truckWork.test.ts`, `power.test.ts`, `powerFix.test.ts`, `concrete.test.ts` | |
| **2 — construction** | `construction.test.ts`, `constructionIntegration.test.ts`, `blueprint.test.ts`, `blueprintPlans.test.ts`, `areaRemoval.test.ts` | |
| **2 — combat/threat** | `threat.test.ts`, `defence.test.ts`, `activeRaids.test.ts`, `openingEncounter.test.ts`, `stalker.test.ts`, `emergence.test.ts` | |
| **3 — campaign** | `campaign*.test.ts` (11 files), `progression`-adjacent (`gameplayProgress`, `gameplayCorrections`, `firstRegion`, `fabrication`, `equipment`, `goal`, `project`, `projects`, `commission`, `expansion`, `heart`, `navigation`, `inspection`) | |
| **3 — world** | `city.test.ts`, `riverfront.test.ts`, `cityDensity.test.ts`, `cityPopulation.test.ts`, `foundersCourt.test.ts`, `neighbourhoodDensity.test.ts`, `urban.test.ts`, `phaserEditorCity.test.ts`, `campaignCityValidation.test.ts`, `light.test.ts` | Mostly become **importer** validation (§6.3) rather than sim tests. |
| **4 — UI** | `uiBuild.test.ts`, `uiGuidance.test.ts`, `uiRedesign.test.ts`, `uiSettings.test.ts`, `playerExperience.test.ts`, `playerTestFixes.test.ts`, `playtestFix2.test.ts`, `homeRepair.test.ts`, `gpFactory.test.ts` | These assert *sim-side* selectors the UI reads; port the selector assertions, rewrite the presentation assertions for UI Toolkit. |
| **Retire** | `defenceHarness.test.ts`, `factoryHarness.test.ts` | Harness-shaped; see §11. |
| **Fixtures (not tests)** | `_city.ts`, `_exportCity.ts`, `suppliedCampaignFixture.ts`, `transportFixture.ts` | Port as fixture builders. |

**Baseline caveat.** The reference suite reports **318 of 464 passing** on the working tree (`docs/Implementation/GP_CHECKPOINT.md:178`). The remaining **146 are reported failures whose individual provenance is not established**. A sample was inspected and looked pre-existing — the checkpoint groups them as empty-Home replay fixtures, 780 s minor-raid grace, bullet stack size, per-item hopper rounds, outage rule, legacy-save and seed fixtures (`docs/Implementation/GP_CHECKPOINT.md:178`) — but that is a sample, not an attribution of each failing test, and no per-test triage exists. **Do not write "146 pre-existing failures"**: the sample supports "looked pre-existing", not "all are". Two consequences:

- **This is not a migration prerequisite.** Nothing in Phase B waits on classifying 146 tests, and no task is blocked by the red baseline — **B-12 does no triage**. Producing a 146-row triage list up front would be a large exercise against a suite most of which is never ported; it is deferred to TASKS.md **DEF-01**, behind that row's trigger.
- **The rule that matters is per-test and local:** when a task ports a reference test, it checks whether that test passes in the reference *first*. **Do not port a failing test as a passing expectation.** If it fails in the reference, the porting task decides then — intended behaviour (port and fix) or superseded behaviour (retire with a one-line note) — and records the decision next to the ported test. `GAME_DESIGN.md` is the authority on which.

### 10.8 Replay and soak — what is in and what waits

`SaveFile.hash` is already used as a replay check in the reference (`save.ts` header: *"a save at minute N reloads to the same hash and replays to the same hash as the unbroken run"*). The port keeps the *idea* and stages the work:

| Check | Phase B? | Notes |
|---|---|---|
| `UnityHash(Load(Save(state))) == UnityHash(state)` — save round-trip | **Yes** — contract H1, §10.4, owned by B-11 | The cheapest high-value check in the package. |
| Same seed + same commands, stepped twice in one process → equal hash | **Yes** — contract H2, owned by B-03 | Needs no replay infrastructure: just step the driver twice. |
| Full replay infrastructure (`replay`, `replayVerdict`, a logged command stream reconstructed from tick 0) | **No** — deferred (§10.6) | Trigger: a suspected drift the two-run check does not reproduce, or a Phase E long scenario. |
| Multi-hour soak asserting `conservation().ok` at intervals against a golden hash | **No** — deferred (§10.6) | Trigger: an observed drift, or Phase F balance/performance baselining. A golden hash recorded before the sim is stable is a maintenance burden, not a guard. |
| Any comparison of a Unity hash against a TypeScript hash | **No** — contract H4, §10.4; deferred as TASKS.md **DEF-02** and not expected to be taken up | |

---

## 11. Tooling: port, adapt, or retire

| Tool | Reference source | Disposition | Reason |
|---|---|---|---|
| Conservation ledger | `packages/sim/src/ledger.ts` | **Port** | It is sim logic and the best acceptance test. |
| Replay / `stateHash` | `save.ts`, `hour.ts` `replay` | **Port** | Determinism guarantee. |
| Experiment runner E1–E9, E-variance, E-walk | `harness/src/run.ts`, `experiments/` | **Adapt** — a headless `dotnet` runner, kept small | These calibrated the block-layer economy, which is Retired for the campaign. Keep the *runner* (a way to run N hours headless and emit JSON); port only experiments that still describe live systems. |
| E-hour, E-rifle, E-heart, E-project | `harness/src/experiments/` | **Retire** | Bound to the legacy block sim, the Heart candidate and the ring — all Retired for the campaign (`rules.ts:47`). |
| Defence scenario CLI | `harness/src/defenceCli.ts`, `defenceScenario.ts` | **Port** | Raid behaviour is live and needs headless soak testing. |
| Factory scenario CLI | `harness/src/factoryCli.ts`, `factoryScenario.ts` | **Port** | Production throughput is live. |
| City validation CLI + worker + population | `harness/src/cityValidation*.ts`, `cityPopulation.ts`, `citycheck.ts` | **Adapt → importer validation** | §6.3. The 10,000-seed sweep is moot: `createCampaign` ignores the seed (`campaign.ts:22`) — the city is authored, not generated. |
| `calibrate.ts`, `bench.ts`, `snapshot.ts`, `goalcheck.ts` | `harness/src` | **Retire** (keep `bench` as a model for a new Unity profiling harness) | Calibration targeted the block sim. |
| `nightly.ts`, nightly reports | `harness/src/nightly.ts`, `docs/evidence/**` | **Adapt** | The *practice* (a scheduled headless soak that writes a report) is worth keeping; the report format and the evidence-freshness stamping are not. |
| `provenance.ts` (`configOf`, evidence hashing) | `harness/src` | **Retire** | Replaced by `dataVersion` (§9.1) + the import manifest (§6.3). |
| `docsync.ts` / `campaign-docsync.ts` | `tools/src` | **Adapt, direction reversed** | §5.3 — assets become truth, an editor command exports tables, CI diffs. |
| `freshness.ts` | `tools/src` | **Retire** | §5.3. Its guarantee is covered by the import manifest and `dataVersion`. |
| `exportTiled.ts` | `tools/src` | **Retire** | Export-only with no importer (`SOURCE_INVENTORY.md`); Unity does not consume `.tmj`. Keep the existing `maps/tiled/riverfront-v4/` output as a preserved historical artifact. |
| `phaserCity.ts` / `phaserCityExport.ts` / `phaserCityModel.ts` / `phaserCityPlots.ts`, `scripts/phaser-editor-*.mjs`, `packages/game/public/relight-editor-pack.json`, `packages/game/src/editor/RiverfrontCity.scene`, `phasereditor2d.config.json` | tools + game + root | **Retire after one final export** | Replaced by the Unity importer (§6). Run the export (§6.2) from the current manifest, freeze the Phaser Editor artifacts as historical source, and do not maintain the loop. |
| `authorCityDensity.ts`, `authorNeighbourhood.ts`, `docs/evidence/city-c/authorFullCity.py` | tools + evidence | **Retire** (preserve) | The city is authored and frozen at `riverfront-arc-v4-editor-ac16d9188c05`. Future editing happens in Unity. Keep the scripts as provenance for how the current city was made. |
| `section18.ts`, `seeds.ts` | `tools/src` | **Retire** | §18 map drawings were already retired in the D5/D6 rework (`docsync.ts` header); seed sweeps are moot for an authored city. |
| `telemetry.ts` | `packages/game/src` | **Retire** — decided | **Owner decisions 2026-09-11 (Q10):** the old telemetry/reporting system is **retired**. What is permitted instead is **small local diagnostic tools, added only when a concrete development or playtest need justifies one** — a counter printed to the console, a frame-time overlay, a one-off CSV dumped beside the saves. What is **not** built: any analytics service or SDK, any network upload, any user identifier, any scheduled reporting programme, any successor to the nightly report format. No Unity task inherits a reporting obligation, and nothing in Phases B–F has an acceptance row that depends on a report being produced. A local tool is written by the task that needs it, lives with that task, and is deleted when the question it answered is closed. (Consequences elsewhere: MIGRATION_MAP.md S-50 stays **Retired**; S-59's "keep the practice" means a headless soak *run*, not a reporting programme; the built-in `com.unity.modules.unityanalytics` module in the project shell stays unused — §4.0.2.1.) |
| Nightly evidence corpus (`docs/evidence/**`) | reference repo | **Preserve, do not port** | Historical evidence. `CLAUDE.md`: "Archives/reports are evidence, not instructions to restore superseded behaviour." |

---

## 12. Verification checklist for Phase B

Before writing production code:

**Platform**
- [x] *(done 2026-09-11/12 — `6000.6.0f1 (f7f8ed4d1e24)` at `D:\Unity\Editor`, DECISIONS.md U-M-27)* Install Unity **6.6 (`6000.6.x`, a Supported/Update release, not LTS)** through Unity Hub. At the time of writing it was not installed (§4.0.2); later the same day the owner installed `6000.6.0f1` and created the `Unity/Relight/` shell (§4.0.2, observed note), so B-01 now starts by recording that install.
- [x] *(done 2026-09-12, U-M-27; SOURCE_INVENTORY.md § Reference snapshot)* Record the exact patch string from `ProjectSettings/ProjectVersion.txt` (e.g. `6000.6.0f1`) in DECISIONS.md U-M-01 and SOURCE_INVENTORY.md.
- [x] *(checked 2026-09-12: only the withdrawal notes match)* Check that no document in this package says **"Unity 6.6 LTS"**. The permitted strings are exactly the short and long forms in §4.0.1: *"Unity 6.6 (6000.6), a Supported (Update) release — not LTS"*. `grep -rn "6.6 LTS" Unity/` must return nothing.
- [x] *(recorded in U-M-27)* Record the upgrade-review trigger for a Supported release (when `6000.7` ships, or when the project needs a fix only present in a later release) — not a date.
- [ ] Confirm URP 2D Renderer is the right pipeline for this game's lighting; measure a dynamic-light scene against the reference's ~24 ms light-mask cost.
- [ ] Build a throwaway 864×576 Tilemap; measure fill time, memory and frame cost; decide chunking.
- [ ] Prototype the inventory slot grid in runtime UI Toolkit including drag-and-drop; if it fails, decide UGUI and record it.
- [ ] Prototype Input System interactive rebinding with the reference's context-aware collision rules.
- [ ] Confirm a canonical JSON serialiser (System.Text.Json or Newtonsoft) is available and AOT-safe for the target platforms.
- [x] *(verified 2026-09-12: `Relight.Sim.asmdef` has `references: []` and `noEngineReferences: true`; batchmode compile clean; `AssemblyBoundaryTests` asserts the referenced-assembly list; the scratch `dotnet build` of the same sources has no engine reference at all)* Confirm asmdef can enforce "no `UnityEngine`" on `Relight.Sim` (it can via an asmdef with no references and `noEngineReferences`) — **verify the exact flag name and behaviour**.
- [x] *(verified 2026-09-12: `Relight.Sim.Tests.asmdef` references `Relight.Sim` plus the two Unity Test Runner assemblies NUnit needs — never `Relight.Data` or `Relight.World`)* Confirm `Relight.Sim.Tests` references **only** `Relight.Sim` — not `Relight.Data`, not `Relight.World` (§4.1). A test that needs a machine spec builds the record in the test, or loads a JSON fixture; it never loads a `ScriptableObject`.
- [x] *(verified 2026-09-12: `Assets/Relight/Sim/**` compiles alone in the scratch `dotnet build`)* Confirm `Relight.Sim` declares the record types and `Relight.Data` / `Relight.World` reference **it** — the dependency arrow never points the other way (§4.1). Deleting `Relight.Data` from the solution must leave `Relight.Sim` compiling.
- [ ] *(Not a Phase B item)* A Unity-free `dotnet test` project sharing `Relight.Sim` sources is **optional and deferred** (TASKS.md **DEF-01**, trigger in §10.6). B-12 delivers the edit-mode test foundation and the replay-consistency check, and nothing beyond it.

**Editor tooling (MCP)**
- [ ] Install the Coplay Unity package in the project and authenticate it; the MCP server does nothing without it (§14.2).
- [x] *(B-02a 2026-09-12, U-M-28: the server starts by hand (4.0.3, 98 tools registered) but Claude Code's 30 s connect timeout is hit at session start; the Unity-side plugin is not installed — owner decision; outcome "MCP unavailable, all capabilities manual")* Get `coplay-mcp` to connect at all. It **failed to connect in the authoring session** (connect timeout, 2026-09-11); until it connects, nothing about its tool surface is verified.
- [ ] Run the vendor's own smoke test — *"List all of the open unity editors"* — and confirm it returns this project, not another editor instance (§14.3).
- [x] *(filled 2026-09-12: every row "not exercised", fallback in force — DECISIONS.md §2.1)* Walk the §14.5 capability table against the live editor and **replace each "UNVERIFIED" with what the tools actually did**. Anything absent is recorded as "manual (owner or worker in the Editor)" with the fallback written beside it.
- [ ] Confirm the UI workflow: a UXML file authored as text opens and remains editable in UI Builder (§14.6, DECISIONS.md U-M-21).

**Sim port fidelity**
- [ ] PRNG bit-identity test against Node-generated vectors (§10.2).
- [ ] Every `Record<Item, number>` replaced by an enum-indexed array; no gameplay-relevant `Dictionary` iteration (§10.5).
- [ ] `double` everywhere in `Relight.Sim`; no `Mathf`, no `float`, no `UnityEngine.Random`.
- [ ] `SAVE_TRANSIENT` equivalent is exactly `{events, acc, speed}` and a test asserts it.
- [ ] `conservation()` ported and green at the end of the short headless run in §10.6 (a multi-hour soak is **deferred**, §10.8).
- [ ] Contract **H1**: `UnityHash(Load(Save(state))) == UnityHash(state)` (§10.4, B-11).
- [ ] Contract **H2**: the same seed and command list stepped twice in one process produces the same hash at the same tick (§10.4, B-03).
- [ ] No test, script or document compares a Unity hash against a TypeScript hash (contract **H4**, §10.4). `grep -rni "match.*reference hash\|reference hash" Unity/` returns nothing that asserts equality.
- [ ] **No general reference-save reader has been built** (§9.3). If one exists, the concrete trigger that justified it is named in writing.

**Structure**
- [ ] No script constructs the world at runtime; the `World` scene is human-readable.
- [ ] Generated content lives under a marked root and is fully regenerated on re-import; manual content is outside it.
- [ ] Every `MachineDefinition` ↔ prefab link validated both ways.
- [ ] `Time.timeScale` is never written by gameplay code; pause is a `SimHost` bool.
- [ ] No `slower`/`faster` input action exists.
- [ ] A play-mode test asserts the camera transform is unchanged across a panel open/close cycle.
- [ ] Escape chain order matches `uiShell.ts` exactly, with a test per step.

**Owner decisions 2026-09-11 (Q05, Q09, Q10, Q11, Q19) — added checks**
- [x] *(B-01: rasterise on export, U-M-27)* Vector Graphics: either `com.unity.vectorgraphics` is installed (version recorded in U-M-01) or the world pipeline rasterises SVG on export, and B-01 says which. `com.unity.modules.vectorgraphics` alone does **not** satisfy this (§4.0.2.1).
- [ ] No analytics service is configured and no Analytics API is called anywhere (Q10, §11). `grep -rn "Unity.Services\|Analytics\." Assets/Relight/` returns nothing that sends data.
- [ ] No gamepad control scheme, glyph system or `Gamepad`-typed code path exists (Q19, §8.4.1); `PerformInteractiveRebinding` excludes `<Gamepad>`; no acceptance row asserts controller support.
- [ ] Autosave (§9.4): the ring writes only under `saves/{profile}/auto/`; a test asserts an autosave cannot address a manual slot and that a manual slot name starting `auto` is refused.
- [ ] Autosave capture happens at a tick boundary in `SimHost`, never inside `Tick` or a command handler; the background writer receives bytes and never a reference to `SimState`.
- [ ] Save write protocol: temp file in the same directory → `Flush(flushToDisk: true)` → `File.Replace` with a `.bak` backup retained. A test simulates a write failure and asserts the previous file and its `.bak` survive intact and the game keeps running.
- [ ] Audio (§15): `grep -rn "UnityEngine.Audio\|AudioSource\|AudioClip\|PlayOneShot" Assets/Relight/Sim/` returns **nothing**. The `Relight.Sim` asmdef still has no `UnityEngine` reference, which makes this structurally impossible — the grep is the cheap confirmation that nobody moved a type.
- [ ] Audio cue assets are excluded from `GameData` and from `dataVersion` (§15.2): changing a sound never invalidates a save.

**Process**
- [ ] Reference saves preserved unmodified as **provenance, not fixtures**; the no-import decision recorded by the coordinator (§9.3).
- [x] *(re-verified 2026-09-12: `sha256sum -c` — 254 OK, 0 failed)* The reference baseline is the preserved snapshot, not a commit (**U-M-19**): **no commit of the reference working tree is required before B-01**, and none is made without owner authorisation. It re-verifies — `cd /mnt/e/Factorio2 && sha256sum -c Unity/Docs/reference-snapshot.sha256` reports no failures (SOURCE_INVENTORY.md § Reference snapshot).
- [ ] *(Not a Phase B item)* A 146-row triage of the reference's reported failures is **not required** — they are "146 reported failures whose individual provenance is not established; a sample looked pre-existing (`GP_CHECKPOINT.md:178`); not a migration prerequisite". Bulk triage is TASKS.md **DEF-01**, trigger-gated. The rule is per-test and local: a task that ports a reference test checks that test in the reference first (§10.7).
- [ ] Open gates from `docs/PROGRESS.md` carried forward, not silently closed (60 fps not certified; P6-AMMO unreproduced; P6-H/P7-H/P8-H/P9-H/EX-08H/T18 not run).

---

## 13. Risks and open technical questions

| # | Risk / question | Severity | Notes |
|---|---|---|---|
| R1 | **Dictionary/set iteration order silently changes behaviour.** | **High** | The most likely source of "the C# sim drifts after 40 minutes". Mitigation in §10.5 must be a porting rule, not a code-review hope. |
| R2 | **Lighting performance.** The reference's worst recorded frames (116.8–300.2 ms, §3.3) are rendering-side; `docs/P5_05_INTEGRATION_REPORT.md:76` measured changed light-mask repaints at ~24 ms with 116.8/150.1 ms worst frames, while the GP checkpoint's 166.8 ms tail has no established cause (`GP_CHECKPOINT.md:47`). Unity's 2D lights may or may not be cheaper at this density. | **High** | Must be prototyped and measured in Phase B before scene structure is fixed, because it may dictate chunking and light baking. |
| R3 | **Test baseline is red.** 318/464 passing (`GP_CHECKPOINT.md:178`); the remainder are **146 reported failures whose individual provenance is not established** — a sample was inspected and looked pre-existing, and no more than that is claimed. Porting one of them as a passing expectation bakes in wrong behaviour. | Medium | **Not a migration prerequisite** and not a batch exercise: no up-front triage is required (§10.7). The mitigation is local — a porting task checks its one test in the reference before porting it, and records the call. `GAME_DESIGN.md` is the design authority when it does; bulk triage is deferred to DEF-01. |
| R4 | **Uncommitted reference work.** The GP checkpoint (72 substantive files) is uncommitted on `main`. If the working tree is lost, the behaviour being ported changes. | **High** | Not this document's call, but flag it: the migration should pin a commit, and the owner should decide whether to commit the checkpoint first. |
| R5 | **Byte-identical cross-language state hashing.** JS/C# number formatting differs; matching `canonicalJson` exactly is fiddly. | Low — **decided** | Not pursued (DECISIONS.md U-M-13, contract H4 in §10.4). The Unity hash is Unity-canonical; cross-language agreement is checked selectively, on gameplay outcomes, inventory totals or ordered event sequences (contract H3). Closed; listed here only so it is not re-opened by accident. |
| R6 | **Runtime UI Toolkit maturity for drag-and-drop slot grids.** | Medium | §4.0, §8.1. Prototype first. Fallback is UGUI, which costs UI Builder editability. |
| R7 | **Tilemap at 497,664 tiles.** | Medium | §3.4. May force region chunking, which changes scene structure. |
| R8 | **`tileHooks` / `threatHooks` module globals** in the reference encode a load-order dependency that is easy to reproduce wrongly. | Medium | §4.3 — replace with explicit interfaces. Requires care: `flow.ts` installs hooks on import, and `sim.ts` calls them only when `st.flow` exists. |
| R9 | **`riverfront.ts` is 33,879 lines of literal data inside the sim package.** Any C# equivalent must not be a 34k-line source file (compile times, IDE, and it is data, not code). | Medium | §6 — it becomes an imported asset. But the *sim* also reads `RIVERFRONT`, `RIVERFRONT_BUILDINGS` and `RIVERFRONT_PROPS` directly (`authoredCity.ts`, `campaignPower.ts`, `riverfrontCampaign.ts`), so `Relight.Sim` needs the geometry injected as `GameData`, not compiled in. This is a real coupling to break. |
| R10 | **Art is owner-pending.** `CLAUDE.md`: "Existing artwork remains pending the owner's new package." The 65 riverfront SVGs are placeholders; the `Assets/urban-decay/` RPG-Maker tileset has unverified licence/provenance (`SOURCE_INVENTORY.md`). | Medium | Worker C's area, but architecturally it means prefab visuals must be swappable without touching sim or data shapes. Addressables or plain prefab variants both work. |
| R11 | **Autosave is a new behaviour**, not a port. | Low — **decided** | **Owner decisions 2026-09-11 (Q11): manual saves plus a rotating autosave ring, in scope.** Specified in **§9.4** (5 minutes of unpaused sim time, 3 ring slots, both configurable; tick-boundary capture; temp → fsync → atomic replace with `.bak` retained; failures are UI data, never a crash or a pause). The residual risk is no longer "will it be added" but "it is new code with no reference to check against" — hence the explicit §12 checks. |
| R12 | **Telemetry's future is undecided.** | Low — **decided** | **Owner decisions 2026-09-11 (Q10): retired.** Small local diagnostic tools only, justified case by case; no analytics service, no scheduled reporting programme, no Unity task inheriting a reporting obligation (§11; MIGRATION_MAP.md S-50). Closed; listed so it is not re-opened by accident. |
| R17 | **Audio is new content with no reference to port from.** The reference game has **no audio at all** — verified 2026-09-11: no `.mp3`/`.ogg`/`.wav`/`.m4a` under `packages/game/public/`, and no `AudioContext`/`new Audio`/`sound` usage in `packages/game/src`. Every cue, mix level and asset is therefore an origination, and the usual failure mode is an audio layer that grows into a workstream or leaks into the sim. | Medium | **Owner decisions 2026-09-11 (Q09): audio is in scope**, proportionate, from reusable properly sourced assets — §15. Bounded by three rules: no audio type or call inside `Relight.Sim`; a fixed voice budget; no bespoke framework, no voice acting, no original soundtrack. Licence and provenance are recorded per asset, the same discipline the art package gets (U-Q-04). |
| R13 | **No measured machine/belt-item ceiling exists.** Sizing the machine loop is therefore an estimate. | Low | §3.2. Measure in Phase B with a deliberately large factory. |
| R15 | **The Unity MCP integration is unverified and has a hard prerequisite chain.** `coplay-mcp` failed to connect on 2026-09-11, and its Unity-side half needs a Unity project + the Coplay package + authentication — none of which exist yet (§14). | Medium | Plan Phase B so nothing *blocks* on MCP: every capability in §14.5 has a manual fallback, and B-02a's job is to find out which fallbacks are actually needed. Do not write tasks whose acceptance depends on an MCP tool that has never run. |
| R16 | **Editor operations performed by an agent are invisible to review.** An MCP-driven scene or asset change leaves no diff to read if it is not saved to a text-serialised asset and, in the end, to git. | Medium | Keep Force Text serialisation on, keep generated content under the marked root (§6.4), and treat "the agent changed something in the Editor" as a change that must appear in the working tree before it counts. |
| R14 | **Interpolation is proposed, not ported.** The reference appears not to interpolate; adding it changes how movement reads. | Low | §4.2, §2.8. Verify the reference's behaviour in `worldScene.ts` before deciding. |

**Open questions for the coordinator / owner:**

1. Cross-language state-hash identity — **decided**: not pursued; verify by selected outcomes / inventory totals / event sequences (DECISIONS.md U-M-13, §10.4 H3–H4, R5).
2. ~~Autosave: add, or match the reference's manual-only saving?~~ — **answered 2026-09-11 (Q11):** manual saves **plus** a rotating autosave ring; §9.4 holds the specification. Closed. (R11)
3. ~~Telemetry: retire, or redesign for Unity?~~ — **answered 2026-09-11 (Q10):** retired; small local diagnostic tools only, on a concrete need. Closed. (R12)
4. ~~Should the GP checkpoint working-tree changes be committed before the migration pins its reference commit?~~ — **answered 2026-09-11 (Q12):** no. The baseline is the preserved snapshot (HEAD, the uncommitted/untracked file list, per-file content hashes); **a commit is not a prerequisite**, and nothing in this migration may reset, discard or commit the owner's changes. Closed (U-M-19; SOURCE_INVENTORY.md § Reference snapshot). R4 stays listed as a *risk* — losing the working tree still changes what is being ported — but it is no longer an open question.
5. Old-save import — **decided**: no import; reference saves preserved as read-only **provenance**, not required fixtures (DECISIONS.md U-M-06, §9.3). **Confirmed 2026-09-11 (Q01/Q07): start fresh; no converter required; conversion removed from completion dependencies.**
6. Unity version and packages — **answered 2026-09-11 (Q05):** Unity 6.6 with UI Toolkit stands; only justified packages are added; a concrete compatibility blocker is **recorded**, not worked around by changing the engine version or the UI system (§4.0.1, §4.0.2.1). The one concrete item outstanding is SVG import (Vector Graphics), which is a package/pipeline choice for B-01, not a version problem.
7. Does the owner want the Coplay Unity package and account in this project at all, given it is a third-party editor plugin with an authentication step (§14.2)? If not, the fallback is manual editor work plus, optionally, Unity's own MCP server (§14.7) — and B-02a becomes "record that MCP is not used".
8. Audio — **answered 2026-09-11 (Q09): in scope**, reversing the earlier "silence with a hook point" default (U-M-24). Architecture in §15; the remaining owner input is asset taste and sourcing, not scope.
9. Controls — **answered 2026-09-11 (Q19):** desktop keyboard and mouse; gamepad out of initial scope, with "extensible" bounded exactly as §8.4.1 defines it.

**Still genuinely open after 2026-09-11:** item 7 (Coplay/MCP), plus the two owner questions this document does not own — the ending (U-Q-02) and the tileset licence and art package (U-Q-04, awaiting external information). *(Correction 2026-09-11: this line also listed the per-feature go-ahead at Phase E and the two browser retest verdicts. Both are resolved — U-Q-06 by U-D-31, which puts the full approved gameplay scope in the port with no per-feature go-ahead, and U-Q-20 by U-D-43, which carries the intended fixes into Unity as C-12 checks and records the browser retests as superseded rather than passed.)*

---

## 14. Unity MCP integration

**Status when this section was written (2026-09-11): configured, not working, nothing verified.** The `coplay-mcp` server is present in the session configuration and **failed to connect** (`CONNECT_TIMEOUT`, 30,000 ms). Every statement below about what the integration *can do* is therefore either (a) quoted from vendor documentation, with the URL, or (b) labelled **UNVERIFIED**. Nothing here was observed working. B-02a exists to convert the UNVERIFIED rows into observed fact — see DECISIONS.md U-M-20.

### 14.1 What it is

An MCP (Model Context Protocol) server that lets a coding agent — this one — operate a running Unity Editor: read its state, run operations inside it, and read back what happened. Two halves, and both must be present:

| Half | What it is | Source |
|---|---|---|
| The MCP server | `coplay-mcp-server`, a Python package run by `uvx`. Configured here as `uvx --python ">=3.11" coplay-mcp-server@latest` over stdio with `MCP_TOOL_TIMEOUT=720000` (12 minutes, because the `coplay_task` tool can run long). | <https://docs.coplay.dev/coplay-mcp/guide> ; <https://pypi.org/project/coplay-mcp-server/> (version 1.5.5 at time of writing) |
| The Unity-side plugin | The **Coplay Unity package**, installed into the project via Package Manager → *Add package from git URL*. **The MCP server is an interface to this plugin, not a standalone bridge** — the vendor Quick Start states the prerequisite plainly: *"Ensure you have Coplay properly installed in your Unity project before proceeding."* | <https://docs.coplay.dev/getting-started/installation> ; <https://docs.coplay.dev/coplay-mcp/guide> |

**Documented git URL discrepancy — resolve at B-02a, do not guess.** The installation page gives `https://github.com/CoplayDev/coplay-unity-plugin.git#beta` (and a `#beta-unity-2021` branch for Unity 2021); the Claude Code setup page gives `https://github.com/CoplayDev/unity-plugin.git#beta`. Both repository URLs resolve (HTTP 200, checked 2026-09-11), so which one the current plugin lives at is **UNVERIFIED**; take the one on the installation page as primary and record which actually installs.

**Prerequisite chain, in order.** Nothing in this section is available until every link exists: a **Unity 6.6 install** (§4.0.2 — `6000.6.0f1` observed installed later on 2026-09-11) → a **Unity project** (B-01) → the **Coplay package installed** in it → **authentication** (the Quick Start says *"Once you have Coplay installed and authenticated"*; what the account or licence requires is **UNVERIFIED**) → the **Editor open with that project loaded** → the **MCP server connecting**. This chain is why B-02a sits early in Phase B but cannot be its first task, and why no other task's acceptance may depend on MCP.

### 14.2 Prerequisites, restated as a checklist

- [ ] Unity `6000.6.x` installed (§4.0.2).
- [ ] `uv` / `uvx` available, Python ≥ 3.11 (<https://docs.coplay.dev/coplay-mcp/guide>).
- [ ] Git available (needed by Package Manager for the git-URL install).
- [ ] Coplay Unity package installed into the project and enabled.
- [ ] Coplay authenticated. **UNVERIFIED**: whether this needs a paid account.
- [ ] Unity Editor open, project loaded. The vendor troubleshooting entry for *"Unity project not detected"* is exactly this: *"Ensure Unity Editor is open with your project loaded."*
- [ ] `claude mcp list` shows `coplay-mcp: ✓ Connected`.

### 14.3 How it attaches to the correct project and editor instance

This matters here because the machine already has multiple Unity editors installed (§4.0.2) and the repository may be open alongside other projects.

| Mechanism | What the documentation says | Source |
|---|---|---|
| Discovery | *"Unity Project Discovery: Automatically discover running Unity Editor instances and their project roots."* The vendor's own smoke test is *"List all of the open unity editors"*, and *"If this yields the actual list of open editors, you're good to go."* | <https://pypi.org/project/coplay-mcp-server/> ; <https://docs.coplay.dev/coplay-mcp/guide> |
| Explicit selection | *"If you're working with multiple Unity projects, you can explicitly set which project Claude should work with"* — by setting the Unity project root (documented as the natural-language instruction *"Set the Unity project root to D:\Unity\MyProject"*). | <https://docs.coplay.dev/coplay-mcp/claude-code-guide> |
| Multiple editors at once | The vendor demonstrates *"edit multiple Unity projects simultaneously"* via MCP. | <https://docs.coplay.dev/coplay-mcp/guide> |

**Working rule for this package.** Before any MCP operation that writes anything, list the editor instances and set the project root explicitly to this repository's Unity project, even when only one editor is open. Record the resolved project root in the task's evidence. An operation applied to the wrong editor is silent — there is no diff to notice it in.

### 14.4 The documented tool surface

The only enumerated capability list the vendor publishes is the PyPI package description (<https://pypi.org/project/coplay-mcp-server/>, version 1.5.5). Quoted:

- *"Schema-Based Tool Registration: Dynamically registers tools from JSON schema files"* — so the exact tool names are supplied by the server at runtime and **depend on the installed Unity plugin version**; they cannot be enumerated from documentation alone. (The same page notes *"Tools are locked to specific schema versions, ensuring compatibility with Unity plugin versions."*)
- *"Unity Project Discovery"* — running editor instances and their project roots.
- *"Unity Editor State: Retrieve current Unity Editor state and scene hierarchy information."*
- *"Script Execution: Execute arbitrary C# scripts within the Unity Editor."*
- *"Log Management: Access and filter Unity console logs."*
- *"GameObject Hierarchy: List and filter GameObjects in the scene hierarchy."*
- *"Task Creation: Create new Coplay tasks directly from MCP clients"* (`coplay_task`).

**No per-tool reference page was reachable.** `https://coplaydev.github.io/unity-mcp/tools/` and `.../reference/tools/` both returned HTTP 404 on 2026-09-11. Treat the seven bullets above as the whole of the *documented* surface and everything else as unknown until B-02a.

**The general-purpose lever.** *Script Execution* is documented, and arbitrary editor C# can in principle reach `AssetDatabase.Refresh()`, `EditorApplication.isPlaying`, `ScreenCapture.CaptureScreenshot`, `CompilationPipeline` and the Test Runner API. That is an **inference about what editor C# can do, not a claim about the integration** — whether a long-running or domain-reloading operation survives an MCP call is exactly the sort of thing that only trying it settles. Every row below that leans on script execution is marked accordingly.

### 14.5 Capability table — what Phase B actually needs

Each row: what the port needs it for, whether the *documented* surface provides it, and the fallback if it does not. **Filled 2026-09-12 (B-02a).** The server registers tools for several rows, but with the Unity-side plugin absent nothing was exercised, so no UNVERIFIED label is disproved and every fallback stays in force; the verbatim tool list is in DECISIONS.md §2.1.

| Capability | Needed for | Documented? | Fallback if absent | Observed at B-02a |
|---|---|---|---|---|
| **Asset refresh / import** | Every time an agent writes a `.cs`, `.uxml`, `.uss` or generated asset outside the Editor, Unity must re-import it before anything sees it. | **Not named as a tool.** Plausible via *Script Execution* (`AssetDatabase.Refresh()`) — **UNVERIFIED**. | Owner or worker focuses the Editor window; Unity auto-refreshes on focus. Cheap and reliable; this is the fallback to assume. | *(B-02a 2026-09-12: no such tool registered; not exercised)* — batchmode import on launch / Editor focus |
| **Compilation result** (did the C# compile, and the errors if not) | The single most valuable one: an agent that cannot read compile errors is writing blind. | **Not documented as a distinct tool.** Partly reachable through *Log Management* — compile errors appear in the Unity console — so "read the console after a refresh" is the documented path. **UNVERIFIED** whether it reports compile *completion* (i.e. whether the agent can tell "still compiling" from "compiled clean"). | Read the console for `CS####` errors after a refresh; if completion cannot be detected, poll the console and treat quiescence as done, or have the owner report. | *(B-02a: `check_compile_errors` registered; not exercised, plugin absent)* — batchmode log `grep "error CS"` |
| **Console inspection** | Reading errors, warnings, `Debug.Log` from sim tests and importers. | **Yes** — *"Log Management: Access and filter Unity console logs."* | — | *(B-02a: `get_unity_logs` registered; not exercised)* — batchmode log file |
| **Scene inspection** | Verifying the `World` scene, prefab wiring, `UIDocument` roots. | **Yes** — *"Unity Editor State ... scene hierarchy information"* and *"GameObject Hierarchy: List and filter GameObjects"*. | — | *(B-02a: `get_unity_editor_state` registered; hierarchy listing not seen in the tool list; not exercised)* — open the scene in the Editor |
| **Scene and asset *operations*** (create GameObjects, add/configure components, create materials, set up Input Actions, create UI elements) | B-02/B-05/B-07 scaffolding. | **Yes, as advertised behaviour**: the vendor lists exactly these — *"Create and modify GameObjects; Add and configure components; Generate materials and textures; Set up Input Actions; Create UI elements"* (<https://docs.coplay.dev/coplay-mcp/claude-code-guide>). Which are *individual MCP tools* versus things the in-Editor Coplay agent does via `coplay_task` is **UNVERIFIED**. | Author assets as text where the format allows (UXML, USS, `.asset` YAML, asmdef, Input Action assets are all text) and let Unity import them. **Prefer this anyway** — a text-authored asset is reviewable in a diff; an MCP-mutated scene is not (R16). | *(B-02a: individual tools ARE registered — `create_scene`, `create_game_object`, `add_component`, `set_property`, `create_prefab`, `create_material`, `create_panel_settings_asset`, Input System tools; not exercised)* — text-authored assets + `-executeMethod` bootstrap, as preferred |
| **Play Mode enter/exit** | Play-mode tests, seeing the sim actually run. | **Not documented.** Plausible via *Script Execution* setting `EditorApplication.isPlaying` — but entering play mode triggers a domain reload, which may well drop the connection mid-call. **UNVERIFIED, and expect trouble.** | Owner or worker presses Play. Edit-mode tests (which are Phase B's whole suite, §10.6) need no play mode at all — this is deliberately a small dependency. | *(B-02a: `play_game` / `stop_game` registered; not exercised)* — owner presses Play |
| **Screenshot capture** | Visual verification of UI and scenes; UI Toolkit work is otherwise unreviewable by an agent. | **Not documented.** Plausible via *Script Execution* (`ScreenCapture.CaptureScreenshot` to a file the agent then reads). **UNVERIFIED.** | Owner takes the screenshot and attaches it, or the worker writes an editor script that captures to disk and reads the file back. Under U-M-21 a visual verification is *"a screenshot or the owner's inspection, recorded as such"* — either satisfies it. | *(B-02a: no screenshot tool registered)* — `Relight.Editor` capture script or owner screenshot |
| **Interaction / input testing** (drive a click, a drag, a keypress) | The inventory slot-grid drag (§8.1, R6) is the hardest interaction in the port and the one most worth automating. | **Not documented at all.** No input-simulation capability appears in any vendor source read. **UNVERIFIED — assume absent.** | Play-mode tests using the Input System's `InputTestFixture` / `InputSystem.QueueStateEvent` inside Unity, driven by the Test Runner — not by MCP. Otherwise: owner plays it. This is the fallback to plan for. | *(B-02a: no input-driving tool registered)* — Input System test fixture / owner play |
| **Running the Test Runner** | Getting edit-mode test results back (§10.6). | **Not documented.** Plausible via *Script Execution* + the `TestRunnerApi`, or via the CLI (`Unity -runTests -batchmode`), which needs no MCP at all. **UNVERIFIED.** | Unity CLI batch-mode test run from the shell, results read from the XML output. This is probably better than MCP even if MCP works, because it produces a file. | *(B-02a: no test-runner tool registered)* — `Unity.exe -batchmode -runTests -testPlatform EditMode` |

**Rule that follows from the table:** *no Phase B task may have an acceptance row that only an MCP tool can satisfy.* Every row above has a manual or CLI fallback; MCP is a speed-up, and B-02a's output is a list of which speed-ups are real.

### 14.6 UI authoring workflow (aligned with DECISIONS.md U-M-21)

The integration does **not** change how UI is authored. The workflow is:

1. **Agents author text.** UXML layouts, USS styles, and the C# view-model/binding code (§8.2) are written as files, like any other source. They are diffable, reviewable and revertible.
2. **Layouts must stay openable in UI Builder.** No code-only construct that UI Builder cannot represent, unless the exception is recorded with a reason. The owner must be able to open any panel visually and move things.
3. **MCP is for editor *operations and inspection*** — refresh, compile, console, and (if verified) Play Mode and screenshots. **It is not used to drive UI Builder or Shader Graph visually**, and this document does not assume any such capability exists; nothing in the documented surface suggests it does.
4. **Visual verification is a screenshot or the owner's inspection, recorded as such.** An agent stating that a layout "looks right" without one of those is not verification.

The same rule holds for Shader Graph and any other visual editor: agents may create and wire the *assets* that feed it, but the graph itself is the owner's tool.

### 14.7 Alternative: Unity's own MCP server

Worth recording because it removes the third-party plugin and account from the chain, and because it is first-party documented.

Unity ships an MCP bridge in the **AI Assistant package** (`com.unity.ai.assistant`, manual read at version `2.11.0-pre.2` on 2026-09-11). Its overview states the architecture directly: *"Unity acts as an MCP server that exposes tools, while external AI clients act as MCP clients that discover and invoke those tools"*; the relay runs as an external process communicating with the Editor over IPC (named pipes on Windows, Unix sockets on macOS/Linux), and it *"supports multi-client connections to the same Unity instance simultaneously"*. The built-in tools are described by category — *"scene management, asset operations, script editing, and console access"* — with *"read console output"* called out; it names **Claude Code** as an example client. It does **not** enumerate tool names, does **not** mention play mode, screenshots or compilation monitoring, and does **not** address multiple Editor instances. Sources: <https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.11/manual/integration/unity-mcp-overview.html>, <https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.11/manual/integration/unity-mcp-landing.html>.

Notes before treating this as a drop-in: the package version read is a **pre-release** (`-pre.2`); which Unity versions ship which AI Assistant version is **UNVERIFIED**; and the package brings Unity's AI Assistant features with it, which is a separate decision from wanting an MCP bridge. It also has documented tool *registration* — custom tools can be exposed — which the Coplay route does not appear to offer.

**Recommendation:** keep `coplay-mcp` as the configured integration (U-M-20), and treat Unity's own server as the fallback to evaluate **if** B-02a finds Coplay unworkable, the plugin/account is unwanted (open question 7, §13), or capability gaps in §14.5 turn out to matter. Do not run both: U-M-20 is explicit that it is one integration, one operator at a time.

### 14.8 What B-02a must produce

Not a working demo — a **record**:

1. Which plugin git URL installed, and the plugin + server versions.
2. Whether the server connects, and what it took (the connect timeout of 2026-09-11 is the starting point, not a fluke to ignore).
3. The **actual tool list** the server registers, pasted verbatim — the schema-based registration means this is the only reliable enumeration.
4. The §14.5 table with every "Observed at B-02a" cell filled and every disproved UNVERIFIED deleted.
5. For each capability found absent: the fallback, written next to it, in a form a later task can just follow.
6. The resolved project root, so later sessions attach to the right editor.

If step 2 fails after a reasonable attempt, that is a complete and acceptable B-02a outcome: record "MCP unavailable, all capabilities manual", and Phase B proceeds unchanged — which is the point of every row in §14.5 having a fallback.

---

## 15. Audio architecture — Owner decisions 2026-09-11 (Q09)

**Decision.** *(Correction 2026-09-11: this section reverses the earlier default "silence with a hook point" recorded as U-M-24. Audio is **in scope**.)* The port ships with audio, implemented proportionately, built from **reusable, properly sourced assets**, with sensible volume controls.

**Required coverage:** player and turret weapons; impacts and appropriate enemy cues; machine operation; crafting and construction feedback; raid warnings; UI interactions; environmental ambience.

**Explicitly not required, and not to be built:** voice acting; a bespoke soundtrack; an elaborate audio framework. No FMOD, no Wwise, no custom DSP graph, no runtime audio synthesis, no adaptive music system. Unity's built-in audio (`com.unity.modules.audio`, already in the shell manifest — §4.0.2.1) is the whole of the technology choice; **no package is added for audio**.

**Baseline fact.** The reference game has **no audio whatsoever** — verified 2026-09-11: no `.mp3`, `.ogg`, `.wav` or `.m4a` anywhere under `packages/game/public/`, and no `new Audio`, `AudioContext` or sound handling in `packages/game/src`. There is nothing to port. Everything here is an origination, which is why the boundary rules below are stated before the content.

### 15.1 Where audio sits relative to the simulation

> **`Relight.Sim` contains no audio type, no audio field, no clip name, no cue id and no call into anything that makes a sound. Ever.**

This is the same rule as §4.1's engine independence and it is enforced by the same mechanism: `Relight.Sim` has **no `UnityEngine` reference** in its asmdef, so `AudioSource`, `AudioClip` and `AudioMixer` are not even nameable there. The §12 grep is a confirmation, not the enforcement.

Audio is driven **from** the simulation, never **by** it:

```
SimState ──tick──► SimEvent records ──┐
                                      ├──► AudioDirector (Relight.Presentation) ──► pooled AudioSource
sim-visible state changes ────────────┘         │
(TurretTracking.shot.t, machine running flag,   │  Relight.UI ──────────────────────► UI cues
 alert list, day phase)                         │
                                                └── reads AudioCueAsset (Relight.Data/Audio)
```

Three consequences a later implementer must not re-derive:

1. **The sim decides *what happened*; presentation decides *what it sounds like*.** A turret firing is `TurretTracking.shot` changing; which clip plays, at what volume and pan, is entirely a presentation decision.
2. **If a cue needs information the sim does not expose, the fix is a plain data field or a new `SimEvent` record — never an audio call.** Adding `WeaponId` to a fire event is correct; adding `PlaySound(...)` to `Relight.Sim` is not, and would break the headless `dotnet` property that makes the sim testable at all.
3. **Audio must never affect the simulation.** No cue result, playback failure, listener state or mixer value is read back by gameplay. Muting the game must not change a single tick — a determinism claim that is trivially testable: run the deterministic check (§10.6) with audio enabled and disabled and compare the hash.

**A practical note on the current event vocabulary.** The reference's `SimEvent` union (`types.ts:241-304`, ~30 variants) is largely the *legacy block layer* — `claim`, `bloom`, `fall`, `project`, `heart`, `stalker`. It carries almost nothing the campaign's audio needs: there is no "turret fired", no "machine crafted", no "enemy hit" event. So most cues are driven by one of two mechanisms, both already precedented in the reference:

| Mechanism | Precedent | Use for |
|---|---|---|
| **Sim-visible "last action" state with a timestamp**, polled by presentation | `TurretTracking.shot = {x, y, angle, t}` — its own comment says `t` "lets the renderer trace it briefly" (`turretTracking.ts:8-9`) | Turret shots, player shots, muzzle effects — anything already drawn from the same field |
| **New plain `SimEvent` records added during the port**, where no state field exists | The existing union's shape | Impacts, enemy death, craft completed, construction completed, raid phase changes |

Both are pure data. Deciding which applies is a per-subsystem call made by the task that ports that subsystem.

### 15.2 Assemblies and assets

| Piece | Assembly | Notes |
|---|---|---|
| `AudioCueAsset` (ScriptableObject: clip variants, bus, gain, pitch range, max concurrent, cooldown, priority, loop flag) | `Relight.Data/Audio/` | A `ScriptableObject`, so it lives with the other authoring assets (§5). **It has no converter to a `Relight.Sim` record** — audio never reaches the sim. |
| `AudioBankAsset` (the cue registry: cue id → `AudioCueAsset`) | `Relight.Data/Audio/` | Validated like the other registries (§5.4): every referenced cue exists, every clip is assigned, no duplicate cue ids. |
| `AudioDirector` (subscribes to events/state, resolves a cue, applies the limiter, plays) | `Relight.Presentation/Audio/` | One instance, owned by the `World` scene. |
| UI cue playback | `Relight.UI` | UI Toolkit callbacks call the director with a cue id; the UI never holds an `AudioSource` itself. |
| Clips | `Assets/Relight/Audio/` | Provenance and licence recorded per asset, the same discipline as the art package (R10, U-Q-04). |

**Audio assets are excluded from `GameData` and from `dataVersion`** (§9.1). Balance data changing should warn a save; a sound changing must not. This also keeps the boot conversion (§5.2) free of audio entirely.

### 15.3 Mixer and buses

One `AudioMixer` asset, five exposed volume parameters, a flat and unambitious tree:

```
Master
├── SFX
│   ├── Weapons      player + turret fire
│   ├── Impacts      hits, enemy cues, structure damage
│   ├── Machines     production loops, belts, generators
│   └── Construction placement, crafting, bot work
├── Alerts           raid warnings, power loss, low ammo   (never ducked)
├── UI               clicks, drags, refusals
├── Ambience         day/night beds, wind, distant city
└── Music            reserved and empty — no bespoke soundtrack (Q09)
```

- **Volume controls:** Master, SFX, Alerts, UI, Ambience (and Music if it is ever populated). Stored with the other **preferences**, not in the save (§4.4, §8.4). Slider `0..1` maps to dB as `20 * log10(v)`, with `v == 0` setting `-80 dB` and muting the group — never `log10(0)`.
- **Alerts are never ducked and never voice-stolen** (§15.4): a raid warning the player cannot hear over a factory is a gameplay failure, not a mix preference. A light duck of `Machines` and `Ambience` while an alert plays is permitted.
- No reverb zones, no snapshots beyond the alert duck, no runtime mixer graph building.

**Reconciliation with TASKS.md D-11**, which names "master, effects, ambience and UI groups": those four are `Master`, `SFX`, `Ambience` and `UI` above. The tree adds exactly two things and no more — `Alerts` is split out of effects so warnings can be exempted from ducking and voice stealing, and `Music` exists as an empty reserved group so nobody adds a soundtrack bus later by improvising one. The `SFX` children are organisational, not extra controls: the exposed volume parameters remain five.

### 15.4 Pooling and the concurrency limit

The reference already has a busy world — 479 buildings, hundreds of placed machines, `livingBudget: 240` actors (`campaignThreat.ts:27-28`). One `AudioSource` per machine is not viable, so:

| Rule | Value | Why |
|---|---|---|
| Pooled one-shot sources | **32 concurrent voices**, from an `ObjectPool<AudioSource>` (the same pooling rule as §7.2's belt items and projectiles) | A hard ceiling makes worst-case audio cost bounded and measurable. 32 is generous for a 2D game and well inside platform limits. |
| Per-cue cap | **max 4 simultaneous instances of the same cue**, plus a **40 ms same-cue debounce** | Stops a raid's worth of simultaneous turret shots becoming a single distorted spike. |
| Voice stealing | By **priority, then age**: `Alerts` > `Weapons`/`Impacts` > `UI` > `Construction` > `Machines` > `Ambience`. An `Alerts` cue always finds a voice; the oldest lowest-priority voice is reclaimed. | Determines what is sacrificed under load, once, instead of per call site. |
| Machine loops | **Not one source per machine.** One looping source per *audible cluster* of same-kind machines within the camera view, its gain scaled by cluster size (sub-linear), culled by distance and stopped when off-screen. | Bounded by screen area, not by factory size — the only formulation that survives a four-factory workload (E-16). |
| Ambience | **2 looping sources** (day bed, night bed) cross-faded on the sim's day phase (`CAMPAIGN_RULES.daySeconds: 1200`, `daylightSeconds: 900`) — outside the 32-voice pool. | Ambience must never be voice-stolen by combat. |
| Listener | One `AudioListener` on the camera rig. 2D positioning: linear rolloff keyed to the camera's view size, stereo pan from screen-relative x. | The world is 864 × 576 tiles; a distance rolloff tuned in world units without reference to zoom will be wrong at every zoom level but one. |

**Measurement.** Voice count and audio CPU are recorded alongside frame time in the F-07 performance pass. If audio is ever a measurable fraction of the frame, the limiter values above are the dial — not the architecture.

### 15.5 Coverage map (what plays, and what drives it)

Content values (which clip, how loud) are not this document's to own; this is the wiring only.

| Required coverage (Q09) | Driven by | Bus |
|---|---|---|
| Player weapon fire | The `fire` command's resolution in `playerBallistics.ts` — a per-weapon fire event or last-shot state added with S-19's port (C-02/C-03) | Weapons |
| Turret fire | `TurretTracking.shot.t` changing (`turretTracking.ts:8-9`) — the same field the tracer is drawn from (S-31, C-04) | Weapons |
| Impacts and enemy cues | Damage resolution in `gameplayCombat.ts` (skitter / spitter / guardian today; Skitter, Spitter, Stalker, Breaker, Howler as E-13/E-14 land them, Q06/Q13). Per-enemy: spawn/alert, attack windup, death. **Windup cues matter**: `windup` is 0.3 s (skitter) to 1.2 s (guardian) — a readable tell (S-29) | Impacts |
| Machine operation | Per-`Kind` loops from `stepFlow`'s machine pass, clustered per §15.4 (S-21). Distinct one-shots for state *changes* the player must notice: hopper empty, generator dry, brownout (`brownout` / `power-ok` / `hopper-empty` / `gen-dry` already exist as `SimEvent`s) | Machines / Alerts |
| Crafting and construction feedback | Hand-craft completion, assembler output, ghost placement, blueprint order accepted, bot work, and a distinct **refusal** cue for a rejected `CommandResult` (S-20, S-26, S-27) | Construction / UI |
| Raid warnings | The campaign director's warning phase — `ACTIVE_RAIDS.warning: 300` s before a major attack, plus raid start and raid resolved (`campaignThreat.ts:27-28`, S-30). Announced-target and approach information is in the alert feed (S-35); the cue marks the moment, the UI carries the detail | Alerts |
| UI interactions | UI Toolkit callbacks in `Relight.UI`: click, panel open/close, drag pick-up and drop, transfer, refusal | UI |
| Environmental ambience | Day/night phase from the sim light model (S-10, C-11); optional local emitters (river, tram) placed in the `World/Manual` root (§6.4) so the importer never overwrites them | Ambience |

### 15.6 Sequencing

Audio is not a Phase B concern: Phase B has no playable world to make a sound. The task placement is TASKS.md's, and this document simply records which row owns which part of the architecture above:

| Task | What it owns from §15 |
|---|---|
| **C-11** | The single `AudioCue` presentation hook — signature and event routing only, no clips. This is §15.1's boundary made concrete. |
| **D-11** | The foundation: the cue catalogue (§15.2), the mixer and its groups (§15.3), the pooled one-shot player with per-cue throttling and distance attenuation (§15.4), and the first clip set — weapons, impacts and enemy cues, machine operation, crafting and construction feedback, raid warnings, UI (§15.5). |
| **C-10** | The volume controls in Settings, stored with preferences and never in the save (§15.3). |
| **F-04** | Completion: ambience with day/night variation, machine loops with falloff and voice limits, the raid-warning and outcome set, and the mix pass across the groups (§15.3–§15.5). |

Cues are still added opportunistically by the Phase C–E tasks that create the moments they mark — a turret port that already exposes `shot.t` costs nothing extra to sound — and **nothing in Phases B–E blocks on audio**. TASKS.md alone owns the status of those rows.

*(Correction 2026-09-11: an earlier draft of this subsection named F-04 as the single home for audio. TASKS.md D-11 is the foundation row and F-04 the completion row; the table above is the accurate placement.)*

---

*Companion: [MIGRATION_MAP.md](MIGRATION_MAP.md) — the per-system disposition table this document's proposals are built on.*
