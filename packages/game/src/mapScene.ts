/** The map view: a 24×24 grid of city blocks drawn from sim state every frame. Flat colour only. The sim is stepped
 *  by main.ts once per game step; this scene draws, consumes events for its pulses, and hands E the hovered block. */
import Phaser from 'phaser';
import {
  SimState, SimEvent, DARK, CONTESTED, HELD, INERT, VOID, idxOf, inBounds, isSolid, isCandidate, rotOf, rotTier,
  frontList, FrontEdgeView, claimInfo, heldInfo, HeldInfo, nearestHeld, facilityList, survivorList, isInterior, poolMax,
} from '@relight/sim';
import { Session, queue } from './session';

export const CELL = 24;
export const PAD = 36;
export const MAP_W = PAD * 2 + 24 * CELL;
export const MAP_H = PAD * 2 + 24 * CELL;

const C = {
  bg: 0x0b0e1a, grid: 0x0e1326,
  dark: 0x16204a, wellZone: 0x121a40, wellCell: 0x1e1250, wellGlow: 0x5a3aa8,
  mottle: [0x000000, 0x2b3a75, 0x4b5fa8, 0x8b9ad8],
  contested: 0xd99a2b, held: 0xe8a93a, interior: 0xf5c24f, white: 0xffffff,
  inert: 0x3f4553, inertLine: 0x2a2f3b, river: 0x2f3a4f, void: 0x1a1e2a,
  edge: 0xe0442a, green: 0x3ddc84, amber: 0xf2b632, red: 0xff3b30,
  pole: 0xb7c0d3, crawler: 0xff6a3d, shade: 0xb06cff, hulk: 0xffffff,
  facility: 0x9aa5b8, facilityHeld: 0xf5c24f, hover: 0xffffff, bad: 0xff3b30, survivor: 0x7fd0ff,
  machine: 0x1a1e2a, rubble: { civ: 0xc9d1de, res: 0xd2691e, ind: 0x4f6fa8, out: 0x000000 } as Record<string, number>,
};

interface Pulse { x: number; y: number; born: number; dur: number; r0: number; r1: number; color: number; width: number; wake: boolean }
interface HulkMark { x: number; y: number; born: number }
interface PoleLine { x0: number; y0: number; x1: number; y1: number }

export interface SceneHooks {
  onHover(info: ReturnType<typeof claimInfo> | HeldInfo | null, px: number, py: number): void;
  onPipSelect(edge: FrontEdgeView | null): void;
}

export class MapScene extends Phaser.Scene {
  private readonly session: Session;
  private readonly hooks: SceneHooks;
  private gCells!: Phaser.GameObjects.Graphics;
  private gEdges!: Phaser.GameObjects.Graphics;
  private gFx!: Phaser.GameObjects.Graphics;
  private labels: Phaser.GameObjects.Text[] = [];
  private axisLabels: Phaser.GameObjects.Text[] = [];
  private pulses: Pulse[] = [];
  private hulks: HulkMark[] = [];
  private poles: PoleLine[] = [];
  private hover: { x: number; y: number } | null = null;
  private selectedEdge: number | null = null;
  private edgesCache: FrontEdgeView[] = [];
  private edgesCacheT = -1;
  private facilityLabelsFor = '';
  private survivorLabels: Phaser.GameObjects.Text[] = [];
  private survivorLabelsFor = '';
  private focusMark: { x: number; y: number; born: number } | null = null;

  // The session comes in through the constructor, not Phaser's init(data): a scene added asleep (?view=world) is never
  // initialised, yet the sim driver hands it every event, and the first one used to crash the render loop.
  constructor(session: Session, hooks: SceneHooks) { super('map'); this.session = session; this.hooks = hooks; }

