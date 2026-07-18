using NUnit.Framework;
using SonsOfTheForest.Gameplay.Inventory;
using SonsOfTheForest.Gameplay.Items;

namespace SonsOfTheForest.Tests.ForestCamp.EditMode
{
    public sealed class InventoryModelTests
    {
        private static readonly ItemId Stick = new("resource.stick");

        [Test]
        public void NewInventory_IsEmpty()
        {
            var inventory = new InventoryModel();

            Assert.That(inventory.GetCount(Stick), Is.Zero);
            Assert.That(inventory.Contains(Stick, 1), Is.False);
        }

        [Test]
        public void TryAdd_StoresRequestedQuantity()
        {
            var inventory = new InventoryModel();

            var result = inventory.TryAdd(new ItemStack(Stick, 3));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value, Is.EqualTo(3));
            Assert.That(inventory.GetCount(Stick), Is.EqualTo(3));
        }

        [Test]
        public void TryAdd_WhenStackWouldOverflow_AddsOnlyAvailableQuantity()
        {
            var inventory = new InventoryModel(5);
            inventory.TryAdd(new ItemStack(Stick, 4));

            var result = inventory.TryAdd(new ItemStack(Stick, 3));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value, Is.EqualTo(1));
            Assert.That(inventory.GetCount(Stick), Is.EqualTo(5));
        }

        [Test]
        public void TryAdd_WhenStackIsFull_FailsWithoutChangingCount()
        {
            var inventory = new InventoryModel(2);
            inventory.TryAdd(new ItemStack(Stick, 2));

            var result = inventory.TryAdd(new ItemStack(Stick, 1));

            Assert.That(result.Failed, Is.True);
            Assert.That(inventory.GetCount(Stick), Is.EqualTo(2));
        }

        [Test]
        public void TryRemove_WhenQuantityExists_RemovesExactlyRequestedAmount()
        {
            var inventory = new InventoryModel();
            inventory.TryAdd(new ItemStack(Stick, 4));

            var result = inventory.TryRemove(new ItemStack(Stick, 3));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(inventory.GetCount(Stick), Is.EqualTo(1));
        }

        [Test]
        public void TryRemove_WhenQuantityIsMissing_DoesNotMutateInventory()
        {
            var inventory = new InventoryModel();
            inventory.TryAdd(new ItemStack(Stick, 2));

            var result = inventory.TryRemove(new ItemStack(Stick, 3));

            Assert.That(result.Failed, Is.True);
            Assert.That(inventory.GetCount(Stick), Is.EqualTo(2));
        }

        [Test]
        public void InvalidStacks_AreRejected()
        {
            var inventory = new InventoryModel();

            Assert.That(
                inventory.TryAdd(new ItemStack(default, 1)).Failed,
                Is.True);
            Assert.That(
                inventory.TryRemove(new ItemStack(Stick, 0)).Failed,
                Is.True);
        }
    }
}
