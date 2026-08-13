using System.Collections;
using SonsOfTheForest.Gameplay.Player;
using SonsOfTheForest.Infrastructure.Forest;
using SonsOfTheForest.Presentation.ForestCamp;
using SonsOfTheForest.Presentation.Player;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Validation.Forest
{
    [DisallowMultipleComponent]
    public sealed class ForestHarvestQualityValidationDriver : MonoBehaviour
    {
        [SerializeField] private PlayerAxeHarvestController axeController;
        [SerializeField] private bool autoEquipOnStart = true;
        [SerializeField] private TransformPlayerLookDriver lookDriver;

        public PlayerAxeHarvestController AxeController => axeController;

        public bool ApplyValidationDamage(ForestInteractiveTree tree, float amount, int sequence)
        {
            if (tree == null || amount <= 0f)
            {
                return false;
            }

            var damage = new TreeDamageEvent(amount,
                tree.transform.position + Vector3.up * 0.7f,
                Vector3.back, Vector3.forward, "validation.harvest", sequence);
            return tree.TryReceiveDamage(in damage, out _);
        }

        private IEnumerator Start()
        {
            if (lookDriver != null)
            {
                lookDriver.ApplyLook(PlayerLookState.Identity);
            }
            yield return null;
            if (autoEquipOnStart && axeController != null)
            {
                axeController.BeginEquip();
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(PlayerAxeHarvestController controller, bool autoEquip,
            TransformPlayerLookDriver playerLookDriver)
        {
            axeController = controller;
            autoEquipOnStart = autoEquip;
            lookDriver = playerLookDriver;
        }
#endif
    }
}
