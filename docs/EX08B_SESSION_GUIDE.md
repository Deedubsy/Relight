# EX-08H-B / P6-H — shared playtest guide

Prepared 2026-09-07 by EX-08B, after the Phase 6 engineering review. This is an **unscored diagnostic follow-up**. Your previously reported successful factory, tram/truck and defence/recovery play is credited. The ammo-loading issue still needs retesting; no loading fix is claimed.

## Start here

[Open the focused defence start](http://127.0.0.1:5177/?rules=exploration-v2&seed=3&view=world&state=/network.json). It opens **paused at 39:50**, ten simulation seconds before the first dawn lock. A home ammunition line, six supplied turrets, three-stop tram freight, restored radio and coal extraction are already built. The first dusk is at 55:00: about fifteen minutes after starting this checkpoint. Press **P** or **1×** when ready.

This opening was built by a script using ordinary paid commands from fresh stock. Its full 353-command prefix is retained. It supplies a convenient defence checkpoint, not evidence that you built or discovered that opening unaided. Existing damage is preserved: Home Court is about 253/300 HP, both remote cores are 300/300; all are operational. The workshop and optional repair-tool encounter remain unfinished. Turrets already contain 43–50 rounds, so a full or nearly full hopper may wait before accepting another ten-round magazine.

[Open the fresh construction start](http://127.0.0.1:5177/?rules=exploration-v2&seed=3&view=world&state=/fresh.json) if you want to revisit direct belt loading or remaining construction observations from an empty court. It opens paused at 00:00 with ordinary starting supplies. These are separate starts; opening either link again resets that tab to its prepared checkpoint.

Use **WASD** to move, **Shift** to sprint, **Space** to dodge, **E** to interact/repair, **B** to build, **F** to inspect, **Tab/I** for pockets and **M** for the map. **G** views a known threatened base; **K** returns to the engineer. The new warning never moves you automatically. Stay at **1×** for pacing observations, pause whenever needed, and note pauses or speed changes. Roughly 30–60 minutes is a practical first follow-up booking, not a completion requirement; save and continue if needed.

## Retain your session

The build is frozen on **port 5177**. Port 5176 remains the development preview and port 5175 the earlier archive. Saves are tied to the browser and exact origin; the new port does not automatically import a save from 5176.

**Save / Ctrl+S** writes campaign slot 1 in this browser; another Save replaces that slot. **Load / Ctrl+O** reloads it paused. For separate checkpoints, open the debug panel with the backquote key, then use **Download save** and **Export telemetry JSON**. Download before closing/restarting, and at warnings, failures and session end when practical. The debug panel is only needed for export; no debug stock, teleport or attack spawning is part of this session.

To resume a downloaded file, retain it unchanged and ask for it to be loaded as a separately identified continuation. Do not overwrite the frozen start files. A pre-P6 save can be used for the original ammo investigation, but record its source/build and compatibility limits; it is not silently substituted for either prepared start.

If the server needs restarting, from `E:\Factorio2` run:

```powershell
python docs/evidence/ex08b-2026-09-07/serve.py
```

It verifies the archive, serves only loopback and leaves the old servers alone. Ctrl+C stops it.

## Focused observations

Choose your own actions; the following topics tell the observer what evidence is missing, not an optimal construction or exploration sequence.

| Topic | What to record |
|---|---|
| Direct conveyor loading | Belt arrow points into the turret footprint; magazine actually reaches the endpoint; turret rounds before/after; core/defence state; whether the belt is moving. A turret accepts ten rounds at once up to 50. If it fails with room available, save before changing the layout and capture F-inspection/status. |
| Warning and preparation | What you thought the target/warning meant; sim time when noticed, arrival and ready time; use of G/K and range feedback; supplies or errands that delayed you. No target or outcome should be inferred before the warning. |
| Remote minor raid | Which stocked outpost was left unattended, ammo/damage afterward and any forced return. A quiet interval or an already disabled base is not a supplied hold. |
| Major and personal support | Actual assault start/end, layout and supply changes, rifle contribution and whether it felt worthwhile or compulsory. The 3–5 minute diagnostic target is not a pass/fail score. |
| Recovery and freight | Clear reasons for missing ammo/fuel/materials; actual stock shipped and retained; paid repair actions; whether the older factory remains useful. If deliberately cutting a route, preserve the pre-cut save and disclose it. Do not require a core loss just to complete a checkbox. |
| Maintenance and responsiveness | Time spent travelling, mining, refuelling, moving magazines and repairing; chosen versus forced returns. Note visible stalls during ordinary pole/lamp/power edits, build gestures or combat. |
| Save/load and renewed exploration | Correct inventory, damage, warning and clock after load; whether you can choose another expedition after recovery. Full protected-rest coverage may need continuation. |

The ready-made checkpoint uses chest/inserter turret feeds. It does **not** retest your direct-conveyor arrangement by itself. Build or revisit a direct feed deliberately and record that observation separately.

## Reconciled factory exercise

The earlier two-assembler exercise is already owner-confirmed successful; do not require repeating it as if no evidence exists. If collecting its missing timing/assistance details or isolating ammo loading, use the **fresh start** and label the attempt. Establish two ordinary paid Shot assemblers with steel and copper carried by belts into their input inserters, take their magazine output onto a conveyor and into a turret (direct endpoint or explicitly recorded powered inserter). Record material acquisition, power, layout changes, completion time and help. Both assemblers making magazines and actual magazines entering the turret establish the technical event; appearance of a connected belt alone does not.

There is no imported 75-minute enclosure goal, no new production-speed score and no granted construction stock. Current recipe selection stays Shot on both assemblers; power shortage slows production. If you use an existing factory or the prepared network instead, record that setup and do not label it construction from scratch. Owner Phase 5 exit and individual D-SA-2/C3/D-B2-2 positions remain explicit decisions, not inferred from a timer.

## Observer rules and fixed settings

Use a copy of the [blank record](EX08B_SESSION_RECORD.md). Session ID: EX-08H-B, with suffixes for fresh-start, network-start and later continuations. Record player/observer, actual viewport/machine/browser, real and sim times, exact comments, help, pauses, speed changes and build/start identifiers before interpreting results. Do not fill observations from automation. Intervene only on request or inability to continue, recording wording and effect. Unexpected duplication/loss, invalid saves, crashes or unusable controls call for preserving the evidence before restarting; ordinary damage, knockdown or recoverable loss are observations.

Profile exploration-v2, seed 3, schema 3, campaign metadata 5, defence metadata 2; unchanged default difficulty. Day length 1200 seconds, daylight 900; first eligible dawn 40:00, dusk 55:00. One locked target, finite 60-crawler major; shared minor opportunities at five/ten minutes into each cycle. Radio is restored in the network start and unrestored in the fresh start. The network already earned the third-base supply milestone; it changes the rest after completion, not the already promised first attack. Full constants and inventories are in [settings](evidence/ex08b-2026-09-07/settings.json).

Serialized/browser start hashes: fresh **ab6af3ec / 6e541bc4** (ordinary zero bookkeeping normalization); network **aa2be5bc / aa2be5bc**. Source/build hashes are in the [preparation report](EX08B_PREPARATION_REPORT.md). Both loads are paused and preserve complete logs.

Telemetry includes state and command history but does not capture every campaign transition or player interpretation; major history is bounded. Keep milestone saves and notes. New campaign logs can be analysed against the frozen source; do not apply legacy frontage/enclosure verdicts. Older defence histories may be incomplete for current-code replay, and old surveys are preserved rather than automatically rerouted.

Known light/power frame stalls and the reference-laptop/GPU gap remain. Local headless timing is not certification of your machine. Report unobserved topics, including optional discovery, scheduled versus restoration-nominated selection and full rest intervals, rather than forcing a long or coached session. The owner supplies separate proceed/revise/insufficient-evidence judgments and Phase 5/Phase 6 positions after actual play.
