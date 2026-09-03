# Relight — decisions

Every constant, direction and verdict a human makes, and every one that is still open. Format per line: **ID · date · status · what · evidence · who decided**. The programme never picks a value from this list; it writes the question and the evidence. `open` means nobody has decided; `made (pre-programme)` means a prior session applied it to the doc before this log existed and a human has not put their name to it; `made` means a human did.

## Open constants (Phase 0 list; Phases 1–4 aim to evidence each)

| ID | Constant | Doc value today | Other values in play | Evidence to date | Question for the human | Status |
|---|---|---|---|---|---|---|
| C1 | Edges fed per assembler | ~15 (§12: one assembler = 20 mag/min; §5 in-play 1.37 mag/edge-min) | proto Mk1 10 mag/min, Mk2 20 → 7 / 15 edges | E5 (`FRONT_FIX_REPORT.md`): demand 8–48 mag/min by 3 h; calibration 2: one Mk1 feeds a 4-block blob to 1:59 | Is the tempo "one assembler per ~15 edges" (an assembler every ~5 claims) or should the proto's Mk1/Mk2 split stay? | open |
| C2 | Claim cadence the game is drawn for | ~5 min after hour one (§18, `[sim: E9-claim-cadence]`) | 8 min (sim before `CALIBRATION_REPORT.md`); 15 min in hour one | E9-claim-cadence: 5 min matches §18's 25 h count; 8 min does not | The §18 drawings assume ~5 min; is that the game, or should the drawings follow a slower cadence humans actually play at (Gate A telemetry)? | open |
| C3 | Machine slots per block | unstated (§5 "machine slots on interior blocks"; §4 lot 24×24 tiles) | 1 (proto, PROTO-ASSUMPTION) | calibration 2: 1 slot makes enclosure the only way to a second assembler; T1 (compact amber inside 60–120 min) missed and called structural | One slot, or a footprint budget per lot (§13 gives 3×3 machines on a 24×24 lot ≈ dozens)? | open |
| C4 | Magazine buffer cap | none stated | 400 magazines / 4,000 rounds (proto) | calibration 1/2: pips leave green only after the buffer is empty, so amber → red is ~2 min | Is a shared buffer a game object (a chest holds 400, §13) or a proto shortcut to delete? | open |
| C5 | HQ start patch sizes | steel "small" (§11); coal ~3,000 (§11/§12) | coal ~700 (`TEST_RESULTS.md` §8); proto steel 7,680 at 64/min, empties at 2:00 | calibration 2: 3,000 coal mines out at ~106 min, 700 at ~30 min; hour-one power lesson depends on it | Which hour does the start coal run out in, and should steel run out first (2:00 in the proto)? | coal **decided by D1: ~700 (west wanted by minute 30)**; steel patch still open |
| C6 | Fall time after the substation stops | 90 s (D2) | 5 min (pre-D2) | E8-fall16: at 5 min nothing ever fell; at 90 s falls happen but humans have not seen one | Is 90 s a rescue window a human can act in (Phase 3 smoke test), or an ambush? | **made** (D2 ratified 2026-09-03); Phase 3 `[play]` check of the window remains |
| C7 | Substation draw | 100 kW front / 20 kW interior (D1) | 200 / 40 (pre-D1) | E4h-*: at 200/40 the start patch could not carry hour one; at 100/20 + four Generators it can | Ratify D1, or keep 200/40 and change the Generator instead? | **made** (D1 ratified 2026-09-03) |
| C8 | Bloom timer base and per-bloom drop | `120/(0.5+d)`, 10 % | `240/(0.5+d)` halves fights, −30 % ammo; 20 % drop −24 % ammo (E10) | E10-bloom-cadence | Which rhythm feels right? Gate A, `[play]` | open |
| C9 | Assembler rate | 20 mag/min (§12, follows from the 3 s recipe) | proto Mk1 10, Mk2 20 | with C1 | Is there a Mk1/Mk2 ladder in the game or only in the proto? | open |
| C10 | Start ammo | 20 magazines (§11) | sim `startRounds` 300 = 30 magazines | none | Reconcile in the Phase 1 doc pass; which is it? | open |

