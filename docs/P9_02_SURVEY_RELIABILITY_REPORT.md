# P9-02 — buildable tram surveys

Implemented 2026-09-08 under the owner's “Ok lets do P9-02” ([D-EX-48](DECISIONS.md)). **Implementation and automated verification complete.** [PROGRESS](PROGRESS.md) owns task status. [Scope](PHASE_9_SCOPE_REPORT.md).

## Result and saved-game behavior

The first tram survey accepted street tiles beside INERT blocks, even though ordinary construction correctly refused them. The later district search already excluded that ground. `initExpansion` now applies the DARK/HELD check to the existing bounded route search and every cell of each stop footprint. It retains deterministic ordering and explicit failure when no route/pad exists. It does not reroll the city, relax construction rules or change prices.

The same seed population that produced 36 rejected track tiles on seeds 6/29 now passes **32/32**. Repeated queries, save/load reports and unchanged-input checks pass for every seed, with identical source throughout. [Summary](evidence/p9-02-2026-09-08/initial-survey/summary.json), [source identity](evidence/p9-02-2026-09-08/initial-survey/manifest.json). The P9-01 failing sweep and paid reproductions remain untouched. No other reservation rewrite was justified by this diagnostic population; the broader 10,000-seed obligation remains P9-04.

New campaigns record `expansion.surveyVersion: 2`; campaign fingerprint changes from `9d725a52` to `9fc92bfc`. Missing survey revision means the older generated layout; unsupported revisions refuse load. Campaign metadata 10 and save schema 3 remain unchanged. `initExpansion` never regenerates an existing survey. Loading the two preserved paid pre-fix saves retains the actual expansion/district records, inventories, machines and state hashes. Those saves keep their old route, including its original restriction; they are not silently repaired.

The session loader retains the supplied old log as evidence but marks it incomplete for replay from the revised fresh-game factory, including old saves already carrying metadata 10. Upgrading metadata cannot manufacture a new survey or restore fresh replay provenance. New revision-2 saves keep complete histories. The generated [campaign reference](CAMPAIGN_RULES.md) documents the contract.

## Ordinary paid routes

The final [eight-seed driver](evidence/p9-02-2026-09-08/paid-routes.ts) uses fresh campaigns, normal home supplies, walking, local fuel collection, paid generators/restorations, the earned first kit and purchased extension parts. It builds three stops and the entire two-section line, transfers five real steel through tram arrivals, recruits the Foreman locally, imports a two-belt plan through the ordinary library command and builds it using the physical truck from a two-steel supply chest.

No stock, position, HP, time, unlock or threat injection is used. The first generator consumes carried coal, so the driver returns to the home chest for the remaining fuel. The supply chest occupies buildable block ground beside the street, preserving vehicle access. This is a scripted feasibility demonstration with known targets, not an unaided discovery or combat-success measurement.

| Seed | Simulated finish (s) | Built track tiles | Stops | Freight delivered | Truck order |
|---|---:|---:|---:|---:|---|
| 3 | 146 | 52 | 3 | 5 steel | 2 belts completed |
| 4 | 188 | 82 | 3 | 5 steel | 2 belts completed |
| 5 | 160 | 67 | 3 | 5 steel | 2 belts completed |
| 6 | 162 | 67 | 3 | 5 steel | 2 belts completed |
| 8 | 162 | 79 | 3 | 5 steel | 2 belts completed |
| 11 | 205 | 78 | 3 | 5 steel | 2 belts completed |
| 13 | 145 | 59 | 3 | 5 steel | 2 belts completed |
| 29 | 195 | 98 | 3 | 5 steel | 2 belts completed |

All eight runs record loading, travelling, building and completion, actual truck displacement, source depletion, cargo payment and unchanged engineer pockets during construction. Conservation, complete fresh-command replay and saved continuation agree in every case. Per-seed records and complete saves are in [paid evidence](evidence/p9-02-2026-09-08/paid/seed-6.json); [final log](evidence/p9-02-2026-09-08/paid-final.log).

