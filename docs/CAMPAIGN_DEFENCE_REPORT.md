# Campaign defence increment — 7 September 2026

Authorisation: D-EX-16, the owner's request to continue the revised campaign. D-EX-15 explicitly confirms that Phase 5 has not started. This is the intervening opening/defence revision after completed Phase 4, not a phase gate.

## Delivered

The exploration campaign now uses a separate threat controller while reusing the existing turret ammunition, aimed rifle, movement, inventory and knockdown systems. Legacy campaigns keep their original machine catalogue, enemy behaviour, saves and evidence.

- Player-built one-tile walls physically block walking and enemy paths. Attackers prefer open routes and breach walls/turrets when necessary. Disabled defences keep their place and inventory, stop working and permit a breach. Damaged defences cannot be packed to erase their damage.
- Home and restored station cores take damage. Defeat disables local service/production, cancels the remaining roster and sends surviving attackers back to their entry point. The base, factory layout, inventory, track and freight remain. Disabled cores cannot be selected again until repaired. The legacy automatic substation recovery rule is excluded from this profile.
- Manual repairs spend carried materials once, take time and pause outside reach or during knockdown. A disabled core waits for attackers to leave before recovery. An ordinary small repair cannot cheaply recommission a core knocked out while the repair was underway.
- Minor raids share two opportunities per day across the network. Each targets one base with a finite group. An occupied opportunity is discarded, not queued; major combat and its immediate recovery interval suppress minor raids.
- Major assaults lock one target at dawn and release a finite roster from dusk. First major: Night 3. Later windows preserve two complete cycles after the actual end, including withdrawal. Restoration nominations coalesce; post-lock restorations wait for a later window. Before radio restoration only Home Court is eligible for majors; afterwards eligible station cores remain eligible even if the radio loses power.
- A powered radio identifies the locked base. An outage retains received intelligence and never changes the target. Reconnection supplies missing intelligence. The paid precision upgrade adds approach direction and broad composition. All displayed warning text comes from simulation state. Before radio restoration the home entrance supplies the coarse approach cue.
- Three deterministic optional ruin guards on the reference seed stay near their sites. They do not march on bases. This is the ordinary site-threat layer; the distinctive Stalker and rare schematic remain EX-07.
- Build menu, defence health, core repair buttons, normal E interaction, warning/support text and radio upgrade controls expose the mechanics through ordinary commands.

## Provisional tuning and explicit boundaries

These are reversible implementation candidates, not owner-approved balance or play observations.

| Setting | Implementation |
|---|---|
| Wall | 2 steel, one tile, 120 HP |
| Gun turret | Existing price/ammunition/rifle damage; campaign durability 100 HP |
| Core | 300 HP |
| Minor raids | Opportunities at elapsed daylight seconds 300 and 600; 8–12 crawlers |
| Major roster | 60 ordinary crawlers, one every four seconds; finite rather than timed despawning |
| Crawler | Retained 12 HP; campaign movement 2 tiles/s; 8 structure HP/s and 5 player HP/s |
| Manual repair | 2 steel + 1 copper, 40 HP after four seconds in reach |
| Disabled core kit | 10 steel + 5 copper, twelve seconds; restores the core after withdrawal |
| Minor recovery exclusion | 300 seconds after actual major end |
| Radio precision upgrade | 15 steel + 10 copper, restored/powered/in-reach tower required |
| Ruin guard | Notice radius 8 tiles; engineer must remain within 12 tiles of the site |

The existing house/court shell remains static city geometry; the new paid walls are the destructible defence type. Protected factory equipment is traversable to creatures so it cannot replace attackable walls; it remains solid to the engineer. Track, freight and production machines are not assault damage targets. Turrets retain the existing targeting model, including shooting over walls.

The current opening supports home plus one commissioned station. The adopted reduction to one quiet cycle after a third station with actual automated resupply belongs to the EX-06 expansion dependency; no timer-only substitute or fake milestone has been introduced. Full workshop automation is also EX-06. Broad seed fairness, encounter variety and balance remain later work. Major attackers wait visibly if a locked spawn approach becomes obstructed; no invalid spawn or automatic disappearance is used to conceal that condition.

## Save and replay

The exploration save envelope remains schema 3; campaign metadata advances to version 3 with versioned defence state, base health, paid repair progress, attack identity/roster, nominations, radio messages, guard identity and bounded recent history. Validation refuses missing/corrupt schedules, invalid health and orphaned creatures instead of resetting an attack.

Earlier campaign previews upgrade deterministically. They receive two complete preparation cycles, no historical major catch-up and no deferred minor backlog. Their old command logs are evidence only, not claimed complete replays under the new starting factory. New sessions and paid defence commands replay from the current factory. Legacy saves never acquire the new rules.

## Browser observation

Local loopback preview, fresh exploration campaign, seed 3; automated browser interaction, not a human playtest:

1. The HUD named Night 3, lighter raids, and locked restoration/radio controls correctly.
2. Took 50 steel from the house through the pocket panel. Selected Wall in the build menu and placed it on clear ground. Pockets changed from 50 to 48 steel; chest stayed at 150.
3. Ran the otherwise unprotected home at accelerated speed. Its first minor raid disabled the core while the layout, supplies and placed wall remained. The panel reported 0/300 HP and the recovery kit.
4. Took 50 copper, selected the paid core repair and resumed. Core recovered to 300/300 HP. Pockets changed from 48 steel / 50 copper to 38 steel / 45 copper, exactly one kit. The next major remained Night 3.
5. Existing browser saves were not overwritten. The temporary tab was closed and the loopback server stopped after verification.

## Automated validation

Final simulation suite: **180/180 passed**, including all 19 campaign/opening/expansion/defence tests and the unchanged legacy regressions. TypeScript checks and the production build passed. Both documentation profiles, evidence freshness and the retained legacy three-hour snapshot passed. Lint passed. Local consistency verified 40 links, 15 EX task rows, 37 unchanged historical task rows, seven unchanged generated legacy blocks and unchanged historical artifacts. The existing Vite large-bundle advisory remains.

The initial focused run found one shot-record assertion mistake, corrected before the successful full-suite run. The final focused run passed 19/19 and also covers truthful exit targeting during withdrawal; the production build was rerun after that inspection-only fix. No failed observation is relabelled as success.

Actual command results are recorded in [the dedicated evidence directory](evidence/campaign-defence-2026-09-07/). Historical opening/station reports and generated legacy blocks are preserved.

The first focused run exposed legacy automatic substation recovery re-enabling a disabled core; the implementation was corrected. The first full run exposed two legacy catalogue count assumptions after adding Wall; the legacy catalogue is now preserved and the campaign catalogue extends it explicitly. A new successful-defence test initially compared the rifle's per-target shot record to a scalar; that assertion was corrected to check the record.

Schedule tests that advance the clock or construct station/power fixtures are labelled scenarios, not opening economy evidence. The complete-roster defence test buys and supplies its turrets through ordinary commands, but advances the clock to isolate combat. It does not measure expedition time or prove a novice can prepare in time.

Human approval: the design and continuation are approved through the cited decisions. Human play evidence: not run. No fun, balanced opening, Phase 5 start/completion, Phase 6 completion or release-readiness claim is made.
