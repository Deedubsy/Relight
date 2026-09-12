using System.Collections.Generic;
using Relight.Data;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// Machine kind → prefab, built from authored rows rather than <c>Resources.Load</c> by string
    /// (TECHNICAL_ARCHITECTURE.md §7.2).
    ///
    /// INTERIM: §7.2 asks for a bidirectional link — the view holds its <see cref="MachineDefinition"/> and the
    /// definition holds its prefab — but <c>MachineDefinition</c> (B-05, <c>Relight.Data</c>, not this task's to
    /// edit) has no prefab field yet. Until it does, the forward half of the link lives on
    /// <see cref="MachineView"/> (which holds its definition) and the backward half lives here. The report proposes
    /// the field; when it exists, this asset is the lookup table built from it, not a second authoring surface.
    /// </summary>
    [CreateAssetMenu(menuName = "Relight/Prefab Registry", fileName = "PrefabRegistry")]
    public sealed class PrefabRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Row
        {
            [Tooltip("The machine definition this prefab draws (its Key is the sim's machine kind).")]
            public MachineDefinition definition;
            public GameObject prefab;
        }

        [SerializeField] private Row[] rows = new Row[0];

        [Tooltip("Drawn for a placed machine kind with no row of its own. Placeholder; Phase C gives every kind a prefab.")]
        [SerializeField] private GameObject fallback;

        private Dictionary<string, GameObject> _byKind;

        public IReadOnlyList<Row> Rows => rows;
        public GameObject Fallback => fallback;

        /// <summary>The prefab for a sim machine kind, or the fallback, or null.</summary>
        public GameObject Get(string kind)
        {
            if (_byKind == null)
            {
                _byKind = new Dictionary<string, GameObject>(rows.Length);
                for (var i = 0; i < rows.Length; i++)
                {
                    var r = rows[i];
                    if (r.definition == null || r.prefab == null || string.IsNullOrEmpty(r.definition.Key)) continue;
                    _byKind[r.definition.Key] = r.prefab;
                }
            }
            return kind != null && _byKind.TryGetValue(kind, out var p) ? p : fallback;
        }

        /// <summary>Used by the editor generator; authoring is otherwise done in the Inspector.</summary>
        public void SetRows(Row[] newRows, GameObject newFallback)
        {
            rows = newRows;
            fallback = newFallback;
            _byKind = null;
        }
    }
}