Initial driver failures are retained separately: fuel was exhausted before the second generator; an addressed chest command does not accept the Depot; half-second sampling missed a short travel phase; a seed-11 supply chest obstructed the truck's street access. The final driver uses normal home-chest retrieval, tick-resolution observations and off-street supply placement. These were probe/setup corrections, not hidden map repairs or dropped failed seeds. Earlier successful saves remain as additional evidence.

## Checks and provenance

Four new survey regressions cover both failed seeds, old geometry/inventory preservation, current/old replay provenance and unknown revisions. The focused survey/validator/expansion run passes **16/16**. The existing old-checkpoint regression now verifies that metadata upgrades do not change survey history; its former expectation caused the initial full run's sole failure. The initial failed run and an interrupted rerun remain archived.

**331/331 full simulation tests pass**, zero failed/skipped, in 410.70 seconds; [full log](evidence/p9-02-2026-09-08/test.log). Types/production build and lint pass; [build](evidence/p9-02-2026-09-08/typecheck-verified.log), [lint](evidence/p9-02-2026-09-08/lint-verified.log), [focused log](evidence/p9-02-2026-09-08/focused.log). Vite retains its existing large-chunk advisory. Native Windows `git diff --check` passes; the retained WSL Git attempt reports CRLF bytes as trailing whitespace across existing checkout files, so no unrelated files were reformatted.

All **12 current campaign experiments / 78 checks** were regenerated at the new fingerprint before promotion into `docs/experiments/campaign`. Their source records precede the documentation-generator update adding the survey-reference row. All recorded runtime inputs still match current code; the sole changed source file is `packages/tools/src/campaign-docsync.ts`, whose earlier bytes are preserved in the P9-01 archive. Results keep their actual recorded source identity. The previous exact bytes and hashes are retained in [previous-campaign.zip](evidence/p9-02-2026-09-08/previous-campaign.zip) and [manifest](evidence/p9-02-2026-09-08/previous-campaign.json). E-chain uses ordinary stock; E-coal, E-tram and the three five-hour E-logistics runs disclose their existing stock/threat assistance. They verify engineering behavior, not unassisted balance. [Run log](evidence/p9-02-2026-09-08/campaign.log).

Headless Chrome 153.0.8010.27 loads an old paid seed-6 save and a new paid seed-6 save at 1366×900 and 900×900. All four paused starts preserve their route, inventory and machine count through actual Ctrl+S/Ctrl+O. New-save replay matches; old-save fresh replay is explicitly unavailable. No page errors. [Browser record](evidence/p9-02-2026-09-08/browser-result.json), [driver](evidence/p9-02-2026-09-08/browser.cjs). The old 1366-wide and new 900-wide screenshots were opened and inspected; these establish actual loaded presentation, not a UI redesign or performance certification. Browser checks use the mutable 5178 build and isolated storage; EX-08D/5180 remains frozen.

Frozen [198-input source archive](evidence/p9-02-2026-09-08/source.zip), [six-file build archive](evidence/p9-02-2026-09-08/build.zip), [hash manifest](evidence/p9-02-2026-09-08/manifest.json) and [line-ending-normalized delta against P9-01](evidence/p9-02-2026-09-08/source-delta.patch) identify the work. [Documentation sync](evidence/p9-02-2026-09-08/docsync.log), [freshness](evidence/p9-02-2026-09-08/freshness.log), [integrity checker](evidence/p9-02-2026-09-08/check.py) and [verification manifest](evidence/p9-02-2026-09-08/verification.json) cover current source/results and preserved P9-01/EX-08D/P8-05/EX-08C evidence.

## Handoff

P9-03 supplies saved names and map wayfinding next. P9-04 retains the declared 10,000-seed run; P9-05 owns engineering readiness and a separate post-P9 playtest preparation. D-UI-02 places the UI redesign after P9-05. Shared human tasks, P9-H and the owner's P6-AMMO retest remain open; no new human result is inferred. The unrelated `power4.json` formatting and existing UI-planning changes are preserved. The current Phase 9 scope report has CRLF-only byte differences from its frozen LF hash; normalized bytes match exactly, and the verifier records the difference without rewriting it. No commit or push.
