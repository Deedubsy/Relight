# Front rule fix: report

Runs: `python frontsim.py --experiments` (E1–E8, seeds 3–5, 5 h unless stated) and `python frontsim.py --experiments E8`. Every run name below is a labelled line in that output. Doc edits: 45, each listed in the `## Changelog` at the end of `RELIGHT-design.md` as `§N — what changed — why — run name`. All new sim behaviour is behind flags; `python frontsim.py` with no flags prints the legacy comparison unchanged.

## 1. Where the existing sim and the doc disagreed (sanity check, before any change)

1. **Bloom cap.** §5 and §7 said blooms are "capped at 40". The legacy sim had no cap. Applied after wake doubling, a normal bloom never reaches 40 (4 + 36·1.0 = 40), so the cap only ever trims wake blooms: a well's wake bloom goes from 198 to 170 rounds [wake-cap].
2. **Rot regrowth.** §5 said an untouched block reaches 90 % of cap "within one to two hours". The doc's own growth rates give 34 min (outskirts) to 67 min (civic) [sanity].
3. **§7 ammo column.** Stepping the doc's own bloom model to steady state gives residential 1.3 mag/min (doc 1.2), industrial 1.8 (doc 2.2), well 5.3–8.7 + 2.1–2.6 shells (doc 7.7 + 2.5); industrial steady d 0.29 (doc 0.28), outskirts 0.50 (doc 0.49) [E3-block].
4. **§17's 3.4×.** Compact vs cheapest in the legacy sim is 1.79× (10,656 vs 5,956 magazines); spike vs cheapest is 2.86×. No run gives 3.4× [E7].
5. **§24 risk 6 "a third".** The cheapest policy used 56 % of compact's ammo, not a third [E7].
6. **§15 "[sim, coal figures]".** Neither sim has a coal or rubble-depletion model. The tag pointed at nothing.
7. **Power figures.** §12 said 0.3–1.5 MW early, §15 said 2 MW at 3 h, §11 had 2 Generators (0.6 MW) at minute 40. The doc's own draws (200 kW front substation, 40 kW interior, 200 kW contested, 220 kW per assembler line) give a 1.24 MW peak inside hour one, 1.46 MW at 1 h and 4.46 MW at 3 h [E4-literal, E2-demand]. The legacy sim had no power model at all.
8. **§5 fall arithmetic.** 1 tile per 3 s and "~90 s" imply a 30-tile walk; a lot-centre substation is 16 tiles from the street, 48 s. Doc-internal contradiction.
9. **§18 captions vs drawings.** The 5 h caption (34 held / front 24 / interior 12) matches the sim but the drawing holds 52 / 24 / 36, with 5 cells drawn as front that are interior by the 4-neighbour rule. The 25 h caption (~180 / 38 / ~120) matches neither the drawing (313 / 67 / 270) nor the sim (184 / 31 / 155) [§18 recount, E2-demand-25h].
10. **First shades.** §11, §15 and §19 put the first shades at the industrial edge at 60–90 min and "one enemy type" in hour one. The legacy sim already spawned shades in residential blooms at d ≥ 0.3; it never reported them by district. First shade: residential at 47 min compact, 17 min spike [E8-first-shade].
11. **First interior at 40 min.** §11 and §15 assume the river T. None of the sim's policies plays it: first interior 60 min (cheapest) to 205 min (compact) [E7]. The §11 claim sequence itself does enclose the HQ at minute 40 [E4]. Not a doc error; left alone.
12. **Untestable claims.** "Nothing cascades" (§5) and the crawler "Punishes" row (§7) had no consequence in the legacy sim: no power model, instantaneous ammo.
13. **Regression.** With every new model off, the new sim reproduces the legacy numbers to the magazine (10,656 / 16,861 / 17,053 / 5,956 / turtle 528; first interior 205 min). The only diff is one mean-awake-rot digit (0.32 vs 0.33) from float ordering [E7].

