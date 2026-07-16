using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SonsOfTheForest.Tests.EditMode.Architecture
{
    public sealed class ArchitectureGuardTests
    {
        private static readonly string GameRoot =
            Path.GetFullPath(Path.Combine(Application.dataPath, "_Game"));

        [Test]
        public void NewCodeDoesNotUseOldNamespace()
        {
            AssertFilesDoNotContain(
                GetFiles(GameRoot, "*.cs"),
                "namespace " + "TheForest",
                "using " + "TheForest");
        }

        [Test]
        public void CoreDoesNotDependOnUnityEngine()
        {
            // No pre-existing Core exception exists as of R0-D.
            string coreRoot = Path.Combine(GameRoot, "Core");

            AssertFilesDoNotContain(
                GetFiles(coreRoot, "*.cs"),
                "using UnityEngine",
                ": MonoBehaviour");
        }

        [Test]
        public void NoGameplayImplementationInFoundationContracts()
        {
            string coreRoot = Path.Combine(GameRoot, "Core");
            string sceneBootstrapRoot = Path.Combine(
                GameRoot,
                "Infrastructure",
                "SceneBootstrap");

            AssertFilesDoNotContain(
                GetFiles(coreRoot, sceneBootstrapRoot, "*.cs"),
                ": MonoBehaviour",
                "FindObjectOfType",
                "FindFirstObjectByType",
                "FindAnyObjectByType",
                "GameObject.Find");
        }

        [Test]
        public void RuntimeAssembliesDoNotReferenceTests()
        {
            AssertFilesDoNotContain(
                GetFiles(GameRoot, "*.asmdef"),
                "SonsOfTheForest.Tests.EditMode");
        }

        private static IEnumerable<string> GetFiles(string root, string searchPattern)
        {
            return GetFiles(new[] { root }, searchPattern);
        }

        private static IEnumerable<string> GetFiles(
            string firstRoot,
            string secondRoot,
            string searchPattern)
        {
            return GetFiles(new[] { firstRoot, secondRoot }, searchPattern);
        }

        private static IEnumerable<string> GetFiles(
            IEnumerable<string> roots,
            string searchPattern)
        {
            foreach (string root in roots)
            {
                foreach (string file in Directory.EnumerateFiles(
                             root,
                             searchPattern,
                             SearchOption.AllDirectories))
                {
                    if (!IsTestPath(file))
                    {
                        yield return file;
                    }
                }
            }
        }

        private static bool IsTestPath(string file)
        {
            string normalized = file.Replace('\\', '/');
            return normalized.IndexOf(
                "/Assets/_Game/Tests/",
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AssertFilesDoNotContain(
            IEnumerable<string> files,
            params string[] forbiddenText)
        {
            var violations = new List<string>();

            foreach (string file in files)
            {
                string content = File.ReadAllText(file);
                foreach (string forbidden in forbiddenText)
                {
                    if (content.IndexOf(forbidden, StringComparison.Ordinal) >= 0)
                    {
                        violations.Add(
                            $"{ToProjectRelativePath(file)} contains '{forbidden}'.");
                    }
                }
            }

            Assert.That(
                violations,
                Is.Empty,
                string.Join(Environment.NewLine, violations));
        }

        private static string ToProjectRelativePath(string file)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
        }
    }
}
