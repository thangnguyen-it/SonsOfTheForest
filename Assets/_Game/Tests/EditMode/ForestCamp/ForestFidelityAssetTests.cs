using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SonsOfTheForest.Tests.ForestCamp.EditMode
{
    public sealed class ForestFidelityAssetTests
    {
        private const string ModelRoot =
            "Assets/_Game/Art/Models/World/Independent/ForestGeometry/";
        private const string PrefabRoot =
            "Assets/_Game/Prefabs/World/ForestFidelity/";
        private const string WorldPath =
            "Assets/_Game/Prefabs/World/ForestCamp/PRF_ForestCampPlayground.prefab";

        [TestCase("A", 3_500_000L)]
        [TestCase("B", 4_200_000L)]
        [TestCase("C", 3_100_000L)]
        public void ConiferPrefab_UsesDenseClosedGeometryInsteadOfCards(
            string suffix,
            long minimumTriangles)
        {
            GameObject tree = Load<GameObject>(
                PrefabRoot + "PRF_Conifer_" + suffix + ".prefab");
            MeshFilter[] filters = tree.GetComponentsInChildren<MeshFilter>(true);

            Assert.That(filters, Has.Length.EqualTo(2));
            Assert.That(filters.Select(value => value.name), Has.None.Contains("Plane"));
            Assert.That(
                filters.Sum(value => TriangleCount(value.sharedMesh)),
                Is.GreaterThanOrEqualTo(minimumTriangles));
            Assert.That(
                filters.Sum(value => (long)value.sharedMesh.vertexCount),
                Is.GreaterThan(2_000_000L));
            Assert.That(
                filters.Any(value => value.name.EndsWith("Needles3D", StringComparison.Ordinal)),
                Is.True);
            Assert.That(tree.GetComponent<CapsuleCollider>(), Is.Not.Null);
        }

        [TestCase("GrassTuft", "A", 2_000L)]
        [TestCase("GrassTuft", "B", 1_500L)]
        [TestCase("GrassTuft", "C", 2_300L)]
        [TestCase("Fern", "A", 4_000L)]
        [TestCase("Fern", "B", 3_000L)]
        [TestCase("Fern", "C", 5_000L)]
        public void GroundCover_UsesVolumetricMesh(
            string label,
            string suffix,
            long minimumTriangles)
        {
            GameObject prefab = Load<GameObject>(
                PrefabRoot + "PRF_" + label + "_" + suffix + ".prefab");
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
                PrefabRoot + "PRF_Conifer_A.prefab");
            GameObject grass = Load<GameObject>(
                PrefabRoot + "PRF_GrassTuft_A.prefab");
            GameObject fern = Load<GameObject>(
                PrefabRoot + "PRF_Fern_A.prefab");
            Material[] materials = new[] { tree, grass, fern }
                .SelectMany(value => value.GetComponentsInChildren<Renderer>(true))
                .SelectMany(value => value.sharedMaterials)
                .Distinct()
                .ToArray();

            Assert.That(materials, Is.Not.Empty);
            Assert.That(materials, Has.None.Null);
            Assert.That(
                materials.All(value => value.shader.name == "HDRP/Lit"),
                Is.True);
            Assert.That(
                materials.All(value => value.GetFloat("_SurfaceType") < 0.5f),
                Is.True);
            Assert.That(
                materials.All(value => value.GetFloat("_AlphaCutoffEnable") < 0.5f),
                Is.True);
            Assert.That(
                materials.All(value => value.GetFloat("_DoubleSidedEnable") < 0.5f),
                Is.True);
        }

        [Test]
        public void WorldPrefab_UsesIrregularMatureCanopyAndPatchUnderstorey()
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
                canopy.Cast<Transform>().Select(value => value.localPosition).Distinct().Count(),
                Is.EqualTo(8));
            Assert.That(
                understorey.Cast<Transform>().Count(value => value.name.StartsWith("GrassPatch_")),
                Is.GreaterThanOrEqualTo(70));
            Assert.That(
                understorey.Cast<Transform>().Count(value => value.name.StartsWith("FernPatch_")),
                Is.GreaterThanOrEqualTo(20));
            Assert.That(
                forest.Cast<Transform>().Any(value => value.name.StartsWith("Fir_")),
                Is.False);
        }

        [TestCase("MSH_Conifer_A.fbx")]
        [TestCase("MSH_Conifer_B.fbx")]
        [TestCase("MSH_Conifer_C.fbx")]
        public void ConiferImporter_PreservesFidelityAndAvoidsCpuMeshCopies(string fileName)
        {
            var importer = AssetImporter.GetAtPath(ModelRoot + fileName) as ModelImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.meshCompression, Is.EqualTo(ModelImporterMeshCompression.Off));
            Assert.That(importer.isReadable, Is.False);
            Assert.That(importer.importAnimation, Is.False);
            Assert.That(importer.importCameras, Is.False);
            Assert.That(importer.importLights, Is.False);
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
