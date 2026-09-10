# Player clarity and practical fun audit

10 September 2026 · Current CITY-F build · Expert assessment, not new-player research

## 1. Verdict

**A persistent first-time player can discover how to build the first machine, but the game does not yet reliably explain enough to make independent exploration and sustained production rewarding.** The strongest part is tangible machinery: mine an item, spend it, place something, inspect why it stopped. The largest risks are losing control after using the UI, encountering combat rules that the visible scene does not explain, and investing in production or exploration whose eventual payoff is weak or absent.

The opening is not devoid of guidance. Mining has progress and collection feedback; the objective changes after building an Excavator; inspection identifies missing power; Projects separates carried and delivered materials. These deserve refinement rather than replacement with a long tutorial. However, knowing the next purchase is different from understanding the useful working loop it will complete.

**Scope and limits.** Inspected `http://127.0.0.1:5178/?view=world` at 1366 × 900 in a fresh, isolated Playwright browser context. Runtime reported `riverfront-arc-v4-editor-ac16d9188c05`, exploration-v2, default seed 3. The opening used ordinary mouse/keyboard input, no stock grants or teleports: empty Home storage, help, mining, one paid Excavator, inspection, Projects and map. Its saved checkpoint was **455 simulation seconds**; wall time included reading and tool delays, so this is not a measured time-to-learn benchmark. Detailed system rules were read after the opening interactions. Prior project knowledge cannot be erased; this is an expert walkthrough, not a genuinely blind participant.

Later checks used a separate disposable page with explicitly changed positions, inventory and clock: quarry defenders/proximity damage, ore inventory and Riverside's untouched project, and Junction Heart's project with the clock moved to the first major-warning boundary. These establish local presentation, not successful natural progression or campaign difficulty. No full assault, completed engineering encounter, freight journey or long production run was played. Source inspection supplied the remaining dependencies. The current confirmed design was used as intention, not evidence; its older building count was superseded by the live 479-building map. No separately named reconstructed GDD was found.

Labels below mean **Observed** in this run, **Source-confirmed** in active implementation, or **Hypothesis** requiring further play. Screenshots: [opening](player-audit-2026-09-10/opening.png), [full map](player-audit-2026-09-10/map-stable.png), [quarry damage](player-audit-2026-09-10/quarry-threat.png). The owner's browser storage and saves were untouched. No gameplay code, balance or design document was changed.

## 2. First-session confusion timeline

These are ordered checkpoints, not invented per-action timings.

| Stage | What the game exposed | Assessment |
| --- | --- | --- |
| Before acting | **Observed:** “Start your workshop” / “Mine the remaining materials by hand”; nearby “E · Open supplies”; empty backpack and empty Home storage. | “Remaining” and “supplies” suggest an earlier step or starting stock. Neither is a fatal blocker, but the primary instruction omits the required item and amount. |
| Looking for controls | **Observed:** Help contains movement, interaction and inventory bindings. After clicking “Why this next?”, B did not open Build; clicking the actual Build button worked. | A reasonable attempt to learn the rules can leave keyboard control apparently broken. Help's instruction to click the city explains the workaround, but should not be necessary after a simple HUD click. |
| Trying the visible steel patch | **Observed:** “Move closer to mine”, “Hold left-click · 1 item / second”, then progress and “+12 Steel plates · Backpack 12”. | Good local feedback. The nearby scene and reach prompt eventually make gathering understandable. |
| Spending the first materials | **Observed:** Excavator cost 10 steel; placement changed the goal to “Gather 30 steel + 10 copper and place a Generator”. Inspection said “Offline · no power”. | The paid action is legible, but its first result is another prerequisite. Fuel guidance exists under “Why this next?”; this is incomplete staging of the payoff, not an impossible opening. |
| Looking further ahead | **Observed:** Projects initially selects “First tram station”, alongside three plants and three identically named “Power core search area” entries. | Multiple possible goals are welcome, but the relationship between station service, stolen cores, factory power and personal preparation is harder to infer than their individual material bills. |
| Checking the city | **Observed:** Named plants, tram route and a prominent YOU marker are visible on the full map. | Useful orientation. The large number of similar blocks and unlabelled purple search circles still require interpretation; opening HUD panels cover part of the map. |

