"""Write regression fixtures for the TypeScript port (packages/sim/test/regression.test.ts).

For seeds 3, 4, 5: the district/well/inert grid (with the jittered initial rot of every cell) and,
per policy (compact, spike, cheapest, turtle) and map (open, scattered), the hourly log lines and
totals of a 5-hour run with the ammo ring, the unfed-substation rule (N = 40) and the wake cap on.

The hulk roll is the deterministic hash (hulk_roll='hash') so that shells are comparable exactly;
the ring adds same-tick edges in sorted order (ring_order='sorted'), which the port reproduces.
"""
import json, os, sys
import frontsim as fs

SEEDS = (3, 4, 5)
POLICIES = ('compact', 'spike', 'cheapest', 'turtle')
HOURS = 5
CFG = dict(production=True, start_rounds=300, unfed='substation', unfed_n=40, wake_cap=True,
           jitter=0.1, hulk_roll='hash', ring_order='sorted', starve_quiet=False,
           shade_thr=0.3, hulk_thr=0.5, bloom_base=4.0, fall_tiles=30, hopper=100, buffer_cap=4000,
           asm_rate=20.0, asm_schedule=[[600, 1], [1800, 2], [3000, 3], [10800, 4]],
           scatter_frac=0.09, validator='none')

def grid_spec(seed):
    """Cells as the sim builds them: district, well flag, dmax, g and the jittered initial rot."""
    import random
    jr = random.Random(seed * 104729 + 7)
    cells = []
    for x in range(fs.W):
        for y in range(fs.H):
            b = fs.Block(x, y)
            d0 = 0.5 * b.dmax * (1 + jr.uniform(-CFG['jitter'], CFG['jitter']))
            cells.append(dict(x=x, y=y, name=b.name, well=b.well, dmax=b.dmax, g=b.g, d0=d0))
    scatter = sorted(fs.make_scatter(seed, CFG['scatter_frac'], CFG['validator']))
    return dict(cells=cells, scattered_inert=[list(p) for p in scatter])

def main(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for seed in SEEDS:
        fx = dict(seed=seed, w=fs.W, h=fs.H, start=list(fs.START), target=list(fs.TARGET),
                  wells=[list(w) for w in fs.WELLS], config=CFG, hours=HOURS)
        fx.update(grid_spec(seed))
        fx['runs'] = {}
        for mapname, scat in (('open', False), ('scattered', True)):
            fx['runs'][mapname] = {}
            for pol in POLICIES:
                kw = dict(CFG); kw['asm_schedule'] = tuple(tuple(a) for a in CFG['asm_schedule'])
                out, grid, st = fs.run(pol, hours=HOURS, seed=seed, log=False, scatter=scat, **kw)
                fx['runs'][mapname][pol] = dict(
                    hourly=[dict(h=o[0], held=o[1], front=o[2], interior=o[3], mags=o[4], shells=o[5],
                                 mean_awake_rot=o[6], lost=o[7]) for o in out],
                    total_mags=st['total_mags'], total_shells=st['total_shells'],
                    first_interior=st['first_interior'], lost=st['lost'], retakes=st['retakes'],
                    claims=st['claims'], held=st['held'], front=st['front'], interior=st['interior'],
                    n_scatter=st['n_scatter'],
                    lost_log=[dict(t=l[0], reason=l[1], x=l[2][0], y=l[2][1]) for l in st['lost_log']])
                print(f"seed {seed} {mapname:9s} {pol:9s} mags {st['total_mags']:.0f} shells {st['total_shells']:.0f} "
                      f"first interior {st['first_interior']} lost {st['lost']}", flush=True)
        with open(os.path.join(out_dir, f'seed{seed}.json'), 'w') as f:
            json.dump(fx, f)
    print('wrote', out_dir)

if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join('packages', 'sim', 'fixtures'))
