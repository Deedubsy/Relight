using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relight.UI
{
    /// <summary>
    /// The art side of the item icons: a key → <see cref="Sprite"/> table an artist can fill in without touching
    /// code. Every lookup that misses falls through to <see cref="ItemIcons"/>'s generated placeholder, so the
    /// game is fully playable with this asset empty, half filled, or absent altogether.
    ///
    /// Keys are the simulation's own ids — item keys from <c>Items.Key</c> ("steel", "magazine") and machine kind
    /// ids from the catalogue ("excavator", "turret"). That is the same namespace the reference's
    /// <c>itemIcons.ts</c> uses, so the two can be filled from the same list.
    ///
    /// Create one with <b>Assets ▸ Create ▸ Relight ▸ Item Icons</b> and drop it on the controllers that draw
    /// slots and cards.
    /// </summary>
    [CreateAssetMenu(menuName = "Relight/Item Icons", fileName = "ItemIcons")]
    public sealed class ItemIconLibrary : ScriptableObject
    {
        /// <summary>One row of the table.</summary>
        [Serializable]
        public struct Entry
        {
            [Tooltip("Item key (\"steel\") or machine kind id (\"excavator\").")]
            public string key;

            [Tooltip("Leave empty to use the generated placeholder for this key.")]
            public Sprite sprite;
        }

        [Tooltip("Key → sprite. Anything not listed uses the generated placeholder.")]
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        private Dictionary<string, Sprite> _byKey;

        /// <summary>True and the sprite when the table has real art for <paramref name="key"/>.</summary>
        public bool TryGet(string key, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(key)) return false;
            if (_byKey == null)
            {
                _byKey = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                for (var i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];
                    if (string.IsNullOrEmpty(e.key) || e.sprite == null) continue;
                    _byKey[e.key] = e.sprite;
                }
            }
            return _byKey.TryGetValue(key, out sprite) && sprite != null;
        }

        /// <summary>Drop the cache — call after editing the table in play mode.</summary>
        public void Invalidate() => _byKey = null;
    }
}
