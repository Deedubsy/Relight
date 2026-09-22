using System;
using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// One queued Home-workshop job: a number of batches of a single recipe, processed one batch at a time.
    /// The ingredients for every batch were taken out of the Backpack when the job was queued and are held in
    /// <see cref="HandState.Reserved"/>, so nothing can be spent twice and a cancel is lossless.
    /// </summary>
    public sealed class WorkshopJob : IVisitable
    {
        public int Id;
        public string RecipeKey = "";
        /// <summary>Batches still to make, including the one in progress.</summary>
        public int Batches;
        /// <summary>Seconds into the current batch.</summary>
        public double Progress;

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("recipeKey", ref RecipeKey);
            v.Field("batches", ref Batches);
            v.Field("progress", ref Progress);
        }
    }

    /// <summary>
    /// The Home workshop's own state: one persistent job queue, the ingredients it holds for them, and the
    /// physical output tray finished goods wait in until the engineer collects them.
    ///
    /// U-D-44 (2026-09-15) supersedes U-D-10: the workshop processes its queue while the player is elsewhere, at
    /// the same slow speeds. Nothing here pins the engineer and nothing is delivered remotely — the products sit
    /// in <see cref="Output"/> at the workshop and only a <see cref="CollectWorkshopCommand"/> in reach moves them.
    ///
    /// The four legacy fields below belong to the pre-schema-6 single-batch model. They are still visited so an
    /// older save reads, and <see cref="HandCraft.Normalise"/> folds them into <see cref="Jobs"/> exactly once on
    /// the first tick after loading. Nothing writes them afterwards.
    /// </summary>
    public sealed class HandState : IVisitable
    {
        /// <summary>Legacy (schema &lt;= 5): batches still queued, including the one in progress.</summary>
        public int Crafts;
        /// <summary>Legacy (schema &lt;= 5): a batch had taken its ingredients and was running.</summary>
        public bool Crafting;
        /// <summary>Legacy (schema &lt;= 5): seconds into the running batch.</summary>
        public double CraftProg;
        /// <summary>Legacy (schema &lt;= 5): the finished batch was waiting on Backpack room.</summary>
        public bool Full;
        /// <summary>Legacy (schema &lt;= 5): the single recipe the queue was locked to.</summary>
        public string RecipeKey = "hand-bullets";
        /// <summary>Legacy (schema &lt;= 5) cancellation debt, drained into the pockets by <see cref="HandCraft.DrainLegacy"/>.</summary>
        public double[] Refunds = new double[Items.Count];
        /// <summary>Legacy (schema &lt;= 5) cancellation debt in Steel; see <see cref="Refunds"/>.</summary>
        public double RefundSteel;
        /// <summary>Legacy (schema &lt;= 5) cancellation debt in Copper; see <see cref="Refunds"/>.</summary>
        public double RefundCopper;

        /// <summary>The workshop's queue, processed front to back. Empty means the workshop is idle.</summary>
        public List<WorkshopJob> Jobs = new List<WorkshopJob>();
        /// <summary>The next job serial. Job ids are stable across a save so the panel's Cancel button cannot slip.</summary>
        public int NextJobId = 1;
        /// <summary>
        /// Ingredients taken from the Backpack and owed to the queued jobs. Counted as held
        /// (<see cref="LedgerPlace.Machines"/>) — never a sink — so a cancel returns exactly what was reserved.
        /// Anything here that no job needs any more drains into <see cref="Output"/>.
        /// </summary>
        public ItemBag Reserved = new ItemBag();
        /// <summary>The workshop's physical output tray. Finished goods wait here for collection in reach.</summary>
        public ItemBag Output = new ItemBag();
        /// <summary>The front batch is finished but the tray has no room for it; nothing is consumed until it has.</summary>
        public bool OutputFull;

        public void Visit(IStateVisitor v)
        {
            v.Field("recipeKey", ref RecipeKey);
            v.Field("refunds", ref Refunds);
            v.Field("crafts", ref Crafts);
            v.Field("crafting", ref Crafting);
            v.Field("craftProg", ref CraftProg);
            v.Field("full", ref Full);
            v.Field("refundSteel", ref RefundSteel);
            v.Field("refundCopper", ref RefundCopper);
            v.List("jobs", Jobs, () => new WorkshopJob());
            v.Field("nextJobId", ref NextJobId);
            v.Object("reserved", ref Reserved, () => new ItemBag());
            v.Object("output", ref Output, () => new ItemBag());
            v.Field("outputFull", ref OutputFull);
        }
    }

    public sealed partial class SimState
    {
        /// <summary>The Home workshop's queue, reserved ingredients and output tray.</summary>
        public HandState Hand = new HandState();
    }

    /// <summary>A workshop batch finished and went into the output tray (drained by the HUD, never saved).</summary>
    public sealed record WorkshopFinishedEvent(double T, string RecipeKey, string DisplayName, int Remaining) : SimEvent(T);

    /// <summary>Queue `Count` batches of `RecipeKey` at the Home workshop. The ingredients are reserved now.</summary>
    public sealed record HandCraftCommand(int Count = 1, string RecipeKey = "hand-bullets") : Command;

    /// <summary>
    /// Cancel workshop work and get the unprocessed ingredients back. <c>JobId</c> names one job; <c>RecipeKey</c>
    /// names every job making that recipe (what a recipe card's Cancel sends); both unset cancels the whole queue.
    /// </summary>
    public sealed record CancelCraftCommand(int JobId = 0, string RecipeKey = null) : Command;

    /// <summary>Collect from the workshop's output tray. A null item means everything the Backpack will take.</summary>
    public sealed record CollectWorkshopCommand(string Item = null, double Count = 0) : Command;

    /// <summary>
    /// The Home workshop. Ported from reference flow.ts (`tickHand` craft half, `handCraftCheck`, `queueCraft`,
    /// `cancelCraft`) and then deliberately changed by U-D-44: the reference pinned the engineer for the duration
    /// of a batch and dropped the product straight into the Backpack. Here the workshop is a place that keeps
    /// working on its own, and the product is a physical thing waiting to be picked up.
    ///
    /// CONSERVATION. Ingredients move Backpack -> <see cref="HandState.Reserved"/> on queue (held, not consumed),
    /// are counted as consumed only at the moment a batch completes, and the outputs appear in
    /// <see cref="HandState.Output"/> in the same step. A cancel moves the reservation back. No path creates or
    /// destroys an item, and <see cref="Ledger"/> counts both bags.
    /// </summary>
    public static class HandCraft
    {
        /// <summary>The recipe station the workshop runs (catalogue §3): a place, not a machine kind.</summary>
        public const string Station = "Home workshop";

        private const double Eps = 1e-9;

        /// <summary>Stacks the output tray holds. Chosen to buffer several slow batches without becoming a free chest.</summary>
        public const int OutputStacks = 20;
        /// <summary>Distinct jobs the queue holds at once; repeat orders of one recipe merge instead of stacking up.</summary>
        public const int MaxJobs = 12;

        /// <summary>The refusal when the tray cannot take a finished batch.</summary>
        public const string TrayFullText = "Workshop output is full — collect the finished goods.";

        /// <summary>
        /// U-D-44: a workshop job no longer pins the engineer. A Home repair (C-05) still does, and this is the
        /// single predicate the mover, the miner, placement and the weapon all read.
        /// </summary>
        public static bool HandLocked(SimState st) => Home.RepairLocked(st);

        /// <summary>The text for whatever is holding the engineer still. Only a repair does, now.</summary>
        public static string LockTextFor(SimState st) => Home.LockText;

        /// <summary>
        /// Reference `nearDepot` (flow.ts:2202 `inReach(st, d.x, d.y, d.size)` against the HQ lot rectangle). Phase B
        /// had no authored lot, so it was "within reach of a placed machine of kind `depot`"; Phase C places that
        /// Depot in the authored Home workshop at new-game time (<see cref="HomeCore.EnsureDepot"/>) and also accepts
        /// reach of the workshop rect itself — the lot the reference measures — so a save made before the Depot
        /// existed still crafts at Home. On a map with no core site (synthetic) a placed depot is still required.
        /// Queueing and collecting need this; processing does not.
        /// </summary>
        public static bool NearDepot(SimContext ctx, SimState st)
        {
            for (var i = 0; i < st.Machines.Count; i++)
            {
                var m = st.Machines[i];
                if (!string.Equals(m.Kind, "depot", StringComparison.Ordinal)) continue;
                if (Interaction.InReach(ctx, st, m)) return true;
            }
            var h = st.Home;
            return h != null && h.Placed && !h.Fallback && Interaction.InReach(ctx, st, h.X, h.Y, h.W, h.H);
        }

        /// <summary>A batch recipe the workshop can run. The rifle is an equipment craft and is not one of these.</summary>
        public static Recipe Recipe(GameData d, string key) =>
            d.TryRecipe(key ?? "hand-bullets", out var r) && r.Station == Station && r.Outputs.Count > 0 ? r : null;

        // ---- the output tray -------------------------------------------------------------------------------

        /// <summary>Stacks the tray is using now.</summary>
        public static int TrayUsed(GameData d, ItemBag bag)
        {
            var keys = new List<ItemKey>();
            bag.Keys(keys);
            var used = 0;
            for (var i = 0; i < keys.Count; i++)
            {
                var n = bag[keys[i]];
                if (n <= 0) continue;
                used += (int)Math.Ceiling(n / StackOf(d, keys[i]));
            }
            return used;
        }

        /// <summary>Empty stacks left in the tray.</summary>
        public static int TrayFree(GameData d, ItemBag bag) => Math.Max(0, OutputStacks - TrayUsed(d, bag));

        private static double StackOf(GameData d, ItemKey key) => key.IsWeapon ? 1 : Math.Max(1, d.StackSize(key));

        /// <summary>Put up to <paramref name="n"/> into the tray and report how much went in. Never overfills.</summary>
        public static double TrayPut(GameData d, ItemBag bag, ItemKey key, double n)
        {
            if (n <= 0) return 0;
            var stack = StackOf(d, key);
            var have = bag[key];
            var slack = have > 0 ? Math.Ceiling(have / stack) * stack - have : 0;
            var fit = Math.Min(n, slack + TrayFree(d, bag) * stack);
            if (fit <= 0) return 0;
            bag[key] = have + fit;
            return fit;
        }

        /// <summary>Could the tray take one whole batch of this recipe's outputs?</summary>
        private static bool Fits(GameData d, HandState h, Recipe r)
        {
            if (r.Outputs.Count == 0) return true;
            var trial = h.Output.Clone();
            for (var i = 0; i < r.Outputs.Count; i++)
                if (TrayPut(d, trial, ItemKey.Of(r.Outputs[i].Item), r.Outputs[i].Count) < r.Outputs[i].Count - Eps)
                    return false;
            return true;
        }

        // ---- reservations ----------------------------------------------------------------------------------

        /// <summary>What the queued jobs still owe, by item.</summary>
        private static ItemBag Needed(GameData d, HandState h)
        {
            var need = new ItemBag();
            for (var i = 0; i < h.Jobs.Count; i++)
            {
                var j = h.Jobs[i];
                if (j.Batches <= 0) continue;
                var r = Recipe(d, j.RecipeKey);
                if (r == null) continue;
                for (var k = 0; k < r.Inputs.Count; k++)
                    need[r.Inputs[k].Item] = need[r.Inputs[k].Item] + r.Inputs[k].Count * j.Batches;
            }
            return need;
        }

        private static bool CanPay(SimState st, Recipe r)
        {
            for (var i = 0; i < r.Inputs.Count; i++)
                if (st.Engineer.Inv[r.Inputs[i].Item] < r.Inputs[i].Count - Eps) return false;
            return true;
        }

        /// <summary>Move one batch's ingredients out of the Backpack and into the workshop's reservation.</summary>
        private static void Pay(GameData d, SimState st, Recipe r)
        {
            for (var i = 0; i < r.Inputs.Count; i++)
            {
                var got = Pockets.Drop(d, st.Engineer, ItemKey.Of(r.Inputs[i].Item), r.Inputs[i].Count);
                st.Hand.Reserved[r.Inputs[i].Item] = st.Hand.Reserved[r.Inputs[i].Item] + got;
            }
        }

        /// <summary>Reserved ingredients no job owns any more move into the output tray for collection.</summary>
        private static void Surplus(GameData d, SimState st)
        {
            var h = st.Hand;
            if (h.Reserved.IsEmpty) return;
            var need = Needed(d, h);
            var keys = new List<ItemKey>();
            h.Reserved.Keys(keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var over = h.Reserved[key] - need[key];
                if (over <= Eps) continue;
                var moved = TrayPut(d, h.Output, key, over);
                if (moved > 0) h.Reserved[key] = h.Reserved[key] - moved;
            }
        }

        // ---- commands --------------------------------------------------------------------------------------

        /// <summary>How many more batches the Backpack can pay for right now, and why not.</summary>
        public static (int room, string reason) Check(SimContext ctx, SimState st, string key = "hand-bullets")
        {
            if (st.Engineer.IsDown) return (0, "Engineer down");
            if (!NearDepot(ctx, st)) return (0, "Walk closer to Home workshop");
            if (Home.RepairLocked(st)) return (0, "Finish or cancel the repair first");
            var r = Recipe(ctx.Data, key);
            if (r == null) return (0, "Recipe is not available at Home workshop");
            var h = st.Hand;
            var last = h.Jobs.Count > 0 ? h.Jobs[h.Jobs.Count - 1] : null;
            var merges = last != null && string.Equals(last.RecipeKey, r.Key, StringComparison.Ordinal);
            if (h.Jobs.Count >= MaxJobs && !merges) return (0, "The workshop queue is full");
            var afford = int.MaxValue;
            for (var i = 0; i < r.Inputs.Count; i++)
                afford = Math.Min(afford, (int)Math.Floor(st.Engineer.Inv[r.Inputs[i].Item] / r.Inputs[i].Count));
            if (r.Inputs.Count == 0) afford = 1;
            return (Math.Max(0, afford), afford > 0 ? "" : "Need recipe ingredients in Backpack");
        }

        /// <summary>Queue batches. Ingredients leave the Backpack now, so nothing can be spent on something else.</summary>
        public static string Queue(SimContext ctx, SimState st, int n, string key = "hand-bullets")
        {
            if (n <= 0) return "Choose at least one batch";
            var (room, reason) = Check(ctx, st, key);
            if (reason.Length > 0) return reason;
            var d = ctx.Data;
            var h = st.Hand;
            var r = Recipe(d, key);
            var take = Math.Min(n, room);
            var made = 0;
            while (made < take && CanPay(st, r)) { Pay(d, st, r); made++; }
            if (made <= 0) return "Need recipe ingredients in Backpack";
            var last = h.Jobs.Count > 0 ? h.Jobs[h.Jobs.Count - 1] : null;
            if (last != null && string.Equals(last.RecipeKey, r.Key, StringComparison.Ordinal)) last.Batches += made;
            else h.Jobs.Add(new WorkshopJob { Id = h.NextJobId++, RecipeKey = r.Key, Batches = made, Progress = 0 });
            return "";
        }

        /// <summary>
        /// Cancel one job (or the whole queue when <paramref name="jobId"/> is 0). Every unprocessed ingredient is
        /// returned: into the Backpack first, because the engineer is standing at the workshop to press this, and
        /// whatever will not fit stays in the workshop and moves to the tray as room appears. Nothing is destroyed.
        /// </summary>
        public static (bool ok, string reason) Cancel(SimContext ctx, SimState st, int jobId = 0, string recipeKey = null)
        {
            var h = st.Hand;
            if (h.Jobs.Count == 0) return (false, "Nothing is queued at the workshop.");
            var removed = 0;
            for (var i = h.Jobs.Count - 1; i >= 0; i--)
            {
                var job = h.Jobs[i];
                var match = jobId != 0 ? job.Id == jobId
                          : recipeKey != null ? string.Equals(job.RecipeKey, recipeKey, StringComparison.Ordinal)
                          : true;
                if (match) { h.Jobs.RemoveAt(i); removed++; }
            }
            if (removed == 0) return (false, "That job has already finished.");
            h.OutputFull = false;

            var d = ctx.Data;
            var need = Needed(d, h);
            var keys = new List<ItemKey>();
            h.Reserved.Keys(keys);
            double stayed = 0;
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var over = h.Reserved[key] - need[key];
                if (over <= Eps) continue;
                var back = Pockets.Take(d, st.Engineer, key, over);
                if (back > 0) h.Reserved[key] = h.Reserved[key] - back;
                stayed += over - back;   // what the Backpack had no room for
            }
            Surplus(d, st);   // and that stays at the workshop, in the tray, to be collected later
            return (true, stayed > Eps
                ? "Job cancelled. The ingredients are waiting in the workshop."
                : "Job cancelled. Ingredients returned.");
        }

        /// <summary>Take finished goods out of the tray and into the Backpack. Only works in reach of the workshop.</summary>
        public static (double moved, string reason) Collect(SimContext ctx, SimState st, string item = null, double count = 0)
        {
            var h = st.Hand;
            if (st.Engineer.IsDown) return (0, "Engineer down");
            if (!NearDepot(ctx, st)) return (0, "Walk closer to Home workshop");
            if (h.Output.IsEmpty) return (0, "The workshop output tray is empty.");
            var d = ctx.Data;
            var keys = new List<ItemKey>();
            h.Output.Keys(keys);
            double moved = 0;
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (item != null && !string.Equals(key.Key, item, StringComparison.Ordinal)) continue;
                var want = count > 0 ? Math.Min(count - moved, h.Output[key]) : h.Output[key];
                if (want <= 0) continue;
                var got = Pockets.Take(d, st.Engineer, key, want);
                if (got <= 0) continue;
                h.Output[key] = h.Output[key] - got;
                moved += got;
                if (count > 0 && moved >= count - Eps) break;
            }
            if (moved <= 0) return (0, item != null ? "Backpack has no room for that." : "Backpack is full.");
            h.OutputFull = false;
            return (moved, "");
        }

        // ---- views -----------------------------------------------------------------------------------------

        /// <summary>
        /// What a recipe card needs: how many batches of this recipe are queued in total, whether one of them is
        /// the batch the workshop is processing now, and how far that batch has got. The queue is one list, so a
        /// recipe with work behind another recipe's job reports its batches but not "running".
        /// </summary>
        public static (int batches, bool running, double progress) QueueOf(SimState st, string key)
        {
            var h = st?.Hand;
            if (h == null || h.Jobs.Count == 0 || key == null) return (0, false, 0);
            var batches = 0;
            for (var i = 0; i < h.Jobs.Count; i++)
                if (string.Equals(h.Jobs[i].RecipeKey, key, StringComparison.Ordinal)) batches += h.Jobs[i].Batches;
            var head = string.Equals(h.Jobs[0].RecipeKey, key, StringComparison.Ordinal);
            return (batches, head && batches > 0, head ? h.Jobs[0].Progress : 0);
        }

        /// <summary>Total batches waiting across every job, for the section heading.</summary>
        public static int QueuedBatches(SimState st)
        {
            var h = st?.Hand;
            if (h == null) return 0;
            var n = 0;
            for (var i = 0; i < h.Jobs.Count; i++) n += h.Jobs[i].Batches;
            return n;
        }

        /// <summary>"Rounds ×40 · Rifle ×1" — what is waiting in the output tray, in canonical key order.</summary>
        public static string TrayContents(GameData d, ItemBag bag)
        {
            if (bag == null) return "";
            var keys = new List<ItemKey>();
            bag.Keys(keys);
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < keys.Count; i++)
            {
                var n = bag[keys[i]];
                if (n <= 0) continue;
                if (sb.Length > 0) sb.Append(" \u00b7 ");
                sb.Append(NameOf(d, keys[i])).Append(" \u00d7").Append(PackLayout.Num(n));
            }
            return sb.ToString();
        }

        /// <summary>
        /// GP-UX-2. One stack-sized chunk of the output tray, or one empty slot of it. The tray is an
        /// <see cref="ItemBag"/> — a pooled count per key, not an array of slots — so a "stack" here is a way of
        /// PRESENTING that pool, never a place a particular item lives. Splitting it this way is what lets the
        /// Backpack and the tray be drawn by the same grid, and what lets the player take one stack instead of
        /// the lot (<see cref="Collect"/> already takes an item and a count; nothing ever asked it for one).
        /// </summary>
        public readonly struct TrayStack
        {
            /// <summary>The item key, or "" for an empty tray slot.</summary>
            public string Item { get; }
            public string DisplayName { get; }

            /// <summary>How many are in THIS chunk: a full stack, or the remainder at the end of a key's run.</summary>
            public double Count { get; }

            /// <summary>The stack size this chunk was cut to, so the panel can draw "38/50" without asking again.</summary>
            public double StackSize { get; }

            public bool IsWeapon { get; }
            public bool Empty => string.IsNullOrEmpty(Item);

            public TrayStack(string item, string name, double count, double stackSize, bool weapon)
            { Item = item; DisplayName = name; Count = count; StackSize = stackSize; IsWeapon = weapon; }
        }

        /// <summary>
        /// GP-UX-2. The output tray as <see cref="OutputStacks"/> slots, in canonical key order, padded with empty
        /// ones — the tray drawn the way every other container in the game is drawn.
        ///
        /// It cuts each key's pooled count into stack-sized chunks by exactly the rule <see cref="TrayUsed"/>
        /// counts them by, so what the player sees occupied and what the workshop calls full are the same number
        /// and can never disagree. A weapon instance is its own chunk (<see cref="StackOf"/> gives it 1).
        ///
        /// Pure: it reads the bag and writes only <paramref name="into"/>. The panel calls it every repaint, so it
        /// fills a caller-owned list rather than allocating one.
        /// </summary>
        public static void TrayStacks(GameData d, ItemBag bag, List<TrayStack> into)
        {
            if (into == null) return;
            into.Clear();
            if (bag != null)
            {
                var keys = new List<ItemKey>();
                bag.Keys(keys);
                for (var i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    var left = bag[key];
                    if (left <= Eps) continue;
                    var stack = StackOf(d, key);
                    var name = NameOf(d, key);
                    while (left > Eps && into.Count < OutputStacks)
                    {
                        var take = Math.Min(stack, left);
                        into.Add(new TrayStack(key.Key, name, take, stack, key.IsWeapon));
                        left -= take;
                    }
                    if (into.Count >= OutputStacks) break;
                }
            }
            while (into.Count < OutputStacks) into.Add(default);
        }

        /// <summary>A tray key's player-facing name. A weapon instance ("rifle:3") reads as its profile's name.</summary>
        public static string NameOf(GameData d, ItemKey key)
        {
            if (key.IsItem(out var id)) return d == null ? Items.Key(id) : d.Item(id).DisplayName;
            return PlayerNames.Weapon(d, key.Key);
        }

        // ---- migration and tick ----------------------------------------------------------------------------

        /// <summary>
        /// Folds a pre-schema-6 single-batch queue into the job queue, exactly once. Nothing writes the legacy
        /// fields any more, so after the first tick they stay at zero and this is a no-op. The running batch's
        /// ingredients had already been counted as consumed under the old rules; they are un-counted and held
        /// instead, so the same units simply change place and the ledger stays balanced.
        /// </summary>
        public static void Normalise(SimContext ctx, SimState st)
        {
            var h = st.Hand;
            if (h.Crafts <= 0 && !h.Crafting) return;
            var d = ctx.Data;
            var r = Recipe(d, h.RecipeKey);
            var running = h.Crafting;
            var queued = Math.Max(0, h.Crafts - (running ? 1 : 0));
            var progress = h.CraftProg;
            h.Crafts = 0; h.Crafting = false; h.CraftProg = 0; h.Full = false;
            if (r == null) return;

            var batches = 0;
            if (running)
            {
                for (var i = 0; i < r.Inputs.Count; i++)
                {
                    h.Reserved[r.Inputs[i].Item] = h.Reserved[r.Inputs[i].Item] + r.Inputs[i].Count;
                    st.Stats.Consumed.Add(r.Inputs[i].Item, -r.Inputs[i].Count);
                }
                batches = 1;
            }
            for (var n = 0; n < queued && CanPay(st, r); n++) { Pay(d, st, r); batches++; }
            if (batches <= 0) return;
            h.Jobs.Insert(0, new WorkshopJob
            {
                Id = h.NextJobId++,
                RecipeKey = r.Key,
                Batches = batches,
                Progress = running ? Math.Min(progress, r.Seconds) : 0,
            });
        }

        /// <summary>Pays a pre-schema-6 cancellation debt back into the pockets as space appears.</summary>
        public static void DrainLegacy(GameData d, SimState st)
        {
            var h = st.Hand;
            if (h.RefundSteel > 0)
                h.RefundSteel = Math.Max(0, h.RefundSteel - Pockets.Take(d, st.Engineer, ItemKey.Of(ItemId.Steel), h.RefundSteel));
            if (h.RefundCopper > 0)
                h.RefundCopper = Math.Max(0, h.RefundCopper - Pockets.Take(d, st.Engineer, ItemKey.Of(ItemId.Copper), h.RefundCopper));
            for (var i = 0; i < Items.Count; i++)
                if (h.Refunds[i] > 0)
                    h.Refunds[i] = Math.Max(0, h.Refunds[i] - Pockets.Take(d, st.Engineer, ItemKey.Of((ItemId)i), h.Refunds[i]));
        }

        /// <summary>
        /// One workshop step. It runs wherever the engineer is and whatever state they are in: the workshop is a
        /// place, not a pair of hands (U-D-44). A downed engineer, a walk across the map and a closed panel all
        /// leave the queue running.
        /// </summary>
        public static void Tick(SimContext ctx, SimState st, double dt)
        {
            var d = ctx.Data;
            var h = st.Hand;
            Normalise(ctx, st);
            DrainLegacy(d, st);
            Surplus(d, st);

            if (h.Jobs.Count == 0) { h.OutputFull = false; return; }
            var job = h.Jobs[0];
            var r = Recipe(d, job.RecipeKey);
            if (r == null || job.Batches <= 0) { h.Jobs.RemoveAt(0); return; }

            // The ingredients were taken when the job was queued. If a save ever lost them, drop the job rather
            // than invent them: the reservation is the only authority on what has been paid for.
            for (var i = 0; i < r.Inputs.Count; i++)
                if (h.Reserved[r.Inputs[i].Item] < r.Inputs[i].Count - Eps) { h.Jobs.RemoveAt(0); return; }

            job.Progress = Math.Min(r.Seconds, job.Progress + dt);
            if (job.Progress < r.Seconds - Eps) return;

            if (!Fits(d, h, r)) { h.OutputFull = true; return; }   // paused: nothing consumed, nothing repeated

            for (var i = 0; i < r.Inputs.Count; i++)
            {
                h.Reserved[r.Inputs[i].Item] = h.Reserved[r.Inputs[i].Item] - r.Inputs[i].Count;
                st.Stats.Consumed.Add(r.Inputs[i].Item, r.Inputs[i].Count);
            }
            for (var i = 0; i < r.Outputs.Count; i++)
            {
                var o = r.Outputs[i];
                TrayPut(d, h.Output, ItemKey.Of(o.Item), o.Count);
                st.Stats.Made.Add(o.Item, o.Count);
                if (o.Item == ItemId.Magazine) st.Stats.MagsMade += o.Count;
            }
            job.Batches--;
            job.Progress = 0;
            h.OutputFull = false;
            st.Stats.HandCrafted++;
            st.Events.Add(new WorkshopFinishedEvent(st.T, r.Key, r.DisplayName, job.Batches));
            if (job.Batches <= 0) h.Jobs.RemoveAt(0);
        }
    }

    public sealed class HandCraftPhase : ITickPhase
    {
        public void Tick(SimContext ctx, SimState st, double dt) => HandCraft.Tick(ctx, st, dt);
    }

    public sealed class HandCraftHandler : ICommandHandler
    {
        public bool TryApply(SimContext ctx, SimState st, Command c, out CommandResult result)
        {
            switch (c)
            {
                case HandCraftCommand q:
                {
                    var reason = HandCraft.Queue(ctx, st, q.Count, q.RecipeKey);
                    result = reason.Length > 0
                        ? CommandResult.Refuse(reason)
                        : CommandResult.Ok("Queued at the Home workshop. It keeps working while you are away.");
                    return true;
                }
                case CancelCraftCommand x:
                {
                    var (ok, reason) = HandCraft.Cancel(ctx, st, x.JobId, x.RecipeKey);
                    result = ok ? CommandResult.Ok(reason) : CommandResult.Refuse(reason);
                    return true;
                }
                case CollectWorkshopCommand g:
                {
                    var (moved, reason) = HandCraft.Collect(ctx, st, g.Item, g.Count);
                    result = moved > 0
                        ? CommandResult.Ok("Collected " + PackLayout.Num(moved) + " from the workshop.")
                        : CommandResult.Refuse(reason);
                    return true;
                }
                default:
                    result = default;
                    return false;
            }
        }
    }
}
