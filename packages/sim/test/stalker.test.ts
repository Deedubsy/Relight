/** RI-04 — the Stalker candidate (plan §7, §7.1; D-RI-5, D-RI-6): its numbers in one data configuration outside the
 *  benchmark; one Stalker at every Dark well block's home; guard → investigate → pursue → attack → return; the leash,
 *  the wind-up, no hit during a valid dodge, nothing after its death or the engineer's fall, a restored site retires
 *  it, a timed respawn out of perception; turrets and the rifle hit it out of the same hoppers and pockets. */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_CONFIG, createState, citySpec, SimState, SimEvent, Command, ensureFlow, advanceFlow, workbenchTile, ground, takeEvents, threatOf,
  enableStalkers, stalkersOf, stalkerAt, describeStalker, stalkerHp, stalkerSpeed, damageStalker, wellBlocks, Stalker, StalkerLayer,
  CANDIDATES, RETALIATE_HP_PER_S, ENGINEER_HP, ENEMIES, ROUND_DMG, HELD, DARK, walkable, flowSummary, makeSave, loadState, headingWord,
} from '../src/index';

type StalkerEvent = Extract<SimEvent, { type: 'stalker' }>;
const SITE = 377;   // seed 3's nearest well to the HQ (6 hops); its home is inside its lot with open ground all round
function city(seed = 3): SimState {
  const cfg = { ...DEFAULT_CONFIG, eco: { ...DEFAULT_CONFIG.eco } };
  const st = createState(citySpec(seed, 'river', cfg), cfg, seed);
  ensureFlow(st);
  st.buffer = 0;
  return st;
}
const farAway = (st: SimState) => { const e = st.engineer, [wx, wy] = workbenchTile(st); e.x = wx + 0.5; e.y = wy + 0.5; };
const dryTurrets = (st: SimState) => { for (const m of st.flow!.machines) if (m.kind === 'turret') m.inv.rounds = 0; };
const layer = (st: SimState): StalkerLayer => threatOf(st.flow!).stalk!;
const siteStalker = (st: SimState, site = SITE): Stalker => stalkersOf(st).find(s => s.site === site)!;
const stalkerEvents = (st: SimState): StalkerEvent[] => takeEvents(st).filter((ev): ev is StalkerEvent => ev.type === 'stalker');
/** The candidate on, the engineer far from every well, then `dist` tiles east of the site's home. */
function meet(st: SimState, dist = 6): Stalker {
  farAway(st);
  enableStalkers(st);
  const s = siteStalker(st), e = st.engineer;
  e.x = s.hx + dist; e.y = s.hy;
  return s;
}
/** Walk the engineer west toward the home and stop within `stopAt` of it; tick until `until` or `maxS` seconds. */
interface Approach { t: number; modes: string[]; hits: number[]; contactAt: number }
function approach(st: SimState, s: Stalker, until: (a: Approach) => boolean, maxS = 12, stopAt = 2): Approach {
  const e = st.engineer, a: Approach = { t: 0, modes: [], hits: [], contactAt: -1 }, { modes, hits } = a;
  let t = 0, walking = false;
  while (t < maxS - 1e-9 && !until(a)) {
    const cmds: Command[] = [];
    if (!walking) { cmds.push({ type: 'walk', dx: -1, dy: 0 }); walking = true; }
    if (e.vel[0] !== 0 && Math.hypot(e.x - s.hx, e.y - s.hy) <= stopAt) cmds.push({ type: 'walk', dx: 0, dy: 0 });
    advanceFlow(st, 0.05, cmds); t = +(t + 0.05).toFixed(2); a.t = t;
    for (const ev of stalkerEvents(st)) {
      if (ev.id !== s.id) continue;
      if (ev.what === 'investigate' || ev.what === 'pursue' || ev.what === 'attack' || ev.what === 'return' || ev.what === 'guard') modes.push(ev.what);
      if (ev.what === 'attack' && a.contactAt < 0) a.contactAt = t;
      if (ev.what === 'hit') hits.push(t);
    }
  }
  return a;
}

