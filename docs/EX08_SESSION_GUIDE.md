# EX-08H session guide

> Scheduling update, 2026-09-07 (D-EX-20): human gameplay testing is deferred until Phase 5 factory implementation is ready. This document preserves the EX-08A checkpoint; EX-08B must prepare an updated build and session documents before EX-08H. No human play or phase approval is recorded.

Prepared 2026-09-07 for **EX-08H-A**. Human play has not started. Use the frozen build described in [the preparation report](EX08_PREPARATION_REPORT.md); gameplay and tuning remain unchanged from EX-07.

## Player quick start

[Open the paused playtest](http://127.0.0.1:5175/?rules=exploration-v2&seed=3&view=world&state=/start.json). The local server must be running. It is open in Codex at handoff. To restart it from the repository’s PowerShell terminal:

```powershell
python docs/evidence/representative-loop-2026-09-07/serve.py
```

This serves the frozen archive, even if development continues elsewhere. Ctrl+C stops the server. Keep using this exact host/port: browser saves are tied to the origin. The archive is checked against its manifest before serving.

The game starts paused at Home Court with its ordinary initial supplies. Press **P** or **1×** to begin. Use **WASD** to move, **Shift** to sprint, **E** to interact, **B** for building, **Tab/I** for pockets, **M** for the map and the mouse wheel to zoom. Explore, establish a supplied home, and choose where to go next. Say what you are trying to do and when something is unclear. There is no prescribed optimal route.

Stay at **1×** during measured play. Pause whenever needed; record breaks. Expect the first major attack at 55 minutes of simulation time, provided the home remains operational and its approach is available. Allow roughly 90–120 minutes initially, with saved continuation if needed. This is a booking estimate, not a required completion time. Do not rush the build to meet it.

To retain a session, press the backquote key (the **`** debug-panel toggle), scroll to the summary and click **Download save** and **Export telemetry JSON**. Download these at stopping points before closing or reloading. The top **Save** button also uses the campaign’s browser slot, overwriting that slot’s previous save; downloadable files preserve separate checkpoints. **Load** restores that browser slot. Opening the start link again begins a fresh run, not the previous session. Label each restart or reload in the record.

## Observer setup — read before play

Use a copy of [the blank record](EX08_SESSION_RECORD.md). Keep the remainder of this guide with the observer, so coverage prompts do not tell the player where to explore before they choose.

| Setting | Prepared value |
|---|---|
| Session | EX-08H-A, primary ordinary play |
| Build | Frozen `playtest-build.zip`; exact hashes and source archive in the preparation report |
| Profile / seed | exploration-v2 / 3; riverside city, culdesac-v1 |
| Save / metadata | Schema 3 / campaign version 5 |
| Start | Tick 0, paused, no injected items, progression or elapsed time; complete empty command log |
| Difficulty | Current campaign defaults; no separate difficulty selector |
| Starting supplies | House: 200 steel, 100 copper, 50 stone, 40 coal, 20 magazines; existing generator has another 40 coal; pockets empty |
| Clock | 20-minute cycle: 15 daylight, 5 night; unchanged during play |
| Major schedule | First eligible dawn lock at 40:00, first dusk at 55:00; one target and 60 crawlers at four-second intervals |
| Major rest | At least two full cycles after actual finish; one after the three-base actual automated-resupply milestone; dawn/dusk alignment can lengthen it |
| Minor raids | Shared opportunities at 5 and 10 minutes into a cycle; suppression during active major combat/recovery and no catch-up |
| Radio | Unrestored initially. Home-only major targeting until restoration; powered radio receives the true target; paid upgrade adds approach/composition |
| Tuning | HP, costs, rates and tool behaviour remain provisional; full values in settings.json and CAMPAIGN_RULES.md |

The interface calls this “Scenario B” because it loads a file. This file is the untouched fresh campaign at time zero, not an advanced construction fixture. Its serialized state hash is `ab93bc27`; the browser reports `21b1fa9f` after its ordinary zero-valued bookkeeping normalization. Both are before any gameplay. The first paused frame can show stale zero power counters; record persistent confusion after starting rather than interpreting that initial frame as depleted coal.

## Evidence rules fixed before observation

This is an **unscored diagnostic session**. No new numerical fun/speed pass thresholds are being adopted. Record elapsed times, comments and behaviour; the owner later chooses proceed, revise or insufficient evidence. Missing coverage remains explicitly unobserved and cannot support a positive conclusion.

Let the player choose their route and preparation. Intervene only when they cannot continue or request help. Record the exact prompt, reason, real/sim time, what happened next and whether success depended on the help. Do not silently supply stock, finish installations, move the engineer, spawn threats or alter the clock. If an assisted/debug follow-up is needed, give it a separate session ID and disclose every change; it cannot measure ordinary pacing.

Stop for a crash, invalid save/profile, unrecoverable input or apparent item duplication/loss. Preserve files and observations first; resume on a separately identified build if a fix is required. Ordinary knockdown, core loss and successful recovery are observations rather than automatic session failure.

## Coverage and checkpoints

Use this list to note coverage, not to coach each action. Capture downloadable save + telemetry at major milestones, warnings and session end. Notes/screenshots supply player interpretation; saves supply state. If exporting would interrupt combat, pause and record that interruption.

| Coverage | What to retain |
|---|---|
| Opening and early payoff | Time to first useful production/restoration; what the player thought it unlocked and then used |
| Independent expedition | Chosen destination and reason; clues noticed or missed; forced returns and assistance |
| Tram / radio / district factories | Actual home-to-outpost supplies and returning goods; station requests/reserves, starvation and corrections; radio availability |
| Optional discovery and distinctive encounter | Whether found unaided; avoidance/distraction/turret/rifle choice; wind-up/leash comprehension; what the tool changed |
| Routine raid while away | Base, warning noticed/ignored, automated outcome, ammunition/repair work and any compelled return |
| Major warning and defence | Save at the lock/warning when practical; target interpreted, travel/preparation time, wall/turret/ammo layout, personal actions, actual start/end |
| Recovery and renewed expedition | Damage, repair/material handling time, retained goods, contribution of the earlier factory; whether the next expedition was voluntary |
| Protected rest and shorter-rest milestone | Observe from actual major finish to the next major if feasible; record milestone time, pre-existing promises and later lock/start. A ten-minute quiet sample is not evidence of a whole protected interval |
| Scheduled versus restoration nomination | Record actual restoration time and pending/locked target; do not infer an immediate extra attack from restoration |

Restoration feeds the shared dawn nomination system; it does not create a second overlapping attack. To cover both a schedule-selected target and a restoration-selected target, continue normal play through eligible locks. If the primary session cannot distinguish them, retain the gap and use a labelled follow-up from the player’s own pre-lock save: one branch without a new restoration, another with an ordinary eligible restoration before dawn. Keep costs, power, time and commands normal; record both branches separately. A restoration after a lock cannot reroll that lock. Prior automated scheduler fixtures are supporting mechanics evidence, not human observations.

The optional cache may remain undiscovered during the independent portion. Record that fact before giving any coverage prompt; subsequent discovery is assisted evidence. If the session ends before an assault, recovery or another expedition, save and continue later at 1×, or report those topics as unobserved. Accelerated runs may inspect mechanics but do not establish the adopted rhythm.

## Recording and export limitations

Record intervals in real time and simulation time, using one primary activity: exploring, travelling, building, fighting, maintenance/supply, waiting or paused. Tally these only after the session; retain mixed/uncertain intervals as such. Record the player’s explanation of rewards and failures verbatim where possible.

Telemetry retains commands, speed changes, minute summaries and the final state, but its event tables are largely inherited from the old frontier game. It does **not** directly record every campaign warning, restoration, repair and raid transition. The final defence history is capped at 32 majors. Download milestone saves and use the observer timeline; do not derive complete warning comprehension or maintenance time from telemetry alone. Legacy frontage/enclosure/rifle-verdict fields are not revised-loop acceptance.

The old root `npm run replay` CLI is legacy-only and must not be used to certify this campaign. Campaign command replay is covered by the current session/simulation tests; preserve the downloaded save’s complete log and the frozen source archive for subsequent campaign analysis. A reload, missing log, changed build or debug edit must be disclosed.

At the end, leave observed facts, player comments, interventions, unobserved coverage and proposed revisions in separate sections of the record. The owner supplies the gate decision. Automated preparation does not mark EX-08H complete.
