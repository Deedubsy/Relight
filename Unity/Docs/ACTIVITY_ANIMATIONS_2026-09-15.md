# Machine and power activity animations — 2026-09-15

The owner requested small electricity, working-machine, blocked/full, no-power and generator animations. The owner explicitly chose to keep conveyors independent of electricity; their animations distinguish movement and blockage only. TASKS.md remains the only implementation-status tracker.

## What the player sees

| Situation | Visible cue |
|---|---|
| Energized cable | Two restrained warm highlights travel along the existing sagging cable. A dead connection remains a dim static wire. Generator leads stop highlighting when that generator is out of fuel. |
| Working processor or extractor | A small rotor turns with a mint status lamp. Low electrical supply slows motion and uses amber. |
| Output full / blocked machine | Mechanical motion stops; an amber double-bar badge breathes slowly. |
| Unpowered machine | Stopped mechanism and a red broken-lightning badge with a slow brightness cycle. |
| Waiting for input | A separate amber clock badge; no working motion. |
| Generator supplying load | Turning rotor, mint lamp and three faint rising exhaust wisps. |
| Fuelled generator without load | Stationary mechanism and a neutral standby lamp. |
| Generator out of fuel | Stationary mechanism, no exhaust and a red power-off symbol. |
| Conveyor | Small deck slats move along the same path as items, including corner entry. The existing single direction chevron remains. A blockage stops the tread and shows an amber jam marker; clearing it resumes movement. Electrical outages do not stop belts. |
| Inserter / turret | Inserter arm position reads its real swing fraction; powered turrets show readiness while retaining their existing aiming/firing presentation. |

Pause freezes all new motion and status breathing. Changed electrical state can still repaint while paused, which makes admin fuel testing truthful.

## Implementation

Only presentation source was changed. `MachineActivityVisual` owns reusable line details, reads `ProductionQueries`, `PowerQueries` and `FlowQueries`, and is called by `MachineView` through `MachinePresenter`. Passive furniture receives no motor overlay. Detail rendering is skipped for off-screen machine sprites; the views share one material and retain their line renderers instead of creating them every frame.

`PowerConnectionPresenter` reuses cable/highlight renderers. Cable topology rebuilds on session or structural revision changes; per-frame supply repaint uses the current cached power network. Motion does not alter simulation time, inventory, production, connectivity, recipes or save format. Machine activity resets when views are rebuilt for a different session. Existing sprites, item interpolation and conveyor spacing remain intact.

## Verification actually performed

- Unity 6000.6.0f1 / URP compilation passed after correcting a local variable-name collision and a conveyor draw-order issue found in the first visual check.
- **19 live Play Mode assertions passed:** actual highlight displacement, powered generator motion, machine working/full/off states, tread draw order, pause, unchanged canonical gameplay state while paused, supply loss while paused, power-independent conveyors, blocked/restored tread, refuelling, waiting for input and fuelled disconnected standby. [Assertions](evidence/activity-animation/runtime.txt), [reproducible check helper](evidence/activity-animation/runtime-checks.cs).
- The initial no-mutation check compared complete save envelopes, which contain a fresh UTC timestamp. It was corrected to compare canonical gameplay state; that comparison passed without changing production code to accommodate the test.
- Actual Game View captures inspected at 1920×1080 and 1280×720: [running, 1080p](evidence/activity-animation/running-1080.png), [outage, 1080p](evidence/activity-animation/outage-1080.png), [running, 720p](evidence/activity-animation/running-720.png). The scene includes temporary machine placements to expose the relevant states; it is not the player's saved game.
- Player-save writes were disabled during the isolated scenario. Test input preferences and Game View size were restored; Play Mode was stopped with World and GameUI scenes clean. No asset or scene YAML was hand-edited.

**8/8 focused FlowPowerFeedbackTests passed**. [Recorded results](evidence/activity-animation/flow-power-tests.json). Report links, imported metadata and scoped source whitespace checks passed; Git emitted existing nested .gitattributes warnings. `npm run docsync:check` and `npm run freshness:check` were attempted, but both could not start because the local `tsx` executable is missing. No standalone build, native mouse acceptance or large-factory performance benchmark was run. Existing placeholder world/machine art remains; this pass adds movement and state feedback over that art. C-ACC remains human.
