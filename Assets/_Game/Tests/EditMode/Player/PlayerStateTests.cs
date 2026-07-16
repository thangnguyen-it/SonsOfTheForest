using NUnit.Framework;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class PlayerStateTests
    {
        [Test]
        public void MovementState_PreservesReportedValues()
        {
            var planarVelocity = new Vector3(3f, 0f, 4f);
            var facing = Vector3.forward;
            var state = new PlayerMovementState(
                PlayerLocomotionMode.Sprint,
                PlayerStance.Standing,
                true,
                true,
                false,
                planarVelocity,
                -2f,
                facing);

            Assert.That(
                state.LocomotionMode,
                Is.EqualTo(PlayerLocomotionMode.Sprint));
            Assert.That(state.Stance, Is.EqualTo(PlayerStance.Standing));
            Assert.That(state.IsGrounded, Is.True);
            Assert.That(state.IsSprinting, Is.True);
            Assert.That(state.IsCrouching, Is.False);
            Assert.That(state.PlanarVelocity, Is.EqualTo(planarVelocity));
            Assert.That(state.VerticalVelocity, Is.EqualTo(-2f));
            Assert.That(state.FacingForward, Is.EqualTo(facing));
        }

        [Test]
        public void MovementState_PlanarSpeedUsesPlanarVelocityMagnitude()
        {
            var state = new PlayerMovementState(
                PlayerLocomotionMode.Walk,
                PlayerStance.Standing,
                true,
                false,
                false,
                new Vector3(3f, 0f, 4f),
                0f,
                Vector3.forward);

            Assert.That(state.PlanarSpeed, Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void LookStateIdentity_HasZeroYawAndPitch()
        {
            PlayerLookState state = PlayerLookState.Identity;

            Assert.That(state.Yaw, Is.EqualTo(0f));
            Assert.That(state.Pitch, Is.EqualTo(0f));
        }
    }
}
