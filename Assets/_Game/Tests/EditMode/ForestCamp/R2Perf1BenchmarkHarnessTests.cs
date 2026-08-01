using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SonsOfTheForest.Tests.ForestCamp.EditMode
{
    public sealed class R2Perf1BenchmarkHarnessTests
    {
        private const string ConfigurationTypeName =
            "SonsOfTheForest.Infrastructure.Benchmark.R2Perf1BenchmarkConfiguration, Assembly-CSharp";
        private const string RunnerTypeName =
            "SonsOfTheForest.Infrastructure.Benchmark.ForestBenchmarkRunner, Assembly-CSharp";
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

        private static Type ConfigurationType =>
            Type.GetType(ConfigurationTypeName, true);

        private static Type RunnerType =>
            Type.GetType(RunnerTypeName, true);

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
                "-sotf-no-screenshot", "true",
                "-sotf-aa", "TAA",
                "-sotf-render-scale", "80",
                "-sotf-upscaler", "CatmullRom");

            Assert.That(Field<bool>(settings, "enabled"), Is.True);
            Assert.That(Field<string>(settings, "quality"), Is.EqualTo("Balanced"));
            Assert.That(Field<string>(settings, "scenario"), Is.EqualTo("ground_only"));
            Assert.That(Field<string>(settings, "runId"), Is.EqualTo("balanced_ground_r1"));
            Assert.That(Field<string>(settings, "buildKind"), Is.EqualTo("release"));
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
                "player.exe", "-sotf-perf1", "-sotf-aa", value);
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
                    "player.exe", "-sotf-perf1", "-sotf-aa", value));
            }
        }

        [Test]
        public void QualityWithSpaces_RemainsOneCommandLineValue()
        {
            object settings = Parse(
                "player.exe",
                "-sotf-perf1",
                "-sotf-quality", "High Fidelity",
                "-sotf-aa", "TAA");

            Assert.That(Field<string>(settings, "quality"), Is.EqualTo("High Fidelity"));
            Assert.That(Field<string>(settings, "antialiasing"), Is.EqualTo("TAA"));
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
                "player.exe", "-sotf-perf1", "-sotf-scenario", scenario);
            Assert.That(Field<string>(settings, "scenario"), Is.EqualTo(scenario));
        }

        [TestCase("all")]
        [TestCase("forest_and_ground")]
        [TestCase("")]
        public void ScenarioValidation_RejectsInvalidValues(string scenario)
        {
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1", "-sotf-scenario", scenario);
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
                "player.exe", "-sotf-perf1", "-sotf-run-id", runId);
            Assert.That(Field<string>(settings, "runId"), Is.EqualTo(runId));
        }

        [TestCase("contains space")]
        [TestCase("path/escape")]
        [TestCase("dot.value")]
        public void RunIdValidation_RejectsUnsafeIds(string runId)
        {
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1", "-sotf-run-id", runId);
        }

        [Test]
        public void Screenshot_DefaultsOffAndCanBeEnabledOnlyExplicitly()
        {
            object defaultSettings = Parse("player.exe", "-sotf-perf1");
            object visualSettings = Parse(
                "player.exe", "-sotf-perf1", "-sotf-no-screenshot", "false");

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
                "player.exe", "-sotf-perf1", "-sotf-render-scale", "101");
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1", "-sotf-unknown", "value");
            AssertParseFailure<ArgumentException>(
                "player.exe", "-sotf-perf1", "-sotf-no-screenshot", "maybe");
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
            string temporaryDirectory = CreateLongRunDirectory(out cleanupRoot);
            try
            {
                object settings = Parse(
                    "player.exe",
                    "-sotf-perf1",
                    "-sotf-quality", "High Fidelity",
                    "-sotf-scenario", "empty_hdrp_camera",
                    "-sotf-run-id", "startup_failure_test",
                    "-sotf-build-kind", "release",
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
            Assert.That(builder, Does.Contain("Release builds must never auto-run"));
            Assert.That(builder, Does.Contain("BuildOptions.ConnectWithProfiler"));
            Assert.That(builder, Does.Contain("BuildOptions.EnableDeepProfilingSupport"));
            Assert.That(builder, Does.Contain("forbidden diagnostic or auto-run flag"));
            Assert.That(builder, Does.Contain("report.summary.options"));
            Assert.That(builder, Does.Contain("GetGraphicsAPIs"));
            Assert.That(builder, Does.Contain("GetArchitecture"));
            Assert.That(builder, Does.Contain("ComputeSha256"));
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
                                harnessPath + "\"",
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

        private static object Parse(params string[] arguments)
        {
            MethodInfo method = ConfigurationType.GetMethod(
                "Parse",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(null, new object[] { arguments });
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
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, "Missing static field: " + name);
            return (T)field.GetValue(null);
        }

        private static string CreateLongRunDirectory(out string cleanupRoot)
        {
            cleanupRoot = Path.Combine(
                Path.GetTempPath(),
                "sotf-b2-" + Guid.NewGuid().ToString("N"));
            int componentLength = Math.Max(32, 128 - cleanupRoot.Length);
            return Path.Combine(cleanupRoot, new string('r', componentLength));
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

Write-Output 'B4_VALIDATOR_MATRIX_PASS'
";
    }
}
