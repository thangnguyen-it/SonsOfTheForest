using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SonsOfTheForest.Tests.EditMode.Architecture
{
    public sealed class PlayerRuntimeArchitectureGuardTests
    {
        private static readonly string GameRoot = Path.GetFullPath(
            Path.Combine(UnityEngine.Application.dataPath, "_Game"));

        [Test]
        public void ApplicationPlayer_DoesNotOwnUnityPhysicsInputOrPresentation()
        {
            AssertSourcesDoNotContain("Application/Player",
                "UnityEngine.InputSystem",
                "Rigidbody",
                "CharacterController",
                "Physics.",
                "Camera.main",
                "Animator",
                "SonsOfTheForest.Presentation");
        }

        [Test]
        public void PlayerPhysics_DoesNotOwnInputCameraOrAnimation()
        {
            AssertSourcesDoNotContain("Infrastructure/PlayerPhysics",
                "UnityEngine.InputSystem",
                "Camera.main",
                "Animator",
                "SonsOfTheForest.Presentation");
        }

        [Test]
        public void PlayerPresentation_DoesNotOwnPhysicsOrInputPolling()
        {
            AssertSourcesDoNotContain("Presentation/Player",
                "UnityEngine.InputSystem",
                "Rigidbody",
                "CharacterController",
                "Physics.");
        }

        [TestCase("Application/Player/SonsOfTheForest.Application.Player.asmdef",
            new[] { "SonsOfTheForest.Core", "SonsOfTheForest.Gameplay.Player" })]
        [TestCase("Infrastructure/PlayerPhysics/SonsOfTheForest.Infrastructure.PlayerPhysics.asmdef",
            new[] { "SonsOfTheForest.Gameplay.Player" })]
        [TestCase("Presentation/Player/SonsOfTheForest.Presentation.Player.asmdef",
            new[] { "SonsOfTheForest.Gameplay.Player" })]
        public void RuntimeAssemblyReferences_AreMinimal(
            string relativePath,
            string[] expectedReferences)
        {
            string json = File.ReadAllText(Path.Combine(GameRoot, relativePath));
            var definition = JsonUtility.FromJson<AssemblyDefinition>(json);

            Assert.That(definition.references, Is.EquivalentTo(expectedReferences));
            Assert.That(json, Does.Not.Contain("Tests"));
        }

        private static void AssertSourcesDoNotContain(
            string relativeRoot,
            params string[] forbidden)
        {
            string root = Path.Combine(
                GameRoot,
                relativeRoot.Replace('/', Path.DirectorySeparatorChar));
            var violations = new List<string>();
            foreach (string path in Directory.EnumerateFiles(
                         root, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(path);
                foreach (string text in forbidden)
                {
                    if (source.IndexOf(text, StringComparison.Ordinal) >= 0)
                    {
                        violations.Add(Path.GetFileName(path) + " contains '" +
                            text + "'.");
                    }
                }
            }

            Assert.That(violations, Is.Empty,
                string.Join(Environment.NewLine, violations));
        }

        [Serializable]
        private sealed class AssemblyDefinition
        {
            public string[] references = Array.Empty<string>();
        }
    }
}
