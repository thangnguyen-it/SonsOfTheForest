using SonsOfTheForest.Gameplay.Player;

namespace SonsOfTheForest.Presentation.Player
{
    internal static class PlayerAnimationSelector
    {
        internal const string Idle = "Idle_Loop";
        internal const string Walk = "Walk_Loop";
        internal const string Sprint = "Sprint_Loop";
        internal const string CrouchIdle = "Crouch_Idle_Loop";
        internal const string CrouchMove = "Crouch_Fwd_Loop";
        internal const string JumpStart = "Jump_Start";
        internal const string JumpLoop = "Jump_Loop";
        internal const string JumpLand = "Jump_Land";

        internal static string ResolveGrounded(PlayerMovementState state)
        {
            if (state.Stance == PlayerStance.Crouching)
            {
                return state.PlanarSpeed > 0.05f
                    ? CrouchMove
                    : CrouchIdle;
            }

            switch (state.LocomotionMode)
            {
                case PlayerLocomotionMode.Sprint:
                    return Sprint;
                case PlayerLocomotionMode.Walk:
                    return Walk;
                default:
                    return Idle;
            }
        }
    }
}
