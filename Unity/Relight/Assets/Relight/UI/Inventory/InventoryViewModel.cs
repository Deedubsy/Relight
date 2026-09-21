using System.Collections.Generic;
using Relight.Presentation;
using Relight.Sim;
using Relight.Sim.UI;

namespace Relight.UI
{
    /// <summary>
    /// C-06. The Backpack/storage panel's view-model: it reads the simulation through <see cref="InventoryQueries"/>
    /// and <see cref="WeaponQueries"/>, formats, and exposes plain data. It holds no <c>VisualElement</c> and no
    /// Unity type, so the panel's layout can be rebuilt in UI Builder without touching it.
    ///
    /// It also encodes the <b>one asymmetry that matters</b> (UI_AND_ONBOARDING.md §5.2): the Backpack is true
    /// slots and is always rendered in full — all forty of them, empty ones included — while storage is a pooled
    /// quantity that is only <i>presented</i> as 50-item chunks. A "full" storage chunk is therefore never a
    /// refusal; it is a display decision, and dropping onto any compatible chunk merges into the pool.
    ///
    /// Nothing here mutates anything. Every count on screen comes from a query and every change is a command.
    /// </summary>
    public sealed class InventoryViewModel
    {
        /// <summary>The reference HUD's refresh throttle, shared with the status panel (hud.ts:47).</summary>
        public const double RefreshSeconds = 0.150;

        /// <summary>inventoryPanel.ts caps the chunk list so a huge pool cannot build an unbounded element list.</summary>
        public const int MaxStoreChunks = 600;

        /// <summary>One rendered cell, on either side. A pack cell maps to a real slot; a store chunk does not.</summary>
        public sealed class Cell
        {
            /// <summary>Backpack: the real slot index. Storage: the chunk's position in the list.</summary>
            public int Index;

            /// <summary>Item key, or null for an empty Backpack slot.</summary>
            public string Item;

            public string DisplayName = "";
            public double Count;
            public int StackSize;
            public bool IsWeapon;
            public string FilterItem, Role = "";
            public double Limit;
            public bool CanLoad = true;

            public bool Empty => string.IsNullOrEmpty(Item);
        }

        private readonly List<Cell> _pack = new List<Cell>(40);
        private readonly List<Cell> _store = new List<Cell>(32);
        private readonly List<Cell> _tray = new List<Cell>(HandCraft.OutputStacks);
        private readonly List<HandCraft.TrayStack> _trayStacks = new List<HandCraft.TrayStack>(HandCraft.OutputStacks);
        private double _lastRefresh = double.NegativeInfinity;

        /// <summary>All Backpack slots, in slot order, empties included (§5.2).</summary>
        public IReadOnlyList<Cell> Pack => _pack;

        /// <summary>The open store's pooled contents, as presentation chunks (§5.2).</summary>
        public IReadOnlyList<Cell> Store => _store;

        /// <summary>
        /// GP-UX-2. The Home workshop's output tray as slots. It is a THIRD grid rather than the store, because the
        /// tray belongs to the engineer's own hand-craft state (<see cref="HandState.Output"/>) and not to any
        /// machine — it has no machine id, so it cannot be paired the way a chest or a smelter is. Empty whenever
        /// the workshop section is not showing.
        /// </summary>
        public IReadOnlyList<Cell> Tray => _tray;

        /// <summary>"3 / 20 stacks", the tray's own capacity line.</summary>
        public string TrayCapacity { get; private set; } = "";

        public string TrayHint { get; private set; } = "";

        /// <summary>False when the engineer has walked away: the slots still show, and refuse to be taken from.</summary>
        public bool TrayInReach { get; private set; }

        /// <summary>The workshop pauses a batch when this is true, so the grid says so rather than just looking busy.</summary>
        public bool TrayFull { get; private set; }

        /// <summary>"12 / 40 slots".</summary>
        public string Capacity { get; private set; } = "";

        /// <summary>The store's display name, or "" when no store is open.</summary>
        public string StoreName { get; private set; } = "";
        public string StoreCapacity { get; private set; } = "";
        public string StoreHint { get; private set; } = "";

        /// <summary>"Home storage &amp; Backpack" or "Backpack" (inventoryPanel.ts).</summary>
        public string Heading { get; private set; } = TransferText.DrawerHeading("");

        /// <summary>True when a store is open but the engineer has walked out of reach of it.</summary>
        public bool StoreOutOfReach { get; private set; }

        /// <summary>The store this panel is paired with, or -1 for the Backpack alone.</summary>
        public int MachineId { get; private set; } = -1;

