using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SonsOfTheForest.Tests.EditMode.Player
{
    public sealed class PlayerVisualAssetTests
    {
        private const string ModelPath =
            "Assets/_Game/Art/Models/Player/Rocketbox/Wood_Male_01/Wood_Male_01.fbx";

        private const string AnimationPath =
            "Assets/_Game/Art/Animations/Player/Quaternius/UAL1/UAL1_Standard.fbx";

        private const string MaterialRoot =
            "Assets/_Game/Art/Materials/Player/Wood_Male_01";

        private const string PrefabPath =
            "Assets/_Game/Prefabs/Player/PRF_PlayerVisual_WoodMale01.prefab";

        [Test]
        public void RocketboxModel_ImportsAsValidAdultHumanoid()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(importer.avatarSetup, Is.EqualTo(ModelImporterAvatarSetup.CreateFromThisModel));
            Assert.That(importer.bakeAxisConversion, Is.True);
            Assert.That(importer.optimizeGameObjects, Is.False);
            Assert.That(model, Is.Not.Null);

            var animator = model.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.avatar, Is.Not.Null);
            Assert.That(animator.avatar.isValid, Is.True);
            Assert.That(animator.avatar.isHuman, Is.True);

            var mesh = model.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
            var size = mesh.bounds.size;
            var largestDimension = Mathf.Max(size.x, size.y, size.z);
            Assert.That(largestDimension, Is.InRange(1.7f, 2.0f));
        }

        [Test]
        public void AnimationLibrary_ProvidesRequiredHumanoidClips()
        {
            var importer = AssetImporter.GetAtPath(AnimationPath) as ModelImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(importer.bakeAxisConversion, Is.True);
            Assert.That(importer.motionNodeName, Is.EqualTo("Armature/root"));

            var clips = AssetDatabase.LoadAllAssetsAtPath(AnimationPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToDictionary(clip => clip.name);

            var requiredLooping = new[]
            {
                "Idle_Loop",
                "Walk_Loop",
                "Sprint_Loop",
                "Crouch_Idle_Loop",
                "Crouch_Fwd_Loop",
                "Jump_Loop",
            };

            foreach (var clipName in requiredLooping)
            {
                Assert.That(clips.ContainsKey(clipName), Is.True, clipName);
                Assert.That(clips[clipName].humanMotion, Is.True, clipName);
                Assert.That(clips[clipName].isLooping, Is.True, clipName);
            }

            foreach (var clipName in new[] { "Jump_Start", "Jump_Land" })
            {
                Assert.That(clips.ContainsKey(clipName), Is.True, clipName);
                Assert.That(clips[clipName].humanMotion, Is.True, clipName);
            }
        }

        [Test]
        public void Materials_UseHdrpLitAndExpectedTextureBindings()
        {
            var body = LoadMaterial("MAT_WoodMale01_Body");
            var head = LoadMaterial("MAT_WoodMale01_Head");
            var hiddenHead = LoadMaterial("MAT_WoodMale01_HeadHidden");

            foreach (var material in new[] { body, head, hiddenHead })
                Assert.That(material.shader.name, Is.EqualTo("HDRP/Lit"), material.name);

            foreach (var material in new[] { body, head })
            {
                Assert.That(material.GetTexture("_BaseColorMap"), Is.Not.Null, material.name);
                Assert.That(material.GetTexture("_NormalMap"), Is.Not.Null, material.name);
                Assert.That(material.GetTexture("_SpecularColorMap"), Is.Not.Null, material.name);
            }

            Assert.That(hiddenHead.GetColor("_BaseColor").a, Is.EqualTo(0f));
        }

        [Test]
        public void VisualPrefab_ProvidesFullLocalAndShadowRendererModes()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var animator = prefab.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.avatar.isValid, Is.True);
            Assert.That(animator.avatar.isHuman, Is.True);
            Assert.That(animator.runtimeAnimatorController, Is.Null);
            Assert.That(animator.applyRootMotion, Is.False);

            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .ToDictionary(renderer => renderer.name);
            Assert.That(renderers.Keys, Is.EquivalentTo(new[]
            {
                "FullBodyRenderer",
                "LocalFirstPersonRenderer",
                "LocalFullBodyShadowRenderer",
            }));

            Assert.That(renderers["FullBodyRenderer"].enabled, Is.True);
            Assert.That(renderers["FullBodyRenderer"].shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
            Assert.That(renderers["LocalFirstPersonRenderer"].enabled, Is.False);
            Assert.That(renderers["LocalFirstPersonRenderer"].shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(renderers["LocalFullBodyShadowRenderer"].enabled, Is.False);
            Assert.That(
                renderers["LocalFullBodyShadowRenderer"].shadowCastingMode,
                Is.EqualTo(ShadowCastingMode.ShadowsOnly));
        }

        [Test]
        public void VisualPrefab_RemainsPresentationOnly()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<CharacterController>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Camera>(true), Is.Empty);
        }

        private static Material LoadMaterial(string name)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{name}.mat");
            Assert.That(material, Is.Not.Null, name);
            return material;
        }
    }
}
