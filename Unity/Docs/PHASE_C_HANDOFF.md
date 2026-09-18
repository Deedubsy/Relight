# Relight — Unity Phase C handoff (Home opening)

Written 2026-09-14 from the Phase C coordinator ledger and the nine worker reports. Phase C engineering is
complete; **C-ACC, the owner's own play-through, has not been run** — §9 is the checklist for it. Task status itself
lives in [TASKS.md](TASKS.md), which is the sole owner of it; this file is the owner-facing summary of what was
built, how to launch it, what was checked and what is still owed.

Where a worker report does not say something, this document says **"not stated in the report"** rather than guessing.

---

## 1. What is playable

The whole Home opening runs in Unity, on the real Founders Court geometry rather than the synthetic test map:

- **The world.** The Home region is imported from the live reference city: a `78 x 367` rect at `(43, 91)` of
  `riverfront-arc-v4-editor-ac16d9188c05`, 49 buildings, 33 props, 28,626 land tiles of which 24,360 are accessible
  and 4,266 solid, with the Founders Court substation at `(78, 347)` inside the window. The engineer spawns at tile
  `(27, 269)` in region coordinates, exactly where the reference puts them.
- **The engineer.** Movement, reach, hold-to-mine with feedback, interaction, machine placement with costs and
  refunds, an action bar where one tool occupies exactly one slot, owned weapons and an equipment strip.
- **Production and power.** Excavators, Foundry, Mixer, Assembler and the rest of the 26 machine assets; recipes and
  stationary handcraft with a working Cancel; a power network with footprint-measured links, circuits, throttling and
  generators burning real coal or refined fuel out of real inventories; conveyors, fast belts, inserters,
  undergrounds, splitters and direct conveyor loading.
- **Defence.** The Home core with HP, priced repair, a disabled/recommission path and a repair card that is first in
  the Home drawer; gun turrets with hoppers, power draw, target tracking, a rotating cannon and visible tracers;
  Skitters and Spitters pathing to the core; a single raid director with saved timestamps.
- **The opening tutorial.** The full state machine — schedule, warning, introductory attack, ammunition feedback,
  automated resupply, three turrets, hand-off to a nearby excursion — with repelled / lost / skipped / **deferred**
  outcomes, a goal card and an opening hint.
- **Front end and saves.** Boot → title screen with New Game, Continue, Load and Settings; save, load, autosave with
  a configurable interval and rotation; a pause menu; rebindable controls and audio sliders stored outside the save.
  Save schema is **v3**, and v1 and v2 saves upgrade forward.

**What is not playable yet:** anything beyond the Home region. There is no full city, no tram, no truck, no
exploration beyond the nearest first-region camp, no second plant and no ending. Those are Phases D–F.

---

## 2. How to launch

**In the editor.** Open `Assets/Relight/Scenes/Boot.unity` and press Play: Boot loads **MainMenu**; choose
**New Game**; MainMenu loads **World**, and World loads **GameUI** additively — the interface is its own scene. That
whole flow was exercised in the integrated run. To skip the front end, open `World.unity` **and** `GameUI.unity`
together and press Play, which starts a new game; opening `World.unity` on its own gives a world with no interface.
The four build scenes, in order, are `Boot`, `MainMenu`, `World`, `GameUI`.

**Continue / Load.** From the title screen, **Continue** picks the newest valid save, manual or autosave, whichever
is newer and actually loads. **Load** lists each save with its day, playtime, save time and map id, and marks
autosaves as such. A save made against a different map id is reported rather than silently loaded.

**A standalone player build.** There is none: no Phase C worker made or ran one. Every check in §6 was run
in-editor on **Unity 6000.6.0f1**, driven through the **Unity CLI 1.0.0-beta.8**.

---

## 3. Controls

Read from `Assets/Relight/Input/RelightControls.inputactions` as it stands on 2026-09-14, **after the correction pass** (TASKS.md CP-02, DECISIONS U-M-39). Click-to-move is gone; Tab opens the Backpack; B opens the Build menu.

