using NUnit.Framework;
using SonsOfTheForest.Gameplay.Inventory;
using SonsOfTheForest.Gameplay.Items;

namespace SonsOfTheForest.Tests.EditMode.Gameplay
{
    public sealed class ItemStackTests
    {
        [Test]
        public void ValidItemIdWithPositiveQuantity_IsValid()
        {
            var stack = new ItemStack(new ItemId("item.stick"), 1);

            Assert.That(stack.IsValid, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ZeroOrNegativeQuantity_IsInvalid(int quantity)
        {
            var stack = new ItemStack(new ItemId("item.stick"), quantity);

            Assert.That(stack.IsValid, Is.False);
        }

        [Test]
        public void InvalidItemId_MakesStackInvalid()
        {
            var stack = new ItemStack(new ItemId(null), 1);

            Assert.That(stack.IsValid, Is.False);
        }
    }
}
