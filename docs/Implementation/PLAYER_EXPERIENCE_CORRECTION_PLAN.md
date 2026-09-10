# Player experience correction plan

10 September 2026 · Approved plan · CITY-F baseline

Implementation was subsequently authorised in D-PE-02; [delivered changes and actual verification](PLAYER_EXPERIENCE_CORRECTIONS.md). Proposed wording below is preserved as the planning record; PROGRESS owns status.

## Scope and authority

The owner requested planning for every finding in the [player clarity and fun audit](PLAYER_CLARITY_AND_FUN_AUDIT_2026-09-10.md) **except finding 6**: “We will add in more turrets that use these resources.” Preserve Wire, Frame and Board recipes and existing stock. Their future turret consumers are an approved direction, but turret types, costs, ammunition, unlocks and balance are not specified or part of this correction plan. Do not hide or remove these recipes as an audit fix, or describe future consumers as already available.

This document defines the proposed changes and acceptance outcomes. [PROGRESS](../PROGRESS.md) remains the only execution/status list. Planning does not mark any gameplay fix implemented or any human gate passed. The original audit remains unchanged as evidence.

Retain real inventory, finite connected power, the fixed four-stop tram, direct conveyors, current campaign geography and normal 1×/pause. Preserve saves, installed cores, earned rewards, player layouts, custom names and action-bar assignments. Do not introduce a new quest framework, mandatory tutorial, new enemy roster or arbitrary resource requirements.

## Proposed implementation sequence

| Order | Scope | Audit finding | Dependency |
| --- | --- | --- | --- |
| A | Reliable controls and returning from UI | 1 | None |
| B | Shared names and clearer inspection | 9 | A for interaction checks |
| C | Truthful projects and encounter progress | 3 | B |
| D | Readable relay hazards and Shade preparation | 2 | B, C |
| E | Deliberate industrial inventory stacks | 4 | None; finish before pacing judgments |
| F | First useful automation and defence guidance | 5 | A–E |
| G | Discoverable places and understandable navigation | 7 | B, C; F before judging travel |
| H | Encounter/reward feedback and practical play review | 8 | C–G |

Each scope should be independently reviewable. Apply shared wording during the relevant change, rather than postponing every copy improvement until the end.

### A. Restore reliable controls after UI interactions

**Change:** separate text-entry/modal capture from focus on passive HUD or closed navigation controls. Closing a drawer or resuming by mouse returns control to the world. Clicking “Why this next?” must not disable unrelated gameplay shortcuts. Preserve keyboard access to controls and the existing plain-Tab inventory close behaviour.

Keep inventory and inspection live initially; only explicit Pause freezes time. Make the live-panel rule discoverable in help, preserve the urgent panel alert, and ensure Escape reliably closes the active layer so the player can react. Opening or closing panels must not move the camera.

**Implementation surface:** [uiShell.ts](../../packages/game/src/uiShell.ts), [main.ts](../../packages/game/src/main.ts), [worldScene.ts](../../packages/game/src/worldScene.ts).

**Acceptance:** after mouse-open/mouse-close of Build, Backpack, inspection and Projects, WASD and shortcuts work immediately. Repeat after HUD explanation and Resume. Typing in quantities, names or blueprint text never moves the engineer. Tab closes inventory from a slot or quantity field. A live threat can be escaped after closing a panel without an extra focus click.

### B. Use consistent names and lead inspection with the next action

**Change:** introduce/reuse one display-name lookup across inventory, recipes, objectives, tooltips, alerts and project material rows. Keep technical IDs stable. Use “Backpack”, “Home storage”, “Steel plates”, “Iron ore”, “Copper ore” and “Shot magazine” consistently; distinguish magazines from rounds. Respect saved player names for places.

Inspection should lead with status, its actual blocker, and a relevant action such as loading fuel, collecting output or locating a known input. Put detailed measured/nominal rates and circuit accounting in the existing details section. Round displayed intervals sensibly and label units. Hide irrelevant upgrade controls until their capability is known; show a clear requirement when a known upgrade cannot yet be used.