## 3. Prioritised findings

### Urgency 1 — blocks understanding or creates seriously misleading/unfair outcomes

#### 1. Using the UI can silently leave world controls disabled

**Problem and evidence. Observed:** clicking the HUD explanation then pressing B did nothing; clicking the Build button worked. **Source-confirmed:** `uiShell.sync()` blocks world input whenever focus is anywhere in the HUD/navigation, even without an open panel. Closing a panel or resuming restores the prior element, which can itself be a navigation button. Ordinary drawers release movement but do not pause the simulation. See [uiShell.ts, lines 38–87 and 130–142](../../packages/game/src/uiShell.ts), [main.ts, lines 270–304](../../packages/game/src/main.ts).

**Player consequence:** a player may read, close a panel and be unable to move or use a shortcut while enemies continue acting. That dangerous sequence is a prediction; no death from it was observed.

**Cause:** keyboard accessibility focus and gameplay input capture share one broad condition.

**Recommended change:** return gameplay input after closing a drawer and after activating passive HUD controls. Preserve native input capture for actual text fields and open modal interactions. Make the distinction between a live inventory and Pause explicit; do not silently freeze or resume time inconsistently.

**Dependencies/trade-offs:** preserve keyboard navigation and the working Tab-to-close inventory behaviour. Changing blanket focus handling must not make typing walk the engineer.

**Acceptance:** open and close Build by mouse, then immediately walk and open Backpack by keyboard; repeat after “Why this next?” and Resume. No extra world click is needed, and form editing remains safe.

#### 2. Core recovery has a hidden lighting prerequisite and an unexplained damage source

**Problem and evidence. Observed, quarry debug setup:** health fell from 100 to approximately 97 during a short approach; the feedback was “Engineer under attack”. One Crawler was visible, while the state contained a Shade too. **Source-confirmed:** an enabled relay deals 2 HP/s within seven tiles with line of sight. Quarry/wharf core guards include a Shade, and recovery requires all assigned guards dead. Shades are neither drawn nor hit on tiles failing `litAt`; the mouse flashlight only changes rendering. See [progression.ts, lines 74 and 109–111](../../packages/sim/src/progression.ts), [threat.ts, lines 428–447](../../packages/sim/src/threat.ts), [worldScene.ts, lines 1231–1247 and 1410–1412](../../packages/game/src/worldScene.ts), [flow.ts, line 1807](../../packages/sim/src/flow.ts).

**Player consequence:** being able to see a room does not mean its defender is vulnerable. A sensible rifle-and-ammunition expedition can reach a mandatory core and appear unable to finish. This is solvable with powered lighting, not a proven circular unlock, but the expedition's preparation advice does not disclose it.

**Cause:** visible brightness, combat illumination and relay damage are separate systems without an equally clear player-facing distinction. The generic damage warning identifies neither attacker nor hazard.

**Recommended change:** show a readable relay hazard cue before entering its damaging area, identify relay damage when it occurs, and teach the Shade rule at the first relevant encounter. Either retain a visibly distinct powered-light reveal rule with explicit preparation advice, or deliberately reconcile flashlight and combat lighting. Copy alone cannot make an invisible immunity state readable.

**Dependencies/trade-offs:** making flashlight light mechanically effective changes portable combat power and the value of installed lighting. If retaining the distinction, a powered-light preparation lead must exist before the long trip; do not merely instruct the player to bring an undefined “light”.

**Acceptance:** with ordinary scout equipment, a new player can identify why the relay hurts them, why a Shade is unhittable, and what available equipment would change that, without reading Help or source code during combat.

#### 3. Project states imply prerequisites are satisfied when they are not

**Problem and evidence. Observed, untouched-project setups:** Riverside said “Prepared materials · awaiting core” above `steel: 0/30 delivered` and `copper: 0/15 delivered`. Junction Heart said “Prepared · engineering 0s” with the same unmet materials. **Source-confirmed:** these status branches do not test material readiness; later encounter progress is displayed in elapsed seconds without its total or a specific stalled condition. [progression.ts, lines 113–135](../../packages/sim/src/progression.ts), [campaignGuidePanel.ts, lines 33–56](../../packages/game/src/campaignGuidePanel.ts).

