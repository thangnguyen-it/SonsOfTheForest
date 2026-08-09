using System;
using System.Linq;
using NUnit.Framework;
using SonsOfTheForest.Core.World;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Editor.ForestCell;
using SonsOfTheForest.Infrastructure.Forest;
using UnityEditor;
using UnityEngine;

namespace SonsOfTheForest.Tests.ForestCell.EditMode
{
    public sealed class ForestCellContractTests
    {
        private const string CellPath =
            "Assets/_Game/Data/World/Forest/Cells/CELL_ProductionForest_001.asset";
        private static readonly string[] SpeciesPaths =
        {
            "Assets/_Game/Data/World/Forest/Species/SPC_Pine.asset",
            "Assets/_Game/Data/World/Forest/Species/SPC_Fir.asset",
            "Assets/_Game/Data/World/Forest/Species/SPC_Maple.asset"
        };
        private const string PrefabPath =
            "Assets/_Game/Prefabs/World/ForestCells/PRF_ForestCell_Production_001.prefab";

        [TestCase("cell.production.forest.001", true)]
        [TestCase("tree.1234abcd", true)]
        [TestCase("", false)]
        [TestCase("Tree With Spaces", false)]
        [TestCase("../escape", false)]
        public void StableIdentity_ValidatesExpectedCharacters(string value, bool expected)
        {
            Assert.That(new ForestCellId(value).IsValid, Is.EqualTo(expected));
            Assert.That(new ForestTreeInstanceId(value).IsValid, Is.EqualTo(expected));
        }

        [Test]
        public void DuplicateTreeInstanceId_IsRejected()
        {
            var cell = ScriptableObject.CreateInstance<ForestCellDefinition>();
            try
            {
                var records = new[]
                {
                    Record("tree.duplicate", Vector3.zero),
                    Record("tree.duplicate", Vector3.right)
                };
                cell.EditorApplyBake(
                    "cell.test.duplicate", 1, 1,
                    new Bounds(Vector3.zero, new Vector3(20f, 10f, 20f)),
                    1, 0.01f, records);

                Assert.That(cell.TryValidate(out string reason), Is.False);
                StringAssert.Contains("Duplicate TreeInstanceId", reason);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cell);
            }
        }

        [Test]
        public void Generator_SameInputProducesSameRecordsAndChecksum()
        {
            ForestPlacementBakeRequest request = Request(seed: 8128, density: 0.012f);
            ForestTreePlacementRecord[] first = ForestPlacementGenerator.Generate(in request);
            ForestTreePlacementRecord[] second = ForestPlacementGenerator.Generate(in request);

            Assert.That(Serialize(first), Is.EqualTo(Serialize(second)));
            Assert.That(Checksum(in request, first), Is.EqualTo(Checksum(in request, second)));
            Assert.That(first.Select(value => value.TreeInstanceId.Value).Distinct().Count(),
                Is.EqualTo(first.Length));
        }

        [Test]
        public void Generator_RelevantInputChangesChecksum()
        {
            ForestPlacementBakeRequest firstRequest = Request(seed: 41, density: 0.01f);
            ForestPlacementBakeRequest secondRequest = Request(seed: 42, density: 0.01f);
            ForestTreePlacementRecord[] first = ForestPlacementGenerator.Generate(in firstRequest);
            ForestTreePlacementRecord[] second = ForestPlacementGenerator.Generate(in secondRequest);

            Assert.That(Checksum(in firstRequest, first),
                Is.Not.EqualTo(Checksum(in secondRequest, second)));
        }