        /// <summary>The Backpack layout token every inventory command must carry, so a stale drop is refused.</summary>
        public string Layout { get; private set; } = "";

        /// <summary>Equipment slots 1 and 2: the item key in each, or null.</summary>
        public string[] Equipped { get; } = new string[2];

        /// <summary>The display name of each equipped slot, for the strip's labels.</summary>
        public string[] EquippedNames { get; } = new string[2];

        /// <summary>The sim's action bar (<see cref="WeaponQueries.Bar"/>), ten entries, "" where empty.</summary>
        public IReadOnlyList<string> Bar { get; private set; } = System.Array.Empty<string>();

        /// <summary>Which of the two equipment slots is in the hands (<c>WeaponState.Active</c>), 0 or 1.</summary>
        public int ActiveSlot { get; private set; }

        /// <summary>True when something is actually in the hands; the strip greys Reload and Swap when it is not.</summary>
        public bool HasWeapon { get; private set; }

        /// <summary>Rounds in the active magazine, its capacity, and the spare rounds in the Backpack.</summary>
        public double Loaded { get; private set; }

        public double MagazineCapacity { get; private set; }

        public double Reserve { get; private set; }

        /// <summary>True while a reload is running; Reload is refused (with the sim's words) rather than hidden.</summary>
        public bool Reloading { get; private set; }

        /// <summary>True while handcrafting holds the engineer in place; the panel then shows the lock prompt.</summary>
        public bool HandLocked { get; private set; }

        public int Refreshes { get; private set; }

        /// <summary>Which store the panel is paired with. -1 closes the storage column (§5.1).</summary>
        public void Pair(int machineId) => MachineId = machineId;

        /// <summary>Recompute if the throttle allows. <paramref name="now"/> is unscaled real time.</summary>
        public bool Refresh(SimHost host, double now, bool force = false)
        {
            if (!force && now - _lastRefresh < RefreshSeconds) return false;
            _lastRefresh = now;
            Refreshes++;

            var sim = host == null ? null : host.Simulation;
            if (sim == null)
            {
                _pack.Clear();
                _store.Clear();
                _tray.Clear();
                TrayCapacity = "";
                TrayHint = "";
                TrayInReach = false;
                TrayFull = false;
                Capacity = "";
                StoreName = "";
                Heading = TransferText.DrawerHeading("");
                StoreOutOfReach = false;
                Layout = "";
                Equipped[0] = Equipped[1] = null;
                EquippedNames[0] = EquippedNames[1] = "";
                Bar = System.Array.Empty<string>();
                ActiveSlot = 0;
                HasWeapon = false;
                Loaded = 0;
                MagazineCapacity = 0;
                Reserve = 0;
                Reloading = false;
                HandLocked = false;
                return true;
            }

            var ctx = sim.Context;
            var st = sim.State;

            ReadPack(ctx, st);
            ReadStore(ctx, st);
            ReadTray(ctx, st);
            ReadEquipment(ctx, st);

            Layout = Backpack.Layout(ctx.Data, st.Engineer);
            HandLocked = HandCraft.HandLocked(st);
            Heading = TransferText.DrawerHeading(StoreName);
            return true;
        }

        private void ReadPack(SimContext ctx, SimState st)
        {
            _pack.Clear();
            var slots = InventoryQueries.Slots(ctx, st);
            for (var i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                var cell = new Cell { Index = i, StackSize = 1 };
                // Slots() always returns a view per slot; an EMPTY slot is one whose Item is null (not a null view).
                if (s != null && !string.IsNullOrEmpty(s.Item))
                {
                    cell.Item = s.Item;
                    cell.DisplayName = s.DisplayName;
                    cell.Count = s.Count;
                    cell.StackSize = s.StackSize > 0 ? s.StackSize : 1;
                    cell.IsWeapon = new ItemKey(s.Item).IsWeapon;
                }
                _pack.Add(cell);
            }

            var cap = InventoryQueries.Capacity(ctx, st);
            Capacity = TransferText.Capacity(cap.used, cap.cap);
        }

