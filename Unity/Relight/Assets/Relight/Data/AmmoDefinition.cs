using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>One row of CONTENT_CATALOGUE.md §6.1 (ammunition, Unity column). Authoring asset for <see cref="AmmoDef"/>.</summary>
    [CreateAssetMenu(menuName = "Relight/Ammo Definition", fileName = "Ammo - New")]
    public sealed class AmmoDefinition : DataDefinition
    {
        [Tooltip("The carried item this ammunition is.")]
        [SerializeField] private ItemId item = ItemId.Magazine;

        [Tooltip("Rounds fired per item consumed. One under U-D-08 (one item = one bullet).")]
        [SerializeField] private int roundsPerItem = 1;

        [SerializeField] private int stackSize = 1;

        [Tooltip("How many items one craft produces.")]
        [SerializeField] private int itemsPerCraft = 1;

        [Tooltip("How many the producing machine buffers before it stalls.")]
        [SerializeField] private int outputBuffer = 1;

        public ItemId Item => item;

        public AmmoDef ToRecord() => new AmmoDef(Key, DisplayName, item, roundsPerItem, stackSize, itemsPerCraft,
            outputBuffer, KindText, Source, Provisional);

        public void Fill(AmmoDef r)
        {
            SetCommon(r.Key, r.DisplayName, r.Kind, r.Source, r.Provisional);
            item = r.Item;
            roundsPerItem = r.RoundsPerItem;
            stackSize = r.StackSize;
            itemsPerCraft = r.ItemsPerCraft;
            outputBuffer = r.OutputBuffer;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (roundsPerItem <= 0) return "rounds per item must be positive";
            if (stackSize <= 0) return "stack size must be positive";
            if (itemsPerCraft <= 0) return "items per craft must be positive";
            if (outputBuffer <= 0) return "output buffer must be positive";
            return null;
        }
    }
}
