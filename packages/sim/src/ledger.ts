/** RI-01: resource conservation — the accounting check across production, transfer, delivery commitment and
 *  consumption (`PROGRESS.md` RI-01; the revised plan's RI-01 row). Every item the tile layer knows (flow.ts `ITEMS`)
 *  is counted where it is now (`held`: the chest, the line buffer, the ring's stand-in hoppers, the pockets, the
 *  machines' inventories and the belts) against what has entered the game since the flow layer opened (rubble units
 *  mined and recipe output, `sources`) and what has left it (recipe inputs, machine prices, claim prices, repairs,
 *  burned coal, fired rounds and rounds a full buffer could not take, `sinks`). Transfers — belts, inserters, the
 *  chest, hand-feeds, the ring's draw from the line buffer, kits — move an item between places and appear on neither
 *  side. `unexplained` is held + sinks − sources by item, against the opening stock: zero when every unit is
 *  explained; positive when items appeared from nowhere, negative when they vanished. Rubble still in the ground is
 *  not an item and is not counted; `stats.minedOf` is where it enters.
 *
 *  The opening stock is recorded on the flow state when it is created (`FlowState.ledger`), as held − net at that
 *  moment, so a save from before RI-01 is checked from the moment it was upgraded rather than from a history it never
 *  kept (flow.ts `upgrade`). Kits are free (chestCount 'kit' is Infinity) and machines carried in the pockets are
 *  not items: their price left the game at their first placement (`stats.placed`). */
import { SimState } from './types';
import { ROUNDS_PER_MAG } from './constants';
import { Item, ITEMS, zeroItems, isItem, recipeOf, recipeOutput, REPAIR_COPPER, ensureFlow } from './flow';   // flow.ts imports openLedger back: only functions cross, at call time

/** Where the items are, one column a place. */
/** RI-03: `committed` is claim material delivered to a Dark block's installation and not yet activated (plan §4.3:
 *  in an inventory, committed to a restoration stage, or consumed — never two of them). */
export type LedgerPlace = 'chest' | 'buffer' | 'ring' | 'pockets' | 'machines' | 'belts' | 'committed';
export const LEDGER_PLACES: readonly LedgerPlace[] = ['chest', 'buffer', 'ring', 'pockets', 'machines', 'belts', 'committed'];

export interface Ledger {
  ok: boolean;
  tolerance: number;
  /** The flow tick the opening stock was recorded at (0 on a city played from its start). */
  openedAt: number;
  opening: Record<Item, number>;
  sources: Record<Item, number>;
  sinks: Record<Item, number>;
  held: Record<Item, number>;
  where: Record<LedgerPlace, Record<Item, number>>;
  unexplained: Record<Item, number>;
  /** One line an item out of tolerance. */
  problems: string[];
}

const ROUNDS = ROUNDS_PER_MAG;

/** The flow state as it stands — `ensureFlow` itself calls `openLedger`, so the counters never call back into it. */
function flowOf(st: SimState) { const f = st.flow; if (!f) throw new Error('ledger: the flow layer is not open (call ensureFlow first)'); return f; }

