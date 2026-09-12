# EX-08A — representative-loop preparation

> Scheduling update, 2026-09-07 (D-EX-20): human gameplay testing is deferred until Phase 5 factory implementation is ready. This document preserves the EX-08A checkpoint; EX-08B must prepare an updated build and session documents before EX-08H. No human play or phase approval is recorded.

Date: 2026-09-07. Authorisation: D-EX-19, “Ok onto the next one”, after EX-08A was identified as next. **Preparation complete; human play not run.** Phase 4 remains complete and Phase 5 has not started.

## Identified build and launch

The prepared build freezes the current EX-04–07 campaign on `codex/tram-expansion`. HEAD is `184908158db1c328b2a01d617a551954a0b3c99e`; EX-07 remains uncommitted. The parent commit alone does not identify these changes. [build-manifest.json](evidence/representative-loop-2026-09-07/build-manifest.json) records SHA-256 hashes for all 146 archived source/build-input files and eight served files. Source and production archives retain the exact tested working-tree content without a commit, merge or push.

- [Frozen playable build](evidence/representative-loop-2026-09-07/playtest-build.zip): SHA-256 `e4034e00621dfa819dcc6388aff206e9ccc222ee60f9ec9c64d038c369b77019`.
- [Frozen source inputs](evidence/representative-loop-2026-09-07/source.zip): SHA-256 `61b8ee96f3e4e92438e855eab699a12de6114ef27fc9c8f367734eb988f04b1c`. Includes workspace manifests, lockfile, source, tests and fixtures; dependencies are installed separately. Rebuild with Node 22.18.0, `npm ci`, then `npm run typecheck` in an extracted copy; avoid replacing this working checkout.
- Campaign configuration fingerprint: `a9aea2fa`. [settings.json](evidence/representative-loop-2026-09-07/settings.json) records the complete actual config, tuning and starting inventories.
- [Paused start](evidence/representative-loop-2026-09-07/start.json): seed 3, exploration-v2, schema 3, metadata 5, tick 0, state hash `ab93bc27`. It is an ordinary fresh campaign with a complete empty command log, serialized by makeSave; no progression, items or elapsed time were injected.

