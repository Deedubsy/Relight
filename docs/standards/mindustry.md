# Mindustry — reference report (from the research agent's 90 tool results; agent stalled, compiled by hand)

Sources: Binding.java, bundle.properties, HudFragment, HintsFragment, SettingsMenuDialog, BlockStatus, Turret/BaseTurret, PowerNode, Placement, DesktopInput, MobileInput, InputHandler, PlacementFragment, OverflowGate, Unloader, ItemBridge, Conveyor, Radar, Rules, MapObjectives, AttackIndicators, OverlayRenderer, SoundControl, Renderer, Weathers, WaveSpawner, WaveInfoDialog (editor), PlanetDialog, bundles dir (all https://github.com/Anuken/Mindustry); Steam store https://store.steampowered.com/app/1127400/Mindustry/ (Very Positive 94 % of 9,743; 22 languages; 87 achievements; Cloud; Workshop); Wikipedia; Steam discussions (QoL thread 3104647961276332769, Gnord long review 4042609216213209006, Steam Deck 3271314273760816558, UI scaling 2137462524948850903); Metacritic user reviews; tildesare.cool review 2025-01-17; Mindustry-Suggestions #327, #1269, #299, #498; releases page (v8 build 159, Jul 2026). Blocked: fandom (402), miraheze (403), defkey (403), protondb (empty), timewasters (cert), Reddit (403). Web-search budget exhausted at 200 queries.

## 1 Controls
- WASD moves the player unit; camera follows (detach-camera setting). Shift boost. Left-click place, right-click break, right-drag = area deconstruct (DesktopInput). Q clears the build queue, E pauses building.
- Drag/line placement: click-drag lays a line (`updateLine`); Ctrl = diagonal/"conveyor pathfinding" (A* when the `conveyorpathfinding` setting is on), auto-substitutes junctions and bridges by cost (conveyor 3, junction 30, bridge 200) — auto-underground/bridge is BUILT IN (Placement.java).
- R / scroll rotate; rotation auto-follows cursor during line placement.
- Pipette: middle-click "eye dropper" picks an existing block or plan (PlacementFragment).
- Schematics = copy/paste + blueprints: hold F drag-select; Z/X flip; scroll rotate; T library with names, tags, search, import/export strings; max 66×66 (Steam guide; bundle keys).
- Undo: none (InputHandler "No undo system"). Ghost/derelict rebuild: `ghostBlocks` rule + rebuild-select.
- Hotbar: category tabs + 4-wide block grid; number keys 1–0 pick within a category; , . cycle categories; arrows navigate (Binding.java).
- Rebinding: full keybind dialog (SettingsMenuDialog "Controls"). Controller: none native; Steam Deck via Steam Input, "playable with the default layout but hard … better with mouse and keyboard" (Steam discussion); mobile has a full touch scheme (MobileInput).
- `blockreplace` setting upgrades same-size blocks during line placement (in-place upgrade ≈ upgrade planner lite).

## 2 Logistics affordances
- Conveyors have no lanes (items spread laterally, `xs` clamped −1..1, capacity 3/tile). 3 → 11 → 40 items/s tiers; routers/junctions/bridges cap at 11/s (wiki search).
- Overflow gate: front first, sides alternate; `invert` = underflow. Sorter/inverted sorter filter by item; Unloader has `sortItem` filter; no per-output priority on plain routers.
- Item bridge (range-limited, auto-links to the last placed bridge, dashed placement range) = the underground equivalent; span shown on placement.
- Block status (F6 / `blockstatus` setting): green active, orange noOutput, red noInput, purple logic-disabled, grey inactive — a per-machine starved/blocked light (BlockStatus.java). Turret with no ammo reports `noInput` (Turret.java).

## 3 Information
- Hover/select a block: bars (power balance "+/s", batteries stored/capacity, items, heat), turret range dashed circle on select and on placement (BaseTurret drawPlace/drawSelect), PowerNode laser range circle + auto-link preview, Radar fog radius circle.
- Stats: block stats via F1 (Core Database / ContentInfoDialog) — reload/s, range, ammo, input/output. No production graph over time found; PlanetDialog shows per-sector production/export per minute.
- Power: per-node bars (PowerNode), F5 toggles power lasers; no network graph found.
- Alerts: "< Core is under attack! >" top banner blinking orange/scarlet, click jumps the camera to the last damaged core (HudFragment); minimap attack indicators persist 15 s (AttackIndicators); off-screen enemy indicators (`indicators` setting) and teammate arrows (`playerindicators`) drawn as edge pointers (OverlayRenderer). Campaign: "Sector X under attack!/lost!" toasts.
- Wave UI: "Wave N", "Wave in mm:ss" countdown when `waveTimer`, "N Enemies Remaining", "Guardian approaching in N waves", boss health bar, skip-wave button enabled only when no enemies remain. First wave spacing = 2× (`initialWaveSpacing`). In-play wave composition preview: NOT in HudFragment; WaveInfoDialog is editor-only; QoL thread asks for "preview of incoming waves" and "highlight enemy spawn positions" — spawn drop zones are drawn as dashed circles when near (OverlayRenderer).
- Minimap: M toggles; `position` setting shows tile coords; "too small", resizable requested (#135 antigrief client, #299).
- Recipe browser: Core Database (items/blocks/units/sectors) from menu and in-game; "check the descriptions of every new block" is the community's onboarding advice.

## 4 Onboarding and help
- Hints system (HintsFragment, 27 DefaultHints) triggered by state: desktopMove, zoom, breaking, shoot, pause, unitControl, schematicSelect, conveyorPathfind, boost, blockInfo, derelict, coreUpgrade, coreIncinerate…; `hints` setting toggles them off. No scripted tutorial in current builds (bundle has `tutorial` string; reviews call the old tutorial "very confusing").
- Complaints: "does a poor job of explaining how to make buildings work" (Metacritic); "[ and ] as controls … not something I'd guess in a million years", "after 60 hours … learned even 10 %" (tildesare); "not immediately obvious that you can jump straight to objective locations", "Logic blocks lack explanation", duct icons "nearly identical" (Gnord review).

## 5 Look and feel
- 36 Draw* block animators (flames, pistons, weave, particles, glow, heat), Fx effects for place/break/hit/heal/transfer. Bloom + pixelate rendering toggles, screen shake 0–4 slider (`screenshake`), block status overlay, laser/bridge opacity sliders, animated surfaces toggle, `effects` toggle.
- Lighting: `rules.lighting` + `ambientLight` colour, unit lights, LightMarker; darkness rule; no day/night cycle field in Rules ("no fields related to day/night").
- Audio: ambient / dark (core < 85 % HP, enemy count, wave depth) / boss music (10 s after boss wave spawns); music, SFX, ambient volume sliders; wave-start sound.
- UI scale 25–300 % (200–300 % breaks the menu per Steam thread). No colour-blind option found.

## 6 Environment
- Serpulo/Erekir procedural sectors: floors by temperature/height, ores, rivers (ridged noise), spore trees, derelict enemy bases with scrap debris. Weather: rain (wet, light −0.2), snow, sandstorm, sporestorm (slows air units), fog (light −0.3); frequency/duration per map; `showweather` graphics toggle. Erekir fog of war (turrets/radar/units reveal; Radar 10-tile radius, 10 s spin-up).
- No day/night. Critters: none found.

## 7 Map and world
- Minimap (M) + full-map zoom (0.5×–6×); planet view (N) with sector threat, resources, under-attack/vulnerable flags. Map objectives (13 types) with markers: point, shape, text, line, texture, light, minimap flag (MapObjectives). Player-placed pins: NOT found (markers are map-author/logic tools). Player position readout optional.
- Landmarks: the core; enemy core build-radius rings; spawn drop-zone circles.

## 8 Meta
- Saves: `saveinterval` setting autosave; save slots; Cloud. 43 bundle files (Steam lists 22 languages). 87 achievements. Mods: in-game Mod Browser from the auto-generated Anuken/MindustryMods list, JSON/HJSON/JS/Java, Workshop for maps/schematics. Multiplayer: cross-platform co-op, PvP, public servers; new Steam networking backend (v8 159.3). Free/open-source GPLv3; ~A$14.50 on Steam.
- Settings: full keybinds, language, data import/export, developer console.

## 9 Performance
- Update/render separated (Renderer.java); v8 adds LOD detail hiding when zoomed out, on-demand sound loading (−150 MB), 800×800 maps. Late game "wave 500+ … TPS plunges" (server guide); "lags heavily during an attack" (#731). Fast-forward: developer considers it "not technically viable or needed" (frame-dependent).

## A Body (player unit)
- Player is a ship/mech: item stack carried in the unit, deposited into blocks (depositItems hint), drop validation squares; respawn at core; `autotarget` setting (mobile) / desktopShoot hint; boost. Unit health bars requested in the QoL thread. No inventory grid; the core is the inventory.

## B Threat UI (defence-as-demand)
- Wave counter + countdown + enemies-remaining label; guardian warning N waves ahead; boss HP bar; core-under-attack banner + click-to-jump; minimap attack pings 15 s; off-screen enemy indicators; turret range on place/select; turret no-ammo = red status light; dark music when the core is hurt; spawn zones as dashed circles; campaign sector threat level and under-attack toasts. Wave composition preview during play: not found (requested).

## C City / navigation
- Not a city game. Navigation cues: core landmark, minimap, coordinates, objective markers (map-author), no player pins found.

## Reception (what players complained about)
- Tutorial/UI confusing; unit control overlaps build controls; block re-ordering breaks muscle memory; identical duct icons; hidden controls ([ ]); "obtuse on purpose" learning curve; cannot save a schematic without building first; no undo; minimap too small; UI scale breaks at 200 %+; late-game lag; mobile too slow to react; QoL thread asks for wave preview, spawn highlight, turret range overlay (exists on select), building status overlay (exists as F6 — hidden QoL).
