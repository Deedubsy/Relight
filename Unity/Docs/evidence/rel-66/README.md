# REL-66 — what a load has to tell the player

PER-06. A save can load successfully and still have something the player needs to know: the file was written by an
earlier build and was read through an upgrade, it was made against different balance data, or the file asked for was
unusable and its backup was loaded instead. All three were effectively invisible.

Built 2026-09-23. Code and automated checks only. **Nobody has played this build.**

## The premise, checked first — and it is wrong in detail

The issue says: *"per agent 9, the save-upgrade sentence and the `.bak` recovery sentence are written with
`Debug.Log` only"*, and asks for that reading to be confirmed before starting. It is not accurate. Two on-screen
routes already existed. Both were broken, in different ways, and neither reached all three sentences.

| The claim | What the code said |
| --- | --- |
| The sentences go to `Debug.Log` only | **No.** `AutosaveController.Load` logs all three (`AutosaveController.cs:231-233`) **and** two other paths tried to show them. |
| — route 1 | `FrontEndBootstrap.Collect` put them into `StartupNotice` through an `else if` chain: at most **one** of the three, whichever the chain reached first. `StartupNotice` is read by the pause menu, so the sentence surfaced only if and when the player next opened it — a *startup* notice, shown at an unrelated moment, about a *load*. |
| — route 2 | `PauseMenuController.DoLoad` wrote `Recovered` into `_loadNotice` — and then switched the panel to "pause" on the next line and closed the overlay on the one after. The sentence was written and thrown away inside the same call; nothing was ever on screen long enough to read. That route never carried `Upgraded` or `Warning` at all. |
| Effect on the player | The balance-data sentence in particular had no on-screen route of any kind. A load that quietly changed what recipes cost said nothing. |

So this is not "add the missing route". It is **one route replacing two broken ones**, and the two are retired here.

## What changed

**1. `HudViewModel.SayLoad(LoadResult, now)` — the one place a finished load speaks.** It takes the load that is
current, and says each non-empty sentence as its own HUD row:

| Sentence | Key | Kind |
| --- | --- | --- |
| Recovered from the backup | `load:recovered` | Warning |
| Different balance data | `load:warning` | Warning |
| Upgraded / moved region / healed on load | `load:upgraded` | Info |

Three separate rows, not a chain: they are independent facts about the same file, and an `else if` chain silently
drops the ones it does not reach — which is exactly what route 1 did. They are posted through `Teach`, the standing
guide-row path (REL-118), so a burst of raid rows can only *delay* a load sentence, never expire it unread: **"once"
means once seen, not once timed out.**

`LoadSentence` capitalises the store's words and ends them with a stop. Nothing is reworded. The save layer owns what
is true about the file; a HUD that paraphrased it would be a second version of the same fact.

**2. The `LoadResult` instance is what marks it said.** `SayLoad` keeps a reference to the last result it spoke and
ignores that same object ever after. So the caller needs no timing, no event and no flag of its own — it can hand the
same load over on every paint — and a HUD that only wakes *after* the load still says it, which the two old routes
could not.

**3. `HudController.Paint` hands it over**, on the ordinary paint, from the `AutosaveController` it finds once. A load
raises `SessionChanged`, and `Paint` rebuilds `_model` when the session changes, so the new model gets the sentence
for the load that just happened.

**4. A refusal is not said here.** It changed nothing — the running game is untouched — and the Load screen already
shows the reason on the screen the player is looking at. Saying it again on the HUD would be a second copy of a
message the player has just read.

**5. The log is untouched.** `AutosaveController.cs:231-233` still writes all three, as accept criterion 1 requires.

**Shared, as the issue asks.** *"the old-saves issue (ECO-05) needs the same on-screen route; build it once here and
share it"* — the route is `HudViewModel.SayLoad`, which takes a `LoadResult` and nothing else. ECO-05 can reach it
without a new mechanism.

## Checks

| Suite | Result | Before |
| --- | --- | --- |
| Offline sim (`dotnet test`) | **936 passed, 1 skipped** | 932 passed, 1 skipped |
| Unity EditMode | **1032 / 1032** | 1028 / 1028 |
| Unity PlayMode | **97 total — 89 passed, 3 failed, 5 skipped** | 97 total — 89 passed, 3 failed, 5 skipped |

PlayMode was run even though the issue only asks for EditMode, because this change adds work to `HudController.Paint`
and could have raised an unexpected row in a scene test. It is **identical to the REL-62 baseline**: the same three
tests fail with the same three messages — `DroppedCargoPlayTests` ("E did not collect the pile"), `OpeningUiPlayTests`
U6 ("E at the damaged core did not open the Home drawer") and `EscapeCancelsRepairPlayTests` ("Escape did not cancel
the repair"). Nothing here made them worse and nothing here fixed them; the drop-collect one is open as REL-125.

Four new tests, `Tests/Sim/UI/LoadNoticeTests.cs`. Three of them drive a **real save on a real disk**, in a throwaway
folder under the system temp path (the pattern `RealDiskRecoveryTests` established), so the fixtures are files the
test makes and deletes — accept criterion 2 asks for fixtures outside `Unity/Docs/evidence/`, and these are outside
the repository altogether.

1. `ASaveFromAnEarlierBuildSaysSoOnTheHudAfterItLoads` — a genuine version-11 document (the state without the quiet
   spell v12 added, re-stamped and re-checksummed as that build would have written it) is put on disk, loaded through
   `SaveStore`, and the `load:upgraded` row carries the store's own sentence, as Info.
2. `AGameLoadedFromItsBackupSaysSoOnTheHud` — two real saves so the write protocol leaves a `.bak`, the primary is
   then made unreadable on disk, and the recovery raises `load:recovered` as a Warning.
3. `ASaveMadeWithDifferentBalanceDataSaysSoOnTheHud` — a saved file re-stamped with a foreign `dataVersion`, which
   loads (a changed recipe is a difference, not damage) and raises `load:warning`.
4. `TwoSentencesAtOnceAreBothSaidOncePerLoadAndARefusalSaysNothing` — a result carrying two sentences says **both**,
   five further hand-overs of the same result add nothing, the rows retire once seen rather than on a clock, and a
   `LoadResult.Refuse` says nothing at all.

**Accept criterion 3 is argued, not tested.** "The notice does not pause the game or move the camera" holds by
construction: `HudViewModel` holds no engine reference and can reach neither the clock nor the camera, and the row is
posted into the same inbox every other HUD notice uses. **No test in this file proves it**, and none is claimed to.

No new decision. No save-schema change.

## Files

* `Unity/Relight/Assets/Relight/Sim/UI/Hud/HudViewModel.cs` — `SayLoad`, `LoadSentence`, the three keys, `_loadSaid`.
* `Unity/Relight/Assets/Relight/UI/Hud/HudController.cs` — the hand-over in `Paint`.
* `Unity/Relight/Assets/Relight/UI/FrontEnd/FrontEndBootstrap.cs` — route 1 retired (the refusal path is untouched).
* `Unity/Relight/Assets/Relight/UI/PauseMenu/PauseMenuController.cs` — route 2 retired.
* `Unity/Relight/Assets/Relight/Tests/Sim/UI/LoadNoticeTests.cs` — new, four tests.
* `Unity/Relight/Assets/Relight/Presentation/Persistence/AutosaveController.cs` — **unchanged**, deliberately: the
  three log lines stay.
