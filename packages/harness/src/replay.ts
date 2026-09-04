/** `npm run replay -- <telemetry.json> [--rifle-on]` (prompt B M6): Gate B's "at what minute did the player first
 *  fire, and did it matter?" — rebuild the session's city from the export's seed and URL, re-run its command log
 *  with the rifle's aim commands dropped, and print every hand-fired fight's verdict (held anyway / saved it / fell
 *  anyway). `--rifle-on` keeps the aim: a determinism check (the replay should match the played end state). Only a
 *  fresh-start session (scenario A, the default flow layer and river map) replays; a snapshot session's base state is
 *  not rebuilt here. GAME-ASSUMPTION (M6): the state is rebuilt as session.ts createSession builds it. */
import { readFileSync } from 'node:fs';
import { DEFAULT_CONFIG, protoCalibrated, createState, citySpec, generateMap, ensureFlow, replay, replayVerdict, LoggedCommand, SimState, HELD, mmss, CityPreset, CITY_PRESETS } from '@relight/sim';

interface Export { meta: { seed: number; url: string; scenario: string }; commands?: LoggedCommand[]; finalState: SimState }

const args = process.argv.slice(2);
const file = args.find(a => !a.startsWith('--'));
if (!file) { console.error('usage: npm run replay -- <telemetry.json> [--rifle-on]'); process.exit(2); }
const ex = JSON.parse(readFileSync(file, 'utf8')) as Export;
if (!ex.commands) { console.error('no command log in the export (a session before M6, or not a telemetry export)'); process.exit(2); }
if (ex.meta.scenario !== 'A') { console.error('a snapshot session (scenario B) does not replay'); process.exit(2); }
const q = new URL(ex.meta.url).searchParams;
const seed = ex.meta.seed, map = q.get('map') ?? 'river';
const cfg = protoCalibrated({ ...DEFAULT_CONFIG, scatter: q.get('scatter') !== '0', economy: q.get('economy') !== '0', walk: true });
Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
const spec = map === 'lattice' ? generateMap(seed, cfg) : citySpec(seed, (CITY_PRESETS as readonly string[]).includes(map) ? map as CityPreset : 'river', cfg);
const st = createState(spec, cfg, seed);
ensureFlow(st);
const rifleOn = args.includes('--rifle-on');
replay(st, ex.commands, ex.finalState.flow?.tick ?? 0, { dropAim: !rifleOn });
const v = replayVerdict(ex.finalState, st);
console.log(`seed ${seed}, ${ex.commands.length} commands, ${mmss(ex.finalState.t)} played; replay with the rifle ${rifleOn ? 'kept' : 'off'}`);
console.log(`first shot: ${mmss(v.firstShot)}; held ${v.heldPlayed} (played) vs ${v.heldReplayed} (replayed); HQ: ${v.hq}`);
for (const f of v.fights) console.log(`  ${mmss(f.t)} edge ${f.edge} block (${f.bx},${f.by}) ${f.rounds} rounds: ${f.verdict}`);
console.log(`verdict: ${v.verdict}`);
if (rifleOn) {
  const same = st.blocks.every((b, i) => b.state === ex.finalState.blocks[i].state) && st.flow!.machines.length === (ex.finalState.flow?.machines.length ?? 0);
  console.log(`determinism: ${same ? 'the replay matches the played end state' : 'the replay DIVERGED from the played end state'} (${st.blocks.filter(b => b.state === HELD).length} Held)`);
}
