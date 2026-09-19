# Founders Court substation — assessment and accepted proposal

Date: 2026-09-19. Status: **design accepted by the owner and implemented the same day (engineering; owner acceptance and the Play Mode check pending, §7).** The owner asked *"What does the Substation do in the starting area? It's a part of the tutorial but does nothing"*, then proposed *"Maybe it could power some light poles in the cul de sac?"* and accepted the worked proposal below (*"Yeah thats perfect"*). Court decision D55 records the acceptance. §1–§6 are the assessment as written before the code changed; §7 records what was built.

## 1. What the substation does today

**Reference (Phaser, `packages/sim/src/campaignPower.ts`).** Each city block's substation was a real power node (`:28`). Poles linked to it, and the block's core load and its streetlights were billed to the circuit it sat on (`:51-55`). Connecting it lit the block, put the core on supply and fed the block throttle behind expansion, camps and the radio. Objective 8 (`goal.ts:113`) tested that circuit.

**Unity port.** `Sim/Power/PowerNetwork.cs` (`PowerGrid.Build`, comment at :70-79) makes every reach node a placed machine and models no core, radio or encounter demand; the block economy was retired under U-D-32. So:

- The authored site `Substation 0 [substation:0]` at (93,350), 3×3, is **not a node**. No pole links to it and it has no reach.
- Its only sim effects are nine solid tiles (`SceneWorld.Compile` FillSolid, court D17) and being drawn as a lot by `CityPresenter.BuildSites`. It cannot be inspected.
- Streetlights (`StreetLights.Sites`) join the grid by the same "nearest covering reach node" path as any machine (`PowerNetwork.cs:245-259`) and light when that circuit's throttle is above zero (`LightSources.Collect`). The substation is never consulted.
- The Home core has no power draw at all.
- The other eight authored substations across the city are in the same state.

## 2. Opening objective 8 versus the sim

Objective 8 (`Sim/Campaign/Opening/OpeningQueries.cs:301-310`, `NearestSubstation` / `SubstationLive` at :797-830) is titled "Connect Founders Court's substation".

| The objective text claims | In the port |
|---|---|
| "The Home core draws 100 kW" | False. The grid has no core demand; `CoreKw` is read only by the data table and this sentence. |
| "the streetlights run from the block substation" | False. Each streetlight joins the nearest placed pole. |
| "Poles link within 8 tiles of … the substation footprint" | False. Poles link only to other placed machines. No cable is drawn to the site. |
| "Until it is connected the base has no power" | False. Machines run the moment they sit in a powered pole's reach. |
| Completion condition | A pole, big pole or placed Substation on a circuit with `Supply > 0` whose centre is within `ReachTiles + max(W,H)/2` (about 9.5 tiles) of the site centre. The code comment calls it a "PORT SUBSTITUTE". When it passes, nothing in the world changes. |

Side findings:

- The site was moved from (78,347) into the Works Yard at (93,350) during the court rework (`FOUNDERS-COURT-CHANGES.md` §6). The objective marker follows the site, but `UI_AND_ONBOARDING.md`, `PHASE_C_HANDOFF.md` and `WORLD_AND_ASSETS.md` still quote (78,347).
- The buildable Substation (`CatalogueData.g.cs:118`: 3×3, 50 Steel + 25 Copper, `ReachTiles` 8, key `]`) is a reach-8 node, which is a Pole's reach at fifty times a Pole's cost. It lost its reference purpose with the block economy.

## 3. Court geometry that the proposal has to respect

Live-scene probe, 2026-09-19 (`Unity/Relight/Temp/fc_sub_probe.cs`, git-ignored):

