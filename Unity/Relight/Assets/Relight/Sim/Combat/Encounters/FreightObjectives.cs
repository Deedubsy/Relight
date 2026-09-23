using System;
using System.Collections.Generic;
using System.Globalization;

namespace Relight.Sim
{
    /// <summary>A circle the HUD draws on the world: a camp's search area, or the ring a key camp is held inside.</summary>
    public readonly struct FreightCircle
    {
        /// <summary>The camp it belongs to.</summary>
        public string Camp { get; }
        public Vec2 Centre { get; }
        public double Radius { get; }
        /// <summary>True for a hold ring at the precise marker; false for a search circle over rounded ground.</summary>
        public bool Hold { get; }
        /// <summary>A hold ring's clock, 0..1; 0 for a search circle or a ring nobody is standing in.</summary>
        public double Fraction { get; }

        public FreightCircle(string camp, Vec2 centre, double radius, bool hold, double fraction)
        {
            Camp = camp; Centre = centre; Radius = radius; Hold = hold; Fraction = fraction;
        }
    }

    /// <summary>
    /// Batch 4, FRT-10 (REL-145): the Freight chapter's nine objectives (FREIGHT_STRONGHOLD_DESIGN §5.9) and the HUD
    /// reads that go with them (§8). Pure reads: nothing here changes the state.
    ///
    /// <para>The nine follow the opening. <see cref="OpeningQueries"/> asks <see cref="Next"/> just before its
    /// terminal row, so the chapter starts once every opening row is behind the player and the terminal row moves
    /// to after the ninth. Each objective is read from the state it names, never from a flag of its own, so a camp
    /// found or held early is simply skipped and a save loads onto the right row.</para>
    ///
    /// <para>A map without the chapter's places (the tests' open squares, a procedural map) has no chapter:
    /// <see cref="Present"/> is false and every read here is empty.</para>
    ///
    /// <para>§5.1 says a search camp's <see cref="EncounterRecord.Discovered"/> is set as its objective becomes
    /// active. The port derives the search circle from the active step instead (U-P-45), so no read of the HUD
    /// writes to the save.</para>
    /// </summary>
    public static class FreightObjectives
    {
        public const string Stronghold = "freight";
        public const string Plant = "plant:riverside";
        public static readonly string[] Camps = { "freight:camp:1", "freight:camp:2", "freight:camp:3" };

        /// <summary>§5.1: a search circle is centred on the marker rounded to this many tiles.</summary>
        public const double SearchRound = 20;
        /// <summary>§5.1: and has this radius.</summary>
        public const double SearchRadius = 20;
        /// <summary>§5.9 row 7: the core has arrived when it is this close to the plant.</summary>
        public const double CoreArrivedTiles = 8;

        /// <summary>The step the chapter is on: 1..9, or 10 once the plant is commissioned.</summary>
        public const int Done = 10;

        /// <summary>The chapter's places are all on this map: the three key camps, the arena and the plant.</summary>
        public static bool Present(SimContext ctx)
        {
            var s = ctx?.Sites;
            if (s == null || s.Count == 0) return false;
            for (var i = 0; i < Camps.Length; i++) if (s.Find(Camps[i]) == null) return false;
            var sh = StrongholdDef();
            return sh != null && s.Find(sh.Arena) != null && s.Find(Plant) != null && EncounterCatalogue.Plant(Plant) != null;
        }

        static StrongholdDef StrongholdDef()
        {
            var all = EncounterCatalogue.Strongholds;
            for (var i = 0; i < all.Count; i++) if (string.CompareOrdinal(all[i].Id, Stronghold) == 0) return all[i];
            return null;
        }

        /// <summary>The first objective not yet met, 1..9, or <see cref="Done"/>. 0 when the chapter is not on this map.</summary>
        public static int Step(SimContext ctx, SimState st)
        {
            if (!Present(ctx)) return 0;
            var enc = st.Encounters;
            var p = EncounterCatalogue.Plant(Plant);
            var rec = enc.Plant(Plant);
            if (rec != null && rec.Commissioned) return Done;
            if (!Resolved(st, Camps[0])) return 1;
            for (var k = 0; k < Camps.Length; k++) if (!enc.Holds(Key(Camps[k]))) return 2 + k;
            if (!enc.IsOpen(Stronghold)) return 5;
            if (!enc.HasFallen(Stronghold)) return 6;
            if (!CoreArrived(ctx, st, p)) return 7;
            if (rec == null || !rec.Prepared) return 8;
            return 9;
        }

