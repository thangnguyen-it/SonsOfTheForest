using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SonsOfTheForest.Core.World;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Forest;
using SonsOfTheForest.Presentation.ForestCamp;
using UnityEditor;
using UnityEngine;

namespace SonsOfTheForest.Tests.ForestCell.EditMode
{
    public sealed class ForestHarvestContractTests
    {
        private const string KitRoot = "Assets/_Game/Data/World/Forest/Harvest/Kits";
        private const string LogRoot = "Assets/_Game/Prefabs/World/Forest/Harvest/Logs";
        private const string FellingRoot = "Assets/_Game/Prefabs/World/Forest/Harvest/FellingKits";

        [Test]
        public void ProductionHarvest_HasTwelveStableVariantKitsAcrossThreeSpecies()
        {
            ForestFellingKitDefinition[] kits = AssetDatabase.FindAssets("t:ForestFellingKitDefinition",
                    new[] { KitRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ForestFellingKitDefinition>)
                .ToArray();

            Assert.That(kits, Has.Length.EqualTo(12));
            Assert.That(kits.Select(value => value.VariantId).Distinct().Count(), Is.EqualTo(12));
            Assert.That(kits.Select(value => value.SpeciesId).Distinct(),
                Is.EquivalentTo(new[] { "species.pine", "species.fir", "species.maple" }));
            foreach (ForestFellingKitDefinition kit in kits)
            {
                Assert.That(kit.TryValidate(out string reason), Is.True, reason);
                Assert.That(kit.LogYield, Is.InRange(3, 5));
                Assert.That(kit.WholeLogPrefabs.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void FellingVisuals_HaveRealProgressiveGeometryAndNoRawPackDependency()
        {
            string[] paths = AssetDatabase.FindAssets("t:Prefab", new[] { FellingRoot })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.That(paths, Has.Length.EqualTo(12));
            foreach (string path in paths)
            {
                GameObject prefab = Load<GameObject>(path);
                ForestFellingVisual visual = prefab.GetComponent<ForestFellingVisual>();
                Assert.That(visual, Is.Not.Null, path);
                Assert.That(visual.TryValidate(out string reason), Is.True, reason);
                Assert.That(visual.NotchStageCount, Is.EqualTo(3));
                Assert.That(visual.UpperRoot.GetComponent<LODGroup>(), Is.Not.Null);
                Assert.That(visual.StumpRoot.GetComponent<LODGroup>(), Is.Not.Null);
                Assert.That(prefab.transform.Find("Seam_Intact")?.gameObject.activeSelf, Is.True,
                    path + " must conceal the production split while standing.");
                Assert.That(prefab.GetComponentsInChildren<Transform>(true)
                    .Where(value => value.name.StartsWith("NotchDirection_", StringComparison.Ordinal))
                    .All(value => !value.gameObject.activeSelf), Is.True,
                    path + " must not expose notch geometry before the first hit.");
                AssertRenderDependenciesAreValid(prefab, path);
                foreach (string dependency in AssetDatabase.GetDependencies(path, true))
                {
                    Assert.That(dependency, Does.Not.StartWith("Assets/pine-trees-pack"));
                    Assert.That(dependency, Does.Not.StartWith("Assets/realistic-fir-trees-pack"));
                    Assert.That(dependency, Does.Not.StartWith("Assets/maple-trees-pack"));
                }
            }
        }

        [Test]
        public void WholeLogs_HaveSixSpeciesMatchedPhysicsLodPrefabs()
        {
            string[] paths = AssetDatabase.FindAssets("t:Prefab", new[] { LogRoot })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(value => value).ToArray();
            Assert.That(paths, Has.Length.EqualTo(6));
            Assert.That(paths.Count(value => value.Contains("_Pine_")), Is.EqualTo(2));
            Assert.That(paths.Count(value => value.Contains("_Fir_")), Is.EqualTo(2));
            Assert.That(paths.Count(value => value.Contains("_Maple_")), Is.EqualTo(2));
            foreach (string path in paths)
            {
                GameObject prefab = Load<GameObject>(path);
                Assert.That(prefab.GetComponent<ForestHarvestLog>(), Is.Not.Null);
                Assert.That(prefab.GetComponent<Rigidbody>(), Is.Not.Null);
                Assert.That(prefab.GetComponents<Collider>(), Has.Length.EqualTo(1));
                Assert.That(prefab.GetComponent<LODGroup>()?.lodCount, Is.EqualTo(2));
                Assert.That(prefab.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(value => value.sharedMaterials)
                    .Any(value => value != null && value.name.Contains("EndGrain")), Is.True);
                AssertRenderDependenciesAreValid(prefab, path);
            }
        }

        [Test]
        public void PlayerAndCell_OwnAxeDamageAndPooledPresentationBridges()
        {
            GameObject player = Load<GameObject>(
                "Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab");
            Assert.That(player.GetComponent<PlayerAxeHarvestController>(), Is.Not.Null);
            Assert.That(player.GetComponent<ForestImpactDamageReceiver>(), Is.Not.Null);
            GameObject cell = Load<GameObject>(
                "Assets/_Game/Prefabs/World/ForestCells/PRF_ForestCell_Production_001.prefab");
            ForestHarvestPresentationPool pool = cell.GetComponent<ForestHarvestPresentationPool>();
            Assert.That(pool, Is.Not.Null);
            Assert.That(cell.GetComponentsInChildren<ParticleSystem>(true), Has.Length.EqualTo(7));
            Assert.That(pool.HasFinalChopAudio, Is.False, "Final chop audio is an explicit missing asset.");
            Assert.That(pool.HasFinalImpactAudio, Is.False, "Final impact audio is an explicit missing asset.");
        }

        [Test]
        public void ProjectOwnedAxeViewmodel_HasSynchronizedRigClipsGripAndContactContract()
        {
            GameObject axe = Load<GameObject>(
                "Assets/_Game/Prefabs/Items/Tools/PRF_SurvivalAxe.prefab");
            AxeViewmodelAnimator viewmodel = axe.GetComponent<AxeViewmodelAnimator>();
            Assert.That(viewmodel, Is.Not.Null);
            Assert.That(viewmodel.TryValidate(out string reason), Is.True, reason);
            Animation animation = axe.GetComponentInChildren<Animation>(true);
            Assert.That(animation, Is.Not.Null);
            string[] clips =
            {
                AxeViewmodelAnimator.IdleClip, AxeViewmodelAnimator.EquipClip,
                AxeViewmodelAnimator.UnequipClip, AxeViewmodelAnimator.ChopLeftClip,
                AxeViewmodelAnimator.ChopRightClip, AxeViewmodelAnimator.ChopHeavyClip,
                AxeViewmodelAnimator.RecoveryClip
            };
            Assert.That(clips.All(value => animation.GetClip(value) != null), Is.True,
                "Axe and both arms must be animated by one authored clip set.");
            Assert.That(viewmodel.LeftGrip, Is.Not.Null);
            Assert.That(viewmodel.RightGrip, Is.Not.Null);
            Assert.That(viewmodel.BladeBase.IsChildOf(viewmodel.transform), Is.True);
            Assert.That(viewmodel.BladeTip.IsChildOf(viewmodel.transform), Is.True);
            Assert.That(AssetDatabase.GetDependencies(
                    "Assets/_Game/Prefabs/Items/Tools/PRF_SurvivalAxe.prefab", true),
                Has.None.StartsWith("E:/SOTF_AssetIntake"));
        }

        [Test]
        public void StableLogIdentity_DoesNotDependOnPrefabOrListOrdering()
        {
            var go = new GameObject("LogIdentityTest");
            try
            {
                go.AddComponent<Rigidbody>();
                go.AddComponent<CapsuleCollider>();
                ForestHarvestLog log = go.AddComponent<ForestHarvestLog>();
                log.Configure(new ForestCellId("cell.test"),
                    new ForestTreeInstanceId("tree.stable"), 3, 20f);
                Assert.That(log.StableLogId, Is.EqualTo("tree.stable.log.03"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BlenderPipelineReports_AreStructuredAndValidated()
        {
            string build = Path.GetFullPath("Artifacts/ForestPipeline/build.json");
            string validation = Path.GetFullPath("Artifacts/ForestPipeline/validate.json");
            Assert.That(File.Exists(build), Is.True);
            Assert.That(File.Exists(validation), Is.True);
            string validationJson = File.ReadAllText(validation);
            Assert.That(validationJson, Does.Contain("\"valid\": true"));
            Assert.That(validationJson, Does.Contain("\"variantCount\": 12"));
            Assert.That(validationJson, Does.Contain("\"chipMeshCount\": 2"));
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(value, Is.Not.Null, "Missing asset: " + path);
            return value;
        }

        private static void AssertRenderDependenciesAreValid(GameObject prefab, string path)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, path + " has no renderer.");
            foreach (Renderer renderer in renderers)
            {
                Assert.That(renderer.sharedMaterials, Has.None.Null,
                    path + "/" + renderer.name + " has a missing material.");
                foreach (Material material in renderer.sharedMaterials)
                {
                    Assert.That(material.shader, Is.Not.Null,
                        path + "/" + renderer.name + " has a missing shader.");
                    Assert.That(material.shader.name, Is.Not.EqualTo("Hidden/InternalErrorShader"),
                        path + "/" + renderer.name + " resolved to Unity's error shader.");
                    Assert.That(AssetDatabase.GetAssetPath(material), Is.Not.Empty,
                        path + "/" + renderer.name + " uses a transient material.");
                }
            }
        }
    }
}
