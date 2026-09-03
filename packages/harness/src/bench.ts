/** Throughput check: 24×22, compact bot, ammo ring on. The proto needs 16 ticks per frame at 60 fps = 960 ticks/s. */
import { generateMap, createState, DEFAULT_CONFIG, step, createBot, botCommands, Command } from '@relight/sim';

const spec = generateMap(3, DEFAULT_CONFIG);
const st = createState(spec, { ...DEFAULT_CONFIG, economy: true, startAssemblers: 1, asmSchedule: [] }, 3);
const bot = createBot('compact');
const cmds: Command[] = [];
const ticks = 5 * 3600;
const t0 = performance.now();
for (let k = 0; k < ticks; k++) { cmds.length = 0; botCommands(st, bot, cmds); step(st, cmds); st.events.length = 0; }
const dt = (performance.now() - t0) / 1000;
console.log(`${ticks} ticks in ${dt.toFixed(3)} s = ${(ticks / dt).toFixed(0)} ticks/s (${(ticks / dt / 60).toFixed(0)}× real time at 60 fps needs 1 tick/frame; 16× needs 960 ticks/s)`);
console.log(`held ${st.hourly.at(-1)?.held} front ${st.hourly.at(-1)?.front} lost ${st.stats.lost} mags ${st.totalRounds / 10}`);