**Player consequence:** players cannot trust the headline state or distinguish “ready”, “running” and “stalled”. Repeatedly pressing Start or installing a core becomes trial and error.

**Cause:** generic lifecycle labels hide the actual conjunction of materials, power, equipment and defenders.

**Recommended change:** derive the headline from unmet requirements, then show ready/running/paused/completed accurately. During encounters, identify the current blocker and progress out of total. Show the reward before committing materials. For the Heart, show which feeder side is missing; for the Furnace, identify whether two processors are actually working.

**Dependencies/trade-offs:** use the existing sim checks; avoid presenting a merely nearby pole as a powered feeder. Encounter UI must reflect its actual conditions, including the left/right geometry in the tick.

**Acceptance:** an untouched plant never says prepared; a stopped encounter explains the next corrective action without restarting it or losing progress.

### Urgency 2 — makes the core experience confusing, repetitive or unrewarding

#### 4. New industrial resources turn the backpack into a transfer chore

**Problem and evidence. Observed, inventory setup:** twelve iron ore filled twelve slots; fifty steel plates filled one. **Source-confirmed:** `stackSize()` falls back to one, and the stack table includes `iron` but not `ironore`, `copperore`, `crude`, `fuel`, `polymer` or `shell`. Storage renders those same single-item stacks; Shift-click transfers the selected stack. [engineer.ts, lines 34–35](../../packages/sim/src/engineer.ts), [inventoryPanel.ts, lines 35–45](../../packages/game/src/inventoryPanel.ts).

**Player consequence:** ore processing and oil products can require many trips/clicks for quantities that earlier resources taught players would be compact. This increases attention spent on inventory without a demonstrated interesting decision.

**Cause:** industrial items inherit the default used for individually carried objects.

**Recommended change:** define deliberate bulk stack sizes for ordinary industrial resources and ammunition; keep unique cores/artifacts individually identifiable. Choose sizes in relation to recipes and transport, rather than inventing a new capacity system. Retain visible partial-transfer feedback.

**Dependencies/trade-offs:** existing saved pack layouts and truck capacity need conservation-safe handling. Larger stacks affect the value of freight; that is a tuning consideration, not a reason to keep an accidental one-item default.

**Acceptance:** a small ore-processing trip fits a useful recipe batch plus tools, and unloading it requires a few meaningful transfers rather than one per ore.

#### 5. The opening teaches purchasing machinery more clearly than sustained defence

**Problem and evidence. Observed:** mine → Excavator → Generator is a functioning sequence of changing objectives. **Source-confirmed:** the opening controls hint hides once `st.t > 0`. Later guidance says “Craft Shot at the Home workbench and load a turret”; the next gate accepts any turret with positive rounds, then asks for two carried magazines. It does not establish automatic ammunition supply. The headline “Scheduled assault · Night 3” is supplemented with an earlier-raids caveat in its tooltip/inbox, while first-day minor opportunities occur at 900 and 1080 seconds. [hud.ts, lines 26–29 and 57–61](../../packages/game/src/hud.ts), [goal.ts, lines 81–103](../../packages/sim/src/goal.ts), [campaignAlerts.ts, line 20](../../packages/sim/src/campaignAlerts.ts), [campaignThreat.ts, lines 224–230](../../packages/sim/src/campaignThreat.ts).

**Player consequence — hypothesis:** the game appears to approve scouting after a token load, although the player has not learned the ammunition loop that makes remote defence sustainable. Conversely, a cautious player may stay at Home handcrafting repeatedly because automation's next benefit is not demonstrated.

**Recommended change:** make the smallest coherent opening culminate in useful output reaching storage or a turret. Keep sandbox choice, but distinguish “first turret loaded” from “base supplied”. Put the first relevant raid warning in ordinary visible text before it happens. Keep the movement hint until dismissed or the relevant action is learned, rather than the first simulation tick.

**Dependencies/trade-offs:** do not imply any fixed ammo count guarantees survival. Explain current supply and exposure, preserving the possibility of taking a calculated early scouting risk. This needs goal/feedback adjustments, not a mandatory long quest chain.

