# REL-127 — buildings give off their own small light

**The owner's note (verbatim):** *"Buildings should have their own light, not large, but big enough to
illuminate a small area around them, excludes walls and power poles."*

**The owner's answer when asked whether that light should be real or only seen:** **"Seen only"** — the
drawing changes, the simulation does not (U-D-71 b).

That answer is the whole shape of this change. Everything below follows from it.

---

## What was built

| Piece | Where |
| --- | --- |
| The rule — which kinds glow, how far, how strong | `Assets/Relight/Sim/UI/BuildingGlow.cs` (new) |
| The drawing — the glow subtracted from the darkness overlay | `Assets/Relight/Presentation/Light/LightingPresenter.cs` |
| Proof of the rule, and that the sim cannot see it | `Assets/Relight/Tests/Sim/Campaign/Light/BuildingGlowTests.cs` (new, 8 tests) |
| Proof that it is actually drawn in the authored scene | `Assets/Relight/Tests/Play/Scene/BuildingGlowPlayTests.cs` (new, 1 test) |
| Proof that no future sim rule starts reading it | `Assets/Relight/Tests/Editor/GlowStaysDrawnTests.cs` (new, 1 test) |

The two numbers are the implementer's under U-D-28 — the note says only "not large". They are recorded as
**U-P-32**: the glow reaches **2.5 tiles** beyond the footprint and removes **0.75** of the darkness where it
is strongest.

---

## Why it is not a `MachineSpec.LightRadiusTiles` row

That field is what `LightSources.Collect` reads, so a value there stamps the simulation's own lit mask. Three
rules read that mask:

- raiders **hesitate** on lit ground (`RaidDirectorTuning.LightHesitateS`),
- turrets see further in it,
- the engineer's own lit flag drives opening events.

A real glow on every building would therefore have turned any base into one large hesitation field and made
raids measurably easier — a balance change nobody asked for, arriving as a side effect of a lighting note. The
owner's "seen only" answer rules it out, and `BuildingGlow` is parked in `Sim/UI` beside `DarknessLook` and
`LightSweep`, which are picture rules for the same reason.

`GlowStaysDrawnTests.NoSimulationRuleAsksTheBuildingGlow` scans every `.cs` under `Assets/Relight/Sim` with
comments stripped and fails if any file but `Sim/UI/BuildingGlow.cs` so much as names the class. It is written
in the idiom `PresentationReadsLightTests` established for REL-9's mirror-image rule.

---

## Which kinds glow, and why it is not a list of keys

The issue's own acceptance line said "every buildable except wall, gate and the two poles". Taken literally
that makes belts, fast belts, undergrounds, splitters, inserters and barricades glow — and a line of forty
glowing belt tiles is exactly the "large" light the note rules out. So the rule reads the catalogue instead:

```
Glows(spec) = spec.Size >= 2 && spec.ReachTiles <= 0 && spec.LightRadiusTiles <= 0 && spec.ConeRangeTiles <= 0
```

| Clause | Excludes | Why |
| --- | --- | --- |
| footprint of 2 tiles or more | belt, fast belt, underground, splitter, inserter, **wall**, **barricade** | the one-tile parts laid down in lines; the Wall is the owner's own exclusion, and REL-132's Gate will be one tile, so it is excluded by the same clause without a special case |
| carries no power reach | **pole**, **big pole**, **substation** | the owner's "power poles" |
| is not already a light | lamp, arc lamp, floodlight | their light is real, comes from the mask, and goes out with their power; a fake glow under a dead lamp would be a lie about the grid |

What is left is the thirteen kinds the note calls buildings: Excavator, Pumpjack, Foundry, Refinery, Assembler,
Assembler Mk2, Mixer, Alien workbench, Supply chest, Generator, Gun turret, Cannon, Depot.
`TheBuildingsGlowAndNothingElseInTheCatalogueDoes` pins that exact set, so a kind added to the catalogue later
fails the test until somebody decides whether it is a building.

---

## The two numbers (U-P-32)

- **Reach 2.5 tiles, measured from the footprint edge, not the centre.** A 6×6 Depot and a 2×2 chest spill the
  same distance onto the ground around them, which is what "a small area around them" means. Corners are
  measured diagonally, so the spill is a rounded rectangle rather than a cross.
- **Peak 0.75, falling linearly to 0 at the reach.** Held below 1 on purpose: a building that lit its
  surroundings as well as a Lamp does would make the Lamp pointless. The point of the note is to see the ground
  by a building, not to replace the lighting the player builds.
- Overlapping glows take the **maximum**, not the sum, so a yard of machines has one even spill instead of a
  bright seam down the middle of it.

---

## How it is drawn

`LightingPresenter` has two halves: `Shade()`, a cached tile grid rebuilt only when something moves, and
`Paint()`, which runs every frame and subtracts the pointer's torch beam. The glow went into the **cached**
half, because buildings stand still and the pointer does not. A `_glowFold` over the visible footprints
rebuilds the cache when the camera pans or a building is placed or removed, and leaves it alone on a quiet
second.

One thing outside the overlay had to change with it. `Reveal(Vec2)` has exactly one consumer —
`EnemyPresenter` darkens a raider standing on unlit ground — and it read the beam alone. Left as it was, a
raider standing on visibly lit ground beside a Foundry would have been painted as a flat black silhouette. It
now returns the larger of the beam and the glow. Still picture-only: no simulation query is touched.

---

## Checks

| Check | Result |
| --- | --- |
| Offline sim suite (`dotnet test`) | **949 passed, 1 skipped, 950 total** — baseline 941 passed, 1 skipped; the 8 new sim tests |
| Unity EditMode | **1049 / 1049** — baseline 1040 / 1040; the 8 sim tests plus the 1 editor scan test |
| Unity PlayMode | see the run recorded in `Unity/Docs/TASKS.md` for this change |

`BuildingGlowTests.ABuildingChangesNothingTheSimulationCanSee` is the load-bearing one: it runs the same
powered lamp for one tick on empty ground and then inside a yard of four chests, and compares the light-source
count, the light fold and the **lit mask byte for byte**. That comparison is exact rather than approximate
because `LightRules.Opaque` shows only authored solids and walls/barricades block light — no other machine
affects the mask at all — so a chest that changed a single byte of it could only have done so through the glow.

`TheGroundBesideABuildingIsStillDarkToEveryRuleThatAsks` stands a Foundry at 40,40 and asserts `LitAt` reads
false across the 8×8 box around it. `LitAt` is what raider hesitation, turret sight and the engineer's lit flag
all read.

The Play test takes its readings as a **difference across one placement** rather than as absolute values,
because the authored Home already stands inside the visible rectangle and its own buildings glow too. It
clears the torch beam immediately before each pair of reads, with no frame in between, so the pointer cannot
point it again.

---

## Not done here

**Not seen in play.** Nobody has looked at this on screen; whether 2.5 tiles at 0.75 reads as "not large" is a
question only the owner can answer.
