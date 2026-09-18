/** D-01a / D-01b — one-directional region export for the Unity importer (D-02a / C-01).
 *
 *  Tooling only: reads the canonical authored sources, writes JSON + binary side-cars under `Unity/Import/<region>/`,
 *  and changes no gameplay. Never re-imported (WORLD_AND_ASSETS.md §7.2 note 6); the Tiled export under `maps/tiled/`
 *  is stale and is never an input (U-D-21).
 *
 *  Usage: `npm run map:export-unity -- --region home|full [--out <dir>]`
 *
 *  Two regions, one code path and one schema:
 *    * `home` — the Home opening crop derived from the anchors below (78x367 at 43,91, named "Founders Court").
 *    * `full` — the whole canonical riverfront city, 864x576 at origin (0,0), named "Riverfront". Because the origin
 *      is (0,0) every exported coordinate is already an ABSOLUTE city coordinate, so full-map absolute =
 *      Home-crop local + (43,91) exactly (correction-pass contract C6).
 *  `home`'s `city.json` and side-cars are byte-identical to the D-01a export: nothing below adds, removes or
 *  reorders a field for the Home crop, and every `full` difference is a value the region rect already decided.
 *
 *  Every coordinate in `city.json` is REGION-LOCAL: the region origin has been subtracted. Terrain side-cars cover
 *  the region rect only, row-major, `t = y * width + x`, Y down (no flip here; the Unity importer flips, §2.2).
 *  Determinism: no timestamp is written into `city.json`; `generatedAt` lives only in `export-report.json`, so two
 *  runs produce byte-identical `city.json` and side-cars.
 */
import {mkdirSync, readFileSync, writeFileSync} from 'node:fs';
import {resolve, join} from 'node:path';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {
  RIVERFRONT as C, RIVERFRONT_BUILDINGS as buildings, RIVERFRONT_PROPS as props,
  createCampaign, ground, groundTiles, TILE_PX, riverfrontRail, doorRect,
  T_RIVER, TILE_NAMES,
} from '@relight/sim';
import {validateRiverfront} from '../../sim/src/city/validateRiverfront';
import {validateFirstRegion} from '../../sim/src/firstRegion';
import {FIRST_CAMPS, FREIGHT_GATES, FREIGHT_ARENA} from '../../sim/src/city/gameplaySites';

type Rect = {x: number; y: number; w: number; h: number};
type Anchor = {name: string; why: string} & Rect;

const root = resolve(fileURLToPath(new URL('../../../', import.meta.url)));

// ------------------------------------------------------------------ CLI
const argv = process.argv.slice(2);
const flag = (name: string): string | undefined => {
  const i = argv.indexOf('--' + name);
  return i >= 0 ? argv[i + 1] : undefined;
};
const regionId = flag('region') ?? 'home';
/** The two authored regions. `full` is the whole city, so its origin is (0,0) and its coordinates are absolute. */
const REGIONS: Record<string, {name: string; whole: boolean}> = {
  home: {name: 'Founders Court', whole: false},
  full: {name: 'Riverfront', whole: true},
};
if (!REGIONS[regionId]) throw Error(`Unknown region '${regionId}'. Known regions: ${Object.keys(REGIONS).join(', ')}.`);
const outDir = resolve(root, flag('out') ?? join('Unity/Import', regionId));

// ------------------------------------------------------------------ sources (exportTiled.ts:15-17 preamble)
const state = createCampaign();
const G = ground(state);
const {reachable, ...cityValidation} = validateRiverfront(state);
cityValidation.errors.push(...validateFirstRegion(state));
if (cityValidation.errors.length) throw Error(cityValidation.errors.join('\n'));
void reachable;   // the region's own accessible count is re-derived below from the exported arrays

const manifest = JSON.parse(readFileSync(join(root, 'maps/phaser/manifest.json'), 'utf8')) as
  {version: number; sourceHash: string; base: {city: {id: string}}; fixedBuildings: string[]};
const fixedIds = new Set(manifest.fixedBuildings);

const rail = riverfrontRail();
const tiles = groundTiles(state);                    // the renderer's own kind/variant/patch (ground.ts:561)
const solid = G.urban!.solid;                        // the authored collision mask (ground.ts:628-630)
const W = C.width, H = C.height;

