# P9-01 — composed campaign city validation

Implemented 2026-09-08 under the owner's “Lets do P9-01” (D-EX-47). **Implementation and automated verification complete.** The city sweep retains two demonstrated geometry failures for P9-02. [Scope](PHASE_9_SCOPE_REPORT.md), [task status](PROGRESS.md).

## Result and actionable findings

The finalized **32-seed campaign sweep passes 30 seeds and reports two failures**. Seeds 3/4/5 and holdouts 8/11/13 all pass. The failed seeds are retained, with a failing process exit code; a detected city defect is not a failed validator regression.

| Seed | Finding | Ordinary-command reproduction |
|---|---|---|
| 6 | One surveyed track tile is not buildable: tile 501122, coordinates 322,626, nearest block 338 in INERT state 3. | Paid station generation/restoration, collected earned kit, walked into reach and submitted the track command at 00:27. It refuses with “not buildable ground”; inventory/machine count unchanged, conservation and full opening replay agree. |
| 29 | 35 surveyed track tiles are not buildable. First finding: tile 508293, coordinates 293,635, nearest block 358 in INERT state 3. | Same ordinary sequence, first rejected tile at 00:22; no stock, position, time or HP injection. Inventory/machine count unchanged, conservation and full opening replay agree. |

[Full sweep manifest](evidence/p9-01-2026-09-08/campaign/manifest.json), [summary](evidence/p9-01-2026-09-08/campaign/summary.json), [seed 6](evidence/p9-01-2026-09-08/campaign/seed-6.json), [seed 29](evidence/p9-01-2026-09-08/campaign/seed-29.json). [Reproduction source](evidence/p9-01-2026-09-08/reproduce.ts), [log](evidence/p9-01-2026-09-08/reproduce.log), [seed 6 paid commands](evidence/p9-01-2026-09-08/reproduction-6.json), [seed 29 paid commands](evidence/p9-01-2026-09-08/reproduction-29.json).

These are P9-02 generator/reservation inputs. No map repair, rerouting, price change or broader seed-fairness verdict is included here. P9-04's 10,000-seed population and human ten-seed review remain outstanding.

## Implementation and query boundaries

`packages/sim/src/campaignCityValidation.ts` exports `validateCampaignCity` and validator version 1. It accepts an initialized current campaign, works on a private structured clone and returns stable failure categories plus deterministic measurements. It never commissions a station, grants a truck/unlock, repairs a save or simulates travel. Invalid dimensions, missing metadata and malformed site rectangles return explicit input/footprint findings.

The query checks:

- The home wall/gate partition, contiguous four-tile mouth on the declared side, shared collision and connected exterior access; remaining reachable home steel/copper/coal and a legal accessible assembler footprint.
- All 14 composed reservation records: two stations, radio, workshop, Turbine, cache, all five recruits and three sources. Full rectangles, pairwise overlaps and reachable on-foot interaction positions are checked. Approach metrics use shortest cardinal steps, not player travel time. Paths do not establish combat-safe passage or the absence of a guardian encounter.
- Three distinct non-home source regions and an accessible legal excavator footprint on each source pad. This does not prove supplied power, output handling or extraction throughput.
- Connected unique cardinal tram survey sections without side junctions/shortcuts; ordinary physical placement eligibility of every track tile and stop footprint; adjacent track, non-overlapping stop pads and accessible stop construction positions.
- Retained urban building door access and footprints; the current origin/staging query for home and both potential station cores. Remote cores are queried as geometry records, not registered or commissioned; no assault is scheduled.
- Optional physical truck route/service queries to the two stations only when a truck actually exists and fits its present pose. Fresh campaigns explicitly report `not-unlocked`. A queried route or blocked destination is a snapshot observation, not proof that arbitrary layouts are truck-serviceable.

`flow.ts` now exposes `placementGeometryProblem`, extracting the existing physical placement body. Ordinary `placeable` preserves its bookkeeping and unlock check, and `canPlace` still checks costs; no command can use the validator to bypass payment or unlocks. The query excludes inventory, reach and unlock prerequisites by design and says so in its scope. Actual refused builds on both flagged seeds confirm that the geometric finding survives the real paid unlock.

