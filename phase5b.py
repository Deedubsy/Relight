"""E11 follow-up: the same Relight hold with an ammo line that is not already short at 25 h (6 assemblers = 120 mag/min from 4 h)."""
import statistics, json, time
import frontsim as F
SEEDS = (3, 4, 5)
J = dict(jitter=0.1)
P = dict(production=True, start_rounds=300, unfed='substation', unfed_n=40,
         asm_schedule=((600,1),(1800,2),(3000,3),(10800,4),(14400,6)))
def fm(s): return 'never' if s is None else f"{s/60:.1f} min"
print("E11b Relight hold with 6 assemblers (120 mag/min) from 4 h; otherwise as E11.")
res = {}
for rl in (None, (25, 10, 3)):
    need, before, lost_w, lost_after, wells_bl, peak, bigb = [], [], [], [], [], [], []
    for sd in SEEDS:
        kw = dict(J); kw.update(P)
        out, grid, st = F.run('compact', hours=25.5, seed=sd, log=False, scatter=True, relight=rl, **kw)
        w0, w1 = 25*3600, 25*3600 + 600
        br_w = [r for r in st['brec'] if w0 <= r[0] < w1]
        br_b = [r for r in st['brec'] if w0 - 3600 <= r[0] < w0]
        need.append(sum(r[7] for r in br_w) / 10 / 10); before.append(sum(r[7] for r in br_b) / 10 / 60)
        wells_bl.append(sum(1 for r in br_w if r[2]))
        per_min = {}
        for r in br_w: per_min[(r[0]-w0)//60] = per_min.get((r[0]-w0)//60, 0) + r[7]/10
        peak.append(max(per_min.values()) if per_min else 0)
        bigb.append(max((r[4] for r in br_w), default=0))
        lost_w.append(sum(1 for e in st['lost_log'] if w0 <= e[0] < w1 + 600))
        lost_after.append(sum(1 for e in st['lost_log'] if e[0] >= w0))
        h25 = next(r for r in out if r[0] == 25)
        print(f"    seed {sd} {'relight' if rl else 'none   '}: 25 h held/front/interior {h25[1]}/{h25[2]}/{h25[3]}, lost before 25 h {sum(1 for e in st['lost_log'] if e[0] < w0)}")
    key = 'no relight' if rl is None else 'relight x3 for 10 min'
    res[key] = dict(need=need, before=before, lost_window=lost_w, lost_after=lost_after, well_blooms=wells_bl, peak=peak, biggest_bloom=bigb)
    print(f"  {key:22s}: mag/min demanded in the window {F.mr(need,1)} (hour before {F.mr(before,1)}; peak minute {F.mr(peak,0)} mags; biggest bloom {bigb} crawlers); well blooms in the window {wells_bl};"
          f" blocks lost 25:00–25:20 {lost_w}; lost from 25:00 to 25:30 {lost_after}")
json.dump(res, open('phase5b_results.json', 'w'), indent=1)
