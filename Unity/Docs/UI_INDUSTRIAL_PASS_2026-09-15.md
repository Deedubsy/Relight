# Illustrated industrial UI pass — 2026-09-15

Implemented against the owner's supplied two-view reference in the existing Unity project. The image supplies the art direction; all recipes, quantities, equipment slots and completion conditions remain the game's own. TASKS.md owns status. C-ACC remains pending for the owner's playtest.

## Delivered

- Painted charcoal/olive equipment-case surfaces, worn brass edging and fasteners, ivory enamel headings, recessed item slots, amber selection rims and mint operational meters. Shared surfaces extend to Build, machine inspection, tooltips, pause, Save, Load and Settings.
- Shallow permanent ten-slot dock with Build [B] and Inventory [Tab] beside it. Existing assignment/swapping, weapon identity, loaded counts and construction availability remain live. Selection overlays ignore picking so they cannot interrupt clicks or drags.
- A narrow Backpack case, wider paired storage view, dense independently scrolling item grid and the two actual weapon equipment slots. Equipment captions stay inside their slots; the footer retains quantity, transfer, split, move and sort controls.
- Reusable recipe equations with separate ingredient illustrations, live have/need quantities, output, duration, Craft, progress and Cancel. Workshop can expand beneath Backpack or use its focused view on short screens. The final recipe remains reachable by scrolling at 720p/125%.
- A maintenance ticket that stays visible beside drawers. Material ticks and small meters come from actual available/required values, not sample reference quantities. Detailed explanation remains expandable.
- A target-adjacent inspection label, projected from the actual hovered tile/footprint, flipped/clamped to the viewport above the dock. Mining uses the real simulation fraction and existing resource-gain notices. Power labels distinguish supply, demand, disconnected and unfuelled states; the meter shows delivered demand fraction.

## Art and editing

Nineteen generated transparent PNG assets live in `Assets/Relight/UI/Art/Industrial/`:

| Asset | Use | Import / slicing |
|---|---|---|
| `panel-frame.png` | HUD tray, status, ticket, drawers, tooltips and menus | Cropped sprite rect; 110px borders; USS slice scale 0.12 |
| `item-slot.png` | Dock, inventory, equipment and recipe ingredients/output | Cropped sprite rect; 95px borders; slice scale 0.07 |
| `enamel-plate.png` | Live section and inspection headings | 130px left/right, 65px top/bottom; scale 0.10 |
| `button-face.png` | Shared button family | 95px left/right, 65px top/bottom; scale 0.10 |
| `selection-rim.png` | Selected dock, inventory and equipment slots | Transparent centre; separate non-pickable overlay |
| `Items/*.png` (14) | Steel, copper, coal, bullets, Rifle, Generator, Excavator, Chest, Belt, Pole, Assembler, Turret, Lamp and core | Bilinear, no mipmaps, uncompressed, 512px maximum import size |

The five frame PNGs retain their original pixels; Unity's Sprite Editor data provider supplies crop rectangles and fixed borders. All have real alpha; no text or item counts are baked into them. Meter geometry uses USS and real UI Toolkit fills. Generated source files remain at their original locations. Built-in image generation was used, one individual asset per call. [Prompts and provenance](evidence/industrial-ui/generation-prompts.json), [frame import metadata](evidence/industrial-ui/imported-art.json), and import helpers are retained with the evidence.

The existing 60-entry `UI/Icons/Resources/RelightItemIcons.asset` now points fourteen exact keys to the new art (`bullet.png` binds the existing `magazine` key, whose player-facing name is Bullets). The other 46 catalogue icons retain the existing reference artwork; a full late-game item-art replacement was not performed.

Edit the shared appearance in `Assets/Relight/UI/Styles/industrial.uss`, imported after component sheets. Base palette variables remain in `Styles/tokens.uss`. Layouts are in `Hud/Hud.uxml`, `Inventory/InventoryPanel.uxml`, `Workshop/RecipeCard.uxml` and `Guide/GoalCard.uxml`. State binding remains in their existing controllers and `Hud/GameplayDock.cs`. No simulation, save-schema or world-art source was changed in this pass.

## Actual Unity evidence

These are Game View captures, not generated mockups. HUD/mining use the earlier ordinary-stock opening snapshot. Inventory/storage/workshop use the explicitly prepared inspection snapshot from the previous pass, with extra materials and an owned Rifle; their stock is not new-game stock.

- [HUD, 1920×1080](evidence/industrial-ui/hud-1080.png)
- [Backpack and workshop, 1920×1080](evidence/industrial-ui/workshop-1080.png)
- [Target inspection and mining](evidence/industrial-ui/mining-1080.png)
- [Paired storage](evidence/industrial-ui/storage-1080.png) and [machine inspection](evidence/industrial-ui/machine-1080.png)
- [Build, 1280×720](evidence/industrial-ui/build-720.png) and [placement/refusal](evidence/industrial-ui/placement-720.png)
- [Backpack, 720p/125%](evidence/industrial-ui/inventory-720-scale125.png), [focused workshop](evidence/industrial-ui/workshop-720-scale125.png), [last recipe scrolled into view](evidence/industrial-ui/workshop-last-recipe-720-scale125.png)
- [Tooltip at viewport edge](evidence/industrial-ui/tooltip-720-scale125.png), [Save](evidence/industrial-ui/save-720-scale125.png), [Load](evidence/industrial-ui/load-720.png)

## Verification and limits

Unity 6000.6.0f1 compilation passed. Existing `OpeningObjectiveTests`: **12/12 passed**, including empty-third-turret/loading and completed-three-turret progression. No full simulation or PlayMode suite was rerun.

Live checks passed for matching hover/mining target, clamped label, actual resource gain, Tab, material tick truth, storage ownership, loading seven steel and taking three back with conserved stock, Craft/progress/Cancel with refunded ingredients, selected weapon rim, fresh-spawn held movement, B, dock assignment without spending stock, placement mode, Escape ordering, Pause/Load/Resume, actual Settings scale 125%, panel separation and the last recipe's scroll reachability. UI pointer/navigation events and queued Input System input were used; native OS mouse testing is still an owner check.

The first selection assertion ran before the 100ms HUD refresh; waiting for the existing refresh resolved it. Movement assertions against both reused inspection saves initially failed: those saves place a machine under the engineer. The input diagnostic confirmed correct walking intent; a fresh-spawn check passed. These fixture failures remain in the evidence logs and were not treated as UI defects.

Visible defects corrected during inspection: inherited recipe stretching, objective hidden by an open drawer, clipped equipment caption, spurious horizontal inventory scrollbar and low-contrast placement refusal text. At small sizes, equipment and later recipes require vertical scrolling or Workshop focus; controls remain reachable.

Scoped whitespace and local artifact/link checks pass. `npm run docsync:check` and `npm run freshness:check` could not start because `tsx` is missing, matching the earlier environment limitation. No dependencies were installed for this visual pass.

World scenery still uses the existing placeholder presentation. No standalone player build, native mouse pass, complete campaign or owner acceptance is claimed. Runtime inspection disabled autosaves and did not write player save slots. The original edit-mode scene setup and viewing preferences are restored at handoff.
