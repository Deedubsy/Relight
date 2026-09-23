# FRT-11 (REL-146) — the Freight kite measurement (2026-09-23)

**What this is:** one headless reading of design §9 item 11 (`FREIGHT_STRONGHOLD_DESIGN_2026-09-18.md`): *"a scripted engineer shooting from 18 tiles and retreating; report kills per minute and whether the occupation rule blocks the claim. Recorded as evidence, not pass/fail."* It is a measurement, not a check and not a balance verdict. The owner's play is not part of it, and the difficulty of the fights is an owner playtest item (design §9).

**Who ran it:** the assistant, 2026-09-23, with `FreightKiteMeasurement` (an `[Explicit]` NUnit test in `Unity/Relight/Assets/Relight/Tests/Sim/Combat/Encounters/`). No ordinary test run includes it. It was run in the offline sim-test project (`dotnet test --filter FullyQualifiedName~FreightKiteMeasurement`) against git `a6c1369e` plus the uncommitted measurement file. The Unity editor compiled the same file with no errors.

## Files

| File | What it is |
|---|---|
| `kite-2026-09-23.txt` | The report exactly as the run printed it: the inputs read from the game data, then every case. |

## How it is set up

- **Map:** `RaidFixture`'s open 160 × 160 square with no streets or walls. The West passage marker is at (130, 130) with its four groups, the layout `EncounterPhaseTests` uses. The garrison is the real catalogue row: 12 bodies, 2 of them spitters.
- **Tick order:** the game's own (`SimComposition.Phases`) without `DirectorPhase` and `OpeningPhase`, so no raid or tutorial wave joins in. There are no turrets.
- **Engineer:** a rifle in the hands and 2,000 rounds carried. The engineer walks only and never sprints or dodges. It reloads when the magazine is empty.
- **The script, every tick:** find the nearest living body of the encounter. Fire at it if it is within the rifle's 30-tile reach. Walk directly away while it is nearer than 18 tiles, stand still at 18 or more, and walk towards it when it is out of reach. Once nobody is left, walk to the marker and stand.
- **Seeds:** the seed does not reach the encounter (the garrison's places come from the catalogue), so the three kite cases vary the side the engineer walks in from.

Inputs read from the data: rifle effective range 18 / maximum 30 tiles, 10 damage, 2.5 rounds/s, magazine 10, reload 1.5 s. Engineer walk 6 tiles/s, 100 HP. Skitter 20 HP at 5.4 tiles/s. Spitter 50 HP at 3.9 tiles/s, range 7. Guardian 600 HP at 2.7 tiles/s.

## Results

| Case | Kills | Kills per minute | Damage taken | Hold clock |
|---|---|---|---|---|
| West passage, walk in from the south, kite | 12 / 12 in 30.0 s | 24.0 | 0 | claimed at 67.1 s |
| West passage, walk in from the west, kite | 12 / 12 in 30.6 s | 23.6 | 0 | claimed at 67.9 s |
| West passage, walk in from the south-west, kite | 12 / 12 in 29.4 s | 24.5 | 0 | claimed at 66.9 s |
| West passage, kite 8 s, then walk to the marker and stand | 3 / 12 | 8.4 | 100, **down at 23.7 s** | never started |
| West passage, never shoot, walk to the marker and stand | 0 / 12 | 0 | 100, **down at 16.0 s** | never started |
| Guardian alone, kite from 18 tiles | 1 / 1 in 43.1 s (83 rounds) | 1.4 | 0 | — |
| Guardian alone, stand still and shoot (contrast) | 0 / 1 (30 rounds) | 0 | 100, **down at 16.1 s** after 4 charges | — |

## What it shows, and what it does not

- **Kills per minute:** a walking kiter clears West passage at about 24 kills a minute, with about 3.8 rounds per kill, and takes no damage from any of the three sides.
- **Does the occupation rule block the claim?** Yes, in both cases that tried to get round it. Standing on the marker while guards lived never started the clock, and the engineer went down first. When every guard was dead, the claim landed about 35 s after the last kill: the walk back plus the 30 s hold.
- **The guardian:** it never began a charge against the kiter, and fell in 43 s. An engineer who stood still was charged four times and went down in 16 s.
- **Why the kite is free:** walking at 6 tiles/s outpaces a skitter at 5.4, and the guardian walks at 2.7. This is a reading of today's provisional numbers. Whether the fights are too easy is **for the owner to judge in play**. Nothing here changes a number.
- **Not measured:**
  - the real city's streets, walls and darkness;
  - sprinting and the dodge;
  - Old utility (22) and Northwood (20);
  - the guardian together with its 60;
  - a raid arriving during the fight;
  - how the 30 s hold feels.
