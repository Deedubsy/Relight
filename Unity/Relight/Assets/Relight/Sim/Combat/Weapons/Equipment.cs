using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>Craft a Rifle at the Home workshop (reference equipment.ts <c>{type:'craftRifle'}</c>).</summary>
    public sealed record CraftRifleCommand : Command;

    /// <summary>Abandon the Rifle craft; the reserved plates come back (reference <c>{type:'cancelRifle'}</c>).</summary>
    public sealed record CancelRifleCraftCommand : Command;

    /// <summary>Put a carried weapon in equipment slot 0 or 1 (reference <c>{type:'equip'}</c>).</summary>
    public sealed record EquipCommand(string Item, int Slot) : Command;

    /// <summary>Take the weapon out of slot 0 or 1 and back into the Backpack (reference <c>{type:'unequip'}</c>).</summary>
    public sealed record UnequipCommand(int Slot) : Command;

    /// <summary>Switch which equipment slot is in the hands (reference <c>{type:'swap'}</c>).</summary>
    public sealed record SwapWeaponCommand : Command;

    /// <summary>
    /// Put <paramref name="Key"/> on action-bar slot <paramref name="Slot"/> (0..9 = keys 1..0), or clear it with
    /// an empty key (UI_AND_ONBOARDING §5.5). A tool never sits on two slots: assigning one that is already
    /// elsewhere SWAPS the two slots rather than duplicating it.
    /// </summary>
    public sealed record AssignBarCommand(int Slot, string Key) : Command;

    /// <summary>A Rifle came off the Home workbench (presentation: the toast and the Backpack flash).</summary>
    public sealed record WeaponCraftedEvent(double T, string Item, string Kind) : SimEvent(T);

    /// <summary>Equipment changed: which slot, what is in it now ("" = empty) and which slot is in the hands.</summary>
    public sealed record EquipmentChangedEvent(double T, int Slot, string Item, int Active) : SimEvent(T);

    /// <summary>
    /// Owning, crafting, equipping and reloading weapons, ported from reference equipment.ts
    /// (<c>equipmentCheck</c> 42-64, <c>equipmentCommand</c> 65-85, <c>cancelRifleCraft</c> 88-92,
    /// <c>tickEquipment</c> 94-111, <c>fireEquipment</c> 112-117).
    ///
    /// Deliberate differences, each with its reason:
    /// * The reference gates every action on <c>st.campaign.progression.gameplay</c> and refuses with
    ///   "Equipment belongs to the authored campaign"; the port has no campaign progression yet (C-09) and the
    ///   equipment record always exists, so that gate is absent.
    /// * <c>truckSeat</c> / <c>passenger</c> are retired with the truck (CONTENT_CATALOGUE §17), so "on foot"
    ///   means "not down".
    /// * The reference's paused-game reload wording (<c>st.speed === 0</c>) is a session concern, not sim state.
    /// * A cancelled craft's plates that the Backpack cannot take yet become an explicit debt on
    ///   <see cref="WeaponState.CraftRefundSteel"/> rather than the retired Depot <c>st.stock</c>, exactly as
    ///   <see cref="HandCraft"/> does for a cancelled bullet batch — and <c>consumed</c> is un-counted only as the
    ///   debt is actually paid back, so the conservation ledger stays balanced at every tick without a new term.
    /// * Capacity, reload time and rate come from the weapon's own <see cref="WeaponDef"/> instead of the
    ///   reference's rifle-only <c>RIFLE</c> constants; for the rifle the numbers are identical (U-P-03).
    /// </summary>
    public static class WeaponRules
    {
        // ---- refusal texts, verbatim from reference equipment.ts:42-63 --------------------------------------
        public const string OnFootText = "Use equipment on foot";
        public const string NoCraftText = "No Rifle is being crafted";
        public const string WalkHomeText = "Walk to Home workshop";
        public const string BusyCraftText = "Rifle crafting already in progress";
        public const string SlotText = "Choose equipment slot 1 or 2";
        public const string CarriedText = "Select a carried weapon";
        public const string RoomText = "Make room in the Backpack for the equipped weapon";
        public const string OtherEmptyText = "Other equipment slot is empty";
        public const string NoWeaponText = "Equip a Rifle";
        public const string ReloadingText = "Reloading";
        public const string MagFullText = "Magazine is full";
        public const string NoBulletsText = "Carry bullets in your Backpack";
        public const string BarSlotText = "Choose an action bar slot";

        /// <summary>The rifle's recipe key in <see cref="GameData.Recipes"/> (catalogue row "rifle").</summary>
        public const string RifleRecipe = "rifle";

        /// <summary>
        /// The bullet row of <see cref="GameData.Ammunition"/> (reference flow.ts:156 <c>ammoUnit</c>: one item is
        /// <c>RoundsPerItem</c> rounds, always 1 per U-D-08). <see cref="WeaponDef"/> carries no ammunition key, so
        /// the port resolves it by the catalogue key rather than adding a field to a record another slot exports.
        /// </summary>
        public static AmmoDef Ammo(GameData d)
        {
            for (var i = 0; i < d.Ammunition.Count; i++)
                if (string.Equals(d.Ammunition[i].Key, "bullet", StringComparison.Ordinal)) return d.Ammunition[i];
            return d.Ammunition.Count > 0 ? d.Ammunition[0] : null;
        }

        /// <summary>Rounds the pockets can feed a magazine (reference <c>Math.round(inv.magazine * ammoUnit(st))</c>).</summary>
        public static double Reserve(GameData d, Engineer e)
        {
            var a = Ammo(d);
            return a == null ? 0 : Math.Round(e.Inv[a.Item] * a.RoundsPerItem);
        }

        private static bool OnFoot(SimState st) => !st.Engineer.IsDown;

        // ---- crafting --------------------------------------------------------------------------------------

        /// <summary>Reference <c>equipmentCheck</c> for <c>craftRifle</c> (47-48).</summary>
        public static string CheckCraft(SimContext ctx, SimState st)
        {
            if (!OnFoot(st)) return OnFootText;
            if (!HandCraft.NearDepot(ctx, st)) return WalkHomeText;
            if (st.Weapons.Crafting) return BusyCraftText;
            if (Home.RepairLocked(st)) return "Finish or cancel the repair first";
            var d = ctx.Data;
            if (!d.TryRecipe(RifleRecipe, out var r)) return "no such recipe";
            for (var i = 0; i < r.Inputs.Count; i++)
                if (st.Engineer.Inv[r.Inputs[i].Item] < r.Inputs[i].Count) return CarryText(d, r);
            return "";
        }

        /// <summary>Reference "Carry 10 Steel plates + 4 Copper", built from the recipe so tuning edits reach it.</summary>
        private static string CarryText(GameData d, Recipe r)
        {
            var s = "Carry";
            for (var i = 0; i < r.Inputs.Count; i++)
                s += (i == 0 ? " " : " + ") + r.Inputs[i].Count + " " + d.Item(r.Inputs[i].Item).DisplayName;
            return s;
        }

        /// <summary>Reference <c>equipmentCommand</c> 68-70: the plates are reserved up front and counted as consumed.</summary>
        public static (bool ok, string reason) Craft(SimContext ctx, SimState st)
        {
            var reason = CheckCraft(ctx, st);
            if (reason.Length > 0) return (false, reason);
            var d = ctx.Data;
            d.TryRecipe(RifleRecipe, out var r);
            for (var i = 0; i < r.Inputs.Count; i++)
            {
                Pockets.Drop(d, st.Engineer, ItemKey.Of(r.Inputs[i].Item), r.Inputs[i].Count);
                st.Stats.Consumed.Add(r.Inputs[i].Item, r.Inputs[i].Count);
            }
            st.Weapons.Crafting = true;
            st.Weapons.CraftSeconds = 0;
            return (true, $"Crafting {r.DisplayName} at the Home workshop · {PackLayout.Num(r.Seconds)} seconds. It keeps working while you are away; collect it from the workshop.");
        }

        /// <summary>
        /// Reference <c>cancelRifleCraft</c> (88-92). What the Backpack cannot take becomes a debt that
        /// <see cref="Drain"/> pays in as space appears; <c>consumed</c> is un-counted only for plates actually
        /// returned, so nothing is ever both un-counted and nowhere.
        /// </summary>
        public static bool CancelCraft(SimContext ctx, SimState st)
        {
            var q = st.Weapons;
            if (!q.Crafting) return false;
            var d = ctx.Data;
            d.TryRecipe(RifleRecipe, out var r);
            for (var i = 0; i < r.Inputs.Count; i++)
            {
                var owed = r.Inputs[i].Count;
                if (r.Inputs[i].Item == ItemId.Copper) q.CraftRefundCopper += owed;
                else q.CraftRefundSteel += owed;
            }
            q.Crafting = false;
            q.CraftSeconds = 0;
            Drain(d, st);
            return true;
        }

        /// <summary>Pays the cancelled craft's plates back into the pockets, as much as fits, and un-counts them.</summary>
        public static void Drain(GameData d, SimState st)
        {
            var q = st.Weapons;
            var e = st.Engineer;
            if (q.CraftRefundSteel > 0)
            {
                var back = Pockets.Take(d, e, ItemKey.Of(ItemId.Steel), q.CraftRefundSteel);
                q.CraftRefundSteel = Math.Max(0, q.CraftRefundSteel - back);
                st.Stats.Consumed.Add(ItemId.Steel, -back);
            }
            if (q.CraftRefundCopper > 0)
            {
                var back = Pockets.Take(d, e, ItemKey.Of(ItemId.Copper), q.CraftRefundCopper);
                q.CraftRefundCopper = Math.Max(0, q.CraftRefundCopper - back);
                st.Stats.Consumed.Add(ItemId.Copper, -back);
            }
        }

        /// <summary>Reference <c>createWeapon</c> (22-25): the next serial, the instance, and the id it is known by.</summary>
        public static string Create(SimState st, string kind)
        {
            var q = st.Weapons;
            var id = kind + ":" + q.Next.ToString(System.Globalization.CultureInfo.InvariantCulture);
            q.Next++;
            q.Owned.Add(new WeaponInstance { Id = id, Kind = kind, Loaded = 0, Cooldown = 0, Reload = 0 });
            return id;
        }

        // ---- equip / unequip / swap -------------------------------------------------------------------------

        /// <summary>Reference <c>equipmentCheck</c> 49-59 for <c>equip</c> / <c>unequip</c>.</summary>
        public static string CheckSlotChange(SimContext ctx, SimState st, string item, int slot, bool equip)
        {
            if (!OnFoot(st)) return OnFootText;
            if (slot != 0 && slot != 1) return SlotText;
            var d = ctx.Data;
            var e = st.Engineer;
            var q = st.Weapons;
            if (equip)
            {
                if (string.IsNullOrEmpty(item)) return CarriedText;
                var key = new ItemKey(item);
                if (!key.IsWeapon || q.Find(item) == null || e.Inv[key] != 1) return CarriedText;
            }
            var old = q.SlotAt(slot);
            if (!string.IsNullOrEmpty(old))
            {
                var trial = Pockets.Trial(e);
                if (equip) Pockets.Drop(d, trial, new ItemKey(item), 1);
                if (Pockets.Take(d, trial, new ItemKey(old), 1) != 1) return RoomText;
            }
            return "";
        }

        /// <summary>Reference <c>equipmentCommand</c> 76-84: the item leaves the pockets, the old one comes back.</summary>
        public static (bool ok, string reason) SlotChange(SimContext ctx, SimState st, string item, int slot, bool equip)
        {
            var reason = CheckSlotChange(ctx, st, item, slot, equip);
            if (reason.Length > 0) return (false, reason);
            var d = ctx.Data;
            var e = st.Engineer;
            var q = st.Weapons;
            var old = q.SlotAt(slot);
            if (equip) Pockets.Drop(d, e, new ItemKey(item), 1);
            if (!string.IsNullOrEmpty(old)) Pockets.Take(d, e, new ItemKey(old), 1);
            q.SetSlot(slot, equip ? item : null);
            if (equip) q.Active = slot;
            e.Cooldown = q.ActiveWeapon()?.Cooldown ?? 0;
            st.Events.Add(new EquipmentChangedEvent(st.T, slot, equip ? item : "", q.Active));
            return (true, "Equipment updated; loaded ammunition and cooldown retained");
        }

        /// <summary>Reference <c>equipmentCheck</c>/<c>equipmentCommand</c> for <c>swap</c> (60, 75).</summary>
        public static (bool ok, string reason) Swap(SimContext ctx, SimState st)
        {
            if (!OnFoot(st)) return (false, OnFootText);
            var q = st.Weapons;
            if (string.IsNullOrEmpty(q.SlotAt(q.Active == 0 ? 1 : 0))) return (false, OtherEmptyText);
            q.Active = q.Active == 0 ? 1 : 0;
            st.Engineer.Cooldown = q.ActiveWeapon()?.Cooldown ?? 0;
            st.Events.Add(new EquipmentChangedEvent(st.T, q.Active, q.SlotAt(q.Active) ?? "", q.Active));
            return (true, "Equipment updated; loaded ammunition and cooldown retained");
        }

        // ---- reload ------------------------------------------------------------------------------------------

        /// <summary>Reference <c>equipmentCheck</c> 61-63 (the fall-through case is <c>reload</c>).</summary>
        public static string CheckReload(SimContext ctx, SimState st)
        {
            if (!OnFoot(st)) return OnFootText;
            var w = st.Weapons.ActiveWeapon();
            if (w == null) return NoWeaponText;
            if (w.Reload > 0) return ReloadingText;
            var cap = Capacity(ctx.Data, w);
            if (w.Loaded >= cap) return MagFullText;
            if (Reserve(ctx.Data, st.Engineer) <= 0) return NoBulletsText;
            return "";
        }

        /// <summary>Reference <c>equipmentCommand</c> 73: the reload is started here and finishes in the tick.</summary>
        public static (bool ok, string reason) Reload(SimContext ctx, SimState st)
        {
            var reason = CheckReload(ctx, st);
            if (reason.Length > 0) return (false, reason);
            var w = st.Weapons.ActiveWeapon();
            var seconds = Seconds(ctx.Data, w);
            w.Reload = seconds;
            return (true, $"Reloading · {seconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} seconds; uses carried bullets");
        }

        public static double Capacity(GameData d, WeaponInstance w) =>
            w != null && d.TryWeapon(w.Kind, out var p) ? p.Capacity : 0;

        public static double Seconds(GameData d, WeaponInstance w) =>
            w != null && d.TryWeapon(w.Kind, out var p) ? p.ReloadSeconds : 0;

        // ---- action bar --------------------------------------------------------------------------------------

        /// <summary>Action-bar slots, keys 1..9 then 0 (UI_AND_ONBOARDING §5.5).</summary>
        public const int BarSlots = 10;
        public static readonly string[] DefaultBar = { "generator", "excavator", "chest", "belt", "pole", "assembler", "turret", "lamp", "", "" };

        /// <summary>
        /// §5.5: one tool per slot and never the same tool twice. Assigning a key that already sits on another
        /// slot swaps the two slots; an empty key clears the slot.
        /// </summary>
        public static (bool ok, string reason) AssignBar(SimState st, int slot, string key)
        {
            if (slot < 0 || slot >= BarSlots) return (false, BarSlotText);
            var bar = st.Weapons.Bar;
            while (bar.Count < BarSlots) bar.Add("");
            key = key ?? "";
            var at = -1;
            for (var i = 0; i < BarSlots; i++) if (key.Length > 0 && string.Equals(bar[i], key, StringComparison.Ordinal)) { at = i; break; }
            if (at == slot) return (true, "");
            var displaced = bar[slot];
            bar[slot] = key;
            if (at >= 0) bar[at] = displaced;      // a swap, never a duplicate
            return (true, "");
        }

        /// <summary>A loaded arrangement with a duplicate collapses to the first occurrence (§5.5).</summary>
        public static void NormaliseBar(SimState st)
        {
            var bar = st.Weapons.Bar;
            while (bar.Count < BarSlots) bar.Add("");
            if (bar.Count > BarSlots) bar.RemoveRange(BarSlots, bar.Count - BarSlots);
            for (var i = 0; i < bar.Count; i++)
            {
                if (bar[i] == null) { bar[i] = ""; continue; }
                if (bar[i].Length == 0) continue;
                for (var j = 0; j < i; j++) if (string.Equals(bar[j], bar[i], StringComparison.Ordinal)) { bar[i] = ""; break; }
            }
        }
    }

    public sealed class EquipmentHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            switch (c)
            {
                case CraftRifleCommand _:
                {
                    var (ok, reason) = WeaponRules.Craft(ctx, st);
                    result = ok ? CommandResult.Ok(reason) : CommandResult.Refuse(reason);
                    return true;
                }
                case CancelRifleCraftCommand _:
                {
                    if (!st.Weapons.Crafting) { result = CommandResult.Refuse(WeaponRules.NoCraftText); return true; }
                    ctx.Data.TryRecipe(WeaponRules.RifleRecipe, out var r);
                    WeaponRules.CancelCraft(ctx, st);
                    var back = "";
                    for (var i = 0; i < r.Inputs.Count; i++)
                        back += (i == 0 ? " " : " + ") + r.Inputs[i].Count + " " + ctx.Data.Item(r.Inputs[i].Item).DisplayName;
                    result = CommandResult.Ok($"{r.DisplayName} crafting cancelled.{back} returned.");
                    return true;
                }
                case EquipCommand e:
                {
                    var (ok, reason) = WeaponRules.SlotChange(ctx, st, e.Item, e.Slot, true);
                    result = ok ? CommandResult.Ok(reason) : CommandResult.Refuse(reason);
                    return true;
                }
                case UnequipCommand u:
                {
                    var (ok, reason) = WeaponRules.SlotChange(ctx, st, null, u.Slot, false);
                    result = ok ? CommandResult.Ok(reason) : CommandResult.Refuse(reason);
                    return true;
                }
                case SwapWeaponCommand _:
                {
                    var (ok, reason) = WeaponRules.Swap(ctx, st);
                    result = ok ? CommandResult.Ok(reason) : CommandResult.Refuse(reason);
                    return true;
                }
                case AssignBarCommand a:
                {
                    var (ok, reason) = WeaponRules.AssignBar(st, a.Slot, a.Key);
                    result = ok ? CommandResult.Ok() : CommandResult.Refuse(reason);
                    return true;
                }
                default:
                    result = default;
                    return false;
            }
        }
    }
}
