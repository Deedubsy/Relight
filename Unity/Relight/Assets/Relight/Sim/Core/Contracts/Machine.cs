using System.Collections.Generic;

namespace Relight.Sim
{
    /// <summary>
    /// A placed machine's shared header (reference flow.ts `Machine` id/kind/x/y/dir/size + the inventory fields the
    /// foundation needs). Declared in the contracts because three subsystems touch it: the world reads footprints for
    /// occupancy (B-08), the inventory subsystem owns inventories and transfers (B-06), production drives it (Phase C).
    /// Belts, inserters, assembler crafting state and the observation/decode fields arrive with Phase C in a partial file.
    /// Machines live in <see cref="SimState.Machines"/> in placement order; ids are never reused.
    /// </summary>
    public sealed partial class Machine : IVisitable
    {
        public int Id;
        /// <summary>Machine kind key (MachineSpec.Key, reference `Kind`).</summary>
        public string Kind = "";
        public int X;
        public int Y;
        public Dir Dir;
        public int Size = 1;
        /// <summary>General inventory (chest stock, generator coal, processor inputs) — reference `m.inv`.</summary>
        public ItemCounts Inv = new ItemCounts();
        /// <summary>Finished output count for processors, magazine count for the ammo box — reference `m.out`.</summary>
        public int Out;
        /// <summary>Loaded rounds for turrets/cannon (one round = one bullet, U-D-08) — reference `m.rounds`.</summary>
        public int Rounds;
        /// <summary>
        /// The encounter this machine belongs to, or "" for anything the player built (FREIGHT_STRONGHOLD_DESIGN
        /// §5.3, FRT-03). A camp's cache crate is the only such machine: the sim put it there, so it can be emptied
        /// but never picked up (<see cref="Placement.CanPickUp"/>), and it is nobody's Home stock.
        /// </summary>
        public string Site = "";

        /// <summary>The sim placed this for an encounter; the player did not build it.</summary>
        public bool IsSiteBound => !string.IsNullOrEmpty(Site);

        public (int w, int h) Dimensions => Footprints.Dimensions(Kind, Dir, Size);
        public TileRect Rect { get { var (w, h) = Dimensions; return new TileRect(X, Y, w, h); } }

        public void Visit(IStateVisitor v)
        {
            v.Field("id", ref Id);
            v.Field("kind", ref Kind);
            v.Field("x", ref X);
            v.Field("y", ref Y);
            var dir = (int)Dir; v.Field("dir", ref dir); Dir = (Dir)dir;
            v.Field("size", ref Size);
            v.Object("inv", ref Inv, () => new ItemCounts());
            v.Field("out", ref Out);
            v.Field("rounds", ref Rounds);
            v.Field("site", ref Site); // save v14
            VisitProduction(v); // Phase C
        }

        partial void VisitProduction(IStateVisitor v);
    }

    public sealed partial class SimState
    {
        /// <summary>All placed machines in placement order (reference `f.machines`).</summary>
        public readonly List<Machine> Machines = new List<Machine>();

        public Machine MachineById(int id)
        {
            for (var i = 0; i < Machines.Count; i++) if (Machines[i].Id == id) return Machines[i];
            return null;
        }
    }
}