// ------------------------------------------------------------------ the Home region rect
/** Everything the Home opening and its approaches touch, each taken from the code, not from a document. */
const anchors: Anchor[] = [
  {name: 'opening enclosure', why: 'ground.ts:634 G.opening.bounds = homeOrigin-[7,1], size 40',
   x: C.homeOrigin[0] - 7, y: C.homeOrigin[1] - 1, w: 40, h: 40},
  {name: 'Home lot (HQ frame)', why: 'riverfront.ts:33875 blocks[0].sqx/sqy = 58,345, sq = 24',
   x: 58, y: 345, w: 24, h: 24},
  {name: 'Founders Court bulb', why: `riverfront.ts court ${C.court.x},${C.court.y} r ${C.court.radius}`,
   x: Math.floor(C.court.x - C.court.radius), y: Math.floor(C.court.y - C.court.radius),
   w: Math.ceil(C.court.radius * 2) + 1, h: Math.ceil(C.court.radius * 2) + 1},
  {name: 'raid approach line', why: `ground.ts:634 opening gate row homeRaidY ${C.homeRaidY}, court.x-3 .. +2`,
   x: C.court.x - 3, y: C.homeRaidY, w: 6, h: 1},
  {name: 'Home factory yard', why: 'riverfront.ts yards[0]', ...C.yards[0]},
  {name: 'Home substation', why: 'riverfront.ts substations[0], 3x3 (ground.ts:630)',
   x: C.substations[0][0], y: C.substations[0][1], w: 3, h: 3},
  {name: 'tram:home stop', why: 'riverfront.ts stops[0], 2x2 platform (exportTiled.ts)',
   x: C.stops[0].x, y: C.stops[0].y, w: 2, h: 2},
];
for (const [item, x, y, w, h] of C.resources) {
  // Home salvage only: the three HQ-lot patches (tiles.ts HQ_PATCHES, resolved against homeOrigin).
  if (x >= C.homeOrigin[0] - 1 && x < C.homeOrigin[0] + 24 && y >= C.homeOrigin[1] && y < C.homeOrigin[1] + 24)
    anchors.push({name: `Home ${item} salvage`, why: `riverfront.ts resources ${item} ${x},${y}`, x, y, w, h});
}
for (const camp of FIRST_CAMPS) {
  anchors.push({name: camp.id, why: `gameplaySites.ts FIRST_CAMPS key marker`, x: camp.x, y: camp.y, w: 1, h: 1});
  for (const [gx, gy] of camp.groups)
    anchors.push({name: camp.id + ' group', why: 'gameplaySites.ts FIRST_CAMPS spawn group', x: gx, y: gy, w: 1, h: 1});
}
// firstRegion.ts:31 places camp defenders up to 4 tiles from each group centre.
const CAMP_SPAWN_RADIUS = 4;
/** Padding so the region edge is outside anything the opening uses (brief: "pad so the edges are outside"). */
const PAD = 8;

let x0 = Infinity, y0 = Infinity, x1 = -Infinity, y1 = -Infinity;
for (const a of anchors) {
  const grow = a.name.endsWith(' group') ? CAMP_SPAWN_RADIUS : 0;
  x0 = Math.min(x0, a.x - grow); y0 = Math.min(y0, a.y - grow);
  x1 = Math.max(x1, a.x + a.w - 1 + grow); y1 = Math.max(y1, a.y + a.h - 1 + grow);
}
// The whole city is not a crop: its rect is the map, its origin is (0,0), and the anchors above are kept for the
// report only (they still say which tiles the Home opening needs, which is how a later crop would be re-derived).
const region: Rect = REGIONS[regionId].whole
  ? {x: 0, y: 0, w: W, h: H}
  : {x: Math.max(0, x0 - PAD), y: Math.max(0, y0 - PAD), w: 0, h: 0};
if (!REGIONS[regionId].whole) {
  region.w = Math.min(W, x1 + 1 + PAD) - region.x;
  region.h = Math.min(H, y1 + 1 + PAD) - region.y;
}
const RX = region.x, RY = region.y, RW = region.w, RH = region.h;

