# Relight — opening design recommendations

Prepared 2026-09-06 at the owner's request to draft recommendation options. **For review: none of the new rules or numbers below is approved or implemented.** Existing approved direction remains in D-EX-01–10; these proposals address D-EX-Q01–06. Phase 4 is complete.

## Recommended starting package

| Decision | Recommendation | Alternative and tradeoff |
|---|---|---|
| Bases and expansion | Each commissioned home/station core anchors one defined neighbourhood base. Build freight links between bases without defending every metre. | Fully player-drawn territories offer freedom but complicate targeting, ownership and exploits. |
| Attack rhythm | Start with a 20-minute day/night cycle; major assaults on every third night, with two full intervening peaceful days. Restoration changes the next eligible assault. | Shorter cycles create urgency but leave less room for roaming; longer cycles make expeditions spacious but delay defence feedback. |
| Radio | A working first tower identifies the threatened base at dawn on its attack day; an upgrade reveals approaches and rough enemy composition. | Keeping the target vague longer increases uncertainty and risks wasting the player's preparation time. |
| Trams | Second-area restoration grants the initial usable tram kit. First slice: one main line, multiple stations, selected deliveries and return freight; local belts branch outward. | Branching tram lines immediately express the full tree but add route/junction problems before freight controls are proven. |
| Resources | Finite loose salvage gets the opening running; persistent extraction sites keep specialised older districts useful. | Entirely finite districts give depletion pressure but risk making old factories obsolete. |
| Defence and discoveries | Walls/turrets take damage; core defeat disables the base; production equipment and cargo remain recoverable. Useful, one-off site rewards rather than random enemy drops. | Broad destruction and dropped inventories raise stakes but can turn expeditions into repair and retrieval chores. |

These are an integrated starting proposal, not a claim that one option is universally best. Values are initial playtest candidates. Exact recipe prices, rates, HP and equipment values should follow the existing economy inventory, with tuning authority recorded before implementation.

## 1. Bases, routes and loss — D-EX-Q01

The starting house contains the first base core. Each later station project can commission one further core associated with a named, fixed neighbourhood. A workshop or loose turret does not independently register another base. The player sees the boundary and hostile approaches before commissioning.

Exploration and temporary construction are allowed ahead of ownership, within normal reach and inventory rules. Temporary machinery needs actual local power. A connected or locally powered core, supplied with the restoration materials, establishes the base; no continuous chain of Held blocks is required. Ownership protects/organises a base but does not create free electricity or a global inventory.

Track and freight corridors do not themselves accumulate per-edge assault demand. Site creatures remain in uncleared locations; major targets are registered bases. Retire ordinary frontier blooms from the new profile. Keep rot as site condition and atmosphere initially; preserve wells as candidate dangerous sites, but do not import their old constant pressure or enclosure kill rule automatically.

**Recommended failure:** the core is disabled, its service stops, and the assault ends once surviving attackers leave along valid routes. The site is recoverable with a visible repair kit. Contents and factory layout remain. Define the disabled core as ineligible for further major targeting until recommissioned, which uses the shared rest schedule. Fighting early waves still consumes ammunition and damages defences, so failure has a cost without routinely erasing a factory.

Tradeoff: fixed neighbourhoods are easy to explain and target, but allow less arbitrary settlement shape. Start there; only add custom boundaries if play shows a need.

## 2. Days, attacks and protected exploration — D-EX-Q02

**Candidate clock:** 20 real minutes at normal speed: 15 daylight, 5 night. Pause freezes the world and scheduler. Test a reasonable range around that candidate only after the owner approves tuning authority. Initially leave mechanical lamp coverage and Shade vulnerability independent of the visual sun; a daylight rendering effect must not pretend an unlit tile is mechanically safe.

**Illustrative opening schedule:** Days 1 and 2 have no major assault. The first starts at dusk on Day 3, about 55 minutes after a dawn start. Days 4 and 5 remain free of major assaults; the next begins on Night 6. The tram restoration should provide the earlier payoff; the first major battle is not the opening's first reward.