| Action | Binding | What it does |
|---|---|---|
| `World/Move` | **W A S D** and the **arrow keys** | Walk. |
| `World/Sprint` | **Left Shift** | Sprint (the sprint flag is carried on the engineer input, wave1-WC report §6). |
| `World/Dodge` | **Space** | Dodge (short burst, costs stamina); handled as `DodgeCommand` in `Sim/Actor/Movement/MovementCommands.cs:67`. |
| `World/Point` | mouse position | Aim; the mine target re-aims as the cursor crosses tiles. |
| `World/Click` | **left mouse** | Does whatever the hand holds: fire the equipped weapon, mine the tile under the cursor, place the held machine. **An empty hand does nothing** — walk-here was removed by the correction pass (R-CP-3), and a press that begins over UI never reaches the world (`UiShell.PointerOverUi`, R-CP-5). |
| `World/Place` | **right mouse** | Clears the hand (drops the ghost). Picking a **placed** machine back up is C-13, accepted 2026-09-14 (coordinator review): `CommandResultEventTests` 4/4 and `HudViewModelTests` 22/22 in EditMode; `WorldInputFeedbackTests` 3/3 in PlayMode (ghost preview, right-click pick-up, `R` rotate). |
| `World/Equip` | **E** | Equip / use: equips the selected weapon, and on the Home core opens the Home drawer with the core card first. |
| `Global/Reload` | **R** | Contextual, resolved in one place (R-CP-6): rotate the held ghost, else rotate the machine under the cursor, else reload the equipped weapon — **including while the Backpack is open** (defect U-4). Rotating a machine already on the ground is C-13, accepted 2026-09-14 (coordinator review): `CommandResultEventTests` 4/4 and `HudViewModelTests` 22/22 in EditMode; `WorldInputFeedbackTests` 3/3 in PlayMode (ghost preview, right-click pick-up, `R` rotate). |
| `Global/Slot` | **1–9** and **0** | Select action-bar slot 1–10. One tool occupies exactly one slot; assigning it elsewhere **swaps**. |
| `Global/TogglePanel` | **Tab** | Open / close the **Backpack** (`UiShell.tabPanel = inventory-panel`, R-CP-1). The HUD's *Inventory [Tab]* button does the same. |
| `Global/ToggleBuild` | **B** | Open / close the **Build menu** (`UiShell.buildPanel = build-panel`, R-CP-2; added by the correction pass). The HUD's *Build [B]* button does the same. |
| `Global/Pause` | **P** | Pause. The game runs at a fixed 1× with pause; there is no fast-forward (U-D-04). |
| `Global/Cancel` | **Escape** | Cancels down a fixed ladder: 100 cancel drag → 200 close slot menu → 300 close nav menu (and confirm dialogs) → 400 close modal child → 500 unpause → 600 close the open drawer → 700 empty the hand / cancel the world selection → 800 pause (600–800 added by the correction pass, R-CP-7). |
| `UI/Navigate`, `UI/Submit` | arrows, **Enter** | Menu navigation. |

Settings lists the ten live controls (`BindingMap.Controls`: Backpack — Tab, Build menu — B, Rotate ghost / Reload — R, Cancel — Esc, Pause — P, Move — WASD / arrows, Sprint — Shift, Dodge — Space, Equip — E, Action bar — 1-9, 0) and the HUD buttons read the stored rebinding (`BindingMap.KeyTextFor`). There are no `slower` / `faster` bindings at all (U-D-04). Bindings are `PlayerPrefs`, stored outside the save; stored rebindings that predate `build` / `rotate` survive the correction pass.

---

## 4. The expected sequence

The objective chain as C-09 implements it (wave3-WA report). Each step advances from actual sim state, and a step
that is already satisfied when you arrive is recognised rather than repeated — a progressed save skips completed
steps and never re-awards an encounter.

1. **Generator** — build the generator.
2. **Excavator** — build the excavator on a deposit.
3. **Chest** — give the excavator somewhere to put its output.
4. **Belts** — connect excavator to chest.
5. **Fuel** — get coal or refined fuel into the generator (by hand or by delivery).
6. **Poles** — put power poles down.
7. **Running line** — the line actually runs.
8. **Substation** — reach the Founders Court substation at `(78, 347)`.
9. **Rifle** — craft the Rifle. There is **no starting rifle**; the run opens on a starting stake of 20 Steel and
   5 Copper.
10. **Equip** — equip it (an already-equipped weapon is recognised, not demanded again).
11. **Hand bullets** — hand-craft ammunition. One inventory unit is one bullet, and no player-facing string uses the
    word "magazine" (asserted by a test).
12. **Turret** — place the gun turret.
13. **Power** — get it on the grid. An unpowered or unfed turret holds fire.
14. **Load** — load the hopper.
15. **Warning** — a short directional warning, **25 s** (provisional, U-P-04), naming the approach.
16. **Attack** — a small introductory attack from one approach: readable and forgiving; you can watch the turret work
    and are not expected to fight alone.
17. **Repelled** — the outcome is decided by the **prepared turret**, not by the core; you get feedback on the
    ammunition actually consumed, and automatic replenishment is taught through a **real** delivery from real stock
    (the objective watches `Stats.TurretFed`).
18. **Three turrets** — guidance to three turrets covering different approaches. Guidance, not a guarantee of safety.
19. **Excursion** — hand-off to a useful nearby excursion while production continues: leave Home carrying at least
    eight bullets, reach the nearest first-region camp, return, and find production still running.

**If a raid is already running when step 15 would fire**, the introduction is **deferred**, not skipped: it is
scheduled once, refreshed from `OpeningRules.SafeAt`, and resumes when the area is safe (U-D-26). It is bounded —
if you reach three loaded turrets, or leave for the first camp, the encounter is **dropped without reward** and
says so. Ordinary raids keep running on their own cadence meanwhile.

**To see an ordinary raid without waiting**, use the editor-only menu **Relight → Debug → Trigger raid (editor only)**
(or *Trigger small raid — skitters only*, and *Clear staged raid*). It is excluded from player builds and, per C-ACC,
a debug-triggered raid is recorded as **not proving natural pacing**.

---

## 5. Ported sources and deliberate differences

