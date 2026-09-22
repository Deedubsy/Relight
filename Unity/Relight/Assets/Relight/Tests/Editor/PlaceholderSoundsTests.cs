using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Relight.Presentation;
using Relight.Sim;
using UnityEngine;

namespace Relight.Authoring.Tests
{
    /// <summary>
    /// REL-67 (U-D-69 (d), U-D-62, U-P-28): the rough sounds made in code, the approach cues and the levels the
    /// volume sliders set. What a test can say about a sound is that it exists, is audible, is well formed and is
    /// the same every time; whether it sounds right is the owner's listening pass.
    /// </summary>
    public sealed class PlaceholderSoundsTests
    {
        static string RouterSource => Path.Combine(Application.dataPath, "Relight", "Presentation", "Audio", "AudioCueRouter.cs");

        /// <summary>Every key the router raises, read from its own source so a new row is covered with no edit here.
        /// A key built from a kind (<c>"weapon.shot." + w.Kind</c>) is tested with a kind no row names.</summary>
        static IEnumerable<string> RoutedKeys()
        {
            var text = File.ReadAllText(RouterSource, Encoding.UTF8);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(text, "Cast\\(\\s*(?:t\\.Hit \\? |m\\.Cleared \\? |p\\.Stopped \\? )?\"([a-z0-9.\\-]+)\""))
                keys.Add(m.Groups[1].Value.EndsWith(".") ? m.Groups[1].Value + "someday-kind" : m.Groups[1].Value);
            foreach (Match m in Regex.Matches(text, ": \"([a-z0-9.\\-]+)\", new Vector2"))
                keys.Add(m.Groups[1].Value);
            return keys;
        }