A major attack lasts through its finite roster, with an initial target of 3–5 minutes of prepared defence, not an endless spawn until daylight. Measure actual duration and losses. The next attack window follows two complete quiet day/night cycles after the previous assault finishes; if a fight overruns, postpone the next window. After the later progression milestone, use one complete quiet cycle instead. Proposed milestone: the third commissioned station base with working automated resupply, rather than elapsed time alone.

At dawn on an attack day, select and lock one eligible target. Before that lock, a major restoration can nominate its base for the next eligible assault. Multiple restorations coalesce into one response: at lock-in choose among restored eligible bases deterministically, with priority to the most recently commissioned and a stable tie-break. Pending nominations are consumed by that response. A restoration after lock-in belongs to a later eligible window. No accumulated backlog creates attacks on consecutive protected days. The commissioning preview explains whether restoration affects this window or a later one.

**Minor raids:** begin with two opportunities per day across the whole network, each choosing one base and bringing roughly 8–12 ordinary enemies. Introduce the opening with a smaller group if the initial defence needs it; that is a separate candidate. Do not run minor raids during a major assault or immediately after it, and do not stack a deferred backlog. A supplied, established base should normally handle these without the player.

Tradeoff: the initial major defence arrives late in a short session. Test an explicitly labelled defence scenario for development, while judging normal opening pacing from a genuine fresh start. If players find the first hour empty, adjust the cycle after play evidence rather than quietly breaking the promised quiet days.

## 3. Radio warnings — D-EX-Q03

Before radio restoration, the cul-de-sac remains the only eligible major-assault target; background raids and ruin creatures still matter elsewhere. Warn players of the coming assault day and coarse approach cues. This keeps the first defence learnable while the second area introduces attack intelligence.

Restoring the first tower makes subsequent commissioned bases eligible and identifies the locked target at dawn, leaving the candidate 15 daylight minutes to travel and prepare. A later upgrade adds approach direction and broad composition, not a perfect list of every spawn. The basic warning should already be actionable; precision is a benefit rather than relief from guessing which distant factory is about to fail.

For the first slice, one powered tower covers the existing network. Coverage expansion can become a later regional requirement once distances justify it. A warning already received remains in the journal/HUD if power fails. An outage before the next warning blocks new precision and is visibly reported; it does not reroll a committed target. The known assault day remains visible. Target eligibility is unlocked permanently by the first restoration so turning the tower off cannot protect outposts from targeting.

Tradeoff: network-wide early coverage is generous, but avoids adding a radio-coverage puzzle before the player understands trams. It also avoids an upgrade that merely makes the original warning usable.

## 4. Tram access and station freight — D-EX-Q04

The second area's physical restoration costs materials carried by the player. Completion grants the ability to build track/stops/trams and a once-only starter kit containing one tram, two stops and enough track for the generated home-to-station route. The required amount is derived from that route, with grant and delivery recorded through ordinary inventories. The player still lays and powers the line. This is a proposed reward grant, not permission to create untracked debug stock.

Start with one tram on a multi-stop out-and-back main line. Local belts/chests/inserters form the first branches. Later track branches use explicitly selected routes and a reviewed junction model; signals and vehicle traffic are not required in the opening slice. This is a staged implementation of the full network idea, not a final prohibition on tram branches.

Each station has a small table of items it requests up to a target stock and exports above a local reserve. A route manifest assigns cargo amounts to destination stops. Loading commits items to those destinations; earlier stops cannot unload them. The UI shows short supply rather than promising nonexistent goods. Return trips carry designated surplus toward home or another stop on the route.

If a destination is full or unpowered, its cargo stays onboard and the tram continues after bounded dwell. Other reservations remain protected. At its origin, repeatedly undeliverable goods can return to storage, releasing capacity. Track removal stops movement with a visible reason; it never deletes the vehicle or contents. Stuck destinations and any starvation of later allocations must be inspectable.

Example: home loads distinct ammunition allotments for the workshop and northern station. The workshop takes only its allotment. The northern station receives its own and loads local metal for home. No transfer changes the total item ledger.

