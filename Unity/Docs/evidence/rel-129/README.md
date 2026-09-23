# REL-129 — the resupply card gets out of the way

PLAY-04, the fourth of the owner's ten notes from the 2026-09-22 play of the batch-3 build:

> "Automatic resupply working" sticks for a minute after completing before changing to "Expand your defences"

Built 2026-09-23. Code and automated checks only. **Nobody has played this build.**

## This is not a defect

Worth stating plainly, because the note reads like a bug report and isn't one. The card is *meant* to hold: it is
the acknowledgement that the objective was met, and `OpeningQueries` has held it deliberately since the opening
tutorial was written. The mechanism is right. The **figure** was wrong — the reference's `supplyAckS` is 45 s, which
the owner read as a minute.

So this commit changes one number and nothing else.

| | Was | Now |
| --- | --- | --- |
| `SupplyAckS` — "Automatic resupply working" | 45 s | **10 s** |
| `AckS` — "Attack repelled" / "Attack over" | 60 s | **60 s**, untouched |

**The owner named no figure.** The 10 is the implementer's under U-D-28, recorded as **U-P-31**.

### Why only one of the two cards moved

They look alike and behave differently, and the difference is the whole argument:

* **"Attack repelled"** reports an event that just happened, with nothing queued behind it. The player is not
  waiting for anything, so 60 s costs them nothing.
* **"Automatic resupply working"** stands *between* a finished objective and the next one. Every second of it is a
  second in which the player has completed the task and cannot see what to do next. That is exactly the experience
  the note describes — not "this card is too long" but "it sticks *before changing to* Expand your defences".

And what the card advises — watch the Assembler's Steel, Copper and power — is neither time-critical nor only
available here.

## Where the change lives — and why not in the tuning asset

The same rule as REL-126 and REL-128: `Sim/Data/Generated/CatalogueData.g.cs` is generated from `packages/`, which
this project may not edit, and is what `ReferenceData.Create()` — the data every offline sim test reads — is built
from, while the `.asset` is what the game reads. A figure moved in only one of them leaves the tests and the game
disagreeing.

This one needed no new layer: **`Sim/Data/OpeningBalance.cs`** already exists, already owns the opening's costs and
recipes, is already applied in both chains, and is already absent from `BuildLegacy()` — which is right here too,
since a pre-layout save's opening timings are not this opening's. It writes the value absolutely, so applying the
layer twice lands where applying it once did, and leaves data carrying no opening record untouched.

## Checks

| Suite | Result | Before |
| --- | --- | --- |
| Offline sim (`dotnet test`) | **941 passed, 1 skipped** | 940 passed, 1 skipped |
| Unity EditMode | **1040 / 1040** (173 s) | 1038 / 1038 |

Two new tests, and EditMode runs the sim tests too, so both totals move by the same two.

PlayMode was not run: the card is text produced by `OpeningQueries.Objective`, and no scene or controller changed.
Its three known failures (`DroppedCargoPlayTests`, `OpeningUiPlayTests` U6, `EscapeCancelsRepairPlayTests`) are
untouched and still open.

No existing test needed changing. `ARunningDeliverySkipsTheWholeResupplyBlock` advances the clock by 1000 s, far
past any window, so it never depended on the figure.

### Two tests added

* `OpeningObjectiveTests.TheResupplyCardYieldsToTheNextObjectiveInSecondsNotAMinute` — the card is up on delivery,
  still up one second before the window closes, and replaced by "Expand your defences" one second after. The window
  is read from `OpeningBalance.SupplyAckSeconds` rather than from the literal 10, so the test states the *rule* and
  not a second copy of the number; the one literal it does pin is the one the owner cares about — that the hold is
  well under the minute they complained about.
* `GameDataRegistryTests.TheResupplyCardHoldsForSecondsInEveryPathANewGameUses` — the same through the
  **asset-backed** path, plus the assertion that `BuildOriginal()` still carries the reference's 45, that the card
  yields sooner than "Attack repelled", and that `ReferenceData.Create()` and `registry.Build()` answer the same
  number.

## Stated limits

* **Nobody has played it, and nobody has read the card at 10 s.** Whether ten seconds is long enough to finish
  reading two sentences of advice is a question only a player can answer. If it proves short, the figure is one
  constant.
* **The card cannot be dismissed early.** It still yields on a timer, not on an action. Making it dismissable is a
  mechanism change, and the note asked for a duration.

No save-schema change — the hold is tuning, not state; a save mid-window simply sees the new figure. No new
decision: U-D-71 already records the note and says the figure, not the mechanism, is wrong.

## Files

* `Unity/Relight/Assets/Relight/Sim/Data/OpeningBalance.cs` — `SupplyAckSeconds`, and the `Opening` pass in `Apply`.
* `Unity/Relight/Assets/Relight/Tests/Sim/Campaign/Opening/OpeningObjectiveTests.cs` — one test added.
* `Unity/Relight/Assets/Relight/Tests/Editor/GameDataRegistryTests.cs` — one test added.
* `Unity/Docs/DECISIONS.md` — U-P-31.
* **Unchanged, deliberately:** `Data/Tuning/OpeningEncounterTuningAsset.cs`,
  `Data/Generated/Tuning/Tuning - Opening.asset`, `Sim/Data/Generated/CatalogueData.g.cs`, and `AckS`.
