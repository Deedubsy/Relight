using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Relight.UI
{
    /// <summary>
    /// C-06. The slot context menu (reference inventoryPanel.ts's <c>.slot-menu</c>, closed by uiShell.ts:117 —
    /// the SECOND link of the Escape ladder, after cancelling a drag and before anything else).
    ///
    /// It is a plain wrapper around the <c>slot-menu</c> element that already exists in InventoryPanel.uxml: it
    /// never creates a button, never positions itself in world space, and holds no item state — the panel keeps
    /// the selection and the menu only decides which of the four actions are offered for it. That keeps the menu
    /// editable in UI Builder and keeps "what a stack can do" in one place.
    ///
    /// It is opened at the slot, not at the pointer, so a keyboard-opened menu lands somewhere sensible.
    /// </summary>
    public sealed class SlotMenu : IEscapeHandler
    {
        private readonly VisualElement _root;
        private readonly Button _transfer, _split, _move, _sort;

        /// <summary>Escape link 200 (uiShell.ts:117): the menu closes before the drawer does.</summary>
        public int Order => EscapeOrder.CloseSlotMenu;

        /// <summary>True while the menu is on screen.</summary>
        public bool Open { get; private set; }

        public SlotMenu(VisualElement root,
            Action transfer, Action split, Action move, Action sort)
        {
            _root = root;
            if (_root == null) return;
            _transfer = _root.Q<Button>("menu-transfer");
            _split = _root.Q<Button>("menu-split");
            _move = _root.Q<Button>("menu-move");
            _sort = _root.Q<Button>("menu-sort");
            Wire(_transfer, transfer);
            Wire(_split, split);
            Wire(_move, move);
            Wire(_sort, sort);
            Close();
        }

        private void Wire(Button b, Action a)
        {
            if (b == null) return;
            b.clicked += () => { Close(); a?.Invoke(); };
        }

        /// <summary>
        /// Show the menu beside <paramref name="near"/>. <paramref name="transferText"/> is the first item's
        /// label, which differs by side ("Load into Home storage" / "Take into Backpack"), and
        /// <paramref name="canSplit"/> is false for a storage chunk, which has no slot to split into.
        /// </summary>
        public void Show(VisualElement near, string transferText, bool canSplit)
        {
            if (_root == null) return;
            if (_transfer != null) _transfer.text = transferText ?? "";
            if (_split != null) _split.SetEnabled(canSplit);
            if (_move != null) _move.SetEnabled(canSplit);

            // Position: the menu is absolutely positioned by USS; only its offset within the panel is dynamic,
            // which is a measured quantity and cannot be a class.
            if (near != null && _root.parent != null)
            {
                var at = _root.parent.WorldToLocal(new Vector2(near.worldBound.xMin, near.worldBound.yMax));
                _root.style.left = at.x;
                _root.style.top = at.y;
            }
            _root.AddToClassList("open");
            _root.BringToFront();
            Open = true;
        }

        public void Close()
        {
            if (_root == null) return;
            _root.RemoveFromClassList("open");
            Open = false;
        }

        /// <summary>Escape: close the menu if it is open, and say so, so the drawer does not also close.</summary>
        public bool OnEscape()
        {
            if (!Open) return false;
            Close();
            return true;
        }
    }
}
