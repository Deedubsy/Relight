# Exploration, tram expansion and base defence: document impact review

Review date: 2026-09-06. This is an advisory review of the current design against the owner's voice discussion. It does not replace the governing design, change task statuses, or claim implementation or playtest evidence. The direction below captures the discussion; proposed details remain recommendations.

Subsequent work: the owner authorised the documentation migration and confirmed Phase 4 complete. See [the migration report](EXPLORATION_DEFENCE_MIGRATION_REPORT.md) for the applied changes. This review is retained as the earlier assessment.

## Baseline and scope

The checkout is `ri-pass-1` at `678478a`. The fetched `origin/main` is `b32e4c3`, with newer city and Heart validation work. The earlier pull fetched main but did not put those changes in this checkout. This review inspected both the local governing documents and the main-branch document changes. Any subsequent rewrite should use the intended implementation baseline and preserve that newer work.

The local handoff calls RI-06 unverified. Main's handoff instead reports city geometry integrated, Heart validation repaired, six Heart tests and fifteen experiments passing, and a 156-test suite passing sequentially. These are existing reports, not checks rerun in this review. Main still lists human city-recognition and reference-machine performance work as outstanding.

The owner's estimate of approximately 40% implemented is a planning estimate. Completed phases and task counts do not establish a percentage of the revised game. Re-estimation needs a revised feature inventory.

## Direction established in the conversation

- Free roaming beyond powered territory, with organic discoveries, rare useful finds, and distinctive enemies at exploration sites.
- A house in an easily defended cul-de-sac, with one early attack approach.
- The second area opens tram access immediately and offers the opportunity to restore a radio tower. Exact activation and construction requirements remain to be specified.
- A main tram route with stations supporting local factories and branches into the city. District specialisation gives older production sites a continuing purpose.
- Scheduled major assaults and major restoration assaults, with only one base targeted by a major assault at a time.
- Initially two days without major assaults, potentially falling to one later in progression. Minor raids and site creatures still exist during those days.
- Small raids, roughly a dozen enemies as a tuning proposal, a few times per day. A shared frequency across bases was proposed to avoid interruptions multiplying with expansion and accepted as part of the discussed direction; exact rate and scaling remain unset.
- Radio infrastructure makes attack warnings a restoration reward. The discussed warning progression starts with coarse direction or district information and can improve toward base and timing information; precision and lead time are unset.
- Restored places provide practical capabilities, including transport and workshop support, rather than territory alone.

The discussion does not specify enemy loot farming, equipment rarity tiers, renewable resources, survival needs, or a rail-signalling simulation. None should be inferred from the desire for rare discoveries or a larger tram network.

## Conflicts that must be resolved explicitly

| Existing rule | Effect of the new direction | Required treatment |
|---|---|---|
| Design §7 and §23 choose creeping frontage pressure and reject timed hordes. | Assault scheduling and intermittent raids become core threat behaviours. | Replace the active threat specification and identify what happens to existing bloom, density and well rules. Merely adding hordes would retain the constant pressure the owner wants to reduce. |
| Design §22 and §23 exclude day/night. | Days and nights become a player-facing planning unit. | Specify clock length, first attack, rest intervals and presentation. Decide separately whether ambient daylight changes mechanical lamp coverage or Shade targeting. |
| Design §5 and §9 reward compact enclosure and charge for every exposed edge. | A branching tram network and specialised outposts favour long connections. | Define bases, territory, route safety and attack targeting. Reusing the old frontage charge unchanged could penalise the new central strategy. |
| Field construction and activation depend on adjacency to Held blocks. | Free discovery and potentially distant stations need a defined establishment path. | Distinguish exploring, claiming, connecting and operating a station. Free walking alone does not settle remote construction rules. |
| Design §11 and the hour benchmark teach a river HQ and staged enclosure. | A cul-de-sac leads quickly to a second-area tram reward. | Write a new opening and acceptance criteria, keeping old measurements explicitly historical. |
| Design §12 and §17 distribute finite rubble by district and progression. | Concentrated resources must keep older factories useful. | Rework distribution and seed guarantees; decide resource longevity. Renewable salvage was an assistant suggestion, not a settled rule. |
| Design §14 and Phase 5 define one tram per track and two stops. | Stations need selective loading, unloading and onward delivery. | Specify multi-stop routes, reservations, return freight and route interruption. Track branches and multiple vehicles require explicit scope decisions. |
| Constitution rule 2 and the adopted plan exclude random equipment drops. | Rare finds and unique encounters are wanted. | Define discoveries separately from enemy drops. Authored or seeded site rewards can satisfy the direction without assuming a loot-tier system. |
| The rifle is chiefly a rescue tool; several combat and damage systems are restricted or incomplete. | Personal participation, walls, workshops and distinctive encounters matter more. | Revisit equipment, damage, repair and defeat rules. Do not assume the old combat balance supports the new experience. |

