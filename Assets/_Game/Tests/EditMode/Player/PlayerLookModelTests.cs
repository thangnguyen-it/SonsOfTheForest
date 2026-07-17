using NUnit.Framework;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class PlayerLookModelTests
    {
        private readonly PlayerLookModel _model = new();

        [Test]
        public void Step_DeltaAndRateUseDifferentTimeSemantics()
        {
            var config = Config(mouseSensitivity: 2f, stickSensitivity: 10f);

            PlayerLookState delta = _model.Step(
                PlayerLookState.Identity,
                new LookIntent(Vector2.right, LookInputKind.Delta),
                config,
                0.5f);
            PlayerLookState rate = _model.Step(
                PlayerLookState.Identity,
                new LookIntent(Vector2.right, LookInputKind.Rate),
                config,
                0.5f);

            Assert.That(delta.Yaw, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(rate.Yaw, Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void Step_AppliesConfiguredSensitivity()
        {
            PlayerLookState result = _model.Step(
                PlayerLookState.Identity,
                new LookIntent(new Vector2(2f, 0f), LookInputKind.Delta),
                Config(mouseSensitivity: 0.25f),
                1f);

            Assert.That(result.Yaw, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void Step_InvertYReversesPitchDirection()
        {
            LookIntent intent = new(Vector2.up, LookInputKind.Delta);

            PlayerLookState normal = _model.Step(
                PlayerLookState.Identity,
                intent,
                Config(mouseSensitivity: 2f, invertY: false),
                1f);
            PlayerLookState inverted = _model.Step(
                PlayerLookState.Identity,
                intent,
                Config(mouseSensitivity: 2f, invertY: true),
                1f);

            Assert.That(normal.Pitch, Is.EqualTo(-2f).Within(0.0001f));
            Assert.That(inverted.Pitch, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void Step_ClampsPitchToConfiguredLimits()
        {
            var config = Config(
                mouseSensitivity: 10f,
                minPitch: -20f,
                maxPitch: 30f);

            PlayerLookState up = _model.Step(
                PlayerLookState.Identity,
                new LookIntent(Vector2.down * 10f, LookInputKind.Delta),
                config,
                1f);
            PlayerLookState down = _model.Step(
                PlayerLookState.Identity,
                new LookIntent(Vector2.up * 10f, LookInputKind.Delta),
                config,
                1f);

            Assert.That(up.Pitch, Is.EqualTo(30f));
            Assert.That(down.Pitch, Is.EqualTo(-20f));
        }

        [Test]
        public void Step_WrapsYawDeterministically()
        {
            PlayerLookState positive = _model.Step(
                new PlayerLookState(179f, 0f),
                new LookIntent(Vector2.right * 2f, LookInputKind.Delta),
                Config(),
                1f);
            PlayerLookState negative = _model.Step(
                new PlayerLookState(-179f, 0f),
                new LookIntent(Vector2.left * 2f, LookInputKind.Delta),
                Config(),
                1f);

            Assert.That(positive.Yaw, Is.EqualTo(-179f).Within(0.0001f));
            Assert.That(negative.Yaw, Is.EqualTo(179f).Within(0.0001f));
        }

        [Test]
        public void Step_ZeroInputPreservesValidState()
        {
            var previous = new PlayerLookState(45f, -20f);

            PlayerLookState result = _model.Step(
                previous,
                LookIntent.None,
                Config(),
                0.25f);

            Assert.That(result.Yaw, Is.EqualTo(previous.Yaw));
            Assert.That(result.Pitch, Is.EqualTo(previous.Pitch));
        }

        [Test]
        public void Step_InvalidInputCannotEscapeAsNonFiniteState()
        {
            var previous = new PlayerLookState(
                float.PositiveInfinity,
                float.NaN);
            var intent = new LookIntent(
                new Vector2(float.NaN, float.NegativeInfinity),
                LookInputKind.Delta);

            PlayerLookState result = _model.Step(
                previous,
                intent,
                Config(),
                1f);

            Assert.That(float.IsNaN(result.Yaw), Is.False);
            Assert.That(float.IsInfinity(result.Yaw), Is.False);
            Assert.That(float.IsNaN(result.Pitch), Is.False);
            Assert.That(float.IsInfinity(result.Pitch), Is.False);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        public void Step_InvalidDeltaTimeDoesNotAdvanceRateLook(float deltaTime)
        {
            var previous = new PlayerLookState(12f, -5f);

            PlayerLookState result = _model.Step(
                previous,
                new LookIntent(Vector2.one, LookInputKind.Rate),
                Config(stickSensitivity: 100f),
                deltaTime);

            Assert.That(result.Yaw, Is.EqualTo(previous.Yaw));
            Assert.That(result.Pitch, Is.EqualTo(previous.Pitch));
        }

        private static PlayerLookConfig Config(
            float mouseSensitivity = 1f,
            float stickSensitivity = 1f,
            float minPitch = -89f,
            float maxPitch = 89f,
            bool invertY = false)
        {
            return new PlayerLookConfig(
                mouseSensitivity,
                stickSensitivity,
                minPitch,
                maxPitch,
                invertY);
        }
    }
}
