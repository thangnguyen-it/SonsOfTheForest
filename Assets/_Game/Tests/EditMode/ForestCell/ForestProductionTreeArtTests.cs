using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Forest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SonsOfTheForest.Tests.ForestCell.EditMode
{
    public sealed class ForestProductionTreeArtTests
    {
        private const string CuratedRoot =
            "Assets/_Game/Art/World/Forest/Trees/Curated";
        private const string CellPath =
            "Assets/_Game/Data/World/Forest/Cells/CELL_ProductionForest_001.asset";
        private const string CellPrefabPath =
            "Assets/_Game/Prefabs/World/ForestCells/PRF_ForestCell_Production_001.prefab";
        private const string FoundationScenePath =
            "Assets/_Game/Scenes/SCN_Foundation.unity";
        private const string ExpectedIdentityDigest =
            "5d6d68f59f7b6a1ffdc8929f1576629b8fe5419bd5127c53eab5ab10f3486e71";

        private static readonly string[] SpeciesPaths =
        {
            "Assets/_Game/Data/World/Forest/Species/SPC_Pine.asset",
            "Assets/_Game/Data/World/Forest/Species/SPC_Fir.asset",
            "Assets/_Game/Data/World/Forest/Species/SPC_Maple.asset"
        };

        [TestCase(0, "species.pine", 6)]
        [TestCase(1, "species.fir", 2)]
        [TestCase(2, "species.maple", 4)]
        public void ProductionSpecies_UseCuratedProjectOwnedVariants(
            int speciesIndex,
            string expectedSpeciesId,
            int expectedVariantCount)
        {
            ForestSpeciesDefinition species =
                Load<ForestSpeciesDefinition>(SpeciesPaths[speciesIndex]);

            Assert.That(species.SpeciesId.Value, Is.EqualTo(expectedSpeciesId));
            Assert.That(species.VisualVariants.Count, Is.EqualTo(expectedVariantCount));
            Assert.That(species.TryValidate(out string reason), Is.True, reason);
            foreach (ForestVisualVariant variant in species.VisualVariants)
            {
                string path = AssetDatabase.GetAssetPath(variant.VisualPrefab);
                Assert.That(path, Does.StartWith(CuratedRoot + "/Prefabs/"));
                AssertTreeWrapper(variant.VisualPrefab);
            }
        }

        [Test]
        public void CuratedTreeClosure_HasNoRawIgnoredInterimOrLocalTrialDependency()
        {
            foreach (string speciesPath in SpeciesPaths)
            {
                ForestSpeciesDefinition species = Load<ForestSpeciesDefinition>(speciesPath);
                foreach (ForestVisualVariant variant in species.VisualVariants)
                {
                    string prefabPath = AssetDatabase.GetAssetPath(variant.VisualPrefab);
                    string[] dependencies = AssetDatabase.GetDependencies(prefabPath, true);
                    foreach (string dependency in dependencies)
                    {
                        if (dependency.StartsWith("Assets/", StringComparison.Ordinal))
                        {
                            Assert.That(dependency, Does.StartWith("Assets/_Game/"), dependency);
                        }
                        Assert.That(dependency, Does.Not.Contain("_LocalTrials"), dependency);
                        Assert.That(dependency, Does.Not.Contain("ForestLod"), dependency);
                        Assert.That(dependency, Does.Not.Contain("pine-trees-pack"), dependency);
                        Assert.That(dependency, Does.Not.Contain("realistic-fir-trees-pack"), dependency);
                        Assert.That(dependency, Does.Not.Contain("maple-trees-pack"), dependency);
                    }
                }
            }
        }

        [Test]
        public void ProductionCell_PreservesStableIdentityAndUsesApprovedSpeciesMix()
        {
            ForestCellDefinition cell = Load<ForestCellDefinition>(CellPath);
            Dictionary<string, int> counts = cell.Placements
                .GroupBy(value => value.SpeciesId.Value)
                .ToDictionary(value => value.Key, value => value.Count(), StringComparer.Ordinal);

            Assert.That(cell.ForestCellId.Value, Is.EqualTo("cell.production.forest.001"));
            Assert.That(cell.SchemaVersion, Is.EqualTo(2));
            Assert.That(cell.PlacementCount, Is.EqualTo(38));
            Assert.That(counts["species.pine"], Is.EqualTo(25));
            Assert.That(counts["species.fir"], Is.EqualTo(5));
            Assert.That(counts["species.maple"], Is.EqualTo(8));
            Assert.That(counts.ContainsKey("species.birch"), Is.False);
            Assert.That(IdentityDigest(cell.Placements), Is.EqualTo(ExpectedIdentityDigest));
            Assert.That(cell.Placements.Select(value => value.TreeInstanceId.Value).Distinct().Count(),
                Is.EqualTo(38));
        }

        [Test]
        public void ProductionCell_RespectsTreeSpacingAndPlayableClearings()
        {
            ForestCellDefinition cell = Load<ForestCellDefinition>(CellPath);
            float minimumSpacing = float.MaxValue;
            for (int first = 0; first < cell.PlacementCount; first++)
            {
                Vector2 firstPosition = Horizontal(cell.Placements[first].LocalPosition);
                for (int second = first + 1; second < cell.PlacementCount; second++)
                {
                    minimumSpacing = Mathf.Min(
                        minimumSpacing,
                        Vector2.Distance(
                            firstPosition,
                            Horizontal(cell.Placements[second].LocalPosition)));
                }
            }

            float playerClearance = cell.Placements.Min(value =>
                Horizontal(value.LocalPosition).magnitude);
            float campfireClearance = cell.Placements.Min(value =>
                Vector2.Distance(Horizontal(value.LocalPosition), new Vector2(0f, 5f)));
            ForestTreePlacementRecord corrected = cell.Placements.Single(value =>
                value.TreeInstanceId.Value == "tree.ba23c0bf6f61c9a1747f");

            Assert.That(minimumSpacing, Is.GreaterThanOrEqualTo(7.5f));
            Assert.That(playerClearance, Is.GreaterThanOrEqualTo(6.5f));
            Assert.That(campfireClearance, Is.GreaterThanOrEqualTo(7.5f));
            Assert.That(corrected.LocalPosition, Is.EqualTo(new Vector3(-12f, 0f, 10f)));
        }

        [Test]
        public void FoundationScene_OwnsOneProductionCellAndDisablesInterimMatureTrees()
        {
            Scene scene = SceneManager.GetSceneByPath(FoundationScenePath);
            bool close = !scene.IsValid() || !scene.isLoaded;
            if (close)
            {
                scene = EditorSceneManager.OpenScene(FoundationScenePath, OpenSceneMode.Additive);
            }

            try
            {
                ForestCellRuntime[] cells = ComponentsInScene<ForestCellRuntime>(scene).ToArray();
                Assert.That(cells, Has.Length.EqualTo(1));
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(cells[0].gameObject),
                    Is.EqualTo(CellPrefabPath));
                ForestCellInteractionCoordinator coordinator =
                    cells[0].GetComponent<ForestCellInteractionCoordinator>();
                Transform localPlayer = ComponentsInScene<Transform>(scene)
                    .Single(value => value.name == "LocalPlayer");
                Transform legacy = ComponentsInScene<Transform>(scene)
                    .Single(value => value.name == "MatureConiferClusters");
                BoxCollider ground = ComponentsInScene<BoxCollider>(scene)
                    .Single(value => value.name == "ForestGround");

                Assert.That(coordinator.Observer, Is.SameAs(localPlayer));
                Assert.That(legacy.gameObject.activeSelf, Is.False);
                Assert.That(ComponentsInScene<Transform>(scene).Count(value =>
                    value.name == "PRF_ForestCampPlayground"), Is.EqualTo(1));
                Assert.That(ComponentsInScene<Transform>(scene).Any(value =>
                    value.name == "CampfireConstructionSite"), Is.True);
                Bounds cellBounds = cells[0].Definition.Bounds;
                Assert.That(ground.bounds.min.x, Is.LessThanOrEqualTo(cellBounds.min.x));
                Assert.That(ground.bounds.max.x, Is.GreaterThanOrEqualTo(cellBounds.max.x));
                Assert.That(ground.bounds.min.z, Is.LessThanOrEqualTo(cellBounds.min.z));
                Assert.That(ground.bounds.max.z, Is.GreaterThanOrEqualTo(cellBounds.max.z));
                string[] dependencies = AssetDatabase.GetDependencies(FoundationScenePath, true);
                Assert.That(dependencies, Does.Not.Contain(
                    "Assets/_Game/Data/World/Forest/Species/SPC_InterimConifer.asset"));
            }
            finally
            {
                if (close)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void AssertTreeWrapper(GameObject prefab)
        {
            Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            LODGroup group = prefab.GetComponent<LODGroup>();
            Assert.That(group, Is.Not.Null, prefab.name);
            LOD[] lods = group.GetLODs();
            Assert.That(lods, Has.Length.EqualTo(4), prefab.name);
            Assert.That(lods[3].renderers.All(value =>
                value.shadowCastingMode == ShadowCastingMode.Off), Is.True, prefab.name);

            Material[] materials = prefab.GetComponentsInChildren<Renderer>(true)
                .SelectMany(value => value.sharedMaterials)
                .Distinct()
                .ToArray();
            Assert.That(materials, Is.Not.Empty, prefab.name);
            foreach (Material material in materials)
            {
                Assert.That(material, Is.Not.Null);
                Assert.That(material.shader.name, Is.EqualTo("HDRP/Lit"), material.name);
                Assert.That(material.enableInstancing, Is.True, material.name);
                if (material.name.IndexOf("Bark", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Assert.That(material.GetFloat("_AlphaCutoffEnable"), Is.EqualTo(1f), material.name);
                    Assert.That(material.GetFloat("_DoubleSidedEnable"), Is.EqualTo(1f), material.name);
                }
            }
        }

        private static string IdentityDigest(IReadOnlyList<ForestTreePlacementRecord> placements)
        {
            string value = string.Join("\n", placements
                .Select(item => item.TreeInstanceId.Value)
                .OrderBy(item => item, StringComparer.Ordinal)) + "\n";
            using (SHA256 sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value))
                    .Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static Vector2 Horizontal(Vector3 value) => new(value.x, value.z);

        private static IEnumerable<T> ComponentsInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<T>(true));

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(value, Is.Not.Null, "Missing asset: " + path);
            return value;
        }
    }
}
