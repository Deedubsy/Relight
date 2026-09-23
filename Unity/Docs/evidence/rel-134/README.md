# REL-134 — a damaged building carries a health bar

## The owner's note

> "When a building has lost health, it should show a small health bar above it to show it's been damaged. Also
> when inspecting a building, the health bar should display more prominently"

The ninth of the owner's ten notes from their 2026-09-22 play.

## What was already true (the premise, checked first)

`grep -rn "health-bar\|HpBar\|healthBar\|hp-bar"` across every `.cs`, `.uxml` and `.uss` in the project returned
**nothing**. Hit points were reported in words and only in words:

| Where a building's health was visible | Kind |
|---|---|
| `HomeQueries.MachineRepairLine` → the inspect card's `machine-repair-text` | text ("Damaged · 18 / 40 HP") |
| `HudController` — the core's condition row | text |
| `StatusPanelViewModel` | text, and it is the **engineer's** health, not a building's |

So there was no half-built bar to extend anywhere, and the "more prominently" half of the note is a row that had
**no bar at all** rather than a small one. Both halves are new.

## What was built

| Piece | File | What it does |
|---|---|---|
| The rule | `Sim/UI/BuildingCondition.cs` | `Health(d, st, m)`, `CoreHealth(d, st)` and the `HealthReading` struct: hp, max, the bands. |
| The drawing | `Presentation/Building/BuildingHealthVisual.cs` | A track and a fill, above the footprint, sized from the camera. |
| The machine wiring | `Presentation/MachineView.cs` | One gate per view, ahead of the REL-133 repair gate. |
| The core wiring | `Presentation/MachinePresenter.cs` | `AnimateCore` — the Home core's own object, and `HalfView()`. |
| The card's bar | `UI/Inventory/InventoryPanel.uxml` | `machine-health`: track, fill and figure, first in the repair row. |
| Its styles | `UI/Inventory/InventoryPanel.uss` | Appended at the end of the file, which is what wins that cascade. |
| Its painting | `UI/Inventory/InventoryPanelController.cs` | `PaintHealthBar`, called from `PaintRepairRow`. |

## Why the rule reads `TurretRules` and not `MachineRepairCard`

The acceptance criterion is *"the bar tracks the same `Hp`/`Max` the text shows, so the two can never disagree"*,
and the text comes from `HomeQueries.MachineRepairCard`. Reading that same card would have looked like the safer
way to honour it. The call chain says otherwise — it was followed to the bottom rather than assumed:

```
HomeQueries.MachineRepairCard(ctx, st, id)
  → HomeCore.Price(d, st, RepairKinds.Machine, id)
      → HomeCore.MachineDefenceHp          (Sim/Combat/Turrets/HomeCore.Turrets.cs)
          max = TurretRules.MaxHp(d, m);
          hp  = max > 0 ? TurretRules.Hp(d, st, m) : 0;
```

The card's two figures **are** `TurretRules.MaxHp` and `TurretRules.Hp`, one lookup further away. Everything the
card does on top of them — pricing the repair, walking the engineer's reach and pockets, building a refusal
string — is work a picture drawn every frame should not pay for. So the rule reads the pair directly, and
`AssertAgreesWithTheCard` puts the two side by side in four damage states plus the core and requires them equal
within 1e-9. The claim is tested, not assumed.

For the core, `Price(..., RepairKinds.Core, -1)` gives `hp = h.Placed ? h.Hp : 0` and `max = d.Defence.CoreHp`, and
`CoreHealth` reads exactly those.

## Why the bands live on the rule

`HurtBelow = 0.66` and `CriticalBelow = 0.30` are `public const` on `BuildingCondition`. The bar over the building
is drawn in `Presentation` and the bar in the card is styled in `UI`, in different assemblies; two copies of 0.66
in two places is exactly the kind of pair that drifts and is never noticed, because each one looks right on its
own. `TheBandsChangeAtTheFractionsTheCardAndTheWorldBothRead` walks a building down through both thresholds
using the constants themselves, so a later re-tuning moves the test with the game instead of breaking it.

## Where the bar sits

**Above the top edge of the footprint** — the space REL-133 deliberately left clear, `BuildingConditionVisual`
drawing everything *inside* the rectangle. A building being repaired is always a damaged building, so the repair
ring and the health bar are on screen together nearly every time. They cannot collide because one is inside the
rectangle and the other is outside it, not because two offsets were tuned until they happened to miss;
`TheBarHangsAboveTheFootprintAndClearOfTheRepairMark` measures both sets of strokes, thickness included, and
requires the gap.

The strokes sort at `DrawOrder.MachineCue` (512), above `DrawOrder.Darkness` (500). A raid is the only reason a
building is damaged and a raid happens in the dark.

## Why the thickness follows the camera and the width does not

REL-130 opened the view by half again, so the same world measurement is two thirds as many pixels at the far
notch as at the near one. The acceptance says *"a bar that is unreadable zoomed out is not done"*.

