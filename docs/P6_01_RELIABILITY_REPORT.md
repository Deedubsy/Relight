# P6-01 — attack approach and lifecycle reliability

Date: 2026-09-07. Authorisation: D-EX-29, owner request “Ok lets move to P6-01”. Baseline `17939145b5d40afe4dcfc1937a3ec6d34b763df6`, branch `codex/tram-expansion`.

## Implementation specification

Retain the dawn-locked base and origin as warning identity. Prefer that origin for each spawn; when occupied or unsafe, choose the nearest empty staging tile within 12 ground-path steps, still on the same compass approach and 20–60 breach-path steps from the core. The fallback cannot cross another intact wall/turret or move closer to the core than the original entry, so relocation cannot bypass the player's defences. Preserve the home court boundary exclusion and eight-tile player safety distance. Deterministic distance/tile ordering breaks ties. The radius and one-second retry are provisional engineering defaults specified for this task, not balance approvals.

If no staging tile is valid, retain the finite roster and mark the assault delayed at its approach, retrying once per simulation second. Minor withdrawal at dusk gets a distinct waiting reason; no major body spawns until the minor group physically leaves. Target, advertised dusk and warning identity never reroll. The next protected rest interval still starts from actual completion.

A crawler begins withdrawal by discarding its attack waypoint once, then follows the physical route to its actual birth tile. Paid walls on that route are breached; protected production remains traversable under the existing hostile-movement contract. No stuck-body deletion or teleport completes an assault.

Defence metadata advances from version 1 to **2** independently of the unchanged schema-3/campaign-5 envelope. Old saves retain targets, rosters, promised windows, health, inventories and paid repair jobs. Obsolete movement waypoints are cleared once so bodies replan from their saved position. The browser marks older defence command histories incomplete for replay against the current factory. Fresh sessions retain full replay and new saves retain deterministic continuation.

Save validation now rejects mismatched body/group targets, duplicate attack/body identities, unaccounted major roster entries, invalid deferred/withdrawal states and nonadjacent current-version waypoints. Pursuit clears an old attack waypoint before moving away; withdrawal does so once on transition. The base target and received radio warning remain stable through fallback and outage.

Implementation is in [campaignThreat.ts](../packages/sim/src/campaignThreat.ts), [campaignDefence.ts](../packages/sim/src/campaignDefence.ts), [defenceValidation.ts](../packages/sim/src/defenceValidation.ts), [threat.ts](../packages/sim/src/threat.ts) and the [session loader](../packages/game/src/session.ts). Gameplay remains simulation-owned. The renderer continues to display the ordinary simulation warning text.

## Verification

Final code verification passed:

- **243/243 simulation tests**, including eleven new [reliability tests](../packages/sim/test/campaignReliability.test.ts) and expanded [district milestone coverage](../packages/sim/test/campaignDistricts.test.ts). [Full log](evidence/p6-01-2026-09-07/tests-final.log).
- All package typechecks and production build. [Build log](evidence/p6-01-2026-09-07/typecheck-final.log). Vite retains the existing large-bundle advisory.
- Lint and the unchanged legacy three-hour snapshot. [Lint](evidence/p6-01-2026-09-07/lint.log), [snapshot](evidence/p6-01-2026-09-07/snapshot.log).
- Both documentation profiles, evidence freshness, local references, task consistency and source/build/history checks. [Documentation](evidence/p6-01-2026-09-07/docsync.log), [freshness](evidence/p6-01-2026-09-07/freshness.log), [integrity](evidence/p6-01-2026-09-07/consistency.log).

The final [manifest](evidence/p6-01-2026-09-07/build-manifest.json) identifies the tested working-tree source and build; HEAD alone is not their identity. Independent source/build archives accompany it. Earlier EX/P5 reports, experiments and frozen archives are preserved. Their config hashes still match unchanged balance constants, but that does not make their historical measurements new evidence for the changed routing code.

## Coverage and limits

The 18-case matrix covers home and both stations on seeds 3/4/5 and holdouts 8/11/13. It checks safe fallback after paid walls, belts or poles, or player presence where natural origin ground cannot be built on, retaining the same target/compass approach. The broader sweep checks 96 fresh origins across three base positions on seeds 1–32. These use explicit registry/clock/position fixtures, not claims about the time or economy needed to restore every base.

Additional checks cover a fully occupied staging area, saved deferral, normal removal and replacement of an obstruction, physical breach of a blocked withdrawal tile, minor departure before major spawning, stable restoration tie-breaks, disabled/recommissioned targets and protected windows. Received upgraded radio intelligence survives fallback, power loss and load. The genuinely earned third-base freight milestone is saved during a pending lock without moving that promise; the subsequent isolated completion applies one quiet cycle.

A declared pre-dusk checkpoint logs ordinary paid wall placement and walking, then runs the resulting unprepared assault through defeat and physical withdrawal. Its saved continuation and complete replay from that checkpoint match, and item accounting is unchanged. This is checkpoint replay, not a claimed unaided campaign from a fresh start. Existing fresh-command replay, normally paid full-roster turret defence and discovery regressions also pass in the full suite. P6-03 still owns supplied-network balance and unattended defence evidence.

Initial focused checks passed 32/33 and then 8/9: holdout fixtures attempted walls on rubble and on non-buildable natural origin ground. The corrected fixtures use permitted paid poles or player presence and retain normal placement refusal. No gameplay expectation was relaxed to allow illegal construction. A subsequent focused run passed 16/16; the final full suite includes the additional radio check. Initial/final logs remain separate.

No new browser or human session ran for this simulation/loader increment. Warning states are checked through their actual sim query and the production game is rebuilt; P6-02 owns the new visual controls and their browser checks. Known light-map repaint and reference-machine performance findings remain open. No clock, roster strength, HP, repair price, rest policy, new enemy or progression contract was retuned. P6-01 is complete; P6-02 is next. No commit, merge or push performed.