        [Test]
        public void ProductionCell_IsValidAndMatchesBakedVisuals()
        {
            ForestCellDefinition cell = Load<ForestCellDefinition>(CellPath);
            ForestSpeciesDefinition[] species = SpeciesPaths
                .Select(Load<ForestSpeciesDefinition>)
                .ToArray();
            GameObject prefab = Load<GameObject>(PrefabPath);
            ForestCellRuntime runtime = prefab.GetComponent<ForestCellRuntime>();

            Assert.That(cell.TryValidate(out string cellReason), Is.True, cellReason);
            foreach (ForestSpeciesDefinition definition in species)
            {
                Assert.That(
                    definition.TryValidate(out string speciesReason),
                    Is.True,
                    speciesReason);
            }
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.TryValidateConfiguration(out string runtimeReason), Is.True, runtimeReason);
            Assert.That(runtime.StaticBindings.Count, Is.EqualTo(cell.PlacementCount));
            Assert.That(runtime.RenderCounters.StaticTreeCount, Is.EqualTo(cell.PlacementCount));
            Assert.That(runtime.RenderCounters.ColliderCount, Is.Zero);
            Assert.That(runtime.RenderCounters.LodGroupCount, Is.EqualTo(cell.PlacementCount));
            Assert.That(runtime.RenderCounters.Lod0TriangleCount, Is.GreaterThan(0));
        }

        [Test]
        public void ProductionCell_HasNoPerTreeBehaviourOrColliderAndNoLocalTrialDependency()
        {
            GameObject prefab = Load<GameObject>(PrefabPath);
            ForestCellRuntime runtime = prefab.GetComponent<ForestCellRuntime>();

            foreach (ForestStaticVisualBinding binding in runtime.StaticBindings)
            {
                Assert.That(binding.VisualRoot.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
                Assert.That(binding.VisualRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
            }

            string[] dependencies = AssetDatabase.GetDependencies(PrefabPath, true);
            Assert.That(dependencies.Any(path =>
                    path.IndexOf("_LocalTrials", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("pineForset_MayoGames_free", StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False);
        }

        [Test]
        public void Runtime_LoadUnloadIsIdempotentAndDoesNotRegeneratePlacement()
        {
            GameObject instance = UnityEngine.Object.Instantiate(Load<GameObject>(PrefabPath));
            try
            {
                ForestCellRuntime runtime = instance.GetComponent<ForestCellRuntime>();
                string checksum = runtime.Definition.PlacementChecksum;
                int bindingCount = runtime.StaticBindings.Count;

                runtime.Unload();
                runtime.Unload();
                Assert.That(runtime.IsLoaded, Is.False);
                Assert.That(runtime.TryLoad(out string firstReason), Is.True, firstReason);
                Assert.That(runtime.TryLoad(out string secondReason), Is.True, secondReason);
                Assert.That(runtime.IsLoaded, Is.True);
                Assert.That(runtime.Definition.PlacementChecksum, Is.EqualTo(checksum));
                Assert.That(runtime.StaticBindings.Count, Is.EqualTo(bindingCount));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Runtime_MissingSpeciesOrVariantFailsClosed()
        {
            GameObject instance = UnityEngine.Object.Instantiate(Load<GameObject>(PrefabPath));
            try
            {
                ForestCellRuntime runtime = instance.GetComponent<ForestCellRuntime>();
                Transform visualRoot = instance.transform.Find("StaticVisuals");
                runtime.EditorConfigure(
                    runtime.Definition,
                    Array.Empty<ForestSpeciesDefinition>(),
                    visualRoot,
                    runtime.StaticBindings.ToArray(),
                    runtime.RenderCounters,
                    false);

                Assert.That(runtime.TryLoad(out string reason), Is.False);
                StringAssert.Contains("species", reason.ToLowerInvariant());
                Assert.That(visualRoot.gameObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void ForestCellRuntime_HasNoPerFrameUpdateMethod()
        {
            Assert.That(typeof(ForestCellRuntime).GetMethod(
                "Update", System.Reflection.BindingFlags.Instance |
                          System.Reflection.BindingFlags.Public |
                          System.Reflection.BindingFlags.NonPublic), Is.Null);
        }

        private static ForestPlacementBakeRequest Request(int seed, float density)
        {
            return new ForestPlacementBakeRequest(
                new ForestCellId("cell.test.generator"),
                1,
                new Bounds(new Vector3(0f, 5f, 0f), new Vector3(40f, 10f, 40f)),
                seed,
                density,
                new[]
                {
                    new ForestSpeciesBakeInput(
                        "species.test", new[] { "variant.a", "variant.b" },
                        new[] { 1f, 2f }, 1f, 0.8f, 1.2f)
                });
        }

        private static string Checksum(
            in ForestPlacementBakeRequest request,
            ForestTreePlacementRecord[] records)
        {
            return ForestPlacementChecksum.Compute(
                request.CellId, 1, request.GeneratorVersion, request.Bounds,
                request.Seed, request.DensityPerSquareMeter, records);
        }

        private static string Serialize(ForestTreePlacementRecord[] records)
        {
            return string.Join("|", records.Select(value =>
                value.TreeInstanceId.Value + ":" + value.SpeciesId.Value + ":" +
                value.VariantId.Value + ":" + value.LocalPosition + ":" +
                value.YawDegrees + ":" + value.UniformScale));
        }

        private static ForestTreePlacementRecord Record(string id, Vector3 position)
        {
            return new ForestTreePlacementRecord(
                id, "species.test", "variant.test", position, 0f, 1f);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, "Missing asset: " + path);
            return asset;
        }
    }
}
