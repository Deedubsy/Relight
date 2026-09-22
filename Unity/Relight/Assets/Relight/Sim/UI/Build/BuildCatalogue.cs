using System;
using System.Collections.Generic;
using System.Globalization;

namespace Relight.Sim.UI
{
    /// <summary>The four tabs of the build menu (reference <c>buildCatalogue.ts</c> <c>BuildCategory</c>).</summary>
    public enum BuildCategory
    {
        Production = 0,
        Logistics = 1,
        Power = 2,
        Defence = 3
    }

    /// <summary>One line of a machine's price, with what the Backpack actually holds against it.</summary>
    public readonly struct BuildCostRow
    {
        public BuildCostRow(string item, string displayName, int required, int available)
        {
            Item = item;
            DisplayName = displayName;
            Required = required;
            Available = available;
        }

        /// <summary>Item key ("steel"), for an icon lookup.</summary>
        public string Item { get; }

        /// <summary>The catalogue's own display name.</summary>
        public string DisplayName { get; }

        /// <summary>How many the price asks for.</summary>
        public int Required { get; }

        /// <summary>How many are in the Backpack right now.</summary>
        public int Available { get; }

        /// <summary>True when the Backpack covers this line.</summary>
        public bool Ok => Available >= Required;
    }

    /// <summary>
    /// Everything the build menu needs to draw one card, and nothing a renderer would need. Strings only, so the
    /// UI layer copies them in and never formats a number of its own.
    /// </summary>
    public sealed class BuildCardView
    {
        /// <summary>Machine kind id — the argument to <c>WorldInput.HoldTool</c> and to the placement command.</summary>
        public string Kind = "";

        /// <summary>Catalogue display name ("Gun turret").</summary>
        public string DisplayName = "";

        /// <summary>Which tab this card belongs to.</summary>
        public BuildCategory Category;

        /// <summary>"Cost: 15 Steel + 5 Copper", or "Cost: nothing".</summary>
        public string CostText = "";

        /// <summary>
        /// §5-style availability, exactly the reference's three cases: "Materials available in Backpack.",
        /// "Missing: 10 Steel", or "2 packed · used before materials".
        /// </summary>
        public string Availability = "";

        /// <summary>True when the Backpack (or a packed machine) can pay for this right now.</summary>
        public bool Affordable;

        /// <summary>Packed machines of this kind in the Backpack; spent before materials.</summary>
        public int Carried;

        /// <summary>"" or the catalogue's Unlock gate ("Concrete crew", "Schematic", "material (Polymer)").</summary>
        public string Unlock = "";

        /// <summary>"Generates 300 kW" / "Demands 60 kW" / "" — sign convention is the catalogue's.</summary>
        public string PowerText = "";

        /// <summary>Facts from the catalogue row: size, power, slots, rate, reach, ammo, fuel, HP.</summary>
        public string Facts = "";

        public string Description = "";
        public string OutputSummary = "";

        /// <summary>Cost lines with have/need, for the card tooltip.</summary>
        public IReadOnlyList<BuildCostRow> Cost = Array.Empty<BuildCostRow>();
    }

    /// <summary>
    /// The build menu's catalogue, ported from <c>packages/game/src/buildCatalogue.ts</c> and
    /// <c>buildPanel.ts</c>'s <c>details()</c>. Lives in <c>Relight.Sim</c> so it can be tested without Unity and
    /// so the panel controller stays a thin copier (TECHNICAL_ARCHITECTURE.md §8.2).
    ///
    /// <b>On <c>knownEquipment</c>.</b> The reference hides a kind until the campaign has earned its plans
    /// (<c>campaignGuide.ts:101</c>, built on <c>lockReason</c> and the recruit sites). The Unity port has neither
    /// a progression record nor a recruit roster, and <see cref="Placement.Buildable"/> — the only authority on
    /// whether a machine may be placed — asks about geometry, reach and price and nothing else. Hiding or
    /// disabling a card on a gate the simulation does not enforce would be the UI inventing a rule, so every
    /// catalogue row is listed and the catalogue's own <c>Unlock</c> column is surfaced as a note
    /// (<see cref="BuildCardView.Unlock"/>). When a progression model arrives, filter here, in one place.
    /// </summary>
    public static class BuildCatalogue
    {
        /// <summary>The tabs, in the reference's order.</summary>
        public static readonly BuildCategory[] Categories =
        {
            BuildCategory.Production, BuildCategory.Logistics, BuildCategory.Power, BuildCategory.Defence
        };

        /// <summary>§5.4-style line when the Backpack covers the whole price.</summary>
        public const string ReadyText = "Materials available in Backpack.";

