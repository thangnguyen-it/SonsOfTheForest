using System;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Presentation.Player
{
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public sealed class PlayerViewHeightController : MonoBehaviour
    {
        [SerializeField]
        private Transform viewRoot;

        [SerializeField]
        private MonoBehaviour stateReaderComponent;

        [SerializeField]
        private float standingViewHeight = 1.65f;

        [SerializeField]
        private float crouchingViewHeight = 1.05f;

        [SerializeField]
        private float transitionSpeed = 3.5f;

        private IPlayerMovementStateReader stateReader;

        private void Awake()
        {
            if (viewRoot == null)
            {
                throw new InvalidOperationException(
                    "PlayerViewHeightController requires a view root.");
            }

            stateReader = stateReaderComponent as IPlayerMovementStateReader;
            if (stateReader == null)
            {
                throw new InvalidOperationException(
                    "PlayerViewHeightController requires a movement-state reader.");
            }
        }

        private void Update()
        {
            float targetHeight =
                stateReader.CurrentMovementState.Stance ==
                PlayerStance.Crouching
                    ? crouchingViewHeight
                    : standingViewHeight;
            Vector3 position = viewRoot.localPosition;
            position.y = Mathf.MoveTowards(
                position.y,
                targetHeight,
                Mathf.Max(0f, transitionSpeed) * Time.deltaTime);
            viewRoot.localPosition = position;
        }
    }
}
