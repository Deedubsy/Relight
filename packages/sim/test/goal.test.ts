/** RI-02 (run name RI-02-goal): the stable block names, the current-goal line read off the state, the machine
 *  status words, and the save / load baseline (state hash, validated load, replay identity after a load). */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, protoCalibrated, createState, citySpec, ensureFlow, SimState, LoggedCommand, HELD, DARK,
  createHourBot, runHour, replay, hqIdx, ground, TILE_TPS, neighbourToward,
  blockName, blockNameAt, blockCoords, hqNeighbourToward, streetName, bearingOf,
  currentGoal, GOAL_ORDER, machineStatus, patchExcavators, patchLeft, generators, shotAssemblers,
  stateHash, loadState, makeSave, isSaveFile, canonicalJson, SAVE_TRANSIENT,
} from '../src/index';

function city(seed = 3, flow = true): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true });
  Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  if (flow) ensureFlow(st);
  return st;
}

test('names: every block has a stable, unique, state-free name; the HQ is "HQ"; the rail yard is named; the §11 directions are three distinct HQ neighbours', () => {
  for (const seed of [3, 4, 5]) {
    const st = city(seed), hq = hqIdx(st), G = ground(st);
    const names = st.blocks.map((_, i) => blockName(st, i));
    assert.equal(names[hq], 'HQ');
    assert.equal(new Set(names).size, names.length, `seed ${seed}: names unique (${names.length})`);
    assert.ok(G.railYard >= 0 && names[G.railYard].endsWith('rail yard'), `seed ${seed}: rail yard named "${names[G.railYard]}"`);
    const e = hqNeighbourToward(st, 'east'), w = hqNeighbourToward(st, 'west'), n = hqNeighbourToward(st, 'north');
    assert.ok(e >= 0 && w >= 0 && n >= 0 && e !== w && w !== n && e !== n, `seed ${seed}: east ${e} west ${w} north ${n}`);
    assert.equal(w, G.railYard, 'west is the rail yard (D-P4-12)');
    for (const i of [e, w, n]) assert.ok(st.nb[hq].includes(i), 'an HQ neighbour');
    assert.equal(blockNameAt(st, st.blocks[e].x, st.blocks[e].y), names[e]);
    assert.match(blockCoords(st, e), /^\(-?\d+,-?\d+\)$/);
    assert.match(streetName(st, hq, e), /^HQ's (north|south|east|west|north-east|north-west|south-east|south-west) street$/);
    assert.ok(bearingOf(st, hq, e).length > 0);
    // the goal line's east / west / north are the hour bot's (hour.ts neighbourToward, the same cosine rule on Dark candidates)
    assert.equal(e, neighbourToward(st, 'east'), 'east agrees with the bot');
    assert.equal(w, neighbourToward(st, 'west'), 'west agrees with the bot');
    st.blocks[e].state = HELD; st.blocks[w].state = HELD;
    assert.equal(n, neighbourToward(st, 'north'), 'north agrees with the bot once east and west are Held');
    st.blocks[e].state = DARK; st.blocks[w].state = DARK;
    // state-free: claiming, falling and digging never rename a block
    const bot = createHourBot(false);
    runHour(st, bot, 20 * 60);
    assert.deepEqual(st.blocks.map((_, i) => blockName(st, i)), names, `seed ${seed}: names unchanged after 20 minutes`);
  }
});

test('goal: a fresh city asks for hand-mined steel; a block-only state asks for the front; every id shown in 20 minutes is a §11 row in schedule order and never wrong against the state', () => {
  const st0 = city(3, false);
  assert.equal(currentGoal(st0).goal.id, 'front');
  const st = city(3);
  const g0 = currentGoal(st);
  assert.equal(g0.goal.id, 'mine');
  assert.match(g0.goal.text, /Mine steel/); assert.ok(g0.goal.why.length > 10, 'a reason');
  assert.equal(g0.support, null, 'nothing is short at 0:00');
  const bot = createHourBot(false), seen: string[] = [];
  let last = '', lastAt = -1, revisits = 0;
  const f = st.flow!;
  const oracle = (): string => {
    const g = currentGoal(st).goal, id = g.id;
    const gens = generators(st).length, shot = shotAssemblers(st).length;
    const dir = (d: 'east' | 'west' | 'north') => st.blocks[hqNeighbourToward(st, d)];
    switch (id) {
      case 'mine': return shot === 0 && f.stats.handCrafted < 10 && f.stats.handMined < 20 ? '' : 'mine while done';
      case 'craft': return shot === 0 && f.stats.handCrafted < 10 ? '' : 'craft while done';
      case 'coal-line': return patchExcavators(st, 'coal').length === 0 && patchLeft(st, 'coal') > 0 ? '' : 'coal line while dug';
      case 'gen-2': return gens < 2 ? '' : `gen-2 with ${gens}`;
      case 'steel-line': return patchExcavators(st, 'steel').length === 0 ? '' : 'steel line exists';
      case 'copper-line': return patchExcavators(st, 'copper').length === 0 ? '' : 'copper line exists';
      case 'shot-line': return shot === 0 ? '' : 'shot line exists';
      case 'steel-2': return patchExcavators(st, 'steel').length < 2 ? '' : 'steel-2 exists';
      case 'gen-3': return gens < 3 ? '' : `gen-3 with ${gens}`;
      case 'claim-east': return dir('east').state === DARK ? '' : 'claim east while not Dark';
      case 'contest-east': return dir('east').state === 1 ? '' : 'contest east while not Contested';
      case 'claim-west': return dir('west').state === DARK ? '' : 'claim west while not Dark';
      case 'contest-west': return dir('west').state === 1 ? '' : 'contest west while not Contested';
      case 'rail-coal': return dir('west').state === HELD ? '' : 'rail coal while west not Held';
      default: return `unexpected id ${id} in the first 20 minutes`;
    }
  };
  const end = f.tick + 20 * 60 * TILE_TPS;
  while (f.tick < end) {
    runHour(st, bot, 1);
    const g = currentGoal(st).goal;
    assert.ok((GOAL_ORDER as readonly string[]).includes(g.id), g.id);
    assert.equal(oracle(), '', `${st.t}s: ${g.id}`);
    if (g.id !== last) {
      if (seen.includes(g.id)) revisits++;
      const order = GOAL_ORDER as readonly string[];
      assert.ok(order.indexOf(g.id) > order.indexOf(last) || last === '' || g.id.startsWith('contest') || g.id.startsWith('retake'), `${st.t}s: ${last} → ${g.id} goes backward`);
      seen.push(g.id); last = g.id; lastAt = st.t;
    }
  }
  assert.ok(lastAt >= 0);
  assert.equal(revisits, 0, `no goal id came back within 20 minutes (${seen.join(' → ')})`);
  // the bot places Generator 3 and claims east in the same second, so a 1 Hz sample may see contest-east straight after gen-3
  assert.ok(seen.includes('craft') && seen.includes('coal-line') && seen.includes('shot-line') && (seen.includes('claim-east') || seen.includes('contest-east')), seen.join(' → '));
});

test('machineStatus: one of five words with a reason; the line at minute 10 has running Excavators, and an unpowered block reads off', () => {
  const st = city(3), bot = createHourBot(false);
  runHour(st, bot, 10 * 60);
  const f = st.flow!, words = new Set(['running', 'starved', 'blocked', 'idle', 'off']);
  let running = 0;
  for (const m of f.machines) {
    const s = machineStatus(st, m);
    assert.ok(words.has(s.state) && s.reason.length > 0, `${m.kind}: ${s.state} (${s.reason})`);
    if (m.kind === 'excavator' && s.state === 'running') running++;
  }
  assert.ok(running >= 1, `an Excavator is digging (${f.machines.filter(m => m.kind === 'excavator').map(m => machineStatus(st, m).reason).join('; ')})`);
  const gen = f.machines.find(m => m.kind === 'generator')!;
  assert.equal(machineStatus(st, gen).state, gen.busy ? 'running' : (gen.inv.coal ?? 0) > 0 ? 'idle' : 'starved');
  const asm = f.machines.find(m => m.kind === 'assembler')!;
  st.blocks[hqIdx(st)].subOn = false;
  assert.deepEqual(machineStatus(st, asm), { state: 'off', reason: 'no power' });
});

test('save: the hash ignores only the transients, a load validates and deep-copies, and a save at 1:30 replays to the unbroken run\'s hash at 3:00', () => {
  const a = city(4), bot = createHourBot(false), log: LoggedCommand[] = [];
  const h0 = stateHash(a);
  a.speed = 3; a.acc = 0.5; a.events.push({ type: 'claim', t: 0, x: 0, y: 0 } as never);
  assert.equal(stateHash(a), h0, `transients ${SAVE_TRANSIENT.join(', ')} do not move the hash`);
  a.events.length = 0; a.acc = 0;
  assert.throws(() => loadState({}), /not a Relight state: version/);
  assert.throws(() => loadState({ version: 1 }), /no blocks/);
  assert.throws(() => loadState(null), /not an object/);
  assert.equal(canonicalJson({ b: 1, a: [undefined, 2] }), '{"a":[null,2],"b":1}');
  runHour(a, bot, 90, log);
  const save = makeSave(a, { log, logComplete: true });
  assert.ok(isSaveFile(save) && save.t === a.t && save.tick === a.flow!.tick && save.hash === stateHash(a));
  const b = loadState(JSON.parse(JSON.stringify(save)));
  assert.notEqual(b, a); assert.notEqual(b.blocks, a.blocks);
  assert.equal(stateHash(b), save.hash, 'the loaded state hashes as the saved one');
  assert.equal(b.speed, 0); assert.equal(b.events.length, 0);
  const wrapped = loadState({ finalState: JSON.parse(JSON.stringify(a)) });
  assert.equal(stateHash(wrapped), save.hash, 'a telemetry export loads');
  // the unbroken run continues with the bot; the loaded copy replays the same commands
  const from = log.length;
  runHour(a, bot, 90, log);
  replay(b, log.slice(from), a.flow!.tick);
  assert.equal(b.flow!.tick, a.flow!.tick);
  assert.equal(stateHash(b), stateHash(a), 'the loaded state replays to the unbroken run');
});
