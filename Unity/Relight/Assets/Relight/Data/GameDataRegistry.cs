using System.Collections.Generic;
using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>
    /// The single authoring root for balance data (TA §5.2): it lists every definition asset and converts them,
    /// once at boot, into the engine-independent <see cref="GameData"/> the simulation uses. The sim never sees a
    /// ScriptableObject (U-M-25), so this class is the only place the two worlds meet.
    /// </summary>
    [CreateAssetMenu(menuName = "Relight/Game Data Registry", fileName = "GameDataRegistry")]
    public sealed class GameDataRegistry : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] items = new ItemDefinition[0];
        [SerializeField] private MachineDefinition[] machines = new MachineDefinition[0];
        [SerializeField] private RecipeDefinition[] recipes = new RecipeDefinition[0];
        [SerializeField] private WeaponDefinition[] weapons = new WeaponDefinition[0];
        [SerializeField] private EnemyDefinition[] enemies = new EnemyDefinition[0];
        [SerializeField] private AmmoDefinition[] ammunition = new AmmoDefinition[0];
        [SerializeField] private TurretDefinition[] turrets = new TurretDefinition[0];

        [SerializeField] private PowerTuningAsset power;
        [SerializeField] private TimeTuningAsset time;
        [SerializeField] private RaidDirectorTuningAsset raids;
        [SerializeField] private OpeningEncounterTuningAsset opening;
        [SerializeField] private StartingStakeAsset stake;
        [SerializeField] private DefenceTuningAsset defence;
        [SerializeField] private EngineerTuningAsset engineer;
        [SerializeField] private WorldTuningAsset world;
        [SerializeField] private SiegeTuningAsset siege;

        public IReadOnlyList<ItemDefinition> Items => items;
        public IReadOnlyList<MachineDefinition> Machines => machines;
        public IReadOnlyList<RecipeDefinition> Recipes => recipes;
        public IReadOnlyList<WeaponDefinition> Weapons => weapons;
        public IReadOnlyList<EnemyDefinition> Enemies => enemies;
        public IReadOnlyList<AmmoDefinition> Ammunition => ammunition;
        public IReadOnlyList<TurretDefinition> Turrets => turrets;
        public PowerTuningAsset Power => power;
        public TimeTuningAsset Time => time;
        public RaidDirectorTuningAsset Raids => raids;
        public OpeningEncounterTuningAsset Opening => opening;
        public StartingStakeAsset Stake => stake;
        public DefenceTuningAsset Defence => defence;
        public EngineerTuningAsset Engineer => engineer;
        public WorldTuningAsset World => world;
        public SiegeTuningAsset Siege => siege;

        /// <summary>Used by the editor generator. Authoring is otherwise done in the Inspector.</summary>
        public void SetContents(ItemDefinition[] newItems, MachineDefinition[] newMachines, RecipeDefinition[] newRecipes,
            WeaponDefinition[] newWeapons, EnemyDefinition[] newEnemies, AmmoDefinition[] newAmmunition,
            TurretDefinition[] newTurrets, PowerTuningAsset newPower, TimeTuningAsset newTime,
            RaidDirectorTuningAsset newRaids, OpeningEncounterTuningAsset newOpening, StartingStakeAsset newStake, DefenceTuningAsset newDefence,
            EngineerTuningAsset newEngineer, WorldTuningAsset newWorld, SiegeTuningAsset newSiege)
        {
            items = newItems; machines = newMachines; recipes = newRecipes; weapons = newWeapons;
            enemies = newEnemies; ammunition = newAmmunition; turrets = newTurrets;
            power = newPower; time = newTime; raids = newRaids; opening = newOpening; stake = newStake; defence = newDefence;
            engineer = newEngineer; world = newWorld; siege = newSiege;
        }

        /// <summary>Every definition asset, in family order, for the validator and the table exporter.</summary>
        public IEnumerable<DataDefinition> All()
        {
            foreach (var d in items) yield return d;
            foreach (var d in machines) yield return d;
            foreach (var d in recipes) yield return d;
            foreach (var d in weapons) yield return d;
            foreach (var d in enemies) yield return d;
            foreach (var d in ammunition) yield return d;
            foreach (var d in turrets) yield return d;
            yield return power;
            yield return time;
            yield return raids;
            yield return opening;
            yield return stake;
            yield return defence;
            yield return engineer;
            yield return world;
            yield return siege;
        }

        /// <summary>Converts the whole registry into the simulation's data record. Call this once, at boot.</summary>
        public GameData Build() =>
            DarkWorld.Apply(CombatBalance.Apply(OpeningBalance.Apply(GridBalance.Apply(BuildOriginal()))));

        /// <summary>
        /// The data a save from before the opening resource layout loads with: the original balance, but still no
        /// sun (U-D-58). <see cref="GridBalance"/> is balance, so it is deliberately absent — a save laid out under
        /// eight-tile poles keeps the grid it was built for (REL-128).
        /// </summary>
        public GameData BuildLegacy() => DarkWorld.Apply(BuildOriginal());

        public GameData BuildOriginal()
        {
            var itemDefs = new ItemDef[items.Length];
            for (var i = 0; i < items.Length; i++) itemDefs[i] = items[i].ToRecord();
            var machineDefs = new MachineSpec[machines.Length];
            for (var i = 0; i < machines.Length; i++) machineDefs[i] = machines[i].ToRecord();
            var recipeDefs = new Recipe[recipes.Length];
            for (var i = 0; i < recipes.Length; i++) recipeDefs[i] = recipes[i].ToRecord();
            var weaponDefs = new WeaponDef[weapons.Length];
            for (var i = 0; i < weapons.Length; i++) weaponDefs[i] = weapons[i].ToRecord();
            var enemyDefs = new EnemyDef[enemies.Length];
            for (var i = 0; i < enemies.Length; i++) enemyDefs[i] = enemies[i].ToRecord();
            var ammoDefs = new AmmoDef[ammunition.Length];
            for (var i = 0; i < ammunition.Length; i++) ammoDefs[i] = ammunition[i].ToRecord();
            var turretDefs = new TurretDef[turrets.Length];
            for (var i = 0; i < turrets.Length; i++) turretDefs[i] = turrets[i].ToRecord();

            return new GameData(itemDefs, machineDefs, recipeDefs,
                engineer != null ? engineer.ToRecord() : null,
                world != null ? world.ToRecord() : null,
                weaponDefs, enemyDefs, ammoDefs, turretDefs,
                power != null ? power.ToRecord() : null,
                time != null ? time.ToRecord() : null,
                raids != null ? raids.ToRecord() : null,
                opening != null ? opening.ToRecord() : null,
                stake != null ? stake.ToRecord() : null,
                defence != null ? defence.ToRecord() : null,
                siege != null ? siege.ToRecord() : null);
        }
    }
}
