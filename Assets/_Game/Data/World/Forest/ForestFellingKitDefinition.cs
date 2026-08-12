using System;
using System.Collections.Generic;
using UnityEngine;

namespace SonsOfTheForest.Data.Forest
{
    [CreateAssetMenu(fileName = "KIT_ForestTree", menuName = "Sons Of The Forest/Forest/Felling Kit")]
    public sealed class ForestFellingKitDefinition : ScriptableObject
    {
        [SerializeField] private string speciesId = string.Empty;
        [SerializeField] private string variantId = string.Empty;
        [SerializeField] private GameObject fellingVisualPrefab;
        [SerializeField] private GameObject[] wholeLogPrefabs = Array.Empty<GameObject>();
        [SerializeField, Min(0.1f)] private float cutHeight = 1f;
        [SerializeField, Min(0.05f)] private float trunkRadius = 0.35f;
        [SerializeField, Range(3, 5)] private int logYield = 4;
        [SerializeField, Min(0.5f)] private float usableTrunkLength = 10f;

        public string SpeciesId => speciesId;
        public string VariantId => variantId;
        public GameObject FellingVisualPrefab => fellingVisualPrefab;
        public IReadOnlyList<GameObject> WholeLogPrefabs => wholeLogPrefabs;
        public float CutHeight => cutHeight;
        public float TrunkRadius => trunkRadius;
        public int LogYield => logYield;
        public float UsableTrunkLength => usableTrunkLength;

        public bool TryValidate(out string reason)
        {
            if (string.IsNullOrWhiteSpace(speciesId) || string.IsNullOrWhiteSpace(variantId) ||
                fellingVisualPrefab == null || wholeLogPrefabs == null || wholeLogPrefabs.Length < 2 ||
                cutHeight <= 0f || trunkRadius <= 0f || logYield < 3 || logYield > 5 ||
                usableTrunkLength <= 0f)
            {
                reason = "Felling kit identity, geometry or yield is invalid.";
                return false;
            }

            for (int index = 0; index < wholeLogPrefabs.Length; index++)
            {
                if (wholeLogPrefabs[index] == null)
                {
                    reason = "Felling kit contains a missing whole-log prefab.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            string stableSpeciesId, string stableVariantId, GameObject visual,
            GameObject[] logs, float height, float radius, int yield, float trunkLength)
        {
            speciesId = stableSpeciesId ?? string.Empty;
            variantId = stableVariantId ?? string.Empty;
            fellingVisualPrefab = visual;
            wholeLogPrefabs = logs ?? Array.Empty<GameObject>();
            cutHeight = height;
            trunkRadius = radius;
            logYield = yield;
            usableTrunkLength = trunkLength;
        }
#endif
    }
}
