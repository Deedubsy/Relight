# Always dark — night and light spec

Date: 2026-09-19. Status: **approved by the owner on 2026-09-19 (*"Go with your recommendations for all of them except I think buildings should block light"*).** Stage 1 (L-01) is implemented as of 2026-09-20 (engineering only; owner acceptance pending), see evidence/always-dark-stage-1/checks.md; stages 2 and 3 (L-02, L-03) are not implemented. This document owns the darkness, light and light-avoidance rules for the Unity port, and the documents listed in §9 point here. Decision: **U-D-58**. Tasks: L-01, L-02, L-03 and L-ACC in `TASKS.md`.

## 1. How we got here

The owner said: *"I feel like the darkness aspect of this game has disappeared and that makes the lighting irrelevant."* The investigation on 2026-09-19 found three causes in the port:

1. **Night barely happens.** `LightQueries.Daylight` is a V-shaped curve. The screen is at least 50% dark for 133 s of each 1200 s day, against 302 s in the reference, and the first dark moment is about 16½ minutes into a session.
2. **The Home core lot is always lit**, so the place the player stands in the opening never goes dark.
3. **Light does nothing to enemies.** Nothing in `Sim/Combat` reads `LightQueries.LitAt`. The hesitation field exists and never triggers (`EnemyPhase.cs:563`).

The documentation had its own gaps: no document specified the night curve, none said what night is for after the "Night 3" rule was retired, `GAME_DESIGN.md` §8.3 claims light avoidance is "Implemented and retained" when the port has none, and no task owns it.

The owner then explained: *"Originally the world was always going to be dark so lighting was much more important."* After weighing a day and night cycle against a permanently dark world, the owner decided: *"Let's go with always dark"*, with the fiction that the aliens blacked out the sun.

## 2. The decision

**The world is always dark. There is no day and night cycle.** Safety depends on where the player is, not on what time it is. The raid director remains the game's only pacing clock.

One rule governs alien behaviour: **aliens avoid light.** "Light" means the sim's lit-tile mask and nothing else:

| Counts as light | Does not count as light |
|---|---|
| Streetlights fed from a connected substation (court D55) | The mouse flashlight (D-UI-11: visibility only) |
| Placed Lamps, Arc lamps and Floodlights with power | Anything the renderer draws that the sim does not know about |
| The always-lit Home core lot (§4) | |

The owner accepted three conditions with the decision. Each is a requirement of this spec:

1. **Dark is a rule, not a black screen** (§3). Unlit ground must stay readable.
2. **Dark districts hold a fixed roaming population, not endless spawns** (§6).
3. **Relief comes from light itself** (§5.4): walking back into lit ground, and a district lighting up, must be strong, visible and audible moments, because there is no dawn.

## 3. The look

- **Unlit ground is a dim twilight.** The darkness overlay's strength on unlit tiles becomes a tunable value, **provisional 0.55** (the port uses 0.86 today, which was set for a short night). The owner tunes this by eye in Play Mode. The working range to try is 0.50–0.60.
- **Lit ground is drawn at full colour**, as now. The contrast between the two is what makes light read as territory.
- **The flashlight is always on.** `MouseFlashlightPresenter`'s `nightOnly` test goes, because it is always night.
- **A brightness setting** is added to the settings screen and scales the overlay strength within safe limits (provisional: 0.35 to 0.70). It changes the picture only and never the lit mask.
- **Aliens always show at least a silhouette.** The owner asked for this: *"I want to at least see a siluette."* Today aliens are drawn under the darkness overlay, so they are dimmed exactly as the ground is and have no guaranteed contrast. The rule:
  - On unlit ground an alien is drawn as a flat dark shape with a faint pale rim, above the darkness overlay, so it reads against the twilight at any overlay strength or brightness setting. Its outline shows its size and type. Its colour and detail do not show.
  - On lit ground, or inside the flashlight beam, the same alien is drawn in full colour.
  - Attack tells (the wind-up tint and the aim line) are always drawn at full strength, in the dark as well, so that an attack can always be read and answered.
  - Silhouettes show anywhere on screen, not only near the engineer. This is a picture rule only. It changes no sim rule: what an alien notices (§5.5) and what a turret can target are unaffected.
