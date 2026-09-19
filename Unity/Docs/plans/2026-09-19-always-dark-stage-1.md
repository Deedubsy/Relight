# Always Dark — Stage 1 (L-01) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Unity port permanently dark and readable: no day and night cycle, a twilight overlay with a brightness setting, lights that shrink in a brownout, alien silhouettes, player feedback drawn above the darkness, an elapsed-time HUD clock and an in-light HUD cue.

**Architecture:** The sim stays the only authority on what is lit. A new port-owned data layer sets `DaylightSeconds` to 0 and `LightQueries.Daylight` treats 0 as "no sun". Light radius follows circuit throttle inside `LightSources.Collect`, so the mask, the picture and later alien rules all read one value. Everything else is presentation: overlay strength, draw order, silhouettes and two HUD strings.

**Tech Stack:** Unity 6000.6.0f1, C# 9, NUnit (EditMode assembly `Relight.Sim.Tests`), UI Toolkit (UXML/USS), Unity CLI 1.0.0-beta.8 driving the owner's open editor.

**Spec:** `Unity/Docs/ALWAYS_DARK_SPEC.md` (§3, §4, §7 stage 1). Decision U-D-58. Task row L-01 in `Unity/Docs/TASKS.md`.

## Global Constraints

- The sim alone decides what is lit. Presentation never computes light and nothing in presentation is read back into the sim (spec §2, §3).
- The flashlight never counts as light (D-UI-11). Do not add it to the mask.
- Light never changes alien hit points, speed or damage.
- The Home core lot stays always lit and does not shrink in a brownout (spec §3, §4).
- Overlay strength on unlit tiles: provisional **0.55**. Brightness setting limits: **0.35 to 0.70**.
- Brownout rule, verbatim from the spec: a light's radius is its full value multiplied by `0.5 + 0.5 × throttle`; off with no supply.
- `Sim/Data/Generated/CatalogueData.g.cs` is generated from the reference. Never edit it. Port-owned values go in a layer beside `OpeningBalance` and `CombatBalance`.
- Never hand-edit `.unity`, `.prefab` or `.asset` files while the editor is reachable.
- No save-format change in this stage.
- Work on branch `unity-always-dark`. End every commit message with `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`. Do not push.
- All paths below are relative to `Unity/Relight/Assets/Relight/` unless they start with `Unity/`.

## Running checks

The owner's editor must be open on `Unity/Relight`. From WSL:

```bash
U=/mnt/c/Users/dw/AppData/Local/Unity/bin/unity.exe
$U status --format json --no-banner                      # expect state "ready"
$U command recompile --format json --no-banner           # after adding or editing .cs files
$U command console_status --format json --no-banner      # expect "compilationFailed": false, "consoleErrors": 0
# run EditMode tests whose full name contains FILTER, then poll until status is "completed"
$U command run_tests --mode editor --filter FILTER --filter_type testName --async_tests true --format json --no-banner
$U command test_status --format json --no-banner | python3 -c "import json,sys;r=json.load(sys.stdin)['data']['result'];print(r['status'],r['summary']);[print(x['FullName'],x['Message']) for x in r['results'] if x['Status']!='Passed']"
```

A new `.cs` file gets its `.meta` when the editor imports it (the `recompile` command triggers this). Commit the `.meta` with the file.

If no editor is connected, use the scratch `dotnet test` project described in `Unity/Docs/evidence/substation-2026-09-19/checks.md` §1 and say so in the evidence.

## File map

| File | Responsibility | Task |
|---|---|---|
| `Sim/Data/DarkWorld.cs` (create) | Port-owned data layer: no sun | 1 |
| `Sim/Data/ReferenceData.cs`, `Data/GameDataRegistry.cs` | Apply the layer in both data paths | 1 |
| `Sim/Campaign/Light/LightQueries.cs` | `DaylightSeconds` 0 means constant dark | 1 |
| `Sim/Campaign/Light/LightRules.cs`, `LightSources.cs` | Brownout scale on every powered light | 2 |
| `Sim/UI/DarknessLook.cs` (create) | Pure overlay-strength function | 3 |
| `Presentation/Light/LightingPresenter.cs` | Twilight strength, brightness, beam query | 3, 4 |
| `UI/Settings/Preferences.cs`, `SettingsController.cs`, `SettingsPanel.uxml`, `Sim/UI/FrontEndText.cs` | Brightness setting | 3 |
| `Presentation/DrawOrder.cs` (create) | One place for sorting orders around the darkness | 4 |
| `Presentation/Combat/EnemyPresenter.cs` | Silhouettes and full-strength tells | 4 |
| `Presentation/Building/PlacementPreviewPresenter.cs`, `Flow/PowerConnectionPresenter.cs`, `Flow/MachineActivityVisual.cs`, `EngineerView.cs` | Drawn above the darkness | 4 |
| `Sim/UI/Hud/HudViewModel.cs`, `UI/StatusPanelViewModel.cs`, `UI/Hud/Hud.uxml`, `UI/Hud/HudController.cs` | Elapsed clock, in-light cue | 5 |
| `Sim/Campaign/Opening/OpeningQueries.cs` | Substation step text | 6 |
| `Unity/Docs/TASKS.md`, `Unity/Docs/evidence/always-dark-stage-1/` | Status and evidence | 7 |

---

### Task 1: The world has no sun

**Files:**
- Create: `Sim/Data/DarkWorld.cs`
- Modify: `Sim/Data/ReferenceData.cs:17`, `Data/GameDataRegistry.cs:85`, `Sim/Campaign/Light/LightQueries.cs:85-110`
- Test: `Tests/Sim/Campaign/Light/LightTests.cs:215-240`

**Interfaces:**
- Produces: `DarkWorld.Apply(GameData) : GameData`; `ctx.Data.Time.DaylightSeconds == 0` in every context built by `ReferenceData.Create()` or `GameDataRegistry.Build()`; `LightQueries.Daylight(ctx, st)` returns `IsDay == false`, `Daylight == 0` at every `st.T` unless `st.Admin.Lighting >= 0`.

