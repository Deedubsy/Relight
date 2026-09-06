# Second-area station and radio restoration

Work began 2026-09-06 and completed 2026-09-07 (local time), following home-opening checkpoint `f055cf9`. The owner authorised continuation with the second-area station and radio opportunity (D-EX-14). EX-04 is complete for this opening increment; EX-05 supplies the still-missing defence system.

## Implemented result

The player can walk from Home Court into unrestored districts, carry supplies to Switchyard, build and fuel a local generator or connect its substation through real poles, and restore the station. Commissioning registers the existing fixed neighbourhood as a second base, without a chain of Held blocks or a frontier attack. Normal reach, collision, materials and power requirements apply to temporary construction beyond ownership.

The station unlocks Track, Tram stop and Tram. Its once-only inventory contains one tram, two stops and one track per tile of the generated home-to-station street route. The seed-3 route needs 48 track. Collection transfers as much as fits into normal pockets; remaining machines stay at the station. Repeated interaction/restoration or save/load never grants another kit. The material ledger counts committed restoration supplies and their once-only consumption; machine rewards have their own persisted grant and remaining-inventory record, consistent with the existing ledger's treatment of carried machines.

The player lays the route and powers both stops. Blue survey marks are a suggested placement plan, not built infrastructure. Existing reserved freight then operates normally, including destination requests, return cargo and interruption handling. The complete automated expedition laid its kit and delivered five reserved steel units between independently powered bases with no debug inventory or ownership shortcuts.

A nearby radio installation becomes restorable after the station. It consumes carried materials and requires power; its saved restored identity is permanent, while its status reflects power outages. It reports readiness and the pending warning system truthfully. It does not select targets, generate attacks or pretend to issue warnings.

## Provisional implementation choices

| Choice | Current setting and boundary |
|---|---|
| Station price | 30 steel and 15 copper, delivered from the pockets. Provisional task tuning. |
| Radio price and draw | 20 steel, 10 copper and 20 kW after restoration. Provisional task tuning. |
| Commissioning | Immediate when delivered, powered and within reach; no inherited burn-off or automatic attack. |
| Base footprint | Home plus one station's existing named city cell; streets visibly bound the neighbourhood. Future hostile approaches and core damage are EX-05. |
| Local power | Retained whole-cell service and 100 kW core demand. Fueled generators supply their own circuit; actual pole/substation links join circuits. An unconnected remote generator cannot support home or vice versa. |
| Shortage | Each circuit applies its own production throttle and shares load among its fueled generators. The HUD totals all circuits; it does not replace per-circuit operation. |
| Route | Deterministic clear street path with two valid stop pads; saved with the campaign. Survey paint places nothing and does not prevent the player choosing another route. |
| Inventory | Existing pocket limits and machine stack sizes retained. Large kits can require multiple collections. |

These are implementation defaults within the adopted direction, not owner-approved balance or evidence of enjoyable pacing. The radio warning/eligibility logic remains EX-05; restoration timestamps provide its future input. Persistent extraction, workshop repair and optional discoveries remain later work.

## Save and legacy compatibility

New expanded games retain save envelope schema 3 but use campaign metadata version 2. Older builds reject that metadata version. The current loader validates site/route/reward metadata, and older home-only campaign saves gain deterministic station/radio records without a reward grant. Their earlier command logs are marked incomplete for replay under the changed initial-state factory; the original save remains intact until the user saves again. Fresh expanded sessions replay the complete logged expedition and freight delivery deterministically, and continued saves match uninterrupted runs.

Legacy schema 1/2 saves and campaigns retain their previous rules and hashes. Historical generated blocks, experiments, fixtures, snapshots and evidence are preserved. Campaign evidence fingerprints now include restoration and power settings. Generated campaign values remain separate in [CAMPAIGN_RULES.md](CAMPAIGN_RULES.md).

## Verification

Runtime: WSL Ubuntu-24.04, Node 22.18.0, existing Linux dependencies. No Windows dependency repair, branch merge, push or human gate was performed in this increment.

| Check | Result | Evidence |
|---|---|---|
| Complete simulation suite | 170/170 passed | [test log](evidence/station-restoration-2026-09-06/test.log) |
| Focused opening and expansion tests | 9/9 passed | [focused log](evidence/station-restoration-2026-09-06/focused.log) |
| TypeScript and production build | Passed | [build log](evidence/station-restoration-2026-09-06/typecheck.log) |
| Lint | Passed | [lint log](evidence/station-restoration-2026-09-06/lint.log) |
| Generated documentation | Both profiles passed | [docsync log](evidence/station-restoration-2026-09-06/docsync.log) |
| Historical evidence freshness | Passed | [freshness log](evidence/station-restoration-2026-09-06/freshness.log) |
| Legacy snapshot | Retained three-hour snapshot matched | [snapshot log](evidence/station-restoration-2026-09-06/snapshot.log) |
| Documentation consistency | Local links, EX rows and unchanged legacy blocks checked | [consistency log](evidence/station-restoration-2026-09-06/consistency.log) |

Five seeds (3, 4, 5, 8, 13) exercise the physical expedition, cost and power refusal, valid generated track, restoration, radio, reward idempotence, conservation and save continuation. The complete seed-3 freight expedition uses real walking, transfers and placements, then replays its command log to the same hash. A separate real-placement test connects the remote installation through poles and verifies disconnection after pickup. Invalid metadata and old-log upgrade behaviour are checked.

The full suite ran before the final saved-map bounds correction and guidance refinement. Focused tests checked the bounds correction; the final build checks the guidance. Initial failures exposed an omitted command dispatch case and incorrect type assumptions for geometry helpers/map bounds; these were corrected. Vite still reports its existing large-bundle advisory.

## Browser observations

The fresh seed-3 game accepted house supply transfers and map walking into Switchyard. The first browser pass exposed the old Dark-block click restriction; the campaign map now sends ordinary walking commands for open ground. A normally priced generator was placed and fed, and the station control consumed 30 steel/15 copper, registered base two and exposed the kit. With full pockets, 13 track remained safely at the station after repeated collection attempts.

Walking to the radio enabled its restoration control. It consumed 20 steel/10 copper and reported restored/powered with warnings explicitly pending. Saving and reopening in a fresh tab preserved both restorations, two bases and the 13 remaining track. The goal correctly asked for the still-unlaid tram route and powered stops, rather than claiming a connection already existed. Browser checks did not build the whole line; the logged headless expedition verifies that path.

These are agent-operated checks, not a human playtest. Attack pacing, defence damage, warning comprehension and fun remain unobserved. The next runnable task is EX-05: defences, core recovery, threats, raids, the major schedule and radio warnings. Pre-existing `.serena/` and `docs.zip` remain untouched.
