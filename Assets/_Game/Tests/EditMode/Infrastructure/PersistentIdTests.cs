using NUnit.Framework;
using SonsOfTheForest.Infrastructure.Persistence;

namespace SonsOfTheForest.Tests.EditMode.Infrastructure
{
    public sealed class PersistentIdTests
    {
        [Test]
        public void InvalidPersistentId_IsInvalid()
        {
            Assert.That(new PersistentId(null).IsValid, Is.False);
            Assert.That(new PersistentId(string.Empty).IsValid, Is.False);
        }

        [Test]
        public void ValidPersistentId_PreservesValue()
        {
            var id = new PersistentId("world.camp");

            Assert.That(id.IsValid, Is.True);
            Assert.That(id.Value, Is.EqualTo("world.camp"));
        }

        [Test]
        public void SamePersistentIds_AreEqual()
        {
            var left = new PersistentId("same");
            var right = new PersistentId("same");

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
        }

        [Test]
        public void DifferentPersistentIds_AreNotEqual()
        {
            var left = new PersistentId("left");
            var right = new PersistentId("right");

            Assert.That(left, Is.Not.EqualTo(right));
            Assert.That(left != right, Is.True);
        }
    }
}
