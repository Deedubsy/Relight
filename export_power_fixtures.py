"""Power-model fixtures for the TypeScript port (packages/sim/test/power.test.ts).

For seeds 3, 4, 5, compact policy on the scattered map, 5 hours, power on with the tracked supply
(headroom 500 kW), production off, a 15 % shortfall for ten minutes at hour 4, both shedding orders and both
draws (doc 200/40 and D1 100/20): per-minute demand and effective supply, the shed log, the lost log.
Same deterministic settings as export_fixtures.py (jitter 0.1, hulk_roll='hash', ring_order='sorted').
"""
import json, os, sys
import frontsim as fs

SEEDS = (3, 4, 5)
BASE = dict(production=False, power=True, supply='track', headroom=500.0, shortfall=(15, 4, 10),
            jitter=0.1, hulk_roll='hash', ring_order='sorted', wake_cap=True, scatter=True)
VARIANTS = {
    'doc-substations': dict(draw='doc', shed='substations'),
    'doc-machines-first': dict(draw='doc', shed='machines-first'),
    'half-substations': dict(draw='half', shed='substations'),
    'half-machines-first-grace300': dict(draw='half', shed='machines-first', interior_grace=300),
    'schedule-doc': dict(draw='doc', shed='substations', supply='schedule', shortfall=None),
}

def main(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for seed in SEEDS:
        fx = dict(seed=seed, hours=5, base=dict(BASE, shortfall=list(BASE['shortfall'])), runs={})
        for name, kw in VARIANTS.items():
            cfg = dict(BASE); cfg.update(kw)
            out, grid, st = fs.run('compact', hours=5, seed=seed, log=False, **cfg)
            fx['runs'][name] = dict(
                config=dict(cfg, shortfall=list(cfg['shortfall']) if cfg['shortfall'] else None),
                demand_kw=st['demand_kw'], supply_kw=st['supply_kw'], shed_log=st['shed_log'],
                shed_events=st['shed_events'], first_brownout=st['first_brownout'],
                lost=st['lost'], lost_in_window=st['lost_in_window'], lost_after_window=st['lost_after_window'],
                lost_log=[dict(t=l[0], reason=l[1], x=l[2][0], y=l[2][1]) for l in st['lost_log']],
                hourly=[dict(h=o[0], held=o[1], front=o[2], interior=o[3], mags=o[4], shells=o[5], lost=o[7]) for o in out],
                total_mags=st['total_mags'], held=st['held'], front=st['front'], interior=st['interior'])
            print(f"seed {seed} {name:30s} sheds {st['shed_events']:3d} lost {st['lost']:2d} (window {st['lost_in_window']}, after {st['lost_after_window']}) first brownout {st['first_brownout']}", flush=True)
        with open(os.path.join(out_dir, f'power{seed}.json'), 'w') as f:
            json.dump(fx, f)
    print('wrote', out_dir)

if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join('packages', 'sim', 'fixtures'))
