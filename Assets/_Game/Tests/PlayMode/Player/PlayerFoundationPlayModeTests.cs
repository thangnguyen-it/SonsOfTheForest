using System.Collections;
using NUnit.Framework;
using SonsOfTheForest.Application.Player;
using SonsOfTheForest.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace SonsOfTheForest.Tests.Player.PlayMode
{
    public sealed class PlayerFoundationPlayModeTests : InputTestFixture
    {
        private const string PrefabPath =
            "Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab";

        private Keyboard keyboard;
        private Mouse mouse;
        private GameObject ground;
        private GameObject player;
        private PlayerInput playerInput;
        private PlayerFoundationController controller;
        private CapsuleCollider capsule;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();

            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Player Test Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(30f, 1f, 30f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            player = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            player.name = "Player Foundation PlayMode Test";
            playerInput = player.GetComponent<PlayerInput>();
            controller = player.GetComponent<PlayerFoundationController>();
            capsule = player.GetComponent<CapsuleCollider>();
            playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, mouse);
        }

        public override void TearDown()
        {
            if (player != null)
            {
                Object.DestroyImmediate(player);
            }

            if (ground != null)
            {
                Object.DestroyImmediate(ground);
            }

            base.TearDown();
        }

        [UnityTest]
        public IEnumerator KeyboardInput_DrivesGroundedWalkAndSprint()
        {
            yield return FixedFrames(4);
            yield return HoldForFixedFrames(20, Key.W);

            float walkSpeed = controller.CurrentMovementState.PlanarSpeed;
            Assert.That(player.transform.position.z, Is.GreaterThan(0.5f));
            Assert.That(walkSpeed, Is.GreaterThan(2f));

            bool observedSprint = false;
            float maximumSprintSpeed = 0f;
            for (int index = 0; index < 20; index++)
            {
                SetKeyboardState(Key.W, Key.LeftShift);
                yield return new WaitForFixedUpdate();
                observedSprint |= controller.CurrentMovementState.IsSprinting;
                maximumSprintSpeed = Mathf.Max(
                    maximumSprintSpeed,
                    controller.CurrentMovementState.PlanarSpeed);
            }

            Assert.That(observedSprint, Is.True,
                "Sprint was never observed; final mode=" +
                controller.CurrentMovementState.LocomotionMode +
                ", grounded=" + controller.CurrentMovementState.IsGrounded +
                ", speed=" + controller.CurrentMovementState.PlanarSpeed +
                ", z=" + player.transform.position.z);
            Assert.That(maximumSprintSpeed, Is.GreaterThan(walkSpeed + 1f));
            SetKeyboardState();
        }

        [UnityTest]
        public IEnumerator RigidbodyCapsule_CollidesWithEnvironmentWall()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Player Test Wall";
            wall.transform.position = new Vector3(0f, 1f, 2.5f);
            wall.transform.localScale = new Vector3(4f, 2f, 0.4f);
            try
            {
                yield return FixedFrames(4);
                yield return HoldForFixedFrames(60, Key.W);
                SetKeyboardState();

                Assert.That(player.transform.position.z, Is.LessThan(2.1f));
                Assert.That(player.transform.position.z, Is.GreaterThan(1f));
            }
            finally
            {
                Object.DestroyImmediate(wall);
            }
        }

        [UnityTest]
        public IEnumerator Jump_RisesFallsAndReturnsToGroundedState()
        {
            yield return FixedFrames(5);
            SetKeyboardState(Key.Space);
            SetKeyboardState();
            yield return FixedFrames(8);

            Assert.That(player.transform.position.y, Is.GreaterThan(0.05f));
            Assert.That(controller.CurrentMovementState.IsGrounded, Is.False);

            int remaining = 160;
            while (!controller.CurrentMovementState.IsGrounded && remaining-- > 0)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(remaining, Is.GreaterThan(0), "Player did not land.");
            Assert.That(player.transform.position.y, Is.EqualTo(0f).Within(0.08f));
        }

        [UnityTest]
        public IEnumerator CrouchRelease_BlocksStandingUntilClearanceReturns()
        {
            yield return FixedFrames(4);
            yield return HoldForFixedFrames(4, Key.C);
            Assert.That(capsule.height, Is.EqualTo(1.2f).Within(0.001f));

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Player Test Low Ceiling";
            ceiling.transform.position = new Vector3(0f, 1.5f, 0f);
            ceiling.transform.localScale = new Vector3(3f, 0.3f, 3f);
            try
            {
                SetKeyboardState();
                yield return FixedFrames(5);
                Assert.That(controller.CurrentMovementState.Stance,
                    Is.EqualTo(PlayerStance.Crouching));
                Assert.That(capsule.height, Is.EqualTo(1.2f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(ceiling);
            }

            yield return FixedFrames(5);
            Assert.That(controller.CurrentMovementState.Stance,
                Is.EqualTo(PlayerStance.Standing));
            Assert.That(capsule.height, Is.EqualTo(2f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator MouseDelta_DrivesFirstPersonYawAndPitch()
        {
            yield return null;
            Set(mouse.delta, new Vector2(30f, -20f));
            yield return null;

            Assert.That(Mathf.Abs(controller.CurrentLookState.Yaw),
                Is.GreaterThan(0.1f));
            Assert.That(Mathf.Abs(controller.CurrentLookState.Pitch),
                Is.GreaterThan(0.1f));

            Transform bodyYaw = player.transform.Find("BodyYaw");
            Transform viewPitch = bodyYaw.Find("ViewPitch");
            Assert.That(bodyYaw.localEulerAngles.y, Is.Not.EqualTo(0f).Within(0.01f));
            Assert.That(viewPitch.localEulerAngles.x, Is.Not.EqualTo(0f).Within(0.01f));
        }

        private static IEnumerator FixedFrames(int count)
        {
            for (int index = 0; index < count; index++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator HoldForFixedFrames(
            int count,
            params Key[] keys)
        {
            for (int index = 0; index < count; index++)
            {
                SetKeyboardState(keys);
                yield return new WaitForFixedUpdate();
            }
        }

        private void SetKeyboardState(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
    }
}
