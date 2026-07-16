using NUnit.Framework;
using SonsOfTheForest.Gameplay.Player;
using SonsOfTheForest.Presentation.Input;
using UnityEngine;

namespace SonsOfTheForest.Tests.Input.EditMode
{
    public sealed class PlayerInputBufferTests
    {
        [Test]
        public void InitialStateProducesNoIntent()
        {
            var buffer = new PlayerInputBuffer();

            MovementIntent movement = buffer.ConsumeMovementIntent();
            LookIntent look = buffer.ConsumeLookIntent();

            Assert.That(movement.Move, Is.EqualTo(Vector2.zero));
            Assert.That(movement.SprintRequested, Is.False);
            Assert.That(movement.CrouchRequested, Is.False);
            Assert.That(movement.JumpRequested, Is.False);
            Assert.That(look.Value, Is.EqualTo(Vector2.zero));
            Assert.That(look.InputKind, Is.EqualTo(LookInputKind.Delta));
        }

        [Test]
        public void MoveValueIsPreservedAndMovementIntentClampsMagnitude()
        {
            var buffer = new PlayerInputBuffer();
            buffer.SetMove(new Vector2(1f, 1f));

            MovementIntent intent = buffer.ConsumeMovementIntent();

            Assert.That(intent.Move.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(intent.Move.x, Is.EqualTo(intent.Move.y).Within(0.0001f));
        }

        [Test]
        public void SprintHeldIsPreserved()
        {
            var buffer = new PlayerInputBuffer();
            buffer.SetSprintHeld(true);

            Assert.That(buffer.ConsumeMovementIntent().SprintRequested, Is.True);
            Assert.That(buffer.ConsumeMovementIntent().SprintRequested, Is.True);
        }

        [Test]
        public void JumpIsConsumedExactlyOnce()
        {
            var buffer = new PlayerInputBuffer();
            buffer.QueueJump();

            Assert.That(buffer.ConsumeMovementIntent().JumpRequested, Is.True);
            Assert.That(buffer.ConsumeMovementIntent().JumpRequested, Is.False);
        }

        [Test]
        public void RepeatedJumpPerformedBeforeConsumeStillProducesOneRequest()
        {
            var buffer = new PlayerInputBuffer();
            buffer.QueueJump();
            buffer.QueueJump();

            Assert.That(buffer.ConsumeMovementIntent().JumpRequested, Is.True);
            Assert.That(buffer.ConsumeMovementIntent().JumpRequested, Is.False);
        }

        [Test]
        public void JumpCanceledBehaviorDoesNotRemoveQueue()
        {
            var buffer = new PlayerInputBuffer();
            buffer.QueueJump();

            Assert.That(buffer.ConsumeMovementIntent().JumpRequested, Is.True);
        }

        [Test]
        public void HoldCrouchFollowsPerformedAndCanceled()
        {
            var buffer = new PlayerInputBuffer(CrouchInputPolicy.Hold);

            buffer.HandleCrouchPerformed();
            Assert.That(buffer.ConsumeMovementIntent().CrouchRequested, Is.True);

            buffer.HandleCrouchCanceled();
            Assert.That(buffer.ConsumeMovementIntent().CrouchRequested, Is.False);
        }

        [Test]
        public void ToggleCrouchChangesOnlyOnPerformed()
        {
            var buffer = new PlayerInputBuffer(CrouchInputPolicy.Toggle);

            buffer.HandleCrouchPerformed();
            buffer.HandleCrouchCanceled();
            Assert.That(buffer.ConsumeMovementIntent().CrouchRequested, Is.True);

            buffer.HandleCrouchPerformed();
            Assert.That(buffer.ConsumeMovementIntent().CrouchRequested, Is.False);
        }

        [Test]
        public void PointerDeltaReplacesPreviousValueInsteadOfAdding()
        {
            var buffer = new PlayerInputBuffer();
            buffer.SetPointerDelta(new Vector2(2f, 3f));
            buffer.SetPointerDelta(new Vector2(5f, 7f));

            Assert.That(
                buffer.ConsumeLookIntent().Value,
                Is.EqualTo(new Vector2(5f, 7f)));
        }

        [Test]
        public void PointerDeltaClearsAfterConsume()
        {
            var buffer = new PlayerInputBuffer();
            buffer.SetPointerDelta(new Vector2(2f, 3f));

            Assert.That(buffer.ConsumeLookIntent().Value, Is.Not.EqualTo(Vector2.zero));
            Assert.That(buffer.ConsumeLookIntent().Value, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void GamepadRatePersistsAcrossConsumes()
        {
            var buffer = new PlayerInputBuffer();
            var rate = new Vector2(0.25f, -0.5f);
            buffer.SetGamepadLookRate(rate);

            Assert.That(buffer.ConsumeLookIntent().Value, Is.EqualTo(rate));
            Assert.That(buffer.ConsumeLookIntent().Value, Is.EqualTo(rate));
        }

        [Test]
        public void GamepadRateClearsWhenSetToZero()
        {
            var buffer = new PlayerInputBuffer();
            buffer.SetGamepadLookRate(new Vector2(0.25f, -0.5f));
            buffer.SetGamepadLookRate(Vector2.zero);

            LookIntent intent = buffer.ConsumeLookIntent();
            Assert.That(intent.Value, Is.EqualTo(Vector2.zero));
            Assert.That(intent.InputKind, Is.EqualTo(LookInputKind.Rate));
        }

        [Test]
        public void ClearPointerDeltaDoesNotOverrideActiveGamepadRate()
        {
            var buffer = new PlayerInputBuffer();
            var rate = new Vector2(0.25f, -0.5f);
            buffer.SetGamepadLookRate(rate);

            buffer.ClearPointerDelta();

            LookIntent intent = buffer.ConsumeLookIntent();
            Assert.That(intent.Value, Is.EqualTo(rate));
            Assert.That(intent.InputKind, Is.EqualTo(LookInputKind.Rate));
        }

        [Test]
        public void ClearGamepadRateDoesNotOverridePendingPointerDelta()
        {
            var buffer = new PlayerInputBuffer();
            var delta = new Vector2(5f, 7f);
            buffer.SetPointerDelta(delta);

            buffer.ClearGamepadLookRate();

            LookIntent intent = buffer.ConsumeLookIntent();
            Assert.That(intent.Value, Is.EqualTo(delta));
            Assert.That(intent.InputKind, Is.EqualTo(LookInputKind.Delta));
        }

        [Test]
        public void SuspendClearsMoveSprintJumpAndLook()
        {
            var buffer = new PlayerInputBuffer();
            buffer.SetMove(Vector2.one);
            buffer.SetSprintHeld(true);
            buffer.QueueJump();
            buffer.SetPointerDelta(Vector2.one);
            buffer.SetGamepadLookRate(Vector2.one);

            buffer.SetGameplayInputEnabled(false);
            buffer.SetGameplayInputEnabled(true);

            MovementIntent movement = buffer.ConsumeMovementIntent();
            LookIntent look = buffer.ConsumeLookIntent();
            Assert.That(movement.Move, Is.EqualTo(Vector2.zero));
            Assert.That(movement.SprintRequested, Is.False);
            Assert.That(movement.JumpRequested, Is.False);
            Assert.That(look.Value, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SuspendRetainsCrouchDesired()
        {
            var buffer = new PlayerInputBuffer();
            buffer.HandleCrouchPerformed();

            buffer.SetGameplayInputEnabled(false);
            buffer.SetGameplayInputEnabled(true);

            Assert.That(buffer.ConsumeMovementIntent().CrouchRequested, Is.True);
        }

        [Test]
        public void DisabledBufferReturnsNone()
        {
            var buffer = new PlayerInputBuffer();
            buffer.HandleCrouchPerformed();
            buffer.SetGameplayInputEnabled(false);

            MovementIntent movement = buffer.ConsumeMovementIntent();
            LookIntent look = buffer.ConsumeLookIntent();

            Assert.That(movement.Move, Is.EqualTo(Vector2.zero));
            Assert.That(movement.CrouchRequested, Is.False);
            Assert.That(look.Value, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ReenableDoesNotRestoreTransientInput()
        {
            var buffer = new PlayerInputBuffer();
            buffer.QueueJump();
            buffer.SetPointerDelta(Vector2.one);

            buffer.SetGameplayInputEnabled(false);
            buffer.SetGameplayInputEnabled(true);

            Assert.That(buffer.ConsumeMovementIntent().JumpRequested, Is.False);
            Assert.That(buffer.ConsumeLookIntent().Value, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void FullResetClearsCrouchDesired()
        {
            var buffer = new PlayerInputBuffer();
            buffer.HandleCrouchPerformed();

            buffer.ResetAll();

            Assert.That(buffer.ConsumeMovementIntent().CrouchRequested, Is.False);
        }

        [Test]
        public void FullResetClearsAllOtherState()
        {
            var buffer = new PlayerInputBuffer();
            buffer.SetMove(Vector2.one);
            buffer.SetSprintHeld(true);
            buffer.QueueJump();
            buffer.SetPointerDelta(Vector2.one);
            buffer.SetGamepadLookRate(Vector2.one);

            buffer.ResetAll();

            MovementIntent movement = buffer.ConsumeMovementIntent();
            LookIntent look = buffer.ConsumeLookIntent();
            Assert.That(movement.Move, Is.EqualTo(Vector2.zero));
            Assert.That(movement.SprintRequested, Is.False);
            Assert.That(movement.JumpRequested, Is.False);
            Assert.That(look.Value, Is.EqualTo(Vector2.zero));
            Assert.That(look.InputKind, Is.EqualTo(LookInputKind.Delta));
        }

        [Test]
        public void FullResetRestoresGameplayInputEnabled()
        {
            var buffer = new PlayerInputBuffer();
            buffer.SetGameplayInputEnabled(false);

            buffer.ResetAll();

            Assert.That(buffer.GameplayInputEnabled, Is.True);
            MovementIntent movement = buffer.ConsumeMovementIntent();
            LookIntent look = buffer.ConsumeLookIntent();
            Assert.That(movement.Move, Is.EqualTo(Vector2.zero));
            Assert.That(movement.SprintRequested, Is.False);
            Assert.That(movement.CrouchRequested, Is.False);
            Assert.That(movement.JumpRequested, Is.False);
            Assert.That(look.Value, Is.EqualTo(Vector2.zero));
            Assert.That(look.InputKind, Is.EqualTo(LookInputKind.Delta));
        }
    }
}