        /// <summary>The objective the chapter is on, or null when it is not on this map or is finished.</summary>
        public static ObjectiveView? Next(SimContext ctx, SimState st)
        {
            var step = Step(ctx, st);
            if (step <= 0 || step >= Done) return null;
            var d = ctx.Data;
            var p = EncounterCatalogue.Plant(Plant);
            var plantAt = ctx.Sites.Find(Plant).Centre;
            switch (step)
            {
                case 1:
                    return At("freight-1", "Find where they're coming from",
                        "Search the marked area for the camp the raids come from",
                        "The circle marks roughly where it is. The camp shows itself once you are within "
                        + Num(EncounterCatalogue.SearchResolveTiles) + " tiles; carry rounds when you go.",
                        SearchCentre(ctx, Camps[0]));
                case 2:
                    return Hold(ctx, st, "freight-2", "Secure the West passage (hold the camp)", Camps[0]);
                case 3:
                    return Hold(ctx, st, "freight-3", "Push north — Old utility is dark; bring lights", Camps[1]);
                case 4:
                    return Hold(ctx, st, "freight-4", "Northwood approach — something bigger guards it", Camps[2]);
                case 5:
                    return At("freight-5", "Open the warehouse",
                        "Walk up to a warehouse door with your three keys",
                        "The keys you took from the three camps open the Freight warehouse. Something waits inside.",
                        NearestDoor(ctx, st));
                case 6:
                    return At("freight-6", "Bring down the guardian",
                        "Kill the guardian inside the warehouse",
                        "It charges in straight lines, and faster once it is badly hurt. Keep moving and keep firing. "
                        + "It holds the warehouse's Power core.", GuardianAt(ctx, st));
                case 7:
                {
                    var holding = CoreCarry.Holding(st.Engineer);
                    var core = CorePos(st, out _);
                    var left = Math.Max(0, Distance(core, plantAt) - CoreArrivedTiles);
                    return At("freight-7", "Carry the core to " + p.Name + " (walk only — it's heavy)",
                        holding
                            ? "Walk the Power core to " + p.Name + " — about " + Num(Math.Ceiling(left)) + " tiles to go"
                            : "Pick up the Power core (press E beside it) and carry it to " + p.Name,
                        "With the core in both hands you cannot sprint, dodge, fire or build. E sets it down; if you "
                        + "fall, it stays where you fell.", holding ? plantAt : core);
                }
                case 8:
                {
                    string text;
                    if (!Plants.Cleared(st, p))
                    {
                        var squat = st.Encounters.Find(p.Squat);
                        var left = squat != null && squat.Resolved ? EncounterPhase.Living(st, p.Squat) : 0;
                        text = left > 0
                            ? "Clear the squatters out of " + p.Name + " (" + left + " left)"
                            : "Clear the squatters out of " + p.Name;
                    }
                    else text = "Press E at " + p.Name + " to pay its repair";
                    return new ObjectiveView("freight-8", "Prepare " + p.Name + " (clear the squat, pay the repair)",
                        text, "The repair comes from your Backpack, not from stock at Home.", true, plantAt,
                        Materials(d, st, p.RepairCost));
                }
                default:
                    return new ObjectiveView("freight-9", "Commission the plant",
                        "Press E at " + p.Name + " with the Power core in both hands",
                        "The core and the cost go in together, from your hands and your Backpack. Once it is lit it "
                        + "powers the north, and the next major raid comes for it.", true, plantAt,
                        Materials(d, st, p.Commission));
            }
        }

        // -------------------------------------------------------------------- HUD reads (§8)

        /// <summary>
        /// The circles to draw: a search circle over each key camp the chapter has sent the player to and that is
        /// not found yet, and a hold ring at each found key camp not yet held, with its clock.
        /// </summary>
        public static void Circles(SimContext ctx, SimState st, List<FreightCircle> into)
        {
            into.Clear();
            var step = Step(ctx, st);
            if (step <= 0) return;
            for (var k = 0; k < Camps.Length; k++)
            {
                var def = EncounterCatalogue.Find(Camps[k]);
                var site = ctx.Sites.Find(Camps[k]);
                if (def == null || site == null) continue;
                var rec = st.Encounters.Find(def.Id);
                if (rec == null || !rec.Resolved)
                {
                    // Camp 1 is searched from step 1, camp 2 from step 3, camp 3 from step 4.
                    if (step < Done && step >= SearchFrom(k))
                        into.Add(new FreightCircle(def.Id, SearchCentre(ctx, def.Id), SearchRadius, false, 0));
                    continue;
                }
                if (rec.Claimed || !EncounterPhase.Occupies(def)) continue;
                into.Add(new FreightCircle(def.Id, site.Centre, def.OccupyRadius, true, HoldFraction(st, def, rec)));
            }
        }