- **Feedback about the player's own actions always draws at full strength.** Today the darkness overlay (sorting order 500) sits above the placement ghost and the power-link preview (20), belt items (12), machine direction arrows (10) and cables (9–10), so all of them would be dimmed on unlit ground. The rule: the placement ghost, the power-link and light-coverage previews, cables, machine status cues, alien attack tells and the engineer are drawn above the overlay. The ground, buildings, machines' bodies and belt items stay under it.
- **Placing a light shows what it will light.** The placement preview for a Lamp, Arc lamp or Floodlight outlines the tiles it would light, with blocking applied (§5.3), from the same sim rule that stamps the mask. The player sees coverage and gaps before paying.
- **The HUD says whether the engineer is in light or in the dark.** Safety depends on position and the lit edge is soft on screen, so a small indicator reads `LightQueries.LitAt` for the engineer's tile.
- **The map, when it is built, shows lit and dark districts.** The port has no map screen yet. This is a requirement on that later task, not part of L-01.
- **Solid things block light in the picture and in the rule together** (§5.3). The mask is the single source for both, as it is today.

**Brownouts shrink lights.** Today a light is fully on while its circuit delivers any power at all (`Throttle > 0`), so an overloaded generator costs the player nothing in light. The rule becomes: a light's radius (a Floodlight's range) is its full value multiplied by `0.5 + 0.5 × throttle`, so it is full size at full power, half size at the edge of failure, and off with no supply. The mask, the picture and the alien rules all follow the shrunken radius, and the player sees the pools of light contract. This applies to streetlights, Lamps, Arc lamps and Floodlights. The Home core lot is not a powered light and does not shrink.

How the code expresses "always dark": `TimeTuning.DaylightSeconds = 0` means there is no sun, and `LightQueries.Daylight` then returns `IsDay = false` and `Daylight = 0` at every time. The current formula would turn a zero into a V-shaped curve across the whole day, so this is an explicit case, not only a data change. The admin override (`Admin.Lighting`, F8) stays: "force daylight" remains useful for looking at the map while testing.

## 4. Home and the opening

- **The Home core lot stays always lit.** This is the existing rule (`LightPhase.HomeLot`: the core footprint plus `World.MarginTiles` = 4, an 18×22 rectangle). In an always-dark world it is the new player's one safe, readable spot. The documents say "Home fully lit"; they are corrected to "the Home core lot" (§9), because the rest of Founders Court is dark until its substation is connected.
- **The player spawns inside the lit lot** at (70.5, 360.5). This stays.
- **The Works Yard starts dark.** The first Excavator, the ore nodes and the first belts are built in twilight by flashlight. This is intended, and it is the first thing the acceptance check in §7 looks at: if it is not comfortable at the chosen overlay strength, the strength changes, not the rule.
- **Tutorial step 8 pays off at once.** Connecting the court substation (D55) lights the court's six streetlights the moment the last Pole links. The step's text changes from "the court lights up at night" to "the court lights up".
- **The "readable opening daylight" convention ends.** Readability in the opening now comes from the lit core lot and the twilight strength.

## 5. Light and the attackers

These rules follow the design draft §11 (`docs/Design/RELIGHT_PROGRESSION_AND_WEAPONS_DRAFT.md`, "Light avoidance"), which is approved direction. Hit points, speed and damage never change with light, so turret and ammunition balance is untouched.

### 5.1 Hesitation

An alien that is **not committed** pauses for `RaidTuning.LightHesitateS` (0.65 s, already in the data) before stepping from an unlit tile onto a lit one. The pause is visible: the player can watch aliens gather at the edge of the light. Aliens that are engaged with the player, or that belong to a committed raid group, do not hesitate.

### 5.2 Approach preference

- When the director chooses an entry tile and a route, **lit tiles cost more than unlit tiles**. The extra cost is finite (provisional: a lit tile costs 4 steps), so a fully lit base is still reachable. A raid must never be left without a route; this is a tested guarantee (§7).
- The player can therefore shape attacks: light the approaches that should not be used and leave a dark lane that leads into the turrets.
- A brownout shrinks every light on the circuit (§3), which opens gaps in a lit perimeter. The mask is rebuilt when a light's radius changes, and the director's cached route field (`RaidFieldCache`) must be refreshed whenever the mask is rebuilt; its key does not include light today.

### 5.3 Solid things block light

**What stops a bullet stops light.** The owner decided this: *"I think buildings should block light."* A light does not light a tile when `Sightline.Clear` (`Sim/Combat/Sightline.cs`) says the line between them is blocked. That is the one rule the port already uses for turrets, bullets and the range preview: authored solid building tiles block, the player's own Walls and Barricades block, and no other machine does. As in that rule, the tiles the two ends stand on are exempt, so a lamp placed against a wall still lights its own side.

The mask is rebuilt whenever `st.Rev` changes, which includes every machine placement. With blocking added, each rebuild casts one line per candidate tile per light. L-02 measures this on the full map with a realistic number of lights and, if it is too slow, caches each light's stamped tiles until something inside its radius changes.

