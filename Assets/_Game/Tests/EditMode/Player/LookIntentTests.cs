using NUnit.Framework;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class LookIntentTests
    {
        [Test]
        public void None_IsZero()
        {
            LookIntent intent = LookIntent.None;

            Assert.That(intent.Value, Is.EqualTo(Vector2.zero));
            Assert.That(intent.IsZero, Is.True);
        }

        [Test]
        public void Constructor_PreservesDeltaKind()
        {
            var intent = new LookIntent(Vector2.one, LookInputKind.Delta);

            Assert.That(intent.InputKind, Is.EqualTo(LookInputKind.Delta));
        }

        [Test]
        public void Constructor_PreservesRateKind()
        {
            var intent = new LookIntent(Vector2.one, LookInputKind.Rate);

            Assert.That(intent.InputKind, Is.EqualTo(LookInputKind.Rate));
        }

        [Test]
        public void IsZero_IsTrueForZeroValue()
        {
            var intent = new LookIntent(Vector2.zero, LookInputKind.Rate);

            Assert.That(intent.IsZero, Is.True);
        }

        [Test]
        public void IsZero_IsFalseForNonZeroValue()
        {
            var intent = new LookIntent(Vector2.up, LookInputKind.Delta);

            Assert.That(intent.IsZero, Is.False);
        }
    }
}