Every row is a difference somebody chose, with the reason. Reference paths are relative to the repository root;
Unity paths are relative to `Unity/Relight/Assets/Relight/`.

| Reference (TypeScript) | Unity | Deliberate difference, and why |
|---|---|---|
| `packages/sim/src/city/riverfront.ts`, `ground.ts:527,561,628-630`, `parcelGeometry.ts:8`, `phaserCityModel.ts:20`, `firstRegion.ts:31`, `tiles.ts:30` | `packages/tools/src/exportUnity.ts` → `Unity/Import/home/city.json`; `Editor/World/{CityFile,HomeRegionImporter,ImportedRegionPainter}.cs`; `Sim/World/ImportedGeometry.cs` | `city.json` is shaped for `JsonUtility`: no nulls, no nested arrays, `isFixed` rather than `fixed`. A fifth side-car `terrain.solid.bin` is exported **and** re-derived on import and compared (0 mismatches of 28,626). `RegionGeometry` wraps `ArrayGeometry` rather than replacing it. No building prefabs are imported (that is D-02b). Nothing was added to `SimState`. (wave1-WA report) |
| `campaignPower.ts:16-19,29,30-34,41-44,46-47,57,61`; `flow.ts:1045-1057` | `Sim/Power/**` | **No outage at start** (U-D-12): the reference's legacy brownout step is not ported, and exactly one power-alert producer exists (defect U-3). Circuit identity is a `NetworkKey` struct, not a string (S-25). There is no block or district layer. Node kinds are data-driven (`ReachTiles > 0` is a reach node, `PowerKw < 0` a source) instead of a hard-coded kind list. `PowerPhase` runs build → report → burn. (wave1-WB report) |
| `flow.ts:929-978` (`tickExcavator`, `findRubble`, `asmCanStart/Start`, `tickAssembler`), `recipeOf:200` and neighbours | `Sim/Production/Machines/**` | Per-machine progress lives in **side tables keyed by machine id** (`production.work[]`, `power.burn[]`), never as new `Machine` fields, so v1/v2 saves upgrade honestly under schema v3. A processor's output goes to `Machine.Inv[outputItem]`; **`Machine.Out` is retired** — the field stays, unread and always 0. The excavator has no `hold`. The Mixer gains a station and a default recipe. `Yield` is the full count (U-D-08). The Arsenal gate is declared in data but not applied in Phase C. (wave1-WB, wave2-WA reports) |
| `flow.ts:755-767,769-810,845-875,880-925,1063-1100,1275-1460`; all of `routing.ts`; all of `directConveyor.ts` | `Sim/Production/Flow/**` | **Flow runs after machines**, so an item a processor finishes on tick N can be taken on tick N. `running()` is always true — the Held/Dark block economy is retired (U-D-32) — and only the power half survives, as a `Throttle <= 0` gate on inserters. **Conveyors draw no power** (U-D-13). `beltDeliver`, the Depot's abstract delivery and tram cargo are not ported. One round is one bullet, so the reference's loose-remainder branches collapse. Belt-over-belt replacement is not ported. `PlaceUndergroundPairCommand` is new. Turret feeding is kind-agnostic through `TurretHopper`. **A real port bug was found and fixed here:** an unpowered inserter still grabbed one item. (wave2-WA report) |
| `flow.ts:1019,1333-1340,1367-1375,1439-1457`; `worldScene.ts:215,704,711,728,743,810`; `equipment.ts` | `Sim/Actor/Mining/**`, `Sim/Building/Placement.cs`, `Sim/Actor/Engineer.Equipment.cs`, `Presentation/WorldInput.cs` | **Right-click empties the hand**, so the reference's B-13 chest binding is retired. A weapon can never be put in a chest (`ItemCounts` is enum-only) — the C-02 "transfer a rifle to a chest and back" criterion is therefore untestable and was deferred beyond Phase C. Mining progress is engineer state (`Engineer.VisitEquipment`), not a map field. **Removal refunds the machine, not its price** (coordinator ruling). There is no starting rifle; the run opens on a `StartingStake` of 20 Steel and 5 Copper. (wave1-WC report) |
| `gameplayCombat.ts firePlayerWeapon`; `weaponProfiles.ts:13`; `playerBallistics.ts:9-14`; `goal.ts:126` | `Sim/Combat/Weapons/**`, `Presentation/Combat/TracerPresenter.cs` | Damage falls off linearly from `EffectiveTiles` to zero at `MaxTiles`, ported verbatim. The bolt sweeps in 0.2-tile sub-steps with at most 24 tracers. `Ballistics.Fire` originates at the **engineer centre**, not at a muzzle transform, so the sim stays engine-free. One item = one bullet (U-D-08). (wave1-WC report) |
| `gameplayCombat.ts tickTurrets`; `campaignThreat.ts` (`campaignCrawlerAction`, `walkField`, `field`, `open`, `initActiveRaidClock`, `tickActiveRaids`, `future`, `raidChoice`, `birth`, `stagingTile`, `approaches`) | `Sim/Combat/Turrets/**`, `Sim/Combat/Enemies/**`, `Sim/Combat/Director/**` | A **BFS distance field** replaces the reference's per-body A*, and it stores a 141×141 box rather than a whole-map array (79 KB against 2 MB) with identical answers. Multi-base machinery, radio intelligence and target nomination are retired (one Home core). **Defer vs skip:** ordinary majors keep the reference's `future()` skip because U-D-38 forbids accumulated attack debt; `Director.Defer` exists for the C-09 opening encounter only (U-D-26, U-Q-21). Danger seconds are charged by `SecondPhase`, never by `EnemyPhase`, so they cannot be double-counted. `MachineOperatingState.Disabled` is new. Combat tick order is Weapons → Turrets → Enemies → Director with a 0.1-tile aim lead. The interface is **`IRaidDirector`**, not the brief's `IThreatLayer` — that name was already taken by the danger-seconds layer. (wave2-WB report) |
| `campaignDefence.ts` (`DEFENCE:13`, `defenceMax:75`, `damageCore`, `repairCheck`, `repairCost`, `startRepair`, `tickRepair`); `inventoryPanel.ts coreCard` | `Sim/Campaign/HomeCore/**`, `Data/Tuning/DefenceTuningAsset.cs` | One core, not `bases[]` (U-D-19, U-D-32). No block commissioning notice, no DISCOVERY repair multiplier. **`CancelRepairCommand` is new** because the port hand-locks the engineer for the repair; it refunds exactly what was charged (U-D-05). The abort notice travels as `CoreRepairAbortedEvent` so the sim stays engine-independent. With no imported core site the core falls back to a 3×3 at the spawn tile. A core knocked out mid-patch voids the work and refunds, because the recommission price differs. Three refusal texts are the brief's wording, not the reference's. (wave2-WA report) |
| `inventoryPanel.ts`, `uiDrag.ts`, `buildPanel.ts`, `controls.ts`, `settingsPanel.ts` | `Sim/UI/**` (13 engine-free models), `UI/Inventory/**`, `UI/Workshop/**`, `UI/FrontEnd/**`, `UI/Settings/**`, `UI/PauseMenu/**` | Seventeen recorded differences, the load-bearing ones being: **no equipment block in the workshop** (U-D-11) — the equipment strip stays under the Backpack grid; no `slower`/`faster` bindings (U-D-04); nine rebindable actions with `reload` deliberately absent; key capture instead of `PerformInteractiveRebinding`; `PlayerPrefs`, not `localStorage`; **autosave UI defaults 10 minutes / 5 kept against the engine's 5 / 3**; audio buses Master/UI/World/Alerts at 80/100/100/100 %; "follow system" motion means the port holds no opinion; `AutosaveEntry` carries no `T`; Delete stays enabled on an unloadable row; autosaves count as save points; `ConfirmDialog` sits at `EscapeOrder.CloseNavMenu` (300); the title screen reads Escape from `Keyboard.current`; Continue is never hidden. A drag carries a pick-up snapshot, so a stack that changed underneath the player is **refused**, never re-targeted. (wave2-WC report) |
| `openingEncounter.ts`; `campaignThreat.ts:204`; `goal.ts:68,80-167,151`; `hud.ts:15-17,32,39-40,47,52-54,69`; `worldScene.ts:460-461,923` | `Sim/Campaign/Opening/**`, `UI/Guide/**` | Deferred, never skipped, with a cap that drops the encounter **without reward** at three loaded turrets or on leaving for the first camp. Repelled or lost is decided by the prepared turret, not the core. `OpeningQueries.Objective` takes `(SimContext, SimState)`. Resupply watches `Stats.TurretFed`. `Director.Reserve` expresses `openingBlocksRaids`. No player-facing string contains the word "magazine" (asserted by test). A step skipped for geometry says so. Material rows append "· N at Home". No "(K)" suffix on "Return to engineer". Key names come from live bindings, else documented defaults. The place name comes from the nearest `SiteKind.Label`. The camera peek toggles `CameraRig.enabled`, so the camera still never reads a UI inset (D-UI-10). (wave3-WA report) |
| `flow.ts lightCovers`, `light.ts stampLight`, `flow.ts:620 subPowered`, `constants.ts:58` | `Sim/Campaign/Light/**`, `Presentation/Light/**` | Ported line for line including the `1e-9` slack and the Floodlight's `d² ≤ 2` origin glow, but with **no per-block loop**. The Home lot is always lit, derived from `HomeQueries.CoreRect` plus `GameData.World.MarginTiles` (4). The mask is cached and re-stamped only when the lit picture would differ (a `SimState.Rev` or fold change), and `LightState` saves nothing. Street-light demand is added unconditionally. The darkness overlay is a **code-built texture on `Sprites/Default`**, not a URP 2D light. The mouse flashlight is visibility only (D-UI-11). (wave3-WB report) |
| `hud.ts`; UI_AND_ONBOARDING.md §4 | `Sim/UI/Hud/**` (engine-free, so the sim tests can reach it), `UI/Hud/**` | The status strip is content-width (defect U-5) — it was still stretching when Wave 4 verification started and was fixed there. Lamps never appear in HUD problem rows. Only `Announced` and `MinorRaid` map to the warning cue. "Has ever placed a generator" is **derived** through `PowerAlertSource.EverHadPower` (`PowerGrid.IsSource` / `Stats.GenFed` / `Stats.CoalBurned`) rather than stored, so an upgraded save answers it correctly. (wave3-WB report) |
| — (new in the port) | `Presentation/Audio/{AudioCue,AudioCueRouter}.cs` | A 20-row `SimEvent` → cue-key → mixer-group table exists, but **no AudioMixer asset and no clips exist**: the router runs with `mixer` null. When the mixer is authored (D-11) its exposed parameters must be exactly `MasterVol`, `UiVol`, `WorldVol`, `AlertsVol`. (wave2-WC, wave3-WB reports) |

