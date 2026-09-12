using UnityEngine;
using Relight.Sim;

namespace Relight.Data
{
    /// <summary>One row of CONTENT_CATALOGUE.md §1 (items). Authoring asset for <see cref="ItemDef"/>.</summary>
    [CreateAssetMenu(menuName = "Relight/Item Definition", fileName = "Item - New")]
    public sealed class ItemDefinition : DataDefinition
    {
        [Tooltip("The sim's enum id. Key and id must agree.")]
        [SerializeField] private ItemId id = ItemId.Steel;

        [Tooltip("How many fit in one inventory stack (engineer.ts STACK).")]
        [SerializeField] private int stackSize = 1;

        [Tooltip("Left empty by B-05: item icons are rasterised in C-06 (U-M-27).")]
        [SerializeField] private Sprite icon;

        public ItemId Id => id;
        public int StackSize => stackSize;
        public Sprite Icon => icon;

        public ItemDef ToRecord() => new ItemDef(id, Key, DisplayName, stackSize, KindText, Source, Provisional);

        public void Fill(ItemDef r)
        {
            SetCommon(r.Key, r.DisplayName, r.Kind, r.Source, r.Provisional);
            id = r.Id;
            stackSize = r.StackSize;
        }

        public override string Problem()
        {
            var baseProblem = base.Problem();
            if (baseProblem != null) return baseProblem;
            if (stackSize <= 0) return "stack size must be positive";
            if (Items.Key(id) != Key) return $"key '{Key}' does not match the id {id} ('{Items.Key(id)}')";
            return null;
        }
    }
}
