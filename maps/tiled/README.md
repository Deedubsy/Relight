# Riverfront in Tiled

Open **`riverfront-v4/riverfront.tmj`** in Tiled (File → Open). Keep the entire `riverfront-v4` folder together: it includes the external tilesets and their artwork. Use **Save As** to make your working copy. This is the current 864 × 576 Riverfront v4 map, including all 287 buildings and the four new Founders Court houses.

## Editing

- Select **Buildings — select and move these**, then use Tiled's Select Objects tool to move, duplicate or resize buildings. The building image is the object itself. The Properties panel retains `stableId`, `kind`, `facing`, `variant`, `enterable` and source metadata.
- One Tiled grid cell is one game tile: **32 × 32 pixels**. Object X/Y/width/height are pixels, so divide by 32 for game coordinates. Image objects use **top-left alignment**, including resources. Prefer grid snapping and whole-cell sizes.
- Terrain, roads/pavements, footpaths and open yards are separate paintable tile layers. The terrain tileset provides their palette.
- Props, resources, plants/cores/artifacts, tram stops, projects, recruits, lights, substations and district/spawn markers have separate object layers. Props and bindings use simple editor shapes; building roofs use the actual SVG artwork. Dynamic lighting, roof fading, enemy activity and machine inventories are not baked into this layout.
- Toggle the initially hidden **Doorways**, **Parcels**, **Factory reservations**, **Road centre lines**, **Drives and entrance paths**, **Tram controls**, **Collision** and **Tram clearance** layers when arranging the map. The rounded tram line comes from the actual runtime geometry.
- Building floors, collision and rounded rail geometry are locked reference layers. You can unlock them in Tiled, but they remain snapshots: moving a building does **not** automatically move its separate doors/parcels/path, repaint terrain, recalculate collision or reroute the tram. Keep associated objects together using their building IDs. `sourceRecord` is original metadata, not a live position field.

**Saving in Tiled does not update the game yet.** This is an export, not an importer or a replacement for the authoring script. Save your edited map and keep the assets beside it; it can be used as the source for the next integration step. Changes still need conversion, clearance validation and safe map-version handling before use in gameplay. No current game data or saves were changed by the export.

## Repeat the export

From the repository root:

```sh
npm run map:export-tiled -- maps/tiled/riverfront-new-export
```

Always choose a **new folder**. The exporter refuses to overwrite an existing `riverfront.tmj`, protecting your editor changes. It imports the live canonical TypeScript definition and shared tram/door geometry, runs `validateRiverfront`, then creates the map, two tilesets, 65 building SVGs, a terrain palette, `source-layout.json` and `export-report.json` with the source hash. The default output is `maps/tiled/riverfront-v4`.

The map follows the [official Tiled JSON map format](https://doc.mapeditor.org/en/stable/reference/json-map-format/). Tile layers use lossless zlib/base64 data; buildings are editable tile objects, not a single flattened screenshot. `preview.png` was rendered by the installed Tiled `tmxrasterizer`, not by a substitute viewer. `verification.json` records coordinate, asset, layer and native-render checks.
