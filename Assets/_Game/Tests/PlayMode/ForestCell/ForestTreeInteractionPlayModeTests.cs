using System.Collections;
using System.Linq;
using NUnit.Framework;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Forest;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SonsOfTheForest.Tests.ForestCell.PlayMode
{
    public sealed class ForestTreeInteractionPlayModeTests
    {
        private const string PrefabPath =
            "Assets/_Game/Prefabs/World/ForestCells/PRF_ForestCell_Production_001.prefab";

        [UnityTest]
        public IEnumerator UnchangedTree_PromotesAndDemotesWithoutChangingIdentity()
        {
#if UNITY_EDITOR
            TestWorld world = CreateWorld();
            yield return null;
            ForestStaticVisualBinding binding = world.Runtime.StaticBindings[0];
            world.Observer.transform.position = binding.VisualRoot.position + Vector3.right * 2f;
            world.Coordinator.EvaluateProximity();
            LogAssert.NoUnexpectedReceived();

            Assert.That(world.Coordinator.TryGetLease(
                binding.TreeInstanceId.Value, out ForestInteractiveTree tree), Is.True);
            Assert.That(tree.TreeInstanceId, Is.EqualTo(binding.TreeInstanceId));
            Assert.That(tree.SpeciesId, Is.EqualTo(binding.SpeciesId));
            Assert.That(tree.VariantId, Is.EqualTo(binding.VariantId));
            Assert.That(tree.transform.localPosition, Is.EqualTo(binding.VisualRoot.localPosition));
            Assert.That(
                Quaternion.Angle(tree.transform.localRotation, binding.VisualRoot.localRotation),
                Is.LessThan(0.01f));
            Assert.That(tree.transform.localScale, Is.EqualTo(binding.VisualRoot.localScale));
            Mesh[] staticMeshes = binding.VisualRoot
                .GetComponentsInChildren<MeshFilter>(true)
                .Select(value => value.sharedMesh)
                .ToArray();
            Mesh[] promotedMeshes = tree.transform.Find("VisualAnchor/InteractiveVisual")
                .GetComponentsInChildren<MeshFilter>(true)
                .Select(value => value.sharedMesh)
                .ToArray();
            Assert.That(promotedMeshes, Is.EqualTo(staticMeshes));
            CapsuleCollider trunkCollider = tree.GetComponent<CapsuleCollider>();
            Assert.That(trunkCollider.radius, Is.InRange(0.32f, 0.68f));
            Assert.That(trunkCollider.height, Is.GreaterThan(8f));
            Assert.That(binding.VisualRoot.gameObject.activeSelf, Is.False);

            world.Observer.transform.position = binding.VisualRoot.position + Vector3.right * 100f;
            world.Coordinator.EvaluateProximity();
            Assert.That(world.Coordinator.TryGetLease(binding.TreeInstanceId.Value, out _), Is.False);
            Assert.That(binding.VisualRoot.gameObject.activeSelf, Is.True);
            Assert.That(world.Coordinator.DeltaStore.Count, Is.Zero);
            DestroyWorld(world);
            yield return null;
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator DamagedTree_CannotDemoteOrResurrectAcrossCellReload()
        {
#if UNITY_EDITOR
            TestWorld world = CreateWorld();
            yield return null;
            ForestStaticVisualBinding binding = world.Runtime.StaticBindings[0];
            world.Observer.transform.position = binding.VisualRoot.position + Vector3.right * 2f;
            world.Coordinator.EvaluateProximity();
            var damage = new TreeDamageEvent(
                25f, binding.VisualRoot.position + Vector3.up, Vector3.forward);

            Assert.That(world.Coordinator.TryDamage(
                binding.TreeInstanceId.Value, in damage, out string reason), Is.True, reason);
            Assert.That(world.Coordinator.TryGetLease(
                binding.TreeInstanceId.Value, out ForestInteractiveTree tree), Is.True);
            Assert.That(tree.State, Is.EqualTo(ForestTreeLifecycleState.Damaged));
            Assert.That(world.Coordinator.DeltaStore.Count, Is.EqualTo(1));

            world.Observer.transform.position = binding.VisualRoot.position + Vector3.right * 100f;
            world.Coordinator.EvaluateProximity();
            Assert.That(world.Coordinator.TryDemote(binding.TreeInstanceId.Value), Is.False);
            Assert.That(binding.VisualRoot.gameObject.activeSelf, Is.False);

            world.Runtime.Unload();
            Assert.That(world.Coordinator.TryGetLease(binding.TreeInstanceId.Value, out _), Is.False);
            Assert.That(world.Runtime.TryLoad(out reason), Is.True, reason);
            Assert.That(world.Coordinator.TryGetLease(
                binding.TreeInstanceId.Value, out tree), Is.True);
            Assert.That(tree.State, Is.EqualTo(ForestTreeLifecycleState.Damaged));
            Assert.That(tree.TreeInstanceId, Is.EqualTo(binding.TreeInstanceId));
            Assert.That(binding.VisualRoot.gameObject.activeSelf, Is.False);
            DestroyWorld(world);
            yield return null;
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator PineFirAndMaple_AllResolveMatchingFellingKitsAndAcceptChops()
        {
#if UNITY_EDITOR
            TestWorld world = CreateWorld();
            yield return null;
            string[] expectedSpecies = { "species.pine", "species.fir", "species.maple" };
            for (int index = 0; index < expectedSpecies.Length; index++)
            {
                ForestStaticVisualBinding binding = world.Runtime.StaticBindings
                    .First(value => value.SpeciesId.Value == expectedSpecies[index]);
                world.Observer.transform.position = binding.VisualRoot.position + Vector3.back * 2f;
                world.Coordinator.EvaluateProximity();
                var damage = new TreeDamageEvent(
                    10f, binding.VisualRoot.position + Vector3.up,
                    Vector3.back, Vector3.forward, "tool.axe.test", index + 1);

                Assert.That(world.Coordinator.TryDamage(
                    binding.TreeInstanceId.Value, in damage, out string reason), Is.True, reason);
                Assert.That(world.Coordinator.TryGetLease(
                    binding.TreeInstanceId.Value, out ForestInteractiveTree tree), Is.True);
                Assert.That(tree.SpeciesId.Value, Is.EqualTo(expectedSpecies[index]));
                Assert.That(tree.State, Is.EqualTo(ForestTreeLifecycleState.Damaged));
                Assert.That(tree.NotchStage, Is.GreaterThan(0));
            }

            DestroyWorld(world);
            yield return null;
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator LethalDamage_TransitionsThroughFallingToFelledAndPersistsInSession()
        {
#if UNITY_EDITOR
            TestWorld world = CreateWorld(withGround: true);
            yield return null;
            ForestStaticVisualBinding binding = world.Runtime.StaticBindings[0];
            world.Observer.transform.position = binding.VisualRoot.position + Vector3.left * 2f;
            world.Coordinator.EvaluateProximity();
            Assert.That(world.Coordinator.TryGetLease(
                binding.TreeInstanceId.Value, out ForestInteractiveTree promoted), Is.True);
            var damage = new TreeDamageEvent(
                promoted.DamageThreshold, binding.VisualRoot.position + Vector3.up * 1.2f,
                Vector3.left, Vector3.right, "tool.axe.test", 1);

            Assert.That(world.Coordinator.TryDamage(
                binding.TreeInstanceId.Value, in damage, out string reason), Is.True, reason);
            Assert.That(world.Coordinator.TryGetLease(
                binding.TreeInstanceId.Value, out ForestInteractiveTree tree), Is.True);
            Assert.That(tree.State, Is.EqualTo(ForestTreeLifecycleState.Falling));

            float deadline = Time.time + 13f;
            while (tree.State == ForestTreeLifecycleState.Falling && Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(tree.State, Is.EqualTo(ForestTreeLifecycleState.Felled));
            Assert.That(tree.OutputsSpawned, Is.True);
            Assert.That(tree.Logs.Count, Is.InRange(3, 5));
            string[] stableLogIds = tree.Logs.Select(value => value.StableLogId).ToArray();
            Assert.That(stableLogIds.Distinct().Count(), Is.EqualTo(stableLogIds.Length));
            Assert.That(binding.VisualRoot.gameObject.activeSelf, Is.False);
            Assert.That(world.Coordinator.TryDemote(binding.TreeInstanceId.Value), Is.False);
            world.Runtime.Unload();
            Assert.That(world.Runtime.TryLoad(out reason), Is.True, reason);
            Assert.That(world.Coordinator.TryGetLease(
                binding.TreeInstanceId.Value, out tree), Is.True);
            Assert.That(tree.State, Is.EqualTo(ForestTreeLifecycleState.Felled));
            Assert.That(tree.TreeInstanceId, Is.EqualTo(binding.TreeInstanceId));
            Assert.That(tree.Logs.Select(value => value.StableLogId), Is.EquivalentTo(stableLogIds));
            Assert.That(tree.Logs.Count, Is.EqualTo(stableLogIds.Length));
            DestroyWorld(world);
            yield return null;
#else
            yield break;
#endif
        }

#if UNITY_EDITOR
        private static TestWorld CreateWorld(bool withGround = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);
            var observer = new GameObject("ForestInteractionTestObserver");
            var coordinator = instance.GetComponent<ForestCellInteractionCoordinator>();
            coordinator.SetObserver(observer.transform);
            GameObject ground = null;
            if (withGround)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "ForestInteractionTestGround";
                ground.transform.position = new Vector3(0f, -0.5f, 0f);
                ground.transform.localScale = new Vector3(200f, 1f, 200f);
            }

            return new TestWorld(
                instance,
                observer,
                ground,
                instance.GetComponent<ForestCellRuntime>(),
                coordinator);
        }

        private static void DestroyWorld(TestWorld world)
        {
            Object.Destroy(world.Instance);
            Object.Destroy(world.Observer);
            if (world.Ground != null)
            {
                Object.Destroy(world.Ground);
            }
        }

        private readonly struct TestWorld
        {
            public TestWorld(
                GameObject instance,
                GameObject observer,
                GameObject ground,
                ForestCellRuntime runtime,
                ForestCellInteractionCoordinator coordinator)
            {
                Instance = instance;
                Observer = observer;
                Ground = ground;
                Runtime = runtime;
                Coordinator = coordinator;
            }

            public GameObject Instance { get; }
            public GameObject Observer { get; }
            public GameObject Ground { get; }
            public ForestCellRuntime Runtime { get; }
            public ForestCellInteractionCoordinator Coordinator { get; }
        }
#endif
    }
}