| Thing | Position |
|---|---|
| Home core | (65,347) 10×14 |
| Cul-de-sac circle | centre (72,374), radius 5.5; court road band x 67..76 from row 369 south to the mouth at row 432 |
| Works Yard | (83,350) 30×29, tiles x 83..112, y 350..378 |
| Substation site | (93,350) 3×3, centre (94.5,351.5), inside the yard beside the copper spool |
| Yard nodes | iron (84,352) 3×4, copper (96,352) 3×4, coal (96,369) 3×3 |
| Court streetlights (6 of the map's 18) | (64,363), (78,363), (89,360), (108,360), (89,375), (108,375); lit disc radius 7 (`LightRules.StreetLightRadiusTiles`), draw 2 kW each (`LightRules.StreetLightKw`) |

Distances from the substation centre to the six lights run from about 10 tiles ((89,360)) to about 32 tiles ((64,363)). **A reach rule cannot do this:** a reach-8 node at (93,350) covers none of them, and a reach-12 node covers one. The reference never used reach for lights either; each block owned its lights and billed them to its substation.

No light is actually on the circle. Four of the six are inside the Works Yard fence and two flank the road just below the core.

## 4. Accepted proposal: the substation powers the court's streetlights

The reference's model for lights, without the core demand. The tutorial step becomes true and its payoff is visible at night.

### 4.1 Sim changes

1. **Substation sites become nodes.** `PowerGrid.Build` adds each `SiteKind.Substation` site as a reach-8 node (`Machine = -1`, no supply, no draw of its own). The reference's `nodeReach('substation')` is `POLE_REACH`, so poles link to it exactly as they link to each other through `NodesLinked`.
2. **Streetlights change owner.** Instead of `Join(nearest covering node)`, each light attaches to the circuit of its **nearest substation site** (by centre distance) and bills `StreetLights.Kw` to it. If that substation is on no supplied circuit the light is unattached and off. The court's six draw 12 kW. On this map all 18 lights map to a sensible substation; the court's six map to `substation:0`.
3. **Step 8 becomes the real thing.** `SubstationLive` reads `grid.OfSite(site.Id)` with `Supply > 0`, replacing the "pole within 9.5 tiles" substitute. `NearestSubstation` is unchanged.
4. **The text tells the truth.** Drop the core sentence and the "no power" sentence. Say: Founders Court's six streetlights run from the block substation in the Works Yard; chain Poles from your network to it and the court lights up at night.

### 4.2 What the player gets

- Two Poles (1 Steel + 1 Copper each) bridge the 19-tile gap between the core's east edge (x 74) and the substation (x 93). The step stays cheap and still teaches pole chaining, which its text already claims to do.
- At night the darkness overlay is 0.86 (`LightingPresenter.nightDarkness`), so six lit discs of radius 7 around the core, the road and the yard are a clear change. In daylight the effect is nil; the opening's readable daylight already accepts that.
- Lights are visual only today: nothing in `Sim/Combat` reads `LightQueries.LitAt`, so this adds no defence effect. Any enemy hesitation in light is a separate decision.
- The other eight authored substations pick up the same rule for free, so district lights across the city come on as poles reach their substation.

### 4.3 Open points to settle when implementing

- **Lights on the circle.** The owner's words were "light poles in the cul de sac". There are none on the circle at (72,374). Adding two or three authored Light sites on the circle's kerb is an Editor scene edit, not code, and can ride along with the implementation. Positions to be chosen against the road band and D42 closure.
- **Ownership rule.** Nearest-substation is a one-line rule and is the recommendation. An explicit owner field on `SiteRecord` is more honest but touches `CityFile`/`HomeRegionImporter` and the scene asset.
- **Buildable Substation machine.** It should probably also adopt nearby streetlights so a player-built one means something, and it still needs a reason to cost more than a Pole or the Electricians gate proposed in the design draft. Left out of this proposal.
- **Saves.** The grid is rebuilt from state each tick, so existing saves need no upgrade; lights that were lit from a nearby pole may go dark until the district substation is reached. Acceptable for the current uncommitted-save test stage; record it if it surprises anyone.

### 4.4 Verification to add

- `PowerGridTests`: a substation site is a node; a pole within 8 tiles of the site's footprint links to it; a site with no linked source has `Supply` 0.
- `StreetLights`/`LightSources` test: a light attaches to its nearest substation's circuit, not to a nearer pole; unattached when that substation is unpowered.
- `OpeningQueries` test: objective 8 stays open with a supplied pole 9 tiles from the site that does not link to it, and completes once the site's own circuit has supply.
- Founders Court Verify: a new check that the court's six lights resolve to `substation:0` and that two poles from the core's east edge reach the site.
- Full `Relight.Sim.Tests`, then a Play Mode night check that the court lights come on when step 8 completes.

## 5. Other routes considered

1. **Full reference behaviour.** The proposal plus core demand, so the substation also carries the Home core's load on the HUD.
2. **Cut step 8 and keep the site as scenery.** The false sentences go with it.
3. **Make it the Works Yard's hub**, so yard extraction only runs once the substation is connected. This changes the taught order.

The owner chose the streetlight proposal in §4.

## 6. Sources read

`Sim/Campaign/Opening/OpeningQueries.cs` (:260-264, :301-310, :797-830), `Sim/Power/PowerNetwork.cs` (:53-58, :68-80, :140-215, :245-259), `Sim/Campaign/Light/StreetLights.cs`, `LightSources.cs`, `LightRules.cs` (:61, :64), `LightPhase.cs`, `Sim/World/WorldSites.cs` (`SiteRecord`), `Sim/Data/Generated/CatalogueData.g.cs:110-121`, `Sim/UI/Build/BuildCatalogue.cs`, `Presentation/City/CityPresenter.cs:414-418`, `Presentation/Light/LightingPresenter.cs`; reference `packages/sim/src/campaignPower.ts:16-83`, `goal.ts:108-113`, `flow.ts:620`; docs `FOUNDERS-COURT-PLAN.md` (circle, lots), `FOUNDERS-COURT-CHANGES.md` §6, `FOUNDERS-COURT-DECISIONS.md` D17/D48–D53, `GAME_DESIGN.md:393`, `UI_AND_ONBOARDING.md:313,330`, `CONTENT_CATALOGUE.md` §17.2/§18. Visual version of this record: the "Founders Court Substation" artifact published from the 2026-09-19 session.

## 7. Implementation (2026-09-19)

The owner asked to *"Implement the substation plan"*. Built as §4.1, with these specifics:

| Piece | Where | What it does |
|---|---|---|
| Substation sites are nodes | `Sim/Power/PowerNetwork.cs` `PowerGrid.Build` | Every `SiteKind.Substation` site is added after the placed nodes, with reach `PowerTuning.SubstationReachTiles` (8). It links to poles, big poles and generators by `NodesLinked` and bridges pole networks through it. |
| Site on a circuit | same, `PowerNetwork.OfSite` | A site is attached only once a placed node is in its group, so a lot no pole has reached creates no circuit and adds no demand. |
| Lights owned by substations | same, `PowerGrid.SubstationOf` | Each streetlight is billed 2 kW to its nearest substation site (centre distance, ties to export order), and is unattached and dark while that site is on no circuit. A region with no substation sites keeps the old nearest-pole rule. |
| Step 8 is real | `Sim/Campaign/Opening/OpeningQueries.cs` `SubstationLive` | `OfSite(site).Supply > 0` replaces the "pole within 9.5 tiles" substitute. The detail text drops the core and "no power" sentences, counts the site's own lights ("Founders Court's 6 streetlights run from the block substation in the Works Yard") and ends "Chain Poles from your network to it and the court lights up at night." |
| Preview and cables | `Sim/Power/PowerLinks.cs`, `Presentation/Flow/PowerConnectionPresenter.cs` | A ghost pole or generator shows its purple link to a substation lot in reach (`PowerLink.SiteId`); a placed one draws a persistent cable to the lot, live when its circuit has supply. |
| Verify C21 | `Assets/Editor/FoundersCourtCompound.cs` | Every streetlight inside the block resolves to `substation:0`, and a network pole at the core's east edge plus two Poles on non-solid tiles put the site on that pole's circuit in the real grid. Verify now reports "of twenty-one checks". |

**One rule differs from the reference and from a literal reading of §4.1.1:** a substation site cables, but never
*owns* a consuming machine. In the reference a substation is a full node, so a machine nearest to it joins it. In the
court the copper node's excavator footprint is 1.6 tiles from the substation centre. Under the reference rule, a
player powering it from a pole to the south (in reach of the excavator but not of the lot) would find it captured
by the lot's dead circuit. Consumers therefore join placed nodes only (pinned by
`ASubstationSiteNeverOwnsAConsumingMachine`).

**Test note:** §4.4 asked for an objective test with "a supplied pole 9 tiles from the site that does not link to
it". With a Pole's reach of 8 and a 3×3 lot, the old substitute's radius (reach + 1.5 from the lot centre) equals the
link radius on the axes and is inside it on the diagonals, so no such position exists. The test instead shows step 8
staying open with a supplied Pole 14 tiles away and completing when the second Pole links.

**Still open:**
- §4.3's first point is not done. There are still no lights on the cul-de-sac circle. Adding two or three Light
  sites there is an Editor scene edit, and no editor was available.
- The buildable Substation machine is unchanged (§4.3).
- Existing saves need no upgrade. A light that was lit from a nearby pole goes dark until its district's substation
  is reached (§4.3).
- Checked in the 6000.6.0f1 editor through the Unity CLI: it compiles with no errors, the `Relight.Sim.Tests`
  EditMode run had 650 passed and 0 failed, and Founders Court Verify had 21 of 21 passed, including C21. Not run:
  the Play Mode night check. Evidence: `evidence/substation-2026-09-19/checks.md`.
