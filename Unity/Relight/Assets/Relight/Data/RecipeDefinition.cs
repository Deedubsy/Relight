using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>One row of CONTENT_CATALOGUE.md §3 (recipes). Authoring asset for <see cref="Recipe"/>.</summary>
    [CreateAssetMenu(menuName = "Relight/Recipe Definition", fileName = "Recipe - New")]
    public sealed class RecipeDefinition : DataDefinition
    {
        [SerializeField] private ItemAmount[] inputs = new ItemAmount[0];
        [SerializeField] private ItemAmount[] outputs = new ItemAmount[0];

        [Tooltip("Seconds for one craft at the listed station.")]
        [SerializeField] private double seconds = 1;

        [Tooltip("Where it is crafted: a machine display name, or \"Home workshop\" for hand crafting.")]
        [SerializeField] private string station = string.Empty;

        [Tooltip("Set only when the product is not an item (the Rifle produces a weapon).")]
        [SerializeField] private string outputKey = string.Empty;

        public ItemAmount[] Inputs => inputs;
        public ItemAmount[] Outputs => outputs;
        public double Seconds => seconds;
        public string Station => station;
        public string OutputKey => outputKey;

        public Recipe ToRecord() => new Recipe(Key, DisplayName, ItemAmount.ToStacks(inputs), ItemAmount.ToStacks(outputs),
            seconds, station, outputKey, KindText, Source, Provisional);

        public void Fill(Recipe r)
        {
            SetCommon(r.Key, r.DisplayName, r.Kind, r.Source, r.Provisional);
            inputs = ItemAmount.From(r.Inputs);
            outputs = ItemAmount.From(r.Outputs);
            seconds = r.Seconds;
            station = r.Station;
            outputKey = r.OutputKey;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (seconds <= 0) return "craft time must be positive";
            if (string.IsNullOrEmpty(station)) return "station is empty";
            if ((outputs == null || outputs.Length == 0) && string.IsNullOrEmpty(outputKey))
                return "produces neither an item nor a named product";
            foreach (var a in inputs) if (a.count <= 0) return $"input {a.item} has a non-positive count";
            foreach (var a in outputs) if (a.count <= 0) return $"output {a.item} has a non-positive count";
            return null;
        }
    }
}
