using NUnit.Framework;
using SonsOfTheForest.Gameplay.Player;
using SonsOfTheForest.Presentation.Player;
using UnityEngine;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class PlayerFoundationPresentationTests
    {
        [TestCase(PlayerLocomotionMode.Idle, PlayerStance.Standing, 0f, "Idle_Loop")]
        [TestCase(PlayerLocomotionMode.Walk, PlayerStance.Standing, 2f, "Walk_Loop")]
        [TestCase(PlayerLocomotionMode.Sprint, PlayerStance.Standing, 6f, "Sprint_Loop")]
        [TestCase(PlayerLocomotionMode.Crouch, PlayerStance.Crouching, 0f, "Crouch_Idle_Loop")]
        [TestCase(PlayerLocomotionMode.Crouch, PlayerStance.Crouching, 1f, "Crouch_Fwd_Loop")]
        public void GroundedAnimationSelection_IsDeterministic(
            PlayerLocomotionMode mode,
            PlayerStance stance,
            float planarSpeed,
            string expected)
        {
            var state = new PlayerMovementState(
                mode,
                stance,
                true,
                mode == PlayerLocomotionMode.Sprint,
                stance == PlayerStance.Crouching,
                Vector3.forward * planarSpeed,
                -2f,
                Vector3.forward);

            Assert.That(PlayerAnimationSelector.ResolveGrounded(state),
                Is.EqualTo(expected));
        }

        [Test]
        public void AnimationSelection_ExposesAllJumpPhases()
        {
            Assert.That(PlayerAnimationSelector.JumpStart, Is.EqualTo("Jump_Start"));
            Assert.That(PlayerAnimationSelector.JumpLoop, Is.EqualTo("Jump_Loop"));
            Assert.That(PlayerAnimationSelector.JumpLand, Is.EqualTo("Jump_Land"));
        }
    }
}
