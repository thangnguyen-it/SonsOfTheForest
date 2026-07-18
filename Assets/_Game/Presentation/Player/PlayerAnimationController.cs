using System;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Presentation.Player
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private MonoBehaviour stateReaderComponent;

        [SerializeField]
        private float crossFadeDuration = 0.12f;

        [SerializeField]
        private float takeoffPresentationTime = 0.12f;

        [SerializeField]
        private float landingPresentationTime = 0.18f;

        private IPlayerMovementStateReader stateReader;
        private bool initialized;
        private bool previouslyGrounded;
        private float takeoffUntil;
        private float landingUntil;
        private int currentStateHash;

        private void Awake()
        {
            if (animator == null)
            {
                throw new InvalidOperationException(
                    "PlayerAnimationController requires an Animator.");
            }

            stateReader = stateReaderComponent as IPlayerMovementStateReader;
            if (stateReader == null)
            {
                throw new InvalidOperationException(
                    "PlayerAnimationController requires a movement-state reader.");
            }

            animator.applyRootMotion = false;
        }

        private void Update()
        {
            PlayerMovementState state = stateReader.CurrentMovementState;
            if (!initialized)
            {
                initialized = true;
                previouslyGrounded = state.IsGrounded;
                Play(ResolveSteadyState(state));
                return;
            }

            if (previouslyGrounded &&
                !state.IsGrounded &&
                state.VerticalVelocity > 0f)
            {
                takeoffUntil = Time.time +
                    Mathf.Max(0f, takeoffPresentationTime);
                Play(PlayerAnimationSelector.JumpStart);
            }
            else if (!previouslyGrounded && state.IsGrounded)
            {
                landingUntil = Time.time +
                    Mathf.Max(0f, landingPresentationTime);
                Play(PlayerAnimationSelector.JumpLand);
            }
            else if (!state.IsGrounded && Time.time >= takeoffUntil)
            {
                Play(PlayerAnimationSelector.JumpLoop);
            }
            else if (state.IsGrounded && Time.time >= landingUntil)
            {
                Play(PlayerAnimationSelector.ResolveGrounded(state));
            }

            previouslyGrounded = state.IsGrounded;
        }

        private static string ResolveSteadyState(PlayerMovementState state)
        {
            return state.IsGrounded
                ? PlayerAnimationSelector.ResolveGrounded(state)
                : PlayerAnimationSelector.JumpLoop;
        }

        private void Play(string stateName)
        {
            int stateHash = Animator.StringToHash(stateName);
            if (stateHash == currentStateHash)
            {
                return;
            }

            currentStateHash = stateHash;
            animator.CrossFadeInFixedTime(
                stateHash,
                Mathf.Max(0f, crossFadeDuration),
                0,
                0f);
        }
    }
}
