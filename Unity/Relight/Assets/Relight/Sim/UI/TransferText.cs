using System;
using System.Collections.Generic;
using System.Globalization;

namespace Relight.Sim.UI
{
    /// <summary>
    /// C-06. The Backpack/storage panel's own sentences, ported from
    /// <c>packages/game/src/inventoryPanel.ts</c> and fixed by UI_AND_ONBOARDING.md §5.4.
    ///
    /// Only the panel's <b>own</b> text lives here. Every refusal that comes from the simulation —
    /// <c>MachineInventory</c>, <c>Pockets</c>, <c>InventoryCommands</c>, <c>WeaponRules</c>, <c>HandCraft</c> —
    /// is shown verbatim as the command returned it and is never restated, reworded or duplicated in this file.
    /// </summary>
    public static class TransferText
    {
        /// <summary>
        /// §5.1 / inventoryPanel.ts, under the item-detail block. GP-UX-9 names the split gesture here: it is the
        /// only place a player finds out that Shift-dragging splits a stack into the slot under the pointer.
        /// </summary>
        public const string Hint =
            "Drag to move · Shift-drag to split · Shift-click to transfer · Sort is on the Backpack header";

        /// <summary>inventoryPanel.ts, the Backpack column's subtitle.</summary>
        public const string CarriedNote = "Your carried items stay with the engineer.";

        /// <summary>inventoryPanel.ts, the storage column's subtitle at Home.</summary>
        public const string StorageNote = "Home · supplies remain here until collected";

        /// <summary>inventoryPanel.ts, nothing selected.</summary>
        public const string SelectStack = "Select a stack";

        /// <summary>Buttons, inventoryPanel.ts.</summary>
        public const string SplitStack = "Split stack";
        public const string Sort = "Sort";
        public const string MoveToSlot = "Move to slot";
        public const string TakeIntoBackpack = "Take into Backpack";

        /// <summary>§5.4 and inventoryPanel.ts, word for word.</summary>
        public const string StoreChanged = "Store changed; select the stack again.";
        public const string StoreRemoved = "Store removed. Select another store.";
        public const string SourceChanged = "Source stack changed. Select it again.";
        public const string SplitNeedsEmpty = "Backpack is full; splitting needs an empty slot.";
        public const string WholeQuantity = "Choose a whole positive quantity.";
        public const string ChooseDestination = "Choose a backpack destination slot.";
        public const string BackpackChanged = "Backpack changed; try again.";

        /// <summary>
        /// GP-UX-9. A release that landed away from every slot grid. A near miss INSIDE a grid is snapped to the
        /// nearest slot instead, so this is only said when the stack really was let go somewhere that is not a
        /// slot — and it has to be said, because saying nothing is what made a lost drop look like an auto-sort.
        /// </summary>
        public const string DropOffSlot = "Released away from the slots; nothing moved.";

        /// <summary>GP-UX-9. The sim's two split refusals, shown on the ghost before the player commits.</summary>
        public const string SplitNeedsEmptySlot = "Splitting needs an empty slot.";
        public const string SplitTooFew = "Choose fewer than the stack contains.";

        /// <summary>§5.6 — the only legal targets for a weapon are equipment and action-bar slots.</summary>
        public const string WeaponTarget =
            "Drop weapons on equipment or action-bar slots; other items go in inventory slots.";

        /// <summary>
        /// New string, Wave 1 integration note: a weapon may not be put into a chest or a machine at all, so the
        /// panel refuses the drop before a command is sent rather than letting a transfer look possible.
        /// </summary>
        public const string WeaponsNotStored = "Weapons stay in the Backpack or on the belt.";

        /// <summary>Drag ghosts, inventoryPanel.ts.</summary>
        public const string DragEquip = "Equip weapon · action bar selects your active equipment";
        public const string DragMove = "Move stack";
        public const string DragCancel = "Release to cancel";
        public const string DragUnequip = "Return weapon to Backpack";

        /// <summary>GP-UX-9. The ghost's line while Shift is held: the amount and the destination slot.</summary>
        public static string DragSplit(int n, int slot)
            => "Split " + n.ToString(CultureInfo.InvariantCulture)
               + " into slot " + (slot + 1).ToString(CultureInfo.InvariantCulture);

        /// <summary>Equipment strip, §5.6 / inventoryPanel.ts.</summary>
        public const string EquipDropHint = "Drop a carried weapon here to equip it";
        public static string EquipSlot(int index, string equipmentName)
            => "Slot " + (index + 1).ToString(CultureInfo.InvariantCulture) + ": "
               + (string.IsNullOrEmpty(equipmentName) ? "Empty" : equipmentName + " · Unequip");

        /// <summary>inventoryPanel.ts: <c>`${number(p.count)} / ${stackSize(p.item)} in this stack · …`</c></summary>
        public static string StackDetail(double count, int stackSize, bool inBackpack)
            => PackLayout.Num(count) + " / " + stackSize.ToString(CultureInfo.InvariantCulture)
               + " in this stack · " + (inBackpack ? "Backpack" : "Storage");

        /// <summary>inventoryPanel.ts: <c>`Load into ${storeName()}`</c>.</summary>
        public static string LoadInto(string storeName) => "Load into " + storeName;

        /// <summary>inventoryPanel.ts: the drawer heading is the store's name and the Backpack, or just the Backpack.</summary>
        public static string DrawerHeading(string storeName)
            => string.IsNullOrEmpty(storeName) ? "Backpack" : storeName + " & Backpack";

        /// <summary>inventoryPanel.ts: <c>`${pocketUsed} / ${invCap} slots`</c>.</summary>
        public static string Capacity(int used, int cap)
            => used.ToString(CultureInfo.InvariantCulture) + " / " + cap.ToString(CultureInfo.InvariantCulture) + " slots";

        /// <summary>
        /// The one sentence a partial transfer produces, and the reason this file is unit-tested. §5.4:
        /// <c>"N Item moved to X. M stay in Y (X full)."</c> — the panel reports the amount the command actually
        /// moved, never the amount that was asked for, and names where the remainder stayed and why.
        /// A full transfer says only the first sentence.
        /// </summary>
        public static string Moved(double moved, double requested, string itemName, string fromName, string toName)
        {
            var text = PackLayout.Num(moved) + " " + itemName + " moved to " + toName + ".";
            if (moved < requested)
                text += " " + PackLayout.Num(requested - moved) + " stay in " + fromName + " (" + toName + " full).";
            return text;
        }

        /// <summary>inventoryPanel.ts: <c>`${storeName()} no longer holds ${name(p.item)}.`</c></summary>
        public static string NoLongerHolds(string storeName, string itemName)
            => storeName + " no longer holds " + itemName + ".";
    }
}
