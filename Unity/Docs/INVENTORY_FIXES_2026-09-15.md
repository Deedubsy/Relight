# Inventory slots and drag corrections — 2026-09-15

The owner reported that machines/generators showed no empty inventory slots and that dragging items did not behave naturally. This pass fixes the storage presentation and complete drag interaction. TASKS.md alone owns implementation status.

## Result and controls

- Generators show empty coal and refined-fuel slots, with current amounts and the **shared** fuel capacity. Faint item icons identify accepted fuel without inventing stock.
- Processors show labelled recipe inputs and take-only output slots, even before they hold anything. Input/output limits come from the existing production rules. Extractors expose their one-item output, and turrets expose their ammunition slot.
- Empty chests show open storage cells and the existing shared item capacity. These cells are views of the machine's pooled contents; this pass does not add a new physical-slot limit or save a chest arrangement.
- The inventories appear first in the drawer. Machine configuration follows below and remains accessible by scrolling. Complete slots are visible immediately at 720p.
- Left-drag moves Backpack stacks, swaps different Backpack items, merges matching stacks, transfers between Backpack and compatible machine storage, and drops into the chosen Backpack slot including the last scrolled slot.
- Shift-click quick-transfers a stack. Existing quantity, split, move and right-click stack actions remain available. Packed structures and owned weapons can be dragged onto the existing hotbar as shortcuts.
- The dragged item icon and quantity follow the pointer in the full-screen overlay. Valid targets highlight; incompatible fuel/input/output targets show a red outline and a reason. The preview clamps after its text/layout changes as well as while moving.
- Escape, drawer close and panel replacement cancel a drag without transferring items. A successful inventory action marks unsaved progress even without a simulation tick.

For an established convention, Factorio documents Shift-left-click as a selected-stack transfer between inventories; that is the quick-transfer convention retained here. [Factorio controls](https://wiki.factorio.com/Controls).

## What changed

`InventoryViewModel.ReadStore` previously emitted only occupied chunks. Empty generators therefore had no slot buttons and no empty-slot drop target. It now creates typed fuel/input/output/ammo cells and empty chest cells, while reading all amounts and limits from the simulation. Counts and actual transfers remain governed by existing inventory commands.

`StackDragManipulator` now positions its preview in the preview parent's coordinate space, and the preview sits on the full-screen inventory template host rather than inside the clipped drawer. The item icon is visible while dragging. Geometry changes reclamp the preview, so a longer label cannot move its right edge outside the screen. Drop cleanup completes before the resulting repaint, and UiShell cancels drags when replacing/closing panels. The unnecessary runtime usage-hint reassignment was removed; no exception from that assignment was observed in this Unity version.

`InventoryPanelController` supplies slot roles, placeholder icons, counts/limits, acceptance previews and output-only refusal. Its successful transfer/general-command paths mark session progress dirty. `InventoryPanel.uxml/.uss` provide the additional labels/preview and put the inventory pair ahead of machine settings. No simulation rules, capacities, recipes or save schema were changed.

## Actual verification

- Unity 6000.6.0f1 compiled the C# changes successfully and imported the final UXML layout.
- **6/6 TransferTests passed**, covering inventory transfer targets and validation. [Recorded results](evidence/inventory-drag/transfer-tests.json).
- Full synthetic UI Toolkit pointer-down/move/up sweeps exercised the actual manipulators, capture, `panel.Pick` hit testing and drop handlers, rather than calling only the drag endpoints. Tests covered generator deposits/withdrawals, matching-stack merge, different-item swap, invalid inputs, the preview and target outlines, cancellation/close, exact final-slot drops after scrolling, Shift-click, empty chests, processor inputs/output-only refusal and packed-building hotbar assignment. [Main gestures](evidence/inventory-drag/gestures.txt), [additional gestures](evidence/inventory-drag/additional.txt).
- The additional sweep found a preview resize/clamp defect, which was fixed and the sweep rerun successfully. The first 720p processor capture exposed clipping caused by settings preceding inventory; inventories were moved first and actual visible-slot bounds and drops were rechecked. [Final layout, transfer and paused-save checks](evidence/inventory-drag/layout.txt).
- Actual captures were visually inspected: [drag preview at 1080p](evidence/inventory-drag/drag-preview-1080.png), [processor at 720p](evidence/inventory-drag/processor-720.png), [generator at 720p](evidence/inventory-drag/generator-720.png). The preview capture predates the final inventory/settings reorder; final 720p captures show the shipped ordering.
- Test machine placement and item grants were isolated runtime setup with save writes disabled. Input preferences and Game View size were restored; Play Mode was stopped. No scene or asset YAML was hand-edited.

The queued-device mouse attempt did not drive this Editor's UI, so the complete gestures above used synthetic UI Toolkit pointer events on the live panel. Native OS mouse acceptance and a standalone build were not run; the owner's normal playtest remains the final interaction acceptance. C-ACC remains human.

Scoped `git diff --check` and report-link checks passed; Git printed existing nested .gitattributes warnings. `npm run docsync:check` and `npm run freshness:check` were attempted but could not start because the local `tsx` executable is missing. The three completed live helpers record 44, 44 and 17 passing assertions respectively. A final defensive preview guard labels packed structures as Backpack-only for machine storage, matching the existing transfer restriction; they still support hotbar assignment.
