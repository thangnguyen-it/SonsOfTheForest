using System.IO;
using NUnit.Framework;
using SonsOfTheForest.Gameplay.Player;
using SonsOfTheForest.Presentation.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools.Utils;

namespace SonsOfTheForest.Tests.Input.EditMode
{
    public sealed class PlayerInputAdapterInputTests : InputTestFixture
    {
        private const string InputAssetPath =
            "Assets/InputSystem_Actions.inputactions";

        private Keyboard keyboard;
        private Mouse mouse;
        private Gamepad gamepad;
        private InputActionAsset isolatedActions;
        private GameObject playerObject;
        private PlayerInput playerInput;
        private PlayerInputAdapter adapter;

        public override void Setup()
        {
            base.Setup();

            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            gamepad = InputSystem.AddDevice<Gamepad>();

            InputActionAsset source =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            Assert.That(source, Is.Not.Null);

            isolatedActions = Object.Instantiate(source);
            isolatedActions.name = source.name + " (R1-B Test Copy)";

            playerObject = new GameObject("R1-B PlayerInput Test");
            playerObject.hideFlags = HideFlags.HideAndDontSave;
            playerObject.SetActive(false);

            playerInput = playerObject.AddComponent<PlayerInput>();
            playerInput.actions = isolatedActions;
            playerInput.defaultActionMap = "Player";
            playerInput.defaultControlScheme = "Keyboard&Mouse";
            playerInput.neverAutoSwitchControlSchemes = false;
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            playerInput.runInEditMode = true;

            adapter = playerObject.AddComponent<PlayerInputAdapter>();
            adapter.runInEditMode = true;
            playerObject.SetActive(true);
        }

        public override void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }

            if (isolatedActions != null)
            {
                Object.DestroyImmediate(isolatedActions);
            }

