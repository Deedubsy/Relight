# Relight — programme constitution

**Owner implementation direction (2026-09-09):** implement EX-09C-01–09 plus RI-10/RI-12 combat/encounters now. The active campaign uses defended factory/plant sites, never a territorial frontage/ring or adjacent-block-claim economy. Site lifecycle and explicit truck recovery replace the former EX-09C-04 territory scope. Normal new-game entry is to use exploration-v2; explicit legacy saves/mode remain isolated. Real inventory, connected finite power and 1×/pause remain authoritative. Q07 final requirements and human/release gates stay open, not prerequisites of this authorised correction set.

Current version: 2026-09-06, D-EX-01–11. The owner has confirmed Phase 4 complete and authorised the documentation revision. The previous constitution is preserved in [the archive](archive/pre-exploration-defence-2026-09-06/docs/CONSTITUTION.md). Rule numbers remain stable for historical citations.

## Scope

Relight is a 2D city-reclamation factory game whose revised loop is explore → restore → connect → produce → defend → relight. Free roaming, useful discoveries, specialised districts, a multi-station tram network and intermittent base defence are the adopted direction. [RELIGHT_CONFIRMED_GAMEPLAY.md](RELIGHT_CONFIRMED_GAMEPLAY.md) holds the current owner-confirmed gameplay rules; [RELIGHT-design.md](RELIGHT-design.md) retains implementation and legacy references; [EXPLORATION_DEFENCE_PLAN.md](EXPLORATION_DEFENCE_PLAN.md) defines delivery; [PROGRESS.md](PROGRESS.md) alone orders tasks. Phase 4 is the last completed phase; selected later features already exist.

## The rules

1. **Design, code and evidence.** The GDD states intended rules, code implements them, simulations measure named implementations under stated conditions, and humans judge the experience. Label intended/unbuilt behaviour, existing implementation, tuning proposals and observations separately. Generated tables never silently redefine the game. The GDD's legacy appendix describes old code and measurements only.

2. **The scope test.** A feature strengthens the revised loop, has a clear implementation boundary and justifies its interaction/testing cost. No inferred survival needs, settlement happiness, dialogue trees, XP, loot-tier system, random enemy equipment drops, mandatory reflex-heavy progression gates or railway signals. Rare site discoveries and personal defence participation are in scope, with their catalogue and damage/repair rules explicit. The historical rejection of timed assaults and day/night planning is superseded by D-EX-03/04. Daylight's mechanical effects are still open.

3. **Headless first.** Pure TypeScript simulation owns gameplay through step(state, commands). The renderer draws state and submits commands. A red check is recorded and investigated within its declared profile; legacy behaviour and revised gameplay have separate acceptance.

4. **Gameplay rules live in the design reference.** The owner-designated RELIGHT_CONFIRMED_GAMEPLAY.md takes precedence over earlier GDD and decision wording where they disagree. This file defines authority and process, not a second set of gameplay constants. Decisions record approvals and supersessions; the GDD incorporates their current meaning.

5. **Bots are instruments; people judge experience.** Bots use ordinary player commands, inventories and reach. Debug stock or bypassed commands make a run a labelled scenario. Bot success does not establish enjoyable exploration, warning comprehension or reduced repetition.

6. **Assumptions are tagged.** Distinguish owner-approved direction, approved values, authorised tuning ranges, recommendations and implementation assumptions. D-EX-Q01–06 are adopted through D-EX-11; Q07 owns the unresolved later campaign contract. Unspecified tuning is task-level work. Routine reversible implementation choices may proceed within agreed scope.

7. **Decision authority.** The owner decides core rules, progression, scope, player-facing acceptance and phase gates. Current user instructions supersede older records. Record direct authorisation accurately; do not invent names, quotes, approvals or earlier playtests. An unresolved contract blocks only work that depends on it. The documentation request authorises documentation, not every illustrative feature or an external merge/push.

8. **Teach through the game.** Use state-driven guidance, tooltips, maps, warnings and visible results. No new quest/dialogue framework or tutorial-screen system is implied. Guidance should support self-directed roaming and reflect actual radio/discovery information.

9. **Phase exit.** Record implementation, automated checks, human decision and human observations separately; check current acceptance and retained cross-cutting obligations. Preserve completed Phase 0–4 history. Forward-built features do not complete their later phase. Revise future acceptance explicitly rather than treating an old gate as evidence for a new requirement.

10. **Stop conditions.** Seek a decision only when a necessary core choice is unresolved or scope genuinely changes. Complete independent authorised work first. Do not infer permission for destructive operations, evidence deletion, pushes, merges, deployment or external messages. An environment-blocked check is a validation limitation, not permission to falsify success or silently revise expectations.

11. **Provenance.** Every decided row names the owner/person as actually known, date and authorising conversation/report. Distinguish design approval, implementation measurement and human observation. Preserve historical reports and stamps; never regenerate them merely to turn checks green. Old decision counts and old acceptance statements are historical once superseded.

12. **Undecided numbers.** List required missing values under their contract with recommendations. The 20-minute cycle and other stated starting values are adopted through D-EX-11; initial duration/raid-strength targets still need validation. Do not invent an unspecified radio range, kit cost, HP or repair rate as a locked value or alter adopted settings without the relevant recorded decision. Numerical uncertainty does not block document migration or independent technical analysis.

13. **Four separate completion claims.** Report implementation complete, automated validation (not_run/passed/failed/partial with evidence), human approval, and human play evidence independently. A documentation task can complete its editorial scope while reporting environment-blocked validation; it cannot claim all checks passed. Old code and revised gameplay never share an unlabeled completion claim.

14. **Provisional technical choices.** Within approved scope, reversible technical defaults can be recorded as provisional with their origin and limits. This is not authority to decide open core contracts, secretly promote candidates, change locks or attribute an assistant proposal to the owner. Unaffected work proceeds while dependent decisions remain open.

## Verification policy

For meaningful code changes: npm test, npm run typecheck, npm run lint and npm run docsync:check, plus focused experiments for changed behaviour. Full profile-appropriate verification is required at phase gates or when requested. Choose tests that exercise the actual changed contract.

For documentation changes: docsync:check, freshness:check and local referenced-path/consistency checks. Record environment failures separately from content failures. No unrelated simulation is required for prose edits.

EX-03 establishes explicit legacy and Version 2 profile routing for fixtures, snapshots, generated tables, experiments and save state before changing gameplay constants. Existing 75-minute and frontage measurements remain reproducibility evidence of the old game. They neither veto adopted Version 2 rules nor become new-game acceptance through relabelling. Profile promotion and deliberate expected-result changes require a recorded decision and fresh evidence. Preserve old data.

## Authority and status vocabulary

Current user instruction → relevant current decided row → active GDD → current successor plan. PROGRESS owns executable task order; PROGRAMME_STATE owns the current handoff. Legacy appendices and archives never override the active documents.

Task states: todo, in_progress, blocked (named dependency), done, waived (explicit human authority). Decision states: open, recommended, provisional, decided, superseded. Human tasks are completed only by the human. Supersession can be partial: record precisely which rule changed and which old decision remains relevant to legacy reproduction.

## Engine and cross-cutting obligations

Retain TypeScript, the simulation/renderer boundary, item conservation, deterministic replay, accessible feedback, performance and release obligations. No engine migration is implied. PROGRESS remains the only task list, DECISIONS the decision registry, and PROGRAMME_STATE a replacement handoff rather than an accumulating log.

## Changelog

- 2026-09-06 — D-EX-01–09: aligned scope and authority with the revised direction, preserved numbered evidence rules, clarified legacy profiles and owner-confirmed Phase 4 completion.
