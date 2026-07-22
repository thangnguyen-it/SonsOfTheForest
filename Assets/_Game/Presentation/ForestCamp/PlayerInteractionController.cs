using System;
using SonsOfTheForest.Application.ForestCamp;
using SonsOfTheForest.Gameplay.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SonsOfTheForest.Presentation.ForestCamp
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerInventory))]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        private const string PlayerMapName = "Player";
        private const string InteractActionName = "Interact";

        [SerializeField]
        private GameObject actor;

        [SerializeField]
        private Transform interactionOrigin;

        [SerializeField]
        private Camera viewCamera;

        [SerializeField]
        private InteractionPromptView promptView;

        [SerializeField, Min(0.1f)]
        private float maxDistance = 3f;

        [SerializeField, Range(0f, 0.5f)]
        private float focusRadius = 0.08f;

        [SerializeField]
        private LayerMask collisionMask = ~0;

        private readonly RaycastHit[] hits = new RaycastHit[16];
        private PlayerInput playerInput;
        private InputAction interactAction;
        private MonoBehaviour focusedBehaviour;
        private IInteractable focusedInteractable;
        private bool subscribed;
        private bool enabledInteractActionForSubscription;
        private int lastInteractionFrame = -1;

        public IInteractable FocusedInteractable => focusedInteractable;

        private void Awake()
        {
            actor ??= gameObject;
            playerInput = GetComponent<PlayerInput>();
            ResolveRequiredAction();
            promptView?.BindInventory(GetComponent<PlayerInventory>());
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetFocus(null, null);
            promptView?.SetPrompt(InteractionPrompt.None);
        }

        private void LateUpdate()
        {
            RefreshFocus();
            if (interactAction != null && interactAction.WasPressedThisFrame())
            {
                TryInteractOnceThisFrame();
            }
        }

        public void RefreshFocus()
        {
            if (interactionOrigin == null || viewCamera == null)
            {
                SetFocus(null, null);
                promptView?.SetPrompt(InteractionPrompt.None);
                return;
            }

            FindNearestInteractable(
                out MonoBehaviour behaviour,
                out IInteractable interactable);
            SetFocus(behaviour, interactable);

            InteractionPrompt prompt = focusedInteractable != null
                ? focusedInteractable.GetPrompt(BuildContext())
                : InteractionPrompt.None;
            promptView?.SetPrompt(prompt);
        }

        public InteractionResult TryInteract()
        {
            if (focusedInteractable == null)
            {
                return InteractionResult.NotHandled();
            }

            InteractionContext context = BuildContext();
            if (!focusedInteractable.CanInteract(in context))
            {
                var unavailable = InteractionResult.HandledFailure(
                    "The interaction requirements are not met.");
                promptView?.ShowResult(unavailable);
                return unavailable;
            }

            InteractionResult result = focusedInteractable.Interact(in context);
            promptView?.ShowResult(result);
            RefreshFocus();
            return result;
        }

        private void ResolveRequiredAction()
        {
            if (playerInput.actions == null)
            {
                throw new InvalidOperationException(
                    "PlayerInteractionController requires PlayerInput.actions.");
            }

            InputActionMap playerMap = playerInput.actions.FindActionMap(
                PlayerMapName,
                false);
            interactAction = playerMap?.FindAction(InteractActionName, false);
            if (interactAction == null)
            {
                throw new InvalidOperationException(
                    $"PlayerInteractionController requires '{PlayerMapName}/{InteractActionName}'.");
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            interactAction.performed += OnInteractPerformed;
            if (!interactAction.enabled)
            {
                interactAction.Enable();
                enabledInteractActionForSubscription = true;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            interactAction.performed -= OnInteractPerformed;
            if (enabledInteractActionForSubscription && interactAction.enabled)
            {
                interactAction.Disable();
            }

            enabledInteractActionForSubscription = false;
            subscribed = false;
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            TryInteractOnceThisFrame();
        }

        private void TryInteractOnceThisFrame()
        {
            if (lastInteractionFrame == Time.frameCount)
            {
                return;
            }

            lastInteractionFrame = Time.frameCount;
            TryInteract();
        }

        private InteractionContext BuildContext()
        {
            return new InteractionContext(
                actor,
                actor != null ? actor.transform : transform,
                viewCamera,
                Time.deltaTime,
                InteractionInputKind.Press);
        }

        private void FindNearestInteractable(
            out MonoBehaviour nearestBehaviour,
            out IInteractable nearestInteractable)
        {
            nearestBehaviour = null;
            nearestInteractable = null;
            float nearestDistance = float.MaxValue;
            int count = Physics.SphereCastNonAlloc(
                interactionOrigin.position,
                focusRadius,
                viewCamera.transform.forward,
                hits,
                maxDistance,
                collisionMask,
                QueryTriggerInteraction.Collide);

            for (int index = 0; index < count; index++)
            {
                Collider collider = hits[index].collider;
                if (collider == null ||
                    (actor != null && collider.transform.IsChildOf(actor.transform)) ||
                    hits[index].distance >= nearestDistance ||
                    !TryFindInteractable(
                        collider,
                        out MonoBehaviour behaviour,
                        out IInteractable interactable))
                {
                    continue;
                }

                nearestDistance = hits[index].distance;
                nearestBehaviour = behaviour;
                nearestInteractable = interactable;
            }
        }

        private static bool TryFindInteractable(
            Collider collider,
            out MonoBehaviour behaviour,
            out IInteractable interactable)
        {
            MonoBehaviour[] candidates =
                collider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour candidate in candidates)
            {
                if (candidate is IInteractable value)
                {
                    behaviour = candidate;
                    interactable = value;
                    return true;
                }
            }

            behaviour = null;
            interactable = null;
            return false;
        }

        private void SetFocus(
            MonoBehaviour behaviour,
            IInteractable interactable)
        {
            if (focusedBehaviour == behaviour)
            {
                focusedInteractable = interactable;
                return;
            }

            InteractionContext context = BuildContext();
            if (focusedInteractable is IFocusableInteractable oldFocusable)
            {
                oldFocusable.OnFocusLost(in context);
            }

            focusedBehaviour = behaviour;
            focusedInteractable = interactable;
            if (focusedInteractable is IFocusableInteractable newFocusable)
            {
                newFocusable.OnFocus(in context);
            }
        }
    }
}
