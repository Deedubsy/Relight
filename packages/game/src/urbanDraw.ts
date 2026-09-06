/** RI-02A code-native visual kit. No external assets; all footprints come from Ground. */
import Phaser from 'phaser';
import { Ground, SimState, TILE_PX as P, HELD, CONTESTED, depotRect } from '@relight/sim';

export function drawUrban(g: Phaser.GameObjects.Graphics, st: SimState, G: Ground, visible: number[], zoom: number): void {
  g.clear();
  const city = G.urban;
  if (!city) return;
  const vis = new Set(visible);
  if (G.opening && st.campaign && vis.has(st.campaign.homeBlock)) {
    for (const t of G.opening.walls) {
      const x = (t % G.tw) * P, y = Math.floor(t / G.tw) * P;
      g.fillStyle(0x18262b, 1); g.fillRect(x + 4, y + 6, P, P);
      g.fillStyle(0x76817a, 1); g.fillRect(x, y, P, P);
      g.lineStyle(1, 0xb1b8a5, 0.9); g.strokeRect(x + 2, y + 2, P - 4, P - 4);
    }
    for (const t of G.opening.gate) {
      g.fillStyle(0xc49a59, 0.65); g.fillRect((t % G.tw) * P, Math.floor(t / G.tw) * P, P, P);
    }
  }
  const ex = st.campaign?.expansion;
  if (ex) {
    const path = ex.route;
    // Survey paint identifies the supplied route; it is not laid track or free transport.
    if (ex.station.restoredAt >= 0) for (const t of path) { const bi = G.near[t]; if (!vis.has(bi)) continue; const x = (t % G.tw) * P, y = Math.floor(t / G.tw) * P; g.lineStyle(1 / zoom, 0x8fcad1, 0.6); g.strokeRect(x + P * 0.3, y + P * 0.3, P * 0.4, P * 0.4); }
    if (vis.has(ex.station.block)) {
      const s = ex.station; g.lineStyle(3 / zoom, s.restoredAt >= 0 ? 0x88d5b0 : 0xf1c36b, 0.95); g.strokeRect(s.x * P, s.y * P, s.size * P, s.size * P);
      const r = ex.radio, x = (r.x + 0.5) * P, y = (r.y + 0.5) * P;
      g.fillStyle(0x243640, 1); g.fillRect(r.x * P, r.y * P, P, P);
      g.lineStyle(2 / zoom, r.restoredAt >= 0 ? 0x88d5b0 : 0xc9a773, 1); g.lineBetween(x, y - P * 0.45, x - P * 0.35, y + P * 0.35); g.lineBetween(x, y - P * 0.45, x + P * 0.35, y + P * 0.35); g.lineBetween(x - P * 0.3, y - P * 0.1, x + P * 0.3, y - P * 0.1);
    }
    if (ex.station.restoredAt >= 0) for (const [x,y] of ex.stops) { if (!vis.has(G.near[y * G.tw + x])) continue; g.lineStyle(2 / zoom, 0x8fcad1, 0.8); g.strokeRect(x * P, y * P, P * 2, P * 2); }
  }
  for (const place of city.places) {
    if (!vis.has(place.block)) continue;
    const { x, y, w, h } = place.pad, held = st.blocks[place.block].state === HELD;
    // Painted pad corners, never a wall or a new placement restriction.
    g.lineStyle(1.3 / zoom, held ? 0xd0b078 : 0x637676, held ? 0.48 : 0.28);
    for (const [cx, cy, dx, dy] of [[x, y, 1, 1], [x + w, y, -1, 1], [x, y + h, 1, -1], [x + w, y + h, -1, -1]]) {
      g.lineBetween(cx * P, cy * P, (cx + dx * 1.5) * P, cy * P);
      g.lineBetween(cx * P, cy * P, cx * P, (cy + dy * 1.5) * P);
    }
    if (place.role === 'rail') {
      // Disused rail bed on the loading reservation. Flush rails are walkable;
      // only player-laid Track belongs to the transport network.
      const p = city.railReserve.loading;
      for (const off of [2, 5]) {
        for (let xx = p.x; xx < p.x + p.w; xx++) {
          if (G.rank[(p.y + off) * G.tw + xx] >= 0) continue;
          const sub = G.blocks[place.block].sub;
          if (sub && xx >= sub.x && xx < sub.x + sub.size && p.y + off >= sub.y && p.y + off < sub.y + sub.size) continue;
          if (st.flow?.occ[(p.y + off) * G.tw + xx] !== undefined) continue;
          g.lineStyle(2, 0x454d50, 0.7); g.lineBetween((xx + 0.25) * P, (p.y + off - 0.45) * P, (xx + 0.25) * P, (p.y + off + 0.45) * P);
          g.lineStyle(2, held ? 0xa9936b : 0x758182, 0.8);
          for (const yy of [-0.25, 0.25]) g.lineBetween(xx * P, (p.y + off + yy) * P, (xx + 0.85) * P, (p.y + off + yy) * P);
        }
      }
      const [ix, iy] = city.railReserve.installation;
      g.lineStyle(2, held ? 0xe9bd6b : 0x667b82, 0.8); g.strokeRect((ix - 0.3) * P, (iy - 0.3) * P, 3.6 * P, 3.6 * P);
    }
  }
  for (const s of city.structures) {
    if (!vis.has(s.block)) continue;
    const held = st.blocks[s.block].state === HELD, contested = st.blocks[s.block].state === CONTESTED;
    const x = s.x * P, y = s.y * P, w = s.w * P, h = s.h * P;
    const residential = s.role === 'residential', civic = s.role === 'civic';
    const roof = residential ? 0x384448 : civic ? 0x414c50 : 0x34434c;
    // A solid silhouette stays readable in ambient dark. Shadow stays within the solid footprint.
    g.fillStyle(0x101b22, 1); g.fillRect(x, y, w, h);
    g.fillStyle(roof, 1); g.fillRect(x + 4, y + 4, w - 8, h - 15);
    g.lineStyle(1.5 / zoom, held ? 0xd5b179 : 0x7a898b, 0.7); g.strokeRect(x + 2, y + 2, w - 4, h - 4);
    if (residential) {
      g.fillStyle(0x465358, 1); g.fillTriangle(x + 5, y + h / 2, x + w / 2, y + 5, x + w - 5, y + h / 2);
      g.lineStyle(3, 0x1d2b31, 0.9); g.lineBetween(x + 5, y + h / 2, x + w - 5, y + h / 2);
      g.fillStyle(0x16262b, 1); g.fillRect(x + w - 34, y + 16, 15, 20);
    } else if (civic) {
      g.fillStyle(0x5c6766, 1); g.fillTriangle(x + 8, y + h - 25, x + w / 2, y + h - 55, x + w - 8, y + h - 25);
      for (let xx = 15; xx < w - 10; xx += 28) { g.fillStyle(0x6e7773, 1); g.fillRect(x + xx, y + h - 24, 9, 18); }
    } else {
      for (let yy = 16; yy < h - 20; yy += 20) {
        g.lineStyle(2, 0x53636a, 0.65); g.lineBetween(x + 8, y + yy, x + w - 8, y + yy);
      }
      for (let xx = 24; xx < w - 25; xx += 65) { g.fillStyle(0x1e303b, 1); g.fillRect(x + xx, y + 18, 35, h * 0.32); g.lineStyle(1, 0x607881, 0.7); g.strokeRect(x + xx, y + 18, 35, h * 0.32); }
    }
    // Door drawn on the physical face. Its approach tile is outside the solid footprint.
    const doorX = (s.door[0] + 0.5) * P;
    g.fillStyle(0x101d24, 1); g.fillRect(doorX - 15, y + h - 15, 30, 15);
    g.lineStyle(3, held || contested ? 0xf1bb68 : 0x7d9298, 0.9); g.lineBetween(doorX - 17, y + h - 1, doorX + 17, y + h - 1);
    for (let xx = 15; xx < w - 15; xx += residential ? 35 : 50) {
      g.fillStyle(held ? 0xd1a36b : contested ? 0x9e824f : 0x536e77, held ? 0.85 : 0.6); g.fillRect(x + xx, y + h - 12, 12, 5);
    }
  }
  // The Depot uses its actual six-tile collision footprint, with the engineer/workbench south of it.
  const d = depotRect(st), hq = G.blocks.find(b => b.hq)!;
  if (vis.has(hq.i)) {
    const x = d.x * P, y = d.y * P, w = d.size * P;
    g.fillStyle(0x304854, 1); g.fillRect(x + 3, y + 3, w - 6, w - 6);
    g.fillStyle(0x45626a, 1); g.fillTriangle(x + 5, y + 5, x + w - 5, y + 5, x + w - 5, y + w / 2);
    g.lineStyle(2, 0x7b9598, 0.9); g.strokeRect(x + 3, y + 3, w - 6, w - 6);
    g.lineStyle(3, 0x203a44, 1); g.lineBetween(x + 8, y + w / 2, x + w - 8, y + w / 2);
    for (let xx = 15; xx < w - 20; xx += 28) { g.fillStyle(0xdcb577, 0.75); g.fillRect(x + xx, y + w - 32, 15, 9); }
    g.fillStyle(0x101f28, 1); g.fillRect(x + w / 2 - 22, y + w - 22, 44, 22);
    g.lineStyle(4, 0xf0c482, 1); g.lineBetween(x + w / 2 - 26, y + w - 2, x + w / 2 + 26, y + w - 2);
    // Workbench landing is open ground, clearly separated from the door.
    g.lineStyle(1, 0xd5b984, 0.6); g.strokeRect(x + w / 2 - 28, y + w + 3, 56, 24);
  }
}
