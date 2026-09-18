# Correction pass C6 — full-city import evidence

The whole riverfront city exported for Unity beside the Phase C Home crop, and what each file contains.
Regenerate both with, from the repository root:

```
npm run map:export-unity -- --region home
npm run map:export-unity -- --region full
```

Neither file records a timestamp inside `city.json`; only `export-report.json` carries `generatedAt`, so a
re-export of the same sources produces byte-identical map data.

## Exporter output (2026-09-14)

```
Exported Unity/Import/full: region 864x576 at 0,0; 479 buildings, 370 props, 345482/412634 accessible land tiles.
  roads 210 nodes / 371 edges / 371 polylines; 479 paths, 4 drives; tram 5 control, 4909 baked samples, 4 stops, rail length 1226.548.
  sites: 9 substations, 18 lights, 11 resources, 4 yards, 1 service areas, 113 squares, 3 plants, 3 cores, 3 artifacts, 9 projects, 7 recruits, 9 labels.
  combat: 3 camps (12 groups), 2 freight gates, arena inside the region.

Exported Unity/Import/home: region 78x367 at 43,91; 49 buildings, 33 props, 24360/28626 accessible land tiles.
  roads 9 nodes / 24 edges / 24 polylines; 47 paths, 1 drives; tram 5 control, 194 baked samples, 1 stops, rail length 1226.548.
  sites: 2 substations, 7 lights, 3 resources, 1 yards, 0 service areas, 8 squares, 0 plants, 0 cores, 0 artifacts, 0 projects, 2 recruits, 1 labels.
  combat: 3 camps (12 groups), 0 freight gates, arena outside the region.
```

## `Unity/Import/full/`

| file | bytes |
| --- | --- |
| `city.json` | 1,030,827 |
| `terrain.kind.bin` | 497,664 |
| `terrain.variant.bin` | 497,664 |
| `terrain.patch.bin` | 497,664 |
| `terrain.solid.bin` | 497,664 |
| `export-report.json` | 6,547 |

`schema` 1, `mapId` `riverfront-arc-v4-editor-ac16d9188c05`, `tilePixels` 32, `yAxis` `down`,
`sourceSha256` `b19f5a64d9e6cda7441c3867127bb77bec08481db2eab3f724ad6287233c2f3a`,
`manifest.sourceHash` `c4a874ce4064c8fe295c6ca89f1f3fc4f25d2cb271f988c1e2a847973ace1631`.

`region`: `{ id: "full", name: "Riverfront", origin: { x: 0, y: 0 }, size: { width: 864, height: 576 } }`.

`validation`: 497,664 tiles, 412,634 land, 345,482 accessible, 479 buildings, 370 props. The importer
re-derives the solid mask from the buildings and props and compares these counts; a mismatch fails the import.

### Counts

* Buildings 479 (22 enterable, 22 fixed): house 293, shop 76, terrace 32, garage 26, workshop 16, warehouse 15,
  office 5, apartment 5, utility 4, department 2, arcade 2, parking 2, townhall 1. Every building carries a
  `hasDoor` flag and a rect visual.
* Props 370: tree 150, fence 131, furniture 57, container 18, debris 5, converter 3, excavator 2, gate 1,
  crane 1, rock 1, statue 1.
* Roads 210 nodes / 371 edges / 371 polylines; 479 building paths; 4 service drives.
* Tram 5 control points, 4,909 baked samples, length 1226.5476612469843, half-width 3, 1,243 rail tiles, 4 stops.
* Sites: 1 core, 9 substations, 18 lights, 3 plants, 3 cores (campaign), 3 artifacts, 4 stops, 9 projects,
  7 recruits, 11 resources, 4 yards, 1 service area, 113 squares, 9 labels.
* Combat: 3 first camps with 12 spawn groups, 2 freight gates, freight arena inside the region,
  raid line `{ y: 391, x: 69, w: 6 }`.
* Scalars: cell 36, roadHalf 5, pavement 2, setback 2, railHalf 3, tramRadius 12, tramSpeed 40, tramDwell 1.5,
  riverY 477, homeOrigin (58, 345), court (72, 374) r 5.5, homeRaidY 391, lightKw 2.

## The offset relation

Both regions export their own coordinates as region-local, so the same authored place reads
`full = home + (43, 91)`:

| place | `home` | `full` |
| --- | --- | --- |
| region origin | (43, 91), 78x367 | (0, 0), 864x576 |
| spawn | (27, 269) | (70, 360) |
| Home core rect | (22, 256) 10x14 | (65, 347) 10x14 |
| core door | (26, 269) 3x1 | (69, 360) 3x1 |
| court | (29, 283) r 5.5 | (72, 374) r 5.5 |
| homeRaidY | 300 | 391 |
| raid line | `{ y: 300, x: 26, w: 6 }` | `{ y: 391, x: 69, w: 6 }` |
| first camp `freight:camp:1` | (52, 195) | (95, 286) |
| first substation | (35, 256) | (78, 347) |

Pinned by `Assets/Relight/Tests/Sim/World/RegionOffsetTests.cs`, which checks the relation both against
constants and, when the export is present in the checkout, against the two `city.json` files themselves.

## The Home crop is unchanged

Re-exporting `--region home` after the full-region work produced the same bytes as the Phase C export:

```
2bdc22d58047101193c354efd274887d601a44da7d8e4830e7ee8c6f0a4d1be4  city.json
13d322138f9b3afe2f68d57b3770a95f8aadc82c87cead7f9cd87c903bc035f5  terrain.kind.bin
599eabbefde0609063a5de36af78e25464ec21fe9395007ca6979acbbf1f8e92  terrain.patch.bin
162cd9a5c0d220e78ce2c8e1f830b355ef256a7299c18cfac806652529ae43bf  terrain.solid.bin
f609619c986a4f83de3c312ccc353e6d874268292df908fb9460eff1770dc4b0  terrain.variant.bin
```

(`Unity/Import/` is not tracked by git, so byte-identity is shown by hash rather than by `git diff`.)