### 5.4 Relief events

The sim raises two events so that presentation can mark the moments that replace dawn:

| Event | Raised when | Presentation |
|---|---|---|
| `DistrictLitEvent` | A substation site gains supply and its streetlights turn on | A visible sweep of light across the district and an audio cue |
| `EnteredLightEvent` | The engineer steps from unlit ground onto lit ground, at most once per 10 s | A soft audio cue |

### 5.5 Perception

An uncommitted alien standing on a lit tile notices the player from a shorter distance (provisional: 0.6 of the normal distance). `EnemyDef` has no perception field today, so L-02 adds one tuning value for this and does not change any existing one. This is the only stat-like effect of light. It lets the player slip past the lit edge of a camp and makes an alien in the dark the more dangerous one. One consequence is accepted on purpose: a player who runs a power line out to a camp and floodlights it half-blinds its guards. That is a siege tactic with a real cost, and Spitters shooting out lamps (E-13, E-14) will be its counter.

### 5.6 Teaching the rule

- The first time a group of attackers hesitates at a lit edge, the guide shows one line that says aliens avoid light and will look for a dark way in.
- The opening gains one step after the substation step: place one Lamp. It teaches that light is something the player builds, and what it costs in power.

## 6. The roaming population

This stage belongs with the roster work (tasks E-13 and E-14), because roaming aliens barely exist in the port today. The rules are fixed now so that the earlier stages do not contradict them.

