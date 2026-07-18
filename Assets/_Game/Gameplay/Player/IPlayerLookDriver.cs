using UnityEngine;

namespace SonsOfTheForest.Gameplay.Player
{
    public interface IPlayerLookDriver
    {
        Vector3 PlanarForward { get; }

        Vector3 PlanarRight { get; }

        void ApplyLook(PlayerLookState state);
    }
}
