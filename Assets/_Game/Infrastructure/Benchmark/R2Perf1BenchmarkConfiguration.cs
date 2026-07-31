using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SonsOfTheForest.Infrastructure.Benchmark
{
    /// <summary>
    /// Command-line contract and reversible runtime configuration for the
    /// R2-PERF1 standalone-only benchmark. Every process measures exactly one
    /// quality/AA/render-scale/scenario tuple.
    /// </summary>
    public static class R2Perf1BenchmarkConfiguration
    {
        public const string EmptyScenario = "empty_hdrp_camera";
        public const string GroundScenario = "ground_only";
        public const string FullForestScenario = "full_forest";
        public const string DiagnosticBuildKind = "diagnostic";
        public const string ReleaseBuildKind = "release";
        public const string ManifestIncomplete = "incomplete";
        public const string ManifestAwaitingValidation = "awaiting_offline_validation";
        public const string ManifestValid = "valid";
        public const string ManifestInvalid = "invalid";
        public const string AntialiasingNone = "None";
        public const string AntialiasingFxaa = "FXAA";
        public const string AntialiasingSmaa = "SMAA";
        public const string AntialiasingTaa = "TAA";
        public const string RuntimeManifestFileName = "runtime.manifest.json";
        public const string RuntimeReportJsonFileName = "runtime.report.json";
        public const string RuntimeReportMarkdownFileName = "runtime.report.md";
        public const string RuntimeScreenshotFileName = "runtime.screenshot.png";
        public const string AtomicTemporarySuffix = ".tmp";
        public const int MaximumRuntimeOutputFileNameLength = 32;

        private static readonly string[] SupportedAntialiasingValues =
        {
            AntialiasingNone,
            AntialiasingFxaa,
            AntialiasingSmaa,
            AntialiasingTaa,
        };

        public sealed class Settings
        {
            public bool enabled;
            public string quality = "High Fidelity";
            public string scenario = FullForestScenario;
            public string runId = "run1";
            public string buildKind = DiagnosticBuildKind;
            public bool noScreenshot = true;
            public string antialiasing = "None";
            public int renderScalePercent = 100;
            public string upscaler = "CatmullRom";
            public bool fullscreenEffects = true;
            public bool shadows = true;
            public float warmupSeconds = 10f;
            public float sampleSeconds = 8f;
            public string driverVersion = "UNKNOWN";
            public string powerPlan = "UNKNOWN";
            public bool powerOnline;
            public string windowsGraphicsPreference = "UNKNOWN";
            public string outputDirectory = string.Empty;

            public bool CaptureScreenshot => !noScreenshot;
            public bool MeasurementEligible => noScreenshot;

            public string Phase => Sanitize(
                "r2_perf1_" + buildKind + "_" + scenario + "_" + quality + "_" +
                antialiasing + "_rs" + renderScalePercent + "_" + upscaler + "_" + runId);
        }

        private sealed class RuntimeStateSnapshot
        {
            public int qualityLevel;
            public int vSyncCount;
            public int targetFrameRate;
            public bool runInBackground;
            public float widthScale;
            public float heightScale;
            public HDRenderPipelineAsset originalPipelineAsset;
            public RenderPipelineSettings originalPipelineSettings;
            public HDRenderPipelineAsset configuredPipelineAsset;
            public RenderPipelineSettings configuredPipelineSettings;
        }

        private sealed class VolumeState
        {
            public Volume volume;
            public bool enabled;
        }

        private sealed class LightState
        {
            public Light light;
            public LightShadows shadows;
        }

        private static readonly List<VolumeState> VolumeStates = new List<VolumeState>();
        private static readonly List<LightState> LightStates = new List<LightState>();
        private static RuntimeStateSnapshot _runtimeState;
        private static bool _quittingHandlerRegistered;

        public static Settings Current { get; private set; } = new Settings();
        public static bool HasCapturedRuntimeState => _runtimeState != null;
        public static bool StartupFailurePending { get; private set; }
        public static IReadOnlyList<string> SupportedAntialiasingNames =>
            Array.AsReadOnly(SupportedAntialiasingValues);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureBeforeSceneLoad()
        {
            Settings candidate = null;
            StartupFailurePending = false;
            try
            {
                candidate = ParseValues(Environment.GetCommandLineArgs());
                ValidateSettings(candidate);
                Current = candidate;
                if (!Current.enabled)
                {
                    return;
                }

                ValidateRuntimeSettings(Current, Debug.isDebugBuild);
                CaptureRuntimeState();
                int qualityIndex = Array.FindIndex(
                    QualitySettings.names,
                    value => string.Equals(
                        value,
                        Current.quality,
                        StringComparison.OrdinalIgnoreCase));
                if (qualityIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Unknown R2-PERF1 quality preset: " + Current.quality);
                }

                QualitySettings.SetQualityLevel(qualityIndex, true);
                CaptureConfiguredPipelineState();
                QualitySettings.vSyncCount = 0;
                UnityEngine.Application.targetFrameRate = -1;
                UnityEngine.Application.runInBackground = true;
                ApplyDynamicResolution(Current);
                RegisterQuittingHandler();
            }
            catch (Exception exception)
            {
                Current = candidate ?? new Settings();
                StartupFailurePending = true;
                int exitCode = ForestBenchmarkRunner.RecoverFromStartupFailure(
                    candidate,
                    exception,
                    RestoreRuntimeState,
                    out _);
                Debug.LogException(exception);
                ForestBenchmarkRunner.RequestStartupFailureExit(exitCode);
            }
        }

        public static Settings Parse(IReadOnlyList<string> arguments)
        {
            Settings settings = ParseValues(arguments);
            ValidateSettings(settings);
            return settings;
        }

        private static Settings ParseValues(IReadOnlyList<string> arguments)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            var settings = new Settings();
            for (int index = 0; index < arguments.Count; index++)
            {
                string argument = arguments[index] ?? string.Empty;
                if (string.Equals(argument, "-sotf-perf1", StringComparison.OrdinalIgnoreCase))
                {
                    settings.enabled = true;
                    continue;
                }

                if (!argument.StartsWith("-sotf-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = RequireValue(arguments, ref index, argument);
                switch (argument.ToLowerInvariant())
                {
                    case "-sotf-quality":
                        settings.quality = RequireText(value, argument);
                        break;
                    case "-sotf-scenario":
                        settings.scenario = ValidateScenario(value);
                        break;
                    case "-sotf-run-id":
                        settings.runId = ValidateRunId(value);
                        break;
                    case "-sotf-build-kind":
                        settings.buildKind = ValidateBuildKind(value);
                        break;
                    case "-sotf-no-screenshot":
                        settings.noScreenshot = ParseBool(value, argument);
                        break;
                    case "-sotf-aa":
                        settings.antialiasing = RequireText(value, argument);
                        break;
                    case "-sotf-render-scale":
                        settings.renderScalePercent = ParseInt(value, 50, 100, argument);
                        break;
                    case "-sotf-upscaler":
                        settings.upscaler = RequireText(value, argument);
                        break;
                    case "-sotf-fullscreen-effects":
                        settings.fullscreenEffects = ParseBool(value, argument);
                        break;
                    case "-sotf-shadows":
                        settings.shadows = ParseBool(value, argument);
                        break;
                    case "-sotf-warmup":
                        settings.warmupSeconds = ParseFloat(value, 3f, 60f, argument);
                        break;
                    case "-sotf-sample":
                        settings.sampleSeconds = ParseFloat(value, 3f, 60f, argument);
                        break;
                    case "-sotf-driver":
                        settings.driverVersion = RequireText(value, argument);
                        break;
                    case "-sotf-power-plan":
                        settings.powerPlan = RequireText(value, argument);
                        break;
                    case "-sotf-power-online":
                        settings.powerOnline = ParseBool(value, argument);
                        break;
                    case "-sotf-gpu-preference":
                        settings.windowsGraphicsPreference = RequireText(value, argument);
                        break;
                    case "-sotf-output-directory":
                        settings.outputDirectory = Path.GetFullPath(RequireText(value, argument));
                        break;
                    default:
                        throw new ArgumentException("Unknown R2-PERF1 argument: " + argument);
                }
            }

            return settings;
        }

        public static string ValidateScenario(string value)
        {
            if (string.Equals(value, EmptyScenario, StringComparison.OrdinalIgnoreCase))
            {
                return EmptyScenario;
            }

            if (string.Equals(value, GroundScenario, StringComparison.OrdinalIgnoreCase))
            {
                return GroundScenario;
            }

            if (string.Equals(value, FullForestScenario, StringComparison.OrdinalIgnoreCase))
            {
                return FullForestScenario;
            }

            throw new ArgumentException(
                "-sotf-scenario expects empty_hdrp_camera, ground_only, or full_forest.");
        }

        public static string ValidateBuildKind(string value)
        {
            if (string.Equals(value, DiagnosticBuildKind, StringComparison.OrdinalIgnoreCase))
            {
                return DiagnosticBuildKind;
            }

            if (string.Equals(value, ReleaseBuildKind, StringComparison.OrdinalIgnoreCase))
            {
                return ReleaseBuildKind;
            }

            throw new ArgumentException("-sotf-build-kind expects diagnostic or release.");
        }

        public static string ValidateRunId(string value)
        {
            value = RequireText(value, "-sotf-run-id");
            if (value.Length > 80)
            {
                throw new ArgumentException("-sotf-run-id must not exceed 80 characters.");
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                {
                    throw new ArgumentException(
                        "-sotf-run-id may contain only letters, digits, '-' and '_'.");
                }
            }

            return value;
        }

        public static void ValidateRuntimeBuildKind(string buildKind, bool isDevelopmentBuild)
        {
            string validated = ValidateBuildKind(buildKind);
            if (validated == ReleaseBuildKind && isDevelopmentBuild)
            {
                throw new InvalidOperationException(
                    "A release R2-PERF1 run requires a non-Development build.");
            }

            if (validated == DiagnosticBuildKind && !isDevelopmentBuild)
            {
                throw new InvalidOperationException(
                    "A diagnostic R2-PERF1 run requires a Development build.");
            }
        }

        public static string ValidateAntialiasing(string value)
        {
            string normalized = RequireText(value, "-sotf-aa").ToUpperInvariant();
            switch (normalized)
            {
                case "NONE":
                    return AntialiasingNone;
                case AntialiasingFxaa:
                    return AntialiasingFxaa;
                case AntialiasingSmaa:
                    return AntialiasingSmaa;
                case AntialiasingTaa:
                    return AntialiasingTaa;
                default:
                    throw new ArgumentException("-sotf-aa expects None, FXAA, SMAA, or TAA.");
            }
        }

        public static HDAdditionalCameraData.AntialiasingMode ResolveAntialiasingMode(
            string value)
        {
            switch (ValidateAntialiasing(value))
            {
                case AntialiasingNone:
                    return HDAdditionalCameraData.AntialiasingMode.None;
                case AntialiasingFxaa:
                    return HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
                case AntialiasingSmaa:
                    return HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                case AntialiasingTaa:
                    return HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
                default:
                    throw new InvalidOperationException("Validated AA contract was not mapped.");
            }
        }

        public static void ValidateRuntimeSettings(Settings settings, bool isDevelopmentBuild)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            ValidateSettings(settings);
            if (!settings.enabled)
            {
                return;
            }

            ValidateRuntimeBuildKind(settings.buildKind, isDevelopmentBuild);
            ResolveAntialiasingMode(settings.antialiasing);
            ResolveUpscaler(settings.upscaler);
            if (Array.FindIndex(
                    QualitySettings.names,
                    value => string.Equals(
                        value,
                        settings.quality,
                        StringComparison.OrdinalIgnoreCase)) < 0)
            {
                throw new InvalidOperationException(
                    "Unknown R2-PERF1 quality preset: " + settings.quality);
            }

            if (settings.renderScalePercent < 100)
            {
                if (!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset))
                {
                    throw new InvalidOperationException("R2-PERF1 requires an active HDRP asset.");
                }
            }
        }

        public static bool CanMarkManifestValid(
            string currentStatus,
            int processExitCode,
            bool outputsVerified,
            bool telemetryValid)
        {
            return string.Equals(
                       currentStatus,
                       ManifestAwaitingValidation,
                       StringComparison.Ordinal) &&
                   processExitCode == 0 &&
                   outputsVerified &&
                   telemetryValid;
        }

        public static void ApplyCamera(Camera camera)
        {
            if (!Current.enabled || camera == null)
            {
                return;
            }

            HDAdditionalCameraData.AntialiasingMode mode =
                ResolveAntialiasingMode(Current.antialiasing);
            HDAdditionalCameraData data = camera.GetComponent<HDAdditionalCameraData>();
            if (data == null)
            {
                data = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            }

            data.antialiasing = mode;
            data.allowDynamicResolution = Current.renderScalePercent < 100;
            camera.allowDynamicResolution = Current.renderScalePercent < 100;
        }

        public static void ApplyScenePolicy()
        {
            if (!Current.enabled)
            {
                return;
            }

            RestoreScenePolicy();
            if (!Current.fullscreenEffects)
            {
                foreach (Volume volume in UnityEngine.Object.FindObjectsByType<Volume>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    VolumeStates.Add(new VolumeState { volume = volume, enabled = volume.enabled });
                    volume.enabled = false;
                }
            }

            if (!Current.shadows)
            {
                foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    LightStates.Add(new LightState { light = light, shadows = light.shadows });
                    light.shadows = LightShadows.None;
                }
            }
        }

        public static bool RestoreRuntimeState()
        {
            RestoreScenePolicy();
            RuntimeStateSnapshot snapshot = _runtimeState;
            if (snapshot == null)
            {
                return true;
            }

            try
            {
                if (snapshot.configuredPipelineAsset != null)
                {
                    snapshot.configuredPipelineAsset.currentPlatformRenderPipelineSettings =
                        snapshot.configuredPipelineSettings;
                }

                QualitySettings.SetQualityLevel(snapshot.qualityLevel, true);
                if (snapshot.originalPipelineAsset != null)
                {
                    snapshot.originalPipelineAsset.currentPlatformRenderPipelineSettings =
                        snapshot.originalPipelineSettings;
                }

                ScalableBufferManager.ResizeBuffers(snapshot.widthScale, snapshot.heightScale);
                QualitySettings.vSyncCount = snapshot.vSyncCount;
                UnityEngine.Application.targetFrameRate = snapshot.targetFrameRate;
                UnityEngine.Application.runInBackground = snapshot.runInBackground;
                return QualitySettings.GetQualityLevel() == snapshot.qualityLevel &&
                       QualitySettings.vSyncCount == snapshot.vSyncCount &&
                       UnityEngine.Application.targetFrameRate == snapshot.targetFrameRate &&
                       UnityEngine.Application.runInBackground == snapshot.runInBackground;
            }
            finally
            {
                _runtimeState = null;
                UnregisterQuittingHandler();
            }
        }

        public static string ResolveOutputDirectory(string applicationDataPath)
        {
            return ResolveOutputDirectory(Current, applicationDataPath);
        }

        public static string ResolveOutputDirectory(Settings settings, string applicationDataPath)
        {
            string directory;
            if (settings != null && settings.enabled &&
                !string.IsNullOrWhiteSpace(settings.outputDirectory))
            {
                directory = settings.outputDirectory;
            }
            else
            {
                directory = Path.Combine(applicationDataPath, "..", "Benchmarks");
            }

            return ValidateRunDirectory(directory);
        }

        public static string EnsureOutputDirectory(Settings settings, string applicationDataPath)
        {
            string directory = ResolveOutputDirectory(settings, applicationDataPath);
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static string ValidateRunDirectory(string directory)
        {
            directory = RequireText(directory, "-sotf-output-directory");
            string fullPath = Path.GetFullPath(directory);
            string root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root) ||
                string.Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "-sotf-output-directory must identify a run directory, not a filesystem root.");
            }

            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string ResolveContainedOutputPath(string runDirectory, string fileName)
        {
            runDirectory = ValidateRunDirectory(runDirectory);
            fileName = RequireText(fileName, nameof(fileName));
            if (fileName.Length > MaximumRuntimeOutputFileNameLength ||
                Path.IsPathRooted(fileName) ||
                !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal) ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException(
                    "Runtime output must use a bounded filename inside the validated run directory.",
                    nameof(fileName));
            }

            return ValidateContainedOutputPath(
                runDirectory,
                Path.Combine(runDirectory, fileName));
        }

        public static string ValidateContainedOutputPath(string runDirectory, string outputPath)
        {
            runDirectory = ValidateRunDirectory(runDirectory);
            outputPath = Path.GetFullPath(RequireText(outputPath, nameof(outputPath)));
            string prefix = runDirectory + Path.DirectorySeparatorChar;
            if (!outputPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Runtime output path escapes the validated run directory.",
                    nameof(outputPath));
            }

            return outputPath;
        }

        private static void ValidateSettings(Settings settings)
        {
            if (!settings.enabled)
            {
                return;
            }

            settings.scenario = ValidateScenario(settings.scenario);
            settings.buildKind = ValidateBuildKind(settings.buildKind);
            settings.runId = ValidateRunId(settings.runId);
            settings.quality = RequireText(settings.quality, "-sotf-quality");
            settings.antialiasing = ValidateAntialiasing(settings.antialiasing);
            settings.upscaler = RequireText(settings.upscaler, "-sotf-upscaler");
        }

        private static void CaptureRuntimeState()
        {
            if (_runtimeState != null)
            {
                throw new InvalidOperationException("R2-PERF1 runtime state was already captured.");
            }

            var asset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            _runtimeState = new RuntimeStateSnapshot
            {
                qualityLevel = QualitySettings.GetQualityLevel(),
                vSyncCount = QualitySettings.vSyncCount,
                targetFrameRate = UnityEngine.Application.targetFrameRate,
                runInBackground = UnityEngine.Application.runInBackground,
                widthScale = ScalableBufferManager.widthScaleFactor,
                heightScale = ScalableBufferManager.heightScaleFactor,
                originalPipelineAsset = asset,
                originalPipelineSettings = asset != null
                    ? asset.currentPlatformRenderPipelineSettings
                    : default,
            };
        }

        private static void CaptureConfiguredPipelineState()
        {
            if (_runtimeState == null)
            {
                throw new InvalidOperationException("R2-PERF1 runtime state was not captured.");
            }

            var asset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            _runtimeState.configuredPipelineAsset = asset;
            _runtimeState.configuredPipelineSettings = asset != null
                ? asset.currentPlatformRenderPipelineSettings
                : default;
        }

        private static void ApplyDynamicResolution(Settings settings)
        {
            if (settings.renderScalePercent >= 100)
            {
                ScalableBufferManager.ResizeBuffers(1f, 1f);
                return;
            }

            var asset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if (asset == null)
            {
                throw new InvalidOperationException("R2-PERF1 requires an active HDRP asset.");
            }

            DynamicResUpscaleFilter filter = ResolveUpscaler(settings.upscaler);

            RenderPipelineSettings pipelineSettings = asset.currentPlatformRenderPipelineSettings;
            GlobalDynamicResolutionSettings dynamicSettings = pipelineSettings.dynamicResolutionSettings;
            dynamicSettings.enabled = true;
            dynamicSettings.forceResolution = true;
            dynamicSettings.forcedPercentage = settings.renderScalePercent;
            dynamicSettings.minPercentage = settings.renderScalePercent;
            dynamicSettings.maxPercentage = settings.renderScalePercent;
            dynamicSettings.upsampleFilter = filter;
            dynamicSettings.useMipBias = true;
            pipelineSettings.dynamicResolutionSettings = dynamicSettings;
            asset.currentPlatformRenderPipelineSettings = pipelineSettings;

            float scale = settings.renderScalePercent / 100f;
            ScalableBufferManager.ResizeBuffers(scale, scale);
        }

        private static DynamicResUpscaleFilter ResolveUpscaler(string value)
        {
            if (Enum.TryParse(value, true, out DynamicResUpscaleFilter filter))
            {
                return filter;
            }

            throw new InvalidOperationException("Unknown R2-PERF1 upscaler: " + value);
        }

        private static void RestoreScenePolicy()
        {
            foreach (VolumeState state in VolumeStates)
            {
                if (state.volume != null)
                {
                    state.volume.enabled = state.enabled;
                }
            }

            foreach (LightState state in LightStates)
            {
                if (state.light != null)
                {
                    state.light.shadows = state.shadows;
                }
            }

            VolumeStates.Clear();
            LightStates.Clear();
        }

        private static void RegisterQuittingHandler()
        {
            if (_quittingHandlerRegistered)
            {
                return;
            }

            UnityEngine.Application.quitting += RestoreOnQuit;
            _quittingHandlerRegistered = true;
        }

        private static void UnregisterQuittingHandler()
        {
            if (!_quittingHandlerRegistered)
            {
                return;
            }

            UnityEngine.Application.quitting -= RestoreOnQuit;
            _quittingHandlerRegistered = false;
        }

        private static void RestoreOnQuit()
        {
            RestoreRuntimeState();
        }

        private static string RequireValue(
            IReadOnlyList<string> arguments,
            ref int index,
            string argument)
        {
            if (index + 1 >= arguments.Count ||
                string.IsNullOrWhiteSpace(arguments[index + 1]) ||
                arguments[index + 1].StartsWith("-sotf-", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(argument + " requires a value.");
            }

            index++;
            return arguments[index];
        }

        private static string RequireText(string value, string argument)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(argument + " requires a non-empty value.");
            }

            return value.Trim();
        }

        private static bool ParseBool(string value, string argument)
        {
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }

            throw new ArgumentException(argument + " expects true or false.");
        }

        private static int ParseInt(string value, int minimum, int maximum, string argument)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) &&
                result >= minimum && result <= maximum)
            {
                return result;
            }

            throw new ArgumentException(
                argument + " expects a value from " + minimum + " to " + maximum + ".");
        }

        private static float ParseFloat(
            string value,
            float minimum,
            float maximum,
            string argument)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) &&
                result >= minimum && result <= maximum)
            {
                return result;
            }

            throw new ArgumentException(
                argument + " expects a value from " + minimum + " to " + maximum + ".");
        }

        private static string Sanitize(string value)
        {
            char[] characters = value.ToLowerInvariant().ToCharArray();
            for (int index = 0; index < characters.Length; index++)
            {
                if (!char.IsLetterOrDigit(characters[index]))
                {
                    characters[index] = '_';
                }
            }

            return new string(characters);
        }
    }
}