## Decisions applied to the doc before this log existed — ratified 2026-09-03

| ID | Date | What | Evidence | Decided by | Status |
|---|---|---|---|---|---|
| D1 | ratified 2026-09-03 | Substation draw 100 kW front / 20 kW interior; HQ starts with four Generators; **start coal patch ~700 units** (was 3,000 — that number mined out at 106 min; the intent is "west wanted by minute 30"). Reason given: at the halved draw E4 showed machines, not substations, set the Generator count (660 of 980 kW), so ammo is the hour-one lesson. | `CALIBRATION_REPORT_2.md`, E4h-* | human | **made** — doc §11/§12 coal ~3,000 → ~700 in the Phase 1 doc pass; closes C5 (coal) and C7 |
| D2 | ratified 2026-09-03 | Fall time 90 s after the substation stops. Reason: the 30 s rescue window against the 60 s refeed reset is what makes a red pip a call to action rather than an obituary. | `CALIBRATION_REPORT_2.md`, E8-fall16 | human | **made** — closes C6; Phase 3 still checks a human can act in the window `[play]` |
| D3 | ratified 2026-09-03 | Unshot-arrival counter is per block; a refeed within 60 s resets it. Corners fail faster, which is right. | `CALIBRATION_REPORT_2.md` | human | **made** — §5 must say it, so the prototype's corner falls are not read as a bug (Phase 1 doc pass) |
| D4 | ratified 2026-09-03 | The Relight hold is survivable by banking stock. Ratified as a **design statement, not a number**, explicitly pending E9-full (Phase 10 E20). | `CALIBRATION_REPORT_2.md`, E11-relight-hold | human | **made (direction)** — the numbers stay open until E9-full |

## Reopened

| ID | Date | What | Evidence | Question | Status |
|---|---|---|---|---|---|
| D-25-13 | 2026-09-03 | §25 item 13 "Spike on the scattered map — closed, loses nothing" no longer matches the sim | `CALIBRATION_REPORT.md`: at the 5-minute cadence spike loses 4–11 blocks on the scattered map, 2.2× compact's ammo | Does the doc say "spike is punished by losses" (retag, §7/§17/§25 rewrite) or is a spike that bleeds blocks the wrong tuning (change dmax/g/cadence)? | **reopened by the human 2026-09-03** — §25 item 13 goes back to open in the Phase 1 doc pass; Phase 1 E7 reruns it at the 5-min cadence and the human decides on that evidence |

## Direction / verdicts

| ID | Date | What | Evidence | Decided by | Status |
|---|---|---|---|---|---|
| D-CI | 2026-09-03 | **GitHub private repo, GitHub Actions, npm workspaces.** One monorepo, `packages/*` as workspaces, `main` protected behind CI. On every push: lint, `tsc --strict` on all packages, `npm test` (fixtures), and the experiment suite at three seeds (E1–E9 at 5 h × 3 seeds × 4 policies; at ~60 k ticks/s well under a minute, cheap enough to gate on). Nightly: the 10,000-seed and 25-hour runs, results uploaded as artifacts and a summary committed to `docs/experiments/`; anything that needs hours runs on a self-hosted runner on the WSL2 box if Actions minutes get tight. The Python sim stays only as the fixture exporter until the TS sim has been the reference for a phase; then it is retired and the fixtures frozen. Linear's GitHub integration for task links. | Phase 0 inventory | human | **made** — Phase 1 sets it up (first task) |
| Gate A | — | `TEST_RESULTS.md` `verdict: go | rework | kill` | `PROTOTYPE_TEST_PLAN.md` | human | pending — not run |

## Phase 0 report decisions (the three, restated from `PHASE_0_REPORT.md`)

1. **Ratify or reverse D1–D4** — ratified with corrections 2026-09-03 (see table): D1 adds coal patch ~700; D4 is direction pending E9-full.
2. **D-25-13** — reopened 2026-09-03; Phase 1 E7 supplies the evidence.
3. **D-CI** — decided 2026-09-03: GitHub private repo + Actions + npm workspaces (see table).