        /// <summary>The Depot is free and is not built by the player (reference filters it out of the catalogue).</summary>
        public const string DepotKind = "depot";

        private static readonly string[] ProductionKinds =
            { "alienworkbench", "excavator", "assembler", "assembler2", "mixer", "foundry", "refinery", "pumpjack" };

        private static readonly string[] DefenceKinds = { "turret", "cannon", "wall", "barricade" };

        private static readonly string[] PowerKinds =
            { "generator", "pole", "bigpole", "substation", "lamp", "arclamp", "floodlight" };

        /// <summary>Reference <c>buildCategory</c> (buildCatalogue.ts:19-23); everything else is Logistics.</summary>
        public static BuildCategory Category(string kind)
        {
            if (Contains(ProductionKinds, kind)) return BuildCategory.Production;
            if (Contains(DefenceKinds, kind)) return BuildCategory.Defence;
            if (Contains(PowerKinds, kind)) return BuildCategory.Power;
            return BuildCategory.Logistics;
        }

        /// <summary>The tab's name as the player reads it.</summary>
        public static string Name(BuildCategory c)
        {
            switch (c)
            {
                case BuildCategory.Production: return "Production";
                case BuildCategory.Logistics: return "Logistics";
                case BuildCategory.Power: return "Power";
                default: return "Defence";
            }
        }

        /// <summary>
        /// Every buildable kind in catalogue order, priced against the engineer's Backpack. Depot is excluded
        /// (reference <c>buildCatalogue</c> filters it: it is free, and the campaign places it).
        /// </summary>
        public static List<BuildCardView> Cards(SimContext ctx, SimState st)
        {
            var cards = new List<BuildCardView>();
            if (ctx == null || ctx.Data == null) return cards;
            var d = ctx.Data;
            var inv = st == null || st.Engineer == null ? null : st.Engineer.Inv;

            for (var i = 0; i < d.Machines.Count; i++)
            {
                var spec = d.Machines[i];
                if (string.Equals(spec.Key, DepotKind, StringComparison.Ordinal)) continue;
                cards.Add(Card(d, spec, inv));
            }
            return cards;
        }

        /// <summary>One card. Public so a test can price a single kind without walking the catalogue.</summary>
        public static BuildCardView Card(GameData d, MachineSpec spec, ItemBag inv)
        {
            var rows = new List<BuildCostRow>();
            for (var i = 0; i < spec.Cost.Count; i++)
            {
                var line = spec.Cost[i];
                if (line.Count <= 0) continue;
                var have = inv == null ? 0 : (int)Math.Floor(inv[line.Item]);
                rows.Add(new BuildCostRow(Items.Key(line.Item), d.Item(line.Item).DisplayName, line.Count, have));
            }

            var carried = inv == null ? 0 : (int)Math.Floor(inv[new ItemKey(spec.Key)]);
            var affordable = carried >= 1;
            if (!affordable)
            {
                affordable = true;
                for (var i = 0; i < rows.Count; i++)
                    if (!rows[i].Ok) { affordable = false; break; }
            }

            return new BuildCardView
            {
                Kind = spec.Key,
                DisplayName = spec.DisplayName,
                Category = Category(spec.Key),
                CostText = "Cost: " + Placement.CostText(d, spec.Cost),
                Availability = AvailabilityText(carried, rows),
                Affordable = affordable,
                Carried = carried,
                Unlock = spec.Unlock ?? "",
                PowerText = PowerLine(spec),
                Facts = FactsLine(spec),
                Description = DescriptionFor(spec.Key),
                OutputSummary = OutputsLine(d, spec),
                Cost = rows
            };
        }