## 2. Issue → runs → decision → sections changed

| # | Issue | Runs | Decision | Sections |
|---|---|---|---|---|
| 1 | Unfed edge has no consequence | E1-ring-{none, substation-N10/N20/N40, creep}; E1-starve-{none, substation-N10/N20/N40, creep}; E1-start; E1-start-min; E1-ring-spike; E8-starve-N40-fall16 | Substation rule: 40 unshot crawler arrivals stop the substation. It is the smallest §5/§7 change and the only variant that keeps §11's 20 magazines valid (N = 20 loses the HQ at 7.9 min; N = 40 never). A residential block falls 5.5 [4.2–7.6] min after its first unfed bloom, 7.8 [6.2–10.0] after the ring stops feeding it. The creep variant falls in 2.8 min with nothing to count. Fed compact play at 76 % load loses 0 blocks; under-produced spike play (7–30 % short) loses 4 in 5 h, all on industrial or well edges. | §5, §7, §19, §24 |
| 2 | Brownout cascade | E2-h{500,1000}-{5,15,25}pct-{doc, draw=flat, shed=machines-first, interior-grace=300, flat+grace}; E2 coupling check; E2-demand; E2-demand-25h; E8-h500-*-fall16 | "Nothing cascades" is false while a shortfall lasts: each fallen front block hands 160 kW of new draw to its neighbours, about one knock-on per two direct sheds (15 %: 3 lost from 3 sheds; 25 %: 9 from 9). Shedding machines before substations cuts 15 % to 0 lost and 25 % to 2. Flat draw (4 lost at 15 %) and interior grace (3 and 7) rejected. Losses stop at restoration under both rules. Supply following the §15 schedule loses a block at 12 min. 1 MW headroom with machines-first loses 0 at 25 %. | §5, §12, §14, §15, §19, §24 |
| 3 | Shades and hulks | E3-{compact,spike}-shade{0.3,0.25}-hulk{0.5,0.45}; E3-block; E8-first-shade; E8-first-hulk | Keep thresholds 0.3/0.5 (0.25/0.45 only raises res-far and industrial steady ammo by 0.4 mag/min). Shades on steady blooms: civic 0.02, residential 0.15, deep industrial and outskirts 1.00, wells 0.84–1.00. Hulks: outskirts 47 % of blooms, wells 25–34 %, industrial 4 %. First shade residential 47 min (compact) / 17 (spike); first hulk 154 min at a well in compact play, 52 min on industrial in spike play. §7 rows replaced with stepped steady-state figures. | §5, §7, §11, §15, §19 |
| 4 | First hour | E4-literal; E4-hq-exempt; E4-hq-draw-40; E4-flat-120; E4-start-coal-rubble; E4-coal-120; E4-coal-200; E4-rubble+gen@6,15,25,40(,45); E4-rubble+hq40+gen@15,25,40(,45) | §11 as written browns out at 6.0 min, sheds the HQ at 6.3 (most dark neighbours), runs out of coal at 10.9. Fix inside the rules: a coal patch on the start lot, a second Generator at minute 6 and one per claim (5 by minute 40): no brownout, coal never out, 777 coal burned in hour one. HQ exemption rejected (ammo line stalls, coal still out); 40 kW HQ draw only moves the brownout to 15 min. | §11, §12, §24 |
| 5 | Front bite | E5-{compact,spike}-{base, early-rate-10, bloom-base-8, both, 1-shot-asm-h1, track-70pct} | No rule change. One Shot assembler covers hour one (demand/production 0.41 compact, 0.62 spike); the doc's assembler schedule covers compact play at 0.76–0.81 and spike at 1.30 at 3 h. Bloom base 8 and a faster early bloom rate rejected: numbers with no evidence behind them. §12 early demand replaced by measured 8 → 48 mag/min (1 h → 3 h). | §11, §12, §18 |
| 6 | Inert free walls | E6-{noscatter,scatter}-{none,no2x2,maxrun2}-{compact,river}; E6-scatter-inert-dark-*; E6-{noscatter,scatter}-{compact,spike,cheapest} | Validator is a no-op at 8–10 % scatter (0–1 2×2 squares per seed; identical ammo with and without). Scatter changes the doc's headline ratios: spike/compact 1.60× → 3.21×, compact/cheapest 1.77× → 0.76×. Hugging the river costs +7 % (open) to +96 % (scattered) against compact; no doc sentence depended on it. | §7, §9, §17, §24, §25 |
| 7 | Regression | E7 | Exact match to the legacy sim; old and new numbers are comparable. | §7, §19 (tags only) |
| 8 | Fall distance | E8-starve-N40-fall{30,16}; E8-h500-{15,25}pct-{doc, shed=machines-first}-fall{30,16} | 48 s against 90 s changes no decision: E1 timers move by 0.7–0.8 min, E2 losses are identical. Doc states both figures. | §5, §25 |
| – | Wording: are lots lit? | none needed | Lots are unlit; streets and lamp radii are lit; burn-off clears the whole cell; the shade corridor rule applies on the lot, so the defence is the sealed street ring. | §5 |
| – | Wording: "lights out" | none needed | The substation stopped. An eaten lamp is an unlit gap (shade corridor), not lights out; rot does not creep through it. | §5 |
| – | Wording: Freight tram load | none needed | A Tram stop takes up to 6 inserters (one per tile of its long sides): 600 items in 100 s, 200 in 34 s. | §13, §14 |