Remove obsolete placeable-tram instructions from the active campaign's help while retaining legacy-mode support. **Do not remove the component recipes excluded under finding 6.** For these items, accurately state current uses; future turret plans belong in development documentation until implemented.

**Implementation surface:** [itemIcons.ts](../../packages/game/src/itemIcons.ts), [itemGuide.ts](../../packages/sim/src/itemGuide.ts), [inspection.ts](../../packages/sim/src/inspection.ts), [inspectionPanel.ts](../../packages/game/src/inspectionPanel.ts), [campaignGuidePanel.ts](../../packages/game/src/campaignGuidePanel.ts), [controls.ts](../../packages/game/src/controls.ts).

**Acceptance:** the ingredient in a recipe has the same name as its backpack stack and shortage message. An unpowered Excavator's immediate problem is easier to find than its historical output statistics. Current help explains permanent tram service without suggesting the player build its tracks or vehicle.

### C. Derive project readiness and progress from actual requirements

**Change:** expose a shared read-only readiness model using the simulation's real checks. Distinguish discovered, needs materials, needs core/power/equipment, ready, running, paused/stalled, disabled and complete. Do not conflate delivered materials with installed cores or operating power.

For plants, display remaining materials, the carried-core requirement, and the existing benefit/consequence: finite 600 kW regional supply and a defended attack target. Explain that any recovered core can be used at an eligible plant. For encounters, show productive seconds out of the correct total, the specific current blocker and the existing reward before starting. Display which Heart feeder side is missing, whether two Furnace processors are busy, and whether qualifying Crown lights are powered. Use exact geometry/power checks rather than an approximate UI-only version.

Avoid repeated status text and repeated calls to Start. Retain progress/materials when an encounter pauses. Report the actual command result instead of a generic success message if the state changed before the click was applied.

**Implementation surface:** [progression.ts](../../packages/sim/src/progression.ts), [campaignGuide.ts](../../packages/sim/src/campaignGuide.ts), [goal.ts](../../packages/sim/src/goal.ts), [campaignGuidePanel.ts](../../packages/game/src/campaignGuidePanel.ts).

**Acceptance:** a 0/30, 0/15 plant never says “Prepared”. An in-progress encounter explains its stopped condition and resumes after that condition is corrected. Installation, disabling and repair produce consistent state in the project, HUD and map.

### D. Explain relay danger and the powered-light combat rule

**Proposed rule:** retain powered lighting as the source of Shade vulnerability initially. The flashlight remains a visibility aid. This avoids silently changing combat balance while fixing the missing explanation; it is a planning recommendation, not a claim of owner approval of that rule.

**Change:** add a visible local relay hazard cue before damage, respecting walls and the actual damaging range. Attribute relay damage distinctly from creature contact in the immediate feedback. Give an unhittable Shade a readable, non-targetable presence/cue rather than implying that an empty room is safe. Explain the difference between flashlight visibility and powered-light exposure at the first relevant discovery, and preserve the information in the known project's detail.

Before a relevant expedition, provide a short preparation lead for existing Lamp/Generator/fuel equipment, with power connection details available on demand. Do not require finding an optional lighting recruit for a basic solution. Keep unknown destinations and enemy positions hidden until legitimately discovered. Avoid a permanent world overlay of every enemy detection radius.

Suggested copy: “Shades need powered lighting to become vulnerable.” Optional detail: “Your flashlight helps you see; it does not expose Shades.” On attributed relay damage: “Alien relay is draining your health. Move away or behind solid cover.”

**Implementation surface:** [progression.ts](../../packages/sim/src/progression.ts), [campaignThreat.ts](../../packages/sim/src/campaignThreat.ts), [campaignAlerts.ts](../../packages/sim/src/campaignAlerts.ts), [threat.ts](../../packages/sim/src/threat.ts), [worldScene.ts](../../packages/game/src/worldScene.ts), [riverfrontDraw.ts](../../packages/game/src/riverfrontDraw.ts).