const inRegionTile = (x: number, y: number) => x >= RX && y >= RY && x < RX + RW && y < RY + RH;
const rectHits = (r: Rect) => r.x < RX + RW && r.x + r.w > RX && r.y < RY + RH && r.y + r.h > RY;
const polylineHits = (path: readonly (readonly number[])[]) => path.some(([x, y]) => inRegionTile(x, y)) ||
  path.slice(1).some(([bx, by], i) => {
    const [ax, ay] = path[i];
    return Math.min(ax, bx) < RX + RW && Math.max(ax, bx) >= RX && Math.min(ay, by) < RY + RH && Math.max(ay, by) >= RY;
  });

// ------------------------------------------------------------------ region-local helpers
//
// Container shape, deliberately different from the WORLD_AND_ASSETS.md §7.2 sketch: every point is `{x,y}`, every
// rect `{x,y,w,h}`, every polyline `{points:[…]}`, every map a list of `{id,…}`, and no field is ever `null`
// (an absent record is an empty list or a `has…` flag). Reason: this Unity project has no JSON library —
// `Packages/manifest.json` carries only `com.unity.modules.jsonserialize`, i.e. `JsonUtility`, which cannot read
// tuple arrays (`[[x,y],…]`), dictionaries or nulls. Keeping the export in JsonUtility's subset means the D-02a
// importer is a `JsonUtility.FromJson<CityFile>` call instead of a hand-rolled parser. Values and names are
// otherwise §7.2's.
const lx = (x: number) => x - RX, ly = (y: number) => y - RY;
const pt = (x: number, y: number) => ({x: lx(x), y: ly(y)});
const localRect = (r: Rect) => ({x: lx(r.x), y: ly(r.y), w: r.w, h: r.h});
const localPath = (path: readonly (readonly number[])[]) => ({points: path.map(([x, y]) => pt(x, y))});
const site = (s: {id: string; name: string; x: number; y: number}) => ({id: s.id, name: s.name, x: lx(s.x), y: ly(s.y)});

// ------------------------------------------------------------------ terrain side-cars (region rect only)
const n = RW * RH;
const kind = Buffer.alloc(n), variant = Buffer.alloc(n), patch = Buffer.alloc(n);
const solidMask = new Uint8Array(n);
for (let y = 0; y < RH; y++) for (let x = 0; x < RW; x++) {
  const src = (RY + y) * W + RX + x, dst = y * RW + x;
  kind[dst] = tiles.kind[src];
  variant[dst] = tiles.variant[src];
  patch[dst] = tiles.patch[src];
  solidMask[dst] = solid[src];
}

// ------------------------------------------------------------------ spawn and the region's own validation counts
const spawn = pt(Math.floor(state.engineer.x), Math.floor(state.engineer.y));
if (!inRegionTile(Math.floor(state.engineer.x), Math.floor(state.engineer.y)))
  throw Error('The engineer start is outside the chosen region rect.');

/** The importer re-derives these from the exported arrays alone; keep both sides on this exact algorithm. */
function accessibleFromSpawn(): number {
  const seen = new Uint8Array(n), queue = new Int32Array(n);
  const start = spawn.y * RW + spawn.x;
  let count = 0, end = 1;
  queue[0] = start; seen[start] = 1;
  for (let at = 0; at < end; at++) {
    const t = queue[at], x = t % RW, y = (t - t % RW) / RW;
    count++;
    for (const [xx, yy] of [[x - 1, y], [x + 1, y], [x, y - 1], [x, y + 1]]) {
      if (xx < 0 || yy < 0 || xx >= RW || yy >= RH) continue;
      const k = yy * RW + xx;
      if (seen[k] || solidMask[k] || kind[k] === T_RIVER) continue;
      seen[k] = 1; queue[end++] = k;
    }
  }
  return count;
}
let land = 0;
for (let t = 0; t < n; t++) if (kind[t] !== T_RIVER) land++;

const regionBuildings = buildings.filter(b => rectHits(b));
const regionProps = props.filter(p => rectHits(p));
const validation = {
  buildings: regionBuildings.length,
  props: regionProps.length,
  land,
  accessible: accessibleFromSpawn(),
  tiles: n,
};

