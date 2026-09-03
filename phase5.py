"""Phase 5 of the map-view prototype brief: sim runs E9–E13 on frontsim.py. Three seeds each.
Run: python phase5.py > phase5_out.txt. Appended to FRONT_FIX_REPORT.md by hand."""
import statistics, json, sys, time
import frontsim as F

SEEDS = (3, 4, 5)
J = dict(jitter=0.1)
P = dict(production=True, start_rounds=300, unfed='substation', unfed_n=40)   # the ammo-line model used by E1/E8

def R(pol, seed, hours, **kw):
    kw2 = dict(J); kw2.update(kw)
    return F.run(pol, hours=hours, seed=seed, log=False, **kw2)

def hrow(out, h): return next((r for r in out if r[0] == h), None)
def mr(vals, p=1): return F.mr(list(vals), p)
def fm(s): return 'never' if s is None else f"{s/60:.1f} min"
results = {}
T0 = time.time()

# ---------------------------------------------------------------- E9 claim cadence
print("E9 claim cadence: compact on the scattered map, no ammo line; hour one unchanged (four claims), then one claim every 4/6/8 min.")
print("   §18 drawings: 5 h = 52 held / 24 front / 36 interior; 25 h = 313 / 67 / 270. Analytic claims needed: 48 in h2–5 → gap 5.0 min; 309 in h2–25 → gap 4.7 min.")
results['E9'] = {}
for gap in (240, 360, 480):
    rows5, rows25, claims = [], [], []
    for sd in SEEDS:
        out, grid, st = R('compact', sd, 25, scatter=True, gap_after=gap)
        rows5.append(hrow(out, 5)[1:4]); rows25.append(hrow(out, 25)[1:4]); claims.append(st['claims'])
    m5 = [statistics.mean(r[i] for r in rows5) for i in range(3)]; m25 = [statistics.mean(r[i] for r in rows25) for i in range(3)]
    results['E9'][gap] = dict(h5=rows5, h25=rows25, claims=claims)
    print(f"  gap {gap//60} min: 5 h held/front/interior = {m5[0]:.0f}/{m5[1]:.0f}/{m5[2]:.0f} {[list(r) for r in rows5]};"
          f" 25 h = {m25[0]:.0f}/{m25[1]:.0f}/{m25[2]:.0f} {[list(r) for r in rows25]}; claims {claims}")
print(f"  [{time.time()-T0:.0f} s]")

# ---------------------------------------------------------------- E10 bloom cadence
print("\nE10 bloom cadence: compact, scattered map, no ammo line, 5 h; metrics over hours 2–5 (after the hour-one claims).")
print("   fights/min = blooms per minute scaled to a 20-edge front (blooms / edge-minutes × 20); mag/edge/min = magazines per edge-minute.")
results['E10'] = {}
for bt, bd in ((120, 0.9), (240, 0.9), (120, 0.8), (240, 0.8)):
    fights, mag_edge, total, wake_share = [], [], [], []
    for sd in SEEDS:
        out, grid, st = R('compact', sd, 5, scatter=True, bloom_t=bt, bloom_drop=bd)
        br = [r for r in st['brec'] if r[0] >= 3600 and not r[3]]
        em = sum(st['edge_min'].values())
        fights.append(len(br) / em * 20 if em else float('nan'))
        mag_edge.append(sum(r[7] for r in br) / 10 / em if em else float('nan'))
        total.append(st['total_mags'])
    results['E10'][f"{bt}/{bd}"] = dict(fights=fights, mag_edge=mag_edge, total=total)
    print(f"  T = {bt}/(0.5+d), drop {bd}: fights/min on 20 edges {mr(fights, 2)}; mag/edge/min {mr(mag_edge, 3)}; total mags 5 h {mr(total, 0)}")
print(f"  [{time.time()-T0:.0f} s]")

