using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace SonsOfTheForest.Tests.ForestCamp.EditMode
{
    public sealed class R2Perf1BenchmarkHarnessTests
    {
        private const string ConfigurationTypeName =
            "SonsOfTheForest.Infrastructure.Benchmark.R2Perf1BenchmarkConfiguration, Assembly-CSharp";
        private const string RunnerTypeName =
            "SonsOfTheForest.Infrastructure.Benchmark.ForestBenchmarkRunner, Assembly-CSharp";
        private const string BuildProvenanceTypeName =
            "SonsOfTheForest.Infrastructure.Editor.ForestModelIntake.R2Perf1BuildProvenance, Assembly-CSharp-Editor";
        private const string RunnerPath =
            "Assets/_Game/Infrastructure/Benchmark/ForestBenchmarkRunner.cs";
        private const string ConfigurationPath =
            "Assets/_Game/Infrastructure/Benchmark/R2Perf1BenchmarkConfiguration.cs";
        private const string BuilderPath =
            "Assets/_Game/Infrastructure/Editor/ForestModelIntake/SotfForestPhase1TrialBuilder.cs";
        private const string OfflineRunnerPath =
            "Tools/Performance/Invoke-R2Perf1Offline.ps1";
        private const string OfflineWrapperPath =
            "Tools/Performance/Invoke-R2Perf1Offline.cmd";
        private const string PairRunnerPath =
            "Tools/Performance/Invoke-R2Perf1Pair.ps1";
        private const string PairWrapperPath =
            "Tools/Performance/Invoke-R2Perf1Pair.cmd";

        private static Type ConfigurationType =>
            Type.GetType(ConfigurationTypeName, true);

        private static Type RunnerType =>
            Type.GetType(RunnerTypeName, true);

        private static Type BuildProvenanceType =>
            Type.GetType(BuildProvenanceTypeName, true);

        [Test]
        public void CommandLineParsing_ProducesOneExplicitMeasurementTuple()
        {
            object settings = Parse(
                "player.exe",
                "-sotf-perf1",
                "-sotf-quality", "Balanced",
                "-sotf-scenario", "ground_only",
                "-sotf-run-id", "balanced_ground_r1",
                "-sotf-build-kind", "release",
                "-sotf-measurement-role", "release_performance",
                "-sotf-no-screenshot", "true",
                "-sotf-aa", "TAA",
                "-sotf-render-scale", "80",
                "-sotf-upscaler", "CatmullRom");

            Assert.That(Field<bool>(settings, "enabled"), Is.True);
            Assert.That(Field<string>(settings, "quality"), Is.EqualTo("Balanced"));
            Assert.That(Field<string>(settings, "scenario"), Is.EqualTo("ground_only"));
            Assert.That(Field<string>(settings, "runId"), Is.EqualTo("balanced_ground_r1"));
            Assert.That(Field<string>(settings, "buildKind"), Is.EqualTo("release"));
            Assert.That(
                Field<string>(settings, "measurementRole"),
                Is.EqualTo("release_performance"));
            Assert.That(Field<bool>(settings, "noScreenshot"), Is.True);
            Assert.That(Field<string>(settings, "antialiasing"), Is.EqualTo("TAA"));
            Assert.That(Field<int>(settings, "renderScalePercent"), Is.EqualTo(80));
            Assert.That(Field<string>(settings, "upscaler"), Is.EqualTo("CatmullRom"));
        }

        [TestCase("None", "None")]
        [TestCase("FXAA", "FastApproximateAntialiasing")]
        [TestCase("SMAA", "SubpixelMorphologicalAntiAliasing")]
        [TestCase("TAA", "TemporalAntialiasing")]
        [TestCase(" taa ", "TemporalAntialiasing")]
        public void AntialiasingContract_MapsCommandTokensExplicitly(
            string commandToken,
            string expectedEnumName)
        {
            MethodInfo method = ConfigurationType.GetMethod(
                "ResolveAntialiasingMode",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            object result = method.Invoke(null, new object[] { commandToken });
            Assert.That(result.ToString(), Is.EqualTo(expectedEnumName));
        }

        [TestCase("TemporalAntialiasing")]
        [TestCase("MSAA")]
        [TestCase("")]
        public void AntialiasingContract_RejectsValuesOutsideCommandContract(string value)
        {
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-aa", value);
        }

        [Test]
        public void OfflineRunnerSupportedAaSet_HasRuntimeMappingForEveryValue()
        {
            string script = File.ReadAllText(OfflineRunnerPath);
            Match match = Regex.Match(
                script,
                "\\[ValidateSet\\(\"None\",\\s*\"FXAA\",\\s*\"SMAA\",\\s*\"TAA\"\\)\\]");
            Assert.That(match.Success, Is.True, "PowerShell AA ValidateSet changed unexpectedly.");

            string[] supported = ((System.Collections.IEnumerable)ConfigurationType
                .GetProperty("SupportedAntialiasingNames", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null)).Cast<string>().ToArray();
            Assert.That(supported, Is.EquivalentTo(new[] { "None", "FXAA", "SMAA", "TAA" }));
            foreach (string value in supported)
            {
                Assert.DoesNotThrow(() => Parse(
                    "player.exe", "-sotf-perf1",
                    "-sotf-measurement-role", "development_gc",
                    "-sotf-aa", value));
            }
        }

        [Test]
        public void QualityWithSpaces_RemainsOneCommandLineValue()
        {
            object settings = Parse(
                "player.exe",
                "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-quality", "High Fidelity",
                "-sotf-aa", "TAA");

            Assert.That(Field<string>(settings, "quality"), Is.EqualTo("High Fidelity"));
            Assert.That(Field<string>(settings, "antialiasing"), Is.EqualTo("TAA"));
        }

        [TestCase("release_performance")]
        [TestCase("development_gc")]
        public void MeasurementRoleContract_AcceptsExactlyTheTwoAuthorityTokens(string role)
        {
            string buildKind = role == "release_performance" ? "release" : "diagnostic";
            object settings = Parse(
                "player.exe", "-sotf-perf1",
                "-sotf-build-kind", buildKind,
                "-sotf-measurement-role", role);

            Assert.That(Field<string>(settings, "measurementRole"), Is.EqualTo(role));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("RELEASE_PERFORMANCE")]
        [TestCase("performance")]
        public void MeasurementRoleContract_RejectsMissingUnknownOrAliasedTokens(string role)
        {
            if (role == null)
            {
                AssertParseFailure<ArgumentException>("player.exe", "-sotf-perf1");
                return;
            }

            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", role);
        }

        [Test]
        public void UnknownMeasurementRole_UsesSafeRecoveryEnvelopeAndWritesOnlyInvalidManifest()
        {
            string cleanupRoot;
            string runDirectory = CreateLongRunDirectory(
                out cleanupRoot,
                "unknown_role_recovery");
            string[] arguments =
            {
                "player.exe",
                "-sotf-perf1",
                "-sotf-run-id", "unknown_role_recovery",
                "-sotf-measurement-role", "unsupported_role",
                "-sotf-output-directory", runDirectory,
            };
            try
            {
                object envelope = InvokeConfiguration(
                    "TryCreateRecoveryEnvelope",
                    new object[] { arguments });
                Assert.That(envelope, Is.Not.Null);
                AssertParseFailure<ArgumentException>(arguments);

                MethodInfo recover = RunnerType.GetMethod(
                    "RecoverFromStartupFailureWithEnvelope",
                    BindingFlags.Public | BindingFlags.Static);
                Assert.That(recover, Is.Not.Null);
                bool restoreCalled = false;
                var restore = new Func<bool>(() =>
                {
                    restoreCalled = true;
                    return true;
                });
                object[] parameters =
                {
                    envelope,
                    new ArgumentException("unsupported measurement role"),
                    restore,
                    null,
                };

                int exitCode = (int)recover.Invoke(null, parameters);
                string manifestPath = (string)parameters[3];

                Assert.That(exitCode, Is.EqualTo(2));
                Assert.That(restoreCalled, Is.True);
                Assert.That(Path.GetFileName(manifestPath), Is.EqualTo("runtime.manifest.json"));
                Assert.That(File.Exists(manifestPath), Is.True);
                string manifest = File.ReadAllText(manifestPath);
                Assert.That(manifest, Does.Contain("\"status\": \"invalid\""));
                Assert.That(manifest, Does.Contain("unsupported measurement role"));
                Assert.That(manifest, Does.Contain("\"reportWritten\": false"));
                Assert.That(File.Exists(Path.Combine(runDirectory, "runtime.report.json")), Is.False);
                Assert.That(File.Exists(Path.Combine(runDirectory, "runtime.report.md")), Is.False);
            }
            finally
            {
                if (Directory.Exists(cleanupRoot))
                {
                    Directory.Delete(cleanupRoot, true);
                }
            }
        }

        [Test]
        public void RecoveryEnvelope_RejectsTraversalBeforeAnyOutputIsWritten()
        {
            string runDirectory = Path.Combine(
                Path.GetTempPath(),
                "sotf-r2-perf1-unsafe-recovery-" + Guid.NewGuid().ToString("N"));
            string[] arguments =
            {
                "player.exe",
                "-sotf-perf1",
                "-sotf-run-id", "../escaped_run",
                "-sotf-output-directory", runDirectory,
            };

            object envelope = InvokeConfiguration(
                "TryCreateRecoveryEnvelope",
                new object[] { arguments });

            Assert.That(envelope, Is.Null);
            Assert.That(Directory.Exists(runDirectory), Is.False);
            Assert.That(File.Exists(Path.Combine(runDirectory, "runtime.manifest.json")), Is.False);
        }

        [Test]
        public void RecoveryEnvelope_RejectsFilesystemRootAsUnsafeOutputDirectory()
        {
            string filesystemRoot = Path.GetPathRoot(Path.GetTempPath());
            string[] arguments =
            {
                "player.exe",
                "-sotf-perf1",
                "-sotf-run-id", "unsafe_output_root",
                "-sotf-output-directory", filesystemRoot,
            };

            object envelope = InvokeConfiguration(
                "TryCreateRecoveryEnvelope",
                new object[] { arguments });

            Assert.That(envelope, Is.Null);
        }

        [Test]
        public void RecoveryEnvelope_RejectsDirectoryLeafThatDoesNotExactlyMatchRunId()
        {
            string cleanupRoot;
            string runDirectory = CreateLongRunDirectory(out cleanupRoot, "different_run");
            try
            {
                object envelope = InvokeConfiguration(
                    "TryCreateRecoveryEnvelope",
                    new object[]
                    {
                        new[]
                        {
                            "player.exe",
                            "-sotf-perf1",
                            "-sotf-run-id", "expected_run",
                            "-sotf-output-directory", runDirectory,
                        },
                    });

                Assert.That(envelope, Is.Null);
                Assert.That(Directory.Exists(runDirectory), Is.False);
            }
            finally
            {
                if (Directory.Exists(cleanupRoot))
                {
                    Directory.Delete(cleanupRoot, true);
                }
            }
        }

        [TestCase("runtime.manifest.json")]
        [TestCase("runtime.report.json")]
        [TestCase("runtime.report.md")]
        [TestCase("runtime.manifest.json.tmp")]
        [TestCase("runtime.report.json.tmp")]
        [TestCase("runtime.report.md.tmp")]
        [TestCase("runtime.screenshot.png")]
        [TestCase("runtime.screenshot.png.tmp")]
        public void RecoveryEnvelope_PreservesExistingForeignRuntimeOutputByteForByte(
            string fileName)
        {
            const string runId = "foreign_evidence_run";
            string cleanupRoot;
            string runDirectory = CreateLongRunDirectory(out cleanupRoot, runId);
            byte[] foreignBytes = { 0x00, 0x25, 0x7f, 0x80, 0xff };
            try
            {
                Directory.CreateDirectory(runDirectory);
                string foreignPath = Path.Combine(runDirectory, fileName);
                File.WriteAllBytes(foreignPath, foreignBytes);

                object envelope = InvokeConfiguration(
                    "TryCreateRecoveryEnvelope",
                    new object[]
                    {
                        RecoveryArguments(runId, runDirectory),
                    });

                Assert.That(envelope, Is.Null);
                Assert.That(File.ReadAllBytes(foreignPath), Is.EqualTo(foreignBytes));
                Assert.That(
                    Directory.GetFiles(runDirectory).Select(Path.GetFileName).ToArray(),
                    Is.EquivalentTo(new[] { fileName }));
            }
            finally
            {
                if (Directory.Exists(cleanupRoot))
                {
                    Directory.Delete(cleanupRoot, true);
                }
            }
        }

        [TestCase("runtime.manifest.json")]
        [TestCase("runtime.manifest.json.tmp")]
        public void RecoveryWriter_FailsClosedWhenTargetAppearsAfterEnvelopeValidation(
            string racedFileName)
        {
            const string runId = "recovery_race_run";
            string cleanupRoot;
            string runDirectory = CreateLongRunDirectory(out cleanupRoot, runId);
            byte[] foreignBytes = { 0xde, 0xad, 0xbe, 0xef };
            try
            {
                object envelope = InvokeConfiguration(
                    "TryCreateRecoveryEnvelope",
                    new object[] { RecoveryArguments(runId, runDirectory) });
                Assert.That(envelope, Is.Not.Null);

                Directory.CreateDirectory(runDirectory);
                string manifestPath = Path.Combine(runDirectory, "runtime.manifest.json");
                string racedPath = Path.Combine(runDirectory, racedFileName);
                File.WriteAllBytes(racedPath, foreignBytes);

                UnityEngine.TestTools.LogAssert.Expect(
                    UnityEngine.LogType.Error,
                    new Regex(
                        "Failed to persist startup-failure manifest:.*" +
                        Regex.Escape(racedFileName),
                        RegexOptions.Singleline));
                int exitCode = RecoverWithEnvelope(envelope, out string writtenManifestPath);

                Assert.That(exitCode, Is.EqualTo(2));
                Assert.That(writtenManifestPath, Is.Null);
                Assert.That(File.ReadAllBytes(racedPath), Is.EqualTo(foreignBytes));
                Assert.That(
                    File.Exists(manifestPath),
                    Is.EqualTo(string.Equals(
                        racedFileName,
                        "runtime.manifest.json",
                        StringComparison.Ordinal)));
                Assert.That(File.Exists(Path.Combine(runDirectory, "runtime.report.json")), Is.False);
                Assert.That(File.Exists(Path.Combine(runDirectory, "runtime.report.md")), Is.False);
            }
            finally
            {
                if (Directory.Exists(cleanupRoot))
                {
                    Directory.Delete(cleanupRoot, true);
                }
            }
        }

        [TestCase("release_performance", "diagnostic")]
        [TestCase("development_gc", "release")]
        public void MeasurementRoleContract_RejectsDeclaredRoleBuildMismatch(
            string role,
            string buildKind)
        {
            AssertParseFailure<InvalidOperationException>(
                "player.exe", "-sotf-perf1",
                "-sotf-build-kind", buildKind,
                "-sotf-measurement-role", role);
        }

        [TestCase("release_performance", "release", false)]
        [TestCase("development_gc", "diagnostic", true)]
        public void MeasurementAuthority_AcceptsMatchingRuntimeBuild(
            string role,
            string buildKind,
            bool developmentBuild)
        {
            Assert.DoesNotThrow(() => InvokeConfiguration(
                "ValidateMeasurementAuthority",
                role,
                buildKind,
                developmentBuild));
        }

        [TestCase("release_performance", "release", true)]
        [TestCase("development_gc", "diagnostic", false)]
        public void MeasurementAuthority_RejectsActualRuntimeBuildMismatch(
            string role,
            string buildKind,
            bool developmentBuild)
        {
            AssertConfigurationFailure<InvalidOperationException>(
                "ValidateMeasurementAuthority",
                role,
                buildKind,
                developmentBuild);
        }

        [Test]
        public void RuntimeOutputContract_UsesBoundedFixedNamesWithoutRepeatedConfiguration()
        {
            string manifest = StaticField<string>(ConfigurationType, "RuntimeManifestFileName");
            string reportJson = StaticField<string>(ConfigurationType, "RuntimeReportJsonFileName");
            string reportMarkdown =
                StaticField<string>(ConfigurationType, "RuntimeReportMarkdownFileName");
            string screenshot = StaticField<string>(ConfigurationType, "RuntimeScreenshotFileName");
            string temporarySuffix = StaticField<string>(ConfigurationType, "AtomicTemporarySuffix");
            int maximumLength =
                StaticField<int>(ConfigurationType, "MaximumRuntimeOutputFileNameLength");

            Assert.That(manifest, Is.EqualTo("runtime.manifest.json"));
            Assert.That(reportJson, Is.EqualTo("runtime.report.json"));
            Assert.That(reportMarkdown, Is.EqualTo("runtime.report.md"));
            Assert.That(screenshot, Is.EqualTo("runtime.screenshot.png"));
            Assert.That(manifest + temporarySuffix, Is.EqualTo("runtime.manifest.json.tmp"));
            Assert.That(reportJson + temporarySuffix, Is.EqualTo("runtime.report.json.tmp"));
            foreach (string fileName in new[] { manifest, reportJson, reportMarkdown, screenshot })
            {
                Assert.That(fileName.Length, Is.LessThanOrEqualTo(maximumLength));
                Assert.That(fileName, Does.Not.Contain("High_Fidelity"));
                Assert.That(fileName, Does.Not.Contain("empty_hdrp_camera"));
                Assert.That(fileName, Does.Not.Contain("runId"));
            }
        }

        [Test]
        public void AtomicRuntimeWrite_CreatesLongValidatedParentAndLeavesNoTemporaryFile()
        {
            string cleanupRoot;
            string runDirectory = CreateLongRunDirectory(out cleanupRoot);
            try
            {
                Assert.That(runDirectory.Length, Is.GreaterThanOrEqualTo(128));
                string reportName =
                    StaticField<string>(ConfigurationType, "RuntimeReportJsonFileName");
                string reportPath = (string)InvokeConfiguration(
                    "ResolveContainedOutputPath",
                    runDirectory,
                    reportName);
                Assert.That(Directory.Exists(runDirectory), Is.False);

                MethodInfo writeAtomic = RunnerType.GetMethod(
                    "WriteAllTextAtomic",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(writeAtomic, Is.Not.Null);
                writeAtomic.Invoke(null, new object[] { runDirectory, reportPath, "{\"ok\":true}" });

                Assert.That(Directory.Exists(runDirectory), Is.True);
                Assert.That(File.ReadAllText(reportPath), Is.EqualTo("{\"ok\":true}"));
                Assert.That(File.Exists(reportPath + ".tmp"), Is.False);
            }
            finally
            {
                if (Directory.Exists(cleanupRoot))
                {
                    Directory.Delete(cleanupRoot, true);
                }
            }
        }

        [Test]
        public void RuntimeOutputContainment_RejectsTraversalAndEscapedAbsolutePath()
        {
            string runDirectory = Path.Combine(
                Path.GetTempPath(),
                "sotf-r2-perf1-containment-" + Guid.NewGuid().ToString("N"));
            string escapedPath = Path.Combine(
                Directory.GetParent(runDirectory).FullName,
                "escaped-runtime.report.json");

            AssertConfigurationFailure<ArgumentException>(
                "ResolveContainedOutputPath",
                runDirectory,
                Path.Combine("..", "escaped.json"));
            AssertConfigurationFailure<ArgumentException>(
                "ValidateContainedOutputPath",
                runDirectory,
                escapedPath);
        }

        [Test]
        public void ValidLifecycleManifest_ReferencesOnlyShortContainedRuntimeOutputs()
        {
            string cleanupRoot;
            string runDirectory = CreateLongRunDirectory(out cleanupRoot);
            try
            {
                object settings = Parse(
                    "player.exe",
                    "-sotf-perf1",
                    "-sotf-quality", "High Fidelity",
                    "-sotf-scenario", "empty_hdrp_camera",
                    "-sotf-run-id", "unique_configuration_that_must_not_be_in_filenames",
                    "-sotf-build-kind", "release",
                    "-sotf-measurement-role", "release_performance",
                    "-sotf-aa", "TAA",
                    "-sotf-output-directory", runDirectory);
                MethodInfo createManifest = RunnerType.GetMethod(
                    "CreateManifest",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(createManifest, Is.Not.Null);
                object manifest = createManifest.Invoke(
                    null,
                    new[] { settings, Property<string>(settings, "Phase") });

                string reportJsonPath = Field<string>(manifest, "reportJsonPath");
                string reportMarkdownPath = Field<string>(manifest, "reportMarkdownPath");
                Assert.That(Path.GetFileName(reportJsonPath), Is.EqualTo("runtime.report.json"));
                Assert.That(Path.GetFileName(reportMarkdownPath), Is.EqualTo("runtime.report.md"));
                Assert.That(reportJsonPath, Does.StartWith(runDirectory));
                Assert.That(reportMarkdownPath, Does.StartWith(runDirectory));
                Assert.That(reportJsonPath, Does.Not.Contain(Field<string>(settings, "runId")));
                Assert.That(
                    Field<string>(manifest, "schemaVersion"),
                    Is.EqualTo("r2-perf1-manifest/2"));
                Assert.That(
                    Field<string>(manifest, "measurementRole"),
                    Is.EqualTo("release_performance"));
                Assert.That(Field<bool>(manifest, "pairingEligible"), Is.False);
                Assert.That(
                    Field<string>(manifest, "evidenceValidity"),
                    Is.EqualTo("PENDING_OFFLINE_VALIDATION"));
                Assert.That(
                    Field<string>(manifest, "aggregateProductGate"),
                    Is.EqualTo("INCOMPLETE"));
                Assert.That(Field<string>(manifest, "measurementSetId"), Is.EqualTo("test_measurement_set"));
                Assert.That(Field<string>(manifest, "sourceCommit"), Is.EqualTo("EA1086D2887C2B3F985F3376520BA9E99339AD58"));
                Assert.That(Field<bool>(manifest, "sourceTreeCleanAvailable"), Is.True);
                Assert.That(Field<string>(manifest, "buildArtifactId"), Is.EqualTo(new string('A', 64)));
                Assert.That(Field<string>(manifest, "contentFingerprint"), Is.EqualTo(new string('B', 64)));
                Assert.That(Field<string>(manifest, "configurationFingerprint"), Is.EqualTo(new string('C', 64)));
                Assert.That(Field<string>(manifest, "hardwareFingerprint"), Is.EqualTo(new string('D', 64)));
            }
            finally
            {
                if (Directory.Exists(cleanupRoot))
                {
                    Directory.Delete(cleanupRoot, true);
                }
            }
        }

        [TestCase("empty_hdrp_camera")]
        [TestCase("ground_only")]
        [TestCase("full_forest")]
        public void ScenarioValidation_AcceptsOnlySupportedScenarios(string scenario)
        {
            object settings = Parse(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-scenario", scenario);
            Assert.That(Field<string>(settings, "scenario"), Is.EqualTo(scenario));
        }

        [TestCase("all")]
        [TestCase("forest_and_ground")]
        [TestCase("")]
        public void ScenarioValidation_RejectsInvalidValues(string scenario)
        {
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-scenario", scenario);
        }

        [TestCase("diagnostic", true)]
        [TestCase("release", false)]
        public void BuildKindValidation_AcceptsMatchingRuntime(
            string buildKind,
            bool developmentBuild)
        {
            MethodInfo method = ConfigurationType.GetMethod(
                "ValidateRuntimeBuildKind",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { buildKind, developmentBuild }));
        }

        [TestCase("release", true)]
        [TestCase("diagnostic", false)]
        public void BuildKindValidation_RejectsMismatchedRuntime(
            string buildKind,
            bool developmentBuild)
        {
            MethodInfo method = ConfigurationType.GetMethod(
                "ValidateRuntimeBuildKind",
                BindingFlags.Public | BindingFlags.Static);
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => method.Invoke(null, new object[] { buildKind, developmentBuild }));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        }

        [TestCase("bad kind")]
        [TestCase("profile")]
        public void BuildKindParsing_RejectsUnknownKinds(string buildKind)
        {
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1", "-sotf-build-kind", buildKind);
        }

        [TestCase("valid-run_01")]
        [TestCase("R2PERF1")]
        public void RunIdValidation_AcceptsFileSafeUniqueIds(string runId)
        {
            object settings = Parse(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-run-id", runId);
            Assert.That(Field<string>(settings, "runId"), Is.EqualTo(runId));
        }

        [TestCase("contains space")]
        [TestCase("path/escape")]
        [TestCase("dot.value")]
        public void RunIdValidation_RejectsUnsafeIds(string runId)
        {
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-run-id", runId);
        }

        [Test]
        public void Screenshot_DefaultsOffAndCanBeEnabledOnlyExplicitly()
        {
            object defaultSettings = Parse(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc");
            object visualSettings = Parse(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-no-screenshot", "false");

            Assert.That(Field<bool>(defaultSettings, "noScreenshot"), Is.True);
            Assert.That(Property<bool>(defaultSettings, "CaptureScreenshot"), Is.False);
            Assert.That(Property<bool>(defaultSettings, "MeasurementEligible"), Is.True);
            Assert.That(Field<bool>(visualSettings, "noScreenshot"), Is.False);
            Assert.That(Property<bool>(visualSettings, "CaptureScreenshot"), Is.True);
            Assert.That(Property<bool>(visualSettings, "MeasurementEligible"), Is.False);
        }

        [Test]
        public void InvalidConfiguration_IsRejectedBeforeRuntimeMutation()
        {
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-render-scale", "101");
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-unknown", "value");
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-no-screenshot", "maybe");
        }

        [Test]
        public void RuntimeValidation_PrecedesCameraAndRuntimeMutation()
        {
            string runner = File.ReadAllText(RunnerPath);
            string configuration = File.ReadAllText(ConfigurationPath);

            Assert.That(
                runner.IndexOf("ValidateRuntimeSettings", StringComparison.Ordinal),
                Is.LessThan(runner.IndexOf("FindObjectsByType<Camera>", StringComparison.Ordinal)));
            Assert.That(
                configuration.IndexOf("ValidateRuntimeSettings(Current", StringComparison.Ordinal),
                Is.LessThan(configuration.IndexOf("CaptureRuntimeState();", StringComparison.Ordinal)));
            Assert.That(
                configuration.IndexOf("ResolveAntialiasingMode(Current.antialiasing)", StringComparison.Ordinal),
                Is.LessThan(configuration.IndexOf("camera.gameObject.AddComponent", StringComparison.Ordinal)));
        }

        [Test]
        public void StartupFailure_RestoresStateWritesInvalidManifestAndRequestsNonZeroExit()
        {
            string cleanupRoot;
            string temporaryDirectory = CreateLongRunDirectory(
                out cleanupRoot,
                "startup_failure_test");
            try
            {
                object settings = Parse(
                    "player.exe",
                    "-sotf-perf1",
                    "-sotf-quality", "High Fidelity",
                    "-sotf-scenario", "empty_hdrp_camera",
                    "-sotf-run-id", "startup_failure_test",
                    "-sotf-build-kind", "release",
                    "-sotf-measurement-role", "release_performance",
                    "-sotf-no-screenshot", "true",
                    "-sotf-aa", "TAA",
                    "-sotf-output-directory", temporaryDirectory);
                bool restoreCalled = false;
                var restore = new Func<bool>(() =>
                {
                    restoreCalled = true;
                    return true;
                });
                MethodInfo recover = RunnerType.GetMethod(
                    "RecoverFromStartupFailure",
                    BindingFlags.Public | BindingFlags.Static);
                Assert.That(recover, Is.Not.Null);
                object[] parameters =
                {
                    settings,
                    new InvalidOperationException("synthetic startup failure"),
                    restore,
                    null,
                };

                int exitCode = (int)recover.Invoke(null, parameters);
                string manifestPath = (string)parameters[3];

                Assert.That(restoreCalled, Is.True);
                Assert.That(exitCode, Is.EqualTo(2));
                Assert.That(File.Exists(manifestPath), Is.True);
                Assert.That(Path.GetFileName(manifestPath), Is.EqualTo("runtime.manifest.json"));
                string manifest = File.ReadAllText(manifestPath);
                Assert.That(manifest, Does.Contain("\"status\": \"invalid\""));
                Assert.That(manifest, Does.Contain("synthetic startup failure"));
                Assert.That(manifest, Does.Contain("\"runtimeStateRestored\": true"));
                Assert.That(manifest, Does.Contain("\"reportWritten\": false"));
                Assert.That(
                    Directory.GetFiles(temporaryDirectory, "*.json")
                        .Where(path => !path.EndsWith(".manifest.json", StringComparison.Ordinal))
                        .ToArray(),
                    Is.Empty,
                    "Startup failure must not create a performance report.");
                Assert.That(Directory.GetFiles(temporaryDirectory, "*.md"), Is.Empty);
            }
            finally
            {
                if (Directory.Exists(cleanupRoot))
                {
                    Directory.Delete(cleanupRoot, true);
                }
            }
        }

        [TestCase("awaiting_offline_validation", 0, true, true, true)]
        [TestCase("incomplete", 0, true, true, false)]
        [TestCase("awaiting_offline_validation", 1, true, true, false)]
        [TestCase("awaiting_offline_validation", 0, false, true, false)]
        [TestCase("awaiting_offline_validation", 0, true, false, false)]
        public void ManifestValidityTransition_RequiresNormalVerifiedOfflineCompletion(
            string status,
            int exitCode,
            bool outputsVerified,
            bool telemetryValid,
            bool expected)
        {
            MethodInfo method = ConfigurationType.GetMethod(
                "CanMarkManifestValid",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            bool actual = (bool)method.Invoke(
                null,
                new object[] { status, exitCode, outputsVerified, telemetryValid });
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void RuntimeHarness_UsesSingleScenarioManifestAndReversibleState()
        {
            string runner = File.ReadAllText(RunnerPath);
            string configuration = File.ReadAllText(ConfigurationPath);

            Assert.That(runner, Does.Contain("RunSelectedPerf1Scenario"));
            Assert.That(runner, Does.Contain("ManifestIncomplete"));
            Assert.That(runner, Does.Contain("ManifestAwaitingValidation"));
            Assert.That(runner, Does.Contain("finally"));
            Assert.That(runner, Does.Not.Contain("Shader.WarmupAllShaders"));
            Assert.That(runner, Does.Contain("double.IsNaN"));
            Assert.That(runner, Does.Contain("double.IsInfinity"));
            Assert.That(runner, Does.Contain("preallocated frame capacity"));
            Assert.That(configuration, Does.Contain("CaptureRuntimeState"));
            Assert.That(configuration, Does.Contain("RestoreRuntimeState"));
            Assert.That(configuration, Does.Contain("Application.quitting"));
            Assert.That(configuration, Does.Contain("ScalableBufferManager.ResizeBuffers"));
            Assert.That(runner, Does.Not.Contain("GC.GetAllocatedBytesForCurrentThread"));
            Assert.That(configuration, Does.Not.Contain("GC.GetAllocatedBytesForCurrentThread"));
        }

        [Test]
        public void RuntimeMarkdownScenarioTable_ProductionFormatterCreatesExactlyNineteenColumns()
        {
            string header = StaticField<string>(RunnerType, "ScenarioMarkdownHeader");
            string separator = StaticField<string>(RunnerType, "ScenarioMarkdownSeparator");
            Type scenarioType = RunnerType.GetNestedType("ScenarioResult", BindingFlags.Public);
            Assert.That(scenarioType, Is.Not.Null);
            object scenario = Activator.CreateInstance(scenarioType);
            scenarioType.GetField("name").SetValue(scenario, "representative");
            scenarioType.GetField("performanceBudgetStatus").SetValue(scenario, "PASS");
            scenarioType.GetField("gcBudgetStatus").SetValue(scenario, "NOT_AUTHORITY");
            scenarioType.GetField("bottleneck").SetValue(scenario, "GPU");
            scenarioType.GetField("drawCallsAvailable").SetValue(scenario, true);
            scenarioType.GetField("batchesAvailable").SetValue(scenario, true);
            scenarioType.GetField("setPassCallsAvailable").SetValue(scenario, true);
            scenarioType.GetField("trianglesAvailable").SetValue(scenario, true);
            MethodInfo formatter = RunnerType.GetMethod(
                "FormatScenarioMarkdownRow",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(formatter, Is.Not.Null);
            string dataRow = (string)formatter.Invoke(null, new[] { scenario });

            int headerColumns = header.Split('|').Length - 2;
            int separatorColumns = separator.Split('|').Length - 2;
            int dataColumns = dataRow.Split('|').Length - 2;
            Assert.That(headerColumns, Is.EqualTo(19));
            Assert.That(separatorColumns, Is.EqualTo(headerColumns));
            Assert.That(dataColumns, Is.EqualTo(headerColumns));
        }

        [TestCase("release_performance", false, 120f, 10f, 15f, "PASS", "NOT_AUTHORITY")]
        [TestCase("release_performance", false, 40f, 21f, 26f, "FAIL", "NOT_AUTHORITY")]
        [TestCase("development_gc", false, 120f, 10f, 15f, "NOT_AUTHORITY", "INCOMPLETE")]
        [TestCase("development_gc", true, 120f, 10f, 15f, "NOT_AUTHORITY", "PASS")]
        public void MemberBudgetMatrix_SeparatesAuthorityAndKeepsPairIncomplete(
            string role,
            bool globalGcAvailable,
            float averageFps,
            float p95Ms,
            float p99Ms,
            string expectedPerformance,
            string expectedGc)
        {
            Type scenarioType = RunnerType.GetNestedType(
                "ScenarioResult",
                BindingFlags.Public);
            Assert.That(scenarioType, Is.Not.Null);
            object scenario = Activator.CreateInstance(scenarioType);
            scenarioType.GetField("avgFps").SetValue(scenario, averageFps);
            scenarioType.GetField("p95Ms").SetValue(scenario, p95Ms);
            scenarioType.GetField("p99Ms").SetValue(scenario, p99Ms);
            scenarioType.GetField("globalGcAllocationAvailable").SetValue(
                scenario,
                globalGcAvailable);
            scenarioType.GetField("globalGcAllocatedPeakBytes").SetValue(scenario, 0L);

            MethodInfo evaluate = RunnerType.GetMethod(
                "EvaluateBudget",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(evaluate, Is.Not.Null);
            evaluate.Invoke(null, new[] { scenario, role });

            Assert.That(
                Field<string>(scenario, "performanceBudgetStatus"),
                Is.EqualTo(expectedPerformance));
            Assert.That(Field<string>(scenario, "gcBudgetStatus"), Is.EqualTo(expectedGc));
            Assert.That(
                StaticField<string>(ConfigurationType, "AggregateProductGateIncomplete"),
                Is.EqualTo("INCOMPLETE"));
        }

        [Test]
        public void Builder_SeparatesDiagnosticAndNonDevelopmentReleaseOutputs()
        {
            string builder = File.ReadAllText(BuilderPath);

            Assert.That(builder, Does.Contain("R2_PERF1_Diagnostic"));
            Assert.That(builder, Does.Contain("R2_PERF1_Release"));
            Assert.That(builder, Does.Contain("SOTF_R2_PERF1.exe"));
            Assert.That(builder, Does.Contain("BuildOptions.Development"));
            Assert.That(builder, Does.Contain("BuildOptions.None"));
            Assert.That(builder, Does.Contain("provenance builds must never auto-run"));
            Assert.That(builder, Does.Contain("BuildOptions.ConnectWithProfiler"));
            Assert.That(builder, Does.Contain("BuildOptions.EnableDeepProfilingSupport"));
            Assert.That(builder, Does.Contain("ValidateBuildRoleContract"));
            Assert.That(builder, Does.Contain("WriteCompletedBuildProvenance"));
            Assert.That(builder, Does.Contain("report.summary.options"));
            Assert.That(builder, Does.Contain("GetGraphicsAPIs"));
            Assert.That(builder, Does.Contain("GetArchitecture"));
            Assert.That(builder, Does.Contain("ComputeSha256"));
            Assert.That(PlayerSettings.enableFrameTimingStats, Is.True);
        }

        [Test]
        public void BuildProvenance_FrameTimingRequirementFailsClosedWithoutMutatingProjectSettings()
        {
            bool before = PlayerSettings.enableFrameTimingStats;
            Assert.That(before, Is.True, "The committed project contract must enable frame timing.");
            Assert.DoesNotThrow(() => InvokeBuildProvenance(
                "ValidateFrameTimingStatsEnabled",
                true));

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                InvokeBuildProvenance("ValidateFrameTimingStatsEnabled", false));

            Assert.That(exception.InnerException.Message, Does.Contain("frame_timing_disabled"));
            Assert.That(PlayerSettings.enableFrameTimingStats, Is.EqualTo(before));
        }

        [Test]
        public void BuildProvenance_ContentFingerprintIsStableAcrossDependencyOrder()
        {
            string root = CreateProvenanceTempRoot();
            try
            {
                object first = CreateFileDependency(
                    root,
                    "Assets/Benchmark/First.asset",
                    "first-content",
                    "first-meta",
                    "guid-first",
                    "hash-first");
                object second = CreateFileDependency(
                    root,
                    "Assets/Benchmark/Second.asset",
                    "second-content",
                    "second-meta",
                    "guid-second",
                    "hash-second");

                string ordered = ComputeContentFingerprint(first, second);
                string reversed = ComputeContentFingerprint(second, first);
                string repeated = ComputeContentFingerprint(first, second);

                Assert.That(reversed, Is.EqualTo(ordered));
                Assert.That(repeated, Is.EqualTo(ordered));
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_AssetAndMetaChangesAlterContentFingerprint()
        {
            string root = CreateProvenanceTempRoot();
            try
            {
                const string path = "Assets/Benchmark/Mutable.asset";
                object baseline = CreateFileDependency(
                    root,
                    path,
                    "asset-v1",
                    "meta-v1",
                    "guid-mutable",
                    "import-v1");
                string baselineFingerprint = ComputeContentFingerprint(baseline);

                object assetChanged = CreateFileDependency(
                    root,
                    path,
                    "asset-v2",
                    "meta-v1",
                    "guid-mutable",
                    "import-v1");
                object metaChanged = CreateFileDependency(
                    root,
                    path,
                    "asset-v1",
                    "meta-v2",
                    "guid-mutable",
                    "import-v2");

                Assert.That(
                    ComputeContentFingerprint(assetChanged),
                    Is.Not.EqualTo(baselineFingerprint));
                Assert.That(
                    ComputeContentFingerprint(metaChanged),
                    Is.Not.EqualTo(baselineFingerprint));
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_CanonicalContentNeverContainsAbsoluteWorkspacePath()
        {
            string root = CreateProvenanceTempRoot();
            try
            {
                object dependency = CreateFileDependency(
                    root,
                    "Assets/Benchmark/Canonical.asset",
                    "content",
                    "meta",
                    "guid-canonical",
                    "hash-canonical");
                string canonical = (string)InvokeBuildProvenance(
                    "CanonicalizeContentDependencies",
                    CreateDependencyArray(dependency));

                Assert.That(canonical, Does.Contain("Assets/Benchmark/Canonical.asset"));
                Assert.That(canonical, Does.Not.Contain(Path.GetFullPath(root)));
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_IgnoredBenchmarkContentIsNotRepositoryReproducible()
        {
            string root = CreateProvenanceTempRoot();
            try
            {
                object dependency = CreateFileDependency(
                    root,
                    "Assets/_LocalTrials/Benchmark.unity",
                    "scene",
                    "scene-meta",
                    "scene-guid",
                    "scene-import");
                string status = (string)InvokeBuildProvenance(
                    "DetermineContentTrackingStatus",
                    false,
                    true);
                bool reproducible = (bool)InvokeBuildProvenance(
                    "DetermineRepositoryReproducibility",
                    false,
                    CreateDependencyArray(dependency));

                Assert.That(status, Is.EqualTo("local_ignored_content"));
                Assert.That(reproducible, Is.False);
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_MissingRequiredDependencyFailsClosed()
        {
            string missing = Path.Combine(
                Path.GetTempPath(),
                "sotf-missing-provenance-" + Guid.NewGuid().ToString("N"),
                "missing.asset");
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                InvokeBuildProvenance(
                    "CreateFileDependencyRecord",
                    "Assets/Benchmark/Missing.asset",
                    missing,
                    "guid-missing",
                    "hash-missing",
                    "project_asset",
                    "tracked",
                    string.Empty));

            Assert.That(exception.InnerException.Message, Does.Contain("dependency_unhashable"));
        }

        [Test]
        public void BuildProvenance_ReleaseAndDiagnosticUseSeparateSidecarsAndExactFlags()
        {
            string root = CreateProvenanceTempRoot();
            string releaseDirectory = Path.Combine(root, "R2_PERF1_Release");
            string diagnosticDirectory = Path.Combine(root, "R2_PERF1_Diagnostic");
            string releasePath = (string)InvokeBuildProvenance(
                "GetSidecarPath",
                releaseDirectory);
            string diagnosticPath = (string)InvokeBuildProvenance(
                "GetSidecarPath",
                diagnosticDirectory);
            object none = BuildOptionsValue("None");
            object development = BuildOptionsValue("Development");

            Assert.That(releasePath, Is.Not.EqualTo(diagnosticPath));
            Assert.That(Path.GetFileName(releasePath), Is.EqualTo("r2-perf1.build-provenance.json"));
            Assert.DoesNotThrow(() => InvokeBuildProvenance(
                "ValidateBuildRoleContract",
                "release_performance",
                "release",
                none));
            Assert.DoesNotThrow(() => InvokeBuildProvenance(
                "ValidateBuildRoleContract",
                "development_gc",
                "diagnostic",
                development));
            DeleteDirectory(root);
        }

        [TestCase("AutoRunPlayer")]
        [TestCase("ConnectWithProfiler")]
        [TestCase("EnableDeepProfilingSupport")]
        public void BuildProvenance_ForbiddenBuildFlagsFailClosed(string forbiddenFlag)
        {
            object options = BuildOptionsValue(forbiddenFlag);
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                InvokeBuildProvenance(
                    "ValidateBuildRoleContract",
                    "release_performance",
                    "release",
                    options));

            Assert.That(exception.InnerException.Message, Does.Contain("forbidden_build_flag"));
        }

        [Test]
        public void BuildProvenance_FrameTimingIsRecordedAndChangesConfigurationFingerprint()
        {
            string root = CreateArtifactFixture(false, "assembly");
            try
            {
                object sidecar = CreateReleaseSidecarFixture(root);
                Assert.That(Field<bool>(sidecar, "frameTimingStatsEnabled"), Is.True);

                object none = BuildOptionsValue("None");
                object[] common =
                {
                    "6000.3.10f1",
                    "StandaloneWindows64",
                    "x86_64/player-settings-1",
                    none,
                    new[] { "Direct3D11" },
                    "Assets/_LocalTrials/Benchmark.unity",
                    "scene-guid",
                    "release_performance",
                    "release",
                    false,
                    false,
                    false,
                    false,
                };
                string enabled = (string)InvokeBuildProvenance(
                    "ComputeBuildConfigurationFingerprint",
                    common.Concat(new object[] { true }).ToArray());
                string disabled = (string)InvokeBuildProvenance(
                    "ComputeBuildConfigurationFingerprint",
                    common.Concat(new object[] { false }).ToArray());

                Assert.That(enabled, Is.Not.EqualTo(disabled));
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_ArtifactIdIsOrderIndependentAndTracksManagedAssembly()
        {
            string firstRoot = CreateArtifactFixture(false, "assembly-v1");
            string secondRoot = CreateArtifactFixture(true, "assembly-v1");
            try
            {
                object first = InvokeBuildProvenance("ComputeArtifactIdentity", firstRoot);
                object second = InvokeBuildProvenance("ComputeArtifactIdentity", secondRoot);
                string baseline = Field<string>(first, "buildArtifactId");
                Assert.That(Field<string>(second, "buildArtifactId"), Is.EqualTo(baseline));

                File.WriteAllText(
                    Path.Combine(secondRoot, "SOTF_R2_PERF1_Data", "Managed", "Assembly-CSharp.dll"),
                    "assembly-v2");
                object changed = InvokeBuildProvenance("ComputeArtifactIdentity", secondRoot);
                Assert.That(Field<string>(changed, "buildArtifactId"), Is.Not.EqualTo(baseline));
            }
            finally
            {
                DeleteDirectory(firstRoot);
                DeleteDirectory(secondRoot);
            }
        }

        [Test]
        public void BuildProvenance_ArtifactIdExcludesSidecarAndTemporarySidecar()
        {
            string root = CreateArtifactFixture(false, "assembly");
            try
            {
                string baseline = Field<string>(
                    InvokeBuildProvenance("ComputeArtifactIdentity", root),
                    "buildArtifactId");
                File.WriteAllText(Path.Combine(root, "r2-perf1.build-provenance.json"), "old");
                File.WriteAllText(Path.Combine(root, "r2-perf1.build-provenance.json.tmp"), "partial");
                object withSidecars = InvokeBuildProvenance("ComputeArtifactIdentity", root);

                Assert.That(Field<string>(withSidecars, "buildArtifactId"), Is.EqualTo(baseline));
                Assert.That(Field<int>(withSidecars, "artifactFileCount"), Is.EqualTo(2));
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_ArtifactEntryRejectsReparsePointBeforeHashing()
        {
            string root = CreateProvenanceTempRoot();
            try
            {
                string candidate = Path.Combine(root, "linked-artifact.bin");
                TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                    InvokeBuildProvenance(
                        "ValidateArtifactEntryForHash",
                        root,
                        candidate,
                        FileAttributes.ReparsePoint));

                Assert.That(exception.InnerException.Message, Does.Contain("artifact_reparse_point"));
                Assert.That(File.Exists(candidate), Is.False,
                    "The behavior seam must reject before attempting to read content.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_ArtifactEntryRejectsPathOutsideArtifactRoot()
        {
            string root = CreateProvenanceTempRoot();
            string outside = Path.Combine(
                Path.GetTempPath(),
                "sotf-outside-artifact-" + Guid.NewGuid().ToString("N") + ".bin");
            try
            {
                TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                    InvokeBuildProvenance(
                        "ValidateArtifactEntryForHash",
                        root,
                        outside,
                        FileAttributes.Normal));

                Assert.That(exception.InnerException.Message, Does.Contain("artifact_path_escape"));
                Assert.That(File.Exists(outside), Is.False);
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_SerializedSchemaHasRequiredFieldsAndTypes()
        {
            string root = CreateArtifactFixture(false, "assembly");
            try
            {
                object sidecar = CreateReleaseSidecarFixture(root);
                string json = (string)InvokeBuildProvenance("SerializeSidecar", sidecar);
                Type sidecarType = sidecar.GetType();

                Assert.That(Field<string>(sidecar, "schemaVersion"),
                    Is.EqualTo("r2-perf1-build-provenance/1"));
                AssertSidecarFieldType(sidecarType, "generatedUtc", typeof(string));
                AssertSidecarFieldType(sidecarType, "measurementRole", typeof(string));
                AssertSidecarFieldType(sidecarType, "buildKind", typeof(string));
                AssertSidecarFieldType(sidecarType, "sourceCommit", typeof(string));
                AssertSidecarFieldType(sidecarType, "sourceTreeClean", typeof(bool));
                AssertSidecarFieldType(sidecarType, "frameTimingStatsEnabled", typeof(bool));
                AssertSidecarFieldType(sidecarType, "graphicsApis", typeof(string[]));
                AssertSidecarFieldType(sidecarType, "benchmarkContentTracked", typeof(bool));
                AssertSidecarFieldType(sidecarType, "repositoryReproducible", typeof(bool));
                AssertSidecarFieldType(sidecarType, "dependencyCount", typeof(int));
                AssertSidecarFieldType(sidecarType, "artifactFileCount", typeof(int));
                foreach (string required in new[]
                         {
                             "schemaVersion", "generatedUtc", "measurementRole", "buildKind",
                             "sourceCommit", "sourceTreeClean", "unityVersion", "platform",
                             "architecture", "buildOptions", "developmentBuild", "autoRunPlayer",
                             "autoConnectProfiler", "deepProfiling", "frameTimingStatsEnabled",
                             "graphicsApis",
                             "benchmarkScenePath", "benchmarkSceneGuid", "benchmarkContentTracked",
                             "repositoryReproducible", "contentTrackingStatus", "dependencyCount",
                             "contentFingerprint", "buildConfigurationFingerprint",
                             "buildArtifactId", "artifactFileCount",
                         })
                {
                    Assert.That(json, Does.Contain("\"" + required + "\""));
                }

                Assert.That(json, Does.Not.Contain(Path.GetFullPath(root)));
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_AtomicWriterReplacesFinalWithoutLeavingTemporaryFile()
        {
            string root = CreateArtifactFixture(false, "assembly");
            try
            {
                object sidecar = CreateReleaseSidecarFixture(root);
                string finalPath = Path.Combine(root, "r2-perf1.build-provenance.json");
                string temporaryPath = finalPath + ".tmp";
                File.WriteAllText(finalPath, "previous-sidecar");

                InvokeBuildProvenance("WriteSidecarAtomic", finalPath, sidecar);

                Assert.That(File.ReadAllText(finalPath), Does.Contain("r2-perf1-build-provenance/1"));
                Assert.That(File.Exists(temporaryPath), Is.False);
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_AtomicWriterFailurePreservesExistingFinalAndCleansTemporaryFile()
        {
            string root = CreateArtifactFixture(false, "assembly");
            try
            {
                object sidecar = CreateReleaseSidecarFixture(root);
                string finalPath = Path.Combine(root, "r2-perf1.build-provenance.json");
                string temporaryPath = finalPath + ".tmp";
                File.WriteAllText(finalPath, "trusted-sidecar");
                using (new FileStream(finalPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    Assert.Throws<TargetInvocationException>(() =>
                        InvokeBuildProvenance("WriteSidecarAtomic", finalPath, sidecar));
                }

                Assert.That(File.ReadAllText(finalPath), Is.EqualTo("trusted-sidecar"));
                Assert.That(File.Exists(temporaryPath), Is.False);
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void BuildProvenance_AtomicWriterDoesNotDeleteAnotherInvocationsTemporaryFile()
        {
            string root = CreateArtifactFixture(false, "assembly");
            try
            {
                object sidecar = CreateReleaseSidecarFixture(root);
                string finalPath = Path.Combine(root, "r2-perf1.build-provenance.json");
                string temporaryPath = finalPath + ".tmp";
                File.WriteAllText(finalPath, "trusted-sidecar");
                File.WriteAllText(temporaryPath, "other-invocation");

                TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                    InvokeBuildProvenance("WriteSidecarAtomic", finalPath, sidecar));

                Assert.That(exception.InnerException.Message, Does.Contain("sidecar_write_failed"));
                Assert.That(File.ReadAllText(finalPath), Is.EqualTo("trusted-sidecar"));
                Assert.That(File.ReadAllText(temporaryPath), Is.EqualTo("other-invocation"));
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        [Test]
        public void OfflineRunner_HasFailClosedProcessAndTelemetryContract()
        {
            string script = File.ReadAllText(OfflineRunnerPath);

            Assert.That(script, Does.Contain("Start-Process"));
            Assert.That(script, Does.Contain("-PassThru"));
            Assert.That(script, Does.Contain("$process.Id"));
            Assert.That(script, Does.Contain("$process.HasExited"));
            Assert.That(script, Does.Contain("Stop-Process -Id $Process.Id"));
            Assert.That(script, Does.Contain("Get-Process -Name \"Unity\", \"Code\""));
            Assert.That(script, Does.Contain("nvidia-smi"));
            Assert.That(script, Does.Contain("awaiting_offline_validation"));
            Assert.That(script, Does.Contain("valid-runs.csv"));
            Assert.That(script, Does.Contain("valid-visual-runs.csv"));
            Assert.That(script, Does.Contain("invalid-runs.csv"));
            Assert.That(script, Does.Contain("$DryRun"));
            Assert.That(script, Does.Contain("Invoke-GpuIdleGate"));
            Assert.That(script, Does.Contain("Elapsed.TotalSeconds -lt 5.0"));
            Assert.That(script, Does.Contain("Get-NvidiaPmonSnapshot"));
            Assert.That(script, Does.Contain("Get-NvidiaSnapshot -Phase \"during\""));
            Assert.That(script, Does.Not.Contain("Write-NvidiaSnapshot -Phase \"during\""));
            Assert.That(script, Does.Contain("P8/300 MHz is allowed while idle"));
            Assert.That(script, Does.Contain("Test-PathWithinDirectory"));
            Assert.That(script, Does.Contain("Wait-ForStableRunOutputs"));
            Assert.That(script, Does.Contain("Get-DescendantProcessIds"));
            Assert.That(script, Does.Contain("foreign manifest was not modified"));
            Assert.That(script, Does.Contain("IsNaN"));
            Assert.That(script, Does.Contain("IsInfinity"));
            Assert.That(script, Does.Not.Contain("Stop-Process -Name Unity"));
            Assert.That(script, Does.Not.Contain("Stop-Process -Name Code"));
        }

        [Test]
        public void OfflineRunner_BoundsPairedRunIdentifiersDeterministically()
        {
            string runner = File.ReadAllText(OfflineRunnerPath);
            int mainBoundary = runner.IndexOf(
                "$Executable = Resolve-ProjectPath",
                StringComparison.Ordinal);
            Assert.That(mainBoundary, Is.GreaterThan(0), "Offline runner main boundary was not found.");

            string cleanupRoot = Path.Combine(
                Path.GetTempPath(),
                "sotf-r2-perf1-run-id-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(cleanupRoot);
            string harnessPath = Path.Combine(cleanupRoot, "run-id-contract.ps1");
            try
            {
                var harness = new StringBuilder(runner.Substring(0, mainBoundary));
                harness.AppendLine();
                harness.AppendLine(@"
function Assert-RunIdContract {
    param([string]$Id, [string]$Label)
    if ($Id.Length -gt 80) { throw ($Label + ' exceeded 80 characters: ' + $Id.Length) }
    if ($Id -cnotmatch '^[A-Za-z0-9_-]+$') { throw ($Label + ' contains unsafe characters: ' + $Id) }
    $directory = Join-Path 'C:\bounded-output' $Id
    if ((Split-Path -Leaf $directory) -cne $Id) { throw ($Label + ' directory leaf mismatch') }
}

$releaseArgs = @{
    MeasurementRole = 'release_performance'; BuildKind = 'release'; Quality = 'High Fidelity'
    Scenario = 'empty_hdrp_camera'; RenderScalePercent = 100; RunNumber = 1
    Timestamp = '20260801T133945981Z'
}
$developmentArgs = @{
    MeasurementRole = 'development_gc'; BuildKind = 'diagnostic'; Quality = 'High Fidelity'
    Scenario = 'empty_hdrp_camera'; RenderScalePercent = 100; RunNumber = 1
    Timestamp = '20260801T134000757Z'
}
$releaseId = New-BoundedRunId @releaseArgs
$developmentId = New-BoundedRunId @developmentArgs
Assert-RunIdContract $releaseId 'failed release ID regression'
Assert-RunIdContract $developmentId 'failed development ID regression'
if ($releaseId -ceq 'release_performance_release_High_Fidelity_empty_hdrp_camera_rs100_r1_20260801T133945981Z') {
    throw 'release ID was not bounded'
}
if ($developmentId -ceq 'development_gc_diagnostic_High_Fidelity_empty_hdrp_camera_rs100_r1_20260801T134000757Z') {
    throw 'development ID was not bounded'
}
if ((New-BoundedRunId @releaseArgs) -cne $releaseId) { throw 'same input was not deterministic' }

$longArgs = @{
    MeasurementRole = 'release_performance'; BuildKind = 'release'
    Quality = ('quality with spaces / unsafe ! characters ' * 100)
    Scenario = ('scenario.with.arbitrarily.long.input/' * 100)
    RenderScalePercent = 100; RunNumber = 1; Timestamp = '20260801T133945981Z'
}
$longId = New-BoundedRunId @longArgs
Assert-RunIdContract $longId 'arbitrarily long input'

$differentRunArgs = $releaseArgs.Clone(); $differentRunArgs.RunNumber = 2
$differentTimeArgs = $releaseArgs.Clone(); $differentTimeArgs.Timestamp = '20260801T133945982Z'
$differentQualityArgs = $releaseArgs.Clone(); $differentQualityArgs.Quality = 'Balanced'
$ids = @($releaseId, $developmentId, $longId,
    (New-BoundedRunId @differentRunArgs),
    (New-BoundedRunId @differentTimeArgs),
    (New-BoundedRunId @differentQualityArgs))
if (@($ids | Sort-Object -Unique).Count -ne $ids.Count) { throw 'distinct inputs collided' }
Write-Output 'BOUNDED_RUN_ID_CONTRACT_PASS'
");
                File.WriteAllText(harnessPath, harness.ToString());

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" +
                                harnessPath + "\" -MeasurementRole release_performance " +
                                "-MeasurementSetId bounded_run_id_contract",
                    WorkingDirectory = Path.GetFullPath("."),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using (Process process = Process.Start(startInfo))
                {
                    Assert.That(process, Is.Not.Null);
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    bool exited = process.WaitForExit(60000);
                    if (!exited)
                    {
                        process.Kill();
                    }

                    Assert.That(exited, Is.True, "PowerShell bounded run-ID contract timed out.");
                    Assert.That(
                        process.ExitCode,
                        Is.EqualTo(0),
                        "PowerShell bounded run-ID contract failed.\nSTDOUT:\n" + output +
                        "\nSTDERR:\n" + error);
                    Assert.That(output, Does.Contain("BOUNDED_RUN_ID_CONTRACT_PASS"));
                }
            }
            finally
            {
                if (Directory.Exists(cleanupRoot))
                {
                    Directory.Delete(cleanupRoot, true);
                }
            }
        }

        [Test]
        public void PowerShellSourceCommitContract_CanonicalizesCaseAndRejectsInvalidHashes()
        {
            string offlineRunner = File.ReadAllText(OfflineRunnerPath);
            int offlineBoundary = offlineRunner.IndexOf(
                "$Executable = Resolve-ProjectPath",
                StringComparison.Ordinal);
            Assert.That(offlineBoundary, Is.GreaterThan(0));

            string pairRunner = File.ReadAllText(PairRunnerPath);
            int pairFunctionStart = pairRunner.IndexOf(
                "function Convert-ToCanonicalSourceCommit",
                StringComparison.Ordinal);
            int pairFunctionEnd = pairRunner.IndexOf(
                "function Resolve-ProjectPath",
                pairFunctionStart,
                StringComparison.Ordinal);
            Assert.That(pairFunctionStart, Is.GreaterThan(0));
            Assert.That(pairFunctionEnd, Is.GreaterThan(pairFunctionStart));

            string cleanupRoot = Path.Combine(
                Path.GetTempPath(),
                "sotf-r2-perf1-source-commit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(cleanupRoot);
            string harnessPath = Path.Combine(cleanupRoot, "source-commit-contract.ps1");
            try
            {
                var harness = new StringBuilder(offlineRunner.Substring(0, offlineBoundary));
                harness.AppendLine(pairRunner.Substring(pairFunctionStart, pairFunctionEnd - pairFunctionStart));
                harness.AppendLine(@"
$lower = '2e55294f9321819e770914eafaab6a449d6e92db'
$upper = $lower.ToUpperInvariant()
if ((Convert-ToCanonicalSourceCommit $lower) -cne $upper) { throw 'lowercase canonicalization failed' }
if ((Convert-ToCanonicalSourceCommit $upper) -cne $upper) { throw 'uppercase canonicalization failed' }
$lowerUpper = Get-PairedSourceCommitComparison $lower $upper
$upperLower = Get-PairedSourceCommitComparison $upper $lower
if (-not $lowerUpper.Matches -or $lowerUpper.CanonicalSourceCommit -cne $upper) { throw 'lower/upper pair failed' }
if (-not $upperLower.Matches -or $upperLower.CanonicalSourceCommit -cne $upper) { throw 'upper/lower pair failed' }
$different = Get-PairedSourceCommitComparison $lower '3e55294f9321819e770914eafaab6a449d6e92db'
if ($different.Matches) { throw 'different SHA accepted' }
foreach ($invalid in @('not-hex', ('a' * 39), ('a' * 41), (('a' * 39) + 'g'))) {
    try { Convert-ToCanonicalSourceCommit $invalid; throw ('invalid SHA accepted: ' + $invalid) }
    catch { if ($_.Exception.Message -notmatch 'exactly 40 hexadecimal') { throw } }
}
Write-Output 'SOURCE_COMMIT_CONTRACT_PASS'
");
                File.WriteAllText(harnessPath, harness.ToString());
                AssertPowerShellHarnessPasses(
                    harnessPath,
                    "-MeasurementRole release_performance -MeasurementSetId source_commit_contract",
                    "SOURCE_COMMIT_CONTRACT_PASS");
            }
            finally
            {
                DeleteDirectory(cleanupRoot);
            }
        }

        [Test]
        public void OfflineValidator_FunctionalSafetyAndCompletenessMatrixPasses()
        {
            string runner = File.ReadAllText(OfflineRunnerPath);
            int mainBoundary = runner.IndexOf(
                "$Executable = Resolve-ProjectPath",
                StringComparison.Ordinal);
            Assert.That(mainBoundary, Is.GreaterThan(0), "Offline runner main boundary was not found.");

            string cleanupRoot = Path.Combine(
                Path.GetTempPath(),
                "sotf-r2-perf1-b4-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(cleanupRoot);
            string harnessPath = Path.Combine(cleanupRoot, "validator-contract.ps1");
            try
            {
                var harness = new StringBuilder(runner.Substring(0, mainBoundary));
                harness.AppendLine();
                harness.AppendLine("$ContractRoot = '" + EscapePowerShellLiteral(cleanupRoot) + "'");
                harness.AppendLine(OfflineValidatorContractMatrixScript);
                File.WriteAllText(harnessPath, harness.ToString());

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" +
                                harnessPath + "\" -MeasurementRole release_performance " +
                                "-MeasurementSetId validator_contract_set",
                    WorkingDirectory = Path.GetFullPath("."),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using (Process process = Process.Start(startInfo))
                {
                    Assert.That(process, Is.Not.Null);
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    bool exited = process.WaitForExit(60000);
                    if (!exited)
                    {
                        process.Kill();
                    }

                    Assert.That(exited, Is.True, "PowerShell validator contract matrix timed out.");
                    Assert.That(
                        process.ExitCode,
                        Is.EqualTo(0),
                        "PowerShell validator contract matrix failed.\nSTDOUT:\n" + output +
                        "\nSTDERR:\n" + error);
                    Assert.That(output, Does.Contain("B4_VALIDATOR_MATRIX_PASS"));
                    Assert.That(output, Does.Contain("B5A_AUTHORITY_MATRIX_PASS"));
                }
            }
            finally
            {
                if (Directory.Exists(cleanupRoot))
                {
                    Directory.Delete(cleanupRoot, true);
                }
            }
        }

        [Test]
        public void OfflineValidator_UsesNamedChecksAndPerRunExceptionBoundary()
        {
            string script = File.ReadAllText(OfflineRunnerPath);

            Assert.That(script, Does.Contain("function New-ValidationCheck"));
            Assert.That(script, Does.Contain("Passed = $Passed"));
            Assert.That(script, Does.Contain("Reason = $Reason"));
            Assert.That(script, Does.Not.Contain("$check[0]"));
            Assert.That(script, Does.Not.Contain("$check[1]"));
            Assert.That(script, Does.Contain("$scenarios = @($report.scenarios)"));
            Assert.That(script, Does.Contain("$matchingScenarios.Count -eq 0"));
            Assert.That(script, Does.Contain("$matchingScenarios.Count -gt 1"));
            Assert.That(script, Does.Contain("required metric is unavailable"));
            Assert.That(script, Does.Contain("measurement completeness gate failed"));
            Assert.That(script, Does.Contain("New-ValidatorExceptionRunResult"));
            Assert.That(script, Does.Contain("offline.invalid.manifest.json"));
            Assert.That(
                Regex.Matches(script, "\\$results \\+= \\$runResult").Count,
                Is.EqualTo(1),
                "Each run must append exactly one structured result.");
        }

        [Test]
        public void CmdWrapper_ForwardsQuotedArgumentsWithoutReconstruction()
        {
            string wrapper = File.ReadAllText(OfflineWrapperPath);

            Assert.That(wrapper, Does.Contain("\"%~dp0Invoke-R2Perf1Offline.ps1\" %*"));
            Assert.That(wrapper, Does.Contain("exit /b %ERRORLEVEL%"));
        }

        [TestCase("release_performance", "release")]
        [TestCase("development_gc", "diagnostic")]
        public void CmdWrapper_DryRunExecutesRoleContractWithoutLaunchingPlayerOrCreatingOutput(
            string measurementRole,
            string buildKind)
        {
            string wrapper = Path.GetFullPath(OfflineWrapperPath);
            string executable = Path.GetFullPath(
                "Builds/Benchmarks/R2_PERF1_Release/SOTF_R2_PERF1.exe");
            Assert.That(File.Exists(executable), Is.True, "Release dry-run stub is missing.");
            string outputRoot = Path.Combine(
                Path.GetTempPath(),
                "sotf-r2-perf1-cmd-dry-run-" + Guid.NewGuid().ToString("N"));
            int playerCountBefore = Process.GetProcessesByName("SOTF_R2_PERF1").Length;
            string command = "\"" + wrapper + "\"" +
                             " -ExecutablePath \"" + executable + "\"" +
                             " -Quality \"High Fidelity\"" +
                             " -Scenario empty_hdrp_camera" +
                             " -Antialiasing TAA" +
                             " -RenderScalePercent 100" +
                             " -BuildKind " + buildKind +
                             " -MeasurementRole " + measurementRole +
                             " -MeasurementSetId cmd_dry_run_set" +
                             " -Runs 1" +
                             " -OutputRoot \"" + outputRoot + "\"" +
                             " -DryRun";
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/d /s /c \"" + command + "\"",
                WorkingDirectory = Path.GetFullPath("."),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using (Process process = Process.Start(startInfo))
            {
                Assert.That(process, Is.Not.Null);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                bool exited = process.WaitForExit(60000);
                if (!exited)
                {
                    process.Kill();
                }

                Assert.That(exited, Is.True, "CMD dry-run timed out.");
                Assert.That(
                    process.ExitCode,
                    Is.EqualTo(0),
                    "CMD dry-run failed.\nSTDOUT:\n" + output + "\nSTDERR:\n" + error);
                Assert.That(output, Does.Contain("High Fidelity"));
                Assert.That(output, Does.Contain(measurementRole));
                Assert.That(output, Does.Contain("-sotf-measurement-role"));
                Assert.That(output, Does.Contain("Dry-run completed"));
            }

            Assert.That(Directory.Exists(outputRoot), Is.False);
            Assert.That(
                Process.GetProcessesByName("SOTF_R2_PERF1").Length,
                Is.EqualTo(playerCountBefore));
        }

        [Test]
        public void ProvenanceContract_LocalIgnoredContentIsOnlyLocalComparable()
        {
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-content-tracking-status", "local_ignored_content",
                "-sotf-repository-reproducible", "true",
                "-sotf-comparison-scope", "repository_comparable");

            object settings = Parse(
                "player.exe", "-sotf-perf1",
                "-sotf-measurement-role", "development_gc",
                "-sotf-content-tracking-status", "local_ignored_content",
                "-sotf-repository-reproducible", "false",
                "-sotf-comparison-scope", "local_comparable");
            Assert.That(Field<bool>(settings, "repositoryReproducible"), Is.False);
            Assert.That(Field<string>(settings, "comparisonScope"), Is.EqualTo("local_comparable"));
        }

        [Test]
        public void PairCmd_DryRunDoesNotLaunchPlayerOrCreateMeasurementOutput()
        {
            string wrapper = Path.GetFullPath(PairWrapperPath);
            Assert.That(File.Exists(wrapper), Is.True);
            Assert.That(File.ReadAllText(wrapper), Does.Contain("\"%~dp0Invoke-R2Perf1Pair.ps1\" %*"));
            string outputRoot = Path.Combine(
                Path.GetTempPath(),
                "sotf-r2-perf1-pair-dry-run-" + Guid.NewGuid().ToString("N"));
            int playerCountBefore = Process.GetProcessesByName("SOTF_R2_PERF1").Length +
                                    Process.GetProcessesByName("SOTF_R2_PERF1_Diagnostic").Length;
            string command = "\"" + wrapper + "\"" +
                             " -ReleaseExecutablePath \"missing release path.exe\"" +
                             " -DiagnosticExecutablePath \"missing diagnostic path.exe\"" +
                             " -Quality \"High Fidelity\"" +
                             " -Scenario empty_hdrp_camera -Antialiasing TAA" +
                             " -RenderScalePercent 100 -OutputRoot \"" + outputRoot + "\" -DryRun";
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/d /s /c \"" + command + "\"",
                WorkingDirectory = Path.GetFullPath("."),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (Process process = Process.Start(startInfo))
            {
                Assert.That(process, Is.Not.Null);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                Assert.That(process.WaitForExit(60000), Is.True);
                Assert.That(process.ExitCode, Is.EqualTo(0), output + "\n" + error);
                Assert.That(output, Does.Contain("Paired dry-run completed"));
            }
            Assert.That(Directory.Exists(outputRoot), Is.False);
            Assert.That(
                Process.GetProcessesByName("SOTF_R2_PERF1").Length +
                Process.GetProcessesByName("SOTF_R2_PERF1_Diagnostic").Length,
                Is.EqualTo(playerCountBefore));
        }

        private static object Parse(params string[] arguments)
        {
            MethodInfo method = ConfigurationType.GetMethod(
                "Parse",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(null, new object[] { WithProvenanceArguments(arguments) });
        }

        private static string[] WithProvenanceArguments(string[] arguments)
        {
            if (arguments == null || !arguments.Contains("-sotf-perf1"))
            {
                return arguments;
            }

            var result = new List<string>(arguments);
            void AddIfMissing(string name, string value)
            {
                if (!result.Contains(name))
                {
                    result.Add(name);
                    result.Add(value);
                }
            }

            AddIfMissing("-sotf-measurement-set-id", "test_measurement_set");
            AddIfMissing("-sotf-source-commit", "ea1086d2887c2b3f985f3376520ba9e99339ad58");
            AddIfMissing("-sotf-source-tree-clean", "true");
            AddIfMissing("-sotf-build-artifact-id", new string('A', 64));
            AddIfMissing("-sotf-content-fingerprint", new string('B', 64));
            AddIfMissing("-sotf-configuration-fingerprint", new string('C', 64));
            AddIfMissing("-sotf-hardware-fingerprint", new string('D', 64));
            AddIfMissing("-sotf-repository-reproducible", "true");
            AddIfMissing("-sotf-content-tracking-status", "tracked_content");
            AddIfMissing("-sotf-comparison-scope", "repository_comparable");
            return result.ToArray();
        }

        private static object InvokeBuildProvenance(string methodName, params object[] arguments)
        {
            MethodInfo[] candidates = BuildProvenanceType.GetMethods(
                    BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == methodName &&
                                 method.GetParameters().Length == arguments.Length)
                .ToArray();
            Assert.That(candidates, Has.Length.EqualTo(1),
                "Expected one provenance method overload: " + methodName);
            return candidates[0].Invoke(null, arguments);
        }

        private static string CreateProvenanceTempRoot()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "sotf-r2-perf1-b5b1-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static object CreateFileDependency(
            string root,
            string assetPath,
            string assetContent,
            string metaContent,
            string guid,
            string dependencyHash)
        {
            string physical = Path.Combine(
                root,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(physical));
            File.WriteAllText(physical, assetContent);
            File.WriteAllText(physical + ".meta", metaContent);
            return InvokeBuildProvenance(
                "CreateFileDependencyRecord",
                assetPath,
                physical,
                guid,
                dependencyHash,
                "project_asset",
                "tracked",
                string.Empty);
        }

        private static Array CreateDependencyArray(params object[] dependencies)
        {
            Type dependencyType = BuildProvenanceType.GetNestedType(
                "ContentDependencyRecord",
                BindingFlags.Public);
            Assert.That(dependencyType, Is.Not.Null);
            Array result = Array.CreateInstance(dependencyType, dependencies.Length);
            for (int index = 0; index < dependencies.Length; index++)
            {
                result.SetValue(dependencies[index], index);
            }

            return result;
        }

        private static string ComputeContentFingerprint(params object[] dependencies)
        {
            return (string)InvokeBuildProvenance(
                "ComputeContentFingerprint",
                CreateDependencyArray(dependencies));
        }

        private static object BuildOptionsValue(string name)
        {
            MethodInfo validate = BuildProvenanceType.GetMethod(
                "ValidateBuildRoleContract",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(validate, Is.Not.Null);
            Type optionsType = validate.GetParameters()[2].ParameterType;
            return Enum.Parse(optionsType, name);
        }

        private static string CreateArtifactFixture(bool reverseCreationOrder, string assemblyContent)
        {
            string root = CreateProvenanceTempRoot();
            string executable = Path.Combine(root, "SOTF_R2_PERF1.exe");
            string assembly = Path.Combine(
                root,
                "SOTF_R2_PERF1_Data",
                "Managed",
                "Assembly-CSharp.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(assembly));
            if (reverseCreationOrder)
            {
                File.WriteAllText(assembly, assemblyContent);
                File.WriteAllText(executable, "player-stub");
            }
            else
            {
                File.WriteAllText(executable, "player-stub");
                File.WriteAllText(assembly, assemblyContent);
            }

            return root;
        }

        private static object CreateReleaseSidecarFixture(string artifactRoot)
        {
            object dependency = CreateFileDependency(
                artifactRoot,
                "Assets/_LocalTrials/Benchmark.unity",
                "scene-content",
                "scene-meta",
                "scene-guid",
                "scene-import");
            Array dependencies = CreateDependencyArray(dependency);
            Type sourceType = BuildProvenanceType.GetNestedType("SourceProvenance", BindingFlags.Public);
            Type contentType = BuildProvenanceType.GetNestedType(
                "ContentFingerprintResult",
                BindingFlags.Public);
            Type contextType = BuildProvenanceType.GetNestedType("PreBuildContext", BindingFlags.Public);
            object source = Activator.CreateInstance(sourceType);
            sourceType.GetField("sourceCommit").SetValue(
                source,
                "62ac676c998df1c232e9075f1fc51f6f935684ab");
            sourceType.GetField("sourceTreeClean").SetValue(source, true);
            object content = Activator.CreateInstance(contentType);
            contentType.GetField("dependencies").SetValue(content, dependencies);
            contentType.GetField("canonicalData").SetValue(content, "fixture-canonical");
            contentType.GetField("contentFingerprint").SetValue(
                content,
                ComputeContentFingerprint(dependency));
            contentType.GetField("benchmarkContentTracked").SetValue(content, false);
            contentType.GetField("repositoryReproducible").SetValue(content, false);
            contentType.GetField("contentTrackingStatus").SetValue(
                content,
                "local_ignored_content");
            object context = Activator.CreateInstance(contextType);
            contextType.GetField("source").SetValue(context, source);
            contextType.GetField("content").SetValue(context, content);
            contextType.GetField("measurementRole").SetValue(context, "release_performance");
            contextType.GetField("buildKind").SetValue(context, "release");
            contextType.GetField("unityVersion").SetValue(context, "6000.3.10f1");
            contextType.GetField("platform").SetValue(context, "StandaloneWindows64");
            contextType.GetField("architecture").SetValue(context, "x86_64/player-settings-1");
            contextType.GetField("frameTimingStatsEnabled").SetValue(context, true);
            contextType.GetField("graphicsApis").SetValue(context, new[] { "Direct3D11" });
            contextType.GetField("benchmarkScenePath").SetValue(
                context,
                "Assets/_LocalTrials/Benchmark.unity");
            contextType.GetField("benchmarkSceneGuid").SetValue(context, "scene-guid");

            return InvokeBuildProvenance(
                "CreateCompletedSidecar",
                context,
                BuildOptionsValue("None"),
                "StandaloneWindows64",
                artifactRoot,
                "2026-08-01T00:00:00.0000000Z");
        }

        private static void AssertSidecarFieldType(Type sidecarType, string fieldName, Type expected)
        {
            FieldInfo field = sidecarType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, "Missing sidecar field: " + fieldName);
            Assert.That(field.FieldType, Is.EqualTo(expected), "Sidecar field type: " + fieldName);
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private static object InvokeConfiguration(string methodName, params object[] arguments)
        {
            MethodInfo method = ConfigurationType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "Missing configuration method: " + methodName);
            return method.Invoke(null, arguments);
        }

        private static void AssertConfigurationFailure<TException>(
            string methodName,
            params object[] arguments)
            where TException : Exception
        {
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => InvokeConfiguration(methodName, arguments));
            Assert.That(exception.InnerException, Is.TypeOf<TException>());
        }

        private static T StaticField<T>(Type type, string name)
        {
            FieldInfo field = type.GetField(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, "Missing static field: " + name);
            return (T)field.GetValue(null);
        }

        private static string[] RecoveryArguments(string runId, string runDirectory)
        {
            return new[]
            {
                "player.exe",
                "-sotf-perf1",
                "-sotf-run-id", runId,
                "-sotf-output-directory", runDirectory,
            };
        }

        private static int RecoverWithEnvelope(object envelope, out string manifestPath)
        {
            MethodInfo recover = RunnerType.GetMethod(
                "RecoverFromStartupFailureWithEnvelope",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(recover, Is.Not.Null);
            object[] parameters =
            {
                envelope,
                new InvalidOperationException("synthetic recovery failure"),
                new Func<bool>(() => true),
                null,
            };
            int exitCode = (int)recover.Invoke(null, parameters);
            manifestPath = (string)parameters[3];
            return exitCode;
        }

        private static string CreateLongRunDirectory(
            out string cleanupRoot,
            string runId = null)
        {
            cleanupRoot = Path.Combine(
                Path.GetTempPath(),
                "sotf-b2-" + Guid.NewGuid().ToString("N"));
            int componentLength = Math.Max(32, 128 - cleanupRoot.Length);
            string longParent = Path.Combine(cleanupRoot, new string('r', componentLength));
            return string.IsNullOrEmpty(runId)
                ? longParent
                : Path.Combine(longParent, runId);
        }

        private static T Field<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, "Missing settings field: " + name);
            return (T)field.GetValue(target);
        }

        private static T Property<T>(object target, string name)
        {
            PropertyInfo property = target.GetType().GetProperty(
                name,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, "Missing settings property: " + name);
            return (T)property.GetValue(target);
        }

        private static void AssertParseFailure<TException>(params string[] arguments)
            where TException : Exception
        {
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => Parse(arguments));
            Assert.That(exception.InnerException, Is.TypeOf<TException>());
        }

        private static string EscapePowerShellLiteral(string value)
        {
            return value.Replace("'", "''");
        }

        private static void AssertPowerShellHarnessPasses(
            string harnessPath,
            string arguments,
            string expectedOutput)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" +
                            harnessPath + "\" " + arguments,
                WorkingDirectory = Path.GetFullPath("."),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (Process process = Process.Start(startInfo))
            {
                Assert.That(process, Is.Not.Null);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                bool exited = process.WaitForExit(60000);
                if (!exited)
                {
                    process.Kill();
                }

                Assert.That(exited, Is.True, "PowerShell contract harness timed out.");
                Assert.That(
                    process.ExitCode,
                    Is.EqualTo(0),
                    "PowerShell contract harness failed.\nSTDOUT:\n" + output +
                    "\nSTDERR:\n" + error);
                Assert.That(output, Does.Contain(expectedOutput));
            }
        }

        private const string OfflineValidatorContractMatrixScript = @"
function Assert-Contract {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function New-ContractScenario {
    param(
        [string]$Name = 'empty_hdrp_camera',
        [string]$BudgetStatus = 'PASS',
        [string]$BudgetFailure = '',
        [bool]$GcAvailable = $true,
        [bool]$GfxMemoryAvailable = $true
    )
    return [pscustomobject]@{
        name = $Name
        avgMs = 5.0
        medianMs = 5.0
        p95Ms = 6.0
        p99Ms = 7.0
        avgFps = 200.0
        onePercentLowFps = 142.0
        cpuTotalAvgMs = 4.0
        cpuMainThreadAvgMs = 3.5
        cpuRenderThreadAvgMs = 1.0
        gpuAvgMs = 2.0
        gcAllocatedAverageBytes = 0.0
        totalUsedMemoryAverageMb = 200.0
        gfxUsedMemoryAverageMb = 100.0
        textureMemoryAverageMb = 50.0
        drawCallsAvailable = $true
        batchesAvailable = $true
        setPassCallsAvailable = $true
        trianglesAvailable = $true
        verticesAvailable = $true
        gcAllocationAvailable = $GcAvailable
        totalUsedMemoryAvailable = $true
        gfxUsedMemoryAvailable = $GfxMemoryAvailable
        textureMemoryAvailable = $true
        budgetStatus = $BudgetStatus
        budgetFailure = $BudgetFailure
    }
}

function New-ContractFixture {
    param([string]$Label, [object[]]$Scenarios)
    $directory = Join-Path $ContractRoot $Label
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $runId = 'run_' + $Label
    $reportPath = Join-Path $directory 'runtime.report.json'
    $markdownPath = Join-Path $directory 'runtime.report.md'
    $report = [pscustomobject]@{
        schemaVersion = 'r2-perf1/1'
        benchmarkRunId = $runId
        benchmarkScenario = 'empty_hdrp_camera'
        buildKind = 'release'
        qualityLevel = 'High Fidelity'
        antialiasingMode = 'TAA'
        renderScalePercent = 100
        developmentBuild = $false
        width = 1280
        height = 720
        graphicsDeviceName = 'NVIDIA GeForce MX550'
        graphicsDeviceType = 'Direct3D11'
        profilerEnabled = $false
        profilerBinaryLogEnabled = $false
        deepProfilingBuild = $false
        measurementEligible = $true
        scenarios = @($Scenarios)
        phase = 'contract_phase'
    }
    $manifest = [pscustomobject]@{
        schemaVersion = 'r2-perf1-manifest/1'
        runId = $runId
        status = 'awaiting_offline_validation'
        scenario = 'empty_hdrp_camera'
        buildKind = 'release'
        quality = 'High Fidelity'
        antialiasing = 'TAA'
        renderScalePercent = 100
        screenshotRequested = $false
        screenshotPath = ''
        reportJsonPath = $reportPath
        reportMarkdownPath = $markdownPath
    }
    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    'contract report' | Set-Content -LiteralPath $markdownPath -Encoding UTF8
    $manifest | ConvertTo-Json -Depth 12 |
        Set-Content -LiteralPath (Join-Path $directory 'runtime.manifest.json') -Encoding UTF8
    return [pscustomobject]@{
        Directory = $directory
        RunId = $runId
        ReportPath = $reportPath
        ManifestPath = Join-Path $directory 'runtime.manifest.json'
    }
}

function Invoke-ContractValidation {
    param([object]$Fixture)
    return Test-RunOutputs `
        -RunDirectory $Fixture.Directory `
        -RunId $Fixture.RunId `
        -ExpectedScenario 'empty_hdrp_camera' `
        -ExpectedBuildKind 'release' `
        -ExpectedQuality 'High Fidelity' `
        -ExpectedAntialiasing 'TAA' `
        -ExpectedRenderScalePercent 100 `
        -ExpectedGpuName 'NVIDIA GeForce MX550' `
        -ExpectedWidth 1280 `
        -ExpectedHeight 720 `
        -ScreenshotExpected $false
}

$matching = New-ContractFixture -Label 'matching' -Scenarios @((New-ContractScenario))
$matchingResult = Invoke-ContractValidation $matching
Assert-Contract $matchingResult.Valid 'matching manifest/report must validate without throwing'
Assert-Contract (-not [bool]$matchingResult.PairingEligible) 'v1 evidence must never be pairing-eligible'
Assert-Contract ([string]::IsNullOrEmpty((Get-Content $matching.ManifestPath -Raw | ConvertFrom-Json).screenshotPath)) 'disabled screenshot must allow an empty path'
Assert-Contract ((Split-Path $matching.ManifestPath -Leaf) -eq 'runtime.manifest.json') 'short manifest discovery changed'
Assert-Contract ((Split-Path $matching.ReportPath -Leaf) -eq 'runtime.report.json') 'short report discovery changed'

$manifestMismatch = New-ContractFixture -Label 'manifest_mismatch' -Scenarios @((New-ContractScenario))
$manifestObject = Get-Content $manifestMismatch.ManifestPath -Raw | ConvertFrom-Json
$manifestObject.quality = 'Balanced'
$manifestObject | ConvertTo-Json -Depth 12 | Set-Content $manifestMismatch.ManifestPath -Encoding UTF8
$manifestMismatchResult = Invoke-ContractValidation $manifestMismatch
Assert-Contract (-not $manifestMismatchResult.Valid) 'manifest mismatch must be rejected'
Assert-Contract ($manifestMismatchResult.Reason -eq 'manifest quality mismatch') 'manifest mismatch reason changed'

$reportMismatch = New-ContractFixture -Label 'report_mismatch' -Scenarios @((New-ContractScenario))
$reportObject = Get-Content $reportMismatch.ReportPath -Raw | ConvertFrom-Json
$reportObject.qualityLevel = 'Balanced'
$reportObject | ConvertTo-Json -Depth 12 | Set-Content $reportMismatch.ReportPath -Encoding UTF8
$reportMismatchResult = Invoke-ContractValidation $reportMismatch
Assert-Contract (-not $reportMismatchResult.Valid) 'report mismatch must be rejected'
Assert-Contract ($reportMismatchResult.Reason -eq 'report quality mismatch') 'report mismatch reason changed'

$zeroManifest = New-ContractFixture -Label 'zero_manifest' -Scenarios @((New-ContractScenario))
Remove-Item -LiteralPath $zeroManifest.ManifestPath
$zeroManifestResult = Invoke-ContractValidation $zeroManifest
Assert-Contract ($zeroManifestResult.Reason -match 'found 0') 'zero manifest count must be structured'

$multipleManifest = New-ContractFixture -Label 'multiple_manifest' -Scenarios @((New-ContractScenario))
Copy-Item $multipleManifest.ManifestPath (Join-Path $multipleManifest.Directory 'duplicate.manifest.json')
$multipleManifestResult = Invoke-ContractValidation $multipleManifest
Assert-Contract ($multipleManifestResult.Reason -match 'found 2') 'multiple manifest count must be structured'

$zeroScenario = New-ContractFixture -Label 'zero_scenario' -Scenarios @()
$zeroScenarioResult = Invoke-ContractValidation $zeroScenario
Assert-Contract ($zeroScenarioResult.Reason -match 'was not found') 'zero scenarios must be rejected before indexing'

$missingScenario = New-ContractFixture -Label 'missing_scenario' -Scenarios @((New-ContractScenario -Name 'ground_only'))
$missingScenarioResult = Invoke-ContractValidation $missingScenario
Assert-Contract ($missingScenarioResult.Reason -match 'was not found') 'missing expected scenario must be rejected'

$duplicateScenario = New-ContractFixture -Label 'duplicate_scenario' -Scenarios @((New-ContractScenario), (New-ContractScenario))
$duplicateScenarioResult = Invoke-ContractValidation $duplicateScenario
Assert-Contract ($duplicateScenarioResult.Reason -match 'duplicated') 'duplicate expected scenario must be rejected'

$extraScenario = New-ContractFixture -Label 'extra_scenario' -Scenarios @((New-ContractScenario), (New-ContractScenario -Name 'ground_only'))
$extraScenarioResult = Invoke-ContractValidation $extraScenario
Assert-Contract ($extraScenarioResult.Reason -match 'unexpected extra') 'multiple scenarios must be rejected safely'

$incomplete = New-ContractFixture -Label 'incomplete' -Scenarios @((New-ContractScenario -BudgetStatus 'INCOMPLETE' -BudgetFailure 'counter unavailable'))
$incompleteResult = Invoke-ContractValidation $incomplete
Assert-Contract (-not $incompleteResult.Valid) 'INCOMPLETE evidence must be rejected'
Assert-Contract ($incompleteResult.Reason -match 'completeness gate') 'INCOMPLETE reason changed'

$gcUnavailable = New-ContractFixture -Label 'gc_unavailable' -Scenarios @((New-ContractScenario -GcAvailable $false))
$gcUnavailableResult = Invoke-ContractValidation $gcUnavailable
Assert-Contract (-not $gcUnavailableResult.Valid) 'unavailable GC counter must be rejected even when value is zero'
Assert-Contract ($gcUnavailableResult.Reason -match 'gcAllocationAvailable') 'GC availability reason changed'

$requiredUnavailable = New-ContractFixture -Label 'required_unavailable' -Scenarios @((New-ContractScenario -GfxMemoryAvailable $false))
$requiredUnavailableResult = Invoke-ContractValidation $requiredUnavailable
Assert-Contract (-not $requiredUnavailableResult.Valid) 'unavailable required metric must be rejected'

$budgetFail = New-ContractFixture -Label 'budget_fail' -Scenarios @((New-ContractScenario -BudgetStatus 'FAIL' -BudgetFailure 'p95 exceeded'))
$budgetFailResult = Invoke-ContractValidation $budgetFail
Assert-Contract $budgetFailResult.Valid 'complete FAIL data remains valid evidence'
Assert-Contract ($budgetFailResult.ProductBudgetStatus -eq 'FAIL') 'product budget status was not preserved'
Assert-Contract (-not [bool]$budgetFailResult.ProductBudgetPassed) 'product budget failure was confused with evidence invalidity'

$exceptionRoot = Join-Path $ContractRoot 'exception_boundary'
New-Item -ItemType Directory -Path $exceptionRoot -Force | Out-Null
$exceptionResult = New-ValidatorExceptionRunResult `
    -RunDirectory $exceptionRoot `
    -RunId 'exception_run' `
    -Exception ([InvalidOperationException]::new('synthetic validator failure')) `
    -ExitCode 0 `
    -Scenario 'empty_hdrp_camera' `
    -Quality 'High Fidelity' `
    -Antialiasing 'TAA' `
    -RenderScalePercent 100 `
    -BuildKind 'release' `
    -MeasurementRole 'release_performance' `
    -MeasurementEligible $true
Assert-Contract ($exceptionResult.status -eq 'invalid') 'validator exception must return structured invalid result'
$exceptionManifest = Get-Content (Join-Path $exceptionRoot 'offline.invalid.manifest.json') -Raw | ConvertFrom-Json
Assert-Contract ($exceptionManifest.status -eq 'invalid') 'validator exception manifest must be invalid'
Assert-Contract ($exceptionManifest.statusReason -match 'synthetic validator failure') 'validator exception reason was not persisted'
$tablesRoot = Join-Path $ContractRoot 'exception_tables'
New-Item -ItemType Directory -Path $tablesRoot -Force | Out-Null
Write-ResultTables -Results @($exceptionResult) -Directory $tablesRoot
$validRows = @(Import-Csv (Join-Path $tablesRoot 'valid-runs.csv'))
$invalidRows = @(Import-Csv (Join-Path $tablesRoot 'invalid-runs.csv'))
Assert-Contract ($validRows.Count -eq 0) 'validator exception leaked into valid-runs.csv'
Assert-Contract ($invalidRows.Count -eq 1) 'validator exception must create exactly one invalid row'
Assert-Contract ($invalidRows[0].runId -eq 'exception_run') 'invalid row runId changed'

function New-V2ContractFixture {
    param(
        [string]$Label,
        [ValidateSet('release_performance', 'development_gc')]
        [string]$Role,
        [string]$PerformanceStatus,
        [string]$GcStatus,
        [bool]$GcAvailable,
        [bool]$TotalMemoryAvailable = $true,
        [bool]$GfxMemoryAvailable = $true,
        [bool]$TextureMemoryAvailable = $true,
        [string]$SourceCommit = '2e55294f9321819e770914eafaab6a449d6e92db'
    )
    $directory = Join-Path $ContractRoot $Label
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $runId = 'run_' + $Label
    $buildKind = if ($Role -ceq 'release_performance') { 'release' } else { 'diagnostic' }
    $developmentBuild = $buildKind -eq 'diagnostic'
    $reportPath = Join-Path $directory 'runtime.report.json'
    $markdownPath = Join-Path $directory 'runtime.report.md'
    $scenario = [pscustomobject]@{
        name = 'empty_hdrp_camera'
        frames = 480
        avgMs = 5.0
        medianMs = 5.0
        p95Ms = 6.0
        p99Ms = 7.0
        avgFps = 200.0
        onePercentLowFps = 142.0
        timingSamples = 480
        cpuTotalAvgMs = 4.0
        cpuTotalP95Ms = 5.0
        cpuMainThreadAvgMs = 3.5
        cpuMainThreadP95Ms = 4.5
        cpuMainThreadPresentWaitAvgMs = 0.5
        cpuMainThreadWorkAvgMs = 3.0
        cpuRenderThreadAvgMs = 1.0
        cpuRenderThreadP95Ms = 1.5
        gpuAvgMs = 2.0
        gpuP95Ms = 2.5
        drawCalls = 12.0
        batches = 10.0
        setPassCalls = 5.0
        triangleMillions = 0.01
        vertexMillions = 0.02
        globalGcAllocationAvailable = $GcAvailable
        globalGcMetricSource = if ($GcAvailable) { 'unity_profiler_recorder' } else { '' }
        globalGcMetricScope = if ($GcAvailable) { 'unity_gc_allocated_in_frame' } else { '' }
        globalGcDiagnosticOnly = $false
        globalGcAllocatedAverageBytes = 0.0
        globalGcAllocatedPeakBytes = 0
        globalGcAllocationCountAverage = 0.0
        totalUsedMemoryAverageMb = 200.0
        totalUsedMemoryPeakMb = 205.0
        gfxUsedMemoryAverageMb = 100.0
        gfxUsedMemoryPeakMb = 105.0
        textureMemoryAverageMb = 50.0
        textureMemoryPeakMb = 52.0
        drawCallsAvailable = $true
        batchesAvailable = $true
        setPassCallsAvailable = $true
        trianglesAvailable = $true
        verticesAvailable = $true
        totalUsedMemoryAvailable = $TotalMemoryAvailable
        gfxUsedMemoryAvailable = $GfxMemoryAvailable
        textureMemoryAvailable = $TextureMemoryAvailable
        bottleneck = 'GPU'
        performanceBudgetStatus = $PerformanceStatus
        performanceBudgetFailure = if ($PerformanceStatus -eq 'FAIL') { 'p95 exceeded' } else { '' }
        gcBudgetStatus = $GcStatus
        gcBudgetFailure = if ($GcStatus -in @('FAIL', 'INCOMPLETE')) { 'global GC unavailable or over budget' } else { '' }
        ignoredStartupStallFrames = 0
    }
    $report = [pscustomobject]@{
        schemaVersion = 'r2-perf1/2'
        benchmarkRunId = $runId
        benchmarkScenario = 'empty_hdrp_camera'
        buildKind = $buildKind
        measurementRole = $Role
        measurementSetId = ''
        sourceCommit = $SourceCommit
        sourceTreeClean = $false
        sourceTreeCleanAvailable = $false
        buildArtifactId = ''
        contentFingerprint = ''
        configurationFingerprint = ''
        hardwareFingerprint = ''
        repositoryReproducible = $false
        contentTrackingStatus = ''
        comparisonScope = ''
        pairingEligible = $false
        evidenceValidity = 'PENDING_OFFLINE_VALIDATION'
        performanceBudgetStatus = $PerformanceStatus
        gcBudgetStatus = $GcStatus
        aggregateProductGate = 'INCOMPLETE'
        qualityLevel = 'High Fidelity'
        antialiasingMode = 'TAA'
        renderScalePercent = 100
        developmentBuild = $developmentBuild
        width = 1280
        height = 720
        graphicsDeviceName = 'NVIDIA GeForce MX550'
        graphicsDeviceType = 'Direct3D11'
        profilerEnabled = $false
        profilerBinaryLogEnabled = $false
        deepProfilingBuild = $false
        measurementEligible = $true
        screenshotRequested = $false
        scenarios = @($scenario)
        phase = 'contract_phase'
    }
    $manifest = [pscustomobject]@{
        schemaVersion = 'r2-perf1-manifest/2'
        runId = $runId
        status = 'awaiting_offline_validation'
        scenario = 'empty_hdrp_camera'
        buildKind = $buildKind
        measurementRole = $Role
        measurementSetId = ''
        sourceCommit = $SourceCommit
        sourceTreeClean = $false
        sourceTreeCleanAvailable = $false
        buildArtifactId = ''
        contentFingerprint = ''
        configurationFingerprint = ''
        hardwareFingerprint = ''
        repositoryReproducible = $false
        contentTrackingStatus = ''
        comparisonScope = ''
        pairingEligible = $false
        evidenceValidity = 'PENDING_OFFLINE_VALIDATION'
        performanceBudgetStatus = $PerformanceStatus
        gcBudgetStatus = $GcStatus
        aggregateProductGate = 'INCOMPLETE'
        quality = 'High Fidelity'
        antialiasing = 'TAA'
        renderScalePercent = 100
        upscaler = 'CatmullRom'
        screenshotRequested = $false
        screenshotPath = ''
        reportJsonPath = $reportPath
        reportMarkdownPath = $markdownPath
    }
    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    'contract report' | Set-Content -LiteralPath $markdownPath -Encoding UTF8
    $manifest | ConvertTo-Json -Depth 12 |
        Set-Content -LiteralPath (Join-Path $directory 'runtime.manifest.json') -Encoding UTF8
    return [pscustomobject]@{
        Directory = $directory
        RunId = $runId
        BuildKind = $buildKind
        Role = $Role
        ManifestPath = Join-Path $directory 'runtime.manifest.json'
        ReportPath = $reportPath
    }
}

function Invoke-V2ContractValidation {
    param(
        [object]$Fixture,
        [string]$ExpectedSourceCommit = '2e55294f9321819e770914eafaab6a449d6e92db'
    )
    return Test-RunOutputs `
        -RunDirectory $Fixture.Directory `
        -RunId $Fixture.RunId `
        -ExpectedScenario 'empty_hdrp_camera' `
        -ExpectedBuildKind $Fixture.BuildKind `
        -ExpectedMeasurementRole $Fixture.Role `
        -ExpectedSourceCommit $ExpectedSourceCommit `
        -ExpectedQuality 'High Fidelity' `
        -ExpectedAntialiasing 'TAA' `
        -ExpectedRenderScalePercent 100 `
        -ExpectedGpuName 'NVIDIA GeForce MX550' `
        -ExpectedWidth 1280 `
        -ExpectedHeight 720 `
        -ScreenshotExpected $false
}

$releaseNoGc = New-V2ContractFixture -Label 'v2_release_no_gc' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$releaseNoGcResult = Invoke-V2ContractValidation $releaseNoGc
Assert-Contract $releaseNoGcResult.Valid 'release evidence must remain valid when GC is unavailable'
Assert-Contract ($releaseNoGcResult.PerformanceBudgetStatus -eq 'PASS') 'release performance authority changed'
Assert-Contract ($releaseNoGcResult.GcBudgetStatus -eq 'NOT_AUTHORITY') 'release must not claim GC authority'
Assert-Contract ($releaseNoGcResult.AggregateProductGate -eq 'INCOMPLETE') 'single release member cannot complete aggregate gate'

$releaseFail = New-V2ContractFixture -Label 'v2_release_fail' -Role 'release_performance' -PerformanceStatus 'FAIL' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$releaseFailResult = Invoke-V2ContractValidation $releaseFail
Assert-Contract $releaseFailResult.Valid 'budget failure must remain valid evidence'
Assert-Contract ($releaseFailResult.PerformanceBudgetStatus -eq 'FAIL') 'performance FAIL was not preserved'

$developmentGc = New-V2ContractFixture -Label 'v2_development_gc' -Role 'development_gc' -PerformanceStatus 'NOT_AUTHORITY' -GcStatus 'PASS' -GcAvailable $true
$developmentGcResult = Invoke-V2ContractValidation $developmentGc
Assert-Contract $developmentGcResult.Valid 'authoritative development GC evidence must validate'
Assert-Contract ($developmentGcResult.PerformanceBudgetStatus -eq 'NOT_AUTHORITY') 'development timing must not become authoritative'
Assert-Contract ($developmentGcResult.GcBudgetStatus -eq 'PASS') 'development global GC PASS was not preserved'
Assert-Contract ($developmentGcResult.AggregateProductGate -eq 'INCOMPLETE') 'single GC member cannot complete aggregate gate'

$developmentMissingGc = New-V2ContractFixture -Label 'v2_development_missing_gc' -Role 'development_gc' -PerformanceStatus 'NOT_AUTHORITY' -GcStatus 'INCOMPLETE' -GcAvailable $false
$developmentMissingGcResult = Invoke-V2ContractValidation $developmentMissingGc
Assert-Contract (-not $developmentMissingGcResult.Valid) 'missing authoritative GC recorder must not pass'
Assert-Contract ($developmentMissingGcResult.GcBudgetStatus -ne 'PASS') 'missing global GC was converted into PASS'

$memoryContracts = @(Get-MemoryDiagnosticContracts)
$expectedMemoryAvailabilityFields = @('gfxUsedMemoryAvailable', 'textureMemoryAvailable', 'totalUsedMemoryAvailable')
$actualMemoryAvailabilityFields = @($memoryContracts | ForEach-Object { [string]$_.AvailabilityField } | Sort-Object)
Assert-Contract (($actualMemoryAvailabilityFields -join ',') -ceq ($expectedMemoryAvailabilityFields -join ',')) 'memory diagnostic authority table is incomplete'

foreach ($memoryContract in $memoryContracts) {
    $availabilityField = [string]$memoryContract.AvailabilityField
    foreach ($role in @('release_performance', 'development_gc')) {
        $parameters = @{
            Label = ('v2_optional_' + $role + '_' + $availabilityField)
            Role = $role
            PerformanceStatus = if ($role -ceq 'release_performance') { 'PASS' } else { 'NOT_AUTHORITY' }
            GcStatus = if ($role -ceq 'release_performance') { 'NOT_AUTHORITY' } else { 'PASS' }
            GcAvailable = $role -ceq 'development_gc'
        }
        if ($availabilityField -ceq 'totalUsedMemoryAvailable') { $parameters.TotalMemoryAvailable = $false }
        elseif ($availabilityField -ceq 'gfxUsedMemoryAvailable') { $parameters.GfxMemoryAvailable = $false }
        elseif ($availabilityField -ceq 'textureMemoryAvailable') { $parameters.TextureMemoryAvailable = $false }
        $fixture = New-V2ContractFixture @parameters
        $value = Get-Content $fixture.ReportPath -Raw | ConvertFrom-Json
        foreach ($metricField in @($memoryContract.MetricFields)) {
            $value.scenarios[0].PSObject.Properties.Remove([string]$metricField)
        }
        $value | ConvertTo-Json -Depth 12 | Set-Content $fixture.ReportPath -Encoding UTF8
        $result = Invoke-V2ContractValidation $fixture
        Assert-Contract $result.Valid ($role + ' must allow unavailable optional diagnostic ' + $availabilityField)
    }

    $missingFixture = New-V2ContractFixture -Label ('v2_missing_' + $availabilityField) -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
    $missingValue = Get-Content $missingFixture.ReportPath -Raw | ConvertFrom-Json
    $missingValue.scenarios[0].PSObject.Properties.Remove([string]$memoryContract.MetricFields[0])
    $missingValue | ConvertTo-Json -Depth 12 | Set-Content $missingFixture.ReportPath -Encoding UTF8
    $missingResult = Invoke-V2ContractValidation $missingFixture
    Assert-Contract (-not $missingResult.Valid) ('available diagnostic must require ' + $memoryContract.MetricFields[0])

    $wrongTypeFixture = New-V2ContractFixture -Label ('v2_wrong_type_' + $availabilityField) -Role 'development_gc' -PerformanceStatus 'NOT_AUTHORITY' -GcStatus 'PASS' -GcAvailable $true
    $wrongTypeValue = Get-Content $wrongTypeFixture.ReportPath -Raw | ConvertFrom-Json
    $wrongTypeValue.scenarios[0].($memoryContract.MetricFields[1]) = '1.0'
    $wrongTypeValue | ConvertTo-Json -Depth 12 | Set-Content $wrongTypeFixture.ReportPath -Encoding UTF8
    $wrongTypeResult = Invoke-V2ContractValidation $wrongTypeFixture
    Assert-Contract (-not $wrongTypeResult.Valid) ('available diagnostic must reject wrong type for ' + $memoryContract.MetricFields[1])
}

$uppercaseCommit = '2E55294F9321819E770914EAFAAB6A449D6E92DB'
$lowercaseCommit = $uppercaseCommit.ToLowerInvariant()
$lowerSidecarUpperRuntime = New-V2ContractFixture -Label 'v2_lower_sidecar_upper_runtime' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false -SourceCommit $uppercaseCommit
$lowerSidecarUpperRuntimeResult = Invoke-V2ContractValidation $lowerSidecarUpperRuntime -ExpectedSourceCommit $lowercaseCommit
Assert-Contract $lowerSidecarUpperRuntimeResult.Valid 'lowercase sidecar + uppercase runtime must validate'

$upperSidecarLowerRuntime = New-V2ContractFixture -Label 'v2_upper_sidecar_lower_runtime' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false -SourceCommit $lowercaseCommit
$upperSidecarLowerRuntimeResult = Invoke-V2ContractValidation $upperSidecarLowerRuntime -ExpectedSourceCommit $uppercaseCommit
Assert-Contract $upperSidecarLowerRuntimeResult.Valid 'uppercase sidecar + lowercase runtime must validate'

$differentCommit = New-V2ContractFixture -Label 'v2_different_commit' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false -SourceCommit ('3' + $lowercaseCommit.Substring(1))
$differentCommitResult = Invoke-V2ContractValidation $differentCommit -ExpectedSourceCommit $lowercaseCommit
Assert-Contract (-not $differentCommitResult.Valid) 'different source commit must be rejected'

foreach ($malformedCommit in @('not-hex', ('a' * 39), ('a' * 41), (('a' * 39) + 'g'))) {
    $malformedFixture = New-V2ContractFixture -Label ('v2_malformed_commit_' + $malformedCommit.Length) -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false -SourceCommit $malformedCommit
    $malformedResult = Invoke-V2ContractValidation $malformedFixture -ExpectedSourceCommit $lowercaseCommit
    Assert-Contract (-not $malformedResult.Valid) 'malformed source commit must be rejected'
}

$missingRole = New-V2ContractFixture -Label 'v2_missing_role' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$missingRoleManifest = Get-Content $missingRole.ManifestPath -Raw | ConvertFrom-Json
$missingRoleManifest.measurementRole = ''
$missingRoleManifest | ConvertTo-Json -Depth 12 | Set-Content $missingRole.ManifestPath -Encoding UTF8
$missingRoleResult = Invoke-V2ContractValidation $missingRole
Assert-Contract (-not $missingRoleResult.Valid) 'missing runtime role must be rejected'

$roleMismatch = New-V2ContractFixture -Label 'v2_role_mismatch' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$roleMismatchReport = Get-Content $roleMismatch.ReportPath -Raw | ConvertFrom-Json
$roleMismatchReport.measurementRole = 'development_gc'
$roleMismatchReport | ConvertTo-Json -Depth 12 | Set-Content $roleMismatch.ReportPath -Encoding UTF8
$roleMismatchResult = Invoke-V2ContractValidation $roleMismatch
Assert-Contract (-not $roleMismatchResult.Valid) 'runtime report role mismatch must be rejected'

$roleBuildMismatch = New-V2ContractFixture -Label 'v2_role_build_mismatch' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$roleBuildMismatch.BuildKind = 'diagnostic'
$roleBuildMismatchResult = Invoke-V2ContractValidation $roleBuildMismatch
Assert-Contract (-not $roleBuildMismatchResult.Valid) 'role/build mismatch must fail closed'

$missingManifestProperty = New-V2ContractFixture -Label 'v2_missing_manifest_property' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$missingManifestPropertyValue = Get-Content $missingManifestProperty.ManifestPath -Raw | ConvertFrom-Json
$missingManifestPropertyValue.PSObject.Properties.Remove('sourceCommit')
$missingManifestPropertyValue | ConvertTo-Json -Depth 12 | Set-Content $missingManifestProperty.ManifestPath -Encoding UTF8
$missingManifestPropertyResult = Invoke-V2ContractValidation $missingManifestProperty
Assert-Contract (-not $missingManifestPropertyResult.Valid) 'missing mandatory manifest property must fail closed'
Assert-Contract ($missingManifestPropertyResult.Reason -match 'missing required JSON property: sourceCommit') 'missing manifest property reason changed'

$wrongManifestType = New-V2ContractFixture -Label 'v2_wrong_manifest_type' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$wrongManifestTypeValue = Get-Content $wrongManifestType.ManifestPath -Raw | ConvertFrom-Json
$wrongManifestTypeValue.sourceTreeCleanAvailable = 'false'
$wrongManifestTypeValue | ConvertTo-Json -Depth 12 | Set-Content $wrongManifestType.ManifestPath -Encoding UTF8
$wrongManifestTypeResult = Invoke-V2ContractValidation $wrongManifestType
Assert-Contract (-not $wrongManifestTypeResult.Valid) 'wrong manifest property type must fail closed'
Assert-Contract ($wrongManifestTypeResult.Reason -match 'wrong type') 'wrong manifest type reason changed'

$wrongManifestCase = New-V2ContractFixture -Label 'v2_wrong_manifest_case' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$wrongManifestCaseValue = Get-Content $wrongManifestCase.ManifestPath -Raw | ConvertFrom-Json
$wrongManifestCaseValue.performanceBudgetStatus = 'pass'
$wrongManifestCaseValue | ConvertTo-Json -Depth 12 | Set-Content $wrongManifestCase.ManifestPath -Encoding UTF8
$wrongManifestCaseResult = Invoke-V2ContractValidation $wrongManifestCase
Assert-Contract (-not $wrongManifestCaseResult.Valid) 'wrong-case manifest status must fail closed'
Assert-Contract ($wrongManifestCaseResult.Reason -match 'wrong-case') 'wrong-case manifest status reason changed'

$fabricatedManifestProvenance = New-V2ContractFixture -Label 'v2_fabricated_manifest_provenance' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$fabricatedManifestProvenanceValue = Get-Content $fabricatedManifestProvenance.ManifestPath -Raw | ConvertFrom-Json
$fabricatedManifestProvenanceValue.sourceCommit = 'fabricated-commit'
$fabricatedManifestProvenanceValue | ConvertTo-Json -Depth 12 | Set-Content $fabricatedManifestProvenance.ManifestPath -Encoding UTF8
$fabricatedManifestProvenanceResult = Invoke-V2ContractValidation $fabricatedManifestProvenance
Assert-Contract (-not $fabricatedManifestProvenanceResult.Valid) 'non-empty manifest provenance placeholder must fail closed'

$missingReportProperty = New-V2ContractFixture -Label 'v2_missing_report_property' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$missingReportPropertyValue = Get-Content $missingReportProperty.ReportPath -Raw | ConvertFrom-Json
$missingReportPropertyValue.PSObject.Properties.Remove('hardwareFingerprint')
$missingReportPropertyValue | ConvertTo-Json -Depth 12 | Set-Content $missingReportProperty.ReportPath -Encoding UTF8
$missingReportPropertyResult = Invoke-V2ContractValidation $missingReportProperty
Assert-Contract (-not $missingReportPropertyResult.Valid) 'missing mandatory report property must fail closed'
Assert-Contract ($missingReportPropertyResult.Reason -match 'missing required JSON property: hardwareFingerprint') 'missing report property reason changed'

$wrongReportType = New-V2ContractFixture -Label 'v2_wrong_report_type' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$wrongReportTypeValue = Get-Content $wrongReportType.ReportPath -Raw | ConvertFrom-Json
$wrongReportTypeValue.pairingEligible = 'false'
$wrongReportTypeValue | ConvertTo-Json -Depth 12 | Set-Content $wrongReportType.ReportPath -Encoding UTF8
$wrongReportTypeResult = Invoke-V2ContractValidation $wrongReportType
Assert-Contract (-not $wrongReportTypeResult.Valid) 'wrong report property type must fail closed'
Assert-Contract ($wrongReportTypeResult.Reason -match 'wrong type') 'wrong report type reason changed'

$wrongReportCase = New-V2ContractFixture -Label 'v2_wrong_report_case' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$wrongReportCaseValue = Get-Content $wrongReportCase.ReportPath -Raw | ConvertFrom-Json
$wrongReportCaseValue.gcBudgetStatus = 'not_authority'
$wrongReportCaseValue | ConvertTo-Json -Depth 12 | Set-Content $wrongReportCase.ReportPath -Encoding UTF8
$wrongReportCaseResult = Invoke-V2ContractValidation $wrongReportCase
Assert-Contract (-not $wrongReportCaseResult.Valid) 'wrong-case report status must fail closed'
Assert-Contract ($wrongReportCaseResult.Reason -match 'wrong-case') 'wrong-case report status reason changed'

$fabricatedReportProvenance = New-V2ContractFixture -Label 'v2_fabricated_report_provenance' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$fabricatedReportProvenanceValue = Get-Content $fabricatedReportProvenance.ReportPath -Raw | ConvertFrom-Json
$fabricatedReportProvenanceValue.hardwareFingerprint = 'fabricated-hardware'
$fabricatedReportProvenanceValue | ConvertTo-Json -Depth 12 | Set-Content $fabricatedReportProvenance.ReportPath -Encoding UTF8
$fabricatedReportProvenanceResult = Invoke-V2ContractValidation $fabricatedReportProvenance
Assert-Contract (-not $fabricatedReportProvenanceResult.Valid) 'non-empty report provenance placeholder must fail closed'

foreach ($missingProfilerField in @('profilerEnabled', 'profilerBinaryLogEnabled', 'deepProfilingBuild')) {
    $fixture = New-V2ContractFixture -Label ('v2_missing_' + $missingProfilerField) -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
    $value = Get-Content $fixture.ReportPath -Raw | ConvertFrom-Json
    $value.PSObject.Properties.Remove($missingProfilerField)
    $value | ConvertTo-Json -Depth 12 | Set-Content $fixture.ReportPath -Encoding UTF8
    $result = Invoke-V2ContractValidation $fixture
    Assert-Contract (-not $result.Valid) ('missing ' + $missingProfilerField + ' must return structured invalid')
    Assert-Contract ($result.Reason -match ('missing required JSON property: ' + $missingProfilerField)) ('missing ' + $missingProfilerField + ' reason changed')
}

$availabilityString = New-V2ContractFixture -Label 'v2_availability_string' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$availabilityStringValue = Get-Content $availabilityString.ReportPath -Raw | ConvertFrom-Json
$availabilityStringValue.scenarios[0].drawCallsAvailable = 'false'
$availabilityStringValue | ConvertTo-Json -Depth 12 | Set-Content $availabilityString.ReportPath -Encoding UTF8
$availabilityStringResult = Invoke-V2ContractValidation $availabilityString
Assert-Contract (-not $availabilityStringResult.Valid) 'string availability flag must return structured invalid'
Assert-Contract ($availabilityStringResult.Reason -match 'wrong type') 'string availability reason changed'

$widthString = New-V2ContractFixture -Label 'v2_width_string' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$widthStringValue = Get-Content $widthString.ReportPath -Raw | ConvertFrom-Json
$widthStringValue.width = '1280'
$widthStringValue | ConvertTo-Json -Depth 12 | Set-Content $widthString.ReportPath -Encoding UTF8
$widthStringResult = Invoke-V2ContractValidation $widthString
Assert-Contract (-not $widthStringResult.Valid) 'string width must return structured invalid'
Assert-Contract ($widthStringResult.Reason -match 'wrong type') 'string width reason changed'

$numericString = New-V2ContractFixture -Label 'v2_numeric_string' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$numericStringValue = Get-Content $numericString.ReportPath -Raw | ConvertFrom-Json
$numericStringValue.scenarios[0].avgMs = '5.0'
$numericStringValue | ConvertTo-Json -Depth 12 | Set-Content $numericString.ReportPath -Encoding UTF8
$numericStringResult = Invoke-V2ContractValidation $numericString
Assert-Contract (-not $numericStringResult.Valid) 'string numeric metric must return structured invalid'
Assert-Contract ($numericStringResult.Reason -match 'wrong type') 'string numeric metric reason changed'

$missingAvailableMetric = New-V2ContractFixture -Label 'v2_available_metric_missing' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$missingAvailableMetricValue = Get-Content $missingAvailableMetric.ReportPath -Raw | ConvertFrom-Json
$missingAvailableMetricValue.PSObject.Properties['scenarios'].Value[0].PSObject.Properties.Remove('drawCalls')
$missingAvailableMetricValue | ConvertTo-Json -Depth 12 | Set-Content $missingAvailableMetric.ReportPath -Encoding UTF8
$missingAvailableMetricResult = Invoke-V2ContractValidation $missingAvailableMetric
Assert-Contract (-not $missingAvailableMetricResult.Valid) 'available metric without value must return structured invalid'
Assert-Contract ($missingAvailableMetricResult.Reason -match 'missing required JSON property: drawCalls') 'missing available metric reason changed'

$booleanMetric = New-V2ContractFixture -Label 'v2_boolean_metric' -Role 'release_performance' -PerformanceStatus 'PASS' -GcStatus 'NOT_AUTHORITY' -GcAvailable $false
$booleanMetricValue = Get-Content $booleanMetric.ReportPath -Raw | ConvertFrom-Json
$booleanMetricValue.scenarios[0].avgMs = $false
$booleanMetricValue | ConvertTo-Json -Depth 12 | Set-Content $booleanMetric.ReportPath -Encoding UTF8
$booleanMetricResult = Invoke-V2ContractValidation $booleanMetric
Assert-Contract (-not $booleanMetricResult.Valid) 'boolean numeric metric must return structured invalid'
Assert-Contract ($booleanMetricResult.Reason -match 'wrong type') 'boolean numeric metric reason changed'

Write-Output 'B4_VALIDATOR_MATRIX_PASS B5A_AUTHORITY_MATRIX_PASS'
";
    }
}