        private void ReadStore(SimContext ctx, SimState st)
        {
            _store.Clear();StoreName="";StoreCapacity="";StoreHint="";StoreOutOfReach=false;
            if(MachineId<0)return;
            var view=InventoryQueries.Machine(ctx,st,MachineId);
            if(view==null){MachineId=-1;return;}
            StoreName=view.DisplayName;StoreOutOfReach=!view.InReach;
            var m=st.MachineById(MachineId);var d=ctx.Data;
            if(!view.HasInventory){StoreHint="This machine has no item storage.";return;}
            var seen=new HashSet<string>();
            double Count(ItemId item){foreach(var row in view.Items)if(row.Item==item)return row.Count;return 0;}
            void Reserved(ItemId item,string role,double limit,bool canLoad)
            {
                var key=Items.Key(item);if(!seen.Add(key))return;var count=Count(item);
                _store.Add(new Cell{Index=_store.Count,Item=count>0?key:null,FilterItem=key,DisplayName=d.Item(item).DisplayName,
                    Count=count,StackSize=d.StackSize(ItemKey.Of(item)),Role=role,Limit=limit,CanLoad=canLoad});
            }
            if(PowerGrid.IsSource(d,m))
            {
                var cap=MachineInventory.GeneratorFuelCap(d);
                Reserved(ItemId.Coal,"Fuel",cap,true);Reserved(ItemId.Fuel,"Fuel",cap,true);
                StoreCapacity=$"{Count(ItemId.Coal)+Count(ItemId.Fuel):0.#} / {cap:0} fuel · shared capacity";
                // GP-W6: the practical reserve, from the same formula as the HUD's fuel estimate.
                StoreHint="Drop coal or refined fuel into its slot. Both use the same fuel capacity. A full slot runs it "+PowerQueries.FuelTimeText(PowerQueries.FullSlotSeconds(d))+" flat out (estimate).";
            }
            else if(TurretHopper.IsTurret(d,m))
            {
                var ammo=TurretHopper.Ammo(d,m);var cap=TurretHopper.Capacity(d,m);Reserved(ammo,"Ammo",cap,true);
                StoreCapacity=$"{Count(ammo):0} / {cap:0} rounds";StoreHint="Drop compatible ammunition into the ammo slot.";
            }
            else if(ProductionRules.IsMiner(d,m))
            {
                foreach(var row in view.Items)Reserved(row.Item,"Output",1,false);
                if(_store.Count==0)_store.Add(new Cell{Index=0,Role="Output",Limit=1,CanLoad=false,DisplayName="Extractor output"});
                StoreCapacity=$"{m.Inv.Total:0.#} / 1 output";StoreHint="Output only. Drag extracted items into Backpack; conveyors can collect them automatically.";
            }
            else if(ProductionRules.IsProcessor(d,m))
            {
                var recipe=ProductionRules.RecipeOf(d,st,m);
                if(recipe!=null)
                {
                    foreach(var input in recipe.Inputs)Reserved(input.Item,"Input",input.Count*ProductionRules.InputBufferMul(d,m),true);
                    foreach(var output in recipe.Outputs)Reserved(output.Item,"Output",ProductionRules.OutputCap(d,m,recipe),false);
                }
                StoreCapacity="Recipe inputs & output";StoreHint=recipe==null?"Choose a recipe to see its input and output slots.":"Drop ingredients into matching input slots. Drag finished output into Backpack.";
            }
            else
            {
                StoreCapacity=m.Kind=="chest"?$"{m.Inv.Total:0.#} / {MachineInventory.ChestCap(d):0} items · shared capacity":"Stored items";
                StoreHint="Drag items between Backpack and storage. Shift-click transfers a stack.";
            }
            // Keep stock from an earlier recipe visible and transferable too.
            foreach(var amount in view.Items)
            {
                if(seen.Contains(amount.Key))continue;
                var size=System.Math.Max(1,d.StackSize(new ItemKey(amount.Key)));var left=amount.Count;
                while(left>0&&_store.Count<MaxStoreChunks)
                {var n=System.Math.Min(size,left);_store.Add(new Cell{Index=_store.Count,Item=amount.Key,DisplayName=amount.DisplayName,Count=n,StackSize=size,Role="Stored"});left-=n;}
            }
            if(m.Kind=="chest")
            {
                var room=m.Inv.Total<MachineInventory.ChestCap(d);var want=System.Math.Max(10,_store.Count+(room?1:0));
                while(_store.Count<want&&_store.Count<MaxStoreChunks)_store.Add(new Cell{Index=_store.Count,Role=room?"Empty":"Full",CanLoad=room,DisplayName=room?"Empty storage slot":"Storage full"});
            }
        }

