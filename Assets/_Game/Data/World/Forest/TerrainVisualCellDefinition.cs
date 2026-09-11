using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SonsOfTheForest.Core;
using UnityEngine;

namespace SonsOfTheForest.Data.Forest
{
    /// <summary>
    /// Project-owned authored contract for one Terrain render tile.
    /// Gameplay ForestCellDefinition assets remain the authority for interactive trees.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TERRAIN_VisualCell",
        menuName = "Sons Of The Forest/World/Terrain Visual Cell")]
    public sealed class TerrainVisualCellDefinition : ScriptableObject
    {
        [SerializeField]
        private string worldCellId = string.Empty;

        [SerializeField]
        [Min(1)]
        private int schemaVersion = 1;

        [SerializeField]
        [Min(1)]
        private int generatorVersion = 1;

        [SerializeField]
        private Vector2Int tileCoordinate;

        [SerializeField]
        private Vector3 terrainSize = new(256f, 64f, 256f);

        [SerializeField]
        [Min(33)]
        private int heightmapResolution = 257;

        [SerializeField]
        private int seed = 26001;

        [SerializeField]
        private string heightmapChecksum = string.Empty;

        [SerializeField]
        private string featureChecksum = string.Empty;

        [SerializeField]
        private bool valuesAreProvisional = true;

        [SerializeField]
        private TerrainData terrainData;

        public StableStringId WorldCellId => new(worldCellId);

        public int SchemaVersion => schemaVersion;

        public int GeneratorVersion => generatorVersion;

        public Vector2Int TileCoordinate => tileCoordinate;

        public Vector3 TerrainSize => terrainSize;

        public int HeightmapResolution => heightmapResolution;

        public int Seed => seed;

        public string HeightmapChecksum => heightmapChecksum;

        public string FeatureChecksum => featureChecksum;

        public bool ValuesAreProvisional => valuesAreProvisional;

        public TerrainData TerrainData => terrainData;

        public Vector3 WorldOrigin => new(
            tileCoordinate.x * terrainSize.x - terrainSize.x * 0.5f,
            0f,
            tileCoordinate.y * terrainSize.z - terrainSize.z * 0.5f);

        public Bounds LocalBounds => new(
            new Vector3(terrainSize.x * 0.5f, terrainSize.y * 0.5f, terrainSize.z * 0.5f),
            terrainSize);

        public bool TryValidate(out string reason)
        {
            if (!WorldCellId.IsValid)
            {
                reason = "WorldCellId is required and must use a stable identifier.";
                return false;
            }

            if (schemaVersion <= 0 || generatorVersion <= 0)
            {
                reason = "SchemaVersion and GeneratorVersion must be positive.";
                return false;
            }

            if (!IsFinite(terrainSize.x) || !IsFinite(terrainSize.y) ||
                !IsFinite(terrainSize.z) || terrainSize.x <= 0f ||
                terrainSize.y <= 0f || terrainSize.z <= 0f)
            {
                reason = "Terrain size must be finite and positive.";
                return false;
            }

            if (heightmapResolution < 33 || !IsSupportedHeightmapResolution(heightmapResolution))
            {
                reason = "Heightmap resolution is not a supported Unity Terrain resolution.";
                return false;
            }

            if (terrainData == null)
            {
                reason = "TerrainData is required.";
                return false;
            }

            if (terrainData.heightmapResolution != heightmapResolution ||
                !Approximately(terrainData.size, terrainSize))
            {
                reason = "TerrainData settings do not match the authored definition.";
                return false;
            }

            if (!IsSha256(heightmapChecksum) || !IsSha256(featureChecksum))
            {
                reason = "Heightmap and feature SHA-256 checksums are required.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public static string ComputeFeatureChecksum(
            string stableId,
            int version,
            Vector2Int coordinate,
            Vector3 size,
            int resolution,
            int terrainSeed,
            string featureLayout)
        {
            string source = string.Join(
                "|",
                stableId ?? string.Empty,
                version.ToString(CultureInfo.InvariantCulture),
                coordinate.x.ToString(CultureInfo.InvariantCulture),
                coordinate.y.ToString(CultureInfo.InvariantCulture),
                size.x.ToString("R", CultureInfo.InvariantCulture),
                size.y.ToString("R", CultureInfo.InvariantCulture),
                size.z.ToString("R", CultureInfo.InvariantCulture),
                resolution.ToString(CultureInfo.InvariantCulture),
                terrainSeed.ToString(CultureInfo.InvariantCulture),
                featureLayout ?? string.Empty);
            using SHA256 sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
            var result = new StringBuilder(bytes.Length * 2);
            for (int index = 0; index < bytes.Length; index++)
            {
                result.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            string stableId,
            int newSchemaVersion,
            int newGeneratorVersion,
            Vector2Int coordinate,
            Vector3 size,
            int resolution,
            int terrainSeed,
            string terrainChecksum,
            string layoutChecksum,
            TerrainData configuredTerrainData,
            bool provisional)
        {
            worldCellId = stableId ?? string.Empty;
            schemaVersion = newSchemaVersion;
            generatorVersion = newGeneratorVersion;
            tileCoordinate = coordinate;
            terrainSize = size;
            heightmapResolution = resolution;
            seed = terrainSeed;
            heightmapChecksum = terrainChecksum ?? string.Empty;
            featureChecksum = layoutChecksum ?? string.Empty;
            terrainData = configuredTerrainData;
            valuesAreProvisional = provisional;
        }
#endif

        private static bool IsSupportedHeightmapResolution(int resolution) =>
            resolution == 33 || resolution == 65 || resolution == 129 ||
            resolution == 257 || resolution == 513 || resolution == 1025 ||
            resolution == 2049 || resolution == 4097;

        private static bool Approximately(Vector3 left, Vector3 right) =>
            Mathf.Abs(left.x - right.x) <= 0.001f &&
            Mathf.Abs(left.y - right.y) <= 0.001f &&
            Mathf.Abs(left.z - right.z) <= 0.001f;

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64)
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool hex = character >= '0' && character <= '9' ||
                           character >= 'a' && character <= 'f' ||
                           character >= 'A' && character <= 'F';
                if (!hex)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
