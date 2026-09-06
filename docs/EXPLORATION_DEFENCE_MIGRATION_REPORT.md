# Relight — Version 2 documentation migration

Date: 2026-09-06. Task: EX-00. **Editorial scope complete; automated validation partial.** No Version 2 gameplay was implemented or playtested by this pass.

## Authorisation and milestone

The owner requested the documentation changes after the design discussion and impact review. They then clarified: "we have just finished phase four". D-EX-01–10 record the direction, documentation authority and Phase 4 milestone without inventing additional approvals. Phase 5 is incomplete; forward-built features remain individually credited.

## Changes

- [Game design](RELIGHT-design.md): rewrote active sections around free roaming, the cul-de-sac, second-area tram access, radio restoration, useful sites, specialised resources, multi-stop freight and three threat layers. Open contracts are explicit. Original generated tables and guarded prose remain in a labelled legacy reference because the current docsync tool expects them in this file.
- [Decisions](DECISIONS.md) and [constitution](CONSTITUTION.md): recorded current authority, precise supersession boundaries and unresolved contracts. Old no-horde/no-day-cycle rules and rescue-only combat constraints cannot silently veto the new direction. No exact reward catalogue, renewable resource system or unrequested survival/loot system was inferred.
- [Successor plan](EXPLORATION_DEFENCE_PLAN.md), [phases](PHASES.md) and [progress](PROGRESS.md): replaced the executable direction, mapped reusable and remaining work, revised upcoming acceptance, and prevented unfinished legacy rows from running unchanged. Completed/waived old rows retain their original meaning and evidence. EX-01 is next and ready.
- [Programme state](PROGRAMME_STATE.md): recorded local checkout versus fetched main, the owner's milestone, reusable systems, unbuilt revised scope and verification limits. Main's city/Heart work remains visible without claiming it runs locally.
- Root README, CLAUDE and AGENTS: aligned coding-agent entry points with the current design, single tracker and successor plan. CodeGraph guidance retained.
- [Standards](STANDARDS.md), [deferred obligations](DEFERRED.md) and [new play protocol](EXPLORATION_DEFENCE_PLAYTEST.md): current warnings, station controls, exploration and defence acceptance take precedence; historical comparisons and unpaid obligations remain traceable.
- [Archive index](archive/pre-exploration-defence-2026-09-06/INDEX.md): preserved original working documents and selected fetched-main references. The adopted Version 1 plan body remains verbatim under a supersession preface.

## Preserved boundaries

No game code, dependency manifests, constants, generated results, snapshots or historical experiment/gate reports were changed. No commit, branch switch, merge or push was performed. Pre-existing untracked `.serena/` and `docs.zip` were left intact. The prior impact review remains a dated advisory record; this migration report is the implementation record of the documentation revision.

## Validation

The normal `npm run docsync:check` and `npm run freshness:check` commands could not start because the local command shim is unavailable. Direct invocation of the installed tsx runner first failed on sandbox account lookup; approved outside-sandbox retries revealed a Linux-only esbuild installation incompatible with Windows. Therefore neither tool produced a document-consistency or freshness verdict. Dependencies were not reinstalled or changed for this prose-only task.

Static verification passed: links across 15 active documents resolve; all seven generated blocks are identical to the pre-revision document; all non-heading legacy design prose is retained; all 13 archived originals match their source content (allowing line endings); the adopted Version 1 body is unchanged; 50 task rows have the expected table shape, with completed/waived rows preserved and all unfinished legacy rows gated on reconciliation; all 17 new decision IDs are unique; checked task dependencies resolve; no non-Markdown tracked file changed. The scoped git whitespace check also passed. These checks do not substitute for an executed docsync/freshness pass.

No gameplay tests or simulations were run. No new human play evidence is claimed.

## Next task

**EX-01 is ready:** inspect the local/fetched-main baseline and produce a concrete integration/reuse report plus recommended opening contracts. Implementation depends on the relevant owner answers recorded by EX-02. Core questions cover bases/routes/rot, day and assault scheduling, radio information, station freight, resource longevity/discoveries and equipment/repair. Endgame details can wait for EX-09.

## Subsequent adoption — D-EX-11

After the recommendation draft, the owner said: "Yes, I like all of those recommendations. Let's go with those". The recommended package and stated initial settings were adopted; alternatives and unspecified tuning were not. Q01–06 are decided, their rules are incorporated into the active GDD, and EX-02 records the completed human decision. EX-01 remains in progress for its full technical baseline audit. Phase 4 remains the last completed phase.

The original draft is archived. This adoption changed documentation only: no gameplay, branch integration, constants or historical simulation evidence. Static consistency checks were repeated for the adoption; the previously recorded dependency/platform blocker still limits full documentation-tool validation.
