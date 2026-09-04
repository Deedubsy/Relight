# Oxygen Not Included — standards audit reference (2026-09-04)

Sources: wiki.gg (W), fandom (F), Klei forums (K), Steam (S), PCGamingWiki (P). "not found" = no evidence located this pass (web-search budget ran out; later items are fetch-only).

## 1. Controls
- Camera — WASD pan, scroll zoom, `H` home; `Ctrl+1..0` set / `Shift+1..0` jump to camera waypoints — https://oxygennotincluded.wiki.gg/wiki/Controls
- Placement — drag to lay tiles/pipes/wires; `Shift` locks a straight drag; `O` rotates; mirror: not found — https://oxygennotincluded.wiki.gg/wiki/Controls
- Pipette — `B` "Copy Building" picks the hovered building into the build cursor — https://oxygennotincluded.wiki.gg/wiki/Controls
- Ghost placement — build orders are errands carrying a 1–9 sub-priority; `P` priority tool paints priorities onto ghosts/buildings; Yellow Alert overrides schedules — https://oxygennotincluded.wiki.gg/wiki/Priority
- Undo/redo — none in game; requested since Feb 2017 ("remember last 5 actions") — https://kleiforums.com/forums/topic/74134-undo-queued-order/
- Copy settings — per-building "Copy Settings" button, only between identical/compatible pairs (e.g. Storage Locker ↔ Smart Storage Locker); wider pairs still requested — https://kleiforums.com/klei-bug-tracker/oni/copy-settings-between-more-compatible-buildings-r51119/
- Blueprints — no official copy/paste layout mode; Klei's "Blueprints" are cosmetic skins in the Supply Closet — https://oxygennotincluded.wiki.gg/wiki/Versions/U49-575720 ; layout blueprints exist only as the Workshop mod (83,934 subscribers) — https://steamcommunity.com/sharedfiles/filedetails/?id=1814341183
- Deconstruct/cancel — `X` deconstruct, `C` cancel — https://oxygennotincluded.wiki.gg/wiki/Controls
- Area tools — drag-select `G` dig, `Y` harvest, `M` mop, `K` sweep/clear, `I` disinfect, `T` attack, `N` capture — https://oxygennotincluded.wiki.gg/wiki/Controls
- Build menu — bottom-left category toolbar with pop-up sub-windows; players complain the pop-ups clip at 1080p — https://steamcommunity.com/app/457140/discussions/0/5386857950356619700/
- Rebinding — keyboard remapping supported — https://www.pcgamingwiki.com/wiki/Oxygen_Not_Included
- Controller — no native controller UI; 2022 update added Big Picture default configs (Steam Deck, Xbox, DS4) driving a virtual cursor; "mouse/trackpad still the most comfortable" — https://www.gamingonlinux.com/2022/03/oxygen-not-included-got-a-few-improvements-recently-to-help-steam-deck/
- Steam Deck — Klei announced Deck support on its roadmap; Verified badge not confirmed in sources fetched — https://steamcommunity.com/app/457140/discussions/0/5913829825699445320/

## 2. Logistics affordances
- Conveyor rails — 1×1 rail between Conveyor Loader and Receptacle/Chute, passes through walls; needs Mechatronics skill — https://oxygennotincluded.wiki.gg/wiki/Conveyor_Rail
- Conveyor Loader filters — per-material filter, 1000 kg buffer, 20 kg/s unload, automation port, "Allow Manual Use" toggle — https://oxygennotincluded.wiki.gg/wiki/Conveyor_Loader
- Conveyor Shutoff/Bridge/Meter — shutoff gated by automation signal; bridge crosses rails; meter measures flow — https://oxygennotincluded.wiki.gg/wiki/Conveyor_Shutoff
- Liquid pipes — 10 kg packet per segment, 1 update/s; bridges, filters, valves, shutoffs, vents; insulated/radiant variants; phase change breaks pipes — https://oxygennotincluded.wiki.gg/wiki/Liquid_Pipe
- Gas pipes — 1 kg packet per segment; valves/bridges force direction — https://oxygennotincluded.wiki.gg/wiki/Gas_Pipe
- Valve — manual 0–10,000 g/s cap, needs a dupe to set — https://oxygennotincluded.wiki.gg/wiki/Liquid_Valve
- "What's in this pipe" — hovering a pipe in the Plumbing overlay shows contents and flow status — https://oxygennotincluded.wiki.gg/wiki/Liquid_Pipe
- Auto-Sweeper — 9×9 range, 120 W only while moving, moves items low→high priority containers, honours sweep marks — https://oxygennotincluded.wiki.gg/wiki/Auto-Sweeper
- Storage priorities — bin filters by material; priority 1–9 orders where dupes/sweepers deliver and pull from — https://oxygennotincluded.wiki.gg/wiki/Storage_Bin