            base.TearDown();
        }

        [Test]
        public void KeyboardMovePerformedAndCanceled()
        {
            Press(keyboard.wKey);
            Assert.That(
                adapter.ConsumeMovementIntent().Move,
                Is.EqualTo(Vector2.up));

            Release(keyboard.wKey);
            Assert.That(
                adapter.ConsumeMovementIntent().Move,
                Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void KeyboardDiagonalMoveIsClampedByMovementIntent()
        {
            Press(keyboard.wKey);
            Press(keyboard.dKey);

            Vector2 move = adapter.ConsumeMovementIntent().Move;

            Assert.That(move.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(move.x, Is.EqualTo(move.y).Within(0.0001f));
        }

        [Test]
        public void GamepadLeftStickProducesMove()
        {
            UseGamepad();
            var input = new Vector2(0.25f, 0.75f);

            Set(gamepad.leftStick, input);
            Vector2 expected = gamepad.leftStick.ReadValue();

            Assert.That(
                adapter.ConsumeMovementIntent().Move,
                Is.EqualTo(expected).Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void SprintPressAndRelease()
        {
            Press(keyboard.leftShiftKey);
            Assert.That(adapter.ConsumeMovementIntent().SprintRequested, Is.True);

            Release(keyboard.leftShiftKey);
            Assert.That(adapter.ConsumeMovementIntent().SprintRequested, Is.False);
        }

        [Test]
        public void JumpConsumesExactlyOnceWhileButtonRemainsHeld()
        {
            Press(keyboard.spaceKey);

            Assert.That(adapter.ConsumeMovementIntent().JumpRequested, Is.True);
            Assert.That(adapter.ConsumeMovementIntent().JumpRequested, Is.False);
        }

        [Test]
        public void JumpQuickPressAndReleaseBetweenConsumesIsNotLost()
        {
            PressAndRelease(keyboard.spaceKey);

            Assert.That(adapter.ConsumeMovementIntent().JumpRequested, Is.True);
            Assert.That(adapter.ConsumeMovementIntent().JumpRequested, Is.False);
        }

        [Test]
        public void HoldCrouchTracksButtonState()
        {
            adapter.SetCrouchPolicy(CrouchInputPolicy.Hold);

            Press(keyboard.cKey);
            Assert.That(adapter.ConsumeMovementIntent().CrouchRequested, Is.True);

            Release(keyboard.cKey);
            Assert.That(adapter.ConsumeMovementIntent().CrouchRequested, Is.False);
        }

        [Test]
        public void ToggleCrouchChangesOnlyOnPress()
        {
            adapter.SetCrouchPolicy(CrouchInputPolicy.Toggle);

            Press(keyboard.cKey);
            Release(keyboard.cKey);
            Assert.That(adapter.ConsumeMovementIntent().CrouchRequested, Is.True);

            Press(keyboard.cKey);
            Assert.That(adapter.ConsumeMovementIntent().CrouchRequested, Is.False);
        }

        [Test]
        public void MouseLookProducesDeltaIntent()
        {
            var expected = new Vector2(4f, -3f);
            Set(mouse.delta, expected);

            LookIntent intent = adapter.ConsumeLookIntent();

            Assert.That(intent.Value, Is.EqualTo(expected));
            Assert.That(intent.InputKind, Is.EqualTo(LookInputKind.Delta));
        }

        [Test]
        public void MouseDeltaIsNotMultipliedByDeltaTime()
        {
            var rawDelta = new Vector2(12f, -8f);

            Set(mouse.delta, rawDelta);

            Assert.That(adapter.ConsumeLookIntent().Value, Is.EqualTo(rawDelta));
        }

        [Test]
        public void MouseDeltaDoesNotDoubleAccumulate()
        {
            Set(mouse.delta, new Vector2(2f, 3f));
            Set(mouse.delta, new Vector2(5f, 7f));

            Assert.That(
                adapter.ConsumeLookIntent().Value,
                Is.EqualTo(new Vector2(5f, 7f)));
        }

        [Test]
        public void MouseDeltaClearsAfterConsume()
        {
            Set(mouse.delta, new Vector2(2f, 3f));

            Assert.That(adapter.ConsumeLookIntent().Value, Is.Not.EqualTo(Vector2.zero));
            Assert.That(adapter.ConsumeLookIntent().Value, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void GamepadLookProducesRateIntent()
        {
            UseGamepad();
            var input = new Vector2(0.25f, -0.5f);
            Set(gamepad.rightStick, input);
            Vector2 expected = gamepad.rightStick.ReadValue();

            LookIntent intent = adapter.ConsumeLookIntent();

            Assert.That(
                intent.Value,
                Is.EqualTo(expected).Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.That(intent.InputKind, Is.EqualTo(LookInputKind.Rate));
        }

        [Test]
        public void GamepadLookRatePersistsAcrossConsumes()
        {
            UseGamepad();
            var input = new Vector2(0.25f, -0.5f);
            Set(gamepad.rightStick, input);
            Vector2 rate = gamepad.rightStick.ReadValue();

            Assert.That(
                adapter.ConsumeLookIntent().Value,
                Is.EqualTo(rate).Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.That(
                adapter.ConsumeLookIntent().Value,
                Is.EqualTo(rate).Using(Vector2ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void GamepadLookRateClearsOnReleaseOrZero()
        {
            UseGamepad();
            Set(gamepad.rightStick, new Vector2(0.25f, -0.5f));
            Set(gamepad.rightStick, Vector2.zero);

            LookIntent intent = adapter.ConsumeLookIntent();
            Assert.That(intent.Value, Is.EqualTo(Vector2.zero));
            Assert.That(intent.InputKind, Is.EqualTo(LookInputKind.Rate));
        }

        [Test]
        public void ControlsChangedClearsLookAndJumpTransientState()
        {
            Press(keyboard.spaceKey);
            Set(mouse.delta, new Vector2(4f, 5f));

            UseGamepad();

            Assert.That(adapter.ConsumeMovementIntent().JumpRequested, Is.False);
            Assert.That(adapter.ConsumeLookIntent().Value, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SuspendClearsMovementAndTransientInput()
        {
            Press(keyboard.wKey);
            Press(keyboard.leftShiftKey);
            Press(keyboard.spaceKey);
            Set(mouse.delta, new Vector2(4f, 5f));

            adapter.SetGameplayInputEnabled(false);

            MovementIntent disabledMovement = adapter.ConsumeMovementIntent();
            Assert.That(disabledMovement.Move, Is.EqualTo(Vector2.zero));
            Assert.That(disabledMovement.SprintRequested, Is.False);
            Assert.That(disabledMovement.JumpRequested, Is.False);
            Assert.That(adapter.ConsumeLookIntent().Value, Is.EqualTo(Vector2.zero));

            Release(keyboard.wKey);
            Release(keyboard.leftShiftKey);
            Release(keyboard.spaceKey);
            adapter.SetGameplayInputEnabled(true);

            MovementIntent reenabledMovement = adapter.ConsumeMovementIntent();
            Assert.That(reenabledMovement.Move, Is.EqualTo(Vector2.zero));
            Assert.That(reenabledMovement.SprintRequested, Is.False);
            Assert.That(reenabledMovement.JumpRequested, Is.False);
            Assert.That(adapter.ConsumeLookIntent().Value, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SuspendPreservesCrouchDesired()
        {
            Press(keyboard.cKey);

            adapter.SetGameplayInputEnabled(false);
            adapter.SetGameplayInputEnabled(true);

            Assert.That(adapter.ConsumeMovementIntent().CrouchRequested, Is.True);
        }

        [Test]
        public void FullResetClearsCrouchDesired()
        {
            Press(keyboard.cKey);

            adapter.ResetAllInputState();

            Assert.That(adapter.ConsumeMovementIntent().CrouchRequested, Is.False);
        }

        [Test]
        public void AdapterDoesNotSubscribeToAttackInteractPreviousOrNext()
        {
            PressAndRelease(mouse.leftButton);
            PressAndRelease(keyboard.enterKey);
            PressAndRelease(keyboard.eKey);
            PressAndRelease(keyboard.digit1Key);
            PressAndRelease(keyboard.digit2Key);

            MovementIntent movement = adapter.ConsumeMovementIntent();
            LookIntent look = adapter.ConsumeLookIntent();
            Assert.That(movement.Move, Is.EqualTo(Vector2.zero));
            Assert.That(movement.SprintRequested, Is.False);
            Assert.That(movement.CrouchRequested, Is.False);
            Assert.That(movement.JumpRequested, Is.False);
            Assert.That(look.Value, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void AdapterDoesNotEnableOrDisableUiMap()
        {
            InputActionMap uiMap = playerInput.actions.FindActionMap("UI", true);
            Assert.That(uiMap.enabled, Is.False);

            adapter.SetGameplayInputEnabled(false);
            Assert.That(uiMap.enabled, Is.False);

            adapter.SetGameplayInputEnabled(true);
            adapter.ResetAllInputState();
            Assert.That(uiMap.enabled, Is.False);
        }

        [Test]
        public void GameplayPlayerAssemblyStillHasNoInputSystemReference()
        {
            string playerRoot = Path.Combine(
                Application.dataPath,
                "_Game",
                "Gameplay",
                "Player");
            string asmdef = File.ReadAllText(Path.Combine(
                playerRoot,
                "SonsOfTheForest.Gameplay.Player.asmdef"));

            Assert.That(asmdef, Does.Not.Contain("Unity.InputSystem"));
            foreach (string source in Directory.EnumerateFiles(
                         playerRoot,
                         "*.cs",
                         SearchOption.AllDirectories))
            {
                Assert.That(
                    File.ReadAllText(source),
                    Does.Not.Contain("UnityEngine.InputSystem"));
            }
        }

        [Test]
        public void DisableAndReenableAdapterRestoresCleanOperationalState()
        {
            Press(keyboard.cKey);
            Press(keyboard.spaceKey);
            Set(mouse.delta, new Vector2(4f, 5f));
            adapter.SetGameplayInputEnabled(false);

            adapter.enabled = false;
            adapter.enabled = true;

            Assert.That(adapter.GameplayInputEnabled, Is.True);
            Press(keyboard.wKey);
            MovementIntent movement = adapter.ConsumeMovementIntent();
            LookIntent look = adapter.ConsumeLookIntent();
            Assert.That(movement.Move, Is.EqualTo(Vector2.up));
            Assert.That(movement.CrouchRequested, Is.False);
            Assert.That(movement.JumpRequested, Is.False);
            Assert.That(look.Value, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void AdapterInitializesWithoutReflectedLifecycleCalls()
        {
            Assert.That(adapter.GameplayInputEnabled, Is.True);

            Press(keyboard.wKey);

            Assert.That(
                adapter.ConsumeMovementIntent().Move,
                Is.EqualTo(Vector2.up));
        }

        private void UseGamepad()
        {
            playerInput.SwitchCurrentControlScheme("Gamepad", gamepad);
        }
    }
}
