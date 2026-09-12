# Relight prototypes (Phase B, task B-14)

Three **proportionate prototypes**, built to retire the three technical risks that the Unity migration could not
retire by reading: they are measurements, not features. Each one is inspected and recorded *before* scene structure
is fixed, and each one is grown by a named later task rather than thrown away.

| Folder | Risk | What it proves | Grown by |
|---|---|---|---|
| `R6-SlotGrid/` | **R6** — runtime UI Toolkit has no built-in drag-and-drop | 40 + 20 slots in **one** `UIDocument`, `uiDrag.ts` ported rule for rule | **C-06** Backpack / storage panel |
| `R7-Tilemap/`&nbsp;(scene only) | **R7** — 864 × 576 = 497,664 tiles | the whole map painted into one scene over the planned layers | **D-09** World rendering |
| `R2-Lighting/` | **R2** — the reference's worst frames are a light-mask cost | 200 URP 2D point lights, 20 flickering, 10 moving, 50 shadow casters | **C-11** Lighting and time of day |

## What each prototype leaves behind

* **R6 → C-06.** `SlotDragController` is the port of `packages/game/src/uiDrag.ts` (rules, not DOM code: the mapping
  table is in the class comment). `SlotGrid.uxml` / `SlotGrid.uss` are the grid layout, using only `tokens.uss`
  variables. C-06 keeps all three and replaces `SlotGridModel` — every mutation becomes a B-06 sim command — and
  grows the three drag callbacks into the reference's real policies (`inventoryPanel.ts`: transfer across sides,
  equip, quickbar, "Release to cancel").
* **R7 → D-09.** The scene authored by `Relight/Prototypes/Paint R7` and the measured cost of painting, saving,
  loading and panning it. `R7Map` is throwaway test terrain; the real region arrives with C-01.
* **R2 → C-11.** `LightingRig`, the light radii taken from `CONTENT_CATALOGUE.md` (Lamp 4 t, Arc lamp 6 t,
  Floodlight 12 t) and the three-pass measurement that separates the cost of tiles, lights and shadows.

## Building and measuring

Editor menus (they write assets and scenes; nothing is constructed at runtime — `TECHNICAL_ARCHITECTURE.md` §7.1):

```
Relight/Prototypes/Build R6     icons + Scenes/R6-SlotGrid.unity
Relight/Prototypes/Paint R7     Scenes/R7-Tilemap.unity, 497,664 tiles, 4 layers
Relight/Prototypes/Build R2     Scenes/R2-Lighting.unity (no camera: loaded additively over R7)
Relight/Prototypes/Build All    all three, in that order
```

The measurements are play-mode tests in `Assets/Relight/Tests/Play/Prototypes/`. They write their figures to
`Unity/Docs/evidence/phase-b/b14-*.json`, so every number in the B-14 report has a file behind it. Tests that need
real UI layout or a real renderer call `PrototypeFixture.RequireLayout` / `RequireGraphics`, which **ignore**
themselves under `-nographics` rather than report a figure that was never measured.

### The painted map is not in the repository

`Scenes/R7-Tilemap.unity` is **197,826,489 bytes (188.66 MiB)** of `ForceText` YAML once all 497,664 cells are
painted. That size is itself the R7 finding, so the scene is measured and then left out of version control
(`Scenes/.gitignore`); `Relight/Prototypes/Paint R7` rebuilds it in about 3.5 s. The R7 and R2 measurement tests
call `PrototypeFixture.RequirePaintedMap`, which **ignores** them with that instruction when the scene is absent —
so a clean checkout is green and nobody is shown a figure that was not measured.

### Two kinds of frame number

Batchmode has no window and no swap chain, so its main loop spins uncapped and `Time.unscaledDeltaTime` measures
almost nothing a player would pay for. The sweep tests therefore record **both**: `sweep_*` (deltaTime — evidence
that the sweep ran and never stalled, *not* a frame budget) and `probe_*` (`RenderProbe`: `Camera.Render` into a
real 1920 × 1080 `RenderTexture` followed by a one-pixel `ReadPixels`, which blocks until the GPU is finished).
Only the `probe_*` figures are quoted as costs.

## Boundaries

Nothing here is referenced by `Relight.Sim`, `Relight.World`, `Relight.Presentation` or `Relight.UI`; the
dependency runs one way only. `Relight.Prototypes` may be deleted wholesale once C-06, D-09 and C-11 have taken
what they need.
