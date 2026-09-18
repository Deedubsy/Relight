namespace Relight.Sim.Tests.Support
{
    /// <summary>
    /// The one short headless run the four shared Phase B checks share (TECHNICAL_ARCHITECTURE.md §10.6: "3–4 reuse
    /// the same short run"). It is deliberately one scenario, not four: a conservation failure, a replay divergence
    /// and a save-round-trip loss are then all statements about the same five minutes of play.
    ///
    /// What it exercises, in the shape B-ACC asks for (move, craft, transfer):
    /// <list type="bullet">
    /// <item>B-08 movement — walk, sprint, a dodge, an aim and a click-to-move target, over the synthetic map;</item>
    /// <item>B-06 workshop crafting — the whole opening Backpack (steel 20, copper 5) turned into 50 bullets in
    ///       five batches at a placed Depot, i.e. 1,200 ticks in which a tick phase is doing real work, collected
    ///       out of the workshop's output tray in two trips (U-D-44: finished goods are never delivered remotely);</item>
    /// <item>B-06 transfers — steel into a supply chest and part of it back out, then bullets in;</item>
    /// <item>refusals — a craft queued with nothing left to pay for it, which must be refused identically in every
    ///       run (a refusal that varied would show up as a hash difference).</item>
    /// </list>
    /// Hand mining is not in it: it needs ground deposits, which are Phase C (Actor/HandCraft.cs).
    /// </summary>
    public static class Scenarios
    {
        /// <summary>5 sim-minutes at 20 Hz.</summary>
        public const int ShortRunTicks = 6000;

        /// <summary>Depot footprint origin (6×6, on ground and rubble, in reach of the spawn at (6.5, 6.5)).</summary>
        public const int DepotX = 8, DepotY = 8;
        /// <summary>Supply chest footprint origin (2×2, on the street north-east of the spawn).</summary>
        public const int ChestX = 10, ChestY = 4;

        /// <summary>Machine ids are handed out in placement order from 1 (SimState.NextId).</summary>
        public const int DepotId = 1, ChestId = 2;

        /// <summary>Bullets the three hand batches make (10 a batch, reference flow.ts HAND_BULLET_SECONDS/HAND_BULLETS).</summary>
        public const int ExpectedBullets = 50;

        /// <summary>Indices in the log of the two walk commands the order-matters check swaps.</summary>
        public const int WalkStartIndex = 2, WalkStopIndex = 3;

        public static ScenarioBuilder ShortRun(int seed = 7) =>
            ScenarioBuilder.New()
                .WithSeed(seed)
                // Home workshop and a supply chest, both within the engineer's reach of the spawn.
                .Do(0, new PlaceMachineCommand("depot", DepotX, DepotY))
                .Do(1, new PlaceMachineCommand("chest", ChestX, ChestY))
                // Move first: west, then south at a sprint, a dodge, an aim, then click-to-move back to the spawn.
                // Since U-D-44 the batches no longer pin the engineer, so the order is only the scenario's own: the
                // movement block is kept ahead of them so each check reads as one thing at a time.
                .Do(5, new WalkCommand(-1, 0))       // WalkStartIndex
                .Do(15, new WalkCommand(0, 0))       // WalkStopIndex
                .Do(20, new SprintCommand(true))
                .Do(25, new WalkCommand(0, 1))
                .Do(40, new WalkCommand(0, 0))
                .Do(45, new SprintCommand(false))
                .Do(60, new DodgeCommand())
                .Do(80, new AimCommand(true, 10.5, 9.5))
                .Do(120, new MoveCommand(6.5, 6.5))
                // Five batches: every plate and every copper in the opening Backpack, queued once the engineer is
                // back beside the Depot. The workshop processes them on its own from here.
                .Do(250, new HandCraftCommand(3))
                .Do(400, new InventorySortCommand())
                // The first three batches finish around tick 970 and wait in the workshop's output tray; they have
                // to be collected in reach before any of them can be carried to the chest.
                .Do(2300, new CollectWorkshopCommand())
                .Do(2400, new MachineTransferCommand(ChestId, ItemId.Steel, 4, true))
                .Do(2500, new MachineTransferCommand(ChestId, ItemId.Steel, 1, false))
                .Do(2600, new MachineTransferCommand(ChestId, ItemId.Magazine, 25, true))
                .Do(3000, new AimCommand(false, 0, 0))
                .Do(3200, new WalkCommand(1, 0))
                .Do(3210, new WalkCommand(0, 0))
                // The cheaper chest leaves enough material for two more batches; replay must account for them.
                .Do(4000, new HandCraftCommand(2))
                // They finish around tick 4,480, again into the tray, and are collected on the second trip.
                .Do(4900, new CollectWorkshopCommand())
                .Do(5000, new InventorySortCommand());
    }
}