## Document-by-document changes

| Document | Change needed |
|---|---|
| `docs/DECISIONS.md` | Record the owner's new direction with provenance; explicitly supersede conflicting decisions by topic. Separate settled direction, proposed implementation defaults, tuning values and unanswered rules. Check D-RI-3, D-RI-5, D-RI-6 and affected opening, resource and transport rows individually. |
| `docs/CONSTITUTION.md` | Update the stated loop and relevant scope boundaries to match exploration, restoration, transport and intermittent defence. Preserve simulation purity, evidence provenance, human design authority and the distinction between code, validation and play evidence. Clarify new-versus-legacy benchmark policy. |
| `docs/RELIGHT-design.md` | Rewrite the active specification coherently. Main affected sections: §2–3 identity; §4 presentation; §5–9 territory, rewards and threats; §10–11 loop/opening; §12–15 resources, machines, logistics and progression; §17 generation; §18–19 examples and play criteria; §21–28 positioning, exclusions, risks, questions and adopted direction. Review §16 endgame dependencies too. Preserve section anchors where practical and label existing implementation separately. |
| `docs/REVISED_DEVELOPMENT_PLAN.md` | Its adopted body is preserved verbatim by the current document rules. Preserve that version as history; introduce a clearly versioned successor and update the authority links/preface. The successor needs integration and replacement boundaries, not another cumulative wishlist. |
| `docs/PROGRESS.md` | Remain the only executable task list. Map unfinished work to retained, revised, blocked or replaced work using the existing status vocabulary with explicit explanations. Keep completed implementation and evidence records intact. Replace the current opening integration task's obsolete acceptance before an AI continues it. |
| `docs/PROGRAMME_STATE.md` | Identify the actual baseline and new direction, distinguish reusable work from incompatible behaviour, state implementation/validation/play status, and name the next runnable task and unresolved dependencies. Carry forward main's city and Heart updates. |
| `docs/PHASES.md` | Revise upcoming scope and exit criteria, particularly factory/transport, threat, found technology, city generation, progression and onboarding. Preserve completed-phase history; completed old milestones are not proof of new requirements. Re-estimate remaining work rather than retaining old durations as promises. |
| `CLAUDE.md`, `README.md`, main's `AGENTS.md` | Point all coding-agent entry points at the same current design and successor plan. Update stale task, branch and validation summaries. Keep one rule authority and one task tracker. |
| `docs/STANDARDS.md` | Update Relight-specific expectations for warning timers, raid alerts, station controls, exploration wayfinding, returning to defend, and current implementation status. B.1 currently explicitly forbids a wave timer. Preserve external comparison evidence as historical source material. |
| `docs/DEFERRED.md` | Reconcile old front, transport and economy obligations with the new plan. Link scheduled obligations to the canonical tracker; do not quietly lose relevant debts or maintain a second backlog. |
| `docs/PROTOTYPE_TEST_PLAN.md`, gate records, phase reports, other reports and `docs/archive/` | Preserve completed protocols and evidence as history. Introduce a new representative-loop play protocol instead of rewriting old sessions or verdicts. Existing reports, including main's city rebuild report, establish only what their original scope measured. |
| `docs/EXPERIMENTS.md`, generated experiment data, calibration, snapshots and seed/territory images | Retain provenance and label old frontage benchmarks by scope. Generate new evidence only after implementation and actual runs. Do not rename old results into proof of the new design or adjust expected results just to pass. |