[Open the playtest](http://127.0.0.1:5175/?rules=exploration-v2&seed=3&view=world&state=/start.json). The frozen archive server and visible Codex browser tab are ready at handoff, with the game paused. [serve.py](evidence/representative-loop-2026-09-07/serve.py) checks the archive and each served file against the manifest, extracts to a temporary directory and binds only loopback. Restart it with `python docs/evidence/representative-loop-2026-09-07/serve.py`; Ctrl+C stops it. It does not serve the live development tree.

The browser’s normal ensureFlow initialization adds zero-valued bookkeeping fields, producing tick-zero hash `21b1fa9f`. This is documented rather than silently changing the source snapshot. The first prepared-state assertion caught this normalization when a ledger query was run on the original; preparation now checks conservation on a copy and preserves the fresh state exactly. The failed initial preparation log is retained.

## Automated readiness

EX-08A makes no gameplay-code or tuning changes. The freshly run focused suite passed **38/38** tests, covering the following contracts. Each is mechanics evidence under the conditions in its test, not a claim of enjoyable or complete human play.

| Contract | Current evidence | Scope and limitation |
|---|---|---|
| Profile isolation, clock, opening, separate slots and save identity | campaign.test.ts (4 tests) | Includes ordinary fresh sessions and seed geometry; does not measure onboarding |
| Paid station/radio restoration, tram kit, local/linked power and freight access | expansion.test.ts (5 tests) | Ordinary carried materials and commands; no transport-before-unlock circularity |
| One major lock, restoration coalescing, quiet intervals, minor suppression and radio outages | campaignDefence.test.ts (10 tests) | Scheduler/ownership fixtures explicitly edit time or setup; separate paid-command replay and real complete-roster defence tests |
| Distinct extraction, workshop supplies, three-base onward/return freight and shorter-rest milestone | campaignDistricts.test.ts (6 tests) | Integrated logistics fixture adds finite stock and defers threats; separate fresh paid-restoration replay uses normal stock |
| Optional guardian, paid weapon use, once-only tool, repair costs and saved continuation | campaignDiscovery.test.ts (9 tests) | Seven-seed bypass reachability; labelled combat fixtures plus a separate fresh ordinary-command discovery replay |
| Reservation/capacity invariants, refusal/return, power loss, endpoint removal and in-flight load | freight.test.ts (4 tests) | Isolated deterministic freight fixtures; supports the campaign’s reused transport code |

The current production build and every package typecheck also passed. The latest full simulation suite (195/195), lint, legacy snapshot and 32-seed creation/save sweep are retained in [the EX-07 report](CAMPAIGN_DISCOVERY_REPORT.md) and its raw logs; those were verified immediately before this preparation with unchanged gameplay sources. They are not presented as new human results. Vite retains its existing large-bundle advisory.

Current focused/build/prepare/document checks and artifact verification are retained under [evidence/representative-loop-2026-09-07](evidence/representative-loop-2026-09-07/). Both documentation profiles, artifact freshness and local reference consistency are checked for this preparation. [Browser observation](evidence/representative-loop-2026-09-07/browser.md) confirmed the frozen page renders Home Court at time zero and remains paused. No gameplay commands were sent during launch verification.

There is no claim that one uninterrupted unmodified automated run completed the entire revised loop, its logistics, both assault-selection paths and the full recovery interval. Existing fixtures and fresh-command segments establish their named invariants. Human observation now addresses the combined experience.

## Human session prepared before observation

[EX08_SESSION_GUIDE.md](EX08_SESSION_GUIDE.md) contains a short player start, observer-only coverage, fixed settings, checkpoint/export instructions and known limitations. [EX08_SESSION_RECORD.md](EX08_SESSION_RECORD.md) is a blank record: every observation and the owner verdict remain unfilled. The governing [playtest protocol](EXPLORATION_DEFENCE_PLAYTEST.md) remains marked not run.

The primary session is unscored and diagnostic. It uses normal 1× time, with pauses and continuations recorded. The first eligible dawn lock is at 40:00 and attack at 55:00. Record a major’s actual finish, recovery, a further expedition and later attack/lock when feasible; short quiet samples cannot prove the full interval. Observe independent exploration before prompting coverage of a missed optional discovery. Record both scheduled and restoration-nominated targets; if necessary use separately labelled branches from the player’s own pre-lock save, preserving ordinary costs and timing. No debug attack is passed off as normal progression.

No new numerical fun, speed or phase-pass threshold is adopted by the agent. Before play, the guide fixes what evidence is required for an informed owner decision, intervention handling, technical stop conditions and explicit unobserved outcomes. The owner later chooses proceed, revise or insufficient evidence.

## Known limitations and handoff

Day/night presentation still lacks a visual sun cycle. The initial paused power counters have not ticked. The court boundary is static geometry; stock, HP, costs/rates and repair-tool value remain provisional. Broad seed fairness, pacing, repetitive maintenance, warning comprehension and the usefulness of old factories have not been established by human play.

Telemetry is partly legacy-oriented and does not retain all campaign event transitions. It stores the final state, commands and speed/minute data; milestone saves and observer notes are necessary for campaign warnings, repairs, restored sites and interpretation. Major history is bounded to 32 records. The old root replay CLI is legacy-only and is not a campaign determinism certificate; retain complete save logs and the frozen source for proper campaign analysis.

**Four claims:** preparation implemented; automated checks passed within the scopes above; direction/preparation authorised by D-EX-11 and D-EX-19; human play and owner gate **not run/not recorded**. EX-08A is complete. EX-08H is ready for the human session; EX-08 remains dependent on its observations and verdict. No Phase 5 start or content expansion is implied. EX-07 and this preparation remain uncommitted; unrelated `.serena/` and `docs.zip` are intact.