test('the candidate is one data configuration holding §7.1\'s values, outside SimConfig: the benchmark has no Stalker layer, no Stalkers, no stalker events', () => {
  const c = CANDIDATES.stalker;
  assert.deepEqual([c.perception, c.leash, c.hpMul, c.speedMul, c.windupS, c.attackS, c.attackHp], [8, 16, 2, 1.2, 0.8, 1, RETALIATE_HP_PER_S]);
  assert.equal(stalkerHp(c), ENEMIES[0].hp * 2, '2× a Crawler');
  assert.ok(Math.abs(stalkerSpeed(c) - ENEMIES[0].tilesPerSec * 1.2) < 1e-9, '1.2× a Crawler');
  assert.ok(!JSON.stringify(DEFAULT_CONFIG).toLowerCase().includes('stalker'), 'no SimConfig field (D-RI-5: the config hash is untouched)');
  const st = city(); farAway(st);
  advanceFlow(st, 5, []);
  assert.equal(threatOf(st.flow!).stalk, undefined, 'no layer until the switch');
  assert.equal(stalkersOf(st).length, 0); assert.equal(flowSummary(st).stalkers, 0);
  assert.equal(stalkerEvents(st).length, 0);
});

test('switched on, one Stalker guards each Dark well block (seed 3: five) at a passable home tile inside its site; none is fielded within perception of the engineer, but that site fills once they leave; the layer survives a save round trip', () => {
  const st = city(); farAway(st);
  const S = enableStalkers(st)!, G = ground(st);
  const sites = wellBlocks(st);
  assert.equal(sites.length, 5); assert.equal(S.stalkers.length, 5); assert.equal(flowSummary(st).stalkers, 5);
  assert.deepEqual(S.stalkers.map(s => s.site).sort((a, b) => a - b), [...sites].sort((a, b) => a - b), 'one per well block');
  for (const s of S.stalkers) {
    const b = st.blocks[s.site], tx = Math.floor(s.hx), ty = Math.floor(s.hy), t = ty * G.tw + tx;
    assert.equal(b.state, DARK, `site ${s.site} is Dark`);
    assert.ok(b.well, 'the well block');
    assert.equal(s.x, s.hx); assert.equal(s.y, s.hy); assert.equal(s.mode, 'guard'); assert.equal(s.hp, stalkerHp(S.cand));
    assert.ok(walkable(G, tx, ty), 'the home is walkable'); assert.equal(G.owner[t], s.site, 'the home is inside the site\'s lot');
    assert.equal(stalkerAt(st, s.x, s.y), s);
    assert.match(describeStalker(st, s), /^Stalker · guarding · 24\/24 HP · site \(\d+,\d+\) · leash 16 tiles from home$/);
  }
  assert.equal(stalkerEvents(st).filter(ev => ev.what === 'spawn').length, 5);
  // a switch thrown with the engineer inside perception of a home: that site waits
  const st2 = city(); farAway(st2);
  const home = siteStalker(st).hx; const homeY = siteStalker(st).hy;
  st2.engineer.x = home + 5; st2.engineer.y = homeY;
  const S2 = enableStalkers(st2)!;
  assert.equal(S2.stalkers.length, 4); assert.equal(siteStalker(st2), undefined, 'no spawn behind the engineer');
  farAway(st2); advanceFlow(st2, 0.05, []);
  assert.equal(S2.stalkers.length, 5); assert.ok(siteStalker(st2), 'fielded once the engineer left');
  // a save carries the layer and its candidate
  const ld = loadState(makeSave(st));
  assert.equal(stalkersOf(ld).length, 5); assert.equal(layer(ld).cand.perception, 8);
});