# ---------------------------------------------------------------- E12 spike on scattered (before E11: cheaper)
print("\nE12 spike on the scattered map with the ammo line (doc assembler schedule, unfed=substation N40, 300 starting rounds), 5 h.")
results['E12'] = {}
for scat in (False, True):
    lost, mags, held, ff, reasons = [], [], [], [], {}
    for sd in SEEDS:
        out, grid, st = R('spike', sd, 5, scatter=scat, **P)
        lost.append(st['lost']); mags.append(st['total_mags']); held.append(hrow(out, 5)[1]); ff.append(st['first_fall'])
        for e in st['lost_log']:
            k = f"{e[1]}"; reasons[k] = reasons.get(k, 0) + 1
    results['E12']['scattered' if scat else 'open'] = dict(lost=lost, mags=mags, held=held, first_fall=ff, reasons=reasons)
    print(f"  {'scattered' if scat else 'open ground'}: blocks lost {lost} (mean {statistics.mean(lost):.1f}); held at 5 h {held}; total mags {mr(mags, 0)}; first fall {[fm(x) for x in ff]}; fall reasons {reasons}")
# same on compact for the ratio the doc quotes
cl = []
for sd in SEEDS:
    out, grid, st = R('compact', sd, 5, scatter=True, **P)
    cl.append((st['lost'], st['total_mags']))
print(f"  compact scattered with the ammo line: lost {[c[0] for c in cl]}, mags {[round(c[1]) for c in cl]}")
results['E12']['compact_scattered'] = cl
print(f"  [{time.time()-T0:.0f} s]")

# ---------------------------------------------------------------- E13 inert-as-dark
print("\nE13 scattered inert counted as Dark for enclosure (validator inert-dark: scattered cells are void — neither solid nor hostile; the river stays solid). 5 h, no ammo line.")
results['E13'] = {}
for val in ('none', 'inert-dark'):
    for pol in ('compact', 'cheapest', 'river'):
        mags, fi, rows = [], [], []
        for sd in SEEDS:
            out, grid, st = R(pol, sd, 5, scatter=True, validator=val)
            mags.append(st['total_mags']); fi.append(st['first_interior']); rows.append(hrow(out, 5)[1:4])
        results['E13'][f"{val}/{pol}"] = dict(mags=mags, first_interior=fi, rows=rows)
        print(f"  {val:10s} {pol:8s}: total mags {mr(mags, 0)}; first interior {[fm(x) for x in fi]}; 5 h held/front/interior {[list(r) for r in rows]}")
print(f"  [{time.time()-T0:.0f} s]")

# ---------------------------------------------------------------- E11 Relight hold
print("\nE11 Relight hold: compact, scattered map, ammo line (doc schedule → 4 assemblers = 80 mag/min at 25 h), wells' growth ×3 for ten minutes from 25:00.")
results['E11'] = {}
for rl in (None, (25, 10, 3)):
    need, before, lost_w, lost_after, wells_bl, peak = [], [], [], [], [], []
    for sd in SEEDS:
        out, grid, st = R('compact', sd, 25.5, scatter=True, relight=rl, **P)
        w0, w1 = 25*3600, 25*3600 + 600
        br_w = [r for r in st['brec'] if w0 <= r[0] < w1]
        br_b = [r for r in st['brec'] if w0 - 3600 <= r[0] < w0]
        need.append(sum(r[7] for r in br_w) / 10 / 10)
        before.append(sum(r[7] for r in br_b) / 10 / 60)
        wells_bl.append(sum(1 for r in br_w if r[2]))
        # peak minute inside the window
        per_min = {}
        for r in br_w: per_min[(r[0]-w0)//60] = per_min.get((r[0]-w0)//60, 0) + r[7]/10
        peak.append(max(per_min.values()) if per_min else 0)
        lost_w.append(sum(1 for e in st['lost_log'] if w0 <= e[0] < w1 + 600))
        lost_after.append(sum(1 for e in st['lost_log'] if e[0] >= w0))
    key = 'no relight' if rl is None else 'relight ×3 for 10 min'
    results['E11'][key] = dict(need=need, before=before, lost_window=lost_w, lost_after=lost_after, well_blooms=wells_bl, peak=peak)
    print(f"  {key:22s}: mag/min demanded in the window {mr(need, 1)} (hour before {mr(before, 1)}; peak minute {mr(peak, 0)} mags); well blooms in the window {wells_bl};"
          f" blocks lost 25:00–25:20 {lost_w}; lost from 25:00 to 25:30 {lost_after}")
print(f"  [{time.time()-T0:.0f} s]")
json.dump(results, open('phase5_results.json', 'w'), indent=1, default=str)
print("\nwrote phase5_results.json")
