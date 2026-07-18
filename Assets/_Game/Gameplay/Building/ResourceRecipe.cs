using System;
using System.Collections.Generic;
using SonsOfTheForest.Core;
using SonsOfTheForest.Gameplay.Inventory;

namespace SonsOfTheForest.Gameplay.Building
{
    public sealed class ResourceRecipe
    {
        private readonly ItemStack[] requirements;

        public ResourceRecipe(IEnumerable<ItemStack> requirements)
        {
            if (requirements == null)
            {
                throw new ArgumentNullException(nameof(requirements));
            }

            var consolidated = new List<ItemStack>();
            foreach (ItemStack requirement in requirements)
            {
                if (!requirement.IsValid)
                {
                    throw new ArgumentException(
                        "Every recipe requirement must be a valid item stack.",
                        nameof(requirements));
                }

                int existingIndex = consolidated.FindIndex(
                    stack => stack.ItemId == requirement.ItemId);
                if (existingIndex < 0)
                {
                    consolidated.Add(requirement);
                    continue;
                }

                ItemStack existing = consolidated[existingIndex];
                consolidated[existingIndex] = new ItemStack(
                    existing.ItemId,
                    checked(existing.Quantity + requirement.Quantity));
            }

            if (consolidated.Count == 0)
            {
                throw new ArgumentException(
                    "A resource recipe requires at least one item.",
                    nameof(requirements));
            }

            this.requirements = consolidated.ToArray();
        }

        public IReadOnlyList<ItemStack> Requirements => requirements;

        public bool CanAfford(IInventoryReader inventory)
        {
            if (inventory == null)
            {
                return false;
            }

            foreach (ItemStack requirement in requirements)
            {
                if (!inventory.Contains(
                    requirement.ItemId,
                    requirement.Quantity))
                {
                    return false;
                }
            }

            return true;
        }

        public GameResult TryConsume(IInventoryWriter inventory)
        {
            if (inventory == null)
            {
                return GameResult.Failure("An inventory is required.");
            }

            if (!CanAfford(inventory))
            {
                return GameResult.Failure("The recipe requirements are not available.");
            }

            var removed = new List<ItemStack>(requirements.Length);
            foreach (ItemStack requirement in requirements)
            {
                GameResult result = inventory.TryRemove(requirement);
                if (result.Succeeded)
                {
                    removed.Add(requirement);
                    continue;
                }

                RollBack(inventory, removed);
                return GameResult.Failure(
                    $"Recipe consumption failed: {result.Error}");
            }

            return GameResult.Success();
        }

        private static void RollBack(
            IInventoryWriter inventory,
            IReadOnlyList<ItemStack> removed)
        {
            for (int index = removed.Count - 1; index >= 0; index--)
            {
                inventory.TryAdd(removed[index]);
            }
        }
    }
}
