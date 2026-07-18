using System.Linq;
using NUnit.Framework;
using SonsOfTheForest.Application.ForestCamp;
using SonsOfTheForest.Presentation.ForestCamp;
using UnityEditor;
using UnityEngine;

namespace SonsOfTheForest.Tests.ForestCamp.EditMode
{
    public sealed class ForestCampAssetTests
    {
        private const string WorldPath =
            "Assets/_Game/Prefabs/World/ForestCamp/PRF_ForestCampPlayground.prefab";
        private const string PlayerPath =
            "Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab";
        private const string CampfirePath =
            "Assets/_Game/Prefabs/Building/Campfire/PRF_CampfireConstructionSite.prefab";

        [Test]
        public void WorldPrefab_HasSolidGroundAndDenseModelBackedForest()
        {
            GameObject world = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPath);

            Assert.That(world, Is.Not.Null);
            Transform ground = world.transform.Find("ForestGround");
            Assert.That(ground, Is.Not.Null);
            Assert.That(ground.GetComponent<Collider>().isTrigger, Is.False);
            Assert.That(
                world.GetComponentsInChildren<MeshRenderer>(true).Length,
                Is.GreaterThan(35));
            Assert.That(
                world.GetComponentsInChildren<MeshFilter>(true)
                    .Count(filter => filter.sharedMesh != null &&
                                     filter.sharedMesh.vertexCount > 1000),
                Is.GreaterThanOrEqualTo(20));
        }

        [Test]
        public void WorldRenderers_UseWorkingHdrpMaterials()
        {
            GameObject world = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPath);
            Material[] materials = world.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Distinct()
                .ToArray();

            Assert.That(materials, Is.Not.Empty);
            Assert.That(materials, Has.None.Null);
            Assert.That(materials.All(material =>
                material.shader != null &&
                material.shader.name.StartsWith("HDRP/")), Is.True);
        }

        [Test]
        public void PlayerPrefab_ContainsInventoryInteractionAndHud()
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);

            Assert.That(player.GetComponent<PlayerInventory>(), Is.Not.Null);
            Assert.That(
                player.GetComponent<PlayerInteractionController>(),
                Is.Not.Null);
            Assert.That(
                player.GetComponentInChildren<InteractionPromptView>(true),
                Is.Not.Null);
        }

        [Test]
        public void CampfirePrefab_ContainsConstructionAndAnimatedFireModels()
        {
            GameObject campfire = AssetDatabase.LoadAssetAtPath<GameObject>(CampfirePath);

            Assert.That(
                campfire.GetComponent<CampfireConstructionSite>(),
                Is.Not.Null);
            Assert.That(
                campfire.GetComponentInChildren<CampfireFlameFlicker>(true),
                Is.Not.Null);
            Assert.That(
                campfire.GetComponentInChildren<Light>(true),
                Is.Not.Null);
            Assert.That(
                campfire.GetComponentsInChildren<MeshRenderer>(true).Length,
                Is.GreaterThan(12));
        }
    }
}
