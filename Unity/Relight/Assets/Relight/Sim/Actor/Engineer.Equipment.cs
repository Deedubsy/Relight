namespace Relight.Sim
{
    /// <summary>
    /// The engineer's Phase C members, added through the one pre-declared <see cref="Engineer.VisitEquipment"/>
    /// hook (Wave 0 contract §State rule 1, the W-C exception).
    ///
    /// Only the HANDS live here. The reference keeps the mining hand in <c>f.hand</c>
    /// (flow.ts:326 <c>hand: { mine, prog, full, ... }</c>, ticked at flow.ts:1007) because its flow state is one
    /// big record; the Wave 1 coordinator ruled that mining progress is engineer state in the port, so the three
    /// reference members are visited as members of the <c>engineer</c> object instead.
    ///
    /// The owned weapons, the two equipment slots and the projectiles are NOT here: they are the top-level
    /// <c>weapons</c> object (<see cref="WeaponState"/>, contracts.md §State table), so a save that has never
    /// seen a weapon carries one absent member rather than five.
    /// </summary>
    public sealed partial class Engineer
    {
        /// <summary>True while the hands are on a tile (reference <c>f.hand.mine !== null</c>).</summary>
        public bool Mining;
        /// <summary>The tile being dug; meaningless while <see cref="Mining"/> is false (reference <c>f.hand.mine</c>).</summary>
        public int MineX;
        public int MineY;
        /// <summary>Fraction of the next unit already dug, 0..1 (reference <c>f.hand.prog</c>).</summary>
        public double MineProg;
        /// <summary>The last stop was "the Backpack would not take it" (reference <c>f.hand.full</c>).</summary>
        public bool MineFull;

        partial void VisitEquipment(IStateVisitor v)
        {
            v.Field("mining", ref Mining);
            v.Field("mineX", ref MineX);
            v.Field("mineY", ref MineY);
            v.Field("mineProg", ref MineProg);
            v.Field("mineFull", ref MineFull);
        }
    }
}