- **Thickness** is `orthographicSize × 0.016`, clamped — the same number of pixels at either end of the wheel.
- **Width** stays in world units and tracks the footprint, clamped to 0.9 – 2.4 tiles. That is what keeps the bar
  visibly attached to the building it belongs to rather than floating at a fixed size over everything.

`TheBarKeepsItsSizeOnScreenWhenTheWheelOpensTheView` drives the real wheel to the far notch and requires the
thickness to have grown by exactly the ratio the camera did.

`MachinePresenter.HalfView()` resolves `Camera.main` lazily and re-resolves when it goes, because a session change
destroys and remakes the world scene around the component. Its fallback is `CameraRig`'s opening framing, so a
scene with no camera at all draws the bar the player opens the game to rather than nothing.

## The card's half

The note says the health bar should display **more prominently** when inspecting — not that the figures should go.
Both are there: the bar is the first child of `machine-repair` and spans the row, with `18 / 40 HP` at its right
end. Mid-repair `MachineRepairLine` carries no HP at all, so the figure on the bar is the only reading in the row
then, which is a second reason to keep it.

It is rounded with `Math.Ceiling` on **both** figures, exactly as `MachineRepairLine` rounds them, so the number on
the bar and the number in the sentence beneath are the same characters rather than the same value rounded twice.

`InventoryPanel.uss` is a flat cascade of successive redefinitions with no media queries, so the new block is
appended at the **end** of the file, after the U-D-51 block — last wins, and a rule put anywhere else would have
been silently overridden.

## The numbers (U-P-35)

The note asks for "a small health bar" and names nothing, so every figure here is the implementer's under U-D-28.

| Number | Value | Why |
|---|---|---|
| Hurt band | below **0.66** | Two thirds: a building that has taken a third of its health is worth walking over to, and not yet an emergency. |
| Critical band | below **0.30** | Far enough below Hurt that the colour change is a distinct event rather than a slow fade, and close enough to down to mean "this one first". |
| Bar width | footprint, clamped **0.9 – 2.4** tiles | The floor makes a 1×1 Wall's bar a little wider than the wall, which is what makes it findable in a line of them; the ceiling stops the 10×14 Home core wearing a bar ten tiles across. |
| Thickness | `halfView × 0.016`, clamped **0.10 – 0.34** | 0.16 tiles at the opening framing and 0.24 at REL-130's far notch — the same pixels either way. The clamps cover a camera that is not the game's. |
| Gap above the footprint | **0.14** tiles | Clear air, so the bar reads as being *about* the building rather than painted on it. |
| Border | **0.22** of the thickness per side | The dark track shows around the fill, so the fill reads on a pale sprite as well as a dark one. |

Two drawing details are deliberate. The fill uses `numCapVertices = 0` — square ends, unlike the repair ring's
rounded ones — because a rounded cap makes the fill overhang its own track at full health, and a bar longer than
its track reads as a bug. And a building still standing keeps at least a **stub** of fill, so one on its last point
of health is never mistaken for one already down; a wreck shows its empty track, which *is* the reading the player
is looking for when deciding what to repair first.

## Still picture only

The note's acceptance says "no sim change", and there is none: no new sim state, no new tick work, no save-schema
change (`SaveSchema.Version` stays 12). `AskingChangesNothingInTheSimulation` places one chest, asks 200 times, and
requires `st.Rev` **exactly** equal, along with the machine's hp, `st.Turrets.Units.Count` and `st.Events.Count` —
the last two because `TurretState.Find` must not become `Of` by accident and start creating a damage entry just by
being asked.

## Checks

| Suite | Result | Before (at `e5b6deab`) |
|---|---|---|
| Offline sim (`dotnet test`) | **980 total — 979 passed, 1 skipped, 0 failed** | 969 total — 968 passed, 1 skipped, 0 failed |
| Unity EditMode | **1079 / 1079** | 1068 / 1068 |
| Unity PlayMode | **113 total — 105 passed, 3 failed, 5 skipped** | 109 total — 104 passed, 0 failed, 5 skipped |

The five PlayMode skips are the declared environmental ones and are unchanged.

### The three PlayMode failures are the known intermittent trio, and the bisect that said otherwise was wrong

The failures were `DroppedCargoPlayTests.DieWalkBackAndPressE_TheCargoReturnsAndThePileIsGone` (REL-125),
`OpeningUiPlayTests.U6_InteractingWithTheCoreOpensOneDrawerWithTheCoreCardFirst` and
`Ui.EscapeCancelsRepairPlayTests.OneEscapeCancelsTheRepairAndLeavesTheDrawerOpen_TheNextOneClosesIt` — **the same
three, by name, that REL-135 recorded as "the three known PlayMode failures"**: 3 failed at `d3827d18`, 0 failed at
REL-135, with REL-135 explicitly declining to claim the fix because no bisect was run. They come and go together.

