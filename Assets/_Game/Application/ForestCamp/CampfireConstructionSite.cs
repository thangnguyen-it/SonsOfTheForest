using System.Text;
using SonsOfTheForest.Core;
using SonsOfTheForest.Gameplay.Building;
using SonsOfTheForest.Gameplay.Interaction;
using SonsOfTheForest.Gameplay.Inventory;
using UnityEngine;

namespace SonsOfTheForest.Application.ForestCamp
{
    [DisallowMultipleComponent]
    public sealed class CampfireConstructionSite : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private CampfireRecipeAsset recipeAsset;

        [SerializeField]
        private GameObject blueprintRoot;

        [SerializeField]
        private GameObject completedRoot;

        [SerializeField]
        private bool built;

        private ResourceRecipe recipe;

        public bool Built => built;

        private void Awake()
        {
            ApplyVisualState();
        }

        public InteractionPrompt GetPrompt(in InteractionContext context)
        {
            if (built || recipeAsset == null)
            {
                return InteractionPrompt.None;
            }

            ResourceRecipe currentRecipe = GetRecipe();
            if (!TryGetInventory(in context, out PlayerInventory inventory) ||
                !currentRecipe.CanAfford(inventory))
            {
                return new InteractionPrompt(
                    $"Need {FormatRequirements(currentRecipe)}",
                    InteractionInputKind.Press,
                    0f,
                    InteractionAvailability.Disabled);
            }

            return InteractionPrompt.Press(
                $"Build campfire  •  {FormatRequirements(currentRecipe)}");
        }

        public bool CanInteract(in InteractionContext context)
        {
            return !built &&
                   recipeAsset != null &&
                   TryGetInventory(in context, out PlayerInventory inventory) &&
                   GetRecipe().CanAfford(inventory);
        }

        public InteractionResult Interact(in InteractionContext context)
        {
            if (built)
            {
                return InteractionResult.NotHandled();
            }

            if (recipeAsset == null)
            {
                return InteractionResult.HandledFailure(
                    "The campfire recipe is not configured.");
            }

            if (!TryGetInventory(in context, out PlayerInventory inventory))
            {
                return InteractionResult.HandledFailure(
                    "The interacting actor has no inventory.");
            }

            ResourceRecipe currentRecipe = GetRecipe();
            GameResult result = currentRecipe.TryConsume(inventory);
            if (result.Failed)
            {
                return InteractionResult.HandledFailure(result.Error);
            }

            built = true;
            ApplyVisualState();
            return InteractionResult.HandledSuccess();
        }

        private ResourceRecipe GetRecipe()
        {
            recipe ??= recipeAsset.CreateRecipe();
            return recipe;
        }

        private void ApplyVisualState()
        {
            if (blueprintRoot != null)
            {
                blueprintRoot.SetActive(!built);
            }

            if (completedRoot != null)
            {
                completedRoot.SetActive(built);
            }
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

        private static string FormatRequirements(ResourceRecipe resourceRecipe)
        {
            var text = new StringBuilder();
            foreach (ItemStack stack in resourceRecipe.Requirements)
            {
                if (text.Length > 0)
                {
                    text.Append("  •  ");
                }

                string label = stack.ItemId == ForestCampItemIds.Stick
                    ? "sticks"
                    : stack.ItemId == ForestCampItemIds.Stone
                        ? "stones"
                        : stack.ItemId.Value;
                text.Append(stack.Quantity).Append(' ').Append(label);
            }

            return text.ToString();
        }
    }
}