test('guard → investigate → pursue → attack: the engineer\'s movement inside perception draws it; the first hit lands one wind-up (0.8 s) after contact, then one hit a second for the retaliation value; it is inspectable throughout', () => {
  const st = city(), e = st.engineer, s = meet(st), S = layer(st);
  const r = approach(st, s, a => a.hits.length >= 4, 8);
  assert.deepEqual(r.modes.slice(0, 3), ['investigate', 'pursue', 'attack'], `the state machine in order: ${r.modes.join(' ')}`);
  assert.equal(S.stats.pursuits, 1);
  assert.ok(r.contactAt > 0 && r.hits.length >= 4, `contact at ${r.contactAt}, hits ${r.hits.join(' ')}`);
  const windup = r.hits[0] - r.contactAt;
  assert.ok(windup >= 0.7 && windup <= 0.85, `the first hit ${windup.toFixed(2)} s after contact (candidate 0.8 s, a 0.05 s tick)`);
  for (let k = 1; k < r.hits.length; k++) assert.ok(Math.abs(r.hits[k] - r.hits[k - 1] - S.cand.attackS) < 1e-6, `one hit a second: ${r.hits.join(' ')}`);
  assert.equal(e.hp, ENGINEER_HP - S.cand.attackHp * r.hits.length, `${S.cand.attackHp} HP a hit, not multiplied by the tick`);
  assert.equal(e.hurt, S.cand.attackHp * r.hits.length); assert.equal(S.stats.hits, r.hits.length); assert.equal(S.stats.attacks, r.hits.length);
  assert.equal(s.mode, 'attack'); assert.ok(s.warmed);
  assert.ok(Math.hypot(e.x - s.x, e.y - s.y) <= 1.2 + 1e-9, 'in contact');
  assert.ok(headingWord(s.dir) !== '·' && (e.x - s.x) * s.dir[0] + (e.y - s.y) * s.dir[1] > 0, 'faces the engineer');
  assert.match(describeStalker(st, s), /^Stalker · attacking · 24\/24 HP · site \(\d+,\d+\) · leash 16 tiles from home$/);
  assert.ok(S.stats.contactS >= r.hits.length - 1, `contact seconds ${S.stats.contactS.toFixed(2)}`);
});

test('a valid dodge: the swing that comes during the dash misses (no HP lost, a "dodged" event); the attack resumes after it', () => {
  const st = city(), e = st.engineer, s = meet(st), S = layer(st);
  const r = approach(st, s, a => a.hits.length >= 2, 8);
  assert.equal(r.hits.length, 2);
  // the next swing is due on the tick that empties `atk`: dodge on that tick
  let dodgedAt = -1, hpBefore = e.hp, t = r.t, guard = 0;
  while (dodgedAt < 0 && guard++ < 40) {
    const due = s.mode === 'attack' && Math.abs(s.atk - 0.05) < 1e-6;
    if (due) hpBefore = e.hp;
    advanceFlow(st, 0.05, due ? [{ type: 'dodge' }] : []); t = +(t + 0.05).toFixed(2);
    const evs = stalkerEvents(st).filter(ev => ev.id === s.id);
    if (due) {
      assert.ok(e.dash > 0 || e.dashCooldown > 0, 'the dash ran');
      assert.equal(evs.filter(ev => ev.what === 'hit').length, 0, 'no hit lands during a valid dodge');
      assert.equal(evs.filter(ev => ev.what === 'dodged').length, 1, 'the swing was dodged');
      assert.equal(e.hp, hpBefore, 'no HP lost');
      dodgedAt = t;
    } else assert.equal(evs.filter(ev => ev.what === 'hit').length, 0, 'no hit before the due tick');
  }
  assert.ok(dodgedAt > 0, 'a swing was dodged'); assert.equal(S.stats.dodged, 1); assert.equal(S.stats.hits, 2);
  // it pursues and lands again once the dash is over
  let hitAgain = -1;
  for (let k = 0; k < 60 && hitAgain < 0; k++) { advanceFlow(st, 0.05, []); t = +(t + 0.05).toFixed(2); if (stalkerEvents(st).some(ev => ev.id === s.id && ev.what === 'hit')) hitAgain = t; }
  assert.ok(hitAgain > dodgedAt, `the attack resumed at ${hitAgain}`); assert.equal(S.stats.hits, 3);
});

