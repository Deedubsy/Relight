# Relight — deferred obligations

Only things the programme still owes and no task carries yet. When an item gets a task,
its line becomes a link to that task and is removed at the next phase exit. The history
of every item that was parked, kept or deleted since Phase 0 (twelve original items and
twenty-four "Re-read at …" sections) is `archive/pre-phase-5-cleanup/DEFERRED-2026-09-05.md`.

## Still unscheduled

- **The reference-machine soak by hand** (owner: human; since prompt B M4). The headless
  swiftshader fps number is for regressions only; the real number needs the reference
  machine. Wanted before Phase 11's engine gate.
- **The stranger test** (owner: human; T7 waived 2026-09-05). Eight questions to a person
  who has not seen the game, on the layout pass's success condition ("a stranger points to
  street, lot edge, lit area, rubble, Depot and engineer unaided"). Wanted before Phase 12.
- **The calibration pip bands are the human's word** (since Phase 3). Amber ≈ 3 min, red ≈
  6 on every seed is measured (`EXPERIMENTS.md` `E-hour`); whether those bands read right
  is a play observation nobody has recorded. Rides on any played session that reaches a
  red pip (T19 may).
- **The headless soak script's home** (since prompt B M4). `soak.cjs` (Playwright over the
  Vite preview) lives in a session scratchpad; committing it under `packages/tools` is
  proposed and undecided.
- **Gate B's unrecorded observations for the next played hour past minute 30**: the first
  shade (§11 and E3 put it at minute 32–47; the bot's six hours produce none), shades on
  lit tiles, the burn-off question ("did the lights coming on land?"), copper for repairs,
  repair-by-E, light on the map view. T19's opening part stops at minute 20; its RI-08 session runs longer
  and can record them (`PROGRESS.md` T19).
- A sink for stone (RI-01, 2026-09-05): rubble is finite typed ore and east's line digs
  1,750 stone in the 75-minute hour on seeds 3 and 5 with nothing to spend it on
  (`E-hour-ledger`); a stone-consuming use is a new mechanic — a separate, visible
  decision, nobody's task yet (`RI_PASS_1_REPORT.md` "RI-01").
- **Basic sound and volume / mute controls** (plan §11.2; RI-02, 2026-09-05): not in
  RI-02's row and not built. Today no essential information travels by sound or colour
  alone (the machine status marks are shapes with words); final audio is Phase 12's.
  Nobody's task yet.

## Scheduled — link only

- Every front edge physical (restore from commit 6694b71) → **RI-03** (field deployment); the rest of T13 → **RI-09**.
- D-P4-11 (the idle steel, the Mk2 purchase, the hand-feed) → measured by **T12b**, then
  the human's row.
- The controls walkthrough's four STANDARDS rows (4.3 / B.3 / B.6 / C.2) → **T19** (now RI-08's human session).
- The Phase 5 gate deletions (`--map lattice`, `?map=lattice`, lattice fixtures and
  results, `?flow=0`, `sim.ts` `syncEdges` GA-B1-15) → **T17**.
- The light-map blur's browser frame cost and the map view's resize raster → **T17**'s
  full verification.
- D-GB-1's hybrid loop → **RI-03**, **RI-05**, **RI-06** (`REVISED_DEVELOPMENT_PLAN.md` §4–§9
  is its definition; D-RI-4 superseded the Phase 6 placement on 2026-09-05).

## Closed at the cleanup (2026-09-05), with the evidence

- "Generated-report freshness in full" — built by the guardrails task (Step 6,
  2026-09-04): `npm run freshness:check` stamps and checks `EXPERIMENTS.md`, every
  `docs/experiments/*.md|json`, the snapshot, the §18 and seed images; green at HEAD on
  2026-09-05. The item's premise (only the snapshot was checked) no longer holds.
