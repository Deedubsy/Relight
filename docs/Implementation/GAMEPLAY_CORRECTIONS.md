# Gameplay audit corrections — implementation and evidence

9 September 2026 · `codex/tram-expansion` · working tree, not committed or published. The checkout already contained UI/tram follow-up changes; those and historical evidence were retained. This report records the owner-authorised EX-09C-01–09 and RI-10/RI-12 correction pass, not a final campaign or release verdict. PROGRESS remains the only task list.

## Playable loop

Ordinary entry now starts exploration-v2 with empty pockets, empty Home storage and no supplied generator or ammunition. Hand-mine Home salvage and coal; build and fuel production, manufacture Shot, load a turret, then scout the broad core-search circles. Clear a relay's defenders and recover its unique physical core. Its damaging equipment shuts off; independent living enemies continue behaving normally. Carry the core to any compatible regional plant, deliver preparation materials and deliberately install it.

A commissioned plant supplies a finite local circuit and remains a raid candidate when switched off. Build a factory and defence, use generators for demand above plant capacity, exchange freight through powered permanent tram stops and carry the last mile or connect conveyors. Damage disables the local base; paid repair retains its installed core. No frontage, ring economy, block claims, derived Interior gate or territory-triggered truck work is used in this campaign. Legacy gameplay remains separately selectable.

The production catalogue now includes ore-to-metal Foundries, crude Pumpjacks, fuel/polymer Refineries, real faster Mk2 assemblers and fast belts. The Arsenal encounter unlocks Shell-fed Cannons and the existing dual-barrel rifle improvement. Optional recovered speed artifacts attach to one machine and survive removal, packing, recovery and reload. Recruited Gunsmiths and Rail crews enable paid hopper/freight upgrades. The existing tram carries the engineer and real backpack; the Foreman truck can explicitly recover selected equipment to a chosen chest.

## Implementation defaults (initial tuning, not proven balance)

Core configuration is `packages/sim/src/progression.ts` (`CORRECTIONS`); recipes/costs/draw remain in the established flow/recipe definitions.

| Mechanic | Initial implementation |
|---|---|
| Opening | First-day minor opportunities at 15 and 18 minutes; first major remains Night 3 at 55 minutes. Later daily slots retain the existing schedule. The old five-minute supplied-opening raid destroyed Home before the measured empty bootstrap could finish. |
| Sources/sites | Three unique cores and three plants near non-Home tram regions; three optional speed-artifact caches. Deterministic reachable additions preserve old structures. Core search radius 28 tiles; discovery within 14. |
| Relay equipment | Active source deals 2 HP/s within 7 tiles, with a visible pulse/radius and damage beam. Recovering its core disables that equipment. |
| Plant | 30 steel + 15 copper and one carried core; 600 kW connected regional supply. Ordinary 300 kW generators consume coal/refined fuel for additional load. Existing scheduler chooses targets; output off does not remove eligibility. |
| Artifacts | One removable +10% processing-speed slot on processors/extractors; inputs, power and output capacity still constrain work. |
| Specialists | Gunsmith hopper +25% with whole-round handling; Rail crew freight +25%. Each costs 20 steel + 10 copper. Packing an upgraded turret returns its upgrade materials so the purchased value is retained. Baseline direct belts, substations and automatic service stay available. |
| Production | 2 iron/copper ore → 1 respective metal in 3 s; 1 crude → 4 fuel in 3 s; 2 crude → 1 polymer in 3 s; 2 steel + 1 coal → 1 Shell in 3 s. Pumpjacks require power; crude cannot be hand-mined. Existing salvage preserves bootstrap access. |
| Equipment | Foundry 80 kW; Pumpjack 60; Refinery 100; Mk2 150 with twice the processing speed. Fast belt movement is twice ordinary speed, with real increased throughput. Mk2 and fast belts consume polymer in construction. |
| Heavy defence | Cannon: 20 Shell capacity, 12-tile range, 50 damage per Shell every 2 s. Breaker 100 HP; Conductor 80 HP, bounded reinforcement every 8 s up to eight; Shade 40 HP with actual powered-light targeting and dark-route preference. |

