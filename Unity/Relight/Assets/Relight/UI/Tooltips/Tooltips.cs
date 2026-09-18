using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// What a tooltip says. Contract C5 of the correction pass.
    ///
    /// Every field is optional; an empty one hides its element rather than printing a blank line. <see cref="Rows"/>
    /// is the "have / need" ledger the build cards, the workshop recipes and the weapon slots all want:
    /// <c>ok == false</c> paints the value red, which is the whole reason the flag exists.
    ///
    /// Nothing here formats a number. The provider is handed the sim's own strings (TransferText, WorkshopText,
    /// GameData display names), so a tooltip can never state a fact the simulation does not.
    /// </summary>
    public sealed class TooltipContent
    {
        /// <summary>One line, accent colour. Usually the thing's display name.</summary>
        public string Title;

        /// <summary>A sentence or two under the title. Wraps.</summary>
        public string Body;

        /// <summary>Label/value pairs. <c>ok == false</c> is shown in the danger colour.</summary>
        public List<(string label, string value, bool ok)> Rows;

        /// <summary>Muted last line: the shortcut, the lock reason, "placeholder art".</summary>
        public string Footer;
    }

    /// <summary>
    /// The one way a panel asks for a tooltip (contract C5). A controller calls
    /// <c>Tooltips.Attach(element, () =&gt; new TooltipContent { ... })</c> once per element and forgets about it;
    /// the provider runs when the pointer has rested on the element for <see cref="TooltipController.DelayMs"/>,
    /// so the numbers are read at the moment they are shown and never go stale.
    ///
    /// The registry is static because the elements outlive nothing: they are children of the one GameUI document,
    /// and the single <see cref="TooltipController"/> in the scene registers itself here when it enables. Attaching
    /// before the controller exists is fine — the callbacks are on the elements, and they simply do nothing until
    /// a controller is present.
    /// </summary>
    public static class Tooltips
    {
        private static readonly Dictionary<VisualElement, Func<TooltipContent>> Providers =
            new Dictionary<VisualElement, Func<TooltipContent>>();

        /// <summary>The live controller, or null. Set by <see cref="TooltipController"/> as it enables.</summary>
        internal static TooltipController Controller;

        /// <summary>Elements currently carrying a provider. Only the controller and the tests need this.</summary>
        public static int AttachedCount => Providers.Count;

        /// <summary>
        /// Give <paramref name="target"/> a tooltip. Re-attaching replaces the provider and does NOT double up the
        /// callbacks, which matters because every panel rebuilds its slots whenever the grid grows.
        /// </summary>
        public static void Attach(VisualElement target, Func<TooltipContent> provider)
        {
            if (target == null || provider == null) return;
            if (!Providers.ContainsKey(target))
            {
                target.RegisterCallback<PointerEnterEvent>(OnEnter);
                target.RegisterCallback<PointerLeaveEvent>(OnLeave);
                target.RegisterCallback<PointerDownEvent>(OnDown);
                target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            }
            Providers[target] = provider;
        }

        /// <summary>Forget <paramref name="target"/>. Safe to call on an element that never had a tooltip.</summary>
        public static void Detach(VisualElement target)
        {
            if (target == null || !Providers.Remove(target)) return;
            target.UnregisterCallback<PointerEnterEvent>(OnEnter);
            target.UnregisterCallback<PointerLeaveEvent>(OnLeave);
            target.UnregisterCallback<PointerDownEvent>(OnDown);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            Controller?.Cancel(target);
        }

        /// <summary>Hide whatever is showing, now. Drag start, panel close, grid rebuild.</summary>
        public static void HideNow() => Controller?.HideNow();

        /// <summary>Read the provider for an element — the controller's only way in, and the tests'.</summary>
        internal static Func<TooltipContent> ProviderFor(VisualElement target)
        {
            if (target == null) return null;
            return Providers.TryGetValue(target, out var p) ? p : null;
        }

        private static void OnEnter(PointerEnterEvent e)
        {
            if (e.currentTarget is VisualElement v) Controller?.Request(v);
        }

        private static void OnLeave(PointerLeaveEvent e)
        {
            if (e.currentTarget is VisualElement v) Controller?.Cancel(v);
        }

        // A press is the start of a click or a drag. Either way the tooltip is in the way (C5).
        private static void OnDown(PointerDownEvent e)
        {
            Controller?.HideNow();
        }

        // The slot grids destroy and rebuild their buttons; an element that has left the panel must not keep a
        // tooltip pinned to a rectangle that no longer exists.
        private static void OnDetach(DetachFromPanelEvent e)
        {
            if (e.currentTarget is VisualElement v) Controller?.Cancel(v);
        }
    }
}
