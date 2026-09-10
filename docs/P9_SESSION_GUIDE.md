# P9 — frozen city playtest guide

Prepared 2026-09-09. **Human play: NOT RUN.** Use the [blank record](P9_SESSION_RECORD.md) for actual observations. [Engineering report](P9_05_READINESS_REPORT.md) and [build manifest](evidence/p9-05-2026-09-09/build-manifest.json) identify this checkpoint. The earlier [EX-08D guide](EX08D_SESSION_GUIDE.md), build on 5180 and blank record are preserved.

## Start and identity

[Open the fresh seed-3 city](http://127.0.0.1:5181/?rules=exploration-v2&seed=3&view=world&state=/fresh-3.json). All starts load paused. Press 1× when ready; record pauses, speed changes and assistance. Restart the frozen server from the repository with `python docs/evidence/p9-05-2026-09-09/serve.py` if needed. It serves the verified archive on loopback, independently of the mutable development build.

Build: exploration-v2, fingerprint `ba11e896`, survey revision 3, save schema 3 / campaign metadata 10; 205 source inputs match P9-04R. Port 5181 browser saves belong to this origin. Ctrl+S saves locally; Ctrl+O loads that slot. Save/export important observations before replacing a slot or starting another seed. Choose an explicit seed link below to restart its fresh fixture.

## Declared ten-seed route

These ten seeds are declared before human review. They combine reference cases, holdouts and repaired problem cases. They are a diagnostic sample, not a random estimate of enjoyment or campaign balance. Review them in the listed order, or record your actual order and reason. Splitting the review across sessions is fine; mark unfinished rows accurately.

| Seed | Fresh paused start | Coverage reason |
|---|---|---|
| 3 | [Open seed 3](http://127.0.0.1:5181/?rules=exploration-v2&seed=3&view=world&state=/fresh-3.json) | Shared reference and prepared starts |
| 4 | [Open seed 4](http://127.0.0.1:5181/?rules=exploration-v2&seed=4&view=world&state=/fresh-4.json) | Reference comparison |
| 5 | [Open seed 5](http://127.0.0.1:5181/?rules=exploration-v2&seed=5&view=world&state=/fresh-5.json) | Reference comparison |
| 8 | [Open seed 8](http://127.0.0.1:5181/?rules=exploration-v2&seed=8&view=world&state=/fresh-8.json) | Holdout |
| 11 | [Open seed 11](http://127.0.0.1:5181/?rules=exploration-v2&seed=11&view=world&state=/fresh-11.json) | Holdout |
| 13 | [Open seed 13](http://127.0.0.1:5181/?rules=exploration-v2&seed=13&view=world&state=/fresh-13.json) | Holdout |
| 80 | [Open seed 80](http://127.0.0.1:5181/?rules=exploration-v2&seed=80&view=world&state=/fresh-80.json) | Previously failed facility-pad class |
| 88 | [Open seed 88](http://127.0.0.1:5181/?rules=exploration-v2&seed=88&view=world&state=/fresh-88.json) | Previously failed workshop/cache composition |
| 102 | [Open seed 102](http://127.0.0.1:5181/?rules=exploration-v2&seed=102&view=world&state=/fresh-102.json) | Previously obstructed stop; route and build access |
| 842 | [Open seed 842](http://127.0.0.1:5181/?rules=exploration-v2&seed=842&view=world&state=/fresh-842.json) | Previously failed recruit placement; narrow-street truck access |

For **each city**, use the same short route and record what actually happens:

1. Before opening the map, describe two places or landmarks that help distinguish this city. Walk out of home through the ordinary entrance, orient toward the known tram station, and describe which visible features you used. Record if places look interchangeable or navigation needs constant map checking.
2. At a junction, describe two routes you considered and why you chose one: distance, visible cover/threat, obstruction, salvage, or a useful place. Walk your choice using normal controls. Record backtracking, dead ends, body/building collisions and whether the alternative was meaningful. A forced single route is a valid observation, not a failure to fill the form.
3. Open Map (M) and Navigation. Rename the known station, pin a position, show the destination and return the camera to the engineer. Check that known and visited labels make sense. Save, reload and use your name/pin to return to a place. Restore a default name/remove a pin. Record confusing labels, focus/keyboard trouble, narrow-window scrolling and any accidental movement.
4. Inspect a reachable survey segment and station footprint. Identify a practical source-chest/construction site and street approach; after earning the station/truck/Foreman normally, exercise actual paid track, freight and queued delivery where reached. Record **inspected only**, **built**, **freight delivered** and **truck completed** separately. Never count a map line as proof of freight or truck access. Do not grind out ten complete campaigns merely to fill a row: unfinished paid work remains an explicit coverage gap.

Record real minutes, sim time, deaths/recovery, interventions, and any seed where this route cannot be completed. Do not substitute a different seed silently. A walkthrough is city evidence; it does not by itself pass Phase 9 or the whole campaign. No target completion time or score is imposed.

## Shared construction, transport and defence observations

The following **seed-3 assisted starts** were generated by replaying ordinary paid commands on this build, with no resource, HP, position or clock injection. They disclose locations and skip work you did not personally perform. Their times are preparation times, not your opening-performance results. All preserve complete replay logs and load paused.

| Start | Prepared state | Useful observations |
|---|---|---|
| [Construction](http://127.0.0.1:5181/?rules=exploration-v2&seed=3&view=world&state=/construction.json) | 01:35; Foreman recruited, second-area station restored; chest #4 has 68 steel / 27 copper; supplied ammunition design/order #3 waits for truck | Select the chest/order, start/pause/resume delivery, inspect shortages and completed machines, save during work and resume. Compare a similar hand-built cell with copied/queued work; record your actual commands/time/confusion. |
| [Defended network](http://127.0.0.1:5181/?rules=exploration-v2&seed=3&view=world&state=/network.json) | 39:50; three live cores, six stocked turrets, ammunition line, tram freight, radio and coal extraction | Natural warning at 40:00 for dusk 55:00. Observe warning comprehension, G/K camera controls, physical freight, fuel/ammo supply, defence, repairs/recovery and continuation beyond another dusk if reached. |
| [Optional Turbine](http://127.0.0.1:5181/?rules=exploration-v2&seed=3&view=world&state=/turbine.json) | 03:17; earlier crews and production prepared, 40 carried concrete, Turbine not restored | Read requirements, pay restoration normally and judge whether the service/reward is understandable. This reveals optional content and cannot establish unaided discovery. |

Construction begins early **without added home defence**. Both initial cores are live, but pause to read and arrange defence during play; use the separate defended network for longer combat observations. Network feeds use chests/inserters. Neither start establishes indefinite unattended survival; fuel/mining/transfers may still be needed. Supplied blueprints do not demonstrate personal design discovery.

For the unresolved direct-belt ammunition report, point the output belt into the turret footprint and note recipe, orientation, actual rounds on the belt, turret ammo before/after, power and target activity. The supplied construction cell provides one reproducible example. Automated examples pass, but the original symptom has not been reproduced or diagnosed. If it recurs, keep the affected save plus seed/time and describe the connection. Record inserter-fed and direct-belt results separately.

Use both a normal desktop window and a narrower window if practical. While the network runs at 1×, open item/recipe help and place/remove nearby lamps and poles using real supplies. Note visible pauses, input delay and camera/lighting stalls, machine count, hardware/browser and duration. Short automated samples do not close the earlier 266–300 ms findings or certify a reference machine.

## Record and decision

The [Phase 5 owner report](PHASE_5_PLAYTEST_REPORT.md) already credits successful two-assembler ammunition, tram/truck and defence/recovery play on the identified Phase 5 build. It is prior evidence, not a new P9 session. Do not duplicate it as current-build success.

Use [P9_SESSION_RECORD.md](P9_SESSION_RECORD.md) to record actual ten-seed observations and any shared P5–P8 evidence. Explicitly state the Phase 9 verdict separately from remaining gaps, ammo retest, P6/P7/P8 and EX-08 positions. RI-02B UI engineering may proceed after P9-05 under D-UI-02; no human gate is automatically passed. Wider Q07 progression/endgame remains outside this review.
