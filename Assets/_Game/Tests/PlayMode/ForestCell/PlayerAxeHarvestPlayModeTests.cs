using System.Collections;
using System.Linq;
using NUnit.Framework;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Forest;
using SonsOfTheForest.Presentation.ForestCamp;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SonsOfTheForest.Tests.ForestCell.PlayMode
{
    public sealed class PlayerAxeHarvestPlayModeTests
    {
        [UnityTest]
        public IEnumerator AxeSwing_UsesSingleDelayedContactMarkerAndMissRecovery()
        {
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab");
            Assert.That(prefab, Is.Not.Null);
            GameObject player = Object.Instantiate(prefab);
            PlayerAxeHarvestController axe = player.GetComponent<PlayerAxeHarvestController>();
            Assert.That(axe, Is.Not.Null);
            yield return null;

            axe.BeginEquip();
            float equipDeadline = Time.time + 0.6f;
            while (axe.MotionState != AxeActionState.Idle &&
                   Time.time < equipDeadline)
            {
                yield return null;
            }

            Assert.That(axe.MotionState, Is.EqualTo(AxeActionState.Idle));
            Assert.That(axe.TryBeginSwing(), Is.True);
            Assert.That(axe.IsHitWindowOpen, Is.False, "Click must not cause immediate contact.");
            Assert.That(axe.ContactDispatchCount, Is.Zero);

            float markerDeadline = Time.time + 0.9f;
            while (axe.ContactDispatchCount == 0 && Time.time < markerDeadline)
            {
                yield return null;
            }

            Assert.That(axe.ContactDispatchCount, Is.EqualTo(1));
            Assert.That(axe.LastContactAccepted, Is.False);
            float missDeadline = Time.time + 1.4f;
            while (axe.MotionState != AxeActionState.MissRecovery &&
                   Time.time < missDeadline)
            {
                yield return null;
            }

            Assert.That(axe.MotionState,
                Is.EqualTo(AxeActionState.MissRecovery));
            Assert.That(axe.TryBeginSwing(), Is.False, "Recovery prevents swing stacking.");
            Object.Destroy(player);
            yield return null;
#else
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator AxeBladeWindow_DealsDamageOnlyThroughPhysicalTreeContact()
        {
#if UNITY_EDITOR
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab");
            GameObject cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/World/ForestCells/PRF_ForestCell_Production_001.prefab");
            Assert.That(playerPrefab, Is.Not.Null);
            Assert.That(cellPrefab, Is.Not.Null);

            GameObject player = Object.Instantiate(playerPrefab);
            GameObject cell = Object.Instantiate(cellPrefab);
            yield return null;

            ForestCellRuntime runtime = cell.GetComponent<ForestCellRuntime>();
            ForestCellInteractionCoordinator coordinator =
                cell.GetComponent<ForestCellInteractionCoordinator>();
            ForestStaticVisualBinding binding = runtime.StaticBindings[0];
            var observer = new GameObject("AxeContactTestObserver");
            observer.transform.position = binding.VisualRoot.position + Vector3.back;
            coordinator.SetObserver(observer.transform);
            coordinator.EvaluateProximity();
            Assert.That(coordinator.TryGetLease(
                binding.TreeInstanceId.Value, out ForestInteractiveTree tree), Is.True);
            foreach (ForestInteractiveTree other in
                     cell.GetComponentsInChildren<ForestInteractiveTree>(true))
            {
                if (other != tree)
                {
                    other.GetComponent<CapsuleCollider>().enabled = false;
                }
            }

            PlayerAxeHarvestController axe = player.GetComponent<PlayerAxeHarvestController>();
            player.transform.position = tree.transform.position + Vector3.back * 1.4f;
            player.transform.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            axe.BeginEquip();
            float equipDeadline = Time.time + 0.7f;
            while (axe.MotionState != AxeActionState.Idle &&
                   Time.time < equipDeadline)
            {
                yield return null;
            }

            Assert.That(tree.AccumulatedDamage, Is.Zero);
            Assert.That(axe.TryBeginSwing(), Is.True);
            float approachDeadline = Time.time + 0.75f;
            while (axe.ActionNormalizedTime < 0.45f && Time.time < approachDeadline)
            {
                yield return null;
            }

            Assert.That(axe.ActionNormalizedTime, Is.GreaterThanOrEqualTo(0.45f));
            Transform bladeBase = player.GetComponentsInChildren<Transform>(true)
                .First(value => value.name == "BladeBase");
            Transform bladeTip = player.GetComponentsInChildren<Transform>(true)
                .First(value => value.name == "BladeTip");
            Vector3 bladeMidpoint = (bladeBase.position + bladeTip.position) * 0.5f;
            CapsuleCollider trunkCollider = tree.GetComponent<CapsuleCollider>();
            Assert.That(trunkCollider.enabled, Is.True);
            float contactDeadline = Time.time + 0.45f;
            while (axe.ContactDispatchCount == 0 && Time.time < contactDeadline)
            {
                bladeMidpoint = (bladeBase.position + bladeTip.position) * 0.5f;
                trunkCollider.center = Vector3.Scale(
                    tree.transform.InverseTransformPoint(bladeMidpoint), tree.transform.localScale);
                trunkCollider.height = 1.2f;
                trunkCollider.radius = 0.28f;
                Physics.SyncTransforms();
                Collider[] immediateContacts = Physics.OverlapCapsule(
                    bladeBase.position, bladeTip.position, 0.07f);
                Assert.That(immediateContacts
                    .Any(value => value.GetComponentInParent<ForestInteractiveTree>() == tree), Is.True,
                    $"Test setup must keep the animated blade capsule on the promoted trunk. " +
                    $"blade={bladeBase.position}/{bladeTip.position}, trunk={trunkCollider.bounds}, " +
                    $"contacts={string.Join(",", immediateContacts.Select(value => value.name))}");
                yield return null;
            }

            Assert.That(axe.ContactDispatchCount, Is.EqualTo(1));
            Assert.That(tree.AccumulatedDamage, Is.GreaterThan(0f),
                "A real animated blade overlap should deliver the tree damage event. " +
                $"Result={axe.LastContactResult}, overlaps={axe.LastOverlapCount}, " +
                $"blade={axe.LastContactBladeBase}/{axe.LastContactBladeTip}, " +
                $"trunk={trunkCollider.bounds}");
            Assert.That(tree.State, Is.EqualTo(ForestTreeLifecycleState.Damaged));
            yield return new WaitForSeconds(0.2f);
            Assert.That(axe.ContactDispatchCount, Is.EqualTo(1),
                "One authored swing must dispatch exactly one physical contact marker.");
            Object.Destroy(observer);
            Object.Destroy(cell);
            Object.Destroy(player);
            yield return null;
#else
            yield break;
#endif
        }
    }
}
