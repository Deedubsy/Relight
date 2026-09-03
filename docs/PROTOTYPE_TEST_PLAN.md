# Map-view prototype — test plan

The prototype is `packages/proto` (`npm run dev -w @relight/proto`, then `http://127.0.0.1:5173/?seed=3`). It tests one question from §25: is choosing blocks on a map with a front count interesting on its own? Everything that would answer it better with tiles, belts, or lighting is in `DEFERRED.md`.

## Protocol

- **Five testers**, one at a time, same seed: `?seed=3` (share the link the "Copy link" button copies). Economy on, scattered map on. No `autoplay`. The HUD export's `meta.configHash` must read `6e74fbfd`, the calibrated config (`CALIBRATION_REPORT_2.md`); an export with another hash is a different game and does not count.
- **One control session**, a sixth tester, with `?seed=3&economy=0` (hash `91bad3aa`): the same map with claims and assemblers free and no rubble; machine slots still apply. It does not count toward the five; it is there to separate what the front rule does from what the steel budget does.
- **Two and a half hours of sim time each**, at 4× (key `2`; the sim clock is in the HUD; the observer ends the session when it passes 2:30:00). That is about 38 minutes of real time. The length is set so that a tester who never builds a second assembler meets the first amber pip inside the session (expected timeline below), which the two-hour session did not guarantee.
- **No instructions beyond §5's rules**, read aloud once before the session starts, verbatim from the doc: the paragraphs *Block states*, *Rot*, *Waking*, *How a block becomes Held* and *Frontage*, nothing after the cost table. The panel's own hint text says the same thing in three sentences; nothing else is explained. Questions during the session get "whatever you think" as the answer.
- **The observer** sits behind the tester with a notepad and writes down, with the sim clock, every unprompted remark about the shape of the territory ("I want to close that gap", "I'm going to go around the well", "this is getting long"), every remark about the ring order, every remark about the machine slots (the small square in a held block: outline = free interior slot, filled = machine, red = machine on a block that is back on the front) and every remark about rubble running out (the strip at the top of a held block fades; the start block is dug out at about 2:00). Prompted remarks (answers to a question) are logged separately and do not count.
- At the end the tester presses **Export telemetry JSON**; the file is named `relight-seed3-<simclock>.json`. The observer collects the file and the notes before the post-test questions.

## Expected timeline

From the calibration harness (`packages/sim/test/calibrate.ts`, config `6e74fbfd`, seeds 3/4/5, 3 h; `CALIBRATION_REPORT_2.md`). Bots claim every 15 min in hour one and every 5 min after, so a tester at that pace holds about 4 blocks at 1:00 and 16 at 2:00. Two doc rules are now in the build: only an interior block (or the HQ) can hold a machine, one per block, and each block's rubble is finite. Times are sim clock, ranges across the three seeds.

