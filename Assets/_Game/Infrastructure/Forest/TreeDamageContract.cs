using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Forest
{
    public readonly struct TreeDamageEvent
    {
        public TreeDamageEvent(float amount, Vector3 hitPosition, Vector3 hitDirection)
            : this(amount, hitPosition, Vector3.zero, hitDirection, "tool.unspecified", 0)
        {
        }

        public TreeDamageEvent(
            float amount,
            Vector3 hitPosition,
            Vector3 hitNormal,
            Vector3 hitDirection,
            string toolId,
            int swingSequence)
        {
            Amount = amount;
            HitPosition = hitPosition;
            HitNormal = hitNormal;
            HitDirection = hitDirection;
            ToolId = toolId ?? string.Empty;
            SwingSequence = swingSequence;
        }

        public float Amount { get; }

        public Vector3 HitPosition { get; }

        public Vector3 HitNormal { get; }

        public Vector3 HitDirection { get; }

        public string ToolId { get; }

        public int SwingSequence { get; }

        public bool IsValid =>
            Amount > 0f && !float.IsNaN(Amount) && !float.IsInfinity(Amount) &&
            IsFinite(HitPosition) && IsFinite(HitNormal) && IsFinite(HitDirection) &&
            !string.IsNullOrWhiteSpace(ToolId) && SwingSequence >= 0;

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