  create(): void {
    this.cameras.main.setBackgroundColor(C.bg);
    this.gCells = this.add.graphics();
    this.gEdges = this.add.graphics();
    this.gFx = this.add.graphics();
    const st = this.session.state;
    // axis labels so testers can refer to blocks by coordinate
    for (let x = 0; x < st.w; x++) this.axisLabels.push(this.add.text(PAD + x * CELL + CELL / 2, PAD - 12, String(x), { fontSize: '9px', color: '#5c6a8a' }).setOrigin(0.5));
    for (let y = 0; y < st.h; y++) this.axisLabels.push(this.add.text(PAD - 12, PAD + y * CELL + CELL / 2, String(y), { fontSize: '9px', color: '#5c6a8a' }).setOrigin(0.5));
    this.axisLabels.push(this.add.text(PAD + (st.w * CELL) / 2, PAD + st.h * CELL + 8, 'river', { fontSize: '10px', color: '#5c6a8a' }).setOrigin(0.5, 0));

    this.input.on('pointermove', (p: Phaser.Input.Pointer) => this.onMove(p));
    this.input.on('pointerdown', (p: Phaser.Input.Pointer) => this.onDown(p));
    this.input.on('gameout', () => { this.hover = null; this.hooks.onHover(null, 0, 0); });
  }

  private cellAt(px: number, py: number): { x: number; y: number } | null {
    const x = Math.floor((px - PAD) / CELL), y = Math.floor((py - PAD) / CELL);
    return inBounds(this.session.state, x, y) ? { x, y } : null;
  }

  private edges(): FrontEdgeView[] {
    const st = this.session.state;
    if (this.edgesCacheT !== st.t || this.edgesCache.length !== st.ring.length) { this.edgesCache = frontList(st); this.edgesCacheT = st.t; }
    return this.edgesCache;
  }

  /** Pixel position of an edge's bar centre: the street between blocks a and b. */
  private edgePos(e: FrontEdgeView): { x: number; y: number; vertical: boolean } {
    const ax = PAD + e.from.x * CELL, ay = PAD + e.from.y * CELL;
    switch (e.dir) {
      case 0: return { x: ax + CELL, y: ay + CELL / 2, vertical: true };
      case 1: return { x: ax, y: ay + CELL / 2, vertical: true };
      case 2: return { x: ax + CELL / 2, y: ay + CELL, vertical: false };
      default: return { x: ax + CELL / 2, y: ay, vertical: false };
    }
  }

  private pipAt(px: number, py: number): FrontEdgeView | null {
    let best: FrontEdgeView | null = null, bd = 49;
    for (const e of this.edges()) {
      const p = this.edgePos(e);
      const d = (p.x - px) ** 2 + (p.y - py) ** 2;
      if (d < bd) { bd = d; best = e; }
    }
    return best;
  }

  private onMove(p: Phaser.Input.Pointer): void {
    const c = this.cellAt(p.x, p.y);
    this.hover = c;
    if (!c) { this.hooks.onHover(null, 0, 0); return; }
    const st = this.session.state;
    const b = st.blocks[idxOf(st, c.x, c.y)];
    if (b.state === DARK) {
      const rect = this.game.canvas.getBoundingClientRect();
      this.hooks.onHover(claimInfo(st, c.x, c.y), rect.left + p.x, rect.top + p.y);
    } else if (b.state === HELD) {
      const rect = this.game.canvas.getBoundingClientRect();
      this.hooks.onHover(heldInfo(st, c.x, c.y), rect.left + p.x, rect.top + p.y);
    }
    else this.hooks.onHover(null, 0, 0);
  }

  private onDown(p: Phaser.Input.Pointer): void {
    const pip = this.pipAt(p.x, p.y);
    if (pip) {
      this.selectedEdge = this.selectedEdge === pip.id ? null : pip.id;
      this.hooks.onPipSelect(this.selectedEdge === null ? null : pip);
      return;
    }
    const c = this.cellAt(p.x, p.y);
    if (!c) return;
    const st = this.session.state;
    const info = claimInfo(st, c.x, c.y);
    if (!info.ok) return;
    queue(this.session, { type: 'claim', x: c.x, y: c.y });
  }

  selectEdge(id: number | null): void { this.selectedEdge = id; }

  /** The block under the cursor, for E: the world camera opens on it. */
  hoverBlock(): [number, number] | null { return this.hover ? [this.hover.x, this.hover.y] : null; }

