using SonsOfTheForest.Gameplay.Items;

namespace SonsOfTheForest.Gameplay.Inventory
{
    public interface IInventoryReader
    {
        int GetCount(ItemId itemId);

        bool Contains(ItemId itemId, int quantity);
    }
}