        static int SearchFrom(int k) => k == 0 ? 1 : k + 2;

        /// <summary>"Keys 1/3" while the keys matter: from the first hold until the warehouse opens.</summary>
        public static string KeysLine(SimContext ctx, SimState st)
        {
            var step = Step(ctx, st);
            if (step < 2 || step > 5) return "";
            var sh = StrongholdDef();
            return "Keys " + StrongholdRules.KeysHeld(st, sh) + "/" + sh.Keys.Count;
        }

        /// <summary>"Holding West passage · 12 / 30 s", or why the hold is not running, while the engineer is at a camp.</summary>
        public static string HoldLine(SimContext ctx, SimState st)
        {
            if (!Present(ctx)) return "";
            var e = st.Engineer;
            for (var k = 0; k < Camps.Length; k++)
            {
                var def = EncounterCatalogue.Find(Camps[k]);
                var rec = st.Encounters.Find(Camps[k]);
                if (def == null || rec == null || !rec.Resolved || rec.Claimed) continue;
                var at = ctx.Sites.Find(def.Id).Centre;
                if (e.IsDown || Distance(e.Pos, at) > def.OccupyRadius) continue;
                if (rec.OccupiedSince >= 0)
                    return "Holding " + def.Name + " · " + Secs(st.T - rec.OccupiedSince) + " / "
                        + Secs(def.OccupySeconds) + " s";
                if (EncounterPhase.GuardNear(st, def.Id, at, def.GuardRadius))
                    return def.Name + ": clear the guards within " + Num(def.GuardRadius) + " tiles to hold it";
            }
            return "";
        }

        public const string CarryLine = "Carrying the Power core · walk only · E sets it down";

        /// <summary>The carrying line while the core is in both hands, "" otherwise.</summary>
        public static string CarryingLine(SimState st) => CoreCarry.Holding(st.Engineer) ? CarryLine : "";

        /// <summary>
        /// The plant panel's one line (§8): its stage, then once lit its kW and hit points. Shown from the carry
        /// step on, or as soon as the plant has a record.
        /// </summary>
        public static string PlantLine(SimContext ctx, SimState st)
        {
            var step = Step(ctx, st);
            if (step <= 0) return "";
            var p = EncounterCatalogue.Plant(Plant);
            var rec = st.Encounters.Plant(Plant);
            if (rec == null && step < 7) return "";
            if (rec != null && rec.Commissioned)
                return rec.Hp > 0
                    ? p.Name + " · lit · " + Num(Plants.Kw(ctx.Data)) + " kW · " + Num(Math.Ceiling(rec.Hp)) + "/"
                      + Num(Plants.MaxHp(ctx.Data)) + " hp"
                    : p.Name + " · fallen · no power";
            if (rec != null && rec.Prepared) return p.Name + " · prepared · waiting for its Power core";
            if (!Plants.Cleared(st, p))
            {
                var squat = st.Encounters.Find(p.Squat);
                var left = squat != null && squat.Resolved ? EncounterPhase.Living(st, p.Squat) : 0;
                return p.Name + " · squatters inside" + (left > 0 ? " (" + left + ")" : "");
            }
            return p.Name + " · clear · needs its repair";
        }

        // -------------------------------------------------------------------- helpers

        /// <summary>§5.1: the marker rounded to the nearest <see cref="SearchRound"/> tiles.</summary>
        public static Vec2 SearchCentre(SimContext ctx, string camp)
        {
            var c = ctx.Sites.Find(camp).Centre;
            return new Vec2(Math.Round(c.X / SearchRound) * SearchRound, Math.Round(c.Y / SearchRound) * SearchRound);
        }

        static string Key(string camp) => EncounterCatalogue.Find(camp)?.Key ?? camp;

        static bool Resolved(SimState st, string id) => st.Encounters.Find(id)?.Resolved == true;

        static double HoldFraction(SimState st, EncounterDef def, EncounterRecord rec) =>
            rec.OccupiedSince < 0 || def.OccupySeconds <= 0 ? 0
                : Math.Min(1, Math.Max(0, (st.T - rec.OccupiedSince) / def.OccupySeconds));