// ------------------------------------------------------------------ records
/** WORLD_AND_ASSETS.md §6.2 — one rule, precomputed here so Unity never reimplements it (phaserCityModel.ts:20). */
const roofKey = (b: typeof buildings[number]) =>
  `rf-${b.kind}${b.kind === 'house' && b.facing !== 'S' ? '-' + b.facing : ''}-roof-${b.variant ?? 0}`;

const buildingRecords = regionBuildings.map(b => {
  const d = doorRect(b);
  return {
    id: b.id, name: b.name, kind: b.kind, facing: b.facing,
    rect: localRect(b), parcel: localRect(b.parcel), visual: localRect(b.visual),
    path: localPath(b.path),
    enterable: !!b.enterable,
    hasDoor: !!b.door,
    door: b.door ? pt(b.door[0], b.door[1]) : pt(RX, RY),
    doorRect: localRect(d),
    doors: (b.doors ?? []).map(([x, y, w, h]) => localRect({x, y, w, h})),
    variant: b.variant ?? 0,
    roofKey: roofKey(b),
    compound: b.compound ?? '',
    content: b.content ?? '',
    note: b.note ?? '',
    // `isFixed`, not `fixed`: `fixed` is a C# keyword and JsonUtility binds by exact field name.
    isFixed: fixedIds.has(b.id),
  };
});
const propRecords = regionProps.map(p => ({id: p.id, kind: p.kind, rect: localRect(p), clearable: !!p.clearable}));

const roadNodesIn = C.roadNodes.filter(nd => inRegionTile(nd.x, nd.y));
const roadNodeIds = new Set(roadNodesIn.map(nd => nd.id));
const roads = {
  nodes: roadNodesIn.map(nd => ({id: nd.id, x: lx(nd.x), y: ly(nd.y), end: nd.end ?? ''})),
  edges: C.roadEdges.filter(([a, b]) => roadNodeIds.has(a) || roadNodeIds.has(b)).map(([a, b]) => ({from: a, to: b})),
  polylines: C.roads.filter(polylineHits).map(localPath),
  halfWidth: C.roadHalf, pavement: C.pavement,
};

const combat = {
  firstCamps: FIRST_CAMPS.filter(c => inRegionTile(c.x, c.y)).map(c => ({
    id: c.id, name: c.name, x: lx(c.x), y: ly(c.y), count: c.count, spawnRadius: CAMP_SPAWN_RADIUS,
    groups: c.groups.map(([x, y]) => pt(x, y)),
  })),
  // Reference-only in the Home region: the freight stronghold itself is D-01b/E-15 content.
  freightGates: FREIGHT_GATES.filter(rectHits).map(g => localRect(g)),
  hasFreightArena: rectHits(FREIGHT_ARENA),
  freightArena: {
    rect: localRect(FREIGHT_ARENA), guardian: pt(FREIGHT_ARENA.guardian[0], FREIGHT_ARENA.guardian[1]),
    count: FREIGHT_ARENA.count, groups: FREIGHT_ARENA.groups.map(([x, y]) => pt(x, y)),
  },
  raidLine: {y: ly(C.homeRaidY), x: lx(C.court.x - 3), w: 6},
};

