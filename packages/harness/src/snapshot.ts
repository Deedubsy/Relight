/** Scenario B snapshot (PROTOTYPE_TEST_PLAN.md): the compact bot, building, on the proto's config, run to 3 h and
 *  written as a raw SimState the proto loads with ?state=<name>. Deterministic: --check regenerates and diffs.
 *
 *    npm run snapshot                       writes packages/game/public/snapshots/b-compact-seed3.json
 *    npm run snapshot -- --check            exits 1 if the committed file differs from a fresh run
 *    npm run snapshot -- --bot spike --seed 4 --hours 2 --out path.json */
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { configOf, stamp, Stamp } from './provenance';
import { configHash, generateMap, createState, step, takeEvents, createBot, botCommands, Command, Policy, heldCount, interior, frontage, clockOf } from '@relight/sim';

const argv = process.argv.slice(2);
const flag = (name: string, dflt: string): string => { const i = argv.indexOf(name); return i >= 0 && i + 1 < argv.length ? argv[i + 1] : dflt; };
const BOT = flag('--bot', 'compact') as Policy;
const SEED = Number(flag('--seed', '3'));
const HOURS = Number(flag('--hours', '3'));
const CHECK = argv.includes('--check');
const here = dirname(fileURLToPath(import.meta.url));
const OUT = argv.includes('--out') ? resolve(process.env.INIT_CWD ?? process.cwd(), flag('--out', '')) : resolve(here, `../../game/public/snapshots/b-${BOT}-seed${SEED}.json`);

const config = configOf({ kind: 'snapshot' });   // protoCalibrated(DEFAULT_CONFIG + scatter + economy), provenance.ts
const st = createState(generateMap(SEED, config), config, SEED);
const bot = createBot(BOT, null, true);   // the proto's bots build (calibration step 4)
const cmds: Command[] = [];
for (let k = 0; k < Math.round(HOURS * 3600); k++) { cmds.length = 0; botCommands(st, bot, cmds); step(st, cmds); takeEvents(st); }
st.speed = 0; st.acc = 0;   // the proto starts a snapshot paused
const prov = stamp({ kind: 'snapshot' });   // rule 11: the state file carries its commit and config hash as its first keys
const withStamp = (p: Stamp) => JSON.stringify({ ...p, ...st });
const json = withStamp(prov);
const line = `${BOT} seed ${SEED} at ${clockOf(st.t)}: held ${heldCount(st)} front ${frontage(st)} interior ${interior(st)} lost ${st.stats.lost} claims ${st.stats.claims} assemblers ${st.stats.assemblersAdded + config.startAssemblers} config ${configHash(config)}`;
if (CHECK) {
  let old = '';
  try { old = readFileSync(OUT, 'utf8'); } catch { console.error(`snapshot: ${OUT} missing`); process.exit(1); }
  // the committed file's own stamp is kept for the comparison: a moved HEAD is not a moved state (freshness:check judges the stamp)
  let oldProv: Stamp = prov;
  try { const o = JSON.parse(old) as Partial<Stamp>; if (o.source_commit && o.config_hash && o.config_ref) oldProv = { source_commit: o.source_commit, config_hash: o.config_hash, config_ref: o.config_ref }; } catch { /* not JSON: the byte comparison below reports it */ }
  if (old !== withStamp(oldProv)) { console.error(`snapshot: ${OUT} differs from a fresh run (${line}); run npm run snapshot`); process.exit(1); }
  console.log(`snapshot: ${OUT} matches (${line})`);
} else {
  mkdirSync(dirname(OUT), { recursive: true });
  writeFileSync(OUT, json);
  console.log(`snapshot: wrote ${OUT} (${json.length} bytes; ${line})`);
}
