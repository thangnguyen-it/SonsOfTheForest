using System.Collections.Generic;
using NUnit.Framework;
using SonsOfTheForest.Core;
using SonsOfTheForest.Gameplay.Building;
using SonsOfTheForest.Gameplay.Inventory;
using SonsOfTheForest.Gameplay.Items;

namespace SonsOfTheForest.Tests.ForestCamp.EditMode
{
    public sealed class ResourceRecipeTests
    {
        private static readonly ItemId Stick = new("resource.stick");
        private static readonly ItemId Stone = new("resource.stone");

        [Test]
        public void Constructor_ConsolidatesDuplicateRequirements()
        {
            var recipe = new ResourceRecipe(new[]
            {
                new ItemStack(Stick, 1),
                new ItemStack(Stone, 2),
                new ItemStack(Stick, 2),
            });

            Assert.That(recipe.Requirements.Count, Is.EqualTo(2));
            Assert.That(recipe.Requirements[0].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void CanAfford_RequiresEveryResource()
        {
            var inventory = new InventoryModel();
            inventory.TryAdd(new ItemStack(Stick, 2));
            var recipe = CreateCampfireRecipe();

            Assert.That(recipe.CanAfford(inventory), Is.False);

            inventory.TryAdd(new ItemStack(Stone, 4));
            Assert.That(recipe.CanAfford(inventory), Is.True);
        }

        [Test]
        public void TryConsume_RemovesAllRecipeResources()
        {
            var inventory = new InventoryModel();
            inventory.TryAdd(new ItemStack(Stick, 3));
            inventory.TryAdd(new ItemStack(Stone, 5));

            GameResult result = CreateCampfireRecipe().TryConsume(inventory);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(inventory.GetCount(Stick), Is.EqualTo(1));
            Assert.That(inventory.GetCount(Stone), Is.EqualTo(1));
        }

        [Test]
        public void TryConsume_WhenUnaffordable_RemovesNothing()
        {
            var inventory = new InventoryModel();
            inventory.TryAdd(new ItemStack(Stick, 2));

            GameResult result = CreateCampfireRecipe().TryConsume(inventory);

            Assert.That(result.Failed, Is.True);
            Assert.That(inventory.GetCount(Stick), Is.EqualTo(2));
        }

        [Test]
        public void TryConsume_WhenWriterFailsMidTransaction_RollsBack()
        {
            var inventory = new FailingSecondRemovalInventory();

            GameResult result = CreateCampfireRecipe().TryConsume(inventory);

            Assert.That(result.Failed, Is.True);
            Assert.That(inventory.GetCount(Stick), Is.EqualTo(2));
            Assert.That(inventory.GetCount(Stone), Is.EqualTo(4));
        }

        private static ResourceRecipe CreateCampfireRecipe()
        {
            return new ResourceRecipe(new[]
            {
                new ItemStack(Stick, 2),
                new ItemStack(Stone, 4),
            });
        }

        private sealed class FailingSecondRemovalInventory : IInventoryWriter
        {
            private readonly Dictionary<ItemId, int> counts = new()
            {
                [Stick] = 2,
                [Stone] = 4,
            };

            private int removalCalls;

            public int GetCount(ItemId itemId)
            {
                return counts.TryGetValue(itemId, out int count) ? count : 0;
            }

            public bool Contains(ItemId itemId, int quantity)
            {
                return GetCount(itemId) >= quantity;
            }

            public GameResult<int> TryAdd(ItemStack stack)
            {
                counts[stack.ItemId] = GetCount(stack.ItemId) + stack.Quantity;
                return GameResult<int>.Success(stack.Quantity);
            }

            public GameResult TryRemove(ItemStack stack)
            {
                removalCalls++;
                if (removalCalls == 2)
                {
                    return GameResult.Failure("Simulated writer failure.");
                }

                counts[stack.ItemId] = GetCount(stack.ItemId) - stack.Quantity;
                return GameResult.Success();
            }
        }
    }
}
