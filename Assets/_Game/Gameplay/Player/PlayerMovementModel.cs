using UnityEngine;

namespace SonsOfTheForest.Gameplay.Player
{
    public sealed class PlayerMovementModel
    {
        private const float DirectionEpsilonSquared = 0.00000001f;
        private const float MotionEpsilonSquared = 0.00000001f;

        public PlayerMovementState Step(
            PlayerMovementState previousState,
            MovementIntent intent,
            MovementConstraints constraints,
            PlayerMovementConfig config,
            PlayerGroundInfo groundInfo,
            Vector3 planarForward,
            Vector3 planarRight,
            float deltaTime)
        {
            float safeDeltaTime = SanitizeDeltaTime(deltaTime);
            Vector3 forward = NormalizePlanar(planarForward);
            Vector3 right = NormalizePlanar(planarRight);
            Vector2 moveInput = SanitizeMoveInput(intent.Move);
            float inputMagnitude = moveInput.magnitude;
            Vector3 inputDirection = ResolveInputDirection(
                moveInput,
                forward,
                right);

            bool standingBlocked =
                !intent.CrouchRequested &&
                previousState.Stance == PlayerStance.Crouching &&
                !constraints.CanStand;
            PlayerStance stance =
                intent.CrouchRequested || standingBlocked
                    ? PlayerStance.Crouching
                    : PlayerStance.Standing;
            bool isCrouching = stance == PlayerStance.Crouching;
            bool hasMovementTarget =
                inputMagnitude > 0f &&
                inputDirection.sqrMagnitude > DirectionEpsilonSquared;
            bool hasWalkableGround =
                groundInfo.IsGrounded && groundInfo.IsWalkable;
            bool isSprinting =
                hasWalkableGround &&
                hasMovementTarget &&
                !isCrouching &&
                intent.SprintRequested &&
                constraints.CanSprint;

            float baseSpeed = isCrouching
                ? SanitizeNonnegative(config.CrouchSpeed)
                : isSprinting
                    ? SanitizeNonnegative(config.SprintSpeed)
                    : SanitizeNonnegative(config.WalkSpeed);
            float speedMultiplier =
                SanitizeNonnegative(constraints.SpeedMultiplier);
            float targetSpeed = SafeNonnegativeProduct(
                baseSpeed,
                speedMultiplier);
            Vector3 targetVelocity = hasMovementTarget
                ? inputDirection * (targetSpeed * inputMagnitude)
                : Vector3.zero;
            targetVelocity = SanitizePlanar(targetVelocity);

            Vector3 currentVelocity =
                SanitizePlanar(previousState.PlanarVelocity);
            float response = targetVelocity.sqrMagnitude > MotionEpsilonSquared
                ? SanitizeNonnegative(config.Acceleration)
                : SanitizeNonnegative(config.Deceleration);
            float maximumVelocityChange = SafeNonnegativeProduct(
                response,
                safeDeltaTime,
                float.MaxValue);
            Vector3 planarVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                maximumVelocityChange);
            planarVelocity = SanitizePlanar(planarVelocity);

            float gravity = SanitizeGravity(config.Gravity);
            float jumpHeight = SanitizeNonnegative(config.JumpHeight);
            bool canTakeOff =
                safeDeltaTime > 0f &&
                hasWalkableGround &&
                stance == PlayerStance.Standing &&
                intent.JumpRequested &&
                constraints.CanJump &&
                gravity < 0f &&
                jumpHeight > 0f;

            bool isGrounded;
            float verticalVelocity;

            if (canTakeOff)
            {
                isGrounded = false;
                verticalVelocity = ResolveTakeoffVelocity(
                    gravity,
                    jumpHeight);
            }
            else if (hasWalkableGround)
            {
                isGrounded = true;
                verticalVelocity = SanitizeGroundSnap(
                    config.GroundSnapVelocity);
            }
            else
            {
                isGrounded = false;
                float startingVelocity = previousState.IsGrounded
                    ? 0f
                    : SanitizeFinite(previousState.VerticalVelocity);
                verticalVelocity = SafeSum(
                    startingVelocity,
                    SafeProduct(gravity, safeDeltaTime));
            }

