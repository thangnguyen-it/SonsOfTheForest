using System.Collections;
using System.Linq;
using NUnit.Framework;
using SonsOfTheForest.Application.Player;
using SonsOfTheForest.Core;
using SonsOfTheForest.Infrastructure.SceneComposition;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SonsOfTheForest.Tests.SceneComposition.PlayMode
{
    public sealed class FoundationScenePlayModeTests
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private FoundationSceneCompositionRoot composition;
        private PlayerFoundationController player;

        [UnitySetUp]
        public IEnumerator LoadFoundationScene()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            yield return SceneManager.LoadSceneAsync(
                "SCN_Foundation",
                LoadSceneMode.Single);
            yield return null;

            composition = Object.FindObjectsByType<FoundationSceneCompositionRoot>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single();
            player = composition.LocalPlayer;
            yield return FixedFrames(5);
        }

        [UnityTearDown]
        public IEnumerator RemoveTestDevices()
        {
            if (keyboard != null && keyboard.added)
            {
                InputSystem.RemoveDevice(keyboard);
            }

            if (mouse != null && mouse.added)
            {
                InputSystem.RemoveDevice(mouse);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator OfficialScene_LoadsReadyGroundedPlayerComposition()
        {
            ConfigurePlayerInput();
            var report = new ValidationReport();
            composition.Validate(report);

            Assert.That(report.IsValid, Is.True, string.Join(
                "\n",
                report.Issues.Select(issue => issue.ToString())));
            Assert.That(player.CurrentMovementState.IsGrounded, Is.True);
            Assert.That(composition.PlayerCamera.isActiveAndEnabled, Is.True);
            Assert.That(composition.FallbackCamera.isActiveAndEnabled, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator KeyboardInput_MovesLocalPlayerAcrossPlayground()
        {
            ConfigurePlayerInput();
            Vector3 start = player.transform.position;
            for (int index = 0; index < 20; index++)
            {
                InputSystem.QueueStateEvent(
                    keyboard,
                    new KeyboardState(Key.W));
                InputSystem.Update();
                yield return new WaitForFixedUpdate();
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();

            Assert.That(player.transform.position.z - start.z,
                Is.GreaterThan(0.5f));
            Assert.That(player.CurrentMovementState.IsGrounded, Is.True);
        }

        private void ConfigurePlayerInput()
        {
            player.GetComponent<PlayerInput>()
                .SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, mouse);
        }

        private static IEnumerator FixedFrames(int count)
        {
            for (int index = 0; index < count; index++)
            {
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
