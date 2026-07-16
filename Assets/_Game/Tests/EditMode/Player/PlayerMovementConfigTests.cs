using NUnit.Framework;
using SonsOfTheForest.Core;
using SonsOfTheForest.Gameplay.Player;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class PlayerMovementConfigTests
    {
        [Test]
        public void Default_ValidatesWithoutErrors()
        {
            var report = new ValidationReport();

            PlayerMovementConfig.Default.Validate(report);

            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void NegativeSpeed_ProducesValidationError()
        {
            var report = new ValidationReport();

            CreateConfig(walkSpeed: -1f).Validate(report);

            Assert.That(report.HasErrors, Is.True);
        }

        [Test]
        public void SprintSpeedBelowWalkSpeed_ProducesValidationError()
        {
            var report = new ValidationReport();

            CreateConfig(walkSpeed: 5f, sprintSpeed: 4f).Validate(report);

            Assert.That(report.HasErrors, Is.True);
        }

        [Test]
        public void CrouchingHeightAtStandingHeight_ProducesValidationError()
        {
            var report = new ValidationReport();

            CreateConfig(
                standingHeight: 1.5f,
                crouchingHeight: 1.5f).Validate(report);

            Assert.That(report.HasErrors, Is.True);
        }

        [Test]
        public void NonNegativeGravity_ProducesValidationError()
        {
            var report = new ValidationReport();

            CreateConfig(gravity: 0f).Validate(report);

            Assert.That(report.HasErrors, Is.True);
        }

        [Test]
        public void InvalidSlopeLimit_ProducesValidationError()
        {
            var report = new ValidationReport();

            CreateConfig(slopeLimitDegrees: 90f).Validate(report);

            Assert.That(report.HasErrors, Is.True);
        }

        private static PlayerMovementConfig CreateConfig(
            float walkSpeed = 4f,
            float sprintSpeed = 7f,
            float crouchSpeed = 2f,
            float acceleration = 20f,
            float deceleration = 25f,
            float gravity = -19.62f,
            float jumpHeight = 1.2f,
            float groundSnapVelocity = -2f,
            float standingHeight = 2f,
            float crouchingHeight = 1.2f,
            float capsuleRadius = 0.35f,
            float stepOffset = 0.3f,
            float slopeLimitDegrees = 45f)
        {
            return new PlayerMovementConfig(
                walkSpeed,
                sprintSpeed,
                crouchSpeed,
                acceleration,
                deceleration,
                gravity,
                jumpHeight,
                groundSnapVelocity,
                standingHeight,
                crouchingHeight,
                capsuleRadius,
                stepOffset,
                slopeLimitDegrees,
                CrouchInputPolicy.Hold);
        }
    }
}