            Vector3 facingForward = inputDirection.sqrMagnitude >
                DirectionEpsilonSquared
                    ? inputDirection
                    : ResolveFallbackFacing(
                        previousState.FacingForward,
                        forward);
            PlayerLocomotionMode locomotionMode = ResolveLocomotionMode(
                isGrounded,
                standingBlocked,
                isCrouching,
                isSprinting,
                planarVelocity);

            return new PlayerMovementState(
                locomotionMode,
                stance,
                isGrounded,
                locomotionMode == PlayerLocomotionMode.Sprint,
                isCrouching,
                planarVelocity,
                verticalVelocity,
                facingForward);
        }

        private static PlayerLocomotionMode ResolveLocomotionMode(
            bool isGrounded,
            bool standingBlocked,
            bool isCrouching,
            bool isSprinting,
            Vector3 planarVelocity)
        {
            if (!isGrounded)
            {
                return PlayerLocomotionMode.Airborne;
            }

            if (standingBlocked)
            {
                return PlayerLocomotionMode.GroundedBlocked;
            }

            if (planarVelocity.sqrMagnitude <= MotionEpsilonSquared)
            {
                return PlayerLocomotionMode.Idle;
            }

            if (isCrouching)
            {
                return PlayerLocomotionMode.Crouch;
            }

            return isSprinting
                ? PlayerLocomotionMode.Sprint
                : PlayerLocomotionMode.Walk;
        }

        private static Vector2 SanitizeMoveInput(Vector2 input)
        {
            if (!IsFinite(input.x) || !IsFinite(input.y))
            {
                return Vector2.zero;
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        private static Vector3 ResolveInputDirection(
            Vector2 input,
            Vector3 forward,
            Vector3 right)
        {
            Vector3 direction = forward * input.y + right * input.x;
            direction = SanitizePlanar(direction);

            return direction.sqrMagnitude > DirectionEpsilonSquared
                ? direction.normalized
                : Vector3.zero;
        }

        private static Vector3 ResolveFallbackFacing(
            Vector3 previousFacing,
            Vector3 forward)
        {
            Vector3 facing = NormalizePlanar(previousFacing);

            if (facing.sqrMagnitude > DirectionEpsilonSquared)
            {
                return facing;
            }

            if (forward.sqrMagnitude > DirectionEpsilonSquared)
            {
                return forward;
            }

            return Vector3.forward;
        }

        private static Vector3 NormalizePlanar(Vector3 value)
        {
            Vector3 planar = SanitizePlanar(value);

            return planar.sqrMagnitude > DirectionEpsilonSquared
                ? planar.normalized
                : Vector3.zero;
        }

        private static Vector3 SanitizePlanar(Vector3 value)
        {
            if (!IsFinite(value.x) || !IsFinite(value.z))
            {
                return Vector3.zero;
            }

            return new Vector3(value.x, 0f, value.z);
        }

        private static float ResolveTakeoffVelocity(
            float gravity,
            float jumpHeight)
        {
            float radicand = SafeNonnegativeProduct(
                2f * Mathf.Abs(gravity),
                jumpHeight);
            float takeoffVelocity = Mathf.Sqrt(radicand);

            return SanitizeFinite(takeoffVelocity);
        }

        private static float SanitizeDeltaTime(float deltaTime)
        {
            return IsFinite(deltaTime) && deltaTime > 0f
                ? deltaTime
                : 0f;
        }

        private static float SanitizeGravity(float gravity)
        {
            return IsFinite(gravity) && gravity < 0f
                ? gravity
                : 0f;
        }

        private static float SanitizeGroundSnap(float groundSnapVelocity)
        {
            return IsFinite(groundSnapVelocity)
                ? Mathf.Min(0f, groundSnapVelocity)
                : 0f;
        }

        private static float SanitizeNonnegative(float value)
        {
            return IsFinite(value) && value >= 0f
                ? value
                : 0f;
        }

        private static float SanitizeFinite(float value)
        {
            return IsFinite(value) ? value : 0f;
        }

        private static float SafeNonnegativeProduct(
            float left,
            float right,
            float overflowFallback = 0f)
        {
            float product = left * right;

            return IsFinite(product) && product >= 0f
                ? product
                : overflowFallback;
        }

        private static float SafeProduct(float left, float right)
        {
            float product = left * right;

            return IsFinite(product) ? product : 0f;
        }

        private static float SafeSum(float left, float right)
        {
            float sum = left + right;

            return IsFinite(sum) ? sum : 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
