using System;
using System.Collections.Generic;
using System.Globalization;

namespace Relight.Sim
{
    /// <summary>Reference openingEncounter.ts:82 <c>OpeningView</c>, plus the port's deferral fields.</summary>
    public readonly struct OpeningView
    {
        public OpeningStatus Status { get; }
        public int Shots { get; }
        public int Count { get; }
        /// <summary>The compass WORD of the announced approach seen from the core ("north-east"); "" before scheduling.</summary>
        public string Direction { get; }
        /// <summary>Whole seconds until the first body, 0 once it has started.</summary>
        public int SecondsLeft { get; }
        public double StartsAt { get; }
        public double EndedAt { get; }
        public double SuppliedAt { get; }
        public bool HasOrigin { get; }
        /// <summary>Tile centre of the announced approach, for the map marker.</summary>
        public Vec2 Origin { get; }
        public int TurretId { get; }
        /// <summary>PORT ADDITION (U-D-26): whole seconds until the deferred encounter is expected; 0 when not deferred.</summary>
        public int DeferSecondsLeft { get; }
        public int DeferCount { get; }
        public string DeferNotice { get; }

        public OpeningView(OpeningStatus status, int shots, int count, string direction, int secondsLeft,
            double startsAt, double endedAt, double suppliedAt, bool hasOrigin, Vec2 origin, int turretId,
            int deferSecondsLeft, int deferCount, string deferNotice)
        {
            Status = status; Shots = shots; Count = count; Direction = direction; SecondsLeft = secondsLeft;
            StartsAt = startsAt; EndedAt = endedAt; SuppliedAt = suppliedAt; HasOrigin = hasOrigin; Origin = origin;
            TurretId = turretId; DeferSecondsLeft = deferSecondsLeft; DeferCount = deferCount;
            DeferNotice = deferNotice ?? "";
        }
    }

    /// <summary>One "Required resources in Backpack" row of the goal card (reference goal.ts <c>NextAction.resources</c>).</summary>
    public readonly struct ObjectiveMaterial
    {
        /// <summary>Pocket key: an item key ("steel") or a packed machine kind ("generator").</summary>
        public string Item { get; }
        public string DisplayName { get; }
        public int Required { get; }
        public int Available { get; }
        /// <summary>How many of the shortfall are sitting in a Home chest (reference <c>shortage[].home</c>).</summary>
        public int AtHome { get; }

        /// <summary>
        /// PORT ADDITION (correction pass R5): where the engineer can actually get more, in a few words —
        /// "mine salvage rubble 12 tiles north-east". "" when the row is already satisfied, when the key is a
        /// packed machine rather than an item, or when nothing on this map yields it. The WHOLE sentence, with
        /// the action to take, is appended to <see cref="ObjectiveView.Detail"/> for the first short row.
        /// Filled by <see cref="OpeningQueries.Source"/>; the default keeps every five-argument call site valid.
        /// </summary>
        public string Source { get; }

        public ObjectiveMaterial(string item, string displayName, int required, int available, int atHome,
            string source = "")
        {
            Item = item; DisplayName = displayName; Required = required; Available = available; AtHome = atHome;
            Source = source ?? "";
        }
    }

    /// <summary>
    /// PORT ADDITION (correction pass R5): the concrete next step for a material the Backpack is short of.
    ///
    /// The reference never had this — its goal card only printed "have/need" — but the owner playtest found the
    /// objective unusable because nothing said WHERE the missing steel was. Everything here is derived from live
    /// state: an authored <see cref="SiteKind.Resource"/> rect, a minable tile of the loaded geometry, a Home
    /// chest that already holds some, or the recipe that makes it. Nothing is granted and no number is invented;
    /// when the map yields none of it, <see cref="Found"/> is false and the text says so plainly.
    /// </summary>
    public readonly struct MaterialSource
    {
        /// <summary>Nothing on this map yields the item.</summary>
        public static readonly MaterialSource None = new MaterialSource(false, "", "", "", 0, "", "");

        public bool Found { get; }
        public bool HasLocation { get; }
        public Vec2 Location { get; }

        /// <summary>"site", "tile", "home", "craft", or "" when nothing yields it. For tests, not for display.</summary>
        public string Kind { get; }

        /// <summary>What to walk to, in words: "salvage rubble", "the steel salvage", "a Home supply chest".</summary>
        public string What { get; }

        /// <summary>Compass word seen FROM THE ENGINEER ("north-east"); "" when there is nowhere to walk.</summary>
        public string Compass { get; }

        /// <summary>Whole tiles from the engineer; 0 when there is nowhere to walk.</summary>
        public int Tiles { get; }

        /// <summary>The goal-card row form: "mine salvage rubble 12 tiles north-east".</summary>
        public string Short { get; }

        /// <summary>The whole sentence, with the action: "Mine salvage rubble 12 tiles north-east of you — hold left-click on it."</summary>
        public string Text { get; }

        public MaterialSource(bool found, string kind, string what, string compass, int tiles, string shortForm, string text, Vec2? location = null)
        {
            HasLocation = location.HasValue; Location = location ?? Vec2.Zero;
            Found = found; Kind = kind ?? ""; What = what ?? ""; Compass = compass ?? "";
            Tiles = tiles; Short = shortForm ?? ""; Text = text ?? "";
        }
    }

    /// <summary>Reference goal.ts:72 <c>NextAction</c>: one objective, derived from live state, never a script step.</summary>
    public readonly struct ObjectiveView
    {
        public string Id { get; }
        public string Title { get; }
        public string Text { get; }
        public string Detail { get; }
        public bool HasLocation { get; }
        public Vec2 Location { get; }
        public IReadOnlyList<ObjectiveMaterial> Materials { get; }

        public ObjectiveView(string id, string title, string text, string detail,
            bool hasLocation, Vec2 location, IReadOnlyList<ObjectiveMaterial> materials)
        {
            Id = id; Title = title; Text = text; Detail = detail;
            HasLocation = hasLocation; Location = location;
            Materials = materials ?? Array.Empty<ObjectiveMaterial>();
        }
    }

    /// <summary>
    /// The read-only side of C-09: the encounter view (reference openingEncounter.ts:87 <c>openingEncounter</c>) and
    /// the objective chain (reference goal.ts:80 <c>campaignNext</c>).
    ///
    /// The chain is a PRIORITY LIST evaluated from live state on every call, never a script: an already-satisfied
    /// condition is simply not returned, so a save that arrives with three turrets, an equipped Rifle or a running
    /// delivery advances past those rows with no completion flag anywhere (Constitution rule 8).
    ///
    /// Every player-facing string here says "bullets", never "magazine": UI_AND_ONBOARDING.md §7.5 correction 7.0-b.
    /// `magazine` survives only as <see cref="ItemId.Magazine"/> and the recipe keys.
    /// </summary>
    public static class OpeningQueries
    {
        // ------------------------------------------------------------------ encounter view

        /// <summary>Reference <c>openingEncounter(st)</c>: everything the guide card and the HUD need in one read.</summary>
        public static OpeningView Encounter(SimContext ctx, SimState st)
        {
            var op = st.Opening;
            var t = OpeningRules.Tuning(ctx.Data);
            var w = ctx.Geometry != null ? ctx.Geometry.Width : 0;
            var has = op.Origin >= 0 && w > 0;
            var origin = has ? new Vec2(op.Origin % w + 0.5, op.Origin / w + 0.5) : Vec2.Zero;
            var left = op.StartsAt >= 0 ? (int)Math.Max(0, Math.Ceiling(op.StartsAt - st.T)) : 0;
            var wait = op.DeferredUntil >= 0 ? (int)Math.Max(0, Math.Ceiling(op.DeferredUntil - st.T)) : 0;
            return new OpeningView(op.Status, op.Shots, op.Count > 0 ? op.Count : t.Count, DirectionOf(ctx, st), left,
                op.StartsAt, op.EndedAt, op.SuppliedAt, has, origin, op.TurretId, wait, op.DeferCount, op.DeferNotice);
        }

