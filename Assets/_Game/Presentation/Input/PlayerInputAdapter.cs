using System;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SonsOfTheForest.Presentation.Input
{
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInputAdapter : MonoBehaviour, IPlayerIntentSource
    {
        private const string PlayerMapName = "Player";
        private const string MoveActionName = "Move";
        private const string LookActionName = "Look";
        private const string SprintActionName = "Sprint";
        private const string CrouchActionName = "Crouch";
        private const string JumpActionName = "Jump";

        [SerializeField]
        private CrouchInputPolicy crouchPolicy = CrouchInputPolicy.Hold;

        private PlayerInput playerInput;
        private PlayerInputBuffer buffer;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction jumpAction;
        private bool subscribed;

        public bool GameplayInputEnabled =>
            buffer != null && buffer.GameplayInputEnabled;

        public CrouchInputPolicy CrouchPolicy =>
            buffer == null ? crouchPolicy : buffer.CrouchPolicy;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            buffer = new PlayerInputBuffer(crouchPolicy);
            ResolveRequiredActions();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            buffer?.ResetAll();
        }

        public MovementIntent ConsumeMovementIntent()
        {
            return buffer == null
                ? MovementIntent.None
                : buffer.ConsumeMovementIntent();
        }

        public LookIntent ConsumeLookIntent()
        {
            return buffer == null
                ? LookIntent.None
                : buffer.ConsumeLookIntent();
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.SetGameplayInputEnabled(enabled);
            if (enabled)
            {
                RehydrateContinuousInput();
            }
        }

        public void SetCrouchPolicy(CrouchInputPolicy policy)
        {
            crouchPolicy = policy;
            if (buffer != null)
            {
                buffer.CrouchPolicy = policy;
            }
        }

        public void ResetAllInputState()
        {
            buffer?.ResetAll();
        }

        private void ResolveRequiredActions()
        {
            if (playerInput.actions == null)
            {
                throw new InvalidOperationException(
                    "PlayerInputAdapter requires PlayerInput.actions to be assigned.");
            }

            InputActionMap playerMap = playerInput.actions.FindActionMap(
                PlayerMapName,
                false);
            if (playerMap == null)
            {
                throw new InvalidOperationException(
                    $"PlayerInputAdapter could not find required action map '{PlayerMapName}' in PlayerInput.actions.");
            }

            moveAction = ResolveAction(playerMap, MoveActionName);
            lookAction = ResolveAction(playerMap, LookActionName);
            sprintAction = ResolveAction(playerMap, SprintActionName);
            crouchAction = ResolveAction(playerMap, CrouchActionName);
            jumpAction = ResolveAction(playerMap, JumpActionName);
        }

        private static InputAction ResolveAction(
            InputActionMap map,
            string actionName)
        {
            InputAction action = map.FindAction(actionName, false);
            if (action == null)
            {
                throw new InvalidOperationException(
                    $"PlayerInputAdapter could not find required action '{PlayerMapName}/{actionName}' in PlayerInput.actions.");
            }

            return action;
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            moveAction.performed += OnMovePerformed;
            moveAction.canceled += OnMoveCanceled;
            lookAction.performed += OnLookPerformed;
            lookAction.canceled += OnLookCanceled;
            sprintAction.performed += OnSprintPerformed;
            sprintAction.canceled += OnSprintCanceled;
            crouchAction.performed += OnCrouchPerformed;
            crouchAction.canceled += OnCrouchCanceled;
            jumpAction.performed += OnJumpPerformed;
            jumpAction.canceled += OnJumpCanceled;
            playerInput.onControlsChanged += OnControlsChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
            lookAction.performed -= OnLookPerformed;
            lookAction.canceled -= OnLookCanceled;
            sprintAction.performed -= OnSprintPerformed;
            sprintAction.canceled -= OnSprintCanceled;
            crouchAction.performed -= OnCrouchPerformed;
            crouchAction.canceled -= OnCrouchCanceled;
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.canceled -= OnJumpCanceled;
            playerInput.onControlsChanged -= OnControlsChanged;
            subscribed = false;
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            buffer.SetMove(context.ReadValue<Vector2>());
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            buffer.SetMove(Vector2.zero);
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            buffer.SetSprintHeld(true);
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            buffer.SetSprintHeld(false);
        }

        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            buffer.HandleCrouchPerformed();
        }

        private void OnCrouchCanceled(InputAction.CallbackContext context)
        {
            buffer.HandleCrouchCanceled();
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            buffer.QueueJump();
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            if (context.control.device is Pointer)
            {
                buffer.SetPointerDelta(value);
            }
            else
            {
                buffer.SetGamepadLookRate(value);
            }
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            if (context.control.device is Pointer)
            {
                buffer.SetPointerDelta(Vector2.zero);
            }
            else
            {
                buffer.SetGamepadLookRate(Vector2.zero);
            }
        }

        private void OnControlsChanged(PlayerInput changedPlayerInput)
        {
            if (changedPlayerInput != playerInput)
            {
                return;
            }

            buffer.ClearLookAndJumpTransientState();
            if (buffer.GameplayInputEnabled)
            {
                RehydrateContinuousInput();
            }
        }

        private void RehydrateContinuousInput()
        {
            buffer.SetMove(moveAction.ReadValue<Vector2>());
            buffer.SetSprintHeld(sprintAction.IsPressed());
        }
    }
}