**Acceptance:** before leaving, a player can explain where turret ammo comes from, what happens when it runs out, and that Night 3 is not the first possible attack.

#### 6. Some advertised production chains have no useful destination

**Problem and evidence. Source-confirmed:** the active Assembler offers Wire, Frame and Board. Wire feeds Board, but inspected active construction, restoration and upgrade costs do not consume Frame or Board. Their item guide therefore ends with “No implemented consumer yet. Producing this item is optional; keep or transport it.” Electrical equipment still costs raw steel/copper. Polymer, by contrast, has real consumers in Fast belts and Assembler Mk2. [flow.ts, lines 141 and 173–215](../../packages/sim/src/flow.ts), [itemGuide.ts, lines 12–40](../../packages/sim/src/itemGuide.ts), [progression.ts, lines 14 and 49–50](../../packages/sim/src/progression.ts).

**Player consequence — hypothesis:** someone pursuing the apparent electrical/component chain can build working machinery that produces no useful capability. Efficient players avoid it altogether. Automation becomes an attractive-looking dead end.

**Cause:** recipe availability survived while meaningful consumers were replaced by raw-material costs.

**Recommended change:** simplify the default catalogue by removing inactive recipes from the normal choice set, or reconnect a small existing equipment chain where using these components genuinely improves the loop. Do not add arbitrary component tolls to every recipe to justify their existence. Put usefulness ahead of preserving implemented content.

**Dependencies/trade-offs:** preserve existing stock and saves; make any retained experimental recipes explicitly nonessential before players invest. Reconnecting costs requires a separately authorised balance change.

**Acceptance:** every normally advertised production recipe has a discoverable practical use, and a player can name the capability their new line will enable.

#### 7. The expanded city has much more scenery than interactive discovery

**Problem and evidence. Observed:** the map is a large field of repeated blocks with readable major infrastructure. **Source-confirmed:** the current data has 479 buildings, 22 enterable buildings and four with note text. New background density does not itself add loot, inhabitants or rewards. House-note interaction is explicitly tied to authored notes. [riverfront.ts, building data starting line 12516](../../packages/sim/src/city/riverfront.ts), [interaction.ts, line 39](../../packages/sim/src/interaction.ts).

**Player consequence — hypothesis:** investigating plausible-looking doors may teach players to ignore buildings and run directly between project markers. Increased visual density alone cannot establish interesting exploration. No long city walk was performed here, so this is not an observed boredom statistic.

**Recommended change:** make the existing interactive doors and inhabited/occupied places legible through consistent local visual cues; retain broad search areas and mystery about contents. Concentrate clues and distinct existing encounters on useful routes. Assess whether less traversal between these places would deliver the same city fantasy before adding more containers or systems.

**Dependencies/trade-offs:** avoid making every building enterable or revealing undiscovered rewards on the map. Future artwork must preserve gameplay cues, not just architectural variety.

**Acceptance:** on one natural walk between known destinations, a player identifies a worthwhile optional stop from the world and understands why other boarded buildings are background.

#### 8. Later encounters risk being equipment checklists with poor payback

**Problem and evidence. Observed:** the Heart panel describes feeder poles, defence and a 100 kW load, but does not advertise its reward. **Source-confirmed:** Heart/Furnace/Crown progress for 30/40/50 productive seconds, gated by two feeder sides/two busy processors/two powered lights and cleared guards. Each emits bounded waves. Furnace/Crown require Arsenal completion and each cost 30 steel + 15 copper before equipment, power or ammunition. Their reward is 50 steel or copper placed in Home stock; the completion status is simply “Completed”. Heart's Arsenal and rifle upgrade are a real capability change. Artifacts instead give +10% processing speed. [progression.ts, lines 14, 79 and 112–135](../../packages/sim/src/progression.ts), [itemGuide.ts, line 25](../../packages/sim/src/itemGuide.ts).

**Player consequence — hypothesis:** later named encounters promise distinctive engineering battles but may reduce to installing the minimum qualifying objects and waiting between fights. Reusing arbitrary busy recipes or clustering two lights can satisfy counts without creating the intended spatial decision. Fifty raw materials sent somewhere else may not justify the expedition; an artifact may be wasted on a supply-blocked machine.

