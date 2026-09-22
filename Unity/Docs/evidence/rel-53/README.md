# REL-53 (AUD-UI-03) — one word for one state, and a count that says what it counts

**Date:** 2026-09-23. **Build:** local `main`, unpushed. **Played by:** nobody.

## What the audit saw

> the problems row says "2 × Gun turret: disabled (0 hp) — walk to it and repair it"; the defence row says
> "2 wrecks", and counts walls. `[CODE]` `[SEEN]`

## Premise checked first

Both rows were read before anything was changed:

- **The same predicate.** `HudViewModel` splits the problem rows by kind and state, and a wreck reaches them as
  `ProductionQueries.OperatingState` → `Disabled`, which is `TurretRules.Wrecked` (0 hit points) for **every**
  machine kind, not only turrets. `DefenceAlertSource.Refresh` counts `TurretRules.Wrecked` too. So the counts
  were never disagreeing about the world — the two rows were describing the same set of machines.
- **What was actually wrong** was therefore not arithmetic:
  1. the same state had two words — "disabled (0 hp)" on the problem row, "wrecks" on the defence row; and
  2. "2 wrecks" sitting immediately after "1 turret dry" in one sentence reads as *wrecked turrets*, so a wrecked
     wall was silently reported as a gun.

## What changed

| Where | Was | Now |
| --- | --- | --- |
| `ProductionQueries.StateText(Disabled)` | `disabled (0 hp)` | `wrecked (0 hp)` |
| `DefenceAlertSource.Sentence` wreck clause | `2 wrecks` | `2 structures wrecked` (`1 structure wrecked`) |

"Wrecked" is the word the rest of the game already uses for 0 hit points — the world badge, the repair card and
the raid account ("2 structures were wrecked") — so this removes the second vocabulary rather than inventing a
third. "Structures" is the raid account's own word for the same tally.

**Scope deviation, stated plainly.** The issue's scope line says "wording and counting in the HUD view-model
only". The one-word change had to be made in `ProductionQueries.StateText`, which is sim code, not the view
model: the HUD prints that text **verbatim** by design, because the brief forbids a second vocabulary for the
same state. Papering over it inside the HUD would have created exactly the defect the issue is about — the
machine panel and the world hover would then have kept saying "disabled". Nothing else in `ProductionQueries`
moved, and `MachineOperatingState.Disabled` keeps its name and its saved numeric value.

**U-D-61's rule is untouched.** The defence row still counts every machine at 0 hit points at the place, walls
and conveyors included. The sentence now says so; the rule did not change.

## The capture

`one-word-for-one-state.png` — 1920×1080 Game view frame, staged at Founders Court. Three rows in the one HUD
column, top right:

```
Founders Court: 1 turret dry, 2 structures wrecked      (defence row, warning)
Gun turret: wrecked (0 hp) — walk to it and repair it   (problem row)
Wall: wrecked (0 hp) — walk to it and repair it         (problem row)
```

On the ground below, left to right: a fuelled generator, a pole, a powered turret with the dry badge, a wrecked
turret and a wrecked wall, both carrying the red wreck badge. The defence row's "2" is the turret **and** the
wall, and the two problem rows say which is which.

**How it was staged.** An editor script inserted the generator, pole, two turrets and the wall into the running
state on clear ground near the engineer, marked the first turret as having fired (so its empty hopper counts —
`TurretAmmo.EverLoaded`), and damaged the other turret and the wall past their hit points with
`TurretRules.Damage`. The camera was parked and `WorldInput` disabled for the frame. That is capture tooling
only: no game rule was changed for it, and the live figures the script read back for this frame were
`supplied(dry)=True ammo=Dry`, two problem rows and one defence row, exactly as printed above.

## Checks

- Offline sim tests: **919 passed, 1 skipped** (was 918 + 1).
- EditMode: **1015 of 1015** (was 1014).
- PlayMode: **97 total — 92 passed, 0 failed, 5 skipped** (unchanged).
- New test, `HudViewModelTests.AWreckedTurretAndAWreckedWallReadTheSameWayInBothRows`: one wrecked turret plus
  one wrecked wall plus one dry turret; it asserts no row says "disabled", that each wreck row says
  "wrecked (0 hp)" and offers the repair, that a wreck ranks first, that the defence row is one row for the
  place, that its `Wrecks` equals the two problem rows' machines, and that its sentence reads
  "Home: 1 turret dry, 2 structures wrecked" and never "turrets wrecked".
- Three existing assertions were adapted to the intended wording (`DefenceAlertTests` ×2, `TurretTests` ×1) and
  three comments updated (`HomeQueries`, `TurretRepairTests`, `TurretTests`). No test was weakened: each one
  still asserts the exact sentence, now the intended one.

## Limits

- Nobody has played this build. The capture was taken by the assistant.
- The wording of the repair half of the line ("walk to it and repair it") is GP-W5's and was not touched.
- Whether a wrecked wall should count towards the defence row at all is U-D-61's question, not this issue's.