        static ObjectiveView Hold(SimContext ctx, SimState st, string id, string title, string camp)
        {
            var def = EncounterCatalogue.Find(camp);
            var rec = st.Encounters.Find(camp);
            var secs = Secs(def.OccupySeconds);
            var detail = "Stand within " + Num(def.OccupyRadius) + " tiles of the camp's marker for " + secs
                + " seconds with none of its guards within " + Num(def.GuardRadius) + " tiles. Stepping out starts "
                + "the clock again. Holding it gives you its key.";
            if (rec == null || !rec.Resolved)
                return At(id, title, "Search the marked area for the camp", detail, SearchCentre(ctx, camp));
            var at = ctx.Sites.Find(camp).Centre;
            if (rec.OccupiedSince >= 0)
                return At(id, title, "Holding: " + Secs(st.T - rec.OccupiedSince) + " of " + secs + " seconds", detail, at);
            var living = EncounterPhase.Living(st, camp);
            return At(id, title,
                living > 0
                    ? "Clear the camp (" + living + " left), then hold its marker for " + secs + " seconds"
                    : "Stand on the camp's marker for " + secs + " seconds",
                detail, at);
        }

        static Vec2 NearestDoor(SimContext ctx, SimState st)
        {
            var sh = StrongholdDef();
            var e = st.Engineer.Pos;
            var best = ctx.Sites.Find(sh.Arena).Centre;
            var bestD = double.MaxValue;
            for (var i = 0; i < sh.Doors.Count; i++)
            {
                var r = ctx.Sites.Find(sh.Doors[i]);
                if (r == null) continue;
                var dd = Distance(e, r.Centre);
                if (dd < bestD) { bestD = dd; best = r.Centre; }
            }
            return best;
        }

        static Vec2 GuardianAt(SimContext ctx, SimState st)
        {
            var sh = StrongholdDef();
            var actors = st.Enemies.Actors;
            for (var i = 0; i < actors.Count; i++)
                if (actors[i].Hp > 0 && GuardianRules.Is(actors[i]) && string.CompareOrdinal(actors[i].Site, sh.Arena) == 0)
                    return actors[i].Pos;
            return ctx.Sites.Find(sh.Arena).Centre;
        }

        /// <summary>Where the freight core is: the engineer while it is in both hands, else where it lies.</summary>
        static Vec2 CorePos(SimState st, out bool found)
        {
            found = true;
            if (string.CompareOrdinal(st.Engineer.Carrying, Stronghold) == 0) return st.Engineer.Pos;
            var cores = st.Encounters.Cores;
            for (var i = 0; i < cores.Count; i++)
                if (string.CompareOrdinal(cores[i].Stronghold, Stronghold) == 0) return cores[i].Pos;
            found = false;
            return Vec2.Zero;
        }

        /// <summary>§5.9 row 7: the core, carried or set down, is within <see cref="CoreArrivedTiles"/> of the plant.</summary>
        static bool CoreArrived(SimContext ctx, SimState st, PlantDef p)
        {
            var at = CorePos(st, out var found);
            return found && Distance(at, ctx.Sites.Find(p.Id).Centre) <= CoreArrivedTiles;
        }

        static IReadOnlyList<ObjectiveMaterial> Materials(GameData d, SimState st, IReadOnlyList<ItemStack> cost)
        {
            var rows = new ObjectiveMaterial[cost.Count];
            for (var i = 0; i < cost.Count; i++)
            {
                var id = cost[i].Item;
                rows[i] = new ObjectiveMaterial(Items.Key(id), d.Item(id).DisplayName, cost[i].Count,
                    (int)Math.Floor(st.Engineer.Inv[id]), OpeningRules.HomeStock(st, id));
            }
            return rows;
        }

        static ObjectiveView At(string id, string title, string text, string detail, Vec2 where) =>
            new ObjectiveView(id, title, text, detail, true, where, Array.Empty<ObjectiveMaterial>());

        static double Distance(Vec2 a, Vec2 b) => DirectorRules.Distance(a.X, a.Y, b.X, b.Y);

        static string Secs(double s) => ((int)Math.Floor(Math.Max(0, s))).ToString(CultureInfo.InvariantCulture);

        static string Num(double v) => v == Math.Floor(v)
            ? ((long)v).ToString(CultureInfo.InvariantCulture)
            : v.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
