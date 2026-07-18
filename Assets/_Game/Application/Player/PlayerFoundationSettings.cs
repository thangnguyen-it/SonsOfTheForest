using SonsOfTheForest.Core;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Application.Player
{
    [CreateAssetMenu(
        fileName = "CFG_PlayerFoundation",
        menuName = "Sons of the Forest/Player/Foundation Settings")]
    public sealed class PlayerFoundationSettings : ScriptableObject, IValidatable
    {
        [Header("Provisional movement")]
        [SerializeField]
        private float walkSpeed = 4f;

        [SerializeField]
        private float sprintSpeed = 7f;

        [SerializeField]
        private float crouchSpeed = 2f;

        [SerializeField]
        private float acceleration = 20f;

        [SerializeField]
        private float deceleration = 25f;

        [SerializeField]
        private float gravity = -19.62f;

        [SerializeField]
        private float jumpHeight = 1.2f;

        [SerializeField]
        private float groundSnapVelocity = -2f;

        [Header("Capsule")]
        [SerializeField]
        private float standingHeight = 2f;

        [SerializeField]
        private float crouchingHeight = 1.2f;

        [SerializeField]
        private float capsuleRadius = 0.35f;

        [SerializeField]
        private float stepOffset = 0.3f;

        [SerializeField]
        private float slopeLimitDegrees = 45f;

        [SerializeField]
        private CrouchInputPolicy crouchPolicy = CrouchInputPolicy.Hold;

        [Header("Provisional look")]
        [SerializeField]
        private float mouseSensitivity = 0.1f;

        [SerializeField]
        private float stickSensitivity = 120f;

        [SerializeField]
        private float minimumPitch = -89f;

        [SerializeField]
        private float maximumPitch = 89f;

        [SerializeField]
        private bool invertY;

        public PlayerMovementConfig MovementConfig =>
            new(
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
                crouchPolicy);

        public PlayerLookConfig LookConfig =>
            new(
                mouseSensitivity,
                stickSensitivity,
                minimumPitch,
                maximumPitch,
                invertY);

        public void Validate(ValidationReport report)
        {
            MovementConfig.Validate(report);
            LookConfig.Validate(report);

            if (!IsFinite(groundSnapVelocity) || groundSnapVelocity > 0f)
            {
                report.AddError(
                    "player.foundation.groundSnap",
                    "Ground snap velocity must be finite and nonpositive.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
