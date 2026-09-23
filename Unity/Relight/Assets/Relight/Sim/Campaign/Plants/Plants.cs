using System;
using System.Collections.Generic;
using System.Globalization;

namespace Relight.Sim
{
    /// <summary>Prepare the plant in reach: its squat cleared, its repair paid from carried stock (§5.7).</summary>
    public sealed record PreparePlantCommand : Command;

    /// <summary>Put the carried Power core into the prepared plant in reach, with its cost from carried stock (§5.7).</summary>
    public sealed record CommissionPlantCommand : Command;

    /// <summary>A plant was prepared.</summary>
    public sealed record PlantPreparedEvent(double T, string Plant) : SimEvent(T);

    /// <summary>
    /// A plant was commissioned and now supplies <paramref name="Kw"/> (§5.7 <c>PlantCommissionedEvent</c>): the
    /// milestone card and the event autosave.
    /// </summary>
    public sealed record PlantCommissionedEvent(double T, string Plant, string Name, double Kw, double X, double Y)
        : SimEvent(T);

    /// <summary>A raid took a commissioned plant to 0 hit points: it no longer supplies (FRT-09).</summary>
    public sealed record PlantFellEvent(double T, string Plant, string Name) : SimEvent(T);

    /// <summary>
    /// Batch 4, FRT-08 (REL-143): clearing, preparing and commissioning a power plant (FREIGHT_STRONGHOLD_DESIGN
    /// §4.4, §5.7). Riverside Works is the one plant with a row.
    ///
    /// <para>Two steps, each at the plant, each paid from the engineer's own Backpack and never from stock at Home.
    /// Prepare needs the squat born and every squatter dead, then the repair cost. Commission needs the plant
    /// prepared, the Power core in both hands and the commission cost; it takes all three. From then the plant is a
    /// supply node in the power model (<see cref="PowerGrid"/>) giving <see cref="PowerTuning.PlantKw"/> for as long
    /// as it has hit points, and it is the campaign's <see cref="EncounterState.NewestPlant"/>.</para>
    /// </summary>
    public static class Plants
    {
        public const string NoPlantText = "No power plant in reach.";
        public const string DownText = "The engineer is down.";

        public static string SquatText(PlantDef p) => "Clear the squatters out of " + p.Name + " first.";
        public static string PreparedText(PlantDef p) => p.Name + " is already prepared.";
        public static string UnpreparedText(PlantDef p) => "Prepare " + p.Name + " first.";
        public static string LitText(PlantDef p) => p.Name + " is already lit.";
        public static string CoreText(PlantDef p) => "Bring the Power core to " + p.Name + ".";
        public static string ReadyText(PlantDef p) => p.Name + " is ready for its Power core.";

        /// <summary>The milestone card (§5.7): "Riverside Works is lit · 600 kW · the north answers".</summary>
        public static string CardText(string name, double kw) =>
            name + " is lit · " + kw.ToString("0", CultureInfo.InvariantCulture) + " kW · the north answers";

        /// <summary>The kW a commissioned plant gives, from <see cref="PowerTuning.PlantKw"/> (reference 600).</summary>
        public static double Kw(GameData d) => d?.Power != null && d.Power.PlantKw > 0 ? d.Power.PlantKw : 600;

        /// <summary>A commissioned plant's hit points: the Home core's, as a starting value (U-P-43).</summary>
        public static double MaxHp(GameData d) => d?.Defence != null && d.Defence.CoreHp > 0 ? d.Defence.CoreHp : 500;

        /// <summary>The plant rows whose site is on this map and in the engineer's reach; the nearest, or null.</summary>
        public static PlantDef NearestInReach(SimContext ctx, SimState st, out SiteRecord site)
        {
            site = null;
            if (ctx?.Sites == null) return null;
            var p = st.Engineer.Pos;
            PlantDef best = null;
            var bestD = double.MaxValue;
            var rows = EncounterCatalogue.Plants;
            for (var i = 0; i < rows.Count; i++)
            {
                var s = ctx.Sites.Find(rows[i].Id);
                if (s == null || !Interaction.InReach(ctx, st, s.X, s.Y, s.W, s.H)) continue;
                var c = s.Centre;
                var d = DirectorRules.Distance(p.X, p.Y, c.X, c.Y);
                if (d < bestD) { bestD = d; best = rows[i]; site = s; }
            }
            return best;
        }

