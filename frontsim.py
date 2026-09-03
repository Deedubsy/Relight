"""Throwaway headless sim of the front rule (RELIGHT-design.md).
Block grid, cellular rot with district caps and wells, bloom timers, ammo demand.
Compares expansion policies at equal block counts.

Default run (no flags) reproduces the original sim's output exactly.
New models are all opt-in flags so experiments can isolate one at a time:
  --production          assemblers on a schedule, per-edge hoppers, ammo ring
  --unfed=none|substation|creep   consequence of an edge with empty hoppers
  --unfed-n=N           crawler-arrivals at the substation that disable it
  --starve-quiet        ring gives nothing to edges whose dark block has d<0.3
  --power               substation + machine draw, supply, brownout shedding
  --supply=track|schedule   player keeps 500 kW headroom (recomputed each 10 min) | §15 schedule
  --shortfall=PCT,HOUR,MIN  impose a supply shortfall
  --draw=doc|flat|half  200/40 kW per §5 | 120 kW every substation | 100/20 kW (D1, calibration 2)
  --shed=substations|machines-first
  --fall-tiles=N        creep tiles from lights-out to the substation (30 = 90 s; 16 = lot-centre, 48 s)
  --interior-grace=S    newly exposed interior keeps 40 kW draw for S seconds
  --scatter / --no-scatter   8-10 % scattered inert cells (seeded)
  --validator=none|no2x2|maxrun2|inert-dark
  --shade-threshold, --hulk-threshold, --wake-cap, --bloom-base, --asm-early-rate
  --policy, --hours, --seed, --first-hour, --experiments
"""
import random, math, sys, argparse, statistics

W, H = 24, 22            # city blocks; row 21 = river (inert)
START = (12, 20)
TARGET = (12, 10)        # "the foundry", 10 blocks north
WELLS = [(12, 8), (2, 2), (21, 3), (2, 18), (22, 19)]

def district(x, y):
    d = abs(x-START[0]) + abs(y-START[1])
    if y < 4 or x < 3 or x > 20:      base, g, name = 1.0, 0.0008, 'out'
    elif y < 11:                       base, g, name = 0.6, 0.0006, 'ind'
    elif y < 17:
        if 9 <= x <= 15:               base, g, name = 0.6, 0.0006, 'ind'
        else:                          base, g, name = 0.45, 0.0005, 'res'
    else:
        if x % 3 == 0:                 base, g, name = 0.45, 0.0005, 'res'
        else:                          base, g, name = 0.3, 0.0004, 'civ'
    dmax = min(1.0, base * (1 + 0.3 * d / 20))
    infl = 0.0
    for wx, wy in WELLS:
        wd = abs(x-wx) + abs(y-wy)
        if wd <= 3: infl = max(infl, 1 - wd/4)
    dmax = min(1.0, dmax + 0.3*infl)
    g *= (1 + 3*infl)
    return dmax, g, name

def wellname(x, y):
    return any(abs(x-wx)+abs(y-wy) <= 3 for wx, wy in WELLS)

DEFAULTS = dict(
    production=False, asm_schedule=((600,1),(1800,2),(3000,3),(10800,4)),
    asm_rate=20.0, asm_early_rate=None, hopper=100, buffer_cap=4000, start_rounds=200,
    unfed='none', unfed_n=20, starve_quiet=False,
    power=False, supply='track', headroom=500.0, shortfall=None, draw='doc',
    shed='substations', interior_grace=0,
    scatter=False, scatter_frac=0.09, validator='none',
    shade_thr=0.3, hulk_thr=0.5, wake_cap=False, bloom_base=4.0, jitter=0.0, asm_track=0.0, fall_tiles=30,
    hulk_roll='random',   # 'hash': deterministic per (seed, t, x, y); used by export_fixtures.py so the TS port matches shells exactly
    ring_order='sorted',  # edges created in the same tick join the ring sorted by position; 'set' = Python set order (pre-port behaviour)
    bloom_t=120.0, bloom_drop=0.9,   # bloom timer T = bloom_t/(0.5+d); d *= bloom_drop per bloom (E10)
    relight=None,         # (hour, minutes, well_g_mult): wells' growth x mult for the window (E11)
    gap_after=5*60,       # claim gap after hour one when claim_gap is None; 5 min = what §18 is drawn at (E9); was 8 min
)

def hash01(seed, t, x, y):
    """Deterministic uniform in [0,1) from integers; bit-identical in packages/sim/src/prng.ts."""
    M = 0xFFFFFFFF
    h = (seed ^ 0x9E3779B9) & M
    for v in (t, x, y):
        h = ((h ^ (v & M)) * 0x9E3779B1) & M
        h ^= h >> 15
    h ^= h >> 16; h = (h * 0x85EBCA6B) & M; h ^= h >> 13; h = (h * 0xC2B2AE35) & M; h ^= h >> 16
    return h / 4294967296.0

class Block:
    __slots__ = ('x','y','state','d','dmax','g','name','timer','contest_until','upto','awake',
                 'sub_on','shed','creep','unfed','fed_timer','shade_off','exposed','exposed_at','well','unfed_since','starve_since')
    def __init__(s, x, y):
        s.x, s.y = x, y
        s.dmax, s.g, s.name = district(x, y)
        s.well = wellname(x, y)
        s.d = 0.5 * s.dmax
        s.state = 'inert' if y == 21 else 'dark'
        s.timer = None; s.contest_until = 0; s.upto = 0; s.awake = False
        s.sub_on = True; s.shed = False; s.creep = 0.0; s.unfed = 0.0; s.fed_timer = 0
        s.shade_off = 0; s.exposed = True; s.exposed_at = 0; s.unfed_since = None; s.starve_since = None

def neighbours(x, y):
    for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
        nx, ny = x+dx, y+dy
        if 0 <= nx < W and 0 <= ny < H: yield nx, ny

NB = {(x,y): tuple(neighbours(x,y)) for x in range(W) for y in range(H)}

def make_scatter(seed, frac, validator):
    """Seeded scattered inert cells. Same map for every policy at a given seed."""
    rng = random.Random(seed*7919 + 13)
    cells = [(x,y) for x in range(W) for y in range(H-1)]
    protect = {START, TARGET, *WELLS, *NB[START]}
    target = round(frac*len(cells))
    inert = set()
    tries = 0
    while len(inert) < target and tries < 20000:
        tries += 1
        p = rng.choice(cells)
        if p in inert or p in protect: continue
        x, y = p
        ok = True
        if validator == 'no2x2':
            for dx in (-1, 1):
                for dy in (-1, 1):
                    q = {(x+dx,y), (x,y+dy), (x+dx,y+dy)}
                    if all(c in inert or (c[1] == H-1) for c in q): ok = False
        elif validator == 'maxrun2':
            for ax in ((1,0),(0,1)):
                run_ = 1
                for sgn in (1,-1):
                    k = 1
                    while True:
                        c = (x+ax[0]*k*sgn, y+ax[1]*k*sgn)
                        if c in inert: run_ += 1; k += 1
                        else: break
                if run_ > 2: ok = False
        if ok: inert.add(p)
    return inert

