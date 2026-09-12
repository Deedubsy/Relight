using UnityEngine;

namespace Relight.Data
{
    /// <summary>
    /// Shared authoring fields for every content definition asset: the stable key the sim uses, the catalogue
    /// status, the provenance string and the provisional flag (CONTENT_CATALOGUE.md §17.4, TA §5.2).
    /// Definitions are authoring data only — no sim signature ever takes a ScriptableObject (U-M-25).
    /// </summary>
    public abstract class DataDefinition : ScriptableObject
    {
        [Tooltip("Stable lower-case identifier used by the simulation. Never localise this.")]
        [SerializeField] private string key = string.Empty;

        [Tooltip("Name shown to the player.")]
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("Catalogue status: current = shipped, approved = accepted but unimplemented, provisional = to be retuned.")]
        [SerializeField] private ContentKind kind = ContentKind.Current;

        [Tooltip("Where the numbers came from, as \"<file>:<line>\" (semicolon separated).")]
        [SerializeField, TextArea(1, 4)] private string source = string.Empty;

        [Tooltip("Set for values that playtesting is expected to change. Kept in step with Kind = Provisional.")]
        [SerializeField] private bool provisional;

        [Tooltip("Set by the generator: a fingerprint of the values it last wrote. When the values no longer match it, the asset has been hand-edited and Generate Data Assets leaves it alone.")]
        [SerializeField, HideInInspector] private string generatedFingerprint = string.Empty;

        public string Key => key;
        public string DisplayName => displayName;
        public ContentKind Kind => kind;
        public string Source => source;
        public bool Provisional => provisional;
        public string KindText => ContentKinds.ToText(kind);

        /// <summary>Used by the editor generator; authoring is otherwise done in the Inspector.</summary>
        public void SetCommon(string newKey, string newDisplayName, string kindText, string newSource, bool isProvisional)
        {
            key = newKey;
            displayName = newDisplayName;
            kind = ContentKinds.Parse(kindText);
            source = newSource;
            provisional = isProvisional;
        }

        /// <summary>The fingerprint the generator stamped after its last write; empty for an asset it has not stamped.</summary>
        public string GeneratedFingerprint => generatedFingerprint;

        /// <summary>Used by the editor generator only.</summary>
        public void SetGeneratedFingerprint(string fingerprint) => generatedFingerprint = fingerprint ?? string.Empty;

        /// <summary>Problems that make this asset unusable, or null when it is fine. The validator collects these.</summary>
        public virtual string Problem()
        {
            if (string.IsNullOrEmpty(key)) return "key is empty";
            if (string.IsNullOrEmpty(displayName)) return "display name is empty";
            if (string.IsNullOrEmpty(source)) return "source is empty";
            if (key.Contains("Unresolved") || source.Contains("Unresolved")) return "carries an Unresolved marker";
            if (provisional != (kind == ContentKind.Provisional)) return "Provisional flag disagrees with Kind";
            return null;
        }

        protected virtual void OnValidate()
        {
            var problem = Problem();
            if (problem != null) Debug.LogWarning($"{GetType().Name} '{name}': {problem}", this);
        }
    }

    /// <summary>An item and a count, as serialised in recipe and cost lists.</summary>
    [System.Serializable]
    public struct ItemAmount
    {
        public Relight.Sim.ItemId item;
        public int count;

        public ItemAmount(Relight.Sim.ItemId item, int count)
        {
            this.item = item;
            this.count = count;
        }

        public Relight.Sim.ItemStack ToStack() => new Relight.Sim.ItemStack(item, count);

        public static ItemAmount[] From(System.Collections.Generic.IReadOnlyList<Relight.Sim.ItemStack> stacks)
        {
            if (stacks == null) return new ItemAmount[0];
            var a = new ItemAmount[stacks.Count];
            for (var i = 0; i < stacks.Count; i++) a[i] = new ItemAmount(stacks[i].Item, stacks[i].Count);
            return a;
        }

        public static Relight.Sim.ItemStack[] ToStacks(ItemAmount[] amounts)
        {
            if (amounts == null) return new Relight.Sim.ItemStack[0];
            var a = new Relight.Sim.ItemStack[amounts.Length];
            for (var i = 0; i < amounts.Length; i++) a[i] = amounts[i].ToStack();
            return a;
        }
    }
}
