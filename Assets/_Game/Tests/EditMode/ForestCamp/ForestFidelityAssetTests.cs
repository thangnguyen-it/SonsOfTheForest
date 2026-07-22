using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SonsOfTheForest.Tests.ForestCamp.EditMode
{
    public sealed class ForestFidelityAssetTests
    {
        private const string SourcePrefabRoot = "Assets/_Game/Prefabs/World/ForestFidelity/";
        private const string LodPrefabRoot = "Assets/_Game/Prefabs/World/ForestLod/";
        private const string WorldPath =
            "Assets/_Game/Prefabs/World/ForestCamp/PRF_ForestCampPlayground.prefab";

        private static readonly float[] ExpectedThresholds = { 0.99f, 0.35f, 0.10f, 0.005f };
        private static readonly long[] MaxTriangles = { 1_250_000L, 12_000L, 3_000L, 220L };

        [TestCase("A")]
        [TestCase("B")]
        [TestCase("C")]
        public void SourceConiferPrefab_PreservesVolumetricNeedlesInsteadOfCards(string suffix)
        {
            GameObject tree = Load<GameObject>(
                SourcePrefabRoot + "PRF_Conifer_" + suffix + ".prefab");
            MeshFilter[] filters = tree.GetComponentsInChildren<MeshFilter>(true);
            long triangles = filters.Sum(filter => TriangleCount(filter.sharedMesh));
            long vertices = filters.Sum(filter => (long)filter.sharedMesh.vertexCount);

            Assert.That(filters, Has.Length.EqualTo(2));
            Assert.That(filters.Select(filter => filter.name), Has.None.Contains("Plane"));
            Assert.That(
                filters.Any(filter => filter.name.EndsWith("Needles3D", System.StringComparison.Ordinal)),
                Is.True);
            Assert.That(triangles, Is.InRange(750_000L, 1_250_000L));
            Assert.That(vertices, Is.InRange(500_000L, 850_000L));
            Assert.That(tree.GetComponent<CapsuleCollider>(), Is.Not.Null);
        }

        [TestCase("GrassTuft", "A", 1_500L)]
        [TestCase("GrassTuft", "B", 1_200L)]
        [TestCase("GrassTuft", "C", 1_800L)]
        [TestCase("Fern", "A", 3_000L)]
        [TestCase("Fern", "B", 2_400L)]
        [TestCase("Fern", "C", 4_000L)]
        public void GroundCover_UsesVolumetricMesh(
            string label,
            string suffix,
            long minimumTriangles)
        {
            GameObject prefab = Load<GameObject>(
                SourcePrefabRoot + "PRF_" + label + "_" + suffix + ".prefab");
            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);

            Assert.That(filters, Has.Length.EqualTo(1));
            Assert.That(filters[0].name, Does.Not.Contain("Plane"));
            Assert.That(TriangleCount(filters[0].sharedMesh), Is.GreaterThan(minimumTriangles));
            Assert.That(filters[0].sharedMesh.bounds.size.y, Is.GreaterThan(0.2f));
        }

        [Test]
        public void ForestMaterials_AreOpaqueHdrpAndDoNotUseAlphaCutout()
        {
            GameObject tree = Load<GameObject>(
                SourcePrefabRoot + "PRF_Conifer_A.prefab");
            GameObject grass = Load<GameObject>(
                SourcePrefabRoot + "PRF_GrassTuft_A.prefab");
            GameObject fern = Load<GameObject>(
                SourcePrefabRoot + "PRF_Fern_A.prefab");
            Material[] materials = new[] { tree, grass, fern }
                .SelectMany(value => value.GetComponentsInChildren<Renderer>(true))
                .SelectMany(value => value.sharedMaterials)
                .Distinct()
                .ToArray();

            Assert.That(materials, Is.Not.Empty);
            Assert.That(materials, Has.None.Null);
            Assert.That(materials.All(value => value.shader.name == "HDRP/Lit"), Is.True);
            Assert.That(materials.All(value => value.GetFloat("_SurfaceType") < 0.5f), Is.True);
            Assert.That(materials.All(value => FloatIsOff(value, "_AlphaCutoffEnable")), Is.True);
            Assert.That(materials.All(value => FloatIsOff(value, "_DoubleSidedEnable")), Is.True);
        }

        [TestCase("A")]
        [TestCase("B")]
        [TestCase("C")]
        public void ConiferLodPrefab_KeepsVolumetricLod0AndSolidDistanceLods(string suffix)
        {
            GameObject tree = Load<GameObject>(
                LodPrefabRoot + "PRF_ConiferLod_" + suffix + ".prefab");
            LODGroup lodGroup = tree.GetComponent<LODGroup>();

            Assert.That(lodGroup, Is.Not.Null);
            LOD[] lods = lodGroup.GetLODs();
            Assert.That(lods, Has.Length.EqualTo(4));
            for (int index = 0; index < lods.Length; index++)
            {
                Assert.That(
                    lods[index].screenRelativeTransitionHeight,
                    Is.EqualTo(ExpectedThresholds[index]).Within(0.011f),
                    $"LOD{index} screen threshold");
                long triangles = TriangleCount(lods[index]);
                Assert.That(triangles, Is.GreaterThan(0), $"LOD{index} has no geometry");
                Assert.That(
                    triangles,
                    Is.LessThanOrEqualTo(MaxTriangles[index]),
                    $"LOD{index} triangle budget");
            }

            Assert.That(TriangleCount(lods[0]), Is.GreaterThanOrEqualTo(700_000L));
            Assert.That(
                lods[0].renderers.Any(
                    renderer => renderer.name.EndsWith("Needles3D", System.StringComparison.Ordinal)),
                Is.True);
            Assert.That(lods[0].renderers.Select(renderer => renderer.name), Has.None.Contains("Plane"));
            Assert.That(lods[1].renderers.Select(renderer => renderer.name), Has.None.Contains("Card"));
            Assert.That(lods[2].renderers.Select(renderer => renderer.name), Has.None.Contains("Card"));

            Assert.That(
                lods[0].renderers.All(
                    renderer => renderer.shadowCastingMode == ShadowCastingMode.On),
                Is.True,
                "LOD0 must cast shadows");
            Assert.That(
                lods[3].renderers.All(
                    renderer => renderer.shadowCastingMode == ShadowCastingMode.Off),
                Is.True,
                "LOD3 far proxy must not cast realtime shadows");
            Assert.That(tree.GetComponent<CapsuleCollider>(), Is.Not.Null);
            Assert.That(tree.GetComponentsInChildren<Collider>(true), Has.Length.EqualTo(1));
        }

        [Test]
        public void ConiferLodVariants_ShareInstancedOpaqueMaterials()
        {
            Material[] materials = new[] { "A", "B", "C" }
                .Select(suffix => Load<GameObject>(
                    LodPrefabRoot + "PRF_ConiferLod_" + suffix + ".prefab"))
                .SelectMany(prefab => prefab.GetComponentsInChildren<Renderer>(true))
                .SelectMany(renderer => renderer.sharedMaterials)
                .Distinct()
                .ToArray();

            Assert.That(materials, Is.Not.Empty);
            Assert.That(materials, Has.None.Null);
            Assert.That(materials.Length, Is.LessThanOrEqualTo(3));
            Assert.That(materials.All(material => material.shader.name == "HDRP/Lit"), Is.True);
            Assert.That(materials.All(material => material.enableInstancing), Is.True);
            Assert.That(materials.All(material => material.GetFloat("_SurfaceType") < 0.5f), Is.True);
            Assert.That(materials.All(material => FloatIsOff(material, "_AlphaCutoffEnable")), Is.True);
            Assert.That(materials.All(material => FloatIsOff(material, "_DoubleSidedEnable")), Is.True);
        }

        [Test]
        public void WorldPrefab_UsesLodConifersAndQuietUnderstorey()
        {
            GameObject world = Load<GameObject>(WorldPath);
            Transform forest = world.transform.Find("ForestModels");
            Transform fidelity = forest.Find("ForestFidelityVegetation");
            Transform canopy = fidelity.Find("MatureConiferClusters");
            Transform understorey = fidelity.Find("UnderstoreyPatches");

            Assert.That(forest, Is.Not.Null);
            Assert.That(fidelity, Is.Not.Null);
            Assert.That(canopy, Is.Not.Null);
            Assert.That(understorey, Is.Not.Null);
            Assert.That(canopy.childCount, Is.EqualTo(8));
            Assert.That(
                canopy.Cast<Transform>().Any(value => value.name.StartsWith("Fir_")),
                Is.False);
            Assert.That(
                canopy.Cast<Transform>().All(
                    tree => tree.GetComponentInChildren<LODGroup>(true) != null),
                Is.True);
            Assert.That(
                understorey.Cast<Transform>().Count(value => value.name.StartsWith("GrassPatch_")),
                Is.GreaterThanOrEqualTo(70));
            Assert.That(
                understorey.Cast<Transform>().Count(value => value.name.StartsWith("FernPatch_")),
                Is.GreaterThanOrEqualTo(20));
            Assert.That(
                understorey.GetComponentsInChildren<Renderer>(true)
                    .All(renderer => renderer.shadowCastingMode == ShadowCastingMode.Off),
                Is.True);
            Transform performanceVolume = world.transform.Find("ForestPerformanceVolume");
            Assert.That(performanceVolume, Is.Not.Null);
            Assert.That(performanceVolume.GetComponent("Volume"), Is.Not.Null);
        }

        [TestCase("GrassTuft", "A")]
        [TestCase("GrassTuft", "B")]
        [TestCase("GrassTuft", "C")]
        [TestCase("Fern", "A")]
        [TestCase("Fern", "B")]
        [TestCase("Fern", "C")]
        public void GroundCover_CullsEarlyAndCastsNoShadows(string label, string suffix)
        {
            GameObject prefab = Load<GameObject>(
                SourcePrefabRoot + "PRF_" + label + "_" + suffix + ".prefab");
            LODGroup lodGroup = prefab.GetComponent<LODGroup>();

            Assert.That(lodGroup, Is.Not.Null, "Ground cover needs a cull-only LODGroup");
            LOD[] lods = lodGroup.GetLODs();
            Assert.That(lods.Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(
                lods[lods.Length - 1].screenRelativeTransitionHeight,
                Is.LessThanOrEqualTo(0.04f),
                "Ground cover must cull at small screen sizes");
            Assert.That(
                prefab.GetComponentsInChildren<Renderer>(true)
                    .All(renderer => renderer.shadowCastingMode == ShadowCastingMode.Off),
                Is.True);
        }

        private static bool FloatIsOff(Material material, string property)
        {
            return !material.HasProperty(property) || material.GetFloat(property) < 0.5f;
        }

        private static long TriangleCount(LOD lod)
        {
            return lod.renderers
                .Select(renderer => renderer.GetComponent<MeshFilter>())
                .Where(filter => filter != null && filter.sharedMesh != null)
                .Sum(filter => TriangleCount(filter.sharedMesh));
        }

        private static long TriangleCount(Mesh mesh)
        {
            long indexCount = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                indexCount += (long)mesh.GetIndexCount(subMesh);
            }

            return indexCount / 3L;
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"Missing asset: {path}");
            return asset;
        }
    }
}