def run(policy, hours=5.0, seed=1, log=True, claim_gap=None, **kw):
    cfg = dict(DEFAULTS); cfg.update(kw)
    random.seed(seed)
    grid = {(x,y): Block(x,y) for x in range(W) for y in range(H)}
    if cfg['jitter']:
        jr = random.Random(seed*104729 + 7)
        for b in grid.values(): b.d = 0.5*b.dmax*(1 + jr.uniform(-cfg['jitter'], cfg['jitter']))
    scatter = set()
    if cfg['scatter']:
        scatter = make_scatter(seed, cfg['scatter_frac'], cfg['validator'])
        for p in scatter:
            grid[p].state = 'void' if cfg['validator'] == 'inert-dark' else 'inert'
    grid[START].state = 'held'; grid[START].d = 0
    blocks = list(grid.values())
    t = 0; dt = 1
    blooms = []            # (t, rounds, shells)
    brec = []              # (t, district, well, wake, crawlers, shades, hulk, rounds, shells, n_edges)
    first_interior = None
    next_claim = 15*60
    out = []
    stats = dict(lost=0, lost_log=[], retakes=0, unfed_total=0.0, first_unfed=None, first_fall=None,
                 first_brownout=None, shed_events=0, min_asm=None, edge_min={}, awake_min={},
                 dist_mags={}, ratios={}, hopper_samples=[], buffer_samples=[], demand_kw=[], supply_kw=[],
                 lost_in_window=0, lost_after_window=0, claims=0, max_creep_blocks=0, lost_by_hour={}, fall_delays=[], shed_log=[], asm_at={})
    prod = cfg['production']; pw = cfg['power']
    edges = {}; ring = []
    buffer = float(cfg['start_rounds']) if prod else 0.0
    engagements = []
    asm_active = 0; asm_shed = 0
    supply = 300.0; over_timer = 0; shed_stack = []; last_shed = -999
    fallen = set()
    dirty = True

    def held(p): return grid[p].state == 'held'
    def solid(p): return grid[p].state in ('held', 'inert')
    def hostile(p): return grid[p].state in ('dark', 'contested')
    def frontage():
        return sum(1 for b in blocks if b.state=='held' for n in NB[(b.x,b.y)] if hostile(n))
    def front_blocks():
        return sum(1 for b in blocks if b.state=='held' and any(hostile(n) for n in NB[(b.x,b.y)]))
    def interior():
        return sum(1 for b in blocks if b.state=='held' and all(solid(n) for n in NB[(b.x,b.y)]))
    def frontage_if(p):
        b = grid[p]; old = b.state; b.state = 'held'; f = frontage(); b.state = old; return f
    def candidates():
        return [p for p,b in grid.items() if b.state=='dark' and any(held(n) for n in NB[p])]
    rl = cfg['relight']
    def g_of(b):
        if rl and b.well and rl[0]*3600 <= t < rl[0]*3600 + rl[1]*60: return b.g * rl[2]
        return b.g
    def catch_up(b, upto):
        n = upto - b.upto
        if n <= 0: return
        g = g_of(b)
        if n == 1: b.d += g * (b.dmax - b.d)
        else: b.d = b.dmax - (b.dmax - b.d) * (1 - g) ** n
        b.upto = upto
    def recompute_awake(p):
        b = grid[p]
        if b.state != 'dark': b.awake = False; return
        aw = any(grid[n].state in ('held','contested') for n in NB[p])
        if not aw: b.timer = None
        b.awake = aw
    def state_change(p):
        nonlocal dirty
        dirty = True
        recompute_awake(p)
        for n in NB[p]: recompute_awake(n)
    def choose():
        c = candidates()
        if not c: return None
        if fallen:
            f = [p for p in c if p in fallen]
            if f: return min(f, key=lambda p: abs(p[0]-START[0])+abs(p[1]-START[1]))
        def dist(p): return abs(p[0]-TARGET[0]) + abs(p[1]-TARGET[1])
        if policy == 'compact':
            return min(c, key=lambda p: (frontage_if(p), grid[p].d, abs(p[0]-START[0])+abs(p[1]-START[1])))
        if policy == 'spike':
            return min(c, key=lambda p: (dist(p), grid[p].d))
        if policy == 'balanced':
            return min(c, key=lambda p: (frontage_if(p) + dist(p), grid[p].d))
        if policy == 'cheapest':
            return min(c, key=lambda p: (grid[p].d, frontage_if(p)))
        if policy == 'river':
            def adj_inert(p): return sum(1 for n in NB[p] if grid[n].state == 'inert')
            return min(c, key=lambda p: (-adj_inert(p), frontage_if(p), grid[p].d))
        return None
    def roll(p):
        if cfg['hulk_roll'] == 'hash': return hash01(seed, t, p[0], p[1])
        return random.random()
    def bloom_size(d, p=None):
        crawlers = round(cfg['bloom_base'] + 36*d)
        shades = crawlers // 8 if d >= cfg['shade_thr'] else 0
        hulk = 1 if (d >= cfg['hulk_thr'] and roll(p) < 0.4) else 0
        return crawlers, shades, hulk
    def wake_bloom(d, p=None):
        cr, sh, hu = bloom_size(d, p)
        if cfg['wake_cap']:
            cr2 = min(40, 2*cr); sh2 = cr2 // 8 if d >= cfg['shade_thr'] else 0
            return cr2, sh2, 2*hu
        return 2*cr, 2*sh, 2*hu
    def n_edges_of(p):
        return sum(1 for n in NB[p] if grid[n].state == 'held')
    def record_bloom(p, cr, sh, hu, wake):
        b = grid[p]
        rounds = cr*3 + sh*10; shells = hu*10
        blooms.append((t, rounds, shells))
        ne = n_edges_of(p)
        brec.append((t, b.name, b.well, wake, cr, sh, hu, rounds, shells, ne))
        if prod and ne > 0:
            keys = [(n, p) for n in NB[p] if grid[n].state == 'held']
            for i, k in enumerate(keys):
                engagements.append(dict(key=k, cr=cr/len(keys), sh=sh/len(keys),
                                        rcr=cr/len(keys)/15.0, rsh=sh/len(keys)/15.0))
    asm_n = [0, -1]                      # [count, last 10-min mark evaluated]
    def asm_count(tt):
        if cfg['asm_track']:
            if tt >= 600 and tt % 600 == 0 and asm_n[1] != tt:
                asm_n[1] = tt
                recent = sum(x[1] for x in blooms if x[0] > tt-600)/100.0   # mag/min over last 10 min
                if asm_n[0] == 0: asm_n[0] = 1
                elif recent / (asm_n[0]*rate(tt)) > cfg['asm_track'] and asm_n[0] < 8: asm_n[0] += 1
            return asm_n[0]
        n = 0
        for at, k in cfg['asm_schedule']:
            if tt >= at: n = k
        return n
    def rate(tt):
        if cfg['asm_early_rate'] is not None and tt < 3*3600: return cfg['asm_early_rate']
        return cfg['asm_rate']
    def sync_edges():
        nonlocal buffer
        cur = set()
        for b in blocks:
            if b.state != 'held': continue
            for n in NB[(b.x,b.y)]:
                if hostile(n): cur.add(((b.x,b.y), n))
        for k in list(edges):
            if k not in cur:
                buffer += edges[k]['hopper']; del edges[k]; ring.remove(k)
        for k in (sorted(cur) if cfg['ring_order'] == 'sorted' else cur):
            if k not in edges:
                edges[k] = dict(hopper=0.0, empty=0); ring.append(k)
    def sub_draw(b, unshed=False):
        if not unshed and (not b.sub_on or t < b.shade_off): return 0.0
        if cfg['draw'] == 'flat': return 120.0
        half = cfg['draw'] == 'half'            # D1 (calibration 2): 100/20 kW
        if not b.exposed: return 20.0 if half else 40.0
        if cfg['interior_grace'] and t - b.exposed_at < cfg['interior_grace']: return 20.0 if half else 40.0
        return 100.0 if half else 200.0
    def demand_unshed():
        return sum(sub_draw(b, True) for b in blocks if b.state == 'held') \
               + sum(100.0 if cfg['draw'] == 'half' else 200.0 for b in blocks if b.state == 'contested') + asm_count(t)*220.0
    def demand_kw():
        dsum = 0.0
        for b in blocks:
            if b.state == 'held': dsum += sub_draw(b)
            elif b.state == 'contested': dsum += 100.0 if cfg['draw'] == 'half' else 200.0
        return dsum + (asm_active) * 220.0
    def effective_supply():
        s = supply
        if cfg['supply'] == 'schedule':
            sched = ((0,300),(1800,600),(3600,900),(7200,1200),(10800,1500),(14400,1800),(18000,2100),(21600,7000))
            s = 300.0
            for at, kw_ in sched:
                if t >= at: s = float(kw_)
        sf = cfg['shortfall']
        if sf and sf[1]*3600 <= t < sf[1]*3600 + sf[2]*60: s *= (1 - sf[0]/100.0)
        return s
    def in_window():
        sf = cfg['shortfall']
        return bool(sf) and sf[1]*3600 <= t < sf[1]*3600 + sf[2]*60
    def fall(p, reason):
        b = grid[p]
        b.state = 'dark'; b.d = 0.3; b.upto = t + 1; b.timer = None; b.creep = 0.0; b.unfed = 0.0
        b.sub_on = True; b.shed = False; b.shade_off = 0; b.fed_timer = 0
        if p in shed_stack: shed_stack.remove(p)
        delay = (t - b.unfed_since) if b.unfed_since is not None else None
        starved = [n for n in NB[p] if (p, n) in edges and edges[(p, n)]['hopper'] <= 1e-9]
        sd = ('well' if grid[starved[0]].well else grid[starved[0]].name) if starved else '-'
        stats['lost'] += 1; stats['lost_log'].append((t, reason, p, delay, sd))
        stats['lost_by_hour'][t//3600] = stats['lost_by_hour'].get(t//3600, 0) + 1
        sdelay = (t - b.starve_since) if b.starve_since is not None else None
        if delay is not None: stats['fall_delays'].append((sd, delay/60.0, reason, None if sdelay is None else sdelay/60.0))
        b.unfed_since = None; b.starve_since = None
        if stats['first_fall'] is None: stats['first_fall'] = t
        sf = cfg['shortfall']
        if sf:
            if in_window(): stats['lost_in_window'] += 1
            elif t >= sf[1]*3600 + sf[2]*60: stats['lost_after_window'] += 1
        fallen.add(p)
        state_change(p)
        if prod: sync_edges()      # edges change now, not at the next claim

    state_change(START)
    while t < hours*3600:
        # claims
        if policy != 'turtle' and t >= next_claim:
            p = choose()
            if p:
                b = grid[p]
                catch_up(b, t)
                cr, sh, hu = wake_bloom(b.d, p)
                record_bloom(p, cr, sh, hu, True)
                b.state = 'contested'; b.contest_until = t + 20 + 60*b.d
                b.awake = False
                if p in fallen: stats['retakes'] += 1
                stats['claims'] += 1
                state_change(p)
            gap = claim_gap if claim_gap else (15*60 if t < 3600 else cfg['gap_after'])
            next_claim = t + gap
        # contested -> held
        for b in blocks:
            if b.state == 'contested' and t >= b.contest_until:
                b.state = 'held'; b.d = 0; b.timer = None; b.sub_on = True; b.creep = 0.0
                b.unfed = 0.0; b.shed = False; b.shade_off = 0; b.unfed_since = None; b.starve_since = None
                fallen.discard((b.x, b.y))
                state_change((b.x, b.y))
        # rot growth + blooms in awake dark blocks (asleep blocks grow lazily, closed form)
        for b in blocks:
            if b.state != 'dark' or not b.awake: continue
            catch_up(b, t+1)
            if b.timer is None: b.timer = t + cfg['bloom_t']/(0.5+b.d)
            if t >= b.timer:
                if rl and rl[0]*3600 <= t < rl[0]*3600 + rl[1]*60: cr, sh, hu = wake_bloom(b.d, (b.x, b.y))
                else: cr, sh, hu = bloom_size(b.d, (b.x, b.y))
                record_bloom((b.x,b.y), cr, sh, hu, False)
                b.d = max(0.05, b.d*cfg['bloom_drop'])
                b.timer = t + cfg['bloom_t']/(0.5+b.d)
        # exposure bookkeeping (power) and edges (production)
        if dirty:
            for b in blocks:
                if b.state != 'held': continue
                ex = any(hostile(n) for n in NB[(b.x,b.y)])
                if ex and not b.exposed: b.exposed_at = t
                b.exposed = ex
            if prod: sync_edges()
        # ---- ammo production and the ring ----
        if prod:
            asm_active = max(0, asm_count(t) - asm_shed)
            if stats['min_asm'] is None or asm_active < stats['min_asm']: stats['min_asm'] = asm_active
            avail = buffer + asm_active * rate(t) / 6.0     # rounds per second
            buffer = 0.0
            for k in ring:
                e = edges[k]
                if cfg['starve_quiet'] and k[0] != START and grid[k[1]].d < 0.3 and grid[k[1]].state == 'dark':
                    if grid[k[0]].starve_since is None: grid[k[0]].starve_since = t
                    continue
                need = cfg['hopper'] - e['hopper']
                give = min(need, avail)
                if give > 0: e['hopper'] += give; avail -= give
            buffer = min(cfg['buffer_cap'], avail)
            # engagements: crawlers arrive over 15 s; fed ones die, unfed ones proceed
            keep = []
            for en in engagements:
                k = en['key']
                if k not in edges: continue
                e = edges[k]; hp = grid[k[0]]
                a = min(en['cr'], en['rcr']); en['cr'] -= a
                fed = min(a, e['hopper']/3.0); e['hopper'] -= fed*3.0
                un = a - fed
                s_ = min(en['sh'], en['rsh']); en['sh'] -= s_
                fed_s = min(s_, e['hopper']/10.0); e['hopper'] -= fed_s*10.0
                un_s = s_ - fed_s
                if un > 1e-9 or un_s > 1e-9:
                    stats['unfed_total'] += un
                    if stats['first_unfed'] is None: stats['first_unfed'] = t
                    if hp.unfed_since is None: hp.unfed_since = t
                    if cfg['unfed'] == 'substation':
                        hp.unfed += un
                        if un_s > 1e-9: hp.shade_off = max(hp.shade_off, t) + 30*un_s
                if en['cr'] > 1e-9 or en['sh'] > 1e-9: keep.append(en)
            engagements[:] = keep
            for k, e in edges.items():
                e['empty'] = e['empty'] + 1 if e['hopper'] <= 1e-9 else 0
            if cfg['unfed'] != 'none':
                for b in blocks:
                    if b.state != 'held': continue
                    p = (b.x, b.y)
                    mine = [edges[(p, n)] for n in NB[p] if (p, n) in edges]
                    if cfg['unfed'] == 'substation':
                        if all(e['hopper'] > 1e-9 for e in mine):
                            b.fed_timer += 1
                            if b.fed_timer >= 60:
                                b.unfed = 0.0; b.unfed_since = None
                        else: b.fed_timer = 0
                        if b.unfed >= cfg['unfed_n']: b.sub_on = False
                        elif not b.shed and b.unfed < cfg['unfed_n'] and not b.sub_on and b.fed_timer >= 60:
                            b.sub_on = True
                    elif cfg['unfed'] == 'creep':
                        if any(e['empty'] > 60 for e in mine):
                            if b.unfed_since is None: b.unfed_since = t - 60
                            b.creep += 1/3.0
                            if b.creep >= cfg['fall_tiles']: fall(p, 'starved')
            if t % 60 == 0:
                if edges:
                    stats['hopper_samples'].append(sum(e['hopper'] for e in edges.values())/len(edges))
                stats['buffer_samples'].append(buffer)
        # ---- power ----
        if pw:
            if not prod: asm_active = max(0, asm_count(t) - asm_shed)
            dem = demand_kw()
            if cfg['supply'] == 'track' and t % 600 == 0 and not in_window():
                supply = max(supply, demand_unshed() + cfg['headroom'])
            eff = effective_supply()
            if t % 60 == 0: stats['demand_kw'].append(demand_unshed()); stats['supply_kw'].append(eff)
            if dem > eff:
                over_timer += 1
                if stats['first_brownout'] is None: stats['first_brownout'] = t
                if over_timer >= 20:
                    over_timer = 0; last_shed = t; stats['shed_events'] += 1; stats['shed_log'].append(t)
                    if cfg['shed'] == 'machines-first' and asm_shed < asm_count(t):
                        asm_shed += 1; shed_stack.append('asm')
                    else:
                        cands = [b for b in blocks if b.state=='held' and b.sub_on and not b.shed]
                        if cands:
                            v = max(cands, key=lambda b: (sum(1 for n in NB[(b.x,b.y)] if hostile(n)),
                                                          abs(b.x-START[0])+abs(b.y-START[1])))
                            v.sub_on = False; v.shed = True; shed_stack.append((v.x, v.y))
            else:
                over_timer = 0
                if shed_stack and t - last_shed >= 20:
                    u = shed_stack[-1]
                    if u == 'asm':
                        if dem + 220 <= eff: shed_stack.pop(); asm_shed -= 1; last_shed = t
                    else:
                        b = grid[u]
                        if b.state != 'held': shed_stack.pop()
                        else:
                            b.sub_on = True; b.shed = False
                            if demand_kw() <= eff: shed_stack.pop(); last_shed = t
                            else: b.sub_on = False; b.shed = True
            if not prod: asm_active = max(0, asm_count(t) - asm_shed)
        # ---- creep and fall (substation off for any reason) ----
        if prod or pw:
            ncreep = 0
            for b in blocks:
                if b.state != 'held': continue
                on = b.sub_on and t >= b.shade_off
                if not on:
                    b.creep += 1/3.0; ncreep += 1
                    if b.creep >= cfg['fall_tiles']:
                        fall((b.x, b.y), 'brownout' if b.shed else ('shade' if t < b.shade_off else 'unfed'))
                elif b.creep > 0:
                    b.creep = max(0.0, b.creep - 1/20.0)
            stats['max_creep_blocks'] = max(stats['max_creep_blocks'], ncreep)
        if dirty:
            if first_interior is None and interior() > 0: first_interior = t
            dirty = False
        # per-district edge-minutes and awake-block-minutes (after first hour)
        if t % 60 == 0 and t >= 3600:
            for b in blocks:
                if b.state == 'held':
                    for n in NB[(b.x,b.y)]:
                        if grid[n].state == 'dark':
                            key = ('well' if grid[n].well else grid[n].name)
                            stats['edge_min'][key] = stats['edge_min'].get(key, 0) + 1
                elif b.state == 'dark' and b.awake:
                    key = ('well' if b.well else b.name)
                    stats['awake_min'][key] = stats['awake_min'].get(key, 0) + 1
        t += dt
        if t % 3600 == 0:
            recent = [x for x in blooms if x[0] > t-600]
            mags = sum(x[1] for x in recent)/10/10   # per minute
            shells = sum(x[2] for x in recent)/10
            nheld = sum(1 for b in blocks if b.state=='held')
            F = frontage(); I = interior(); FB = front_blocks()
            awake_d = [grid[n].d for b in blocks if b.state=='held'
                       for n in NB[(b.x,b.y)] if grid[n].state=='dark']
            md = sum(awake_d)/len(awake_d) if awake_d else 0
            produ = asm_active * rate(t) if prod else None
            stats['ratios'][t//3600] = (mags, produ); stats['asm_at'][t//3600] = asm_active
            out.append((t//3600, nheld, F, I, mags, shells, md, stats['lost']))
            if log:
                extra = f"  frontblocks={FB:3d}"
                if prod: extra += f"  prod={produ:5.1f} mag/min ratio={mags/produ if produ else float('nan'):.2f} buf={buffer/10:.0f}mag"
                if pw: extra += f"  power={demand_kw()/1000:.2f}/{effective_supply()/1000:.2f} MW"
                if prod or pw: extra += f"  lost={stats['lost']}"
                print(f"{policy:9s} h{t//3600}: held={nheld:3d} front={F:3d} interior={I:3d} "
                      f"ammo={mags:6.1f} mag/min shells={shells:5.1f}/min  mean awake rot={md:.2f}{extra}")
    if log:
        print(f"{policy:9s} first interior at "
              f"{'never' if first_interior is None else str(round(first_interior/60))+' min'}; "
              f"total mags {sum(x[1] for x in blooms)/10:.0f}, shells {sum(x[2] for x in blooms):.0f}")
    stats['total_mags'] = sum(x[1] for x in blooms)/10
    stats['total_shells'] = sum(x[2] for x in blooms)
    stats['first_interior'] = first_interior
    stats['brec'] = brec
    stats['held'] = sum(1 for b in blocks if b.state=='held')
    stats['front'] = frontage(); stats['interior'] = interior(); stats['front_blocks'] = front_blocks()
    stats['n_scatter'] = len(scatter)
    return out, grid, stats

# ---------------------------------------------------------------- first hour
def first_hour(start_coal=40, hq_exempt=False, hq_draw=200.0, coal_rubble=False, gen2_at=30*60,
               log=True, draw_flat=False, gens=None, label='', sub_kw=200.0, interior_kw=40.0, patch=None,
               patch_exc_at=6*60):
    """§11 literally: 300 kW generator, 40 coal at 4 MJ, HQ substation draw, machines at the
    minutes stated, three starting neighbours, coal rubble only when the west block is Held."""
    E = start_coal * 4.0                      # MJ
    gen_kw = 300.0
    # D1 (calibration 2): sub_kw/interior_kw = front/interior substation draw (doc default 200/40; D1 100/20);
    # patch = units in the HQ-lot coal patch, mined by an Excavator (60 kW, 0.5/s) placed at patch_exc_at.
    patch_left = float(patch) if patch is not None else 0.0
    patch_out = None
    events = []                               # (t, description)
    # timeline from §11 (assumptions where §11 gives a window, stated in the report)
    claim_e, claim_w, claim_n = 15*60, 25*60, 40*60
    burn_e = 20 + 60*0.22; burn_w = 20 + 60*0.22; burn_n = 20 + 60*0.15
    machines = []                             # (t, kW)
    machines += [(6*60, 120.0), (8*60, 100.0)]            # 2 excavators, shot assembler
    machines += [(claim_e + burn_e + 60, 60.0), (claim_e + burn_e + 120, 100.0)]  # copper exc, wire asm
    coal_exc_at = claim_w + burn_w + 60
    machines += [(coal_exc_at, 60.0)]
    machines += [(45*60, 60.0), (45*60, 100.0)]                  # 5th excavator, 3rd assembler (§11: 5 exc, 3 asm at 60 min)
    if patch is not None: machines += [(patch_exc_at, 60.0)]      # excavator on the HQ coal patch (§11 minute 6)
    if gens is None: gens = [0, gen2_at]
    first_brownout = None; coal_out = None; shed_hq_at = None; throttle_min = 1.0
    peak = 0.0; gens_needed = {}; coal_burned = 0.0; burn_30 = 0.0
    t = 0
    subs = {'hq': True, 'e': False, 'w': False, 'n': False}
    while t < 3600:
        # substations (contested or held draw 200 kW while any dark neighbour; HQ per hq_draw)
        d = 0.0
        d += (120.0 if draw_flat else hq_draw) if subs['hq'] else 0.0
        if t >= claim_e: d += 120.0 if draw_flat else sub_kw
        if t >= claim_w: d += 120.0 if draw_flat else sub_kw
        if t >= claim_n: d += 120.0 if draw_flat else sub_kw
        # HQ becomes interior when N is held (E, W, N held + river)
        if t >= claim_n + burn_n and not draw_flat and hq_draw == sub_kw: d -= (sub_kw - interior_kw)
        mach = sum(kw for at, kw in machines if t >= at)
        d += mach
        cap = gen_kw * sum(1 for g in gens if t >= g)
        if E <= 0 and not (coal_rubble or t >= coal_exc_at): cap = 0.0
        peak = max(peak, d); gens_needed[t//600] = max(gens_needed.get(t//600, 0), math.ceil(d/gen_kw - 1e-9))
        if d > cap:
            if first_brownout is None: first_brownout = t
            if hq_exempt or draw_flat:
                throttle_min = min(throttle_min, cap/d if d else 1.0)
            else:
                # doc rule: shed the substation with the most dark neighbours = HQ (3) at start
                if shed_hq_at is None: shed_hq_at = t + 20
        burn = min(d, cap)
        coal_burned += burn/1000.0/4.0
        if t == 30*60: burn_30 = coal_burned
        if coal_rubble or t >= coal_exc_at:
            E += 0.5*4.0            # one excavator on coal: 0.5/s * 4 MJ = 2 MW-equivalent of fuel
        if patch is not None and t >= patch_exc_at and patch_left > 0:
            mined = min(0.5, patch_left); patch_left -= mined; E += mined*4.0
            if patch_left <= 0 and patch_out is None: patch_out = t
        E -= burn/1000.0            # MJ
        if E <= 0 and coal_out is None and not coal_rubble and t < coal_exc_at: coal_out = t
        E = max(E, 0.0) if not (coal_rubble or t >= coal_exc_at) else min(E, 1e9)
        t += 1
    res = dict(first_brownout=first_brownout, coal_out=coal_out, shed_hq_at=shed_hq_at,
               throttle_min=throttle_min, coal_arrives=coal_exc_at, peak=peak, gens_needed=gens_needed, coal_burned=coal_burned,
               patch_out=patch_out, patch_left=patch_left, burn_30=burn_30)
    if log:
        fm = lambda s: 'never' if s is None else f"{s/60:.1f} min"
        print(f"  {label:34s} coal={start_coal:3d} hq_exempt={int(hq_exempt)} hq_draw={hq_draw:.0f} coal_rubble={int(coal_rubble)} gens@min={[g//60 for g in gens]} flat={int(draw_flat)}"
              f"{'' if patch is None else f' sub={sub_kw:.0f}/{interior_kw:.0f} patch={patch:.0f}'}"
              f" -> brownout {fm(first_brownout)}, coal out {fm(coal_out)}, HQ shed {fm(shed_hq_at)}, worst throttle {throttle_min:.2f}, peak {peak:.0f} kW, coal burned in h1 {coal_burned:.0f}"
              f"{'' if patch is None else f', patch out {fm(patch_out)} ({patch_left:.0f} left)'}")
    return res

# ---------------------------------------------------------------- reporting helpers
def district_report(stats, label=''):
    br = [r for r in stats['brec'] if r[0] >= 3600]
    rows = []
    for key in ('civ', 'res', 'ind', 'out', 'well'):
        ss = [r for r in br if (('well' if r[2] else r[1]) == key) and not r[3]]
        wk = [r for r in br if (('well' if r[2] else r[1]) == key) and r[3]]
        em = stats['edge_min'].get(key, 0); am = stats['awake_min'].get(key, 0)
        mags = sum(r[7] for r in ss)/10.0
        rows.append(dict(d=key, n_ss=len(ss), n_wake=len(wk),
                         sh_ss=(sum(1 for r in ss if r[5] > 0)/len(ss) if ss else float('nan')),
                         hu_ss=(sum(1 for r in ss if r[6] > 0)/len(ss) if ss else float('nan')),
                         sh_wk=(sum(1 for r in wk if r[5] > 0)/len(wk) if wk else float('nan')),
                         hu_wk=(sum(1 for r in wk if r[6] > 0)/len(wk) if wk else float('nan')),
                         mag_edge=(mags/em if em else float('nan')),
                         mag_block=(mags/am if am else float('nan')),
                         shell_edge=(sum(r[8] for r in ss)/em if em else float('nan')),
                         wake_rounds=(sum(r[7] for r in wk)/len(wk) if wk else float('nan'))))
    return rows

def fmt(x, w=5, p=2):
    return f"{x:{w}.{p}f}" if isinstance(x, float) and not math.isnan(x) else f"{'-':>{w}}"

def mr(vals, p=1):
    vals = [v for v in vals if v is not None]
    if not vals: return 'n/a'
    return f"{statistics.mean(vals):.{p}f} [{min(vals):.{p}f}–{max(vals):.{p}f}]"

# ---------------------------------------------------------------- experiments
def stepped_block(dmax, g, shade_thr=0.3, hulk_thr=0.5, base=4.0, hours=6):
    """One awake block in isolation, stepped with the sim's own bloom model; returns steady-state d, mag/min, shells/min."""
    d = 0.5*dmax; T = 0; rec = []
    for t in range(hours*3600):
        d += g*(dmax-d); T += 1
        if T >= 120/(0.5+d):
            cr = round(base + 36*d); sh = cr//8 if d >= shade_thr else 0; hu = 0.4 if d >= hulk_thr else 0.0
            rec.append((t, d, cr*3 + sh*10, hu*10)); d = max(0.05, 0.9*d); T = 0
    tail = [r for r in rec if r[0] > (hours-2)*3600]
    span = (tail[-1][0] - tail[0][0])/60.0 if len(tail) > 1 else 1
    return statistics.mean(r[1] for r in tail), sum(r[2] for r in tail[:-1])/10/span, sum(r[3] for r in tail[:-1])/span

def experiments(seeds=(3, 4, 5), hours=5):
    P = dict(production=True, start_rounds=300)
    J = dict(jitter=0.1)
    def R(pol, **kw):
        kw2 = dict(J); kw2.update(kw); kw2.setdefault('hours', hours); kw2.setdefault('log', False)
        return run(pol, **kw2)[2]
    print(f"ASSUMPTIONS: seeds {seeds}; initial rot jitter +-10 % per block (seeded); Shot assemblers 1@10min 2@30min 3@50min 4@3h;")
    print(f"  hopper 100 rounds/edge; starting ammo 300 rounds (three full hoppers) unless stated; power headroom 500 kW recomputed each 10 min unless stated;")
    print(f"  bloom stream 15 s; unfed shade = +30 s substation outage; creep 1 tile/3 s, fall at 30 tiles (90 s); retake of a fallen block preempts the next claim.")
    print("=" * 78); print("SANITY: per-district steady state of an isolated awake block (analytic vs stepped)")
    for name, dmax, g in (('civ',0.30,0.0004),('res',0.45,0.0005),('ind',0.60,0.0006),('out',1.0,0.0008)):
        d = 0.5*dmax; T = 0; ds = []
        for step in range(20000):
            d += g*(dmax-d)
            T += 1
            if T >= 120/(0.5+d):
                ds.append(d); d = max(0.05, 0.9*d); T = 0
        tail = ds[-10:]
        print(f"  {name}: stepped steady-state d = {statistics.mean(tail):.3f}  (dmax {dmax}, g {g})")
    print("  untouched blocks: 90 % of cap after ln(5)/g s = civ 67 min, res 54 min, ind 45 min, out 34 min")
    print("  isolated awake block, sim bloom model, steady state (last 2 h of 6): d, mag/min, shells/min  [E3-block]")
    for name, dmax, g in (('civ',0.30,0.0004),('res',0.45,0.0005),('res far (x1.3 depth)',0.585,0.0005),('ind',0.60,0.0006),('ind far (x1.3)',0.78,0.0006),
                          ('out',1.0,0.0008),('well on res (dmax+0.3, g x4)',0.75,0.002),('well on ind',0.9,0.0024),('well on out',1.0,0.0032),('res 1 block from well',0.675,0.001625)):
        ss, mags, sh = stepped_block(dmax, g)
        ss2, mags2, sh2 = stepped_block(dmax, g, 0.25, 0.45)
        print(f"    {name:30s} dmax {dmax:.3f} g {g:.4f}: d {ss:.3f}  {mags:4.1f} mag/min  {sh:4.1f} shells/min   | thresholds .25/.45: {mags2:4.1f} mag/min {sh2:4.1f} shells/min")
    print()

    # E7 regression first
    print("=" * 78); print("E7 REGRESSION (all new models off, seed 3)")
    reg = {}
    for pol in ('compact', 'balanced', 'spike', 'cheapest'):
        s = R(pol, hours=5, seed=3, log=False, jitter=0.0)
        reg[pol] = s
        print(f"  {pol:9s} total mags {s['total_mags']:.0f} shells {s['total_shells']:.0f} first interior "
              f"{'never' if s['first_interior'] is None else round(s['first_interior']/60)} min front={s['front']} frontblocks={s['front_blocks']}")
    s = R('turtle', hours=3, seed=3, log=False, jitter=0.0)
    print(f"  turtle    total mags {s['total_mags']:.0f}")
    print()

    # Part 1.8 wake cap
    print("=" * 78); print("WAKE-BLOOM CAP (spike policy reaches outskirts/wells; mean rounds per wake bloom, seeds 3-5)")
    for cap in (False, True):
        agg = {}
        for sd in seeds:
            s = R('spike', seed=sd, wake_cap=cap)
            for r in district_report(s):
                agg.setdefault(r['d'], []).append(r['wake_rounds'])
        print(f"  wake_cap={cap}: " + '  '.join(f"{k}={mr([v for v in vs if not math.isnan(v)],0)}" for k, vs in agg.items()))
    print()

    # E1
    P = dict(production=True, start_rounds=300)
    print("=" * 78); print("E1 AMMO CONSEQUENCES (compact, 5 h, production on)")
    print("  E1-start: §11 literally (2 turrets, 20 magazines = 200 rounds; HQ has 3 edges x 100-round hoppers)")
    for unfed in ('substation', 'creep'):
        ff = []
        for sd in seeds:
            s = R('compact', hours=1, seed=sd, log=False, production=True, start_rounds=200, unfed=unfed)
            ff.append(s['first_fall'])
        print(f"    unfed={unfed:10s} HQ falls at {mr([x/60 for x in ff if x is not None],1)} min  (never in {sum(1 for x in ff if x is None)} of {len(seeds)} seeds)")
    print("  E1-start-min: starting rounds needed so the HQ survives under unfed=substation N=40 (production from 10 min)")
    for sr in range(200, 320, 20):
        falls = [R('compact', seed=sd, production=True, start_rounds=sr, unfed='substation', unfed_n=40)['first_fall'] for sd in seeds]
        print(f"    start_rounds={sr}: HQ/early fall at {[('never' if f is None else round(f/60,1)) for f in falls]} min")
        if all(f is None for f in falls): break
    print("  E1-ring-spike: fed ring on the spike policy (industrial + well edges; a well bloom is up to 170 rounds against a 100-round hopper)")
    for unfed, n in (('none', 20), ('substation', 20), ('substation', 40), ('creep', 20)):
        lost, unf, fdw = [], [], []
        for sd in seeds:
            s = R('spike', seed=sd, **P, unfed=unfed, unfed_n=n)
            lost.append(s['lost']); unf.append(s['unfed_total']); fdw += [x[0] for x in s['fall_delays']]
            r3 = s['ratios'][3][0]/s['ratios'][3][1]; minbuf = min(s['buffer_samples'][120:]) if len(s['buffer_samples']) > 120 else float('nan')
        print(f"    unfed={unfed:10s} N={n:2d}  blocks lost in 5 h {mr(lost,1)}  unfed crawlers {mr(unf,0)}  starved-edge districts of falls {sorted(set(fdw))}  demand/production@3h {r3:.2f}  min buffer after 2 h {minbuf:.0f} rounds")
    print("  main runs use 300 starting rounds (three full hoppers); starve variant withholds from every edge whose dark block has d < 0.3, HQ edges exempt")
    for starve in (False, True):
        for unfed in ('none', 'substation', 'creep'):
            for n in ((20,) if unfed != 'substation' else (10, 20, 40)):
                lost, ratio5, hop, byh, fdr, sdr, fdw, unf = [], [], [], [], [], [], [], []
                for sd in seeds:
                    s = R('compact', seed=sd, **P, unfed=unfed, unfed_n=n, starve_quiet=starve)
                    lost.append(s['lost']/hours); unf.append(s['unfed_total'])
                    m, p_ = s['ratios'][hours]; ratio5.append(m/p_ if p_ else float('nan'))
                    hop.append(statistics.mean(s['hopper_samples']) if s['hopper_samples'] else 0)
                    byh.append([s['lost_by_hour'].get(h, 0) for h in range(hours)])
                    fdr += [x[1] for x in s['fall_delays'] if x[0] in ('res','civ')]
                    sdr += [x[3] for x in s['fall_delays'] if x[0] in ('res','civ') and x[3] is not None]
                    fdw += [x[1] for x in s['fall_delays'] if x[0] in ('well','out','ind')]
                tag = f"E1-{'starve' if starve else 'ring'}-{unfed}" + (f"-N{n}" if unfed=='substation' else '')
                print(f"  {tag:26s} lost/h {mr(lost,2)}  by hour(seed{seeds[0]}) {byh[0]}  unfed crawlers {mr(unf,0)}  ratio@5h {mr(ratio5,2)}  mean hopper {mr(hop,0)} rounds")
                if fdr or fdw:
                    print(f"      res/civ falls {len(fdr)}: first-unfed->fall {mr(fdr,1)} min, ring-withholds->fall {mr(sdr,1)} min;  well/out/ind falls {len(fdw)}: first-unfed->fall {mr(fdw,1)} min")
    print()

    # E2
    print("=" * 78); print("E2 BROWNOUT CASCADE (compact, 5 h, power on, production off; supply shortfall at h4 for 10 min)")
    for headroom in (500.0, 1000.0):
        print(f"  headroom {headroom:.0f} kW (player's spare supply above full demand, recomputed every 10 min; 500 kW is the least that survives two claims between marks)")
        for pct in (5, 15, 25):
            for variant, kw in (('doc', {}), ('draw=flat', dict(draw='flat')), ('shed=machines-first', dict(shed='machines-first')),
                                ('interior-grace=300', dict(interior_grace=300)), ('flat+grace', dict(draw='flat', interior_grace=300))):
                lost, inw, aft, shed_ev, dem, t3 = [], [], [], [], [], None
                for sd in seeds:
                    s = R('compact', seed=sd, power=True, headroom=headroom, shortfall=(pct, 4, 10), **kw)
                    lost.append(s['lost']); inw.append(s['lost_in_window']); aft.append(s['lost_after_window'])
                    shed_ev.append(s['shed_events']); dem.append(s['demand_kw'][239]/1000 if len(s['demand_kw']) > 239 else float('nan'))
                    if sd == seeds[0]: t3 = (s['shed_log'], [(round(x[0]/60,1), x[1]) for x in s['lost_log']])
                print(f"    E2-h{headroom:.0f}-{pct}pct-{variant:20s} lost {mr(lost,1)}  in-window {mr(inw,1)}  after-window {mr(aft,1)}  shed events {mr(shed_ev,1)}  demand@4h {mr(dem,2)} MW")
                if variant == 'doc' and t3 and t3[0]:
                    print(f"        seed {seeds[0]} timeline: sheds at min {[round(x/60,1) for x in t3[0]][:12]}  falls {t3[1][:12]}")
    print("  E2 coupling check: machines-first with production + unfed=substation N=40 on (15 %, headroom 500 / 1000)")
    for headroom in (500.0, 1000.0):
        lost = []
        for sd in seeds:
            s = R('compact', seed=sd, power=True, headroom=headroom, shortfall=(15, 4, 10), shed='machines-first', **P, unfed='substation', unfed_n=40)
            lost.append(s['lost'])
        print(f"    E2-h{headroom:.0f}-15pct-machines-first+prod  lost {mr(lost,1)}")
    print("  §12/§15 power figures vs modelled demand with all substations on (compact, seed 3, 200/40 kW draw, 4 exc + 3-4 asm):")
    s = R('compact', seed=3, power=True)
    for h in range(1, 6):
        print(f"    h{h}: demand {s['demand_kw'][h*60-1]/1000:.2f} MW   (§15 says ~0.3-1.5 MW early, 2 MW at 3 h; §11 says 2 Generators = 0.6 MW at 1 h)")
    s25 = R('compact', seed=3, power=True, hours=25)
    print("    E2-demand-25h (compact, seed 3): " + "  ".join(f"h{h}: {s25['demand_kw'][h*60-1]/1000:.1f} MW" for h in (5, 10, 15, 20, 25)) + f"   held at 25 h {s25['held']} front {s25['front']} interior {s25['interior']}")
    sh = R('compact', seed=3, power=True, draw='half')
    print("    E2-demand-half (D1 draw 100/20 kW, compact, seed 3): " + "  ".join(f"h{h}: {sh['demand_kw'][h*60-1]/1000:.2f} MW" for h in range(1, 6)))
    sh25 = R('compact', seed=3, power=True, hours=25, draw='half')
    print("    E2-demand-half-25h: " + "  ".join(f"h{h}: {sh25['demand_kw'][h*60-1]/1000:.1f} MW" for h in (5, 10, 15, 20, 25)))
    s2 = R('compact', seed=3, power=True, supply='schedule')
    print(f"    blocks lost if supply follows the §15 schedule (0.6 MW@30min .. 1.5 MW@3h): {s2['lost']} (first fall at {'never' if s2['first_fall'] is None else round(s2['first_fall']/60)} min)")
    print()

    # E3
    print("=" * 78); print("E3 SHADES AND HULKS BY DISTRICT (5 h; ss = steady-state blooms after h1, wk = wake blooms)")
    for pol in ('compact', 'spike'):
        for thr in ((0.3, 0.5), (0.25, 0.45)):
            agg = {}
            for sd in seeds:
                s = R(pol, seed=sd, shade_thr=thr[0], hulk_thr=thr[1])
                for r in district_report(s):
                    agg.setdefault(r['d'], []).append(r)
            print(f"  E3-{pol}-shade{thr[0]}-hulk{thr[1]}")
            print(f"    {'dist':5s} {'n_ss':>5s} {'n_wk':>5s} {'shade_ss':>9s} {'hulk_ss':>8s} {'shade_wk':>9s} {'hulk_wk':>8s} {'mag/edge/min':>13s} {'mag/block/min':>14s} {'shell/edge/min':>15s}")
            for k, rs in agg.items():
                def m(f):
                    v = [r[f] for r in rs if not math.isnan(r[f])]
                    return statistics.mean(v) if v else float('nan')
                print(f"    {k:5s} {sum(r['n_ss'] for r in rs):5d} {sum(r['n_wake'] for r in rs):5d} {fmt(m('sh_ss'),9)} {fmt(m('hu_ss'),8)} {fmt(m('sh_wk'),9)} {fmt(m('hu_wk'),8)} {fmt(m('mag_edge'),13)} {fmt(m('mag_block'),14)} {fmt(m('shell_edge'),15,3)}")
    print()

    # E4
    print("=" * 78); print("E4 FIRST HOUR (§11 literally: 300 kW generator, 40 coal, HQ 200 kW, 2 exc + 1 asm by 8 min, east claim 15 min (+copper exc, wire asm),")
    print("   west claim 25 min (+coal exc 60 s after Held), 5th exc + 3rd asm at 45 min, north claim 40 min; brownout sheds the substation with most dark neighbours = HQ)")
    first_hour(label='E4-literal')
    first_hour(label='E4-hq-exempt', hq_exempt=True)
    first_hour(label='E4-hq-draw-40', hq_draw=40.0)
    first_hour(label='E4-flat-120', draw_flat=True)
    first_hour(label='E4-start-coal-rubble', coal_rubble=True)
    first_hour(label='E4-coal-120', start_coal=120, hq_exempt=True)
    print("  minimum starting coal so fuel lasts until the west coal excavator + 2 min (HQ exempt so nothing sheds):")
    for c in range(40, 400, 10):
        r = first_hour(start_coal=c, hq_exempt=True, log=False)
        if r['coal_out'] is None or r['coal_out'] >= r['coal_arrives'] + 120:
            print(f"    E4-min-coal: {c} coal (coal rubble from the west block arrives at {r['coal_arrives']/60:.1f} min)"); break
    print("  generator count needed per 10-min slot under §11's machine list (demand / 300 kW, rounded up):")
    r = first_hour(log=False, hq_exempt=True, coal_rubble=True)
    print(f"    E4-gens-needed HQ 200 kW: {[r['gens_needed'][k] for k in sorted(r['gens_needed'])]}  peak {r['peak']:.0f} kW")
    r = first_hour(log=False, hq_exempt=True, coal_rubble=True, hq_draw=40.0)
    print(f"    E4-gens-needed HQ  40 kW: {[r['gens_needed'][k] for k in sorted(r['gens_needed'])]}  peak {r['peak']:.0f} kW")
    print("  combinations (generator placed at listed minutes):")
    first_hour(label='E4-rubble+gen@6,15,25,40', coal_rubble=True, gens=[0, 6*60, 15*60, 25*60, 40*60])
    first_hour(label='E4-rubble+gen@6,15,25,40,45', coal_rubble=True, gens=[0, 6*60, 15*60, 25*60, 40*60, 45*60])
    first_hour(label='E4-rubble+hq40+gen@15,25,40', coal_rubble=True, hq_draw=40.0, gens=[0, 15*60, 25*60, 40*60])
    first_hour(label='E4-rubble+hq40+gen@15,25,40,45', coal_rubble=True, hq_draw=40.0, gens=[0, 15*60, 25*60, 40*60, 45*60])
    first_hour(label='E4-coal120+gen@6,15,25,40,45', start_coal=120, gens=[0, 6*60, 15*60, 25*60, 40*60, 45*60])
    first_hour(label='E4-coal200+gen@6,15,25,40,45', start_coal=200, gens=[0, 6*60, 15*60, 25*60, 40*60, 45*60])
    print("  E4h (calibration 2, D1): substation 100/20 kW, HQ 100 kW, ~3,000-unit coal patch on the HQ lot mined by an Excavator from minute 6")
    H = dict(sub_kw=100.0, interior_kw=20.0, hq_draw=100.0, patch=3000)
    first_hour(label='E4h-literal-1gen', **H)
    first_hour(label='E4h-hq-exempt-1gen', hq_exempt=True, **H)
    for gens in ([0, 6*60], [0, 6*60, 15*60], [0, 6*60, 15*60, 25*60], [0, 6*60, 15*60, 45*60]):
        first_hour(label='E4h-gen@' + ','.join(str(g//60) for g in gens), gens=gens, **H)
    r = first_hour(log=False, hq_exempt=True, **H)
    print(f"    E4h-gens-needed per 10-min slot: {[r['gens_needed'][k] for k in sorted(r['gens_needed'])]}  peak {r['peak']:.0f} kW")
    print("    E4h patch size vs mined-out minute (one Excavator at 0.5/s from minute 6):")
    for pz in (450, 700, 1000, 3000):
        r = first_hour(log=False, gens=[0, 6*60, 15*60, 25*60], sub_kw=100.0, interior_kw=20.0, hq_draw=100.0, patch=pz)
        print(f"      E4h-patch{pz}: mined out {'never' if r['patch_out'] is None else f'{r['patch_out']/60:.1f} min'}, coal burned by 30 min {r['burn_30']:.0f}, in h1 {r['coal_burned']:.0f}")
    print()

    # E5
    print("=" * 78); print("E5 EARLY BITE: demand/production at 1, 3, 5 h (production on)")
    for pol in ('compact', 'spike'):
        for variant, kw in (('base', {}), ('early-rate-10', dict(asm_early_rate=10.0)), ('bloom-base-8', dict(bloom_base=8.0)),
                            ('both', dict(asm_early_rate=10.0, bloom_base=8.0)), ('1-shot-asm-h1', dict(asm_schedule=((600,1),(5400,2),(9000,3),(12600,4)))),
                            ('track-70pct', dict(asm_track=0.7))):
            rr = {1: [], 3: [], 5: []}; dem = {1: [], 3: [], 5: []}; na = {1: [], 3: [], 5: []}
            for sd in seeds:
                s = R(pol, seed=sd, **P, **kw)
                for h in (1, 3, 5):
                    m, p_ = s['ratios'][h]; rr[h].append(m/p_ if p_ else float('nan')); dem[h].append(m); na[h].append(s['asm_at'][h])
            print(f"  E5-{pol}-{variant:14s} ratio 1h {mr(rr[1],2)}  3h {mr(rr[3],2)}  5h {mr(rr[5],2)}   demand mag/min 1h {mr(dem[1],1)} 3h {mr(dem[3],1)} 5h {mr(dem[5],1)}   Shot assemblers 1h/3h/5h {mr(na[1],0)}/{mr(na[3],0)}/{mr(na[5],0)}")
    print()

    # E6
    print("=" * 78); print("E6 INERT CELLS (5 h; front per held block, total mags)")
    for sd in seeds:
        for val in ('none', 'no2x2', 'maxrun2'):
            sc = make_scatter(sd, 0.09, val)
            allin = sc | {(x, H-1) for x in range(W)}
            sq = sum(1 for (x,y) in sc if {(x+1,y),(x,y+1),(x+1,y+1)} <= allin)
            runs3 = sum(1 for (x,y) in sc if {(x+1,y),(x+2,y)} <= sc or {(x,y+1),(x,y+2)} <= sc)
            print(f"  scatter seed {sd} validator {val:8s}: {len(sc)} cells, 2x2 inert squares (incl. river) {sq}, straight runs of 3+ {runs3}")
    print("  shape spread on the scattered map (the doc's 1.6x / 3.4x claims were measured without scatter):")
    for scat in (False, True):
        tot = {}
        for pol in ('compact', 'spike', 'cheapest'):
            mags = [R(pol, seed=sd, scatter=scat)['total_mags'] for sd in seeds]
            tot[pol] = statistics.mean(mags)
            print(f"    E6-{'scatter' if scat else 'noscatter'}-{pol:9s} total mags {mr(mags,0)}")
        print(f"      spike/compact {tot['spike']/tot['compact']:.2f}x   compact/cheapest {tot['compact']/tot['cheapest']:.2f}x")
    for scat, val in ((False, 'none'), (True, 'none'), (True, 'no2x2'), (True, 'maxrun2'), (True, 'inert-dark')):
        res = {}
        for pol in ('compact', 'river'):
            fph, mags, held_, inter, nsc = [], [], [], [], []
            for sd in seeds:
                s = R(pol, seed=sd, scatter=scat, validator=val)
                fph.append(s['front']/s['held']); mags.append(s['total_mags']); held_.append(s['held']); inter.append(s['interior']); nsc.append(s['n_scatter'])
            res[pol] = (fph, mags, held_, inter)
            print(f"  E6-{'scatter' if scat else 'noscatter'}-{val:10s}-{pol:8s} held {mr(held_,0)} front/held {mr(fph,2)} interior {mr(inter,0)} total mags {mr(mags,0)} inert cells {mr(nsc,0)}")
        gap = statistics.mean(res['compact'][1]) / statistics.mean(res['river'][1]) - 1 if statistics.mean(res['river'][1]) else float('nan')
        rv, cp = statistics.mean(res['river'][1]), statistics.mean(res['compact'][1])
        print(f"    -> compact/river ammo gap: river uses {100*(rv/cp-1):+.0f} % ammo relative to compact")
    print()
    experiments_e8(seeds, hours)

# ---------------------------------------------------------------- main

def experiments_e8(seeds=(3, 4, 5), hours=5):
    """E8: (a) fall distance 16 tiles (lot-centre substation at 1 tile/3 s = 48 s) instead of the sim's 30 (90 s);
    (b) when the first shade appears, per district (compact and spike)."""
    P = dict(production=True, start_rounds=300)
    def R(pol, **kw):
        kw2 = dict(jitter=0.1); kw2.update(kw); kw2.setdefault('hours', hours); kw2.setdefault('log', False)
        return run(pol, **kw2)[2]
    print("=" * 78); print("E8 FALL DISTANCE (doc: 1 tile/3 s and ~90 s; a lot-centre substation is 16 tiles from the street = 48 s)")
    for ft in (30, 16):
        lost, fdr, sdr = [], [], []
        for sd in seeds:
            s = R('compact', seed=sd, **P, unfed='substation', unfed_n=40, starve_quiet=True, fall_tiles=ft)
            lost.append(s['lost']/hours)
            fdr += [x[1] for x in s['fall_delays'] if x[0] in ('res','civ')]
            sdr += [x[3] for x in s['fall_delays'] if x[0] in ('res','civ') and x[3] is not None]
        print(f"  E8-starve-N40-fall{ft:<3d} lost/h {mr(lost,2)}  res/civ falls {len(fdr)}: first-unfed->fall {mr(fdr,1)} min, ring-withholds->fall {mr(sdr,1)} min")
    for ft in (30, 16):
        for pct in (15, 25):
            for variant, kw in (('doc', {}), ('shed=machines-first', dict(shed='machines-first'))):
                lost, shed_ev = [], []
                for sd in seeds:
                    s = R('compact', seed=sd, power=True, headroom=500.0, shortfall=(pct, 4, 10), fall_tiles=ft, **kw)
                    lost.append(s['lost']); shed_ev.append(s['shed_events'])
                print(f"  E8-h500-{pct}pct-{variant:20s}-fall{ft:<3d} lost {mr(lost,1)}  shed events {mr(shed_ev,1)}")
    print("  E8-first-shade: minutes to the first bloom carrying a shade, per district (wake blooms marked w); 5 h, thresholds 0.3/0.5")
    for pol in ('compact', 'spike'):
        first = {}
        for sd in seeds:
            s = R(pol, seed=sd, **P)
            seen = set(); hseen = set()
            for r in s['brec']:
                key = 'well' if r[2] else r[1]
                if r[5] > 0 and key not in seen:
                    seen.add(key); first.setdefault(key, []).append((r[0]/60, r[3]))
                if r[6] > 0 and key not in hseen:
                    hseen.add(key); first.setdefault('hulk-'+key, []).append((r[0]/60, r[3]))
        for kind in ('', 'hulk-'):
            parts = []
            for key in ('civ', 'res', 'ind', 'out', 'well'):
                v = first.get(kind+key)
                parts.append(f"{key}={'never' if not v else mr([x[0] for x in v],0)}{'w' if v and all(x[1] for x in v) else ''}")
            print(f"    E8-first-{'shade' if not kind else 'hulk'}-{pol:8s} " + '  '.join(parts) + "  (min)")
    print()

def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--policy', default=None); ap.add_argument('--hours', type=float, default=5)
    ap.add_argument('--seed', type=int, default=3)
    ap.add_argument('--production', action='store_true'); ap.add_argument('--unfed', default='none')
    ap.add_argument('--unfed-n', type=float, default=20); ap.add_argument('--starve-quiet', action='store_true')
    ap.add_argument('--start-rounds', type=float, default=200, help='starting ammo in rounds (§11: 20 magazines = 200)')
    ap.add_argument('--headroom', type=float, default=500.0, help='kW of supply headroom the player keeps (supply=track)')
    ap.add_argument('--power', action='store_true'); ap.add_argument('--supply', default='track')
    ap.add_argument('--shortfall', default=None, help='PCT,HOUR,MIN'); ap.add_argument('--draw', default='doc')
    ap.add_argument('--shed', default='substations'); ap.add_argument('--interior-grace', type=float, default=0)
    ap.add_argument('--scatter', action='store_true'); ap.add_argument('--no-scatter', action='store_true')
    ap.add_argument('--validator', default='none')
    ap.add_argument('--shade-threshold', type=float, default=0.3); ap.add_argument('--hulk-threshold', type=float, default=0.5)
    ap.add_argument('--wake-cap', action='store_true'); ap.add_argument('--bloom-base', type=float, default=4.0)
    ap.add_argument('--asm-early-rate', type=float, default=None); ap.add_argument('--jitter', type=float, default=0.0)
    ap.add_argument('--asm-track', type=float, default=0.0, help='add a Shot assembler when demand/production exceeds this (0 = fixed schedule)')
    ap.add_argument('--fall-tiles', type=float, default=30)
    ap.add_argument('--first-hour', action='store_true'); ap.add_argument('--experiments', nargs='?', const='all', default=None, help='all (default) or E8')
    a = ap.parse_args()
    kw = dict(production=a.production, unfed=a.unfed, unfed_n=a.unfed_n, starve_quiet=a.starve_quiet, start_rounds=a.start_rounds, headroom=a.headroom,
              power=a.power, supply=a.supply, draw=a.draw, shed=a.shed, interior_grace=a.interior_grace,
              scatter=(a.scatter and not a.no_scatter), validator=a.validator,
              shade_thr=a.shade_threshold, hulk_thr=a.hulk_threshold, wake_cap=a.wake_cap,
              bloom_base=a.bloom_base, asm_early_rate=a.asm_early_rate, jitter=a.jitter, asm_track=a.asm_track, fall_tiles=a.fall_tiles)
    if a.shortfall:
        p, h, m = a.shortfall.split(','); kw['shortfall'] = (float(p), float(h), float(m))
    if a.first_hour:
        first_hour(); return
    if a.experiments == 'E8':
        experiments_e8(); return
    if a.experiments:
        experiments(); return
    if a.policy:
        _, _, s = run(a.policy, hours=a.hours, seed=a.seed, **kw)
        if a.production or a.power:
            print(f"lost {s['lost']} retakes {s['retakes']} unfed crawlers {s['unfed_total']:.0f} shed events {s['shed_events']} lost by hour {dict(sorted(s['lost_by_hour'].items()))}")
            for (tt, reason, p, delay, sd) in s['lost_log'][:12]:
                print(f"  fall at {tt/60:6.1f} min {p} reason={reason} starved edge={sd} unfed->fall={'-' if delay is None else f'{delay/60:.1f} min'}")
            fd = s['fall_delays']
            for key in ('civ','res','ind','out','well'):
                v = [x for x in fd if x[0] == key]
                if v: print(f"  {key}: falls {len(v)} unfed->fall {mr([x[1] for x in v])} min  starve->fall {mr([x[3] for x in v if x[3] is not None])} min")
            for r in district_report(s):
                print(f"  {r['d']}: ss blooms {r['n_ss']} wake {r['n_wake']} shade_ss {fmt(r['sh_ss'])} hulk_ss {fmt(r['hu_ss'])} mag/edge/min {fmt(r['mag_edge'])}")
        return
    # legacy default output
    print("== 5 hours, same claim cadence (4 blocks in h1, then 1 per 8 min) ==")
    for pol in ('compact', 'balanced', 'spike', 'cheapest'):
        run(pol, hours=5, seed=3, **kw)
        print()
    print("== turtle: never expand, 3 h ==")
    run('turtle', hours=3, seed=3, **kw)
    print()
    print("== untouched rot levels over time (asleep blocks) ==")
    for (x,y) in [(12,18),(6,13),(12,7),(2,2),(12,9)]:
        b = Block(x,y); d=b.d; row=[]
        for h in range(0,6):
            row.append(f"{d:.2f}")
            for _ in range(3600): d += b.g*(b.dmax-d)
        print(f"  {b.name:3s} ({x:2d},{y:2d}) dmax={b.dmax:.2f}: h0..h5 = {' '.join(row)}")

if __name__ == '__main__':
    main()
