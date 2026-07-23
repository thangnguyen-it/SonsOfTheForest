using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SonsOfTheForest.Tests.ForestCamp.EditMode
{
    public sealed class ForestModelIntakePolicyTests
    {
        private const string CatalogPath =
            "Assets/_Docs/Research/R2_TREE_MODEL_SOURCE_CATALOG.md";
        private const string WorldPath =
            "Assets/_Game/Prefabs/World/ForestCamp/PRF_ForestCampPlayground.prefab";

        private const long PreferredNearHeroLod0Budget = 120_000L;
        private const long HardNearExceptionalLimit = 200_000L;
        private const long RawMantissaMapleTrialTriangles = 2_417_961L;
        private const long RawPolyHavenAdultFirTrialTriangles = 6_982_937L;

        [Test]
        public void SourceCatalog_RecordsStableSixtyFpsAsModelAcceptanceGate()
        {
            string catalog = File.ReadAllText(CatalogPath);

            Assert.That(catalog, Does.Contain("Non-negotiable 60 FPS gate"));
            Assert.That(catalog, Does.Contain("stable minimum of 60 FPS"));
            Assert.That(catalog, Does.Contain("Near hero tree LOD0 | 30k-120k tris"));
            Assert.That(catalog, Does.Contain("Near exceptional tree | up to 200k tris"));
            Assert.That(catalog, Does.Contain("cluster benchmark: at least 10 near, 50 mid, and 300 far/impostor trees"));
        }

        [Test]
        public void SourceCatalog_RejectsRawMantissaMapleAsGameplayPrefab()
        {
            string catalog = File.ReadAllText(CatalogPath);

            Assert.That(catalog, Does.Contain("Unity raw import measurement for Maple 004"));
            Assert.That(catalog, Does.Contain("2,417,961 triangles"));
            Assert.That(catalog, Does.Contain("reject raw Maple 004 as direct gameplay forest content"));
            Assert.That(catalog, Does.Contain("raw extracted Maple files were removed from `Assets/`"));
        }

        [Test]
        public void SourceCatalog_RejectsRawPolyHavenAdultFirAsGameplayPrefab()
        {
            string catalog = File.ReadAllText(CatalogPath);

            Assert.That(catalog, Does.Contain("`fir_tree_01` adult conifer inspection"));
            Assert.That(catalog, Does.Contain("6,982,937 triangles"));
            Assert.That(catalog, Does.Contain("reject raw import as direct gameplay content"));
            Assert.That(catalog, Does.Contain("temporary raw import was removed from `Assets/`"));
        }

        [Test]
        public void RuntimeBudgets_RejectMillionTriangleTreesBeforeGameplayPromotion()
        {
            Assert.That(RawMantissaMapleTrialTriangles, Is.GreaterThan(HardNearExceptionalLimit));
            Assert.That(RawPolyHavenAdultFirTrialTriangles, Is.GreaterThan(HardNearExceptionalLimit));
            Assert.That(HardNearExceptionalLimit, Is.GreaterThan(PreferredNearHeroLod0Budget));
            Assert.That(
                RawMantissaMapleTrialTriangles / HardNearExceptionalLimit,
                Is.GreaterThanOrEqualTo(12L),
                "The rejected raw Maple trial is more than an order of magnitude over the hard near-tree limit.");
            Assert.That(
                RawPolyHavenAdultFirTrialTriangles / HardNearExceptionalLimit,
                Is.GreaterThanOrEqualTo(34L),
                "The rejected raw adult Fir trial is far beyond the hard near-tree limit.");
        }

        [Test]
        public void Assets_DoNotContainRejectedRawAdultFirImport()
        {
            string[] rawFirAssets = AssetDatabase.FindAssets("fir_tree_01")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith("Assets/", System.StringComparison.Ordinal))
                .ToArray();

            Assert.That(
                rawFirAssets,
                Is.Empty,
                "Poly Haven adult fir raw source must stay outside Git/Assets until a curated LOD version exists.");
        }

        [Test]
        public void PlayableWorld_DoesNotReferenceExternalTrialOrRawMarketplacePrefabs()
        {
            GameObject world = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPath);
            Assert.That(world, Is.Not.Null);

            string[] transformNames = world
                .GetComponentsInChildren<Transform>(true)
                .Select(value => value.name)
                .ToArray();

            Assert.That(transformNames, Has.None.Contains("PRF_TRIAL_PolyHavenModelLineup"));
            Assert.That(transformNames.Any(value => value.Contains("ExternalTrials")), Is.False);
            Assert.That(transformNames.Any(value => value.Contains("Mantissa")), Is.False);
            Assert.That(transformNames.Any(value => value.Contains("Fab")), Is.False);
            Assert.That(transformNames.Any(value => value.Contains("Sketchfab")), Is.False);
            Assert.That(transformNames.Any(value => value.Contains("fir_tree_01")), Is.False);
        }
    }
}
