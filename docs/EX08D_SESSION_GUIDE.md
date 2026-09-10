# Post-P8 shared playtest guide

Prepared 2026-09-08 by EX-08D. This is an **unscored diagnostic playtest** of construction tools, exploration, factories and defence. Earlier successful owner testing of the two-assembler line, tram/truck transport and defence/recovery remains credited. The original conveyor-ammo report still needs the owner's retest; automated success does not diagnose it.

## Choose a start

All four links use the frozen post-P8 build on **port 5180** and open **paused**. Press **P** or **1×** when ready. Reopening a start link resets that tab to its prepared state. Use fresh first if you want to observe discovery without the assisted starts revealing places and recipes.

| Start | Purpose and preparation |
|---|---|
| [Fresh discovery — 00:00](http://127.0.0.1:5180/?rules=exploration-v2&seed=3&view=world&state=/fresh.json) | Untouched opening, ordinary stock and no recruited crews. Recommended for self-directed discovery, Foreman recruitment and a personal construction comparison. |
| [Construction and direct-ammo check — 01:35](http://127.0.0.1:5180/?rules=exploration-v2&seed=3&view=world&state=/construction.json) | Foreman recruited, second-area station restored and powered, existing truck unlocked. Chest #4 at 372,630 holds 68 steel and 27 copper; order #3 is a six-machine ammunition cell at 373,635. Pockets hold 13 steel, 8 copper and two packed belts. Both cores have 300 HP. Home has no added defences: pause while learning, and use the defended network for longer combat observations. |
| [Prepared defence network — 39:50](http://127.0.0.1:5180/?rules=exploration-v2&seed=3&view=world&state=/network.json) | Three bases, six stocked turrets, home ammunition production, tram freight, radio and coal extraction. Natural dawn warning is ten simulation seconds away; dusk is 55:00. Home retains about 253/300 HP, other cores 300. Foreman has not been recruited. |
| [Optional Turbine restoration — 03:17](http://127.0.0.1:5180/?rules=exploration-v2&seed=3&view=world&state=/turbine.json) | Ordinary paid concrete production, carried 40 concrete, local generation and some crews already prepared. Useful for restoration/service comprehension; not unaided discovery evidence. |

The three prepared starts are scripted assistance with complete ordinary command histories: no injected stock, positions, clock, HP or rewards. You did not perform their openings. The construction cell was imported using the normal portable-blueprint command; its design is supplied help. Its earlier two-belt order is cancelled and retained in history. The network's original turret feeds use chests/inserters, so it does not by itself retest direct belt loading. Supplies still require normal mining, fuel, transfers and maintenance; these starts do not promise indefinite unattended survival.

## Construction start: optional assisted exercise

Skip this walkthrough if observing unaided tool comprehension. Record any steps read as assistance.

1. In **Truck construction**, select **Chest #4 at 372,630** and order **#3 Belt-fed ammunition cell**, then **Start truck delivery**. Unpause. The truck loads actual materials and builds the queued machines; engineer pockets stay separate. Pause whenever needed.
2. Try **Pause order / Resume order**, **Pause truck / Resume truck**, and save/load during a job. Inspect cargo and remaining parts. Stop or cancel keeps cargo and built machines. Boarding pauses automation; resume explicitly after leaving the truck. A changed destination requires cancelling the old order and queuing a new one.
3. Once complete, walk to the new **input chest at 373,635**, point at it and press **E**. Put steel and copper from pockets into that chest. The assembler uses the Shot recipe; its output inserter feeds an east-pointing belt directly into the turret at 381,636. Watch the actual magazine and turret rounds. The source chest at 372,630 is for construction and does not feed the assembler.
4. For a repeat pattern, copy complete machine footprints with **Ctrl+C** and drag, **Ctrl+V** to paste, **R** rotate, **H/V** mirror, and **Esc** cancel. Save a named library entry, export/import text, or queue another order on legal ground. It needs more real materials or packed machines. Ghosts reserve a plan but do not operate or grant free buildings. **Build remaining** uses nearby pockets; truck construction uses its selected chest/cargo. Record the time and effort of equivalent manual and tool-built work when practical.
5. Try **Select removal area**, drag whole footprints, read the preview, then explicitly **Pack selected machines**. Cancel a preview to inspect without changes. Packing needs reach and pocket capacity and can refuse busy machines, damaged defences or contents that cannot be safely returned. **Ctrl+Z/Y** applies ordinary paid undo/redo; automatic truck builds do not enter the engineer's manual undo history. Packing a completed order's buildings does not requeue it.

The prepared cell demonstrates one assembler and direct loading. It does not replace the remaining two-assembler, belted-input timing/assistance observation. Earlier owner success remains credited; repeat only to collect missing details or investigate the ammo fault. No inherited ten-minute score is applied.

## Controls, saving and continuation

WASD moves, Shift sprints, Space dodges, E interacts/repairs, B opens building, F inspects, Tab/I opens pockets, M opens the map. **Discoveries** and **Items and recipes** show current knowledge, services, requirements and recipes. **G** views a known threatened base; **K** returns to the engineer without travelling. Click/focus the canvas before keyboard shortcuts; text fields retain normal editing keys. At compact widths, scroll between the canvas and construction panels.

Use **1×** for pacing observations. Record pauses, speed changes, help and interruptions. A 30–60 minute first session is a practical booking, not a completion criterion; save and continue instead of forcing every topic into one sitting.

**Save / Ctrl+S** replaces campaign slot 1 on this browser origin; **Load / Ctrl+O** reloads it paused. Port 5180 has separate storage from 5176–5179. Before switching starts, keep an independent file with the backquote debug panel's **Download save** and **Export telemetry JSON**. Use that panel for evidence export here. Debug stock, teleports or spawned attacks would make a separately labelled assisted scenario.

Keep milestone and fault saves before changing layouts. Preserve each continuation's original build, start, command history and pauses. Do not overwrite frozen starts or this blank record. An affected historical owner save remains useful for the ammo investigation with its own provenance.

Restart from `E:\Factorio2` if needed:

```powershell
python docs/evidence/ex08d-2026-09-08/serve.py
```

The server verifies its archive and files and binds loopback only. EX-08C/5179 and EX-08B/5177 remain historical frozen checkpoints; 5178 is a mutable P8 preview. The source is the reviewed local P8-05 tree over commit `8e54ed6`, not a new pushed commit. [Exact provenance](EX08D_PREPARATION_REPORT.md).

## Observation topics

Choose your own actions. These are topics to record, not a mandatory optimal route.

| Topic | Useful observations |
|---|---|
| Discovery and rewards | Clues noticed, route choice, useful/unused capabilities, optional detours and prior knowledge. Use fresh for self-directed discovery. |
| Restoration and production | Remaining materials, required power, recipe unlocks and actual service. Record any confusion between locked, starved, unpowered and broken. |
| Construction workload | Comparable layouts/materials, active and elapsed build time, mining/carrying, setup overhead, corrections, copy/rotation/library clarity and actual repetition avoided. Separate supplied blueprint help from personal design. |
| Ghosts and truck | Real source/cargo/pocket accounting, progress/shortage feedback, reachable streets, obstruction recovery, order pause/cancel, source reassignment, manual takeover and saved continuation. Record waiting and intervention rather than assuming autonomy. |
| Removal and history | Preview clarity, contents/capacity/refusals, cancellation, packing and undo/redo; verify no loss or duplication. |
| Direct conveyor ammo | Belt arrow into turret footprint, actual magazine reaching endpoint, rounds before/after, turret and core HP, pause state, power/inserters where relevant. A magazine contains ten rounds; turret capacity is 50. Save a failure with room available before modifying it. A passing replacement layout does not diagnose the original failure. |
| Warning and defence | Warning noticed / arrived / ready times, G/K feedback, supplied outpost while absent, major start/end, personal support, interruptions, paid repair and renewed expedition. Do not deliberately lose a core just to fill a row. |
| Performance and maintenance | Chosen versus forced exploring/travel/mining/carrying/refuelling/fighting/repair time. Note stalls during lamp/pole, construction and combat actions on the actual machine. |
| Continuation | Saved inventory, orders, cargo, damage, discoveries, warnings and time; restored ability to continue an expedition. Later assaults/protected intervals may require another sitting. |

Copy the [blank record](EX08D_SESSION_RECORD.md) for each attempt. Keep observed facts, exact player comments, assistance, interpretations and missing evidence separate. Preserve crashes, invalid state, lost items and unusable controls before restarting. Record ordinary damage and recoverable losses as observations.

Settings: exploration-v2, seed 3, save schema 3, campaign metadata 10, unchanged difficulty and current provisional construction costs/rates. Day length 1200 seconds, daylight 900, first eligible dawn 40:00, dusk 55:00. No new timing, fun or hardware-performance threshold is adopted.

Prior light/power stalls and reference-machine certification remain open despite short successful automated samples. The owner supplies separate Phase 5/6/7/8 verdicts, D-SA-2/C3/D-B2-2 positions and the ammo outcome after actual play. This preparation records no human result.
