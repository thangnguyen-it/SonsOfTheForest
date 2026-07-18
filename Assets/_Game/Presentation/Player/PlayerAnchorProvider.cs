using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Presentation.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnchorProvider : MonoBehaviour,
        IPlayerAnchorProvider
    {
        [SerializeField]
        private Transform bodyRoot;

        [SerializeField]
        private Transform view;

        [SerializeField]
        private Transform interactionOrigin;

        [SerializeField]
        private Transform hands;

        [SerializeField]
        private Transform carry;

        public bool TryGetAnchor(
            PlayerAnchorKind kind,
            out Transform anchor)
        {
            switch (kind)
            {
                case PlayerAnchorKind.BodyRoot:
                    anchor = bodyRoot;
                    break;
                case PlayerAnchorKind.View:
                    anchor = view;
                    break;
                case PlayerAnchorKind.InteractionOrigin:
                    anchor = interactionOrigin;
                    break;
                case PlayerAnchorKind.Hands:
                    anchor = hands;
                    break;
                case PlayerAnchorKind.Carry:
                    anchor = carry;
                    break;
                default:
                    anchor = null;
                    break;
            }

            return anchor != null;
        }
    }
}
