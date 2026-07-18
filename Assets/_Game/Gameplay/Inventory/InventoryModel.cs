using System;
using System.Collections.Generic;
using SonsOfTheForest.Core;
using SonsOfTheForest.Gameplay.Items;

namespace SonsOfTheForest.Gameplay.Inventory
{
    public sealed class InventoryModel : IInventoryWriter
    {
        private readonly Dictionary<ItemId, int> counts = new();
        private readonly int maxPerItem;

        public InventoryModel(int maxPerItem = 50)
        {
            if (maxPerItem <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPerItem));
            }

            this.maxPerItem = maxPerItem;
        }

        public int MaxPerItem => maxPerItem;

        public int GetCount(ItemId itemId)
        {
            return itemId.IsValid && counts.TryGetValue(itemId, out int count)
                ? count
                : 0;
        }

        public bool Contains(ItemId itemId, int quantity)
        {
            return quantity > 0 && GetCount(itemId) >= quantity;
        }

        public GameResult<int> TryAdd(ItemStack stack)
        {
            if (!stack.IsValid)
            {
                return GameResult<int>.Failure("The item stack is invalid.");
            }

            int current = GetCount(stack.ItemId);
            int available = maxPerItem - current;
            if (available <= 0)
            {
                return GameResult<int>.Failure("The item stack is full.");
            }

            int added = Math.Min(stack.Quantity, available);
            counts[stack.ItemId] = current + added;
            return GameResult<int>.Success(added);
        }

        public GameResult TryRemove(ItemStack stack)
        {
            if (!stack.IsValid)
            {
                return GameResult.Failure("The item stack is invalid.");
            }

            int current = GetCount(stack.ItemId);
            if (current < stack.Quantity)
            {
                return GameResult.Failure("The inventory does not contain the requested quantity.");
            }

            int remaining = current - stack.Quantity;
            if (remaining == 0)
            {
                counts.Remove(stack.ItemId);
            }
            else
            {
                counts[stack.ItemId] = remaining;
            }

            return GameResult.Success();
        }
    }
}