test('nothing after death: a Stalker killed mid-attack swings no more and its site remembers the time; the engineer down, it stops hitting and returns home', () => {
  const st = city(), e = st.engineer, s = meet(st), S = layer(st);
  const r = approach(st, s, a => a.hits.length >= 1, 8);
  assert.equal(r.hits.length, 1);
  assert.ok(damageStalker(st, S, s, s.hp), 'dead');
  assert.equal(stalkerEvents(st).filter(ev => ev.id === s.id && ev.what === 'dead').length, 1);
  assert.ok(!S.stalkers.includes(s)); assert.equal(S.stats.kills, 1); assert.equal(S.sites[SITE].diedAt, st.t);
  const hp0 = e.hp;
  advanceFlow(st, 3, []);
  assert.equal(e.hp, hp0, 'no attack after death');
  assert.equal(stalkerEvents(st).filter(ev => ev.id === s.id).length, 0, 'no events from a dead Stalker');
  assert.equal(siteStalker(st), undefined, 'no instant respawn');
  // the engineer's fall ends the attack: one hit downs a 5 HP engineer, then no more and the Stalker goes home
  const st2 = city(), e2 = st2.engineer, s2 = meet(st2), S2 = layer(st2);
  approach(st2, s2, a => a.hits.length >= 1, 8);
  e2.hp = S2.cand.attackHp;   // the next hit downs them (HP regenerates between hits, so it is set now, not before the walk)
  const r2 = approach(st2, s2, () => e2.down >= 0, 3);
  assert.ok(e2.down >= 0 && e2.hp === 0, 'down at the next hit'); assert.equal(r2.hits.length, 1);
  const hits = S2.stats.hits;
  let late = 0;
  for (let k = 0; k < 40; k++) { advanceFlow(st2, 0.05, []); for (const ev of takeEvents(st2)) if (ev.type === 'stalker' && ev.what === 'hit') late++; }
  assert.equal(late, 0, 'no hits on a downed engineer'); assert.equal(S2.stats.hits, hits);
  assert.ok(s2.mode === 'return' || s2.mode === 'guard', `it left: ${s2.mode}`);
});

test('the leash: pursuit never carries it past 16 tiles from home; it returns, reaches the home tile and guards again, warmed down', () => {
  const st = city(), e = st.engineer, s = meet(st, 4), L = layer(st);
  let maxDH = 0, returnedAt = -1, homeAt = -1, t = 0;
  const seen = new Set<string>();
  for (let k = 0; k < 20 * 40 && homeAt < 0; k++) {
    if (s.mode === 'pursue' || s.mode === 'attack') { e.x = s.x + 3; e.y = s.y; }   // always three tiles ahead of it, in sight
    else if (s.mode === 'guard' || s.mode === 'investigate') e.x += k % 2 ? 0.1 : -0.1;   // fidgeting: a cue every tick
    advanceFlow(st, 0.05, []); t = +(t + 0.05).toFixed(2);
    seen.add(s.mode);
    maxDH = Math.max(maxDH, Math.hypot(s.x - s.hx, s.y - s.hy));
    if (returnedAt < 0 && s.mode === 'return') returnedAt = t;
    if (returnedAt > 0 && s.mode === 'guard') homeAt = t;
  }
  const stepTiles = stalkerSpeed(L.cand) * 0.05;
  assert.ok(maxDH <= L.cand.leash + stepTiles + 1e-9, `farthest ${maxDH.toFixed(2)} tiles from home (leash ${L.cand.leash} + one step ${stepTiles.toFixed(2)})`);
  assert.ok(maxDH > L.cand.leash - 1, 'it did reach the leash');
  assert.ok(returnedAt > 0 && homeAt > returnedAt, `returned at ${returnedAt}, home at ${homeAt}`);
  assert.deepEqual([...seen].sort(), ['guard', 'investigate', 'pursue', 'return']);
  assert.equal(s.x, s.hx); assert.equal(s.y, s.hy); assert.ok(!s.warmed); assert.equal(s.mode, 'guard');
  assert.equal(e.hp, ENGINEER_HP, 'never caught');
  assert.ok(L.stalkers.includes(s), 'a survivor stays');
});

