using UnityEngine;

namespace SonsOfTheForest.Gameplay.Player
{
    public readonly struct PlayerGroundInfo
    {
        public PlayerGroundInfo(
            bool isGrounded,
            Vector3 normal,
            float slopeAngle,
            bool isWalkable)
        {
            IsGrounded = isGrounded;
            Normal = normal;
            SlopeAngle = slopeAngle;
            IsWalkable = isWalkable;
        }

        public bool IsGrounded { get; }

        public Vector3 Normal { get; }

        public float SlopeAngle { get; }

        public bool IsWalkable { get; }

        public static PlayerGroundInfo Airborne =>
            new(false, Vector3.zero, 0f, false);
    }
}