**Recommended change:** state existing rewards and destination before commitment and on completion. Keep Heart's capability payoff. Reassess the other encounters against an actual player decision: simplify or remove a repetitive requirement if it only counts equipment; retain distinct coverage, production and threat decisions only where play supports them. For artifacts, preview the affected machine's capacity and explain that input shortages still limit output.

**Dependencies/trade-offs:** these are redesign directions, not approved reward values or a request for new bosses. Preserve earned rewards and encounter progress. No completed encounter was observed in this audit.

**Acceptance:** a player can explain how their encounter layout mattered, identify a meaningful new benefit afterward, and find the reward without searching inventories blindly.

### Urgency 3 — clarity and presentation after the core issues

#### 9. Naming and information hierarchy make useful help unnecessarily technical

**Problem and evidence. Observed:** Backpack says “Steel plates” and “Iron ore”; Help's item selector says “Steel” and “Ironore”; goal detail says “Pockets”; the main button says “Backpack”. First-machine inspection leads with measured production over fractional “simulation seconds”, while unavailable artifact actions are also visible. **Source-confirmed:** several help/project labels derive directly from item IDs; old placeable tram construction entries remain in item help even though the active build catalogue correctly hides them. [campaignGuidePanel.ts, lines 19–25](../../packages/game/src/campaignGuidePanel.ts), [itemGuide.ts, lines 28–40](../../packages/sim/src/itemGuide.ts), [inspectionPanel.ts, lines 7–44](../../packages/game/src/inspectionPanel.ts), [campaignGuide.ts, lines 107–111](../../packages/sim/src/campaignGuide.ts).

**Player consequence:** players must translate labels while learning resource relationships. A truthful diagnostic screen can still obscure the next action.

**Cause:** technical identifiers and legacy help entries bypass the newer item presentation, and advanced detail competes with immediate blockers.

**Recommended change:** use one player-facing name per item/location/container across HUD, recipes, map and inventory. Lead inspection with what is happening and the corrective action; retain rate/circuit detail in its existing optional section. Hide or clearly separate inactive legacy construction information.

**Dependencies/trade-offs:** names may change without changing save IDs. Keep exact quantities and accessible labels; thematic names can remain if their purpose is explained once.

**Acceptance:** a player can match the required ingredient to its backpack stack and find a stopped machine's next action without interpreting internal names or opening multiple reference screens.

## 4. UI copy and objective rewrites

These are proposals, not implemented text. References name the current source and context. Behaviour dependencies are explicit.

| Current wording/context | Proposed primary wording | Supporting behaviour needed |
| --- | --- | --- |
| “Mine the remaining materials by hand” — opening HUD ([goal.ts:95](../../packages/sim/src/goal.ts)) | “Hold left-click on steel salvage to collect 10 steel plates.” | Use the actual remaining quantity and existing known patch location; keep the reach prompt. Do not promise a new waypoint. |
| “E · Open supplies” — empty Home ([interaction.ts:38](../../packages/sim/src/interaction.ts)) | “E · Open Home storage” | Show “Empty” inside when appropriate; do not imply starting supplies exist. |
| “Gather 30 steel + 10 copper and place a Generator” ([goal.ts:81](../../packages/sim/src/goal.ts)) | “Build a Generator: 30 steel plates + 10 copper.” Detail: “Load it with coal to power your Excavator.” | Retain state-sensitive fuel and connection checks; do not promise power across unrelated service areas. |
| “Prepared materials · awaiting core” — untouched plant ([progression.ts:132](../../packages/sim/src/progression.ts)) | “Needs materials and a power core.” Once paid: “Materials ready · bring a recovered power core.” | Derive readiness from actual delivered amounts. Optional detail should explain the existing 600 kW output and attack-target consequence. |
| “Prepared · engineering 0s” — Heart ([progression.ts:132–133](../../packages/sim/src/progression.ts)) | “Not ready · deliver materials.” During work: “Commissioning: {progress}/{required} s”. | Add the real blocker and reward description; total progress must come from that encounter's duration. |
| “Engineer under attack” / “Take cover or defend yourself.” ([campaignAlerts.ts:15](../../packages/sim/src/campaignAlerts.ts)) | For relay damage: “Alien relay is draining your health. Move away or behind solid cover.” | **New supporting feedback needed:** damage attribution and a pre-damage hazard cue. Do not use this message for creature contact. |
| **No message/prompt present:** flashlight brightness is not Shade-revealing light | “Shades need powered lighting to become vulnerable.” Detail: “Your flashlight helps you see; it does not expose Shades.” | Add a discoverable first-encounter cue and preparation lead if retaining this rule. This is not a claim that every visible dark enemy is a Shade. |
| “Scheduled assault · Night 3” with earlier-raids caveat in tooltip ([campaignAlerts.ts:20](../../packages/sim/src/campaignAlerts.ts)) | “Major assault: Night 3. Smaller raids can come sooner.” | Keep changing warnings actionable and show known target when selected. Do not state a five-day schedule that the current sim does not use. |
| “Completed” — Furnace/Crown project ([progression.ts:132](../../packages/sim/src/progression.ts)) | “Completed · 50 steel plates added to Home storage” / corresponding copper text | Use the actual reward branch and keep this information recoverable from the project after a transient notice. |
| “Ironore”, “Pockets”, “simulation seconds” — help/goal/inspection | “Iron ore”, “Backpack”, “Measured over {time}” | Shared names, sensible rounding and advanced diagnostics below the immediate action. |

