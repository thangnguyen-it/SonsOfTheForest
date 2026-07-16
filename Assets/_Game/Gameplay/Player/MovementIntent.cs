using UnityEngine;

namespace SonsOfTheForest.Gameplay.Player
{
    public readonly struct MovementIntent
    {
        private const float MovementInputThresholdSquared = 0.0001f;

        public MovementIntent(
            Vector2 move,
            bool sprintRequested,
            bool crouchRequested,
            bool jumpRequested)
        {
            Move = move.sqrMagnitude > 1f ? move.normalized : move;
            SprintRequested = sprintRequested;
            CrouchRequested = crouchRequested;
            JumpRequested = jumpRequested;
        }

        public Vector2 Move { get; }

        public bool SprintRequested { get; }

        public bool CrouchRequested { get; }

        public bool JumpRequested { get; }

        public bool HasMovementInput =>
            Move.sqrMagnitude > MovementInputThresholdSquared;

        public static MovementIntent None =>
            new(Vector2.zero, false, false, false);
    }
}