  /** Mark the block the world view was centred on when E brought the map back. */
  markFocus(b: [number, number], now: number): void { this.focusMark = { x: b[0], y: b[1], born: now }; }

  update(time: number): void { this.draw(time); }

  consume(events: SimEvent[], now: number): void {
    const st = this.session.state;
    for (const ev of events) {
      if (ev.type === 'bloom') {
        const r1 = 6 + ev.cr * 0.55;                       // 4 crawlers → 8 px, 40 → 28 px; wake blooms are up to 2× larger
        this.pulses.push({ x: ev.x, y: ev.y, born: now, dur: ev.wake ? 1500 : 900, r0: 3, r1, color: C.crawler, width: ev.wake ? 3 : 1.5, wake: ev.wake });
        if (ev.sh > 0) this.pulses.push({ x: ev.x, y: ev.y, born: now + 120, dur: ev.wake ? 1500 : 900, r0: 2, r1: r1 * 0.7, color: C.shade, width: ev.wake ? 2.5 : 1.5, wake: ev.wake });
        if (ev.hu > 0) this.hulks.push({ x: ev.x, y: ev.y, born: now });
      } else if (ev.type === 'claim') {
        const from = nearestHeld(st, ev.x, ev.y);
        if (from) this.poles.push({ x0: from.x, y0: from.y, x1: ev.x, y1: ev.y });
      } else if (ev.type === 'fall') {
        this.pulses.push({ x: ev.x, y: ev.y, born: now, dur: 1200, r0: 14, r1: 4, color: C.red, width: 3, wake: false });
        this.poles = this.poles.filter(l => !(l.x1 === ev.x && l.y1 === ev.y));
      }
    }
  }

