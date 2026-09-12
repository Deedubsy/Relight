# Campaign districts — EX-06

Date: 2026-09-07. Authorisation: D-EX-17, owner request “OK lets move onto EX-06”. Branch: `codex/tram-expansion`, changes verified against baseline `cba5296`. The owner subsequently authorised the local checkpoint with “OK commit that”; no push performed. Phase 4 remains complete; Phase 5 has not started.

## Implementation

The campaign now has a deterministic extension from its starter tram trunk to a later station, with a surveyed stop pad and ordinary paid track/stop construction. The later station costs 40 steel + 20 copper and needs local/connected power; commissioning registers the third base (Home Court plus two outlying stations), nominates eligible future assaults and grants no duplicate starter kit.

Three distinct neighbourhoods contain persistent steel, copper and coal source pads. Existing excavators produce physical items at 0.5/s each while drawing 60 kW from fueled grids. Output blockage stops production. Source pools do not deplete; manual mining cannot bypass powered extraction. Existing finite home salvage and the item catalogue remain. Items enter the same mining ledger as other extraction.

The later station's workshop costs 25 steel + 15 copper and draws 40 kW. It repairs walls and turrets within 14 tiles in its own neighbourhood, drawing ordinary materials from a chest or station arrivals buffer within 4 tiles. Each repair restores up to 40 HP, costs 2 steel + 1 copper and requires two powered seconds. Brownouts slow progress; outages pause it. Active major assaults and local minor raids pause service. Manual repair has priority, and disabled base cores still require manual paid recovery. A removed repair target clears pending workshop progress.

Existing freight supplies the workshop over local inserters/belts and returns district output to earlier bases. Campaign inserters taking from mixed supply storage now select an input that the recipient can accept; a full steel input cannot prevent needed copper from reaching an assembler. Legacy inserter behaviour is preserved.

The shorter-rest milestone requires three operational base stops connected to a tram route and actual destination-reserved deliveries from home to the later station: at least 10 steel, 5 copper, 5 magazines and two distinct ammunition visits. Hand supplies, unreserved cargo and returned shipments do not count. Its saved completion never advances an already promised dawn or rerolls a lock; one complete quiet cycle applies when the next major finishes.

All newly specified costs, ranges and milestone thresholds are provisional engineering defaults within the adopted contracts, not owner-approved balance. The existing 20-minute cycle, first assault, inventory conservation and ordinary command/reach requirements are retained.

## Saves and presentation

Save envelope remains schema 3; campaign metadata is version 4. Earlier district-free previews gain deterministic sites without changing their existing defence schedule, inventories or machines. Their prior logs are marked incomplete for replay against the new campaign factory. Invalid partial records, source types, routes and unearned milestones are rejected. Fresh station/workshop commands replay from the unmodified campaign factory.

World view shows source pads, the extension survey, later station and workshop. The panel exposes restoration controls, current costs/refusals and service/resupply progress. The default URL still selects the legacy profile; the new game is selected with `?rules=exploration-v2&view=world`.

## Verification

Final verification passed: **186/186 simulation tests**, all package typechecks and the production build, lint, both documentation profiles, artifact freshness and the unchanged legacy three-hour snapshot. The six focused EX-06 tests passed. Vite retains its pre-existing large-bundle advisory. Logs are retained under [evidence/campaign-districts-2026-09-07](evidence/campaign-districts-2026-09-07/).

The focused tests cover five seeds' reachable sites and unbranched survey, separate persistent extraction, power loss, refusal of hand extraction, item conservation, paid workshop repairs and save continuation, paused service during active majors/outages, and a physical three-base route. The integrated scenario demonstrates onward allocation, a real home assembler producing magazines, copper extracted at the later site returning home, and actual tram supplies moving over a local belt branch into workshop repairs. A separate fresh campaign buys both outlying stations and the workshop with normal starting stock and replays its entire command log identically.

The integrated construction scenario explicitly adds finite chest stock and defers threats to isolate logistics. It is not a campaign-balance run. Copper is carried from its extraction chest to its export platform in that scenario; the workshop branch and home magazine export use automatic inserters/belts. Legacy freight tests retain coverage of full/unpowered destinations, disconnected track, return recovery and reservations across multiple trams.

Initial checks found a new-module naming collision (resolved without changing the existing legacy districts module), an unused import (removed), an oversized reach rectangle in the test builder (corrected to each machine's actual size), and the mixed-input feeder starvation described above. The extra fresh replay test initially stopped on the final command's tick; advancing the queued command before comparing fixed the test. Failed check logs remain distinct from final results; no legacy expectations or historical artifacts were regenerated to pass.

## Browser observation

Automated local browser observation, seed 3. The fresh campaign displayed disabled out-of-reach later-station/workshop controls. A labelled construction snapshot displayed three registered cores, extraction pads, the workshop, a local supply branch and earned resupply counters. A second snapshot placed the engineer beside the uncommissioned workshop. Clicking the normal restoration button reduced pockets from 101 to 76 steel and 48 to 33 copper, then disabled the button as restored and displayed the workshop's ready state. Existing browser saves were not overwritten. Temporary review snapshots and the loopback server are removed/stopped after review.

## Acceptance and limits

Implementation complete; automated validation passed. Human design/continuation approval: D-EX-11 and D-EX-17. Human play evidence: not run. EX-07 still owns the optional repair schematic and distinctive workshop guardian; EX-08 owns representative-loop play and revisions. Broad seed fairness, extraction rates, maintenance burden, pacing and fun remain unvalidated. No Phase 5 or release gate is claimed.