        /// <summary>
        /// The compass word of the announced approach seen from the Home core, reference openingEncounter.ts:92.
        /// "" before an origin is chosen; "outskirts" when the heading is not one of the eight.
        /// </summary>
        public static string DirectionOf(SimContext ctx, SimState st)
        {
            var op = st.Opening;
            var w = ctx.Geometry != null ? ctx.Geometry.Width : 0;
            if (op.Origin < 0 || w <= 0) return "";
            var (cx, cy, cw, ch) = HomeQueries.CoreRect(st);
            if (cw <= 0) return "";
            var at = new Vec2(op.Origin % w + 0.5, op.Origin / w + 0.5);
            return OpeningRules.Compass(DirectorQueries.Direction(at, cx + cw / 2.0, cy + ch / 2.0));
        }

        // ------------------------------------------------------------------ the objective chain

        /// <summary>
        /// Reference goal.ts <c>campaignNext</c>. Takes the context rather than the reference's <c>(st, data)</c>
        /// because the port reads power circuits, authored sites and map geometry, all of which live on
        /// <see cref="SimContext"/> — recorded as a deliberate signature difference in the wave-3 W-A report.
        /// </summary>
        public static ObjectiveView Objective(SimContext ctx, SimState st) => Explain(ctx, st, Chain(ctx, st));

