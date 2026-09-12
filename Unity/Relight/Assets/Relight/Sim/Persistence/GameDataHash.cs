using System.Text;

namespace Relight.Sim
{
    /// <summary>
    /// The content hash of the balance tables a save was made with (TECHNICAL_ARCHITECTURE.md §9.1's
    /// <c>dataVersion</c>). The reference needs no equivalent because its constants are compiled in; in Unity the
    /// balance data is an asset that can change under a save, so the file records which data it was made with and
    /// the loader <b>warns</b> — it never refuses, because a changed recipe cost is a balance difference, not a
    /// corrupt file.
    ///
    /// It is deliberately a one-way summary of <see cref="GameData"/> in declaration order, built with the same
    /// number and string formatting as <see cref="CanonicalJsonWriter"/> and hashed with <see cref="StateHash"/>'s
    /// FNV-1a. Adding a table to <see cref="GameData"/> means adding one block here; every existing save then
    /// reports a balance-data difference, which is the intended signal.
    /// </summary>
    public static class GameDataHash
    {
        /// <summary>Eight lowercase hex digits, or "00000000" for no data.</summary>
        public static string Compute(GameData data) => data == null ? "00000000" : StateHash.Of(Text(data));

        /// <summary>The hashed text; exposed so a test can see what changed.</summary>
        public static string Text(GameData data)
        {
            var sb = new StringBuilder();
            sb.Append("{\"items\":[");
            for (var i = 0; i < data.Items.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var it = data.Items[i];
                sb.Append('[').Append((int)it.Id).Append(',');
                Str(sb, it.Key).Append(',');
                Str(sb, it.DisplayName).Append(',').Append(it.StackSize).Append(']');
            }
            sb.Append("],\"machines\":[");
            for (var i = 0; i < data.Machines.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var m = data.Machines[i];
                sb.Append('[');
                Str(sb, m.Key).Append(',');
                Str(sb, m.DisplayName).Append(',').Append(m.Size).Append(',')
                  .Append(m.HasInventory ? '1' : '0').Append(',')
                  .Append(CanonicalJsonWriter.Num(m.PowerKw)).Append(',')
                  .Append(m.InventorySlots).Append(',')
                  .Append(CanonicalJsonWriter.Num(m.FuelCap)).Append(',')
                  .Append(m.AmmoCap).Append(',');
                Stacks(sb, m.Cost);
                sb.Append(']');
            }
            sb.Append("],\"recipes\":[");
            for (var i = 0; i < data.Recipes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var r = data.Recipes[i];
                sb.Append('[');
                Str(sb, r.Key).Append(',');
                Str(sb, r.DisplayName).Append(',');
                Str(sb, r.Station).Append(',').Append(CanonicalJsonWriter.Num(r.Seconds)).Append(',');
                Stacks(sb, r.Inputs);
                sb.Append(',');
                Stacks(sb, r.Outputs);
                sb.Append(']');
            }
            sb.Append("],\"engineer\":");
            var e = data.Engineer;
            sb.Append('[')
              .Append(CanonicalJsonWriter.Num(e.WalkTilesPerS)).Append(',').Append(CanonicalJsonWriter.Num(e.SprintMul)).Append(',')
              .Append(CanonicalJsonWriter.Num(e.SprintDrainPerS)).Append(',').Append(CanonicalJsonWriter.Num(e.StaminaRegenPerS)).Append(',')
              .Append(CanonicalJsonWriter.Num(e.DashTiles)).Append(',').Append(CanonicalJsonWriter.Num(e.DashSeconds)).Append(',')
              .Append(CanonicalJsonWriter.Num(e.DashCooldownS)).Append(',').Append(CanonicalJsonWriter.Num(e.IFramesS)).Append(',')
              .Append(CanonicalJsonWriter.Num(e.DashCost)).Append(',').Append(CanonicalJsonWriter.Num(e.MaxHp)).Append(',')
              .Append(CanonicalJsonWriter.Num(e.RegenPerS)).Append(',').Append(CanonicalJsonWriter.Num(e.RegenDelayS)).Append(',')
              .Append(CanonicalJsonWriter.Num(e.RespawnS)).Append(',').Append(e.InvStacks).Append(',').Append(e.TruckStacks).Append(',')
              .Append(CanonicalJsonWriter.Num(e.ReachTiles)).Append(',').Append(CanonicalJsonWriter.Num(e.HandMinePerS)).Append(',')
              .Append(CanonicalJsonWriter.Num(e.HandBulletSeconds)).Append(',').Append(e.HandBulletsPerCraft).Append(',')
              .Append(e.HandBulletSteel).Append(',').Append(e.HandBulletCopper).Append(',')
              .Append(CanonicalJsonWriter.Num(e.BodyRadiusTiles)).Append(']');
            sb.Append(",\"world\":");
            var w = data.World;
            sb.Append('[').Append(w.TilePx).Append(',').Append(w.CellTiles).Append(',').Append(w.LotTiles).Append(',')
              .Append(w.MarginTiles).Append(',').Append(w.DepotTiles).Append(',').Append(w.SubstationTiles).Append(',')
              .Append(w.RubbleUnitsPerTile).Append(',').Append(w.RubbleTilesMin).Append(',').Append(w.RubbleTilesMax).Append(']');
            // B-05's tables (coordinator, 2026-09-12): one block each, so a balance change to any of them changes
            // the data hash. Emitted generically from the record's public properties, sorted by name so the text does
            // not depend on reflection order; the same Num/QuoteString formatting as above.
            Table(sb, "weapons", data.Weapons);
            Table(sb, "enemies", data.Enemies);
            Table(sb, "ammunition", data.Ammunition);
            Table(sb, "turrets", data.Turrets);
            Record(sb, "power", data.Power);
            Record(sb, "time", data.Time);
            Record(sb, "raids", data.Raids);
            Record(sb, "opening", data.Opening);
            Record(sb, "stake", data.Stake);
            sb.Append('}');
            return sb.ToString();
        }

