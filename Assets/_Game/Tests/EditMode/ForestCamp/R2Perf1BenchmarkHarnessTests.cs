using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
            string temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "sotf-r2-perf1-startup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
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
                Assert.That(exitCode, Is.Not.EqualTo(0));
                Assert.That(File.Exists(manifestPath), Is.True);
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
                Directory.Delete(temporaryDirectory, true);
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
    }
}