## Engineering encounter state/transition specification

Each site is discovered → materially prepared → explicitly started → progressed in powered, defended stages → completed once. Starting requires the full 30 steel + 15 copper delivery. Each running encounter imposes 100 kW commissioning demand. Three wave thresholds pause productive progress while defenders remain. Unsafe spawn access refuses the wave and pauses safely. Power/engineering prerequisites pause progress; manual pause or an enemy reaching the installation retains materials and completed stages for resume. Saving preserves timers, wave identities and completion. Completed sites never restart on ordinary power loss.

| Site | Distinct engineering/combat requirement | Completion |
|---|---|---|
| Junction Heart / Arsenal | Powered feeder poles on both sides; maintain supply and clear three valid-approach waves; 30 productive seconds. | Arsenal access, Cannon/Shell capability and dual-barrel rifle. |
| Furnace Walker | Two nearby processors actively working under real load; heavy Breaker waves; 40 productive seconds. | Once-only 50 steel stored at Home. |
| Blackout Crown | Two nearby powered lights expose Shades; remove bounded Conductor reinforcement sources; 50 productive seconds. | Once-only 50 copper stored at Home. |

These later encounters are bounded implementations of their adopted roles, not invented Q07 ending requirements. Reused geometry provides distinct gameplay, not bespoke final boss art. Their prepared automated-defence checks prove mechanics; organic progression, difficulty, reward value and player enjoyment still need observation.

## Verification and limits

All paths below are under [the evidence directory](../evidence/gameplay-corrections/).

- `ordinary-opening.ts`, `ordinary-final.txt`, `ordinary-result.json` and `ordinary-save.json`: ordinary walking, gathering, paid construction, fuel loading, workbench Shot and rifle commands at 1×. Hand inputs at 306 s; powered production and loaded defence at 399 s; first relay cleared and core installed at the chosen plant at 465 s, 94.8 HP. No inventory grants, teleports or fast-forward. Conservation and save validation pass. This is an efficient scripted route, not a fresh-player timing verdict.
- `gameplayCorrections.test.ts`: unique core/artifact capacity and repeated-action handling; alternative plant choices; finite power and generator supplementation; actual scheduled arrival/damage at an output-off plant; repair preserving its core; each new processor, Pumpjack and faster belts; real speed modifiers; paid recruits; passenger travel/outage/reload; truck contents/artifact recovery; Cannon/Shade light counterplay; all three engineering encounters; old campaign and explicit legacy load paths.
- The integrated factory check continues the real first-core checkpoint, with **labelled later construction supplies**. Actual freight carries 20 steel + 10 copper between powered stops; local carried transfer supplies chest → belt → assembler → belt → chest → belt → turret, without inserters. No items are conjured to claim an organic factory build. The attack and supplementation checks use this same ordinary plant checkpoint with separately labelled clock/load preparation.
- Encounter fixtures use supplied construction, ammunition and processor inputs; the Furnace fixture explicitly drains finished output to Home and the Crown fixture adds forward defence against its Conductor. They verify the encounter conditions and automated damage, not an unaided natural playthrough.
- `focused-complete.txt`: **47/47 pass**, including 15 correction tests and retained guidance, inspection, inventory/input/settings tests. `build-visual-final.txt`: final types/build; `lint-complete.txt` and `docsync-complete.txt`: pass. `walkthrough.json`: actual browser checks and no page errors. The last carried-core/passenger guidance assertions pass 23/23 with their focused neighbours in `guidance-final.txt`. Diagnostic failures remain in earlier logs.
- `retained-closure.txt`: **84/101 pass, 17 fail** after explicitly preparing historical stock in the existing fixtures. The remaining cases include fixed recruit-list positions, old first-day raid times, hard-coded obstruction/concrete layouts, paid-log replay from the changed factory, catalogue binding assumptions and the old truck journey. They are unresolved regression findings, not waived gameplay acceptance. `suppliedCampaignFixture.ts` exists only for these prepared subsystem tests; it is never normal entry or empty-start evidence. The final two focused fixture corrections separately pass 16/16 in `focused-retest.txt`.
- One broad `npm test` run (`tests-01.txt`) took about 29 minutes: 379 tests, 235 passed and 144 failed at that checkpoint. It includes old supplied-opening assumptions, archived replay/hash comparisons and other retained failures. The baseline was already dirty and not measured, so these are **not all claimed pre-existing**. Targeted adaptations and reruns are separate evidence; this is not a current full-suite pass.
- `freshness-reviewed.txt`: twelve archived campaign experiment records retain their old configuration fingerprint and are correctly stale. They are preserved, not re-stamped or regenerated into misleading current balance evidence. Legacy evidence remains under its own profile.

