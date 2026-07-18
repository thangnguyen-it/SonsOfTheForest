using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SonsOfTheForest.Application.Player;
using SonsOfTheForest.Core;
using SonsOfTheForest.Infrastructure.PlayerPhysics;
using SonsOfTheForest.Presentation.Input;
using SonsOfTheForest.Presentation.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class PlayerFoundationAssetTests
    {
        private const string SettingsPath =
            "Assets/_Game/Data/Player/CFG_PlayerFoundation.asset";

        private const string ControllerPath =
            "Assets/_Game/Art/Animations/Player/Controllers/AC_PlayerFoundation.controller";

        private const string PrefabPath =
            "Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab";

        private const string ScenePath =
            "Assets/_Game/Scenes/SCN_PlayerFoundationValidation.unity";

        [Test]
        public void FoundationSettings_AreValidAndRemainProvisionalDefaults()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerFoundationSettings>(
                SettingsPath);
            Assert.That(settings, Is.Not.Null);

            var report = new ValidationReport();
            settings.Validate(report);

            Assert.That(report.IsValid, Is.True, string.Join(
                Environment.NewLine,
                report.Issues.Select(issue => issue.ToString())));
            Assert.That(settings.MovementConfig.WalkSpeed, Is.EqualTo(4f));
            Assert.That(settings.MovementConfig.SprintSpeed, Is.EqualTo(7f));
            Assert.That(settings.MovementConfig.CrouchSpeed, Is.EqualTo(2f));
            Assert.That(settings.MovementConfig.JumpHeight, Is.EqualTo(1.2f));
        }

        [Test]
        public void FoundationPrefab_ComposesSeparatedRuntimeResponsibilities()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var body = prefab.GetComponent<Rigidbody>();
            var capsule = prefab.GetComponent<CapsuleCollider>();
            Assert.That(body, Is.Not.Null);
            Assert.That(body.useGravity, Is.False);
            Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.FreezeRotation));
            Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
            Assert.That(
                body.collisionDetectionMode,
                Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));
            Assert.That(capsule, Is.Not.Null);
            Assert.That(capsule.sharedMaterial, Is.Not.Null);

            Assert.That(prefab.GetComponent<PlayerInput>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerInputAdapter>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<RigidbodyPlayerMotionDriver>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<TransformPlayerLookDriver>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerFoundationController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerViewHeightController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerAnimationController>(), Is.Not.Null);
            Assert.That(
                prefab.GetComponentInChildren<LocalPlayerVisualController>(true),
                Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<Camera>(true), Is.Not.Null);
        }

        [Test]
        public void FoundationPrefab_HasAllRequiredSerializedReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            AssertObjectReference(prefab.GetComponent<PlayerFoundationController>(),
                "settings", "intentSourceComponent", "motionDriverComponent",
                "lookDriverComponent");
            AssertObjectReference(prefab.GetComponent<TransformPlayerLookDriver>(),
                "bodyYaw", "viewPitch");
            AssertObjectReference(prefab.GetComponent<PlayerViewHeightController>(),
                "viewRoot", "stateReaderComponent");
            AssertObjectReference(prefab.GetComponent<PlayerAnimationController>(),
                "animator", "stateReaderComponent");

            var visual = prefab.GetComponentInChildren<LocalPlayerVisualController>(true);
            AssertObjectReference(visual, "fullBodyRenderer", "localBodyRenderer",
                "fullBodyShadowRenderer");
        }

        [Test]
        public void AnimatorController_MapsEveryRequiredHumanoidClip()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath);
            Assert.That(controller, Is.Not.Null);

            var states = controller.layers[0].stateMachine.states
                .Select(child => child.state)
                .ToDictionary(state => state.name);
            string[] expected =
            {
                "Idle_Loop",
                "Walk_Loop",
                "Sprint_Loop",
                "Crouch_Idle_Loop",
                "Crouch_Fwd_Loop",
                "Jump_Start",
                "Jump_Loop",
                "Jump_Land",
            };

            Assert.That(states.Keys, Is.EquivalentTo(expected));
            foreach (string name in expected)
            {
                Assert.That(states[name].motion, Is.TypeOf<AnimationClip>(), name);
                Assert.That(states[name].motion.name, Is.EqualTo(name), name);
            }
        }

        [Test]
        public void ValidationScene_ContainsPrefabAndFocusedObstacleSet()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
            string yaml = File.ReadAllText(ScenePath);
            Assert.That(yaml, Does.Contain("PlayerFoundation"));
            Assert.That(yaml, Does.Contain("CollisionWall"));
            Assert.That(yaml, Does.Contain("LowStep_0_25m"));
            Assert.That(yaml, Does.Contain("HighStep_0_45m"));
            Assert.That(yaml, Does.Contain("WalkableSlope_20deg"));
            Assert.That(yaml, Does.Contain("CrouchClearanceTunnel"));
        }

        private static void AssertObjectReference(
            UnityEngine.Object target,
            params string[] propertyNames)
        {
            Assert.That(target, Is.Not.Null);
            var serialized = new SerializedObject(target);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                Assert.That(property, Is.Not.Null, propertyName);
                Assert.That(property.objectReferenceValue, Is.Not.Null, propertyName);
            }
        }
    }
}
