namespace Relight.Sim
{
    /// <summary>Identity of the ported rule set (reference: packages/sim/src/rules.ts CAMPAIGN_RULESET).</summary>
    public static class SimVersion
    {
        public const string Ruleset = "exploration-v2";
        /// <summary>
        /// Bumped to 2 on 2026-09-12 with <see cref="SaveSchema.Version"/>: the hand-craft state gained the
        /// pending-refund fields (HandCraft.cs), and the reader guesses nothing, so a schema-1 save has no
        /// honest reading. Kept in lockstep with the file schema so a save's own <c>version</c> field agrees
        /// with its envelope.
        /// </summary>
        public const int SchemaVersion = 2;
    }
}
