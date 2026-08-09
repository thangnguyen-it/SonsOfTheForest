using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Forest
{
    public readonly struct TreeDamageEvent
    {
        public TreeDamageEvent(float amount, Vector3 hitPosition, Vector3 hitDirection)
        {
            Amount = amount;
            HitPosition = hitPosition;
            HitDirection = hitDirection;
        }

        public float Amount { get; }

        public Vector3 HitPosition { get; }

        public Vector3 HitDirection { get; }

        public bool IsValid =>
            Amount > 0f && !float.IsNaN(Amount) && !float.IsInfinity(Amount) &&
            IsFinite(HitPosition) && IsFinite(HitDirection);

        private static bool IsFinite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }

    public interface ITreeDamageReceiver
    {
        bool TryReceiveDamage(in TreeDamageEvent damageEvent, out string reason);
    }
}
