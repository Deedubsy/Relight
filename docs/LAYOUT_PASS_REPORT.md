# Layout and readability pass — built 2026-09-05 (unverified)

`docs/PROGRESS.md` **T3**. Owner Claude Code. Branch `phase-4`. This is T3's evidence file.

The task is `ROADMAP.md` §2's row *"Layout and readability pass (new, pre-test)"*, whose scope
line reads:

> Full-viewport canvas; HUD as overlays (territory top-right, power and pockets bottom-left,
> speed and clock bottom-right, toasts bottom-centre); map view as a full-screen overlay;
> default zoom with the HQ lot ≈ one third of screen height; street surface and kerb line;
> rubble as clumped rectangles thinning with density; soft light falloff; distinct machine
> silhouettes with outlines; engineer as a two-tone sprite with facing and a lamp cone;
> front-segment pip state on the kerb in the world view; distinct crawler/shade shapes.
> Flat colour only, no art direction.

**Everything in this pass is drawing.** No sim file was opened. `packages/sim` is byte-identical
to `HEAD` before the pass; the snapshot, the D6 fixtures and the config hash are unmoved
(`snapshot:check` green, config `0176f61d`). Nothing here can move a measured number, and nothing
here is a rule.

This finishes what the economy-fix task started on 2026-09-04 (`SLICE_REPORT.md` "Economy fix, the
layout pass and E-rifle at tile scale", ROADMAP §0 line 1): that pass built `Scale.RESIZE` and the
full-viewport world view, the top-left HUD block and the bottom-left key strip, the HQ's kerb pips,
and the Depot's outline, fill bar and edge beacon. This one builds the rest of §2's list and moves
the HUD those two blocks made into the four corners the list names.

**Ran out of order** on the human's instruction of 2026-09-05: T1 and T2 sit above it in
`PROGRESS.md` and are blocked on D-P4-9, D-HOUR-2 and D-P4-10.

---

## 1. Constitution rule 12 — the prompt against the doc

Rule 12: *before starting any milestone, compare the prompt with the design doc and the decided
rows of `DECISIONS.md`; if the prompt contains a number or a rule that is not in either, list every
one and stop.* The comparison was made before any code was written and is recorded here.

**The one contradiction — the prompt line was not built.**

| the prompt asks | the decided row says | what was done |
|---|---|---|
| "engineer as a two-tone sprite with facing **and a lamp cone**" | **D-B5-1** (`decided`, Daniel, 2026-09-04): no personal light — *"the dark is what the claim pays off"*; the hand lamp exists only as the `?handlamp=1` preview | the two-tone sprite and the facing are built; **the lamp cone is not**. `ROADMAP.md` §2's line is wrong and the decided row stands (`worldScene.ts` carries the reason at the drawing site). D-B5-1 can only reopen on **STANDARDS row A.7**, the played-dark sentence — and T5–T8 were waived, so that sentence does not exist (`GATE_B.md`). |

**The numbers the prompt carries that the design doc does not.** None of them binds a sim
constant; every one is a render value and is tagged `GAME-ASSUMPTION` at its definition. They are
listed rather than obeyed silently, per rule 6. Rule 12's "stop" was not triggered by them because
none is a number or rule *of the game* — but the human still writes the rows (§6 below).

| the prompt's value | in `RELIGHT-design.md`? | in `DECISIONS.md`? | built as |
|---|---|---|---|
| "the HQ lot ≈ **one third** of screen height" at the opening zoom | no | no | `HQ_SCREEN_FRAC = 1/3`, clamped to the constitution's 0.5–3× (D-P4-1) → **D-LP-1** |
| the four HUD corners, by name | no (§4 asks only that the key strip be on the HUD, STANDARDS 4.3) | no | four corner overlays; the key strip takes the fifth corner, top-left → **D-LP-2** |
| "**soft** light falloff" | no — §4 says the unlit tile is "desaturated and darkened to ~25 %" and §6/§7 treat lit as binary | no | a render-side blur, `LIGHT_SOFT = 0.7`; `lightPix` / `lightMask` / `litAt` untouched → **D-LP-3** |
| "kerb line" where a lot meets a street | no — §4 has streets and lots, no kerb line | no | a 1.5 screen-px pale line on every lot↔street tile edge |
| "distinct machine silhouettes **with outlines**" | no | no | one drop shadow + one 2 screen-px rim on every box machine |
| "distinct crawler/shade shapes" | §7 distinguishes them by rule (a shade is only on lit tiles), never by shape | no | crawler disc, shade diamond |
| the key strip's wrap width | no | no | `KEY_STRIP_FRAC = 0.56` of the viewport |

---

## 2. Built

The eleven items of the prompt's list. Ten built, one refused (the lamp cone, above).

| # | prompt item | state before this pass | state now |
|---|---|---|---|
| 1 | Full-viewport canvas | **already built** (`Scale.RESIZE`, M1) | unchanged; the opening zoom now re-fits with it (item 4) |
| 2 | HUD as overlays, four named corners | one text block in the top-left; toasts bottom-left, on top of it | **built.** Four corner overlays: top-left the key strip and the in-hand line, top-right the view / tile count / zoom / focused block, bottom-left pockets and power, bottom-right the clock and the speed. Toasts moved to bottom-centre (`style.css`), centred on the **canvas** rather than the window so the 420 px panel column does not pull them off-centre. Each corner is `setScrollFactor(0)`, scaled `1/zoom` and pinned with the zoom-about-centre compensation, so the HUD is screen-constant at every zoom. |
| 3 | Map view as a full-screen overlay | a fixed 648 px square in the canvas corner | **built.** `layout()` fits the map to the canvas: the largest scale that fits inside a 4 px inset, centred, refitted on every resize. The ground raster is one sample per pixel, so it is rebuilt when the fitted size changes (`CanvasTexture.setSize` + `setTexture`, which Phaser needs to re-read the frame). Every `PAD`-as-offset use — the eight of them, including hit-testing and the walk-here click — became `this.ox` / `this.oy`. |
| 4 | Default zoom, HQ lot ≈ ⅓ of screen height | a fixed 1× | **built.** `fitZoom()` on create and on resize; the human's wheel wins for good after the first turn (`zoomTouched`). Measured in §4. |
| 5 | Street surface and kerb line | street surface built (tileset `T_STREET`); no kerb line | **kerb line built.** A pale 1.5 screen-px line on every edge where an owned lot tile meets a street tile, skipped against the river (owner −2, which has its own bank), viewport-culled. |
| 6 | Rubble clumped, thinning with density | **already built** (M1) | unchanged |
| 7 | Soft light falloff | a hard texel step at the edge of a lamp's reach | **built.** Two passes of a separable [1,2,1]/4 blur over the lit mask; an unlit texel is lerped `LIGHT_SOFT` of the way towards white, a lit texel stays full white. The texture is still one texel per tile drawn at 32 px with `antialias: true`, so the card's bilinear filter carries the ramp the rest of the way. **The sim's lit set is untouched** — `lightPix` holds the binary mask, `lightMask` / `litAt` still decide what a shade may stand on and what burns off. |
| 8 | Distinct machine silhouettes with outlines | shapes distinct, outlines not: the turret's showed only while idle, the Generator / Excavator / assembler had an inner line and no rim, the lamp and pole had neither | **built.** Every box machine (`BOX_MACHINE`: turret, floodlight, Generator, Excavator, assembler, big pole) takes the same two marks — a dark drop shadow and a 2 screen-px rim. The Depot takes the shadow only, because it already owns the STANDARDS C.2 three-screen-px white outline and would otherwise carry two rims. Belts, inserters and posts are not boxes (a rim per tile would draw a ladder down a belt run); the inserter gets a light box rim, the lamp and pole a dark backing slab. **Two state marks survive the change and are drawn after the rim so they still win:** the turret's grey inner square when `busy` is false (`busy` on a turret means "covers a live edge", `flow.ts:114` — a real signal, not decoration) and the red empty-blink on a turret with no rounds or a Generator with no coal. |
| 9 | Engineer two-tone with facing (**and a lamp cone**) | two-tone, head at a fixed offset | **facing built, cone refused.** The amber head rides the sim's own `e.face` vector with a chevron on the leading edge; `lastFace` holds the last non-zero facing because `face` is `[0,0]` at a standstill, so which way the engineer is turned reads while standing still. The lamp cone is D-B5-1's; not drawn. |
| 10 | Front-segment pip state on the kerb | the HQ's segments only | **built.** Every **Held** block's front now carries its own kerb pip — the same `pipOf` the map view and the panel use, so the three cannot disagree — with a viewport cull, since the set is no longer four segments. |
| 11 | Distinct crawler / shade shapes | two discs a pixel apart in radius | **built.** The crawler keeps the disc; the shade is a diamond, which reads at 0.5×. The hitbox, the rifle and §7's lit-tile rule are unchanged: the diamond is the drawing of the same body. |

**Not in the prompt, done because the pass broke it:** nothing. No behaviour was added.

---

## 3. Assumed (every `GAME-ASSUMPTION` this pass added)

Seven new tags, all in `packages/game`. Each is quoted from its site.

| tag site | the assumption |
|---|---|
| `worldScene.ts` `hudText` → four corner fields | *"the ROADMAP names those four corners and the key strip (STANDARDS 4.3) is not one of them, so it takes the corner left over — top-left, with the in-hand line, the two things that answer 'what am I holding and what can I press'."* The Depot beacon (C.2) still rides the viewport edge nearest the Depot while it is out of view. |
| `worldScene.ts` `fitZoom()` | *"the ROADMAP asks for 'the HQ lot ≈ one third of screen height' and names no tolerance;* `HQ_SCREEN_FRAC` *is that third, clamped to the constitution's 0.5–3× range (D-P4-1), and it is drawing only — no rule reads it."* Recomputed on resize, never after the wheel is touched. |
| `worldScene.ts` kerb line | *"drawing only; nothing reads it."* |
| `worldScene.ts` `paintLight()` | *"the blur is a render choice —* `lightPix` *is untouched,* `lightMask`/`litAt` *still decide what is lit, so what a shade may stand on and what burns off is unchanged."* |
| `worldScene.ts` `drawMachines()` | *"drawing only"* — the shadow and rim carry no state; the two state marks are drawn after them. |
| `worldScene.ts` `drawEngineer()` | *"drawing only — nothing reads the chevron, and the lamp cone the ROADMAP line also asks for is NOT drawn (D-B5-1 decided against a personal light)."* |
| `worldScene.ts` `drawThreat()` | *"drawing only — the hitbox, the rifle and §7's lit-tile rule are unchanged."* |

Untagged but new, and named here so they are not silent: `KEY_STRIP_FRAC = 0.56` (the key strip's
wrap width), `PAD = 4` in `cityMapScene.ts` (was the inset inside the 648 px square, now the inset
from the canvas edge), and `--panel-w` / `--gutter` in `style.css` (the two lengths the toasts need
to centre on the canvas). They are lengths in a stylesheet, not values a rule could read.

**§26 recount: three systems, complexity 5/10, 33 → 33 untagged.** The pass built no system and
moved no untagged number: every value it added is a render constant with a `GAME-ASSUMPTION` tag
or a stylesheet length, and none of them is one of the doc's 33.

---

## 4. Measured — *unverified*

Cheap checks only (a `go` is code + light review + docs, `CLAUDE.md`). **The verification pass has
not run on this commit.** What is green:

| check | result |
|---|---|
| `npm test` | **121 / 121 pass** (sim, harness, tools) |
| `npm run typecheck` | clean, including the game build |
| `npm run lint` | clean |
| `npm run docsync:check` | green — doc tables match `packages/sim` |
| `npm run snapshot:check` | matches: `held 28 front 11 interior 18 lost 0 claims 27 assemblers 3 config 0176f61d` |

**Numbers computed from the code, not from a running browser** (`tsx` over the sim's own city, the
D6 seeds; the arithmetic of `fitZoom()` and of the blur, no renderer involved):

| seed | light map | HQ lot | `fitZoom()` at 1040 px canvas | at 1400 px | at 2000 px | blur, one repaint |
|---|---|---|---|---|---|---|
| 3 | 800 × 800 = 640,000 texels | 30 × 30 tiles = 960 px | 0.50× → lot is **46.2 %** of height | 0.50× → **34.3 %** | 0.69× → **33.3 %** |  8.7 ms |
| 4 | 800 × 800 | 31 × 46 tiles = 1,472 px | 0.50× → **70.8 %** | 0.50× → **52.6 %** | 0.50× → **36.8 %** | 11.7 ms |
| 5 | 800 × 800 | 37 × 29 tiles = 928 px | 0.50× → **44.6 %** | 0.50× → **33.3 %** | 0.72× → **33.3 %** | 7.1 ms |

Two things the human should see in that table:

- **The ⅓ is only reached on a tall canvas.** On a 1080p window the fit clamps to `ZOOM_MIN` 0.5×
  and the lot fills 45–71 % of the height instead of 33 %. The clamp is the constitution's
  (D-P4-1); the third is the ROADMAP's. They do not both fit on a 1080p screen with a D6 lot,
  because a D6 lot is 29–46 tiles tall, not §4's 24. **D-LP-1.**
- **Seed 4's HQ lot is 46 tiles tall** and never reaches a third at any canvas size in the table.
  The rule as written cannot hold for every seed; what it can hold is *"as close to a third as the
  zoom range allows"*, which is what is built.

**The blur cost is real and is the one thing this pass could regress.** 7–12 ms per repaint in
Node, at `LIGHT_REFRESH_MS = 125` (eight repaints a second) and only when a texel changed. That is
a Node measurement of the arithmetic alone — no `putImageData`, no upload — on a machine that is
not the reference machine. **It is the pass's first item for the verification pass.**

### What the verification pass should measure

1. **The light-map repaint against the 60 fps DoD.** The 900 s Playwright soak, seed 3, world view,
   with the burn-off running (a claim changes texels for 20 + 60·d s, so repaints are back-to-back
   through it). Before this pass the soak read 0 frames over 50 ms in an hour and five bursts over
   50 ms on the bot's walks and claims (`DEFERRED.md`). If the burst count rises, the blur is the
   suspect and the fix is a dirty-rectangle blur over the changed texels rather than the whole map.
2. **The map view's raster rebuild on resize.** `layout()` rebuilds `W × H` samples whenever the
   fitted size changes; dragging a window edge changes it every frame. Measure a slow drag from
   1280 to 2560 px wide with the map open.
3. **The four corners at 0.5× and 3× on the reference machine.** The pin arithmetic is compensating
   for Phaser zooming `setScrollFactor(0)` objects about the camera centre; a screenshot at each
   end of the range is the check.
4. **A re-run of the existing soak's frame table**, so the pass's before/after is on one machine.

Until then every number in this section is *unverified* except the five cheap checks.

---

## 5. Deferred, and where it went

- **The four STANDARDS rows this pass exists to enable (4.3, B.3, B.6, C.2) are still open.** They
  are one-minute *human* checks and T5, the walkthrough that carries them, was **waived, not run**
  (`GATE_B.md`). The pass builds what they check — the key strip is on the HUD as a corner overlay
  (4.3), the kerb pip shows a segment's state and an empty turret on every Held block (B.3, B.6),
  the Depot beacon rides the viewport edge (C.2) — but building is not checking. `ROADMAP.md` §6
  still reads **closed: 0 of 46** and is not changed by this pass.
- **The stranger test** — the prompt's own check is *"a stranger points to street, lot edge, lit
  area, rubble, Depot and engineer unaided"*. `docs/layout-pass/STRANGER_TEST.md` was never
  written and T7 was waived. **The pass's success condition is untested.** This is the largest
  thing this report cannot claim.
- **Art direction** — `ROADMAP.md` §7 says the readability pass would ask for the grim-and-quiet
  vs saturated lean. It did not need to: every value here is a shade of the palette already in the
  code, and the pass is flat colour only, as the prompt requires. The question stays Phase 12's.
- **Dirty-rectangle light blur** → the verification pass, only if item 1 above goes red.
- `.serena/` appeared untracked in the working tree (an MCP server's cache). Not committed, not
  ignored — a `.gitignore` line is the human's call.

---

## 6. Three decisions for the human

New rows in `DECISIONS.md`, all `recommended`, none `decided`. The programme never picks a value
from this list (rule 7).

- **D-LP-1 — the opening zoom.** The lot fills 33 % only on a tall canvas; on 1080p it clamps to
  0.5× and fills 45–71 %, and seed 4's 46-tile lot never reaches a third. (a) as built — a third,
  clamped, best-effort; (b) fit the whole lot plus one street on every side, whatever fraction that
  is; (c) drop the rule and go back to a fixed 1×. **Recommended (a).**
- **D-LP-2 — the key strip's corner.** The ROADMAP names four corners and the key strip is not one
  of them; it took top-left with the in-hand line. (a) as built; (b) the key strip hides after the
  first ten minutes or on a key; (c) it moves into a `?` overlay and the top-left carries the
  in-hand line alone. **Recommended (a) now, (b) at Phase 12's onboarding** — STANDARDS 4.3 asks
  only that it be on the HUD rather than in a tooltip, and it is.
- **D-LP-3 — the soft light falloff.** `LIGHT_SOFT = 0.7` with a two-pass blur softens the *edge*
  of the lit set without changing the set. It touches the game's identity: §4's dark is the claim's
  price, and a soft edge reads as less dark. (a) as built; (b) hard edge, revert the blur — the
  binary map is the look; (c) softer still (a wider blur or `LIGHT_SOFT` towards 1). **Recommended
  (a), and it is the one item on this list I would most like a human eye on** — it is a look
  judgement, it is cheap to reverse (one constant and one loop), and it is also the pass's only
  frame-cost risk. If the answer is (b), items 1 and the dirty-rectangle fallback in §4 both go
  away.

---

## 7. Where this pass disagrees with the doc

- **`ROADMAP.md` §2's layout row asks for a lamp cone that D-B5-1 decided against.** The row is
  wrong, not the decision. It is left as written rather than edited, because editing a roadmap line
  to match what was built is exactly the move rule 12 exists to prevent; the disagreement is
  reported here and the human edits the line (or reopens D-B5-1 — which needs STANDARDS A.7's
  played-dark sentence, which the waiver did not produce).
- **The design doc has no kerb, no HUD corners and no falloff.** §4 describes ground, light and
  zoom; none of the seven values in §1's second table is in it. They are render values and stay in
  `packages/game`, not in `constants.ts`, precisely because no rule may read them.
- **§4's lot is 24 × 24 tiles; the D6 city's HQ lot is 29–46.** Not new, and not this pass's — but
  it is why the ⅓ rule cannot be met at 1080p, so it surfaces here for the first time as a visible
  consequence.