- **A district is the set of tiles whose nearest substation site is the same one.** This is the rule court D55 already uses to assign streetlights, so "lighting a district" and "the district's lights" mean the same thing.
- **Each dark district holds a fixed roaming population** (provisional: 3–6 per district, from the design draft's "ordinary dark street" row). Killed roamers refill slowly (provisional: one per 120 s) up to that number. They never accumulate beyond it, and the world cap of 240 living aliens still applies.
- **Roamers spawn only on unlit tiles that the player cannot see**, at least 20 tiles from the engineer.
- **A lit district has no roaming population.** A district counts as lit while its substation site is on a supplied circuit. When a district becomes lit, its surviving roamers retreat to the nearest unlit tile outside it and no more spawn.
- **Camps are separate and always at full strength.** The owner decided this: *"camps should always have full amounts."* Camp guards are not drawn from, or added to, the roaming population.
- **Roamers hunt the player when disturbed and prefer unlit routes.** They do not know where Home is and do not join raids (design draft §11).
- **Roamers ignore structures.** They do not attack pole lines, remote Excavators or anything else the player has built in a dark district. Only raids attack structures. A long power line through the dark must not become constant repair work.
- **Roamers drop nothing.** Standing under a lamp and killing them must not be a source of materials.

## 7. Stages, checks and saves

| Stage | Contents | Size | Checks |
|---|---|---|---|
| **1. Make it dark** | §3 and §4: always-dark rule in `LightQueries.Daylight`, overlay strength 0.55 and brightness setting, alien silhouettes, the draw-order rule, brownouts shrink lights, the in-light HUD cue, elapsed-time HUD clock, flashlight always on, step 8 text, doc corrections (§9) | Small to medium | Sim tests: `Daylight` is constant with `DaylightSeconds = 0`; admin override still works; a light at half throttle covers three-quarters of its radius and one with no supply covers nothing. Play Mode: the opening is comfortable to play from spawn to the first Excavator; an alien on unlit ground reads clearly as a silhouette at the darkest brightness setting and turns full colour in the beam and under a streetlight; connecting the court substation visibly lights the court. |
| **2. Make light matter** | §5: hesitation, approach preference, solid things block light, light-coverage placement preview, relief events, perception, teaching the rule | Medium | Sim tests: an uncommitted alien pauses 0.65 s at a lit edge and a committed one does not; with every approach lit, the director still returns an entry tile and a route; a lamp behind a solid building or a player-built Wall does not light the far side; the route field refreshes when the mask is rebuilt; a mask rebuild on the full map stays inside the tick budget; `DistrictLitEvent` fires once per connection. Founders Court Verify: C20 (first attack origin) still passes. |
| **3. Populate the dark** | §6 | Large; with E-13 and E-14 | Sim tests: population never exceeds the district number; no spawn on a lit or visible tile; a lit district empties and stays empty; save and load keeps the roamers. |

- **Saves.** Stage 1 and stage 2 change no save format: `LightState` saves nothing and the hesitation field is already saved. Stage 3 adds roaming state to the save and needs a version step.
- **The clock.** `st.T` and every raid timer are unchanged. The HUD's "Day N" counter loses its meaning without a sun; see §10.

## 8. Task rows

| ID | Task | Depends on |
|---|---|---|
| L-01 | Stage 1: always dark, twilight overlay, brightness setting, silhouettes, draw order, brownouts shrink lights, in-light HUD cue, elapsed-time clock, flashlight always on, opening text | D55 (done) |
| L-02 | Stage 2: hesitation, approach preference, solid things block light, coverage preview, relief events, perception, teaching the rule | L-01 |
| L-03 | Stage 3: district roaming population | L-02, E-13 |
| L-ACC | Owner check: the opening played in the dark; a raid against a partly lit base | L-02 |

## 9. Documents corrected at approval (2026-09-19)

| Document | Correction |
|---|---|
| `GAME_DESIGN.md` §7, §8.3 | Point here. §8.3's "Implemented and retained" for light avoidance is wrong for the port; status becomes "specified, L-02". Remove the Shade reference as the reason light matters. |
| `GAME_DESIGN.md` Day/time entry (`daySeconds 1200, daylightSeconds 900`) | The cycle is retired in the port; `daylightSeconds` becomes 0. `daySeconds` remains only as the HUD clock's unit until §10 is settled. |
| `WORLD_AND_ASSETS.md` §2.8 | Remove the "presentation-only daylight curve". State that the overlay strength is a presentation value and the mask is the sim's. Correct the streetlight count: the full map has 18, six of them in Founders Court. |
| `TASKS.md` C-11 | "Home fully lit" becomes "the Home core lot is always lit". Its note "the integrated run never reached night" is closed by L-01's Play Mode check. |
| `TASKS.md` F-04 | Audio: drop the day and night variation of the ambience; keep interior and exterior. Add the two relief cues (§5.4). |
| `UI_AND_ONBOARDING.md` flashlight row | The beam is always on. `docs/RI-02B_UI_SPEC.md` (D-UI-11, "Home is fully lit") belongs to the paused reference project and is left as written; for the port it means the Home core lot. |
| `DECISIONS.md` | Add U-D-58 (always dark) with the owner's words from §1. `U-P-10` (seven emitters) is stale: 18. |
| `FOUNDERS-COURT-SUBSTATION.md` §4.2, §7 | "lights up at night" and "In daylight the effect is nil" no longer apply. |
| `MIGRATION_MAP.md` lighting and day/time rows | Record that the reference's day and night cycle and its daylight curve are deliberately not ported (U-D-58). |
| `CLAUDE.md` | One pointer under the Unity migration paragraph: the port is always dark, and "readable opening daylight" describes the reference only. |

## 10. Decisions taken at approval, and what is still open

Decided by the owner on 2026-09-19, each on the recommendation given:

| Question | Decision |
|---|---|
| Brownouts and lights | Radius shrinks with power, down to half size (§3) |
| Silhouettes | Shown anywhere on screen (§3) |
| The HUD clock | Elapsed play time; the "Day N" counter goes (L-01) |
| Lighting up a camp | Accepted as a siege tactic (§5.5) |
| What blocks light | Everything that stops a bullet: authored buildings and the player's Walls and Barricades (§5.3). This is the owner's own decision, against the recommendation to wait. |
| The Home core lot when the core is disabled | Stays lit. Losing Home must not make recovery harder. |

Still open:

1. **Overlay strength.** 0.55 is the starting value. The owner sets the final value by eye during L-01.
2. **Where the substation step sits in the tutorial.** It stays after the Foundry for L-01. The owner decides whether to move it earlier after playing the opening in the dark.
3. **Tram stops.** A powered stop should light its platform as a safe island. Agreed in principle; specified with the tram work.
4. **The lit court's dark centre, and hard-edged streetlight pools (found 2026-09-20).** Powered, the six Founders Court streetlights leave a diamond-shaped unlit gap in the middle of the court, and their pools have tile-stepped edges beside the soft Home lot and flashlight. Once raids prefer dark approaches (§5.2) the gap is a way in. Decide in L-02: keep it as a deliberate weak spot, or close it (a seventh light or a larger radius), and whether to soften the edges.
5. **The ending.** "Bring the sun back" is a natural campaign goal for this fiction. It is recorded here as a hook and is not designed.

## 11. Out of scope

Flashlight batteries or fuel; damage or fear from darkness; lamps that burn out; any change to alien hit points, speed or damage by light; Spitters targeting lamps and Breakers forcing lit approaches (both arrive with the roster, E-13 and E-14); night-only or time-of-day raid schedules; per-district skies.
