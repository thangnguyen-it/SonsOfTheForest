using System;
using System.Collections.Generic;
using SonsOfTheForest.Core.World;
using UnityEngine;

namespace SonsOfTheForest.Data.Forest
{
    [CreateAssetMenu(fileName = "CELL_Forest", menuName = "Sons Of The Forest/Forest/Cell Definition")]
    public sealed class ForestCellDefinition : ScriptableObject
    {
        [SerializeField]
        private string forestCellId = string.Empty;

        [SerializeField]
        [Min(1)]
        private int schemaVersion = 1;

        [SerializeField]
        [Min(1)]
        private int generatorVersion = 1;

        [SerializeField]
        private Vector3 boundsCenter;

        [SerializeField]
        private Vector3 boundsSize = new(80f, 30f, 80f);

        [SerializeField]
        private int seed;

        [SerializeField]
        [Min(0f)]
        private float densityPerSquareMeter;

        [SerializeField]
        private string placementChecksum = string.Empty;

        [SerializeField]
        private ForestTreePlacementRecord[] placements = Array.Empty<ForestTreePlacementRecord>();

        public ForestCellId ForestCellId => new(forestCellId);

        public int SchemaVersion => schemaVersion;

        public int GeneratorVersion => generatorVersion;

        public Bounds Bounds => new(boundsCenter, boundsSize);

        public int Seed => seed;

        public float DensityPerSquareMeter => densityPerSquareMeter;

        public string PlacementChecksum => placementChecksum;

        public IReadOnlyList<ForestTreePlacementRecord> Placements => placements;

        public int PlacementCount => placements?.Length ?? 0;

        public bool TryValidate(out string reason)
        {
            if (!ForestCellId.IsValid)
            {
                reason = "ForestCellId is missing or invalid.";
                return false;
            }

            if (schemaVersion <= 0 || generatorVersion <= 0)
            {
                reason = "SchemaVersion and GeneratorVersion must be positive.";
                return false;
            }

            if (!IsFinite(boundsSize.x) || !IsFinite(boundsSize.y) || !IsFinite(boundsSize.z) ||
                boundsSize.x <= 0f || boundsSize.y <= 0f || boundsSize.z <= 0f ||
                !IsFinite(densityPerSquareMeter) || densityPerSquareMeter < 0f)
            {
                reason = "Bounds and density must be finite and non-negative.";
                return false;
            }

            if (placements == null || !ForestPlacementChecksum.IsSha256(placementChecksum))
            {
                reason = "Baked placements and a SHA-256 checksum are required.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            Bounds cellBounds = Bounds;
            for (int index = 0; index < placements.Length; index++)
            {
                ForestTreePlacementRecord placement = placements[index];
                if (!placement.TryValidate(out reason))
                {
                    return false;
                }

                if (!ids.Add(placement.TreeInstanceId.Value))
                {
                    reason = "Duplicate TreeInstanceId: " + placement.TreeInstanceId.Value;
                    return false;
                }

                Vector3 position = placement.LocalPosition;
                if (position.x < cellBounds.min.x || position.x > cellBounds.max.x ||
                    position.z < cellBounds.min.z || position.z > cellBounds.max.z)
                {
                    reason = "Placement lies outside the configured cell bounds: " +
                             placement.TreeInstanceId.Value;
                    return false;
                }
            }

            string computed = ForestPlacementChecksum.Compute(
                ForestCellId,
                schemaVersion,
                generatorVersion,
                cellBounds,
                seed,
                densityPerSquareMeter,
                placements);
            if (!string.Equals(computed, placementChecksum, StringComparison.Ordinal))
            {
                reason = "Placement checksum does not match the baked records.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void EditorApplyBake(
            string stableCellId,
            int newSchemaVersion,
            int newGeneratorVersion,
            Bounds newBounds,
            int newSeed,
            float newDensityPerSquareMeter,
            IReadOnlyList<ForestTreePlacementRecord> bakedPlacements)
        {
            if (bakedPlacements == null)
            {
                throw new ArgumentNullException(nameof(bakedPlacements));
            }

            forestCellId = stableCellId ?? string.Empty;
            schemaVersion = newSchemaVersion;
            generatorVersion = newGeneratorVersion;
            boundsCenter = newBounds.center;
            boundsSize = newBounds.size;
            seed = newSeed;
            densityPerSquareMeter = newDensityPerSquareMeter;
            placements = new ForestTreePlacementRecord[bakedPlacements.Count];
            for (int index = 0; index < bakedPlacements.Count; index++)
            {
                placements[index] = bakedPlacements[index];
            }

            placementChecksum = ForestPlacementChecksum.Compute(
                ForestCellId,
                schemaVersion,
                generatorVersion,
                Bounds,
                seed,
                densityPerSquareMeter,
                placements);
        }
#endif

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