## 5. Paper design versus practical play

| Mechanic | Intended appeal | Actual action/repetition | Evidence | Practical risk or payoff | Recommendation |
| --- | --- | --- | --- | --- | --- |
| Hand mining → machines | Earn relief from manual work | Hold to gather; buy Excavator, then power/fuel/output | Observed opening; goal/flow source | Good concrete feedback; first purchase delays usefulness | **Keep, clarify** the first complete working loop |
| Direct conveyor transfer | Readable automation with few parts | Point belt into/away from a consumer; outputs feed eligible downstream demand | Source-confirmed [directConveyor.ts](../../packages/sim/src/directConveyor.ts) | Strong simplification; mixed-input reservation already prevents some frustrating overfeeding | **Keep**; teach with one useful delivery |
| Component production | Interdependent factory decisions | Wire → Board; Frame/Board have no inspected active consumer | Source-confirmed | A sensible player avoids entire advertised chains | **Simplify or reconnect** |
| Regional plants | Recover stolen technology and restore a district | Clear core defenders, carry core, pay local materials, install | Source-confirmed; untouched plant observed | 600 kW and a new defended site are meaningful; hidden combat prerequisite undermines access | **Keep, clarify** readiness and risk |
| Ruin combat | Choose engagements and escape | Guards patrol locally, notice at eight tiles, chase until twenty; most pursuit/contact behaviour is shared | Source-confirmed [campaignThreat.ts:20, 251–327](../../packages/sim/src/campaignThreat.ts) | Player walking speed 6 versus pursuit speed 2 makes kiting plausible; no proven dominant tactic from this short check | **Keep, assess** one natural fight before retuning |
| Shades and relays | Lighting/position matter | Powered light determines targetability; relay applies continuous nearby damage | Observed damage, source-confirmed rules | Cause is difficult to read; flashlight implies the wrong solution | **Clarify/reconcile** before expanding combat |
| Large authored city | Curiosity and discovery | Mostly non-enterable blocks, a small authored set of interiors and projects | Source-confirmed counts; map observed | Quiet space supports planning, but direct marker-running may dominate exploration | **Clarify and concentrate** existing discoveries |
| Permanent tram and truck | Travel becomes a logistics reward | Power any two stops; freight requires export/request settings and actual local stock; truck handles cargo/construction | Source-confirmed [fixedTram.ts:84–89](../../packages/sim/src/fixedTram.ts), [panel.ts:186–217](../../packages/game/src/panel.ts), [truckWorkPanel.ts](../../packages/game/src/truckWorkPanel.ts) | Tangible capability payoff. “Configure both ends” is already explained; boarding and delivery usability were not played here | **Keep**, inspect one ordinary delivery later |
| Periodic defence and repair | Factory output proves useful under pressure | First major at Night 3; smaller raids earlier; damage, paid repair, retained layouts | Source-confirmed rules; warning boundary inspected | Recoverability is good. Endless repair or an optimal funnel was not established. No kill-salvage loop was found in the inspected campaign kill paths | **Clarify** preparation; assess aftermath value |
| Engineering encounters and artifacts | Distinct challenges and exciting finds | Conditional timers/waves; raw-stock rewards after Heart; removable +10% speed | Source-confirmed; Heart panel observed | Heart changes capability; later requirements/rewards may be chores | **Keep Heart; simplify/reassess** the others |