- [ ] **Step 1: Replace the data-bound daylight test and add the always-dark test**

In `Tests/Sim/Campaign/Light/LightTests.cs`, replace the whole `DaylightBoundariesFollowTheDataRecord` test (it starts at the `[Test]` above line 218 and ends before `DaylightIsAlwaysDayWhenTheRecordIsEmpty`) with these two tests. Keep every assertion of the old test that came after the two `Assert.That(day/light, Is.EqualTo(...))` lines; they are reproduced here with literal arguments.

```csharp
        [Test]
        public void TheDaylightCurveStillAnswersForARecordWithASun()
        {
            // The pure function is kept for the admin override and for any data set that has a sun.
            const double day = 1200, light = 900;

            Assert.That(LightQueries.Daylight(0, day, light).IsDay, Is.True);
            Assert.That(LightQueries.Daylight(899.9, day, light).IsDay, Is.True);
            Assert.That(LightQueries.Daylight(900, day, light).IsDay, Is.False, "daylightSeconds is the first dark instant");
            Assert.That(LightQueries.Daylight(1199.9, day, light).IsDay, Is.False);
            Assert.That(LightQueries.Daylight(1200, day, light).IsDay, Is.True, "dawn of day 2");

            Assert.That(LightQueries.Daylight(0, day, light).Day, Is.EqualTo(1));
            Assert.That(LightQueries.Daylight(1200, day, light).Day, Is.EqualTo(2));
            Assert.That(LightQueries.Daylight(1200, day, light).TimeOfDay, Is.EqualTo(0).Within(1e-9));

            Assert.That(LightQueries.Daylight(600, day, light).Daylight, Is.EqualTo(1).Within(1e-9));
            Assert.That(LightQueries.Daylight(1050, day, light).Daylight, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void TheWorldIsAlwaysDark()
        {
            // U-D-58: the port's data has no sun, and zero daylight seconds means dark at every moment —
            // not the V-shaped curve the old formula would have made of a zero.
            var ctx = Ctx();
            var st = Fresh(ctx);
            Assert.That(ctx.Data.Time.DaylightSeconds, Is.Zero);

            foreach (var t in new[] { 0.0, 1.0, 600.0, 900.0, 1050.0, 1199.9, 1200.0, 5000.0 })
            {
                st.T = t;
                var v = LightQueries.Daylight(ctx, st);
                Assert.That(v.IsDay, Is.False, "t=" + t);
                Assert.That(v.Daylight, Is.Zero, "t=" + t);
            }

            st.Admin.Lighting = 1;
            Assert.That(LightQueries.Daylight(ctx, st).IsDay, Is.True, "the admin override still forces daylight");
        }
```

- [ ] **Step 2: Run the two tests and confirm the new one fails**

Run with `FILTER=LightTests`. Expected: `TheWorldIsAlwaysDark` FAILS on `Expected: 0 But was: 900`; `TheDaylightCurveStillAnswersForARecordWithASun` passes.

- [ ] **Step 3: Create the data layer**

`Sim/Data/DarkWorld.cs`:

```csharp
namespace Relight.Sim
{
    /// <summary>
    /// U-D-58: the port is always dark. <c>Sim/Data/Generated/CatalogueData.g.cs</c> is generated from the
    /// TypeScript reference, which has a sun (<c>daylightSeconds</c> 900), so the port's own value lives in this
    /// layer, applied after <see cref="CombatBalance"/> in both data paths (<see cref="ReferenceData.Create"/> and
    /// the Unity <c>GameDataRegistry.Build</c>). Zero daylight seconds means there is no sun:
    /// <see cref="LightQueries.Daylight(double,double,double)"/> answers dark at every time.
    /// Rules: Unity/Docs/ALWAYS_DARK_SPEC.md.
    /// </summary>
    public static class DarkWorld
    {
        public static GameData Apply(GameData d)
        {
            if (d == null || d.Time == null || d.Time.DaylightSeconds == 0) return d;
            return new GameData(d.Items, d.Machines, d.Recipes, d.Engineer, d.World, d.Weapons, d.Enemies,
                d.Ammunition, d.Turrets, d.Power, d.Time with { DaylightSeconds = 0 }, d.Raids, d.Opening,
                d.Stake, d.Defence, d.Siege);
        }
    }
}
```

- [ ] **Step 4: Apply it in both data paths**

`Sim/Data/ReferenceData.cs:17` becomes:

```csharp
        public static GameData Create() => DarkWorld.Apply(CombatBalance.Apply(OpeningBalance.Apply(CatalogueData.Build())));
```

`Data/GameDataRegistry.cs:85` becomes:

```csharp
        public GameData Build() => DarkWorld.Apply(CombatBalance.Apply(OpeningBalance.Apply(BuildOriginal())));
```

- [ ] **Step 5: Teach `Daylight` that zero means no sun**

In `Sim/Campaign/Light/LightQueries.cs`, inside `Daylight(double t, double daySeconds, double daylightSeconds)`, insert the marked block directly after the `tod` clamp and before `var isDay = ...`:

```csharp
            var day = (int)Math.Floor(t / daySeconds) + 1;
            var tod = t - (day - 1) * daySeconds;
            if (tod < 0) tod = 0;

            // U-D-58: no daylight seconds means no sun. Without this case the formula below would spread one long
            // dusk-and-dawn V across the whole day.
            if (daylightSeconds <= 0) return new DaylightView(tod, day, false, 0);

            var isDay = tod < daylightSeconds;
```

Also change the summary on `Daylight(SimContext, SimState)` from `Day/night now, from GameData.Time (daySeconds 1200, daylightSeconds 900).` to `Day/night now, from GameData.Time. The port's data has daylightSeconds 0 (U-D-58), so this is dark unless the admin override forces otherwise.`

- [ ] **Step 6: Run `LightTests`, then the whole assembly**

`FILTER=LightTests`: all pass. Then `--filter Relight.Sim.Tests --filter_type assembly`: expected 651 passed, 0 failed (650 before, one test replaced by two).

- [ ] **Step 7: Commit**

