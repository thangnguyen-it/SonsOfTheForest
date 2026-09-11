using NUnit.Framework;
using SonsOfTheForest.Data.Forest;
using UnityEngine;

namespace SonsOfTheForest.Tests.ForestCell.EditMode
{
    public sealed class TerrainVisualCellContractTests
    {
        [Test]
        public void Definition_Uses256MeterTileAndSupportedResolution()
        {
            TerrainData data = CreateTerrainData();
            TerrainVisualCellDefinition definition = CreateDefinition(data, "world.visual.test.000.000");

            Assert.That(definition.TerrainSize, Is.EqualTo(new Vector3(256f, 64f, 256f)));
            Assert.That(definition.HeightmapResolution, Is.EqualTo(257));
            Assert.That(definition.TryValidate(out string reason), Is.True, reason);

            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Definition_RejectsMissingStableId()
        {
            TerrainData data = CreateTerrainData();
            TerrainVisualCellDefinition definition = CreateDefinition(data, string.Empty);

            Assert.That(definition.TryValidate(out string reason), Is.False);
            Assert.That(reason, Does.Contain("WorldCellId"));

            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void WorldOrigin_IsDerivedFromTileCoordinateWithoutRuntimeRandomness()
        {
            TerrainData data = CreateTerrainData();
            TerrainVisualCellDefinition definition = CreateDefinition(
                data,
                "world.visual.test.002.003",
                new Vector2Int(2, 3));

            Assert.That(definition.WorldOrigin, Is.EqualTo(new Vector3(384f, 0f, 640f)));
            Assert.That(
                definition.WorldCellId.Value,
                Is.EqualTo("world.visual.test.002.003"));

            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void FeatureChecksum_IsDeterministicForSameInput()
        {
            string first = TerrainVisualCellDefinition.ComputeFeatureChecksum(
                "world.visual.test.000.000",
                1,
                Vector2Int.zero,
                new Vector3(256f, 64f, 256f),
                257,
                26001,
                "river:z64");
            string second = TerrainVisualCellDefinition.ComputeFeatureChecksum(
                "world.visual.test.000.000",
                1,
                Vector2Int.zero,
                new Vector3(256f, 64f, 256f),
                257,
                26001,
                "river:z64");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Has.Length.EqualTo(64));
        }

        private static TerrainData CreateTerrainData()
        {
            var data = new TerrainData
            {
                heightmapResolution = 257,
                size = new Vector3(256f, 64f, 256f)
            };
            return data;
        }

        private static TerrainVisualCellDefinition CreateDefinition(
            TerrainData data,
            string id,
            Vector2Int coordinate = default)
        {
            TerrainVisualCellDefinition definition =
                ScriptableObject.CreateInstance<TerrainVisualCellDefinition>();
            string checksum = new string('a', 64);
            definition.EditorConfigure(
                id,
                1,
                1,
                coordinate,
                new Vector3(256f, 64f, 256f),
                257,
                26001,
                checksum,
                checksum,
                data,
                true);
            return definition;
        }
    }
}
