using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>CONTENT_CATALOGUE.md §15 (what the engineer starts with). The retired START_* constants are absent (§17.2).</summary>
    [CreateAssetMenu(menuName = "Relight/Tuning/Starting Stake", fileName = "Tuning - Starting stake")]
    public sealed class StartingStakeAsset : DataDefinition
    {
        [Tooltip("What is in the engineer's pockets on a new game.")]
        [SerializeField] private ItemAmount[] pockets = new ItemAmount[0];

        [Tooltip("The ruleset a new game starts under. 'exploration-v2'; legacy-v1 is retired (§17.2).")]
        [SerializeField] private string ruleset = "exploration-v2";

        public StartingStake ToRecord() => new StartingStake(ItemAmount.ToStacks(pockets), ruleset, KindText, Source, Provisional);

        public void Fill(StartingStake r)
        {
            SetCommon("stake", "Starting stake", r.Kind, r.Source, r.Provisional);
            pockets = ItemAmount.From(r.Pockets);
            ruleset = r.Ruleset;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (ruleset == "legacy-v1") return "legacy-v1 is a retired ruleset (CONTENT_CATALOGUE §17.2)";
            if (string.IsNullOrEmpty(ruleset)) return "ruleset is empty";
            foreach (var a in pockets) if (a.count <= 0) return $"starting stack {a.item} has a non-positive count";
            return null;
        }
    }
}