        /// <summary>Its squat has been born and every squatter is dead.</summary>
        public static bool Cleared(SimState st, PlantDef p)
        {
            if (string.IsNullOrEmpty(p.Squat)) return true;
            var rec = st.Encounters.Find(p.Squat);
            return rec != null && rec.Resolved && EncounterPhase.Living(st, p.Squat) == 0;
        }

        /// <summary>What the Backpack is short of, as "20 steel and 10 concrete"; "" when it holds the lot.</summary>
        public static string Short(GameData d, SimState st, IReadOnlyList<ItemStack> cost)
        {
            for (var i = 0; i < cost.Count; i++)
                if (st.Engineer.Inv[cost[i].Item] < cost[i].Count - 1e-9) return List(d, cost);
            return "";
        }

        static string List(GameData d, IReadOnlyList<ItemStack> cost)
        {
            var parts = new List<string>();
            for (var i = 0; i < cost.Count; i++)
                parts.Add(cost[i].Count.ToString(CultureInfo.InvariantCulture) + " " + Name(d, cost[i].Item));
            return parts.Count <= 1 ? string.Join("", parts)
                : string.Join(", ", parts.GetRange(0, parts.Count - 1)) + " and " + parts[parts.Count - 1];
        }

        static string Name(GameData d, ItemId id)
        {
            var def = d?.Item(id);
            return (def != null && !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : Items.Key(id))
                .ToLowerInvariant();
        }

        /// <summary>The cost leaves the Backpack and the game: counted as consumed, so the ledger still balances.</summary>
        static void Pay(GameData d, SimState st, IReadOnlyList<ItemStack> cost)
        {
            for (var i = 0; i < cost.Count; i++)
            {
                var gone = Pockets.Drop(d, st.Engineer, ItemKey.Of(cost[i].Item), cost[i].Count);
                st.Stats.Consumed.Add(cost[i].Item, gone);
            }
        }

        public static (bool ok, string reason) Prepare(SimContext ctx, SimState st)
        {
            if (st.Engineer.IsDown) return (false, DownText);
            var p = NearestInReach(ctx, st, out _);
            if (p == null) return (false, NoPlantText);
            var rec = st.Encounters.Plant(p.Id);
            if (rec != null && rec.Prepared) return (false, rec.Commissioned ? LitText(p) : PreparedText(p));
            if (!Cleared(st, p)) return (false, SquatText(p));
            var shortOf = Short(ctx.Data, st, p.RepairCost);
            if (shortOf.Length > 0) return (false, "Preparing " + p.Name + " needs " + shortOf + ".");
            Pay(ctx.Data, st, p.RepairCost);
            if (rec == null) st.Encounters.Plants.Add(rec = new PlantRecord { Id = p.Id });
            rec.Prepared = true;
            st.Events.Add(new PlantPreparedEvent(st.T, p.Id));
            return (true, ReadyText(p));
        }

        public static (bool ok, string reason) Commission(SimContext ctx, SimState st)
        {
            var e = st.Engineer;
            if (e.IsDown) return (false, DownText);
            var p = NearestInReach(ctx, st, out var site);
            if (p == null) return (false, NoPlantText);
            var rec = st.Encounters.Plant(p.Id);
            if (rec == null || !rec.Prepared) return (false, UnpreparedText(p));
            if (rec.Commissioned) return (false, LitText(p));
            if (string.CompareOrdinal(e.Carrying, p.Core) != 0) return (false, CoreText(p));
            var shortOf = Short(ctx.Data, st, p.Commission);
            if (shortOf.Length > 0) return (false, "Commissioning " + p.Name + " needs " + shortOf + ".");
            Pay(ctx.Data, st, p.Commission);
            e.Carrying = "";
            rec.CommissionedAt = st.T;
            rec.Hp = MaxHp(ctx.Data);
            st.Encounters.NewestPlant = p.Id;
            st.Director.PlantRaid = p.Id;              // FRT-09: the next major raid comes for it
            st.Rev++;                                  // the power model is keyed on the revision: rebuild it now
            var c = site.Centre;
            st.Events.Add(new PlantCommissionedEvent(st.T, p.Id, p.Name, Kw(ctx.Data), c.X, c.Y));
            return (true, "");
        }