```bash
git add Unity/Relight/Assets/Relight/Sim/Data/DarkWorld.cs Unity/Relight/Assets/Relight/Sim/Data/DarkWorld.cs.meta \
  Unity/Relight/Assets/Relight/Sim/Data/ReferenceData.cs Unity/Relight/Assets/Relight/Data/GameDataRegistry.cs \
  Unity/Relight/Assets/Relight/Sim/Campaign/Light/LightQueries.cs Unity/Relight/Assets/Relight/Tests/Sim/Campaign/Light/LightTests.cs
git commit -m "Always dark L-01: the data has no sun and Daylight answers dark (U-D-58)"
```

---

### Task 2: Brownouts shrink lights

**Files:**
- Modify: `Sim/Campaign/Light/LightRules.cs` (add one method), `Sim/Campaign/Light/LightSources.cs:41-73`
- Test: `Tests/Sim/Campaign/Light/LightTests.cs`

**Interfaces:**
- Produces: `LightRules.BrownoutScale(double throttle) : double`. `Light.R` from `LightSources.Collect` is already scaled. `Light.Lit` is unchanged (`throttle > 0`).

- [ ] **Step 1: Write the failing tests**

Add to `LightTests.cs` in the street-lights region:

```csharp
        [TestCase(1.0, 1.0)]
        [TestCase(0.5, 0.75)]
        [TestCase(0.0, 0.5)]
        [TestCase(1.7, 1.0)]
        [TestCase(-3.0, 0.5)]
        public void BrownoutScaleIsHalfPlusHalfTheThrottle(double throttle, double expected)
        {
            Assert.That(LightRules.BrownoutScale(throttle), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void ALampShrinksWhenItsCircuitIsOverloaded()
        {
            var ctx = Ctx();
            var st = Fresh(ctx);
            var lamp = RaidFixture.Add(ctx, st, "lamp", 30, 30);   // radius 4, 5 kW
            RaidFixture.Power(ctx, st, 32, 30);                // pole (32,30), 300 kW generator (34,30)
            RaidFixture.Run(ctx, st, 1, Phases());
            Assert.That(LightQueries.LitAt(st, 34, 30), Is.True, "full power: 4 tiles out is lit");

            // Six 100 kW assemblers inside the pole's reach: demand 605 kW on 300 kW, throttle ~0.496,
            // scale ~0.748, radius ~2.99.
            foreach (var (x, y) in new[] { (28, 33), (32, 33), (36, 33), (28, 37), (32, 37), (36, 37) })
                RaidFixture.Add(ctx, st, "assembler", x, y);
            RaidFixture.Run(ctx, st, 1, Phases());

            var throttle = PowerQueries.Throttle(ctx, st, lamp.Id);
            Assert.That(throttle, Is.GreaterThan(0).And.LessThan(0.6), "the fixture must actually brown out");
            Assert.That(LightQueries.LitAt(st, 34, 30), Is.False, "4 tiles out went dark");
            Assert.That(LightQueries.LitAt(st, 32, 30), Is.True, "2 tiles out is still lit");
        }
```

An idle Assembler still draws its 100 kW (`PowerGrid.DemandKw` reads the spec, not the recipe), and `RaidFixture.Add` does not check overlaps, so the six placements are valid as written.

- [ ] **Step 2: Run and confirm failure**

`FILTER=LightTests`. Expected: the five `BrownoutScale` cases fail to compile (`LightRules` has no `BrownoutScale`). That compile failure is the failing state.

- [ ] **Step 3: Add the rule**

In `Sim/Campaign/Light/LightRules.cs`, after the `ConeOriginGlowD2` constant:

```csharp
        /// <summary>
        /// U-D-58, ALWAYS_DARK_SPEC.md §3: a powered light's reach follows the power it actually gets — full at
        /// full throttle, half at the edge of failure. Off is a separate fact (<see cref="Light.Lit"/>).
        /// </summary>
        public static double BrownoutScale(double throttle)
        {
            if (double.IsNaN(throttle)) return 0.5;
            var t = throttle < 0 ? 0 : throttle > 1 ? 1 : throttle;
            return 0.5 + 0.5 * t;
        }
```

- [ ] **Step 4: Scale every powered light in `LightSources.Collect`**

Streetlights — replace the loop body:

```csharp
                    var s = sites[i];
                    var c = grid.OfSite(s.Id);
                    var throttle = c != null ? c.Throttle : 0;
                    var on = throttle > 0;                     // flow.ts:620 subPowered
                    into.Add(new Light(s.X, s.Y, r * LightRules.BrownoutScale(throttle), LightKind.StreetLight, on));
```

Machines — replace the two `into.Add` calls:

```csharp
                var throttle = PowerQueries.Throttle(ctx, st, m.Id);
                var scale = LightRules.BrownoutScale(throttle);
                if (spec.LightRadiusTiles > 0)
                {
                    // flow.ts:1812 — a Lamp or Arc lamp sits on its tile index and lights a disc while it is running.
                    into.Add(new Light(m.X, m.Y, spec.LightRadiusTiles * scale, LightKind.Lamp,
                        throttle > 0, m.Dir, 0, m.Id));
                }
                else if (spec.ConeRangeTiles > 0)
                {
                    // flow.ts:1814 — a Floodlight throws its cone from the footprint centre along its facing.
                    var (w, h) = m.Dimensions;
                    into.Add(new Light(m.X + w / 2.0, m.Y + h / 2.0, spec.ConeRangeTiles * scale, LightKind.Floodlight,
                        throttle > 0, m.Dir, spec.ConeHalfAngleRad, m.Id));
                }
```

Move the two new `var` lines inside the `if (!d.TryMachine(...)) continue;` guard so they run only for a known kind, and only when `spec.LightRadiusTiles > 0 || spec.ConeRangeTiles > 0` (wrap them in that condition) so ordinary machines cost no throttle lookup. `PowerQueries.Supplied` is `Throttle > 0`, so `Lit` is unchanged. `LightSources.Fold` already hashes `R`, so the mask rebuilds when a radius changes.

