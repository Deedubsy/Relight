# Gameplay audit evidence — 2026-09-15

Normal-stake New Game play run for [GAMEPLAY_FUN_AUDIT.md](../../GAMEPLAY_FUN_AUDIT.md), Play Mode in the open editor, Standard ruleset, default seed, no grants, no debug raid, no admin drawer. Player actions were sim commands (Input System events do not fire unfocused); see `play-log.md` for the method and the step table.

| File | Sim time | Shows |
|---|---|---|
| 03-start.png | 0 | First goal card at Founders Court; flat tints, no visible ore |
| 08-bullets.png | ≈1000 | Hand-crafted bullets and rifle; card "Prepare your first turret" |
| 09-turret-placed.png | ≈1100 | First turret west of the workshop, hand-loaded |
| 10-warning.png | ≈1105 | "Small enemy group approaching from the W" warning strip |
| 11-approach.png | ≈1140 | Intro skitters approaching |
| 12-contact.png | ≈1160 | Turret engaging; ten rounds fired |
| 13-after-minor-raid.png | ≈2000 | Core 108/300 after the unannounced minor raid; "No power · Home workshop × 667" spam; blocked-assault notice |
| 14-assembler-belt.png | ≈2180 | Assembler → belt → turret; "Automatic resupply working" |
| 15-three-turrets.png | 2336 | End state: three turrets, Power 600 kW · Need 304, output-full strip, final generic card |

Not a reference save; `auto-quit.json` from this run is the audit's end state only.