  private draw(now: number): void {
    const st = this.session.state;
    const g = this.gCells;
    g.clear();
    const throb = 0.5 + 0.5 * Math.sin(now / 600);
    const flicker = (Math.sin(now / 45) > 0 ? 1 : 0.55);
    const blinkSlow = Math.floor(now / 400) % 2 === 0;
    const wellSet = new Set(st.wells.map(([x, y]) => x * st.h + y));
    for (let i = 0; i < st.blocks.length; i++) {
      const b = st.blocks[i];
      const px = PAD + b.x * CELL, py = PAD + b.y * CELL;
      switch (b.state) {
        case DARK: {
          const isWell = wellSet.has(i);
          g.fillStyle(isWell ? C.wellCell : b.well ? C.wellZone : C.dark, 1);
          g.fillRect(px + 1, py + 1, CELL - 2, CELL - 2);
          const d = rotOf(st, i), tier = rotTier(d);
          if (tier > 0) {
            g.fillStyle(C.mottle[tier], 0.9);
            const n = tier * 2 + 1;                        // 3, 5, 7 dots
            for (let k = 0; k < n; k++) {
              const h = ((b.x * 73 + b.y * 151 + k * 37) % 97) / 97, v = ((b.x * 29 + b.y * 61 + k * 53) % 89) / 89;
              g.fillRect(px + 3 + h * (CELL - 8), py + 3 + v * (CELL - 8), 2, 2);
            }
          }
          if (isWell) { g.lineStyle(1.5, C.wellGlow, 0.35 + 0.55 * throb); g.strokeRect(px + 3, py + 3, CELL - 6, CELL - 6); }
          if (b.awake && b.timer >= 0) {                  // bloom timer ring: fills as the timer counts down
            const T = st.config.bloomT / (0.5 + b.d);
            const frac = Math.max(0, Math.min(1, 1 - (b.timer - st.t) / T));
            g.lineStyle(1.5, C.mottle[3], 0.8);
            g.beginPath();
            g.arc(px + CELL / 2, py + CELL / 2, 6, -Math.PI / 2, -Math.PI / 2 + frac * Math.PI * 2, false);
            g.strokePath();
          }
          break;
        }
        case CONTESTED:
          g.fillStyle(C.contested, 0.45 + 0.55 * flicker);
          g.fillRect(px + 1, py + 1, CELL - 2, CELL - 2);
          break;
        case HELD: {
          const inner = isInterior(st, i);
          g.fillStyle(inner ? C.interior : C.held, 1);
          g.fillRect(px + 1, py + 1, CELL - 2, CELL - 2);
          if (inner) { g.lineStyle(1, C.white, 0.9); g.strokeRect(px + 2.5, py + 2.5, CELL - 5, CELL - 5); }
          // §12 finite rubble: a strip along the top in the district's rubble colour; it fades as the pool drains and is gone when dry
          const pm = poolMax(st, b.name);
          if (pm > 0 && b.pool > 0) { g.fillStyle(C.rubble[b.name], 0.2 + 0.8 * (b.pool / pm)); g.fillRect(px + 4, py + 4, CELL - 8, 3); }
          // §5 machine slot: a small square at the bottom right — outline = empty interior slot, filled = assembler
          // (the HQ's Mk1 included), red = a machine on a block that is no longer interior (a neighbour fell)
          const hq = b.x === st.start[0] && b.y === st.start[1];
          if (b.machine) {
            const risk = !inner && !hq;
            g.fillStyle(risk ? C.red : C.machine, 1); g.fillRect(px + CELL - 10, py + CELL - 10, 7, 7);
            if (risk && blinkSlow) { g.lineStyle(1.5, C.red, 1); g.strokeRect(px + CELL - 11.5, py + CELL - 11.5, 10, 10); }
          } else if (inner) { g.lineStyle(1.2, C.machine, 0.9); g.strokeRect(px + CELL - 9.5, py + CELL - 9.5, 6, 6); }
          if (!b.subOn) { g.fillStyle(C.red, 0.9); g.fillRect(px + CELL / 2 - 2, py + CELL / 2 - 2, 4, 4); }
          break;
        }
        case INERT:
          g.fillStyle(b.y === st.h - 1 ? C.river : C.inert, 1);
          g.fillRect(px + 1, py + 1, CELL - 2, CELL - 2);
          if (b.y !== st.h - 1) { g.lineStyle(1, C.inertLine, 1); g.lineBetween(px + 4, py + CELL - 4, px + CELL - 4, py + 4); }
          break;
        case VOID:
          g.fillStyle(C.void, 1); g.fillRect(px + 1, py + 1, CELL - 2, CELL - 2);
          break;
      }
    }
    // start marker
    const [sx, sy] = st.start;
    g.lineStyle(1, 0x000000, 0.5); g.strokeRect(PAD + sx * CELL + 6, PAD + sy * CELL + 6, CELL - 12, CELL - 12);

    // facilities: §8 skyline, silhouettes within SKYLINE_RANGE blocks of a Held block
    const facs = facilityList(st).filter(f => f.visible);
    const key = facs.map(f => `${f.name}${f.held ? 1 : 0}`).join('|');
    for (const f of facs) this.drawFacility(g, f);
    if (key !== this.facilityLabelsFor) {
      this.facilityLabelsFor = key;
      for (const l of this.labels) l.destroy();
      this.labels = facs.map(f => this.add.text(PAD + f.x * CELL + CELL / 2, PAD + f.y * CELL + CELL + 1, f.name,
        { fontSize: '9px', color: f.held ? '#f5c24f' : '#c7cfe0', backgroundColor: '#0b0e1acc', padding: { x: 2, y: 1 } }).setOrigin(0.5, 0).setDepth(5));
    }

    // survivors: §8, a block's contents show once a 4-neighbour is Held; the §18 letter in a badge, name below
    const survs = survivorList(st).filter(f => f.revealed);
    const skey = survs.map(f => `${f.tag}${f.held ? 1 : 0}`).join('|');
    for (const f of survs) {
      const px = PAD + f.x * CELL, py = PAD + f.y * CELL, col = f.held ? C.facilityHeld : C.survivor;
      g.fillStyle(col, 1); g.fillCircle(px + 8, py + 8, 3);                       // head
      g.fillRoundedRect(px + 4, py + 11, 8, 8, 2);                               // body
      g.fillStyle(0x000000, 0.55); g.fillRoundedRect(px + 13, py + 3, 9, 10, 2);  // letter badge ground
    }
    if (skey !== this.survivorLabelsFor) {
      this.survivorLabelsFor = skey;
      for (const l of this.survivorLabels) l.destroy();
      this.survivorLabels = survs.flatMap(f => [
        this.add.text(PAD + f.x * CELL + 17.5, PAD + f.y * CELL + 8, f.tag, { fontSize: '9px', fontStyle: 'bold', color: f.held ? '#f5c24f' : '#7fd0ff' }).setOrigin(0.5).setDepth(5),
        this.add.text(PAD + f.x * CELL + CELL / 2, PAD + f.y * CELL + CELL + 1, f.name,
          { fontSize: '9px', color: f.held ? '#f5c24f' : '#7fd0ff', backgroundColor: '#0b0e1acc', padding: { x: 2, y: 1 } }).setOrigin(0.5, 0).setDepth(5),
      ]);
    }

    // hover
    if (this.hover) {
      const b = st.blocks[idxOf(st, this.hover.x, this.hover.y)];
      const px = PAD + this.hover.x * CELL, py = PAD + this.hover.y * CELL;
      if (b.state === DARK) {
        const ok = isCandidate(st, idxOf(st, this.hover.x, this.hover.y));
        g.lineStyle(2, ok ? C.hover : C.bad, ok ? 0.95 : 0.6);
        g.strokeRect(px + 1, py + 1, CELL - 2, CELL - 2);
      }
    }

    // the block the world view was looking at, for 2.5 s after E: corner brackets that fade
    if (this.focusMark) {
      const k = (now - this.focusMark.born) / 2500;
      if (k >= 1) this.focusMark = null;
      else {
        const px = PAD + this.focusMark.x * CELL, py = PAD + this.focusMark.y * CELL, L = 7;
        g.lineStyle(2, C.white, 1 - k);
        g.lineBetween(px, py, px + L, py); g.lineBetween(px, py, px, py + L);
        g.lineBetween(px + CELL, py, px + CELL - L, py); g.lineBetween(px + CELL, py, px + CELL, py + L);
        g.lineBetween(px, py + CELL, px + L, py + CELL); g.lineBetween(px, py + CELL, px, py + CELL - L);
        g.lineBetween(px + CELL, py + CELL, px + CELL - L, py + CELL); g.lineBetween(px + CELL, py + CELL, px + CELL, py + CELL - L);
      }
    }

    // frontage edges + pips
    const ge = this.gEdges;
    ge.clear();
    const blink = Math.floor(now / 260) % 2 === 0;
    const edges = this.edges();
    for (const e of edges) {
      const p = this.edgePos(e);
      ge.fillStyle(C.edge, 0.95);
      if (p.vertical) ge.fillRect(p.x - 2, p.y - CELL / 2 + 2, 4, CELL - 4); else ge.fillRect(p.x - CELL / 2 + 2, p.y - 2, CELL - 4, 4);
      // shape-coded pips (colour-blind safe): ● green disc, ▲ amber triangle, ✕ red cross that blinks
      if (e.pip === 'green') {
        ge.fillStyle(C.green, 1); ge.fillCircle(p.x, p.y, 3.5);
        ge.lineStyle(1, 0x000000, 0.6); ge.strokeCircle(p.x, p.y, 3.5);
      } else if (e.pip === 'amber') {
        ge.fillStyle(C.amber, 1); ge.fillTriangle(p.x, p.y - 4.5, p.x + 4.5, p.y + 3.5, p.x - 4.5, p.y + 3.5);
        ge.lineStyle(1, 0x000000, 0.6); ge.strokeTriangle(p.x, p.y - 4.5, p.x + 4.5, p.y + 3.5, p.x - 4.5, p.y + 3.5);
      } else if (blink) {
        ge.lineStyle(2.5, C.red, 1); ge.lineBetween(p.x - 4, p.y - 4, p.x + 4, p.y + 4); ge.lineBetween(p.x - 4, p.y + 4, p.x + 4, p.y - 4);
      }
      if (this.selectedEdge === e.id) {
        ge.lineStyle(2, C.white, 1); ge.strokeCircle(p.x, p.y, 6.5);
        ge.lineStyle(2, C.white, 0.8);
        ge.strokeRect(PAD + e.to.x * CELL + 1, PAD + e.to.y * CELL + 1, CELL - 2, CELL - 2);
      }
    }
    if (this.selectedEdge !== null && !edges.some(e => e.id === this.selectedEdge)) { this.selectedEdge = null; this.hooks.onPipSelect(null); }

    // fx: pole lines, pulses, hulk marks
    const gf = this.gFx;
    gf.clear();
    for (const l of this.poles) {
      const x0 = PAD + l.x0 * CELL + CELL / 2, y0 = PAD + l.y0 * CELL + CELL / 2, x1 = PAD + l.x1 * CELL + CELL / 2, y1 = PAD + l.y1 * CELL + CELL / 2;
      gf.lineStyle(1, C.pole, 0.55); gf.lineBetween(x0, y0, x1, y1);
      gf.fillStyle(C.pole, 0.8); gf.fillCircle(x0, y0, 1.5); gf.fillCircle(x1, y1, 1.5);
    }
    this.pulses = this.pulses.filter(p => now - p.born < p.dur);
    for (const p of this.pulses) {
      const k = Math.max(0, (now - p.born) / p.dur);
      const r = p.r0 + (p.r1 - p.r0) * k;
      gf.lineStyle(p.width, p.color, 1 - k);
      gf.strokeCircle(PAD + p.x * CELL + CELL / 2, PAD + p.y * CELL + CELL / 2, r);
      if (p.wake) { gf.lineStyle(1, p.color, (1 - k) * 0.5); gf.strokeCircle(PAD + p.x * CELL + CELL / 2, PAD + p.y * CELL + CELL / 2, r * 1.25); }
    }
    this.hulks = this.hulks.filter(h => now - h.born < 1600);
    for (const h of this.hulks) {
      const cx = PAD + h.x * CELL + CELL / 2, cy = PAD + h.y * CELL + CELL / 2, a = 1 - (now - h.born) / 1600;
      gf.fillStyle(C.hulk, a);
      gf.fillPoints([{ x: cx, y: cy - 6 }, { x: cx + 6, y: cy }, { x: cx, y: cy + 6 }, { x: cx - 6, y: cy }], true);
    }
  }

