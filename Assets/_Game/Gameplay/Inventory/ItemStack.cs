using SonsOfTheForest.Gameplay.Items;

namespace SonsOfTheForest.Gameplay.Inventory
{
    public readonly struct ItemStack
    {
        public ItemStack(ItemId itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        public ItemId ItemId { get; }

        public int Quantity { get; }

        public bool IsValid => ItemId.IsValid && Quantity > 0;
    }
}
