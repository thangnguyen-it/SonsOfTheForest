using NUnit.Framework;
using SonsOfTheForest.Application.ForestCamp;
using SonsOfTheForest.Gameplay.Interaction;
using SonsOfTheForest.Gameplay.Inventory;
using UnityEditor;
using UnityEngine;

namespace SonsOfTheForest.Tests.ForestCamp.EditMode
{
    public sealed class ForestCampInteractionTests
    {
        [Test]
        public void ResourcePickup_AddsResourceAndDeactivatesWhenCollected()
        {
            var actor = new GameObject("Actor");
            var pickupObject = new GameObject("StickPickup");
            try
            {
                PlayerInventory inventory = actor.AddComponent<PlayerInventory>();
                ResourcePickup pickup = pickupObject.AddComponent<ResourcePickup>();
                InteractionContext context = CreateContext(actor);

                InteractionResult result = pickup.Interact(in context);

                Assert.That(result.Result.Succeeded, Is.True);
                Assert.That(
                    inventory.GetCount(ForestCampItemIds.Stick),
                    Is.EqualTo(1));
                Assert.That(pickupObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(pickupObject);
            }
        }

        [Test]
        public void CampfireConstruction_ConsumesRecipeAndSwitchesVisualState()
        {
            var actor = new GameObject("Actor");
            var siteObject = new GameObject("CampfireSite");
            var blueprint = new GameObject("Blueprint");
            var completed = new GameObject("Completed");
            var recipe = ScriptableObject.CreateInstance<CampfireRecipeAsset>();
            blueprint.transform.SetParent(siteObject.transform);
            completed.transform.SetParent(siteObject.transform);
            try
            {
                PlayerInventory inventory = actor.AddComponent<PlayerInventory>();
                inventory.TryAdd(new ItemStack(ForestCampItemIds.Stick, 2));
                inventory.TryAdd(new ItemStack(ForestCampItemIds.Stone, 4));
                ConfigureRecipe(recipe);

                CampfireConstructionSite site =
                    siteObject.AddComponent<CampfireConstructionSite>();
                var serializedSite = new SerializedObject(site);
                serializedSite.FindProperty("recipeAsset").objectReferenceValue = recipe;
                serializedSite.FindProperty("blueprintRoot").objectReferenceValue = blueprint;
                serializedSite.FindProperty("completedRoot").objectReferenceValue = completed;
                serializedSite.ApplyModifiedPropertiesWithoutUndo();
                InteractionContext context = CreateContext(actor);

                InteractionResult result = site.Interact(in context);

                Assert.That(result.Result.Succeeded, Is.True);
                Assert.That(site.Built, Is.True);
                Assert.That(inventory.GetCount(ForestCampItemIds.Stick), Is.Zero);
                Assert.That(inventory.GetCount(ForestCampItemIds.Stone), Is.Zero);
                Assert.That(blueprint.activeSelf, Is.False);
                Assert.That(completed.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(siteObject);
                Object.DestroyImmediate(recipe);
            }
        }

        private static InteractionContext CreateContext(GameObject actor)
        {
            return new InteractionContext(
                actor,
                actor.transform,
                null,
                0.016f,
                InteractionInputKind.Press);
        }

        private static void ConfigureRecipe(CampfireRecipeAsset recipe)
        {
            var serializedRecipe = new SerializedObject(recipe);
            SerializedProperty requirements =
                serializedRecipe.FindProperty("requirements");
            requirements.arraySize = 2;
            ConfigureRequirement(
                requirements.GetArrayElementAtIndex(0),
                ForestCampItemIds.Stick.Value,
                2);
            ConfigureRequirement(
                requirements.GetArrayElementAtIndex(1),
                ForestCampItemIds.Stone.Value,
                4);
            serializedRecipe.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRequirement(
            SerializedProperty requirement,
            string itemId,
            int quantity)
        {
            requirement.FindPropertyRelative("itemId").stringValue = itemId;
            requirement.FindPropertyRelative("quantity").intValue = quantity;
        }
    }
}
