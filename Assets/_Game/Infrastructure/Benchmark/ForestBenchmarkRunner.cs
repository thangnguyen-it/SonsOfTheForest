using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
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
        [Serializable]
        public sealed class ScenarioResult
        {
            public string name;
            public int frames;
            public float avgMs;
            public float medianMs;
            public float p95Ms;
            public float avgFps;
            public float batches;
            public float setPassCalls;
            public float triangleMillions;
            public float vertexMillions;
            public int ignoredStartupStallFrames;
        }

        [Serializable]
        public sealed class Report
        {
            public string phase;
            public string createdUtc;
            public float shadowDistanceAtStart;
            public List<ScenarioResult> scenarios = new List<ScenarioResult>();
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
        private Camera _camera;
        private GameObject _midRingRoot;
        private GameObject _shadowVolume;
        private LODGroup[] _lodGroups = Array.Empty<LODGroup>();
        private bool _farRingActive;
        private bool _walkthrough;
        private float _scenarioClock;
        private PropertyInfo _statBatches;
        private PropertyInfo _statSetPass;
        private PropertyInfo _statTriangles;
        private PropertyInfo _statVertices;
        private int _previousVSync;
        private int _previousTargetFrameRate;

        private void Start()
        {
            _previousVSync = QualitySettings.vSyncCount;
            _previousTargetFrameRate = UnityEngine.Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            UnityEngine.Application.targetFrameRate = -1;

            Type stats = Type.GetType("UnityEditor.UnityStats,UnityEditor");
            if (stats != null)
            {
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                _statBatches = stats.GetProperty("batches", flags);
                _statSetPass = stats.GetProperty("setPassCalls", flags);
                _statTriangles = stats.GetProperty("triangles", flags);
                _statVertices = stats.GetProperty("vertices", flags);
            }

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

            _lodGroups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var report = new Report
            {
                phase = phase,
                createdUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                shadowDistanceAtStart = ReadShadowDistance(),
            };

            yield return SampleScenario(report, "baseline_orbit", null, null);
            yield return SampleScenario(report, "shadows_off_orbit", ApplyShadowsOff, RevertShadows);
            yield return SampleScenario(report, "shadow_distance_60", ApplyShadowDistance60, RevertShadowDistance);
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

            WriteReport(report);
            QualitySettings.vSyncCount = _previousVSync;
            UnityEngine.Application.targetFrameRate = _previousTargetFrameRate;
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        private IEnumerator SampleScenario(Report report, string name, Action apply, Action revert)
        {
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
                yield return null;
            }

            var frameTimes = new List<float>(1024);
            double batches = 0d;
            double setPass = 0d;
            double triangles = 0d;
            double vertices = 0d;
            int statSamples = 0;
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
                frameTimes.Add(frameMs);
                MoveCamera(_scenarioClock / sampleSeconds);
                if (_statBatches != null)
                {
                    batches += Convert.ToDouble(_statBatches.GetValue(null));
                    setPass += Convert.ToDouble(_statSetPass.GetValue(null));
                    triangles += Convert.ToDouble(_statTriangles.GetValue(null));
                    vertices += Convert.ToDouble(_statVertices.GetValue(null));
                    statSamples++;
                }

                yield return null;
            }

            if (revert != null)
            {
                revert();
            }

            frameTimes.Sort();
            float total = 0f;
            for (int index = 0; index < frameTimes.Count; index++)
            {
                total += frameTimes[index];
            }

            int count = Mathf.Max(1, frameTimes.Count);
            var result = new ScenarioResult
            {
                name = name,
                frames = frameTimes.Count,
                avgMs = total / count,
                medianMs = frameTimes.Count > 0 ? frameTimes[frameTimes.Count / 2] : 0f,
                p95Ms = frameTimes.Count > 0
                    ? frameTimes[Mathf.Min(frameTimes.Count - 1, Mathf.FloorToInt(frameTimes.Count * 0.95f))]
                    : 0f,
                ignoredStartupStallFrames = ignoredStartupStallFrames,
            };
            result.avgFps = result.avgMs > 0.0001f ? 1000f / result.avgMs : 0f;
            if (statSamples > 0)
            {
                result.batches = (float)(batches / statSamples);
                result.setPassCalls = (float)(setPass / statSamples);
                result.triangleMillions = (float)(triangles / statSamples / 1_000_000d);
                result.vertexMillions = (float)(vertices / statSamples / 1_000_000d);
            }

            report.scenarios.Add(result);
            Debug.Log(
                $"[ForestBenchmark] {phase}/{name}: {result.avgFps:F1} fps avg, {result.avgMs:F1} ms avg, " +
                $"{result.p95Ms:F1} ms p95, {result.triangleMillions:F2}M tris, {result.batches:F0} batches, " +
                $"{result.ignoredStartupStallFrames} startup stalls ignored");
        }

        private void MoveCamera(float normalizedTime)
        {
            if (_camera == null)
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

        private void ApplyShadowDistance60()
        {
            _shadowVolume = new GameObject("ForestBenchmarkShadowVolume");
            var volume = _shadowVolume.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 5000f;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var shadows = profile.Add<HDShadowSettings>(true);
            shadows.maxShadowDistance.overrideState = true;
            shadows.maxShadowDistance.value = 60f;
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
            for (int index = 0; index < _conifers.Count; index++)
            {
                _conifers[index].SetActive(false);
            }
        }

        private void RevertTreeVisibility()
        {
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

            return -1f;
        }

        private void WriteReport(Report report)
        {
            string directory = Path.GetFullPath(Path.Combine(
                UnityEngine.Application.dataPath,
                "..",
                "Benchmarks"));
            Directory.CreateDirectory(directory);
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(Path.Combine(directory, $"forest_benchmark_{report.phase}.json"), json);

            var markdown = new StringBuilder();
            markdown.AppendLine($"# Forest benchmark - phase `{report.phase}`");
            markdown.AppendLine();
            markdown.AppendLine($"- Created (UTC): {report.createdUtc}");
            markdown.AppendLine($"- Shadow distance at start: {report.shadowDistanceAtStart.ToString("F0", CultureInfo.InvariantCulture)} m");
            markdown.AppendLine();
            markdown.AppendLine("| Scenario | Avg FPS | Avg ms | Median ms | p95 ms | Batches | SetPass | Tris (M) | Verts (M) | Ignored startup stalls |");
            markdown.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (ScenarioResult scenario in report.scenarios)
            {
                markdown.AppendLine(
                    $"| {scenario.name} | {Format(scenario.avgFps, 1)} | {Format(scenario.avgMs, 2)} | " +
                    $"{Format(scenario.medianMs, 2)} | {Format(scenario.p95Ms, 2)} | " +
                    $"{Format(scenario.batches, 0)} | {Format(scenario.setPassCalls, 0)} | " +
                    $"{Format(scenario.triangleMillions, 2)} | {Format(scenario.vertexMillions, 2)} | " +
                    $"{scenario.ignoredStartupStallFrames} |");
            }

            File.WriteAllText(Path.Combine(directory, $"forest_benchmark_{report.phase}.md"), markdown.ToString());
            Debug.Log($"[ForestBenchmark] Report written to Benchmarks/forest_benchmark_{report.phase}.json");
        }

        private static string Format(float value, int decimalPlaces)
        {
            return value.ToString("F" + decimalPlaces, CultureInfo.InvariantCulture);
        }

        private void OnDestroy()
        {
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
    }
}