const sites = {
  /** U-D-19: the Home core IS the Home workshop building. */
  hasCore: buildings.some(q => q.id === 'home-workshop' && rectHits(q)),
  core: (() => {
    const b = buildings.find(q => q.id === 'home-workshop')!;
    return {id: b.id, name: b.name, rect: localRect(b), door: localRect(doorRect(b))};
  })(),
  // 3x3 lots (ground.ts:630): filtered on the whole lot, not its corner, so the importer's solid re-derivation
  // sees every substation that touches the region.
  substations: C.substations.filter(([x, y]) => rectHits({x, y, w: 3, h: 3})).map(([x, y]) => ({x: lx(x), y: ly(y), size: 3})),
  lights: C.lights.filter(([x, y]) => inRegionTile(x, y)).map(([x, y]) => pt(x, y)),
  plants: C.plants.filter(p => inRegionTile(p.x, p.y)).map(site),
  cores: C.cores.filter(p => inRegionTile(p.x, p.y)).map(site),
  artifacts: C.artifacts.filter(p => inRegionTile(p.x, p.y)).map(site),
  stops: C.stops.filter(p => inRegionTile(p.x, p.y)).map(site),
  projects: Object.entries(C.projects).filter(([, [x, y]]) => inRegionTile(x, y))
    .map(([id, [x, y]]) => ({id, x: lx(x), y: ly(y)})),
  recruits: C.recruits.filter(([, x, y]) => inRegionTile(x, y)).map(([k, x, y]) => ({kind: k, x: lx(x), y: ly(y)})),
  resources: C.resources.filter(([, x, y, w, h]) => rectHits({x, y, w, h}))
    .map(([item, x, y, w, h, units]) => ({item, x: lx(x), y: ly(y), w, h, units})),
  yards: C.yards.filter(rectHits).map(r => ({x: lx(r.x), y: ly(r.y), w: r.w, h: r.h})),
  serviceAreas: C.serviceAreas.filter(rectHits).map(r => ({x: lx(r.x), y: ly(r.y), w: r.w, h: r.h, block: r.block})),
  squares: C.squares.filter(rectHits).map(r => ({x: lx(r.x), y: ly(r.y), w: r.w, h: r.h, name: r.name})),
  labels: C.regions.filter(([, x, y]) => inRegionTile(x, y)).map(([name, x, y]) => ({name, x: lx(x), y: ly(y)})),
};

const tram = {
  hasControl: polylineHits(C.tram),
  control: localPath(polylineHits(C.tram) ? C.tram : []),
  baked: rail.samples.filter(p => inRegionTile(Math.floor(p.x), Math.floor(p.y)))
    .map(p => ({x: p.x - RX, y: p.y - RY})),
  length: rail.length,
  radius: C.tramRadius, speed: C.tramSpeed, dwell: C.tramDwell,
  stops: sites.stops,
};

// ------------------------------------------------------------------ write
const sourceFiles = [
  'packages/sim/src/city/riverfront.ts',
  'packages/sim/src/city/gameplaySites.ts',
  'packages/sim/src/firstRegion.ts',
];
const sourceSha256 = createHash('sha256')
  .update(Buffer.concat(sourceFiles.map(f => readFileSync(join(root, f)))))
  .digest('hex');

const city = {
  schema: 1,
  mapId: C.id,
  region: {id: regionId, name: REGIONS[regionId].name, origin: {x: RX, y: RY}, size: {width: RW, height: RH}},
  sourceSha256,
  manifest: {version: manifest.version, sourceHash: manifest.sourceHash, cityId: manifest.base.city.id},
  tilePixels: TILE_PX,
  yAxis: 'down',
  spawn,
  scalars: {
    cell: C.cell, roadHalf: C.roadHalf, pavement: C.pavement, setback: C.setback, railHalf: C.railHalf,
    tramRadius: C.tramRadius, tramSpeed: C.tramSpeed, tramDwell: C.tramDwell,
    riverY: ly(C.riverY), homeOrigin: pt(C.homeOrigin[0], C.homeOrigin[1]),
    court: {x: lx(C.court.x), y: ly(C.court.y), radius: C.court.radius},
    homeRaidY: ly(C.homeRaidY), lightKw: C.lightKw,
  },
  terrain: {
    kind: 'terrain.kind.bin', variant: 'terrain.variant.bin', patch: 'terrain.patch.bin',
    // The authored collision mask, verbatim from G.urban.solid (ground.ts:628-630). The importer re-derives it from
    // the building/prop/substation rules and compares against this file, so the ported rule cannot drift silently.
    solid: 'terrain.solid.bin',
    names: TILE_NAMES,
  },
  buildings: buildingRecords,
  props: propRecords,
  roads,
  paths: C.paths.filter(polylineHits).map(localPath),
  drives: C.drives.filter(polylineHits).map(localPath),
  tram,
  sites,
  combat,
  validation,
};