Add one bullet to the class summary's difference list: `A powered light's radius is scaled by LightRules.BrownoutScale of its circuit's throttle (U-D-58); the reference lights at full radius whenever the throttle is above zero.`

- [ ] **Step 5: Run `LightTests`, then the whole assembly**

Expected: all pass. `AnAuthoredStreetLightDrawsItsKwAndLightsItsRadiusWhenTheCircuitIsUp` still passes because its circuit is at full throttle.

- [ ] **Step 6: Commit**

```bash
git add Unity/Relight/Assets/Relight/Sim/Campaign/Light Unity/Relight/Assets/Relight/Tests/Sim/Campaign/Light/LightTests.cs
git commit -m "Always dark L-01: brownouts shrink every powered light"
```

---

### Task 3: Twilight strength and the brightness setting

**Files:**
- Create: `Sim/UI/DarknessLook.cs`, `Tests/Sim/UI/DarknessLookTests.cs`
- Modify: `Presentation/Light/LightingPresenter.cs`, `UI/Settings/Preferences.cs`, `UI/Settings/SettingsController.cs`, `UI/Settings/SettingsPanel.uxml`, `Sim/UI/FrontEndText.cs`

**Interfaces:**
- Produces: `DarknessLook.Strength(double unlit, double brightness) : double`; `DarknessLook.DefaultUnlit = 0.55`, `MinStrength = 0.35`, `MaxStrength = 0.70`, `DefaultBrightness = 0.5`; `LightingPresenter.BrightnessKey = "relight.video.brightness"`; `LightingPresenter.SetBrightness(float)`; `Preferences.Brightness`.
- The flashlight needs no change: `MouseFlashlightPresenter` turns the beam off only while `IsDay`, which is now false except under the admin "force daylight" override, where hiding the beam is correct.

- [ ] **Step 1: Write the failing test**

`Tests/Sim/UI/DarknessLookTests.cs`:

```csharp
using NUnit.Framework;
using Relight.Sim.UI;

namespace Relight.Sim.Tests.UI
{
    /// <summary>ALWAYS_DARK_SPEC.md §3: unlit ground is a readable twilight, and brightness stays inside safe limits.</summary>
    public sealed class DarknessLookTests
    {
        [Test]
        public void TheDefaultIsTheSpecsTwilight()
        {
            Assert.That(DarknessLook.Strength(DarknessLook.DefaultUnlit, DarknessLook.DefaultBrightness),
                Is.EqualTo(0.55).Within(1e-9));
        }

        [TestCase(0.0, 0.70)]
        [TestCase(1.0, 0.375)]
        [TestCase(-5.0, 0.70)]
        [TestCase(9.0, 0.375)]
        public void BrightnessMovesTheStrengthInsideTheLimits(double brightness, double expected)
        {
            Assert.That(DarknessLook.Strength(0.55, brightness), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void AnAuthoredStrengthOutsideTheLimitsIsClamped()
        {
            Assert.That(DarknessLook.Strength(0.95, 0.5), Is.EqualTo(DarknessLook.MaxStrength).Within(1e-9));
            Assert.That(DarknessLook.Strength(0.05, 0.5), Is.EqualTo(DarknessLook.MinStrength).Within(1e-9));
        }
    }
}
```

Check the namespace of the existing `Sim/UI/Hud/HudViewModel.cs` (`Relight.Sim.UI`) and the test namespace used by `Tests/Sim/UI/HudViewModelTests.cs`; match both.

- [ ] **Step 2: Run and confirm it fails to compile** (`DarknessLook` does not exist).

- [ ] **Step 3: Create the pure function**

`Sim/UI/DarknessLook.cs`:

```csharp
namespace Relight.Sim.UI
{
    /// <summary>
    /// How dark unlit ground is DRAWN (U-D-58, ALWAYS_DARK_SPEC.md §3). Picture only: the lit mask, and therefore
    /// every rule, is untouched by any value here. Dark is a rule, not a black screen, so the strength is held
    /// inside limits whatever the scene or the player's setting asks for.
    /// </summary>
    public static class DarknessLook
    {
        /// <summary>Overlay alpha on unlit tiles. Provisional; the owner tunes it by eye.</summary>
        public const double DefaultUnlit = 0.55;
        public const double MinStrength = 0.35;
        public const double MaxStrength = 0.70;
        /// <summary>The settings slider's middle, which leaves the authored strength alone.</summary>
        public const double DefaultBrightness = 0.5;
        /// <summary>How far the slider can move the strength in total, end to end.</summary>
        public const double BrightnessSpan = 0.35;

        public static double Strength(double unlit, double brightness)
        {
            if (double.IsNaN(unlit)) unlit = DefaultUnlit;
            if (double.IsNaN(brightness)) brightness = DefaultBrightness;
            var b = brightness < 0 ? 0 : brightness > 1 ? 1 : brightness;
            var s = unlit + (DefaultBrightness - b) * BrightnessSpan;
            return s < MinStrength ? MinStrength : s > MaxStrength ? MaxStrength : s;
        }
    }
}
```

Check: `Strength(0.55, 0) = 0.55 + 0.175 = 0.725 → 0.70`; `Strength(0.55, 1) = 0.55 − 0.175 = 0.375`.

- [ ] **Step 4: Run `DarknessLookTests`** — all pass.

- [ ] **Step 5: Use it in the presenter**

In `Presentation/Light/LightingPresenter.cs`:

1. Replace the `nightDarkness` field. The scene serialises `nightDarkness: 0.86` on the component in `World.unity`; a new field name makes Unity drop that stale value and use the new default, with no scene edit:

```csharp
        [Tooltip("How dark unlit ground is drawn, 0 clear to 1 opaque. U-D-58: a readable twilight, not black. " +
                 "DarknessLook holds it inside 0.35-0.70 and the player's brightness setting moves it.")]
        [SerializeField, Range(0f, 1f)] private float unlitDarkness = (float)Relight.Sim.UI.DarknessLook.DefaultUnlit;
```

2. Add below the fields:

