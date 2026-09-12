using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// Immutable balance and content data the sim reads (TECHNICAL_ARCHITECTURE.md §4.4). Built once at boot by
    /// Relight.Data from ScriptableObjects (B-05) and never mutated afterwards. Plain records only: no engine types.
    /// Every record carries the reference source it was taken from in <c>Source</c> and the catalogue's
    /// <c>Kind</c> (`current` / `approved` / `provisional`, CONTENT_CATALOGUE.md §17.4).
    /// </summary>
    public sealed class GameData
    {
        public IReadOnlyList<ItemDef> Items { get; }
        public IReadOnlyList<MachineSpec> Machines { get; }
        public IReadOnlyList<Recipe> Recipes { get; }
        public EngineerTuning Engineer { get; }
        public WorldTuning World { get; }

        /// <summary>Player weapon profiles (CONTENT_CATALOGUE.md §5).</summary>
        public IReadOnlyList<WeaponDef> Weapons { get; }
        /// <summary>The regular enemy types the opening and the early raids use (CONTENT_CATALOGUE.md §7.1/§7.4).</summary>
        public IReadOnlyList<EnemyDef> Enemies { get; }
        /// <summary>Ammunition kinds (CONTENT_CATALOGUE.md §6.1).</summary>
        public IReadOnlyList<AmmoDef> Ammunition { get; }
        /// <summary>Turret profiles, Unity/campaign column only (CONTENT_CATALOGUE.md §6.2).</summary>
        public IReadOnlyList<TurretDef> Turrets { get; }
        /// <summary>Power constants (CONTENT_CATALOGUE.md §12).</summary>
        public PowerTuning Power { get; }
        /// <summary>Time constants (CONTENT_CATALOGUE.md §13).</summary>
        public TimeTuning Time { get; }
        /// <summary>The raid director (CONTENT_CATALOGUE.md §8.1/§8.2).</summary>
        public RaidTuning Raids { get; }
        /// <summary>The introductory encounter (CONTENT_CATALOGUE.md §8.4).</summary>
        public OpeningEncounterTuning Opening { get; }
        /// <summary>The campaign starting stake (CONTENT_CATALOGUE.md §15).</summary>
        public StartingStake Stake { get; }

        private readonly ItemDef[] _byItem = new ItemDef[Sim.Items.Count];
        private readonly Dictionary<string, MachineSpec> _machineByKey = new Dictionary<string, MachineSpec>(StringComparer.Ordinal);
        private readonly Dictionary<string, Recipe> _recipeByKey = new Dictionary<string, Recipe>(StringComparer.Ordinal);
        private readonly Dictionary<string, WeaponDef> _weaponByKey = new Dictionary<string, WeaponDef>(StringComparer.Ordinal);
        private readonly Dictionary<string, EnemyDef> _enemyByKey = new Dictionary<string, EnemyDef>(StringComparer.Ordinal);
        private readonly Dictionary<string, TurretDef> _turretByKey = new Dictionary<string, TurretDef>(StringComparer.Ordinal);

        private static readonly ItemStack[] NoStacks = new ItemStack[0];
        private static readonly WeaponDef[] NoWeapons = new WeaponDef[0];
        private static readonly EnemyDef[] NoEnemies = new EnemyDef[0];
        private static readonly AmmoDef[] NoAmmo = new AmmoDef[0];
        private static readonly TurretDef[] NoTurrets = new TurretDef[0];

        /// <summary>
        /// The extra tables are optional so that a fixture may build only the foundation tables; the generated
        /// <see cref="CatalogueData.Build"/> and <c>GameDataRegistry.Build()</c> always pass all of them.
        /// </summary>
        public GameData(IReadOnlyList<ItemDef> items, IReadOnlyList<MachineSpec> machines, IReadOnlyList<Recipe> recipes,
            EngineerTuning engineer, WorldTuning world,
            IReadOnlyList<WeaponDef> weapons = null, IReadOnlyList<EnemyDef> enemies = null,
            IReadOnlyList<AmmoDef> ammunition = null, IReadOnlyList<TurretDef> turrets = null,
            PowerTuning power = null, TimeTuning time = null, RaidTuning raids = null,
            OpeningEncounterTuning opening = null, StartingStake stake = null)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Machines = machines ?? throw new ArgumentNullException(nameof(machines));
            Recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
            Engineer = engineer ?? throw new ArgumentNullException(nameof(engineer));
            World = world ?? throw new ArgumentNullException(nameof(world));
            Weapons = weapons ?? NoWeapons;
            Enemies = enemies ?? NoEnemies;
            Ammunition = ammunition ?? NoAmmo;
            Turrets = turrets ?? NoTurrets;
            Power = power;
            Time = time;
            Raids = raids;
            Opening = opening;
            Stake = stake;
            foreach (var it in items) _byItem[(int)it.Id] = it;
            for (var i = 0; i < _byItem.Length; i++)
                if (_byItem[i] == null) throw new InvalidOperationException($"GameData is missing item '{Sim.Items.Key((ItemId)i)}'");
            foreach (var m in machines) _machineByKey[m.Key] = m;
            foreach (var r in recipes) _recipeByKey[r.Key] = r;
            foreach (var w in Weapons) _weaponByKey[w.Key] = w;
            foreach (var e in Enemies) _enemyByKey[e.Key] = e;
            foreach (var t in Turrets) _turretByKey[t.Key] = t;
        }

        public ItemDef Item(ItemId id) => _byItem[(int)id];
        /// <summary>Stack size for any pocket key (reference engineer.ts `stackSize`): items from data, weapons and packed machines 1.</summary>
        public int StackSize(ItemKey key) => key.IsItem(out var id) ? _byItem[(int)id].StackSize : 1;
        public bool TryMachine(string key, out MachineSpec spec) => _machineByKey.TryGetValue(key, out spec);
        public bool TryRecipe(string key, out Recipe recipe) => _recipeByKey.TryGetValue(key, out recipe);
        public bool TryWeapon(string key, out WeaponDef weapon) => _weaponByKey.TryGetValue(key, out weapon);
        public bool TryEnemy(string key, out EnemyDef enemy) => _enemyByKey.TryGetValue(key, out enemy);
        public bool TryTurret(string key, out TurretDef turret) => _turretByKey.TryGetValue(key, out turret);
    }

    /// <summary>One retained item (CONTENT_CATALOGUE.md §1; reference flow.ts ITEMS, engineer.ts STACK, itemNames.ts).</summary>
    public sealed record ItemDef(ItemId Id, string Key, string DisplayName, int StackSize,
        string Kind = "current", string Source = "", bool Provisional = false);

    /// <summary>
    /// A recipe (CONTENT_CATALOGUE.md §3; reference recipes.ts RECIPES / flow.ts ASSEMBLER_RECIPES and hand crafting).
    /// Quantities are per batch and <c>Seconds</c> is the unmodified craft time. <c>OutputKey</c> is set only for a
    /// craft whose product is not an <see cref="ItemId"/> — today just the Rifle, whose product is a weapon instance.
    /// </summary>
    public sealed record Recipe(string Key, string DisplayName, IReadOnlyList<ItemStack> Inputs, IReadOnlyList<ItemStack> Outputs,
        double Seconds, string Station,
        string OutputKey = "", string Kind = "current", string Source = "", bool Provisional = false);

    public readonly struct ItemStack
    {
        public ItemId Item { get; }
        public int Count { get; }
        public ItemStack(ItemId item, int count) { Item = item; Count = count; }
        public override string ToString() => $"{Count}x{Items.Key(Item)}";
    }

    /// <summary>
    /// A placeable machine kind (CONTENT_CATALOGUE.md §4; reference flow.ts MACHINE_SIZE / MACHINE_COST / MACHINE_KW).
    /// Size is the square footprint in tiles; splitters are the only non-square kind and are handled by
    /// <c>Footprints.Dimensions</c>. <c>PowerKw</c> is negative for a supply (the Generator).
    /// The trailing fields carry the rest of the §4 row so one record covers the whole table; a kind that has no
    /// value for a field leaves it 0 / empty and the validator does not require it.
    /// </summary>
    public sealed record MachineSpec(string Key, string DisplayName, int Size, IReadOnlyList<ItemStack> Cost,
        bool HasInventory, double PowerKw, int InventorySlots,
        /// <summary>Fuel item capacity (reference flow.ts GENERATOR_COAL_CAP 50); 0 for kinds that burn nothing.</summary>
        double FuelCap = 0,
        /// <summary>Ammunition hopper capacity in rounds (reference recipes.ts TURRET_HOPPER 50); 0 for non-weapons.</summary>
        int AmmoCap = 0,
        /// <summary>Throughput in items/s for a belt, drill or inserter; 0 when the kind has no rate row.</summary>
        double RatePerS = 0,
        /// <summary>Power-network reach in tiles for a pole, big pole or substation; 0 otherwise.</summary>
        double ReachTiles = 0,
        /// <summary>Lit radius in tiles for a lamp or arc lamp; 0 otherwise.</summary>
        double LightRadiusTiles = 0,
        /// <summary>Floodlight cone range in tiles; 0 otherwise.</summary>
        double ConeRangeTiles = 0,
        /// <summary>Floodlight cone half-angle in radians (π/6 = a 60° cone); 0 otherwise.</summary>
        double ConeHalfAngleRad = 0,
        /// <summary>Structure hit points (CONTENT_CATALOGUE.md §10 "Structure HP"); 0 when the kind has no HP row.</summary>
        double Hp = 0,
        /// <summary>What gates the kind ("", "Arsenal", "Electricians", …); free text from the §4 Unlock column.</summary>
        string Unlock = "",
        string Kind = "current", string Source = "", bool Provisional = false);

    /// <summary>
    /// A player weapon profile (CONTENT_CATALOGUE.md §5; reference weaponProfiles.ts WEAPON_PROFILES).
    /// Damage falls off linearly between <c>EffectiveTiles</c> and <c>MaxTiles</c> and is 0 beyond it.
    /// <c>ProjectileSpeed</c> 0 means hitscan. <c>Capacity</c>/<c>ReloadSeconds</c> are 0 where the catalogue
    /// states none (only the Rifle has a loaded-capacity row).
    /// </summary>
    public sealed record WeaponDef(string Key, string DisplayName,
        double EffectiveTiles, double MaxTiles, double Damage, double RatePerS,
        int Pellets, double SpreadRad, double HitRadiusTiles, double ProjectileSpeed,
        int Capacity, double ReloadSeconds,
        string Kind = "provisional", string Source = "", bool Provisional = true);

    /// <summary>
    /// A regular enemy type (CONTENT_CATALOGUE.md §7.1 live values, §7.4 approved roles; reference gameplayCombat.ts
    /// GP_COMBAT). Guardians (§9.2) and the unimplemented Stalker / Breaker / Howler are extension points, not rows.
    /// </summary>
    public sealed record EnemyDef(string Key, string DisplayName,
        double Hp, double SpeedTilesPerS, double Damage, double IntervalS, double WindupS, double RangeTiles,
        bool Ranged, string Role,
        string Kind = "provisional", string Source = "", bool Provisional = true);

    /// <summary>Ammunition (CONTENT_CATALOGUE.md §6.1). One item is <c>RoundsPerItem</c> rounds — always 1 in the port (U-D-08).</summary>
    public sealed record AmmoDef(string Key, string DisplayName, ItemId Item,
        int RoundsPerItem, int StackSize, int ItemsPerCraft, int OutputBuffer,
        string Kind = "current", string Source = "", bool Provisional = false);

    /// <summary>A turret profile, campaign column only (CONTENT_CATALOGUE.md §6.2). The Legacy column is excluded by §17.2.</summary>
    public sealed record TurretDef(string Key, string DisplayName,
        double RangeTiles, double RoundsPerS, double DamagePerRound, int Hopper, double HopperUpgradeMul,
        double PowerKw, double Hp, double TurnSpeedRadPerS, double MuzzleTiles, double ShotFlashS, ItemId Ammo,
        string Kind = "provisional", string Source = "", bool Provisional = true);

    /// <summary>Power constants (CONTENT_CATALOGUE.md §12). SUBSTATION_KW is excluded by §17.2.</summary>
    public sealed record PowerTuning(
        double GeneratorKw, double CoalMj, double GeneratorFuelCap,
        double PlantKw, double TurbineHallKw, double CoreKw, double RadioKw,
        double PoleReachTiles, double BigPoleReachTiles, double SubstationReachTiles,
        double TurretKw, double LampKw, double LampRadiusTiles, double ArcLampKw, double ArcLampRadiusTiles,
        double FloodlightKw, double FloodlightRangeTiles, double FloodlightHalfAngleRad,
        string BrownoutRule,
        string Kind = "current", string Source = "", bool Provisional = false);

    /// <summary>Time constants (CONTENT_CATALOGUE.md §13). firstAssaultNight and HOUR_MINUTES are excluded by §17.2.</summary>
    public sealed record TimeTuning(
        int TileTps, double TileDt, double DaySeconds, double DaylightSeconds, string OpeningId,
        double CampRepeatSeconds, double DecodeSeconds, double OpeningMinorSlotFirstS, double OpeningMinorSlotSecondS,
        string Kind = "current", string Source = "", bool Provisional = false);

    /// <summary>
    /// The raid director (CONTENT_CATALOGUE.md §8.1 ACTIVE_RAIDS and §8.2 CAMPAIGN_THREAT) plus the §7.1 shared
    /// actor constants. The superseded 8/12 minor band is not carried: the live band is
    /// <c>MinorCountBase + raidChoice(seed, serial, MinorCountRange)</c> = 6–10 (§16 row 7).
    /// </summary>
    public sealed record RaidTuning(
        double FirstMinS, double FirstRangeS, double IntervalMinS, double IntervalRangeS,
        double WarningS, double GraceS, double WindowS, double RecoveryS,
        int Total, int ActiveRaidBudget, int LivingBudget,
        double MinorMinS, double MinorRangeS, int MinorCountBase, int MinorCountRange,
        int MajorCount, int MajorSkitters, int MajorSpitters, int MajorSectors,
        double SpawnEveryS, double ContactDps, double StructureDps, double BreakerStructureMul,
        double SpeedTilesPerS, double MinorAfterMajorS,
        double GuardLeashTiles, double GuardNoticeTiles, double ChaseEscapeTiles, double PatrolRadiusTiles,
        int RadioUpgradeSteel, int RadioUpgradeCopper,
        double ProjectileSpeedTilesPerS, double ProjectileLifeS, double NoticeTiles, double EscapeTiles,
        double AlertRadiusTiles, double LightHesitateS, int AssaultHistory,
        string Kind = "current", string Source = "", bool Provisional = false);

    /// <summary>The introductory encounter (CONTENT_CATALOGUE.md §8.4; reference openingEncounter.ts).</summary>
    public sealed record OpeningEncounterTuning(
        double WarningS, int Count, double MaxDurationS, double RecoveryS, double GuardS, double AckS, double SupplyAckS,
        int SupplyChainDepth, int TurretObjective,
        string Kind = "provisional", string Source = "", bool Provisional = true);

    /// <summary>The campaign starting stake (CONTENT_CATALOGUE.md §15; reference rules.ts CAMPAIGN_START_POCKETS). Home storage is empty.</summary>
    public sealed record StartingStake(IReadOnlyList<ItemStack> Pockets, string Ruleset,
        string Kind = "approved", string Source = "", bool Provisional = false);

    /// <summary>Engineer body, pocket and hand constants (CONTENT_CATALOGUE.md §14; reference engineer.ts and flow.ts hand section).</summary>
    public sealed record EngineerTuning(
        double WalkTilesPerS, double SprintMul, double SprintDrainPerS, double StaminaRegenPerS,
        double DashTiles, double DashSeconds, double DashCooldownS, double IFramesS, double DashCost,
        double MaxHp, double RegenPerS, double RegenDelayS, double RespawnS,
        int InvStacks, int TruckStacks, double ReachTiles,
        double HandMinePerS, double HandBulletSeconds, int HandBulletsPerCraft, int HandBulletSteel, int HandBulletCopper,
        double BodyRadiusTiles,
        double TruckMul = 3, double RetaliateHpPerS = 5,
        string Kind = "current", string Source = "", bool Provisional = false);

    /// <summary>World geometry constants (reference tiles.ts): tile pixel size and the standard lot sizes.</summary>
    public sealed record WorldTuning(int TilePx, int CellTiles, int LotTiles, int MarginTiles, int DepotTiles, int SubstationTiles,
        int RubbleUnitsPerTile, int RubbleTilesMin, int RubbleTilesMax,
        int StreetTiles = 8, int DepotLotTiles = 9,
        string Kind = "current", string Source = "", bool Provisional = false);
}
