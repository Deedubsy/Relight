using UnityEngine;
using UnityEngine.InputSystem;

namespace Relight.UI
{
    /// <summary>
    /// B-13. Owns which Input System action map is live (TECHNICAL_ARCHITECTURE.md §8.3 rows 1-2, §8.4). There are
    /// three maps in <c>RelightControls.inputactions</c> and exactly one rule:
    /// <list type="bullet">
    /// <item><c>World</c> — engineer movement, click-to-move, placement. Disabled while a panel is open, which is
    ///       the port of the reference's capture-phase <c>stopImmediatePropagation</c> wall (uiShell.ts:104-131):
    ///       there the world listeners were starved of events, here the map is simply off.</item>
    /// <item><c>UI</c> — panel navigation. Enabled while a panel is open.</item>
    /// <item><c>Global</c> — Escape (Cancel) and Pause. Always enabled, because Escape is what closes the panel and
    ///       the reference binds it on the window, not on the drawer.</item>
    /// </list>
    ///
    /// Held keys (TA §8.3 row 2): a key held while the World map is disabled and released while it is still
    /// disabled must not act when the map comes back. Re-ENTRY needs no guard here — the Input System re-enables a
    /// map with its actions in the default state, so a still-held key produces no new press edge (that half is
    /// pinned by <c>HeldKeyTests</c>). The EXIT half does need one, and it lives in <c>WorldInput</c>, not here:
    /// whatever the player is holding when this disables the map has already become state in the sim (the walk
    /// vector, the sprint flag), and the release edge that would clear it is never delivered. <c>WorldInput</c>
    /// therefore clears both once when it sees the map go off, which is the port of the reference's
    /// <c>held</c>/<c>suppressed</c> sets (uiShell.ts:85-92). This router stays a pure routing switch.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Input Router")]
    public sealed class InputRouter : MonoBehaviour
    {
        [Tooltip("The shared RelightControls asset. The same asset WorldInput reads.")]
        [SerializeField] private InputActionAsset actions;

        private InputActionMap _world, _ui, _global;

        /// <summary>True while the UI map owns input and the World map is off.</summary>
        public bool UiActive { get; private set; }

        /// <summary>The Global map's Cancel action (Escape), for whoever drives the escape chain.</summary>
        public InputAction Cancel { get; private set; }

        /// <summary>The Global map's Pause action.</summary>
        public InputAction Pause { get; private set; }

        /// <summary>The Global map's panel toggle (Tab), the reference's drawer buttons in one key.</summary>
        public InputAction TogglePanel { get; private set; }

        /// <summary>The asset, so a test or a settings screen can reach the maps without a second reference.</summary>
        public InputActionAsset Actions => actions;

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError("Relight: InputRouter has no InputActionAsset.");
                return;
            }
            _world = actions.FindActionMap("World", false);
            _ui = actions.FindActionMap("UI", false);
            _global = actions.FindActionMap("Global", false);
            Cancel = _global?.FindAction("Cancel", false);
            Pause = _global?.FindAction("Pause", false);
            TogglePanel = _global?.FindAction("TogglePanel", false);
        }

        private void OnEnable()
        {
            _global?.Enable();
            Route(UiActive);
        }

        private void OnDisable()
        {
            _global?.Disable();
            _ui?.Disable();
            _world?.Disable();
        }

        /// <summary>Panels opened or closed: move input to the UI map, or back to the world.</summary>
        public void SetUiActive(bool active)
        {
            UiActive = active;
            Route(active);
        }

        private void Route(bool ui)
        {
            if (ui)
            {
                _world?.Disable();
                _ui?.Enable();
            }
            else
            {
                _ui?.Disable();
                _world?.Enable();
            }
        }
    }
}