```csharp
        /// <summary>PlayerPrefs key of the brightness setting. The settings screen writes it (Preferences.Brightness).</summary>
        public const string BrightnessKey = "relight.video.brightness";

        private float _brightness = (float)Relight.Sim.UI.DarknessLook.DefaultBrightness;

        /// <summary>The overlay strength on unlit tiles after limits and brightness. Read-only readback.</summary>
        public float UnlitStrength =>
            (float)Relight.Sim.UI.DarknessLook.Strength(unlitDarkness, _brightness);

        /// <summary>Live change from the settings screen. Picture only.</summary>
        public void SetBrightness(float value) => _brightness = Mathf.Clamp01(value);
```

3. At the end of `Awake()` add:

```csharp
            _brightness = PlayerPrefs.GetFloat(BrightnessKey, (float)Relight.Sim.UI.DarknessLook.DefaultBrightness);
```

4. In `LateUpdate()` replace `var darkness = Mathf.Lerp(nightDarkness, dayDarkness, Daylight);` with:

```csharp
            var darkness = Mathf.Lerp(UnlitStrength, dayDarkness, Daylight);
```

5. Update the class summary's first paragraph: replace "the day/night strength comes from `LightQueries.Daylight`…so dusk does not step" with a sentence saying the world is always dark (U-D-58), `Daylight` is 0 unless the admin override forces daylight, and the strength is `DarknessLook.Strength`.

- [ ] **Step 6: Add the setting**

`Sim/UI/FrontEndText.cs` — beside `MasterVolume` add:

```csharp
        public const string DisplayHeading = "Display";
        public const string Brightness = "Brightness in the dark";
```

`UI/Settings/Preferences.cs` — beside the other keys and properties add:

```csharp
        public const string KeyBrightness = Prefix + "video.brightness";
```
```csharp
        /// <summary>0 darkest to 1 brightest; 0.5 leaves the authored twilight alone. Picture only (U-D-58).</summary>
        public static float Brightness { get => GetVolume(KeyBrightness, 0.5f); set => SetVolume(KeyBrightness, value); }
```

`KeyBrightness` must equal `LightingPresenter.BrightnessKey` (`"relight.video.brightness"`); `Prefix` is `"relight."`. `GetVolume`/`SetVolume` are the existing clamped 0–1 float accessors.

`UI/Settings/SettingsPanel.uxml` — insert a section between `section-interface` and `section-audio`, copying the row markup of the master-volume row:

```xml
            <ui:VisualElement name="section-display" class="settings-section">
                <ui:Label name="heading-display" text="Display" class="settings-heading" />

                <ui:VisualElement class="settings-row">
                    <ui:Label name="brightness-label" text="Brightness in the dark" class="settings-label" />
                    <ui:Slider name="brightness-slider" low-value="0" high-value="1" class="settings-control" />
                    <ui:Label name="brightness-value" text="" class="settings-value" />
                </ui:VisualElement>
            </ui:VisualElement>
```

`UI/Settings/SettingsController.cs`:

```csharp
        private Slider _brightness;
        private Label _brightnessValue;
```
In `Bind`, after the `_alertsValue` line:
```csharp
            _brightness = _root.Q<Slider>("brightness-slider");
            _brightnessValue = _root.Q<Label>("brightness-value");
```
Beside the other `Label(_root, ...)` calls:
```csharp
            Label(_root, "heading-display", FrontEndText.DisplayHeading);
            Label(_root, "brightness-label", FrontEndText.Brightness);
```
In `ReadIntoControls`, after the `_alerts` line:
```csharp
                SetSlider(_brightness, _brightnessValue, Preferences.Brightness);
```
In `Wire`, after the `_mute` line:
```csharp
            if (_brightness != null) _brightness.RegisterValueChangedCallback(e => OnBrightness(e.newValue));
```
New method beside `OnVolume`:
```csharp
        private void OnBrightness(float v)
        {
            if (_brightnessValue != null) _brightnessValue.text = FrontEndText.Percent(v);
            if (_quiet) return;
            Preferences.Brightness = v;
            // Live in a session; on the title screen there is no presenter and the saved value is read at Awake.
            var lighting = FindAnyObjectByType<Relight.Presentation.LightingPresenter>();
            if (lighting != null) lighting.SetBrightness(v);
            Preferences.Save();
            ShowStatus();
        }
```
Match how `OnVolume` ends (whether it calls `Preferences.Save()` and `ShowStatus()`); copy its ending exactly.

- [ ] **Step 7: Recompile and check the console** — `compilationFailed: false`, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add Unity/Relight/Assets/Relight/Sim/UI Unity/Relight/Assets/Relight/Tests/Sim/UI \
  Unity/Relight/Assets/Relight/Presentation/Light/LightingPresenter.cs Unity/Relight/Assets/Relight/UI/Settings
