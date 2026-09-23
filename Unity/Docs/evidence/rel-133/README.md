# REL-133 — a building shows that it is being repaired

## The owner's note

> "When a building is being repaired, it needs a little animation or something to show it's being repaired apart
> from just text"

The eighth of the owner's ten notes from their 2026-09-22 play.

## What was already true (the premise, checked first)

A repair announced itself in words and nowhere else:

| Where a running repair was visible | Kind |
|---|---|
| `HudController` — the repair row | text |
| `StatusPanelViewModel` | text |
| `WorkshopPanelController` — the machine repair card | text |

**Nothing in `Presentation/` drew anything on the building being repaired.** There was no half-built version of
this to extend, which is why the change is a new visual rather than an edit to an existing one.

## What was built

| Piece | File | What it does |
|---|---|---|
| The rule | `Sim/UI/BuildingCondition.cs` | Which building is being repaired, and how far through it is. |
| The drawing | `Presentation/Building/BuildingConditionVisual.cs` | A progress ring, a welding arc at its leading end and four falling sparks, in the building's own local space. |
| The machine wiring | `Presentation/MachineView.cs` | One gate per view, ahead of the activity gate. |
| The core wiring | `Presentation/MachinePresenter.cs` | `AnimateCore` — the Home core's own object. |

### Why the rule lives in `Sim/UI` and not in `HomeQueries`

`Sim/UI` is where picture-only rules live inside the Sim assembly, beside `DarknessLook`, `LightSweep` and
`BuildingGlow` (REL-127). Being in the Sim assembly is what makes it testable offline; being in `UI` is what says
the simulation must not read it.

It is not folded into `HomeQueries` because the two things there that touch repair do more work than a picture
should pay for every frame: `MachineRepairCard` **prices** the repair and `RepairProblem` walks the engineer's
reach and pockets. These three answers are field reads.

### Why the total is read from tuning and not from `HomeCore.Price`

`Price` re-reads the building's hit points. Used as the denominator it would disagree with the countdown the
instant a repair healed the core past the recommission threshold — the ring would jump. `HomeCommands` sets
`RepairRemaining` from one of two tuning rows and records which in `RepairRecommission`; the rule reads the same
two rows and the same flag, so the ring and the countdown cannot drift.

### Why it is not part of `MachineActivityVisual`

That class's `Supports` gate is: a belt, an arm, a power source, a processor, a miner, a turret, plus anything
wrecked. **A Wall, a chest and a pole are outside it — and they are exactly the things that get repaired.**
Widening the gate would also have stamped the production operating badge on every damaged wall, which says
something about throughput that a wall has no part in. So the repair mark is a second, independent visual with a
gate of its own, drawn above the activity gate in `Animate`. It keeps the existing `isVisible` culling and the
existing per-view pooling for free.

### Why the Home core is drawn from the presenter

The core is **not a `Machine`**. It is a rect on `HomeState` — nothing places it and nothing may pack it — so it
has no `MachineView` to hang a mark on, and nothing in `Presentation/` drew it at all before this. It gets one
lazily-made object of its own in `MachinePresenter`.

It is drawn at all because the core **is** a building to the player: it is the thing they repair most, and the
note says "when a building is being repaired" without carving it out.

Two details are deliberate: the object's position is written **every frame** rather than once, because
`HomeCore.Ensure` can place the core on a later tick and a loaded save brings a core that may stand somewhere
else; and `MachinePresenter.Clear()` destroys only the entries in `_views`, so the core's object survives a
session change, with `AnimateCore` calling `Hide()` on the first frame after a load when no core repair is running.

## Where it sits, and why that matters

**Everything is drawn inside the footprint, centred.** The space above the top edge is left deliberately clear:
REL-134 puts a damaged building's health bar there, and a building being repaired is always a damaged building, so
the two are on screen together nearly every time. They cannot collide by construction rather than by tuning.

The strokes sort at `DrawOrder.MachineCue` (512), above `DrawOrder.Darkness` (500) — a repair is readable in the
dark, which is when repairs happen.

## The numbers (U-P-34)

The note asks for "a little animation or something" and names nothing, so every figure here is the implementer's
under U-D-28.

