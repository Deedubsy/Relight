# REL-130 — the mouse wheel opens the view by half again

**The owner's note (verbatim):** *"Can we have a zoom out functionality, mouse scroll. Not too far, just about
another 50%"*

There was no zoom at all before this. `CameraRig.Awake` set `orthographicSize` once from `viewTilesHigh` and
nothing ever wrote it again.

---

## What was built

| Piece | Where |
| --- | --- |
| The wheel, the notches, the glide, the clamp | `Assets/Relight/Presentation/CameraRig.cs` |
| Proof, in the authored city | `Assets/Relight/Tests/Play/Scene/CameraZoomPlayTests.cs` (new, 6 tests) |

`viewTilesHigh` (20 in the scene) is no longer *the* view. It is now the **closest** view — the framing the game
opens at, unchanged. The wheel only ever opens outward from it, to `ViewTilesWidest`, and back.

---

## The numbers (U-P-33)

The note gives one of them — "about another 50%". The rest are the implementer's under U-D-28.

| | Value | Why |
| --- | --- | --- |
| `ZoomOutFactor` | **1.5** | the owner's "about another 50%". 20 tiles high becomes 30. |
| `ZoomNotches` | **4** | four turns of the wheel cross the whole range, so each notch is 2.5 tiles — small enough not to lurch, few enough that the player reaches the wide view without grinding the wheel. |
| `ZoomPerSecond` | **16** | the exponential rate of the glide toward the notch. Roughly a tenth of a second to settle, so the wheel feels connected rather than snapping. |

The factor multiplies the **authored** `viewTilesHigh` rather than sitting as a second authored number, so moving
the opening framing in the scene moves the zoom-out with it and the two can never drift apart.
`TheWheelOpensTheViewByHalfAgainAndStopsThere` asserts that relationship, not the literal 30.

---

## What it deliberately does not do

- **There is no zoom in.** Wheeling up returns toward the opening framing and stops there. The owner asked for
  zoom out; letting the player go closer than the authored framing changes how the game reads at rest, which is
  a different decision from the one the note makes.
- **It is not saved.** Zoom is a view preference like a window size, not world state, so it is not in
  `SaveSchema` and a load re-opens at the closest framing. Nothing about the save format changed.
- **It cannot move the camera's target.** `Zoom()` writes `orthographicSize` and nothing else. The follow and the
  map clamp below it are untouched, which is why the wider view still cannot show black past the map edge.

---

## How it is wired

`MouseFlashlightPresenter` already reads `Mouse.current` directly rather than going through an action, and the
wheel follows it. **No action was added to `RelightControls.inputactions` on purpose:** that file carries the
player's saved rebindings through `Bindings.cs`, and adding a map entry for something nobody rebinds would
disturb them for no gain.

The wheel is guarded by `WorldInput.UiPointerProbe` — the same probe `WorldInput` consults before turning a
click into a world command — so turning the wheel over an open drawer scrolls the drawer's own list and leaves
the city where it was.

`Zoom()` runs as the **first** line of `LateUpdate`, before the follow, so the map clamp below it is computed
against the size the frame will actually draw. Running it after would clamp one frame behind and let a sliver of
black show at the edge on the frame the view widens.

`orthographicSize` is written only when it differs from the size the rig wants. That is not a micro-optimisation:
`CameraInsetTests.OpeningAndClosingAPanelNeverMovesTheCamera` and the opening UI tests assert exact float
equality across an open/close cycle (D-UI-10), and an unconditional write of a recomputed float would break them.

The darkness overlay needed no change. `LightingPresenter.Rect` reads `cam.orthographicSize` live every frame and
pads by 3 tiles, so the overlay resizes with the view on its own — `TheDarknessStillCoversTheScreenAtFullZoomOut`
is there to keep that true.

---

## Checks

| Check | Result |
| --- | --- |
| Unity PlayMode | see the run recorded in `Unity/Docs/TASKS.md` for this change |
| Unity EditMode | unchanged by this work — nothing outside `Presentation` and a Play test was touched |

The six Play tests are driven in the authored scene rather than on a bare GameObject, because both things that
could go wrong are scene-shaped: the map clamp has to hold at the wider view on the real 864×576 city, and the
darkness overlay has to keep covering the screen. A rig built in a test would have neither a city nor an overlay.

Four of the six drive `Wheel(float)` directly — that is the seam where the range and the clamp live, and it can
be tested on a machine with no mouse. The other two turn a **real** virtual mouse wheel, in the idiom
`ControlsCorrectionTests` established, because the first four would pass just as happily if nobody had wired the
mouse to the rig at all:

- `TurningTheRealWheelOverTheWorldOpensTheView` — the wiring exists.
- `TurningTheWheelOverAnOpenPanelLeavesTheCityWhereItWas` — the guard works. This one aims at the open status
  drawer's **own** centre, worked back from its `worldBound` into screen coordinates, rather than at the middle
  of the screen: the drawer does not cover the middle, so a test that aimed there skipped itself and proved
  nothing.

---

## Not done here

**Not seen in play.** Whether four notches at 16/s feels right on a real wheel is a question only the owner can
answer.
