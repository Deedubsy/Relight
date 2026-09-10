# Player playtest follow-up 02

10 September 2026 · PE-PLAYTEST-02 · owner follow-up to PE-IMPLEMENT

## Changes

- The automatic opening now follows **1 Generator → 1 Excavator → 1 Supply chest → connecting Belts**. Each building step displays its actual cost and remaining carried-material shortage; packed machines count as ready. Belt cost is per tile because the player's chosen gap determines the total. Fuel guidance asks for at least one Coal and explains choosing a loading quantity.
- The next steps are **place a turret → craft at least one magazine → load the turret**, before supplying an Assembler ammunition line. Costs, magazine yield and crafting time come from simulation constants. Home interaction says **Open Home workshop & storage**. Its existing crafting control is now prominent at the top of the inventory, with recipe, queue and progress feedback; it still consumes carried materials, needs Home proximity and produces into the Backpack.
- **No power · Founders Court** (or the other affected defended site's name) persists while its actual circuit has no generation or its core is disabled. The clock shows this separately from combat/schedule alerts; open drawers retain an outage badge. The local/full maps show crossed lightning, and the full map also labels Home **NO POWER**. Supplying fuel or repairing/reconnecting power clears the warning. Uncommissioned plants are not reported as failed bases. Power-shortage toast wording no longer says “Nothing switches off” at zero supply.
- Inspecting a Generator, Excavator, Pumpjack, processor, Turret or Cannon now includes **Backpack on the left and machine inventory on the right**. Select a stack, enter Quantity, then Load or Take; Shift-click and drag move a whole available stack. Existing chest/station transfers retain their separate physical pools. Processors expose buffered ingredients and finished output; already committed ingredients stay in the active recipe. Machine capacities and accepted items remain authoritative. Turrets convert whole magazines to rounds and only return whole magazines; loose rounds remain loaded and are disclosed.
- Drag feedback shows the **item icon and stack count centred on the mouse**, with the drop instruction alongside. Native browser dragging is suppressed. Escape/cancellation removes the preview without moving items. Plain Tab closes machine inventories, including from a quantity field. Selected stack controls scroll into view inside the panel; inventory grids are bounded at small viewport sizes. Opening a panel still does not move the world camera.

No item recipes, building prices, combat values, map placements or future-turret specifications were changed. Wire, Frame and Board remain retained under the earlier exclusion.

## Verification

Focused simulation checks cover ordered stages, packed equipment, actual belt connection, turret-before-ammunition, conservation/capacity/reach, stale source rejection, full Backpack refusal, output withdrawal, partial turret rounds, save/load, persistent outages and recovery. The prior focused checks remain included.

The isolated browser used the current CITY-F map at port 5178; owner saves were untouched. Test resources were supplied only in that disposable browser for the targeted inventory checks. Ordinary movement/E interaction and UI controls were then used. Observed:

- Home's visible crafting button consumed 2 Steel plates + 1 Copper and produced 1 Shot magazine after its existing six-second craft.
- Generator inventory loaded exactly 7 Coal and returned exactly 2. A later whole-stack drag showed 15 Coal following the pointer and transferred it on release. Fuel continued burning normally.
- Removing the remaining fuel produced the persistent named outage and map marker. The open inspection header retained its outage badge.
- Quantity typing did not move the engineer. Tab closed the paired inventory. Normal 1366×900 / 100% and small 1100×720 / 125% layouts were inspected.

Evidence: [transfer record](../evidence/player-experience-followup/inventory-review.json), [Generator inventory](../evidence/player-experience-followup/generator.png), [drag feedback](../evidence/player-experience-followup/drag.png), [outage map](../evidence/player-experience-followup/outage-map.png), [small quantity controls](../evidence/player-experience-followup/machine-small.png). Some images record an intermediate layout; the final small/map images are refreshed after the last layout refinement.

**Final checks:** production build/typecheck and lint passed; all **10 focused tests passed**. `docsync:check` passed. `freshness:check` retains the same 12 stale campaign evidence files; they were not regenerated. Referenced local paths and scoped `git diff --check` passed. The bounded mandatory `npm test` attempt recorded **46 passes and the same eight previously reported failures** before its 180-second limit: old blueprint/campaign migration and identity, Foreman/cul-de-sac geometry assumptions, minor-raid policy and the paid defence replay fixture. This is a partial regression run, not a green full suite. Historical campaign freshness findings and human/release gates remain separate.

Reload **5178** to use the rebuilt interface; existing current-city saves can be retained. Local changes are uncommitted.