| Number | Value | Why |
|---|---|---|
| Ring radius | `min(w, h) × 0.30`, clamped to **0.24 – 0.75** tiles | Clamped so the mark means the same thing on a 1×1 Wall and on the 10×14 Home core instead of growing with the building. The lower bound keeps it inside a one-tile footprint. |
| Ring width | 0.085 backing, 0.055 filled | The dark backing is drawn first and slightly wider, so the filled part reads on a pale sprite as well as a dark one without the visual needing to know what it stands on. |
| Spark life / count | **0.55 s**, **4** in the air | Four strokes on one shared life at even phases. No particle system, no allocation. |
| Arc flicker | `0.45 + 0.55 × |sin(31.7t) · sin(11.3t)|` | Two rates that do not divide each other, so it never settles into a visible beat. Deterministic and allocation-free. |

`Ring` spends points on **arc length**, not angle: the 0.045-radius arc dot costs five points instead of the forty
a fixed-resolution circle would have spent on a shape a twentieth the size.

## Still picture only

Nothing in the simulation changed. No new sim state, no new tick work, no save-schema change. `AskingChangesNothingInTheSimulation`
calls all three answers 200 times and asserts `Rev`, `RepairRemaining`, the machine's `Hp`, `st.Turrets.Units.Count`
and `st.Events.Count` are all untouched — the last of those because `TurretState.Find` must not become `Of` by
accident and start creating a damage entry just by being asked.

## Checks

| Suite | Result |
|---|---|
| Offline sim (`dotnet test`) | 968 passed, 1 skipped, 0 failed (was 957 + 11 new) |
| Unity EditMode | 1068 / 1068, 0 failed (was 1057 + 11 new) |
| Unity PlayMode | 109 total: 104 passed, 0 failed, 5 skipped (was 107 / 102 / 0 / 5) |

The five PlayMode skips are the declared environmental ones and are unchanged.

### All three halves were shown to bite

| Mutation | Result |
|---|---|
| `RepairingMachine` drops the `RepairId` check | 10 passed / **1 failed** — `StartingAMachineRepairMarksThatMachineAndOnlyThatMachine`, the one test that stands a second turret beside the first. Restored: 11 / 11. |
| `RepairProgress` always measures against `RepairSeconds` | 10 passed / **1 failed** — `ARecommissionIsMeasuredAgainstTheRecommissionTimeNotTheRepairTime`. Restored: 11 / 11. |
| The drawing removed from **both** `MachineView.Animate` and `MachinePresenter.LateUpdate` | the 2 Play tests went to **0 passed / 2 failed**, with their own messages: *"a repair is running on this chest and the building is not showing it"* and *"the core is being repaired and nothing is showing it"*. Restored: 2 / 2. |

The third mutation is the one that matters. Without it, a rule that answers perfectly and that **nothing draws**
would have passed all eleven sim tests.

New tests:

- `Tests/Sim/UI/BuildingConditionTests.cs` — 11 tests. They run the **real** `RepairCommand` and the real
  `HomeCorePhase` rather than setting `st.Home`'s fields by hand, because a test that wrote `RepairKind` itself
  would still pass if the command stopped setting it. Cancelling, finishing and going down (REL-16) each go through
  their own real path for the same reason.
- `Tests/Play/Scene/RepairMarkPlayTests.cs` — 2 tests in the authored scene. Both count the **enabled strokes**
  under the building, not only the flag, because a flag set by a presenter that draws nothing is the defect they
  exist to catch. The machine tested is a **chest** on purpose: it is outside `MachineActivityVisual.Supports`, so
  it is one of the buildings the existing activity strokes never touch.

## Two things the tests guard that are easy to get wrong

- **The core's sentinel id.** `RepairId` is `-1` while the core is being repaired.
  `ARepairOfTheCoreMarksTheCoreAndNoMachine` asks `RepairingMachine(st, -1)` and requires **false**, because the
  ids this rule is asked about come from views and must never collide with the sentinel.
- **An overlong `RepairRemaining`.** An old save, or one whose tuning has since been shortened, can hand the
  presenter a remaining time longer than the total. `AnOverlongRemainingTimeIsClampedRatherThanRunBackwards`
  requires it to clamp: a ring drawn from a negative fraction is a ring drawn the wrong way round.

## Not done here

- **The inspect card is untouched.** The note's "apart from just text" asks for something *on the building*; the
  card's wording is unchanged.
- **Not seen in play.** No owner playtest of this change, and no assistant screenshot is offered as one. Whether a
  ring-and-arc reads as "being repaired" at a glance, and whether 0.24 tiles is big enough to notice on a Wall, are
  questions only the owner can answer.
