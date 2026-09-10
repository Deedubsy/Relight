import {drawCityRail,drawCityTram,cityTramVisualPose} from './riverfrontRailDraw';
import {paintRiverfrontLight} from './riverfrontLighting';
import {drawRiverfront} from './riverfrontDraw';
import {cityVisible,RIVERFRONT_BUILDINGS} from '@relight/sim';
import {CORRECTIONS,hopperCapacity} from '@relight/sim';
import {machinePorts,connectionText} from './machineConnections';
import {quickbarView,DEFAULT_QUICKBAR} from './uiPreferences';
import {itemName,knownEquipment, pocketSlots, stackSize, HAND_MINE_PER_S} from '@relight/sim';
import {campaignInteraction} from '@relight/sim';
import { uiInput, uiPalette } from './uiShell';
import { removalPreview, type RemovalPreview, blueprintGate, blueprintCheck, blueprintQueueCheck, blueprintEntityBuilt, type BlueprintTransform } from '@relight/sim';
import { truckRect, truckOccupies, truckDescription } from '@relight/sim';
/** World view (Phase 4 M1 + M2, prompt B M1): the city at tile level, on foot. Every tile is drawn from the ground
 *  layer in @relight/sim (ground.ts: block faces rasterised to tiles, streets on the ridges, the river), cached per
 *  32×32 chunk on `chunkKey`; the machines are drawn from `state.flow` (flow.ts). This scene holds only camera,
 *  tool and input state. On foot (the flow layer is up): the camera follows the engineer, the wheel zooms 0.5–3×
 *  about the engineer, M goes back to the map at the engineer's block; hand actions (mine, feed, place, remove)
 *  reach 8 tiles — outside that the cursor says "walk closer". Without the flow layer (?flow=0, the block-only bot
 *  comparisons) the old camera stays: drag or WASD pan, M at the block under the centre.
 *  D-B1-5 direct control: WASD moves the engineer (Shift sprints, Space dodges); there is no click-to-walk here —
 *  the map is for inspection and pins. Left-click does what the hand holds: empty
 *  hand → hold on rubble to mine it, click a turret or Generator to feed it; a building in hand → place; the rifle
 *  in hand → hold to fire toward the cursor. Right-click with an empty hand removes with a full refund, with
 *  something in hand clears it. E interacts (the Depot chest, the workbench, a machine, the truck, survivors) and
 *  never mines, places or fires. The hotbar is 1–8 for the buildings and 9 the rifle; B is the build menu, R
 *  rotates, Q pipettes the machine under the cursor (or clears the hand), Esc closes anything.
 *  Prompt B M5 Light: a one-texel-per-tile light map (`lightMask`, the shade rule's own lit set) on a canvas
 *  texture the city's size, multiplied over the ground and the machines; lit tiles full colour, unlit ones darkened
 *  to ~25 % with a cool cast. Rot (the navy tint and the specks) never sits on a lit tile; on a Contested block the
 *  streetlights come on in sequence from the substation out and the specks burn off outward from every lit lamp over
 *  the block's 20 + 60·d s. E on a broken or eaten light repairs it from the pockets. */
import Phaser from 'phaser';
import {
  SimState, idxOf, DARK, CONTESTED, HELD, INERT, VOID, isInterior,
  TILE_PX, RUBBLE_VARIANTS, T_STREET, T_GROUND, T_RUBBLE, T_INERT, T_RIVER, T_DEPOSIT, T_PATCH,
  ground, chunkTiles, chunkKey, CHUNK, describeGround, blockOfTile, inReach, depotRect, REACH, INV_STACKS, invStacks, currentPath,
  Kind, Dir, DX, DY, DIR_NAMES, Machine, MACHINE_SIZE, SHOT, EXCAVATOR_PER_S,
  machineAt, powered, rubbleAt, canPlace, canPickUp, costStr, entryDir, describeMachine, outputTile, inputTile,
  blockLights, workbenchTile, substationAt, isSubstationTile, poleGrid, flowSummary, TURRET_HOPPER, TURRET_FLASH_S,
  POLE_REACH, ROUNDS_PER_MAG, RIFLE_RANGE, DODGE_COST, segBetween, cityGeomOf, CityGeom, pipOf, edgeCap,
  lockReason, KIND_LABEL, FLOODLIGHT_RANGE, FLOODLIGHT_HALF_ANGLE, BIG_POLE_REACH,
  RECIPE_IDS, recipeOf, recipeOutput,
  threatActive, crawlerAt, describeCrawler, litAt, ENEMIES, ENGINEER_HP,
  TURRET_RANGE, machineById, knownCampaignThreat, crawlerTarget, shadeTraces, shadeNear, TRACE_S, activeEmergencePoints, stalkersOf, stalkerAt, describeStalker, stalkerHp,
  lightMask, lightAt, canRepair, contestProgress, REPAIR_COPPER,
  heartOf, heartAt, cabinetAt, describeCabinet, describeHeart, cabinetConnected, cabinetRepairCheck, emergencePoint,   // RI-06
  blockLabel, machineStatus, MachineState, bearingOf,
  activationCheck, claimNeed, deliveredTo,
} from '@relight/sim';
import { Session, queue, dispatch } from './session';
import { View, debugView, hudInset, inspectionView } from './view';
import { coreAt, defenceMax, defenceHp, defenceDescription, repairCheck } from '@relight/sim';
import {navigationName,knownDistrict,blockName,campaignDiscoveries} from '@relight/sim';
import { knownSite, turbineAt, describeTurbine, ARC_LAMP_RADIUS, campaignSiteAt, campaignSite, describeSite, SITE_IDS, persistentSource, recruitAt, recruitCheck, recruitDescription, RECRUITS, discoveryAt, discoveryCheck, discoveryDescription, stalkerLayer } from '@relight/sim';
import { extendBuildPath, pathEdits, constructionCheck, type TilePoint } from '@relight/sim';
import { bound, movementKeys } from './controls';
import { toolForKey, toolKeyLine } from './buildCatalogue';
import { dimensions, machineDimensions, undergroundCheck, undergroundMate, splitterPorts, routingDescription, ITEMS, type BuildEdit } from '@relight/sim';
import { FACTORY_TEXT, FACTORY_STATUS_GLYPH } from './factoryStrings';
import { drawUrban } from './urbanDraw';

/** RI-02 (§11.2 "machine purpose and working / starved / blocked state", never colour alone): the state word's
 *  glyph, in the tooltip and the E toast, and drawn as the same shape on the machine (drawStatusMark). */
const STATUS_GLYPH = FACTORY_STATUS_GLYPH;
const STATUS_COL: Record<MachineState, number> = { running: 0x6fe08a, starved: 0xe8a93a, blocked: 0xe05a5a, idle: 0x9aa5b8, off: 0x9aa5b8 };
/** Routine conveyor marks appear on hover; stopped devices expose their mark in place. */
const STATUS_KIND = new Set<Kind>(['foundry','refinery','assembler2','pumpjack','cannon','arclamp','mixer','turret', 'generator', 'excavator', 'assembler', 'floodlight','inserter','underground','splitter','lamp','pole','bigpole','substation','tramstop']);

/** GAME-ASSUMPTION: the constitution's 0.5–3× zoom, not §4's 1.0–0.2×; the doc is edited to this range (D-P4-1). */
export const ZOOM_MIN = 0.5, ZOOM_MAX = 3, ZOOM_STEP = 1.15;
const PAN_PX_PER_S = 900;
/** Layout pass, drawing only: the share of the viewport's height the HQ lot fills at the opening zoom. */
const HQ_SCREEN_FRAC = 1 / 3;
/** Layout pass, drawing only: how much of the viewport's width the top-left key strip may wrap across before it
 *  would run under the territory block in the opposite corner. */
const KEY_STRIP_FRAC = 0.56;
/** The sim clock for the bottom-right corner: m:ss inside the hour, h:mm:ss past it. */
const clock = (t: number): string => {
  const s = Math.max(0, Math.floor(t)), h = Math.floor(s / 3600), m = Math.floor(s / 60) % 60, sec = s % 60;
  return h > 0 ? `${h}:${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}` : `${m}:${String(sec).padStart(2, '0')}`;
};
const CHUNK_PX = CHUNK * TILE_PX;   // 1024 px
const HALF = TILE_PX / 2;
/** Camera follow: the fraction of the gap to the engineer closed per second (the sim moves them 20× a second). */
const FOLLOW_PER_S = 14;

// tileset frames: street, ground, inert, river, then rubble (stone/copper/steel/coal × 5 variants — coal is the rail
// yard's heap, RI-01), then deposits (iron/coal × 5), then the HQ patches (steel, copper, coal)
const F_STREET = 0, F_GROUND = 1, F_INERT = 2, F_RIVER = 3, F_RUBBLE = 4, F_DEPOSIT = F_RUBBLE + 4 * RUBBLE_VARIANTS, F_PATCH = F_DEPOSIT + 2 * RUBBLE_VARIANTS;
const F_PAVE = F_PATCH + 3, F_QUAY = F_PAVE + 1, F_DASH_H = F_QUAY + 1, F_DASH_V = F_DASH_H + 1, F_COUNT = F_DASH_V + 1;
const RUBBLE_IDX: Record<string, number> = { stone: 0, copper: 1, steel: 2, coal: 3 };
const DEPOSIT_IDX: Record<string, number> = { iron: 0, coal: 1 };
const RUBBLE_COL = ['#89948f', '#aa734c', '#81979e', '#333c3e'];
const RUBBLE_DARK = ['#616d6b', '#705540', '#4e676e', '#20282a'];
const DEPOSIT_COL = ['#9a4c3a', '#1a1b20'];
/** GAME-ASSUMPTION: flat item colours (steel blue, copper orange, stone grey, coal black, magazine brass; RI-01's wire
 *  a paler copper, frame a paler steel, board green) until the art pass. */
const ITEM_COL: Record<string, number> = {ironore:0xa27d6a,copperore:0xb5784b,crude:0x463d59,fuel:0xdaba4f,polymer:0xa9b5ac,shell:0xc88948,core1:0x71e3c1,core2:0x71e3c1,core3:0x71e3c1,artifact1:0xc39ae8,artifact2:0xc39ae8,artifact3:0xc39ae8, steel: 0x7fa0d8, copper: 0xd9743a, stone: 0xc7ccd6, coal: 0x202126, magazine: 0xe6d45a, wire: 0xf0a86a, frame: 0xb4c6e8, concrete: 0xb8b4a2, board: 0x5fae6a };
const MACHINE_COL: Record<Kind, number> = {foundry:0xd5834d,refinery:0x869b58,pumpjack:0x987440,assembler2:0xbba547,fastbelt:0xc9a94a,cannon:0x858fa5, excavator: 0x4d5a6a, belt: 0x2a2d36, inserter: 0x5a4a2a, assembler: 0x5a4a6a, depot: 0x0b0e1a,
  arclamp: 0x9db4ca, turret: 0x3d4452, lamp: 0x6b6f7a, pole: 0x6e5a3a, generator: 0x5a2e2e, floodlight: 0x5c6270, bigpole: 0x7a6440, substation: 0x2b2f3a,
  underground:0x4a5945, splitter:0x364759, mixer:0x797c64, barricade:0xc1bcae, wall: 0x858b91, chest: 0x4a4636, track: 0x3a3c44, tramstop: 0x3f4a5e, tram: 0xb0572a };   // RI-05: the supply chest and the rail kit (flat colours until the art pass)
const LIGHT_COL = 0xffe9a0;
/** M5 light map. GAME-ASSUMPTION: the unlit texel is a multiply of ~25 % with a cool cast (§4's "desaturated and
 *  darkened to ~25 %" — a multiply cannot desaturate, so the cast stands in for it until the Phase 12 art pass);
 *  the map is re-read from the sim eight times a second (the §6 sequence is three lights a second) and re-uploaded
 *  only when a texel changed; a Contested lot's specks burn off within `SWEEP_R` tiles of every lit lamp as the
 *  burn-off runs, so the far corners clear last. */
const UNLIT_RGB = [62, 66, 98], LIGHT_REFRESH_MS = 125, SWEEP_R = 16;
/** Layout pass, drawing only: how far towards lit an unlit tile is carried by the blurred light map. Below 1 so the
 *  falloff softens the edge without lighting the dark: at 0.7 the first unlit ring reads about a third lit. */
const LIGHT_SOFT = 0.7;
/** Layout pass, drawing only: the machines whose footprint is a box, so they take the common drop shadow and rim.
 *  Belts, inserters and the posts are not boxes — a rim per tile would draw a ladder down a belt run. */
const BOX_MACHINE = new Set(['foundry','refinery','pumpjack','assembler2','cannon','turret', 'floodlight', 'generator', 'excavator', 'assembler', 'mixer', 'bigpole', 'chest', 'tramstop']);   // RI-05: the chest and the stop are boxes too
/** Presentation defaults for the owner-requested mouse-aimed flashlight. */
const FLASHLIGHT_RANGE = 12;
const FLASHLIGHT_HALF_ANGLE = Math.PI / 6;

export type Tool = 'hand' | 'rifle' | Exclude<Kind, 'depot'>;
/** D-B1-5: the hotbar. GAME-ASSUMPTION: 1 belt, 2 inserter, 3 Excavator, 4 Assembler, 5 turret, 6 lamp, 7 pole,
 *  8 Generator, 9 the rifle — the rifle is a hotbar item like any building, and while it is in hand you cannot mine
 *  or place until you clear it (Q, or pick something else). Prompt B M3: the Electricians' unlocks sit after the
 *  digits — 0 Floodlight, [ Big pole, ] Substation — and answer with why they are locked until the group joins. */
export const TOOL_KEY_LINE = toolKeyLine;


export interface WorldHooks {
  onHoverText(text: string | null, px: number, py: number): void;
  onToast(msg: string, kind?: 'info' | 'bad' | 'good'): void;
  /** E on the Depot: open the chest (the pockets panel). */
  onChest(): void;
  /** RI-05: E on a Supply chest or a Tram stop within reach — the pockets, targeted at it (chestTake / chestPut at). */
  onChestAt(x: number, y: number): void;
  onRoutingAt(x:number,y:number):void;
  onInspectAt(x:number,y:number):void;
  onTruck():void;
}

interface Chunk { key: string; frames: Uint8Array }

export class WorldScene extends Phaser.Scene {
  private blitter!: Phaser.GameObjects.Blitter;
  private bobs: Phaser.GameObjects.Bob[] = [];
  private gOver!: Phaser.GameObjects.Graphics;
  private gMach!: Phaser.GameObjects.Graphics;
  private gPorts!: Phaser.GameObjects.Graphics;
  private gThreat!: Phaser.GameObjects.Graphics;
  private gEng!: Phaser.GameObjects.Graphics;
  private gRoof!: Phaser.GameObjects.Graphics;
  private gUrban!: Phaser.GameObjects.Graphics;
  private lightTex!: Phaser.Textures.CanvasTexture;
  private viewLightTex!: Phaser.Textures.CanvasTexture;
  private viewLightImage!: Phaser.GameObjects.Image;
  private flashlightAngle = 0;
  private lightPix!: Uint8Array;        // the lit mask the texture shows (1 lit)
  private lightScratch!: Uint8Array;
  /** Layout pass, drawing only: the blurred copy of the lit mask the texture is painted from. `lightPix` stays the
   *  sim's binary lit set — this is only how it is coloured. */
  private lightSoft!: Float32Array;
  private lightBlur!: Float32Array;
  private lightAtMs = -Infinity;
  /** Visibility beam only; the simulation light mask retains defence-light authority. */
  handLamp = false;
  private depotText!: Phaser.GameObjects.Text;
  private depotLabel = '';
  /** Layout pass (ROADMAP §2 "Layout and readability pass"): the HUD is four corner overlays rather than one block
   *  in the top-left — territory top-right, power and pockets bottom-left, speed and clock bottom-right, toasts
   *  bottom-centre (the toasts are the panel's, `style.css`). GAME-ASSUMPTION: the ROADMAP names those four corners
   *  and the key strip (STANDARDS 4.3) is not one of them, so it takes the corner left over — top-left, with the
   *  in-hand line, the two things that answer "what am I holding and what can I press". The Depot beacon (C.2)
   *  rides the viewport edge nearest the Depot while it is out of view. */
  private terrText!: Phaser.GameObjects.Text;
  private powText!: Phaser.GameObjects.Text;
  private clockText!: Phaser.GameObjects.Text;
  private keyText!: Phaser.GameObjects.Text;
  private beaconText!: Phaser.GameObjects.Text;
  private keyWrap = 0;
  private powWrap = 0;
  /** The engineer's facing as last drawn, so an idle engineer keeps pointing where they last moved (`e.face` is the
   *  sim's own facing vector, set by walk.ts; this only survives the frames where it reads [0,0]). */
  private lastFace: [number, number] = [1, 0];
  /** Set the moment the human turns the wheel: after that the opening `fitZoom` never overrides their choice. */
  private zoomTouched = false;
  /** The city's geometry (segment midpoints for the kerb pips), generated once — `cityGeomOf` regenerates the city. */
  private cg: CityGeom | null = null;
  private labels: Phaser.GameObjects.Text[] = [];
  private chunks = new Map<number, Chunk>();
  private keys!: Record<'W' | 'A' | 'S' | 'D' | 'UP' | 'LEFT' | 'DOWN' | 'RIGHT' | 'SHIFT' | 'SPACE', Phaser.Input.Keyboard.Key>;
  private firing = false;      // the rifle in hand and the left button held
  private lastAim = '';
  private sprinting = false;
  private hoverTile: { tx: number; ty: number } | null = null;
  private dragging = false;
  private placing = false;
  private mining = false;
  private mineAttempt: {state:SimState; at:[number,number]; before:Record<string,number>; until:number} | null = null;

  /** Read actual hand progress and mined counters; never estimate a pickup from elapsed UI time. */
  miningFeedback() {
    const st=this.st,f=st.flow,h=this.hoverTile;
    if(this.mineAttempt?.state!==st)this.mineAttempt=null;
    const attempt=this.mineAttempt, recent=attempt&&(this.mining||performance.now()<attempt.until);
    const at=recent?attempt.at:h?[h.tx,h.ty]:null;
    if(!f||!at||this.tool!=='hand'||this.blueprintMode)return null;
    const r=rubbleAt(st,at[0],at[1]);if(!r&&!recent)return null;
    const label=itemName;
    const gains=recent?Object.entries(f.stats.handMinedOf).filter(([item,n])=>n>(attempt.before[item]??0)).map(([item,n])=>`+${n-(attempt.before[item]??0)} ${label(item)} · Backpack ${Math.floor(st.engineer.inv[item]??0)}`):[];
    const collected=gains.join(' · ');
    const room=!r||pocketSlots(st.engineer).some(slot=>!slot||(!slot.reserved&&slot.item===r.type&&slot.count<stackSize(r.type)));
    const reason=st.engineer.down>=0?'Engineer down':r?.persistent?'Requires a powered Excavator':!inReach(st,at[0],at[1])?'Move closer to mine':!r?'No rubble left here':!room?'Backpack full — make room':st.speed===0?'Paused — resume to mine':this.mining&&!f.hand.mine?'Mining stopped — release and hold again':'';
    const active=this.mining&&f.hand.mine?.[0]===at[0]&&f.hand.mine[1]===at[1]&&!reason;
    const progress=active?Math.max(0,Math.min(1,f.hand.prog)):null;
    return {title:reason||(active?`Mining ${label(r!.type)} · ${Math.floor(progress!*100)}%`:recent?'Mining stopped':`${label(r!.type)} · Hold left-click to mine`),
      detail:collected||(r?`Backpack ${Math.floor(st.engineer.inv[r.type]??0)} ${label(r.type)} · ${r.persistent?'Machine extraction only':`Hold left-click · ${HAND_MINE_PER_S} item / second`}`:'Nothing collected'),progress};
  }
  private pending: [number, number] | null = null;
  private lastWalk = '0,0';
  private walkCloserToasted = false;
  private cursor = 'default';
  private snapped = false;
  viewingThreat = false;
  /** Current tool and the direction the next machine faces. */
  tool: Tool = 'hand';
  dir: Dir = 1;
  /** Tiles drawn last frame; a dev/test number. */
  drawn = 0;
  /** Draw cost, ms: an EMA over frames and the worst since last read (the soak's `world.drawMs`). */
  drawMs = 0; drawWorstMs = 0;
  /** Renderer-only cumulative diagnostics for reproducible light-map engineering checks. */
  renderTiming={lightChecks:0,lightMs:0,lightMaxMs:0,lightPaints:0,lightPaintMs:0,lightPaintMaxMs:0};

  constructor(private session: Session, private view: View, private hooks: WorldHooks) { super('world'); }

  private get st(): SimState { return this.session.state; }
  /** On foot: the flow layer ticks the engineer on the tiles (walk.ts). Without it the old free camera stays. */
  private get onFoot(): boolean { return !!this.st.flow; }