        /// <summary>Reference <c>details()</c>: packed machines first, then the shortage, then the ready line.</summary>
        public static string AvailabilityText(int carried, IReadOnlyList<BuildCostRow> rows)
        {
            if (carried > 0)
                return carried.ToString(CultureInfo.InvariantCulture) + " packed · used before materials";

            var missing = "";
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Ok) continue;
                if (missing.Length > 0) missing += " + ";
                missing += (r.Required - r.Available).ToString(CultureInfo.InvariantCulture) + " " + r.DisplayName;
            }
            return missing.Length > 0 ? "Missing: " + missing : ReadyText;
        }

        /// <summary>
        /// The catalogue's power column, read the way the simulation reads it: a NEGATIVE
        /// <see cref="MachineSpec.PowerKw"/> is generation (the Generator is -300), a positive one is demand.
        /// </summary>
        public static string PowerLine(MachineSpec spec)
        {
            if (spec.PowerKw < 0)
                return "Generates " + Num(-spec.PowerKw) + " kW";
            if (spec.PowerKw > 0)
                return "Demands " + Num(spec.PowerKw) + " kW";
            return "";
        }

        public static string DescriptionFor(string kind)
        {
            switch (kind)
            {
                case "excavator": return "Automatically mines nearby solid deposits and rubble. Connect a conveyor to carry the resources away.";
                case "pumpjack": return "Extracts oil from nearby crude deposits for processing in a Refinery.";
                case "foundry": return "Smelts raw ore into metals used in components, buildings and ammunition.";
                case "refinery": return "Processes crude oil into fuel and industrial materials.";
                case "assembler": return "Automatically crafts components and ammunition from supplied ingredients. Choose a recipe after placing it.";
                case "assembler2": return "An upgraded Assembler that crafts the same recipes faster.";
                case "mixer": return "Mixes raw materials into concrete for construction.";
                case "alienworkbench": return "Turns recovered alien materials into advanced equipment.";
                case "chest": return "Stores resources and supplies. Connect conveyors or inserters to move items in and out.";
                case "belt": return "Carries items in its arrow direction between machines and storage. Runs without electricity.";
                case "fastbelt": return "Moves items faster than a standard Belt. Runs without electricity.";
                case "inserter": return "Transfers items from behind its arm to the front. Set an item filter to control what it picks up.";
                case "underground": return "Carries items beneath obstacles between a paired entrance and exit.";
                case "splitter": return "Divides item flow between two outputs. Choose balanced flow or give one side priority.";
                case "generator": return "Burns coal or refined fuel to supply electricity through connected power poles.";
                case "pole": return "Connects nearby machines to the power network and links to other poles.";
                case "bigpole": return "Extends the power network over longer distances and connects nearby machines.";
                case "substation": return "Distributes electricity to machines across a broad area.";
                case "lamp": return "Lights the surrounding area when connected to power.";
                case "arclamp": return "Provides wider-area lighting when connected to power.";
                case "floodlight": return "Projects a directional cone of light. Rotate it to aim the beam.";
                case "turret": return "Automatically fires at enemies in range. Supply rounds and connect power.";
                case "cannon": return "Defends the area with heavy shells. Supply shells and connect power.";
                case "wall": return "Blocks movement and absorbs enemy attacks to protect your base.";
                case "barricade": return "A defensive obstacle that blocks movement and absorbs enemy attacks.";
                default: return "";
            }
        }

        public static string OutputsLine(GameData data, MachineSpec spec)
        {
            if (spec.Key == "excavator") return "Extracts: Ore, coal, stone and salvage, depending on the deposit.";
            if (spec.Key == "pumpjack") return "Extracts: " + data.Item(ItemId.Crude).DisplayName + ".";
            if (spec.PowerKw < 0) return "Produces: Electricity.";
            var recipes = new List<Recipe>();
            ProductionRules.RecipesFor(data, new Machine { Kind = spec.Key }, recipes);
            var outputs = new List<string>();
            var seen = new HashSet<ItemId>();
            foreach (var recipe in recipes)
            {
                if (recipe.Outputs == null || recipe.Outputs.Count == 0) continue;
                var item = ProductionRules.OutputItem(recipe);
                if (seen.Add(item)) outputs.Add(data.Item(item).DisplayName);
            }
            return outputs.Count == 0 ? "" : "Produces: " + string.Join(", ", outputs) + ".";
        }

        public static string FactsLine(MachineSpec spec)
        {
            var s = spec.Size + "×" + spec.Size + " tiles";
            if (spec.RatePerS > 0) s += " · " + Num(spec.RatePerS) + "/s";
            // MachineInventory.ChestCap reads this as a whole-item capacity, so "200 slots" over-promised a chest
            // by two hundred times — it holds 200 items in total, of any mix.
            if (spec.InventorySlots > 0) s += " · holds " + spec.InventorySlots + (spec.InventorySlots == 1 ? " item" : " items");
            if (spec.FuelCap > 0) s += " · fuel " + Num(spec.FuelCap);
            if (spec.AmmoCap > 0) s += " · ammo " + spec.AmmoCap;
            if (spec.ReachTiles > 0) s += " · reach " + Num(spec.ReachTiles) + " tiles";
            if (spec.LightRadiusTiles > 0) s += " · light " + Num(spec.LightRadiusTiles) + " tiles";
            if (spec.Hp > 0) s += " · " + Num(spec.Hp) + " HP";
            return s;
        }

        private static string Num(double v) =>
            v == Math.Floor(v)
                ? ((long)v).ToString(CultureInfo.InvariantCulture)
                : v.ToString("0.##", CultureInfo.InvariantCulture);

        private static bool Contains(string[] set, string kind)
        {
            for (var i = 0; i < set.Length; i++)
                if (string.Equals(set[i], kind, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
