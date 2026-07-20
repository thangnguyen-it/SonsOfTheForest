using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SonsOfTheForest.Infrastructure.Editor.ForestCamp
{
    public static class ForestFidelityContentBuilder
    {
        private const string ModelRoot =
            "Assets/_Game/Art/Models/World/Independent/ForestGeometry/";
        private const string MaterialRoot =
            "Assets/_Game/Art/Materials/World/ForestFidelity/";
        private const string PrefabRoot =
            "Assets/_Game/Prefabs/World/ForestFidelity/";
        private const string WorldPrefabPath =
            "Assets/_Game/Prefabs/World/ForestCamp/PRF_ForestCampPlayground.prefab";

        private const string BarkDiffuse =
            "Assets/_Game/Art/Textures/World/PolyHaven/FirSapling/fir_sapling_branches_diff_1k.jpg";
        private const string BarkNormal =
            "Assets/_Game/Art/Textures/World/PolyHaven/FirSapling/fir_sapling_branches_nor_dx_1k.jpg";

        private static readonly Vector3[] TreePositions =
        {
            new(-15.2f, 0f, -10.8f),
            new(-8.7f, 0f, -17.4f),
            new(1.8f, 0f, -22.1f),
            new(17.6f, 0f, -9.5f),
            new(21.8f, 0f, -1.7f),
            new(13.7f, 0f, 17.5f),
            new(5.5f, 0f, 22.4f),
            new(-18.8f, 0f, 13.2f),
        };

        private static readonly float[] TreeYaw =
        {
            19f, 137f, 274f, 61f, 213f, 326f, 102f, 248f,
        };

        private static readonly float[] TreeScale =
        {
            1.02f, 0.91f, 1.07f, 0.88f, 0.96f, 1.04f, 0.9f, 1f,
        };

        [MenuItem("Sons Of The Forest/Build Forest Fidelity Pass")]
        public static void BuildAll()
        {
            MaterialSet materials = CreateMaterials();
            ConfigureModels();
            GameObject[] trees = CreateTreePrefabs(materials);
            GameObject[] grass = CreateGroundCoverPrefabs(
                "GrassTuft",
                materials);
            GameObject[] ferns = CreateGroundCoverPrefabs(
                "Fern",
                materials);
            UpdateWorldPrefab(trees, grass, ferns);
            ValidateGeneratedContent(trees, grass, ferns, materials);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Forest fidelity pass built: closed 3D conifers, grass and ferns; no foliage cards.");
        }

        private static void ConfigureModels()
        {
            string[] modelPaths = AssetDatabase.FindAssets("t:Model", new[] { ModelRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (modelPaths.Length != 9)
            {
                throw new InvalidOperationException(
                    $"Expected nine independent forest FBX files, found {modelPaths.Length}.");
            }

            foreach (string path in modelPaths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException($"Missing model importer: {path}");
                }

                bool changed = false;
                if (importer.importAnimation)
                {
                    importer.importAnimation = false;
                    changed = true;
                }

                if (importer.importCameras)
                {
                    importer.importCameras = false;
                    changed = true;
                }

                if (importer.importLights)
                {
                    importer.importLights = false;
                    changed = true;
                }

                if (importer.isReadable)
                {
                    importer.isReadable = false;
                    changed = true;
                }

                if (importer.meshCompression != ModelImporterMeshCompression.Off)
                {
                    importer.meshCompression = ModelImporterMeshCompression.Off;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static MaterialSet CreateMaterials()
        {
            Texture2D barkDiffuse = Load<Texture2D>(BarkDiffuse);
            Texture2D barkNormal = Load<Texture2D>(BarkNormal);
            return new MaterialSet
            {
                Bark = CreateOrUpdateLit(
                    "MAT_ConiferBark",
                    new Color(0.34f, 0.22f, 0.13f, 1f),
                    0.14f,
                    barkDiffuse,
                    barkNormal,
                    true,
                    1.35f),
                NeedlesDeep = CreateOrUpdateLit(
                    "MAT_ConiferNeedlesDeep",
                    new Color(0.035f, 0.16f, 0.065f, 1f),
                    0.17f),
                NeedlesLight = CreateOrUpdateLit(
                    "MAT_ConiferNeedlesLight",
                    new Color(0.075f, 0.25f, 0.09f, 1f),
                    0.15f),
                GrassDeep = CreateOrUpdateLit(
                    "MAT_GrassDeep",
                    new Color(0.075f, 0.22f, 0.055f, 1f),
                    0.12f),
                GrassDry = CreateOrUpdateLit(
                    "MAT_GrassDry",
                    new Color(0.29f, 0.27f, 0.09f, 1f),
                    0.09f),
                Fern = CreateOrUpdateLit(
                    "MAT_FernDeep",
                    new Color(0.045f, 0.2f, 0.06f, 1f),
                    0.13f),
            };
        }

        private static Material CreateOrUpdateLit(
            string assetName,
            Color color,
            float smoothness,
            Texture2D diffuse = null,
            Texture2D normal = null,
            bool triplanar = false,
            float worldScale = 1f)
        {
            string path = MaterialRoot + assetName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("HDRP/Lit");
                if (shader == null)
                {
                    throw new InvalidOperationException("HDRP/Lit shader is unavailable.");
                }

                material = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_AlphaCutoffEnable", 0f);
            material.SetFloat("_DoubleSidedEnable", 0f);
            material.SetFloat("_SurfaceType", 0f);
            material.SetTexture("_BaseColorMap", diffuse);
            material.SetTexture("_NormalMap", normal);
            material.SetFloat("_NormalScale", normal != null ? 0.8f : 0f);
            material.SetFloat("_UVBase", triplanar ? 5f : 0f);
            material.SetFloat("_TexWorldScale", Mathf.Max(0.01f, worldScale));
            material.enableInstancing = true;
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject[] CreateTreePrefabs(MaterialSet materials)
        {
            var result = new GameObject[3];
            for (int index = 0; index < result.Length; index++)
            {
                string suffix = ((char)('A' + index)).ToString();
                string modelPath = ModelRoot + "MSH_Conifer_" + suffix + ".fbx";
                string prefabPath = PrefabRoot + "PRF_Conifer_" + suffix + ".prefab";
                GameObject source = Load<GameObject>(modelPath);
                GameObject root = UnityEngine.Object.Instantiate(source);
                root.name = "PRF_Conifer_" + suffix;
                ApplyMaterials(root, materials);
                StripImportedCamerasAndLights(root);
                AddTreeCollider(root);
                SavePrefab(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                result[index] = Load<GameObject>(prefabPath);
            }

            return result;
        }

        private static GameObject[] CreateGroundCoverPrefabs(
            string modelLabel,
            MaterialSet materials)
        {
            var result = new GameObject[3];
            for (int index = 0; index < result.Length; index++)
            {
                string suffix = ((char)('A' + index)).ToString();
                string modelPath = ModelRoot + "MSH_" + modelLabel + "_" + suffix + ".fbx";
                string prefabPath = PrefabRoot + "PRF_" + modelLabel + "_" + suffix + ".prefab";
                GameObject source = Load<GameObject>(modelPath);
                GameObject root = UnityEngine.Object.Instantiate(source);
                root.name = "PRF_" + modelLabel + "_" + suffix;
                ApplyMaterials(root, materials);
                StripImportedCamerasAndLights(root);
                SavePrefab(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                result[index] = Load<GameObject>(prefabPath);
            }

            return result;
        }

        private static void ApplyMaterials(GameObject root, MaterialSet materials)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] assigned = renderer.sharedMaterials
                    .Select(source => ResolveMaterial(source != null ? source.name : string.Empty, materials))
                    .ToArray();
                if (assigned.Any(value => value == null))
                {
                    throw new InvalidOperationException(
                        $"Could not resolve all material slots on {root.name}/{renderer.name}.");
                }

                renderer.sharedMaterials = assigned;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
            }
        }

        private static Material ResolveMaterial(string sourceName, MaterialSet materials)
        {
            switch (sourceName)
            {
                case "SOTF_Bark":
                    return materials.Bark;
                case "SOTF_Needles_Deep":
                    return materials.NeedlesDeep;
                case "SOTF_Needles_Light":
                    return materials.NeedlesLight;
                case "SOTF_Grass_Deep":
                    return materials.GrassDeep;
                case "SOTF_Grass_Dry":
                    return materials.GrassDry;
                case "SOTF_Fern":
                    return materials.Fern;
                default:
                    return null;
            }
        }

        private static void AddTreeCollider(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Bounds bounds = CalculateBounds(root);
            var colliderComponent = root.AddComponent<CapsuleCollider>();
            colliderComponent.direction = 1;
            colliderComponent.center = root.transform.InverseTransformPoint(
                new Vector3(bounds.center.x, bounds.size.y * 0.5f, bounds.center.z));
            colliderComponent.height = Mathf.Max(2f, bounds.size.y);
            colliderComponent.radius = Mathf.Clamp(bounds.size.y * 0.031f, 0.36f, 0.62f);
        }

        private static void StripImportedCamerasAndLights(GameObject root)
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            {
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }

            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                UnityEngine.Object.DestroyImmediate(light.gameObject);
            }
        }

        private static void UpdateWorldPrefab(
            IReadOnlyList<GameObject> trees,
            IReadOnlyList<GameObject> grass,
            IReadOnlyList<GameObject> ferns)
        {
            GameObject world = PrefabUtility.LoadPrefabContents(WorldPrefabPath);
            try
            {
                Transform forest = world.transform.Find("ForestModels");
                if (forest == null)
                {
                    throw new InvalidOperationException("World prefab is missing ForestModels.");
                }

                foreach (Transform child in forest.Cast<Transform>()
                             .Where(value => value.name.StartsWith("Fir_", StringComparison.Ordinal) ||
                                             value.name == "ForestFidelityVegetation")
                             .ToArray())
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }

                var fidelityRoot = new GameObject("ForestFidelityVegetation");
                fidelityRoot.transform.SetParent(forest, false);
                var canopy = new GameObject("MatureConiferClusters");
                canopy.transform.SetParent(fidelityRoot.transform, false);
                for (int index = 0; index < TreePositions.Length; index++)
                {
                    GameObject prefab = trees[index % trees.Count];
                    GameObject instance = InstantiatePrefab(prefab, canopy.transform);
                    instance.name = "MatureConifer_" + index.ToString("00");
                    instance.transform.localPosition = TreePositions[index];
                    instance.transform.localRotation = Quaternion.Euler(
                        index % 3 == 0 ? -1.2f : 0.6f,
                        TreeYaw[index],
                        index % 2 == 0 ? 1.6f : -0.9f);
                    instance.transform.localScale = Vector3.one * TreeScale[index];
                }

                var understorey = new GameObject("UnderstoreyPatches");
                understorey.transform.SetParent(fidelityRoot.transform, false);
                PlaceGrassPatches(understorey.transform, grass);
                PlaceFernPatches(understorey.transform, ferns);

                PrefabUtility.SaveAsPrefabAsset(world, WorldPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(world);
            }
        }

        private static void PlaceGrassPatches(
            Transform parent,
            IReadOnlyList<GameObject> grass)
        {
            Vector2[] patchCenters =
            {
                new(-9f, -8f), new(-13f, 7f), new(11f, -9f),
                new(13f, 8f), new(-4f, 14f), new(4f, -15f),
            };
            var random = new System.Random(50329);
            int placed = 0;
            for (int attempt = 0; attempt < 130 && placed < 78; attempt++)
            {
                Vector2 center = patchCenters[random.Next(patchCenters.Length)];
                float angle = (float)(random.NextDouble() * Mathf.PI * 2f);
                float radius = Mathf.Sqrt((float)random.NextDouble()) *
                               (2.2f + (float)random.NextDouble() * 3.8f);
                Vector3 position = new(
                    center.x + Mathf.Cos(angle) * radius,
                    0.015f,
                    center.y + Mathf.Sin(angle) * radius * 0.72f);
                if (new Vector2(position.x, position.z).sqrMagnitude < 38f ||
                    Vector2.Distance(new Vector2(position.x, position.z), new Vector2(0f, 5f)) < 4.4f)
                {
                    continue;
                }

                GameObject instance = InstantiatePrefab(
                    grass[placed % grass.Count],
                    parent);
                instance.name = "GrassPatch_" + placed.ToString("00");
                instance.transform.localPosition = position;
                instance.transform.localRotation = Quaternion.Euler(
                    0f,
                    (float)random.NextDouble() * 360f,
                    0f);
                float scale = 0.62f + (float)random.NextDouble() * 0.72f;
                instance.transform.localScale = new Vector3(
                    scale * (0.88f + (float)random.NextDouble() * 0.24f),
                    scale,
                    scale * (0.88f + (float)random.NextDouble() * 0.24f));
                placed++;
            }
        }

        private static void PlaceFernPatches(
            Transform parent,
            IReadOnlyList<GameObject> ferns)
        {
            var random = new System.Random(71203);
            int placed = 0;
            for (int treeIndex = 0; treeIndex < TreePositions.Length; treeIndex++)
            {
                int count = 2 + treeIndex % 3;
                for (int local = 0; local < count; local++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float radius = 2.2f + (float)random.NextDouble() * 3.5f;
                    Vector3 position = TreePositions[treeIndex] + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0.025f,
                        Mathf.Sin(angle) * radius);
                    GameObject instance = InstantiatePrefab(
                        ferns[placed % ferns.Count],
                        parent);
                    instance.name = "FernPatch_" + placed.ToString("00");
                    instance.transform.localPosition = position;
                    instance.transform.localRotation = Quaternion.Euler(
                        0f,
                        (float)random.NextDouble() * 360f,
                        0f);
                    float scale = 0.72f + (float)random.NextDouble() * 0.55f;
                    instance.transform.localScale = Vector3.one * scale;
                    placed++;
                }
            }
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"Could not instantiate prefab {prefab.name}.");
            }

            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static void ValidateGeneratedContent(
            IReadOnlyList<GameObject> trees,
            IReadOnlyList<GameObject> grass,
            IReadOnlyList<GameObject> ferns,
            MaterialSet materials)
        {
            foreach (GameObject tree in trees)
            {
                MeshFilter[] filters = tree.GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length != 2 ||
                    filters.Sum(value => (long)value.sharedMesh.vertexCount) < 2_000_000L ||
                    filters.Any(value => value.name.Contains("Plane")))
                {
                    throw new InvalidOperationException(
                        $"Conifer geometry gate failed for {tree.name}.");
                }

                if (tree.GetComponent<CapsuleCollider>() == null)
                {
                    throw new InvalidOperationException(
                        $"Conifer collider gate failed for {tree.name}.");
                }
            }

            foreach (GameObject cover in grass.Concat(ferns))
            {
                MeshFilter filter = cover.GetComponentInChildren<MeshFilter>(true);
                if (filter == null || filter.sharedMesh.vertexCount < 1_000 ||
                    filter.name.Contains("Plane"))
                {
                    throw new InvalidOperationException(
                        $"Ground-cover geometry gate failed for {cover.name}.");
                }
            }

            foreach (Material material in materials.All)
            {
                if (material.shader == null ||
                    !material.shader.name.StartsWith("HDRP/", StringComparison.Ordinal) ||
                    material.GetFloat("_AlphaCutoffEnable") > 0.5f)
                {
                    throw new InvalidOperationException(
                        $"Opaque HDRP material gate failed for {material.name}.");
                }
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"No renderers found on {root.name}.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Missing asset: {path}");
            }

            return asset;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (saved == null)
            {
                throw new InvalidOperationException($"Could not save prefab: {path}");
            }
        }

        private sealed class MaterialSet
        {
            public Material Bark;
            public Material NeedlesDeep;
            public Material NeedlesLight;
            public Material GrassDeep;
            public Material GrassDry;
            public Material Fern;

            public IEnumerable<Material> All
            {
                get
                {
                    yield return Bark;
                    yield return NeedlesDeep;
                    yield return NeedlesLight;
                    yield return GrassDeep;
                    yield return GrassDry;
                    yield return Fern;
                }
            }
        }
    }
}
