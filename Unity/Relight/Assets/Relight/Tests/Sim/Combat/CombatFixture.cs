using System.Collections.Generic;
using NUnit.Framework;

namespace Relight.Sim.Tests
{
    /// <summary>
    /// The engineer's own phases and handlers on the flat street map: inventory, hand craft, mining and the weapon.
    /// Deliberately NOT <see cref="SimComposition"/> — the production, power and campaign slots belong to other
    /// workers and none of them is needed to fire a rifle.
    /// </summary>
    public static class CombatFixture
    {
        public const double Dt = Fixture.Dt;

        public static SimContext Context() => Fixture.Context();

        public static SimState State(SimContext ctx)
        {
            var st = Fixture.State(ctx);
            new WeaponInitializer().Init(ctx, st);
            return st;
        }

        public static void Run(SimContext ctx, SimState st, int ticks)
        {
            var phases = new List<ITickPhase> { new HandCraftPhase(), new MiningPhase(), new WeaponPhase() };
            for (var i = 0; i < ticks; i++)
            {
                for (var p = 0; p < phases.Count; p++) phases[p].Tick(ctx, st, Dt);
                st.Tick++;
                st.T += Dt;
            }
        }

        public static CommandResult Apply(SimContext ctx, SimState st, Command c)
        {
            var handlers = new List<ICommandHandler>
            {
                new InventoryCommandHandler(), new HandCraftHandler(), new MachineTransferHandler(),
                new PlacementHandler(), new MiningHandler(), new EquipmentHandler(), new WeaponHandler(),
            };
            for (var i = 0; i < handlers.Count; i++)
                if (handlers[i].TryApply(ctx, st, c, out var r)) return r;
            return CommandResult.Refuse("no handler");
        }

        /// <summary>The Home workshop next to the engineer, so the rifle recipe may run.</summary>
        public static Machine Home(SimContext ctx, SimState st) => Fixture.Place(ctx, st, "depot", 18, 16);

        /// <summary>
        /// Crafts one rifle at Home and returns its instance id (<c>rifle:1</c> for the first). Since U-D-44 the
        /// finished rifle waits in the workshop's output tray, so this collects it too — the engineer is standing
        /// at the workshop, which is where <see cref="Home"/> put it.
        /// </summary>
        public static string CraftRifle(SimContext ctx, SimState st)
        {
            var r = Apply(ctx, st, new CraftRifleCommand());
            Assert.IsTrue(r.Accepted, r.Problem);
            ctx.Data.TryRecipe(WeaponRules.RifleRecipe, out var recipe);
            Run(ctx, st, (int)System.Math.Round(recipe.Seconds / Dt) + 1);
            var q = st.Weapons;
            Assert.IsFalse(q.Crafting, "the craft finished");
            Assert.Greater(q.Owned.Count, 0, "a weapon exists");
            var id = q.Owned[q.Owned.Count - 1].Id;
            Assert.AreEqual(1, st.Hand.Output[new ItemKey(id)], "and it is waiting in the workshop tray");
            var got = Apply(ctx, st, new CollectWorkshopCommand(id, 1));
            Assert.IsTrue(got.Accepted, got.Problem);
            Assert.AreEqual(1, st.Engineer.Inv[new ItemKey(id)], "collected into the Backpack");
            return id;
        }

        /// <summary>
        /// Puts <paramref name="n"/> of an item in the Backpack AND counts it as made, so the conservation ledger
        /// stays balanced: a test that conjures stock must say where it came from.
        /// </summary>
        public static void Grant(SimContext ctx, SimState st, ItemId item, double n)
        {
            var got = Pockets.Take(ctx.Data, st.Engineer, ItemKey.Of(item), n);
            Assert.AreEqual(n, got, 1e-9, "the Backpack had room");
            st.Stats.Made.Add(item, got);
        }

        /// <summary>Home placed, one rifle crafted and equipped in slot 1, and <paramref name="bullets"/> carried.</summary>
        public static string Armed(SimContext ctx, SimState st, double bullets = 40)
        {
            Home(ctx, st);
            var id = CraftRifle(ctx, st);
            var e = Apply(ctx, st, new EquipCommand(id, 0));
            Assert.IsTrue(e.Accepted, e.Problem);
            if (bullets > 0) Grant(ctx, st, ItemId.Magazine, bullets);
            return id;
        }

        public static int Count<T>(SimState st) where T : SimEvent
        {
            var n = 0;
            for (var i = 0; i < st.Events.Count; i++) if (st.Events[i] is T) n++;
            return n;
        }

        public static T Last<T>(SimState st) where T : class
        {
            for (var i = st.Events.Count - 1; i >= 0; i--) if (st.Events[i] is T t) return t;
            return null;
        }
    }
}
