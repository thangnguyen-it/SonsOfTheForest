using System.Collections;
using System.Linq;
using NUnit.Framework;
using SonsOfTheForest.Application.ForestCamp;
using SonsOfTheForest.Gameplay.Interaction;
using SonsOfTheForest.Infrastructure.SceneComposition;
using SonsOfTheForest.Presentation.ForestCamp;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SonsOfTheForest.Tests.ForestCamp.PlayMode
{
    public sealed class ForestCampPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "SCN_Foundation",
                LoadSceneMode.Single);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnloadScene()
        {
            Scene loadedScene = SceneManager.GetSceneByName("SCN_Foundation");
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                yield break;
            }

            Scene cleanupScene = SceneManager.CreateScene(
                "ForestCampTestCleanup");
            SceneManager.SetActiveScene(cleanupScene);
            yield return SceneManager.UnloadSceneAsync(loadedScene);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OfficialScene_CompletesResourceToCampfireLoop()
        {
            FoundationSceneCompositionRoot composition = Object
                .FindObjectsByType<FoundationSceneCompositionRoot>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(root => root.gameObject.scene.name == "SCN_Foundation");
            GameObject actor = composition.LocalPlayer.gameObject;
            PlayerInventory inventory = actor.GetComponent<PlayerInventory>();
            ResourcePickup[] pickups = composition.Playground
                .GetComponentsInChildren<ResourcePickup>(false);
            InteractionContext context = new(
                actor,
                actor.transform,
                composition.PlayerCamera,
                Time.deltaTime,
                InteractionInputKind.Press);

            foreach (ResourcePickup pickup in pickups)
            {
                if (inventory.GetCount(ForestCampItemIds.Stick) >= 2 &&
                    inventory.GetCount(ForestCampItemIds.Stone) >= 4)
                {
                    break;
                }

                if ((pickup.ItemId == ForestCampItemIds.Stick &&
                     inventory.GetCount(ForestCampItemIds.Stick) >= 2) ||
                    (pickup.ItemId == ForestCampItemIds.Stone &&
                     inventory.GetCount(ForestCampItemIds.Stone) >= 4))
                {
                    continue;
                }

                pickup.Interact(in context);
            }

            CampfireConstructionSite campfire = composition.Playground
                .GetComponentsInChildren<CampfireConstructionSite>(true)
                .Single();
            InteractionResult result = campfire.Interact(in context);

            Assert.That(result.Result.Succeeded, Is.True);
            Assert.That(campfire.Built, Is.True);
            Assert.That(inventory.GetCount(ForestCampItemIds.Stick), Is.Zero);
            Assert.That(inventory.GetCount(ForestCampItemIds.Stone), Is.Zero);
            Assert.That(
                campfire.GetComponentInChildren<Light>(true).gameObject
                    .activeInHierarchy,
                Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InteractBinding_CollectsFocusedResourceWithE()
        {
            FoundationSceneCompositionRoot composition = Object
                .FindObjectsByType<FoundationSceneCompositionRoot>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(root => root.gameObject.scene.name == "SCN_Foundation");
            GameObject actor = composition.LocalPlayer.gameObject;
            PlayerInventory inventory = actor.GetComponent<PlayerInventory>();
            PlayerInteractionController controller =
                actor.GetComponent<PlayerInteractionController>();
            var target = new GameObject("FocusedStick");
            var collider = target.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            ResourcePickup pickup = target.AddComponent<ResourcePickup>();
            target.transform.position = composition.PlayerCamera.transform.position +
                                        composition.PlayerCamera.transform.forward * 1.5f;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            actor.GetComponent<PlayerInput>().SwitchCurrentControlScheme(
                "Keyboard&Mouse",
                keyboard,
                mouse);
            Physics.SyncTransforms();
            controller.RefreshFocus();

            Assert.That(controller.FocusedInteractable, Is.SameAs(pickup));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            InputSystem.Update();
            yield return null;

            Assert.That(
                inventory.GetCount(ForestCampItemIds.Stick),
                Is.EqualTo(1));
            Assert.That(target.activeSelf, Is.False);
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            Object.Destroy(target);
            yield return null;
        }
    }
}
