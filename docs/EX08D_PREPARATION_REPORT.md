# EX-08D — post-P8 playtest preparation

Prepared 2026-09-08 under the owner's “OK do EX-08D”. **Preparation complete; human play NOT RUN.** [Guide](EX08D_SESSION_GUIDE.md), [blank record](EX08D_SESSION_RECORD.md). P8 implementation and engineering checks are separate from owner approval, observed play and the unreproduced P6-AMMO report.

## Frozen identity

- Branch `codex/tram-expansion`; parent commit `8e54ed602da159905b0428f316f6e75cb25ae351`. P8 and preparation changes are local and uncommitted; this task neither commits nor pushes.
- **193 source/test/build inputs match P8-05 byte for byte.** The six compiled/static production files also match P8-05; its two temporary scenario JSON files are excluded from this playable bundle. No gameplay source, tuning or production build was changed or regenerated.
- **11 served files:** six production files, four new starts and settings. [Manifest](evidence/ex08d-2026-09-08/build-manifest.json), [source archive](evidence/ex08d-2026-09-08/source.zip), [playable archive](evidence/ex08d-2026-09-08/playtest-build.zip).
- Source ZIP SHA-256: `69b3524e1e2d8e73ac8fff27c8a3675644c3215b02a510901bd6ef080e8349e5`.
- Playable ZIP SHA-256: `778fd38913767367703be77457bc4b2c9bbca28278fd3bccf60c9d606ee6e3f2`.
- Config fingerprint `9d725a52`; exploration-v2, seed 3, save schema 3 / campaign metadata 10, unchanged defaults. [Settings and inventories](evidence/ex08d-2026-09-08/settings.json).

