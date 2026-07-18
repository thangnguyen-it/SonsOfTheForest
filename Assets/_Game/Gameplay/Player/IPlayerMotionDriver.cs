using UnityEngine;

namespace SonsOfTheForest.Gameplay.Player
{
    public interface IPlayerMotionDriver
    {
        Vector3 CurrentPlanarVelocity { get; }

        float CurrentVerticalVelocity { get; }

        PlayerGroundInfo SampleGround(float slopeLimitDegrees);

        bool CanOccupyStance(
            PlayerStance stance,
            PlayerMovementConfig config);

        void ApplyMotion(
            PlayerMovementState state,
            PlayerGroundInfo groundInfo,
            PlayerMovementConfig config);
    }
}