The reference research in `docs/standards/` generally remains usable. Only current Relight conclusions and acceptance mappings need reconciliation; source research does not need rewriting to match a changed design.

## Reuse and implementation implications

Likely reusable foundations include the simulation/renderer boundary, commands, item accounting, save/replay infrastructure, inventory, recipes, production, belts, power, movement, light queries, project records, physical commissioning, and enemy encounter primitives. Main adds useful shared building geometry, landmarks, factory yards and a rail reservation. Reuse is a design assessment, not certification that these systems need no changes.

Substantial extensions or replacements concern threat scheduling, base identity and targeting, warning infrastructure, cul-de-sac generation, resource placement, multi-stop transport, discovery persistence and opening guidance. Existing Heart and Stalker work can inform encounters, but their current locations, unlock gates and triggers should not dictate the revised opening.

Save compatibility needs an explicit policy for world profiles, clock/scheduler state, discovered rewards, bases and routes. An old save must not silently acquire an incoherent mixture of both threat models.

## Recommended implementation sequence

This is a proposed sequence for incorporation into the canonical tracker, not an additional task list with independent statuses.

1. Align design authority, vocabulary and replacement boundaries on the intended baseline.
2. Build a representative opening: cul-de-sac, second area, tram access and radio restoration opportunity, reusing the city geometry.
3. Implement the shared major-assault schedule, target selection, rest intervals and warning state; then add bounded minor raids and persistent site encounters.
4. Expand transport to multiple stations with selective cargo handling and demonstrate two specialised factories exchanging real goods.
5. Add one useful optional discovery and one distinctive encounter, alongside a restoration benefit the player actually uses.
6. Play the full cycle before expanding content: explore, return, build, defend, restore, distribute, explore again.

Automated acceptance should cover: no overlapping major targets; rest intervals respected when restoration events occur; warnings surviving save/load; bounded raid frequency as bases increase; valid enemy approaches; items conserved across station transfers; later stops receiving reserved freight; non-duplicating discovery rewards; reachable early resources and no tram unlock circularity.

Human acceptance should examine: a useful payoff arrives early; players choose to explore; reliable defences permit excursions; warnings leave time to respond; combat and building both contribute; stations are understandable; older factories remain useful; repair and resupply do not recreate the original repetition.

## Decisions still needed before dependent implementation

- What is a base: a station settlement, a registered core building, a neighbourhood, or another defined boundary? How are vulnerable routes treated?
- Does new threat logic replace regular frontier blooms completely? What remains of rot, wells, enclosure, block loss and retaking?
- How long is an in-game day, when is the first major assault, and what event shortens the quiet interval?
- How are scheduled and restoration assaults combined, deferred or queued while preserving rest days? Can multiple restorations accumulate?
- What does a radio tower reveal, how early, over what coverage, and what happens if it loses power?
- What station actions and vehicle routing are in the first version? Is tram access a recipe unlock, an operational vehicle, or a restored station plus buildable kit?
- How do concentrated resources remain useful over the intended campaign length?
- What kinds of rare discoveries and personal equipment are in scope, and what can be damaged, repaired or lost?

These questions can be resolved per milestone; they do not prevent the coherent document migration. Until specified, exact values and assistant examples remain proposals.

## Review outcome

This is a substantial change to the game's design and acceptance criteria, with considerable existing implementation available for reuse. A coordinated document revision should precede further work on the old opening and threat roadmap. This review changes no governing rules, code, historical evidence or task status.

Review verification: document synchronization and evidence-freshness checks were attempted but could not start because the local `tsx` executable is unavailable. No simulation or gameplay tests ran for this advisory review.