        /// <summary>
        /// The chain itself, unchanged. <see cref="Objective"/> wraps it so the R5 "where do I get this" work
        /// happens in ONE place instead of in each of the dozen returns below, and so every id, title and text
        /// the regression tests pin stays exactly as it was.
        /// </summary>
        private static ObjectiveView Chain(SimContext ctx, SimState st)
        {
            var d = ctx.Data;
            var e = st.Engineer;
            var (hx, hy, hw, hh) = HomeQueries.CoreRect(st);
            var home = hw > 0 ? new Vec2(hx + hw / 2.0, hy + hh / 2.0) : e.Pos;

            // 0a — the engineer is down: nothing else can be acted on.
            if (e.IsDown)
                return At("recovery", "Recover at Home",
                    "Back on your feet in " + Secs(e.Down - st.T) + " s",
                    RecoveryDetail(st), home);

            // 0b — a disabled Home outranks everything except recovery (reference goal.ts:84).
            //
            // OPN-07 (REL-72): every sentence here is checked against the sim. The old card was titled "Restore
            // Home power" and promised that the repair "restarts every machine that was running", but nothing in
            // Unity reads the core except this card and the opening's "repelled" verdict: power, production,
            // turrets, the Depot and respawn all carry on. What a core at 0 HP really does is E-18's failure path:
            // the raid is called off (DirectorPhase.CoreFell), no raid targets a downed core
            // (EnemyCoreHook.Rect), and a large assault that took it pushes the next one out a full interval
            // (DirectorPhase.EndMajor). The repair is started from the Home workshop's core card, which E on the
            // core opens; it is never a hold. It is refused, and pauses, while raiders are within
            // SiegeTuning.CoreThreatTiles (HomeCore.RepairProblem, HomeCorePhase).
            if (st.Home != null && st.Home.Placed && !HomeQueries.CoreOperational(st))
            {
                var card = HomeQueries.RepairCard(st, d);
                var text = card.InProgress
                    ? "Repairing the Home core · " + Secs(card.RemainingS) + " s left"
                    : Home.AttackersNearby(ctx, st)
                        ? "Raiders are still near the core — the repair waits until they leave"
                        : "Press E at the Home core, then " + RecommissionButton + " in the workshop";
                // The repair CHARGES these, so the row carries them as material rows rather than prose: the chips
                // show what is carried against what is needed, and Explain routes an empty Backpack to the ore.
                return new ObjectiveView("home-recovery", "Repair the Home core", text,
                    "The Home core is down. The raid that beat it has been called off, and no raid attacks a core "
                    + "that is down. Your power, machines and turrets keep running. Hand mining still works, and so "
                    + "does the Home workshop. To bring the core back, carry the steel and copper to it, press E to open "
                    + "the Home workshop and choose " + RecommissionButton + ". The repair waits while raiders are "
                    + "close to the core, and it restores the core to full. After a large assault takes the core, the "
                    + "next one waits a full interval.",
                    true, home,
                    new[] { Row(d, st, ItemId.Steel, d.Defence.CoreSteel), Row(d, st, ItemId.Copper, d.Defence.CoreCopper) });
            }

            // U-D-44: the workshop keeps processing while the player is elsewhere, so the objective no longer tells
            // them to stand still — it tells them what else to do with the time, and reads the live job queue.
            var smelting = HandCraft.QueueOf(st, "hand-steel");
            if(st.OpeningResourceVersion>0 && st.Stats.Made[ItemId.Steel]<5 && smelting.batches>0)
                return At("opening-smelt","Smelt your first Steel plates","Smelting Steel plates at Home",
                    smelting.batches+" batches queued and processing. Go back to mining or scouting while the workshop works; "
                    +"collect the plates from its output tray when you return. Cancel returns the reserved ore.",home);
            if(st.OpeningResourceVersion>0 && st.Stats.Made[ItemId.Steel]<5)
                return new ObjectiveView("opening-smelt","Smelt your first Steel plates","Mine Iron ore, then smelt 5 Steel plates at Home",
                    "Home Workshop converts 1 Iron ore into 1 Steel plate in 4 seconds without electricity. Queue batches using carried ore and walk away — it keeps working; a Foundry will automate this later.",true,home,One(ctx,st,"ironore",(int)Math.Ceiling(5-st.Stats.Made[ItemId.Steel])));

            var gen = FirstOfKind(st, "generator");
            if (gen == null) return Build(ctx, st, "generator", "1 · Build a Generator", "Place 1 Generator in Founders Court", home);

            var excavator = FirstOfKind(st, "excavator");
            if (excavator == null)
                return Build(ctx, st, "excavator", "2 · Build an Excavator",
                    "Place 1 Excavator at a resource patch edge, with a clear side for belts", home);

            if (FirstOfKind(st, "chest") == null)
                return Build(ctx, st, "chest", "3 · Build storage", "Place 1 Supply chest near the Excavator", home);

            if (!ReachesKind(st, excavator, "chest") && !(st.OpeningResourceVersion>0 && ReachesKind(st,excavator,"foundry")))
                return new ObjectiveView("opening-workshop", "4 · Connect belts to storage",
                    "Connect Excavator → Supply chest (resources per Belt)",
                    "Use at least 1 Belt; the total depends on the gap. Point its arrow away from the Excavator and into "
                    + "the chest. R rotates. Clear salvage along the route; inserters are not needed.",
                    true, Centre(excavator), Packed(ctx, st, "belt"));

            if (gen.Inv[ItemId.Coal] + gen.Inv[ItemId.Fuel] <= 0)
                return new ObjectiveView("opening-workshop", "Fuel your Generator",
                    "Gather Coal, then open the Generator inventory",
                    "Drag Coal from your Backpack into the Generator fuel slot. One Coal runs it for about "
                    + Num(Math.Round(d.Power.CoalMj * 1000 / d.Power.GeneratorKw)) + " s at its full "
                    + Num(d.Power.GeneratorKw) + " kW, and burns slower when less is drawn. The slot holds "
                    + Num(d.Power.GeneratorFuelCap) + ", so fill it rather than topping it up — and an Excavator on a "
                    + "Coal patch with a belt into the Generator ends hand-feeding it for good.",
                    true, Centre(gen), One(ctx, st, Items.Key(ItemId.Coal), 1));

            if (!PowerQueries.Supplied(ctx, st, excavator.Id))
                return new ObjectiveView("opening-power", "Connect your extraction power",
                    "Place Poles between your Generator and Excavator",
                    "Poles connect by visible cables within 8 tiles (Big poles: 12). Machines must be within coverage of "
                    + "a connected Pole or Substation. Belts need no power. Inspect the Excavator for its own network supply.",
                    true, Centre(excavator), Packed(ctx, st, "pole"));

            var digging = ProductionQueries.Status(ctx, st, excavator.Id);
            if (digging.State != MachineOperatingState.Running && digging.State != MachineOperatingState.Throttled && !(st.OpeningResourceVersion>0 && st.Stats.MinedOf[ItemId.IronOre]>0))
                return At("opening-workshop", "Start your extraction line", digging.Text,
                    "Inspect the Excavator to check its resource, power and output connection.", Centre(excavator));

            if(st.OpeningResourceVersion>0)
            {
                var foundry=FirstOfKind(st,"foundry");
                if(foundry==null)return Build(ctx,st,"foundry","Automate Steel plates","Build a Foundry beside the Iron ore line",Centre(excavator));
                if(ProductionRules.RecipeOf(d,st,foundry)?.Key!="steel-plates")return At("opening-smelt","Set up the Foundry","Select the Steel plates recipe","The Foundry takes Iron ore and outputs Steel plates.",Centre(foundry));
                if(!ReachesKind(st,excavator,"foundry") || !ReachesKind(st,foundry,"chest"))return At("opening-smelt","Connect your plate production","Connect Excavator → Foundry → Supply chest","Use the clear processing space south of the ore strip. Rotate each output toward the next machine. Belts need no power.",Centre(foundry));
                // The step-4 ore belt can still end at the SAME chest the Foundry's plates go to. A Supply chest
                // holds its whole capacity in items of ANY kind, so raw ore fills it, the Foundry's plate belt backs
                // up behind a full chest and the Foundry stalls at "output full" for the rest of the run. Judged on
                // the ONE chest both machines belt straight into (U-D-55): a buffer chest BETWEEN the Excavator and
                // the Foundry is a working line and must not be accused, and the ore that matters is the ore in the
                // shared chest, not the fullest chest on the map. Only raised once it is actually going wrong, so a
                // player whose chest has room is never nagged about a working base.
                var sink=SharedSink(st,excavator,foundry);
                if(sink!=null)
                {
                    var chestCap=MachineInventory.ChestCap(d);
                    var stored=sink.Inv[ItemId.IronOre];
                    if(stored>=chestCap/2 || ProductionQueries.Status(ctx,st,foundry.Id).State==MachineOperatingState.OutputFull)
                        return At("opening-smelt","Send your ore through the Foundry",
                            "Remove the belt that runs Excavator → Supply chest",
                            "Both belts end at the same chest, and it holds "+Num(chestCap)+" items of any kind. Raw ore has "
                            +"filled it, so the Foundry's plates have nowhere to go and it has stopped at \"output full\". "
                            +"Pick up the belt running straight to the chest so the Excavator feeds the Foundry alone — or "
                            +"give the plates a chest of their own. Take the stored ore out by hand to restart the line.",
                            Centre(excavator));
                }
                if(!PowerQueries.Supplied(ctx,st,foundry.Id))return At("opening-power","Power your Foundry","Connect the Foundry to your Generator","An Excavator and Foundry draw 140 kW together. Inspect the Foundry for its network connection.",Centre(foundry));
            }

            // 8 — GP-POWER-FIX, made real by court D55: the authored block substation must be brought onto a supplied
            // circuit, and it then feeds the streetlights it owns.
            var sub = NearestSubstation(ctx, hx + hw / 2.0, hy + hh / 2.0);
            if (sub != null && !SubstationLive(ctx, st, sub))
            {
                var lights = SubstationLights(ctx, sub);
                return new ObjectiveView("opening-power", "Connect Founders Court’s substation",
                    "Chain Poles from your network to the Founders Court substation",
                    (lights == 0 ? "Founders Court’s streetlights run" : lights == 1 ? "Founders Court’s streetlight runs"
                        : "Founders Court’s " + Num(lights) + " streetlights run")
                    + " from the block substation in the Works Yard, marked here. Poles link within "
                    + Num(PowerGrid.SiteReach(d)) + " tiles of another Pole, a Generator or the substation footprint; a "
                    + "cable appears once linked and the placement preview shows purple lines to everything in reach. "
                    + "Chain Poles from your network to it and the court lights up.",
                    true, sub.Centre, Packed(ctx, st, "pole"));
            }

            // 8b — L-02 (ALWAYS_DARK_SPEC §5.6): one Lamp, lit, before the Rifle. It is the one moment the opening
            // has to teach that light is something the player places and that aliens and turrets both answer to it.
            // Asked only while no weapon is owned, so a save already past the Rifle is never sent back for a Lamp.
            if (st.Weapons.Owned.Count == 0)
            {
                var lamp = FirstOfKind(st, "lamp");
                var teach = "Aliens avoid lit ground: they pause at its edge and look for a darker way in. Your turrets "
                    + "see an alien on lit ground at full range and one in the dark only up close. Your flashlight "
                    + "lights the way for you alone — it does not count for either.";
                if (lamp == null)
                    return new ObjectiveView("opening-light", "Light the ground you will defend",
                        "Place 1 Lamp within reach of a Pole",
                        "A Lamp lights " + Num(d.Power.LampRadiusTiles) + " tiles around it while it has power ("
                        + Num(d.Power.LampKw) + " kW). " + teach,
                        true, home, LampRows(ctx, st));
                if (!PowerQueries.Supplied(ctx, st, lamp.Id))
                    return new ObjectiveView("opening-light", "Light the ground you will defend",
                        "Connect your Lamp to power",
                        "A Lamp is dark until it stands within reach of a connected Pole. " + teach,
                        true, Centre(lamp), Packed(ctx, st, "pole"));
            }

            // 9 — the Rifle. Re-arms by itself when a craft was cancelled: nothing is owned, so the row returns.
            if (st.Weapons.Owned.Count == 0)
            {
                var rifle = Recipe(d, WeaponRules.RifleRecipe);
                return new ObjectiveView("opening-rifle", "Prepare your expedition Rifle",
                    "Craft a Rifle at Home workshop · " + Num(rifle != null ? rifle.Seconds : 0) + " s",
                    "Craft Rifle using carried supplies. The finished Rifle waits in the Home workshop's output tray, "
                    + "not your Backpack — collect it there, then Equip it in slot 1 and select weapon tool 9. Keep "
                    + "bullets for the expedition as well as turret defence.",
                    true, home, Inputs(ctx, st, rifle));
            }

            // 10 — equipping it. Skipped outright on a save that already has a weapon in a slot.
            if (st.Weapons.Slot0 == null && st.Weapons.Slot1 == null)
                return At("opening-equip", "Equip your crafted Rifle",
                    "Collect the Rifle from the Home workshop tray, then Equip it in slot 1",
                    "A finished Rifle is delivered to the Home workshop's output tray, never straight to the Backpack: "
                    + "stand in reach and collect it first, or the Backpack will look empty. Equipment slots are separate "
                    + "from construction shortcuts, and loaded rounds and cooldown stay with each weapon.",
                    home);

            return Defence(ctx, st, home);
        }

        // ------------------------------------------------------------------ rows 11-14, the encounter and everything after