## 3. Numbers changed

| § | What | Old | New | Run |
|---|---|---|---|---|
| §5 | 90 % of rot cap | 1–2 h | 34–67 min | sanity |
| §5, §7 | residential / rail yard ammo | 1.2 mag/min | 1.3 | E3-block |
| §5, §7 | industrial ammo | 2.2 mag/min (0.07/tile) | 1.8 (0.06/tile) | E3-block |
| §7 | industrial steady d | 0.28 | 0.29 | E3-block |
| §7 | outskirts steady d | 0.49 | 0.50 | E3-block |
| §7 | well steady d / ammo | ~0.75 / 7.7 + 2.5 | 0.54–0.78 / 5.3–8.7 + 2.1–2.6 | E3-block |
| §5 | in-play per-edge cost | (absent) | residential 1.5, civic 0.9 mag/min | E3-compact-shade0.3-hulk0.5 |
| §5 | ways a block falls | 3 | 4 (40 unshot crawlers), 5–8 min on a residential edge | E1-starve-substation-N40 |
| §5 | fall time, lot-centre | ~90 s | ~48 s (16 tiles); 90 s far side | E8-fall16 |
| §5 | cascade | "nothing cascades" | 3 / 9 lost (15 / 25 %) substation-first; 0 / 2 machines-first | E2-h500-* |
| §5, §7 | wake cap effect | (unstated) | 198 → 170 rounds; worst fight 40 crawlers + 5 shades | wake-cap |
| §7, §9 | spike / compact | 1.6× (17,053 / 10,656) | 1.6× open (17,052 / 10,679); 3.2× scattered (14,152 / 4,414) | E6-noscatter, E6-scatter |
| §7 | hulk share of blooms | (unstated) | wells / outskirts 25–47 %, industrial 4 % | E3-* |
| §11 | Generators by minute 40 | 2 | 5 | E4-rubble+gen@6,15,25,40 |
| §11 | hour-one peak draw | (unstated) | 1.24 MW | E4-literal |
| §11 | hour-one ammo demand vs one assembler | (unstated) | 8 vs 20 mag/min | E5-compact-1-shot-asm-h1 |
| §11, §15, §19 | first shade | industrial, 60–90 min | residential, 47 min compact / 17 spike | E8-first-shade |
| §19 | first hulk | with the shades | well or outskirts, 99–154 min | E8-first-hulk |
| §12 | early ammo demand | 10–30 mag/min | 8–48 | E5-compact-base |
| §12 | early power | 0.3–1.5 MW | 1.2–4.5 | E4-literal, E2-demand |
| §12 | mid power | 3–8 MW | 4.5–8 | E2-demand-25h |
| §13, §14 | Tram stop inserters / Freight load | (unstated) | up to 6; 100 s (600 items), 34 s (200) | wording, no sim |
| §15 | power at 3 h | 2 MW | 4.5 | E2-demand |
| §15 | demand the Turbine hall's 5 MW meets | (untagged) | 5.5 MW at 4 h, 7.6 at 10 h | E2-demand-25h |
| §17 | compact vs quietest-block | 3.4× | 1.77× open; 0.76× scattered | E6-noscatter, E6-scatter |
| §18 | 5 h caption | 34 / 24 / 12 | drawing 52 / 24 / 36; sim 34 / 24 / 12 | E7-compact, recount |
| §18 | 25 h caption | ~180 / 38 / ~120 | drawing 313 / 67 / 270; sim 184 / 31 / 155 | E2-demand-25h, recount |
| §18 | 5 h assemblers | 3 | 3–4 | E5-compact-base |
| §24 | cheapest / compact ammo | a third | 56 % open; 131 % scattered | E6-noscatter, E6-scatter |
| §24 | "20 % short is survivable" | true | 4 blocks lost in 5 h on spike; 0 on compact at 76 % | E1-ring-spike, E1-ring-substation-N40 |

