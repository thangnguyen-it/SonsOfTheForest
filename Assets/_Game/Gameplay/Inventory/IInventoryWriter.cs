using SonsOfTheForest.Core;

namespace SonsOfTheForest.Gameplay.Inventory
{
    public interface IInventoryWriter : IInventoryReader
    {
        GameResult<int> TryAdd(ItemStack stack);

        GameResult TryRemove(ItemStack stack);
    }
}