        private static ObjectiveView Defence(SimContext ctx, SimState st, Vec2 home)
        {
            var d = ctx.Data;
            var e = st.Engineer;
            var op = st.Opening;
            var t = OpeningRules.Tuning(d);
            d.TryTurret("turret", out var spec);
            var range = spec != null ? Num(spec.RangeTiles) : "9";
            var kw = spec != null ? Num(spec.PowerKw) : "20";

            var turrets = new List<Machine>();
            OpeningRules.LiveTurrets(ctx, st, turrets);
            var first = FirstLoaded(turrets);

            if (op.Status == OpeningStatus.Pending || op.Status == OpeningStatus.Deferred)
            {
                var bullets = Recipe(d, "hand-bullets");
                var perCraft = bullets != null ? ProductionRules.Yield(bullets) : 10;

                // 11 — make the first ammunition.
                if (first == null && e.Inv[ItemId.Magazine] < 1)
                    return new ObjectiveView("opening-workshop", "Make turret ammunition",
                        "Open Home Workshop → Craft " + perCraft + " bullets",
                        "Make " + perCraft + " bullets (" + Num(bullets != null ? bullets.Seconds : 0)
                        + " seconds). Open Backpack and expand Home Workshop while near Home. Stay nearby until "
                        + "it finishes; the bullets appear in your Backpack. A turret holds " + Hopper(d)
                        + ", so keep crafting while you gather.",
                        true, home, Inputs(ctx, st, bullets));

                // 12 — build it.
                if (first == null)
                {
                    var step = Build(ctx, st, "turret", "Prepare your first turret",
                        "Prepare your first turret. Build it near your base and fill it with ammunition.", home);
                    return With(step, "Turrets draw " + kw + " kW: place it within coverage of a connected Pole. While "
                        + "placing, the faint circle is its " + range + "-tile reach and the solid outline inside it is the "
                        + "ground it can actually see and shoot; a warning appears if buildings would blind it. Cover the "
                        + "open ground attackers must cross to reach the Home core. Once it is fully loaded, a small enemy "
                        + "group will test it. " + step.Detail);
                }

                // 13 — power it.
                if (!PowerQueries.Supplied(ctx, st, first.Id))
                    return new ObjectiveView("opening-power", "Connect your turret power",
                        "Place Poles so your turret sits within coverage of your powered network",
                        "Turrets draw " + kw + " kW and hold fire without power. It reports “"
                        + ProductionQueries.Status(ctx, st, first.Id).Text
                        + "”. Poles connect by visible cables within 8 tiles; the Generator must have fuel. Once it is "
                        + "powered and fully loaded, a small enemy group will test it.",
                        true, Centre(first), NoMaterials);

                // A deferred encounter has a prepared turret already: say when it is coming instead of row 14.
                if (op.Status == OpeningStatus.Deferred && TurretQueries.Ready(ctx, st, first.Id))
                {
                    var wait = (int)Math.Max(0, Math.Ceiling(op.DeferredUntil - st.T));
                    return At("opening-attack", "Enemy group delayed",
                        "A raid is under way. The small enemy group will come once the area is safe (about " + wait + " s).",
                        (op.DeferNotice.Length > 0 ? op.DeferNotice + " " : "")
                        + "Your turret is loaded and will fire automatically within " + range
                        + " tiles. Defend the raid that is here first; the introductory group waits, and ordinary raids "
                        + "keep their own schedule.",
                        Centre(first));
                }

                // 14 — fill it.
                var cap = TurretHopper.Capacity(d, first);
                var carried = (int)Math.Floor(e.Inv[ItemId.Magazine]);
                var missing = Math.Max(0, (int)cap - first.Rounds);
                return new ObjectiveView("opening-workshop", "Prepare your first turret",
                    "Prepare your first turret. Build it near your base and fill it with ammunition.",
                    Loaded(d, first) + " · " + missing + " more to fill"
                    + (carried < missing ? " · craft " + (missing - carried) + " more bullets at Home Workshop ("
                        + perCraft + " per craft)" : "")
                    + ". It is powered (" + kw + " kW). Open the turret inventory, select bullets, choose a quantity and "
                    + "Load (1 item = 1 bullet); belts can fill it later. Once it is fully loaded, a small enemy group will test it.",
                    true, Centre(first),
                    new[] { new ObjectiveMaterial(Items.Key(ItemId.Magazine), Name(d, ItemId.Magazine), missing, carried,
                        Math.Min(missing, OpeningRules.HomeStock(st, ItemId.Magazine))) });
            }

            var guardedTurret = st.MachineById(op.TurretId);
            if (guardedTurret == null || TurretQueries.Hp(ctx, st, guardedTurret.Id) <= 0) guardedTurret = first;
            var at = guardedTurret != null ? Centre(guardedTurret) : home;
            var loadedPrefix = guardedTurret != null ? Loaded(d, guardedTurret) : "";
            var direction = DirectionOf(ctx, st);

            if (op.Status == OpeningStatus.Scheduled)
            {
                var view = Encounter(ctx, st);
                return At("opening-attack", "Small enemy group approaching",
                    "Small enemy group approaching from the " + direction + ". Stay near your turret and help defend.",
                    "Arrives in " + view.SecondsLeft + " s · about " + view.Count + " basic enemies from the " + direction
                    + " marker. Your turret fires automatically within " + range + " tiles"
                    + (loadedPrefix.Length > 0 ? " · " + loadedPrefix : "")
                    + "; use your Rifle on anything that gets past it." + Uncovered(ctx, st, guardedTurret, spec),
                    view.HasOrigin ? view.Origin : at);
            }

            if (op.Status == OpeningStatus.Active)
                return At("opening-attack", "Defend your turret",
                    "Small enemy group attacking from the " + direction + ". Stay near your turret and help defend.",
                    (loadedPrefix.Length > 0 ? loadedPrefix + " · " : "") + op.Shots
                    + " bullets fired so far. Reload by hand if it runs dry; the group withdraws once beaten or after "
                    + Num(t.MaxDurationS / 60) + " minutes." + Uncovered(ctx, st, guardedTurret, spec),
                    at);

            var ended = op.Status == OpeningStatus.Repelled || op.Status == OpeningStatus.Lost;
            if (ended && op.EndedAt >= 0 && st.T < op.EndedAt + t.AckS)
                return At("opening-attack", op.Status == OpeningStatus.Repelled ? "Attack repelled" : "Attack over",
                    (op.Status == OpeningStatus.Repelled ? "Attack repelled. " : "") + "Your turret used " + op.Shots
                    + " bullets. Connect ammunition production to keep it supplied.",
                    (loadedPrefix.Length > 0 ? loadedPrefix + ". " : "")
                    + "Each bullet is one ammunition item. An Assembler set to Bullets, with a belt into the turret, "
                    + "keeps it filled without hand loading.",
                    at);

            // ---------------------------------------------------------- automated resupply (§7.5)
            if (op.SuppliedAt < 0)
            {
                if(st.OpeningResourceVersion>0)
                {
                    var generators=0;Machine emptyGenerator=null;var supplying=0;
                    foreach(var m in st.Machines)if(m.Kind=="generator")
                    {
                        generators++;
                        if(PowerQueries.FuelUnits(st,m.Id)<=0)emptyGenerator=m;
                        if(PowerQueries.GeneratorShareKw(ctx,st,m.Id)>0)supplying++;
                    }
                    if(generators<2)return Build(ctx,st,"generator","Expand power for ammunition","Build a second Generator",home);
                    if(emptyGenerator!=null && supplying<2)return At("opening-power","Fuel your extra Generator","Drag Coal into the Generator fuel slot","Two connected Generators supply 600 kW. Ore processing, Home and ammunition production exceed one Generator's 300 kW.",Centre(emptyGenerator));
                    if(supplying<2)return At("opening-power","Connect your extra Generator","Link both Generators to the production network with Poles","Inspect the power cables and machine supply before adding the Assembler's 100 kW load.",home);
                }
                if (turrets.Count == 0)
                {
                    var lost = op.Status == OpeningStatus.Lost;
                    var step = Build(ctx, st, "turret", lost ? "Rebuild your turret" : "Prepare your first turret",
                        "Build a turret near your base and load it with ammunition", home);
                    return With(step, (lost ? "The attack disabled your defence. " : "") + "Turrets draw " + kw
                        + " kW within Pole coverage; the faint circle is its " + range + "-tile reach and the outline inside "
                        + "it is the ground it can actually cover, with a warning for a blind position. " + step.Detail);
                }

                var ammo = BulletAssembler(ctx, st);
                if (ammo == null)
                {
                    var batch = Recipe(d, "bullet-batch");
                    var steel = batch != null ? ProductionRules.Need(batch, ItemId.Steel) : 2;
                    var copper = batch != null ? ProductionRules.Need(batch, ItemId.Copper) : 1;
                    var yield = batch != null ? ProductionRules.Yield(batch) : 10;
                    var mix = steel + " Steel + " + copper + " Copper make " + yield + " bullets.";

                    // U-D-53 split the reference's single step in two. The Assembler now ships with no recipe, so
                    // "built but unset" is a state the player can sit in, and the second half of "Build 1 Assembler
                    // and set it to Bullets" has to be an instruction they can act on rather than a clause on a
                    // step they already finished.
                    var idle = UnsetBulletProcessor(ctx, st);
                    if (idle != null)
                        return At("opening-ammo", "Set up the " + KindName(d, idle), "Select the Bullets recipe",
                            "A loaded turret runs dry. " + mix + " Feed the machine Steel plates and Copper, then run "
                            + "a belt from its output into the turret (or into a chest with an inserter onward).",
                            Centre(idle));

                    return new ObjectiveView("opening-ammo", "Automate your turret’s ammunition supply",
                        "Build 1 Assembler and set it to Bullets",
                        "A loaded turret runs dry. Feed Steel plates and Copper to a powered Assembler, then run a belt "
                        + "from its output into the turret (or into a chest with an inserter onward). " + mix,
                        true, at, Packed(ctx, st, "assembler"));
                }

                var status = ProductionQueries.Status(ctx, st, ammo.Id);
                var ammoAt = Centre(ammo);
                var buffered = OpeningRules.OutputBuffer(ctx, st, ammo);
                if (status.State != MachineOperatingState.Running && status.State != MachineOperatingState.Throttled && buffered == 0)
                    return At("opening-ammo", "Automate your turret’s ammunition supply", status.Text,
                        "Inspect your bullet Assembler for its current input, power or output shortage. A loaded turret "
                        + "is only a reserve.", ammoAt);

                if (!ReachesAny(ctx, st, ammo, turrets))
                    return At("opening-ammo", "Automate your turret’s ammunition supply",
                        HasRelayReceiver(ctx, st, ammo)
                            ? "Extend the route from the Assembler output to a turret"
                            : "Connect the Assembler output to your turret",
                        "Point a belt away from the Assembler and into the turret; storage in between needs an inserter "
                        + "onward. A route that ends elsewhere does not supply the turret.", ammoAt);

                if (op.ProducedAt < 0 && buffered <= 0)
                    return At("opening-ammo", "Automate your turret’s ammunition supply",
                        "Route connected; waiting for the Assembler to produce",
                        "Follow the first bullets along the belt. A connection alone does not mean the turret has ammunition.",
                        ammoAt);

                return At("opening-ammo", "Automate your turret’s ammunition supply",
                    "Bullets produced; waiting for the first batch to reach the turret",
                    "The objective completes when produced bullets enter the turret through the belt. Keep Steel plates, "
                    + "Copper and generator fuel supplied.", ammoAt);
            }

            if (st.T < op.SuppliedAt + t.SupplyAckS)
                return At("opening-ammo", "Automatic resupply working",
                    "Automatic resupply working. Your production line is replenishing the turret.",
                    "Bullets now arrive without hand loading. Watch the Assembler’s Steel, Copper and power; the "
                    + "reserve is only as deep as its inputs.", at);

            // ---------------------------------------------------------- §7.6 three turrets
            var live = turrets.Count;
            if (live < t.TurretObjective)
            {
                var more = Math.Max(0, t.TurretObjective - live);
                var word = more >= 1 && more <= Words.Length ? Words[more - 1] : more.ToString(CultureInfo.InvariantCulture);
                var step = Build(ctx, st, "turret", "Expand your defences (" + live + "/" + t.TurretObjective + ")",
                    "Larger attacks can approach from any direction. Build " + word + " more turret" + (more == 1 ? "" : "s")
                    + " and spread your defences around the base. Enemies can attack from any direction, so cover "
                    + "different approaches.", home);
                return With(step, "Three is a starting recommendation, not guaranteed protection. Keep turrets supplied, "
                    + "cover different approaches and support them with your Rifle. Existing turrets count; disabled "
                    + "turrets need repair. Materials below are for the next turret. " + step.Detail);
            }

            var emptyTurret = FirstEmpty(turrets);
            if (emptyTurret != null)
                return At("opening-workshop", "Load your new turret",
                    "Load bullets into the empty turret or extend your ammunition belt to it",
                    Loaded(d, emptyTurret) + ". A turret without ammunition covers nothing. Belts, inserters or hand "
                    + "loading all work.", Centre(emptyTurret));

            // ---------------------------------------------------------- §7.7 the first excursion
            if (e.Inv[ItemId.Magazine] < ScoutBullets && !OpeningRules.LeftForFirstCamp(ctx, st))
                return At("opening-workshop", "Prepare to scout",
                    "Carry at least eight bullets for the first camp",
                    "Your turrets are supplied and your Rifle is ready. Check turret ammunition and generator fuel before "
                    + "leaving; the reserve is finite. The nearest freight camp is a short trip from Home.", home);

            return new ObjectiveView("opening-workshop", "Keep your workshop producing",
                "Explore and connect your known destinations",
                "Your existing production is running. Use Projects for restoration and service details.",
                false, Vec2.Zero, NoMaterials);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Reference goal.ts:69 <c>['no','one','two','three']</c>, minus its first entry. This row renders only
        /// while <c>live &lt; TurretObjective</c>, so <c>more</c> is at least 1 and "no more turrets" was
        /// unreachable; the index shifts by one to match.
        /// </summary>
        private static readonly string[] Words = { "one", "two", "three" };

        /// <summary>Reference goal.ts:157: eight bullets before the first camp is worth walking to.</summary>
        private const int ScoutBullets = 8;

        private static readonly ObjectiveMaterial[] NoMaterials = Array.Empty<ObjectiveMaterial>();

        /// <summary>INT-01: the recovery row says the Backpack is lying where the engineer fell, when it is.</summary>
        /// <summary>
        /// The name of the workshop button that brings a downed core back, as the core-down card quotes it. It is
        /// the head of <see cref="UI.WorkshopText.CoreButton"/>'s recommission label; a test holds the two together.
        /// </summary>
        public const string RecommissionButton = "Recommission core";

        private static string RecoveryDetail(SimState st)
        {
            const string resume = "Movement and interaction resume after recovery.";
            return DeathCache.Live(st, out _) == 0
                ? resume
                : resume + " Your Backpack's cargo is lying where you fell: walk back and press E beside it to collect.";
        }

        private static ObjectiveView At(string id, string title, string text, string detail, Vec2 where) =>
            new ObjectiveView(id, title, text, detail, true, where, NoMaterials);

        /// <summary>
        /// One sentence when the approach C-09 actually chose passes outside the turret's reach, "" otherwise.
        /// <see cref="DirectorQueries.OpeningOrigin"/> already steers the group down the approach that passes
        /// NEAREST the prepared turret, so this never fires on a turret the director could have covered — but the
        /// nearest of a poor set can still be far out of range, and until now the only way to discover that was to
        /// watch the group walk past a silent turret and flatten the core.
        /// </summary>
        private static string Uncovered(SimContext ctx, SimState st, Machine turret, TurretDef spec)
        {
            if (turret == null || st.Opening == null || st.Opening.Origin < 0) return "";
            var reach = spec != null ? spec.RangeTiles : 9.0;
            if (!DirectorRules.Target(ctx, st, out var bx, out var by, out var size)) return "";
            var gap = DirectorQueries.Clearance(ctx, st, st.Opening.Origin, turret, bx, by, size);
            if (double.IsInfinity(gap) || gap <= reach) return "";
            return " Their path passes about " + Num(Math.Round(gap)) + " tiles from your turret, which only reaches "
                + Num(reach) + " — it will not fire on them. Build another between the marker and the Home core.";
        }

        private static ObjectiveView With(ObjectiveView step, string detail) =>
            new ObjectiveView(step.Id, step.Title, step.Text, detail, step.HasLocation, step.Location, step.Materials);

        private static string Secs(double s) =>
            ((int)Math.Ceiling(Math.Max(0, s))).ToString(CultureInfo.InvariantCulture);

        /// <summary>A whole number where the value is whole, so "20 kW" never reads "20.0 kW".</summary>
        private static string Num(double v) => v == Math.Floor(v)
            ? ((long)v).ToString(CultureInfo.InvariantCulture)
            : v.ToString("0.##", CultureInfo.InvariantCulture);

        private static string Name(GameData d, ItemId id) => d.Item(id).DisplayName;

        private static string Hopper(GameData d) => d.TryTurret("turret", out var s) ? Num(s.Hopper) : "50";

        /// <summary>Reference goal.ts:128 <c>loadedText</c>, already in bullets.</summary>
        private static string Loaded(GameData d, Machine m) =>
            "Loaded " + m.Rounds + " / " + Num(TurretHopper.Capacity(d, m)) + " bullets";

        private static Vec2 Centre(Machine m)
        {
            var (w, h) = m.Dimensions;
            return new Vec2(m.X + w / 2.0, m.Y + h / 2.0);
        }

        private static Recipe Recipe(GameData d, string key) => d.TryRecipe(key, out var r) ? r : null;

        private static Machine FirstOfKind(SimState st, string kind)
        {
            for (var i = 0; i < st.Machines.Count; i++)
                if (string.Equals(st.Machines[i].Kind, kind, StringComparison.Ordinal)) return st.Machines[i];
            return null;
        }

        /// <summary>Reference <c>turrets.find(m=&gt;m.inv.rounds&gt;0) ?? turrets[0]</c>.</summary>
        private static Machine FirstLoaded(List<Machine> turrets)
        {
            for (var i = 0; i < turrets.Count; i++) if (turrets[i].Rounds > 0) return turrets[i];
            return turrets.Count > 0 ? turrets[0] : null;
        }

        private static Machine FirstEmpty(List<Machine> turrets)
        {
            for (var i = 0; i < turrets.Count; i++) if (turrets[i].Rounds <= 0) return turrets[i];
            return null;
        }

        private static Machine BulletAssembler(SimContext ctx, SimState st)
        {
            for (var i = 0; i < st.Machines.Count; i++)
                if (OpeningRules.IsBulletProducer(ctx, st, st.Machines[i])) return st.Machines[i];
            return null;
        }

        /// <summary>
        /// A placed machine that COULD run <c>bullet-batch</c> but has no recipe on it. U-D-53 cleared the
        /// Assembler's data default, so <see cref="BulletAssembler"/> — which needs a live recipe — stopped
        /// being the answer to "has the player built one?"; this is.
        /// </summary>
        private static Machine UnsetBulletProcessor(SimContext ctx, SimState st)
        {
            var d = ctx.Data;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (ProductionRules.RecipeOf(d, st, m) != null) continue;
                if (ProductionRules.Supports(d, m, "bullet-batch")) return m;
            }
            return null;
        }

