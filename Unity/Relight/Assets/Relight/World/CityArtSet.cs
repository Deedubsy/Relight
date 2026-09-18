using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relight.World
{
    /// <summary>
    /// Correction pass C6. The hook for the city's real artwork: a key → <see cref="Sprite"/> table the presentation
    /// consults before it falls back to a flat tint.
    ///
    /// <b>Why this exists empty.</b> The reference art is 68 SVG files under
    /// <c>packages/game/public/art/riverfront/</c>. Unity cannot import SVG without
    /// <c>com.unity.vectorgraphics</c>, which is not in this project's manifest, and this machine has no rasteriser
    /// to convert them offline. So the whole city is drawn procedurally from the riverfrontDraw.ts palette for now,
    /// and every building still carries its <c>roofKey</c> from the exporter. Drop the sprites into an asset of this
    /// type later and the presentation uses them instead — no code change.
    ///
    /// <b>Keys.</b> Exactly the reference's asset keys:
    /// <list type="bullet">
    ///   <item><c>rf-&lt;kind&gt;-roof-&lt;variant&gt;</c> — the exporter's <c>roofKey</c>, e.g. <c>rf-shop-roof-1</c>.
    ///     Houses also carry a facing: <c>rf-house-N-roof-0</c>, <c>-E-</c>, <c>-W-</c> (south-facing houses have no
    ///     letter, matching build-facing-houses.py).</item>
    ///   <item><c>rf-&lt;kind&gt;-floor</c> — the interior floor of an enterable building, e.g. <c>rf-house-floor</c>.</item>
    /// </list>
    /// A key with no sprite (or no asset assigned at all) draws the procedural tint, so a half-filled set is valid.
    /// </summary>
    [CreateAssetMenu(menuName = "Relight/City Art Set", fileName = "CityArtSet")]
    public sealed class CityArtSet : ScriptableObject
    {
        /// <summary>One key → sprite row. A list, not a dictionary: Unity serialises lists.</summary>
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("rf-<kind>-roof-<variant> or rf-<kind>-floor.")]
            public string key;
            public Sprite sprite;
        }

        [Tooltip("Empty until the owner's art package arrives; every missing key draws the procedural tint.")]
        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, Sprite> _byKey;

        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>True when at least one sprite is assigned; false means every key falls back to the tint.</summary>
        public bool HasAny
        {
            get
            {
                for (var i = 0; i < entries.Count; i++)
                    if (entries[i] != null && entries[i].sprite != null) return true;
                return false;
            }
        }

        /// <summary>The sprite for a key, or null. Builds its lookup on first use; call <see cref="Invalidate"/> after editing.</summary>
        public Sprite Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_byKey == null)
            {
                _byKey = new Dictionary<string, Sprite>(entries.Count, StringComparer.Ordinal);
                foreach (var e in entries)
                {
                    if (e == null || string.IsNullOrEmpty(e.key) || e.sprite == null) continue;
                    _byKey[e.key] = e.sprite;
                }
            }
            return _byKey.TryGetValue(key, out var s) ? s : null;
        }

        /// <summary>The floor key for a building kind (<c>rf-house-floor</c>).</summary>
        public static string FloorKey(string kind) => string.IsNullOrEmpty(kind) ? null : "rf-" + kind + "-floor";

        /// <summary>Drop the cached lookup (after the list is edited in the Inspector).</summary>
        public void Invalidate() => _byKey = null;

        private void OnValidate() => Invalidate();
    }
}
