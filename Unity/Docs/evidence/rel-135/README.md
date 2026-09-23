# REL-135 — typing a save name no longer works the hotkeys underneath

PLAY-10, the tenth of the owner's ten notes from the 2026-09-22 play of the batch-3 build:

> When typing in the save menu, if I press 'B' it still opens up the build menu. It shouldn't

Built 2026-09-23. Code and automated checks only. **Nobody has played this build.**

## The premise, checked — one fault, not the two the issue claimed

The issue was written with two stacked faults in `UiShell.TextInputFocused()`. Only the first is real.

| Claimed | Verdict |
| --- | --- |
| `if (_active == null) return false;` gives up before it looks at focus | **Real, and the whole cause.** |
| `document.rootVisualElement.focusController` is the *shell's* controller and cannot see the pause menu's field | **Wrong.** |

`_active` is the shell's own open drawer. `Rebuild()` collects only elements carrying the `panel` class in the
shell's document, and `PauseMenuController` owns its own `UIDocument` and never registers there — so with the save
dialog up the shell had no drawer open, answered "nobody is typing" before looking at anything, and B opened the
build menu underneath the player's save name.

The second claim is wrong because **both documents point at one `PanelSettings` asset**
(`Relight/UI/PanelSettings.asset`, guid `35808e5a…`, referenced by `GameUI.unity` at `sortingOrder` 0 and 10, and by
`MainMenu.unity`). UIDocuments sharing a PanelSettings are siblings inside **one runtime panel**, so they share one
`FocusController`. The shell's root really does resolve the controller the pause menu's field is focused in.

This is not a detail: had the second claim been true the fix would have been a cross-document focus search on every
frame. The test states the dependency instead — `Is.SameAs` on the two focus controllers — so if someone later gives
a screen its own PanelSettings, the test names the reason rather than the symptom.

## What changed

```
- if (_active == null) return false;
- var focused = document?.rootVisualElement?.focusController?.focusedElement as VisualElement;
- return focused is TextField || focused?.GetFirstAncestorOfType<TextField>() != null || …
+ var focused = document?.rootVisualElement?.focusController?.focusedElement as VisualElement;
+ var field = TextEntry(focused);              // the field, or the field the inner text element sits in
+ return field != null && OnScreen(field);     // display and visibility, all the way up
```

### The visibility walk is not tidying

Without it this commit would have traded one bug for a worse one. The pause menu is dismissed by switching a parent
to `display: none` over a field that **still holds focus** (`PauseMenuController.ShowOverlay` toggles the `hidden`
class; `PauseMenu.uss:24` is `.pause-menu.hidden { display: none; }`). UI Toolkit does not reliably blur an element
switched off that way, so a guard trusting focus alone would have left B dead for the rest of the session after one
save name was typed. A field nobody can see is not a field anybody is typing into.

The second half of the new test is exactly that case, and it dismisses the dialog the way Resume does — by
unpausing, with the field never blurred.

## Three keys, not one

The owner named B. The same method gates Tab here, and is published as `WorldInput.UiTextInputFocused`, which gates
the **digit** slot keys and **R** (`WorldInput.cs:255-256`). All four were open in the same way and all four close
together; nothing else reads it.

## What this did *not* fix, and why

The issue listed two more screens. Checked, and they need no change:

* **`SettingsController._key`, the key-binding capture** — it is a `TextField` (`SettingsController.cs:82`,
  read-only, capturing `KeyDownEvent`), it lives in `GameUI.unity` beside the shell, and it is focused while
  capturing. It is **fixed by this commit**, not separately.
* **`TitleScreenController._seedField`** — also a `TextField`, but `MainMenu.unity` is loaded `Single` and holds no
  `UiShell` and no `WorldInput`. There are no game hotkeys in that scene to fire. The acceptance criterion is
  satisfied by construction, not by this change, and it is listed here so nobody reads a fix into it.

## Checks

| Suite | Result | Before |
| --- | --- | --- |
| Unity EditMode | **1040 / 1040** (124 s) | 1040 / 1040 |
| Unity PlayMode | **98 total — 93 passed, 0 failed, 5 skipped** (279 s) | 97 total — 89 passed, 3 failed, 5 skipped |

The offline sim suite was **not run**: nothing in `Sim` changed, and `UiShell` is not in that project. PlayMode was
run in full rather than filtered, because this method gates every single-key hotkey in the game.

### The three known PlayMode failures now pass — cause unattributed

`DroppedCargoPlayTests` (REL-125), `OpeningUiPlayTests` U6 and `EscapeCancelsRepairPlayTests` all pass. **This
commit is not claimed as the fix.** The last full PlayMode measurement was taken at `d3827d18`; REL-14, REL-62,
REL-66, REL-126, REL-128 and REL-129 have landed since, any of which could be the cause, and no bisect was run.

REL-125 was checked separately because its title is "fails when run on its own": run alone here, it **passed**
(1/1). One green run does not retire an intermittent failure, so REL-125 stays open with this measurement recorded
on it rather than closed.

The five skips are all declared and environmental: the long timing form, a save with nothing to merge, and three
prototype tests needing `R7-Tilemap.unity`, which is deliberately not in the repository.

### The new test bites

A passing test proves nothing on its own, so the old guard was put back for one run:

```
Status: Failed
  B typed into a save name opened the build menu underneath it.
  Expected: null
  But was:  "build-panel"
```

That is the owner's note, reproduced in a test. With the fix in place the same test passes (10.0 s). The temporary
line was removed and the editor recompiled clean before the full run.

### One test added

`ControlsCorrectionTests.TypingASaveNameLeavesTheHotkeysAloneAndHandsThemBackAfterwards` — pauses, clicks **Save**
through a real submit event, focuses the real `save-name` field, presses **B** on a real virtual keyboard and
asserts no panel opened; then unpauses, leaving the field focused but hidden, and asserts B opens the build menu
again. It also asserts the one-panel dependency the guard rests on. Driven through devices and the authored scenes,
because a test that focused a field in the shell's *own* drawer would have passed throughout the bug's whole life.

## Files

* `Unity/Relight/Assets/Relight/UI/UiShell.cs` — `TextInputFocused` rewritten, `TextEntry` and `OnScreen` added.
* `Unity/Relight/Assets/Relight/Tests/Play/Input/ControlsCorrectionTests.cs` — one test added.
* **Unchanged, deliberately:** `SettingsController.cs`, `TitleScreenController.cs`, `PauseMenuController.cs`,
  `WorldInput.cs`, and every UXML/USS file.