`packages/harness/src/cityValidationRun.ts` adds strict seed-list parsing, per-seed creation, unchanged-input checks, repeat-query comparison and save/load report comparison. Generation errors remain failed attempted seeds. `cityValidationCli.ts` records a source SHA-256 digest and per-file hashes in addition to parent commit, unchanged campaign fingerprint `9d725a52`, profile and ordered requested seeds. Source identity is checked again at completion; partial runs retain their written seed files. Existing output directories are refused and files use exclusive creation; [verified refusal](evidence/p9-01-2026-09-08/existing-output-refusal.log).

```powershell
npm run city:validate -- --seeds 1-32 --out docs/evidence/my-new-city-survey
```

Run in the configured Node environment (this checkout uses WSL Ubuntu-24.04 / Node 22.18.0). The output directory must not exist. Exit 1 means a detected seed failure or source mismatch; inspect `summary.json` and seed files. The current runner supports up to 10,000 requested seeds but does not resume; resumable population execution belongs to P9-04. The default list is 3,4,5,8,11,13. No evidence is silently replaced.

## Verification

**7/7 final validator regressions pass**, including repeat/load equality and untouched input, all current sites, physical placement versus the still-locked ordinary command, malformed/missing metadata, overlapping/source/route mutations, a blocked interaction area, an existing paid truck checkpoint and generation-error/seed-list handling. [Final focused log](evidence/p9-01-2026-09-08/focused-final.log). Blocked/malformed fixtures are explicitly artificial; they do not establish paid construction or player travel. The truck test loads the preserved ordinary P8-03 checkpoint and makes only queries.

The finalized sweep attempts every seed 1–32 with identical source throughout, and every created state's repeated/load reports agree without changing caller state. It returns **30 passed / 2 failed**, 36 exact rail-placement findings total. Combined per-seed generation, private query copies, repeat and load validation took approximately 1.37–2.72 seconds, median 1.59 seconds on this host with other checks active. These include warm caches and do not certify cold generation or frame performance. [Runner log](evidence/p9-01-2026-09-08/campaign.log).

Initial test mistakes (counting 13 instead of 14 sites and comparing normal placement's initial bookkeeping against an unnormalized baseline) are preserved in the initial log; corrected expectations and explicit normal initialization pass. The validator's private-copy read-only check passed independently. The initial sweep already identified the same seeds; its source-stamped files remain separate from the finalized sweep.

Package typechecks/production build and lint passed; [build log](evidence/p9-01-2026-09-08/typecheck-final.log), [lint](evidence/p9-01-2026-09-08/lint.log). **327/327 full simulation tests passed**, zero failed/skipped, in 455.89 seconds; [log](evidence/p9-01-2026-09-08/full-test.log). Vite retains the large-chunk advisory. No renderer/UI behavior was added, so no new browser session or performance pass is claimed. Prior P8/EX-08D UI evidence remains historical.

Frozen [197-input source archive](evidence/p9-01-2026-09-08/source.zip), [six-file production archive](evidence/p9-01-2026-09-08/build.zip), [hash manifest](evidence/p9-01-2026-09-08/manifest.json) and [source delta against EX-08D](evidence/p9-01-2026-09-08/source-delta.patch) identify this implementation.

[Documentation sync](evidence/p9-01-2026-09-08/docsync.log), [freshness](evidence/p9-01-2026-09-08/freshness.log), [integrity/reference check](evidence/p9-01-2026-09-08/check.py) and [verification manifest](evidence/p9-01-2026-09-08/verification.json) identify the resulting source/build and retained evidence. The unrelated `power4.json` fixture formatting change recorded by P9-00 remains untouched and semantically identical to its frozen original. EX-08A/B/C/D files and the Phase 9 scope report remain historical evidence; current code/build naturally differs after this implementation. Port 5180 continues serving its archived post-P8 build.

## Handoff

P9-02 addresses the demonstrated INERT-adjacent survey failures without altering existing saved routes. P9-03 wayfinding, P9-04 broad fairness and P9-05 readiness remain separate. Existing shared human tasks and P6-AMMO remain open; this does not record human play, phase approval or a resolution of the original ammunition issue. No commit or push.