**Design boundaries:** this active campaign does not implement the requested checklist's “five-day defence” cadence: a day is 20 minutes, first major Night 3 (nominally 55 minutes), with later timing based on post-assault quiet cycles. The inspected enemies do not form a neutral-worker/hostility or component-targeting loot system; do not evaluate hypothetical worker farming or weak-point precision as if present. Breakers and Conductors add durability/structure damage and bounded reinforcements, respectively. Whether their shapes and priorities read in a natural battle remains untested. No impossible material cycle was found in the inspected opening → core → plant → Arsenal dependencies; unclear preparation is the demonstrated problem.

## 6. Recommended fix order

1. **Restore trust in actions and feedback:** resolve focus capture, accurate project readiness, and the relay/Shade hazard explanation. These are the smallest coherent set preventing apparently broken or unfair outcomes.
2. **Complete one understandable opening payoff:** retain mining feedback, show useful automated delivery, communicate earlier raids, and distinguish a loaded turret from sustained supply.
3. **Remove avoidable routine:** correct industrial stack behaviour and stop promoting recipes without practical consumers. Do this before judging travel or freight balance.
4. **Then judge rewards and exploration:** use the existing city and encounters to check whether detours and engineering choices earn their cost; simplify weak repetition before adding content. Apply consistent names and cleaner information hierarchy alongside the relevant fixes.

This is an order of dependencies for addressing the audit, not a new development roadmap or approval to implement them.

## 7. Strengths to preserve and remaining uncertainty

- **Observed:** mining progress, collected quantity and actual backpack totals make gathering understandable once reached. Keep this standard for other actions.
- **Observed:** the Excavator changes the objective, and inspection names its missing power. Build costs and the distinction between carried and stored materials are concrete.
- **Observed/source-confirmed:** the prominent player marker, named plants and fixed tram route support orientation; map navigation selects destinations rather than commanding a walk. Broad core searches preserve uncertainty about exact sites.
- **Source-confirmed:** direct conveyors, finite connected power, transferable real inventory and permanent capability unlocks give the factory a meaningful physical basis. Recovery preserves layouts, stock and installed cores, reducing the cost of ordinary failure.
- **Source-confirmed:** five-second toasts are also recorded in the session alert inbox; radio intelligence persists in saved warning state. Do not claim all feedback disappears permanently. General inbox history itself is session-local ([panel.ts:435–444](../../packages/game/src/panel.ts), [alertInbox.ts](../../packages/game/src/alertInbox.ts)).

Uninspected in natural play: a complete defence/aftermath, remote freight interruption, truck driving/construction, completed encounters, all interiors, resource depletion, audio, smaller screens and sustained performance. Kiting, excessive repair, economy dominance, encounter boredom and traversal pacing remain hypotheses, not proven exploits or average-player reactions. No automated test suite, multi-seed run or campaign simulation was created for this review.

Three useful questions for an actual first-time player:

1. After the first useful machine runs, what do you think you should do next, and why? Ask them to demonstrate how the base will keep receiving ammunition while they leave.
2. At their first dangerous relay, can they identify the source of damage and explain how to deal with the defender they cannot hit, using only the game?
3. After one optional detour or engineering encounter, what changed that made the effort worthwhile—and would they choose another detour over going straight to the next marker?

Documentation checks: `docsync:check` passed; all 43 local report references resolved. `freshness:check` reported 12 stale campaign evidence files, matching the previously recorded baseline; those historical files were not regenerated. No gameplay tests or build were required for this audit-only change.
