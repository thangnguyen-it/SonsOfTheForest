using NUnit.Framework;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class MovementIntentTests
    {
        [Test]
        public void None_HasZeroMoveAndNoRequests()
        {
            MovementIntent intent = MovementIntent.None;

            Assert.That(intent.Move, Is.EqualTo(Vector2.zero));
            Assert.That(intent.SprintRequested, Is.False);
            Assert.That(intent.CrouchRequested, Is.False);
            Assert.That(intent.JumpRequested, Is.False);
            Assert.That(intent.HasMovementInput, Is.False);
        }

        [Test]
        public void Constructor_ClampsMoveVectorToUnitMagnitude()
        {
            var intent = new MovementIntent(
                new Vector2(3f, 4f),
                false,
                false,
                false);

            Assert.That(intent.Move.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void HasMovementInput_IsFalseForNearZeroVector()
        {
            var intent = new MovementIntent(
                new Vector2(0.001f, 0f),
                false,
                false,
                false);

            Assert.That(intent.HasMovementInput, Is.False);
        }

        [Test]
        public void HasMovementInput_IsTrueForNonZeroVector()
        {
            var intent = new MovementIntent(
                new Vector2(0.25f, 0f),
                false,
                false,
                false);

            Assert.That(intent.HasMovementInput, Is.True);
        }

        [Test]
        public void Constructor_PreservesRequestFlags()
        {
            var intent = new MovementIntent(
                Vector2.zero,
                true,
                true,
                true);

            Assert.That(intent.SprintRequested, Is.True);
            Assert.That(intent.CrouchRequested, Is.True);
            Assert.That(intent.JumpRequested, Is.True);
        }
    }
}
