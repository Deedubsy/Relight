# Conveyor, extractor and power feedback corrections — 2026-09-15

Implemented by Codex under the owner's five gameplay correction requests. TASKS.md owns status; this report records the engineering evidence, not human acceptance.

## Player-facing changes

- Hover and machine inspection independently show power state: disconnected, connected without supply, insufficient supply, or powered. Production stalls no longer hide the electricity information. Passive structures say that they do not require power.
- Conveyors use dark beds and one amber travel chevron. Extractors show a chevron at their direct output port; placement previews also show only forward travel.
- Extractors retain one extracted item in their ordinary inventory when output cannot leave. The item appears in inspection, can be collected, survives saves and machine removal, and is counted by the normal stock ledger. A conveyor pointing away from any adjacent edge can pull it; the direct facing port also pushes into an accepting machine/conveyor.
- All 20 item definitions now reference the existing item artwork. The belt renderer previously had no item icons. Sprite dimensions are normalized to at most 0.16 tiles against 0.25-tile centre spacing, leaving visible gaps. Queues now preserve that spacing across belt boundaries too.
- Belt items interpolate previous/current simulation positions each rendered frame. Handoffs between ordinary belts preserve the moving item's previous position; corner positions follow the incoming and outgoing centreline. Hidden underground spans are not drawn above ground.
- Persistent cables connect poles, generators and electrically consuming machines. Live consumer cables follow their nearest covering pole, as the simulation does. Energized cables are amber; cables without supply are muted. Pole placement previews also show nearby consumer candidates using the same reach/footprint predicate.
- Machine extraction no longer produces the misleading “+1 … to Backpack” notice. Hand mining still does.

## Why the extractor appeared broken

The port intentionally omitted the Phaser miner's held output. It checked the single facing tile before mining and reported “output full” when that tile had no accepting receiver, even though its inventory was empty. Belts attached elsewhere could not pull anything. Phaser's `flow.ts` keeps a one-item `hold`; this correction uses the existing Unity machine inventory for that role, avoiding a save-schema change while making the output visible and transferable. Extraction rate remains 0.5/s for the default extractor; the buffer is one item, not unlimited storage.

## Implementation

- Simulation: `Production/Machines/{MachinePhase,ProductionQueries}.cs`, `Production/Flow/{FlowPhase,FlowQueries,FlowRules,FlowState}.cs`, `Power/{PowerQueries,PowerLinks}.cs`, and mining event origin in `Actor/Mining`.
- Presentation: `Flow/BeltItemPresenter.cs`, new `Flow/PowerConnectionPresenter.cs`, `MachineView.cs`, and `Building/PlacementPreviewPresenter.cs`.
- UI: `Hud/GameplayDock.cs`, `Inventory/InventoryPanelController.cs`, and `Sim/UI/Hud/HudViewModel.cs`.
- Assets: existing item definitions' icon references and the World scene's persistent cable component were edited through Unity Editor APIs. `Editor/Scene/SceneSetup.cs` retains the cable component when regenerating the scene.
- No balance rates, generator output, reach tuning, recipes or serialized save fields changed. Interpolation snapshots are transient and excluded from state serialization/hash.

## Verification

Unity compilation passed. Focused suites and raw result files are under [evidence/flow-power](evidence/flow-power/): production/flow **47/47**, power **9/9**, HUD **23/23**. Hand mining **8/8**; **87 focused tests passed** in total. Raw results include `mining-tests.json`.

Actual Play Mode used the authored Home map and a disposable prepared machine layout at normal 1× speed, with autosaves and quit-saving disabled. `runtime-checks.cs` and `runtime.txt` verify extraction into a chest, a four-item-per-tile dead-end queue, four permanent cables, removal of the receiver through a real command, visible buffered output in the interaction panel, ordinary hand collection, pole previews, and conserved stock. The checks passed again after the final visual/notification corrections.

`motion-check.cs` / `motion.txt` observed sprite positions changing between frames with the simulation tick unchanged, proving that interpolation actually runs. The 720p hover capture uses actual mouse position input and actual UI text. Panel opening used the game's world-interaction callback; collection/removal used normal simulation commands. This is not a claim of native OS mouse or full campaign acceptance.

Actual Unity captures:

- [Queued items and persistent cables](evidence/flow-power/queued-items.png)
- [Visible extractor output and power in inspection](evidence/flow-power/extractor-output.png)
- [Pole placement connection candidates](evidence/flow-power/pole-preview.png)
- [Power hover at 1280×720](evidence/flow-power/hover-720.png)

World machine/building sprites remain the existing placeholders; this correction does not replace the world art. Standalone-build and human campaign playtests were not run. Documentation tooling retains the previously observed missing-`tsx` environment blocker. No commit, merge or push was performed.