        /// <summary>A kind's player-facing name ("Assembler", "Assembler Mk2"), for a step that points at one.</summary>
        private static string KindName(GameData d, Machine m) =>
            d.TryMachine(m.Kind, out var s) && s.DisplayName.Length > 0 ? s.DisplayName : m.Kind;

        private static bool ReachesAny(SimContext ctx, SimState st, Machine source, List<Machine> targets)
        {
            var depth = OpeningRules.Tuning(ctx.Data).SupplyChainDepth;
            for (var i = 0; i < targets.Count; i++)
                if (OpeningRules.SupplyChainReaches(ctx, st, source, targets[i], depth)) return true;
            return false;
        }

        /// <summary>Reference <c>receiversOf(ammo).filter(kind in turret|chest|tramstop|depot)</c>: is the belt going anywhere at all?</summary>
        private static bool HasRelayReceiver(SimContext ctx, SimState st, Machine source)
        {
            var routes = FlowRules.Destinations(st);
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var b = st.Machines[i];
                if (!FlowRules.IsConveyor(b.Kind)) continue;
                var behind = ProductionRules.MachineAt(st, b.X - Dirs.DX[(int)b.Dir], b.Y - Dirs.DY[(int)b.Dir]);
                if (behind == null || behind.Id != source.Id) continue;
                var route = FlowRules.RouteOf(routes, b.Id);
                if (route == null) continue;
                for (var j = 0; j < route.Ends.Count; j++)
                {
                    var k = route.Ends[j].Kind;
                    if (OpeningRules.IsRelay(k) || ctx.Data.TryTurret(k, out _)) return true;
                }
            }
            return false;
        }