Campaign evidence fingerprints include the new central correction defaults, production recipes, machine costs and draw; legacy hashes are preserved.

New progression saves validate unique reward locations, installed plant components, paid state, legal recipes, artifact eligibility, seats and recovery jobs. Older campaign saves receive deterministic safe additions while preserving earned inventory; their old command logs remain evidence but lose the promise of replay from a changed new-game start. Explicit legacy loads remain legacy. The bounded seed-3 walkthrough and alternative destinations do not replace the older geometry survey or establish new seed fairness.

## Actual-game visual review

The mutable port 5178 serves rebuilt `packages/game/dist`; frozen ports were not used as current evidence. `walkthrough.cjs` records loaded assets and browser errors in `walkthrough.json`. It uses ordinary entry and separately labelled saved checkpoints, not a mock interface.

Inspect `opening.png`, `core-search.png`, `core-recovered.png`, `plant-factory.png`, `passenger.png`, `machine-artifact.png` and `machine-artifact-1280.png`. The review checks the empty start, concealed relay coordinates, real carried core/source-offline state, commissioned plant, actual passenger interaction and +10% artifact feedback at ordinary and small screen sizes. It corrected a startup catalogue crash for new kinds, missing plant requirements, stale station/workshop guidance while carrying a core or riding, missing world-view search boundaries and unclear relay activity. Basic shapes reuse the existing art system.

## Run and try it

From the repository: `npm run dev`. Open the local URL Vite prints with `?view=world`; no rules parameter or save is needed for a new campaign. The current rebuilt preview is [fresh campaign on 5178](http://127.0.0.1:5178/?view=world). Explicit legacy testing uses `?rules=legacy-v1`. Loading a valid save follows that save's mode.

1. Start fresh; hold on accessible Home salvage to gather steel/copper/coal. Build an Excavator and Generator, fuel it, then make Shot and load a turret. Check guidance, mining feedback and the available preparation time.
2. In Projects, track a core search area and show it on the map. Scout, clear defenders, recover the core and choose a regional plant. Check that source equipment stops and the core exists once in your Backpack.
3. Commission the plant, connect local supply/production and defence, and power two permanent stops. Test freight delivery, direct conveyor loading, passenger boarding/exit, outage, plant damage and paid repair.
4. Try the optional artifact and survivor upgrades; Foundry/Refinery chains, Mk2/fast belts and explicit Foreman recovery. Save and reload during travel or a job.
5. Prepare the Heart, then Furnace and Crown deliberately. Check that requirements are readable, supplies matter, a failed attempt can resume and rewards happen once.

Keep observations under existing EX-08H/P6-H/P7-H/P8-H/P9-H/T18/P6-AMMO gates, identifying this changed build. Earlier successful Phase 5 play remains credited; it does not validate this new opening. Q07 final campaign requirements, full project/economy longevity, new-seed fairness, human fun/readability, reference-machine performance and release gates remain open. No publish, push or launch-ready claim is part of this pass.