        /// <summary>A commissioned plant that still has hit points.</summary>
        public static bool Standing(SimState st, string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var rec = st.Encounters?.Plant(id);
            return rec != null && rec.Commissioned && rec.Hp > 0;
        }

        /// <summary>
        /// FRT-09: a standing plant's footprint as a raid destination, in the director's terms (as
        /// <see cref="EnemyCoreHook.Rect"/> reports the Home core). False once it falls.
        /// </summary>
        public static bool Rect(SimContext ctx, SimState st, string id, out int x, out int y, out int w, out int h)
        {
            x = 0; y = 0; w = 0; h = 0;
            if (!Standing(st, id)) return false;
            var s = ctx?.Sites?.Find(id);
            if (s == null) return false;
            // REL-124: the site's real width and height, not a square on its longer side.
            x = s.X; y = s.Y; w = s.W > 0 ? s.W : 1; h = s.H > 0 ? s.H : 1;
            return true;
        }

        /// <summary>A commissioned plant that a raid has taken to 0 hit points.</summary>
        public static bool Down(SimState st, string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var rec = st.Encounters?.Plant(id);
            return rec != null && rec.Commissioned && rec.Hp <= 0;
        }

        /// <summary>
        /// A raid body's hit on a plant. At 0 it stops supplying: the revision moves so the power model is rebuilt,
        /// and <see cref="PlantFellEvent"/> tells the HUD. Nothing brings it back yet (FRT-09 builds no repair).
        /// </summary>
        public static void Damage(SimContext ctx, SimState st, string id, double amount)
        {
            if (amount <= 0 || !Standing(st, id)) return;
            var rec = st.Encounters.Plant(id);
            rec.Hp = Math.Max(0, rec.Hp - amount);
            if (rec.Hp > 0) return;
            st.Rev++;
            st.Events.Add(new PlantFellEvent(st.T, id, EncounterCatalogue.Plant(id)?.Name ?? id));
        }

        /// <summary>
        /// FRT-09 (design §5.8, U-D-73): the plant the next major raid is booked against — the newest commissioned
        /// plant while it is owed its raid and still stands — or "" for the Home core, the normal selection.
        /// </summary>
        public static string RaidAim(SimState st)
        {
            var id = st.Director?.PlantRaid ?? "";
            return Standing(st, id) ? id : "";
        }

        public static string FellText(string name) => name + " has fallen. It no longer supplies power.";

        /// <summary>The commissioned plants that supply now, with their sites on this map, in catalogue order.</summary>
        public static void Supplying(SimContext ctx, SimState st, List<SiteRecord> into)
        {
            into.Clear();
            var plants = st.Encounters?.Plants;
            if (plants == null || plants.Count == 0 || ctx?.Sites == null) return;
            var rows = EncounterCatalogue.Plants;
            for (var i = 0; i < rows.Count; i++)
            {
                var rec = st.Encounters.Plant(rows[i].Id);
                if (rec == null || !rec.Commissioned || rec.Hp <= 0) continue;
                var s = ctx.Sites.Find(rows[i].Id);
                if (s != null) into.Add(s);
            }
        }
    }

    public sealed class PlantHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            switch (c)
            {
                case PreparePlantCommand _:
                {
                    var (ok, reason) = Plants.Prepare(ctx, st);
                    result = ok ? CommandResult.Ok(reason) : CommandResult.Refuse(reason);
                    return true;
                }
                case CommissionPlantCommand _:
                {
                    var (ok, reason) = Plants.Commission(ctx, st);
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