**Acceptance:** in one local relay encounter, the player can identify the damaging source, recognise the unexposed defender, and use ordinary available equipment to make it vulnerable. The visual hazard boundary agrees with damage and solid cover. If this remains confusing after the cues are improved, compare a flashlight-based reveal rule in a separately reviewed change; do not add damage or speed to compensate without evidence.

### E. Make industrial inventory capacity deliberate

**Change:** explicitly classify ordinary bulk items and unique objects. Proposed starting values: stack Iron ore, Copper ore, Crude, Fuel and Polymer to **50**, matching existing bulk materials; stack Shells to **20**, matching the existing ammunition-stack convention and a Cannon's base hopper. These are proposed values for this correction, not measured balance targets. Preserve existing magazine size and one-slot cores/artifacts/packed machines.

Keep transfers based on actual quantities. Display stack capacity in item details where useful. Existing allocated pack layouts must remain valid and conserved: do not automatically reorder a player's pack on load merely because more could fit. Sorting may consolidate; normal receipt should use available room correctly. Verify truck capacity and station/chest item limits remain distinct from backpack stack limits.

**Implementation surface:** [engineer.ts](../../packages/sim/src/engineer.ts), [inventoryPanel.ts](../../packages/game/src/inventoryPanel.ts), [truck.ts](../../packages/sim/src/truck.ts), save validation and existing transfer checks.

**Acceptance:** twelve Iron ore can occupy one slot; a useful Foundry batch plus ordinary equipment can be transported without per-item unloading. Load an old allocated pack, sort, split and transfer through a chest/truck without loss, duplication or hidden overflow. Confirm freight still offers a worthwhile benefit after the stack correction, before adjusting transport balance.

### F. End the opening guidance at a useful supplied loop

**Change:** keep the compact, state-driven goal approach and existing mining progress. Show the current shortage directly: “Hold left-click on steel salvage to collect {remaining} steel plates.” Rename the empty opening interaction to “Open Home storage”. Keep control hints until dismissed or demonstrated, rather than hiding them as soon as time advances.

Guide the current bottleneck through extraction, power/fuel, output collection and useful delivery. Support reasonable alternate build orders and hand-supplied starts. Once the first machine works, make the next payoff visible: an Assembler producing magazines from supplied materials and a conveyor delivering them to storage or a turret. Do not mark a loaded turret as a sustained supply line. Report observed production/delivery and current shortages, not an unearned promise that the base is safe.

Show “Major assault: Night 3. Smaller raids can come sooner” without requiring hover. When an attack target becomes known, show the actual destination and preparation lead. Preserve current timings initially. Scouting remains optional at any stage; a player may knowingly leave an incomplete factory.

Explain the campaign connection in optional goal detail: build supplies and defence → recover a core → restore a plant → gain local power and another site to defend. A permanent tram starts once two stops have power; restoration/truck unlocks are separate benefits. Reuse existing known locations and project tracking rather than inventing markers or sources.

**Implementation surface:** [goal.ts](../../packages/sim/src/goal.ts), [hud.ts](../../packages/game/src/hud.ts), [interaction.ts](../../packages/sim/src/interaction.ts), [campaignAlerts.ts](../../packages/sim/src/campaignAlerts.ts), existing production/connection selectors.

**Acceptance:** a short ordinary new game reaches a visible useful output and an understandable next action. Before leaving, the player can explain how ammunition reaches a turret, what will stop it, and that smaller raids can precede Night 3. Destroying, removing or starving a machine updates the suggestion truthfully; guidance does not impose a rigid quest order.

### G. Make existing discoveries recognisable without spoiling them

**Change:** define a small consistent visual vocabulary for an accessible door, boarded background building, inhabited location and occupied dangerous installation. Apply it to existing content using current art; document the cues so the future artwork package preserves them. Reveal content-specific markers only when knowledge permits.

