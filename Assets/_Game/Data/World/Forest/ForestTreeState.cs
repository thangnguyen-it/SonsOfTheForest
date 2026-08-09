using System;
using SonsOfTheForest.Core.World;
using UnityEngine;

namespace SonsOfTheForest.Data.Forest
{
    public enum ForestTreeLifecycleState
    {
        Standing = 0,
        Damaged = 1,
        Falling = 2,
        Felled = 3
    }

    public readonly struct ForestTreeDeltaKey : IEquatable<ForestTreeDeltaKey>
    {
        public ForestTreeDeltaKey(ForestCellId cellId, ForestTreeInstanceId treeInstanceId)
        {
            CellId = cellId;
            TreeInstanceId = treeInstanceId;
        }

        public ForestCellId CellId { get; }

        public ForestTreeInstanceId TreeInstanceId { get; }

        public bool IsValid => CellId.IsValid && TreeInstanceId.IsValid;

        public bool Equals(ForestTreeDeltaKey other) =>
            CellId.Equals(other.CellId) && TreeInstanceId.Equals(other.TreeInstanceId);

        public override bool Equals(object obj) =>
            obj is ForestTreeDeltaKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (CellId.GetHashCode() * 397) ^ TreeInstanceId.GetHashCode();
            }
        }
    }

    [Serializable]
    public struct ForestTreeStateDelta
    {
        public ForestTreeStateDelta(
            ForestTreeLifecycleState state,
            float accumulatedDamage,
            Vector3 lastHitDirection,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            State = state;
            AccumulatedDamage = accumulatedDamage;
            LastHitDirection = lastHitDirection;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
        }

        public ForestTreeLifecycleState State { get; }

        public float AccumulatedDamage { get; }

        public Vector3 LastHitDirection { get; }

        public Vector3 LocalPosition { get; }

        public Quaternion LocalRotation { get; }

        public bool IsChanged => State != ForestTreeLifecycleState.Standing || AccumulatedDamage > 0f;

        public bool TryValidate(out string reason)
        {
            if (!Enum.IsDefined(typeof(ForestTreeLifecycleState), State) ||
                !IsFinite(AccumulatedDamage) || AccumulatedDamage < 0f ||
                !IsFinite(LastHitDirection) || !IsFinite(LocalPosition) ||
                !IsFinite(LocalRotation.x) || !IsFinite(LocalRotation.y) ||
                !IsFinite(LocalRotation.z) || !IsFinite(LocalRotation.w))
            {
                reason = "Tree delta contains an invalid state or transform.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool IsFinite(Vector3 value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