Numbers left alone for lack of a run: §12 mid and late ammo demand (60–150, 150–250 mag/min), §12 late power (10–15 MW; the sim's 12.9 MW at 25 h sits inside it), §15 "coal runs out for the second player in three", the claim cadence, and the 0.3/0.5 shade and hulk thresholds (tested, kept).

## 4. Not settled, and what would settle it

1. **Belt latency.** Hoppers fill instantly from the ring whenever rounds exist, so the 5–8 min unfed timer is the ring-empty case only. Add a per-edge delay proportional to ring distance and rerun E1-starve.
2. **Claim cadence.** Every run uses 4 claims in hour one then 1 per 8 min, and the §18 drawings imply faster play (52 and 313 blocks against 34 and 184). Sweep the claim gap from 4 to 8 min and compare held counts to the drawings; then either redraw §18 or change the cadence. *Done as E9 in section 6: the drawings imply a claim every ~5 minutes; the drawings stay and the prototype's claims-per-hour telemetry decides.*
3. **Outskirts and well edge cost in play.** The 12 mag/edge/min outskirts figure rests on 15 blooms, the well figures on one well. A 10-hour compact run that reaches the outskirts (or a spike run past the first well) would give real per-edge means.
4. **Coal depletion and the Turbine hall.** No rubble-depletion model, so the §15 timing claim is unsupported. Needs excavator throughput against per-block rubble stock; then rerun E2-demand-25h with coal as the supply.
5. **Ammo demand past 5 h, power past 25 h.** The mid and late §12 columns keep their estimates. Extend E5 and E2-demand to 12 and 40 h.
6. **Fall distance, 48 s vs 90 s.** The sim is indifferent; which one ships is a feel question for the prototype.
7. **The river T.** No policy encloses the HQ first, so the 40 min first-interior claim is only supported by the scripted §11 sequence. A policy that prioritises the HQ's neighbours would show what it costs in ammo.
8. **Shade threshold at 0.25.** Steady-state ammo barely moves, but first-shade timing at 0.25 was not measured. Rerun E8-first-shade with `--shade-threshold=0.25`.

## 5. Tempted to add as a new system, and did not

1. **A hopper ration or priority rule.** The starve variant (withhold from edges with d < 0.3) is a sim policy standing in for player behaviour, not a game rule; the doc keeps hopper distribution as the player's problem.
2. **Interior grace** (`--interior-grace`): interior substations survive a shed for 300 s. Tested; losses went 3 → 3 and 9 → 7. A new rule that does not fix the problem.
3. **Flat 120 kW substation draw** (`--draw=flat`). Tested; front and interior then tie in the shed order and losses rise (4 at 15 %). Worse.
4. **HQ exempt from shedding** (`E4-hq-exempt`). The ammo line stalls instead and coal still runs out at 10.9 min. A special case that moves the failure.
5. **Coal and rubble depletion.** Would be an economy subsystem; left as open question 9.
6. **Belt latency on the ammo ring.** Left as open question (item 1 above).
7. **Lot lighting from the substation.** Considered for the "are lots lit" gap; it would be a new lighting mechanic, so the doc instead defines lots as unlit and the shade corridor rule as applying on the lot.
8. **A stronger first-hour bite** (bloom base 8, faster early blooms, `E5-*-bloom-base-8`, `E5-*-early-rate-10`). E5 shows one Shot assembler already covers hour one; changing the numbers had no evidence behind it.
9. **An arrivals counter on the front pip.** The 40-arrival rule wants a visible count; the doc already has the supply pip going red, and no new HUD element was added.
10. **Assembler auto-tracking** (`--asm-track`). A sim model of a player adding assemblers as demand grows; not a rule.

## 6. Prototype-brief runs (Phase 5: E9–E13)

Run scripts: `phase5.py` (E9, E10, E12, E13, E11 → `phase5_results.json`) and `phase5b.py` (E11 with six assemblers → `phase5b_results.json`). All runs: scattered map (canonical), seeds 3/4/5, jitter 0.1, the fall-sync fix from section 2 in place. One knob was added to `frontsim.py` for E9: `gap_after` (default 8 min) sets the claim gap after hour one when `claim_gap` is not given; hour one keeps the 15-minute gap in every run. Nothing else in the sim changed.

### E9 — claim cadence (compact, no ammo line, 25 h)

Question: what claim gap after hour one matches the §18 drawings (52 blocks at 5 h, 313 at 25 h)?

| gap after h1 | 5 h held / front / interior | 25 h held / front / interior | claims by 25 h |
|---|---|---|---|
| 4 min | 64 / 22 / 45 | 364 / 24 / 341 | 363 |
| 6 min | 44 / 19 / 27 | 244 / 33 / 213 | 243 |
| 8 min (doc) | 34 / 16 / 19 | 184 / 39 / 148 | 183 |

Per-seed held counts are identical across seeds (the cadence, not the rot, sets the count); front and interior vary by a few blocks. The drawings' counts need 48 claims in hours 2–5 (a 5.0-minute gap) and 309 in hours 2–25 (4.7 minutes). Decision: the drawings stay; §25 item 10 and the §18 captions say the drawings imply a claim every ~5 minutes, and the prototype's claims-per-hour telemetry decides which cadence players actually pick. Tag `[sim: E9-claim-cadence]`.

### E10 — bloom cadence (compact, no ammo line, hours 2–5 of a 5 h run)

Question: the bloom timer `T = 120/(0.5+d)` and the 10 % rot drop per bloom had never been varied.

| timer base / drop | fights per min on 20 edges | mag per edge-minute | total mags 5 h [seeds] |
|---|---|---|---|
| 120 s / 0.9 (doc) | 6.76 [6.62–6.83] | 1.370 [1.341–1.397] | 4414 [3768–5307] |
| 240 s / 0.9 | 3.62 [3.50–3.73] | 0.965 [0.876–1.066] | 3204 [2556–3725] |
| 120 s / 0.8 | 6.12 [5.87–6.34] | 1.042 [0.907–1.183] | 3434 [2823–3884] |
| 240 s / 0.8 | 3.28 [3.20–3.33] | 0.690 [0.670–0.705] | 2331 [1990–2787] |

A 240 s timer halves the fight rate and cuts ammo per edge 30 %; a 20 % drop keeps the fight rate and cuts ammo 24 %. Decision: which rhythm is right is a feel question for the prototype, not a sim one; §25 item 1 carries the numbers. Tag `[sim: E10-bloom-cadence]`.

### E11 — the Relight hold (compact, ammo line, doc assembler schedule, 25 h)

Question: §25 item 5 — is ten minutes at ×3 wells a wall or a nothing? The sim's relight multiplies well growth by 3 for the window; it does not make every awake block bloom at its wake size (§16 says both), so these are floor numbers.

| assemblers at 25 h | window demand mag/min (hour before) | peak minute, mags | blocks lost 25:00–25:20 | same without relight |
|---|---|---|---|---|
| 4 (80 mag/min made) | 133 [127–137] (83) | 194 [175–208] (120) | 18 / 24 / 20 | 2 / 3 / 2 |
| 6 (120 mag/min made, E11b) | 192 [185–195] (123) | 293 [268–306] (162) | 29 / 24 / 28 | 2 / 3 / 3 |

At both production levels compact play at the doc cadence is at parity by 25 h (83 demanded against 80 made, 123 against 120): the front grows until the ammo line caps it. The surge is 1.6× the preceding hour and 2.3× at peak, and costs 18–29 blocks in twenty minutes. Decision: a wall at parity, §16 step 4 and §25 item 5 updated. Not run: a banked stock of magazines before the Relight, because no compact run ever has surplus to bank; the E11b runs also lost 25–62 blocks before 25 h (held 122/151/159 at 25 h), so the doc cadence with six assemblers is still ammo-limited. Tag `[sim: E11-relight-hold]`.

### E12 — spike on the scattered map (spike, ammo line, doc schedule, unfed rule N40, 300 start rounds, 5 h)

Question: §25 item 13 — is spike a trap on the scattered map?

| map | blocks lost 5 h | held 5 h | total mags | first fall |
|---|---|---|---|---|
| open ground | 4 / 4 / 4 (all to shades) | 30 | 16478 [16457–16503] | ~160 min |
| scattered | 0 / 0 / 0 | 34 | 14152 [13657–14473] | never |
| compact, scattered, same line (reference) | 0 | 34 | 4414 [3768–5307] | never |

Spike costs 3.2× compact and loses nothing. Decision: item 13 closed as a choice, not a trap; §19's loss count is marked open-ground. Tag `[sim: E12-spike-scattered]`.

### E13 — scattered inert as Dark (5 h, no ammo line, three policies)

Question: §5 counts scattered inert cells as solid for enclosure; the other reading counts them as Dark (neither solid nor hostile in the sim's `inert-dark` validator).

| policy | mags, inert as solid (doc) | mags, inert as Dark | first interior, doc → inert-dark (min) |
|---|---|---|---|
| compact | 4414 [3768–5307] | 4414 (same) | 109 / 69 / 101 → 141 / 77 / 101 |
| cheapest | 5772 [5551–6026] | 5772 (same) | 60.5 all → 60.5 all |
| river | 7177 [6838–7799] | 8656 [7189–9489] (+21 %) | 109 / 69 / 101 → 189 / 213 / 213 |

Compact and cheapest make the same claims either way; only the river-hugging policy pays, and its first enclosure comes 1.5–2.5 hours later. Decision: §5 stands; §25 item 2 says what the other reading would cost. Tag `[sim: E13-inert-dark]`.

### Left open by Phase 5

1. Which claim cadence players pick (E9 gives the map from cadence to count; the prototype's telemetry gives the cadence).
2. Which bloom rhythm feels right (E10 gives the ammo cost of each; only play tells).
3. Whether a banked magazine stock survives the Relight hold (E11 never had surplus to bank; a run with production set above demand at 25 h would say).
4. The wake-size bloom of every awake block during the hold is not modelled; E11 is a floor.
