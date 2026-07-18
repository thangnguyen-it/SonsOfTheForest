using SonsOfTheForest.Core;
using SonsOfTheForest.Gameplay.Interaction;
using SonsOfTheForest.Gameplay.Inventory;
using SonsOfTheForest.Gameplay.Items;
using UnityEngine;

namespace SonsOfTheForest.Application.ForestCamp
{
    [DisallowMultipleComponent]
    public sealed class ResourcePickup : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string itemId = "resource.stick";

        [SerializeField]
        private string displayName = "Stick";

        [SerializeField, Min(1)]
        private int quantity = 1;

        public ItemId ItemId => new(itemId);

        public int RemainingQuantity => quantity;

        public InteractionPrompt GetPrompt(in InteractionContext context)
        {
            return quantity > 0 && ItemId.IsValid
                ? InteractionPrompt.Press($"Pick up {displayName}")
                : InteractionPrompt.None;
        }

        public bool CanInteract(in InteractionContext context)
        {
            return quantity > 0 &&
                   ItemId.IsValid &&
                   TryGetInventory(in context, out _);
        }

        public InteractionResult Interact(in InteractionContext context)
        {
            if (!TryGetInventory(in context, out PlayerInventory inventory))
            {
                return InteractionResult.HandledFailure(
                    "The interacting actor has no inventory.");
            }

            GameResult<int> result = inventory.TryAdd(
                new ItemStack(ItemId, quantity));
            if (result.Failed)
            {
                return InteractionResult.HandledFailure(result.Error);
            }

            quantity -= result.Value;
            if (quantity <= 0)
            {
                gameObject.SetActive(false);
            }

            return InteractionResult.HandledSuccess();
        }

        private static bool TryGetInventory(
            in InteractionContext context,
            out PlayerInventory inventory)
        {
            inventory = context.Actor != null
                ? context.Actor.GetComponent<PlayerInventory>()
                : null;
            return inventory != null;
        }
    }
}
