"""Throwaway headless sim of the front rule.
Block grid, cellular rot with district caps and wells, bloom timers,
ammo demand. Compares expansion policies at equal block counts."""
import random, math, sys

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

class Block:
    __slots__ = ('x','y','state','d','dmax','g','name','timer','contest_until')
    def __init__(s, x, y):
        s.x, s.y = x, y
        s.dmax, s.g, s.name = district(x, y)
        s.d = 0.5 * s.dmax
        s.state = 'inert' if y == 21 else 'dark'
        s.timer = None; s.contest_until = 0

def neighbours(x, y):
    for dx, dy in ((1,0),(-1,0),(0,1),(0,-1)):
        nx, ny = x+dx, y+dy
        if 0 <= nx < W and 0 <= ny < H: yield nx, ny

def bloom_size(d):
    crawlers = round(4 + 36*d)
    shades = crawlers // 8 if d >= 0.3 else 0
    hulk = 1 if (d >= 0.5 and random.random() < 0.4) else 0
    return crawlers, shades, hulk

def run(policy, hours=5.0, seed=1, log=True, claim_gap=None):
    random.seed(seed)
    grid = {(x,y): Block(x,y) for x in range(W) for y in range(H)}
    grid[START].state = 'held'; grid[START].d = 0
    t = 0; dt = 1
    blooms = []            # (t, rounds, shells)
    first_interior = None
    next_claim = 15*60
    out = []
    def held(p): return grid[p].state == 'held'
    def solid(p): return grid[p].state in ('held', 'inert')
    def frontage():
        return sum(1 for b in grid.values() if b.state=='held'
                   for n in neighbours(b.x,b.y) if grid[n].state in ('dark','contested'))
    def interior():
        return sum(1 for b in grid.values() if b.state=='held'
                   and all(solid(n) for n in neighbours(b.x,b.y)))
    def frontage_if(p):
        b = grid[p]; old = b.state; b.state = 'held'; f = frontage(); b.state = old; return f
    def candidates():
        return [p for p,b in grid.items() if b.state=='dark'
                and any(held(n) for n in neighbours(*p))]
    def choose():
        c = candidates()
        if not c: return None
        def dist(p): return abs(p[0]-TARGET[0]) + abs(p[1]-TARGET[1])
        if policy == 'compact':
            return min(c, key=lambda p: (frontage_if(p), grid[p].d, abs(p[0]-START[0])+abs(p[1]-START[1])))
        if policy == 'spike':
            return min(c, key=lambda p: (dist(p), grid[p].d))
        if policy == 'balanced':
            return min(c, key=lambda p: (frontage_if(p) + dist(p), grid[p].d))
        if policy == 'cheapest':
            return min(c, key=lambda p: (grid[p].d, frontage_if(p)))
        return None
    while t < hours*3600:
        # claims
        if policy != 'turtle' and t >= next_claim:
            p = choose()
            if p:
                b = grid[p]
                cr, sh, hu = bloom_size(b.d)       # wake bloom = 2x
                blooms.append((t, 2*(cr*3+sh*10), 2*hu*10))
                b.state = 'contested'; b.contest_until = t + 20 + 60*b.d
            gap = claim_gap if claim_gap else (15*60 if t < 3600 else 8*60)
            next_claim = t + gap
        # contested -> held
        for b in grid.values():
            if b.state == 'contested' and t >= b.contest_until:
                b.state = 'held'; b.d = 0; b.timer = None
        # rot growth + blooms in dark blocks
        for b in grid.values():
            if b.state != 'dark': continue
            b.d += b.g * (b.dmax - b.d) * dt
            awake = any(grid[n].state in ('held','contested') for n in neighbours(b.x,b.y))
            if not awake: b.timer = None; continue
            if b.timer is None: b.timer = t + 120/(0.5+b.d)
            if t >= b.timer:
                cr, sh, hu = bloom_size(b.d)
                blooms.append((t, cr*3+sh*10, hu*10))
                b.d = max(0.05, b.d*0.9)
                b.timer = t + 120/(0.5+b.d)
        if first_interior is None and interior() > 0: first_interior = t
        t += dt
        if t % 3600 == 0:
            recent = [x for x in blooms if x[0] > t-600]
            mags = sum(x[1] for x in recent)/10/10   # per minute
            shells = sum(x[2] for x in recent)/10
            nheld = sum(1 for b in grid.values() if b.state=='held')
            F = frontage(); I = interior()
            awake_d = [grid[n].d for b in grid.values() if b.state=='held'
                       for n in neighbours(b.x,b.y) if grid[n].state=='dark']
            md = sum(awake_d)/len(awake_d) if awake_d else 0
            out.append((t//3600, nheld, F, I, mags, shells, md))
            if log:
                print(f"{policy:9s} h{t//3600}: held={nheld:3d} front={F:3d} interior={I:3d} "
                      f"ammo={mags:6.1f} mag/min shells={shells:5.1f}/min  mean awake rot={md:.2f}")
    if log:
        print(f"{policy:9s} first interior at "
              f"{'never' if first_interior is None else str(round(first_interior/60))+' min'}; "
              f"total mags {sum(x[1] for x in blooms)/10:.0f}, shells {sum(x[2] for x in blooms):.0f}")
    return out, grid

if __name__ == '__main__':
    print("== 5 hours, same claim cadence (4 blocks in h1, then 1 per 8 min) ==")
    for pol in ('compact', 'balanced', 'spike', 'cheapest'):
        run(pol, hours=5, seed=3)
        print()
    print("== turtle: never expand, 3 h ==")
    run('turtle', hours=3, seed=3)
    print()
    print("== untouched rot levels over time (asleep blocks) ==")
    for (x,y) in [(12,18),(6,13),(12,7),(2,2),(12,9)]:
        b = Block(x,y); d=b.d; row=[]
        for h in range(0,6):
            row.append(f"{d:.2f}")
            for _ in range(3600): d += b.g*(b.dmax-d)
        print(f"  {b.name:3s} ({x:2d},{y:2d}) dmax={b.dmax:.2f}: h0..h5 = {' '.join(row)}")