[Verified loopback server](evidence/ex08d-2026-09-08/serve.py) checks archive and per-file hashes, validates extraction paths and serves a temporary copy on **5180**. Restart with `python docs/evidence/ex08d-2026-09-08/serve.py`. [Fresh start](http://127.0.0.1:5180/?rules=exploration-v2&seed=3&view=world&state=/fresh.json). Earlier EX-08A/B/C archives, reports and blank records are preserved; ports 5177/5179 are unchanged. Port 5178 remains mutable.

## New starts and assistance

All saves load paused and keep a complete current-code replay log. [Preparation script](evidence/ex08d-2026-09-08/prepare.ts), [log](evidence/ex08d-2026-09-08/prepare.log).

| Start | Sim time | Commands | Saved hash | Browser-normalized hash |
|---|---|---|---|---|
| construction | 01:35 | 27 | `78e0dde7` | `78e0dde7` |
| fresh | 00:00 | 0 | `9443010d` | `1ffb5d3d` |
| network | 39:50 | 353 | `f885f625` | `f885f625` |
| turbine | 03:17 | 31 | `112165f4` | `112165f4` |

Fresh is untouched tick zero. Browser normalization adds ordinary initial bookkeeping, explaining its distinct hash. No starting-stock, HP, clock, position or reward injection was used in any start.

Construction replays the ordinary P8-03 opening, cancels the earlier two-belt order, imports an explicitly supplied ammunition design through the normal command, queues it at a valid powered/truck-served location, mines steel and fills the real chest. Foreman and the restored second-area station are earned through recorded commands. At 01:35 both cores have 300 HP, chest #4 at 372,630 contains exactly 68 steel and 27 copper, order #3 at 373,635 waits for the truck, and pockets contain 13 steel / 8 copper / two packed belts. It has no added home defence and is an early tool exercise, not the recommended long combat start. Pause to read, then defend normally or switch to the separate network. The supplied design and scripted opening cannot establish unaided design/discovery or personal build time.

A clone of that waiting start completes the real cargo-paid order, feeds 4 steel / 2 copper by hand into the input chest, and delivers **20 rounds through the output belt directly into the turret**. Conservation, saved continuation and replay from the fresh opening agree; [probe](evidence/ex08d-2026-09-08/construction-probe.json). The playable start remains waiting and stocked. This replaces the unsuitable P8-05 browser fixture, whose unattended home was disabled; that historical fixture is preserved.

Network is a new current-code replay of the P7-05 ordinary supplied-network command prefix to 39:50: three live cores (home 252.8 HP, others 300), six turrets with rounds 47/47/50/50/43/43, home ammunition, tram freight, radio and coal extraction. Foreman is unrecruited. The existing feeds use chests/inserters. Natural dawn at 40:00 provides a warning for dusk 55:00. Preparation included normal paid mining, fuelling and transfers and does not prove indefinite unattended survival.

Turbine is a new current-code replay of the ordinary P7-03 pre-restoration prefix at 03:17, with 40 carried concrete, paid production/local generation and earlier crews. It reveals locations and recipes. It is optional assisted restoration/service evidence only.

## Fresh automated verification

- **57/57 focused tests**, zero failed/skipped: clipboard, library/orders, truck, area removal, campaign defence and campaign guide. [Log](evidence/ex08d-2026-09-08/focused.log). No new full suite, typecheck/build or lint run: source and compiled identity match P8-05, whose 320 full, 76 focused plus 7 final integration checks and build/types/lint are retained historical results in the [review](P8_05_REVIEW_REPORT.md).
- **Eight paused start/save cases** (four starts × 1366/900 widths): expected hashes/ticks/log completeness, no horizontal overflow, stable pause and Ctrl+S/Ctrl+O exact reload. [Browser script](evidence/ex08d-2026-09-08/browser.cjs), [results](evidence/ex08d-2026-09-08/browser-result.json).
- **Both widths:** DOM order pause/resume, explicit chest/order selection, truck start/pause and paused-job save/reload/resume, completed cell paid from 68 steel / 27 copper in chest while pockets stay unchanged, ordinary walking then E/chest UI input, direct belt loading and full replay. Both turrets reach 40 rounds in the checked continuations. [Script](evidence/ex08d-2026-09-08/construction.cjs), [results](evidence/ex08d-2026-09-08/construction-browser-result.json). Desktop and compact construction screenshots were visually inspected; the compact layout stacks panels and requires scrolling.
- Natural live 1× warning received at exactly 2400 for target block 332/dusk 3300; G/K camera controls leave state/engineer unchanged. [Result](evidence/ex08d-2026-09-08/warning-result.json).
- Both widths commission Turbine through the ordinary button, spend exactly 40 carried concrete, retain one base and match full replay. [Result](evidence/ex08d-2026-09-08/restoration-result.json).
- **76/76 live lamp/pole edits** succeeded across four five-second 1× samples with recipe information open. Ordinary paid/walking command helpers and real DOM controls; no gameplay-state edits. Headless Chrome on this host, with some other checks running concurrently; not a human session or reference-machine certification.

| Start / width | Successful edits | Mean frame ms | P95 ms | Max ms | Mean light-paint ms |
|---|---|---|---|---|---|
| fresh / 1366 | 18/18 | 16.73 | 17.50 | 33.00 | 22.72 |
| fresh / 900 | 18/18 | 16.67 | 17.90 | 19.70 | 22.01 |
| network / 1366 | 20/20 | 16.79 | 17.60 | 33.30 | 21.31 |
| network / 900 | 20/20 | 16.79 | 17.70 | 33.40 | 22.61 |

These short samples do not close the prior 266–300 ms findings or establish a performance fix. No hardware threshold is silently adopted.

Documentation/profile sync, freshness, archive/source/prior-evidence integrity and referenced-path/status/blank-record checks are recorded in [docsync log](evidence/ex08d-2026-09-08/docsync.log), [freshness log](evidence/ex08d-2026-09-08/freshness.log) and [verification manifest](evidence/ex08d-2026-09-08/verification-manifest.json).

## Human handoff and limits

EX-08D is done. EX-08H, P6-H, P7-H, P8-H and remaining T18 observations are available for actual play, with no result recorded. The blank template credits prior owner Phase 5 success and separately requests assistance/timing, comparable construction work, transport/defence/recovery, discovery/service comprehension, ammo details, continuation, performance and explicit phase/exit positions. The session is unscored; supplied assistance is disclosed. P6-AMMO remains unresolved even though new automated direct-belt examples pass.

No new balance rules, phase verdict, wider Q07 progression decision or performance acceptance is inferred. The [tracker](PROGRESS.md) alone owns task status; EX-08 assessment follows actual human evidence.