  private drawFacility(g: Phaser.GameObjects.Graphics, f: { name: string; x: number; y: number; held: boolean }): void {
    const px = PAD + f.x * CELL, py = PAD + f.y * CELL, col = f.held ? C.facilityHeld : C.facility;
    g.fillStyle(col, f.held ? 1 : 0.9);
    switch (f.name) {
      case 'Foundry':       // low hall with a chimney
        g.fillRect(px + 5, py + 12, 14, 7); g.fillRect(px + 15, py + 4, 3, 8); break;
      case 'Arsenal':       // a stack of crates
        g.fillRect(px + 5, py + 13, 6, 6); g.fillRect(px + 12, py + 13, 6, 6); g.fillRect(px + 8.5, py + 6, 6, 6); break;
      case 'Turbine hall':  // long shed with a curved roof
        g.fillRect(px + 4, py + 12, 16, 7); g.fillCircle(px + 12, py + 12, 6); break;
      case 'Refinery':      // two tanks
        g.fillCircle(px + 8, py + 13, 4.5); g.fillCircle(px + 16, py + 13, 4.5); g.fillRect(px + 11, py + 6, 2, 8); break;
      case 'Power station': // block with two stacks
        g.fillRect(px + 5, py + 11, 14, 8); g.fillRect(px + 7, py + 4, 3, 8); g.fillRect(px + 14, py + 4, 3, 8); break;
      default:
        g.fillRect(px + 7, py + 7, 10, 10);
    }
  }
}
