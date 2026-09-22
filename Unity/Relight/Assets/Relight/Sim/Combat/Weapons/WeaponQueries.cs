using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// What the HUD needs to draw the weapon line (reference equipment.ts <c>equipmentLabel</c> 118-122 and
    /// weaponProfiles.ts <c>weaponRangeText</c>). A readonly struct: no allocation on the per-frame path.
    /// </summary>
    public readonly struct EquippedWeapon
    {
        /// <summary>The instance key, e.g. <c>rifle:1</c>; "" when nothing is equipped.</summary>
        public string Item { get; }
        /// <summary>The profile key, e.g. <c>rifle</c>.</summary>
        public string Kind { get; }
        public string DisplayName { get; }
        public double Loaded { get; }
        public double Capacity { get; }
        /// <summary>0 when not reloading, else 0..1 of the way through it.</summary>
        public double ReloadFraction { get; }
        public bool Reloading { get; }
        /// <summary>Rounds the Backpack can still feed it (reference "reserve rounds").</summary>
        public double Reserve { get; }
        public double EffectiveTiles { get; }
        public double MaxTiles { get; }
        /// <summary>Seconds until it may fire again.</summary>
        public double Cooldown { get; }
        public bool Owned { get; }

        public EquippedWeapon(string item, string kind, string displayName, double loaded, double capacity,
            double reloadFraction, bool reloading, double reserve, double effective, double max, double cooldown, bool owned)
        {
            Item = item; Kind = kind; DisplayName = displayName; Loaded = loaded; Capacity = capacity;
            ReloadFraction = reloadFraction; Reloading = reloading; Reserve = reserve;
            EffectiveTiles = effective; MaxTiles = max; Cooldown = cooldown; Owned = owned;
        }

        public bool Equipped => Item.Length > 0;
    }

    public static class WeaponQueries
    {
        /// <summary>The weapon in the hands and everything the HUD shows about it.</summary>
        public static EquippedWeapon Equipped(SimState st, GameData d)
        {
            var q = st.Weapons;
            var reserve = WeaponRules.Reserve(d, st.Engineer);
            var w = q.ActiveWeapon();
            if (w == null) return new EquippedWeapon("", "", "", 0, 0, 0, false, reserve, 0, 0, 0, q.Owned.Count > 0);
            d.TryWeapon(w.Kind, out var p);
            var seconds = p?.ReloadSeconds ?? 0;
            var fraction = w.Reload > 0 && seconds > 0 ? (seconds - w.Reload) / seconds : 0;
            return new EquippedWeapon(w.Id, w.Kind, PlayerNames.Weapon(d, w.Kind), w.Loaded, p?.Capacity ?? 0,
                fraction, w.Reload > 0, reserve, p?.EffectiveTiles ?? 0, p?.MaxTiles ?? 0, w.Cooldown, true);
        }

        /// <summary>Every owned weapon, in creation order, for the equipment panel.</summary>
        public static IReadOnlyList<WeaponInstance> Owned(SimState st) => st.Weapons.Owned;

        /// <summary>Seconds of the open Rifle craft and the total it needs; <c>total</c> is 0 when none is open.</summary>
        public static (double seconds, double total) Craft(SimState st, GameData d)
        {
            if (!st.Weapons.Crafting || !d.TryRecipe(WeaponRules.RifleRecipe, out var r)) return (0, 0);
            return (st.Weapons.CraftSeconds, r.Seconds);
        }

        /// <summary>
        /// Where the trigger is pointed, in tile space (reference <c>e.aim</c>, worldScene.ts:704 <c>sendAim</c>).
        /// <c>has</c> is false when the engineer is not aiming, and the view should then fall back to
        /// <see cref="EngineerView.Face"/>. Lives here rather than on <c>WorldQueries.EngineerView</c> because that
        /// record is B-13's and aiming is C-03's.
        /// </summary>
        public static (bool has, Vec2 at) Aim(SimState st) =>
            (st.Engineer.HasAim, st.Engineer.HasAim ? st.Engineer.Aim : Vec2.Zero);

        /// <summary>The action bar, ten entries, "" where a slot is empty (UI_AND_ONBOARDING §5.5).</summary>
        public static IReadOnlyList<string> Bar(SimState st)
        {
            WeaponRules.NormaliseBar(st);
            return st.Weapons.Bar;
        }
    }

    public static class ProjectileQueries
    {
        /// <summary>Bolts still flying — the presenter interpolates them with <c>SimHost.Alpha</c>.</summary>
        public static IReadOnlyList<PlayerProjectile> InFlight(SimState st) => st.Weapons.Projectiles;

        /// <summary>Hitscan tracers still inside <see cref="Ballistics.ShotTraceSeconds"/>, oldest first.</summary>
        public static IReadOnlyList<ShotTrace> Tracers(SimState st) => st.Weapons.Shots;

        /// <summary>
        /// How much of a tracer is left to draw: 1 the tick it was fired, 0 at
        /// <see cref="Ballistics.ShotTraceSeconds"/> (reference worldScene.ts fades its shot lines the same way).
        /// Saves the presenter from reading <c>SimState.T</c> itself.
        /// </summary>
        public static double Fade(SimState st, ShotTrace s)
        {
            if (s == null) return 0;
            var age = st.T - s.T;
            if (age <= 0) return 1;
            if (age >= Ballistics.ShotTraceSeconds) return 0;
            return 1 - age / Ballistics.ShotTraceSeconds;
        }
    }
}
