using System.Collections.Generic;
using NUnit.Framework;
using Relight.Sim.Tests.Combat;

namespace Relight.Sim.Tests.Campaign
{
    /// <summary>
    /// C-09's setup. The map, the context and the machine helpers come from
    /// <see cref="RaidFixture"/> by reference (wave-2 W-B's instruction: reuse it, never edit it) — the director's
    /// radii need the 160x160 map, and the opening encounter is scheduled through the same director.
    ///
    /// What is added here is the Home core (the opening needs a placed core) and a phase list that ends with
    /// <see cref="OpeningPhase"/>, which is where it sits in the real composition: Campaign runs after Combat, so
    /// the opening always reads the wave state this tick produced.
    /// </summary>
    public static class OpeningFixture
    {
        public const double Dt = RaidFixture.Dt;

        public static SimContext Context() => RaidFixture.Context();

        /// <summary>A fresh state with the core placed and the director seeded — the state a new game starts from.</summary>
        public static SimState State(SimContext ctx, int seed = 7)
        {
            var st = RaidFixture.State(ctx, seed);
            HomeCore.Ensure(ctx, st);
            new OpeningInitializer().Init(ctx, st);
            return st;
        }

        /// <summary>
        /// Power, turrets and the opening, without the enemy or director phases: a state-machine test drives the
        /// waves itself so nothing else can move while it is looking.
        /// </summary>
        public static List<ITickPhase> Quiet() =>
            new List<ITickPhase> { new PowerPhase(), new TurretPhase(), new OpeningPhase() };

        /// <summary>The whole real order, for the checks that must see the director and the opening interact.</summary>
        public static List<ITickPhase> Full()
        {
            var list = RaidFixture.Phases();
            list.Add(new OpeningPhase());
            return list;
        }

        public static void Run(SimContext ctx, SimState st, int ticks, List<ITickPhase> phases = null)
        {
            phases = phases ?? Quiet();
            for (var i = 0; i < ticks; i++)
            {
                for (var p = 0; p < phases.Count; p++) phases[p].Tick(ctx, st, Dt);
                st.Tick++;
                st.T += Dt;
            }
        }

        public static void Seconds(SimContext ctx, SimState st, double seconds, List<ITickPhase> phases = null) =>
            Run(ctx, st, (int)System.Math.Round(seconds / Dt), phases);

        /// <summary>A powered, fully loaded turret close to the core — the one the encounter is meant to test.</summary>
        public static Machine ReadyTurret(SimContext ctx, SimState st, int dx = 6, int dy = 0)
        {
            var t = RaidFixture.Turret(ctx, st, RaidFixture.CoreX + dx, RaidFixture.CoreY + dy);
            Run(ctx, st, 1);   // one tick of PowerPhase, so the circuit exists and the turret counts as ready
            Assert.That(TurretQueries.Ready(ctx, st, t.Id), Is.True, "the fixture turret must start ready");
            return t;
        }

        /// <summary>
        /// Marks a miner as digging without needing ore under it. The chain's row 7 asks
        /// <see cref="ProductionQueries.Status"/> for Running or Throttled, which is the side-table stall plus live
        /// power — so a test states the stall and lets the real power rules answer the rest.
        /// </summary>
        public static Machine Dig(SimContext ctx, SimState st, Machine m)
        {
            st.Production.Of(m.Id).Stall = (int)MachineOperatingState.Running;
            return m;
        }

        /// <summary>
        /// The opening base as it stands the moment the Rifle is crafted: rows 1-9 of the chain all satisfied, so
        /// the next objective is row 10, "Equip your crafted Rifle". Nothing here charges a build cost; these are
        /// the facts the chain reads, placed directly.
        /// </summary>
        public static void ToRifle(SimContext ctx, SimState st)
        {
            var x = RaidFixture.CoreX + 10;
            var y = RaidFixture.CoreY;
            var gen = RaidFixture.Add(ctx, st, "generator", x, y);
            gen.Inv.Add(ItemId.Coal, 50);
            RaidFixture.Add(ctx, st, "pole", x + 2, y);
            var dig = RaidFixture.Add(ctx, st, "excavator", x + 4, y);
            RaidFixture.Add(ctx, st, "belt", x + 6, y, Dir.E);
            RaidFixture.Add(ctx, st, "belt", x + 7, y, Dir.E);
            RaidFixture.Add(ctx, st, "chest", x + 8, y);
            RaidFixture.Add(ctx, st, "lamp", x + 2, y + 2);           // row 8b (L-02): one Lamp, in the pole's reach
            Run(ctx, st, 1);
            Dig(ctx, st, dig);
            Assert.That(PowerQueries.Supplied(ctx, st, dig.Id), Is.True, "the fixture excavator must be powered");
            WeaponRules.Create(st, "rifle");
            Assert.That(st.Weapons.Owned, Is.Not.Empty);
        }

        /// <summary>A major raid that exists only to make <see cref="Director.Blocked"/> true.</summary>
        public static void FakeMajor(SimState st)
        {
            st.Director.Major = new MajorRaid
            {
                Id = st.Director.NextId++, Origin = 0, StartsAt = st.T, EndsAt = st.T + 300,
                Remaining = 1, Total = 1, Committed = true,
            };
        }

        public static void ClearMajor(SimState st) => st.Director.Major = null;

        /// <summary>Every body of a group killed at once, the way the turret would have done it.</summary>
        public static void Kill(SimState st, int group)
        {
            var list = st.Enemies.Actors;
            for (var i = list.Count - 1; i >= 0; i--) if (list[i].Group == group) list.RemoveAt(i);
            if (st.Director.Minor != null && st.Director.Minor.Id == group) st.Director.Minor = null;
        }

        /// <summary>Drives the encounter to <see cref="OpeningStatus.Active"/> and answers with the staged turret.</summary>
        public static Machine ToActive(SimContext ctx, SimState st)
        {
            var turret = ReadyTurret(ctx, st);
            Run(ctx, st, 1);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Scheduled));
            Seconds(ctx, st, OpeningRules.Tuning(ctx.Data).WarningS + 0.5);
            Assert.That(st.Opening.Status, Is.EqualTo(OpeningStatus.Active));
            return turret;
        }

        /// <summary>A save/load of the whole state through the real serialiser.</summary>
        public static SimState RoundTrip(SimContext ctx, SimState st)
        {
            var text = SaveSerializer.WriteText(st, ctx.Data, "2026-09-14T00:00:00Z");
            var load = SaveSerializer.ReadText(text, ctx.Data);
            Assert.That(load.Ok, Is.True, load.Reason);
            return load.State;
        }
    }
}
