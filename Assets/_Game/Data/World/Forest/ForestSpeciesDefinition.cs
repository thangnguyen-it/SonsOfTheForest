using System;
using System.Collections.Generic;
using SonsOfTheForest.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace SonsOfTheForest.Data.Forest
{
    public enum ForestStaticShadowPolicy
    {
        PrefabLodPolicy,
        Disabled
    }

    [Serializable]
    public sealed class ForestVisualVariant
    {
        [SerializeField]
        private string variantId = string.Empty;

        [SerializeField]
        private GameObject visualPrefab;

        [SerializeField]
        [Min(0.001f)]
        private float placementWeight = 1f;

        public StableStringId VariantId => new(variantId);

        public GameObject VisualPrefab => visualPrefab;

        public float PlacementWeight => placementWeight;

#if UNITY_EDITOR
        public ForestVisualVariant(string variantId, GameObject visualPrefab, float placementWeight)
        {
            this.variantId = variantId ?? string.Empty;
            this.visualPrefab = visualPrefab;
            this.placementWeight = placementWeight;
        }
#endif
    }

    [CreateAssetMenu(fileName = "SPC_ForestSpecies", menuName = "Sons Of The Forest/Forest/Species Definition")]
    public sealed class ForestSpeciesDefinition : ScriptableObject
    {
        [SerializeField]
        private string speciesId = string.Empty;

        [SerializeField]
        private ForestVisualVariant[] visualVariants = Array.Empty<ForestVisualVariant>();

        [Header("Static render policy")]
        [SerializeField]
        private bool requireLodGroup = true;

        [SerializeField]
        private ForestStaticShadowPolicy shadowPolicy = ForestStaticShadowPolicy.PrefabLodPolicy;

        [Header("Future interaction extension points")]
        [SerializeField]
        private GameObject colliderPrefab;

        [SerializeField]
        private GameObject stumpPrefab;

        [SerializeField]
        private GameObject logPrefab;

        [SerializeField]
        private ScriptableObject resourceProfile;

        public StableStringId SpeciesId => new(speciesId);

        public IReadOnlyList<ForestVisualVariant> VisualVariants => visualVariants;

        public bool RequireLodGroup => requireLodGroup;

        public ForestStaticShadowPolicy ShadowPolicy => shadowPolicy;

        public GameObject ColliderPrefab => colliderPrefab;

        public GameObject StumpPrefab => stumpPrefab;

        public GameObject LogPrefab => logPrefab;

        public ScriptableObject ResourceProfile => resourceProfile;

        public bool TryGetVariant(string requestedVariantId, out ForestVisualVariant variant)
        {
            for (int index = 0; index < visualVariants.Length; index++)
            {
                ForestVisualVariant candidate = visualVariants[index];
                if (candidate != null &&
                    string.Equals(candidate.VariantId.Value, requestedVariantId, StringComparison.Ordinal))
                {
                    variant = candidate;
                    return true;
                }
            }

            variant = null;
            return false;
        }

        public bool TryValidate(out string reason)
        {
            if (!SpeciesId.IsValid)
            {
                reason = "SpeciesId is required.";
                return false;
            }

            if (visualVariants == null || visualVariants.Length == 0)
            {
                reason = "At least one visual variant is required.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < visualVariants.Length; index++)
            {
                ForestVisualVariant variant = visualVariants[index];
                if (variant == null || !variant.VariantId.IsValid || variant.VisualPrefab == null)
                {
                    reason = "Every visual variant requires a stable ID and prefab.";
                    return false;
                }

                if (!ids.Add(variant.VariantId.Value))
                {
                    reason = "Duplicate visual variant ID: " + variant.VariantId.Value;
                    return false;
                }

                if (variant.PlacementWeight <= 0f || float.IsNaN(variant.PlacementWeight) ||
                    float.IsInfinity(variant.PlacementWeight))
                {
                    reason = "Visual variant weight must be finite and positive.";
                    return false;
                }

                if (requireLodGroup && variant.VisualPrefab.GetComponentInChildren<LODGroup>(true) == null)
                {
                    reason = "Visual variant requires an LODGroup: " + variant.VariantId.Value;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            string stableSpeciesId,
            ForestVisualVariant[] variants,
            bool requiresLod,
            ForestStaticShadowPolicy staticShadowPolicy)
        {
            speciesId = stableSpeciesId ?? string.Empty;
            visualVariants = variants ?? Array.Empty<ForestVisualVariant>();
            requireLodGroup = requiresLod;
            shadowPolicy = staticShadowPolicy;
        }
#endif
    }
}