They were first read here as a regression from this change, and a bisect was started. **That bisect was wrong, and
it is recorded rather than deleted because the way it went wrong is the lesson.** Each step was a single run of a
test that is known to flake: U6 failed alone (1 run), and passed alone (1 run) with the three UI files reverted —
which is a coin landing the same way twice, not a cause. Re-run afterwards with this change fully in place:

| Class, run alone | Result |
|---|---|
| `Relight.Tests.Play.OpeningUiPlayTests` | **4 / 4** |
| `Relight.Tests.Play.DroppedCargoPlayTests` | **1 / 1** |
| `Relight.Tests.Play.Ui.EscapeCancelsRepairPlayTests` | **2 / 2** |

There is also no mechanism. All three failures are a key press that did not take effect, and this change touches no
input path. `PaintHealthBar` is reached only through `PaintMachineControls`, which returns at `if (m == null)` when
no machine is paired; U6 interacts with the **core**, which pairs none, so the new code cannot run in it at all —
and in U6 the drawer never opened, so nothing in it was painted either way. The UXML and USS were separately shown
to import and instantiate cleanly, with every named element present.

**This commit is not claimed as their fix, exactly as REL-135 did not claim it.** REL-125 stays open. One green
run does not retire an intermittent failure, and three do not either.

### Both halves were shown to bite

| Mutation | Result |
|---|---|
| `Fraction` drops its clamp (`Hp / Max`, unguarded) | 10 passed / **1 failed** — `AnOverHealedOrStaleReadingIsClampedRatherThanDrawnOffItsTrack`. |
| `Exists` becomes `Max >= 0` | 9 passed / **2 failed** — `ABuildingThatCannotBeDamagedNeverHasAReading` and `ThereIsNoReadingForANullMachineOrANullState`. |
| `Damaged` becomes `Max > 0` (merely damageable) | 7 passed / **4 failed**, including `AnUndamagedBuildingWearsNoBar` and `RepairingBackToFullTakesTheBarAway` — the two halves of the owner's "shown only when damaged". |
| The drawing removed from **both** `MachineView.Animate` and `MachinePresenter.AnimateCore` | the 4 Play tests went to **0 passed / 4 failed**, each with its own message: *"the chest has lost half its health and is not showing it"*, *"the damaged chest is not wearing its bar"*, *"the bar was never drawn, so there is no thickness to compare"*, *"the core has lost half its health and is not showing it"*. |

The last one is the one that matters. Without it, a rule that reads perfectly and that **nothing draws** would have
passed all eleven sim tests.

**One mutation was a no-op and is reported as such.** Dropping the `max > 0` guard in `Health` — returning
`new HealthReading(hp, 0)` instead of `default` — left all 11 green. That is correct rather than a gap: for an
undamageable building `TurretRules.Hp` returns 0, so the two readings are identical in every observable field. The
guard saves a `st.Turrets.Find` lookup and says what it means; it is not load-bearing, and the test that covers
that case keys on `Exists`, which is why the sharper `Max >= 0` mutation above is the one that catches it.

New tests:

- `Tests/Sim/UI/BuildingHealthTests.cs` — 11 tests. They damage through the real `TurretRules.Damage` and repair
  through the real `RepairCommand`, not by writing `u.Damage`, so a test cannot pass by agreeing with a field the
  game has stopped setting. `ABuildingThatCannotBeDamagedNeverHasAReading` uses the **Depot**, the one machine
  REL-115/E-24 left without an integrity row, with an `Assume` on its `MaxHp` so it says so if that ever changes.
- `Tests/Play/Scene/HealthBarPlayTests.cs` — 4 tests in the authored scene. All four count the **enabled strokes**
  under the building, not only the flag, because a flag set by a presenter that draws nothing is the defect they
  exist to catch. The machine tested is a **chest**: outside `MachineActivityVisual.Supports`, so a stroke found on
  it can only be this one.

## Two things the tests guard that are easy to get wrong

- **A wreck must still have a bar to read.** `AWreckReadsEmptyAndStillHasABarToRead` requires `Exists` true and
  `Damaged` true at zero hp. A bar that vanished when the building went down would take away the reading exactly
  when the player most needs it.
- **No reading is not full health.** `ABuildingThatCannotBeDamagedNeverHasAReading` requires `Exists` false, not
  `Fraction == 1`. A forgetful caller that drew whatever it was handed would put a full green bar over every Depot
  in the city.

## Not done here

- **The HUD and the status panel are untouched.** The note names the world and the inspect card; neither of those
  is the HUD's core row, and `StatusPanelViewModel`'s HP is the engineer's, not a building's.
- **Not seen in play.** No owner playtest of this change, and no assistant screenshot is offered as one. Whether
  0.16 tiles reads as "a small health bar" over a Wall, and whether the card's bar is prominent enough, are
  questions only the owner can answer.
