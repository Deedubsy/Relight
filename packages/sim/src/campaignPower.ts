import {RIVERFRONT} from './city/riverfront';
import {CORRECTIONS} from './progression';
/** Campaign power: separate local grids joined by actual poles and substations.
 * Existing whole-cell service and 100 kW core draw are retained as provisional defaults. */
import { TURBINE } from './campaignTurbine';
import { SimState, HELD } from './types';
import { ground, blockOfTile, distToRect } from './ground';
import { MACHINE_KW, Machine, FlowState } from './flow';
import { DISTRICT_ECONOMY } from './campaignDistricts';
import { GENERATOR_KW, POLE_REACH, BIG_POLE_REACH } from './recipes';
export const CAMPAIGN_POWER = { coreKw: 100, radioKw: 20 } as const;
export interface Circuit { turbineSupply: number; plantSupply:number; supply: number; demand: number; load: number; throttle: number; generators: number[] }
export interface CampaignGrid { turbineOutput: number; plantOutput:number; blocks: Circuit[]; poles: Map<number,Circuit>; generation: Map<number, number>; supply: number; demand: number; load: number }
const cache = new WeakMap<FlowState, { key: string; grid: CampaignGrid }>();
export function campaignGrid(st: SimState): CampaignGrid {
  const f = st.flow!, G = ground(st), n = st.blocks.length;
  const gens = f.machines.filter(m => m.kind === 'generator' && ((m.inv.coal ?? 0)+(m.inv.fuel??0)) > 0);
  const disabled=new Set(st.campaign?.defence?.bases.filter(b=>b.hp===0).map(b=>b.block));
  const turbine=st.campaign?.turbine;
  const key = `${turbine?.restoredAt}:${turbine?.enabled}:${f.rev}:${f.tick}:${gens.map(m => m.id).join(',')}:${[...disabled].join(',')}:${st.campaign?.expansion?.radio.restoredAt ?? -1}:${st.campaign?.districts?.workshop.restoredAt ?? -1}`;
  const prior = cache.get(f); if (prior?.key === key) return prior.grid;
  const poles = f.machines.filter(m => m.kind === 'pole' || m.kind === 'bigpole');
  const parent = Array.from({ length: n + poles.length }, (_, i) => i);
  const root = (i: number): number => { while (parent[i] !== i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; };
  const union = (a: number, b: number) => { parent[root(a)] = root(b); };
  const reach = (m: Machine) => m.kind === 'bigpole' ? BIG_POLE_REACH : POLE_REACH;
  const subs = G.blocks.map(b => b.sub);
  for (const m of f.machines) if (m.kind === 'substation') { const bi = blockOfTile(st, m.x, m.y); if (bi >= 0) subs[bi] = m; }
  for (let i = 0; i < poles.length; i++) {
    const a = poles[i], ax = a.x + a.size / 2, ay = a.y + a.size / 2;
    for (let bi = 0; bi < n; bi++) { const s = subs[bi]; if (!disabled.has(bi) && s && distToRect(ax, ay, s.x, s.y, s.size, s.size) <= reach(a)) union(n + i, bi); }
    for (let j = 0; j < i; j++) { const b = poles[j]; if (Math.hypot(ax - b.x - b.size / 2, ay - b.y - b.size / 2) <= Math.max(reach(a), reach(b))) union(n + i, n + j); }
  }
  const groups = new Map<number, Circuit>();
  const circuit = (bi: number): Circuit => { const id = root(bi); let c = groups.get(id); if (!c) { c = { turbineSupply: 0, plantSupply:0, supply: 0, demand: 0, load: 0, throttle: 0, generators: [] }; groups.set(id, c); } return c; };
  const blocks = st.blocks.map((_, bi) => circuit(bi));
  if(turbine&&turbine.restoredAt>=0&&turbine.enabled&&!disabled.has(turbine.block)){blocks[turbine.block].supply+=TURBINE.kw;blocks[turbine.block].turbineSupply=TURBINE.kw;}
  for(const plant of st.campaign?.progression?.sites??[])if(plant.kind==='plant'&&plant.installed&&plant.enabled&&!disabled.has(plant.block)){blocks[plant.block].supply+=CORRECTIONS.plantKw;blocks[plant.block].plantSupply+=CORRECTIONS.plantKw;}
  for (const m of gens) { const bi = blockOfTile(st, m.x, m.y); if (bi >= 0 && !disabled.has(bi)) { blocks[bi].supply += GENERATOR_KW; blocks[bi].generators.push(m.id); } }
  for (let bi = 0; bi < n; bi++) if ((st.city?.mapId?st.campaign?.defence?.bases.some(b=>b.block===bi):st.blocks[bi].state === HELD) && !disabled.has(bi)) blocks[bi].demand += CAMPAIGN_POWER.coreKw;
  if(st.city?.mapId)for(const b of G.blocks)if(!disabled.has(b.i))blocks[b.i].demand+=b.lights.length*RIVERFRONT.lightKw;
  for (const m of f.machines) { const bi = blockOfTile(st, m.x, m.y); if (bi >= 0 && !disabled.has(bi)) blocks[bi].demand += MACHINE_KW[m.kind]; }
  for(const site of st.campaign?.progression?.sites??[])if(site.started&&site.restoredAt<0)blocks[site.block].demand+=CORRECTIONS.encounterKw;
  const radio = st.campaign?.expansion?.radio;
  if (radio && radio.restoredAt >= 0 && !disabled.has(radio.block)) blocks[radio.block].demand += CAMPAIGN_POWER.radioKw;
  const workshop = st.campaign?.districts?.workshop;
  if (workshop && workshop.restoredAt >= 0 && !disabled.has(workshop.block)) blocks[workshop.block].demand += DISTRICT_ECONOMY.workshopKw;
  const grid: CampaignGrid = { turbineOutput: 0, plantOutput:0, blocks, poles:new Map(poles.map((m,i)=>[m.id,circuit(n+i)])), generation: new Map(), supply: 0, demand: 0, load: 0 };
  for (const c of groups.values()) {
    c.load = Math.min(c.supply, c.demand); c.throttle = c.supply > 0 ? Math.min(1, c.supply / Math.max(1, c.demand)) : 0;
    grid.supply += c.supply; grid.demand += c.demand; grid.load += c.load;
    const turbineLoad=Math.min(c.load,c.turbineSupply);grid.turbineOutput+=turbineLoad;const plantLoad=Math.min(c.load-turbineLoad,c.plantSupply);grid.plantOutput+=plantLoad;
    for (const id of c.generators) grid.generation.set(id, (c.load-turbineLoad-plantLoad) / c.generators.length);
  }
  cache.set(f, { key, grid }); return grid;
}
export function campaignThrottle(st: SimState, bi: number): number { return bi >= 0 && st.flow ? campaignGrid(st).blocks[bi].throttle : 0; }
