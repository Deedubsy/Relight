using System.Collections.Generic;
using Relight.Presentation;
using UnityEngine;

namespace Relight.UI
{
    /// <summary>
    /// B-13. The single owner of Escape (TECHNICAL_ARCHITECTURE.md §8.3 row 3). Handlers register themselves and the
    /// chain runs them in <see cref="IEscapeHandler.Order"/> order until one returns true — the reference's
    /// ladder at uiShell.ts:114-120, turned into a list so that a Phase C panel adds a link instead of editing a
    /// condition in a shared file.
    ///
    /// Every link of the reference's order exists already. The ones whose feature has not been ported yet are
    /// registered as no-ops that return false (they have nothing to cancel), each naming the Phase C task that
    /// replaces it, so the ORDER is fixed and tested now and later work only has to fill a slot.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Escape Chain")]
    public sealed class EscapeChain : MonoBehaviour
    {
        private readonly List<IEscapeHandler> _handlers = new List<IEscapeHandler>();
        private bool _defaultsInstalled;

        /// <summary>True once <see cref="InstallDefaults"/> has run; the shell's links are present.</summary>
        public bool DefaultsInstalled => _defaultsInstalled;

        /// <summary>Handlers in run order, for tests and for a debug panel.</summary>
        public IReadOnlyList<IEscapeHandler> Handlers => _handlers;

        /// <summary>Register a link. Re-registering the same instance does nothing.</summary>
        public void Register(IEscapeHandler h)
        {
            if (h == null || _handlers.Contains(h)) return;
            var i = 0;
            while (i < _handlers.Count && _handlers[i].Order <= h.Order) i++;
            _handlers.Insert(i, h);
        }

        public void Unregister(IEscapeHandler h)
        {
            if (h != null) _handlers.Remove(h);
        }

        /// <summary>Run the chain. Returns the handler that consumed Escape, or null if none did.</summary>
        public IEscapeHandler Escape()
        {
            for (var i = 0; i < _handlers.Count; i++)
                if (_handlers[i].OnEscape()) return _handlers[i];
            return null;
        }

        /// <summary>
        /// A link that exists so the order is right, and does nothing because its feature is not ported.
        /// <paramref name="owner"/> names the task that replaces it.
        /// </summary>
        public sealed class Placeholder : IEscapeHandler
        {
            public int Order { get; }
            public string Owner { get; }
            public Placeholder(int order, string owner) { Order = order; Owner = owner; }
            public bool OnEscape() => false;
        }

        /// <summary>
        /// A link backed by a delegate, for the two things that DO exist in Phase B (close the open panel, pause).
        /// </summary>
        public sealed class Link : IEscapeHandler
        {
            private readonly System.Func<bool> _run;
            public int Order { get; }
            public Link(int order, System.Func<bool> run) { Order = order; _run = run; }
            public bool OnEscape() => _run != null && _run();
        }

        /// <summary>
        /// Fill the chain with the reference's eight links. Called by <see cref="UiShell"/> at startup; the two
        /// live links are its own.
        /// </summary>
        public void InstallDefaults(System.Func<bool> closeDrawer, System.Func<bool> pause)
        {
            // Correction pass: PauseMenuController registers its three rungs from its own OnEnable, and component
            // order put it before UiShell on the GameUI object. The shell used to install the defaults only when
            // the chain was empty, so the drawer and pause links were never added and Escape with the Backpack
            // open did nothing (the playtest's "Escape is inconsistent"). The chain now owns the guard itself.
            if (_defaultsInstalled) return;
            _defaultsInstalled = true;
            // TODO(C-06): Backpack drag cancel — uiDrag.ts cancelUiDrag(); Escape must cancel a drag before anything else.
            Register(new Placeholder(EscapeOrder.CancelDrag, "C-06 Backpack drag"));
            // TODO(C-06): the slot context menu (uiShell.ts .slot-menu).
            Register(new Placeholder(EscapeOrder.CloseSlotMenu, "C-06 slot menu"));
            // TODO(C-10): the nav bar's open <details> menu.
            Register(new Placeholder(EscapeOrder.CloseNavMenu, "C-10 nav menu"));
            // TODO(C-10): a modal child screen opened from Pause (Settings, Load).
            Register(new Placeholder(EscapeOrder.CloseModalChild, "C-10 pause child screen"));
            // TODO(C-10): unpause from the pause modal. B-13 has no pause modal, only SimHost.Paused.
            Register(new Placeholder(EscapeOrder.Unpause, "C-10 pause modal"));
            Register(new Link(EscapeOrder.CloseDrawer, closeDrawer));
            // C-02 link 400: put away whatever is in the hand — a machine ghost or a weapon. It sits AFTER the
            // drawer link, so Escape with the Backpack open closes the Backpack and leaves the hand alone, and
            // BEFORE pause, so the first Escape after picking a machine cancels the placement instead of pausing.
            Register(new Link(EscapeOrder.CancelWorldSelection, CancelHand));
            Register(new Link(EscapeOrder.Pause, pause));
        }

        /// <summary>
        /// The hand link. <see cref="WorldInput"/> lives in World.unity and this chain on the GameUI document, so
        /// the two cannot be wired in the inspector; the lookup is lazy and cached, and a scene with no world
        /// input (Boot, a UI-only test) simply falls through to pause.
        /// </summary>
        private WorldInput _hand;

        private bool CancelHand()
        {
            if (_hand == null) _hand = FindAnyObjectByType<WorldInput>();
            return _hand != null && _hand.CancelHand();
        }
    }
}