        /// <summary>
        /// GP-UX-2. The output tray, read the same way the miner's output is read: fixed Role-tagged slots that
        /// take nothing and give up what they hold.
        ///
        /// <c>CanLoad = false</c> on every one of them is the rule, not a convenience — the tray is where the
        /// workshop PUTS things. There is no command that loads it, so a slot that accepted a drop would be
        /// promising something the sim cannot do.
        ///
        /// Reach is recorded rather than acted on. The slots stay visible from across the map (the player wants to
        /// know what is waiting before walking back), and <see cref="HandCraft.Collect"/> is what refuses when they
        /// try to take it, so the grid can never claim a reach the sim would decline.
        /// </summary>
        private void ReadTray(SimContext ctx, SimState st)
        {
            _tray.Clear();
            TrayCapacity = "";
            TrayHint = "";
            TrayInReach = false;
            TrayFull = false;
            var hand = st == null ? null : st.Hand;
            if (hand == null || hand.Output == null) return;

            var d = ctx.Data;
            HandCraft.TrayStacks(d, hand.Output, _trayStacks);
            for (var i = 0; i < _trayStacks.Count; i++)
            {
                var row = _trayStacks[i];
                _tray.Add(row.Empty
                    ? new Cell { Index = i, Role = "Output", CanLoad = false, DisplayName = "Empty tray slot" }
                    : new Cell
                    {
                        Index = i, Item = row.Item, DisplayName = row.DisplayName, Count = row.Count,
                        StackSize = (int)System.Math.Max(1, row.StackSize), IsWeapon = row.IsWeapon,
                        Role = "Output", CanLoad = false,
                    });
            }

            var used = HandCraft.TrayUsed(d, hand.Output);
            TrayFull = used >= HandCraft.OutputStacks;
            TrayInReach = HandCraft.NearDepot(ctx, st) && !st.Engineer.IsDown;
            TrayCapacity = $"{used:0} / {HandCraft.OutputStacks:0} stacks";
            TrayHint = TrayFull ? HandCraft.TrayFullText
                : !TrayInReach ? "Finished goods wait here. Walk back to the Home workshop to collect them."
                : used == 0 ? "Finished goods appear here. Drag them into the Backpack, or press Collect for the lot."
                : "Drag a stack into the Backpack, or click it to take it. Collect empties the tray.";
        }

        /// <summary>
        /// Both physical equipment slots (UI_AND_ONBOARDING.md §5.6: "the two physical equipped slots"), not just
        /// the one in the hands. <see cref="WeaponQueries.Equipped"/> answers only for the ACTIVE slot, so the strip
        /// reads <see cref="WeaponState.SlotAt"/> — which holds a weapon INSTANCE id such as <c>rifle:3</c> — and
        /// resolves each through <see cref="WeaponQueries.Owned"/> to get the profile's display name. A slot holding
        /// an id that is no longer owned renders empty rather than throwing.
        /// </summary>
        private void ReadEquipment(SimContext ctx, SimState st)
        {
            Equipped[0] = Equipped[1] = null;
            EquippedNames[0] = EquippedNames[1] = "";
            var q = st.Weapons;
            if (q != null)
            {
                var owned = WeaponQueries.Owned(st);
                for (var slot = 0; slot < 2; slot++)
                {
                    var id = q.SlotAt(slot);
                    if (string.IsNullOrEmpty(id)) continue;
                    for (var i = 0; i < owned.Count; i++)
                    {
                        if (!string.Equals(owned[i].Id, id, System.StringComparison.Ordinal)) continue;
                        Equipped[slot] = id;
                        EquippedNames[slot] = ctx.Data.TryWeapon(owned[i].Kind, out var profile)
                            ? profile.DisplayName : owned[i].Kind;
                        break;
                    }
                }
                ActiveSlot = q.Active == 1 ? 1 : 0;
            }
            else
            {
                ActiveSlot = 0;
            }

            var active = WeaponQueries.Equipped(st, ctx.Data);
            HasWeapon = active.Equipped;
            Loaded = active.Loaded;
            MagazineCapacity = active.Capacity;
            Reserve = active.Reserve;
            Reloading = active.Reloading;
            Bar = WeaponQueries.Bar(st);
        }

        /// <summary>
        /// The cell a drop landed on, or null. Kept here rather than in the manipulator so the rule "a drop on the
        /// rightmost slot and on a slot only reachable by scrolling both work" (§5.3, defect U-1) is decided by an
        /// index, never by a rectangle the layout could get wrong.
        /// </summary>
        public Cell CellAt(bool pack, int index)
        {
            var list = pack ? _pack : _store;
            return index >= 0 && index < list.Count ? list[index] : null;
        }

        /// <summary>The tray slot at this index, or null. The tray's own <see cref="CellAt"/>.</summary>
        public Cell TrayAt(int index) => index >= 0 && index < _tray.Count ? _tray[index] : null;
    }
}
