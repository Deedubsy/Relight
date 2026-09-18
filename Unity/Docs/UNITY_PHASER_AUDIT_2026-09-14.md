# Unity versus Phaser audit — 2026-09-14

The current Unity editor build is not a playable equivalent of the Phaser Home opening. Substantial simulation code is present, but essential player controls are disconnected, UI layout is broken, and the world still uses foundation placeholders. The Phase C handoff's claim that the whole opening is playable is not supported by the current scene and input wiring.

This is an inspection report, not an implementation or a task-status change. `TASKS.md` remains the migration status authority.

## Evidence and scope

Inspected the live Unity 6000.6.0f1 editor at `E:\Factorio2\Unity\Relight`, its current uncommitted source/assets, and the Phaser reference source. The editor started on clean `Boot.unity`, outside Play mode. Entered Play, verified the title UI exists, and started a fresh session through the title controller's existing Start handler. The live map reports `riverfront-arc-v4-editor-ac16d9188c05`, region `home`.

Screenshots are real Unity captures at 1920 × 1080. The engine's camera-only screenshot command excludes UI; the full screenshots below use `ScreenCapture.CaptureScreenshot`. Desktop Computer Use failed to initialize, so handler checks were invoked through Unity CLI, not physical keyboard/mouse input. No project gameplay code, scenes, or assets were edited. Autosaving and quit-saving were disabled in the temporary runtime session before stopping it; those changes were not saved to the scene. Play mode was stopped afterwards.

- [Current opening with UI](evidence/phase-c/audit-opening-ui.png)
- [Backpack forced open for inspection](evidence/phase-c/audit-inventory-forced.png)
- [Current world without UI](evidence/phase-c/audit-opening-world.png)
- [Runtime handler results and all 26 prefab mappings](evidence/phase-c/audit-runtime.json)
- [Measured UI bounds](evidence/phase-c/audit-layout.json)
- [Historical Phaser Founders Court screenshot](../../docs/evidence/city-density/founders.png): useful for visual comparison, not a claim of identical camera position, elapsed time, or current HUD wording.

## Findings, in repair priority order

### 1. P1 — UI template containers have zero height

In the live GameUI document, `hud-host`, `hud-root`, `inventory-panel-host`, `opening-hint-host` and `goal-card-host` all measured 1920 × 0. The health/weapon/Backpack HUD (`engineer-block`) measured `(16, -141, 300, 125)`: it is above the visible screen. The opening hint measured `(0, -133, 1920, 69)`, also offscreen. When opened directly for inspection, the Backpack/workshop displays overlapping headings, cards and controls.

`UI/GameUI.uxml:18–22` instances templates without a viewport-sized host layout. `UI/Hud/Hud.uss:14` anchors the HUD to all four sides of that zero-height parent. `UI/Inventory/InventoryPanel.uss:17–20` likewise uses top/bottom anchoring. Fix the containing layout first, then assess the child layouts at ordinary and enlarged UI sizes. Passing the existing status-strip width test does not establish that the HUD is on screen.

### 2. P1 — There is no usable path to select and build machines

The opening asks the player to build a Generator. The new session's ten action-bar slots are all empty. `Presentation/WorldInput.cs` only obtains a machine tool from that bar. The only non-test UI assignment of a non-empty bar slot is in `UI/Inventory/InventoryPanelController.cs:714–716`, which explicitly rejects anything that is not a weapon. There is no Build panel instance or Build action in the current UI/input asset. `OpeningHintController.cs:123–129` nevertheless advertises B to open Build.

Phaser has a real build picker: `packages/game/src/main.ts:182` connects `panel.onPick` to `worldScene.setTool`, and `uiShell.ts:102` wires drawer buttons. Unity's placement simulation and preview can work when a test assigns the tool, but that does not make construction accessible to a player.

### 3. P1 — Home, Backpack and machine storage interactions are disconnected

`Presentation/WorldInput.cs` calls `Equip` on E. That method immediately returns for an empty hand or a machine tool, and otherwise only equips/unequips a weapon. It never queries a Home or machine interaction target. The live handler check with an empty hand left the active panel null.

