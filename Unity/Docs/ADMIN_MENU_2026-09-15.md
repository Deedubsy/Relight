# Debug admin menu — 2026-09-15

The owner requested an in-game menu for granting resources, spawning enemies and other useful playtest controls. The menu is implemented in the existing Unity UI Toolkit interface, with the shared industrial appearance. TASKS.md alone owns implementation status; human campaign acceptance remains separate.

## Controls and tools

Press **F8** or the visible **Admin [F8]** HUD button. F8 or Escape closes the drawer. Opening it leaves the simulation running; the World tab can pause it and all admin actions remain usable while paused. Closing a paused admin drawer returns to the normal pause menu. Save/load/settings/confirmation screens retain their input priority. Tab and number entry in admin fields do not activate Backpack or gameplay shortcuts.

| Tab | Tools |
|---|---|
| Supplies | Search resources and packed structures; grant a chosen quantity; starter supplies; grant any defined weapon as a real owned Backpack item. Steel is selected initially. |
| Enemies | Select a defined enemy type, count, approach and distance; spawn on clear nearby ground; trigger a normal mixed-roster raid at Home; freeze enemy movement and attacks; clear living enemies, hostile projectiles and scheduled assault remainders without kill rewards. |
| Player / Base | Invulnerability; heal/revive; return to clear ground near Home; repair Home and placed defences; fill generator fuel; refill owned weapon magazines and placed turret hoppers. |
| World | Pause/resume at normal speed; force daylight or night; follow the natural clock; reset all temporary overrides. |

The footer reports each command's actual result or refusal and shows simulation state, enemy count, power supply/demand and position. Active invulnerability, enemy freeze and lighting overrides remain visible on the HUD after the drawer closes.

## Behaviour and boundaries

- Resource grants accept 1–10,000 requested items and add only what fits the real Backpack. Partial grants report actual/requested counts. Packed structures use ordinary inventory and placement. Weapons are unique owned instances and can be equipped through Backpack.
- Starter supplies request 50 steel, 50 copper, 50 coal and 100 bullets. Fuel and ammunition actions respect each real capacity. Granted stock is accounted for explicitly in the inventory ledger without inventing mining/production or concealing an existing discrepancy.
- Nearby spawns accept 1–100 enemies, a distance of 3–30 tiles, and a cardinal approach. A bounded clear-ground search avoids occupied enemy tiles and the immediate three-tile area around the engineer. Direct spawning stops at 250 live enemies and reports partial results. The raid action uses the existing raid director and its normal mixed roster rather than the nearby type selector.
- Invulnerability, enemy freeze and forced lighting are transient per-session overrides, reset by load/new game or Reset overrides. Forced lighting affects light/night rules but does not skip simulation time or raid timers. Reset does not undo inventory grants, spawned actors or repairs.
- Grants, spawns, healing, repairs and other persistent edits affect the current simulation and are included in normal saves. A successful persistent admin action marks progress unsaved even while paused, so existing leave/load confirmations remain truthful. The admin tool itself does not write a save.

## Implementation

`Assets/Relight/Sim/Admin/AdminCommands.cs` implements validated simulation commands and transient admin state. The composition registers the handler; engineer damage, enemy updates and lighting queries read the scoped overrides. `SessionDirty.MarkChanged()` supports changes made without a tick. The serialised save schema is unchanged.

`Assets/Relight/UI/Admin/` contains the UXML, USS and controller. The drawer joins UiShell's existing panel ownership, protects integer/text entry, cooperates with PauseMenu, and refreshes ordinary inventory presentation after an action. GameUI includes the template and controller; SceneSetup retains the component when authoring scenes. The scene component was added and saved through Unity Editor APIs.

## Verification actually performed

- Unity 6000.6.0f1 recompiled successfully after the final source changes.
- **15/15 AdminTests passed**: validation and refusal without partial mutation, Backpack limits, conservation and pre-existing discrepancy preservation, packed placement, weapon ownership/equipment/refill, save round-trip behaviour, clear spawn positions, scoped invulnerability, freeze/lighting, and fuel/ammo capacities. [Recorded results](evidence/admin-menu/admin-tests.json).
- **7/7 SessionDirtyTests passed**, including paused edits becoming dirty and save/reset clearing that state. [Recorded results](evidence/admin-menu/session-dirty-tests.json).
- Live Editor Play Mode checks used queued Input System keyboard events and UI Toolkit navigation-submit events on the actual buttons: F8/Escape, safe numeric/Tab input, grants, weapon creation/refill, pause coexistence, enemy spawns, raid creation/cancellation, override reset, conservation, and scrolling to the last Supplies action at 1280×720 with 125% UI scale. [Runtime assertions](evidence/admin-menu/runtime.txt).
- A final live check verified Steel is initially selected and a grant while paused marks unsaved progress without advancing a tick. [Paused-save assertions](evidence/admin-menu/paused-save.txt).
- Actual Game View captures were visually inspected at 1080p and 720p / 125% scale. [Supplies, 1080p](evidence/admin-menu/supplies-1080.png), [Enemies, 1080p](evidence/admin-menu/enemies-1080.png), [Supplies scrolled, 720p / 125%](evidence/admin-menu/supplies-720-scale125.png).

Player save writes were disabled during these isolated live checks. Test input preferences, Game View size and UI scale were restored, and Play Mode was stopped. Native OS mouse interaction, standalone builds and full campaign acceptance were not exercised by this pass.

Documentation verification: `npm run docsync:check` and `npm run freshness:check` were attempted; both could not start because the local `tsx` executable is missing. The report's relative links resolve. Scoped `git diff --check` reported only Unity-serialised empty YAML fields with trailing spaces in GameUI.unity, plus existing nested .gitattributes warnings; scene YAML was not hand-edited to remove Unity's formatting.
