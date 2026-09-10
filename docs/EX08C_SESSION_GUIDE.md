# Post-P7 shared playtest guide

Prepared 2026-09-08 by EX-08C. This is an **unscored diagnostic playtest**, covering discoveries plus the remaining factory/defence observations. Your earlier successful factory, tram/truck and defence/recovery testing remains credited. The original direct-conveyor ammo issue still needs retesting; no fix or human Phase 7 verdict is claimed.

## Choose a start

All three links use the frozen post-P7 build on **port 5179** and open **paused**. Press **P** or **1×** when ready. Opening a start link again resets that tab to its prepared state.

| Start | Purpose |
|---|---|
| [Fresh discovery start — 00:00](http://127.0.0.1:5179/?rules=exploration-v2&seed=3&view=world&state=/fresh.json) | Recommended for self-directed exploration and discovering the new capabilities. Ordinary home stock and no recruited crews. Also suitable for the direct-conveyor retest. |
| [Prepared defence network — 39:50](http://127.0.0.1:5179/?rules=exploration-v2&seed=3&view=world&state=/network.json) | Skip rebuilding the previously tested opening. Three bases, an ammunition line, six supplied turrets, tram freight, radio and coal extraction. The first dawn warning is ten simulation seconds away; dusk is at 55:00. Existing home damage is preserved at about 253/300 HP. |
| [Prepared Turbine restoration — 03:17](http://127.0.0.1:5179/?rules=exploration-v2&seed=3&view=world&state=/turbine.json) | Optional assisted check of restoration information and the resulting power service. Concrete production, carried materials, nearby generation and some crews are already prepared. This exposes discoveries, so use the fresh start first if you want an unaided discovery observation. |

The network and Turbine starts are scripted assistance, produced from ordinary paid commands and complete logs. You did not perform their openings; they cannot establish unaided building or exploration time. The workshop/repair-tool encounter remains unfinished. The network's existing turret feeds use chests/inserters, so that start alone does not reproduce a direct conveyor connection.

## Controls and saving

WASD moves, Shift sprints, Space dodges, E interacts/repairs, B opens building, F inspects, Tab/I opens pockets, and M opens the map. **Discoveries** shows known places and current service; **Items and recipes** explains actual production, unlocks and uses. G views a known threatened base; K returns to the engineer. Warnings do not move you automatically.

Use **1×** for pacing observations, pause whenever needed, and note speed changes or help. A 30–60 minute first session is a practical booking, not a completion requirement. Save and continue rather than rushing to finish every topic.

**Save / Ctrl+S** replaces campaign slot 1 for this browser and origin. **Load / Ctrl+O** reloads it paused. Port 5179 has separate storage from 5176/5177. Before switching starts, use the backquote debug panel's **Download save** and **Export telemetry JSON** to keep independent files. The panel is for exporting evidence here; no debug stock, teleport or spawned attack belongs in an ordinary attempt.

Keep saves at warnings, faults, recoveries and session end when practical. Preserve a failing layout before changing it. Downloaded continuations should be served as separately identified files, with their original build and history retained; do not overwrite the frozen start files. A historical owner save may help the original ammo investigation but must keep its own provenance.

Restart the frozen server from `E:\Factorio2` if needed:

```powershell
python docs/evidence/ex08c-2026-09-08/serve.py
```

It verifies the archive and serves loopback only. The previous EX-08B build stays on port 5177, development on 5176. The post-P7 source commit is `8e54ed6`; full archive/start hashes are in the [preparation report](EX08C_PREPARATION_REPORT.md).

## What to observe

Choose your own route and actions. These are observation topics, not an optimal build order or mandatory checklist.

| Topic | Useful observations |
|---|---|
| Self-directed discovery | What drew you to a place; whether the clue and route were legible; destinations chosen or skipped; assistance and prior knowledge. Use the fresh start for this. |
| Useful rewards | Whether an earned capability changed a real production, power or defence decision; what you understood before using it; whether an optional detour felt worthwhile. Record unused rewards as unobserved. |
| Restoration and recipes | Whether delivered/remaining materials, required power, operational service and recipe provenance made sense. Was a locked, starved, unpowered or unused item mistaken for a broken system? Foreman blueprints/copy-paste remain unavailable until Phase 8. |
| Direct conveyor ammo | Arrow points into the turret footprint; a magazine reaches the endpoint; rounds before/after; turret/core condition and pause state. A turret accepts one ten-round magazine when it has room within its 50-round capacity. If loading fails with room available, save the layout and F-inspection before changes. A passing new layout does not diagnose the old failure. |
| Warning and preparation | Meaning of the received warning; noticed, arrived and ready times; G/K/range feedback; supplies or errands that delayed you. No target is inferred before actual received intelligence. |
| Defence and recovery | Unattended stocked outpost outcome, actual major start/end, rifle contribution, shipped stock, repairs and reasons for forced returns. Do not deliberately lose a core just to fill a row. Disclose any deliberate freight interruption. |
| Repetition and responsiveness | Time spent exploring, building, mining, carrying, refuelling, fighting and repairing; chosen versus forced travel; visible stalls during ordinary lamp/pole/build/combat actions. |
| Save/load and continuation | Inventory, damage, discoveries, warnings and clock after load; ability to resume an expedition. Later assaults/rest intervals may need a separate continuation. |

Earlier two-assembler success is credited. Repeat it only to isolate direct loading or collect missing timing/assistance details: ordinary paid Shot assemblers, steel/copper delivered by belts to input inserters, and real magazine output reaching a turret. State whether the final feed is direct or uses an inserter. There is no imported enclosure timer or new production-speed score.

## Record evidence and decisions

Copy the [blank shared record](EX08C_SESSION_RECORD.md) for each attempt. Record player/observer, machine/browser/viewport, selected start, real/sim times, pauses, help and exact comments. The observer should intervene on request or inability to continue and record wording/effect. Save unexpected duplication/loss, invalid states, crashes or unusable controls before restarting; ordinary damage and recoverable losses are observations.

Settings are exploration-v2, seed 3, schema 3, campaign metadata 9, unchanged difficulty and provisional costs/rates. Day length is 1200 seconds with 900 daylight; first eligible dawn is 40:00 and dusk 55:00. The network has already earned the supplied three-base milestone. Telemetry does not capture every interpretation or transition; keep notes and milestone saves.

Automated runs found that a reward-first route could lose home, prepared runs still took some core damage, and ordinary light/power edits could hitch. These are findings to investigate, not tuning changes or pass/fail scores. Headless measurements do not certify your hardware. Report gaps rather than extending or coaching the session to force every event.

The owner supplies separate proceed/revise/insufficient-evidence judgments, Phase 7 and Phase 6 verdicts, remaining Phase 5 positions and the ammo outcome after actual play. No observation or human approval has been filled by this preparation.
