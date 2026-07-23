using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SonsOfTheForest.Application.Player;
using SonsOfTheForest.Core;
using SonsOfTheForest.Infrastructure.SceneBootstrap;
using SonsOfTheForest.Infrastructure.SceneComposition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SonsOfTheForest.Tests.SceneComposition.EditMode
{
    public sealed class FoundationSceneCompositionTests
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/SCN_Foundation.unity";

        private const string PlayerPrefabPath =
            "Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab";

        private const string PlaygroundPrefabPath =
            "Assets/_Game/Prefabs/World/ForestCamp/" +
            "PRF_ForestCampPlayground.prefab";

        [Test]
        public void OfficialScene_ComposesValidatedPlayerAndPlaygroundPrefabs()
        {
            WithFoundationScene(scene =>
            {
                FoundationSceneCompositionRoot root = GetOnlyRoot(scene);
                var report = new ValidationReport();
                root.Validate(report);

                Assert.That(report.IsValid, Is.True, FormatIssues(report));
                Assert.That(root.SceneId.Value, Is.EqualTo("scene.foundation"));
                Assert.That(root.GetBootstrapSteps(), Is.Empty);
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                        root.LocalPlayer),
                    Is.EqualTo(PlayerPrefabPath));
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                        root.Playground),
                    Is.EqualTo(PlaygroundPrefabPath));
            });
        }

        [Test]
        public void OfficialScene_HasOneActivePlayerCameraAndAudioListener()
        {
            WithFoundationScene(scene =>
            {
                FoundationSceneCompositionRoot root = GetOnlyRoot(scene);
                Camera[] activeCameras = GetSceneComponents<Camera>(scene)
                    .Where(camera => camera.isActiveAndEnabled)
                    .ToArray();
                AudioListener[] activeListeners =
                    GetSceneComponents<AudioListener>(scene)
                        .Where(listener => listener.isActiveAndEnabled)
                        .ToArray();

                Assert.That(activeCameras, Has.Length.EqualTo(1));
                Assert.That(activeCameras[0], Is.SameAs(root.PlayerCamera));
                Assert.That(activeListeners, Has.Length.EqualTo(1));
                Assert.That(
                    activeListeners[0].gameObject,
                    Is.SameAs(root.PlayerCamera.gameObject));
                Assert.That(root.FallbackCamera.isActiveAndEnabled, Is.False);
            });
        }

        [Test]
        public void OfficialScene_PlayerAndSurfaceUseOwnedContainers()
        {
            WithFoundationScene(scene =>
            {
                FoundationSceneCompositionRoot root = GetOnlyRoot(scene);

                Assert.That(
                    root.LocalPlayer.transform.parent,
                    Is.SameAs(root.PlayerContainer));
                Assert.That(
                    root.Playground.transform.parent,
                    Is.SameAs(root.WorldContainer));
                Assert.That(
                    root.PlayableSurface.transform.IsChildOf(
                        root.Playground.transform),
                    Is.True);
                Assert.That(root.PlayableSurface.isTrigger, Is.False);
            });
        }

        [Test]
        public void OfficialScene_HasExactlyOneForestCampPlaygroundInstance()
        {
            WithFoundationScene(scene =>
            {
                GameObject[] playgrounds = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Where(transform => transform.name == "PRF_ForestCampPlayground")
                    .Select(transform => transform.gameObject)
                    .ToArray();

                Assert.That(playgrounds, Has.Length.EqualTo(1));
                Assert.That(
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                        playgrounds[0]),
                    Is.EqualTo(PlaygroundPrefabPath));
            });
        }

        [Test]
        public void SceneReadinessValidation_RejectsUnexpectedSceneIdentity()
        {
            WithFoundationScene(scene =>
            {
                FoundationSceneCompositionRoot root = GetOnlyRoot(scene);
                var context = new SceneBootstrapContext(
                    new StableStringId("scene.unexpected"),
                    null,
                    null);
                var report = new ValidationReport();

                root.ValidateScene(in context, report);

                Assert.That(report.HasErrors, Is.True);
                Assert.That(
                    report.Issues.Select(issue => issue.Code),
                    Does.Contain("SCENE.ID_MISMATCH"));
            });
        }

        [Test]
        public void SceneCompositionRuntime_UsesOnlyApprovedDependenciesAndNoLookups()
        {
            string projectRoot = Directory.GetParent(
                UnityEngine.Application.dataPath).FullName;
            string assemblyPath = Path.Combine(
                projectRoot,
                "Assets/_Game/Infrastructure/SceneComposition/" +
                "SonsOfTheForest.Infrastructure.SceneComposition.asmdef");
            string sourceRoot = Path.GetDirectoryName(assemblyPath);
            string assemblyJson = File.ReadAllText(assemblyPath);
            var definition = JsonUtility.FromJson<AssemblyDefinition>(
                assemblyJson);

            Assert.That(definition.references, Is.EquivalentTo(new[]
            {
                "SonsOfTheForest.Application.Player",
                "SonsOfTheForest.Core",
                "SonsOfTheForest.Infrastructure.SceneBootstrap",
            }));
            StringAssert.DoesNotContain("Tests", assemblyJson);

            string source = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(sourceRoot, "*.cs")
                    .Select(File.ReadAllText));
            string[] forbidden =
            {
                "GameObject.Find",
                "FindObjectOfType",
                "FindFirstObjectByType",
                "FindAnyObjectByType",
                "Camera.main",
                "Resources.Load",
                "UnityEngine.InputSystem",
            };

            foreach (string value in forbidden)
            {
                Assert.That(source, Does.Not.Contain(value), value);
            }
        }

        private static FoundationSceneCompositionRoot GetOnlyRoot(Scene scene)
        {
            FoundationSceneCompositionRoot[] roots =
                GetSceneComponents<FoundationSceneCompositionRoot>(scene)
                    .ToArray();
            Assert.That(roots, Has.Length.EqualTo(1));
            return roots[0];
        }

        private static IEnumerable<T> GetSceneComponents<T>(Scene scene)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true));
        }

        private static void WithFoundationScene(Action<Scene> assertion)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool closeWhenComplete = !scene.IsValid() || !scene.isLoaded;
            if (closeWhenComplete)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                assertion(scene);
            }
            finally
            {
                if (closeWhenComplete)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static string FormatIssues(ValidationReport report)
        {
            return string.Join(
                Environment.NewLine,
                report.Issues.Select(issue => issue.ToString()));
        }

        [Serializable]
        private sealed class AssemblyDefinition
        {
            public string[] references = Array.Empty<string>();
        }
    }
}