git commit -m "Always dark L-01: twilight overlay strength and a brightness setting"
```

---

### Task 4: Feedback above the darkness, and alien silhouettes

**Files:**
- Create: `Presentation/DrawOrder.cs`
- Modify: `Presentation/Light/LightingPresenter.cs`, `Presentation/Combat/EnemyPresenter.cs`, `Presentation/Building/PlacementPreviewPresenter.cs:154`, `Presentation/Flow/PowerConnectionPresenter.cs` (`Take`), `Presentation/Flow/MachineActivityVisual.cs:116,128`, `Presentation/EngineerView.cs:49`

**Interfaces:**
- Consumes: `LightQueries.LitAt(SimState, int, int)`.
- Produces: `DrawOrder` constants; `LightingPresenter.Reveal(Vec2 simPos) : float` (0 no beam, 1 fully revealed); `LightingPresenter.Dark : bool`; `EnemyPresenter.Silhouettes : int` (count drawn last frame).

- [ ] **Step 1: One place for the orders**

`Presentation/DrawOrder.cs`:

```csharp
namespace Relight.Presentation
{
    /// <summary>
    /// Sorting orders around the darkness overlay (U-D-58, ALWAYS_DARK_SPEC.md §3). World views sort by Z at order
    /// 0 and sit UNDER the darkness. Feedback about the player's own actions, and anything that must always be
    /// readable, sits ABOVE it. City labels are at 600 (CityPresenter).
    /// </summary>
    public static class DrawOrder
    {
        public const int Darkness = 500;
        public const int Cable = 510;
        public const int CablePulse = 511;
        public const int MachineCue = 512;
        public const int PlacementPreview = 520;
        public const int SilhouetteRim = 529;
        public const int Silhouette = 530;
        public const int AttackTell = 531;
        public const int Engineer = 540;
    }
}
```

- [ ] **Step 2: Move the feedback above the overlay**

- `LightingPresenter.cs`: `[SerializeField] private int sortingOrder = DrawOrder.Darkness;` (the scene serialises 500 already; the value is unchanged).
- `PlacementPreviewPresenter.cs:154`: `created.sortingOrder = DrawOrder.PlacementPreview;`
- `PowerConnectionPresenter.cs`, in `Take`: `Wire=Line("Power cable",9,DrawOrder.Cable,.045f)` and `Line("Live current",5,DrawOrder.CablePulse,.07f)`.
- `MachineActivityVisual.cs`: both default parameters `int order=11` become `int order=DrawOrder.MachineCue`. If any call site passes an explicit order, leave its relative offset: replace `N` with `DrawOrder.MachineCue + (N - 11)`.
- `EngineerView.cs`, after `_sprite = GetComponentInChildren<SpriteRenderer>();`: `if (_sprite != null) _sprite.sortingOrder = DrawOrder.Engineer;`

Belt items (`BeltItemPresenter`, 12), machine bodies, buildings and ground stay under the darkness on purpose.

- [ ] **Step 3: Let other presenters ask about the beam**

In `LightingPresenter.cs` add:

```csharp
        /// <summary>True while the darkness overlay is being drawn at all (false under forced daylight).</summary>
        public bool Dark => _sr != null && _sr.enabled;

        /// <summary>How much the flashlight reveals a sim position, 0 to 1. Picture only (D-UI-11).</summary>
        public float Reveal(Vec2 simPos) => _beam ? Beam(simPos.X, simPos.Y) : 0f;
```

- [ ] **Step 4: Draw silhouettes**

In `EnemyPresenter.cs`:

Fields:
```csharp
        [Tooltip("The darkness overlay. Found in the scene if left empty.")]
        [SerializeField] private LightingPresenter lighting;

        [Tooltip("U-D-58: an alien on unlit ground is a flat dark shape with a pale rim, drawn above the darkness.")]
        [SerializeField] private Color silhouetteColour = new Color(0.02f, 0.03f, 0.06f, 1f);
        [SerializeField] private Color rimColour = new Color(0.78f, 0.84f, 0.95f, 0.38f);
        [SerializeField, Range(1f, 1.6f)] private float rimScale = 1.22f;

        private readonly List<SpriteRenderer> _shades = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _rims = new List<SpriteRenderer>();

        /// <summary>Silhouettes drawn on the last frame. For tests.</summary>
        public int Silhouettes { get; private set; }
```
In `Awake`: `if (lighting == null) lighting = FindAnyObjectByType<LightingPresenter>();`

In `LateUpdate`, inside the body loop, directly after `sprite.color = ...;` and BEFORE `if (!drawTell || !winding) continue;`:

```csharp
                // U-D-58: on unlit ground the coloured body is under the darkness; a silhouette above it keeps the
                // alien readable at any overlay strength. Lit ground and the flashlight beam show the real body.
                var unlit = lighting != null && lighting.Dark &&
                            !LightQueries.LitAt(st, (int)System.Math.Floor(at.X), (int)System.Math.Floor(at.Y));
                var hidden = unlit ? 1f - lighting.Reveal(at) : 0f;
                var shade = Slot(_shades, i, "Silhouette", body);
                var rim = Slot(_rims, i, "Silhouette Rim", body);
                shade.enabled = rim.enabled = hidden > 0.01f;
                if (shade.enabled)
                {
                    shade.sortingOrder = DrawOrder.Silhouette;
                    rim.sortingOrder = DrawOrder.SilhouetteRim;
                    shade.transform.position = rim.transform.position = WorldSpace.World(at, z);
                    shade.transform.localScale = Scale(body, bodyTiles);
                    rim.transform.localScale = Scale(body, bodyTiles) * rimScale;
                    // A wind-up must read in the dark too: the silhouette itself flashes toward the wind-up tint.
                    var sc = winding ? Color.Lerp(silhouetteColour, windupTint, 0.6f) : silhouetteColour;
                    shade.color = new Color(sc.r, sc.g, sc.b, hidden);
                    rim.color = new Color(rimColour.r, rimColour.g, rimColour.b, rimColour.a * hidden);
                    silhouettes++;
                }
```
Declare `var silhouettes = 0;` beside `var tells = 0;`. After the loop, beside the line that disables unused `_bodies`:
```csharp
            for (var i = all.Count; i < _shades.Count; i++) if (_shades[i] != null) _shades[i].enabled = false;
            for (var i = all.Count; i < _rims.Count; i++) if (_rims[i] != null) _rims[i].enabled = false;
            Silhouettes = silhouettes;