/** Every item where it stands now, by place and in total. Rounds count as magazines (ten a magazine, fractional). */
export function heldItems(st: SimState): { total: Record<Item, number>; where: Record<LedgerPlace, Record<Item, number>> } {
  const f = flowOf(st);
  const where = {} as Record<LedgerPlace, Record<Item, number>>;
  for (const p of LEDGER_PLACES) where[p] = zeroItems();
  const add = (p: LedgerPlace, k: string, n: number) => { if (isItem(k) && n > 0) where[p][k] += n; };
  add('chest', 'steel', st.stock.steel); add('chest', 'copper', st.stock.copper); add('chest', 'stone', st.stock.stone);
  for (const k in f.store) add('chest', k, f.store[k as keyof typeof f.store]);
  add('buffer', 'magazine', st.buffer / ROUNDS);
  for (const e of st.ring) if (!e.turrets) add('ring', 'magazine', e.hopper / ROUNDS);   // a stand-in edge's hopper; a turret edge's mirrors its turrets
  for (const k in st.engineer.inv) add('pockets', k, st.engineer.inv[k]);
  for (const bi in f.delivered ?? {}) { add('committed', 'steel', f.delivered[bi].steel); add('committed', 'copper', f.delivered[bi].copper); }
  for (const site of Object.values(st.campaign?.expansion ? { station: st.campaign.expansion.station, radio: st.campaign.expansion.radio } : {})) { add('committed', 'steel', site.delivered.steel); add('committed', 'copper', site.delivered.copper); }
  for (const cb of f.heart?.cabinets ?? []) { add('committed', 'steel', cb.delivered.steel); add('committed', 'copper', cb.delivered.copper); }   // RI-06: the feeder cabinets' materials until the Heart is destroyed
  for (const m of f.machines) {
    for (const it of m.items) add('belts', it.k, 1);
    if (m.hold) add(m.kind === 'belt' ? 'belts' : 'machines', m.hold, 1);
    if (m.kind === 'turret') add('machines', 'magazine', (m.inv.rounds ?? 0) / ROUNDS);
    else { for (const k in m.inv) add('machines', k, m.inv[k]); if (m.out > 0) add('machines', m.kind === 'assembler' ? recipeOutput(recipeOf(m)) : 'magazine', m.out); }
    if (m.cargo) for (const k in m.cargo) add('machines', k, m.cargo[k]);   // RI-05: a tram's load, a stop's arrivals
  }
  const total = zeroItems();
  for (const p of LEDGER_PLACES) for (const k of ITEMS) total[k] += where[p][k];
  return { total, where };
}

/** What the counters say entered the game (`sources`) and left it (`sinks`) since the flow layer's counters began. */
export function ledgerFlows(st: SimState): { sources: Record<Item, number>; sinks: Record<Item, number> } {
  const f = flowOf(st), s = f.stats, b = st.stats;
  const sources = zeroItems(), sinks = zeroItems();
  for (const k of ITEMS) { sources[k] += (s.minedOf[k] ?? 0) + (s.made[k] ?? 0); sinks[k] += s.consumed[k] ?? 0; }
  sinks.steel += (s.placed?.steel ?? 0) + (b.spentSteel ?? 0);
  sinks.copper += (s.placed?.copper ?? 0) + (b.spentCopper ?? 0) + (f.repairs ?? 0) * REPAIR_COPPER;
  sinks.coal += s.coalBurned ?? 0;
  sinks.magazine += ((s.fired ?? 0) + (st.engineer.fired ?? 0) + (b.ringFired ?? 0) + (b.roundsLost ?? 0)) / ROUNDS;
  return { sources, sinks };
}

/** The opening record for a flow state: held − net now, so that `conservation` reads zero from here on. */
export function openLedger(st: SimState): { tick: number; base: Record<Item, number> } {
  const { total } = heldItems(st), { sources, sinks } = ledgerFlows(st);
  const base = zeroItems();
  for (const k of ITEMS) base[k] = total[k] - (sources[k] - sinks[k]);
  return { tick: st.flow?.tick ?? 0, base };
}

/** The conservation check. `tolerance` absorbs floating-point drift in fractional rounds and coal (a hundredth of an item). */
export function conservation(st: SimState, tolerance = 0.01): Ledger {
  const f = ensureFlow(st);
  const { total: held, where } = heldItems(st), { sources, sinks } = ledgerFlows(st);
  const opening = f.ledger?.base ?? zeroItems(), openedAt = f.ledger?.tick ?? 0;
  const unexplained = zeroItems(), problems: string[] = [];
  for (const k of ITEMS) {
    unexplained[k] = held[k] + sinks[k] - sources[k] - opening[k];
    if (Math.abs(unexplained[k]) > tolerance) problems.push(`${k}: ${unexplained[k] > 0 ? '+' : ''}${unexplained[k].toFixed(2)} unexplained (opening ${opening[k].toFixed(1)} + sources ${sources[k].toFixed(1)} = held ${held[k].toFixed(1)} + sinks ${sinks[k].toFixed(1)})`);
  }
  return { ok: problems.length === 0, tolerance, openedAt, opening, sources, sinks, held, where, unexplained, problems };
}
