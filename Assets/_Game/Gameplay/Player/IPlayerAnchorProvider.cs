using UnityEngine;

namespace SonsOfTheForest.Gameplay.Player
{
    public interface IPlayerAnchorProvider
    {
        bool TryGetAnchor(PlayerAnchorKind kind, out Transform anchor);
    }
}
