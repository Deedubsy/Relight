using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Relight.UI
{
    /// <summary>
    /// GP-UX-1. "Pressing a movement key while a drawer is open closes the drawer and lets the engineer walk."
    ///
    /// The awkward part is that the drawer has switched the <b>World</b> map off, so <c>World/Move</c> itself
    /// reports nothing: an action on a disabled map has no value and fires no press. The obvious workaround —
    /// mirroring WASD onto the always-live Global map — is wrong here, because movement is rebindable:
    /// <see cref="Settings.BindingMap"/> writes north/south/west/east onto composite parts 1-4 of
    /// <c>World/Move</c>, so a mirror would go on watching W the moment the player moved north to, say, Z.
    ///
    /// So this reads the player's OWN bindings instead. Each non-composite binding's <c>effectivePath</c> (the
    /// override when there is one, the authored path otherwise) is resolved to a live control with
    /// <see cref="InputSystem.FindControl"/>, and the controls are polled directly from their devices — which
    /// works whether or not the action that owns them is enabled. Resolution is redone only when the paths
    /// themselves change, so a rebind is picked up without re-resolving every frame.
    ///
    /// A binding that is not a button (a gamepad stick, were one ever added) is skipped rather than guessed at:
    /// this asks one question — "did the player just press a key they walk with?" — and a stick has no press.
    /// </summary>
    public sealed class MovementKeys
    {
        private readonly List<InputControl> _controls = new List<InputControl>();
        private readonly List<string> _paths = new List<string>();

        /// <summary>The controls currently being watched. For tests and for diagnosis.</summary>
        public IReadOnlyList<InputControl> Controls => _controls;

        /// <summary>True on the frame a key bound to <paramref name="move"/> goes down, map enabled or not.</summary>
        public bool PressedThisFrame(InputAction move)
        {
            Resolve(move);
            for (var i = 0; i < _controls.Count; i++)
                if (_controls[i] is ButtonControl button && button.wasPressedThisFrame) return true;
            return false;
        }

        /// <summary>True while any key bound to <paramref name="move"/> is held. Not used to start a walk — the
        /// sim's walk vector comes from <c>WorldInput</c> — only to answer "is the player trying to move?".</summary>
        public bool HeldNow(InputAction move)
        {
            Resolve(move);
            for (var i = 0; i < _controls.Count; i++)
                if (_controls[i] is ButtonControl button && button.isPressed) return true;
            return false;
        }

        /// <summary>Re-resolve if and only if the action's effective binding paths have changed (a rebind).</summary>
        private void Resolve(InputAction move)
        {
            if (move == null)
            {
                _controls.Clear();
                _paths.Clear();
                return;
            }
            if (!Changed(move)) return;

            _paths.Clear();
            _controls.Clear();
            var bindings = move.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                // The 2DVector composite itself has no path; its four parts do.
                if (bindings[i].isComposite) continue;
                var path = bindings[i].effectivePath;
                if (string.IsNullOrEmpty(path)) continue;
                _paths.Add(path);
                var control = InputSystem.FindControl(path);
                if (control != null && !_controls.Contains(control)) _controls.Add(control);
            }
        }

        // Allocation-free: comparing the paths in place beats rebuilding a signature string every frame.
        private bool Changed(InputAction move)
        {
            var seen = 0;
            var bindings = move.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].isComposite) continue;
                var path = bindings[i].effectivePath;
                if (string.IsNullOrEmpty(path)) continue;
                if (seen >= _paths.Count || !string.Equals(_paths[seen], path, StringComparison.Ordinal)) return true;
                seen++;
            }
            return seen != _paths.Count;
        }
    }
}
