using System;
using System.Collections.Generic;
using Relight.Sim;

namespace Relight.Presentation
{
    /// <summary>
    /// REL-67, U-D-62: "each alien kind in the game has a distinct approach cue, panned by direction, and heard from
    /// beyond sight range." This picks which approach cues to sound this frame; <see cref="AudioCueRouter"/> voices
    /// them at the body's position, so the pan comes from where it is.
    ///
    /// Rules (U-P-28, provisional numbers):
    /// <list type="bullet">
    /// <item>A body is <b>coming</b> when it is not withdrawing and is either a raider (minor or major layer) or a
    /// camp resident that has left its camp to chase the engineer. A camp resident sitting at home is not coming,
    /// so a quiet camp stays quiet.</item>
    /// <item>For each kind, only the nearest coming body within <see cref="HearingTiles"/> of the listener sounds, and
    /// that kind sounds at most once per its cadence, on the sim clock. Five skitters are one chitter, not five.</item>
    /// <item>It reads the state and writes nothing, so no cue changes the sim (§12.1 rule 1). A paused host is not
    /// asked, and the sim clock does not move while paused, so nothing queues up for the resume.</item>
    /// </list>
    /// </summary>
    public sealed class ApproachCues
    {
        /// <summary>
        /// Tiles within which an approach is heard. Well beyond the screen at the default zoom (about 18 tiles to the
        /// side) and beyond every dark sight range in the game (the cannon's 8 tiles). U-P-28, provisional.
        /// </summary>
        public const double HearingTiles = 48;

        /// <summary>The seconds between two cues of the same kind when no kind-specific row exists. U-P-28.</summary>
        public const double DefaultCadence = 2.0;

        /// <summary>One cue to voice: its key and the sim tile it comes from.</summary>
        public readonly struct Cue
        {
            public readonly string Key;
            public readonly Vec2 At;
            public readonly string Kind;
            public Cue(string key, Vec2 at, string kind) { Key = key; At = at; Kind = kind; }
        }

        private readonly Dictionary<string, double> _nextAt = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly Dictionary<string, Enemy> _nearest = new Dictionary<string, Enemy>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _nearestD2 = new Dictionary<string, double>(StringComparer.Ordinal);
        private double _lastT = double.NegativeInfinity;

        /// <summary>The seconds between two cues of one kind. A kind with no row uses <see cref="DefaultCadence"/>.</summary>
        public static double Cadence(string kind)
        {
            switch (kind)
            {
                case "skitter": return 1.4;
                case "spitter": return 2.2;
                case "breaker": return 1.8;
                default: return DefaultCadence;
            }
        }

        /// <summary>The cue key of one kind's approach.</summary>
        public static string Key(string kind) => PlaceholderSounds.ApproachPrefix + kind;

        /// <summary>Whether this body is coming for the player now. See the class remarks.</summary>
        public static bool Coming(Enemy e) =>
            e != null && !e.Withdrawing && (e.Layer != EnemyLayer.Site || e.OnPlayer);

        /// <summary>Forget every cadence, for a new session or a load.</summary>
        public void Reset()
        {
            _nextAt.Clear();
            _lastT = double.NegativeInfinity;
        }

        /// <summary>
        /// Add this frame's approach cues to <paramref name="into"/>, at most one per kind, and return how many were
        /// added. <paramref name="t"/> is the sim clock; if it runs backwards (a load), the cadences start again.
        /// </summary>
        public int Collect(IReadOnlyList<Enemy> enemies, Vec2 listener, double t, List<Cue> into)
        {
            if (t < _lastT) _nextAt.Clear();
            _lastT = t;
            if (enemies == null || into == null) return 0;

            _nearest.Clear();
            _nearestD2.Clear();
            var reach2 = HearingTiles * HearingTiles;
            for (var i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (!Coming(e) || string.IsNullOrEmpty(e.Kind)) continue;
                var dx = e.Pos.X - listener.X;
                var dy = e.Pos.Y - listener.Y;
                var d2 = dx * dx + dy * dy;
                if (d2 > reach2) continue;
                if (_nearestD2.TryGetValue(e.Kind, out var best) && best <= d2) continue;
                _nearest[e.Kind] = e;
                _nearestD2[e.Kind] = d2;
            }

            var added = 0;
            foreach (var pair in _nearest)
            {
                if (_nextAt.TryGetValue(pair.Key, out var due) && t < due) continue;
                _nextAt[pair.Key] = t + Cadence(pair.Key);
                into.Add(new Cue(Key(pair.Key), pair.Value.Pos, pair.Key));
                added++;
            }
            return added;
        }
    }
}
