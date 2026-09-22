# REL-11 (AUD-INT-07) — the place has a name, and coming back is not connecting

**Date:** 2026-09-23. **Build:** local `main`, unpushed. **Played by:** nobody.

## What the audit saw

> "Substation 0 connected", said again on every refuel `[CODE]` `[SEEN]`

Two faults in one line: the notice used the substation **site's** own name, and it treated every regained supply
as a fresh connection, so a belt-fed Generator that ran dry and was refuelled repeated the whole announcement —
line, sweep and fanfare — each time.

## Premise checked first

Both halves were read before anything changed.

- **The name.** `LightPhase.Relief` put `site.Name` into `DistrictLitEvent`, and the city importer names every
  substation site "Substation N". Run live against the loaded city, all nine sites are numbered — and every one
  of them sits inside a district that already has a real name:

  | site.Name | place (`Districts.NameAt`) | streetlights owned |
  | --- | --- | --- |
  | Substation 0 | Founders Court | 6 |
  | Substation 1 | Riverside Works | 2 |
  | Substation 2 | Ironworks | 3 |
  | Substation 3 | Civic Utility | 3 |
  | Substation 4 | Westridge Homes | 1 |
  | Substation 5 | Old Town | 1 |
  | Substation 6 | Northwood Freight | 1 |
  | Substation 7 | Ravenholm Quarry | 1 |
  | Substation 8 | East Wharf | 0 |

  So the fix is not to rename the sites — the place name already exists, and `Districts.NameAt` is the same
  query (INT-09a) the defence and power rows name their place with. One spot is now never named two ways.

- **The repeat.** `s.LiveDistricts` remembered which districts were lit, and the event fired on every off→on
  edge. That is correct for the lamps — they really do come back on — but the *announcement* was wrong: losing
  the line and getting it back is a **return**, not a second connection.

## What changed

| Where | Was | Now |
| --- | --- | --- |
| `DistrictLitEvent` | `(T, SiteId, Name, Lights, X, Y)` | `+ bool Returning = false` |
| `LightPhase.Relief` name | `site.Name` → "Substation 0" | `Districts.NameAt(centre) ?? site.Name` → "Founders Court" |
| `LightPhase.Relief` repeat | every off→on raised a connection | first time connects; afterwards `Returning` |
| `HudViewModel` notice | `Founders Court connected · 6 streetlights on` | on a return: `Founders Court: power back` |
| `AudioCueRouter` | `light.district-on` on every event | cue on the connection only |
| `LightState` | `LiveDistricts` | `+ AnnouncedDistricts` (transient) |

**The sweep still draws on a return.** The lamps genuinely relight, so the picture is not a lie — only the
announcement is. What a return loses is the "connected" line and the fanfare.

**Nothing is saved.** `AnnouncedDistricts` is transient like the rest of the relief memory (U-D-31) and is
cleared in the `IsReading` branch of `Visit`. On load, a district that is already live is taken as already
announced; one that is dark now and comes back later says "connected" once more. No save field, no schema
change — which is what the issue's reconciled note asked for.

## The captures

Both are 1920×1080 Game view frames at the opening, notice row top right.

- `connected.png` — `Founders Court connected · 6 streetlights on`
- `power-back.png` — `Founders Court: power back`

**How they were staged.** The two notices share one notice key, so they cannot appear in the same frame. An
editor script read the **real** first substation site out of the running game (`site.Name` = "Substation 0",
place = "Founders Court", 6 streetlights owned) and handed the HUD one `DistrictLitEvent` for it — the first
capture with `Returning = false`, the second with `Returning = true`. That stages the presentation only: the
sim rule that decides which of the two an event is, is covered by the tests below, and the live figures the
script read back were exactly the two lines printed above.

## Checks

- Offline sim tests: **922 passed, 1 skipped** (was 919 + 1).
- EditMode: **1018 of 1018** (was 1015).
- PlayMode: **97 total — 92 passed, 0 failed, 5 skipped** (unchanged).
- New and renamed tests:
  - `LightReliefTests.LosingTheLineAndRestoringItIsAReturnNotASecondConnection` (renamed from the old
    "…RaisesItAgain"): cut the supply, restore it, and the second event is still raised — the lamps light — but
    the first is `Returning == false` and the second `Returning == true`.
  - `LightReliefTests.ARefuelledGeneratorDoesNotAnnounceTheDistrictAgain`: the issue's own case. A belt-fed
    Generator starves, the district goes dark, the Generator is refuelled — two events, the second a return.
  - `LightReliefTests.TheEventNamesThePlaceAndNeverTheSubstationNumber`: a site literally named "Substation 3"
    inside a district named "Ironworks" raises an event whose `Name` is "Ironworks", contains no "Substation",
    and equals `Districts.NameAt(ev.X, ev.Y)`.
  - `HudViewModelTests.ADistrictComingBackSaysPowerBackAndNotConnectedAgain`: the connection posts
    "Ironworks connected · 6 streetlights on"; the return replaces it with "Ironworks: power back", says
    nothing about connecting, and stays one row.

## Limits

- Nobody has played this build. The captures were taken by the assistant.
- A map with no districts at all still falls back to the site's own name; on the real city that path is never
  taken (all nine sites are inside a named district, shown above).
- The transient set means a save reloaded into a dark district will say "connected" when it comes back. That is
  the issue's own accepted trade — it keeps the save schema untouched.
- Whether the sweep should also be quieter on a return is a look question, not this issue's.