## 3. Information
- Reports — `E` opens Daily Reports (per-cycle summary incl. power wasted); players asked for material graphs — https://oxygennotincluded.fandom.com/wiki/Daily_Reports ; https://forums.kleientertainment.com/forums/topic/82486-daily-reports-report-materials-as-well/
- Consumables — `F` grid of dupes × foods/medicines with per-cell checkboxes, row/column toggles — https://oxygennotincluded.wiki.gg/wiki/Consumables
- Power overlay — `F2` shows grid components; wire tiers (standard/conductive/heavi-watt), transformers, overload — https://oxygennotincluded.wiki.gg/wiki/Overlay ; https://oxygennotincluded.wiki.gg/wiki/Power
- Alerts — right-side notifications; Red Alert toggled by "!" next to cycle clock, screen edges glow red — https://oxygennotincluded.fandom.com/wiki/Red_Alert ; click-to-jump: not found
- Database — `U` opens it; holds lore (emails, logs, research notes) gathered by inspecting ruins, persisted in unlocks.json across saves — https://oxygennotincluded.wiki.gg/wiki/Controls ; https://oxygennotincluded.wiki.gg/wiki/Lore ; "what makes / what uses this": not found
- Building status — status items "No Power", "Insufficient Resource", "Flooded", "Broken" on the building panel — https://oxygennotincluded-archive.fandom.com/wiki/Statuses ; 1.0 patch added "specific reasons why Duplicants can not learn skills" and correct errand ordering in the building errand panel — https://kleiforums.com/game-updates/oni-alpha/356355-r847/
- Overlays — 16: Oxygen F1, Power F2, Temperature F3, Materials F4, Light F5, Plumbing F6, Ventilation F7, Decor F8, Germ F9, Farming F10, Room F11, Exosuit/Automation/Conveyor/Radiation Shift+F1–F4, Priority P; some gated by research — https://oxygennotincluded.wiki.gg/wiki/Overlay
- Room overlay — shows detected room type and missing requirements — https://oxygennotincluded.wiki.gg/wiki/Room
- Decor overlay — green/red tint, hover breakdown of contributors — https://oxygennotincluded.wiki.gg/wiki/Decor
- Minimap — none; requested repeatedly ("scroll me to death"); community workaround is `Alt+S` screenshot mode to zoom further — https://kleiforums.com/forums/topic/88856-minimap-required/

## 4. Onboarding and help
- Tutorial messages — pop-up messages and tutorial videos on triggers in early game; no option to disable, requested Jan 2021 ("really annoying closing all of these") — https://kleiforums.com/forums/topic/126542-allow-users-to-turn-off-tutorials/ ; https://forums.kleientertainment.com/forums/topic/134814-option-to-disable-tutorial-videos/
- Codex — Database (see §3) doubles as lore/help; players say hints "dont seem dynamic enough for specific circumstances" — https://steamcommunity.com/app/457140/discussions/0/5386857950356619700/
- Difficulty presets — Survival / No Sweat plus per-axis sliders (disease, morale, hunger, stress, suit durability, meteors) at new game — https://oxygennotincluded.wiki.gg/wiki/Game_Settings
- Sandbox — everything unlocked/free; permanently disables achievements — https://oxygennotincluded.wiki.gg/wiki/Sandbox_Mode