The live shell's Tab target is `status-panel`, not `inventory-panel`. `InventoryPanelController.OpenStore(int)` exists at line 97, but searching all project C# found its declaration only: no caller pairs the drawer with a machine. The runtime paired-machine id was -1. The panel exists but cannot provide the claimed player route for feeding generators/turrets, collecting production, crafting the rifle, or repairing Home.

Phaser's `worldScene.ts:552–565` resolves interaction targets and dispatches Home/chest/routing/inspection hooks; `main.ts:180` connects those hooks to actual panels. This connection is missing in Unity.

### 4. P1 — Production configuration has simulation commands without controls

`Sim/Production/Machines/SetRecipeCommand.cs` implements recipe selection, but no production UI or presentation caller sends it. Phaser `worldScene.ts:511–520` provides an Assembler recipe action. Unity's `PlaceUndergroundPairCommand` likewise has its declaration and command handler but no player-facing caller. These operations must be wired into the actual construction/inspection flow before automation parity can be claimed.

### 5. P2 — World and machine presentation are still placeholders

`Editor/World/ImportedRegionPainter.cs` paints only Terrain and Solid tilemaps from the foundation palette. Imported building/prop metadata is not the same as rendering the reference artwork. The screenshot shows gridded terrain, flat solid outlines, a square engineer and a large plain rectangle for Home's Depot.

The live registry resolves every one of the 26 machine kinds to `Assets/Relight/Prefabs/Machines/Chest.prefab`. `Presentation/MachineView.cs:35–44` stretches that placeholder to the machine footprint. `TurretPresenter` adds a stand-in barrel, but it does not supply distinct machine artwork. Item assets inspected also have empty icon references.

Phaser's `packages/game/src/riverfrontDraw.ts` loads the floor/roof SVG families, roof variants and directional house roofs, and draws terrain wear, paved areas, roads, paths and street details. Those visual layers have not been reproduced by the Unity painter. This is missing presentation work, not primarily a texture-filtering issue.

## Why the completion evidence did not catch this

The tests prove narrower behaviors than the handoff claims. `Tests/Play/OpeningUiPlayTests.cs:293,314` calls `shell.Open(drawer)` directly; it does not establish that E can open Home. `Tests/Play/Scene/WorldInputFeedbackTests.cs:73` assigns the chest tool with `sim.Apply(new AssignBarCommand(...))`; it bypasses the missing Build picker. The HUD test measures the status strip, with conditional checks for a minimap if one exists; it does not require the lower HUD to be inside the viewport.

The existing Phase C handoff also states that its integrated run granted materials through debug code. Such a run can demonstrate simulation mechanics while bypassing missing player access and normal opening pacing. Its reported suite results were not rerun in this audit, and are not presented as new passes. The live console reported no compilation failure and no new captured error entries during the inspection; an error-free console does not resolve these functional gaps.

## Expected migration gaps

The full city, tram, truck, blueprints, later plants and ending are explicitly deferred to later phases in `PHASE_C_HANDOFF.md`. Audio clips and an AudioMixer are also absent and assigned to D-11. These are incomplete-port scope, distinct from the broken Home opening. Movement, mining, production, power, combat, tutorial state and persistence code do exist; this audit does not claim they are wholly absent or fully correct.

## Recommended next work

1. Repair viewport-sized UI hosts and the Backpack/workshop layout; keep health, ammunition and instructions visible.
2. Wire Build selection, the action bar, Backpack, contextual E interaction and machine storage through the existing simulation commands.
3. Add recipe selection and the missing conveyor construction controls.
4. Give opening machines, engineer, resources and Home surroundings distinct readable visuals, reusing the reference assets and layout data.
5. Verify a fresh New Game through the ordinary controls and starting stake: mine → build → fuel/power → turret → introductory attack → produced ammunition delivery → save/continue. No debug inventory grants or direct command assignments in that acceptance route.

The current evidence supports reopening the affected Phase C integration work before extending the campaign. It does not support marking owner acceptance complete.

## Audit delivery checks

The editor was restored to `Boot.unity`, Play mode off, scene dirty flag false. Both required documentation commands (`npm run docsync:check` and `npm run freshness:check`) were attempted and failed before executing because `tsx` is unavailable in this checkout's shell. Logs: `evidence/phase-c/audit-docsync.log` and `evidence/phase-c/audit-freshness.log`. No dependency installation was performed. No gameplay test suite was rerun for this read-only audit.
