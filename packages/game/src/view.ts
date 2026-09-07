/** Renderer-only view state (never in SimState): which view is up and the block both views agree on. */
export type ViewMode = 'map' | 'world';
export interface View {
  mode: ViewMode;
  /** The block the views hand each other on E: the block under the map cursor, or under the world camera's centre. */
  focus: [number, number];
  /** Sim tick of the last switch, for the map view's returning marker. */
  switchedAt: number;
}

/** RI-02 (§11.2 "debug coordinates behind a toggle"): the HUD, tooltips and toasts name blocks and streets
 *  (names.ts); block and tile coordinates appear only while this is on — the ` key toggles it with the debug panel.
 *  Renderer-only state, shared by the panel and both scenes. */
export const debugView = { coords: false };
/** RI-02: the height the goal overlay (#goal) takes at the top of the canvas, so the world view's top HUD corners
 *  sit under it instead of behind it. main.ts measures it once a panel update; 0 while the overlay is hidden. */
export const hudInset = { top: 0 };

/** Selected stop is presentation only: selecting a route never dispatches a movement command. */
export const transportView: { stopId:number|null } = { stopId:null };
