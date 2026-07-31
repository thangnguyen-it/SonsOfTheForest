using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SonsOfTheForest.Infrastructure.Benchmark
{
    /// <summary>
    /// Phase-1/Phase-3 forest benchmark (spec: fixed camera, 360-degree orbit,
    /// walkthrough, A/B toggles). Runs every scenario in one play session and
    /// writes Benchmarks/forest_benchmark_&lt;phase&gt;.json + .md, then exits play mode.
    /// </summary>
    public sealed class ForestBenchmarkRunner : MonoBehaviour
    {
        public const int StartupFailureExitCode = 2;

        [Serializable]
        public sealed class ScenarioResult
        {
            public string name;
            public int frames;
            public float avgMs;
            public float medianMs;
            public float p95Ms;
            public float p99Ms;
            public float avgFps;
            public float onePercentLowFps;
            public int timingSamples;
            public float cpuTotalAvgMs;
            public float cpuTotalP95Ms;
            public float cpuMainThreadAvgMs;
            public float cpuMainThreadP95Ms;
            public float cpuMainThreadPresentWaitAvgMs;
            public float cpuMainThreadWorkAvgMs;
            public float cpuRenderThreadAvgMs;
            public float cpuRenderThreadP95Ms;
            public float gpuAvgMs;
            public float gpuP95Ms;
            public bool drawCallsAvailable;
            public bool batchesAvailable;
            public bool setPassCallsAvailable;
            public bool trianglesAvailable;
            public bool verticesAvailable;
            public float drawCalls;
            public float batches;
            public float setPassCalls;
            public float triangleMillions;
            public float vertexMillions;
            public bool gcAllocationAvailable;
            public float gcAllocatedAverageBytes;
            public long gcAllocatedPeakBytes;
            public float gcAllocationCountAverage;
            public bool totalUsedMemoryAvailable;
            public bool gfxUsedMemoryAvailable;
            public bool textureMemoryAvailable;
            public float totalUsedMemoryAverageMb;
            public float totalUsedMemoryPeakMb;
            public float gfxUsedMemoryAverageMb;
            public float gfxUsedMemoryPeakMb;
            public float textureMemoryAverageMb;
            public float textureMemoryPeakMb;
            public string bottleneck;
            public string budgetStatus;
            public string budgetFailure;
            public int ignoredStartupStallFrames;
        }

        [Serializable]
        public sealed class Report
        {
            public string phase;
            public string createdUtc;
            public string schemaVersion;
            public string unityVersion;
            public string platform;
            public string operatingSystem;
            public string processorType;
            public int processorCount;
            public int processorFrequencyMhz;
            public int systemMemoryMb;
            public string graphicsDeviceName;
            public string graphicsDeviceType;
            public string graphicsDeviceVendor;
            public string graphicsDeviceVersion;
            public int graphicsMemoryMb;
            public bool graphicsMultiThreaded;
            public string renderPipeline;
            public string qualityLevel;
            public int width;
            public int height;
            public bool developmentBuild;
            public bool runInBackground;
            public bool frameTimingFeatureEnabled;
            public string graphicsDriverVersion;
            public string windowsPowerPlan;
            public bool powerOnline;
            public string windowsGraphicsPreference;
            public int vSyncCount;
            public int targetFrameRate;
            public string fullScreenMode;
            public string antialiasingMode;
            public int renderScalePercent;
            public string upscaler;
            public bool dynamicResolutionEnabled;
            public bool fullscreenEffectsEnabled;
            public bool shadowsEnabled;
            public bool profilerEnabled;
            public bool profilerBinaryLogEnabled;
            public bool deepProfilingBuild;
            public string shaderWarmupProcedure;
            public float shaderWarmupSeconds;
            public string benchmarkRunId;
            public string benchmarkScenario;
            public string buildKind;
            public bool screenshotRequested;
            public bool measurementEligible;
            public string screenshotPath;
            public int minimumPlayableFps;
            public float frameBudgetMilliseconds;
            public float p95BudgetMilliseconds;
            public float p99BudgetMilliseconds;
            public long maximumGcAllocationBytesPerFrame;
            public float shadowDistanceAtStart;
            public List<ScenarioResult> scenarios = new List<ScenarioResult>();
        }

        [Serializable]
        public sealed class RunManifest
        {
            public string schemaVersion = "r2-perf1-manifest/1";
            public string status;
            public string statusReason;
            public string createdUtc;
            public string completedUtc;
            public string runId;
            public string phase;
            public string scenario;
            public string quality;
            public string antialiasing;
            public int renderScalePercent;
            public string upscaler;
            public string buildKind;
            public bool developmentBuild;
            public bool screenshotRequested;
            public bool measurementEligible;
            public bool reportWritten;
            public bool runtimeStateRestored;
            public string reportJsonPath;
            public string reportMarkdownPath;
            public string screenshotPath;
        }

        public string phase = "before";
        public GameObject[] midRingPrefabs;
        public Mesh farInstanceMesh;
        public Material farBarkMaterial;
        public Material farCanopyMaterial;
        public float warmupSeconds = 0.9f;
        public float sampleSeconds = 4f;
        public int minimumWarmupFrames = 8;
        public int maxIgnoredStartupStallFrames = 3;
        public float startupStallFrameThresholdMs = 500f;

        private readonly List<GameObject> _disabledCameras = new List<GameObject>();
        private readonly List<Light> _shadowLights = new List<Light>();
        private readonly List<LightShadows> _shadowBackup = new List<LightShadows>();
        private readonly List<GameObject> _conifers = new List<GameObject>();
        private readonly List<Matrix4x4[]> _farBatches = new List<Matrix4x4[]>();
        private readonly Dictionary<GameObject, bool> _diagnosticLayerStates =
            new Dictionary<GameObject, bool>();
        private Camera _camera;
        private GameObject _midRingRoot;
        private GameObject _shadowVolume;
        private LODGroup[] _lodGroups = Array.Empty<LODGroup>();
        private bool _farRingActive;
        private bool _walkthrough;
        private bool _runnerStateRestored;
        private float _scenarioClock;
        private RuntimePerformanceSampler _runtimeSampler;
        private GameObject _groundLayer;
        private GameObject _sunLayer;
        private GameObject _atmosphereLayer;
        private GameObject _nearLayer;
        private GameObject _midLayer;
        private GameObject _farLayer;
        private GameObject _accentLayer;
        private int _previousVSync;
        private int _previousTargetFrameRate;
        private bool _previousRunInBackground;

        private R2Perf1BenchmarkConfiguration.Settings _perf1;
        private RunManifest _runManifest;
        private string _runManifestPath;

        private void Start()
        {
            _perf1 = R2Perf1BenchmarkConfiguration.Current;
            if (R2Perf1BenchmarkConfiguration.StartupFailurePending)
            {
                return;
            }

            try
            {
                R2Perf1BenchmarkConfiguration.ValidateRuntimeSettings(
                    _perf1,
                    Debug.isDebugBuild);
                if (_perf1.enabled)
                {
                    PrepareRuntimeManifest();
                }

                InitializeRunner();
            }
            catch (Exception exception)
            {
                HandleStartupFailure(exception);
            }
        }

        private void InitializeRunner()
        {
            if (!_perf1.enabled)
            {
                _previousVSync = QualitySettings.vSyncCount;
                _previousTargetFrameRate = UnityEngine.Application.targetFrameRate;
                _previousRunInBackground = UnityEngine.Application.runInBackground;
                QualitySettings.vSyncCount = 0;
                UnityEngine.Application.targetFrameRate = -1;
                UnityEngine.Application.runInBackground = true;
            }

            _runtimeSampler = new RuntimePerformanceSampler();

            foreach (Camera existing in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (existing.enabled)
                {
                    existing.enabled = false;
                    _disabledCameras.Add(existing.gameObject);
                }
            }

            var cameraObject = new GameObject("ForestBenchmarkCamera");
            cameraObject.transform.SetParent(transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.fieldOfView = 60f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 1000f;
            R2Perf1BenchmarkConfiguration.ApplyCamera(_camera);

            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    _shadowLights.Add(light);
                    _shadowBackup.Add(light.shadows);
                }
            }

            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (candidate != null && candidate.name.StartsWith("MatureConifer", StringComparison.Ordinal))
                {
                    _conifers.Add(candidate.gameObject);
                }
            }

            R2Perf1BenchmarkConfiguration.ApplyScenePolicy();
            _lodGroups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            CacheDiagnosticLayers();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (_perf1.enabled)
            {
                phase = _perf1.Phase;
                // The full-scene shader warm-up is handled once by RunPerf1.
                // Scenario transitions only need a short stabilization window.
                warmupSeconds = 1f;
                sampleSeconds = _perf1.sampleSeconds;
            }

            var report = new Report
            {
                phase = phase,
                createdUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                schemaVersion = _perf1.enabled ? "r2-perf1/1" : "r2-perf0/1",
                unityVersion = UnityEngine.Application.unityVersion,
                platform = UnityEngine.Application.platform.ToString(),
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                processorFrequencyMhz = SystemInfo.processorFrequency,
                systemMemoryMb = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor,
                graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
                graphicsMemoryMb = SystemInfo.graphicsMemorySize,
                graphicsMultiThreaded = SystemInfo.graphicsMultiThreaded,
                renderPipeline = GraphicsSettings.currentRenderPipeline != null
                    ? GraphicsSettings.currentRenderPipeline.name
                    : "Built-in Render Pipeline",
                qualityLevel = QualitySettings.names[QualitySettings.GetQualityLevel()],
                width = Screen.width,
                height = Screen.height,
                developmentBuild = Debug.isDebugBuild,
                runInBackground = UnityEngine.Application.runInBackground,
                frameTimingFeatureEnabled = _runtimeSampler.FrameTimingFeatureEnabled,
                graphicsDriverVersion = _perf1.enabled ? _perf1.driverVersion : "UNKNOWN",
                windowsPowerPlan = _perf1.enabled ? _perf1.powerPlan : "UNKNOWN",
                powerOnline = _perf1.enabled && _perf1.powerOnline,
                windowsGraphicsPreference = _perf1.enabled
                    ? _perf1.windowsGraphicsPreference
                    : "UNKNOWN",
                vSyncCount = QualitySettings.vSyncCount,
                targetFrameRate = UnityEngine.Application.targetFrameRate,
                fullScreenMode = Screen.fullScreenMode.ToString(),
                antialiasingMode = _perf1.enabled ? _perf1.antialiasing : "UNRECORDED",
                renderScalePercent = _perf1.enabled ? _perf1.renderScalePercent : 100,
                upscaler = _perf1.enabled ? _perf1.upscaler : "UNRECORDED",
                dynamicResolutionEnabled = _perf1.enabled && _perf1.renderScalePercent < 100,
                fullscreenEffectsEnabled = !_perf1.enabled || _perf1.fullscreenEffects,
                shadowsEnabled = !_perf1.enabled || _perf1.shadows,
                profilerEnabled = Profiler.enabled,
                profilerBinaryLogEnabled = Profiler.enableBinaryLog,
                deepProfilingBuild = false,
                shaderWarmupProcedure = _perf1.enabled
                    ? "Fixed full-forest camera; natural shader variant warm-up"
                    : "Per-scenario timed warm-up",
                shaderWarmupSeconds = _perf1.enabled ? _perf1.warmupSeconds : warmupSeconds,
                benchmarkRunId = _perf1.enabled ? _perf1.runId : "legacy",
                benchmarkScenario = _perf1.enabled ? _perf1.scenario : "legacy-multi-scenario",
                buildKind = _perf1.enabled ? _perf1.buildKind : "legacy",
                screenshotRequested = _perf1.enabled && _perf1.CaptureScreenshot,
                measurementEligible = !_perf1.enabled || _perf1.MeasurementEligible,
                minimumPlayableFps = ProductionForestPerformancePolicy.MinimumPlayableFps,
                frameBudgetMilliseconds = ProductionForestPerformancePolicy.FrameBudgetMilliseconds,
                p95BudgetMilliseconds = ProductionForestPerformancePolicy.P95FrameBudgetMilliseconds,
                p99BudgetMilliseconds = ProductionForestPerformancePolicy.P99FrameBudgetMilliseconds,
                maximumGcAllocationBytesPerFrame =
                    ProductionForestPerformancePolicy.MaximumSteadyStateGcAllocationBytesPerFrame,
                shadowDistanceAtStart = ReadShadowDistance(),
            };

            if (_perf1.enabled)
            {
                yield return RunPerf1(report);
                yield break;
            }

            yield return SampleScenario(report, "baseline_orbit", null, null);

            if (HasDiagnosticLayerSet())
            {
                yield return SampleScenario(
                    report,
                    "empty_hdrp_camera",
                    () => ApplyLayerIsolation(false, false, false, false, false, false, false),
                    RestoreDiagnosticLayers);
                yield return SampleScenario(
                    report,
                    "lighting_volume_only",
                    () => ApplyLayerIsolation(false, true, true, false, false, false, false),
                    RestoreDiagnosticLayers);
                yield return SampleScenario(
                    report,
                    "ground_only",
                    () => ApplyLayerIsolation(true, true, true, false, false, false, false),
                    RestoreDiagnosticLayers);
                yield return SampleScenario(
                    report,
                    "near_10_only",
                    () => ApplyLayerIsolation(true, true, true, true, false, false, false),
                    RestoreDiagnosticLayers);
                yield return SampleScenario(
                    report,
                    "mid_50_only",
                    () => ApplyLayerIsolation(true, true, true, false, true, false, false),
                    RestoreDiagnosticLayers);
                yield return SampleScenario(
                    report,
                    "far_300_instanced_only",
                    () => ApplyLayerIsolation(true, true, true, false, false, true, false),
                    RestoreDiagnosticLayers);
                if (_accentLayer != null)
                {
                    yield return SampleScenario(
                        report,
                        "broadleaf_24_only",
                        () => ApplyLayerIsolation(true, true, true, false, false, false, true),
                        RestoreDiagnosticLayers);
                }

                yield return SampleScenario(
                    report,
                    "full_without_atmosphere",
                    ApplyFullWithoutAtmosphere,
                    RestoreDiagnosticLayers);
            }

            yield return SampleScenario(report, "shadows_off_orbit", ApplyShadowsOff, RevertShadows);
            yield return SampleScenario(report, "shadow_distance_30", ApplyShadowDistance30, RevertShadowDistance);
            yield return SampleScenario(report, "half_trees_orbit", ApplyHalfTrees, RevertTreeVisibility);
            yield return SampleScenario(report, "trees_hidden_orbit", ApplyTreesHidden, RevertTreeVisibility);

            if (_lodGroups.Length > 0)
            {
                yield return SampleScenario(report, "forced_lowest_lod", ApplyForcedLowestLod, RevertForcedLod);
            }

            if (midRingPrefabs != null && midRingPrefabs.Length > 0)
            {
                yield return SampleScenario(report, "mid_ring_50_trees", ApplyMidRing, RevertRings);
                if (farInstanceMesh != null && farCanopyMaterial != null)
                {
                    yield return SampleScenario(report, "full_rings_450_trees", ApplyFullRings, RevertRings);
                }
            }

            _walkthrough = true;
            yield return SampleScenario(report, "walkthrough_camp", null, null);
            _walkthrough = false;

            if (HasDiagnosticLayerSet())
            {
                yield return SampleScenario(
                    report,
                    "empty_hdrp_camera_repeat",
                    () => ApplyLayerIsolation(false, false, false, false, false, false, false),
                    RestoreDiagnosticLayers);
            }

            yield return SampleScenario(report, "baseline_orbit_repeat", null, null);

            WriteReport(report);
            RestoreRunnerState();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        private IEnumerator RunPerf1(Report report)
        {
            string directory = ReportDirectory();
            Directory.CreateDirectory(directory);
            RunManifest manifest = _runManifest ?? CreateManifest(_perf1, report.phase);
            string manifestPath = _runManifestPath ?? ManifestPath(report.phase);
            _runManifest = manifest;
            _runManifestPath = manifestPath;
            WriteManifest(manifestPath, manifest);

            bool completedNormally = false;
            try
            {
                RestoreDiagnosticLayers();
                PositionFixedQualityCamera();

                float warmupClock = 0f;
                int warmupFrames = 0;
                while (warmupClock < _perf1.warmupSeconds || warmupFrames < 120)
                {
                    warmupClock += Time.unscaledDeltaTime;
                    warmupFrames++;
                    _runtimeSampler.CaptureFrameTiming();
                    yield return null;
                }

                if (_perf1.CaptureScreenshot)
                {
                    string screenshotDirectory = Path.Combine(directory, "screenshots");
                    Directory.CreateDirectory(screenshotDirectory);
                    string screenshotPath = Path.Combine(screenshotDirectory, phase + ".png");
                    report.screenshotPath = screenshotPath;
                    manifest.screenshotPath = screenshotPath;
                    ScreenCapture.CaptureScreenshot(screenshotPath, 1);
                    yield return new WaitForEndOfFrame();
                    float screenshotWaitClock = 0f;
                    while (!File.Exists(screenshotPath) && screenshotWaitClock < 5f)
                    {
                        screenshotWaitClock += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
                else
                {
                    yield return RunSelectedPerf1Scenario(report);
                }

                WriteReport(report);
                manifest.reportWritten = VerifyRuntimeOutputs(report, manifest);
                if (!manifest.reportWritten)
                {
                    throw new InvalidDataException(
                        "R2-PERF1 report output did not pass runtime verification.");
                }

                completedNormally = true;
            }
            finally
            {
                RestoreRunnerState();
                manifest.runtimeStateRestored =
                    R2Perf1BenchmarkConfiguration.RestoreRuntimeState();
                manifest.completedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                if (completedNormally && manifest.runtimeStateRestored)
                {
                    manifest.status = R2Perf1BenchmarkConfiguration.ManifestAwaitingValidation;
                    manifest.statusReason = "process_completed_outputs_require_offline_validation";
                }
                else
                {
                    manifest.status = R2Perf1BenchmarkConfiguration.ManifestInvalid;
                    manifest.statusReason = completedNormally
                        ? "runtime_state_restore_failed"
                        : "runtime_did_not_complete_normally";
                }

                WriteManifest(manifestPath, manifest);
            }

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        private IEnumerator RunSelectedPerf1Scenario(Report report)
        {
            switch (_perf1.scenario)
            {
                case R2Perf1BenchmarkConfiguration.EmptyScenario:
                    yield return SampleScenario(
                        report,
                        R2Perf1BenchmarkConfiguration.EmptyScenario,
                        () => ApplyLayerIsolation(false, false, false, false, false, false, false),
                        RestoreDiagnosticLayers);
                    break;
                case R2Perf1BenchmarkConfiguration.GroundScenario:
                    yield return SampleScenario(
                        report,
                        R2Perf1BenchmarkConfiguration.GroundScenario,
                        () => ApplyLayerIsolation(true, true, true, false, false, false, false),
                        RestoreDiagnosticLayers);
                    break;
                case R2Perf1BenchmarkConfiguration.FullForestScenario:
                    yield return SampleScenario(
                        report,
                        R2Perf1BenchmarkConfiguration.FullForestScenario,
                        null,
                        null);
                    break;
                default:
                    throw new InvalidOperationException(
                        "Unsupported R2-PERF1 scenario: " + _perf1.scenario);
            }
        }

        private void PositionFixedQualityCamera()
        {
            if (_camera == null)
            {
                return;
            }

            _camera.transform.position = new Vector3(30f, 4.5f, -42f);
            _camera.transform.LookAt(new Vector3(0f, 8f, 18f));
        }

        private IEnumerator SampleScenario(Report report, string name, Action apply, Action revert)
        {
            int sampleCapacity = Mathf.Max(1024, Mathf.CeilToInt(sampleSeconds * 1000f) + 16);
            var frameTimes = new List<float>(sampleCapacity);
            var cpuTotal = new MetricAccumulator(sampleCapacity);
            var cpuMain = new MetricAccumulator(sampleCapacity);
            var cpuPresentWait = new MetricAccumulator(sampleCapacity);
            var cpuRender = new MetricAccumulator(sampleCapacity);
            var gpu = new MetricAccumulator(sampleCapacity);
            var drawCalls = new MetricAccumulator(sampleCapacity);
            var batches = new MetricAccumulator(sampleCapacity);
            var setPassCalls = new MetricAccumulator(sampleCapacity);
            var triangles = new MetricAccumulator(sampleCapacity);
            var vertices = new MetricAccumulator(sampleCapacity);
            var gcAllocated = new MetricAccumulator(sampleCapacity);
            var gcAllocationCount = new MetricAccumulator(sampleCapacity);
            var totalUsedMemory = new MetricAccumulator(sampleCapacity);
            var gfxUsedMemory = new MetricAccumulator(sampleCapacity);
            var textureMemory = new MetricAccumulator(sampleCapacity);

            if (apply != null)
            {
                apply();
            }

            float settle = 0f;
            int warmupFrames = 0;
            while (settle < warmupSeconds || warmupFrames < minimumWarmupFrames)
            {
                settle += Time.unscaledDeltaTime;
                warmupFrames++;
                MoveCamera(0.25f);
                _runtimeSampler.CaptureFrameTiming();
                yield return null;
            }

            int frameTimingSamples = 0;
            int ignoredStartupStallFrames = 0;
            _scenarioClock = 0f;
            while (_scenarioClock < sampleSeconds)
            {
                float dt = Time.unscaledDeltaTime;
                float frameMs = dt * 1000f;
                if (frameTimes.Count == 0 &&
                    ignoredStartupStallFrames < maxIgnoredStartupStallFrames &&
                    frameMs > startupStallFrameThresholdMs)
                {
                    ignoredStartupStallFrames++;
                    MoveCamera(0f);
                    yield return null;
                    continue;
                }

                _scenarioClock += dt;
                if (frameTimes.Count >= sampleCapacity)
                {
                    throw new InvalidOperationException(
                        "R2-PERF1 sample exceeded its preallocated frame capacity.");
                }

                frameTimes.Add(frameMs);
                MoveCamera(_scenarioClock / sampleSeconds);

                _runtimeSampler.CaptureFrameTiming();
                RuntimePerformanceSampler.FrameSample sample = _runtimeSampler.ReadLastFrame();
                if (sample.hasFrameTiming)
                {
                    frameTimingSamples++;
                }

                cpuTotal.AddIfAvailable(sample.hasCpuTotalTime, sample.cpuTotalMilliseconds);
                cpuMain.AddIfAvailable(sample.hasCpuMainThreadTime, sample.cpuMainThreadMilliseconds);
                cpuPresentWait.AddIfAvailable(
                    sample.hasCpuMainThreadPresentWaitTime,
                    sample.cpuMainThreadPresentWaitMilliseconds);
                cpuRender.AddIfAvailable(sample.hasCpuRenderThreadTime, sample.cpuRenderThreadMilliseconds);
                gpu.AddIfAvailable(sample.hasGpuTime, sample.gpuMilliseconds);
                drawCalls.AddIfAvailable(sample.hasDrawCalls, sample.drawCalls);
                batches.AddIfAvailable(sample.hasBatches, sample.batches);
                setPassCalls.AddIfAvailable(sample.hasSetPassCalls, sample.setPassCalls);
                triangles.AddIfAvailable(sample.hasTriangles, sample.triangles);
                vertices.AddIfAvailable(sample.hasVertices, sample.vertices);
                gcAllocated.AddIfAvailable(sample.hasGcAllocatedBytes, sample.gcAllocatedBytes);
                gcAllocationCount.AddIfAvailable(sample.hasGcAllocationCount, sample.gcAllocationCount);
                totalUsedMemory.AddIfAvailable(sample.hasTotalUsedMemory, sample.totalUsedMemoryBytes);
                gfxUsedMemory.AddIfAvailable(sample.hasGfxUsedMemory, sample.gfxUsedMemoryBytes);
                textureMemory.AddIfAvailable(sample.hasTextureMemory, sample.textureMemoryBytes);

                yield return null;
            }

            if (revert != null)
            {
                revert();
            }

            frameTimes.Sort();
            int count = Mathf.Max(1, frameTimes.Count);
            var result = new ScenarioResult
            {
                name = name,
                frames = frameTimes.Count,
                avgMs = Average(frameTimes, count),
                medianMs = Percentile(frameTimes, 0.50f),
                p95Ms = Percentile(frameTimes, 0.95f),
                p99Ms = Percentile(frameTimes, 0.99f),
                timingSamples = frameTimingSamples,
                cpuTotalAvgMs = (float)cpuTotal.Average,
                cpuTotalP95Ms = (float)cpuTotal.Percentile(0.95f),
                cpuMainThreadAvgMs = (float)cpuMain.Average,
                cpuMainThreadP95Ms = (float)cpuMain.Percentile(0.95f),
                cpuMainThreadPresentWaitAvgMs = (float)cpuPresentWait.Average,
                cpuMainThreadWorkAvgMs = Mathf.Max(
                    0f,
                    (float)(cpuMain.Average - cpuPresentWait.Average)),
                cpuRenderThreadAvgMs = (float)cpuRender.Average,
                cpuRenderThreadP95Ms = (float)cpuRender.Percentile(0.95f),
                gpuAvgMs = (float)gpu.Average,
                gpuP95Ms = (float)gpu.Percentile(0.95f),
                drawCallsAvailable = drawCalls.Count > 0,
                batchesAvailable = batches.Count > 0,
                setPassCallsAvailable = setPassCalls.Count > 0,
                trianglesAvailable = triangles.Count > 0,
                verticesAvailable = vertices.Count > 0,
                drawCalls = (float)drawCalls.Average,
                batches = (float)batches.Average,
                setPassCalls = (float)setPassCalls.Average,
                triangleMillions = (float)(triangles.Average / 1_000_000d),
                vertexMillions = (float)(vertices.Average / 1_000_000d),
                gcAllocationAvailable = gcAllocated.Count > 0,
                gcAllocatedAverageBytes = (float)gcAllocated.Average,
                gcAllocatedPeakBytes = (long)gcAllocated.Maximum,
                gcAllocationCountAverage = (float)gcAllocationCount.Average,
                totalUsedMemoryAvailable = totalUsedMemory.Count > 0,
                gfxUsedMemoryAvailable = gfxUsedMemory.Count > 0,
                textureMemoryAvailable = textureMemory.Count > 0,
                totalUsedMemoryAverageMb = BytesToMegabytes(totalUsedMemory.Average),
                totalUsedMemoryPeakMb = BytesToMegabytes(totalUsedMemory.Maximum),
                gfxUsedMemoryAverageMb = BytesToMegabytes(gfxUsedMemory.Average),
                gfxUsedMemoryPeakMb = BytesToMegabytes(gfxUsedMemory.Maximum),
                textureMemoryAverageMb = BytesToMegabytes(textureMemory.Average),
                textureMemoryPeakMb = BytesToMegabytes(textureMemory.Maximum),
                ignoredStartupStallFrames = ignoredStartupStallFrames,
            };
            result.avgFps = result.avgMs > 0.0001f ? 1000f / result.avgMs : 0f;
            result.onePercentLowFps = result.p99Ms > 0.0001f ? 1000f / result.p99Ms : 0f;
            result.bottleneck = ClassifyBottleneck(result);
            EvaluateBudget(result);

            report.scenarios.Add(result);
            Debug.Log(
                $"[ForestBenchmark] {phase}/{name}: {result.avgFps:F1} fps avg, {result.avgMs:F1} ms avg, " +
                $"{result.p95Ms:F1} ms p95, CPU {result.cpuTotalAvgMs:F1} ms, GPU {result.gpuAvgMs:F1} ms, " +
                $"{result.bottleneck}, budget {result.budgetStatus}");
        }

        private void MoveCamera(float normalizedTime)
        {
            if (_camera == null)
            {
                return;
            }

            if (_perf1 != null && _perf1.enabled)
            {
                return;
            }

            if (_walkthrough)
            {
                float x = Mathf.Lerp(-26f, 26f, normalizedTime);
                _camera.transform.position = new Vector3(x, 1.8f, 2f);
                _camera.transform.LookAt(new Vector3(x + 8f, 3.2f, 2f));
                return;
            }

            float angle = normalizedTime * Mathf.PI * 2f;
            var position = new Vector3(Mathf.Cos(angle) * 30f, 2.2f, Mathf.Sin(angle) * 30f);
            _camera.transform.position = position;
            _camera.transform.LookAt(new Vector3(0f, 6f, 0f));
        }

        private void CacheDiagnosticLayers()
        {
            _groundLayer = FindByName("NeutralForestGround");
            _sunLayer = FindByName("Sun_KeyLight");
            _atmosphereLayer = FindByName("Volume_ForestAtmosphere");
            _nearLayer = FindByName("MatureConiferPhase1_Near_10");
            _midLayer = FindByName("MatureConiferPhase1_Mid_50");
            _farLayer = FindByName("MatureConiferPhase1_Far_300");
            _accentLayer = FindByName("Phase1_BroadleafAccent_24");

            RememberLayer(_groundLayer);
            RememberLayer(_sunLayer);
            RememberLayer(_atmosphereLayer);
            RememberLayer(_nearLayer);
            RememberLayer(_midLayer);
            RememberLayer(_farLayer);
            RememberLayer(_accentLayer);

            if (HasDiagnosticLayerSet())
            {
                _conifers.Clear();
                AddDirectChildren(_nearLayer, _conifers);
                AddDirectChildren(_midLayer, _conifers);
            }
        }

        private bool HasDiagnosticLayerSet()
        {
            return _groundLayer != null &&
                   _sunLayer != null &&
                   _atmosphereLayer != null &&
                   _nearLayer != null &&
                   _midLayer != null &&
                   _farLayer != null;
        }

        private void ApplyLayerIsolation(
            bool ground,
            bool sun,
            bool atmosphere,
            bool near,
            bool mid,
            bool far,
            bool accent)
        {
            RestoreDiagnosticLayers();
            SetLayer(_groundLayer, ground);
            SetLayer(_sunLayer, sun);
            SetLayer(_atmosphereLayer, atmosphere);
            SetLayer(_nearLayer, near);
            SetLayer(_midLayer, mid);
            SetLayer(_farLayer, far);
            SetLayer(_accentLayer, accent);
        }

        private void ApplyFullWithoutAtmosphere()
        {
            RestoreDiagnosticLayers();
            SetLayer(_atmosphereLayer, false);
        }

        private void RestoreDiagnosticLayers()
        {
            foreach (KeyValuePair<GameObject, bool> pair in _diagnosticLayerStates)
            {
                if (pair.Key != null)
                {
                    pair.Key.SetActive(pair.Value);
                }
            }
        }

        private void RememberLayer(GameObject layer)
        {
            if (layer != null && !_diagnosticLayerStates.ContainsKey(layer))
            {
                _diagnosticLayerStates.Add(layer, layer.activeSelf);
            }
        }

        private static void SetLayer(GameObject layer, bool active)
        {
            if (layer != null)
            {
                layer.SetActive(active);
            }
        }

        private static GameObject FindByName(string objectName)
        {
            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static void AddDirectChildren(GameObject parent, ICollection<GameObject> output)
        {
            if (parent == null)
            {
                return;
            }

            foreach (Transform child in parent.transform)
            {
                output.Add(child.gameObject);
            }
        }

        private void Update()
        {
            if (!_farRingActive || farInstanceMesh == null)
            {
                return;
            }

            var bounds = new Bounds(Vector3.zero, new Vector3(700f, 120f, 700f));
            for (int batch = 0; batch < _farBatches.Count; batch++)
            {
                Matrix4x4[] matrices = _farBatches[batch];
                for (int subMesh = 0; subMesh < farInstanceMesh.subMeshCount; subMesh++)
                {
                    Material material = subMesh == 0 && farBarkMaterial != null
                        ? farBarkMaterial
                        : farCanopyMaterial;
                    var renderParams = new RenderParams(material)
                    {
                        worldBounds = bounds,
                        shadowCastingMode = ShadowCastingMode.Off,
                        receiveShadows = false,
                    };
                    Graphics.RenderMeshInstanced(renderParams, farInstanceMesh, subMesh, matrices, matrices.Length, 0);
                }
            }
        }

        private void ApplyShadowsOff()
        {
            for (int index = 0; index < _shadowLights.Count; index++)
            {
                _shadowLights[index].shadows = LightShadows.None;
            }
        }

        private void RevertShadows()
        {
            for (int index = 0; index < _shadowLights.Count; index++)
            {
                _shadowLights[index].shadows = _shadowBackup[index];
            }
        }

        private void ApplyShadowDistance30()
        {
            _shadowVolume = new GameObject("ForestBenchmarkShadowVolume");
            var volume = _shadowVolume.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 5000f;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var shadows = profile.Add<HDShadowSettings>(true);
            shadows.maxShadowDistance.overrideState = true;
            shadows.maxShadowDistance.value = 30f;
            volume.profile = profile;
        }

        private void RevertShadowDistance()
        {
            if (_shadowVolume != null)
            {
                Destroy(_shadowVolume);
                _shadowVolume = null;
            }
        }

        private void ApplyHalfTrees()
        {
            for (int index = 0; index < _conifers.Count; index++)
            {
                if (index % 2 == 1)
                {
                    _conifers[index].SetActive(false);
                }
            }
        }

        private void ApplyTreesHidden()
        {
            if (HasDiagnosticLayerSet())
            {
                SetLayer(_nearLayer, false);
                SetLayer(_midLayer, false);
                SetLayer(_farLayer, false);
                SetLayer(_accentLayer, false);
                return;
            }

            for (int index = 0; index < _conifers.Count; index++)
            {
                _conifers[index].SetActive(false);
            }
        }

        private void RevertTreeVisibility()
        {
            if (HasDiagnosticLayerSet())
            {
                RestoreDiagnosticLayers();
            }

            for (int index = 0; index < _conifers.Count; index++)
            {
                _conifers[index].SetActive(true);
            }
        }

        private void ApplyForcedLowestLod()
        {
            for (int index = 0; index < _lodGroups.Length; index++)
            {
                if (_lodGroups[index] != null)
                {
                    _lodGroups[index].ForceLOD(_lodGroups[index].lodCount - 1);
                }
            }
        }

        private void RevertForcedLod()
        {
            for (int index = 0; index < _lodGroups.Length; index++)
            {
                if (_lodGroups[index] != null)
                {
                    _lodGroups[index].ForceLOD(-1);
                }
            }
        }

        private void ApplyMidRing()
        {
            EnsureMidRing();
            _midRingRoot.SetActive(true);
        }

        private void ApplyFullRings()
        {
            EnsureMidRing();
            EnsureFarBatches();
            _midRingRoot.SetActive(true);
            _farRingActive = true;
        }

        private void RevertRings()
        {
            if (_midRingRoot != null)
            {
                _midRingRoot.SetActive(false);
            }

            _farRingActive = false;
        }

        private void EnsureMidRing()
        {
            if (_midRingRoot != null)
            {
                return;
            }

            _midRingRoot = new GameObject("ForestBenchmarkMidRing");
            _midRingRoot.SetActive(false);
            var random = new System.Random(48151);
            for (int index = 0; index < 50; index++)
            {
                GameObject prefab = midRingPrefabs[index % midRingPrefabs.Length];
                GameObject instance = Instantiate(prefab, _midRingRoot.transform);
                float angle = index * 2.39996f;
                float radius = 38f + (float)random.NextDouble() * 52f;
                instance.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
                instance.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                instance.transform.localScale = Vector3.one * (0.85f + (float)random.NextDouble() * 0.4f);
                foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                {
                    Destroy(collider);
                }
            }
        }

        private void EnsureFarBatches()
        {
            if (_farBatches.Count > 0)
            {
                return;
            }

            var random = new System.Random(90210);
            const int total = 400;
            const int batchSize = 200;
            for (int start = 0; start < total; start += batchSize)
            {
                var matrices = new Matrix4x4[Mathf.Min(batchSize, total - start)];
                for (int index = 0; index < matrices.Length; index++)
                {
                    float angle = (start + index) * 2.39996f;
                    float radius = 95f + (float)random.NextDouble() * 205f;
                    var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    var rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                    float scale = 0.8f + (float)random.NextDouble() * 0.5f;
                    matrices[index] = Matrix4x4.TRS(position, rotation, Vector3.one * scale);
                }

                _farBatches.Add(matrices);
            }
        }

        private static float ReadShadowDistance()
        {
            HDShadowSettings settings;
            if (VolumeManager.instance != null &&
                VolumeManager.instance.stack != null &&
                (settings = VolumeManager.instance.stack.GetComponent<HDShadowSettings>()) != null)
            {
                return settings.maxShadowDistance.value;
            }

            foreach (Volume volume in FindObjectsByType<Volume>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                VolumeProfile profile = volume.profile;
                if (profile != null && profile.TryGet(out settings))
                {
                    return settings.maxShadowDistance.value;
                }
            }

            return -1f;
        }

        private static float Average(IReadOnlyList<float> values, int divisor)
        {
            double total = 0d;
            for (int index = 0; index < values.Count; index++)
            {
                total += values[index];
            }

            return (float)(total / Mathf.Max(1, divisor));
        }

        private static float Percentile(IReadOnlyList<float> sortedValues, float percentile)
        {
            if (sortedValues.Count == 0)
            {
                return 0f;
            }

            int index = Mathf.Clamp(
                Mathf.CeilToInt(sortedValues.Count * percentile) - 1,
                0,
                sortedValues.Count - 1);
            return sortedValues[index];
        }

        private static float BytesToMegabytes(double bytes)
        {
            return (float)(bytes / (1024d * 1024d));
        }

        private static string ClassifyBottleneck(ScenarioResult result)
        {
            float cpu = Mathf.Max(result.cpuMainThreadWorkAvgMs, result.cpuRenderThreadAvgMs);
            float gpu = result.gpuAvgMs;
            if (cpu <= 0f && gpu <= 0f)
            {
                return "UNKNOWN_TIMING_UNAVAILABLE";
            }

            if (gpu > 0f && gpu >= cpu * 1.15f)
            {
                return result.cpuMainThreadPresentWaitAvgMs > result.cpuMainThreadWorkAvgMs
                    ? "GPU_OR_PRESENT_BOUND"
                    : "GPU_BOUND";
            }

            if (cpu > 0f && (gpu <= 0f || cpu >= gpu * 1.15f))
            {
                return result.cpuRenderThreadAvgMs >= result.cpuMainThreadAvgMs * 0.85f
                    ? "CPU_RENDER_BOUND"
                    : "CPU_MAIN_BOUND";
            }

            return "MIXED_CPU_GPU";
        }

        private static void EvaluateBudget(ScenarioResult result)
        {
            bool frameTimePass = ProductionForestPerformancePolicy.MeetsFrameTimeBudget(
                result.avgFps,
                result.p95Ms,
                result.p99Ms);
            bool allocationPass = ProductionForestPerformancePolicy.MeetsAllocationBudget(
                result.gcAllocationAvailable,
                result.gcAllocatedPeakBytes);

            var failures = new List<string>(4);
            if (result.avgFps < ProductionForestPerformancePolicy.MinimumPlayableFps)
            {
                failures.Add("average FPS below 60");
            }

            if (result.p95Ms > ProductionForestPerformancePolicy.P95FrameBudgetMilliseconds)
            {
                failures.Add("p95 above 20 ms");
            }

            if (result.p99Ms > ProductionForestPerformancePolicy.P99FrameBudgetMilliseconds)
            {
                failures.Add("p99 above 25 ms");
            }

            if (!result.gcAllocationAvailable)
            {
                failures.Add("GC counter unavailable");
            }
            else if (!allocationPass)
            {
                failures.Add("steady-state GC allocation detected");
            }

            result.budgetFailure = failures.Count == 0 ? string.Empty : string.Join("; ", failures);
            if (frameTimePass && allocationPass)
            {
                result.budgetStatus = "PASS";
            }
            else if (frameTimePass && !result.gcAllocationAvailable)
            {
                result.budgetStatus = "INCOMPLETE";
            }
            else
            {
                result.budgetStatus = "FAIL";
            }
        }

        private void WriteReport(Report report)
        {
            string directory = ReportDirectory();
            Directory.CreateDirectory(directory);
            string json = JsonUtility.ToJson(report, true);
            WriteAllTextAtomic(ReportJsonPath(report.phase), json);

            var markdown = new StringBuilder();
            markdown.AppendLine($"# Forest benchmark - phase `{report.phase}`");
            markdown.AppendLine();
            markdown.AppendLine($"- Created (UTC): {report.createdUtc}");
            markdown.AppendLine($"- Schema: {report.schemaVersion}");
            markdown.AppendLine($"- Unity/platform: {report.unityVersion} / {report.platform}");
            markdown.AppendLine($"- OS: {report.operatingSystem}");
            markdown.AppendLine($"- CPU: {report.processorType} ({report.processorCount} logical cores, {report.processorFrequencyMhz} MHz)");
            markdown.AppendLine($"- GPU: {report.graphicsDeviceName} ({report.graphicsDeviceType}, {report.graphicsMemoryMb} MB)");
            markdown.AppendLine($"- Driver: {report.graphicsDriverVersion}; Windows power plan: {report.windowsPowerPlan}; AC online: {report.powerOnline}");
            markdown.AppendLine($"- Windows graphics preference: {report.windowsGraphicsPreference}");
            markdown.AppendLine($"- Render pipeline: {report.renderPipeline}; quality: {report.qualityLevel}; resolution: {report.width}x{report.height}");
            markdown.AppendLine($"- Window mode: {report.fullScreenMode}; VSync: {report.vSyncCount}; targetFrameRate: {report.targetFrameRate}");
            markdown.AppendLine($"- AA: {report.antialiasingMode}; render scale: {report.renderScalePercent}%; dynamic resolution: {report.dynamicResolutionEnabled}; upscaler: {report.upscaler}");
            markdown.AppendLine($"- Fullscreen effects: {report.fullscreenEffectsEnabled}; shadows: {report.shadowsEnabled}");
            markdown.AppendLine($"- Development build: {report.developmentBuild}; Profiler: {report.profilerEnabled}; binary log: {report.profilerBinaryLogEnabled}; deep profiling: {report.deepProfilingBuild}");
            markdown.AppendLine($"- Warm-up: {report.shaderWarmupProcedure}; {Format(report.shaderWarmupSeconds, 1)} seconds; run: {report.benchmarkRunId}");
            markdown.AppendLine($"- Scenario: {report.benchmarkScenario}; build kind: {report.buildKind}; measurement eligible: {report.measurementEligible}");
            markdown.AppendLine($"- Screenshot requested: {report.screenshotRequested}");
            markdown.AppendLine($"- Screenshot: {report.screenshotPath}");
            markdown.AppendLine($"- Run in background: {report.runInBackground}");
            markdown.AppendLine($"- Frame timing feature enabled: {report.frameTimingFeatureEnabled}");
            markdown.AppendLine($"- Shadow distance at start: {report.shadowDistanceAtStart.ToString("F0", CultureInfo.InvariantCulture)} m");
            markdown.AppendLine($"- Gate: >= {report.minimumPlayableFps} FPS, p95 <= {Format(report.p95BudgetMilliseconds, 1)} ms, p99 <= {Format(report.p99BudgetMilliseconds, 1)} ms, GC allocation <= {report.maximumGcAllocationBytesPerFrame} B/frame");
            markdown.AppendLine();
            markdown.AppendLine("| Scenario | Gate | Avg FPS | 1% low | Avg ms | p95 | p99 | CPU avg | Main work | Present wait | Render avg | GPU avg | Bottleneck | GC peak B | Draws | Batches | SetPass | Tris M |");
            markdown.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|");
            foreach (ScenarioResult scenario in report.scenarios)
            {
                markdown.AppendLine(
                    $"| {scenario.name} | {scenario.budgetStatus} | {Format(scenario.avgFps, 1)} | " +
                    $"{Format(scenario.onePercentLowFps, 1)} | {Format(scenario.avgMs, 2)} | " +
                    $"{Format(scenario.p95Ms, 2)} | {Format(scenario.p99Ms, 2)} | " +
                    $"{FormatTiming(scenario.cpuTotalAvgMs)} | {FormatTiming(scenario.cpuMainThreadWorkAvgMs)} | " +
                    $"{FormatTiming(scenario.cpuMainThreadPresentWaitAvgMs)} | " +
                    $"{FormatTiming(scenario.cpuRenderThreadAvgMs)} | {FormatTiming(scenario.gpuAvgMs)} | " +
                    $"{scenario.bottleneck} | {FormatAvailable(scenario.gcAllocationAvailable, scenario.gcAllocatedPeakBytes, 0)} | " +
                    $"{FormatAvailable(scenario.drawCallsAvailable, scenario.drawCalls, 0)} | " +
                    $"{FormatAvailable(scenario.batchesAvailable, scenario.batches, 0)} | " +
                    $"{FormatAvailable(scenario.setPassCallsAvailable, scenario.setPassCalls, 0)} | " +
                    $"{FormatAvailable(scenario.trianglesAvailable, scenario.triangleMillions, 3)} |");
            }

            markdown.AppendLine();
            markdown.AppendLine("| Scenario | Used memory avg/peak MB | Gfx memory avg/peak MB | Texture memory avg/peak MB | Timing samples | Ignored startup stalls | Failure reason |");
            markdown.AppendLine("|---|---:|---:|---:|---:|---:|---|");
            foreach (ScenarioResult scenario in report.scenarios)
            {
                markdown.AppendLine(
                    $"| {scenario.name} | {FormatPair(scenario.totalUsedMemoryAvailable, scenario.totalUsedMemoryAverageMb, scenario.totalUsedMemoryPeakMb)} | " +
                    $"{FormatPair(scenario.gfxUsedMemoryAvailable, scenario.gfxUsedMemoryAverageMb, scenario.gfxUsedMemoryPeakMb)} | " +
                    $"{FormatPair(scenario.textureMemoryAvailable, scenario.textureMemoryAverageMb, scenario.textureMemoryPeakMb)} | " +
                    $"{scenario.timingSamples} | {scenario.ignoredStartupStallFrames} | {scenario.budgetFailure} |");
            }

            WriteAllTextAtomic(ReportMarkdownPath(report.phase), markdown.ToString());
            Debug.Log($"[ForestBenchmark] Report written to Benchmarks/forest_benchmark_{report.phase}.json");
        }

        private bool VerifyRuntimeOutputs(Report report, RunManifest manifest)
        {
            if (!File.Exists(manifest.reportJsonPath) ||
                !File.Exists(manifest.reportMarkdownPath))
            {
                return false;
            }

            Report persisted = JsonUtility.FromJson<Report>(
                File.ReadAllText(manifest.reportJsonPath));
            if (persisted == null ||
                !string.Equals(persisted.benchmarkRunId, manifest.runId, StringComparison.Ordinal) ||
                !string.Equals(persisted.benchmarkScenario, manifest.scenario, StringComparison.Ordinal) ||
                !string.Equals(persisted.buildKind, manifest.buildKind, StringComparison.Ordinal) ||
                persisted.developmentBuild != manifest.developmentBuild)
            {
                return false;
            }

            if (manifest.screenshotRequested)
            {
                return !persisted.measurementEligible &&
                       persisted.scenarios.Count == 0 &&
                       !string.IsNullOrWhiteSpace(manifest.screenshotPath) &&
                       File.Exists(manifest.screenshotPath);
            }

            return persisted.measurementEligible &&
                   persisted.scenarios.Count == 1 &&
                   string.Equals(
                       persisted.scenarios[0].name,
                       manifest.scenario,
                       StringComparison.Ordinal) &&
                   IsFiniteMeasurement(persisted.scenarios[0]);
        }

        private static bool IsFiniteMeasurement(ScenarioResult result)
        {
            return result != null &&
                   IsFinite(result.avgMs) &&
                   IsFinite(result.medianMs) &&
                   IsFinite(result.p95Ms) &&
                   IsFinite(result.p99Ms) &&
                   IsFinite(result.avgFps) &&
                   IsFinite(result.onePercentLowFps) &&
                   IsFinite(result.cpuTotalAvgMs) &&
                   IsFinite(result.cpuMainThreadAvgMs) &&
                   IsFinite(result.cpuRenderThreadAvgMs) &&
                   IsFinite(result.gpuAvgMs) &&
                   IsFinite(result.gcAllocatedAverageBytes) &&
                   IsFinite(result.totalUsedMemoryAverageMb) &&
                   IsFinite(result.gfxUsedMemoryAverageMb) &&
                   IsFinite(result.textureMemoryAverageMb);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void WriteManifest(string path, RunManifest manifest)
        {
            WriteAllTextAtomic(path, JsonUtility.ToJson(manifest, true));
        }

        public static int RecoverFromStartupFailure(
            R2Perf1BenchmarkConfiguration.Settings settings,
            Exception exception,
            Func<bool> restoreState,
            out string manifestPath)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            bool restored = false;
            string restoreFailure = string.Empty;
            try
            {
                restored = restoreState == null || restoreState();
            }
            catch (Exception restoreException)
            {
                restoreFailure = restoreException.GetType().FullName + ": " +
                                 restoreException.Message;
            }

            manifestPath = null;
            if (settings == null || !settings.enabled)
            {
                return StartupFailureExitCode;
            }

            try
            {
                settings.runId = R2Perf1BenchmarkConfiguration.ValidateRunId(settings.runId);
                string directory = R2Perf1BenchmarkConfiguration.ResolveOutputDirectory(
                    settings,
                    UnityEngine.Application.dataPath);
                string failurePhase = settings.Phase;
                Directory.CreateDirectory(directory);
                manifestPath = Path.Combine(
                    directory,
                    $"forest_benchmark_{failurePhase}.manifest.json");
                RunManifest manifest = CreateManifest(settings, failurePhase);
                manifest.status = R2Perf1BenchmarkConfiguration.ManifestInvalid;
                manifest.statusReason = "startup_failure: " + exception.GetType().FullName +
                                        ": " + exception.Message;
                if (!string.IsNullOrEmpty(restoreFailure))
                {
                    manifest.statusReason += "; restore_failure: " + restoreFailure;
                }

                manifest.completedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                manifest.reportWritten = false;
                manifest.runtimeStateRestored = restored;
                WriteManifest(manifestPath, manifest);
            }
            catch (Exception manifestException)
            {
                manifestPath = null;
                Debug.LogError(
                    "[ForestBenchmark] Failed to persist startup-failure manifest: " +
                    manifestException);
            }

            return StartupFailureExitCode;
        }

        public static void RequestStartupFailureExit(int exitCode)
        {
            int nonZeroExitCode = exitCode == 0 ? StartupFailureExitCode : exitCode;
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit(nonZeroExitCode);
#endif
        }

        private void PrepareRuntimeManifest()
        {
            phase = _perf1.Phase;
            string directory = ReportDirectory();
            Directory.CreateDirectory(directory);
            _runManifest = CreateManifest(_perf1, phase);
            _runManifestPath = ManifestPath(phase);
            WriteManifest(_runManifestPath, _runManifest);
        }

        private void HandleStartupFailure(Exception exception)
        {
            int exitCode = RecoverFromStartupFailure(
                _perf1,
                exception,
                () =>
                {
                    RestoreRunnerState();
                    return R2Perf1BenchmarkConfiguration.RestoreRuntimeState();
                },
                out _runManifestPath);
            Debug.LogException(exception);
            RequestStartupFailureExit(exitCode);
        }

        private static RunManifest CreateManifest(
            R2Perf1BenchmarkConfiguration.Settings settings,
            string reportPhase)
        {
            string directory = R2Perf1BenchmarkConfiguration.ResolveOutputDirectory(
                settings,
                UnityEngine.Application.dataPath);
            return new RunManifest
            {
                status = R2Perf1BenchmarkConfiguration.ManifestIncomplete,
                statusReason = "process_has_not_completed",
                createdUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                runId = settings.runId,
                phase = reportPhase,
                scenario = settings.scenario,
                quality = settings.quality,
                antialiasing = settings.antialiasing,
                renderScalePercent = settings.renderScalePercent,
                upscaler = settings.upscaler,
                buildKind = settings.buildKind,
                developmentBuild = Debug.isDebugBuild,
                screenshotRequested = settings.CaptureScreenshot,
                measurementEligible = settings.MeasurementEligible,
                reportJsonPath = Path.Combine(
                    directory,
                    $"forest_benchmark_{reportPhase}.json"),
                reportMarkdownPath = Path.Combine(
                    directory,
                    $"forest_benchmark_{reportPhase}.md"),
            };
        }

        private static void WriteAllTextAtomic(string path, string contents)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, contents);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temporaryPath, path);
        }

        private static string ReportJsonPath(string reportPhase)
        {
            return Path.Combine(ReportDirectory(), $"forest_benchmark_{reportPhase}.json");
        }

        private static string ReportMarkdownPath(string reportPhase)
        {
            return Path.Combine(ReportDirectory(), $"forest_benchmark_{reportPhase}.md");
        }

        private static string ManifestPath(string reportPhase)
        {
            return Path.Combine(ReportDirectory(), $"forest_benchmark_{reportPhase}.manifest.json");
        }

        private static string ReportDirectory()
        {
            return R2Perf1BenchmarkConfiguration.ResolveOutputDirectory(
                UnityEngine.Application.dataPath);
        }

        private static string Format(float value, int decimalPlaces)
        {
            return value.ToString("F" + decimalPlaces, CultureInfo.InvariantCulture);
        }

        private static string FormatTiming(float milliseconds)
        {
            return milliseconds > 0f ? Format(milliseconds, 2) : "n/a";
        }

        private static string FormatAvailable(bool available, double value, int decimalPlaces)
        {
            return available
                ? value.ToString("F" + decimalPlaces, CultureInfo.InvariantCulture)
                : "n/a";
        }

        private static string FormatPair(bool available, float average, float peak)
        {
            return available ? $"{Format(average, 1)} / {Format(peak, 1)}" : "n/a";
        }

        private void RestoreRunnerState()
        {
            if (_runnerStateRestored)
            {
                return;
            }

            _runnerStateRestored = true;
            RestoreDiagnosticLayers();
            _runtimeSampler?.Dispose();
            _runtimeSampler = null;
            if (_perf1 == null || !_perf1.enabled)
            {
                QualitySettings.vSyncCount = _previousVSync;
                UnityEngine.Application.targetFrameRate = _previousTargetFrameRate;
                UnityEngine.Application.runInBackground = _previousRunInBackground;
            }

            foreach (GameObject cameraObject in _disabledCameras)
            {
                if (cameraObject != null)
                {
                    Camera restored = cameraObject.GetComponent<Camera>();
                    if (restored != null)
                    {
                        restored.enabled = true;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            RestoreRunnerState();
            if (_perf1 != null && _perf1.enabled)
            {
                R2Perf1BenchmarkConfiguration.RestoreRuntimeState();
            }
        }

        private sealed class MetricAccumulator
        {
            private readonly List<double> _values;
            private double _sum;
            private bool _sorted;

            public MetricAccumulator(int capacity)
            {
                _values = new List<double>(capacity);
            }

            public int Count => _values.Count;
            public double Average => Count > 0 ? _sum / Count : 0d;
            public double Maximum { get; private set; }

            public void AddIfAvailable(bool available, double value)
            {
                if (!available || double.IsNaN(value) || double.IsInfinity(value))
                {
                    return;
                }

                _values.Add(value);
                _sum += value;
                Maximum = Count == 1 ? value : Math.Max(Maximum, value);
                _sorted = false;
            }

            public double Percentile(float percentile)
            {
                if (Count == 0)
                {
                    return 0d;
                }

                if (!_sorted)
                {
                    _values.Sort();
                    _sorted = true;
                }

                int index = Mathf.Clamp(
                    Mathf.CeilToInt(Count * percentile) - 1,
                    0,
                    Count - 1);
                return _values[index];
            }
        }
    }
}
