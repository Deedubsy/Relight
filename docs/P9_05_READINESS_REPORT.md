# P9-05 — city engineering readiness and human preparation

Prepared 2026-09-09 under the owner's “Ok onto P9-05”. **Engineering review and preparation complete; human play NOT RUN and Phase 9 human verdict not recorded.** [Guide](P9_SESSION_GUIDE.md), [blank record](P9_SESSION_RECORD.md), [tracker](PROGRESS.md). D-EX-52 records this handoff. This task changes preparation scripts and documentation; no gameplay, generation, rewards, costs or UI source changed.

## Reviewed identity and increment coverage

Branch `codex/tram-expansion`, parent commit `8e54ed602da159905b0428f316f6e75cb25ae351`; existing Phase 8/9 work remains local and uncommitted. **All 205 source/test/build inputs and six production files match final P9-04R byte for byte**, including after the fresh production rebuild. Config `ba11e896`, exploration-v2, survey revision 3, validator version 1, campaign metadata 10 / save schema 3. Existing saves retain their own surveyed geometry; older logs remain available with honest incomplete fresh-factory replay provenance.

| Increment | Review against the handed-off implementation | Evidence and limits |
|---|---|---|
| P9-01 | Read-only composed validation separates structural geometry, walking, build eligibility and service queries; reports seed/config and deterministic failures | [Original review](P9_01_CITY_VALIDATION_REPORT.md); current final population and focused/full regressions verify the repaired composition. A reachable tile does not prove arbitrary truck layout access or enjoyable exploration. |
| P9-02 | New-generation route/stop reservations preserve older saved layouts, stock and logs | [Original reliability report](P9_02_SURVEY_RELIABILITY_REPORT.md); superseding revision-3 checks in [P9-04R](P9_04R_REPAIR_REPORT.md). This handoff freshly checks historical revision-2 save/reload at both widths. |
| P9-03 | Saved names, pins, known/visited presentation and camera cues remain ordinary validated commands | [Wayfinding report](P9_03_WAYFINDING_REPORT.md); this handoff freshly checks UI annotation, camera, map/world, save/load and full replay for all ten declared review seeds at both widths. Human recognisability and route choice remain unobserved. |
| P9-04 | Complete failed baseline remains intact: 9,742 passes, 258 failures across six classes | [Population report](P9_04_POPULATION_REPORT.md). No seed dropped, historical result rewritten or old preset/frontage evidence relabelled. |
| P9-04R | Revision-3 final composition passes all 10,000 declared seeds; 258 fail→pass, 9,742 pass→pass, unchanged base-generator retry indices | [Repair report](P9_04R_REPAIR_REPORT.md). Exact-source evidence reused, integrity checked, not a new population run. Twelve paid freight/truck routes, 36 extraction probes and two ordinary visits remain separately identified evidence. |

The current campaign measurements remain the P9-04R 78 passing checks (E-chain/E-coal/E-tram and three five-hour logistics runs). Simulation and harness sources match this handoff. Their recorded game/session.ts predates only the final incomplete-log preservation line, an explicit retained provenance exception verified by the evidence checker; fresh full tests/browser migration cover the final session code. No experiment files were regenerated or promoted in this task. Prior P9 reports/evidence, EX-08D and EX-08C remain protected; the previously documented scope-report CRLF-only difference and unrelated fixture formatting are retained.

## Separate frozen checkpoint and assistance

