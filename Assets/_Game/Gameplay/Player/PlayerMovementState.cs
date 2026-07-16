using UnityEngine;

namespace SonsOfTheForest.Gameplay.Player
{
    public readonly struct PlayerMovementState
    {
        public PlayerMovementState(
            PlayerLocomotionMode locomotionMode,
            PlayerStance stance,
            bool isGrounded,
            bool isSprinting,
            bool isCrouching,
            Vector3 planarVelocity,
            float verticalVelocity,
            Vector3 facingForward)
        {
            LocomotionMode = locomotionMode;
            Stance = stance;
            IsGrounded = isGrounded;
            IsSprinting = isSprinting;
            IsCrouching = isCrouching;
            PlanarVelocity = planarVelocity;
            VerticalVelocity = verticalVelocity;
            FacingForward = facingForward;
            PlanarSpeed = planarVelocity.magnitude;
        }

        public PlayerLocomotionMode LocomotionMode { get; }

        public PlayerStance Stance { get; }

        public bool IsGrounded { get; }

        public bool IsSprinting { get; }

        public bool IsCrouching { get; }

        public Vector3 PlanarVelocity { get; }

        public float VerticalVelocity { get; }

        public Vector3 FacingForward { get; }

        public float PlanarSpeed { get; }
    }
}
