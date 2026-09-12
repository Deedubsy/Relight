using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Relight.Data;
using Relight.Sim;

namespace Relight.Editor
{
    /// <summary>
    /// Writes the content registry back out as Markdown tables under `Unity/Docs/generated/` (the project is `Unity/Relight/`, so two levels up from `Assets/`), so a human can diff
    /// the authored assets against CONTENT_CATALOGUE.md. This is the reversed docsync direction of S-61 / TA §5.3:
    /// in Unity the ScriptableObjects are the truth and the documents follow them.
    /// Menu: Relight/Export Data Tables, or -executeMethod Relight.Editor.DataTables.Export.
    /// </summary>
    public static class DataTables
    {
        [MenuItem("Relight/Export Data Tables")]
        public static void Export()
        {
            try
            {
                var registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(DataAssetGenerator.RegistryPath);
                if (registry == null)
                {
                    Debug.LogError($"B05-TABLES: FAILED — no registry at {DataAssetGenerator.RegistryPath}");
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                    return;
                }

                var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Docs", "generated"));
                Directory.CreateDirectory(folder);
                var written = new List<string>();

                Write(folder, "items", "Items (CONTENT_CATALOGUE.md §1)",
                    "| Key | Name | Stack | Kind | Source |", registry.Items, d =>
                    {
                        var r = d.ToRecord();
                        return $"| `{r.Key}` | {r.DisplayName} | {r.StackSize} | {r.Kind} | {Src(r.Source)} |";
                    }, written);

                Write(folder, "machines", "Machines (CONTENT_CATALOGUE.md §4)",
                    "| Key | Name | Size | Cost | kW | Inventory | Slots | Fuel cap | Ammo cap | Rate /s | Reach | Light | Cone | HP | Unlock | Kind | Source |",
                    registry.Machines, d =>
                    {
                        var r = d.ToRecord();
                        var cone = r.ConeRangeTiles > 0 ? $"{Num(r.ConeRangeTiles)} @ {Num(r.ConeHalfAngleRad)} rad" : "—";
                        return $"| `{r.Key}` | {r.DisplayName} | {r.Size} | {Stacks(r.Cost)} | {Num(r.PowerKw)} | " +
                               $"{(r.HasInventory ? "yes" : "no")} | {r.InventorySlots} | {Num(r.FuelCap)} | {r.AmmoCap} | " +
                               $"{Num(r.RatePerS)} | {Num(r.ReachTiles)} | {Num(r.LightRadiusTiles)} | {cone} | {Num(r.Hp)} | " +
                               $"{Dash(r.Unlock)} | {r.Kind} | {Src(r.Source)} |";
                    }, written);

                Write(folder, "recipes", "Recipes (CONTENT_CATALOGUE.md §3)",
                    "| Key | Name | Inputs | Outputs | Seconds | Station | Kind | Source |", registry.Recipes, d =>
                    {
                        var r = d.ToRecord();
                        var outputs = r.Outputs.Count > 0 ? Stacks(r.Outputs) : $"`{r.OutputKey}` (weapon)";
                        return $"| `{r.Key}` | {r.DisplayName} | {Stacks(r.Inputs)} | {outputs} | {Num(r.Seconds)} | " +
                               $"{r.Station} | {r.Kind} | {Src(r.Source)} |";
                    }, written);

                Write(folder, "weapons", "Weapons (CONTENT_CATALOGUE.md §5)",
                    "| Key | Name | Effective | Max | Damage | Rate /s | Pellets | Spread rad | Hit radius | Projectile | Capacity | Reload | Kind | Source |",
                    registry.Weapons, d =>
                    {
                        var r = d.ToRecord();
                        return $"| `{r.Key}` | {r.DisplayName} | {Num(r.EffectiveTiles)} | {Num(r.MaxTiles)} | {Num(r.Damage)} | " +
                               $"{Num(r.RatePerS)} | {r.Pellets} | {Num(r.SpreadRad)} | {Num(r.HitRadiusTiles)} | " +
                               $"{(r.ProjectileSpeed > 0 ? Num(r.ProjectileSpeed) : "hitscan")} | {Count(r.Capacity)} | " +
                               $"{Num(r.ReloadSeconds)} | {r.Kind} | {Src(r.Source)} |";
                    }, written);

                Write(folder, "enemies", "Enemies (CONTENT_CATALOGUE.md §7)",
                    "| Key | Name | HP | Speed | Damage | Interval | Windup | Range | Ranged | Role | Kind | Source |",
                    registry.Enemies, d =>
                    {
                        var r = d.ToRecord();
                        return $"| `{r.Key}` | {r.DisplayName} | {Num(r.Hp)} | {Num(r.SpeedTilesPerS)} | {Num(r.Damage)} | " +
                               $"{Num(r.IntervalS)} | {Num(r.WindupS)} | {Num(r.RangeTiles)} | {(r.Ranged ? "yes" : "no")} | " +
                               $"{r.Role} | {r.Kind} | {Src(r.Source)} |";
                    }, written);

                Write(folder, "ammunition", "Ammunition (CONTENT_CATALOGUE.md §6.1, Unity column)",
                    "| Key | Name | Item | Rounds per item | Stack | Items per craft | Output buffer | Kind | Source |",
                    registry.Ammunition, d =>
                    {
                        var r = d.ToRecord();
                        return $"| `{r.Key}` | {r.DisplayName} | `{Items.Key(r.Item)}` | {r.RoundsPerItem} | {r.StackSize} | " +
                               $"{r.ItemsPerCraft} | {r.OutputBuffer} | {r.Kind} | {Src(r.Source)} |";
                    }, written);

                Write(folder, "turrets", "Turrets (CONTENT_CATALOGUE.md §6.2, Unity column)",
                    "| Key | Name | Range | Rounds /s | Damage | Hopper | Upgrade × | kW | HP | Turn rad/s | Muzzle | Flash | Ammo | Kind | Source |",
                    registry.Turrets, d =>
                    {
                        var r = d.ToRecord();
                        return $"| `{r.Key}` | {r.DisplayName} | {Num(r.RangeTiles)} | {Num(r.RoundsPerS)} | {Num(r.DamagePerRound)} | " +
                               $"{r.Hopper} | {Num(r.HopperUpgradeMul)} | {Num(r.PowerKw)} | {Num(r.Hp)} | {Num(r.TurnSpeedRadPerS)} | " +
                               $"{Num(r.MuzzleTiles)} | {Num(r.ShotFlashS)} | `{Items.Key(r.Ammo)}` | {r.Kind} | {Src(r.Source)} |";
                    }, written);

                var power = registry.Power.ToRecord();
                Pairs(folder, "power", "Power (CONTENT_CATALOGUE.md §12)", power.Kind, power.Source, new[]
                {
                    P("Generator supply kW", Num(power.GeneratorKw)), P("Coal MJ", Num(power.CoalMj)),
                    P("Generator fuel cap", Num(power.GeneratorFuelCap)), P("Plant kW", Num(power.PlantKw)),
                    P("Turbine hall kW", Num(power.TurbineHallKw)), P("Core kW", Num(power.CoreKw)),
                    P("Radio kW", Num(power.RadioKw)), P("Pole reach tiles", Num(power.PoleReachTiles)),
                    P("Big pole reach tiles", Num(power.BigPoleReachTiles)), P("Substation reach tiles", Num(power.SubstationReachTiles)),
                    P("Turret kW", Num(power.TurretKw)), P("Lamp kW", Num(power.LampKw)),
                    P("Lamp radius tiles", Num(power.LampRadiusTiles)), P("Arc lamp kW", Num(power.ArcLampKw)),
                    P("Arc lamp radius tiles", Num(power.ArcLampRadiusTiles)), P("Floodlight kW", Num(power.FloodlightKw)),
                    P("Floodlight range tiles", Num(power.FloodlightRangeTiles)),
                    P("Floodlight half angle rad", Num(power.FloodlightHalfAngleRad)), P("Brownout rule", power.BrownoutRule),
                }, written);

                var time = registry.Time.ToRecord();
                Pairs(folder, "time", "Time (CONTENT_CATALOGUE.md §13)", time.Kind, time.Source, new[]
                {
                    P("Ticks per second", time.TileTps.ToString(CultureInfo.InvariantCulture)), P("Tick seconds", Num(time.TileDt)),
                    P("Day seconds", Num(time.DaySeconds)), P("Daylight seconds", Num(time.DaylightSeconds)),
                    P("Opening id", time.OpeningId), P("Camp repeat seconds", Num(time.CampRepeatSeconds)),
                    P("Decode seconds", Num(time.DecodeSeconds)),
                    P("Opening minor slot 1 (s)", Num(time.OpeningMinorSlotFirstS)),
                    P("Opening minor slot 2 (s)", Num(time.OpeningMinorSlotSecondS)),
                }, written);

                var raids = registry.Raids.ToRecord();
                Pairs(folder, "raids", "Raid director (CONTENT_CATALOGUE.md §8.1–8.3)", raids.Kind, raids.Source, new[]
                {
                    P("First assault min (s)", Num(raids.FirstMinS)), P("First assault range (s)", Num(raids.FirstRangeS)),
                    P("Interval min (s)", Num(raids.IntervalMinS)), P("Interval range (s)", Num(raids.IntervalRangeS)),
                    P("Warning (s)", Num(raids.WarningS)), P("Grace (s)", Num(raids.GraceS)),
                    P("Window (s)", Num(raids.WindowS)), P("Recovery (s)", Num(raids.RecoveryS)),
                    P("Assault roster", Count(raids.Total)), P("Active raid budget", Count(raids.ActiveRaidBudget)),
                    P("Living budget", Count(raids.LivingBudget)), P("Minor min (s)", Num(raids.MinorMinS)),
                    P("Minor range (s)", Num(raids.MinorRangeS)), P("Minor count base", Count(raids.MinorCountBase)),
                    P("Minor count range", Count(raids.MinorCountRange)), P("Minor after major (s)", Num(raids.MinorAfterMajorS)),
                    P("Major count", Count(raids.MajorCount)), P("Major skitters", Count(raids.MajorSkitters)),
                    P("Major spitters", Count(raids.MajorSpitters)), P("Major sectors", Count(raids.MajorSectors)),
                    P("Spawn every (s)", Num(raids.SpawnEveryS)), P("Contact DPS", Num(raids.ContactDps)),
                    P("Structure DPS", Num(raids.StructureDps)), P("Breaker structure ×", Num(raids.BreakerStructureMul)),
                    P("Speed tiles/s", Num(raids.SpeedTilesPerS)), P("Guard leash tiles", Num(raids.GuardLeashTiles)),
                    P("Guard notice tiles", Num(raids.GuardNoticeTiles)), P("Chase escape tiles", Num(raids.ChaseEscapeTiles)),
                    P("Patrol radius tiles", Num(raids.PatrolRadiusTiles)), P("Radio upgrade steel", Count(raids.RadioUpgradeSteel)),
                    P("Radio upgrade copper", Count(raids.RadioUpgradeCopper)),
                    P("Projectile speed tiles/s", Num(raids.ProjectileSpeedTilesPerS)), P("Projectile life (s)", Num(raids.ProjectileLifeS)),
                    P("Notice tiles", Num(raids.NoticeTiles)), P("Escape tiles", Num(raids.EscapeTiles)),
                    P("Alert radius tiles", Num(raids.AlertRadiusTiles)), P("Light hesitate (s)", Num(raids.LightHesitateS)),
                    P("Assault history", Count(raids.AssaultHistory)),
                }, written);

                var opening = registry.Opening.ToRecord();
                Pairs(folder, "opening", "Opening encounter (CONTENT_CATALOGUE.md §8.4)", opening.Kind, opening.Source, new[]
                {
                    P("Warning (s)", Num(opening.WarningS)), P("Attackers", Count(opening.Count)),
                    P("Max duration (s)", Num(opening.MaxDurationS)), P("Recovery (s)", Num(opening.RecoveryS)),
                    P("Guard (s)", Num(opening.GuardS)), P("Acknowledge (s)", Num(opening.AckS)),
                    P("Supply acknowledge (s)", Num(opening.SupplyAckS)),
                    P("Supply chain depth", Count(opening.SupplyChainDepth)), P("Turret objective", Count(opening.TurretObjective)),
                }, written);

                var stake = registry.Stake.ToRecord();
                Pairs(folder, "stake", "Starting stake (CONTENT_CATALOGUE.md §15)", stake.Kind, stake.Source, new[]
                {
                    P("Pockets", Stacks(stake.Pockets)), P("Ruleset", stake.Ruleset),
                }, written);

                var engineer = registry.Engineer.ToRecord();
                Pairs(folder, "engineer", "Engineer (CONTENT_CATALOGUE.md §14)", engineer.Kind, engineer.Source, new[]
                {
                    P("Walk tiles/s", Num(engineer.WalkTilesPerS)), P("Sprint ×", Num(engineer.SprintMul)),
                    P("Sprint drain /s", Num(engineer.SprintDrainPerS)), P("Stamina regen /s", Num(engineer.StaminaRegenPerS)),
                    P("Truck ×", Num(engineer.TruckMul)), P("Body radius tiles", Num(engineer.BodyRadiusTiles)),
                    P("Dash tiles", Num(engineer.DashTiles)), P("Dash seconds", Num(engineer.DashSeconds)),
                    P("Dash cooldown (s)", Num(engineer.DashCooldownS)), P("I-frames (s)", Num(engineer.IFramesS)),
                    P("Dash stamina cost", Num(engineer.DashCost)), P("Max HP", Num(engineer.MaxHp)),
                    P("Regen HP/s", Num(engineer.RegenPerS)), P("Regen delay (s)", Num(engineer.RegenDelayS)),
                    P("Respawn (s)", Num(engineer.RespawnS)), P("Retaliate HP/s", Num(engineer.RetaliateHpPerS)),
                    P("Inventory stacks", Count(engineer.InvStacks)), P("Truck stacks", Count(engineer.TruckStacks)),
                    P("Reach tiles", Num(engineer.ReachTiles)), P("Hand mine /s", Num(engineer.HandMinePerS)),
                    P("Hand bullet seconds", Num(engineer.HandBulletSeconds)),
                    P("Hand bullets per craft", Count(engineer.HandBulletsPerCraft)),
                    P("Hand bullet steel", Count(engineer.HandBulletSteel)), P("Hand bullet copper", Count(engineer.HandBulletCopper)),
                }, written);

                var world = registry.World.ToRecord();
                Pairs(folder, "world", "World geometry (WORLD_AND_ASSETS.md; reference tiles.ts)", world.Kind, world.Source, new[]
                {
                    P("Tile px", Count(world.TilePx)), P("Cell tiles", Count(world.CellTiles)),
                    P("Lot tiles", Count(world.LotTiles)), P("Margin tiles", Count(world.MarginTiles)),
                    P("Street tiles", Count(world.StreetTiles)), P("Depot tiles", Count(world.DepotTiles)),
                    P("Depot lot tiles", Count(world.DepotLotTiles)), P("Substation tiles", Count(world.SubstationTiles)),
                    P("Rubble units per tile", Count(world.RubbleUnitsPerTile)),
                    P("Rubble tiles min", Count(world.RubbleTilesMin)), P("Rubble tiles max", Count(world.RubbleTilesMax)),
                }, written);

                Debug.Log($"B05-TABLES: wrote {written.Count} files to {folder}\n  " + string.Join("\n  ", written));
                Debug.Log("B05-TABLES: OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"B05-TABLES: FAILED {e}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void Write<T>(string folder, string file, string title, string header,
            IReadOnlyList<T> rows, Func<T, string> line, List<string> written)
        {
            var sb = Head(title);
            sb.AppendLine(header);
            sb.AppendLine(Divider(header));
            foreach (var r in rows) if (r != null) sb.AppendLine(line(r));
            Save(folder, file, sb, written);
        }

        private static void Pairs(string folder, string file, string title, string kind, string source,
            KeyValuePair<string, string>[] rows, List<string> written)
        {
            var sb = Head(title);
            sb.AppendLine($"Kind: `{kind}`. Source: {Src(source)}");
            sb.AppendLine();
            sb.AppendLine("| Field | Value |");
            sb.AppendLine("| --- | --- |");
            foreach (var r in rows) sb.AppendLine($"| {r.Key} | {r.Value} |");
            Save(folder, file, sb, written);
        }

        private static StringBuilder Head(string title)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {title}");
            sb.AppendLine();
            sb.AppendLine("GENERATED by `Relight/Export Data Tables` from `Assets/Relight/Data/GameDataRegistry.asset`.");
            sb.AppendLine("The assets are the truth; edit them, then re-export (TECHNICAL_ARCHITECTURE.md §5.3).");
            sb.AppendLine();
            return sb;
        }

        private static string Divider(string header)
        {
            var columns = header.Split('|').Length - 2;
            var sb = new StringBuilder("|");
            for (var i = 0; i < columns; i++) sb.Append(" --- |");
            return sb.ToString();
        }

        private static void Save(string folder, string file, StringBuilder sb, List<string> written)
        {
            var path = Path.Combine(folder, file + ".md");
            File.WriteAllText(path, sb.ToString().Replace("\r\n", "\n"));
            written.Add(file + ".md");
        }

        private static KeyValuePair<string, string> P(string k, string v) => new KeyValuePair<string, string>(k, v);
        private static string Num(double v) => v.ToString("R", CultureInfo.InvariantCulture);
        private static string Count(int v) => v.ToString(CultureInfo.InvariantCulture);
        private static string Dash(string s) => string.IsNullOrEmpty(s) ? "—" : s;
        private static string Src(string s) => string.IsNullOrEmpty(s) ? "—" : "`" + s.Replace("; ", "`, `") + "`";

        private static string Stacks(IReadOnlyList<ItemStack> stacks)
        {
            if (stacks == null || stacks.Count == 0) return "—";
            var sb = new StringBuilder();
            for (var i = 0; i < stacks.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(stacks[i].Count).Append(" × ").Append(Items.Key(stacks[i].Item));
            }
            return sb.ToString();
        }
    }
}