## 5. Look and feel
- Light — sunlight up to 80,000 lux from the surface, 0 at night; lamps, printing pod, shine bugs; `F5` overlay; +15 % work speed in ≥1 lux; ≥200 lux disturbs sleep — https://oxygennotincluded.wiki.gg/wiki/Light
- Item movement — solids travel visibly along conveyor rails between loader and receptacle — https://oxygennotincluded.wiki.gg/wiki/Conveyor_Rail
- Feedback — Red Alert glows screen edges; Decor overlay tints tiles — https://oxygennotincluded.fandom.com/wiki/Red_Alert ; https://oxygennotincluded.wiki.gg/wiki/Decor
- Audio — separate Master/SFX/Music/Ambience/UI sliders, closed captions, mute-on-focus-lost; FMOD + Resonance Audio — https://www.pcgamingwiki.com/wiki/Oxygen_Not_Included
- Music — OST by Vince de Vera sold separately; dynamic-music behaviour: not found — https://store.steampowered.com/app/1244160/Oxygen_Not_Included_Soundtrack/
- Building animation states / particles / screen-feedback toggles — not found

## 6. Environment
- Biomes — Sandstone, Forest, Barren, Tundra, Jungle, Magma, Marsh, Ocean, Oily, Rust, Space, separated by abyssalite/granite veins; DLC add more (Frosty: Ice Cave, Cool Pool, Nectar) — https://oxygennotincluded.wiki.gg/wiki/Biome
- Decor as mechanic — per-object radius values, material multipliers (gold +50 %, diamond +100 %), cycle-averaged into morale (+3/+6/+12 at 30/60/120) — https://oxygennotincluded.wiki.gg/wiki/Decor
- Landmarks — geysers/vents/volcanoes with 25–225-cycle activity/dormancy; Field Research skill analyses them; some buried in granite/obsidian — https://oxygennotincluded.wiki.gg/wiki/Geyser
- Liquids — one element per tile, density stacking, displacement, overflow upward, pressure cracks tiles, flooding at 35 % default mass, liquid locks — https://oxygennotincluded.wiki.gg/wiki/Liquid
- Day/night — 600 s cycle, 525 day / 75 night; dupes sleep on schedule, solar panels stop, sim runs 5 ticks/s — https://oxygennotincluded.wiki.gg/wiki/Cycle
- Critters — Hatches, Pips, Dreckos, Pufts, Slicksters etc.; wild vs tamed, egg-based ranching, resource drops — https://oxygennotincluded.wiki.gg/wiki/Critter
- Weather — meteor showers in seasons/showers; Space Scanners give 1–200 s warning for Bunker Doors; in Spaced Out showers only start once an asteroid is visited — https://oxygennotincluded.wiki.gg/wiki/Meteor_Shower ; https://oxygennotincluded.wiki.gg/wiki/Space_Scanner
- Frosty Planet — cold-bound biomes, thermoregulation, new critters/plants/buildings (Jul 2024); blizzard weather: not found — https://store.steampowered.com/app/2952300/Oxygen_Not_Included_The_Frosty_Planet_Pack/

## 7. Map and world
- Asteroid — single vertical asteroid with surface (Space biome), oil and magma at bottom — https://oxygennotincluded.wiki.gg/wiki/Biome
- Fog — undug areas and sealed ruins stay "unknown" until a door is opened or a dupe enters — https://oxygennotincluded.fandom.com/wiki/Ruins ; https://steamcommunity.com/app/457140/discussions/3/1482109512304514216/
- Map pins/notes — none found; camera waypoints `Ctrl+1..0` are the only bookmarks — https://oxygennotincluded.wiki.gg/wiki/Controls
- Rockets (base) — Telescope unlocks starmap; destinations every 10,000 km, +3 cycles per tier, 27 destination types — https://oxygennotincluded.wiki.gg/wiki/Starmap
- Spaced Out — hex starmap revealed by telescope, multiple asteroids managed simultaneously, modular rocket interiors, radiation — https://oxygennotincluded.wiki.gg/wiki/Spaced_Out!
- "Where am I" — `H` returns camera to home/Printing Pod; no overview map — https://oxygennotincluded.wiki.gg/wiki/Controls

