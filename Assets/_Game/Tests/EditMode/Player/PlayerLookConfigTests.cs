using NUnit.Framework;
using SonsOfTheForest.Core;
using SonsOfTheForest.Gameplay.Player;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class PlayerLookConfigTests
    {
        [Test]
        public void Default_ValidatesWithoutErrors()
        {
            var report = new ValidationReport();

            PlayerLookConfig.Default.Validate(report);

            Assert.That(report.IsValid, Is.True);
        }

        [Test]
        public void NegativeSensitivity_ProducesValidationError()
        {
            var report = new ValidationReport();

            CreateConfig(mouseSensitivity: -0.1f).Validate(report);

            Assert.That(report.HasErrors, Is.True);
        }

        [Test]
        public void MinimumPitchAtMaximumPitch_ProducesValidationError()
        {
            var report = new ValidationReport();

            CreateConfig(minPitch: 10f, maxPitch: 10f).Validate(report);

            Assert.That(report.HasErrors, Is.True);
        }

        [Test]
        public void PitchOutsideAllowedRange_ProducesValidationError()
        {
            var report = new ValidationReport();

            CreateConfig(minPitch: -90f, maxPitch: 90f).Validate(report);

            Assert.That(report.HasErrors, Is.True);
        }

        private static PlayerLookConfig CreateConfig(
            float mouseSensitivity = 0.1f,
            float stickSensitivity = 120f,
            float minPitch = -89f,
            float maxPitch = 89f)
        {
            return new PlayerLookConfig(
                mouseSensitivity,
                stickSensitivity,
                minPitch,
                maxPitch,
                invertY: false);
        }
    }
}