test('a restored site: the Stalker returns and retires at home; no respawn there ever after; elsewhere a dead Stalker returns respawnS later, and only with the engineer beyond perception of the home', () => {
  const st = city(), s = meet(st), S = layer(st);
  approach(st, s, () => s.mode === 'pursue', 4);
  assert.equal(s.mode, 'pursue');
  st.blocks[SITE].state = HELD;   // the site restored by hand: the test's scenario, not a claim
  advanceFlow(st, 0.05, []);
  assert.ok(S.sites[SITE].restored); assert.equal(s.mode, 'return');
  let retired = false;
  for (let k = 0; k < 20 * 30 && !retired; k++) { advanceFlow(st, 0.05, []); if (stalkerEvents(st).some(ev => ev.id === s.id && ev.what === 'retired')) retired = true; }
  assert.ok(retired, 'retired at home'); assert.equal(S.stats.retired, 1); assert.equal(siteStalker(st), undefined);
  farAway(st);
  advanceFlow(st, S.cand.respawnS + 5, []);
  assert.equal(siteStalker(st), undefined, 'a restored site fields no Stalker'); assert.equal(S.stalkers.length, 4);
  // respawn: dead at t0, back at t0 + respawnS — unless the engineer stands within perception of the home
  const st2 = city(); farAway(st2);
  const S2 = enableStalkers(st2)!, s2 = siteStalker(st2), t0 = st2.t;
  damageStalker(st2, S2, s2, s2.hp);
  advanceFlow(st2, S2.cand.respawnS - 1 - (st2.t - t0), []);
  assert.equal(siteStalker(st2), undefined, `none at ${st2.t - t0} s`);
  st2.engineer.x = s2.hx + 4; st2.engineer.y = s2.hy;
  advanceFlow(st2, 3, []);
  assert.equal(siteStalker(st2), undefined, 'not behind the engineer');
  farAway(st2);
  advanceFlow(st2, 0.05, []);
  const s3 = siteStalker(st2);
  assert.ok(s3 && s3.id !== s2.id && s3.x === s2.hx && s3.y === s2.hy, 'respawned at the home');
  assert.equal(S2.sites[SITE].spawned, 2); assert.equal(S2.stats.spawned, 6);
});

test('turrets and the rifle hit it (4 HP a round, 24 HP); its kills count on the Stalker layer, not as turret or rifle kills', () => {
  // a turret: parked inside the HQ turret's range it takes rounds out of the hopper
  const st = city(); farAway(st);
  const S = enableStalkers(st)!, s = siteStalker(st), T = threatOf(st.flow!);
  const m = st.flow!.machines.find(m => m.kind === 'turret' && (m.inv.rounds ?? 0) > 0)!;
  s.x = m.x + m.size / 2 + 2.5; s.y = m.y + m.size / 2;
  const rounds0 = m.inv.rounds!;
  let deadAt = -1;
  for (let k = 0; k < 20 * 10 && deadAt < 0; k++) { advanceFlow(st, 0.05, []); if (stalkerEvents(st).some(ev => ev.id === s.id && ev.what === 'dead')) deadAt = k * 0.05; }
  assert.ok(deadAt > 0, 'shot dead by the turret');
  assert.equal(rounds0 - m.inv.rounds!, stalkerHp(S.cand) / ROUND_DMG, 'six rounds');
  assert.equal(T.stats.turretKills, 0); assert.equal(S.stats.kills, 1); assert.equal(S.sites[SITE].diedAt, st.t);
  // the rifle: aimed at it from five tiles it dies in six rounds; its shots are a cue, so it closes and swings meanwhile
  const st2 = city(), e = st2.engineer; dryTurrets(st2); farAway(st2);
  const S2 = enableStalkers(st2)!, s2 = siteStalker(st2), T2 = threatOf(st2.flow!);
  s2.x = e.x + 5; s2.y = e.y; e.inv.magazine = 5;
  let killedAt = -1, t = 0;
  while (t < 8 && killedAt < 0) { advanceFlow(st2, 0.05, [{ type: 'aim', at: [s2.x, s2.y] }]); t += 0.05; if (!S2.stalkers.includes(s2)) killedAt = t; }
  assert.ok(killedAt > 0 && killedAt < 6, `rifled in ${killedAt.toFixed(2)} s`);
  assert.equal(e.kills, 1); assert.equal(T2.stats.rifleKills, 0); assert.equal(S2.stats.kills, 1);
  assert.ok(Math.round(e.fired) >= 6 && Math.round(e.fired) <= 8, `${e.fired} rounds`);
});
