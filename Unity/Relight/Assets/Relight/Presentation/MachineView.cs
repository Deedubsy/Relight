using Relight.Data;
using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// B-13. One placed machine's visual. It holds the machine's <see cref="MachineDefinition"/> (the forward half
    /// of the data link in TECHNICAL_ARCHITECTURE.md §7.2) and a sim machine <b>id</b> — never a
    /// <c>Machine</c> reference (TA §4.5). Everything else it needs it asks the selectors for.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Machine View")]
    public sealed class MachineView : MonoBehaviour
    {
        [Tooltip("The data asset this prefab draws. Its Key is the sim machine kind, and it is what the Inspector edits.")]
        [SerializeField] private MachineDefinition definition;

        [Tooltip("Sprite depth; machines draw over the tilemap and under the engineer.")]
        [SerializeField] private float z = -1f;

        private SpriteRenderer _sprite;

        /// <summary>The sim id of the machine this view stands for; -1 before <see cref="Bind"/>.</summary>
        public int MachineId { get; private set; } = -1;

        /// <summary>The authored definition, for the presenter's kind check and for tests.</summary>
        public MachineDefinition Definition => definition;

        /// <summary>The sim machine kind this prefab is for, from the data asset.</summary>
        public string Kind => definition == null ? null : definition.Key;

        private void Awake() => _sprite = GetComponentInChildren<SpriteRenderer>();

        /// <summary>Place this view on a machine's footprint. Called by <see cref="MachinePresenter"/> only.</summary>
        public void Bind(in MachinePlacement m)
        {
            MachineId = m.Id;
            if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();
            var centre = WorldSpace.RectCentre(m.X, m.Y, m.W, m.H);
            transform.position = new Vector3(centre.x, centre.y, z);
            // The placeholder sprite is one tile; scale it to the footprint the sim actually occupies.
            if (_sprite != null) _sprite.transform.localScale = new Vector3(m.W, m.H, 1f);
            name = $"{(definition != null ? definition.DisplayName : m.Kind)} #{m.Id}";
        }

        /// <summary>Used by the editor generator when it authors the prefab.</summary>
        public void SetDefinition(MachineDefinition d) => definition = d;
    }
}
