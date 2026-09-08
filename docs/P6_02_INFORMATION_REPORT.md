# P6-02 — defence information and navigation

Implemented 2026-09-07 under D-EX-30, following the owner's “continue” after the P6-01 handoff. This completes the bounded acceptance in [the Phase 6 review](PHASE_6_SCOPE_REPORT.md). P6-03 supplied-network defence evidence is next; this is not a Phase 6 or human-play verdict.

## Result

- Turret placement and selected F inspection show a contrasting circle with cardinal ticks, centred on the footprint and reading the same **9-tile `TURRET_RANGE`** as weapon targeting. Inspection retains actual HP, ammunition, repair, circuit and range text. Removing the selected identity cannot select a replacement machine accidentally; Escape clears inspection.
- **G / View threat** explicitly centres the world camera on the known base; **K / Return to engineer** restores following. Direction is displayed in text and an arrow when the base is outside the world viewport. WASD also resumes following when the player deliberately moves. A held pointer must be released before an explicit camera jump. New warnings never jump the camera, move the engineer or cancel a construction gesture. These are renderer controls, not gameplay commands or saved intelligence.
- `knownCampaignThreat` exposes only a matching received major warning, the guaranteed pre-radio home target, an existing minor raid or a disabled base needing recovery. A current minor raid takes navigation priority over a future major warning; the warning text shows both. Radio outages retain received information but do not reveal an unreceived network target or reuse a previous assault's warning.
- `campaignCrawlerAction` is read-only and shares the actual movement/damage decision with the campaign tick. Hover text and target lines identify an immediate turret attack, wall breach, engineer contact/pursuit, advance, blocked route, guarding or withdrawal separately from the strategic core/exit/ruin destination. Existing damage, speed, roster, range and radio rules are unchanged. Long hover text wraps inside the viewport.

Sources: [campaign threat queries/tick](../packages/sim/src/campaignThreat.ts), [shared targets](../packages/sim/src/threat.ts), [world rendering](../packages/game/src/worldScene.ts), [camera controls](../packages/game/src/main.ts), [inspection](../packages/game/src/panel.ts), [bindings](../packages/game/src/controls.ts).

## Verification

**247/247 tests pass**, including four new information regressions: hidden/stale radio targets and saved receipt; home/minor priority; paid wall breach during attack/withdrawal with saved continuation; real turret/engineer damage matching the immediate target. Existing P6-01 lifecycle tests and campaign defence tests remain green. Required all-package typechecks/production build, lint, unchanged legacy three-hour snapshot, documentation profiles and freshness checks passed; local references, task order and historical evidence integrity are checked in the [evidence directory](evidence/p6-02-2026-09-07/).

Actual Chrome mouse/keyboard checks at **1366×900 and 900×900** cover G/K, button Enter activation, unchanged engineer/hash/tool during camera inspection, Ctrl+S/Ctrl+O, unknown radio target from map start, warning/minor/assault/withdrawal/recovery states, placement and F-selected range rings, and immediate enemy hover. Screenshots were visually inspected for the range rings, wrapped enemy feedback and compact warning controls. No horizontal overflow or browser exceptions occurred.

An additional live check starts from a declared checkpoint two seconds before dusk: the remote assault begins while a belt placement is held, preserves the stationary engineer and camera/tool, and permits actual movement afterward at both sizes. Restored sites, clock and radio receipt are injected only in labelled UI checkpoints; turret construction and stock withdrawal are paid commands. These checks do not establish normal progression, combat balance or human comprehension.

A separate fresh-stock interaction check feeds the existing generator, places a pole/lamp and repeatedly picks up/replaces the lamp through the ordinary command-recording hooks. Both 4.5-second samples assert live 1× simulation time advances and all nine edits succeed. At 1366/900 widths, eight changed texture paints averaged **16.1/14.1 ms**, with sampled RAF maxima **16.8/16.9 ms**. A **31.6 ms** repaint maximum occurred during setup; render-frame work peaked at 24.2 ms in one sample. These local headless Chrome measurements do not resolve the previous larger-workload stalls or the reference-laptop/GPU gate. The initial paused timing sample was detected and replaced with an explicit live-time assertion; it is not cited as live performance evidence.

Development findings retained in logs: the first build found an unreachable narrowed turret comparison; the first focused run found missing immediate-action hover text (24/25); both were corrected before final verification. An initial browser check aimed without the canvas's 12-pixel page offset and was corrected. The final scripts/results and screenshots identify actual coverage; no human observations are inferred from automation.

## Provenance and handoff

Branch `codex/tram-expansion`, parent commit `17939145b5d40afe4dcfc1937a3ec6d34b763df6`. Phase 6 review, P6-01 and P6-02 changes remain **uncommitted**. The local port-5176 production preview serves the rebuilt output; reload to use it. Historical port 5175, prior reports and evidence remain untouched.

The [source/build manifest](evidence/p6-02-2026-09-07/build-manifest.json), [source archive](evidence/p6-02-2026-09-07/source.zip) and [build archive](evidence/p6-02-2026-09-07/build.zip) identify the uncommitted implementation independently of its parent commit. P6-03 now owns ordinary-stock network defence, preparation/travel, supply interruption and recovery measurements. Wider progression Q07, shared EX-08B preparation and outstanding human criteria retain their dependencies.