mkdirSync(outDir, {recursive: true});
const write = (name: string, value: unknown) => writeFileSync(join(outDir, name), JSON.stringify(value, null, 2) + '\n');
writeFileSync(join(outDir, 'terrain.kind.bin'), kind);
writeFileSync(join(outDir, 'terrain.variant.bin'), variant);
writeFileSync(join(outDir, 'terrain.patch.bin'), patch);
writeFileSync(join(outDir, 'terrain.solid.bin'), Buffer.from(solidMask));
write('city.json', city);
write('export-report.json', {
  tool: 'packages/tools/src/exportUnity.ts',
  toolVersion: 1,
  generatedAt: new Date().toISOString(),
  region: city.region,
  rect: {x: RX, y: RY, w: RW, h: RH, padding: PAD, campSpawnRadius: CAMP_SPAWN_RADIUS},
  anchors: anchors.map(a => ({name: a.name, why: a.why, x: a.x, y: a.y, w: a.w, h: a.h})),
  mapId: C.id,
  sourceSha256,
  sourceFiles,
  manifest: city.manifest,
  regionValidation: validation,
  cityValidation,
  solidTiles: solidMask.reduce((a: number, v) => a + v, 0),
  // Everything the Unity importer is expected to reproduce, counted here so a mismatch is visible without opening
  // city.json (D-01b acceptance). These are counts of what was WRITTEN, not of the authored sources.
  counts: {
    bounds: {x: RX, y: RY, width: RW, height: RH, tiles: n},
    buildings: buildingRecords.length,
    enterableBuildings: buildingRecords.filter(b => b.enterable).length,
    fixedBuildings: buildingRecords.filter(b => b.isFixed).length,
    props: propRecords.length,
    roadNodes: roads.nodes.length,
    roadEdges: roads.edges.length,
    roadPolylines: roads.polylines.length,
    paths: city.paths.length,
    drives: city.drives.length,
    tramControlPoints: tram.control.points.length,
    tramBakedSamples: tram.baked.length,
    tramStops: tram.stops.length,
    // The shared rail centreline (riverfrontRail): the tram block above carries the same samples region-clipped,
    // plus `length`; `scalars.railHalf` carries the half-width. Unity draws the line from tram.baked alone.
    rail: {samples: rail.samples.length, tiles: rail.tiles.length, length: rail.length, halfWidth: C.railHalf},
    sites: {
      hasCore: sites.hasCore, substations: sites.substations.length, lights: sites.lights.length,
      plants: sites.plants.length, cores: sites.cores.length, artifacts: sites.artifacts.length,
      stops: sites.stops.length, projects: sites.projects.length, recruits: sites.recruits.length,
      resources: sites.resources.length, yards: sites.yards.length,
      serviceAreas: sites.serviceAreas.length, squares: sites.squares.length, labels: sites.labels.length,
    },
    combat: {
      firstCamps: combat.firstCamps.length,
      firstCampGroups: combat.firstCamps.reduce((a, c) => a + c.groups.length, 0),
      freightGates: combat.freightGates.length,
      hasFreightArena: combat.hasFreightArena,
      raidLine: combat.raidLine,
    },
  },
  note: 'city.json and the side-cars carry no timestamp; two runs are byte-identical.',
});

console.log(`Exported ${outDir}: region ${RW}x${RH} at ${RX},${RY}; ` +
  `${validation.buildings} buildings, ${validation.props} props, ${validation.accessible}/${validation.land} accessible land tiles.`);
console.log(`  roads ${roads.nodes.length} nodes / ${roads.edges.length} edges / ${roads.polylines.length} polylines; ` +
  `${city.paths.length} paths, ${city.drives.length} drives; ` +
  `tram ${tram.control.points.length} control, ${tram.baked.length} baked samples, ${tram.stops.length} stops, ` +
  `rail length ${rail.length.toFixed(3)}.`);
console.log(`  sites: ${sites.substations.length} substations, ${sites.lights.length} lights, ${sites.resources.length} resources, ` +
  `${sites.yards.length} yards, ${sites.serviceAreas.length} service areas, ${sites.squares.length} squares, ` +
  `${sites.plants.length} plants, ${sites.cores.length} cores, ${sites.artifacts.length} artifacts, ` +
  `${sites.projects.length} projects, ${sites.recruits.length} recruits, ${sites.labels.length} labels.`);
console.log(`  combat: ${combat.firstCamps.length} camps (${combat.firstCamps.reduce((a, c) => a + c.groups.length, 0)} groups), ` +
  `${combat.freightGates.length} freight gates, arena ${combat.hasFreightArena ? 'inside' : 'outside'} the region.`);
