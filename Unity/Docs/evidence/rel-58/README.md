# REL-58 part 1 — things built and looked at once (2026-09-22)

**What this is:** the assistant's one look at each REL-58 (UI-08) item in editor Play, at 1920×1080, at 1920×1080 with the UI at 150%, and at 1280×720. The owner's verdict is not here. It belongs to GATE-C (REL-102) and GATE-L. No code changed for this look.

**Who looked:** the assistant only, 2026-09-22. This is not owner play.

## How it was staged

- **Saves:** redirected to a temp folder before Play (`staging/redirect.cs`, `SessionState` key `Relight.SaveRootOverride`). The owner's saves were not touched.
- **Game:** a new game at Founders Court (full 864×576 city).
- **Admin commands** (editor tools, not a player path): materials, weapon, invulnerable, repair, fuel, clear-enemies, reset-overrides. Invulnerability was reset before stopping.
- **Eval scripts:** every `staging/*.cs` file was run in the open editor through the Unity CLI. They place turrets, poles and generators free of charge, refill hoppers, move the Engineer, and read state.
- **Keys:** key presses cannot reach an unfocused Game view, so the handlers were called directly:
  - `UiShell.ToggleBackpack()` for Tab;
  - `UiShell.Toggle(...)` for drawers;
  - `WorldInput.Interact` by reflection for E.
- **Rifle:** put in bar slot 9 directly, then `SelectBarSlot(8)`.
- **Raid account:** the real opening raid was used. A second raid came from the Admin raid path with its `Scripted` flag set to false by hand, because that path never gets an account (REL-119).
- **Sweep:** the camera was zoomed out to 17 and `Time.timeScale` set to 0.05. This slows only the drawing. The sim runs on `Time.unscaledDeltaTime`.
- **Tracer:** frozen with `SimHost.Paused` on the first frame a shot existed (`staging/pausehook.cs`). The Admin drawer was then opened so the pause menu stayed hidden.
- **Drags:** made through the drawer's own seams, `DragFrom` then `DragToPoint`. This is the near-miss path, not a real pointer.
- **Editor left as found:** Play stopped, save redirect off, `timeScale` 1, camera size 10, Game view 1920×1080, UI scale 1.

## Files

The PNGs are 256-colour reductions of the captures, to keep the folder small. The three sweep files are kept lossless.