```
`rim.transform.localScale = Scale(...) * rimScale` scales z as well; set z back to 1: `var rs = Scale(body, bodyTiles) * rimScale; rs.z = 1f; rim.transform.localScale = rs;`.

In `Tell(int i)` add `lr.sortingOrder = DrawOrder.AttackTell;` after `lr.material = _lineMaterial;`.

In `OnDisable` and `Clear`, disable `_shades` and `_rims` the same way `_bodies` are disabled and set `Silhouettes = 0`.

- [ ] **Step 5: Recompile, check the console, run the whole EditMode assembly** — no errors, all pass.

- [ ] **Step 6: See it in Play Mode**

```bash
$U command list_open_scenes --format json --no-banner     # World.unity open and isDirty false; if not, stop and ask
$U command editor_play --timeout 120 --format json --no-banner
```
Wait 8 s, then write `Unity/Relight/Temp/dark_probe.cs`:

```csharp
var h = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
h.Submit(new Relight.Sim.AdminCommand("spawn", "skitter", 3, 1, 12));
return "queued";
```
```bash
$U command eval_file --file "Temp/dark_probe.cs" --format json --no-banner
sleep 2
$U command eval --code 'var e=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.EnemyPresenter>(); var l=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.LightingPresenter>(); return "bodies="+e.Bodies+" silhouettes="+e.Silhouettes+" strength="+l.UnlitStrength+" daylight="+l.Daylight+" dark="+l.Dark;' --format json --no-banner
$U command screenshot --view game --output "Temp/dark_stage1_silhouettes.png" --format json --no-banner
$U command editor_stop --format json --no-banner
```
Expected: `bodies=3`, `silhouettes` between 1 and 3 (one may stand in the beam or on the lit lot), `strength=0.55`, `daylight=0`, `dark=True`. Open the screenshot: aliens on unlit ground read as dark shapes with a pale rim; the placement of cables and the engineer are at full colour. If `AdminCommand`'s direction or distance puts the spawn inside the lit core lot, raise the distance to 20.

Check the saves folder afterwards (`/mnt/c/Users/dw/AppData/LocalLow/DefaultCompany/Relight/saves`): no file newer than the start of the run. Autosave is every 5 minutes; keep the run under 3.

- [ ] **Step 7: Commit**

```bash
git add Unity/Relight/Assets/Relight/Presentation
git commit -m "Always dark L-01: player feedback draws above the darkness; aliens show a silhouette"
```

---

### Task 5: The HUD clock counts elapsed time, and says whether you are in light

**Files:**
- Modify: `Sim/UI/Hud/HudViewModel.cs:65,165,320-328`, `UI/StatusPanelViewModel.cs:30,74,104-105`, `UI/Hud/Hud.uxml:34-36`, `UI/Hud/HudController.cs:53,98,265`
- Test: `Tests/Sim/UI/HudViewModelTests.cs:64-67,267`, `Tests/Play/Scene/WorldSceneTests.cs:117`

**Interfaces:**
- Produces: `HudViewModel.FormatClock(double t, bool paused) : string` → `"0:00:00"`, `"1:24:10 · Paused"`; `HudViewModel.LightText` (`"In light"` / `"In the dark"`), `HudViewModel.InDark : bool`.

- [ ] **Step 1: Rewrite the clock tests and add the light-cue test**

`HudViewModelTests.cs` lines 64–67 become:

```csharp
            Assert.That(HudViewModel.FormatClock(0, false), Is.EqualTo("0:00:00"));
            Assert.That(HudViewModel.FormatClock(600, false), Is.EqualTo("0:10:00"));
            Assert.That(HudViewModel.FormatClock(5050.9, false), Is.EqualTo("1:24:10"));
            Assert.That(HudViewModel.FormatClock(600, true), Is.EqualTo("0:10:00 · Paused"));
            Assert.That(HudViewModel.FormatClock(-4, false), Is.EqualTo("0:00:00"));
```
Line 267 becomes `Assert.That(vm.Clock, Is.EqualTo("0:00:00 · Paused"));`

Add to the same fixture:

```csharp
        [Test]
        public void TheStripSaysWhetherTheEngineerStandsInLight()
        {
            var ctx = RaidFixture.Context();
            var st = RaidFixture.State(ctx);
            HomeCore.Ensure(ctx, st);
            LightPhase.Ensure(ctx, st);
            var vm = new HudViewModel();

            st.Engineer.Pos = new Vec2(RaidFixture.CoreX + 0.5, RaidFixture.CoreY + 0.5);   // on the always-lit core lot
            vm.Refresh(ctx, st, 0, paused: false, menuOpen: false, force: true);
            Assert.That(vm.InDark, Is.False);
            Assert.That(vm.LightText, Is.EqualTo("In light"));

            st.Engineer.Pos = new Vec2(10.5, 10.5);                                         // far from any light
            vm.Refresh(ctx, st, 1, paused: false, menuOpen: false, force: true);
            Assert.That(vm.InDark, Is.True);
            Assert.That(vm.LightText, Is.EqualTo("In the dark"));
        }
```

`Tests/Play/Scene/WorldSceneTests.cs:117` becomes:
```csharp
            Assert.That(status.Model.Clock, Does.StartWith("0:0"), "clock: " + status.Model.Clock);
```

- [ ] **Step 2: Run `HudViewModelTests`** — fails to compile (`FormatClock` has three parameters; `InDark` does not exist).

- [ ] **Step 3: Change the view model**

`HudViewModel.cs`:

```csharp
        /// <summary>Elapsed play time, "H:MM:SS" (U-D-58: there is no sun, so there are no days to count).</summary>
        public string Clock { get; private set; } = "0:00:00";

        /// <summary>"In light" or "In the dark", from the sim's lit mask at the engineer's tile (ALWAYS_DARK_SPEC.md §3).</summary>
        public string LightText { get; private set; } = "";
        public bool InDark { get; private set; }
```
In the no-session branch of `Refresh` add `LightText = ""; InDark = false;`.
Replace line 165 with:
```csharp
            Clock = FormatClock(st.T, paused);
            var at = WorldQueries.Engineer(ctx, st).Pos;
            InDark = !LightQueries.LitAt(st, (int)Math.Floor(at.X), (int)Math.Floor(at.Y));
            LightText = InDark ? "In the dark" : "In light";
```
Replace `FormatClock`:
```csharp
        public static string FormatClock(double t, bool paused)
        {
            if (double.IsNaN(t) || t < 0) t = 0;
            var s = (long)Math.Floor(t);
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}{3}",
                s / 3600, s / 60 % 60, s % 60, paused ? " · Paused" : "");
        }
```
Update its summary: drop the "Day N · HH:MM" description and the reference to day seconds.

`UI/StatusPanelViewModel.cs`: default `Clock` becomes `"0:00:00"`; line 74 becomes `Clock = FormatClock(st.T, host.Paused);`; the wrapper becomes
```csharp
        public static string FormatClock(double t, bool paused) =>
            Relight.Sim.UI.HudViewModel.FormatClock(t, paused);   // C-07: one clock, not two that can drift
