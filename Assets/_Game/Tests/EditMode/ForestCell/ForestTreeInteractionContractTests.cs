using NUnit.Framework;
using SonsOfTheForest.Core.World;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Forest;
using UnityEditor;
using UnityEngine;

namespace SonsOfTheForest.Tests.ForestCell.EditMode
{
    public sealed class ForestTreeInteractionContractTests
    {
        private const string CellPrefabPath =
            "Assets/_Game/Prefabs/World/ForestCells/PRF_ForestCell_Production_001.prefab";
        private const string InteractivePrefabPath =
            "Assets/_Game/Prefabs/World/ForestCells/PRF_InteractiveTreeLease.prefab";

        [Test]
        public void DeltaStore_StoresOnlyChangedTreesByStableCellAndTreeIdentity()
        {
            var store = new ForestTreeDeltaStore();
            var key = new ForestTreeDeltaKey(
                new ForestCellId("cell.test"),
                new ForestTreeInstanceId("tree.test"));
            var unchanged = new ForestTreeStateDelta(
                ForestTreeLifecycleState.Standing, 0f, Vector3.zero,
                Vector3.zero, Quaternion.identity);
            store.Set(key, in unchanged);
            Assert.That(store.Count, Is.Zero);

            var damaged = new ForestTreeStateDelta(
                ForestTreeLifecycleState.Damaged, 25f, Vector3.right,
                Vector3.zero, Quaternion.identity);
            store.Set(key, in damaged);

            Assert.That(store.Count, Is.EqualTo(1));
            Assert.That(store.TryGet(key, out ForestTreeStateDelta restored), Is.True);
            Assert.That(restored.State, Is.EqualTo(ForestTreeLifecycleState.Damaged));
            Assert.That(restored.AccumulatedDamage, Is.EqualTo(25f));
        }

        [Test]
        public void DeltaStore_DistinguishesSameTreeIdAcrossCells()
        {
            var store = new ForestTreeDeltaStore();
            var firstKey = new ForestTreeDeltaKey(
                new ForestCellId("cell.a"), new ForestTreeInstanceId("tree.shared"));
            var secondKey = new ForestTreeDeltaKey(
                new ForestCellId("cell.b"), new ForestTreeInstanceId("tree.shared"));
            var delta = new ForestTreeStateDelta(
                ForestTreeLifecycleState.Felled, 100f, Vector3.forward,
                Vector3.zero, Quaternion.identity);
            store.Set(firstKey, in delta);
            store.Set(secondKey, in delta);

            Assert.That(store.Count, Is.EqualTo(2));
        }

        [Test]
        public void DamageContract_RejectsNonPositiveOrNonFiniteInput()
        {
            Assert.That(new TreeDamageEvent(10f, Vector3.zero, Vector3.right).IsValid, Is.True);
            Assert.That(new TreeDamageEvent(0f, Vector3.zero, Vector3.right).IsValid, Is.False);
            Assert.That(new TreeDamageEvent(float.NaN, Vector3.zero, Vector3.right).IsValid, Is.False);
        }

        [Test]
        public void ProductionCell_UsesOneCoordinatorAndNoStaticTreePhysics()
        {
            GameObject prefab = Load<GameObject>(CellPrefabPath);
            ForestCellRuntime runtime = prefab.GetComponent<ForestCellRuntime>();
            ForestCellInteractionCoordinator coordinator =
                prefab.GetComponent<ForestCellInteractionCoordinator>();

            Assert.That(coordinator, Is.Not.Null);
            Assert.That(coordinator.TryValidateConfiguration(out string reason), Is.True, reason);
            Assert.That(
                prefab.GetComponents<ForestCellInteractionCoordinator>(),
                Has.Length.EqualTo(1));
            foreach (ForestStaticVisualBinding binding in runtime.StaticBindings)
            {
                Assert.That(binding.VisualRoot.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                Assert.That(binding.VisualRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(binding.VisualRoot.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            }
        }

        [Test]
        public void InteractiveLeasePrefab_OwnsPhysicsAndDamageReceiver()
        {
            GameObject prefab = Load<GameObject>(InteractivePrefabPath);
            Assert.That(prefab.GetComponent<ForestInteractiveTree>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Rigidbody>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CapsuleCollider>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<ForestInteractiveTree>(), Is.InstanceOf<ITreeDamageReceiver>());
        }

        [Test]
        public void StaticTreesAndInteractiveTreesHaveNoPerTreeUpdateMethod()
        {
            Assert.That(typeof(ForestInteractiveTree).GetMethod(
                "Update",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic), Is.Null);
        }

        private static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, "Missing asset: " + path);
            return asset;
        }
    }
}
