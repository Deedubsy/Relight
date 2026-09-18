namespace Relight.Sim
{
    /// <summary>Identity of the ported rule set (reference: packages/sim/src/rules.ts CAMPAIGN_RULESET).</summary>
    public static class SimVersion
    {
        public const string Ruleset = "exploration-v2";
        /// <summary>
        /// Bumped to 2 on 2026-09-12 with <see cref="SaveSchema.Version"/>: the hand-craft state gained the
        /// pending-refund fields (HandCraft.cs), and the reader guesses nothing, so a schema-1 save has no
        /// honest reading. Bumped to 3 on 2026-09-14 (Phase C, Wave 0): the Phase C subsystems add their own
        /// top-level state objects, defaulted for older files by <see cref="SaveUpgrade"/>. Bumped to 4 on
        /// 2026-09-14 (correction pass C6): the save header gained the <c>region</c> record, so that the same city
        /// imported as the Home crop and imported whole can be told apart and a save moved between them
        /// (<see cref="SaveRegion"/>, <see cref="SaveRelocate"/>). Nothing in the state itself changed at 4 — the
        /// number moves because it is kept in lockstep with the file schema, so a save's own <c>version</c> field
        /// always agrees with its envelope.
        /// </summary>
        public const int SchemaVersion = SaveSchema.Version;
    }
}