## 8. Meta
- Saves — named saves + per-cycle autosave (~10 kept, frequency option in settings); autosave stalls 30–40 s late game — https://kleiforums.com/forums/topic/110474-disableset-intervall-for-autosave/
- Cloud — Steam Cloud with separate `cloud_save_files` folder — https://steamcommunity.com/app/457140/discussions/0/4030221396680495547/ ; https://oxygennotincluded.wiki.gg/wiki/Save_file_locations
- Settings — resolution/windowed, vsync, 60/120 fps, UI scale (UIScalePref in kplayerprefs.yaml), five audio sliders — https://www.pcgamingwiki.com/wiki/Oxygen_Not_Included ; https://support.klei.com/hc/en-us/articles/360037324151
- Colour-blind — patch 419840 added alternate colour sets for crop/power overlays; PCGamingWiki lists no dedicated mode (conflict) — https://www.player.one/oxygen-not-included-update-419840-patch-notes-ui-changes-color-blind-mode-135273 ; https://www.pcgamingwiki.com/wiki/Oxygen_Not_Included
- Localisation — official English, Simplified Chinese, Korean, Russian; 142 Workshop language packs — https://store.steampowered.com/app/457140/ ; https://steamcommunity.com/app/457140/workshop/
- Achievements — 51 Steam; in-colony Imperatives (6) and Initiatives (~50) with progress in Colony Summary; some become permanently disqualified — https://oxygennotincluded.wiki.gg/wiki/Achievements
- Mods — Steam Workshop (1,480 tweaks, 1,134 features, 510 UI) — https://steamcommunity.com/app/457140/workshop/
- Multiplayer — single-player only — https://store.steampowered.com/app/457140/
- Screenshots/timelapse — `Alt+S` screenshot mode; Colony Summary records a screenshot each cycle start with playback — https://oxygennotincluded.wiki.gg/wiki/Controls ; https://kleiforums.com/forums/topic/109844-how-to-use-time-lapse-feature-in-colony-summaries/

## 9. Performance expectations
- Scale — community ceiling ~20 dupes on average hardware; 50-dupe bases lag; pathfinding ~80 % of frame time; 10–15 s freezes reported at 16 dupes late game — https://steamcommunity.com/app/457140/discussions/0/1635291505031358013/ ; https://steamcommunity.com/sharedfiles/filedetails/?id=3303851652
- Sim/render — Unity 2020; sim ticks 5/s regardless of render; partial multithreading (pipes, errands) but main game logic and save/load single-threaded; Klei later fixed pathfinding race conditions — https://oxygennotincluded.wiki.gg/wiki/Cycle ; https://kleiforums.com/forums/topic/134527-multi-thread-performance-improvements/ ; https://forums.kleientertainment.com/forums/topic/168879-game-update-701091/
- Time scaling — `Space` pause, `Tab` cycles speeds, `Num +/-` step; number of speeds: not found — https://oxygennotincluded.wiki.gg/wiki/Controls
- Save stalls — 30–40 s autosaves after cycle 200 — https://kleiforums.com/forums/topic/110474-disableset-intervall-for-autosave/

## Early reception complaints
- No real tutorial / wiki dependence — "in-game hints are useless", "No tutorial for a game this complexed … UNFINISHED" — https://steamcommunity.com/app/457140/reviews/ ; https://www.metacritic.com/game/oxygen-not-included/user-reviews/
- Learning curve "basically an up arrow"; critics flagged "lack of in-game learning tools" — https://steamcommunity.com/app/457140/discussions/0/1735463620084093038/ ; https://en.wikipedia.org/wiki/Oxygen_Not_Included
- UI clutter/scaling — toolbar pop-ups clip at 1080p, "multi-platform framework … optimization for none", tiny text; scaled-up UI overflows — https://steamcommunity.com/app/457140/discussions/0/5386857950356619700/ ; https://forums.kleientertainment.com/forums/topic/98459-ui-scaling/
- Priority system opaque — "Priority system is broken", dupes "run in circles" — https://steamcommunity.com/app/457140/discussions/0/805720464937780521/
- Late-game performance and autosave freezes — https://steamcommunity.com/app/457140/discussions/0/1635291505031358013/
- Missing QoL — undo, minimap, tutorial off-switch, autosave count control all long-standing requests — https://kleiforums.com/forums/topic/74134-undo-queued-order/ ; https://kleiforums.com/forums/topic/88856-minimap-required/ ; https://kleiforums.com/forums/topic/126542-allow-users-to-turn-off-tutorials/
