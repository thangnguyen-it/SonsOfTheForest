using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Presentation.Input
{
    internal sealed class PlayerInputBuffer
    {
        private Vector2 move;
        private bool sprintHeld;
        private bool crouchDesired;
        private bool jumpQueued;
        private Vector2 pointerDelta;
        private Vector2 gamepadLookRate;
        private LookInputKind activeLookKind = LookInputKind.Delta;
        private bool gameplayInputEnabled = true;
        private CrouchInputPolicy crouchPolicy;

        public PlayerInputBuffer(CrouchInputPolicy crouchPolicy = CrouchInputPolicy.Hold)
        {
            this.crouchPolicy = crouchPolicy;
        }

        public CrouchInputPolicy CrouchPolicy
        {
            get => crouchPolicy;
            set => crouchPolicy = value;
        }

        public bool GameplayInputEnabled => gameplayInputEnabled;

        public void SetMove(Vector2 value)
        {
            if (gameplayInputEnabled)
            {
                move = value;
            }
        }

        public void SetSprintHeld(bool value)
        {
            if (gameplayInputEnabled)
            {
                sprintHeld = value;
            }
        }

        public void HandleCrouchPerformed()
        {
            if (!gameplayInputEnabled)
            {
                return;
            }

            crouchDesired = crouchPolicy == CrouchInputPolicy.Toggle
                ? !crouchDesired
                : true;
        }

        public void HandleCrouchCanceled()
        {
            if (gameplayInputEnabled && crouchPolicy == CrouchInputPolicy.Hold)
            {
                crouchDesired = false;
            }
        }

        public void QueueJump()
        {
            if (gameplayInputEnabled)
            {
                jumpQueued = true;
            }
        }

        public void SetPointerDelta(Vector2 value)
        {
            if (!gameplayInputEnabled)
            {
                return;
            }

            pointerDelta = value;
            activeLookKind = LookInputKind.Delta;
        }

        public void SetGamepadLookRate(Vector2 value)
        {
            if (!gameplayInputEnabled)
            {
                return;
            }

            gamepadLookRate = value;
            activeLookKind = LookInputKind.Rate;
        }

        public void ClearPointerDelta()
        {
            pointerDelta = Vector2.zero;
        }

        public void ClearGamepadLookRate()
        {
            gamepadLookRate = Vector2.zero;
        }

        public void ClearLookAndJumpTransientState()
        {
            jumpQueued = false;
            ClearPointerDelta();
            ClearGamepadLookRate();
            activeLookKind = LookInputKind.Delta;
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            gameplayInputEnabled = enabled;
            if (!enabled)
            {
                move = Vector2.zero;
                sprintHeld = false;
                ClearLookAndJumpTransientState();
            }
        }

        public void ResetAll()
        {
            gameplayInputEnabled = true;
            move = Vector2.zero;
            sprintHeld = false;
            crouchDesired = false;
            ClearLookAndJumpTransientState();
        }

        public MovementIntent ConsumeMovementIntent()
        {
            if (!gameplayInputEnabled)
            {
                return MovementIntent.None;
            }

            var intent = new MovementIntent(
                move,
                sprintHeld,
                crouchDesired,
                jumpQueued);
            jumpQueued = false;
            return intent;
        }

        public LookIntent ConsumeLookIntent()
        {
            if (!gameplayInputEnabled)
            {
                return LookIntent.None;
            }

            if (activeLookKind == LookInputKind.Rate)
            {
                return new LookIntent(gamepadLookRate, LookInputKind.Rate);
            }

            var intent = new LookIntent(pointerDelta, LookInputKind.Delta);
            pointerDelta = Vector2.zero;
            return intent;
        }
    }
}