Tradeoff: destination reservations add data/UI, but make multi-stop delivery understandable and prevent the first station draining the line. Begin with a small item set and fixed route order before richer prioritisation.

## 5. Specialisation, longevity and rare finds — D-EX-Q05

Keep the existing item catalogue for the slice. Concentrate steady steel/copper/coal sources in distinct neighbourhoods, with enough finite loose salvage at home to start production and reach the second area. A resource should not appear everywhere merely to compensate for transport that is not working yet.

Persistent extraction sites have finite throughput and consume operating resources/power; their total resource pool does not run out in the first implementation. They remain strategically useful without requiring a fully renewable scavenging economy. Difficulty comes from capacity, distance, protection and competing supply needs. Define later campaign exhaustion only if it serves the intended old-base usefulness.

Optional caches and unique discoveries are one-off, seeded and saved. Critical transport/power/production unlocks have guaranteed reachable sources. Prefer a distinctive item or blueprint at an identifiable site over low-probability repeated enemy drops.

Tradeoff: inexhaustible sites reduce depletion-driven migration. They strongly support the owner's aim of lasting factories; expansion can instead seek new capabilities, higher throughput and different resources. This is a new core economy proposal requiring approval, not an inference from the prior discussion.

## 6. Defence, workshops and equipment — D-EX-Q06

Use one early wall type, the existing gun turret/ammunition chain and aimed rifle as the first defence kit. Walls physically shape hostile paths; enemies can attack a blocking wall rather than fail pathfinding. Turrets remain useful without the player, and manual fighting helps a weak point or deals with a distinctive enemy.

Walls and turrets can be disabled by damage. A damaged structure keeps its place and remaining contents; replacing/repairing costs normal materials. Belts, track, poles, factory machines and tram cargo remain recoverable and are not routine assault targets. Core defeat follows the rule proposed above. Keep the existing knockdown/inventory-retention model initially; no hunger, armour-tier or corpse-retrieval system is added.

A restored workshop automatically repairs nearby defences between attacks using supplied ordinary materials and power. No free repair and no healing through an active major assault in the first version. The player can repair in reach during a fight at a slower explicit rate with the same material cost. This lets the workshop and tram supplies reduce repetitive maintenance while preparation still matters.

**Proposed first rare reward:** a recovered workshop schematic for a portable field-repair tool that speeds the engineer's repair action while still consuming materials. It opens a battlefield support option without replacing turret/ammunition production. Exact rates and inventory slot behaviour need the equipment contract; this tool is not already approved.

**Proposed distinctive site encounter:** adapt the existing Stalker into a visible guardian of that workshop ruin, with a readable wind-up, a leash and routes to avoid or fight it. A prepared turret approach remains viable. The schematic is a once-only site reward, not a random drop each time the creature respawns. The Heart can serve a later restoration defence after early trams are established.

Tradeoff: recoverable damage is less punishing than full destruction, but preserves the factory-building investment and leaves time for exploration. Workshop repairs need a measured material demand so they do not become an unnoticed infinite resource drain.

## Code baseline note and review boundary

Read-only inspection of fetched main shows reusable city-profile persistence through session/replay, city structures/presentation and Heart attempt/project-state fixes. The local tram implementation explicitly rejects branching track, supporting the staged transport recommendation. These are source observations; test results have not been rerun and the full EX-01 baseline audit remains outstanding. No branch integration occurred.

The most consequential approval is the base/route model and retirement of continuous frontier blooms. The clock and resource-longevity choices come next. Cargo and radio details can then be evaluated together against actual travel and supply distances. The owner may accept, amend or reject individual proposals; none is promoted by elapsed time or by this document existing.

## Validation

Document-only recommendation draft. Local links and decision handles were checked; no game code, constants, save files or historical evidence changed. Existing Windows dependency incompatibility still prevents the full docsync/freshness commands from executing, as recorded in the migration report. No new gameplay tests or human play evidence are claimed.