| Sim clock | Compact play (blob around the start) | Straight push north (spike) | Quiet-block play (cheapest) |
|---|---|---|---|
| 0:00–1:00 | 1 assembler (the HQ's Mk1), 10 mag/min; buffer fills to 400 magazines by 1:20; every pip green; no free slot, so the build button is greyed with the reason | same; every block the push holds is front, so it never has a slot | same |
| 1:00 | first enclosure on the 4th claim (all seeds): one interior slot appears | no interior, ever, on any seed | first enclosure 1:00 |
| 1:03–1:11 | a HUD-watcher buys the second assembler from start stock the minute the slot exists (1:03, seed 5 1:11); after that no pip leaves green before 3:00 | **first amber pip 1:06–1:07, red the same minute or two later**, first block lost 1:08–1:10; nothing can be built because there is no slot; the bot retakes and re-loses the same 2–3 blocks every 5 minutes (23–24 losses by 3:00) | second assembler at 1:03 if the tester watches the HUD |
| 1:41–1:49 | — | — | if nothing is built: first amber and red 1:41–1:44, first loss 1:43–1:49, 16–17 lost by 3:00 held 11–12; a build at the pip loses nothing |
| 1:59–2:47 | if nothing is built: **first amber 1:59 (seed 4: 2:42), red two minutes later**, first loss 2:05–2:47, 3–12 lost by 3:00; a build at the pip (1:59/2:42/1:59) loses nothing | — | — |
| 2:00 | start patch empty and the HQ block dug out (its rubble strip gone); the next three start-row blocks empty at 2:15, 2:30, 2:45; steel then comes from held industrial blocks; slots 2 used / 6 free | 1 used / 0 free; steel 6–7 k, never short | 2–3 used / 3 free; steel 0–0.9 k at 3:00, one seed hits 0 at 2:59 with no stall |
| 2:30 | session ends: held ≈ 22 at bot pace, 0 lost, 4 blocks dug out by 3:00 | ≈ 4–5 held | ≈ 22 |

What the table says the test can and cannot see: the front's price now shows as shape, not as a pip. A push holds four blocks and can never build because none of them is interior; a blob has its first slot at 1:00 and a HUD-watcher who fills it never sees a pip in the session. The pip ladder still gives about two minutes between amber and red, because a pip leaves green only when the 400-magazine buffer is already empty; a tester who waits for a pip before building is one bloom from a loss. Whether a tester reads the slot square and the fading rubble strip as reasons to enclose is what the observer notes are for; the harness could not make compact's first amber fall inside 60–120 min with the levers it was allowed (`CALIBRATION_REPORT_2.md` part 5).

Under `economy=0` (hash `91bad3aa`) assemblers are free but still need an interior slot, so the "if nothing is built" rows hold for every shape until the tester encloses; rubble does not run out.

## Metrics

All from the telemetry export unless marked observer.

| Metric | Where | Why |
|---|---|---|
| Territory shape at 2 h: bounding-box aspect (w/h), perimeter/area, held, interior | `minutes[]` last row; `summary.shape` | Distinct shapes are the test |
| Claims per hour | `summary.claimsPerHour` | §25 Q10, the §18 cadence question |
| Blocks lost | `losses[]`, `summary.lost` | Whether the front is felt |
| Ring ever reordered | `reorders[]` | Whether the ammo order is a decision |
| Time of first enclosure | `summary.firstEnclosure` | Whether Interior is discovered without being taught |
| Assemblers built, and when against the first amber pip | `assemblers[]`, `summary.firstAmber`, `summary.firstRed` | Whether ammo production is managed, ignored, or managed only once a pip turns |
| Config hash | `meta.configHash` | Must be `6e74fbfd` (`91bad3aa` for the control) |
| Machine slots used / free at 1 h, 2 h, end; assemblers rejected | `minutes[]` (`slotsUsed`, `slotsFree`, `atRisk`), `assemblersRejected[]` | Whether the tester reads the interior as the place the factory goes |
| Blocks dug out (rubble gone) and when | `dry[]`, `summary.ranDry` | Whether finite rubble is noticed before it bites (the start block empties at 2:00) |
| Unprompted shape remarks | observer notes | The other half of "interesting" |

**Distinct shapes.** Two testers' territories at 2 h count as distinct if their perimeter/area ratios differ by more than 0.15 or their bounding-box aspects differ by more than 0.3. `PROTO-ASSUMPTION`: these thresholds are picked to separate the sim's compact (aspect ≈ 1, ratio ≈ 0.7 at 30 blocks) from its spike (aspect ≈ 0.4, ratio ≈ 1.2); nothing in the doc sets them. Perimeter counts Held sides facing Dark or Contested only, so inert and the map edge do not inflate it.

## Outcomes

- **Success** — at least 3 of the 5 territories are distinct by the rule above, at least one unprompted shape remark, and at least 2 testers reorder the ring at least once. Proceed to world view. The three decisions in `PROTOTYPE_BUILD_REPORT.md` are made first.
- **Rework** — shapes converge on one policy. Name it by matching each territory to the closest sim bot (compact, spike, balanced, cheapest, river) on aspect and ratio, and say which one won. The tuning surface is the four numbers per district that §24 names (dmax, g, bloom base, bloom slope) plus scatter density. E9 (claim cadence) informs a convergence on compact, because if claims are cheap enough to be constant the front count stops mattering; E12 informs a convergence away from spike, because if spike loses blocks on the scattered map nobody will try it twice; E13 informs a convergence on river or cheapest, because inert-as-solid makes the scattered cells free walls.
- **Kill** — nobody talks about shape and nobody reorders the ring. Go back to §23's rejected list and pick the next mechanic; the front rule as a map-view decision is not carrying the game.

Anything in between (say two distinct shapes, or reorders without remarks) is rework, not success: the bar is the one above, and the sample is five.

## Post-test questions

Asked in this order, open-ended, after the export. The observer writes the answers down and does not follow up.

1. Tell me about the territory you ended up with.
2. Was there a moment where you changed your mind about where to go next? What happened?
3. What did the list on the right mean to you?
4. What did you want to do that you could not?
5. If you played the same map again tomorrow, what would you do differently?

## Changelog

- Protocol — session is 2.5 h sim at 4× (≈ 38 real minutes), a sixth `economy=0` control session, config hash named; expected-timeline table added — the two-hour session ended before the first pip for compact play (build report item 6.1) — CALIBRATION_REPORT.md
- Protocol, timeline, metrics — config hashes `6e74fbfd` / `91bad3aa`; expected-timeline table replaced with the calibration-2 results (slots, run-dry, no compact pip when built); slot and run-dry rows in the metrics and in the observer's brief — machine slots and finite rubble entered the build — CALIBRATION_REPORT_2.md
