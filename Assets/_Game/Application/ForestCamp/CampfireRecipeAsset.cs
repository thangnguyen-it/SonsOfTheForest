using System;
using System.Collections.Generic;
using SonsOfTheForest.Gameplay.Building;
using SonsOfTheForest.Gameplay.Inventory;
using SonsOfTheForest.Gameplay.Items;
using UnityEngine;

namespace SonsOfTheForest.Application.ForestCamp
{
    [CreateAssetMenu(
        fileName = "RCP_Campfire",
        menuName = "Sons Of The Forest/Forest Camp/Campfire Recipe")]
    public sealed class CampfireRecipeAsset : ScriptableObject
    {
        [Serializable]
        public struct Requirement
        {
            [SerializeField]
            private string itemId;

            [SerializeField, Min(1)]
            private int quantity;

            public ItemId ItemId => new(itemId);

            public int Quantity => quantity;
        }

        [SerializeField]
        private Requirement[] requirements = Array.Empty<Requirement>();

        public IReadOnlyList<Requirement> Requirements => requirements;

        public ResourceRecipe CreateRecipe()
        {
            var stacks = new ItemStack[requirements.Length];
            for (int index = 0; index < requirements.Length; index++)
            {
                stacks[index] = new ItemStack(
                    requirements[index].ItemId,
                    requirements[index].Quantity);
            }

            return new ResourceRecipe(stacks);
        }
    }
}
