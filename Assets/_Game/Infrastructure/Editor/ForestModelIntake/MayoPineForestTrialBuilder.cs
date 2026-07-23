using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace SonsOfTheForest.Infrastructure.Editor.ForestModelIntake
{
    /// <summary>
    /// Local-only trial builder for the user-acquired Unity Asset Store package
    /// "Pine forest set [Free sample]" by Mayo Games.
    ///
    /// The raw package folder is intentionally ignored by Git. This tool lets the
    /// project evaluate the package on machines where the user has lawfully
    /// imported it, without making the public repository depend on Asset Store
    /// files or scene GUIDs.
    /// </summary>
    public static class MayoPineForestTrialBuilder
    {
        public const string PackageRoot = "Assets/pineForset_MayoGames_free";
        public const string ComparisonScenePath =
            PackageRoot + "/Scenes/SCN_TRIAL_MayoPineComparison.unity";
        public const string BenchmarkScenePath =
            PackageRoot + "/Scenes/SCN_TRIAL_MayoPineBenchmark.unity";

        private const string CurrentConiferRoot =
            "Assets/_Game/Prefabs/World/ForestLod/";
        private const string MayoPrefabRoot = PackageRoot + "/prefabs/";
        private const string MayoTextureRoot = PackageRoot + "/textures/";
        private const string MayoMaterialRoot = PackageRoot + "/materials/";
        private const string MayoWrapperRoot = PackageRoot + "/SOTF_LocalWrappers/";
        private const string ReportDirectory = "Benchmarks";

        private static readonly string[] CurrentConiferPrefabs =
        {
            CurrentConiferRoot + "PRF_ConiferLod_A.prefab",
            CurrentConiferRoot + "PRF_ConiferLod_B.prefab",
            CurrentConiferRoot + "PRF_ConiferLod_C.prefab",
        };

        private static readonly string[] MayoTreePrefabs =
        {
            MayoPrefabRoot + "furTree.prefab",
            MayoPrefabRoot + "furTreeSmall.prefab",
            MayoPrefabRoot + "furTree_dead.prefab",
        };

        private static readonly string[] MayoGroundPrefabs =
        {
            MayoPrefabRoot + "fern.prefab",
            MayoPrefabRoot + "brunch_1.prefab",
            MayoPrefabRoot + "brunch_2.prefab",
            MayoPrefabRoot + "brunch_3.prefab",
            MayoPrefabRoot + "rock_1.prefab",
        };

        private static readonly IReadOnlyDictionary<string, string> MaterialTextures =
            new Dictionary<string, string>
            {
                { "brunches", MayoTextureRoot + "mish_branches_lambert1_AlbedoTransparency.png" },
                { "fern", MayoTextureRoot + "paprotnik.png" },
                { "furTreeFoliage_wind", MayoTextureRoot + "mish_fur_tree_lambert1_AlbedoTransparency.png" },
                { "furTreeSmall_bark", MayoTextureRoot + "mish_bark.png" },
                { "furTree_bark", MayoTextureRoot + "mish_bark.png" },
                { "rock_1", MayoTextureRoot + "mish_rock_1_lambert1_AlbedoTransparency.png" },
                { "terrain", MayoTextureRoot + "earth_pine_needles_2.png" },
            };

        [MenuItem("Sons Of The Forest/Forest Models/Mayo Local Trial/Build Comparison Scene")]
        public static void BuildComparisonScene()
        {
            EnsurePackageAvailable();
            ConvertLocalMaterialsToHdrp();
            CreateLocalWrapperPrefabs();

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "SCN_TRIAL_MayoPineComparison";
            GameObject root = CreateSceneRoot("SCN_TRIAL_MayoPineComparison");
            CreateGround(root.transform, 34f, 22f);
            CreateSun(root.transform);
            CreateCamera(root.transform, new Vector3(0f, 4.2f, -18f), new Vector3(13f, 0f, 0f));

            GameObject currentRoot = CreateChild(root.transform, "A_CurrentProceduralPlayableConifers");
            GameObject mayoRoot = CreateChild(root.transform, "B_MayoAssetStorePineForestTrial");

            PlacePrefab(CurrentConiferPrefabs[0], currentRoot.transform, "Current_Conifer_A", new Vector3(-10f, 0f, 0f), 0f, 12f, scene);
            PlacePrefab(CurrentConiferPrefabs[1], currentRoot.transform, "Current_Conifer_B", new Vector3(-6.5f, 0f, 1.2f), 34f, 12f, scene);
            PlacePrefab(CurrentConiferPrefabs[2], currentRoot.transform, "Current_Conifer_C", new Vector3(-13.5f, 0f, 1.8f), -28f, 12f, scene);

            string[] treeWrappers = MayoTreeWrapperPrefabs();
            string[] groundWrappers = MayoGroundWrapperPrefabs();
            PlacePrefab(treeWrappers[0], mayoRoot.transform, "Mayo_furTree", new Vector3(5.8f, 0f, 0f), -12f, 12f, scene);
            PlacePrefab(treeWrappers[1], mayoRoot.transform, "Mayo_furTreeSmall", new Vector3(10.0f, 0f, 1.4f), 24f, 6f, scene);
            PlacePrefab(treeWrappers[2], mayoRoot.transform, "Mayo_furTree_dead", new Vector3(13.0f, 0f, 0.1f), -36f, 10f, scene);
            PlacePrefab(groundWrappers[0], mayoRoot.transform, "Mayo_fern_A", new Vector3(7.5f, 0f, -4.0f), 0f, 1.1f, scene);
            PlacePrefab(groundWrappers[0], mayoRoot.transform, "Mayo_fern_B", new Vector3(9.2f, 0f, -3.4f), 56f, 0.9f, scene);
            PlacePrefab(groundWrappers[4], mayoRoot.transform, "Mayo_rock_1", new Vector3(12.0f, 0f, -3.3f), 15f, 0.9f, scene);

            CreateLabel(root.transform, "CURRENT procedural / playable placeholder", new Vector3(-10f, 0.05f, -5.8f));
            CreateLabel(root.transform, "MAYO Asset Store local trial / prototype", new Vector3(9f, 0.05f, -5.8f));

            SaveScene(scene, ComparisonScenePath);
            WriteSceneMetricsReport(scene, "mayo_pine_comparison_trial_scene_metrics.md");
            Debug.Log("[MayoPineTrial] Built comparison scene: " + ComparisonScenePath);
        }

        [MenuItem("Sons Of The Forest/Forest Models/Mayo Local Trial/Build 60 FPS Benchmark Scene")]
        public static void BuildBenchmarkScene()
        {
            EnsurePackageAvailable();
            ConvertLocalMaterialsToHdrp();
            CreateLocalWrapperPrefabs();

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "SCN_TRIAL_MayoPineBenchmark";
            GameObject root = CreateSceneRoot("SCN_TRIAL_MayoPineBenchmark");
            CreateGround(root.transform, 190f, 190f);
            CreateSun(root.transform);
            CreateCamera(root.transform, new Vector3(0f, 8f, -62f), new Vector3(8f, 0f, 0f));

            GameObject near = CreateChild(root.transform, "MayoBenchmark_Near_10");
            GameObject mid = CreateChild(root.transform, "MayoBenchmark_Mid_50");
            GameObject far = CreateChild(root.transform, "MayoBenchmark_Far_300");
            GameObject groundCover = CreateChild(root.transform, "MayoBenchmark_GroundCover");

            PlaceCluster(near.transform, scene, 10, 8f, 24f, 8f, true, 1201);
            PlaceCluster(mid.transform, scene, 50, 28f, 72f, 9f, false, 2202);
            PlaceCluster(far.transform, scene, 300, 78f, 148f, 10f, false, 3303);
            PlaceGroundCover(groundCover.transform, scene, 150, 18f, 80f, 4404);

            SaveScene(scene, BenchmarkScenePath);
            WriteSceneMetricsReport(scene, "mayo_pine_benchmark_scene_metrics.md");
            Debug.Log("[MayoPineTrial] Built benchmark scene: " + BenchmarkScenePath);
        }

        [MenuItem("Sons Of The Forest/Forest Models/Mayo Local Trial/Write Intake Report")]
        public static void WriteLocalIntakeReport()
        {
            EnsurePackageAvailable();
            ConvertLocalMaterialsToHdrp();
            CreateLocalWrapperPrefabs();

            var candidates = MayoTreeWrapperPrefabs()
                .Concat(MayoGroundWrapperPrefabs())
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(value => value != null)
                .ToArray();

            ForestModelIntakeValidator.IntakeReport report =
                ForestModelIntakeValidator.BuildReport(candidates);
            string directory = AbsoluteReportDirectory();
            Directory.CreateDirectory(directory);
            string markdownPath = Path.Combine(directory, "mayo_pine_forest_intake_report.md");
            File.WriteAllText(markdownPath, ToLocalIntakeMarkdown(report));
            Debug.Log("[MayoPineTrial] Wrote local intake report: " + markdownPath);
        }

        public static void ConvertLocalMaterialsToHdrp()
        {
            EnsurePackageAvailable();
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("HDRP/Lit shader is unavailable.");
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MayoMaterialRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    continue;
                }

                material.shader = shader;
                material.SetFloat("_SurfaceType", 0f);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", 0.18f);
                material.enableInstancing = true;

                if (MaterialTextures.TryGetValue(material.name, out string texturePath))
                {
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if (texture != null)
                    {
                        material.SetTexture("_BaseColorMap", texture);
                    }
                }

                if (IsFoliage(material.name))
                {
                    material.SetFloat("_AlphaCutoffEnable", 1f);
                    material.SetFloat("_AlphaCutoff", 0.35f);
                    material.SetFloat("_DoubleSidedEnable", 1f);
                    material.SetFloat("_CullMode", 0f);
                    material.SetColor("_BaseColor", new Color(0.62f, 0.82f, 0.55f, 1f));
                }
                else if (material.name.IndexOf("bark", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    material.SetColor("_BaseColor", new Color(0.55f, 0.45f, 0.35f, 1f));
                }
                else
                {
                    material.SetColor("_BaseColor", Color.white);
                }

                HDMaterial.ValidateMaterial(material);
                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void CreateLocalWrapperPrefabs()
        {
            EnsureFolder(MayoWrapperRoot);

            foreach (string sourcePath in MayoTreePrefabs)
            {
                CreateLocalWrapperPrefab(sourcePath, keepColliders: true);
            }

            foreach (string sourcePath in MayoGroundPrefabs)
            {
                CreateLocalWrapperPrefab(sourcePath, keepColliders: false);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void CreateLocalWrapperPrefab(string sourcePath, bool keepColliders)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                throw new InvalidOperationException("Missing Mayo prefab: " + sourcePath);
            }

            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                root.name = Path.GetFileNameWithoutExtension(WrapperPath(sourcePath));
                ApplyTrialShadowPolicy(root);
                if (!keepColliders)
                {
                    RemoveColliders(root);
                }

                if (PrefabUtility.SaveAsPrefabAsset(root, WrapperPath(sourcePath)) == null)
                {
                    throw new InvalidOperationException("Could not save wrapper prefab: " + WrapperPath(sourcePath));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ApplyTrialShadowPolicy(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.receiveShadows = true;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        material.enableInstancing = true;
                    }
                }
            }

            foreach (LODGroup lodGroup in root.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = lodGroup.GetLODs();
                if (lods.Length == 0)
                {
                    continue;
                }

                foreach (Renderer renderer in lods[lods.Length - 1].renderers)
                {
                    if (renderer != null)
                    {
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                    }
                }
            }
        }

        private static void PlaceCluster(
            Transform parent,
            Scene scene,
            int count,
            float minRadius,
            float maxRadius,
            float targetHeight,
            bool keepColliders,
            int seed)
        {
            var random = new System.Random(seed);
            string[] treePrefabs = MayoTreeWrapperPrefabs();
            for (int index = 0; index < count; index++)
            {
                string prefabPath = treePrefabs[index % treePrefabs.Length];
                float angle = index * 2.39996323f + NextSigned(random) * 0.22f;
                float radius = Mathf.Lerp(minRadius, maxRadius, (float)random.NextDouble());
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
                float scale = targetHeight * Mathf.Lerp(0.82f, 1.18f, (float)random.NextDouble());
                GameObject instance = PlacePrefab(
                    prefabPath,
                    parent,
                    parent.name + "_" + index.ToString("000", CultureInfo.InvariantCulture),
                    position,
                    (float)random.NextDouble() * 360f,
                    scale,
                    scene);
                if (!keepColliders)
                {
                    RemoveColliders(instance);
                }
            }
        }

        private static void PlaceGroundCover(
            Transform parent,
            Scene scene,
            int count,
            float minRadius,
            float maxRadius,
            int seed)
        {
            var random = new System.Random(seed);
            string[] groundPrefabs = MayoGroundWrapperPrefabs();
            for (int index = 0; index < count; index++)
            {
                string prefabPath = groundPrefabs[index % groundPrefabs.Length];
                float angle = index * 2.39996323f + NextSigned(random) * 0.5f;
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
                    Mathf.Lerp(0.55f, 1.35f, (float)random.NextDouble()),
                    scene);
                RemoveColliders(instance);
            }
        }

        private static GameObject PlacePrefab(
            string prefabPath,
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            float targetHeightOrScale,
            Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing prefab: " + prefabPath);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one;

            Bounds bounds = CalculateBounds(instance);
            if (targetHeightOrScale > 2f && bounds.size.y > 0.01f)
            {
                float scale = targetHeightOrScale / bounds.size.y;
                instance.transform.localScale = Vector3.one * scale;
            }
            else
            {
                instance.transform.localScale = Vector3.one * targetHeightOrScale;
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

        private static GameObject CreateSceneRoot(string name)
        {
            return new GameObject(name);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void CreateGround(Transform parent, float width, float depth)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "NeutralGround";
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            ground.transform.localScale = new Vector3(width, 0.1f, depth);
            Material material = new Material(Shader.Find("HDRP/Lit"))
            {
                name = "MAT_TRIAL_NeutralGround_RuntimeOnly",
            };
            material.SetColor("_BaseColor", new Color(0.30f, 0.32f, 0.27f, 1f));
            material.SetFloat("_Smoothness", 0.12f);
            HDMaterial.ValidateMaterial(material);
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateSun(Transform parent)
        {
            var lightObject = new GameObject("Sun_KeyLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.1f;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateCamera(Transform parent, Vector3 position, Vector3 euler)
        {
            var cameraObject = new GameObject("Camera_ComparisonView");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.Euler(euler);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 180f;
            camera.tag = "MainCamera";
        }

        private static void CreateLabel(Transform parent, string text, Vector3 position)
        {
            var labelObject = new GameObject(text.Replace(' ', '_'));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
            TextMesh mesh = labelObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = 0.42f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
        }

        private static void SaveScene(Scene scene, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? PackageRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void WriteSceneMetricsReport(Scene scene, string reportName)
        {
            string directory = AbsoluteReportDirectory();
            Directory.CreateDirectory(directory);
            var builder = new StringBuilder();
            builder.AppendLine("# Mayo local trial scene metrics");
            builder.AppendLine();
            builder.AppendLine("Scene: `" + scene.path + "`");
            builder.AppendLine();
            builder.AppendLine("| Group | Renderers | Materials | Colliders | LODGroups | Total mesh tris | LOD0 tris |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|");

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.transform.Cast<Transform>())
                {
                    if (!child.name.StartsWith("A_", StringComparison.Ordinal) &&
                        !child.name.StartsWith("B_", StringComparison.Ordinal) &&
                        !child.name.StartsWith("MayoBenchmark_", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
                    Material[] materials = renderers
                        .SelectMany(renderer => renderer.sharedMaterials)
                        .Where(material => material != null)
                        .Distinct()
                        .ToArray();
                    LODGroup[] lodGroups = child.GetComponentsInChildren<LODGroup>(true);
                    builder.AppendLine(
                        $"| `{child.name}` | {renderers.Length} | {materials.Length} | " +
                        $"{child.GetComponentsInChildren<Collider>(true).Length} | {lodGroups.Length} | " +
                        $"{TriangleCount(child.gameObject)} | {lodGroups.Sum(Lod0TriangleCount)} |");
                }
            }

            File.WriteAllText(Path.Combine(directory, reportName), builder.ToString());
        }

        private static string ToLocalIntakeMarkdown(ForestModelIntakeValidator.IntakeReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Mayo Pine Forest local intake report");
            builder.AppendLine();
            builder.AppendLine("- Package: Unity Asset Store / Mayo Games / Pine forest set [Free sample]");
            builder.AppendLine("- Local package path: `" + PackageRoot + "`");
            builder.AppendLine("- Raw package folder is ignored by Git; this report is local evidence only.");
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
                .Where(filter => filter.sharedMesh != null)
                .Sum(filter => TriangleCount(filter.sharedMesh));
        }

        private static long Lod0TriangleCount(LODGroup lodGroup)
        {
            LOD[] lods = lodGroup.GetLODs();
            if (lods.Length == 0)
            {
                return 0L;
            }

            return lods[0].renderers
                .Where(renderer => renderer != null)
                .Select(renderer => renderer.GetComponent<MeshFilter>())
                .Where(filter => filter != null && filter.sharedMesh != null)
                .Sum(filter => TriangleCount(filter.sharedMesh));
        }

        private static long TriangleCount(Mesh mesh)
        {
            long total = 0L;
            for (int index = 0; index < mesh.subMeshCount; index++)
            {
                total += (long)mesh.GetIndexCount(index);
            }

            return total / 3L;
        }

        private static bool IsFoliage(string materialName)
        {
            return materialName.IndexOf("foliage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   materialName.IndexOf("fern", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   materialName.IndexOf("brunch", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float NextSigned(System.Random random)
        {
            return (float)(random.NextDouble() * 2d - 1d);
        }

        private static string[] MayoTreeWrapperPrefabs()
        {
            return MayoTreePrefabs.Select(WrapperPath).ToArray();
        }

        private static string[] MayoGroundWrapperPrefabs()
        {
            return MayoGroundPrefabs.Select(WrapperPath).ToArray();
        }

        private static string WrapperPath(string sourcePath)
        {
            return MayoWrapperRoot +
                   "PRF_TRIAL_" +
                   Path.GetFileNameWithoutExtension(sourcePath) +
                   "_SOTFWrapper.prefab";
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

        private static void EnsurePackageAvailable()
        {
            if (!AssetDatabase.IsValidFolder(PackageRoot))
            {
                throw new InvalidOperationException(
                    "Mayo Pine Forest free sample is not imported at " + PackageRoot + ".");
            }

            foreach (string prefabPath in MayoTreePrefabs.Concat(MayoGroundPrefabs))
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    throw new InvalidOperationException("Missing Mayo prefab: " + prefabPath);
                }
            }
        }
    }
}