        private static void Table<T>(StringBuilder sb, string name, System.Collections.Generic.IReadOnlyList<T> rows)
        {
            sb.Append(",\"").Append(name).Append("\":[");
            if (rows != null)
                for (var i = 0; i < rows.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    Fields(sb, rows[i]);
                }
            sb.Append(']');
        }

        private static void Record(StringBuilder sb, string name, object record)
        {
            sb.Append(",\"").Append(name).Append("\":");
            if (record == null) sb.Append("null"); else Fields(sb, record);
        }

        private static void Fields(StringBuilder sb, object record)
        {
            var props = record.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            System.Array.Sort(props, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            sb.Append('[');
            var first = true;
            foreach (var p in props)
            {
                if (p.GetIndexParameters().Length != 0 || p.Name == "EqualityContract") continue;
                if (!first) sb.Append(',');
                first = false;
                var v = p.GetValue(record);
                switch (v)
                {
                    case null: sb.Append("null"); break;
                    case string str: Str(sb, str); break;
                    case bool b: sb.Append(b ? '1' : '0'); break;
                    case double d: sb.Append(CanonicalJsonWriter.Num(d)); break;
                    case int n: sb.Append(n); break;
                    case ItemId id: sb.Append((int)id); break;
                    case System.Collections.Generic.IReadOnlyList<ItemStack> stacks: Stacks(sb, stacks); break;
                    default: Str(sb, System.Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)); break;
                }
            }
            sb.Append(']');
        }

        private static StringBuilder Str(StringBuilder sb, string s)
            => sb.Append(s == null ? "null" : CanonicalJsonWriter.QuoteString(s));

        private static void Stacks(StringBuilder sb, System.Collections.Generic.IReadOnlyList<ItemStack> stacks)
        {
            sb.Append('[');
            if (stacks != null)
                for (var i = 0; i < stacks.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('[').Append((int)stacks[i].Item).Append(',').Append(stacks[i].Count).Append(']');
                }
            sb.Append(']');
        }
    }
}