        /// <summary>The kind a belt run may pass THROUGH on its way to the machine an objective is looking for.</summary>
        private const string BufferKind = "chest";

        /// <summary>
        /// Every machine a belt run leaving <paramref name="source"/> ends at — ONE hop, through no buffer.
        /// The hop is the reference’s <c>receiversOf</c>: a belt is fed from the tile BEHIND its arrow
        /// (<see cref="FlowPhase.LoadConveyor"/>), so the runs that leave a machine are the belts whose behind-tile
        /// is that machine, and <see cref="FlowRules.Destinations"/> already walked each run to its ends.
        /// </summary>
        private static void DirectEnds(SimState st, Machine source, List<Machine> into)
        {
            into.Clear();
            var routes = FlowRules.Destinations(st);
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var b = st.Machines[i];
                if (!FlowRules.IsConveyor(b.Kind)) continue;
                var behind = ProductionRules.MachineAt(st, b.X - Dirs.DX[(int)b.Dir], b.Y - Dirs.DY[(int)b.Dir]);
                if (behind == null || behind.Id != source.Id) continue;
                var route = FlowRules.RouteOf(routes, b.Id);
                if (route == null) continue;
                for (var j = 0; j < route.Ends.Count; j++)
                {
                    var end = route.Ends[j];
                    var have = false;
                    for (var q = 0; q < into.Count; q++) if (into[q].Id == end.Id) { have = true; break; }
                    if (!have) into.Add(end);
                }
            }
        }

        /// <summary>
        /// Reference <c>receiversOf(excavator).some(m =&gt; m.kind === 'chest')</c>, widened by U-D-55 to the
        /// buffered lines players actually build. A Supply chest takes anything (<see cref="FlowRules.Wants"/>)
        /// AND drains onto the next belt (<see cref="FlowPhase.SourceCount"/>), so
        /// <c>Excavator → chest → Foundry</c> carries ore exactly as <c>Excavator → Foundry</c> does;
        /// the chain used to see one hop, find a chest where it wanted a Foundry, and call a working line unbuilt.
        /// The walk hops through a chest and NOTHING else: a processor in the middle changes the item, and a
        /// turret, generator or cannon consumes it.
        /// </summary>
        private static bool ReachesKind(SimState st, Machine source, string kind)
        {
            var seen = new List<int> { source.Id };
            var queue = new List<Machine> { source };
            var ends = new List<Machine>();
            for (var q = 0; q < queue.Count; q++)
            {
                DirectEnds(st, queue[q], ends);
                for (var i = 0; i < ends.Count; i++)
                {
                    var end = ends[i];
                    if (string.Equals(end.Kind, kind, StringComparison.Ordinal)) return true;
                    // `seen` is the cycle guard: two chests belted into each other is a legal build.
                    if (!string.Equals(end.Kind, BufferKind, StringComparison.Ordinal)) continue;
                    if (seen.Contains(end.Id)) continue;
                    seen.Add(end.Id);
                    queue.Add(end);
                }
            }
            return false;
        }

        /// <summary>
        /// The one chest BOTH machines belt straight into, or null. Deliberately not <see cref="ReachesKind"/>:
        /// through a buffer every downstream chest is “reached” from the Excavator too, and a line that
        /// merely passes through a chest on its way to the Foundry is working, not deadlocked.
        /// </summary>
        private static Machine SharedSink(SimState st, Machine a, Machine b)
        {
            var left = new List<Machine>();
            DirectEnds(st, a, left);
            var right = new List<Machine>();
            DirectEnds(st, b, right);
            for (var i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i].Kind, BufferKind, StringComparison.Ordinal)) continue;
                for (var j = 0; j < right.Count; j++)
                    if (right[j].Id == left[i].Id) return left[i];
            }
            return null;
        }

        /// <summary>The substation of the district a position is in (<see cref="Districts"/> owns the rule).</summary>
        private static SiteRecord NearestSubstation(SimContext ctx, double cx, double cy) =>
            Districts.SubstationAt(ctx, cx, cy);

        /// <summary>
        /// The reference's <c>campaignGrid(st).blocks[home].supply &gt; 0</c> (goal.ts:113), asked of the port's grid:
        /// court D55 makes the authored substation a reach node, so this is its own circuit having supply.
        /// </summary>
        private static bool SubstationLive(SimContext ctx, SimState st, SiteRecord site)
        {
            var c = PowerGrid.Of(ctx, st).OfSite(site.Id);
            return c != null && c.Supply > 0;
        }

        /// <summary>How many authored streetlights the substation owns (their nearest substation site is this one).</summary>
        private static int SubstationLights(SimContext ctx, SiteRecord site)
        {
            var lights = StreetLights.Sites(ctx);
            var n = 0;
            for (var i = 0; i < lights.Count; i++)
                if (ReferenceEquals(PowerGrid.SubstationOf(ctx, lights[i]), site)) n++;
            return n;
        }

        // ------------------------------------------------------------------ R5: where do I get this?

        /// <summary>How far the ring scan looks for a minable tile when no authored resource rect matches.</summary>
        public const int SourceScanTiles = 64;

        /// <summary>The station of a recipe the engineer can run by hand, with no machine standing (catalogue §3).</summary>
        public const string HandStation = "Home workshop";

        /// <summary>Said when the loaded map yields an item nowhere at all.</summary>
        public const string NoSourceText = "Nothing on this map yields ";

        /// <summary>
        /// PORT ADDITION (correction pass R5). Where the engineer can actually get more of <paramref name="item"/>
        /// RIGHT NOW, in the order a player would try it:
        ///
        ///   1. the nearest authored <see cref="SiteKind.Resource"/> rect whose item matches (riverfront.ts
        ///      resources / HQ_PATCHES, imported as sites by RegionImporter);
        ///   2. failing that, the nearest minable tile of the loaded geometry inside <see cref="SourceScanTiles"/>,
        ///      asked through <see cref="Mining.TryTile"/> so an <see cref="IMinableGeometry"/> gets to decide and
        ///      an already-dug tile is skipped;
        ///   3. failing that, the nearest Home chest that already holds some;
        ///   4. failing that, the recipe that makes it — the hand recipe in preference to a machine's;
        ///   5. and when none of those exist, <see cref="MaterialSource.Found"/> is false and the text says so.
        ///
        /// Direction and distance are measured from the ENGINEER, not from Home, because the sentence is an
        /// instruction to walk. Nothing is granted, nothing is cached and no constant is invented: the same state
        /// always produces the same sentence.
        /// </summary>
        public static MaterialSource Source(SimContext ctx, SimState st, ItemId item)
        {
            if (ctx == null || st == null || ctx.Data == null) return MaterialSource.None;
            var d = ctx.Data;
            var at = st.Engineer.Pos;

            if(st.OpeningResourceVersion>0 && (item==ItemId.Steel || item==ItemId.Copper))
            {
                var ore=item==ItemId.Steel?ItemId.IronOre:ItemId.CopperOre;
                if(NearestMinable(ctx,st,ore,at,out var oreTile,out _))
                {
                    var hint=Walk("tile","mine",Name(d,ore),at,oreTile,"then smelt it at Home Workshop; 1 ore makes 1 plate without electricity");
                    return new MaterialSource(true,"craft",Name(d,ore),hint.Compass,hint.Tiles,"mine "+Name(d,ore)+" → smelt at Home",hint.Text,oreTile);
                }
            }
            if (NearestMinable(ctx, st, item, at, out var tile, out var cls))
                return Walk("tile", "mine", Name(d,item) + " on " + TileWords(cls), at, tile, "select Hands, then hold the primary action on it; no power needed");

            var chest = NearestStock(st, item, at);
            if (chest != null)
                return Walk("home", "take it from", "a Home supply chest", at, Centre(chest), "open it and take what you need");

            var made = Maker(d, item);
            if (made != null)
            {
                var where = string.IsNullOrEmpty(made.Station) ? "the workshop" : "the " + made.Station;
                return new MaterialSource(true, "craft", where, "", 0,
                    "craft it at " + where,
                    "Craft " + made.DisplayName + " at " + where + ".");
            }

            return new MaterialSource(false, "", "", "", 0, "no source on this map",
                NoSourceText + Name(d, item) + ", so this step cannot be finished on this map.");
        }

        /// <summary>
        /// Fill in <see cref="ObjectiveMaterial.Source"/> for every row the Backpack is short of, and append the
        /// first one's whole sentence to the "Why this next?" detail. Rows that are already satisfied and rows
        /// whose key is a packed machine (their own cost rows carry the answer) are left exactly as the chain
        /// built them, so an objective with nothing missing is byte-for-byte what it was before R5.
        /// </summary>
        private static ObjectiveView Explain(SimContext ctx, SimState st, ObjectiveView step)
        {
            var list = step.Materials;
            if (list == null || list.Count == 0) return step;

            var rows = new ObjectiveMaterial[list.Count];
            var sentence = "";
            var location = step.Location;
            var hasLocation = step.HasLocation;
            var changed = false;
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                rows[i] = m;
                if (m.Available >= m.Required) continue;
                if (!Items.TryParse(m.Item, out var id)) continue;
                var src = Source(ctx, st, id);
                rows[i] = new ObjectiveMaterial(m.Item, m.DisplayName, m.Required, m.Available, m.AtHome, src.Short);
                changed = true;
                if (sentence.Length == 0) { sentence = m.DisplayName + ": " + src.Text; if(src.HasLocation){location=src.Location;hasLocation=true;} }
            }
            if (!changed) return step;

            var detail = step.Detail ?? "";
            if (sentence.Length > 0) detail = detail.Length > 0 ? detail + " " + sentence : sentence;
            return new ObjectiveView(step.Id, step.Title, step.Text, detail, hasLocation, location, rows);
        }

        /// <summary>One sentence about somewhere to walk, plus the short form the goal-card row shows.</summary>
        private static MaterialSource Walk(string kind, string verb, string what, Vec2 at, Vec2 target, string action)
        {
            var tiles = (int)Math.Round(OpeningRules.Dist(target.X, target.Y, at.X, at.Y));
            var compass = OpeningRules.Compass(DirectorQueries.Direction(target, at.X, at.Y));
            var n = tiles.ToString(CultureInfo.InvariantCulture);
            var here = tiles <= 1;
            var where = here ? "right where you stand" : n + " tiles " + compass + " of you";
            var shortForm = verb + " " + what + (here ? " here" : " " + n + " tiles " + compass);
            var text = char.ToUpperInvariant(verb[0]) + verb.Substring(1) + " " + what + " " + where + " — " + action + ".";
            return new MaterialSource(true, kind, what, here ? "" : compass, tiles, shortForm, text, target);
        }

        /// <summary>The nearest authored resource rect that yields exactly this item.</summary>
        private static SiteRecord NearestResource(SimContext ctx, ItemId item, Vec2 at)
        {
            if (ctx.Sites == null) return null;
            var key = Items.Key(item);
            SiteRecord best = null;
            var score = double.PositiveInfinity;
            foreach (var s in ctx.Sites.OfKind(SiteKind.Resource))
            {
                if (!string.Equals(s.Item, key, StringComparison.Ordinal)) continue;
                var c = s.Centre;
                var dd = OpeningRules.Dist(c.X, c.Y, at.X, at.Y);
                if (dd >= score) continue;
                score = dd;
                best = s;
            }
            return best;
        }

        /// <summary>
        /// The nearest tile that still yields this item, found by expanding rings from the engineer so the search
        /// stops at the first hit instead of sweeping the whole city. <see cref="SourceScanTiles"/> bounds it: a
        /// patch further away than that is not the next achievable action anyway.
        /// </summary>
        private static bool NearestMinable(SimContext ctx, SimState st, ItemId item, Vec2 at, out Vec2 where, out TileClass cls)
        {
            where = Vec2.Zero; cls = TileClass.Ground;
            if(ctx.Geometry==null)return false;
            int ox=(int)Math.Floor(at.X),oy=(int)Math.Floor(at.Y),width=ctx.Geometry.Width;
            var queue=new Queue<(int x,int y,int distance)>();
            var seen=new HashSet<int>();queue.Enqueue((ox,oy,0));seen.Add(oy*width+ox);
            while(queue.Count>0)
            {
                var p=queue.Dequeue();
                if(!ctx.Geometry.Solid(p.x,p.y) && ProductionRules.MachineAt(st,p.x,p.y)==null
                   && Mining.TryTile(ctx,st,p.x,p.y,out var got,out _) && got==item)
                { where=new Vec2(p.x+.5,p.y+.5);cls=Ground.TileAt(ctx,st,p.x,p.y);return true; }
                if(p.distance>=SourceScanTiles)continue;
                for(int i=0;i<4;i++)
                {
                    int x=p.x+(i==0?1:i==1?-1:0),y=p.y+(i==2?1:i==3?-1:0);
                    if(!Ground.InBounds(ctx,x,y) || !Ground.CanStand(ctx,st,x+.5,y+.5) || !seen.Add(y*width+x))continue;
                    queue.Enqueue((x,y,p.distance+1));
                }
            }
            return false;
        }

        /// <summary>What the player calls the tile they are being sent to (UI_AND_ONBOARDING.md §7.5 wording).</summary>
        private static string TileWords(TileClass k)
        {
            switch (k)
            {
                case TileClass.Rubble: return "salvage rubble";
                case TileClass.Patch: return "the patch";
                case TileClass.Deposit: return "the deposit";
                default: return "the ground";
            }
        }

        /// <summary>The nearest Home chest that already holds at least one of the item.</summary>
        private static Machine NearestStock(SimState st, ItemId item, Vec2 at)
        {
            Machine best = null;
            var score = double.PositiveInfinity;
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!string.Equals(m.Kind, "chest", StringComparison.Ordinal)) continue;
                if (m.Inv[item] < 1) continue;
                var c = Centre(m);
                var dd = OpeningRules.Dist(c.X, c.Y, at.X, at.Y);
                if (dd >= score) continue;
                score = dd;
                best = m;
            }
            return best;
        }

        /// <summary>The recipe that makes this item; the hand recipe wins, because it needs no machine standing.</summary>
        private static Recipe Maker(GameData d, ItemId item)
        {
            Recipe best = null;
            for (var i = 0; i < d.Recipes.Count; i++)
            {
                var r = d.Recipes[i];
                if (r.Outputs == null) continue;
                var makes = false;
                for (var j = 0; j < r.Outputs.Count; j++)
                {
                    if (r.Outputs[j].Item != item || r.Outputs[j].Count <= 0) continue;
                    makes = true;
                    break;
                }
                if (!makes) continue;
                if (string.Equals(r.Station, HandStation, StringComparison.Ordinal)) return r;
                if (best == null) best = r;
            }
            return best;
        }

        // ------------------------------------------------------------------ materials

        /// <summary>Reference goal.ts:91 <c>buildStep</c>: cost, shortage text and the shared "Opening order" reminder.</summary>
        /// <summary>The Lamp's material row: the packed Lamp when one is carried, otherwise what it costs.</summary>
        private static IReadOnlyList<ObjectiveMaterial> LampRows(SimContext ctx, SimState st) =>
            st.Engineer.Inv[new ItemKey("lamp")] >= 1 ? One(ctx, st, "lamp", 1) : Packed(ctx, st, "lamp");

        private static ObjectiveView Build(SimContext ctx, SimState st, string kind, string title, string instruction, Vec2 where)
        {
            var d = ctx.Data;
            var e = st.Engineer;
            var packed = e.Inv[new ItemKey(kind)] >= 1;
            var rows = packed ? One(ctx, st, kind, 1) : Packed(ctx, st, kind);

            var shortage = "";
            if (!packed && d.TryMachine(kind, out var spec) && spec.Cost != null)
            {
                for (var i = 0; i < spec.Cost.Count; i++)
                {
                    var need = spec.Cost[i].Count - (int)Math.Floor(e.Inv[spec.Cost[i].Item]);
                    if (need <= 0) continue;
                    var stock = Math.Min(need, OpeningRules.HomeStock(st, spec.Cost[i].Item));
                    shortage += (shortage.Length > 0 ? " + " : "") + need + " " + Name(d, spec.Cost[i].Item)
                        + " (" + stock + " available at Home)";
                }
            }

            var detail = (shortage.Length > 0
                ? "Still need " + shortage + ". Hold left-click on salvage to gather, or collect stored supplies."
                : "Materials ready in Backpack. Open Build to place it.")
                + " Opening order: 1 Generator → 1 Excavator → 1 Supply chest → Belts into the chest.";
            return new ObjectiveView("opening-workshop", title, instruction, detail, true, where, rows);
        }

        /// <summary>The machine's build cost as material rows, or the packed machine itself when one is carried.</summary>
        private static IReadOnlyList<ObjectiveMaterial> Packed(SimContext ctx, SimState st, string kind)
        {
            var d = ctx.Data;
            var e = st.Engineer;
            if (e.Inv[new ItemKey(kind)] >= 1) return One(ctx, st, kind, 1);
            if (!d.TryMachine(kind, out var spec) || spec.Cost == null) return NoMaterials;
            var rows = new List<ObjectiveMaterial>(spec.Cost.Count);
            for (var i = 0; i < spec.Cost.Count; i++)
            {
                if (spec.Cost[i].Count <= 0) continue;
                rows.Add(Row(d, st, spec.Cost[i].Item, spec.Cost[i].Count));
            }
            return rows;
        }

        private static IReadOnlyList<ObjectiveMaterial> Inputs(SimContext ctx, SimState st, Recipe r)
        {
            if (r == null || r.Inputs == null) return NoMaterials;
            var rows = new List<ObjectiveMaterial>(r.Inputs.Count);
            for (var i = 0; i < r.Inputs.Count; i++)
            {
                if (r.Inputs[i].Count <= 0) continue;
                rows.Add(Row(ctx.Data, st, r.Inputs[i].Item, r.Inputs[i].Count));
            }
            return rows;
        }

        private static IReadOnlyList<ObjectiveMaterial> One(SimContext ctx, SimState st, string key, int n)
        {
            var d = ctx.Data;
            var have = (int)Math.Floor(st.Engineer.Inv[new ItemKey(key)]);
            if (Items.TryParse(key, out var id)) return new[] { Row(d, st, id, n) };
            var name = d.TryMachine(key, out var spec) ? spec.DisplayName : key;
            return new[] { new ObjectiveMaterial(key, name, n, have, 0) };
        }

        private static ObjectiveMaterial Row(GameData d, SimState st, ItemId id, int required) =>
            new ObjectiveMaterial(Items.Key(id), Name(d, id), required,
                (int)Math.Floor(st.Engineer.Inv[id]), OpeningRules.HomeStock(st, id));
    }
}
