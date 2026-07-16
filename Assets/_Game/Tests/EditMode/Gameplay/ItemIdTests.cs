using NUnit.Framework;
using SonsOfTheForest.Gameplay.Items;

namespace SonsOfTheForest.Tests.EditMode.Gameplay
{
    public sealed class ItemIdTests
    {
        [Test]
        public void InvalidItemId_IsInvalid()
        {
            Assert.That(new ItemId(null).IsValid, Is.False);
            Assert.That(new ItemId(string.Empty).IsValid, Is.False);
        }

        [Test]
        public void ValidItemId_PreservesValue()
        {
            var id = new ItemId("item.stick");

            Assert.That(id.IsValid, Is.True);
            Assert.That(id.Value, Is.EqualTo("item.stick"));
        }

        [Test]
        public void SameItemIds_AreEqual()
        {
            var left = new ItemId("item.same");
            var right = new ItemId("item.same");

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
        }

        [Test]
        public void DifferentItemIds_AreNotEqual()
        {
            var left = new ItemId("item.left");
            var right = new ItemId("item.right");

            Assert.That(left, Is.Not.EqualTo(right));
            Assert.That(left != right, Is.True);
        }
    }
}