| File | Item | What it shows |
|---|---|---|
| `01-tint-1920.png`, `01-tint-1920-ui150.png`, `01-tint-1280.png` | Placement preview (L-02) | Two rings. The coverage outline is cut at the wall, with the advice "Restricted position: it covers about 57% of its range…". The lit tint still covers ground behind the wall. Defect: REL-116. |
| `02-blind-1920.png`, `02-blind-1920-ui150.png`, `02-blind-1280.png` | Blind turret (L-02) | The badge and the line "This turret can't see into the dark. Light the ground it guards." No overlap. "+1 more" is shown. |
| `03-sweep-0-before-1920.png`, `03-sweep-1-1920.png`, `03-sweep-sheet.png` | District sweep (L-02) | The ground lights on the first frame; the lamp heads bloom over about 1.5 s. Defect: REL-117. The notice "Substation 0 connected · 6 streetlights on" is REL-11. Seen at 1920 only. |
| `04-brownout-1920.png`, `04-brownout-1920-ui150.png`, `04-brownout-1280.png` | Brownout notice (L-02) | "Low power at Founders Court · lights are shrinking, and turrets fire slower and see less far". Strip: "Power 600 kW · Need 932 · short". |
| `05-raid-account-1920.png`, `05-raid-account-1920-ui150.png`, `05-raid-account-1280.png` | "Repelled" account | "Raid repelled · 3 aliens killed · 9 rounds fired · nothing lost" (1920 and 150%); "… 6 rounds fired …" (1280). |
| `06-hesitation-1920.png`, `06-hesitation-1920-ui150.png`, `06-hesitation-1280.png` | Hesitation guide line (L-02) | "Aliens avoid light. They pause at its edge and look for a dark way in." It is pushed into "+1 more" within about 4–6 s. Defect: REL-118. |
| `07-raid-core-1920-ui150.png`, `07-raid-after-1920-ui150.png` | Opening raid (real) | The core disabled; goal "Repair the Home core". "Base under attack" stays with no aliens alive (REL-12). |
| `08-nopower-1920.png`, `08-nopower-1920-ui150.png`, `08-nopower-1280.png` | GP-UX-7(b) no power | The deep-red "No power · Home workshop" strip; the top bar reads "Power · no fuel"; streetlights off. No power row reached the inbox. |
| `09-tab-1920.png`, `09-tab-1920-ui150.png`, `09-tab-1280.png` | GP-UX-7(c) Tab, GP-UX-9 Sort | The Backpack opens alone; Sort is on the header. A second Sort remains in the bottom row; "Backpack" appears twice; the hint line is hidden at 150% and 1280. |
| `10-e-depot-1920.png` | E at the depot | The workshop opens beside the Backpack. The drawer covers the alert column, hiding "Major assault approaching from the S · Arrives in 248 s" (REL-59). |
| `11-rifle-hand-1920.png`, `11-rifle-e-depot-1920.png` | GP-UX-8, E with a weapon | With the rifle equipped, E opened the workshop; the tool and equipped weapon both stayed the rifle. Not tested: E with the pointer on a machine. |
| `12-tracer-1920-paused-admin.png` | GP-UX-8 turret tracer | The tracer starts at turret 17 (70.7, 365.7 → 75.2, 369.8), not at the Engineer. World-space, so the sizes were not repeated. |
| `13-build-1920.png`, `13-build-1920-ui150.png`, `13-build-1280.png` | GP-UX-7(d) build cards | Every card is one height. At 150% and 1280 the grid falls to one column, and the stat line runs past the border at all sizes. Defect: REL-120. |
| `14-drag-before-1920.png`, `14-split-after-1920.png`, `14-drag-split-1920-ui150.png`, `14-drag-split-1280.png` | GP-UX-9 near-miss drop and split | 1920: steel landed in the hovered slot 9 ("Stack moved."); a Shift-split of coal put 25 in slot 7, not the first empty slot 3 ("Stack split."). 150%: slot 9 → 5 moved; slot 1 → 11 split 25/25. 1280: slot 5 → 3 moved; slot 7 → 10 split 13/12. No release point was inside any slot. |

## Not seen

- **GP-UX-7(a)**, the buffered objective line. Staging it needs the whole opening chain faked by hand. It is covered only by the offline `BufferedLineTests`. It is left for GATE-C.

## Defects filed or confirmed

| Issue | What |
|---|---|
| REL-116 (UI-08a) | The placement tint shows lit ground behind walls. |
| REL-117 (UI-08b) | The sweep lights the ground at once; only the lamp heads travel. |
| REL-118 (UI-08c) | The once-only hesitation line is pushed off by the raid's own rows. |
| REL-119 (UI-08d) | The Admin raid gets no account; Admin clear-enemies says "Raid repelled". |
| REL-120 (UI-08e) | The build panel falls to one column at 150% and 1280; the stat line crosses the border. |
| REL-11 (comment) | "Substation 0 connected" seen on screen. |
| REL-12 (comment) | "Base under attack" seen with no aliens alive. |
| REL-59 (comment) | The drawer still covers the alert column. |

## Also noted (not filed)

- The goal line reads "Need 10 Steel plates — mine Iron ore → smelt at Home." while the list also needs Copper plate 0/5. This is by design: the line names the first shortfall (`GoalCardViewModel.cs:89-95`), and the list shows both. But while a material is short, it also replaces the card's own status line (for example, "Raiders are still near the core").
- At 150% and 1280 the Engineer panel moves to the top right and drops "Backpack n/40". The goal title wraps.
- Problem lines in the no-power strip are right-aligned.
- The large "Founders Court" world label stays on screen.

## Staging scripts

`staging/` holds all 88 eval scripts as they were run, including the dead ends:

- `panels.cs` does not compile.
- `kTab.cs`, `kB.cs`, `kE.cs`, `kEscape.cs` and `kup.cs` sent key events that had no effect, because the Game view was not focused.
- `slow.cs` does not slow the sim.

They use editor-only calls and reflection. They are records of the look, not tools to reuse.
