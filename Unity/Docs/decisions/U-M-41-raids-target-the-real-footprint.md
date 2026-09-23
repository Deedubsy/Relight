# U-M-41 — A raid targets the real footprint, for its route and its damage

**Status:** Accepted (engineering) · **Date:** 2026-09-23
**Provenance:** engineering (overturnable by the owner); made while fixing REL-124 on branch `defects-rel-124-125`, which waits for the owner's OK before merging
**Supersedes / amends:** none
**Related:** TASKS REL-124 (CMB-15) entry · Linear REL-124 · commit b0195a1e · U-D-RAID-01 (the raid field)

## Context
`DirectorRules.Target`, `EnemyCoreHook.Rect` and `Plants.Rect` described a raid target as a square as wide as its longer side (`size = W > H ? W : H`). For the 10×14 Home that square ran x 65–78 while the core runs x 65–74, so raids routed to, and could bite, empty ground beside the Home. Fixing it meant choosing whether the route, the damage, or both follow the real rectangle.

## Decision
(a) A raid target is its real width × height rectangle everywhere: `EnemyCoreHook.Rect`, `Plants.Rect` and both `DirectorRules.Target` overloads return width and height.
(b) The route field (`RaidField`) seeds its zero ring round the real rectangle, and its cache key includes both dimensions.
(c) Origin, approaches, staging, headings, pacing, the warning, the side words and the opening clearance use the real centre and bounds.
(d) A raider's bite and a spitter's round damage the core or a plant only inside the real footprint (`DirectorRules.Inside`).

## Options considered
- **Chosen:** the real rectangle for both route and damage. Raiders end their route at the real walls and only hit what is there.
- **Rejected:** keep the square for the route and use the real rectangle only for damage. A raider waiting at the square's edge would stand at the route's end and never reach the core.

## Consequences
- Raids against non-square targets arrive at, and strike, the actual walls. The Home's east approach ends 4 tiles nearer than before.
- `RaidField` is not saved, so the save format does not change.
- Checked by `RaidTargetRectTests` (5) and the offline sim suite. EditMode and PlayMode in Unity, including `RealCityRingTests` and `RealCityIntegrationTests`, must run before the merge.
