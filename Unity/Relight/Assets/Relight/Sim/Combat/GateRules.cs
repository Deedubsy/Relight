using System;

namespace Relight.Sim
{
    /// <summary>
    /// REL-132. The Gate: the one tile in a wall that lets the engineer through and nothing else.
    ///
    /// The owner's note from their 2026-09-22 play: "With being able to build walls, we need the ability to build a
    /// gate so we can full wall off an area." Asked how it should open, they chose <b>auto, shuts behind</b>
    /// (U-D-72 a) — no key to press, no switch to leave thrown by mistake with a raid coming.
    ///
    /// <para><b>The gate is a wall that yields to one person.</b> It is solid in
    /// <see cref="World.GroundState.SolidMap"/>, it is <see cref="Sightline.Opaque"/>, it carries hit points, and
    /// <see cref="TurretRules.BlocksRaiders"/> answers true for it without a line of change — so a raid treats it
    /// exactly as it treats the wall either side, walks up to it and breaks it. What is different is one byte:
    /// SolidMap marks a standing gate <see cref="World.GroundState.YieldsToEngineer"/> rather than
    /// <see cref="World.GroundState.Blocked"/>, and the engineer's two readers — <see cref="World.PathFinder"/>
    /// and <see cref="World.Ground.PassableForEngineer"/> — are the only ones that treat that mark as open.
    /// Raider movement never reads SolidMap at all (<c>DirectorRules.HostileOpen</c> and
    /// <see cref="TurretRules.BlocksRaiders"/> are a separate path), so there is no way for this to leak.</para>
    ///
    /// <para><b>Why the engineer passes ALWAYS and not only when <see cref="Open"/> is true.</b> SolidMap is cached
    /// on <see cref="SimState.Rev"/>, which the engineer taking a step does not bump. A mask that depended on where
    /// the engineer is standing would be stale every tick or would have to be rebuilt every tick, and rebuilding it
    /// is 497,664 tiles on the real city map. So the passability is constant and it is the PICTURE that opens:
    /// <see cref="Open"/> is a pure function of where the engineer is, read by the presenter and by nothing in the
    /// tick. Mechanically the gate is a door the engineer never has to wait at, which is what "auto" was asked for;
    /// "shuts behind" is what the drawing does once they are two tiles past it.</para>
    ///
    /// <para><b>Nothing here is saved.</b> A gate is an ordinary machine, so it round-trips with every other one and
    /// <c>SaveSchema.Version</c> stays where it is. Its open/shut state round-trips because the engineer's position
    /// does: ask <see cref="Open"/> after a load and it answers what it answered before.</para>
    ///
    /// <para><b>Light does not pass an open gate.</b> <see cref="Sightline.Opaque"/> is a function of the kind
    /// alone, and <c>WallMask</c> is cached on <see cref="SimState.Rev"/> for the same reason SolidMap is, so an
    /// opacity that followed the leaf would thrash the cache. A gate stops the torch (REL-131) and a turret's
    /// sightline whether it is drawn open or shut — it is a solid door that parts just far enough for one person,
    /// not a hole in the wall. This is a deliberate limit and it is the thing to revisit if it reads badly in play.</para>
    ///
    /// <para>The numbers below are the assistant's under U-D-28; the note gives none. Recorded as U-P-36.</para>
    /// </summary>
    public static class GateRules
    {
        /// <summary>The machine kind. One tile, like the Wall — and deliberately so: <c>BuildingGlow.Glows</c>
        /// excludes anything under 2 tiles, which is how the Gate is kept out of REL-127's building light along
        /// with the Wall the owner excluded by name.</summary>
        public const string Kind = "gate";

        /// <summary>
        /// How close the engineer must be, in tiles from the gate's edge, for it to be DRAWN open. 2.0 is a stride
        /// and a half at walking pace: the leaf is already parting as they arrive rather than snapping aside under
        /// their feet, and it has shut again by the time they are clear of the wall's own thickness.
        /// </summary>
        public const double OpenRadiusTiles = 2.0;

        /// <summary>True for the gate kind.</summary>
        public static bool IsGate(string kind) => string.Equals(kind, Kind, StringComparison.Ordinal);

        /// <summary>True for a placed gate.</summary>
        public static bool IsGate(Machine m) => m != null && IsGate(m.Kind);

        /// <summary>
        /// Whether this gate is DRAWN open: the engineer is within <see cref="OpenRadiusTiles"/> of it, on their
        /// feet, and the gate is still standing. A wreck is a hole rather than a door and is drawn as the wreck it
        /// is. Pure — it reads state and changes none, and nothing in the tick calls it.
        /// </summary>
        public static bool Open(SimContext ctx, SimState st, Machine m)
        {
            if (ctx == null || st == null || !IsGate(m)) return false;
            if (TurretRules.Wrecked(ctx.Data, st, m)) return false;
            var e = st.Engineer;
            if (e == null || e.IsDown) return false;
            var (w, h) = m.Dimensions;
            return Distance(m.X, m.Y, w, h, e.Pos.X, e.Pos.Y) <= OpenRadiusTiles;
        }

        /// <summary>
        /// Which way the leaf lies: true when the gate sits in an EAST-WEST run of wall, so the door swings across
        /// the X axis. Read from the four neighbours rather than from <c>Machine.Dir</c> because the Wall is not a
        /// rotatable kind and a player fitting a gate into a line of wall never chose a facing — the wall either
        /// side is the only statement of intent there is. A gate standing alone, or in a corner, answers true, so
        /// the drawing has one definite default instead of flickering between two.
        /// </summary>
        public static bool Horizontal(SimContext ctx, SimState st, Machine m)
        {
            if (ctx == null || st == null || m == null) return true;
            var (w, h) = m.Dimensions;
            var sides = Walled(ctx, st, m.X - 1, m.Y) + Walled(ctx, st, m.X + w, m.Y);
            var ends = Walled(ctx, st, m.X, m.Y - 1) + Walled(ctx, st, m.X, m.Y + h);
            return sides >= ends;
        }

        private static int Walled(SimContext ctx, SimState st, int x, int y) =>
            st.Walls.At(ctx, st, x, y) ? 1 : 0;

        /// <summary>
        /// Distance in tiles from a point to the nearest edge of the half-open tile rect
        /// <c>[x, x + w) × [y, y + h)</c>, and 0 anywhere inside it. Measured from the EDGE so that the answer does
        /// not change if a gate is ever given a wider footprint.
        /// </summary>
        public static double Distance(int x, int y, int w, int h, double px, double py)
        {
            var dx = px < x ? x - px : px > x + w ? px - (x + w) : 0;
            var dy = py < y ? y - py : py > y + h ? py - (y + h) : 0;
            if (dx <= 0) return dy;
            if (dy <= 0) return dx;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
