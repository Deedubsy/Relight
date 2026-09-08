# P6-AMMO — reported conveyor loading issue

2026-09-07. Owner reports that ammunition on a conveyor connected to a turret did not load automatically during pre-P6 play and requests investigation before P6-04. This is a specific follow-up to the earlier successful gameplay report; that earlier report remains unchanged.

## Findings so far

No loading defect has been reproduced in the current source. Sixteen ordinary-stock campaign setups pass: a conveyor ending directly into each of a turret's eight perimeter tiles, then the same eight connections through a powered inserter. Each setup pays for construction, withdraws twelve real magazines, fills the empty turret to 50 rounds, backs ammunition up on the belt, and resumes loading after the turret is packed and replaced empty. Resource conservation, saved continuation and full normal-start command replay match. No stock, position, time or HP injection is used; short live runs do not exercise combat consumption or disabled-base recovery.

The implementation in [flow.ts](../packages/sim/src/flow.ts) supports direct belt loading: `tickBelt` follows the next tile in the belt's arrow direction; `giveItem` accepts magazines and adds ten rounds, up to the 50-round hopper. The turret's facing does not select an ammunition input side. A belt merely running alongside a turret does not transfer sideways automatically; a powered inserter can transfer from the adjacent tile behind it to the adjacent tile in front. Direct conveyor loading needs no inserter power. A turret with 41–50 rounds cannot accept another whole ten-round magazine yet. These are existing mechanics, not newly introduced fixes.

The prior P6-03 workloads used chest-to-inserter-to-turret supply pods. Their passing defence results did not directly cover a conveyor endpoint, so the new probes add relevant coverage without relabelling the earlier evidence. Production source is unchanged from the frozen P6-03 archive.

## Evidence and remaining diagnosis

- [Direct conveyor probe](evidence/p6-ammo-2026-09-07/probe.ts) and [eight-case result](evidence/p6-ammo-2026-09-07/direct-probe.log).
- [Powered inserter probe](evidence/p6-ammo-2026-09-07/inserter-probe.ts) and [eight-case result](evidence/p6-ammo-2026-09-07/inserter-probe.log).
- [Evidence/source manifest](evidence/p6-ammo-2026-09-07/manifest.json) records hashes and preservation of the P6-03 implementation/evidence. Documentation/freshness and task/reference consistency checks are recorded alongside the probes.

The owner confirms that the conveyor arrow pointed directly into the turret. That connection is supported and passes the direct-loading probes. The original turret ammunition count, core state and saved layout remain unknown. Read-only inspection of the currently open port-5176 tab shows Home Court core disabled (0/300 HP), zero production capacity and the first-production objective; this is not established as the original playtest setup. A disabled core stops its local conveyors through `machineRunning`, but this observation does not prove the cause of the earlier report. The affected playtest save is needed to inspect the actual failure. No speculative gameplay change or claim that the reported problem is fixed is made. P6-AMMO remains in progress and P6-04 stays blocked until this follow-up is resolved. No new full suite/build/lint run is required or claimed because production and regression-test source are unchanged; earlier 250-test verification remains explicitly historical. No commit or push performed.