Add a compact map legend for existing symbols, including core search areas and plant/stop states. Distinguish known search areas by an approximate district/direction rather than presenting three identical names; do not reveal exact relays. Keep the player marker prominent and map clicks non-movement actions. Ensure labels and tracked clues remain readable around HUD overlays.

Use existing house notes, survivors and shortcuts to connect useful routes. Review one route between Home and a known destination plus an optional detour before moving content. First improve cues and clue usefulness; only propose travel-distance or content-placement changes if a natural walk still offers too little choice. Any later placement change must preserve the native Phaser scene, canonical layout and validation agreement.

**Implementation surface:** [riverfrontDraw.ts](../../packages/game/src/riverfrontDraw.ts), [worldScene.ts](../../packages/game/src/worldScene.ts), [cityMapScene.ts](../../packages/game/src/cityMapScene.ts), [navigation.ts](../../packages/sim/src/navigation.ts), [riverfront.ts](../../packages/sim/src/city/riverfront.ts), existing interaction and knowledge rules.

**Acceptance:** a player identifies a worthwhile optional stop from local clues, recognises boarded buildings as background, and understands a broad core search circle. Exact undiscovered rewards remain unknown. No new containers, interiors or generic encounters are required simply to increase activity counts.

### H. Make rewards visible, then test whether the activity earns them

**Change now proposed:** show Heart's existing Arsenal/rifle benefit before commitment. Show Furnace/Crown's current material reward and Home storage destination before and after completion. Keep this on the completed project so a missed notice does not require guessing. For artifacts, show the existing +10% machine capacity benefit and distinguish capacity from realised output when supplies or output space are limiting.

Run a bounded practical review after the above corrections: one representative attempt at each encounter using labelled later-game setups, one optional artifact detour, and one supplied defence with its aftermath. Include a short ordinary tram delivery/truck interaction to check that the stack correction has not made travel logistics pointless. Assisted setups test local behaviour; they do not establish natural campaign pacing.

Compare the actual decisions: feeder placement/approaches, useful busy production, lighting coverage and target priorities. Check whether minimum-count equipment, harmless production, kiting or repeated repairs trivially solve the activity. These are hypotheses to test, not established exploits. Record where the current reward becomes useful and how much of the aftermath is repetitive restoration.

If a requirement only counts objects or idle time, propose simplifying it; if coverage or supply actually changes the battle, preserve that decision. If Furnace/Crown payoff remains weak, propose a concrete existing-capability/reward revision based on the observed cost before implementing it. Do not assign new turret rewards or ingredients under this plan: the owner's future turret direction needs its own specification. No new bosses, enemy mechanics or precision-component drops are implied.

**Implementation surface:** [progression.ts](../../packages/sim/src/progression.ts), [campaignGuidePanel.ts](../../packages/game/src/campaignGuidePanel.ts), [inspectionPanel.ts](../../packages/game/src/inspectionPanel.ts), existing defence and transport UI.

**Acceptance:** players can name the reward and find it after completion. A successful encounter involves an understandable layout/supply/combat choice, not merely opaque waiting. Any tuning or replacement proposal records the tested symptom, relevant trade-off and expected player outcome; untested pacing and human acceptance remain explicitly open.

## Verification and delivery

Use focused sim tests for changed authoritative contracts: readiness/blockers, stack conservation and any new damage attribution. Use actual browser interactions for focus, mining/objective progression, inventory, map cues and local combat feedback. Do not write tests that merely assert rewritten copy. During implementation, run required repository code/documentation checks and report known baseline failures separately; do not launch historical multi-seed/full-campaign work as a substitute for checking the changed experience.

Use isolated saves. Keep one concise evidence record per completed increment, update PROGRESS with actual status, and keep the original audit as the before-state. A real first-time player's answers to the audit's three questions remain the final usability evidence; completing code is not proof of clarity or fun.

Planning verification: all 36 local references resolved; `docsync:check` passed. `freshness:check` retained the same 12 stale campaign evidence files recorded before this plan; historical evidence was not regenerated. No gameplay implementation in this planning change.