[Open post-P9 seed 3](http://127.0.0.1:5181/?rules=exploration-v2&seed=3&view=world&state=/fresh-3.json). Port **5181** serves a verified archive independently of mutable port 5178 and frozen post-P8 port 5180. Restart with `python docs/evidence/p9-05-2026-09-09/serve.py`. [Manifest](evidence/p9-05-2026-09-09/build-manifest.json), [source archive](evidence/p9-05-2026-09-09/source.zip), [playable archive](evidence/p9-05-2026-09-09/playtest-build.zip), [server](evidence/p9-05-2026-09-09/serve.py).

There are **21 served files**: six production files, ten explicit fresh seed saves, four shared starts (including the fresh seed-3 alias), and settings. Ten-seed coverage is declared as **3, 4, 5, 8, 11, 13, 80, 88, 102, 842**: reference seeds, holdouts and repaired problem classes. All load paused. Fresh saves are ordinary tick zero, with only standard startup bookkeeping initialized.

Source ZIP SHA-256: `8a2ec556153fd9e2e93aab1ea9553cec0e23df96c15f1ddfc4af8c4aca110054`. Playable ZIP SHA-256: `fdc443b2e76b80f96464264ecdbeaf25cbba50642ef95a06254d22c2ea9c9df7`.

[Preparation script](evidence/p9-05-2026-09-09/prepare.ts) replays the earlier supplied command prefixes on the current factory, verifies full replay/conservation/saved continuation, and writes new saves. No stock, clock, position, HP or reward injection is used. [Log](evidence/p9-05-2026-09-09/prepare.log), [settings and inventories](evidence/p9-05-2026-09-09/settings.json).

| Shared start | Sim time | Commands | Saved/browser hash |
|---|---|---|---|
| construction | 01:35 | 27 | `19de934f` |
| fresh | 00:00 | 0 | `e13e92a4` |
| network | 39:50 | 353 | `753ff6d8` |
| turbine | 03:17 | 31 | `ec20a187` |

Construction retains a waiting supplied ammunition-cell order, earned Foreman/station, and a real chest holding 68 steel / 27 copper. It starts at 01:35 with live cores but no added home defence. A clone completes the cargo-paid order and delivers **20 rounds by direct output belt** with conservation and full replay. The playable start stays waiting. Network has three live cores, six stocked turrets, supplied production/freight/radio and a natural 40:00 warning approaching; it needs ordinary ongoing supply and does not prove indefinite autonomy. Turbine discloses earlier discoveries and provides 40 paid concrete for optional restoration. None of the supplied openings measures unaided discovery or personal build time.

## Fresh readiness checks

- **351/351 full tests**, zero failed/skipped, 445.00 seconds: [log](evidence/p9-05-2026-09-09/test.log).
- **29/29 focused tests**, zero failed/skipped: survey/replay provenance, navigation and construction integration including ammunition: [log](evidence/p9-05-2026-09-09/focused.log).
- Package typechecks/production build, lint and retained legacy compact snapshot pass: [types/build](evidence/p9-05-2026-09-09/typecheck.log), [lint](evidence/p9-05-2026-09-09/lint.log), [snapshot](evidence/p9-05-2026-09-09/snapshot.log). Vite's large-chunk advisory remains.
- **20 current city browser cases** (ten seeds × 1366/900 widths): initial fixture hashes, paused state, ordinary name/pin controls, camera-only hash stability, map/world, local save/reload and exact full replay. **Two older-save cases** preserve the revision-2 hash and all 96 historical commands with incomplete fresh replay provenance. [Script](evidence/p9-05-2026-09-09/city-browser.cjs), [results](evidence/p9-05-2026-09-09/city-browser-result.json).
- **Eight paused start/save cases** (four shared starts × both widths), no horizontal overflow or page errors: [script](evidence/p9-05-2026-09-09/browser.cjs), [results](evidence/p9-05-2026-09-09/browser-result.json).
- **Both widths:** actual order and truck controls, paused-job local save/reload/resume, chest/cargo-paid completion without spending pockets, ordinary walking and chest input UI, direct belt loading to **40 turret rounds** and full replay: [script](evidence/p9-05-2026-09-09/construction.cjs), [results](evidence/p9-05-2026-09-09/construction-browser-result.json). This is a passing example, not a diagnosis or closure of P6-AMMO.
- Live 1× natural dawn warning at 2400 for dusk 3300; G/K leave state and engineer unchanged: [result](evidence/p9-05-2026-09-09/warning-result.json). Both widths restore optional Turbine through the ordinary button, spend exactly 40 concrete and match full replay: [result](evidence/p9-05-2026-09-09/restoration-result.json).
- **80/80 live lamp/pole edits**, four five-second 1× samples with item/recipe information open, real paid command hooks. Samples ran alongside other checks on this host in headless Chrome; they are neither human play nor reference-machine certification.

| Start / width | Successful edits | Mean frame ms | P95 ms | Max ms | Mean light-paint ms |
|---|---|---|---|---|---|
| fresh / 1366 | 20/20 | 16.67 | 16.80 | 16.80 | 12.88 |
| fresh / 900 | 20/20 | 17.01 | 16.80 | 116.60 | 27.96 |
| network / 1366 | 20/20 | 17.01 | 16.80 | 116.80 | 28.12 |
| network / 900 | 20/20 | 16.67 | 16.80 | 16.80 | 15.90 |

The maximum sampled frame interval is **116.8 ms**; no performance fix or threshold is inferred. Earlier 266–300 ms observations, reference-machine work and D-SA-2/C3/D-B2-2 remain open. Visually inspected the actual compact seed-842 map and desktop completed construction screenshots: markers and controls are present, the narrow layout stacks and scrolls, and current panels remain dense. These images do not establish human legibility/recognition; RI-02B remains the planned UI work.

Documentation/profile sync, freshness, native whitespace check, exact HTTP/archive/source identity, preserved historical evidence, current experiment identity, report references and task/blank-record consistency are recorded in [docsync](evidence/p9-05-2026-09-09/docsync.log), [freshness](evidence/p9-05-2026-09-09/freshness.log), [diff check](evidence/p9-05-2026-09-09/git-diff-check.log), [checker](evidence/p9-05-2026-09-09/check.py) and [verification](evidence/p9-05-2026-09-09/verification.json).

## Human preparation and next work

The separate guide prescribes a comparable route on each declared seed: distinguish places before consulting the map, describe alternatives and a meaningful route choice, exercise names/pins/return/save-load, and distinguish inspected access from actual paid construction/freight/truck completion. It records omissions and assistance rather than requiring ten full campaigns or treating a walkthrough as a phase pass. Shared starts support remaining P5–P8 construction, supply, defence/recovery, discovery/Turbine, direct-ammo and performance observations.

[Prior owner Phase 5 success](PHASE_5_PLAYTEST_REPORT.md) remains credited on its identified build. **No new human session or phase verdict has been supplied.** P9-H is available with the blank record; EX-08H, P6-H, P7-H, P8-H and remaining T18 observations remain open. P6-AMMO awaits the actual owner retest/affected save. Fairness, recognisable places, route choices, repetition and enjoyment cannot be inferred from automated success.

P9-05 engineering/preparation is complete. **Next implementation: RI-02B-UI-01**, shared visual tokens and shell/input foundations, under D-UI-02. Phase 9's human verdict remains separate. Wider Q07 progression/endgame, the broad EX-08 assessment and later content are not authorised by this readiness report. No commit or push was performed.
