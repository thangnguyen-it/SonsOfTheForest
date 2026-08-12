using SonsOfTheForest.Core.World;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Forest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class ForestHarvestLog : MonoBehaviour
    {
        private ForestCellId cellId;
        private ForestTreeInstanceId sourceTreeId;
        private string stableLogId = string.Empty;

        public ForestCellId CellId => cellId;
        public ForestTreeInstanceId SourceTreeId => sourceTreeId;
        public string StableLogId => stableLogId;

        public void Configure(ForestCellId owner, ForestTreeInstanceId source, int index, float mass)
        {
            cellId = owner;
            sourceTreeId = source;
            stableLogId = source.Value + ".log." + index.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
            Rigidbody body = GetComponent<Rigidbody>();
            body.mass = Mathf.Max(0.1f, mass);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = 0.35f;
            body.angularDamping = 0.5f;
            body.sleepThreshold = 0.08f;
        }
    }
}
