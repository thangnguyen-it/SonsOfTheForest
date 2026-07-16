using System;
using SonsOfTheForest.Core;

namespace SonsOfTheForest.Gameplay.Player
{
    public readonly struct PlayerMovementConfig : IValidatable
    {
        public PlayerMovementConfig(
            float walkSpeed,
            float sprintSpeed,
            float crouchSpeed,
            float acceleration,
            float deceleration,
            float gravity,
            float jumpHeight,
            float groundSnapVelocity,
            float standingHeight,
            float crouchingHeight,
            float capsuleRadius,
            float stepOffset,
            float slopeLimitDegrees,
            CrouchInputPolicy crouchPolicy)
        {
            WalkSpeed = walkSpeed;
            SprintSpeed = sprintSpeed;
            CrouchSpeed = crouchSpeed;
            Acceleration = acceleration;
            Deceleration = deceleration;
            Gravity = gravity;
            JumpHeight = jumpHeight;
            GroundSnapVelocity = groundSnapVelocity;
            StandingHeight = standingHeight;
            CrouchingHeight = crouchingHeight;
            CapsuleRadius = capsuleRadius;
            StepOffset = stepOffset;
            SlopeLimitDegrees = slopeLimitDegrees;
            CrouchPolicy = crouchPolicy;
        }

        public float WalkSpeed { get; }

        public float SprintSpeed { get; }

        public float CrouchSpeed { get; }

        public float Acceleration { get; }

        public float Deceleration { get; }

        public float Gravity { get; }

        public float JumpHeight { get; }

        public float GroundSnapVelocity { get; }

        public float StandingHeight { get; }

        public float CrouchingHeight { get; }

        public float CapsuleRadius { get; }

        public float StepOffset { get; }

        public float SlopeLimitDegrees { get; }

        public CrouchInputPolicy CrouchPolicy { get; }

        public static PlayerMovementConfig Default =>
            new(
                walkSpeed: 4f,
                sprintSpeed: 7f,
                crouchSpeed: 2f,
                acceleration: 20f,
                deceleration: 25f,
                gravity: -19.62f,
                jumpHeight: 1.2f,
                groundSnapVelocity: -2f,
                standingHeight: 2f,
                crouchingHeight: 1.2f,
                capsuleRadius: 0.35f,
                stepOffset: 0.3f,
                slopeLimitDegrees: 45f,
                crouchPolicy: CrouchInputPolicy.Hold);

        public void Validate(ValidationReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            ValidateNonNegativeFinite(report, WalkSpeed, "player.walkSpeed");
            ValidateNonNegativeFinite(report, SprintSpeed, "player.sprintSpeed");
            ValidateNonNegativeFinite(report, CrouchSpeed, "player.crouchSpeed");

            if (IsFinite(WalkSpeed) &&
                IsFinite(SprintSpeed) &&
                SprintSpeed < WalkSpeed)
            {
                report.AddError(
                    "player.sprintSpeed.order",
                    "Sprint speed must be greater than or equal to walk speed.");
            }

            if (IsFinite(WalkSpeed) &&
                IsFinite(CrouchSpeed) &&
                CrouchSpeed > WalkSpeed)
            {
                report.AddError(
                    "player.crouchSpeed.order",
                    "Crouch speed must not exceed walk speed.");
            }

            ValidatePositiveFinite(
                report,
                Acceleration,
                "player.acceleration");
            ValidatePositiveFinite(
                report,
                Deceleration,
                "player.deceleration");

            if (!IsFinite(Gravity) || Gravity >= 0f)
            {
                report.AddError(
                    "player.gravity",
                    "Gravity must be finite and negative.");
            }

            ValidateNonNegativeFinite(report, JumpHeight, "player.jumpHeight");

            if (!IsFinite(StandingHeight) ||
                !IsFinite(CrouchingHeight) ||
                StandingHeight <= CrouchingHeight)
            {
                report.AddError(
                    "player.capsule.heightOrder",
                    "Standing height must be greater than crouching height.");
            }

            ValidatePositiveFinite(
                report,
                CapsuleRadius,
                "player.capsule.radius");

            if (IsFinite(CapsuleRadius) && CapsuleRadius > 0f)
            {
                float diameter = CapsuleRadius * 2f;

                if (!IsFinite(CrouchingHeight) ||
                    CrouchingHeight <= diameter)
                {
                    report.AddError(
                        "player.capsule.crouchingHeight",
                        "Crouching height must be greater than capsule diameter.");
                }

                if (!IsFinite(StandingHeight) ||
                    StandingHeight <= diameter)
                {
                    report.AddError(
                        "player.capsule.standingHeight",
                        "Standing height must be greater than capsule diameter.");
                }
            }

            ValidateNonNegativeFinite(report, StepOffset, "player.stepOffset");

            if (!IsFinite(SlopeLimitDegrees) ||
                SlopeLimitDegrees < 0f ||
                SlopeLimitDegrees > 89f)
            {
                report.AddError(
                    "player.slopeLimit",
                    "Slope limit must be between 0 and 89 degrees.");
            }
        }

        private static void ValidateNonNegativeFinite(
            ValidationReport report,
            float value,
            string code)
        {
            if (!IsFinite(value) || value < 0f)
            {
                report.AddError(code, "Value must be finite and nonnegative.");
            }
        }

        private static void ValidatePositiveFinite(
            ValidationReport report,
            float value,
            string code)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                report.AddError(code, "Value must be finite and positive.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
