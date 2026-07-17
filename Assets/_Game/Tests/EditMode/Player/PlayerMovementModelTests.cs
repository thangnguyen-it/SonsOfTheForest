using NUnit.Framework;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class PlayerMovementModelTests
    {
        private readonly PlayerMovementModel _model = new();

        [Test]
        public void Step_MapsCardinalInputToPlanarBases()
        {
            PlayerMovementState forward = Step(
                GroundedState(),
                new MovementIntent(Vector2.up, false, false, false));
            PlayerMovementState right = Step(
                GroundedState(),
                new MovementIntent(Vector2.right, false, false, false));

            AssertVector(forward.PlanarVelocity, Vector3.forward * 4f);
            AssertVector(right.PlanarVelocity, Vector3.right * 4f);
        }

        [Test]
        public void Step_ClampsDiagonalWithoutSpeedBoost()
        {
            PlayerMovementState result = Step(
                GroundedState(),
                new MovementIntent(Vector2.one, false, false, false));

            Assert.That(result.PlanarSpeed, Is.EqualTo(4f).Within(0.0001f));
            AssertVector(
                result.PlanarVelocity.normalized,
                new Vector3(1f, 0f, 1f).normalized);
        }

        [Test]
        public void Step_PreservesAnalogMagnitude()
        {
            PlayerMovementState result = Step(
                GroundedState(),
                new MovementIntent(Vector2.up * 0.5f, false, false, false));

            Assert.That(result.PlanarSpeed, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void Step_RemovesPitchFromMovementBases()
        {
            PlayerMovementState result = Step(
                GroundedState(),
                new MovementIntent(Vector2.up, false, false, false),
                planarForward: new Vector3(0f, 10f, 2f),
                planarRight: new Vector3(2f, -5f, 0f));

            AssertVector(result.PlanarVelocity, Vector3.forward * 4f);
            Assert.That(result.PlanarVelocity.y, Is.Zero);
        }

        [Test]
        public void Step_SelectsWalkSprintAndCrouchModes()
        {
            PlayerMovementState walking = Step(
                GroundedState(),
                new MovementIntent(Vector2.up, false, false, false));
            PlayerMovementState sprinting = Step(
                GroundedState(),
                new MovementIntent(Vector2.up, true, false, false));
            PlayerMovementState crouching = Step(
                GroundedState(),
                new MovementIntent(Vector2.up, true, true, false));

            Assert.That(walking.LocomotionMode, Is.EqualTo(PlayerLocomotionMode.Walk));
            Assert.That(walking.PlanarSpeed, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(sprinting.LocomotionMode, Is.EqualTo(PlayerLocomotionMode.Sprint));
            Assert.That(sprinting.PlanarSpeed, Is.EqualTo(7f).Within(0.0001f));
            Assert.That(crouching.LocomotionMode, Is.EqualTo(PlayerLocomotionMode.Crouch));
            Assert.That(crouching.PlanarSpeed, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(crouching.IsSprinting, Is.False);
        }

        [Test]
        public void Step_DeniesSprintWhenConstraintDisallowsIt()
        {
            var constraints = new MovementConstraints(false, true, true, 1f);

            PlayerMovementState result = Step(
                GroundedState(),
                new MovementIntent(Vector2.up, true, false, false),
                constraints: constraints);

            Assert.That(result.IsSprinting, Is.False);
            Assert.That(
                result.LocomotionMode,
                Is.EqualTo(PlayerLocomotionMode.Walk));
            Assert.That(result.PlanarSpeed, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void Step_RemainsCrouchedWhenStandingIsBlocked()
        {
            var previous = new PlayerMovementState(
                PlayerLocomotionMode.Crouch,
                PlayerStance.Crouching,
                true,
                false,
                true,
                Vector3.zero,
                -2f,
                Vector3.forward);
            var constraints = new MovementConstraints(true, true, false, 1f);

            PlayerMovementState result = Step(
                previous,
                MovementIntent.None,
                constraints: constraints);

            Assert.That(result.Stance, Is.EqualTo(PlayerStance.Crouching));
            Assert.That(result.IsCrouching, Is.True);
            Assert.That(
                result.LocomotionMode,
                Is.EqualTo(PlayerLocomotionMode.GroundedBlocked));
        }

        [Test]
        public void Step_AcceleratesByConfiguredRate()
        {
            PlayerMovementState result = Step(
                GroundedState(),
                new MovementIntent(Vector2.up, false, false, false),
                deltaTime: 0.1f);

            Assert.That(result.PlanarSpeed, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void Step_DeceleratesByConfiguredRate()
        {
            PlayerMovementState result = Step(
                GroundedState(Vector3.forward * 4f),
                MovementIntent.None,
                deltaTime: 0.1f);

            Assert.That(result.PlanarSpeed, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void Step_AppliesConstraintSpeedMultiplier()
        {
            var constraints = new MovementConstraints(true, true, true, 0.5f);

            PlayerMovementState result = Step(
                GroundedState(),
                new MovementIntent(Vector2.up, false, false, false),
                constraints: constraints);

            Assert.That(result.PlanarSpeed, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void Step_TreatsOnlyWalkableGroundAsGrounded()
        {
            PlayerMovementState walkable = Step(
                GroundedState(),
                MovementIntent.None);
            PlayerMovementState unwalkable = Step(
                GroundedState(),
                MovementIntent.None,
                groundInfo: new PlayerGroundInfo(
                    true,
                    Vector3.up,
                    70f,
                    false),
                deltaTime: 0.1f);

            Assert.That(walkable.IsGrounded, Is.True);
            Assert.That(unwalkable.IsGrounded, Is.False);
            Assert.That(
                unwalkable.LocomotionMode,
                Is.EqualTo(PlayerLocomotionMode.Airborne));
            Assert.That(
                unwalkable.VerticalVelocity,
                Is.EqualTo(-1.962f).Within(0.0001f));
        }

        [Test]
        public void Step_JumpsOnlyWhenEligibleOnWalkableGround()
        {
            var blocked = new MovementConstraints(true, false, true, 1f);
            var jumpIntent = new MovementIntent(Vector2.zero, false, false, true);

            PlayerMovementState denied = Step(
                GroundedState(),
                jumpIntent,
                constraints: blocked);
            PlayerMovementState airborne = Step(
                AirborneState(),
                jumpIntent,
                groundInfo: PlayerGroundInfo.Airborne);

            Assert.That(denied.IsGrounded, Is.True);
            Assert.That(denied.VerticalVelocity, Is.EqualTo(-2f));
            Assert.That(airborne.VerticalVelocity, Is.LessThan(0f));
        }

        [Test]
        public void Step_UsesProvisionalBallisticTakeoffVelocity()
        {
            var jumpIntent = new MovementIntent(Vector2.zero, false, false, true);

            PlayerMovementState result = Step(GroundedState(), jumpIntent);
            float expected = Mathf.Sqrt(2f * 19.62f * 1.2f);

            Assert.That(result.IsGrounded, Is.False);
            Assert.That(
                result.LocomotionMode,
                Is.EqualTo(PlayerLocomotionMode.Airborne));
            Assert.That(
                result.VerticalVelocity,
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void Step_IntegratesGravityWhileAirborne()
        {
            PlayerMovementState result = Step(
                AirborneState(verticalVelocity: 2f),
                MovementIntent.None,
                groundInfo: PlayerGroundInfo.Airborne,
                deltaTime: 0.5f);

            Assert.That(
                result.VerticalVelocity,
                Is.EqualTo(-7.81f).Within(0.0001f));
        }

        [Test]
        public void Step_AppliesGroundAdhesionVelocity()
        {
            PlayerMovementState result = Step(
                AirborneState(verticalVelocity: -12f),
                MovementIntent.None);

            Assert.That(result.IsGrounded, Is.True);
            Assert.That(result.VerticalVelocity, Is.EqualTo(-2f));
        }

        [Test]
        public void Step_UpdatesFacingFromInputAndPreservesItAtRest()
        {
            PlayerMovementState moving = Step(
                GroundedState(),
                new MovementIntent(Vector2.right, false, false, false));
            PlayerMovementState resting = Step(
                GroundedState(facing: Vector3.left),
                MovementIntent.None);

            AssertVector(moving.FacingForward, Vector3.right);
            AssertVector(resting.FacingForward, Vector3.left);
        }

        [Test]
        public void Step_SanitizesInvalidVectorsAndMultiplier()
        {
            var previous = new PlayerMovementState(
                PlayerLocomotionMode.Airborne,
                PlayerStance.Standing,
                false,
                false,
                false,
                new Vector3(float.NaN, float.PositiveInfinity, 1f),
                float.NegativeInfinity,
                new Vector3(float.NaN, 0f, float.PositiveInfinity));
            var intent = new MovementIntent(
                new Vector2(float.NaN, float.PositiveInfinity),
                true,
                false,
                false);
            var constraints = new MovementConstraints(
                true,
                true,
                true,
                float.PositiveInfinity);

            PlayerMovementState result = Step(
                previous,
                intent,
                constraints,
                groundInfo: PlayerGroundInfo.Airborne,
                planarForward: new Vector3(float.NaN, 2f, 1f),
                planarRight: new Vector3(1f, 3f, float.PositiveInfinity));

            AssertFinite(result);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        public void Step_InvalidDeltaTimeDoesNotIntegrate(float deltaTime)
        {
            Vector3 velocity = Vector3.forward;
            PlayerMovementState result = Step(
                GroundedState(velocity),
                new MovementIntent(Vector2.up, false, false, false),
                deltaTime: deltaTime);

            AssertVector(result.PlanarVelocity, velocity);
            AssertFinite(result);
        }

        private PlayerMovementState Step(
            PlayerMovementState previous,
            MovementIntent intent,
            MovementConstraints? constraints = null,
            PlayerMovementConfig? config = null,
            PlayerGroundInfo? groundInfo = null,
            Vector3? planarForward = null,
            Vector3? planarRight = null,
            float deltaTime = 1f)
        {
            return _model.Step(
                previous,
                intent,
                constraints ?? MovementConstraints.Permissive,
                config ?? PlayerMovementConfig.Default,
                groundInfo ?? WalkableGround(),
                planarForward ?? Vector3.forward,
                planarRight ?? Vector3.right,
                deltaTime);
        }

        private static PlayerMovementState GroundedState(
            Vector3? planarVelocity = null,
            Vector3? facing = null)
        {
            return new PlayerMovementState(
                PlayerLocomotionMode.Idle,
                PlayerStance.Standing,
                true,
                false,
                false,
                planarVelocity ?? Vector3.zero,
                -2f,
                facing ?? Vector3.forward);
        }

        private static PlayerMovementState AirborneState(
            float verticalVelocity = 0f)
        {
            return new PlayerMovementState(
                PlayerLocomotionMode.Airborne,
                PlayerStance.Standing,
                false,
                false,
                false,
                Vector3.zero,
                verticalVelocity,
                Vector3.forward);
        }

        private static PlayerGroundInfo WalkableGround()
        {
            return new PlayerGroundInfo(true, Vector3.up, 0f, true);
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }

        private static void AssertFinite(PlayerMovementState state)
        {
            Assert.That(float.IsNaN(state.PlanarVelocity.x), Is.False);
            Assert.That(float.IsInfinity(state.PlanarVelocity.x), Is.False);
            Assert.That(float.IsNaN(state.PlanarVelocity.y), Is.False);
            Assert.That(float.IsInfinity(state.PlanarVelocity.y), Is.False);
            Assert.That(float.IsNaN(state.PlanarVelocity.z), Is.False);
            Assert.That(float.IsInfinity(state.PlanarVelocity.z), Is.False);
            Assert.That(float.IsNaN(state.VerticalVelocity), Is.False);
            Assert.That(float.IsInfinity(state.VerticalVelocity), Is.False);
            Assert.That(float.IsNaN(state.FacingForward.x), Is.False);
            Assert.That(float.IsInfinity(state.FacingForward.x), Is.False);
            Assert.That(float.IsNaN(state.FacingForward.y), Is.False);
            Assert.That(float.IsInfinity(state.FacingForward.y), Is.False);
            Assert.That(float.IsNaN(state.FacingForward.z), Is.False);
            Assert.That(float.IsInfinity(state.FacingForward.z), Is.False);
        }
    }
}