        static IEnumerable<string> UiKeys() =>
            typeof(AudioCue.Ui).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue());

        /// <summary>The kinds the game fields: the catalogue plus the Breaker <c>CombatBalance</c> adds.</summary>
        static IEnumerable<string> EnemyKinds() => ReferenceData.Create().Enemies.Select(e => e.Key);

        [Test] public void TheRouterSourceYieldsItsKeys()
        {
            var keys = RoutedKeys().ToList();
            Assert.That(keys, Does.Contain("raid.warning"));
            Assert.That(keys, Does.Contain("impact.miss"), "the ternary arms are read too");
            Assert.That(keys, Does.Contain("mine.cleared"));
            Assert.That(keys.Count, Is.GreaterThan(20));
        }

        [Test] public void EveryRoutedAndInterfaceKeyHasAWellFormedAudibleSound()
        {
            var bad = new List<string>();
            foreach (var key in RoutedKeys().Concat(UiKeys()).Concat(EnemyKinds().Select(ApproachCues.Key)).Distinct())
            {
                var s = PlaceholderSounds.Samples(key);
                if (s == null) { bad.Add(key + ": none"); continue; }
                var seconds = s.Length / (float)PlaceholderSounds.SampleRate;
                var peak = s.Max(Mathf.Abs);
                if (s.Any(x => float.IsNaN(x) || float.IsInfinity(x))) bad.Add(key + ": not finite");
                else if (peak > 1f) bad.Add(key + ": clips at " + peak);
                else if (peak < 0.05f) bad.Add(key + ": near silent at " + peak);
                else if (seconds < 0.02f || seconds > 2f) bad.Add(key + ": " + seconds + " s");
                else if (Mathf.Abs(s[s.Length - 1]) > 0.02f) bad.Add(key + ": ends on a click");
            }
            Assert.That(bad, Is.Empty, string.Join("\n", bad));
        }

        [Test] public void TheSameKeyAlwaysMakesTheSameSound()
        {
            foreach (var key in new[] { "raid.warning", "weapon.shot.rifle", "enemy.approach.skitter", "structure.destroyed" })
                Assert.That(PlaceholderSounds.Samples(key), Is.EqualTo(PlaceholderSounds.Samples(key)), key);
        }

        [Test] public void EachAlienKindInTheGameHasItsOwnApproachSound()
        {
            var kinds = EnemyKinds().ToList();
            Assert.That(kinds, Is.EquivalentTo(new[] { "skitter", "spitter", "breaker" }),
                "a new kind needs its own approach row in PlaceholderSounds (U-D-62)");
            var own = PlaceholderSounds.Keys.ToList();
            foreach (var kind in kinds) Assert.That(own, Does.Contain(ApproachCues.Key(kind)), kind);

            var sounds = kinds.Select(k => PlaceholderSounds.Samples(ApproachCues.Key(k))).ToList();
            for (var i = 0; i < sounds.Count; i++)
            for (var j = i + 1; j < sounds.Count; j++)
                Assert.That(sounds[i], Is.Not.EqualTo(sounds[j]), kinds[i] + " vs " + kinds[j]);
        }

        [Test] public void AnUnknownKindStillGetsASoundAndAnUnknownKeyGetsNone()
        {
            Assert.That(PlaceholderSounds.Has("enemy.approach.lurker"), Is.True);
            Assert.That(PlaceholderSounds.Samples("enemy.approach.lurker"),
                Is.Not.EqualTo(PlaceholderSounds.Samples("enemy.approach.creeper")));
            Assert.That(PlaceholderSounds.Has("enemy.death.lurker"), Is.True, "family fallback");
            Assert.That(PlaceholderSounds.Has("nothing.at.all"), Is.False);
            Assert.That(PlaceholderSounds.Samples(""), Is.Null);
        }

        [Test] public void AClipIsBuiltOnceAndKept()
        {
            var a = PlaceholderSounds.Clip("ui.confirm");
            Assert.That(a, Is.Not.Null);
            Assert.That(a.frequency, Is.EqualTo(PlaceholderSounds.SampleRate));
            Assert.That(a.channels, Is.EqualTo(1));
            Assert.That(PlaceholderSounds.Clip("ui.confirm"), Is.SameAs(a));
            Assert.That(PlaceholderSounds.Clip("nothing.at.all"), Is.Null);
        }

        // ---- groups and levels -------------------------------------------------------------------------------

        [Test] public void EachKeyFallsInTheGroupItsSliderControls()
        {
            Assert.That(AudioCueRouter.GroupOf("ui.confirm"), Is.EqualTo(AudioLevels.UiGroup));
            Assert.That(AudioCueRouter.GroupOf("raid.warning"), Is.EqualTo(AudioLevels.AlertsGroup));
            Assert.That(AudioCueRouter.GroupOf("core.disabled"), Is.EqualTo(AudioLevels.AlertsGroup));
            Assert.That(AudioCueRouter.GroupOf("engineer.down"), Is.EqualTo(AudioLevels.AlertsGroup));
            Assert.That(AudioCueRouter.GroupOf("power.generator-dry"), Is.EqualTo(AudioLevels.AlertsGroup));
            Assert.That(AudioCueRouter.GroupOf("turret.shot"), Is.EqualTo(AudioLevels.WorldGroup));
            Assert.That(AudioCueRouter.GroupOf("enemy.approach.skitter"), Is.EqualTo(AudioLevels.WorldGroup));
        }

        [Test] public void EachSliderChangesOnlyItsOwnGroupAndMuteSilencesAll()
        {
            try
            {
                AudioLevels.Set(0.5f, 1f, 1f, 1f, false);
                Assert.That(AudioLevels.Gain(AudioLevels.UiGroup), Is.EqualTo(0.5f).Within(1e-6));
                AudioLevels.Set(0.5f, 0.2f, 1f, 1f, false);
                Assert.That(AudioLevels.Gain(AudioLevels.UiGroup), Is.EqualTo(0.1f).Within(1e-6));
                Assert.That(AudioLevels.Gain(AudioLevels.WorldGroup), Is.EqualTo(0.5f).Within(1e-6));
                Assert.That(AudioLevels.Gain(AudioLevels.AlertsGroup), Is.EqualTo(0.5f).Within(1e-6));
                AudioLevels.Set(0.5f, 0.2f, 1f, 1f, true);
                Assert.That(AudioLevels.Gain(AudioLevels.UiGroup), Is.EqualTo(0f));
                Assert.That(AudioLevels.Gain(AudioLevels.AlertsGroup), Is.EqualTo(0f));
                AudioLevels.Set(2f, -1f, 1f, 1f, false);
                Assert.That(AudioLevels.Master, Is.EqualTo(1f));
                Assert.That(AudioLevels.Ui, Is.EqualTo(0f));
            }
            finally { AudioLevels.Set(0.8f, 1f, 1f, 1f, false); }
        }

        [Test] public void TheRouterPlaysAPlaceholderAtTheSlidersLevelAndStaysSilentForAnUnknownKey()
        {
            var go = new GameObject("router under test");
            try
            {
                AudioLevels.Set(0.5f, 1f, 1f, 0.4f, false);
                var router = go.AddComponent<AudioCueRouter>();
                router.Play("nothing.at.all", null);
                Assert.That(router.Played, Is.EqualTo(0));
                router.Play("raid.warning", null);
                Assert.That(router.Played, Is.EqualTo(1));
                var src = go.GetComponentInChildren<AudioSource>();
                Assert.That(src, Is.Not.Null);
                Assert.That(src.clip, Is.SameAs(PlaceholderSounds.Clip("raid.warning")));
                Assert.That(src.volume, Is.EqualTo(0.2f).Within(1e-5), "master 0.5 x alerts 0.4");
                Assert.That(src.spatialBlend, Is.EqualTo(0f));

                router.Play("enemy.approach.breaker", new Vector2(30f, 5f));
                Assert.That(router.Played, Is.EqualTo(2));
                var positional = go.GetComponentsInChildren<AudioSource>().First(s => s.clip == PlaceholderSounds.Clip("enemy.approach.breaker"));
                Assert.That(positional.spatialBlend, Is.EqualTo(1f), "panned by where it is");
                Assert.That(positional.maxDistance, Is.GreaterThan((float)ApproachCues.HearingTiles));
                Assert.That(positional.volume, Is.EqualTo(0.5f).Within(1e-5), "master 0.5 x world 1");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                AudioLevels.Set(0.8f, 1f, 1f, 1f, false);
            }
        }

        // ---- approach cues -----------------------------------------------------------------------------------

        static Enemy Body(int id, string kind, double x, double y, int layer = EnemyLayer.Minor, bool withdrawing = false,
            bool onPlayer = false) =>
            new Enemy { Id = id, Kind = kind, Pos = new Vec2(x, y), Layer = layer, Withdrawing = withdrawing, OnPlayer = onPlayer };

        [Test] public void OneCuePerKindAtTheNearestComingBody()
        {
            var cues = new ApproachCues();
            var heard = new List<ApproachCues.Cue>();
            var bodies = new List<Enemy>
            {
                Body(1, "skitter", 30, 0), Body(2, "skitter", -20, 0), Body(3, "skitter", 40, 0),
                Body(4, "breaker", 0, 25, EnemyLayer.Major),
            };
            Assert.That(cues.Collect(bodies, new Vec2(0, 0), 10, heard), Is.EqualTo(2));
            var skitter = heard.Single(c => c.Kind == "skitter");
            Assert.That(skitter.Key, Is.EqualTo("enemy.approach.skitter"));
            Assert.That(skitter.At.X, Is.EqualTo(-20), "the nearest one, on the left");
            Assert.That(heard.Single(c => c.Kind == "breaker").At.Y, Is.EqualTo(25));
        }

        [Test] public void HeardBeyondTheScreenButNotBeyondHearing()
        {
            var cues = new ApproachCues();
            var heard = new List<ApproachCues.Cue>();
            cues.Collect(new[] { Body(1, "spitter", ApproachCues.HearingTiles - 1, 0) }, new Vec2(0, 0), 1, heard);
            Assert.That(heard.Count, Is.EqualTo(1), "30+ tiles away is off screen and still heard");
            heard.Clear();
            cues.Collect(new[] { Body(2, "skitter", ApproachCues.HearingTiles + 1, 0) }, new Vec2(0, 0), 1, heard);
            Assert.That(heard, Is.Empty);
        }

        [Test] public void AQuietCampAndARetreatMakeNoApproachSound()
        {
            var cues = new ApproachCues();
            var heard = new List<ApproachCues.Cue>();
            var bodies = new[]
            {
                Body(1, "skitter", 5, 0, EnemyLayer.Site),
                Body(2, "spitter", 5, 0, EnemyLayer.Minor, withdrawing: true),
                Body(3, "breaker", 5, 0, EnemyLayer.Site, withdrawing: true, onPlayer: true),
            };
            Assert.That(cues.Collect(bodies, new Vec2(0, 0), 1, heard), Is.EqualTo(0));
            Assert.That(cues.Collect(new[] { Body(4, "skitter", 5, 0, EnemyLayer.Site, onPlayer: true) }, new Vec2(0, 0), 2, heard),
                Is.EqualTo(1), "a resident chasing the engineer is coming");
        }

        [Test] public void EachKindKeepsItsOwnCadenceOnTheSimClockAndALoadStartsAgain()
        {
            var cues = new ApproachCues();
            var heard = new List<ApproachCues.Cue>();
            var bodies = new[] { Body(1, "skitter", 10, 0), Body(2, "spitter", 10, 0) };
            Assert.That(cues.Collect(bodies, new Vec2(0, 0), 100, heard), Is.EqualTo(2));
            Assert.That(cues.Collect(bodies, new Vec2(0, 0), 100 + ApproachCues.Cadence("skitter") - 0.01, heard), Is.EqualTo(0));
            Assert.That(cues.Collect(bodies, new Vec2(0, 0), 100 + ApproachCues.Cadence("skitter"), heard), Is.EqualTo(1));
            Assert.That(heard.Last().Kind, Is.EqualTo("skitter"));
            Assert.That(cues.Collect(bodies, new Vec2(0, 0), 100 + ApproachCues.Cadence("spitter"), heard), Is.EqualTo(1));
            Assert.That(heard.Last().Kind, Is.EqualTo("spitter"));
            Assert.That(cues.Collect(bodies, new Vec2(0, 0), 5, heard), Is.EqualTo(2), "the clock ran back: a load");
        }

        [Test] public void ListeningChangesNothingInTheBodies()
        {
            var body = Body(1, "breaker", 12, -3, EnemyLayer.Major);
            var cues = new ApproachCues();
            cues.Collect(new[] { body }, new Vec2(0, 0), 1, new List<ApproachCues.Cue>());
            Assert.That(body.Pos, Is.EqualTo(new Vec2(12, -3)));
            Assert.That(body.Withdrawing, Is.False);
            Assert.That(body.OnPlayer, Is.False);
        }
    }
}
