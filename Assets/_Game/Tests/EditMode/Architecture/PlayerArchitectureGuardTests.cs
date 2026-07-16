using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SonsOfTheForest.Tests.EditMode.Architecture
{
    public sealed class PlayerArchitectureGuardTests
    {
        private static readonly string PlayerRoot =
            Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "_Game",
                    "Gameplay",
                    "Player"));

        [Test]
        public void PlayerGameplayDoesNotUseForbiddenNamespaces()
        {
            AssertPlayerFilesDoNotContain(
                "using " + "TheForest",
                "namespace " + "TheForest",
                "using Unity.Netcode",
                "using UnityEngine.InputSystem",
                "using SonsOfTheForest.Presentation",
                "using SonsOfTheForest.Infrastructure.Persistence",
                "using SonsOfTheForest.Gameplay.AI");
        }

        [Test]
        public void PlayerGameplayDoesNotUseSceneLookupOrResources()
        {
            AssertPlayerFilesDoNotContain(
                "FindObjectOfType",
                "FindFirstObjectByType",
                "FindAnyObjectByType",
                "GameObject.Find",
                "Camera.main",
                "Resources.Load");
        }

        [Test]
        public void PlayerGameplayDoesNotImplementMonoBehavioursYet()
        {
            AssertPlayerFilesDoNotContain(": MonoBehaviour");
        }

        [Test]
        public void PlayerAssemblyHasOnlyAllowedReferences()
        {
            string path = Path.Combine(
                PlayerRoot,
                "SonsOfTheForest.Gameplay.Player.asmdef");
            string json = File.ReadAllText(path);
            AssemblyDefinition definition =
                JsonUtility.FromJson<AssemblyDefinition>(json);

            Assert.That(definition, Is.Not.Null);
            Assert.That(
                definition.references,
                Is.EquivalentTo(new[] { "SonsOfTheForest.Core" }));
            Assert.That(json, Does.Not.Contain("Tests"));
            Assert.That(json, Does.Not.Contain("InputSystem"));
            Assert.That(json, Does.Not.Contain("Netcode"));
        }

        private static void AssertPlayerFilesDoNotContain(
            params string[] forbiddenText)
        {
            var violations = new List<string>();

            foreach (string file in Directory.EnumerateFiles(
                         PlayerRoot,
                         "*.cs",
                         SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(file);

                foreach (string forbidden in forbiddenText)
                {
                    if (content.IndexOf(
                            forbidden,
                            StringComparison.Ordinal) >= 0)
                    {
                        violations.Add(
                            Path.GetFileName(file) +
                            " contains '" +
                            forbidden +
                            "'.");
                    }
                }
            }

            Assert.That(
                violations,
                Is.Empty,
                string.Join(Environment.NewLine, violations));
        }

        [Serializable]
        private sealed class AssemblyDefinition
        {
            public string[] references = Array.Empty<string>();
        }
    }
}
