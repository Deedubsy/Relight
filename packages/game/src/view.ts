/** Renderer-only view state (never in SimState): which view is up and the block both views agree on. */
export type ViewMode = 'map' | 'world';
export interface View {
  mode: ViewMode;
  /** The block the views hand each other on E: the block under the map cursor, or under the world camera's centre. */
  focus: [number, number];
  /** Sim tick of the last switch, for the map view's returning marker. */
  switchedAt: number;
}