```
and its summary lines 15 and 29 are updated to the elapsed format.

- [ ] **Step 4: Show the cue**

`UI/Hud/Hud.uxml` — default clock text `"0:00:00"`, and a second label in the same block:

```xml
                <ui:VisualElement name="clock-block" class="strip-block">
                    <ui:Label name="clock" text="0:00:00" class="strip-value" />
                    <ui:Label name="light-state" text="" class="strip-value" />
                </ui:VisualElement>
```
`UI/Hud/HudController.cs`: add `_light` to the `Label` field list on line 53; after line 98 `_light = _root.Q<Label>("light-state");`; after line 265:
```csharp
            Set(_light, _model.LightText);
            Toggle(_light, "is-danger", _model.InDark);
```
`is-danger` is the existing class the core label uses when disabled; reuse it so no new USS is needed.

- [ ] **Step 5: Run `HudViewModelTests`, then the whole EditMode assembly, then recompile and check the console.** All pass, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Unity/Relight/Assets/Relight/Sim/UI/Hud Unity/Relight/Assets/Relight/UI Unity/Relight/Assets/Relight/Tests
git commit -m "Always dark L-01: elapsed-time HUD clock and an in-light cue"
```

---

### Task 6: The substation step's text

**Files:**
- Modify: `Sim/Campaign/Opening/OpeningQueries.cs` (objective 8 detail text)
- Test: `Tests/Sim/Campaign/Opening/OpeningObjectiveTests.cs` (`TheSubstationRowCompletesOnlyWhenTheSitesOwnCircuitHasSupply`)

- [ ] **Step 1: Add the failing assertion** after the existing `Does.Not.Contain("no power")` line:

```csharp
            Assert.That(open.Detail, Does.Not.Contain("at night"), "U-D-58: the world is always dark");
            Assert.That(open.Detail, Does.EndWith("the court lights up."));
```

- [ ] **Step 2: Run `OpeningObjectiveTests`** — fails on "at night".

- [ ] **Step 3: Change the text.** In `OpeningQueries.cs` replace `"Chain Poles from your network to it and the court lights up at night."` with `"Chain Poles from your network to it and the court lights up."`

- [ ] **Step 4: Run `OpeningObjectiveTests`** — passes.

- [ ] **Step 5: Commit**

```bash
git add Unity/Relight/Assets/Relight/Sim/Campaign/Opening/OpeningQueries.cs Unity/Relight/Assets/Relight/Tests/Sim/Campaign/Opening/OpeningObjectiveTests.cs
git commit -m "Always dark L-01: the substation step no longer waits for night"
```

---

### Task 7: Verify in the editor, record the evidence, update the status

**Files:**
- Create: `Unity/Docs/evidence/always-dark-stage-1/checks.md` (+ screenshots)
- Modify: `Unity/Docs/TASKS.md` (L-01 row and §8 log), `Unity/Docs/ALWAYS_DARK_SPEC.md` (status line)

- [ ] **Step 1: Full EditMode run** — `--filter Relight.Sim.Tests --filter_type assembly`. Record the totals. Expected: 0 failed.

- [ ] **Step 2: Founders Court Verify** — `open_scene Assets/Relight/Scenes/World.unity` if needed, then `menu --path "Relight/Founders Court/Verify"`, and read `Unity/Relight/Temp/fc_verify_log.txt`. Expected: `21 passed, 0 failed of twenty-one checks`.

- [ ] **Step 3: Play Mode look** — enter Play Mode, wait 8 s, and capture three screenshots into `Unity/Docs/evidence/always-dark-stage-1/`:
  1. `spawn.png` — straight after start: the lit core lot, twilight around it, the flashlight cone, the HUD showing `0:00:..` and `In light`.
  2. `silhouettes.png` — after the spawn probe from Task 4 Step 6.
  3. `brightness.png` — after `eval` of `UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.LightingPresenter>().SetBrightness(0f); return "darkest";` to show the darkest setting still reads.
  Stop Play Mode. Confirm no save file was written.

- [ ] **Step 4: Write `checks.md`** with: the branch and commit, the EditMode totals, the Verify line, what each screenshot shows, and a plain "Not done" list: the owner has not yet tuned the overlay strength by eye; the opening has not been played by a person from spawn to the first Excavator; PlayMode test assembly not run (say so if it was not).

- [ ] **Step 5: Update the status**

- `TASKS.md` L-01 status: `done 2026-09-DD (engineering; owner acceptance pending: overlay strength to be tuned by eye, opening to be played in the dark)`. Use the real date.
- Add one §8 log line in the file's existing style: what changed, the check totals, what was not run, the evidence path.
- `ALWAYS_DARK_SPEC.md` status line: append `Stage 1 (L-01) implemented 2026-09-DD; see evidence/always-dark-stage-1/checks.md.`

- [ ] **Step 6: Commit**

```bash
git add Unity/Docs
git commit -m "Always dark L-01: evidence and status"
```

---

## Self-review against the spec

| Spec requirement (stage 1) | Task |
|---|---|
| `DaylightSeconds` 0 means no sun; admin override kept | 1 |
| Brownouts shrink lights; core lot does not shrink | 2 (the lot is stamped by `LightPhase`, not collected as a `Light`, so it is untouched) |
| Overlay strength 0.55, limits 0.35–0.70, brightness setting | 3 |
| Flashlight always on | 3 (no code change needed; reason recorded there) |
| Alien silhouettes; tells at full strength; whole screen | 4 |
| Feedback draws above the darkness | 4 |
| In-light HUD cue | 5 |
| Elapsed-time HUD clock | 5 |
| Substation step text | 6 |
| Document corrections (spec §9) | Done at approval, commit `9df3b157` |
| Play Mode check of the opening | 7, and the owner's own play under L-ACC |
| Light-coverage placement preview, walls block light, hesitation, relief events, perception, teaching the rule | Stage 2 (L-02), not in this plan |
| Map shows lit districts; tram stops | Later tasks, recorded in the spec |
