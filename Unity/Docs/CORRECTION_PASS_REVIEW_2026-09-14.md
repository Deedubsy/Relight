# Correction-pass review — 2026-09-14

Review of Fable's two supplied handoff summaries, current Unity source, and the running World + GameUI scenes. This is review evidence and a proposed repair order; `TASKS.md` remains the migration status owner. No gameplay implementation was changed during this review.

The pass materially improves the interface, but it is not ready for acceptance as a complete correction. The most urgent new finding is that save relocation is bypassed in the actual scene composition.

## Findings

### P1 — The live world drops region metadata and silently skips old-save relocation

`Assets/Relight/World/SiteBridge.cs:24` constructs `new WorldSites(records)` without the imported region ID or origin. `Assets/Relight/Presentation/WorldBootstrap.cs:122` uses that bridge to compose the live simulation. Although the bootstrap uses the full 864 × 576 map, `Context.Sites.RegionId` is empty.

`SaveRegion.Of` consequently returns an unknown region. `SaveRelocate.Needed` requires both regions to be known, so loading a Home-crop save returns success without applying the required +43/+91 offset. Saves written by the live context also contain an empty region record.

Reproduced in Play mode using the existing `phase-c/integrated-run/slot-phasec-int.json` evidence file, loaded into memory through the live context without adopting it into the session or writing the file:

| Check | Observed | Required |
| --- | --- | --- |
| Live and newly written region | Empty ID, zero dimensions, Known=false | Full map region and dimensions |
| Load result | Accepted, empty failure reason | Accept only with correct relocation |
| Loaded engineer position | (30.5, 264.5) | (73.5, 355.5) |

Evidence: [proof.json](evidence/correction-review/proof.json). `oldEngineer` in that output is the loaded result.

The existing relocation tests construct their own region-aware `WorldSites` (`Tests/Sim/Persistence/SaveRelocateTests.cs:256`), bypassing the broken bridge. They therefore do not establish that the real scene migrates correctly.

Proposed fix: carry the actual imported region metadata through bootstrap composition. Add an integration test using the real World scene and an old Home save, plus a new-save round trip. Account for version-4 saves already emitted without region metadata; ambiguous files must not be silently assumed to use one coordinate system.

### P2 — Inventory still processes the same global keys as WorldInput

`Assets/Relight/UI/Inventory/InventoryPanelController.cs:171` polls Reload and Slot on every update, including while the drawer is hidden. `Assets/Relight/Presentation/WorldInput.cs:225` also handles those global actions.

Consequences visible in the code paths:

- R can submit an inventory reload as well as rotate a placement ghost or placed machine. In the reload branch it can submit two reload commands.
- Number keys select the WorldInput tool while inventory also treats the bar entry as equipment (`InventoryPanelController.cs:535`). Machine entries therefore produce an inappropriate equip request.

Proposed fix: give global keyboard actions one dispatcher; keep inventory button actions explicit. Verify one command per press, R with a weapon and machine ghost, R over a rotatable machine, machine and weapon bar entries, and the same actions with Backpack open. This finding is established from the competing source paths; a physical-key reproduction was not completed in this review.

### Remaining functionality still blocks a complete opening loop

These are acknowledged omissions in Fable's handoff, rather than newly discovered claims of completion:

- E still follows the weapon-equip path rather than contextual interaction.
- `InventoryPanelController.OpenStore` has no callers in UI/Presentation. Machine storage, including ordinary fueling and transfer access, remains disconnected.
- UI/Presentation has no callers for `SetRecipeCommand` or `PlaceUndergroundPairCommand`.
- Workshop crafting is now reachable through Backpack; the earlier blanket claim that crafting is inaccessible should not be carried forward.

## What improved, and what still looks unfinished

At 1920 × 1080 the HUD host occupies the viewport, the engineer block fits at the bottom left, and the opening guidance and drawer controls are visible. Build and Backpack both open through their actual UI buttons. The Build catalogue presents categories and resource shortages. Backpack exposes the workshop and inventory in a contained drawer.

Screenshots inspected: [HUD](evidence/correction-review/hud.png), [Build](evidence/correction-review/build-ui.png), [Backpack](evidence/correction-review/inventory-ui.png).

Backpack still needs a readability pass: Copper wraps across lines in a narrow slot, and stack quantities are not visible in the captured occupied slots. The world remains dominated by flat geometric art, while machine identity still depends on placeholder rendering. The full map is present; map coverage should no longer be conflated with finished visual fidelity.

## Proposed repair order

1. Fix region propagation and test old-save migration through the real scene; resolve duplicate keyboard handlers.
2. Complete contextual E, storage opening, fuel/item transfer, recipes and underground placement. Prove the opening with ordinary player inventory and no debug grants.
3. Replace placeholder machine/item art and polish inventory readability. Compare matched Phaser/Unity scenes, preserving the user's intentional removal of click-to-move.
4. Run the relevant automated suites and a standalone player smoke test, then a user playtest of the full opening loop. Check additional viewport sizes and full-city performance.

## Verification and limits

This review used live Editor Play mode at 1920 × 1080, UI submit events on the real Build/Backpack buttons, source inspection, and a focused in-memory serializer reproduction. Low-level keyboard injection did not establish end-to-end key behavior, so Tab/B/R/E/Escape physical-key acceptance is not claimed. The complete EditMode/PlayMode suites and a standalone player build were not rerun; the 563 EditMode and 55 PlayMode passes with five skips remain Fable's reported results.

Temporary input settings were restored and Play mode stopped. World and GameUI were restored as the two open edit scenes, both clean. Runtime autosave and save-on-quit were disabled for the review; no reference save was rewritten.

Documentation checks: `docsync:check` and `freshness:check` could not execute because the local `tsx` command is unavailable. Referenced report paths were checked separately.
