using System;
using SonsOfTheForest.Core;
using SonsOfTheForest.Core.World;
using UnityEngine;

namespace SonsOfTheForest.Data.Forest
{
    [Serializable]
    public struct ForestTreePlacementRecord
    {
        [SerializeField]
        private string treeInstanceId;

        [SerializeField]
        private string speciesId;

        [SerializeField]
        private string variantId;

        [SerializeField]
        private Vector3 localPosition;

        [SerializeField]
        private float yawDegrees;

        [SerializeField]
        private float uniformScale;

        public ForestTreePlacementRecord(
            string treeInstanceId,
            string speciesId,
            string variantId,
            Vector3 localPosition,
            float yawDegrees,
            float uniformScale)
        {
            this.treeInstanceId = treeInstanceId ?? string.Empty;
            this.speciesId = speciesId ?? string.Empty;
            this.variantId = variantId ?? string.Empty;
            this.localPosition = localPosition;
            this.yawDegrees = yawDegrees;
            this.uniformScale = uniformScale;
        }

        public ForestTreeInstanceId TreeInstanceId => new(treeInstanceId);

        public StableStringId SpeciesId => new(speciesId);

        public StableStringId VariantId => new(variantId);

        public Vector3 LocalPosition => localPosition;

        public float YawDegrees => yawDegrees;

        public float UniformScale => uniformScale;

        public bool TryValidate(out string reason)
        {
            if (!TreeInstanceId.IsValid)
            {
                reason = "TreeInstanceId is missing or contains unsupported characters.";
                return false;
            }

            if (!SpeciesId.IsValid || !VariantId.IsValid)
            {
                reason = "SpeciesId and VariantId are required.";
                return false;
            }

            if (!IsFinite(localPosition.x) || !IsFinite(localPosition.y) ||
                !IsFinite(localPosition.z) || !IsFinite(yawDegrees) ||
                !IsFinite(uniformScale) || uniformScale <= 0f)
            {
                reason = "Placement transform contains a non-finite or non-positive value.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
