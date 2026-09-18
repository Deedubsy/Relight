using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// One owned weapon instance (reference equipment.ts:10 <c>Weapon</c>, keyed in <c>q.weapons</c> by its
    /// <c>WeaponItem</c> id such as <c>rifle:3</c>). The port keeps the id ON the instance and the instances in an
    /// ordered list, never a dictionary, so the save order and the tick order are the same in every run (TA §10.5).
    /// </summary>
    public sealed class WeaponInstance : IVisitable
    {
        /// <summary>The item key that identifies this instance in the pockets: <c>"rifle:3"</c>.</summary>
        public string Id = "";
        /// <summary>The profile key in <see cref="GameData.Weapons"/>: <c>"rifle"</c>.</summary>
        public string Kind = "";
        /// <summary>Rounds in the magazine (reference <c>w.loaded</c>). One item is one round (U-D-08).</summary>
        public double Loaded;
        /// <summary>Seconds until it may fire again (reference <c>w.cooldown</c>).</summary>
        public double Cooldown;
        /// <summary>Seconds left of a reload (reference <c>w.reload</c>); 0 when not reloading.</summary>
        public double Reload;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("kind", ref Kind);
            v.Field("loaded", ref Loaded);
            v.Field("cooldown", ref Cooldown);
            v.Field("reload", ref Reload);
        }
    }

    /// <summary>
    /// A bolt in flight (reference playerBallistics.ts:9 <c>PlayerProjectile</c>). Only weapons whose
    /// <see cref="WeaponDef.ProjectileSpeed"/> is above zero make one; the rifle is hitscan and makes none.
    /// Visited, so a save taken mid-flight resumes with the bolt where it was (C-03 acceptance).
    /// </summary>
    public sealed class PlayerProjectile : IVisitable
    {
        /// <summary>The weapon profile key that fired it (reference <c>p.kind</c>).</summary>
        public string Kind = "";
        public Vec2 Pos;
        /// <summary>Unit direction (reference <c>p.ux, p.uy</c>).</summary>
        public Vec2 Dir;
        /// <summary>Tiles travelled so far (reference <c>p.distance</c>); it expires at the profile's max range.</summary>
        public double Distance;
        /// <summary>Where it was fired from (reference <c>p.origin</c>), passed to the damage hand-off.</summary>
        public Vec2 Origin;
        /// <summary>0 = the engineer. Reserved so C-04's turrets can share the list without a second one.</summary>
        public int Owner;

        public void Visit(IStateVisitor v)
        {
            v.Field("kind", ref Kind);
            v.Field("pos", ref Pos);
            v.Field("dir", ref Dir);
            v.Field("distance", ref Distance);
            v.Field("origin", ref Origin);
            v.Field("owner", ref Owner);
        }
    }

    /// <summary>
    /// One resolved hitscan shot for the renderer (reference playerBallistics.ts:11 <c>PlayerShot</c>): muzzle, the
    /// point the ray stopped at, the sim time and whether it hit. The reference keeps these on the transient threat
    /// state; the port visits them so a save/load does not make tracers vanish mid-flight, which would otherwise be
    /// the one observable difference between a restart and a continue (U-D-15 "visible shots").
    /// </summary>
    public sealed class ShotTrace : IVisitable
    {
        public Vec2 From;
        public Vec2 To;
        public double T;
        public bool Hit;

        public void Visit(IStateVisitor v)
        {
            v.Field("from", ref From);
            v.Field("to", ref To);
            v.Field("t", ref T);
            v.Field("hit", ref Hit);
        }
    }

    /// <summary>
    /// The player's weapons (contracts.md §State: the top-level <c>weapons</c> object).
    /// Ported from reference equipment.ts <c>Equipment</c> (11-15) plus the player half of threat.ts's transient
    /// ballistics (<c>T.playerProjectiles</c>, <c>T.playerShots</c>).
    ///
    /// <c>new WeaponState()</c> means "nothing has happened yet": no weapon owned, nothing equipped, nothing in
    /// flight — exactly what a fresh campaign gets (reference <c>initEquipment(st, true)</c>, equipment.ts:29-30,
    /// which creates the record with empty slots and hands out no weapon).
    /// </summary>
    public sealed class WeaponState : IVisitable
    {
        /// <summary>The serial the next created weapon takes (reference <c>q.next</c>, from 1).</summary>
        public int Next = 1;
        /// <summary>Every weapon the engineer owns, equipped or carried, in creation order (reference <c>q.weapons</c>).</summary>
        public List<WeaponInstance> Owned = new List<WeaponInstance>();
        /// <summary>Equipment slot 1 — a weapon id, or null when empty (reference <c>q.slots[0]</c>).</summary>
        public string Slot0;
        /// <summary>Equipment slot 2 (reference <c>q.slots[1]</c>).</summary>
        public string Slot1;
        /// <summary>Which slot is in the hands, 0 or 1 (reference <c>q.active</c>).</summary>
        public int Active;
        /// <summary>Seconds of the Home-workshop rifle craft done so far (reference <c>q.craft.seconds</c>).</summary>
        public double CraftSeconds;
        /// <summary>A rifle craft is open (reference <c>q.craft !== undefined</c>).</summary>
        public bool Crafting;
        /// <summary>
        /// Steel a cancelled Rifle craft owes the engineer that the Backpack could not take yet, and its copper
        /// counterpart. The reference spills the overflow into the retired Depot stock (equipment.ts:90); here it is
        /// an explicit, recoverable debt drained by <see cref="WeaponRules.Drain"/>, and <c>stats.consumed</c> is
        /// un-counted only as it is actually paid, so the ledger never sees a plate that is nowhere.
        /// </summary>
        public double CraftRefundSteel;
        public double CraftRefundCopper;
        /// <summary>
        /// The trigger is held (the port's state for reference worldScene.ts:505's held mouse button).
        /// Where it is pointed is <see cref="Engineer.Aim"/> — the field the body already faces along — so the aim
        /// has one owner and the view needs no second one.
        /// </summary>
        public bool Firing;
        public List<PlayerProjectile> Projectiles = new List<PlayerProjectile>();
        /// <summary>Recent hitscan shots, newest last, capped at 24 (reference <c>noteShot</c>, playerBallistics.ts:14).</summary>
        public List<ShotTrace> Shots = new List<ShotTrace>();
        /// <summary>
        /// The ten action-bar slots, keys 1..0 (UI_AND_ONBOARDING §5.5). "" is an empty slot. A tool never occupies
        /// two slots: <see cref="WeaponRules.AssignBar"/> swaps rather than duplicating, and a loaded arrangement
        /// with a duplicate collapses to the first occurrence (§5.5).
        /// </summary>
        public List<string> Bar = new List<string>();

        /// <summary>
        /// Who the bolts and rays may hit. NOT visited and never saved: C-08 owns the bodies, sets this seam on the
        /// state it drives, and a load leaves it null until C-08 sets it again (contracts.md §State rule 3).
        /// Null means "no bodies": rays then stop at geometry and bolts expire at their range.
        /// </summary>
        public IProjectileTargets Targets;

        public string SlotAt(int i) => i == 0 ? Slot0 : i == 1 ? Slot1 : null;

        public void SetSlot(int i, string id)
        {
            if (i == 0) Slot0 = id;
            else if (i == 1) Slot1 = id;
        }

        /// <summary>The weapon in the hands, or null (reference equipment.ts:19 <c>activeWeapon</c>).</summary>
        public WeaponInstance ActiveWeapon() => Find(SlotAt(Active));

        public WeaponInstance Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (var i = 0; i < Owned.Count; i++)
                if (string.Equals(Owned[i].Id, id, StringComparison.Ordinal)) return Owned[i];
            return null;
        }

        public void Visit(IStateVisitor v)
        {
            v.Field("next", ref Next);
            v.List("owned", Owned, () => new WeaponInstance());
            v.Field("slot0", ref Slot0);
            v.Field("slot1", ref Slot1);
            v.Field("active", ref Active);
            v.Field("craftSeconds", ref CraftSeconds);
            v.Field("crafting", ref Crafting);
            v.Field("craftRefundSteel", ref CraftRefundSteel);
            v.Field("craftRefundCopper", ref CraftRefundCopper);
            v.Field("firing", ref Firing);
            v.List("projectiles", Projectiles, () => new PlayerProjectile());
            v.List("shots", Shots, () => new ShotTrace());
            v.StringList("bar", Bar);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>The player's weapons, ammunition and shots (C-02 / C-03).</summary>
        public WeaponState Weapons = new WeaponState();

        partial void VisitWeapons(IStateVisitor v)
        {
            v.Object("weapons", ref Weapons, () => new WeaponState());
        }
    }
}
