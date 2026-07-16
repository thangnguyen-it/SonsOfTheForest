using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SonsOfTheForest.Tests.Input.EditMode
{
    public sealed class PlayerInputArchitectureGuardTests
    {
        private static readonly string GameRoot = Path.GetFullPath(
            Path.Combine(Application.dataPath, "_Game"));

        private static readonly string PlayerRoot = Path.Combine(
            GameRoot,
            "Gameplay",
            "Player");

        private static readonly string PresentationInputRoot = Path.Combine(
            GameRoot,
            "Presentation",
            "Input");

        [Test]
        public void PresentationInputAssemblyHasOnlyAllowedReferences()
        {
            string path = Path.Combine(
                PresentationInputRoot,
                "SonsOfTheForest.Presentation.Input.asmdef");
            AssemblyDefinition definition = JsonUtility.FromJson<AssemblyDefinition>(
                File.ReadAllText(path));

            Assert.That(definition, Is.Not.Null);
            Assert.That(
                definition.references,
                Is.EquivalentTo(new[]
                {
                    "SonsOfTheForest.Gameplay.Player",
                    "Unity.InputSystem"
                }));
        }

        [Test]
        public void GameplayPlayerStillDoesNotReferenceInputSystem()
        {
            AssertFilesDoNotContain(
                Directory.EnumerateFiles(
                    PlayerRoot,
                    "*.cs",
                    SearchOption.AllDirectories),
                "UnityEngine." + "InputSystem",
                "Unity." + "InputSystem");

            AssertFilesDoNotContain(
                Directory.EnumerateFiles(
                    PlayerRoot,
                    "*.asmdef",
                    SearchOption.TopDirectoryOnly),
                "Unity." + "InputSystem");
        }

        [Test]
        public void InputAdapterDoesNotUseDirectDevicePolling()
        {
            AssertPresentationSourcesDoNotContain(
                "Keyboard." + "current",
                "Mouse." + "current",
                "Gamepad." + "current",
                "InputSystem." + "actions");
        }

        [Test]
        public void InputAdapterDoesNotPerformPlayerRuntimeWork()
        {
            AssertPresentationSourcesDoNotContain(
                "Character" + "Controller",
                "Camera." + "main",
                "transform." + "position",
                "transform." + "rotation",
                "Rigid" + "body",
                "Resources." + "Load",
                "Find" + "ObjectOfType",
                "FindFirst" + "ObjectByType",
                "FindAny" + "ObjectByType",
                "GameObject." + "Find",
                "Survival" + "Stats",
                "IInter" + "actable",
                "Network" + "Object",
                "Unity." + "Netcode");
        }

        [Test]
        public void InputAdapterDoesNotReferenceUnownedActions()
        {
            string source = File.ReadAllText(Path.Combine(
                PresentationInputRoot,
                "PlayerInputAdapter.cs"));
            string[] executableActionLines = source
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line =>
                    line.Contains("FindAction", StringComparison.Ordinal) ||
                    line.Contains(".performed +=", StringComparison.Ordinal) ||
                    line.Contains(".canceled +=", StringComparison.Ordinal))
                .ToArray();

            foreach (string actionName in new[]
                     {
                         "Attack",
                         "Interact",
                         "Previous",
                         "Next"
                     })
            {
                Assert.That(
                    executableActionLines,
                    Has.None.Contains(actionName),
                    $"Executable lookup/subscription references unowned action {actionName}.");
            }
        }

        [Test]
        public void RuntimeAssembliesDoNotReferenceInputTestAssembly()
        {
            IEnumerable<string> runtimeAsmdefs = Directory.EnumerateFiles(
                    GameRoot,
                    "*.asmdef",
                    SearchOption.AllDirectories)
                .Where(path => !IsTestPath(path));

            AssertFilesDoNotContain(
                runtimeAsmdefs,
                "SonsOfTheForest.Tests.Input.EditMode");
        }

        private static bool IsTestPath(string path)
        {
            return path.Replace('\\', '/').Contains(
                "/Assets/_Game/Tests/",
                StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertPresentationSourcesDoNotContain(
            params string[] forbiddenText)
        {
            AssertFilesDoNotContain(
                Directory.EnumerateFiles(
                    PresentationInputRoot,
                    "*.cs",
                    SearchOption.AllDirectories),
                forbiddenText);
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
                    if (content.Contains(forbidden, StringComparison.Ordinal))
                    {
                        violations.Add(
                            $"{Path.GetFileName(file)} contains '{forbidden}'.");
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