  create(): void {
    this.cameras.main.setBackgroundColor(0x05070f);
    this.makeTileset();
    this.blitter = this.add.blitter(0, 0, 'ground');
    this.gMach = this.add.graphics().setDepth(1);
    this.gPorts = this.add.graphics().setDepth(4);
    // M5: the light map — one texel per tile, scaled to the tile size and multiplied over the ground and machines
    const G0 = ground(this.st);
    this.lightTex = (this.textures.exists('light') ? this.textures.get('light') : this.textures.createCanvas('light', G0.tw, G0.th)) as Phaser.Textures.CanvasTexture;
    this.lightPix = new Uint8Array(G0.tw * G0.th); this.lightScratch = new Uint8Array(G0.tw * G0.th);
    this.lightSoft = new Float32Array(G0.tw * G0.th); this.lightBlur = new Float32Array(G0.tw * G0.th);
    this.lightAtMs = -Infinity;
    this.paintLight(this.lightPix);   // all unlit until the first read
    this.viewLightTex=(this.textures.exists('view-light')?this.textures.get('view-light'):this.textures.createCanvas('view-light',1,1)) as Phaser.Textures.CanvasTexture;
    this.viewLightImage=this.add.image(0,0,'view-light').setOrigin(0).setDepth(this.st.city?.mapId?4.8:1.5).setBlendMode(Phaser.BlendModes.MULTIPLY);
    this.gOver = this.add.graphics().setDepth(2);
    this.gUrban = this.add.graphics().setDepth(2.1);
    this.gRoof = this.add.graphics().setDepth(4.5);
    this.gThreat = this.add.graphics().setDepth(3);
    this.gEng = this.add.graphics().setDepth(this.st.city?.mapId?4.9:3);
    this.depotText = this.add.text(0, 0, 'Depot', { fontSize: '20px', color: '#e8ecf4', fontStyle: 'bold' }).setDepth(3).setOrigin(0.5).setVisible(false);
    const hud = { ...uiPalette(), padding: { x: 8, y: 6 } };
    this.keyText = this.add.text(0, 0, '', hud).setScrollFactor(0).setDepth(10).setOrigin(0, 0);
    this.terrText = this.add.text(0, 0, '', { ...hud, align: 'right' }).setScrollFactor(0).setDepth(10).setOrigin(1, 0);
    this.powText = this.add.text(0, 0, '', hud).setScrollFactor(0).setDepth(10).setOrigin(0, 1);
    this.clockText = this.add.text(0, 0, '', { ...hud, align: 'right' }).setScrollFactor(0).setDepth(10).setOrigin(1, 1);
    this.beaconText = this.add.text(0, 0, '', { fontSize: '13px', color: '#ffffff', fontStyle: 'bold', backgroundColor: '#0b0e1acc', padding: { x: 6, y: 3 } }).setScrollFactor(0).setDepth(10).setOrigin(0.5).setVisible(false);
    this.cg = this.st.city ? cityGeomOf(this.st) : null;
    const cam = this.cameras.main, G = ground(this.st);
    cam.setBounds(0, 0, G.tw * TILE_PX, G.th * TILE_PX);
    cam.setZoom(this.fitZoom());
    cam.setRoundPixels(false); // Fractional zoom must not round every ground tile into visible seams.
    this.keys = this.input.keyboard!.addKeys(movementKeys()) as WorldScene['keys'];
    window.addEventListener('relight:preferences',()=>{this.releaseInput();for(const key of new Set(Object.values(this.keys)))this.input.keyboard!.removeKey(key);this.keys=this.input.keyboard!.addKeys(movementKeys()) as WorldScene['keys'];});
    this.input.keyboard!.on('keydown', (event: KeyboardEvent) => { this.modifiedKey = event.ctrlKey || event.metaKey || event.altKey; });
    this.input.keyboard!.on('keyup', (event: KeyboardEvent) => { this.modifiedKey = event.ctrlKey || event.metaKey || event.altKey; });
    // Clicking back into the world leaves station inputs, restoring game keyboard controls.
    this.game.canvas.tabIndex = 0;
    this.input.on('pointerdown', (p: Phaser.Input.Pointer) => { if(uiInput.blocked)return;this.game.canvas.focus({ preventScroll: true }); this.onDown(p); });
    this.input.on('pointerup', () => {if(uiInput.blocked)this.releaseInput();else this.onUp();});
    this.input.on('gameout', () => { this.releaseInput(); this.hoverTile = null; this.hooks.onHoverText(null, 0, 0); });
    this.input.on('pointermove', (p: Phaser.Input.Pointer) => {if(!uiInput.blocked)this.onMove(p);});
    this.input.on('wheel', (p: Phaser.Input.Pointer, _o: unknown, _dx: number, dy: number) => {
      if(uiInput.blocked)return;
      const f = dy > 0 ? 1 / ZOOM_STEP : ZOOM_STEP;
      if (this.onFoot) this.zoomAt(...this.engineerScreen(), f); else this.zoomAt(p.x, p.y, f);
    });
    // Layout pass: the canvas fills the window (Scale.RESIZE), so the opening zoom is re-fitted when the window
    // changes size — until the human turns the wheel, after which their zoom stands.
    this.scale.on('resize', () => { if (!this.zoomTouched) this.cameras.main.setZoom(this.fitZoom()); });
    this.events.on(Phaser.Scenes.Events.WAKE, () => this.onWake());
    this.events.on(Phaser.Scenes.Events.SLEEP, () => { this.beltPath = null; this.undergroundStart = null; this.onUp(); this.sendWalk(0, 0); this.sendSprint(false); });
    this.onWake();
  }

  private onWake(): void {
    this.hoverTile = null;
    this.lastWalk = '0,0';
    if (this.onFoot) { this.snapped = false; return; }   // the camera lands on the engineer in the first update
    if (this.pending) { this.centreOn(this.pending[0], this.pending[1]); this.pending = null; }
  }

  /** Flat-colour tileset on a canvas texture: one 32 px frame per kind/type/variant. GAME-ASSUMPTION: code-drawn
   *  placeholder frames stand in for the §4 tilesets until the Phase 12 art pass; the frame table is the contract. */
  private makeTileset(): void {
    if (this.textures.exists('ground')) return;
    const tex = this.textures.createCanvas('ground', F_COUNT * TILE_PX, TILE_PX)!;
    const ctx = tex.getContext();
    const rect = (f: number, col: string, x = 0, y = 0, w = TILE_PX, h = TILE_PX) => { ctx.fillStyle = col; ctx.fillRect(f * TILE_PX + x, y, w, h); };
    rect(F_STREET, '#35434a');
    rect(F_GROUND, '#424d4e');
    rect(F_PAVE, '#728082'); rect(F_PAVE, '#657475', 0, 0, TILE_PX, 1);
    rect(F_QUAY, '#7c8379'); rect(F_QUAY, '#586b6c', 0, 0, TILE_PX, 2);
    rect(F_DASH_H, '#35434a'); rect(F_DASH_H, '#aea58a', 4, 15, 23, 2);
    rect(F_DASH_V, '#35434a'); rect(F_DASH_V, '#aea58a', 15, 4, 2, 23);
    rect(F_INERT, '#262a34'); ctx.strokeStyle = '#1b1e26'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.moveTo(F_INERT * TILE_PX + 4, TILE_PX - 4); ctx.lineTo(F_INERT * TILE_PX + TILE_PX - 4, 4); ctx.stroke();
    rect(F_RIVER, '#1f3552'); rect(F_RIVER, '#264263', 0, 10, TILE_PX, 3); rect(F_RIVER, '#264263', 0, 22, TILE_PX, 3);
    // rubble: ground with chunks; more and bigger chunks per variant, in the district's colour
    const chunk = (f: number, k: number, col: string, dark: string, n: number, size: number) => {
      for (let i = 0; i < n; i++) {
        const h = ((k * 7919 + i * 104729 + f * 31) % 977) / 977, v = ((k * 6007 + i * 15485863 + f * 17) % 883) / 883;
        const s = size + ((i * 3 + k) % 3);
        const x = 2 + h * (TILE_PX - 4 - s), y = 2 + v * (TILE_PX - 4 - s);
        ctx.fillStyle = i % 3 === 2 ? dark : col; ctx.fillRect(Math.round(f * TILE_PX + x), Math.round(y), s, s);
      }
    };
    for (let t = 0; t < RUBBLE_COL.length; t++) for (let v = 0; v < RUBBLE_VARIANTS; v++) {
      const f = F_RUBBLE + t * RUBBLE_VARIANTS + v;
      rect(f, '#424d4e');
      chunk(f, t * 10 + v, RUBBLE_COL[t], RUBBLE_DARK[t], 3 + v * 3, 3 + v);
    }
    for (let t = 0; t < 2; t++) for (let v = 0; v < RUBBLE_VARIANTS; v++) {
      const f = F_DEPOSIT + t * RUBBLE_VARIANTS + v;
      rect(f, '#3d3a36'); chunk(f, 100 + t * 10 + v, DEPOSIT_COL[t], DEPOSIT_COL[t], 4 + v * 2, 2 + (v >> 1));
    }
    // HQ patches (§11): dense heaps of steel and copper rubble, and the coal seam
    rect(F_PATCH, '#3d3a36'); chunk(F_PATCH, 200, RUBBLE_COL[2], RUBBLE_DARK[2], 14, 4);
    rect(F_PATCH + 1, '#3d3a36'); chunk(F_PATCH + 1, 210, RUBBLE_COL[1], RUBBLE_DARK[1], 14, 4);
    rect(F_PATCH + 2, '#3d3a36'); chunk(F_PATCH + 2, 220, DEPOSIT_COL[1], '#2c2d33', 12, 4);
    tex.refresh();
    for (let f = 0; f < F_COUNT; f++) tex.add(f, 0, f * TILE_PX, 0, TILE_PX, TILE_PX);
  }

  /** The frames of one 32×32 chunk, rebuilt when its `chunkKey` (the state of every block with tiles in it) changes. */
  private chunk(cx: number, cy: number): Uint8Array {
    const st = this.st, G = ground(st), id = cy * G.cw + cx, key = chunkKey(st, cx, cy);
    const hit = this.chunks.get(id);
    if (hit && hit.key === key) return hit.frames;
    const c = chunkTiles(st, cx, cy), frames = new Uint8Array(CHUNK * CHUNK);
    const x0 = cx * CHUNK, y0 = cy * CHUNK;
    for (let i = 0; i < frames.length; i++) {
      const k = c.kind[i];
      let f = F_GROUND;
      if (k === T_STREET) {
        f = F_STREET;
        const x = x0 + i % CHUNK, y = y0 + Math.floor(i / CHUNK), t = y * G.tw + x, surface = G.urban?.surface;
        if (surface?.[t] === 1) f = F_PAVE;
        else if (surface?.[t] === 2) f = F_QUAY;
        else if (surface && x > 5 && y > 5 && x < G.tw - 5 && y < G.th - 5) {
          // Mark the centres of real broad streets, derived from the same tile mask.
          const road = (xx: number, yy: number) => G.owner[yy * G.tw + xx] === -1;
          if (x % 4 < 2 && road(x - 5, y) && road(x + 5, y) && surface[t - 3 * G.tw] && surface[t + 3 * G.tw]) f = F_DASH_H;
          else if (y % 4 < 2 && road(x, y - 5) && road(x, y + 5) && surface[t - 3] && surface[t + 3]) f = F_DASH_V;
        }
      }
      else if (k === T_INERT) f = F_INERT;
      else if (k === T_RIVER) f = F_RIVER;
      else if (k === T_PATCH) f = F_PATCH + Math.max(0, c.patch[i] - 1);
      else if (k === T_RUBBLE || k === T_DEPOSIT) {
        const lx = i % CHUNK, ly = (i - lx) / CHUNK, o = G.owner[(y0 + ly) * G.tw + x0 + lx], bg = o >= 0 ? G.blocks[o] : null;
        f = k === T_RUBBLE ? F_RUBBLE + RUBBLE_IDX[bg?.rubble ?? 'stone'] * RUBBLE_VARIANTS + Math.max(0, c.variant[i] - 1)
          : F_DEPOSIT + DEPOSIT_IDX[bg?.deposit ?? 'iron'] * RUBBLE_VARIANTS + Math.max(0, c.variant[i] - 1);
      } else if (k === T_GROUND) f = F_GROUND;
      frames[i] = f;
    }
    this.chunks.set(id, { key, frames });
    return frames;
  }

  // ------------------------------------------------------------------ camera

