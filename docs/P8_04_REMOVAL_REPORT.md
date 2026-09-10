# P8-04 — conserved area removal and construction order controls

2026-09-08 · `codex/tram-expansion` · parent `8e54ed602da159905b0428f316f6e75cb25ae351` · local changes, not committed or pushed. Scope: [P8-04 acceptance](PHASE_8_SCOPE_REPORT.md), [D-EX-43](DECISIONS.md). P8-05 remains a separate integration review.

## Delivered behaviour

Foreman clipboard controls now include Select removal area and Pack selected machines. Dragging only previews: whole footprints, packed-machine/content returns, loose-round buffer returns and a refusal reason. Up to 128 machines within 128×128 tiles are one transaction. Apply checks that every selected machine identity, position, direction and setting still matches, then rechecks current contents, normal reach, eligibility and cumulative pocket capacity. Any failure changes nothing. New occupants and replacements require a new selection. No preview is persisted as gameplay state.

The existing constructor packs the entire valid group and records one history action. Loaded belt/tunnel items, held items, cargo and recipes/routing/freight settings retain the existing recovery rules. Loose turret rounds require sufficient buffer space. Busy assemblers/Mixers, damaged or repairing defences, fractional stock and restored supply installations refuse. Trams pack before their underlying tracks; undo restores tracks before vehicles. Fixed core/cabinet/restoration/shelter objects are not removable machines. Undo rebuilds settings/identity using current pockets/reach and leaves recovered contents in pockets; it can refuse new obstructions or insufficient stock. Neither undo nor redo rewinds production.

Waiting orders now have Pause order / Resume order. Paused plans reserve their footprint, skip truck work and block Build remaining; ordinary matching placement can still complete them. A saved optional boolean retains plan schema 1 and metadata 10. All-paused work waits visibly instead of finishing the job. Truck controls add Retry truck now and Use selected supply chest. Source changes clear the old route while retaining cargo, order IDs and manual pause. Repeating identical Start, pause/resume, retry, source assignment or cancellation preserves state. A changed destination uses cancel → queue at the new position → assign; already-built machines and cargo remain. Finished records never silently reopen.

## Automated verification

| Check | Result | Evidence |
| --- | --- | --- |
| Complete simulation suite | 313/313 passed, 432.45 seconds; no failures/skips | [Full log](evidence/p8-04-2026-09-08/full-test.log) |
| Final focused removal, construction, plans and truck tests | 40/40 passed, 14.23 seconds; includes changed destination and malformed paused-save checks | [Focused log](evidence/p8-04-2026-09-08/focused-final.log) |
| TypeScript checks and production build | All four packages passed; existing Vite large-chunk advisory remains | [Final build](evidence/p8-04-2026-09-08/typecheck-final.log) |
| Lint | Passed | [Log](evidence/p8-04-2026-09-08/lint.log) |
| Generated documents and freshness | Passed for both profiles; campaign constants unchanged | [Documentation](evidence/p8-04-2026-09-08/docsync.log), [freshness](evidence/p8-04-2026-09-08/freshness.log) |
| Browser at 1366×900 and 900×900 | Both passed; no page errors or horizontal overflow | [Results](evidence/p8-04-2026-09-08/browser-result.json), [desktop preview](evidence/p8-04-2026-09-08/preview-1366.png), [compact preview](evidence/p8-04-2026-09-08/preview-900.png) |

Browser actions use the unchanged, fully command-replayed P8-03 station/Foreman/chest checkpoint. Real DOM controls pause/resume the order, assign/repeat/pause/resume/stop the truck, select the same source and request retry. Build remaining pays from packed pocket stock. Mouse dragging and cancelling leave the gameplay hash unchanged; explicit Pack removes the selected two belts and returns both. Keyboard undo/redo, local save/load and full opening-command replay preserve the expected state. Only a camera zoom helper is used; there are no direct gameplay edits. Screenshots were visually checked at both widths.

Focused regressions additionally cover changed identities/settings and newly occupied selections; moved engineer reach; cumulative full pockets; busy assembler/Mixer, damaged defence and protected installed chest; fractional inventories/cargo; loaded belt and hidden tunnel items; turret loose-round overflow/recovery; held items, cargo and freight/filter/priority settings; tram-before-track removal and reverse reconstruction; saved pause state, source depletion/removal/replacement, mid-route obstruction, manual takeover and destination cancellation/reassignment using retained cargo. Existing underground pairing and paid full replay checks remain included.

The focused fixtures explicitly label injected Foreman/unlock state, loaded contents, damage, fractional stock and capacity setup. Exact inventory/buffer/hash assertions apply to those scenarios; injected items are not credited as earned production. Paid supply, transfer, build and replay scenarios retain conservation checks. Early fixture preparation failures (incorrect recruit kind, pre-unlock tram stop and unreachable near-lot track placement) were corrected through the proper recruit/station and ordinary walking setup; the retained [fixture-failure log](evidence/p8-04-2026-09-08/focused-fixture-failure.log) records the final such failure. No production expectations were weakened.

## Repeated-work comparison

[Machine-readable comparison](evidence/p8-04-2026-09-08/comparison.json) runs the same six-belt layout through build → pack → rebuild → pack. Both paths buy six belts once and reuse them. Opening supplies use ordinary withdrawal; the Foreman is a labelled fixture unlock and the common reachable layout is selected by the test. No gameplay time advances during these commands.

| Path | Commands, including source preparation | Material cost | End stock | Failures | Headless wall time |
| --- | ---: | --- | --- | ---: | ---: |
| Individual construction | 24 | 6 steel | 6 packed belts; same remaining steel/copper | 0 | 64.69 ms |
| Clipboard and area removal | 10 | 6 steel | 6 packed belts; same remaining steel/copper | 0 | 213.85 ms |

The tool path includes copying its ordinarily built source, both previews and all apply checks. It uses fewer commands and more headless processing time in this sample. These are local automated timings, not human task duration, frame certification or evidence that the tools feel less tedious. P8-05 and the post-P8 human session retain performance/usability work.

## Evidence and handoff

The [manifest](evidence/p8-04-2026-09-08/manifest.json), [source archive](evidence/p8-04-2026-09-08/source.zip), [build archive](evidence/p8-04-2026-09-08/build.zip) and [verification](evidence/p8-04-2026-09-08/verification.json) identify this task's files. The mutable development preview is [port 5178](http://127.0.0.1:5178/?rules=exploration-v2&seed=3&view=world); the automated browser fixture reuses P8-03's paid opening checkpoint. P8-03 and earlier reports, archives and frozen EX-08B/C builds remain unchanged. Campaign balance constants/config fingerprint are unchanged; existing campaign measurements are prior P8-03 evidence, not rerun or relabelled P8-04 trials.

Implementation: complete. Automated validation: passed as recorded above; archive/reference checks are recorded in verification.json. Human approval: owner authorised task scope only. Human play: not run for P8-04. Next: P8-05 integration, then EX-08D preparation and shared play. P6-AMMO still needs the owner’s retest. Performance, wider progression/Q07 and formal human verdicts remain open.
