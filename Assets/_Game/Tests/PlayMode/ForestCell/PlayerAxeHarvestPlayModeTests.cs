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
        public IEnumerator AxeSwing_UsesDelayedContactWindowAndMissRecovery()
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
            while (axe.MotionState != PlayerAxeHarvestController.AxeMotionState.Idle &&
                   Time.time < equipDeadline)
            {
                yield return null;
            }

            Assert.That(axe.MotionState, Is.EqualTo(PlayerAxeHarvestController.AxeMotionState.Idle));
            Assert.That(axe.TryBeginSwing(), Is.True);
            Assert.That(axe.IsHitWindowOpen, Is.False, "Click must not cause immediate contact.");

            float windowDeadline = Time.time + 0.45f;
            while (!axe.IsHitWindowOpen && Time.time < windowDeadline)
            {
                yield return null;
            }

            Assert.That(axe.IsHitWindowOpen, Is.True);
            float missDeadline = Time.time + 1.0f;
            while (axe.MotionState != PlayerAxeHarvestController.AxeMotionState.MissRecovery &&
                   Time.time < missDeadline)
            {
                yield return null;
            }

            Assert.That(axe.MotionState,
                Is.EqualTo(PlayerAxeHarvestController.AxeMotionState.MissRecovery));
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
            axe.BeginEquip();
            float equipDeadline = Time.time + 0.7f;
            while (axe.MotionState != PlayerAxeHarvestController.AxeMotionState.Idle &&
                   Time.time < equipDeadline)
            {
                yield return null;
            }

            Assert.That(tree.AccumulatedDamage, Is.Zero);
            Assert.That(axe.TryBeginSwing(), Is.True);
            float windowDeadline = Time.time + 0.6f;
            while (!axe.IsHitWindowOpen && Time.time < windowDeadline)
            {
                yield return null;
            }

            Assert.That(axe.IsHitWindowOpen, Is.True);
            Transform bladeBase = player.GetComponentsInChildren<Transform>(true)
                .First(value => value.name == "BladeBase");
            Transform bladeTip = player.GetComponentsInChildren<Transform>(true)
                .First(value => value.name == "BladeTip");
            Vector3 bladeMidpoint = (bladeBase.position + bladeTip.position) * 0.5f;
            CapsuleCollider trunkCollider = tree.GetComponent<CapsuleCollider>();
            Assert.That(trunkCollider.enabled, Is.True);
            Vector3 trunkContact = trunkCollider.bounds.center;
            Transform equippedAxe = player.GetComponentsInChildren<Transform>(true)
                .First(value => value.name == "EquippedSurvivalAxe");
            equippedAxe.parent.position += trunkContact - bladeMidpoint;
            Physics.SyncTransforms();
            Collider[] immediateContacts = Physics.OverlapCapsule(
                bladeBase.position, bladeTip.position, 0.07f);
            Assert.That(immediateContacts
                .Any(value => value.GetComponentInParent<ForestInteractiveTree>() == tree), Is.True,
                $"Test setup must place the animated blade capsule on the promoted trunk. " +
                $"blade={bladeBase.position}/{bladeTip.position}, trunk={trunkCollider.bounds}, " +
                $"contacts={string.Join(",", immediateContacts.Select(value => value.name))}");

            float contactDeadline = Time.time + 1.1f;
            while (tree.AccumulatedDamage <= 0f && Time.time < contactDeadline)
            {
                yield return null;
            }

            Assert.That(tree.AccumulatedDamage, Is.GreaterThan(0f),
                "A real animated blade overlap should deliver the tree damage event.");
            Assert.That(tree.State, Is.EqualTo(ForestTreeLifecycleState.Damaged));
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