  /** Layout pass: the opening zoom puts the HQ lot at about a third of the viewport's height, so the first thing on
   *  screen is the lot you start on with the streets around it — at the old fixed 1× a 6-tile lot was a fifth of a
   *  short window and a tenth of a tall one. GAME-ASSUMPTION: the ROADMAP asks for "the HQ lot ≈ one third of screen
   *  height" and names no tolerance; `HQ_SCREEN_FRAC` is that third, clamped to the constitution's 0.5–3× range
   *  (D-P4-1), and it is drawing only — no rule reads it. Recomputed on a resize, but never after the human has
   *  touched the wheel (`zoomTouched`). */
  private fitZoom(): number {
    const cam = this.cameras.main, G = ground(this.st), hq = G.blocks.find(b => b.hq);
    if (!hq || cam.height <= 0) return 1;
    if (G.urban) return Math.max(0.65, Math.min(0.85, cam.height / 1400));
    const lotPx = (hq.y1 - hq.y0 + 1) * TILE_PX;
    return Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, (cam.height * HQ_SCREEN_FRAC) / Math.max(1, lotPx)));
  }

  /** World point under a screen point, for the camera's current scroll and zoom (no rotation). */
  private worldAt(sx: number, sy: number, zoom = this.cameras.main.zoom): { x: number; y: number } {
    const cam = this.cameras.main;
    return { x: cam.scrollX + cam.width / 2 + (sx - cam.width / 2) / zoom, y: cam.scrollY + cam.height / 2 + (sy - cam.height / 2) / zoom };
  }

  /** Screen point of a tile-space point (dev hook: the scripted checks aim the mouse with it). */
  screenOf(x: number, y: number): [number, number] {
    const cam = this.cameras.main;
    return [cam.width / 2 + (x * TILE_PX - cam.scrollX - cam.width / 2) * cam.zoom, cam.height / 2 + (y * TILE_PX - cam.scrollY - cam.height / 2) * cam.zoom];
  }

  /** The engineer's screen point (the zoom pivot on foot). */
  private engineerScreen(): [number, number] {
    const cam = this.cameras.main, e = this.st.engineer;
    return [cam.width / 2 + (e.x * TILE_PX - cam.scrollX - cam.width / 2) * cam.zoom, cam.height / 2 + (e.y * TILE_PX - cam.scrollY - cam.height / 2) * cam.zoom];
  }

  zoomAt(sx: number, sy: number, factor: number): void {
    const cam = this.cameras.main;
    this.zoomTouched = true;
    const z1 = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, cam.zoom * factor));
    if (z1 === cam.zoom) return;
    const w = this.worldAt(sx, sy);
    cam.setZoom(z1);
    cam.setScroll(w.x - cam.width / 2 - (sx - cam.width / 2) / z1, w.y - cam.height / 2 - (sy - cam.height / 2) / z1);
  }

  setZoom(z: number): void {
    const [sx, sy] = this.onFoot && !this.viewingThreat ? this.engineerScreen() : [this.cameras.main.width / 2, this.cameras.main.height / 2];
    this.zoomAt(sx, sy, z / this.cameras.main.zoom);
  }
  get zoom(): number { return this.cameras.main.zoom; }

  /** Centre the camera on a block (its pole tile). Safe before create(): the request is kept for the first wake.
   *  On foot the camera follows the engineer, so this only holds until the next frame. */
  centreOn(x: number, y: number): void {
    if (!this.cameras?.main) { this.pending = [x, y]; return; }
    const st = this.st, i = idxOf(st, x, y), G = ground(st);
    const p = i >= 0 ? G.blocks[i].pole : [G.tw / 2, G.th / 2];
    this.cameras.main.centerOn((p[0] + 0.5) * TILE_PX, (p[1] + 0.5) * TILE_PX);
  }

  /** Explicit camera inspection leaves the engineer, selected tool and simulation untouched. */
  viewThreat():void {
    const target=knownCampaignThreat(this.st);if(!target)return;
    if(this.input.activePointer.isDown){this.hooks.onToast('Release the pointer before viewing the threat');return;}
    this.viewingThreat=true;
    this.cameras.main.centerOn(target.x*TILE_PX,target.y*TILE_PX);
  }
  releaseInput():void {
    if(!this.keys)return;
    // Capture cancels unfinished drags: normal pointer-up would commit a belt path or blueprint selection.
    this.beltPath=null;this.copyStart=null;this.undergroundStart=null;this.hoverTile=null;
    this.input.keyboard?.resetKeys();this.modifiedKey=false;this.sendWalk(0,0);this.sendSprint(false);this.onUp();
  }
  cancelSelection():boolean {
    if(this.tool==='hand'&&!this.blueprintMode&&!this.removal&&!this.beltPath&&!this.undergroundStart&&(!inspectionView.machineId||inspectionView.pinned))return false;
    this.releaseInput();this.blueprint('cancel');this.setTool('hand');if(!inspectionView.pinned)inspectionView.machineId=null;return true;
  }
  viewLocation(x:number,y:number):void {this.viewingThreat=true;this.cameras.main.centerOn(x*TILE_PX,y*TILE_PX);}
  returnToEngineer():void {this.viewingThreat=false;this.snapped=false;}

  /** The block the map view gets on M: the engineer's on foot, else the one under the camera's centre. */
  focusBlock(): [number, number] {
    const st = this.st;
    let i = -1;
    if (this.onFoot) { const e = st.engineer; i = e.block >= 0 ? e.block : blockOfTile(st, Math.floor(e.x), Math.floor(e.y)); }
    else { const cam = this.cameras.main, m = this.worldAt(cam.width / 2, cam.height / 2); i = blockOfTile(st, Math.floor(m.x / TILE_PX), Math.floor(m.y / TILE_PX)); }
    const b = st.blocks[i] ?? st.blocks[idxOf(st, st.start[0], st.start[1])];
    return [b.x, b.y];
  }

  // ------------------------------------------------------------------ tools

  /** A key from the page's keydown handler (only while this view is up). Returns true when the scene took it. */
  removal:RemovalPreview|null=null;
  blueprintMode: 'copy' | 'paste' | 'queue' | 'removeArea' | null = null;
  private copyStart: TilePoint | null = null;
  blueprint(action:'copy'|'paste'|'queue'|'removeArea'|'applyRemoval'|BlueprintTransform|'cancel'):void {
    if(action==='cancel'){this.removal=null;this.blueprintMode=null;this.copyStart=null;return;}
    const why=blueprintGate(this.st);if(why){this.hooks.onToast(why,'bad');return;}
    if(action==='applyRemoval'){const selection=this.removal?.selection;if(!selection)return;const r=dispatch(this.session,{type:'removeArea',selection});this.hooks.onToast(r.reason,r.ok?'good':'bad');if(r.ok)this.blueprint('cancel');else if(this.removal){this.removal.ok=false;this.removal.reason=r.reason;}return;}
    if(action==='copy'||action==='paste'||action==='queue'||action==='removeArea'){
      if(action!=='copy'&&action!=='removeArea'&&!this.st.campaign?.clipboard){this.hooks.onToast('Copy a layout first (Ctrl+C).','bad');return;}
      this.beltPath=null;this.copyStart=null;this.onUp();this.setTool('hand');this.blueprintMode=action;this.copyStart=null;
      this.hooks.onToast(action==='removeArea'?'Drag around whole machines, then review and use Pack selected machines.':action==='copy'?'Drag around complete machines to copy their layout and settings. Esc cancels.':action==='queue'?'Click to queue an inert plan. No materials spent. Esc cancels.':'Click to build the whole clipboard from pockets. R rotates; H/V mirrors; Esc cancels.');
    }else{const r=dispatch(this.session,{type:'blueprintTransform',operation:action});this.hooks.onToast(r.reason,r.ok?'good':'bad');}
  }
  key(k: string): boolean {
    const st = this.st;
    if (!st.flow) return false;
    if(this.blueprintMode && this.blueprintMode!=='removeArea' && (bound('rotate',k)||bound('mirrorX',k)||bound('mirrorY',k))){
      this.blueprint(bound('rotate',k)?'rotate':bound('mirrorX',k)?'mirrorX':'mirrorY');return true;
    }
    const tool = toolForKey(k,quickbarView.slots);
    if (tool) { this.setTool(tool); return true; }
    if (bound('cancel', k)) { this.beltPath = null; this.undergroundStart = null; this.setTool('hand'); return true; }
    if (bound('pipette', k)) {
      // pipette: the machine under the cursor into the hand; nothing there (or the Depot) clears the hand
      const h = this.hoverTile, m = h ? machineAt(st, h.tx, h.ty) : undefined;
      this.setTool(m && m.kind !== 'depot' ? m.kind : 'hand');
      if (m && m.kind !== 'depot') this.dir = m.dir;
      return true;
    }
    if (bound('rotate', k)) {
      const h = this.hoverTile, m = h && this.tool === 'hand' ? machineAt(st, h.tx, h.ty) : undefined;
      if (m && m.kind !== 'depot') { if (this.reachable(m.x, m.y, machineDimensions(m)[0], true, machineDimensions(m)[1])) { const r = dispatch(this.session, { type: 'construct', edits: [{ action: 'rotate', x: m.x, y: m.y }] }); this.hooks.onToast(r.reason, r.ok ? 'good' : 'bad'); } } else this.dir = ((this.dir + 1) % 4) as Dir;
      return true;
    }
    if (bound('recipe', k)) {
      // RI-01: cycle the Assembler under the cursor through the recipes its machine can run (D-B2-1 (b)); the sim's
      // refusal (out of reach, full pockets) is the toast
      const h = this.hoverTile, m = h ? machineAt(st, h.tx, h.ty) : undefined;
      if(m?.kind==='inserter'){const choices=[undefined,...ITEMS],filter=choices[(choices.indexOf(m.filter)+1)%choices.length]??null;const r=dispatch(this.session,{type:'factory',action:{type:'routing',x:m.x,y:m.y,filter}});this.hooks.onToast(r.ok?routingDescription(st,m):r.reason,r.ok?'good':'bad');return true;}
      if(m?.kind==='splitter'){const choices=['balanced','left','right'] as const,priority=choices[(choices.indexOf(m.priority??'balanced')+1)%choices.length];const r=dispatch(this.session,{type:'factory',action:{type:'routing',x:m.x,y:m.y,priority}});this.hooks.onToast(r.ok?routingDescription(st,m):r.reason,r.ok?'good':'bad');return true;}
      if (!m || m.kind !== 'assembler') { this.hooks.onToast(FACTORY_TEXT.recipeTarget, 'bad'); return true; }
      if (!this.reachable(m.x, m.y, machineDimensions(m)[0], true, machineDimensions(m)[1])) return true;
      const cur = m.recipe ?? 'shot', next = RECIPE_IDS[(RECIPE_IDS.indexOf(cur) + 1) % RECIPE_IDS.length];
      const r = dispatch(this.session, { type: 'factory', action: { type: 'setRecipe', x: m.x, y: m.y, recipe: next } });
      this.hooks.onToast(r.reason, r.ok ? 'good' : 'bad');
      return true;
    }
    if (bound('inspect', k)) { const h=this.hoverTile;if(h){if(truckOccupies(this.st,h.tx,h.ty))this.hooks.onTruck();else this.hooks.onInspectAt(h.tx,h.ty);}return true; }
    if (bound('interact', k)) { this.interact(); return true; }
    if (bound('abort', k)) {
      // RI-06 (§9.2 default 9): the explicit abort of the Heart's commissioning; nothing else on X
      const H = heartOf(st);
      if (H && H.attempt >= 0) queue(this.session, { type: 'abort' }); else if (H && !H.destroyed) this.hooks.onToast('Nothing to abort — the Heart is not being commissioned');
      return true;
    }
    return false;
  }

  /** Put a tool (a building, the rifle, or nothing) in the hand. The build menu's picks land here too. */
  setTool(t: Tool): void {
    this.removal=null;this.blueprintMode=null;this.copyStart=null;
    if(t!=='hand'&&t!=='rifle'&&this.st.campaign&&!knownEquipment(this.st,t)){this.hooks.onToast('Those plans have not been discovered.');return;}
    if (t !== 'hand' && t !== 'rifle') { const why = lockReason(this.st, t); if (why) { this.hooks.onToast(why, 'bad'); return; } }
    if (t === this.tool) return;
    this.beltPath = null; this.undergroundStart = null;
    if (this.firing) { this.firing = false; this.sendAim(null); }
    this.tool = t;
  }

  /** E interacts (D-B1-5): the Depot opens the chest, the workbench crafts a magazine, a Dark block's substation takes
   *  the claim's materials from the pockets and Activates (RI-03), a machine reports itself, the truck is entered or
   *  left where it was found, a survivor group on the block answers. E never mines, places or fires. Everything but
   *  the truck needs the thing within reach. */
  interaction() { return campaignInteraction(this.st,this.hoverTile); }
  selectedTool() { const h=this.hoverTile,m=h?machineAt(this.st,h.tx,h.ty):undefined;return {tool:this.tool,reason:this.ghostReason,mode:this.blueprintMode,ports:!!m&&machinePorts(m).length>0,point:h?this.screenOf(h.tx+.5,h.ty+.5):null}; }
  private interact(): void {
    const st = this.st, h = this.hoverTile;
    if (!st.flow || !this.onFoot) return;
    const e = st.engineer;
    if(st.campaign){
      const target=this.interaction();
      if(!target){this.hooks.onToast(e.down>=0?'Recover before interacting.':'No interaction in reach. Point at an object to inspect it.');return;}
      if(target.reason){this.hooks.onToast(target.reason,'bad');return;}
      if(target.panel==='home')this.hooks.onChest();
      else if(target.panel==='chest')this.hooks.onChestAt(target.x,target.y);
      else if(target.panel==='routing')this.hooks.onRoutingAt(target.x,target.y);
      else if(target.panel==='inspection')this.hooks.onInspectAt(target.x,target.y);
      else {for(const command of target.commands){if(command.type==='factory'){const r=dispatch(this.session,command);this.hooks.onToast(r.reason,r.ok?'good':'bad');}else queue(this.session,command);}if(!target.commands.some(c=>c.type==='factory'))this.hooks.onToast(target.commands.length?`${target.label} queued.`:target.detail??target.label);}
      return;
    }
    if(st.campaign&&(e.truckSeat||(h&&truckOccupies(st,h.tx,h.ty)))){const r=dispatch(this.session,{type:'factory',action:{type:'truckBoard'}});this.hooks.onToast(r.reason,r.ok?'good':'bad');return;}
    if(h&&st.campaign){const core=coreAt(st,h.tx,h.ty),m=machineAt(st,h.tx,h.ty);if((core&&core.hp<300)||(m&&defenceMax(m)>0&&defenceHp(m)<defenceMax(m))){const why=repairCheck(st,h.tx,h.ty);if(why)this.hooks.onToast(why,'bad');else{queue(this.session,{type:'repairDefence',x:h.tx,y:h.ty});this.hooks.onToast('Repair queued. Stay within reach; materials pay for this repair once.');}return;}}
    if(h){const s=turbineAt(st,h.tx,h.ty);if(s){if(s.restoredAt>=0)queue(this.session,{type:'setTurbineEnabled',enabled:!s.enabled});else{queue(this.session,{type:'deliverTurbine'});queue(this.session,{type:'restoreTurbine'});}this.hooks.onToast(describeTurbine(st));return;}}
    if(h){const s=recruitAt(st,h.tx,h.ty);if(s){const why=recruitCheck(st,s.id);if(why)this.hooks.onToast(why,'bad');else{queue(this.session,{type:'recruitSurvivors',id:s.id});this.hooks.onToast(`Recruiting the ${RECRUITS[s.kind].name}.`);}return;}}
    if(h&&discoveryAt(st,h.tx,h.ty)){const d=st.campaign!.discovery!,why=discoveryCheck(st,d.id);if(why)this.hooks.onToast(why,'bad');else{queue(this.session,{type:'recoverSchematic',id:d.id});this.hooks.onToast('Recovering the field-repair schematic.');}return;}
    const site = h ? campaignSiteAt(st, h.tx, h.ty) : null;
    if (site) {
      const s = campaignSite(st, site)!; if (!this.reachable(s.x, s.y, s.size, true)) return;
      if (s.restoredAt >= 0 && site === 'station') queue(this.session, { type: 'collectTramKit' });
      else { queue(this.session, { type: 'deliverSite', site }); queue(this.session, { type: 'restoreSite', site }); }
      this.hooks.onToast(describeSite(st, site)); return;
    }
    const wb = workbenchTile(st);
    if (h && h.tx >= wb[0] && h.tx < wb[0] + 2 && h.ty >= wb[1] && h.ty < wb[1] + 2) {
      if (!this.reachable(wb[0], wb[1], 2, true)) return;
      const r = dispatch(this.session, { type: 'factory', action: { type: 'craft', item: 'magazine', count: 1 } });
      this.hooks.onToast(r.reason, r.ok ? 'good' : 'bad');
      return;
    }
    // M5: E on a broken (§13) or eaten (M4) light repairs it from the pockets
    const light = h ? lightAt(st, h.tx, h.ty) : null;
    if (light && light.l.why) {
      if (!this.reachable(h!.tx, h!.ty, 1, true)) return;
      const r = dispatch(this.session, { type: 'factory', action: { type: 'repair', x: h!.tx, y: h!.ty } });
      if (!r.ok) { this.hooks.onToast(`No repair: ${r.reason}`, 'bad'); return; }
      const bi = light.bi, b = st.blocks[bi];
      this.hooks.onToast(`${light.l.kind === 'lamp' ? 'Lamp' : 'Streetlight'} repaired (${REPAIR_COPPER} Cu from the pockets) — ${b.state === HELD || b.state === CONTESTED ? 'it lights while the substation powers it' : 'it lights when its block is claimed'}`, 'good');
      return;
    }
    // RI-03 (plan §4.1, D-RI-2): E on a Dark block's substation — the installation — moves what the claim still needs
    // from the pockets into it (a legitimate inventory interaction, logged as `deliver`) and, when every prerequisite
    // holds, sends the explicit Activate; otherwise the toast names the missing prerequisite. A built outskirts
    // Substation is a machine too, so this comes before the machine report.
    // RI-06: E on a feeder cabinet — repair it when it is knocked out (REPAIR_COPPER from the pockets), else move what
    // it still needs from the pockets into it (a `deliver` with `cabinet`); the toast names what it lacks
    const kc = h && st.flow ? cabinetAt(st, h.tx, h.ty) : -1;
    if (kc >= 0) {
      const H = heartOf(st)!, c = H.cabinets[kc], bs = st.blocks[H.site];
      if (!this.reachable(c.x, c.y, 1, true)) return;
      if (c.down) {
        const chk = cabinetRepairCheck(st, kc);
        if (chk.ok) queue(this.session, { type: 'repairCabinet', cabinet: kc }); else this.hooks.onToast(`Feeder cabinet ${kc + 1} · ${chk.reason}`, 'bad');
        return;
      }
      const moved: string[] = [];
      for (const item of ['steel', 'copper'] as const) {
        const short = H.cand.cabinet[item] - c.delivered[item];
        if (short <= 0 || (e.inv[item] ?? 0) <= 0) continue;
        const r = dispatch(this.session, { type: 'factory', action: { type: 'deliver', bx: bs.x, by: bs.y, item, n: short, cabinet: kc } });
        if (r.ok) moved.push(`${r.moved} ${item === 'copper' ? 'Cu' : 'steel'}`);
      }
      this.hooks.onToast(`${moved.length ? `${moved.join(' + ')} delivered · ` : ''}${describeCabinet(st, kc)}`, moved.length ? 'good' : undefined);
      return;
    }
    if (h && isSubstationTile(st, h.tx, h.ty)) {
      const bi = blockOfTile(st, h.tx, h.ty), b = bi >= 0 ? st.blocks[bi] : null;
      if (b && (b.state === DARK || b.state === CONTESTED)) {
        const sub = substationAt(st, b.x, b.y);
        if (!sub || !this.reachable(sub.tx, sub.ty, sub.size, true)) return;
        const name = blockLabel(st, bi, debugView.coords);
        if (b.state === CONTESTED) { this.hooks.onToast(heartAt(st, bi) ? `${name} · ${describeHeart(st)}` : `${name} · commissioning — burn-off ${Math.round(100 * contestProgress(st, bi))} %`); return; }
        const need = claimNeed(st), moved: string[] = [];
        for (const item of ['steel', 'copper'] as const) {
          const short = need[item] - deliveredTo(st, bi)[item];
          if (short <= 0 || (e.inv[item] ?? 0) <= 0) continue;
          const r = dispatch(this.session, { type: 'factory', action: { type: 'deliver', bx: b.x, by: b.y, item, n: short } });
          if (r.ok) moved.push(`${r.moved} ${item === 'copper' ? 'Cu' : 'steel'}`);
        }
        const chk = activationCheck(st, b.x, b.y), got = deliveredTo(st, bi);
        if (chk.ok) {
          queue(this.session, { type: 'activate', bx: b.x, by: b.y });   // the claim event's toast says the rest
          if (moved.length) this.hooks.onToast(`${moved.join(' + ')} delivered to ${name}'s substation — activating`, 'good');
          return;
        }
        this.hooks.onToast(`${name}${moved.length ? ` · ${moved.join(' + ')} delivered` : ''} · ${got.steel}/${need.steel} steel, ${got.copper}/${need.copper} Cu at its substation · not activated: ${chk.reason}`, 'bad');
        return;
      }
    }
    const m = h ? machineAt(st, h.tx, h.ty) : undefined;
    if (m) {
      if (!this.reachable(m.x, m.y, machineDimensions(m)[0], true, machineDimensions(m)[1])) return;
      if(m.kind==='inserter'||m.kind==='splitter'){this.hooks.onRoutingAt(m.x,m.y);return;}
      if (m.kind === 'depot') { this.hooks.onChest(); return; }
      if (m.kind === 'chest' || m.kind === 'tramstop') { this.hooks.onChestAt(m.x, m.y); return; }   // RI-05: the pockets talk to it
      this.hooks.onInspectAt(m.x,m.y);
      return;
    }
    if (!st.campaign && e.truckFound) { queue(this.session, { type: 'enterTruck' }); this.hooks.onToast(e.truck ? 'Out of the truck' : 'In the truck — 3× walk speed, 200 stacks'); return; }
    const b = e.block >= 0 ? st.blocks[e.block] : null;
    const sv = b ? st.survivors.find(v => v.x === b.x && v.y === b.y) : undefined;
    if (!st.campaign && sv) { this.hooks.onToast(`${sv.name} (${sv.tag}): "${b!.state === HELD ? "We're in." : 'Light the street and we talk.'}"`); return; }
    this.hooks.onToast('Nothing here to interact with — E opens the Depot chest, the workbench, a machine, the truck, a survivor group, or repairs a broken light');
  }

  /** Top-left tile of the tool's footprint when the pointer is on (tx, ty): 3×3 machines centre on the pointer,
   *  2×2 ones (turret, Generator) take the pointer's tile as their top-left. */
  private footprint(kind: Kind, tx: number, ty: number): [number, number] {
    const off = Math.floor((MACHINE_SIZE[kind] - 1) / 2);
    return [tx - off, ty - off];
  }

  /** D5 reach: hand actions land within 8 tiles of the engineer. Outside, the first refusal is a toast, every one
   *  the "walk closer" cursor and the ghost's reason. Without the flow layer there is no engineer on the tiles. */
  private reachable(tx: number, ty: number, size: number, loud: boolean, height = size, showNotice = true): boolean {
    if (!this.onFoot || inReach(this.st, tx, ty, size, height)) return true;
    if (loud && this.st.flow) this.st.flow.stats.reachRefused++;   // GAME-ASSUMPTION (M4 telemetry): placements refused for reach count one per click, not per drag step
    if (loud && showNotice && !this.walkCloserToasted) { this.walkCloserToasted = true; this.hooks.onToast(`Walk closer — the engineer reaches ${REACH} tiles (WASD or click the ground)`, 'bad'); }
    return false;
  }

  private tryPlace(tx: number, ty: number, loud: boolean): void {
    const st = this.st;
    if (this.tool === 'hand' || !st.flow) return;
    const kind = this.tool as Kind;
    const [ox, oy] = this.footprint(kind, tx, ty);
    const [width,height]=dimensions(kind,this.dir,MACHINE_SIZE[kind]);
    if (!this.reachable(ox, oy, width, loud,height,!st.campaign)) return;
    const c = canPlace(st, kind, ox, oy,this.dir);
    if (!c.ok) { if (loud && !st.campaign) this.hooks.onToast(`No ${kind} here: ${c.reason}`, 'bad'); return; }
    const r = dispatch(this.session, { type: 'construct', edits: [{ action: 'place', item: kind, x: ox, y: oy, dir: this.dir }] });
    if (!r.ok) this.hooks.onToast(r.reason, 'bad');
  }

  private sendWalk(dx: number, dy: number): void {
    const k = `${dx},${dy}`;
    if (k === this.lastWalk || !this.onFoot) return;
    this.lastWalk = k;
    queue(this.session, { type: 'walk', dx, dy });
  }

  private sendSprint(on: boolean): void {
    if (on === this.sprinting || !this.onFoot) return;
    this.sprinting = on;
    queue(this.session, { type: 'sprint', on });
  }

  /** The rifle's aim: the cursor's world position in tiles (fractional), or null on release. */
  private sendAim(at: [number, number] | null): void {
    const k = at ? `${at[0].toFixed(2)},${at[1].toFixed(2)}` : '';
    if (k === this.lastAim) return;
    this.lastAim = k;
    queue(this.session, { type: 'aim', at });
  }

  private aimAt(p: Phaser.Input.Pointer): [number, number] {
    const w = this.worldAt(p.x, p.y);
    return [w.x / TILE_PX, w.y / TILE_PX];
  }

  private onDown(p: Phaser.Input.Pointer): void {
    const st = this.st;
    const w = this.worldAt(p.x, p.y);
    const tx = Math.floor(w.x / TILE_PX), ty = Math.floor(w.y / TILE_PX);
    if(this.blueprintMode){
      if(p.rightButtonDown()){this.blueprint('cancel');return;}
      if(!p.leftButtonDown())return;
      if(this.blueprintMode==='copy'||this.blueprintMode==='removeArea'){this.removal=null;this.copyStart={x:tx,y:ty};this.hoverTile={tx,ty};}
      else {const r=dispatch(this.session,this.blueprintMode==='queue'?{type:'blueprintOrder',action:{type:'queue',x:tx,y:ty}}:{type:'blueprintPaste',x:tx,y:ty});this.hooks.onToast(r.reason,r.ok?'good':'bad');}
      return;
    }
    if (p.rightButtonDown()) {
      this.beltPath = null; this.undergroundStart = null;
      if (!st.flow) return;
      if (this.tool !== 'hand') { this.setTool('hand'); return; }   // something in hand: right-click clears it
      const m = machineAt(st, tx, ty);
      if (!m) return;
      if (!this.reachable(m.x, m.y, machineDimensions(m)[0], true, machineDimensions(m)[1])) return;
      // M2: a pick-up goes to the pockets with what the machine held; a full pocket refuses with the reason
      const pk = canPickUp(st, tx, ty);
      if (!pk.ok) { this.hooks.onToast(`No pick-up: ${pk.reason}`, 'bad'); return; }
      const result = dispatch(this.session, { type: 'construct', edits: [{ action: 'pickUp', x: tx, y: ty }] });
      if (!result.ok) { this.hooks.onToast(result.reason, 'bad'); return; }
      const held = Object.keys(pk.items).filter(k => k !== m.kind).map(k => `${pk.items[k]} ${k === 'magazine' && pk.items[k] !== 1 ? 'magazines' : k}`).join(', ');
      this.hooks.onToast(`${m.kind} picked up into the pockets (${pk.stacks} stack${pk.stacks === 1 ? '' : 's'}${held ? `: ${held}` : ''}) — ${invStacks(st.engineer.inv)}/${INV_STACKS} stacks`);
      return;
    }
    if (!p.leftButtonDown()) return;
    // D-B1-5: left-click does what the hand holds
    if (this.tool === 'rifle' && st.flow) {
      if (this.onFoot) { this.firing = true; this.sendAim(this.aimAt(p)); }
      return;
    }
    if(this.tool==='underground'&&st.flow){
      if(!this.undergroundStart){const existing=machineAt(st,tx,ty);if(existing?.kind==='underground'&&existing.underground==='input')this.dir=existing.dir;this.undergroundStart={x:tx,y:ty};return;}
      const r=dispatch(this.session,{type:'undergroundPair',from:this.undergroundStart,to:{x:tx,y:ty},dir:this.dir});
      if(r.ok)this.undergroundStart=null;this.hooks.onToast(r.reason,r.ok?'good':'bad');return;
    }
    if (this.tool === 'belt' && st.flow) { this.beltPath = [{ x: tx, y: ty }]; return; }
    if (this.tool !== 'hand' && st.flow) { this.placing = true; this.tryPlace(tx, ty, true); return; }
    if (this.tool === 'hand' && st.flow) {
      // §11: the tester hand-feeds turrets and the Generator; a click on one moves what the Depot has
      const m = machineAt(st, tx, ty);
      if (m && (m.kind === 'turret' || m.kind === 'generator')) {
        if (!this.reachable(m.x, m.y, machineDimensions(m)[0], true, machineDimensions(m)[1])) return;
        const r = dispatch(this.session, { type: 'factory', action: { type: 'feed', x: tx, y: ty } });
        this.hooks.onToast(r.reason, r.ok ? 'good' : 'bad');
        return;
      }
      if (rubbleAt(st, tx, ty)) {
        this.mineAttempt={state:st,at:[tx,ty],before:{...st.flow.stats.handMinedOf},until:performance.now()+3000};
        if (!this.reachable(tx, ty, 1, true)) return;
        this.mining = true; dispatch(this.session, { type: 'factory', action: { type: 'mineAt', x: tx, y: ty } }); return;
      }
      if (this.onFoot) return;   // Walking is controlled with WASD; map clicks only inspect or pin.
    }
    this.dragging = true;
  }

  private beltPath: TilePoint[] | null = null;
  private undergroundStart: TilePoint | null = null;
  private modifiedKey = false;
  private previewKey = '';
  private previewResult = { ok: true, reason: '' };
  private onUp(): void {
    if(this.copyStart){const from=this.copyStart,h=this.hoverTile;this.copyStart=null;
      if(h&&this.blueprintMode==='removeArea'){this.removal=removalPreview(this.st,from,{x:h.tx,y:h.ty});this.hooks.onToast(this.removal.reason,this.removal.ok?'good':'bad');}
      else if(h){const r=dispatch(this.session,{type:'blueprintCopy',from,to:{x:h.tx,y:h.ty}});this.hooks.onToast(r.reason,r.ok?'good':'bad');if(r.ok)this.blueprintMode='paste';}
    }
    if (this.beltPath) {
      const path = this.beltPath; this.beltPath = null; this.undergroundStart = null;
      const r = dispatch(this.session, { type: 'buildPath', path, dir: this.dir });
      this.hooks.onToast(r.reason, r.ok ? 'good' : 'bad');
    }
    this.dragging = false; this.placing = false;
    if (this.mining) { if(this.mineAttempt)this.mineAttempt.until=performance.now()+3000; this.mining = false; if (this.st.flow) { dispatch(this.session, { type: 'factory', action: { type: 'mineAt', x: -1, y: -1 } }); } }   // M6: (-1,-1) = the hands off (no rubble there)
    if (this.firing) { this.firing = false; this.sendAim(null); }
  }

  private onMove(p: Phaser.Input.Pointer,refresh=false): void {
    const cam = this.cameras.main;
    if (this.dragging && p.isDown && !this.onFoot) {
      cam.scrollX -= (p.position.x - p.prevPosition.x) / cam.zoom;
      cam.scrollY -= (p.position.y - p.prevPosition.y) / cam.zoom;
    }
    const w = this.worldAt(p.x, p.y);
    const tx = Math.floor(w.x / TILE_PX), ty = Math.floor(w.y / TILE_PX);
    if (!refresh && this.hoverTile && this.hoverTile.tx === tx && this.hoverTile.ty === ty) return;
    this.hoverTile = { tx, ty };
    const st = this.st, G = ground(st);
    if (this.beltPath && p.isDown) this.beltPath = extendBuildPath(this.beltPath, { x: tx, y: ty });
    if (this.placing && p.isDown) this.tryPlace(tx, ty, false);
    if (this.mining && p.isDown && st.flow) { if(this.mineAttempt)this.mineAttempt.at=[tx,ty]; const was = st.flow.hand.mine; if (!was || was[0] !== tx || was[1] !== ty) { dispatch(this.session, { type: 'factory', action: { type: 'mineAt', x: tx, y: ty } }); } }
    if (this.firing && p.isDown) this.sendAim(this.aimAt(p));
    if (tx < 0 || ty < 0 || tx >= G.tw || ty >= G.th) { this.hooks.onHoverText(null, 0, 0); return; }
    const rect = this.game.canvas.getBoundingClientRect();
    const m = st.flow ? machineAt(st, tx, ty) : undefined;
    const bi = blockOfTile(st, tx, ty);
    // M5: an unlit tile says what that means (rule 8: the lit/unlit rule is on the tooltip, not only in the texture)
    const unlit = st.flow && !this.lightPix[ty * G.tw + tx] ? ' · unlit (rot can sit here; a shade here cannot be hit)' : '';
    // RI-02: the tile coordinates are debug coordinates — shown only behind the ` toggle
    const groundLine = describeGround(st, tx, ty), lines = [(debugView.coords ? groundLine : groundLine.replace(/^tile \(-?\d+,-?\d+\) · /, '')) + unlit];
    if (bi >= 0) lines.push(this.blockLine(bi));
    if(truckOccupies(st,tx,ty))lines.unshift(truckDescription(st)+' · F: cargo');
    const kc = st.flow ? cabinetAt(st, tx, ty) : -1;
    if (kc >= 0) lines.unshift(describeCabinet(st, kc));   // RI-06: a feeder cabinet under the cursor
    const defenceInfo=defenceDescription(st,tx,ty);if(defenceInfo)lines.unshift(defenceInfo);
    const source=persistentSource(st,tx,ty);
    if(source)lines.unshift(`${source.item.toUpperCase()} extraction · persistent source · powered excavator, 0.5 items/s · belt output to your station`);
    if(turbineAt(st,tx,ty)&&(st.campaign?.turbine?.seenAt??-1)>=0)lines.unshift(describeTurbine(st));
    const recruit=recruitAt(st,tx,ty);if(recruit&&recruit.seenAt>=0)lines.unshift(recruitDescription(st,recruit));
    if(discoveryAt(st,tx,ty)&&(st.campaign?.discovery?.seenAt??-1)>=0)lines.unshift(discoveryDescription(st));
    const campaignInstallation = campaignSiteAt(st, tx, ty);
    if (campaignInstallation && knownSite(st,campaignInstallation)) lines.unshift((campaignDiscoveries(st).find(s=>s.id===campaignInstallation)?.title??campaignInstallation)+' · '+describeSite(st, campaignInstallation));
    else if (m) { const connections=connectionText(m,st);if(connections)lines.push(connections);lines.unshift(describeMachine(st, m)); if (STATUS_KIND.has(m.kind)) lines.unshift(this.statusLine(m)); }
    else if (!st.campaign && st.flow && isSubstationTile(st, tx, ty)) {
      const sub = bi >= 0 ? substationAt(st, st.blocks[bi].x, st.blocks[bi].y) : null;
      if (sub && heartAt(st, bi)) lines.unshift(`The Junction Heart · ${describeHeart(st)}`);   // RI-06: the objective, the active failure condition, the next action
      if (sub && st.blocks[bi].state === DARK) {
        // RI-03: the installation UI — what is delivered, and the prerequisite the Activate still lacks
        const chk = activationCheck(st, st.blocks[bi].x, st.blocks[bi].y), need = claimNeed(st), got = deliveredTo(st, bi);
        const materials = need.steel + need.copper > 0 ? `${got.steel}/${need.steel} steel, ${got.copper}/${need.copper} Cu delivered · ` : '';
        lines.unshift(`Substation · Dark · ${materials}${chk.ok ? 'ready — E activates the claim' : `to claim: ${chk.reason}${/more steel|more Cu/.test(chk.reason) ? ' (E delivers from the pockets)' : ''}`}`);
      } else if (sub) lines.unshift(`Substation · ${sub.on ? `on · draws ${sub.kw} kW · streetlights lit` : sub.kw === 0 ? 'Dark · string poles to it to claim' : 'off · no power (the block is unfed, or the grid is dead)'}`);
    }
    const namedPlace=campaignDiscoveries(st).find(s=>tx>=s.x&&tx<s.x+s.size&&ty>=s.y&&ty<s.y+s.size);if(namedPlace&&!campaignInstallation)lines.unshift(namedPlace.title);
    // M5: a light under the cursor — its state, and the repair rule when it is broken or eaten
    const light = lightAt(st, tx, ty);
    if (light && (light.l.kind === 'streetlight' || light.l.why)) {
      const name = light.l.kind === 'lamp' ? 'Lamp' : 'Streetlight';
      const b = st.blocks[light.bi];
      lines.unshift(light.l.why === 'eaten' ? `${name} · put out by a crawler — E repairs it (${REPAIR_COPPER} Cu from the pockets)`
        : light.l.why === 'broken' ? `${name} · broken — E repairs it (${REPAIR_COPPER} Cu from the pockets)`
        : light.l.lit ? `${name} · lit · radius ${light.l.r}` : b.state === CONTESTED ? `${name} · coming on (burn-off ${Math.round(100 * contestProgress(st, light.bi))} %)`
        : b.state === HELD ? `${name} · off · no power` : `${name} · off · its block is Dark`);
    }
    const cw = st.flow ? crawlerAt(st, tx + 0.5, ty + 0.5) : null;   // M4: a crawler under the cursor (shades only on lit tiles)
    if (cw) lines.unshift(describeCrawler(st, cw));
    // RI-04: a Stalker under the cursor; an active emergence point says whose rot comes out of it and where it goes
    const sk = st.flow ? stalkerAt(st, tx + 0.5, ty + 0.5) : null;
    if (sk) lines.unshift(describeStalker(st, sk));
    const ep = st.flow ? activeEmergencePoints(st).find(q => q.tx === tx && q.ty === ty) : undefined;
    if (ep) lines.unshift(`Emergence point ${ep.id} · rot from ${blockLabel(st, ep.block, debugView.coords)} comes out along this street toward ${blockLabel(st, ep.other, debugView.coords)}`);
    // the "walk closer" cursor: something to do here, out of reach
    const actionable = this.onFoot && (this.tool !== 'hand' || !!m || !!rubbleAt(st, tx, ty) || !!(light && light.l.why)
      || (!!st.flow && bi >= 0 && st.blocks[bi].state === DARK && isSubstationTile(st, tx, ty)));
    const far = actionable && !inReach(st, tx, ty, 1);
    if (far) lines.unshift(`Walk closer (reach ${REACH} tiles)`);
    const resource=rubbleAt(st,tx,ty);
    const concise=cw?describeCrawler(st,cw):sk?describeStalker(st,sk):m?`${KIND_LABEL[m.kind]} · ${this.statusLine(m)}`:resource?`${itemName(resource.type)} · ${Math.floor(resource.units)} remaining · hold left-click to mine`:'';
    this.hooks.onHoverText(st.campaign&&!debugView.coords?(this.tool!=='hand'||this.blueprintMode?null:concise||null):lines.join('\n'), rect.left + p.x, rect.top + p.y);
    const cur = far ? 'not-allowed' : 'default';
    if (cur !== this.cursor) { this.cursor = cur; this.input.setDefaultCursor(cur); }
  }

  /** RI-02 (§11.2 "stable named destinations"): the block's name (names.ts — bearing and district, "HQ", the rail
   *  yard) and its state; the block coordinates only behind the ` toggle. */
  private blockLine(i: number): string {
    const st = this.st, b = st.blocks[i];
    if (st.ruleset === 'exploration-v2') return `${blockLabel(st, i, debugView.coords)} · ${i === st.campaign?.homeBlock ? 'Home base' : b.state === HELD ? 'Station base' : 'Unrestored'}`;
    const river = st.lattice ? b.y === st.h - 1 : false;
    const state = river ? 'river' : b.state === DARK ? `Dark · rot ${Math.round(b.d * 100)} %` : b.state === CONTESTED ? 'Contested' : b.state === HELD ? (isInterior(st, i) ? 'Held · interior' : 'Held · front') : b.state === INERT || b.state === VOID ? 'inert' : '?';
    return `${blockLabel(st, i, debugView.coords)} · ${state}`;
  }

  /** RI-02: a machine's state word with its reason, glyph first ("▶ running · digging steel"). */
  private statusLine(m: Machine): string {
    const s = machineStatus(this.st, m);
    return `${STATUS_GLYPH[s.state]} ${s.state} · ${s.reason}`;
  }

  /** RI-02: the state mark on a machine — the tooltip's glyph as a shape at the top-left corner: running a right-pointing
   *  triangle, starved a ring, blocked a square, idle a bar, off a cross. Shape and colour together, so a state reads
   *  without colour (constitution, Phase 2). Drawing only. */
  private drawStatusMark(g: Phaser.GameObjects.Graphics, m: Machine, px: number, py: number, zoom: number): void {
    const s = machineStatus(this.st, m).state, col = STATUS_COL[s], r = 5 / Math.max(1, zoom * 0.75), x = px + 4 + r, y = py + 4 + r;
    g.fillStyle(0x05070d, 0.85); g.fillCircle(x, y, r + 3);
    switch (s) {
      case 'running': g.fillStyle(col, 1); g.fillTriangle(x - r * 0.7, y - r, x - r * 0.7, y + r, x + r, y); break;
      case 'starved': g.lineStyle(2 / Math.max(1, zoom * 0.75), col, 1); g.strokeCircle(x, y, r * 0.8); break;
      case 'blocked': g.fillStyle(col, 1); g.fillRect(x - r * 0.75, y - r * 0.75, r * 1.5, r * 1.5); break;
      case 'idle': g.fillStyle(col, 1); g.fillRect(x - r * 0.8, y - 1.5, r * 1.6, 3); break;
      case 'off': g.lineStyle(2 / Math.max(1, zoom * 0.75), col, 1); g.lineBetween(x - r * 0.7, y - r * 0.7, x + r * 0.7, y + r * 0.7); g.lineBetween(x - r * 0.7, y + r * 0.7, x + r * 0.7, y - r * 0.7); break;
    }
  }

  // ------------------------------------------------------------------ frame

  private hoverRefreshedAt=0;
  update(_time: number, delta: number): void {
    const cam = this.cameras.main, dt = Math.min(0.1, delta / 1000);
    const typing = uiInput.blocked || this.modifiedKey || !!document.activeElement?.closest('input, textarea, select, [contenteditable=true]');
    const right = !typing && (this.keys.D.isDown || this.keys.RIGHT.isDown), left = !typing && (this.keys.A.isDown || this.keys.LEFT.isDown);
    const down = !typing && (this.keys.S.isDown || this.keys.DOWN.isDown), up = !typing && (this.keys.W.isDown || this.keys.UP.isDown);
    if (this.onFoot) {
      // WASD walks the engineer (a sim command; walk.ts moves them with collision); the camera follows.
      // D-B1-5: Shift sprints, Space dodges; the rifle's aim follows the cursor while the button is held.
      this.sendWalk((right ? 1 : 0) - (left ? 1 : 0), (down ? 1 : 0) - (up ? 1 : 0));
      this.sendSprint(!typing && this.keys.SHIFT.isDown);
      if (!typing && Phaser.Input.Keyboard.JustDown(this.keys.SPACE)) queue(this.session, { type: 'dodge' });
      if(typing&&this.firing){this.firing=false;this.sendAim(null);}
      if (this.firing) this.sendAim(this.aimAt(this.input.activePointer));
      // Menus overlay the city. Their dimensions must not move the follow target.
      const e = this.st.engineer, gx = e.x * TILE_PX - cam.width / 2, gy = e.y * TILE_PX - cam.height / 2;
      if(left||right||up||down)this.viewingThreat=false;
      if(this.viewingThreat){ /* Hold the explicitly selected camera position. */ }
      else if (!this.snapped) { cam.setScroll(gx, gy); this.snapped = true; }
      else { const k = Math.min(1, FOLLOW_PER_S * dt); cam.setScroll(cam.scrollX + (gx - cam.scrollX) * k, cam.scrollY + (gy - cam.scrollY) * k); }
    } else {
      const pan = PAN_PX_PER_S * dt / cam.zoom;
      if (left) cam.scrollX -= pan;
      if (right) cam.scrollX += pan;
      if (up) cam.scrollY -= pan;
      if (down) cam.scrollY += pan;
    }
    const t0 = performance.now();
    if(!uiInput.blocked&&this.hoverTile&&!this.input.activePointer.isDown&&t0-this.hoverRefreshedAt>=150){this.hoverRefreshedAt=t0;this.onMove(this.input.activePointer,true);}
    this.draw();
    const ms = performance.now() - t0;
    this.drawMs += (ms - this.drawMs) * 0.05; if (ms > this.drawWorstMs) this.drawWorstMs = ms;
  }

  private draw(): void {
    const st = this.st, cam = this.cameras.main, G = ground(st);
    const tl = this.worldAt(0, 0), br = this.worldAt(cam.width, cam.height);
    const tx0 = Math.max(0, Math.floor(tl.x / TILE_PX)), ty0 = Math.max(0, Math.floor(tl.y / TILE_PX));
    const tx1 = Math.min(G.tw - 1, Math.ceil(br.x / TILE_PX)), ty1 = Math.min(G.th - 1, Math.ceil(br.y / TILE_PX));
    let n = 0;
    const cx0 = tx0 >> 5, cy0 = ty0 >> 5, cx1 = tx1 >> 5, cy1 = ty1 >> 5;
    for (let cy = cy0; cy <= cy1; cy++) for (let cx = cx0; cx <= cx1; cx++) {
      const frames = this.chunk(cx, cy);
      const ax = Math.max(tx0, cx * CHUNK), ay = Math.max(ty0, cy * CHUNK), bx = Math.min(tx1, cx * CHUNK + CHUNK - 1), by = Math.min(ty1, cy * CHUNK + CHUNK - 1);
      for (let ty = ay; ty <= by; ty++) for (let tx = ax; tx <= bx; tx++) {
        const frame = frames[(ty - cy * CHUNK) * CHUNK + (tx - cx * CHUNK)];
        let bob = this.bobs[n];
        if (!bob) { bob = this.blitter.create(tx * TILE_PX, ty * TILE_PX, frame); this.bobs.push(bob); }
        else { bob.setPosition(tx * TILE_PX, ty * TILE_PX); bob.setFrame(frame); bob.setVisible(true); }
        n++;
      }
    }
    for (let k = n; k < this.bobs.length; k++) if (this.bobs[k].visible) this.bobs[k].setVisible(false);
    this.drawn = n;

    // the blocks with tiles in view (their bounding boxes meet the window)
    const vis: number[] = [];
    for (let i = 0; i < G.blocks.length; i++) { const b = G.blocks[i]; if (b.tiles.length && b.x1 >= tx0 && b.x0 <= tx1 && b.y1 >= ty0 && b.y0 <= ty1) vis.push(i); }

    this.gPorts.clear();
    this.drawMachines(tx0, ty0, tx1, ty1, vis);
    this.refreshLight();
    this.paintViewLight();
    const lit = this.lightPix;

    // The block-state overlay on each lot: Dark navy that deepens with the rot, Contested amber flicker, Held rims.
    // M4: rot is tile presence in a Dark face — specks of it sit on a share of the lot's tiles equal to the rot (a hash
    // per tile, so the specks hold still); the specks are a reading of `b.d`, not a second rot model. M5: rot cannot
    // exist on a lit tile — neither the tint nor a speck is drawn where the light map is lit — and on a Contested lot
    // the specks burn off outward from every lit lamp as `contestProgress` runs (the light itself is the texture).
    // M1: lots are faces, so the tint runs along each row of the lot's tiles and the Held rim follows its boundary.
    const g = this.gOver;
    g.clear();
    const flicker = Math.sin(performance.now() / 45) > 0 ? 0.3 : 0.15;
    const authored=!!st.city?.mapId;
    const cls = new Uint8Array(st.blocks.length);   // 1 Dark, 2 Contested, 3 Held front, 4 Held interior
    for (const i of (authored?[]:vis)) { const s = st.blocks[i].state; cls[i] = s === DARK ? 1 : s === CONTESTED ? 2 : s === HELD ? (isInterior(st, i) ? 4 : 3) : 0; }
    const own = G.owner, tw = G.tw;
    for (let ty = ty0; ty <= ty1; ty++) {
      let run = -1, runX = 0;
      for (let tx = tx0; tx <= tx1 + 1; tx++) {
        const o = tx <= tx1 ? own[ty * tw + tx] : -1, c = o >= 0 && !lit[ty * tw + tx] ? cls[o] : 0, key = c === 1 || c === 2 ? o : -1;
        if (key !== run) {
          if (run >= 0) { const rc = cls[run]; if (rc === 1) g.fillStyle(0x0b1830, G.urban ? 0.12 + 0.15 * st.blocks[run].d : 0.25 + 0.35 * st.blocks[run].d); else g.fillStyle(0xd99a2b, flicker * 0.5); g.fillRect(runX * TILE_PX, ty * TILE_PX, (tx - runX) * TILE_PX, TILE_PX); }
          run = key; runX = tx;
        }
      }
    }
    // M4 rot specks on Dark lots (skipped zoomed far out, where the tint carries it). Rects, not circles, and at most
    // a third of the lot's tiles: the first soak drew a circle a tile as the rot deepened and halved the frame rate.
    if (cam.zoom >= 0.6) {
      // M5: a Contested lot's lit lamps and its burn-off progress, for the sweep (specks within p·SWEEP_R of a lit lamp are gone)
      const sweep = new Map<number, { p: number; lamps: [number, number][] }>();
      for (const i of vis) if (cls[i] === 2) sweep.set(i, { p: contestProgress(st, i), lamps: blockLights(st, i).filter(l => l.lit).map(l => [l.tx, l.ty]) });
      g.fillStyle(0x2a1a4a, 0.75);
      for (let ty = ty0; ty <= ty1; ty++) for (let tx = tx0; tx <= tx1; tx++) {
        const o = own[ty * tw + tx];
        if (o < 0 || (cls[o] !== 1 && cls[o] !== 2) || lit[ty * tw + tx]) continue;
        const h = (Math.imul(tx, 73856093) ^ Math.imul(ty, 19349663)) >>> 0;
        if ((h % 1000) / 1000 >= Math.min(0.34, st.blocks[o].d)) continue;
        const sw = sweep.get(o);
        if (sw) {
          const reach = sw.p * SWEEP_R;
          let gone = false;
          for (const [lx, ly] of sw.lamps) if ((tx - lx) * (tx - lx) + (ty - ly) * (ty - ly) <= reach * reach) { gone = true; break; }
          if (gone) continue;
        }
        const r = 3 + (h >>> 5) % 3;
        g.fillRect((tx + 0.3 + 0.4 * ((h >>> 10) % 100) / 100) * TILE_PX - r, (ty + 0.3 + 0.4 * ((h >>> 20) % 100) / 100) * TILE_PX - r, 2 * r, 2 * r);
      }
    }
    // Layout pass (ROADMAP §2 "street surface and kerb line"): the street surface is the tileset's own T_STREET;
    // the kerb is drawn here — a thin pale line on every edge where a lot tile meets a street tile, so the street
    // reads as a street rather than as a gap between lots. Screen-constant width, and not drawn against the river
    // (owner -2), which has its own bank. GAME-ASSUMPTION: drawing only; nothing reads it.
    g.lineStyle(1.5 / cam.zoom, 0x8d95a6, st.city?.mapId?0:0.45);
    for (let ty = ty0; ty <= ty1; ty++) for (let tx = tx0; tx <= tx1; tx++) {
      const o = own[ty * tw + tx];
      if (o < 0) continue;
      const px = tx * TILE_PX, py = ty * TILE_PX;
      if (ty > 0 && own[(ty - 1) * tw + tx] === -1) g.lineBetween(px, py, px + TILE_PX, py);
      if (ty < G.th - 1 && own[(ty + 1) * tw + tx] === -1) g.lineBetween(px, py + TILE_PX, px + TILE_PX, py + TILE_PX);
      if (tx > 0 && own[ty * tw + tx - 1] === -1) g.lineBetween(px, py, px, py + TILE_PX);
      if (tx < tw - 1 && own[ty * tw + tx + 1] === -1) g.lineBetween(px + TILE_PX, py, px + TILE_PX, py + TILE_PX);
    }
    // Held rims: the lot's boundary edges, white for interior, amber for the front
    for (let ty = ty0; ty <= ty1; ty++) for (let tx = tx0; tx <= tx1; tx++) {
      const o = own[ty * tw + tx];
      if (o < 0 || cls[o] < 3) continue;
      const px = tx * TILE_PX, py = ty * TILE_PX;
      g.lineStyle((G.urban ? 1.5 : 4) / cam.zoom, cls[o] === 4 ? 0xffffff : 0xe8a93a, 0.6);
      if (ty === 0 || own[(ty - 1) * tw + tx] !== o) g.lineBetween(px, py, px + TILE_PX, py);
      if (ty === G.th - 1 || own[(ty + 1) * tw + tx] !== o) g.lineBetween(px, py + TILE_PX, px + TILE_PX, py + TILE_PX);
      if (tx === 0 || own[ty * tw + tx - 1] !== o) g.lineBetween(px, py, px, py + TILE_PX);
      if (tx === tw - 1 || own[ty * tw + tx + 1] !== o) g.lineBetween(px + TILE_PX, py, px + TILE_PX, py + TILE_PX);
    }
    // one label per block whose pole tile is in view, kept small on screen
    let li = 0;
    for (const i of vis) {
      if (authored&&!debugView.coords)continue;
      if (G.urban && !debugView.coords && (G.blocks[i].hq || this.st.hops[i] > 2)) continue;
      const p = G.blocks[i].pole;
      if (p[0] < tx0 || p[0] > tx1 || p[1] < ty0 || p[1] > ty1) continue;
      let t = this.labels[li];
      if (!t) { t = this.add.text(0, 0, '', { fontSize: '12px', color: '#c7cfe0', backgroundColor: '#0b0e1aaa', padding: { x: 4, y: 2 } }).setDepth(5); this.labels.push(t); }
      const place = G.urban?.places.find(p => p.block === i);
      t.setFontFamily('Segoe UI, sans-serif').setFontSize(14);
      t.setText(G.urban && !debugView.coords ? (st.campaign&&!knownDistrict(st,i)?'Uncharted district':blockName(st,i)) : this.blockLine(i))
        .setPosition((place ? place.pad.x : p[0]) * TILE_PX + 6, (place ? place.pad.y - 1 : p[1]) * TILE_PX + 6).setScale(1 / cam.zoom).setVisible(true);
      li++;
    }
    for(const p of st.campaign?.navigation?.pins??[]){if(p.x<tx0||p.x>tx1||p.y<ty0||p.y>ty1)continue;g.lineStyle(2/cam.zoom,0xe3b96f,1);g.strokeCircle(p.x*TILE_PX,p.y*TILE_PX,6/cam.zoom);const label=this.labels[li]??(this.labels[li]=this.add.text(0,0,'',{fontSize:'14px',color:'#f3ecdd',backgroundColor:'#151d1ef2',wordWrap:{width:260}}).setDepth(5));label.setText(p.name).setPosition(p.x*TILE_PX+8/cam.zoom,p.y*TILE_PX).setScale(1/cam.zoom).setVisible(true);li++;}
    if (st.campaign?.expansion && (!authored||debugView.coords)) {
    for(const site of st.campaign?.progression?.sites??[]){
      if(site.kind==='core'&&!site.seen){g.lineStyle(2/cam.zoom,0xc99bfa,.65);g.strokeCircle(site.searchX*TILE_PX,site.searchY*TILE_PX,CORRECTIONS.coreSearchRadius*TILE_PX);g.lineStyle(2/cam.zoom,0xc99bfa,.9);g.lineBetween((site.searchX-1)*TILE_PX,site.searchY*TILE_PX,(site.searchX+1)*TILE_PX,site.searchY*TILE_PX);g.lineBetween(site.searchX*TILE_PX,(site.searchY-1)*TILE_PX,site.searchX*TILE_PX,(site.searchY+1)*TILE_PX);}
      if(!site.seen||site.x<tx0||site.x>tx1||site.y<ty0||site.y>ty1)continue;const colour=site.kind==='core'?(site.recovered?0x617470:0xc985fa):site.kind==='plant'?0x74debc:0xe8c878;g.lineStyle(2/cam.zoom,colour,.95);g.fillStyle(colour,.18);g.fillRect(site.x*TILE_PX,site.y*TILE_PX,site.size*TILE_PX,site.size*TILE_PX);g.strokeRect(site.x*TILE_PX,site.y*TILE_PX,site.size*TILE_PX,site.size*TILE_PX);if(site.kind==='core'&&site.enabled&&!site.recovered){
      const cx=site.x*TILE_PX,cy=site.y*TILE_PX,pulse=.3+.15*Math.sin(st.t*5);
      g.lineStyle(2/cam.zoom,0xc985fa,pulse);g.strokeCircle(cx,cy,CORRECTIONS.alienEquipmentRange*TILE_PX);
      g.fillStyle(0xe2b6ff,.8);g.fillCircle((site.x+1.5)*TILE_PX,(site.y+1.5)*TILE_PX,TILE_PX*.35);
      if(st.engineer.down<0&&Math.hypot(st.engineer.x-site.x,st.engineer.y-site.y)<CORRECTIONS.alienEquipmentRange){g.lineStyle(2/cam.zoom,0xe2b6ff,.8);g.lineBetween((site.x+1.5)*TILE_PX,(site.y+1.5)*TILE_PX,st.engineer.x*TILE_PX,st.engineer.y*TILE_PX);}
    }let text=this.labels[li];if(!text){text=this.add.text(0,0,'',{fontSize:'12px',color:'#d7e7e4',backgroundColor:'#102421dd'}).setDepth(8);this.labels.push(text);}text.setText(site.name+' / E'+(site.kind==='core'?(site.recovered?' · source offline':' · powered relay'):site.kind==='plant'?(site.installed?' · commissioned':' · needs core'):''));text.setPosition(site.x*TILE_PX,(site.y-1)*TILE_PX);text.setVisible(true);li++;}
    for(const resource of st.campaign?.progression?.resources??[]){if(resource.remaining<=0||resource.x<tx0||resource.x>tx1||resource.y<ty0||resource.y>ty1)continue;g.fillStyle(resource.item==='crude'?0x594965:resource.item==='ironore'?0x93a1ad:0xc68955,.85);g.fillRect(resource.x*TILE_PX,resource.y*TILE_PX,3*TILE_PX,3*TILE_PX);let text=this.labels[li];if(!text){text=this.add.text(0,0,'',{fontSize:'12px',color:'#e9e5d0',backgroundColor:'#14242bdd'}).setDepth(8);this.labels.push(text);}text.setText(resource.item+' · '+Math.ceil(resource.remaining));text.setPosition(resource.x*TILE_PX,(resource.y-1)*TILE_PX);text.setVisible(true);li++;}
      for (const id of SITE_IDS) {
        if(!knownSite(st,id))continue;
        const site=campaignSite(st,id)!; const name=(campaignDiscoveries(st).find(s=>s.id===id)?.title??id)+' / E';
        if (site.x < tx0 || site.x > tx1 || site.y < ty0 || site.y > ty1) continue;
        let label = this.labels[li]; if (!label) { label = this.add.text(0, 0, '', { fontSize: '12px', color: '#f2d38b', backgroundColor: '#0b0e1aaa', padding: { x: 4, y: 2 } }).setDepth(5); this.labels.push(label); }
        label.setText(name).setPosition(site.x * TILE_PX, (site.y - (id==='workshop'?2:1)) * TILE_PX).setScale(1 / cam.zoom).setVisible(true); li++;
      }
    }
    const hall=st.campaign?.turbine;
    if(hall&&hall.seenAt>=0&&(!authored||cityVisible(st,hall.x+2,hall.y+2))){
      g.fillStyle(hall.restoredAt>=0&&hall.enabled?0x589f9e:0x655a51,1);g.fillRect(hall.x*TILE_PX,hall.y*TILE_PX,hall.size*TILE_PX,hall.size*TILE_PX);
      g.lineStyle(2/cam.zoom,0xbadbd7,1);g.strokeRect(hall.x*TILE_PX,hall.y*TILE_PX,hall.size*TILE_PX,hall.size*TILE_PX);
      const label=this.labels[li]??(this.labels[li]=this.add.text(0,0,'',{fontSize:'12px',color:'#dcece9',backgroundColor:'#10191de0'}).setDepth(15));
      label.setText(`${navigationName(st,`site:${hall.id}`,'Turbine hall')} / ${hall.restoredAt<0?'RESTORE':hall.enabled?'ON':'OFF'} / E`).setPosition(hall.x*TILE_PX,(hall.y-1)*TILE_PX).setScale(1/cam.zoom).setVisible(true);li++;
    }
    for(const s of st.campaign?.recruits?.sites??[]) {
      if(!cityVisible(st,s.x+.5,s.y+.5)||s.seenAt<0||s.x<tx0||s.x>tx1||s.y<ty0||s.y>ty1)continue;
      let label=this.labels[li];if(!label){label=this.add.text(0,0,'',{fontSize:'12px',color:'#c5e4ff',backgroundColor:'#0b0e1add',padding:{x:4,y:2}}).setDepth(5);this.labels.push(label);}
      label.setText(`${navigationName(st,`site:${s.id}`,RECRUITS[s.kind].name)} / ${s.recruitedAt>=0?'RECRUITED':'E'}`).setPosition(s.x*TILE_PX,(s.y-1)*TILE_PX).setScale(1/cam.zoom).setVisible(true);li++;
      g.fillStyle(s.recruitedAt>=0?0x526c67:0x97cdff,1);g.fillRect((s.x+.1)*TILE_PX,(s.y+.1)*TILE_PX,.8*TILE_PX,.8*TILE_PX);
    }
    const discovery=st.campaign?.discovery;
    if(discovery&&cityVisible(st,discovery.x+.5,discovery.y+.5)&&discovery.seenAt>=0&&discovery.x>=tx0&&discovery.x<=tx1&&discovery.y>=ty0&&discovery.y<=ty1){
      const d=discovery;let label=this.labels[li];if(!label){label=this.add.text(0,0,'',{fontSize:'12px',color:'#9de9d2',backgroundColor:'#0b0e1add',padding:{x:4,y:2}}).setDepth(5);this.labels.push(label);}
      label.setText(navigationName(st,`site:${d.id}`,'Workshop records')+(d.recoveredAt>=0?' / EMPTY':' / E')).setPosition(d.x*TILE_PX,(d.y-2)*TILE_PX).setScale(1/cam.zoom).setVisible(true);li++;
      g.fillStyle(d.recoveredAt>=0?0x526c67:0x75d3b7,1);g.fillRect((d.x+.15)*TILE_PX,(d.y+.15)*TILE_PX,.7*TILE_PX,.7*TILE_PX);
      g.lineStyle(2/cam.zoom,0x102c29,1);g.strokeRect((d.x+.3)*TILE_PX,(d.y+.25)*TILE_PX,.4*TILE_PX,.5*TILE_PX);
    }
    for(const core of st.campaign?.defence?.bases??[]) {
      if(authored&&core.hp>=300)continue;
      if(core.x<tx0||core.x>tx1||core.y<ty0||core.y>ty1)continue;
      let label=this.labels[li];if(!label){label=this.add.text(0,0,'',{fontSize:'12px',color:'#f2d38b',backgroundColor:'#0b0e1aaa',padding:{x:4,y:2}}).setDepth(5);this.labels.push(label);}
      // Keep the health label above the building, clear of the engineer at its entrance.
      label.setText(`CORE ${Math.ceil(core.hp)}/300${core.hp===0?' DISABLED / E repairs':''}`).setPosition(core.x*TILE_PX,core.y*TILE_PX-16/cam.zoom).setScale(1/cam.zoom).setVisible(true);li++;
    }
    if(authored){for(const b of RIVERFRONT_BUILDINGS){if(b.x>tx1||b.x+b.w<tx0||b.y>ty1||b.y+b.h<ty0||b.kind==='house')continue;const label=this.labels[li]??(this.labels[li]=this.add.text(0,0,'',{fontSize:'12px',color:'#e8d5ac',backgroundColor:'#182528df',padding:{x:5,y:3}}).setDepth(5));label.setText(b.name).setPosition((b.x+b.w/2)*TILE_PX,(b.y+b.h+.8)*TILE_PX).setOrigin(.5,0).setScale(1/cam.zoom).setVisible(true);li++;}}
    for (let k = li; k < this.labels.length; k++) this.labels[k].setVisible(false);
    if (!st.flow) {
      // GAME-ASSUMPTION: without the flow layer (?flow=0) the HQ is a 6×6-tile slab where the Depot would be
      const d = depotRect(st);
      g.fillStyle(0x0b0e1a, 0.85); g.fillRect(d.x * TILE_PX, d.y * TILE_PX, d.size * TILE_PX, d.size * TILE_PX);
      g.lineStyle(2 / cam.zoom, 0xffffff, 0.8); g.strokeRect(d.x * TILE_PX, d.y * TILE_PX, d.size * TILE_PX, d.size * TILE_PX);
    }
    this.drawKerbPips(g);
    if(authored){this.gUrban.setDepth(.5);drawRiverfront(this.gUrban,this.gRoof,st,cam);}else{this.gRoof.clear();drawUrban(this.gUrban, st, G, vis, cam.zoom);}
    if (this.tool === 'arclamp' || this.tool === 'lamp' || this.tool === 'floodlight' || this.tool === 'rifle') {
      // Binary simulation coverage, not the rendering falloff. In particular an unlit Shade
      // interaction never relies on judging the brightness of a pavement or a window.
      g.lineStyle(1 / cam.zoom, 0xffe6a6, 0.55);
      for (let y = ty0; y <= ty1; y++) for (let x = tx0; x <= tx1; x++) {
        if (!lit[y * tw + x]) continue;
        const px = x * TILE_PX, py = y * TILE_PX;
        if (!lit[(y - 1) * tw + x]) g.lineBetween(px, py, px + TILE_PX, py);
        if (!lit[(y + 1) * tw + x]) g.lineBetween(px, py + TILE_PX, px + TILE_PX, py + TILE_PX);
        if (!lit[y * tw + x - 1]) g.lineBetween(px, py, px, py + TILE_PX);
        if (!lit[y * tw + x + 1]) g.lineBetween(px + TILE_PX, py, px + TILE_PX, py + TILE_PX);
      }
    }
    const inspected=inspectionView.machineId===null?null:machineById(st,inspectionView.machineId);
    if(inspected?.kind==='turret')this.drawTurretRange(g,inspected.x+inspected.size/2,inspected.y+inspected.size/2);
    for(const order of this.st.campaign?.plans?.orders??[])if(order.status==='waiting')for(const e of order.blueprint.entities){
      const [w,h]=dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind]),tx=order.x+e.x,ty=order.y+e.y;
      if(tx>tx1||ty>ty1||tx+w<tx0||ty+h<ty0||blueprintEntityBuilt(this.st,order,e))continue;
      const x=tx*TILE_PX,y=ty*TILE_PX;
      g.fillStyle(0x67c9ff,.08);g.fillRect(x,y,w*TILE_PX,h*TILE_PX);g.lineStyle(1/this.zoom,0x67c9ff,.65);g.strokeRect(x+2,y+2,w*TILE_PX-4,h*TILE_PX-4);
      // Corner marks distinguish a saved plan from a live machine or paid preview.
      g.lineStyle(3/this.zoom,0x67c9ff,.9);g.lineBetween(x,y,x+6,y);g.lineBetween(x,y,x,y+6);g.lineBetween(x+w*TILE_PX,y+h*TILE_PX,x+w*TILE_PX-6,y+h*TILE_PX);
      this.arrow(g,x+w*TILE_PX/2,y+h*TILE_PX/2,e.dir,8,0x67c9ff,.6);
    }
    this.drawGhost(g);
    this.drawThreat();
    this.drawEngineer();

    // The HUD is four corner overlays (ROADMAP §2). `setScrollFactor(0)` still zooms about the camera centre, so
    // every one is pinned at screen scale through `pin`.
    const pin = (sx: number, sy: number): [number, number] => [cam.width / 2 + (sx - cam.width / 2) / cam.zoom, cam.height / 2 + (sy - cam.height / 2) / cam.zoom];
    if(!st.campaign){
    const f = this.focusBlock(), fi = idxOf(st, f[0], f[1]), e = st.engineer;
    // D5: the HP bar and number only when below full; M4: the crawlers on the tile layer and how many have turned on you
    const th = st.flow?.threat, onYou = th ? th.crawlers.filter(c => c.onPlayer).length : 0;
    const threatLine = th && threatActive(st) && th.crawlers.length ? ` · crawlers ${th.crawlers.length}${onYou ? ` (${onYou} on you)` : ''}` : '';
    const rounds = Math.floor((e.inv.magazine ?? 0) * ROUNDS_PER_MAG);
    const inHand = this.tool === 'hand' ? 'empty hand (hold left-click on rubble to mine it; click a turret or Generator to feed it; E interacts)'
      : this.tool === 'rifle' ? `rifle (hold left-click to fire toward the cursor, ${RIFLE_RANGE} tiles; Q puts it away) · ${rounds} round${rounds === 1 ? '' : 's'} in the pockets${rounds ? '' : ' — take magazines from the chest (E on the Depot)'}`
      : `${this.tool} → ${DIR_NAMES[this.dir]}`;
    const moveLine = this.onFoot ? 'M map view (inspect stations; Shift-click to pin) · WASD move · Shift sprint · Space dodge · Tab/I pockets · wheel zoom' : 'M map view · drag / WASD pan · wheel zoom';

    // top-left: the key strip (STANDARDS 4.3, the keys visible rather than in a tooltip) and what is in the hand,
    // wrapped to a little over half the viewport so it never runs under the territory block opposite it
    const wrap = Math.round((cam.width - hudInset.right) * KEY_STRIP_FRAC);
    this.terrText.setVisible(hudInset.right===0);
    const directions = G.urban && this.cg ? G.urban.places.filter(p => (p.role === 'rail' || p.role === 'residential') && this.cg!.blocks[this.cg!.hq].nb.includes(p.block))
      .map(p => `${bearingOf(st, this.cg!.hq, p.block)} · ${blockName(st,p.block)}`).slice(0, 2).join('     /     ') : '';
    if (this.keyWrap !== wrap) { this.keyWrap = wrap; this.keyText.setWordWrapWidth(wrap); }
    this.keyText.setScale(1 / cam.zoom).setPosition(...pin(8, 8 + hudInset.top))
      .setText(G.urban ? `${FACTORY_TEXT.worldKeys}\n${this.tool === 'hand' ? FACTORY_TEXT.miningHand : FACTORY_TEXT.inHand(inHand)} ${this.ghostReason || ''}\n${directions}` : `${moveLine} (${ZOOM_MIN}–${ZOOM_MAX}×) · P pause${st.flow ? `\n${TOOL_KEY_LINE}` : ''}${st.flow ? `\nIn hand: ${inHand}${this.ghostReason ? ` · ${this.ghostReason}` : ''}` : ''}`);

    // top-right: territory — the block under the engineer, what the view holds, the zoom
    this.terrText.setScale(1 / cam.zoom).setPosition(...pin(cam.width - 8 - hudInset.right, 8 + hudInset.top))
      .setText(`World view · ${n} tiles · zoom ${cam.zoom.toFixed(2)}×${fi >= 0 ? `\n${this.blockLine(fi)}` : ''}`);

    // bottom-left: power and pockets (the two stocks that decide the hour)
    const pockets = this.onFoot ? `Pockets ${invStacks(e.inv)}/${INV_STACKS} stacks${e.inv.kit ? ` · ${e.inv.kit} kit${e.inv.kit === 1 ? '' : 's'}` : ''}${e.hp < ENGINEER_HP ? ` · HP ${Math.round(e.hp)}/${ENGINEER_HP}` : ''}${threatLine}` : threatLine.replace(/^ · /, '');
    const power = this.powerLine().replace(/^\n/, '');
    if (this.powWrap !== wrap) { this.powWrap = wrap; this.powText.setWordWrapWidth(wrap); }
    this.powText.setScale(1 / cam.zoom).setPosition(...pin(8, cam.height - 8 - hudInset.bottom))
      .setText([pockets, power].filter(Boolean).join('\n') || ' ');

    // bottom-right: the clock and the speed the sim is running at (P pauses, - / = change it)
    this.clockText.setScale(1 / cam.zoom).setPosition(...pin(cam.width - 8 - hudInset.right, cam.height - 8 - hudInset.bottom))
      .setText(`${clock(st.t)}\n${st.speed <= 0 ? 'PAUSED (P)' : ''}`);
    }else{this.keyText.setVisible(false);this.terrText.setVisible(false);this.powText.setVisible(false);this.clockText.setVisible(false);}
    // the Depot beacon (STANDARDS C.2: the landmark reads from the far edge of the viewport): when the Depot is out
    // of view, its direction and distance sit on the edge of the screen nearest it
    if (st.flow && !st.campaign) {
      const d = depotRect(st), dcx = (d.x + d.size / 2) * TILE_PX, dcy = (d.y + d.size / 2) * TILE_PX, wv = cam.worldView;
      if (wv.width > 0 && !wv.contains(dcx, dcy)) {
        const dx = dcx - wv.centerX, dy = dcy - wv.centerY, m = 44 / cam.zoom;
        const k = Math.min((wv.width / 2 - m) / Math.max(1e-6, Math.abs(dx)), (wv.height / 2 - m) / Math.max(1e-6, Math.abs(dy)));
        const sx = (wv.centerX + dx * k - wv.x) * cam.zoom, sy = (wv.centerY + dy * k - wv.y) * cam.zoom;
        const glyph = Math.abs(dx) > Math.abs(dy) ? (dx > 0 ? '▶' : '◀') : (dy > 0 ? '▼' : '▲');
        const tiles = Math.round(Math.hypot(dx, dy) / TILE_PX);
        this.beaconText.setScale(1 / cam.zoom).setPosition(...pin(sx, sy)).setText(`${glyph} Depot · ${tiles} tiles`).setVisible(true);
      } else this.beaconText.setVisible(false);
    } else this.beaconText.setVisible(false);
  }

  /** Layout pass (STANDARDS B.3, B.6; ROADMAP §2 "front-segment pip state on the kerb"): the map's edge pip on the
   *  kerb — one per live segment at the street's midpoint, at screen scale so it reads at any zoom: ● green at least
   *  half full, ▲ amber running low, ✕ red empty, blinking. The same `pipOf` the map and the panel use, so the three
   *  never disagree. It was the HQ's segments alone; every Held block's front now carries its own, so a claimed block
   *  reads the same as the one you started on. */
  private drawKerbPips(g: Phaser.GameObjects.Graphics): void {
    const st = this.st, cg = this.cg;
    if (!cg || !st.flow) return;
    const cam = this.cameras.main, zoom = cam.zoom, wv = cam.worldView;
    const blink = Math.floor(performance.now() / 260) % 2 === 0, r = 7 / zoom;
    for (const e of st.ring) {
      if (st.blocks[e.a].state !== HELD) continue;
      const sg = segBetween(cg, e.a, e.b);
      if (!sg) continue;
      const pip = pipOf(e.hopper / edgeCap(st, e)), x = sg.mx * TILE_PX, y = sg.my * TILE_PX;
      if (x < wv.x - 32 || x > wv.right + 32 || y < wv.y - 32 || y > wv.bottom + 32) continue;
      g.fillStyle(0x0b0e1a, 0.8); g.fillCircle(x, y, r * 1.7);
      if (pip === 'green') { g.fillStyle(0x3ddc84, 1); g.fillCircle(x, y, r); }
      else if (pip === 'amber') { g.fillStyle(0xf2b632, 1); g.fillTriangle(x, y - r, x - r, y + r * 0.8, x + r, y + r * 0.8); }
      else if (blink) { g.lineStyle(3 / zoom, 0xff3b30, 1); g.lineBetween(x - r, y - r, x + r, y + r); g.lineBetween(x - r, y + r, x + r, y - r); }
    }
  }

  /** M5: re-read the lit set from the sim (at most eight times a second) and repaint the texture when it changed. */
  private cityLightPhase=-1;
  private refreshLight(): void {
    if(this.st.city?.mapId)return; // The authored city uses the culled local presentation mask.
    const now = performance.now();
    if (now - this.lightAtMs < LIGHT_REFRESH_MS) return;
    this.lightAtMs = now;
    const st = this.st, G = ground(st), m = lightMask(st, this.lightScratch);
    const phase=st.city?.mapId?(st.t%1200<900?1:0):-1;
    let changed = phase!==this.cityLightPhase;this.cityLightPhase=phase;
    for (let i = 0; i < m.length; i++) if (m[i] !== this.lightPix[i]) { changed = true; break; }
    if(changed){this.lightPix.set(m);this.paintLight(this.lightPix);}
    const ms=performance.now()-now;this.renderTiming.lightChecks++;this.renderTiming.lightMs+=ms;this.renderTiming.lightMaxMs=Math.max(this.renderTiming.lightMaxMs,ms);
  }
  /** Compose only the visible lighting surface each frame; aiming never repaints the city-sized light mask. */
  private paintViewLight():void {
    const cam=this.cameras.main,tl=this.worldAt(0,0),br=this.worldAt(cam.width,cam.height),scale=4;
    const x=Math.floor(tl.x/TILE_PX)-1,y=Math.floor(tl.y/TILE_PX)-1,w=Math.ceil(br.x/TILE_PX)-x+1,h=Math.ceil(br.y/TILE_PX)-y+1;
    if(this.viewLightTex.width!==w*scale||this.viewLightTex.height!==h*scale)this.viewLightTex.setSize(w*scale,h*scale);
    const ctx=this.viewLightTex.getContext();ctx.globalCompositeOperation='source-over';ctx.globalAlpha=1;ctx.fillStyle='#424e56';ctx.fillRect(0,0,w*scale,h*scale);
    ctx.drawImage(this.lightTex.getSourceImage() as HTMLCanvasElement,x,y,w,h,0,0,w*scale,h*scale);
    const e=this.st.engineer;
    if(this.st.city?.mapId){if(!uiInput.blocked&&this.hoverTile){const aim=this.aimAt(this.input.activePointer);this.flashlightAngle=Math.atan2(aim[1]-e.y,aim[0]-e.x);}paintRiverfrontLight(ctx,this.st,x,y,w,h,scale,{on:this.handLamp&&this.onFoot&&!e.truckSeat,angle:this.flashlightAngle,range:FLASHLIGHT_RANGE,half:FLASHLIGHT_HALF_ANGLE});this.viewLightTex.refresh();this.viewLightImage.setPosition(x*TILE_PX,y*TILE_PX).setDisplaySize(w*TILE_PX,h*TILE_PX);return;}
    if(this.handLamp&&this.onFoot&&e.down<0&&!e.truckSeat){
      if(!uiInput.blocked&&this.hoverTile){const aim=this.aimAt(this.input.activePointer),dx=aim[0]-e.x,dy=aim[1]-e.y;if(Math.hypot(dx,dy)>.1)this.flashlightAngle=Math.atan2(dy,dx);}
      const ex=(e.x-x)*scale,ey=(e.y-y)*scale,r=FLASHLIGHT_RANGE*scale;
      ctx.globalCompositeOperation='screen';
      const gradient=ctx.createRadialGradient(ex,ey,0,ex,ey,r);gradient.addColorStop(0,'rgba(255,245,218,0.95)');gradient.addColorStop(.5,'rgba(255,245,218,0.8)');gradient.addColorStop(1,'rgba(255,245,218,0)');ctx.fillStyle=gradient;
      // Narrow sectors fade the edges of the cone as well as its distance falloff.
      const sectors=48;
      for(let i=0;i<sectors;i++){const a=this.flashlightAngle-FLASHLIGHT_HALF_ANGLE+2*FLASHLIGHT_HALF_ANGLE*i/sectors,b=a+2*FLASHLIGHT_HALF_ANGLE/sectors;ctx.globalAlpha=Math.sin(Math.PI*(i+.5)/sectors);ctx.beginPath();ctx.moveTo(ex,ey);ctx.arc(ex,ey,r,a,b);ctx.closePath();ctx.fill();}
      ctx.globalAlpha=1;const halo=ctx.createRadialGradient(ex,ey,0,ex,ey,scale*1.2);halo.addColorStop(0,'rgba(255,245,218,0.7)');halo.addColorStop(1,'rgba(255,245,218,0)');ctx.fillStyle=halo;ctx.fillRect(ex-scale*1.2,ey-scale*1.2,scale*2.4,scale*2.4);
    }
    ctx.globalAlpha=1;ctx.globalCompositeOperation='source-over';this.viewLightTex.refresh();this.viewLightImage.setPosition(x*TILE_PX,y*TILE_PX).setDisplaySize(w*TILE_PX,h*TILE_PX);
  }
  /** Layout pass (ROADMAP §2 "soft light falloff"): the lit edge was a hard texel step, so a lamp's reach ended on a
   *  straight line. The mask is blurred with two passes of [1,2,1]/4 and an unlit texel is lerped that far towards
   *  white; a lit texel is still full white. GAME-ASSUMPTION: the blur is a render choice — `lightPix` is untouched,
   *  `lightMask`/`litAt` still decide what is lit, so what a shade may stand on and what burns off is unchanged.
   *  The texture is one texel per tile drawn at `TILE_PX`, and `antialias` is on, so the card's bilinear filter
   *  carries the ramp the rest of the way. */
  private paintLight(mask: Uint8Array): void {
    const started=performance.now();
    const G = ground(this.st), tw = G.tw, th = G.th, n = tw * th;
    const ctx = this.lightTex.getContext(), img = ctx.createImageData(tw, th), d = img.data;
    const soft = this.lightSoft, tmp = this.lightBlur;
    for (let pass = 0; pass < 2; pass++) {
      const src: ArrayLike<number> = pass === 0 ? mask : soft;
      for (let y = 0; y < th; y++) for (let x = 0; x < tw; x++) {   // horizontal
        const i = y * tw + x;
        tmp[i] = (src[x > 0 ? i - 1 : i] + 2 * src[i] + src[x < tw - 1 ? i + 1 : i]) / 4;
      }
      for (let y = 0; y < th; y++) for (let x = 0; x < tw; x++) {   // vertical
        const i = y * tw + x;
        soft[i] = (tmp[y > 0 ? i - tw : i] + 2 * tmp[i] + tmp[y < th - 1 ? i + tw : i]) / 4;
      }
    }
    const ambient = this.st.city?.mapId?(this.st.t%1200<900?[205,210,197]:[86,100,110]):G.urban ? [66,78,86] : UNLIT_RGB;
    for (let i = 0, j = 0; i < n; i++, j += 4) {
      const v = mask[i] ? 1 : Math.min(0.6, soft[i] * LIGHT_SOFT);
      d[j] = ambient[0] + (255 - ambient[0]) * v;
      d[j + 1] = ambient[1] + (255 - ambient[1]) * v;
      d[j + 2] = ambient[2] + (255 - ambient[2]) * v;
      d[j + 3] = 255;
    }
    ctx.putImageData(img, 0, 0);
    this.lightTex.refresh();
    const ms=performance.now()-started;this.renderTiming.lightPaints++;this.renderTiming.lightPaintMs+=ms;this.renderTiming.lightPaintMaxMs=Math.max(this.renderTiming.lightPaintMaxMs,ms);
  }

  /** D5 on foot: the engineer (a disc, red while down), their path, and the reach ring — faint, brighter while the
   *  cursor is inside it; D-B1-5: the stamina bar under the HP bar (a tick at the one-dodge floor) and, with the
   *  rifle in hand, the aim line to the cursor out to the rifle's range. GAME-ASSUMPTION: a code-drawn disc stands
   *  in for the engineer sprite until the art pass. */
  private drawEngineer(): void {
    const g = this.gEng, st = this.st;
    g.clear();
    if (!this.onFoot) return;
    const e = st.engineer, zoom = this.cameras.main.zoom,tram=st.city?.mapId&&st.campaign?.progression?.passenger?st.flow?.machines.find(m=>m.id===st.campaign!.fixedTram!.tram):undefined,pose=tram?cityTramVisualPose(tram,st.speed>0):e,ex=pose.x*TILE_PX,ey=pose.y*TILE_PX;
    const truck=st.campaign?.truck;
    if(truck){const r=truckRect(truck),x=r.x*TILE_PX,y=r.y*TILE_PX,w=r.w*TILE_PX,hh=r.h*TILE_PX;
      g.fillStyle(0x121820,1);g.fillRect(x,y,w,hh);g.fillStyle(0xb99144,1);g.fillRect(x+3,y+3,w-6,hh-6);
      g.lineStyle(2/zoom,0xf8dc85,1);g.strokeRect(x+3,y+3,w-6,hh-6);
      const dx=[0,1,0,-1][truck.dir],dy=[-1,0,1,0][truck.dir],cx=truck.x*TILE_PX,cy=truck.y*TILE_PX;
      g.fillStyle(0x18222c,1);
      if(dx){for(const xx of [x+8,x+w-14]){g.fillRect(xx,y,8,6);g.fillRect(xx,y+hh-6,8,6);}g.lineBetween(cx+dx*TILE_PX*.4,y+6,cx+dx*TILE_PX*.4,y+hh-6);}
      else{for(const yy of [y+8,y+hh-14]){g.fillRect(x,yy,6,8);g.fillRect(x+w-6,yy,6,8);}g.lineBetween(x+6,cy+dy*TILE_PX*.4,x+w-6,cy+dy*TILE_PX*.4);}
      g.fillStyle(0x82c8d8,1);g.fillRect(cx+dx*TILE_PX*.8-(dx?4:12),cy+dy*TILE_PX*.8-(dy?4:12),dx?8:24,dy?8:24);
    }
    const path = currentPath(st);
    if (path) {
      g.lineStyle(2 / zoom, 0xffffff, 0.25);
      let lx = ex, ly = ey;
      const G = ground(st);
      for (let k = path.at; k < path.path.length; k++) { const t = path.path[k], tx = t % G.tw, ty = (t - tx) / G.tw, px = (tx + 0.5) * TILE_PX, py = (ty + 0.5) * TILE_PX; g.lineBetween(lx, ly, px, py); lx = px; ly = py; }
    }
    const h = this.hoverTile, inside = !!h && inReach(st, h.tx, h.ty, 1);
    if(debugView.coords){g.lineStyle(2 / zoom, 0xffffff, inside ? 0.35 : 0.1); g.strokeCircle(ex, ey, REACH * TILE_PX);}
    if (this.tool === 'rifle' && e.down < 0) {
      const p = this.input.activePointer, w = this.worldAt(p.x, p.y);
      const dx = w.x - ex, dy = w.y - ey, L = Math.hypot(dx, dy), R = RIFLE_RANGE * TILE_PX;
      if (L > 1e-6) {
        const k = Math.min(1, R / L);
        g.lineStyle(2 / zoom, 0xffd070, this.firing ? 0.8 : 0.3); g.lineBetween(ex, ey, ex + dx * k, ey + dy * k);
        g.lineStyle(1 / zoom, 0xffd070, 0.15); g.strokeCircle(ex, ey, R);
      }
    }
    if (e.down >= 0) { g.fillStyle(0xe05a5a, 0.9); g.fillCircle(ex, ey, 11); return; }
    // Layout pass (ROADMAP §2 "engineer as a two-tone sprite with facing"): the amber head rides the sim's own
    // `face` vector rather than sitting at a fixed offset, with a chevron on the leading edge, so which way the
    // engineer is turned is readable while standing still. `lastFace` holds the last non-zero facing, because
    // `face` is [0, 0] at a standstill. GAME-ASSUMPTION: drawing only — nothing reads the chevron, and the lamp
    // cone the ROADMAP line also asks for is NOT drawn (D-B5-1 decided against a personal light).
    if (e.face[0] || e.face[1]) this.lastFace = [e.face[0], e.face[1]];
    const fl = Math.hypot(this.lastFace[0], this.lastFace[1]) || 1, fx = this.lastFace[0] / fl, fy = this.lastFace[1] / fl;
    if(e.truckSeat)return;
    const bodyScale = ground(st).urban ? Math.max(1, 1 / zoom) : 1;
    g.fillStyle(0x0b0e1a, 0.9); g.fillCircle(ex + 2, ey + 3, 13 * bodyScale);
    g.fillStyle(e.dash > 0 ? 0xbfe8ff : 0xf5f0e0, 1); g.fillCircle(ex, ey, 10 * bodyScale);
    g.fillStyle(0xe8a93a, 1); g.fillCircle(ex + fx * 3, ey + fy * 3, 6 * bodyScale);
    g.lineStyle(2 / zoom, 0x0b0e1a, 0.8);
    g.lineBetween(ex + fx * 10 - fy * 5, ey + fy * 10 + fx * 5, ex + fx * 13, ey + fy * 13);
    g.lineBetween(ex + fx * 10 + fy * 5, ey + fy * 10 - fx * 5, ex + fx * 13, ey + fy * 13);
    if (e.hp < 100) { g.fillStyle(0x1a1d26, 1); g.fillRect(ex - 12, ey + 13, 24, 4); g.fillStyle(0x6fe08a, 1); g.fillRect(ex - 12, ey + 13, 24 * e.hp / 100, 4); }
    if (e.stamina < 1 || e.sprint) {
      g.fillStyle(0x1a1d26, 1); g.fillRect(ex - 12, ey + 18, 24, 3);
      g.fillStyle(e.stamina <= DODGE_COST + 1e-6 ? 0xe0a050 : 0xf0d060, 1); g.fillRect(ex - 12, ey + 18, 24 * e.stamina, 3);
      g.fillStyle(0xffffff, 0.8); g.fillRect(ex - 12 + 24 * DODGE_COST - 0.5, ey + 17, 1, 5);   // one dodge's worth
    }
  }

  /** M4: the crawlers on the tile layer — a dark disc with an HP bar once hurt, a red rim while it has turned on the
   *  engineer (D5 retaliation), a birth ring for its first second at the ridge; shades are faint and drawn only on
   *  lit tiles (§7: untargetable, and unseen, off them). GAME-ASSUMPTION: code-drawn discs stand in for the crawler
   *  and shade sprites until the art pass.
   *  RI-04 (plan §6, §7): the active emergence points pulse on the Dark kerbs the rot comes out of; a crawler carries
   *  a heading tick and, under the cursor, a line to its target tile; a shade leaves a fading trace on the unlit tiles
   *  it crossed (a trace is drawn on unlit ground and never implies the tile is lit); a Stalker is a triangle along its
   *  heading with its perception ring round its home, a yellow wind-up rim before its first strike, red while on you. */
  private drawThreat(): void {
    const g = this.gThreat, st = this.st, th = st.flow?.threat;
    g.clear();
    if (!th || !threatActive(st)) return;
    const cam = this.cameras.main, zoom = cam.zoom, wv = cam.worldView;
    const onScreen = (px: number, py: number, pad = 48) => px >= wv.x - pad && px <= wv.right + pad && py >= wv.y - pad && py <= wv.bottom + pad;
    const now = performance.now();
    // the emergence points: a pulsing ring on the kerb tile, a tick toward the block the rot walks into
    for (const p of (st.city?.mapId&&!debugView.coords?[]:activeEmergencePoints(st))) {
      const px = (p.tx + 0.5) * TILE_PX, py = (p.ty + 0.5) * TILE_PX;
      if (!onScreen(px, py)) continue;
      const o = st.blocks[p.other], b = st.blocks[p.block], dx = o.x - b.x, dy = o.y - b.y, L = Math.hypot(dx, dy) || 1;
      const pulse = 0.5 + 0.5 * Math.sin(now / 400 + p.id);
      g.lineStyle(1.5 / zoom, 0xc06ae0, 0.35 + 0.4 * pulse); g.strokeCircle(px, py, 7 + 4 * pulse);
      g.fillStyle(0xc06ae0, 0.9); g.fillCircle(px, py, 2.5);
      g.lineStyle(2 / zoom, 0xc06ae0, 0.7); g.lineBetween(px, py, px + dx / L * 12, py + dy / L * 12);
    }
    // RI-06: the Junction Heart — a pulsing ring on the yard's substation while it stands; each feeder cabinet as a
    // square (green on the live grid, amber supplied but unpowered, red knocked out, grey empty); a requested packet's
    // approach shown on its emergence point before the bodies are born (§9.2 default 6: "show the approach")
    const H = heartOf(st);
    if (H && !H.destroyed) {
      const hb = st.blocks[H.site], hs = substationAt(st, hb.x, hb.y);
      if (hs) {
        const px = (hs.tx + hs.size / 2) * TILE_PX, py = (hs.ty + hs.size / 2) * TILE_PX, pulse = 0.5 + 0.5 * Math.sin(now / 300);
        if (onScreen(px, py)) { g.lineStyle(2.5 / zoom, 0xe04a6a, 0.45 + 0.45 * pulse); g.strokeCircle(px, py, 14 + 6 * pulse); g.lineStyle(1 / zoom, 0xe04a6a, 0.3); g.strokeCircle(px, py, 26 + 10 * (1 - pulse)); }
      }
      H.cabinets.forEach((c, k) => {
        const px = c.x * TILE_PX, py = c.y * TILE_PX;
        if (!onScreen(px, py)) return;
        const colour = c.down ? 0xe0483a : cabinetConnected(st, k) ? 0x5ad07a : (c.delivered.steel + c.delivered.copper > 0 ? 0xe0b04a : 0x8a8a8a);
        g.fillStyle(colour, 0.85); g.fillRect(px + 2, py + 2, TILE_PX - 4, TILE_PX - 4);
        g.lineStyle(1.5 / zoom, 0x101010, 0.8); g.strokeRect(px + 2, py + 2, TILE_PX - 4, TILE_PX - 4);
        g.lineStyle(1 / zoom, colour, 0.5); g.strokeCircle(px + TILE_PX / 2, py + TILE_PX / 2, TILE_PX * 0.9);
      });
      for (const p of H.pending) {
        const c = H.cabinets[p.cabinet], pt = c ? emergencePoint(st, c.approach, H.site) : undefined;
        if (!pt) continue;
        const px = (pt.tx + 0.5) * TILE_PX, py = (pt.ty + 0.5) * TILE_PX, pulse = 0.5 + 0.5 * Math.sin(now / 150);
        if (!onScreen(px, py)) continue;
        g.lineStyle(2 / zoom, 0xe04a6a, 0.5 + 0.5 * pulse); g.strokeCircle(px, py, 10 + 8 * pulse);
        g.lineStyle(2 / zoom, 0xe04a6a, 0.8); g.lineBetween(px, py, c.x * TILE_PX + TILE_PX / 2, c.y * TILE_PX + TILE_PX / 2);
      }
    }
    // a shade's trace: the tiles it crossed, fading over TRACE_S, on unlit ground only (a lit tile shows the shade itself)
    for (const tr of shadeTraces(st)) {
      if (litAt(st, tr.tx, tr.ty)) continue;
      const px = (tr.tx + 0.5) * TILE_PX, py = (tr.ty + 0.5) * TILE_PX;
      if (!onScreen(px, py)) continue;
      const a = 0.4 * (1 - tr.age / TRACE_S);
      g.fillStyle(0x9a7ab0, a); g.fillPoints([{ x: px, y: py - 5 }, { x: px + 5, y: py }, { x: px, y: py + 5 }, { x: px - 5, y: py }], true);
    }
    const hov = this.hoverTile && st.flow ? crawlerAt(st, this.hoverTile.tx + 0.5, this.hoverTile.ty + 0.5) : null;
    // Layout pass (ROADMAP §2 "distinct crawler / shade shapes"): both were discs a pixel apart in radius. The
    // crawler keeps the disc; the shade is a diamond, which reads at a glance and at 0.5×. GAME-ASSUMPTION:
    // drawing only — the hitbox, the rifle and §7's lit-tile rule are unchanged.
    const dia = (cxp: number, cyp: number, rr: number): Phaser.Types.Math.Vector2Like[] =>
      [{ x: cxp, y: cyp - rr }, { x: cxp + rr, y: cyp }, { x: cxp, y: cyp + rr }, { x: cxp - rr, y: cyp }];
    for (const c of th.crawlers) {
      if(!cityVisible(st,c.x,c.y))continue;
      const px = c.x * TILE_PX, py = c.y * TILE_PX;
      if (px < wv.x - 48 || px > wv.right + 48 || py < wv.y - 48 || py > wv.bottom + 48) continue;
      const shade = c.kind === 'shade';
      if (shade && !litAt(st, Math.floor(c.x), Math.floor(c.y))) {g.lineStyle(2/zoom,0xc9a9ef,.65);g.strokePoints(dia(px,py,11),true);g.lineBetween(px-7,py+7,px+7,py-7);continue;}
      const r=c.role==='breaker'?14:c.role==='conductor'?12:9,age=st.t-c.born,maxHp=c.role==='breaker'?100:c.role==='conductor'?80:shade?40:ENEMIES[0].hp;
      if (shade) {
        g.fillStyle(0x0b0e1a, 0.25); g.fillPoints(dia(px + 2, py + 3, r), true);
        g.fillStyle(0x7a6a9a, 0.45); g.fillPoints(dia(px, py, r), true);
      } else {
        g.fillStyle(0x0b0e1a, 0.7); g.fillCircle(px + 2, py + 3, r);
        g.fillStyle(c.role==='breaker'?0x914f35:c.role==='conductor'?0x466f92:0x3a2a4a,1);g.fillCircle(px,py,r);if(c.role==='conductor'){g.lineStyle(2/zoom,0x79c6eb,.7);g.strokeCircle(px,py,r+5+Math.sin(now/250)*3);}
      }
      const rim = (rr: number) => { if (shade) g.strokePoints(dia(px, py, rr), true); else g.strokeCircle(px, py, rr); };
      if (c.onPlayer) { g.lineStyle(3 / zoom, 0xe05a5a, 0.95); rim(r + 2); }
      else { g.lineStyle(1.5 / zoom, 0x9a7ab0, 0.6); rim(r); }
      if (age < 1) { g.lineStyle(2 / zoom, 0xd0a0ff, 1 - age); g.strokeCircle(px, py, r + 4 + age * 10); }
      if (c.hp < maxHp) { g.fillStyle(0x1a1d26, 1); g.fillRect(px - 10, py - r - 7, 20, 3); g.fillStyle(0xe05a5a, 1); g.fillRect(px - 10, py - r - 7, 20 * Math.max(0, c.hp) / maxHp, 3); }
      // RI-04: the heading tick (§7 "direction and current target are inspectable"); the target line under the cursor
      if (c.dir && (c.dir[0] || c.dir[1])) { g.lineStyle(2 / zoom, c.onPlayer ? 0xe05a5a : 0xd0b0e0, 0.9); g.lineBetween(px + c.dir[0] * r, py + c.dir[1] * r, px + c.dir[0] * (r + 7), py + c.dir[1] * (r + 7)); }
      if (c === hov) {
        const tg = crawlerTarget(st, c);
        if (tg) {
          const qx = tg.tx * TILE_PX, qy = tg.ty * TILE_PX;
          g.lineStyle(1.5 / zoom, 0xe0c0ff, 0.8); g.strokeRect(qx + 1, qy + 1, TILE_PX - 2, TILE_PX - 2);
          g.lineStyle(1 / zoom, 0xe0c0ff, 0.5); g.lineBetween(px, py, qx + TILE_PX / 2, qy + TILE_PX / 2);
        }
      }
    }
    // RI-04: the Stalkers (the candidate is on) — a triangle along the heading, an HP bar once hurt, the perception ring
    // and a mark on the home tile so the territory reads before the first contact, the wind-up rim, red while on you
    const S = stalkerLayer(st);
    if (S) {
      const per = S.cand.perception * TILE_PX, maxHp = stalkerHp(S.cand);
      for (const s of stalkersOf(st)) {
        if(!cityVisible(st,s.x,s.y))continue;
        const px = s.x * TILE_PX, py = s.y * TILE_PX, hx = s.hx * TILE_PX, hy = s.hy * TILE_PX;
        if ((!st.city?.mapId||debugView.coords)&&onScreen(hx, hy, per + 48)) {

          g.lineStyle(1.5 / zoom, 0xc06060, s.mode === 'guard' ? 0.35 : 0.18); g.strokeCircle(hx, hy, per);
          g.lineStyle(1.5 / zoom, 0xc06060, 0.7); g.lineBetween(hx - 5, hy - 5, hx + 5, hy + 5); g.lineBetween(hx - 5, hy + 5, hx + 5, hy - 5);
        }
        if (!onScreen(px, py)) continue;
        const r = 11, [dx, dy] = s.dir[0] || s.dir[1] ? s.dir : [0, -1];
        const tri = (cxp: number, cyp: number, rr: number): Phaser.Types.Math.Vector2Like[] =>
          [{ x: cxp + dx * rr, y: cyp + dy * rr }, { x: cxp - dy * rr * 0.7 - dx * rr * 0.6, y: cyp + dx * rr * 0.7 - dy * rr * 0.6 }, { x: cxp + dy * rr * 0.7 - dx * rr * 0.6, y: cyp - dx * rr * 0.7 - dy * rr * 0.6 }];
        g.fillStyle(0x0b0e1a, 0.7); g.fillPoints(tri(px + 2, py + 3, r), true);
        g.fillStyle(0x5a2430, 1); g.fillPoints(tri(px, py, r), true);
        const onYou = s.mode === 'pursue' || s.mode === 'attack';
        if (s.mode === 'attack' && !s.warmed) { g.lineStyle(3 / zoom, 0xffcc44, 1); g.strokeCircle(px, py, r + 2 + 8 * (1 - s.windup / S.cand.windupS)); }
        else if (onYou) { g.lineStyle(3 / zoom, 0xe05a5a, 0.95); g.strokeCircle(px, py, r + 2); }
        else { g.lineStyle(1.5 / zoom, s.mode === 'investigate' ? 0xe0a060 : 0xc06060, 0.7); g.strokeCircle(px, py, r); }
        if (s.mode === 'investigate') { g.lineStyle(1 / zoom, 0xe0a060, 0.5); g.lineBetween(px, py, s.px * TILE_PX, s.py * TILE_PX); }
        if (s.hp < maxHp) { g.fillStyle(0x1a1d26, 1); g.fillRect(px - 12, py - r - 8, 24, 3); g.fillStyle(0xe05a5a, 1); g.fillRect(px - 12, py - r - 8, 24 * Math.max(0, s.hp) / maxHp, 3); }
      }
    }
  }

  private ghostReason = '';

  /** §14 power as one number: what the grid carries against what the Generators can give, the speed every
   *  machine runs at under a brownout (D-B3-4) and the coal left. Drawn every frame from `flow.power` (set by the sim's power section) and the flow summary. */
  private powerLine(): string {
    const st = this.st, f = st.flow;
    if (!f || !st.config.power) return '';
    const p = f.power, fs = flowSummary(st);
    const mw = (kw: number) => (kw / 1000).toFixed(2);
    const state = p.supply <= 0 ? ' · NO POWER: the Generators are out of coal' : p.demand > p.supply + 1e-9 ? ` · BROWNOUT: every machine at ${Math.round(fs.throttle * 100)} %` : '';
    return `\nPower ${mw(p.load)} / ${mw(p.supply)} MW (demand ${mw(p.demand)})${state} · Generators ${fs.generatorsBurning}/${fs.generators} burning, ${Math.floor(fs.genCoal)} coal · lamps ${fs.lampsLit}/${fs.lamps} lit · brownout ${Math.round(fs.brownoutS)} s`;
  }

  private drawTurretRange(g:Phaser.GameObjects.Graphics,x:number,y:number):void {
    const zoom=this.cameras.main.zoom,r=TURRET_RANGE*TILE_PX,px=x*TILE_PX,py=y*TILE_PX;
    g.lineStyle(4/zoom,0x05070f,.95);g.strokeCircle(px,py,r);
    g.lineStyle(2/zoom,0xffe6a6,1);g.strokeCircle(px,py,r);
    for(const [dx,dy] of [[0,1],[1,0],[0,-1],[-1,0]])g.lineBetween(px+dx*(r-5/zoom),py+dy*(r-5/zoom),px+dx*(r+5/zoom),py+dy*(r+5/zoom));
  }

  /** The tool's footprint under the pointer, green when it can go there, red with the reason otherwise. */
  private drawGhost(g: Phaser.GameObjects.Graphics): void {
    const st = this.st, h = this.hoverTile;
    this.ghostReason = '';
    if(st.flow&&this.blueprintMode==='removeArea'){
      this.ghostReason=this.removal?`REMOVAL · ${this.removal.reason} Use Pack selected machines in the clipboard panel.`:'REMOVAL · drag around whole machines, then review before packing';
      const a=this.copyStart??this.removal?.selection?.from,b=this.copyStart&&h?{x:h.tx,y:h.ty}:this.removal?.selection?.to;
      if(a&&b){const col=this.removal&&!this.removal.ok?0xff655d:0xffc46b,x=Math.min(a.x,b.x),y=Math.min(a.y,b.y),w=Math.abs(a.x-b.x)+1,hh=Math.abs(a.y-b.y)+1;g.fillStyle(col,.18);g.fillRect(x*TILE_PX,y*TILE_PX,w*TILE_PX,hh*TILE_PX);g.lineStyle(2/this.zoom,col);g.strokeRect(x*TILE_PX,y*TILE_PX,w*TILE_PX,hh*TILE_PX);}return;
    }
    if(st.flow&&h&&this.blueprintMode){
      if(this.blueprintMode==='copy'){
        this.ghostReason='COPY · drag around complete machines; Esc cancels';
        if(this.copyStart){const a=this.copyStart,x=Math.min(a.x,h.tx),y=Math.min(a.y,h.ty),w=Math.abs(a.x-h.tx)+1,hh=Math.abs(a.y-h.ty)+1;
          g.fillStyle(0x67c9ff,.15);g.fillRect(x*TILE_PX,y*TILE_PX,w*TILE_PX,hh*TILE_PX);g.lineStyle(2/this.zoom,0x67c9ff);g.strokeRect(x*TILE_PX,y*TILE_PX,w*TILE_PX,hh*TILE_PX);}
      }else{
        const bp=st.campaign?.clipboard;
        if(bp){const key=JSON.stringify(['clipboard',this.blueprintMode,st.campaign?.plans,bp,h,st.flow.rev,Math.floor(st.flow.tick/20),st.engineer.x,st.engineer.y,st.engineer.down,st.engineer.inv]);
          if(key!==this.previewKey){this.previewKey=key;this.previewResult=this.blueprintMode==='queue'?blueprintQueueCheck(st,h.tx,h.ty):blueprintCheck(st,h.tx,h.ty);}
          const check=this.previewResult,col=check.ok?(this.blueprintMode==='queue'?0x67c9ff:0x56e392):0xff655d;
          this.ghostReason=this.blueprintMode==='queue'?(check.ok?'PLAN · click to queue ghosts · no materials spent':`PLAN BLOCKED · ${check.reason}`):check.ok?'PASTE · click to pay and build all · R rotate · H/V mirror · Esc cancel':`PASTE BLOCKED · ${check.reason}`;
          for(const e of bp.entities){const [w,hh]=dimensions(e.kind,e.dir,MACHINE_SIZE[e.kind]),x=(h.tx+e.x)*TILE_PX,y=(h.ty+e.y)*TILE_PX;
            g.fillStyle(col,.22);g.fillRect(x,y,w*TILE_PX,hh*TILE_PX);g.lineStyle(2/this.zoom,col);g.strokeRect(x,y,w*TILE_PX,hh*TILE_PX);
            this.arrow(g,x+w*TILE_PX/2,y+hh*TILE_PX/2,e.dir,10,col,1);}
        }
      }return;
    }
    if (!st.flow || !h || this.tool === 'hand' || this.tool === 'rifle') return;   // the rifle draws its aim line, not a footprint
    if (this.beltPath) {
      const edits = pathEdits(this.beltPath, this.dir);
      const key = `${st.flow.rev}:${Math.floor(st.flow.tick / 20)}:${st.engineer.x}:${st.engineer.y}:${st.engineer.down}:${JSON.stringify(st.engineer.inv)}:${JSON.stringify(edits)}`;
      if (key !== this.previewKey) { this.previewKey = key; this.previewResult = constructionCheck(st, edits); }
      const check = this.previewResult;
      const col = check.ok ? 0x56e392 : 0xff655d;
      this.ghostReason = check.ok ? `${this.beltPath.length} belts · ${FACTORY_TEXT.release}` : check.reason;
      for (const e of edits) {
        g.fillStyle(col, 0.3); g.fillRect(e.x * TILE_PX, e.y * TILE_PX, TILE_PX, TILE_PX);
        if (e.action === 'place') this.arrow(g, (e.x + 0.5) * TILE_PX, (e.y + 0.5) * TILE_PX, e.dir, HALF - 3, col, 1);
      }
      return;
    }
    const kind = this.tool as Kind, size = MACHINE_SIZE[kind];
    const [width,height]=dimensions(kind,this.dir,size);
    if(kind==='underground'&&this.undergroundStart){
      const a=this.undergroundStart,b={x:h.tx,y:h.ty},edits:BuildEdit[]=[{action:'place',item:'underground',...a,dir:this.dir,underground:'input'},{action:'place',item:'underground',...b,dir:this.dir,underground:'output'}];
      const key=JSON.stringify([edits,st.flow?.rev,Math.floor((st.flow?.tick??0)/20),st.engineer.x,st.engineer.y,st.engineer.inv]);
      if(this.previewKey!==key){this.previewKey=key;this.previewResult=undergroundCheck(st,edits);}
      const check=this.previewResult,col=check.ok?0x3ddc84:0xff3b30;this.ghostReason=check.ok?'Click output to build pair; Esc cancels':check.reason;
      g.lineStyle(2,col,.8);g.lineBetween((a.x+.5)*TILE_PX,(a.y+.5)*TILE_PX,(b.x+.5)*TILE_PX,(b.y+.5)*TILE_PX);
      for(const p of [a,b]){g.fillStyle(col,.25);g.fillRect(p.x*TILE_PX,p.y*TILE_PX,TILE_PX,TILE_PX);this.arrow(g,(p.x+.5)*TILE_PX,(p.y+.5)*TILE_PX,this.dir,12,col);}
      return;
    }
    const [ox, oy] = this.footprint(kind, h.tx, h.ty);
    const far = this.onFoot && !inReach(st, ox, oy, width,height);
    const c = canPlace(st, kind, ox, oy,this.dir);
    const existing=kind==='underground'?machineAt(st,ox,oy):undefined;
    const reuse=existing?.kind==='underground'&&existing.underground==='input';
    // M2: the price is read from the pockets — a carried machine goes down as it is
    this.ghostReason = far ? 'walk closer' : reuse ? 'Click this input to reconnect its output' : c.ok ? (c.carried ? `from the pockets (${st.engineer.inv[kind]} carried)` : `${costStr(c.cost)} from the pockets`) : c.reason;
    const col = (c.ok || reuse) && !far ? 0x6fe08a : 0xe05a5a;
    g.fillStyle(col, 0.25); g.fillRect(ox * TILE_PX, oy * TILE_PX, width * TILE_PX, height * TILE_PX);
    g.lineStyle(2 / this.cameras.main.zoom, col, 0.9); g.strokeRect(ox * TILE_PX, oy * TILE_PX, width * TILE_PX, height * TILE_PX);
    this.drawMachinePorts({kind,x:ox,y:oy,size,dir:this.dir,underground:'input'} as Machine,true);
    const gcx = (ox + width / 2) * TILE_PX, gcy = (oy + height / 2) * TILE_PX;
    if(kind==='turret'){this.drawTurretRange(g,gcx/TILE_PX,gcy/TILE_PX);this.ghostReason+=` · range ${TURRET_RANGE} tiles`;}
    else if (kind === 'pole' || kind === 'bigpole') { g.lineStyle(1 / this.cameras.main.zoom, col, 0.6); g.strokeCircle(gcx, gcy, (kind === 'bigpole' ? BIG_POLE_REACH : POLE_REACH) * TILE_PX); }
    else if (kind === 'lamp' || kind === 'arclamp') { g.lineStyle(1 / this.cameras.main.zoom, LIGHT_COL, 0.6); g.strokeCircle(gcx, gcy, (kind==='arclamp'?ARC_LAMP_RADIUS:4) * TILE_PX); }
    else if (kind === 'floodlight') {
      // the cone it would throw (M3: 12 tiles, 60° about the facing)
      const a = Math.atan2(DY[this.dir], DX[this.dir]);
      g.lineStyle(1 / this.cameras.main.zoom, LIGHT_COL, 0.6); g.beginPath(); g.slice(gcx, gcy, FLOODLIGHT_RANGE * TILE_PX, a - FLOODLIGHT_HALF_ANGLE, a + FLOODLIGHT_HALF_ANGLE, false); g.closePath(); g.strokePath();
      this.arrow(g, gcx, gcy, this.dir, size * HALF - 4, col, 0.9);
    }
    else if (kind !== 'substation') this.arrow(g, gcx, gcy, this.dir, size * HALF - 4, col, 0.9);
  }

  /** RI-05: a pool's contents as item-coloured squares, one per ten items (rounded up), at most perRow × rows. */
  private itemSquares(g: Phaser.GameObjects.Graphics, pool: Record<string, number> | undefined, x: number, y: number, perRow: number, rows: number): void {
    if (!pool) return;
    let n = 0;
    for (const [k, v] of Object.entries(pool)) {
      for (let i = 0; i < Math.ceil(v / 10) && n < perRow * rows; i++, n++) {
        g.fillStyle(ITEM_COL[k] ?? 0xffffff, 1); g.fillRect(x + (n % perRow) * 9, y + Math.floor(n / perRow) * 9, 7, 7);
      }
    }
  }

  private arrow(g: Phaser.GameObjects.Graphics, cx: number, cy: number, dir: Dir, len: number, col: number, alpha = 1): void {
    const dx = DX[dir], dy = DY[dir], px = -dy, py = dx;   // perpendicular
    const tipx = cx + dx * len, tipy = cy + dy * len, base = len * 0.45, w = 6;
    g.fillStyle(col, alpha);
    g.fillTriangle(tipx, tipy, cx + dx * base + px * w, cy + dy * base + py * w, cx + dx * base - px * w, cy + dy * base - py * w);
  }

  /** Machines from `state.flow`, drawn every frame for the tiles in view: belts with their items, inserters with
   *  their arm, Excavators and assemblers with progress, the Depot. GAME-ASSUMPTION: flat code-drawn shapes stand
   *  in for machine sprites until the art pass; sizes and facings are the flow layer's. */
  private drawMachines(tx0: number, ty0: number, tx1: number, ty1: number, vis: readonly number[]): void {
    const st = this.st, g = this.gMach, f = st.flow;
    g.clear();
    this.depotText.setVisible(false);
    if (!f) return;
    const zoom = this.cameras.main.zoom;
    const now = performance.now(), blink = Math.floor(now / 260) % 2 === 0;
    // M3/M5 light pass under the machines: the light itself is the M5 texture; here only the streetlight posts
    // (warm when lit, dark with a cross when broken or eaten — E repairs those), then each block's substation slab,
    // then the pole wires
    const lit = new Set<number>();
    // RI-04 (§7 "an approaching Shade has an identifiable trace or flicker"): a lit post within three tiles of a shade
    // flickers — its glyph only; the light layer and the lit/unlit rule are untouched
    const shades = !!st.flow?.threat?.crawlers.some(c => c.kind === 'shade'), flick = Math.floor(now / 90) % 3 === 0;
    for (const bi of vis) {
      for (const l of blockLights(st, bi)) {
        const lx = (l.tx + 0.5) * TILE_PX, ly = (l.ty + 0.5) * TILE_PX;
        if (l.kind !== 'streetlight') { if (l.lit) lit.add(Math.floor(l.tx) * 4096 + Math.floor(l.ty)); continue; }
        const dim = l.lit && shades && flick && shadeNear(st, l.tx, l.ty);
        g.fillStyle(l.broken ? 0x2a2d36 : dim ? 0xb0a070 : l.lit ? 0xfff3b0 : 0x8a8f9a, 1); g.fillCircle(lx, ly, 4);
        if (l.lit) { g.fillStyle(LIGHT_COL, dim ? 0.12 : 0.35); g.fillCircle(lx, ly, 7); }
        if (l.broken) { g.lineStyle(1.5, l.why === 'eaten' ? 0xc06ae0 : 0xe05a5a, 0.9); g.lineBetween(lx - 4, ly - 4, lx + 4, ly + 4); g.lineBetween(lx - 4, ly + 4, lx + 4, ly - 4); }
      }
      const b = st.blocks[bi], sub = substationAt(st, b.x, b.y);
      if (sub && !st.city?.mapId) {
        const sx = sub.tx * TILE_PX, sy = sub.ty * TILE_PX, ss = sub.size * TILE_PX;
        g.fillStyle(0x2b2f3a, 0.95); g.fillRect(sx + 2, sy + 2, ss - 4, ss - 4);
        if (sub.size >= 3) { g.lineStyle(2, 0x4a5060, 1); for (let k = 0; k < 3; k++) g.strokeCircle(sx + ss / 2, sy + 22 + k * 22, 9); }
        g.lineStyle(3 / zoom, sub.on ? 0xb6e36a : sub.kw === 0 ? 0x3a4060 : 0xe05a5a, sub.on ? 0.9 : 0.8); g.strokeRect(sx + 1, sy + 1, ss - 2, ss - 2);
        if (sub.on) { g.fillStyle(0xb6e36a, 0.6 + 0.4 * Math.sin(now / 300)); g.fillCircle(sx + ss - 10, sy + 10, 4); }
      }
    }
    const grid = poleGrid(st);
    g.lineStyle(2 / zoom, 0xc9b98a, 0.8);
    for (const w of grid.links) g.lineBetween(w.x0 * TILE_PX, w.y0 * TILE_PX, w.x1 * TILE_PX, w.y1 * TILE_PX);
    if(st.city?.mapId)drawCityRail(g,this.cameras.main.worldView);
    else if(st.campaign?.fixedTram){g.lineStyle(3,0x7c8d8a,.75);g.beginPath();st.campaign.fixedTram.route.forEach((t,i)=>{const x=(t%f.tw+.5)*TILE_PX,y=(Math.floor(t/f.tw)+.5)*TILE_PX;if(i===0)g.moveTo(x,y);else g.lineTo(x,y);});g.strokePath();}
    for (const m of f.machines) {
      const [width,height]=machineDimensions(m);
      if (m.x + width <= tx0 || m.x > tx1 || m.y + height <= ty0 || m.y > ty1) continue;
      const px = m.x * TILE_PX, py = m.y * TILE_PX, sz = m.size * TILE_PX, cx = px + sz / 2, cy = py + sz / 2;
      // Layout pass (ROADMAP §2 "distinct machine silhouettes with outlines"): the shapes were already distinct but
      // the outlines were not — the turret's showed only while idle, the generator, excavator and assembler had an
      // inner line and no rim, the lamp and pole had neither. Every box machine now gets the same two marks, drawn
      // here and closed after the switch: a dark drop shadow that lifts it off the ground, and a screen-constant
      // rim. The posts (lamp, pole) take a dark backing slab in their own case. GAME-ASSUMPTION: drawing only.
      const box = BOX_MACHINE.has(m.kind);
      if (box || m.kind === 'depot') { g.fillStyle(0x05070d, 0.5); g.fillRect(px + 5, py + 6, sz - 4, sz - 4); }
      if(st.campaign&&defenceMax(m)>0){const hp=defenceHp(m);g.fillStyle(hp===0?0xd55b57:0x6bcc91,1);g.fillRect(px,py-5,sz*hp/defenceMax(m),3);if(hp===0){g.lineStyle(2,0xd55b57,1);g.lineBetween(px,py,px+sz,py+sz);g.lineBetween(px+sz,py,px,py+sz);}}
      switch (m.kind) {
        case 'barricade': case 'wall': { g.fillStyle(defenceHp(m)>0?MACHINE_COL[m.kind]:0x423c3b,1);g.fillRect(px+2,py+2,sz-4,sz-4);g.lineStyle(2,0x292c32,1);g.lineBetween(px+2,cy,px+sz-2,cy);break; }
        case 'cannon': case 'turret': {
          const rounds=m.kind==='cannon'?m.inv.shell??0:m.inv.rounds??0,frac=Math.min(1,rounds/(m.kind==='cannon'?CORRECTIONS.cannon.capacity:hopperCapacity(m,TURRET_HOPPER)));
          g.fillStyle(MACHINE_COL.turret, 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.fillStyle(0x262a34, 1); g.fillCircle(cx, cy, 19);
          // barrel along the facing; the street it covers is the nearest one (describeMachine names it)
          const bx = cx + DX[m.dir] * 27, by = cy + DY[m.dir] * 27;
          g.lineStyle(7, 0x9aa3b2, 1); g.lineBetween(cx, cy, bx, by);
          if (m.kind==='cannon'?m.timer>CORRECTIONS.cannon.seconds-TURRET_FLASH_S:m.timer>0) { g.fillStyle(0xfff0a0, Math.min(1,m.timer / TURRET_FLASH_S)); g.fillCircle(bx + DX[m.dir] * 5, by + DY[m.dir] * 5, 7); }
          // hopper bar: green → amber → red, blinking outline when empty (the same event turns the map pip red)
          g.fillStyle(0x1a1d26, 1); g.fillRect(px + 6, py + sz - 12, sz - 12, 6);
          g.fillStyle(frac > 0.5 ? 0x6fe08a : frac > 0 ? 0xe8a93a : 0xe05a5a, 1); g.fillRect(px + 6, py + sz - 12, (sz - 12) * frac, 6);
          // `busy` is "covers a live edge": a turret that covers nothing keeps its grey mark, now inside the rim
          if (!m.busy) { g.lineStyle(1.5 / zoom, 0x8a8f9a, 0.6); g.strokeRect(px + 6, py + 6, sz - 12, sz - 12); }
          break;
        }
        case 'arclamp': case 'lamp': {
          const on = lit.has(m.x * 4096 + m.y);
          const dim = on && shades && flick && shadeNear(st, m.x, m.y);   // RI-04: the same flicker on a lamp near a shade
          g.fillStyle(0x05070d, 0.5); g.fillRect(cx - 6, py + 6, 12, TILE_PX - 8);
          g.fillStyle(MACHINE_COL[m.kind], 1); g.fillRect(cx - 3, py + 8, 6, TILE_PX - 12);
          g.fillStyle(dim ? 0xb0a070 : on ? 0xfff3b0 : 0x3a3a40, 1); g.fillCircle(cx, py + 9, 6);
          if (on) { g.fillStyle(LIGHT_COL, dim ? 0.12 : 0.35); g.fillCircle(cx, py + 9, 9); }
          break;
        }
        case 'pole': {
          const on = grid.connected.has(m.id);
          g.fillStyle(0x05070d, 0.5); g.fillRect(cx - 11, py + 5, 22, TILE_PX - 6);
          g.fillStyle(MACHINE_COL.pole, 1); g.fillRect(cx - 3, py + 6, 6, TILE_PX - 8);
          g.fillRect(cx - 9, py + 8, 18, 3);
          g.fillStyle(on ? 0xb6e36a : 0x8a8f9a, 1); g.fillCircle(cx - 8, py + 9, 2.5); g.fillCircle(cx + 8, py + 9, 2.5);
          break;
        }
        // M3 (Electricians): a Big pole is a wider, taller pole with two crossarms; a Floodlight a 2×2 base with a
        // head along its facing that glows while lit; a built Substation is drawn as the face's slab above (it is one)
        case 'bigpole': {
          const on = grid.connected.has(m.id);
          g.fillStyle(0x1a1d26, 0.6); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.fillStyle(MACHINE_COL.bigpole, 1); g.fillRect(cx - 5, py + 8, 10, sz - 12);
          g.fillRect(cx - 20, py + 12, 40, 4); g.fillRect(cx - 14, py + 24, 28, 4);
          g.fillStyle(on ? 0xb6e36a : 0x8a8f9a, 1); g.fillCircle(cx - 18, py + 14, 3); g.fillCircle(cx + 18, py + 14, 3); g.fillCircle(cx - 12, py + 26, 3); g.fillCircle(cx + 12, py + 26, 3);
          break;
        }
        case 'floodlight': {
          const on = lit.has(m.x * 4096 + m.y), hx = cx + DX[m.dir] * 16, hy = cy + DY[m.dir] * 16;
          g.fillStyle(MACHINE_COL.floodlight, 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.fillStyle(0x262a34, 1); g.fillCircle(cx, cy, 10);
          g.lineStyle(8, 0x3a3f4c, 1); g.lineBetween(cx, cy, hx, hy);
          g.fillStyle(on ? 0xfff3b0 : 0x3a3a40, 1); g.fillCircle(hx, hy, 9);
          if (on) { g.fillStyle(LIGHT_COL, 0.35); g.fillCircle(hx, hy, 14); }
          break;
        }
        case 'substation': { g.lineStyle(1.5, 0x8a8f9a, 0.6); g.strokeRect(px + 4, py + 4, sz - 8, sz - 8); break; }
        // RI-05: the supply chest and the rail kit. A Track tile draws sleepers and two rails along the axis of its
        // track neighbours (a lone tile along its facing; a junction both — it voids the route, and reads as a cross);
        // a Tram stop is a 2×2 slab with a platform stripe (amber while powered, grey unpowered) on each side that
        // touches track, its platform's items as squares on the left and its arrivals' on the right; a Tram is a
        // one-tile car with a heading arrow and its cargo as squares; a Supply chest a 2×2 box with a lid line and
        // its contents as squares (one square per ten items). GAME-ASSUMPTION: drawing only.
        case 'track': {
          if(st.city?.mapId)break;
          const isTrack = (x: number, y: number) => machineAt(st, x, y)?.kind === 'track';
          const v = isTrack(m.x, m.y - 1) || isTrack(m.x, m.y + 1), hz = isTrack(m.x - 1, m.y) || isTrack(m.x + 1, m.y);
          const axisV = v || (!hz && m.dir % 2 === 0), axisH = hz || (!v && m.dir % 2 === 1);
          const q = TILE_PX / 4, o = TILE_PX / 8;
          if (axisV) { g.fillStyle(MACHINE_COL.track, 1); for (let k = 0; k < 4; k++) g.fillRect(px + o, py + o + k * q, TILE_PX - 2 * o, 3); g.fillStyle(0x9aa3b8, 1); g.fillRect(px + q, py, 3, TILE_PX); g.fillRect(px + TILE_PX - q - 3, py, 3, TILE_PX); }
          if (axisH) { g.fillStyle(MACHINE_COL.track, 1); for (let k = 0; k < 4; k++) g.fillRect(px + o + k * q, py + o, 3, TILE_PX - 2 * o); g.fillStyle(0x9aa3b8, 1); g.fillRect(px, py + q, TILE_PX, 3); g.fillRect(px, py + TILE_PX - q - 3, TILE_PX, 3); }
          break;
        }
        case 'tramstop': {
          g.fillStyle(MACHINE_COL.tramstop, 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          const isTrack = (x: number, y: number) => machineAt(st, x, y)?.kind === 'track', t = 6;
          g.fillStyle(powered(st, m) ? 0xe8c96a : 0x6a6f7a, 1);
          if (isTrack(m.x, m.y - 1) || isTrack(m.x + 1, m.y - 1)) g.fillRect(px + 4, py + 2, sz - 8, t);
          if (isTrack(m.x, m.y + 2) || isTrack(m.x + 1, m.y + 2)) g.fillRect(px + 4, py + sz - 2 - t, sz - 8, t);
          if (isTrack(m.x - 1, m.y) || isTrack(m.x - 1, m.y + 1)) g.fillRect(px + 2, py + 4, t, sz - 8);
          if (isTrack(m.x + 2, m.y) || isTrack(m.x + 2, m.y + 1)) g.fillRect(px + sz - 2 - t, py + 4, t, sz - 8);
          this.itemSquares(g, m.inv, px + 12, py + 14, 3, 3);                 // the platform: waiting for a tram
          this.itemSquares(g, m.cargo, px + sz / 2 + 6, py + 14, 3, 3);       // the arrivals: unloaded, for an inserter
          break;
        }
        case 'tram': {
          if(st.city?.mapId){drawCityTram(g,m,st.speed>0);break;}
          g.fillStyle(0x05070d, 0.5); g.fillRect(px + 5, py + 7, TILE_PX - 6, TILE_PX - 8);
          g.fillStyle(MACHINE_COL.tram, 1); g.fillRect(px + 3, py + 5, TILE_PX - 6, TILE_PX - 10);
          g.lineStyle(2 / zoom, 0xf0d0a0, 0.9); g.strokeRect(px + 3, py + 5, TILE_PX - 6, TILE_PX - 10);
          this.arrow(g, cx, cy, m.dir, TILE_PX / 2 - 5, 0xfff0d0, 0.9);
          this.itemSquares(g, m.cargo, px + 8, py + 9, 4, 2);
          break;
        }
        case 'chest': {
          g.fillStyle(MACHINE_COL.chest, 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.lineStyle(2, 0x2c2a20, 1); g.strokeRect(px + 6, py + 6, sz - 12, sz - 12);
          g.fillStyle(0x7a7050, 1); g.fillRect(px + 6, py + Math.floor(sz / 3), sz - 12, 3);   // the lid line
          this.itemSquares(g, m.inv, px + 12, py + Math.floor(sz / 3) + 8, 6, 3);
          break;
        }
        case 'generator': {
          const coal = m.inv.coal ?? 0;
          g.fillStyle(MACHINE_COL.generator, 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.lineStyle(2, 0x3a1c1c, 1); g.strokeRect(px + 6, py + 6, sz - 12, sz - 12);
          // chimney with a glow while burning
          g.fillStyle(0x2a2a30, 1); g.fillRect(px + sz - 22, py + 6, 12, 22);
          if (m.busy) { g.fillStyle(0xff9a3a, 0.5 + 0.3 * Math.sin(now / 90)); g.fillCircle(px + sz - 16, py + 8, 7); }
          // coal in the hopper: one square per 5 coal, and the burn progress of the current one
          for (let k = 0; k < Math.min(10, Math.ceil(coal / 5)); k++) { g.fillStyle(ITEM_COL.coal, 1); g.fillRect(px + 10 + (k % 5) * 9, py + 12 + Math.floor(k / 5) * 9, 7, 7); }
          g.fillStyle(0x1e1418, 1); g.fillRect(px + 10, py + sz - 16, sz - 20, 8);
          g.fillStyle(0xff9a3a, 1); g.fillRect(px + 10, py + sz - 16, (sz - 20) * Math.min(1, m.timer), 8);
          break;
        }
        case 'splitter': {
          g.fillStyle(0x364759,1);g.fillRect(px+2,py+2,width*TILE_PX-4,height*TILE_PX-4);
          g.lineStyle(2,0x83bfe8,.9);g.strokeRect(px+2,py+2,width*TILE_PX-4,height*TILE_PX-4);
          splitterPorts(m).forEach(([x,y],i)=>this.arrow(g,(x-DX[m.dir]+.5)*TILE_PX,(y-DY[m.dir]+.5)*TILE_PX,m.dir,12,m.priority===(i===0?'left':'right')?0xffce69:0x83bfe8));
          m.items.forEach((it,i)=>{g.fillStyle(ITEM_COL[it.k],1);g.fillRect(px+5+(i%4)*5,py+5+Math.floor(i/4)*5,4,4);});
          break;
        }
        case 'underground': {
          g.fillStyle(m.underground==='output'?0x355b73:0x4a5945,1);g.fillRect(px+2,py+2,TILE_PX-4,TILE_PX-4);
          this.arrow(g,cx,cy,m.dir,12,m.underground==='output'?0x83bfe8:0x86d99a);
          g.lineStyle(3,0xd5b574,1);g.lineBetween(cx-DY[m.dir]*9,cy+DX[m.dir]*9,cx+DY[m.dir]*9,cy-DX[m.dir]*9);
          const mate=undergroundMate(st,m);
          if(mate&&this.hoverTile&&machineAt(st,this.hoverTile.tx,this.hoverTile.ty)?.id===m.id){g.lineStyle(2,0x83bfe8,.7);g.lineBetween(cx,cy,(mate.x+.5)*TILE_PX,(mate.y+.5)*TILE_PX);}
          if(m.items.length){g.fillStyle(ITEM_COL[m.items[m.items.length-1].k],1);g.fillRect(px+4,py+4,6,6);}
          break;
        }
        case 'fastbelt': case 'belt': this.drawBelt(g, m, px, py); break;
        case 'inserter': {
          g.fillStyle(0x3a3220, 1); g.fillRect(px + 2, py + 2, TILE_PX - 4, TILE_PX - 4);
          g.lineStyle(1.5 / zoom, 0x9aa3b8, 0.6); g.strokeRect(px + 2, py + 2, TILE_PX - 4, TILE_PX - 4);
          g.fillStyle(MACHINE_COL.inserter, 1); g.fillCircle(cx, cy, 7);
          const [ix, iy] = inputTile(m), [ox, oy] = outputTile(m);
          const toOut = m.phase === 1;
          const ex = toOut ? ox : ix, ey = toOut ? oy : iy;
          // arm: from the base towards the tile it is reaching into, part-way through the swing
          const prog = m.phase === 0 ? 0 : Math.max(0, Math.min(1, 1 - m.timer / 0.5));
          const reach = m.phase === 0 ? 0.55 : toOut ? 0.25 + prog * 0.5 : 0.75 - prog * 0.5;
          const ax = cx + ((ex + 0.5) * TILE_PX - cx) * reach, ay = cy + ((ey + 0.5) * TILE_PX - cy) * reach;
          g.lineStyle(4, 0xb59a5a, 1); g.lineBetween(cx, cy, ax, ay);
          if (m.hold) { g.fillStyle(ITEM_COL[m.hold], 1); g.fillRect(ax - 4, ay - 4, 8, 8); }
          break;
        }
        case 'pumpjack': case 'excavator': {
          g.fillStyle(MACHINE_COL.excavator, 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.lineStyle(2, 0x2b3440, 1); g.strokeRect(px + 6, py + 6, sz - 12, sz - 12);
          const spin = m.hold ? 0 : (m.timer / (1 / EXCAVATOR_PER_S)) * Math.PI;
          g.lineStyle(5, 0x9fb4cc, 1);
          for (let k = 0; k < 2; k++) { const a = spin + k * Math.PI / 2; g.lineBetween(cx - Math.cos(a) * 14, cy - Math.sin(a) * 14, cx + Math.cos(a) * 14, cy + Math.sin(a) * 14); }
          this.arrow(g, cx, cy, m.dir, sz / 2 - 2, 0xd8dde8, 0.9);
          if (m.hold) { const [ox, oy] = outputTile(m); g.fillStyle(ITEM_COL[m.hold], 1); g.fillRect((ox + 0.5) * TILE_PX - 4 - DX[m.dir] * 10, (oy + 0.5) * TILE_PX - 4 - DY[m.dir] * 10, 8, 8); }
          break;
        }
        case 'foundry': case 'refinery': case 'assembler2': case 'mixer': case 'assembler': {
          g.fillStyle(MACHINE_COL[m.kind], 1); g.fillRect(px + 2, py + 2, sz - 4, sz - 4);
          g.lineStyle(2, 0x33293d, 1); g.strokeRect(px + 6, py + 6, sz - 12, sz - 12);
          // Item connections are shown on every usable side by the port overlay.
          // progress bar and the input/output counts as item squares, in the recipe's own items (RI-01: one row an input)
          const r = recipeOf(m), o = recipeOutput(r);
          const prog = m.busy ? Math.min(1, m.timer / r.seconds) : 0;
          g.fillStyle(0x1e1826, 1); g.fillRect(px + 10, py + sz - 16, sz - 20, 8);
          g.fillStyle(ITEM_COL[o] ?? 0xe6d45a, 1); g.fillRect(px + 10, py + sz - 16, (sz - 20) * prog, 8);
          Object.keys(r.inputs).forEach((k, row) => { for (let n = 0; n < Math.min(8, m.inv[k] ?? 0); n++) { g.fillStyle(ITEM_COL[k] ?? 0xffffff, 1); g.fillRect(px + 10 + n * 9, py + 10 + row * 10, 7, 7); } });
          for (let k = 0; k < Math.min(5, m.out); k++) { g.fillStyle(ITEM_COL[o] ?? 0xe6d45a, 1); g.fillRect(px + sz - 18, py + 10 + k * 9, 7, 7); }
          break;
        }
        case 'depot': {
          // the fill indicator (drawing only): the line buffer the HQ's turrets draw from, as magazines, green → amber →
          // red like a hopper bar; the outline holds 3 screen px so the Depot reads at 0.5× (STANDARDS C.2).
          // GAME-ASSUMPTION (GA-EF-2): the bar is st.buffer / bufferCap (the block sim's line buffer, not the chest's
          // magazines), green above half, amber below it, red with a blinking outline at 0; the beacon at the viewport edge is the
          // Depot's glyph and the tile distance — §19's "visible six blocks out" is Phase 12 art (drawing only).
          const cap = st.config.bufferCap, frac = Math.min(1, st.buffer / cap), mags = Math.floor(st.buffer / SHOT.count);
          g.fillStyle(MACHINE_COL.depot, 0.9); g.fillRect(px, py, sz, sz);
          g.lineStyle(3 / zoom, 0xffffff, 0.9); g.strokeRect(px, py, sz, sz);
          g.fillStyle(0x1a1d26, 1); g.fillRect(px + 12, py + sz - 24, sz - 24, 12);
          g.fillStyle(frac > 0.5 ? 0x6fe08a : frac > 0 ? 0xe8a93a : 0xe05a5a, 1); g.fillRect(px + 12, py + sz - 24, (sz - 24) * frac, 12);
          if (mags <= 0 && blink) { g.lineStyle(3 / zoom, 0xe05a5a, 1); g.strokeRect(px + 3, py + 3, sz - 6, sz - 6); }
          const urban = !!ground(st).urban;
          const label = st.campaign ? 'HOME  /  SUPPLIES' : urban ? 'HQ  /  DEPOT' : `Depot\n${mags} / ${Math.floor(cap / SHOT.count)} mag`;
          if (label !== this.depotLabel) { this.depotLabel = label; this.depotText.setText(label); }
          this.depotText.setFontFamily('Segoe UI, sans-serif').setFontSize(urban ? 15 : 20).setPosition(cx, urban ? py + 25 : cy - 6).setScale(1 / zoom).setVisible(true);
          break;
        }
      }
      this.drawMachinePorts(m,this.hoverTile?machineAt(st,this.hoverTile.tx,this.hoverTile.ty)?.id===m.id:false);
      if (box) {
        g.lineStyle(2 / zoom, 0x9aa3b8, 0.75); g.strokeRect(px + 1, py + 1, sz - 2, sz - 2);
        // the empty alert is drawn after the rim so it still wins: a turret with no rounds, a Generator with no coal
        const empty = m.kind === 'turret' ? (m.inv.rounds ?? 0) <= 0 : m.kind === 'generator' ? (m.inv.coal ?? 0) <= 0 : false;
        if (empty && blink) { g.lineStyle(3 / zoom, 0xe05a5a, 1); g.strokeRect(px + 1, py + 1, sz - 2, sz - 2); }
      }
      if (STATUS_KIND.has(m.kind)||(m.kind==='belt'&&(machineStatus(st,m).state==='blocked'||(this.hoverTile?.tx===m.x&&this.hoverTile?.ty===m.y)))) this.drawStatusMark(g, m, px, py, zoom);
    }
  }

  private drawMachinePorts(m:Machine,focused=false):void {
    if(m.kind==='belt'&&!focused)return;
    const g=this.gPorts,z=this.zoom,[w,h]=machineDimensions(m);
    for(const p of machinePorts(m)){
      // Quiet side-centre badges at rest; every valid edge tile appears on hover/placement.
      if(!focused&&m.kind!=='splitter'&&m.kind!=='inserter'&&m.kind!=='excavator'&&m.kind!=='underground'){
        const along=p.dir%2?p.y-m.y:p.x-m.x,mid=Math.floor((p.dir%2?h:w)/2)+.5;if(Math.abs(along-mid)>.1)continue;
      }
      const col=p.mode==='in'?0x73dce8:0xffc36a,shift=.27;
      const x=(p.x-DY[p.dir]*shift)*TILE_PX,y=(p.y+DX[p.dir]*shift)*TILE_PX;
      g.fillStyle(0x101a20,.95);g.fillCircle(x,y,6/z);g.fillStyle(col,1);
      const dx=DX[p.dir],dy=DY[p.dir];g.fillTriangle(x+dx*5/z,y+dy*5/z,x-dx*3/z-dy*3.5/z,y-dy*3/z+dx*3.5/z,x-dx*3/z+dy*3.5/z,y-dy*3/z-dx*3.5/z);
    }
    if(focused&&m.kind==='inserter')for(const [at,col] of [[inputTile(m),0x73dce8],[outputTile(m),0xffc36a]] as const){g.lineStyle(2/z,col,1);g.strokeRect(at[0]*TILE_PX+2,at[1]*TILE_PX+2,TILE_PX-4,TILE_PX-4);}
  }

  private drawBelt(g: Phaser.GameObjects.Graphics, m: Machine, px: number, py: number): void {
    const st = this.st, d = m.dir, e = entryDir(st, m);
    const cx = px + HALF, cy = py + HALF, w = 20;
    g.fillStyle(MACHINE_COL.belt, 1);
    // body: from the entry side to the centre, then from the centre to the exit side
    const seg = (dir: Dir, from: number, to: number) => {   // along dir, from/to in [-1,1] × half tile
      const ax = cx + DX[dir] * from * HALF, ay = cy + DY[dir] * from * HALF, bx = cx + DX[dir] * to * HALF, by = cy + DY[dir] * to * HALF;
      const x0 = Math.min(ax, bx) - (DX[dir] ? 0 : w / 2), y0 = Math.min(ay, by) - (DY[dir] ? 0 : w / 2);
      g.fillRect(x0, y0, DX[dir] ? Math.abs(bx - ax) : w, DY[dir] ? Math.abs(by - ay) : w);
    };
    seg(e, -1, 0); seg(d, 0, 1);
    g.fillStyle(0x3a3e4a, 1); g.fillRect(cx - w / 2, cy - w / 2, w, w);
    this.arrow(g, cx, cy, d, HALF - 3, 0x9c9d4e, 0.8);
    for (const it of m.items) {
      let x: number, y: number;
      // straight: the whole tile along d; corner: the first half along the entry direction to the centre, then along d
      if (e === d) { x = cx + DX[d] * (-1 + it.p * 2) * HALF; y = cy + DY[d] * (-1 + it.p * 2) * HALF; }
      else if (it.p >= 0.5) { x = cx + DX[d] * (it.p - 0.5) * 2 * HALF; y = cy + DY[d] * (it.p - 0.5) * 2 * HALF; }
      else { x = cx + DX[e] * (-1 + it.p * 2) * HALF; y = cy + DY[e] * (-1 + it.p * 2) * HALF; }
      g.fillStyle(ITEM_COL[it.k], 1); g.fillRect(x - 4, y - 4, 8, 8);
      g.lineStyle(1, 0x000000, 0.5); g.strokeRect(x - 4, y - 4, 8, 8);
    }
  }
}
