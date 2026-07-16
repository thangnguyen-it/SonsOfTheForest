using NUnit.Framework;
using SonsOfTheForest.Core;

namespace SonsOfTheForest.Tests.EditMode.Core
{
    public sealed class StableStringIdTests
    {
        [TestCase(null)]
        [TestCase("")]
        public void EmptyOrNullId_IsInvalid(string value)
        {
            var id = new StableStringId(value);

            Assert.That(id.IsValid, Is.False);
        }

        [Test]
        public void NonEmptyId_IsValid()
        {
            var id = new StableStringId("player.primary");

            Assert.That(id.IsValid, Is.True);
        }

        [Test]
        public void SameValues_AreEqual()
        {
            var left = new StableStringId("same");
            var right = new StableStringId("same");

            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void DifferentValues_AreNotEqual()
        {
            var left = new StableStringId("left");
            var right = new StableStringId("right");

            Assert.That(left, Is.Not.EqualTo(right));
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsValueOrEmptyStringSafely()
        {
            Assert.That(new StableStringId("value").ToString(), Is.EqualTo("value"));
            Assert.That(new StableStringId(null).ToString(), Is.Empty);
        }
    }
}
