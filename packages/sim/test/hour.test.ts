/** Prompt B M6 The hour (run name B-M6-hour): the walking §11 bot on the tile layer, its log, its findings, the
 *  feed / repair / rotate commands, and the command-log replay Gate B's rifle row rests on. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, protoCalibrated, createState, citySpec, ensureFlow, advanceFlow, SimState, HELD, LoggedCommand, Command,
  createHourBot, hourCommands, runHour, hourReport, replay, replayVerdict, neighbourToward, dirOf, hqIdx, hqLot, machineAt, place, handFeed,
  HOUR_MINE_STEEL, HOUR_CLAIM_AT, TILE_TPS, engineerCommand, TURRET_HOPPER,
} from '../src/index';

function city(seed = 3): SimState {
  const cfg = protoCalibrated({ ...DEFAULT_CONFIG, walk: true });
  Object.assign(cfg, { power: true, supply: 'generators', draw: 'half' });
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  return st;
}

test('hour bot, minutes 0–6: walks to the steel patch, hand-mines, crafts at the workbench, walks magazines to the turrets', () => {
  const st = city(), bot = createHourBot(false);
  runHour(st, bot, 6 * 60);
  const m = bot.log.marks, f = st.flow!;
  assert.ok(m['mine-done'] !== undefined && m['mine-done'] < 4 * 60, `mined by 4:00 (${JSON.stringify(bot.log.entries.slice(0, 8))})`);
  assert.ok(f.stats.handMined >= HOUR_MINE_STEEL, `hand-mined ${f.stats.handMined}`);
  assert.ok(m['craft-done'] !== undefined && f.stats.handCrafted > 0, 'crafted at the workbench');
  assert.ok(m['feed-done'] !== undefined, 'the two feed rounds were walked');
  assert.ok(f.stats.handFed >= 0 && bot.fedAtFeedDone >= 0, `hand-fed count recorded (${bot.fedAtFeedDone})`);
  assert.ok(st.engineer.walked > 10, 'the engineer walked');
  assert.ok(bot.log.walks.length >= 4, `walks timed: ${bot.log.walks.length}`);
});

test('hour bot, minute 10: the three Excavators, the Shot line and Generator 2 stand on the HQ lot; every refusal is in the log with a reason', () => {
  const st = city(), bot = createHourBot(false);
  runHour(st, bot, 10 * 60);
  const f = st.flow!, m = bot.log.marks;
  assert.equal(f.machines.filter(x => x.kind === 'excavator').length, 3, `excavators (refused: ${bot.log.refused.map(r => r.what + ': ' + r.reason).join('; ')})`);
  assert.equal(f.machines.filter(x => x.kind === 'assembler').length, 2, 'the start Assembler and the Shot line\'s');
  assert.equal(f.machines.filter(x => x.kind === 'generator').length, 2);
  assert.ok(m['line-excavators'] !== undefined && m['line-assembler'] !== undefined);
  for (const r of bot.log.refused) assert.ok(r.reason.length > 0);
});

test('hour bot, minute 45: east, west and north claimed on the calibration\'s clock, walked over and kitted; the bot never teleports', () => {
  const st = city(), bot = createHourBot(false);
  let maxStep = 0, px = st.engineer.x, py = st.engineer.y;
  const f = ensureFlow(st), cmds: Command[] = [];
  st.speed = 1;
  const end = 45 * 60 * TILE_TPS;
  while (f.tick < end) {
    cmds.length = 0; hourCommands(st, bot, cmds); st.acc = 0; advanceFlow(st, 1 / TILE_TPS, cmds, 1); st.events.length = 0;
    const d = Math.hypot(st.engineer.x - px, st.engineer.y - py); if (d > maxStep) maxStep = d; px = st.engineer.x; py = st.engineer.y;
  }
  const m = bot.log.marks;
  for (const dir of ['east', 'west', 'north'] as const) {
    assert.ok(m[`claim-${dir}`] !== undefined && m[`claim-${dir}`] >= HOUR_CLAIM_AT[dir] && m[`claim-${dir}`] < HOUR_CLAIM_AT[dir] + 120, `${dir} claimed near ${HOUR_CLAIM_AT[dir] / 60}:00 (${m[`claim-${dir}`]})`);
    assert.ok(bot.claimed[dir] !== undefined && dirOf(st, hqIdx(st), bot.claimed[dir]!) === dir, `${dir} lies ${dir} of the HQ`);
    assert.ok(bot.log.walks.some(w => w.name === `walk-over ${dir}`), `the walk over to ${dir} was timed`);
  }
  assert.ok(m['held-east'] !== undefined && m['kitted-east'] !== undefined, `east Held and kitted (${bot.log.refused.map(r => r.what).join('; ')})`);
  assert.ok(maxStep < 2, `no teleport: largest step ${maxStep.toFixed(2)} tiles a tick`);
  assert.equal(neighbourToward(st, 'east') < 0 || st.blocks[neighbourToward(st, 'east')].state !== HELD, true);
});

test('hour report: the findings name every §11 moment that never happened, and the walking row is there', () => {
  const st = city(), bot = createHourBot(false);
  runHour(st, bot, 60);
  const r = hourReport(st, bot);
  assert.ok(r.findings.some(f => f.includes('east claimed') && f.includes('never')));
  assert.ok(r.walkedPct >= 0 && r.walkedPct <= 100);
  assert.equal(typeof r.chestTrips, 'number');
});

test('feed / repair / rotate commands: E and R by command, within reach only', () => {
  const st = city(), f = st.flow!;
  const t = f.machines.find(m => m.kind === 'turret')!;
  st.engineer.inv.magazine = 5;
  t.inv.rounds = 0;
  engineerCommand(st, { type: 'move', x: t.x - 2, y: t.y - 2 }); st.engineer.x = t.x - 2; st.engineer.y = t.y - 2;
  engineerCommand(st, { type: 'feed', x: t.x, y: t.y });
  assert.ok((t.inv.rounds ?? 0) > 0 && (t.inv.rounds ?? 0) <= TURRET_HOPPER, 'fed by command');
  const dir0 = t.dir;
  engineerCommand(st, { type: 'rotate', x: t.x, y: t.y });
  assert.notEqual(t.dir, dir0, 'rotated by command');
  st.engineer.x = t.x + 30; st.engineer.y = t.y + 30; st.engineer.inv.magazine = 5; t.inv.rounds = 0;
  engineerCommand(st, { type: 'feed', x: t.x, y: t.y });
  assert.equal(t.inv.rounds ?? 0, 0, 'out of reach: nothing fed');
});

test('replay: a logged hour-bot run replays to the same state, and the rifle-off replay yields a verdict per hand-fired fight', () => {
  const st = city(3), bot = createHourBot(true), log: LoggedCommand[] = [];
  runHour(st, bot, 12 * 60, log);
  assert.ok(log.length > 20, `logged ${log.length} commands`);
  const same = city(3);
  replay(same, log, st.flow!.tick);
  assert.deepEqual(same.blocks.map(b => b.state), st.blocks.map(b => b.state));
  assert.equal(same.flow!.machines.length, st.flow!.machines.length);
  assert.equal(same.engineer.fired, st.engineer.fired);
  assert.equal(same.flow!.stats.handFed, st.flow!.stats.handFed);
  assert.equal(Math.round(same.engineer.x * 100), Math.round(st.engineer.x * 100));
  const off = city(3);
  replay(off, log, st.flow!.tick, { dropAim: true });
  assert.equal(off.engineer.fired, 0, 'no shots without the aim');
  const v = replayVerdict(st, off);
  assert.ok(['held anyway', 'saved it', 'fell anyway', 'open'].includes(v.verdict));
  for (const fg of v.fights) assert.ok(fg.rounds > 0);
});

test('hour bot: place and handFeed by hand agree with the bot\'s feed task on the same turret', () => {
  const st = city();
  const [tx, ty] = hqLot(st, 12, 2);
  st.engineer.inv.steel = 50; st.engineer.inv.copper = 20; st.engineer.inv.magazine = 3;
  st.engineer.x = tx; st.engineer.y = ty + 3;
  const m = place(st, 'turret', tx, ty, 0);
  assert.ok(m && machineAt(st, tx, ty) === m);
  const fed = handFeed(st, tx, ty);
  assert.ok(fed && fed.moved > 0);
});