### Differences the owner may feel while playing

These are behaviours, not defects; each one matches the reference or a recorded decision.

- **The rifle does not reload itself.** When the magazine empties the shot simply stops until you press **R**. That is
  the reference's behaviour (`packages/sim/src/actor/equipment.ts:73, 106-108`), ported as-is; there is no auto-reload
  to restore.
- **Right-click empties the hand** rather than opening the nearest chest (the reference's B-13 binding is retired), and
  a weapon can never be put into a chest at all.
- **A Home Depot is placed for you at New Game** by `HomeCoreInitializer` (`Sim/Campaign/HomeCore/HomeDepot.cs`), so a
  new run starts with somewhere to craft. A save made before that change has no Depot, and still hand-crafts at Home
  because `HandCraft.NearDepot` is also true anywhere within reach of the authored Home rect.
- **Solid city tiles refuse placement** with "a city structure is there" — the imported Founders Court geometry is
  real, so 4,266 of the window's tiles cannot be built on.
- **The game runs at a fixed 1× with pause**; there is no fast-forward and no `slower` / `faster` binding (U-D-04).
- **Removing a machine refunds the machine, not its price** (coordinator ruling), and there is **no starting rifle** —
  the run opens on 20 Steel and 5 Copper.

---

## 6. Checks performed

All figures are from the coordinator's 2026-09-14 verification on **Unity 6000.6.0f1**, with the owner's editor open,
driven through the **Unity CLI 1.0.0-beta.8**.

| Check | Result |
|---|---|
| EditMode suite, `Relight.Sim.Tests` | **543 / 543 passed** in 6.09 s — the final run, including the new `OpeningSupplyTests` (2), `HomeDepotTests` (4) and `PlacementSolidTests` (1) |
| Unity-free `dotnet test` sim loop (same sources, net8.0 / NUnit 3.14) | **527 / 527 passed** on the pre-fix sources; that loop was not re-run after the Wave 4 fixes |
| PlayMode full suite — first run | 41 total / 28 passed / **9 failed** / 4 skipped (3 skips are the R2/R7 prototype scene, absent by design; 1 is a long timing form) |
| PlayMode full suite — after the production fixes below | 41 total / 32 passed / **5 failed** |
| PlayMode full suite — after the integrated run's fixes | 44 run / 36 passed / **3 failed** — three Phase B scene tests that assumed the synthetic map |
| PlayMode full suite — final rerun | 44 run / 40 passed / 0 failed / 4 skipped (07:00 UTC 2026-09-14; the skips are the 50 s timing form and three R7-Tilemap tests, by design; the `DiskRoundTrip` Phase1/2 and `EvidenceCapture` forms are `[Explicit]` and were not run) |
| Integrated Play Mode run and screenshots | a fresh game on the imported region: the chain built, the Rifle crafted and equipped, a turret placed and loaded, the opening encounter scheduled, `slot-phasec-int` saved and loaded back over the active attack with no duplicate wave, the MainMenu -> New Game flow verified, and the three-turret step and automatic resupply verified after the `Machine.Out` fix, with two debug raids. Captures `evidence/phase-c/integrated-run/raid2-1600x900.png` and `raid2-960x540.png`, plus six `fix-*.png` interface captures at 1600x900 and 960x540; the saves `slot-phasec-int.json` and `slot-phasec-int-end.json` are copied to `evidence/phase-c/integrated-run/` (originals in `C:/Users/Admin/AppData/LocalLow/DefaultCompany/Relight/saves/exploration-v2/`) |
| Street-light retest after the `Tuning - Power.asset` change | not retested at runtime in Phase C (the integrated run never reached night); owed at C-ACC |
| C-13 in-world building feedback (Wave 4 W-D) | accepted 2026-09-14 (coordinator review): `CommandResultEventTests` 4/4 and `HudViewModelTests` 22/22 in EditMode; `WorldInputFeedbackTests` 3/3 in PlayMode (ghost preview, right-click pick-up, `R` rotate) |
| Owner acceptance (C-ACC) | **Not run.** Still owed — see §9 |
| Phase B acceptance (B-ACC) | **Not passed.** The owner deferred it on 2026-09-14 as a prerequisite for starting Phase C; it remains `human` |

**Four production fixes were made during verification**, in shipped code, not in tests:

1. `UI/Hud/Hud.uss` — `#status-strip` was stretching; it is now content-width (defect U-5).
2. `UI/Guide/GoalCard.uss` — a 96 px top offset, because the goal card overlapped the status panel.
3. `Presentation/CameraRig.cs` — re-snaps to the engineer when `SimHost.Session` changes.
4. `UI/StatusPanelController.cs` — walk-here was made a relative six-tile step (`walkStepTiles`) here, and then
   **removed altogether by the correction pass** (R-CP-3) together with its button and scene values.

**A further round of production fixes came out of the integrated run**, again in shipped code:

5. `Sim/Production/**` — **automatic resupply never fired** because `Machine.Out` is retired and was never written, so
   the objective's delivery watch saw nothing. The produced count now lives in `OpeningRules.OutputBuffer` /
   `ProducedThisTick` with `OpeningPhase.NoteProduction`; no save-schema change, covered by `OpeningSupplyTests` (2).
6. `UI/Workshop/WorkshopPanelController.cs` — force-paints on the first shown frame, so the Home drawer can no longer
   show a stale core card (defect U-6).
7. `UI/UiShell.cs` and the `GameUI.unity` scene — the status panel is a right-side drawer at a 96 px top offset and is
   **closed at start**; `UI/StatusPanelController.cs` now scopes its queries to `#status-panel`, because the HUD has a
   "clock" element of its own and the unscoped query found the wrong one.
8. `UI/Hud/Hud.uss` — `.notice-row` and `.alert-strip` are `max-width: 100%`, and `#objective-text` sits on a
   translucent plate so it stays readable over the world.

The Play scene tests were also made map-agnostic (`SceneFixture.FreeTile` / `Last`) now that the real Founders Court
geometry is under them, and the Depot draws through the `PrefabRegistry` fallback (a Chest placeholder) because it has
no art yet.

**A verification limitation worth knowing.** PlayMode `run_tests --filter …` returns 0 tests through the Unity CLI, so
only the unfiltered PlayMode run (~25 s) is usable; EditMode filters work but need the full fixture name. Every
PlayMode figure above is therefore a whole-suite run.

Several Phase B Play tests were adapted where Phase C changed the surface: `AutosaveSessionTests.NearlyDue`, the
`WorldSceneTests` walk-here check, the `SessionChangeTests` / `WorldSceneTests` placements, and the U6 card greying
its button. One earlier PlayMode rerun was aborted by the editor ("Playmode tests were aborted because the player was
stopped") and restarted.

**Defect coverage (UI_AND_ONBOARDING.md §9).** `Tests/Sim/Regression/OpeningDefectsTests.cs` and
`OpeningSaveStatesTests.cs` cover rows **U-1, U-2 (×3), U-3 (×2), U-4 (×2), U-6 (×3), U-7 (×2), U-8 (×2), U-9 and
U-10**, plus a 3,000-tick scripted-opening conservation sweep and the D-UI-10 camera check. **U-5 has no sim half at
all** — the strip's width is a fact about the rendered panel — so it is evidenced only by
`Tests/Play/OpeningUiPlayTests.cs`: 44 run / 40 passed / 0 failed / 4 skipped (07:00 UTC 2026-09-14; the skips are the 50 s timing form and three R7-Tilemap tests, by design; the `DiskRoundTrip` Phase1/2 and `EvidenceCapture` forms are `[Explicit]` and were not run). An `Assert.Ignore("needs seam: …")` is not a pass. U-11
(the ~29-tile Heart wiring break) and U-12 (Furnace / Crown payoff) are unresolved **design** questions, not defects,
and are out of this set. The three historical browser retests for U-2, U-5 and U-6 are recorded as **not performed**
and superseded by named Unity checks (Q20); they are never recorded as passed. The record is
[evidence/phase-c/wave3-WC-superseded.md](evidence/phase-c/wave3-WC-superseded.md).

---

## 7. Remaining defects and limitations

1. **No AudioMixer asset.** Audio cues route through `Presentation/Audio/AudioCueRouter.cs` with `mixer` null, and
   the Settings volume sliders therefore drive nothing. No clips exist either. D-11 owns this.
2. **Every new UI string is provisional (U-P-04)** — in particular the C-09 deferral strings (`DeferReason`,
   `CapNotice`, `ReserveReason`) and the objective row title "Enemy group delayed". They are placeholders, and a
   later rewrite is expected, not a defect.
3. **`LightQueries.LitAt(SimState, …)` is one tick stale.** The pure overload sits in the Combat slot and reads the
   mask stamped on the previous tick; the `SimContext` overload builds first and is for presentation only.
4. **Seven street lights, not eighteen.** Only the emitters inside the Home import window are imported.
   `Data/Generated/Tuning/Tuning - Power.asset` records `streetLightKw` 2 and `streetLightRadiusTiles` 7; the
   documentation was corrected on 2026-09-14 (WORLD_AND_ASSETS.md §2.4, §2.8, §4.5, §6.2) and the change is recorded
   as **U-P-10** in DECISIONS.md §3, because the generated tuning assets carry no change-note field. Retest: not retested at runtime in Phase C (the integrated run never reached night); owed at C-ACC.
5. **The lighting overlay's sorting order (500) is a guess** and was never measured against the other renderers.
6. **The generator data assets showed a false "conflict".** All 26 machine assets reported as "hand-edited AND
   changed in the reference export" after `MachineDefinition` gained five serialized fields in Wave 1; they were
   regenerated with the discard-hand-edits menu, after which `git diff` showed only six `source:` strings and the
   fingerprints changed. If it happens again after a schema addition, it is the same false positive.
7. **Walk-here is gone** (correction pass, R-CP-3): the `MoveCommand` path in `WorldInput`, the status drawer's
   button and its scene values were removed; WASD/arrows are the only way to move.
8. **U-5 is evidenced only in Play Mode** — see §6.
9. **Nothing is committed.** Under the coordinator's Phase C ruling no git commits were made during the phase, so the
   whole tree is uncommitted: every Phase C source, asset, scene and document change, plus
   `Unity/Relight/Packages/manifest.json` and `packages-lock.json`, the untracked `.claude/skills/`, the untracked
   `Unity/Relight/Assets/Relight/Sim/UI/Hud/`, and a git-ignored leftover `Unity/Relight/Assets/InitTestScene….unity`
   written by the test runner (safe to delete). Repository ownership stays the owner's (F8).
10. **The whole 864×576 city is now imported and drawn (correction pass, U-M-40)**, but only the Home opening has
    content: no tram vehicle, truck, blueprints, second plant, further weapons or enemy types, and no ending. The
    freight gates and the arena are imported as data that nothing reads. Every surface is a flat procedural tint —
    the riverfront SVGs cannot be imported on this machine (no `com.unity.vectorgraphics`, no rasteriser) and
    `World/CityArtSet.cs` is the hook for real art.
11. **`CommandResult.Problem` is not empty on success.** It doubles as the confirmation note ("Stack moved.",
    "Reloading · 2 seconds…"), so any new code must read acceptance from `Accepted` alone. An earlier shared note to
    the contrary was retracted.
12. **No player build has been made or run.** All verification was in the editor.
13. **A turret placed inside the workshop walls cannot see out and never fires.** The introductory attack then walks
    past it and disables the core. This was seen in the integrated run and is the one defect most likely to spoil a
    first play. Fix candidates, neither chosen: an objective hint that says "outside the walls", or letting turret
    sight pass through Home walls.
14. **Skitters down a close-range engineer in about 10 seconds.** Standing in the attack to watch it is not survivable
    for long; the opening expects you to let the turret work.
15. **The objective chain can re-surface an earlier step's problem text.** After a repair, "Start your extraction line
    · output full" can reappear when the chest output is full or the generator is empty. The step itself is correct;
    the problem line is stale.
16. **The debug raid trigger is constrained.** It needs `st.Director.DebugAllowed` and refuses an origin within 28
    tiles of the engineer. Debug material grants were also used during the integrated run (120 Steel / 60 Copper /
    10 Coal, then 100 Steel / 40 Copper, then +30 Copper) — editor-only, never player-facing, and they mean the run
    proves the mechanics rather than the economy's pacing.
17. **There is no per-machine produced counter.** Production is visible through inventories and the objective's
    delivery watch, not through a per-machine total.
18. **C-13 has two known gaps.** Picking a placed machine back up produces no success toast, and a burst of refusal
    notices can push the power alert out of the three notice rows for a few seconds.
19. **Correction-pass limitations (2026-09-14, TASKS.md CP-01–07, DECISIONS U-M-39/U-M-40).** The alerts block
    (top-right) sits under an open drawer; the Storage side of the Backpack pair shows only with a machine open; item
    and machine icons are placeholder two-letter squares (`UI/Icons/ItemIconLibrary.cs`); the ghost preview is not
    gated on the pointer being over UI (only the press is); `Tooltips.OnDetach` leaves its registry entry (bounded);
    region labels use the legacy `TextMesh`; `Editor/Scene/SceneSetup.cs`'s *Paint Synthetic Map* menu still paints
    tiles into the scene (run *Relight/World/Clear painted cells* afterwards); `PointerOverUi` falls back to
    `worldBound` for elements that decline picking; the PlayMode controls tests reach Tab/B through `UiShell.Toggle`
    because Input System state events queued from the CLI never reach the actions while the Game view is unfocused;
    the drag tests use the `DragFrom`/`DragTo` seam; no frame-time profile of the full city exists; the repair
    plan's multi-resolution / UI-scale captures (C-R02) were not made — only 1920×1080 and 1280×720; still no player
    build. Saves from Phase C load on the full map by relocation (+43/+91) and are refused only when something would
    land off-map or inside a building.

---

## 8. Where to change things

### Balance and content

| What | Where |
|---|---|
| Tuning values | `Assets/Relight/Data/Generated/Tuning/*.asset` — `Tuning - Defence`, `Tuning - Engineer`, `Tuning - Opening`, `Tuning - Power`, `Tuning - Raids`, `Tuning - Starting stake`, `Tuning - Time`, `Tuning - World` |
| Machines, turrets, belts (26 assets) | `Assets/Relight/Data/Generated/Machines/Machine - *.asset` — cost, power, rate, recipe station, default recipe, speed multiplier, buffer sizes, light radius, cone range |
| Which assets the game loads | `Assets/Relight/Data/GameDataRegistry.asset` |
| Regenerate from the reference catalogue, keeping hand edits | menu **Relight → Setup → Generate Data Assets** |
| Regenerate and **throw away** hand edits | menu **Relight → Setup → Regenerate Data Assets (discard hand edits)** — use this when a schema change makes every asset report a conflict |
| Check the data set is coherent | menu **Relight → Validate Game Data** |
| Export the tables back out | menu **Relight → Export Data Tables** |
| Re-import the Home region | menu **Relight → World → Import Home Region** (or **Re-import Home Region**), then **Relight → World → Paint Imported Region** |
| Hand-authored site corrections that survive a re-import | `Assets/Relight/World/Manual/HomeSitesOverrides.asset` |
| Generated world data (do not hand-edit) | `Assets/Relight/World/Generated/Home/HomeGeometry.asset`, `HomeSites.asset` |
| The source of all of it | `packages/sim/src/city/riverfront.ts` in the reference project, exported by `packages/tools/src/exportUnity.ts` (`npm run map:export-unity`) |

A focused, documented change to a provisional value is allowed under U-M-12 / U-D-28: record the reason, the
before and after values, the build and the retest in `DECISIONS.md` §3 and in the asset's change note. Values are
never changed silently and never changed to make a check pass.

### Interface

| What | Where |
|---|---|
| Layout and style | `Assets/Relight/UI/**/*.uxml` and `*.uss`; shared tokens in `UI/Styles/tokens.uss` |
| The composite shell | `UI/GameUI.uxml` (templates the status panel, inventory panel, HUD, goal card and opening hint) |
| HUD | `UI/Hud/Hud.uxml`, `Hud.uss`, `HudController.cs`; the model is `Sim/UI/Hud/HudViewModel.cs` with `HudNotices.cs` and `PowerAlertSource.cs` |
| Goal card and opening hint | `UI/Guide/GoalCard.uxml`, `GoalCard.uss`, `GoalCardViewModel.cs`, `GoalCardController.cs`; `UI/Guide/OpeningHint.uxml`, `OpeningHint.uss`, `OpeningHintController.cs` |
| Backpack, storage, workshop | `UI/Inventory/**` (`InventoryPanelController`), `UI/Workshop/**` (`WorkshopPanelController`) |
| Status panel | `UI/StatusPanelController.cs`, `UI/StatusPanelViewModel.cs` |
| Title screen, settings, pause | `UI/FrontEnd/**` (`TitleScreenController`, `FrontEndBootstrap`), `UI/Settings/**` (`SettingsController`), `UI/PauseMenu/**` |
| Key routing and the Escape ladder | `UI/InputRouter.cs`, `UI/UiShell.cs` (`EscapeOrder`) |
| Bindings | `Assets/Relight/Input/RelightControls.inputactions` |
| Wording | the engine-free text models in `Sim/UI/**` — `TransferText`, `WorkshopText`, `FrontEndText`, `Confirmation`, `BindingProblem` — so the strings stay unit-testable |

Panels are UI Toolkit documents authored as text; they must open in UI Builder (U-M-21). Every quantity a panel
shows comes from a sim query and every change a panel makes is a sim command — the renderer never invents state.

---

## 9. Owner acceptance checklist (C-ACC)

Phase C is complete only when a person has played this and recorded it. Nothing below has been done.

- [ ] Start a **New Game** and play through to the first fully loaded, powered, running turret.
- [ ] See the **short directional warning** (25 s provisional) name an approach.
- [ ] See the **introductory attack** arrive from that one approach, and watch the turret deal with it.
- [ ] See **feedback on the ammunition actually consumed**.
- [ ] See **automatic replenishment** taught through a real inserter/conveyor delivery from real stock.
- [ ] Get to **three turrets covering different approaches**.
- [ ] Take the **short excursion**: leave Home carrying at least eight bullets, reach the nearest first-region camp,
      return, and confirm production is still running.
- [ ] See **one representative ordinary attack** at the chosen turret rate — either waited for, or produced with
      **Relight → Debug → Trigger raid (editor only)**, which is recorded as *not* proving natural pacing.
- [ ] Check the **turret-sight defect** deliberately: place a turret inside the workshop walls and confirm it cannot
      see out (§7.13), then place one outside and confirm it fires. Say which fix you want.
- [ ] Look at **Home at night** and say whether seven street lights read as enough light (U-P-10); the runtime retest
      of the street-light change is still owed.
- [ ] Confirm **no blocking defect**.
- [ ] **Save and resume twice**, at different points in the opening.
- [ ] Record **feel notes**, and any tuning made under U-M-12 / U-D-28 for U-P-01 (hand mining rate), U-P-02 (hand
      bullet batch) and U-P-05 (gun turret cadence) — the three values whose retests are still pending.

No full campaign run is part of this check.

**Still owed alongside it:** **B-ACC**, which the owner deferred on 2026-09-14 rather than passing — project compiles
clean, the B-03/B-04/B-06/B-08/B-11/B-12 checks green, a repeated scripted headless run giving equal hashes, and the
owner opening the project, editing one balance value in the inspector, moving the engineer and opening the panel in
UI Builder.
