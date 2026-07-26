using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SonsOfTheForest.Infrastructure.Benchmark;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace SonsOfTheForest.Infrastructure.Editor.ForestModelIntake
{
    /// <summary>
    /// Local-only Phase 1 forest visual trial builder.
    ///
    /// The four imported Sketchfab/LOLIPOP packs are CC Attribution candidates,
    /// but their raw folders are intentionally ignored by Git while the project
    /// evaluates source fit, wrapper policy, and the 60 FPS benchmark gate.
    /// </summary>
    public static class SotfForestPhase1TrialBuilder
    {
        public const string TrialRoot = "Assets/_LocalTrials/SOTF_ForestPhase1";
        public const string WrapperRoot = TrialRoot + "/Wrappers/";
        public const string SceneRoot = TrialRoot + "/Scenes/";
        public const string BenchmarkScenePath =
            SceneRoot + "SCN_TRIAL_SOTF_ForestPhase1Benchmark.unity";
        public const string RuntimeBenchmarkPhase = "sotf_forest_phase1_local";

        private const string ReportDirectory = "Benchmarks";
        private const string StandaloneBuildDirectory = "Builds/Benchmarks/SOTF_ForestPhase1";
        private const string StandaloneBenchmarkExecutable = "SOTF_ForestPhase1Benchmark.exe";
        private const float MatureConiferMinHeight = 24f;
        private const float MatureConiferMaxHeight = 35f;
        private const float FirFillMinHeight = 16f;
        private const float FirFillMaxHeight = 26f;
        private const float BirchMinHeight = 10f;
        private const float BirchMaxHeight = 16f;
        private const float MapleMinHeight = 8f;
        private const float MapleMaxHeight = 16f;

        private static readonly float[] LodThresholds = { 0.25f, 0.125f, 0.063f, 0.01f };

        private sealed class SourcePack
        {
            public string Species;
            public string ModelPath;
            public string SourceTitle;
            public string SourceUrl;
            public string Author;
            public string License;
            public bool LodMeshesAreTopLevelSiblings;
        }

        private sealed class DistantRenderSource
        {
            public string Name;
            public Mesh Mesh;
            public Material[] Materials;
            public float SourceHeight;
        }

        private static readonly SourcePack[] Sources =
        {
            new SourcePack
            {
                Species = "Birch",
                ModelPath = "Assets/five-birch-trees-pack-lowpoly-lods/source/Birch_trees_pack.fbx",
                SourceTitle = "Five Birch trees pack (lowpoly, LODs)",
                SourceUrl = "https://sketchfab.com/3d-models/five-birch-trees-pack-lowpoly-lods-08fe5117138e4fdaa7ca440ef1201e07",
                Author = "LOLIPOP",
                License = "CC Attribution",
            },
            new SourcePack
            {
                Species = "Maple",
                ModelPath = "Assets/maple-trees-pack-lowpoly-game-ready-lods/source/Acer tree pack.fbx",
                SourceTitle = "Maple trees pack (lowpoly, game ready, LODs)",
                SourceUrl = "https://sketchfab.com/3d-models/maple-trees-pack-lowpoly-game-ready-lods-b5d2833c258f4054a01ee2b4ef85adf0",
                Author = "LOLIPOP",
                License = "CC Attribution",
            },
            new SourcePack
            {
                Species = "Pine",
                ModelPath = "Assets/pine-trees-pack-lowpoly-game-ready-lods/source/Pine_pack.fbx",
                SourceTitle = "Pine trees pack (lowpoly, game ready, LODs)",
                SourceUrl = "https://sketchfab.com/3d-models/pine-trees-pack-lowpoly-game-ready-lods-e1e9c07b8e2e445c943fec660beefba2",
                Author = "LOLIPOP",
                License = "CC Attribution",
            },
            new SourcePack
            {
                Species = "Fir",
                ModelPath = "Assets/realistic-fir-trees-pack-lods-gameready/source/Christmass trees pack.fbx",
                SourceTitle = "Realistic Fir Trees Pack (LODS, gameready)",
                SourceUrl = "https://sketchfab.com/3d-models/realistic-fir-trees-pack-lods-gameready-f58e8b6d733e4b0586e5b7db847b89e7",
                Author = "LOLIPOP",
                License = "CC Attribution",
                LodMeshesAreTopLevelSiblings = true,
            },
        };

        [MenuItem("Sons Of The Forest/Forest Models/SOTF Phase 1 Local Trial/Build Source Wrappers")]
        public static void BuildSourceWrappers()
        {
            EnsureSourcesAvailable();
            EnsureFolder(WrapperRoot);
            var built = new List<GameObject>();

            foreach (SourcePack source in Sources)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(source.ModelPath);
                if (source.LodMeshesAreTopLevelSiblings)
                {
                    built.AddRange(BuildSiblingLodWrappers(source, model));
                }
                else
                {
                    built.AddRange(BuildChildTreeWrappers(source, model));
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            WriteWrapperSummary(built);
            Debug.Log($"[SOTFForestPhase1] Built {built.Count} local wrapper prefabs under {WrapperRoot}.");
        }

        [MenuItem("Sons Of The Forest/Forest Models/SOTF Phase 1 Local Trial/Build Visual Benchmark Scene")]
        public static void BuildVisualBenchmarkScene()
        {
            BuildVisualBenchmarkSceneInternal(false);
        }

        private static void BuildVisualBenchmarkSceneInternal(bool includeStandaloneBenchmarkRunner)
        {
            BuildSourceWrappers();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SCN_TRIAL_SOTF_ForestPhase1Benchmark";
            GameObject root = new GameObject("SCN_TRIAL_SOTF_ForestPhase1Benchmark");
            CreateGround(root.transform, 220f, 220f);
            CreateSun(root.transform);
            CreateCamera(root.transform);
            CreateAtmosphere(root.transform);

            string[] pine = AcceptedWrapperPaths("Pine");
            string[] fir = AcceptedWrapperPaths("Fir");
            string[] maple = AcceptedWrapperPaths("Maple");
            string[] runtimeConifers = Concat(pine, fir);
            string[] broadleafAccents = maple;

            GameObject near = CreateChild(root.transform, "MatureConiferPhase1_Near_10");
            GameObject mid = CreateChild(root.transform, "MatureConiferPhase1_Mid_50");
            GameObject far = CreateChild(root.transform, "MatureConiferPhase1_Far_300");
            GameObject accents = CreateChild(root.transform, "Phase1_BroadleafAccent_24");

            PlaceCluster(
                near.transform,
                scene,
                runtimeConifers,
                ProductionForestPerformancePolicy.NearPlayableTreeCount,
                8f,
                24f,
                true,
                true,
                1101,
                MatureConiferMinHeight,
                MatureConiferMaxHeight);
            PlaceCluster(
                mid.transform,
                scene,
                runtimeConifers,
                ProductionForestPerformancePolicy.MidStaticTreeCount,
                30f,
                78f,
                false,
                false,
                2202,
                18f,
                30f);
            PlaceInstancedFarCluster(
                far.transform,
                runtimeConifers,
                ProductionForestPerformancePolicy.FarInstancedTreeCount,
                82f,
                150f,
                3303,
                12f,
                28f);
            PlaceCluster(
                accents.transform,
                scene,
                broadleafAccents,
                24,
                16f,
                64f,
                false,
                false,
                4404,
                MapleMinHeight,
                MapleMaxHeight);
            if (includeStandaloneBenchmarkRunner)
            {
                AddBenchmarkRunner(root.transform, RuntimeBenchmarkPhase + "_standalone", 3f, 6f);
            }

            SaveScene(scene, BenchmarkScenePath);
            WriteSceneMetricsReport(scene, "sotf_phase1_forest_benchmark_scene_metrics.md");
            Debug.Log("[SOTFForestPhase1] Built visual benchmark scene: " + BenchmarkScenePath);
        }

        [MenuItem("Sons Of The Forest/Forest Models/SOTF Phase 1 Local Trial/Write Intake Report")]
        public static void WriteIntakeReport()
        {
            BuildSourceWrappers();
            GameObject[] wrappers = AssetDatabase.FindAssets("t:Prefab", new[] { WrapperRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(value => value)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(value => value != null)
                .ToArray();

            ForestModelIntakeValidator.IntakeReport report =
                ForestModelIntakeValidator.BuildReport(wrappers);
            string directory = AbsoluteReportDirectory();
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                Path.Combine(directory, "sotf_phase1_forest_model_intake_report.json"),
                JsonUtility.ToJson(report, true));
            File.WriteAllText(
                Path.Combine(directory, "sotf_phase1_forest_model_intake_report.md"),
                ToLocalIntakeMarkdown(report));
            Debug.Log("[SOTFForestPhase1] Wrote local intake report.");
        }

        [MenuItem("Sons Of The Forest/Forest Models/SOTF Phase 1 Local Trial/Run Runtime FPS Benchmark")]
        public static void RunRuntimeBenchmark()
        {
            EnsureSourcesAvailable();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[SOTFForestPhase1] Runtime benchmark cancelled because the current scene was not saved.");
                return;
            }

            BuildVisualBenchmarkScene();
            EditorSceneManager.OpenScene(BenchmarkScenePath, OpenSceneMode.Single);
            SessionState.SetString(SotfForestPhase1RuntimeBenchmark.RunRequestedKey, RuntimeBenchmarkPhase);
            Debug.Log("[SOTFForestPhase1] Starting runtime FPS benchmark in Play Mode...");
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Sons Of The Forest/Forest Models/SOTF Phase 1 Local Trial/Build Standalone Benchmark Player")]
        public static void BuildStandaloneBenchmarkPlayer()
        {
            BuildStandaloneBenchmarkPlayer(false);
        }

        [MenuItem("Sons Of The Forest/Forest Models/SOTF Phase 1 Local Trial/Build And Run Standalone Benchmark Player")]
        public static void BuildAndRunStandaloneBenchmarkPlayer()
        {
            BuildStandaloneBenchmarkPlayer(true);
        }

        private static void BuildStandaloneBenchmarkPlayer(bool autoRun)
        {
            EnsureSourcesAvailable();
            BuildVisualBenchmarkSceneInternal(true);

            string projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            string outputDirectory = Path.Combine(projectRoot, StandaloneBuildDirectory);
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, StandaloneBenchmarkExecutable);

            BuildOptions options = BuildOptions.Development;
            if (autoRun)
            {
                options |= BuildOptions.AutoRunPlayer;
            }

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { BenchmarkScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = options,
            };
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            WriteStandaloneBuildReport(report, outputPath, autoRun);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Standalone benchmark build failed: " + report.summary.result);
            }

            Debug.Log("[SOTFForestPhase1] Built standalone benchmark player: " + outputPath);
        }

        private static IEnumerable<GameObject> BuildChildTreeWrappers(SourcePack source, GameObject model)
        {
            var built = new List<GameObject>();
            foreach (Transform child in model.transform.Cast<Transform>())
            {
                if (ShouldSkipSourceChild(child.name))
                {
                    continue;
                }

                GameObject clone = UnityEngine.Object.Instantiate(child.gameObject);
                try
                {
                    string wrapperName = WrapperName(source.Species, child.name);
                    clone.name = wrapperName;
                    ApplyWrapperPolicy(clone, source.Species);
                    BuildLodGroupFromChildMeshes(clone);
                    AddTrunkCollider(clone, source.Species);

                    string path = WrapperRoot + wrapperName + ".prefab";
                    GameObject saved = PrefabUtility.SaveAsPrefabAsset(clone, path);
                    if (saved == null)
                    {
                        throw new InvalidOperationException("Could not save wrapper: " + path);
                    }

                    built.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(clone);
                }
            }

            return built;
        }

        private static IEnumerable<GameObject> BuildSiblingLodWrappers(SourcePack source, GameObject model)
        {
            var groups = model.transform.Cast<Transform>()
                .Where(child => !ShouldSkipSourceChild(child.name))
                .GroupBy(child => BaseNameWithoutLod(child.name))
                .OrderBy(group => group.Key);

            var built = new List<GameObject>();
            foreach (IGrouping<string, Transform> group in groups)
            {
                var root = new GameObject(WrapperName(source.Species, group.Key));
                try
                {
                    foreach (Transform child in group.OrderBy(value => LodIndex(value.name)))
                    {
                        GameObject clone = UnityEngine.Object.Instantiate(child.gameObject);
                        clone.name = child.name;
                        clone.transform.SetParent(root.transform, false);
                    }

                    ApplyWrapperPolicy(root, source.Species);
                    BuildLodGroupFromChildMeshes(root);
                    AddTrunkCollider(root, source.Species);

                    string path = WrapperRoot + root.name + ".prefab";
                    GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                    if (saved == null)
                    {
                        throw new InvalidOperationException("Could not save wrapper: " + path);
                    }

                    built.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            return built;
        }

        private static void BuildLodGroupFromChildMeshes(GameObject root)
        {
            LODGroup existing = root.GetComponent<LODGroup>();
            if (existing == null)
            {
                existing = root.AddComponent<LODGroup>();
            }

            var lodRenderers = new SortedDictionary<int, List<Renderer>>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                int index = Mathf.Clamp(LodIndex(renderer.name), 0, LodThresholds.Length - 1);
                if (!lodRenderers.TryGetValue(index, out List<Renderer> renderers))
                {
                    renderers = new List<Renderer>();
                    lodRenderers[index] = renderers;
                }

                renderers.Add(renderer);
            }

            var lods = new List<LOD>();
            foreach (KeyValuePair<int, List<Renderer>> pair in lodRenderers)
            {
                lods.Add(new LOD(
                    LodThresholds[Mathf.Min(pair.Key, LodThresholds.Length - 1)],
                    pair.Value.Where(value => value != null).ToArray()));
            }

            if (lods.Count == 0)
            {
                throw new InvalidOperationException(root.name + " has no renderers for LODGroup.");
            }

            existing.SetLODs(lods.ToArray());
            existing.fadeMode = LODFadeMode.CrossFade;
            existing.animateCrossFading = true;
            existing.RecalculateBounds();

            LOD[] final = existing.GetLODs();
            foreach (Renderer renderer in final[final.Length - 1].renderers)
            {
                if (renderer != null)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        private static void ApplyWrapperPolicy(GameObject root, string species)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.receiveShadows = true;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    material.enableInstancing = true;
                    if (material.shader != null && material.shader.name == "Hidden/InternalErrorShader")
                    {
                        Shader shader = Shader.Find("HDRP/Lit");
                        if (shader != null)
                        {
                            material.shader = shader;
                        }
                    }

                    if (material.HasProperty("_SurfaceType"))
                    {
                        material.SetFloat("_SurfaceType", 0f);
                    }

                    if (material.HasProperty("_Smoothness"))
                    {
                        material.SetFloat("_Smoothness", species == "Birch" ? 0.22f : 0.16f);
                    }

                    HDMaterial.ValidateMaterial(material);
                    EditorUtility.SetDirty(material);
                }
            }
        }

        private static void AddTrunkCollider(GameObject root, string species)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Bounds bounds = CalculateBounds(root);
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.center = new Vector3(0f, bounds.size.y * 0.5f, 0f);
            capsule.height = Mathf.Max(1f, bounds.size.y);
            float factor = species == "Birch" ? 0.025f : 0.034f;
            capsule.radius = Mathf.Clamp(bounds.size.y * factor, 0.18f, 0.58f);
        }

        private static void PlaceCluster(
            Transform parent,
            Scene scene,
            IReadOnlyList<string> prefabPaths,
            int count,
            float minRadius,
            float maxRadius,
            bool keepColliders,
            bool allowRealtimeShadows,
            int seed,
            float minHeight,
            float maxHeight)
        {
            if (prefabPaths.Count == 0)
            {
                throw new InvalidOperationException("No prefabs available for " + parent.name);
            }

            var random = new System.Random(seed);
            for (int index = 0; index < count; index++)
            {
                string prefabPath = prefabPaths[index % prefabPaths.Count];
                float angle = index * 2.39996323f + NextSigned(random) * 0.35f;
                float radius = Mathf.Lerp(minRadius, maxRadius, (float)random.NextDouble());
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
                GameObject instance = PlacePrefab(
                    prefabPath,
                    parent,
                    parent.name + "_" + index.ToString("000", CultureInfo.InvariantCulture),
                    position,
                    (float)random.NextDouble() * 360f,
                    Mathf.Lerp(minHeight, maxHeight, (float)random.NextDouble()),
                    scene);
                if (!keepColliders)
                {
                    RemoveColliders(instance);
                }

                ApplyPlacedInstancePolicy(instance, allowRealtimeShadows);
            }
        }

        private static void ApplyPlacedInstancePolicy(GameObject root, bool allowRealtimeShadows)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = allowRealtimeShadows
                    ? renderer.shadowCastingMode
                    : ShadowCastingMode.Off;
                renderer.receiveShadows = allowRealtimeShadows && renderer.receiveShadows;
            }
        }

        private static void PlaceInstancedFarCluster(
            Transform parent,
            IReadOnlyList<string> prefabPaths,
            int count,
            float minRadius,
            float maxRadius,
            int seed,
            float minHeight,
            float maxHeight)
        {
            DistantRenderSource[] sources = prefabPaths
                .SelectMany(ExtractDistantRenderSources)
                .Where(value => value.Mesh != null && value.Materials.Length > 0)
                .OrderBy(value => value.Name)
                .ToArray();
            if (sources.Length == 0)
            {
                throw new InvalidOperationException("No accepted distant LOD render sources available for " + parent.name);
            }

            var positions = new List<Vector3>[sources.Length];
            var rotations = new List<Vector3>[sources.Length];
            var scales = new List<float>[sources.Length];
            for (int index = 0; index < sources.Length; index++)
            {
                positions[index] = new List<Vector3>();
                rotations[index] = new List<Vector3>();
                scales[index] = new List<float>();
            }

            var random = new System.Random(seed);
            for (int index = 0; index < count; index++)
            {
                int sourceIndex = index % sources.Length;
                float angle = index * 2.39996323f + NextSigned(random) * 0.35f;
                float radius = Mathf.Lerp(minRadius, maxRadius, (float)random.NextDouble());
                float targetHeight = Mathf.Lerp(minHeight, maxHeight, (float)random.NextDouble());
                positions[sourceIndex].Add(new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius));
                rotations[sourceIndex].Add(new Vector3(0f, (float)random.NextDouble() * 360f, 0f));
                scales[sourceIndex].Add(targetHeight / Mathf.Max(0.01f, sources[sourceIndex].SourceHeight));
            }

            var cloudObject = new GameObject(parent.name + "_GPUInstanceCloud");
            cloudObject.transform.SetParent(parent, false);
            var cloud = cloudObject.AddComponent<ProductionForestRenderer>();
            cloud.InstanceSets = sources
                .Select((source, index) => new ProductionForestRenderer.InstanceSet
                {
                    sourceName = source.Name,
                    mesh = source.Mesh,
                    materials = source.Materials,
                    positions = positions[index].ToArray(),
                    eulerAngles = rotations[index].ToArray(),
                    scales = scales[index].ToArray(),
                    shadowCastingMode = ProductionForestPerformancePolicy.FarShadowCastingMode,
                    receiveShadows = ProductionForestPerformancePolicy.FarReceiveShadows,
                })
                .Where(value => value.positions.Length > 0)
                .ToArray();
            EditorUtility.SetDirty(cloudObject);
        }

        private static IEnumerable<DistantRenderSource> ExtractDistantRenderSources(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                yield break;
            }

            LODGroup lodGroup = prefab.GetComponentInChildren<LODGroup>(true);
            if (lodGroup == null)
            {
                yield break;
            }

            LOD[] lods = lodGroup.GetLODs();
            if (lods.Length == 0)
            {
                yield break;
            }

            LOD final = lods[lods.Length - 1];
            foreach (Renderer renderer in final.renderers.Where(value => value != null))
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                yield return new DistantRenderSource
                {
                    Name = prefab.name + "_" + renderer.name,
                    Mesh = filter.sharedMesh,
                    Materials = renderer.sharedMaterials.Where(value => value != null).ToArray(),
                    SourceHeight = Mathf.Max(0.01f, filter.sharedMesh.bounds.size.y),
                };
            }
        }

        private static GameObject PlacePrefab(
            string prefabPath,
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            float targetHeight,
            Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing wrapper prefab: " + prefabPath);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one;

            Bounds bounds = CalculateBounds(instance);
            if (bounds.size.y > 0.01f)
            {
                instance.transform.localScale = Vector3.one * (targetHeight / bounds.size.y);
            }

            return instance;
        }

        private static void RemoveColliders(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void CreateGround(Transform parent, float width, float depth)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "NeutralForestGround";
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            ground.transform.localScale = new Vector3(width, 0.1f, depth);

            Material material = new Material(Shader.Find("HDRP/Lit"))
            {
                name = "MAT_TRIAL_ForestGround_RuntimeOnly",
            };
            material.SetColor("_BaseColor", new Color(0.22f, 0.25f, 0.20f, 1f));
            material.SetFloat("_Smoothness", 0.10f);
            HDMaterial.ValidateMaterial(material);
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateSun(Transform parent)
        {
            var lightObject = new GameObject("Sun_KeyLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(47f, -32f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.75f;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateCamera(Transform parent)
        {
            var cameraObject = new GameObject("Camera_BenchmarkView");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(0f, 7f, -68f);
            cameraObject.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.tag = "MainCamera";
        }

        private static void AddBenchmarkRunner(
            Transform parent,
            string phase,
            float warmupSeconds,
            float sampleSeconds)
        {
            var host = new GameObject("BenchmarkRunner_Standalone");
            host.transform.SetParent(parent, false);
            var runner = host.AddComponent<ForestBenchmarkRunner>();
            runner.phase = phase;
            runner.warmupSeconds = warmupSeconds;
            runner.sampleSeconds = sampleSeconds;
        }

        private static void CreateAtmosphere(Transform parent)
        {
            var volumeObject = new GameObject("Volume_ForestAtmosphere");
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var shadows = profile.Add<HDShadowSettings>(true);
            shadows.maxShadowDistance.overrideState = true;
            shadows.maxShadowDistance.value =
                ProductionForestPerformancePolicy.BenchmarkShadowDistanceMeters;
            var fog = profile.Add<Fog>(true);
            fog.enabled.overrideState = true;
            fog.enabled.value = true;
            fog.meanFreePath.overrideState = true;
            fog.meanFreePath.value = 140f;
            volume.profile = profile;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static string[] WrapperPaths(string species)
        {
            return AssetDatabase.FindAssets("t:Prefab", new[] { WrapperRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path).IndexOf("_" + species + "_", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(path => path)
                .ToArray();
        }

        private static string[] AcceptedWrapperPaths(string species)
        {
            string[] paths = WrapperPaths(species);
            Dictionary<string, string> pathByName = paths.ToDictionary(
                path => Path.GetFileNameWithoutExtension(path),
                path => path,
                StringComparer.Ordinal);
            ForestModelIntakeValidator.IntakeReport report = ForestModelIntakeValidator.BuildReport(
                paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).Where(value => value != null));

            return report.candidates
                .Where(candidate => candidate.passesHardGate)
                .Select(candidate => pathByName.TryGetValue(candidate.name, out string path) ? path : null)
                .Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(path => path)
                .ToArray();
        }

        private static string[] Concat(params string[][] groups)
        {
            return groups.SelectMany(value => value).Where(value => !string.IsNullOrEmpty(value)).ToArray();
        }

        private static void SaveScene(Scene scene, string path)
        {
            EnsureFolder(SceneRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void WriteSceneMetricsReport(Scene scene, string reportName)
        {
            string directory = AbsoluteReportDirectory();
            Directory.CreateDirectory(directory);

            var builder = new StringBuilder();
            builder.AppendLine("# SOTF Phase 1 forest benchmark scene metrics");
            builder.AppendLine();
            builder.AppendLine("Scene: `" + scene.path + "`");
            builder.AppendLine();
            builder.AppendLine("| Group | Renderers | Materials | Colliders | LODGroups | Instanced trees | Total mesh tris | LOD0 tris | Instanced tris/frame |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.transform.Cast<Transform>())
                {
                    if (!child.name.Contains("Phase1"))
                    {
                        continue;
                    }

                    Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
                    Material[] materials = renderers
                        .SelectMany(value => value.sharedMaterials)
                        .Where(value => value != null)
                        .Distinct()
                        .ToArray();
                    LODGroup[] lodGroups = child.GetComponentsInChildren<LODGroup>(true);
                    ProductionForestRenderer[] clouds =
                        child.GetComponentsInChildren<ProductionForestRenderer>(true);
                    builder.AppendLine(
                        $"| `{child.name}` | {renderers.Length} | {materials.Length} | " +
                        $"{child.GetComponentsInChildren<Collider>(true).Length} | {lodGroups.Length} | " +
                        $"{clouds.Sum(value => value.TotalInstanceCount)} | " +
                        $"{TriangleCount(child.gameObject)} | {lodGroups.Sum(Lod0TriangleCount)} | " +
                        $"{clouds.Sum(value => value.TotalInstancedTriangles)} |");
                }
            }

            File.WriteAllText(Path.Combine(directory, reportName), builder.ToString());
        }

        private static string ToLocalIntakeMarkdown(ForestModelIntakeValidator.IntakeReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# SOTF Phase 1 forest model intake report");
            builder.AppendLine();
            builder.AppendLine("- Created UTC: " + report.createdUtc);
            builder.AppendLine("- Source folders: local/ignored Sketchfab CC Attribution packs recorded in `Assets/_Docs/ThirdParty/FOREST_MODEL_ATTRIBUTION.md`.");
            builder.AppendLine();
            builder.AppendLine("| Candidate | Result | Tris | Renderers | Materials | LODs | Errors | Warnings |");
            builder.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");
            foreach (ForestModelIntakeValidator.CandidateReport candidate in report.candidates)
            {
                builder.AppendLine(
                    $"| {candidate.name} | {(candidate.passesHardGate ? "PASS" : "REJECT")} | " +
                    $"{candidate.totalTriangles} | {candidate.rendererCount} | {candidate.materialCount} | " +
                    $"{candidate.lods.Count} | {candidate.errors.Count} | {candidate.warnings.Count} |");
            }

            return builder.ToString();
        }

        private static void WriteWrapperSummary(IEnumerable<GameObject> wrappers)
        {
            string directory = AbsoluteReportDirectory();
            Directory.CreateDirectory(directory);
            var builder = new StringBuilder();
            builder.AppendLine("# SOTF Phase 1 local wrapper summary");
            builder.AppendLine();
            builder.AppendLine("| Wrapper | Tris | LODGroups | Colliders |");
            builder.AppendLine("|---|---:|---:|---:|");
            foreach (GameObject wrapper in wrappers.Where(value => value != null).OrderBy(value => value.name))
            {
                builder.AppendLine(
                    $"| `{wrapper.name}` | {TriangleCount(wrapper)} | " +
                    $"{wrapper.GetComponentsInChildren<LODGroup>(true).Length} | " +
                    $"{wrapper.GetComponentsInChildren<Collider>(true).Length} |");
            }

            File.WriteAllText(Path.Combine(directory, "sotf_phase1_forest_wrapper_summary.md"), builder.ToString());
        }

        private static void WriteStandaloneBuildReport(
            BuildReport report,
            string outputPath,
            bool autoRun)
        {
            string directory = AbsoluteReportDirectory();
            Directory.CreateDirectory(directory);

            var builder = new StringBuilder();
            builder.AppendLine("# SOTF Phase 1 standalone benchmark build");
            builder.AppendLine();
            builder.AppendLine("- Created UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            builder.AppendLine("- Output: `" + outputPath + "`");
            builder.AppendLine("- Scene: `" + BenchmarkScenePath + "`");
            builder.AppendLine("- Auto run: " + autoRun);
            builder.AppendLine("- Result: " + report.summary.result);
            builder.AppendLine("- Platform: " + report.summary.platform);
            builder.AppendLine("- Total size bytes: " + report.summary.totalSize);
            builder.AppendLine("- Total time: " + report.summary.totalTime);
            builder.AppendLine();
            builder.AppendLine("When this player runs, `ForestBenchmarkRunner` writes the FPS report next to the player data folder.");

            File.WriteAllText(
                Path.Combine(directory, "sotf_phase1_standalone_build_report.md"),
                builder.ToString());
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static long TriangleCount(GameObject root)
        {
            return root.GetComponentsInChildren<MeshFilter>(true)
                .Where(value => value.sharedMesh != null)
                .Sum(value => TriangleCount(value.sharedMesh));
        }

        private static long Lod0TriangleCount(LODGroup lodGroup)
        {
            LOD[] lods = lodGroup.GetLODs();
            if (lods.Length == 0)
            {
                return 0L;
            }

            return lods[0].renderers
                .Where(value => value != null)
                .Select(value => value.GetComponent<MeshFilter>())
                .Where(value => value != null && value.sharedMesh != null)
                .Sum(value => TriangleCount(value.sharedMesh));
        }

        private static long TriangleCount(Mesh mesh)
        {
            long indexCount = 0L;
            for (int index = 0; index < mesh.subMeshCount; index++)
            {
                indexCount += (long)mesh.GetIndexCount(index);
            }

            return indexCount / 3L;
        }

        private static int LodIndex(string name)
        {
            if (name.IndexOf("LOD3", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Billboard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 3;
            }

            if (name.IndexOf("LOD2", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 2;
            }

            if (name.IndexOf("LOD1", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 1;
            }

            return 0;
        }

        private static string BaseNameWithoutLod(string name)
        {
            string result = name;
            foreach (string marker in new[] { "_LOD0", "_LOD1", "_LOD2", "_LOD3" })
            {
                int index = result.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    result = result.Substring(0, index);
                    break;
                }
            }

            return result.Trim();
        }

        private static string WrapperName(string species, string sourceName)
        {
            return "PRF_PHASE1_" + species + "_" + Sanitize(sourceName);
        }

        private static string Sanitize(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                builder.Append(char.IsLetterOrDigit(character) ? character : '_');
            }

            return builder.ToString().Trim('_');
        }

        private static bool ShouldSkipSourceChild(string name)
        {
            return name.Equals("Ground", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Back", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Ref_plane", StringComparison.OrdinalIgnoreCase);
        }

        private static float NextSigned(System.Random random)
        {
            return (float)(random.NextDouble() * 2d - 1d);
        }

        private static void EnsureSourcesAvailable()
        {
            foreach (SourcePack source in Sources)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(source.ModelPath) == null)
                {
                    throw new InvalidOperationException("Missing local source model: " + source.ModelPath);
                }
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            string trimmed = folderPath.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(trimmed))
            {
                return;
            }

            string[] segments = trimmed.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        private static string AbsoluteReportDirectory()
        {
            return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ReportDirectory));
        }
    }

    [InitializeOnLoad]
    public static class SotfForestPhase1RuntimeBenchmark
    {
        public const string RunRequestedKey = "SOTF.ForestPhase1.RuntimeBenchmarkPhase";

        static SotfForestPhase1RuntimeBenchmark()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                string phase = SessionState.GetString(RunRequestedKey, string.Empty);
                if (string.IsNullOrEmpty(phase))
                {
                    return;
                }

                if (UnityEngine.Object.FindFirstObjectByType<ForestBenchmarkRunner>() != null)
                {
                    return;
                }

                var host = new GameObject("SOTFForestPhase1BenchmarkHost");
                var runner = host.AddComponent<ForestBenchmarkRunner>();
                runner.phase = phase;
                runner.warmupSeconds = 0.6f;
                runner.sampleSeconds = 3f;
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseString(RunRequestedKey);
            }
        }
    }
}
