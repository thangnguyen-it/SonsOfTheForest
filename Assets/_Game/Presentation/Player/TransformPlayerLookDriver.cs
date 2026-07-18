using System;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Presentation.Player
{
    [DisallowMultipleComponent]
    public sealed class TransformPlayerLookDriver : MonoBehaviour,
        IPlayerLookDriver
    {
        [SerializeField]
        private Transform bodyYaw;

        [SerializeField]
        private Transform viewPitch;

        public Vector3 PlanarForward => ResolvePlanar(bodyYaw.forward);

        public Vector3 PlanarRight => ResolvePlanar(bodyYaw.right);

        private void Awake()
        {
            if (bodyYaw == null || viewPitch == null)
            {
                throw new InvalidOperationException(
                    "TransformPlayerLookDriver requires body-yaw and view-pitch transforms.");
            }
        }

        public void ApplyLook(PlayerLookState state)
        {
            bodyYaw.localRotation = Quaternion.Euler(0f, state.Yaw, 0f);
            viewPitch.localRotation = Quaternion.Euler(state.Pitch, 0f, 0f);
        }

        private static Vector3 ResolvePlanar(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > 0.000001f
                ? value.normalized
                : Vector3.forward;
        }
    }
}
