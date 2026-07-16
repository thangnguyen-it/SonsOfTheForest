using NUnit.Framework;
using SonsOfTheForest.Gameplay.Player;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class MovementConstraintsTests
    {
        [Test]
        public void Permissive_AllowsSprintJumpAndStand()
        {
            MovementConstraints constraints = MovementConstraints.Permissive;

            Assert.That(constraints.CanSprint, Is.True);
            Assert.That(constraints.CanJump, Is.True);
            Assert.That(constraints.CanStand, Is.True);
            Assert.That(constraints.SpeedMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Constructor_ClampsNegativeSpeedMultiplierToZero()
        {
            var constraints = new MovementConstraints(
                true,
                true,
                true,
                -1f);

            Assert.That(constraints.SpeedMultiplier, Is.EqualTo(0f));
        }

        [Test]
        public void Constructor_PreservesExplicitFlags()
        {
            var constraints = new MovementConstraints(
                false,
                true,
                false,
                0.5f);

            Assert.That(constraints.CanSprint, Is.False);
            Assert.That(constraints.CanJump, Is.True);
            Assert.That(constraints.CanStand, Is.False);
            Assert.That(constraints.SpeedMultiplier, Is.EqualTo(0.5f));
        }
    }
}
