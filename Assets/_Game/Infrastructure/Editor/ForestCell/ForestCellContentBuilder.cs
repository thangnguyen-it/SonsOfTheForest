using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Forest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SonsOfTheForest.Infrastructure.Editor.ForestCell
{
    public static class ForestCellContentBuilder
    {
        public const string SpeciesPath =
            "Assets/_Game/Data/World/Forest/Species/SPC_InterimConifer.asset";
        public const string CellPath =
            "Assets/_Game/Data/World/Forest/Cells/CELL_ProductionForest_001.asset";
        public const string PrefabPath =
            "Assets/_Game/Prefabs/World/ForestCells/PRF_ForestCell_Production_001.prefab";
        public const string ValidationScenePath =
            "Assets/_Game/Scenes/Validation/SCN_Validation_ForestCell.unity";

        private const string ConiferRoot = "Assets/_Game/Prefabs/World/ForestLod/";
        private const string FloorMaterialPath =
            "Assets/_Game/Art/Materials/World/ForestCamp/MAT_ForestFloor.mat";
        private const string StableCellId = "cell.production.forest.001";
        private const string StableSpeciesId = "species.conifer.interim";
        private const int Seed = 481516;
        private const float Density = 0.006f;

        private static readonly string[] VariantIds =
        {
            "variant.conifer.a", "variant.conifer.b", "variant.conifer.c"
        };

        private static readonly string[] VariantPrefabPaths =
        {
            ConiferRoot + "PRF_ConiferLod_A.prefab",
            ConiferRoot + "PRF_ConiferLod_B.prefab",
            ConiferRoot + "PRF_ConiferLod_C.prefab"
        };

        [MenuItem("Sons Of The Forest/Forest Cell/Build Production Forest Cell")]
        public static void BuildAll()
        {
            EnsureFolder(Path.GetDirectoryName(SpeciesPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(CellPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(ValidationScenePath)?.Replace('\\', '/'));

            ForestSpeciesDefinition species = BuildSpecies();
            ForestCellDefinition cell = BuildCell(species);
            GameObject prefab = BuildPrefab(cell, species);
            BuildValidationScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ForestCell] Built {cell.PlacementCount} deterministic static trees, " +
                      $"checksum {cell.PlacementChecksum}.");
        }

        private static ForestSpeciesDefinition BuildSpecies()
        {
            ForestSpeciesDefinition species =
                AssetDatabase.LoadAssetAtPath<ForestSpeciesDefinition>(SpeciesPath);
            if (species == null)
            {
                species = ScriptableObject.CreateInstance<ForestSpeciesDefinition>();
                AssetDatabase.CreateAsset(species, SpeciesPath);
            }

            var variants = new ForestVisualVariant[VariantIds.Length];
            for (int index = 0; index < variants.Length; index++)
            {
                GameObject prefab = Load<GameObject>(VariantPrefabPaths[index]);
                variants[index] = new ForestVisualVariant(VariantIds[index], prefab, 1f);
            }

            species.EditorConfigure(
                StableSpeciesId,
                variants,
                true,
                ForestStaticShadowPolicy.PrefabLodPolicy);
            if (!species.TryValidate(out string reason))
            {
                throw new InvalidOperationException("Species validation failed: " + reason);
            }

            EditorUtility.SetDirty(species);
            return species;
        }

        private static ForestCellDefinition BuildCell(ForestSpeciesDefinition species)
        {
            ForestCellDefinition cell =
                AssetDatabase.LoadAssetAtPath<ForestCellDefinition>(CellPath);
            if (cell == null)
            {
                cell = ScriptableObject.CreateInstance<ForestCellDefinition>();
                AssetDatabase.CreateAsset(cell, CellPath);
            }

            var variantIds = species.VisualVariants.Select(value => value.VariantId.Value).ToArray();
            var weights = species.VisualVariants.Select(value => value.PlacementWeight).ToArray();
            var bounds = new Bounds(new Vector3(0f, 15f, 0f), new Vector3(80f, 30f, 80f));
            var request = new ForestPlacementBakeRequest(
                new Core.World.ForestCellId(StableCellId),
                1,
                bounds,
                Seed,
                Density,
                new[]
                {
                    new ForestSpeciesBakeInput(
                        StableSpeciesId, variantIds, weights, 1f, 0.88f, 1.15f)
                });
            ForestTreePlacementRecord[] placements = ForestPlacementGenerator.Generate(in request);
            cell.EditorApplyBake(StableCellId, 1, 1, bounds, Seed, Density, placements);
            if (!cell.TryValidate(out string reason))
            {
                throw new InvalidOperationException("Cell validation failed: " + reason);
            }

            EditorUtility.SetDirty(cell);
            return cell;
        }

        private static GameObject BuildPrefab(
            ForestCellDefinition cell,
            ForestSpeciesDefinition species)
        {
            var root = new GameObject("PRF_ForestCell_Production_001");
            try
            {
                var visualRootObject = new GameObject("StaticVisuals");
                visualRootObject.transform.SetParent(root.transform, false);
                var bindings = new List<ForestStaticVisualBinding>(cell.PlacementCount);
                long lod0Triangles = 0;

                foreach (ForestTreePlacementRecord placement in cell.Placements)
                {
                    if (!species.TryGetVariant(placement.VariantId.Value, out ForestVisualVariant variant))
                    {
                        throw new InvalidOperationException(
                            "Missing variant: " + placement.VariantId.Value);
                    }

                    GameObject visual = PrefabUtility.InstantiatePrefab(variant.VisualPrefab) as GameObject;
                    if (visual == null)
                    {
                        throw new InvalidOperationException("Could not instantiate visual prefab.");
                    }

                    visual.name = "Tree_" + placement.TreeInstanceId.Value;
                    visual.transform.SetParent(visualRootObject.transform, false);
                    visual.transform.localPosition = placement.LocalPosition;
                    visual.transform.localRotation = Quaternion.Euler(0f, placement.YawDegrees, 0f);
                    visual.transform.localScale = Vector3.one * placement.UniformScale;
                    foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                    {
                        UnityEngine.Object.DestroyImmediate(collider);
                    }

                    SetStaticRecursively(visual);
                    bindings.Add(new ForestStaticVisualBinding(
                        placement.TreeInstanceId.Value,
                        placement.SpeciesId.Value,
                        placement.VariantId.Value,
                        visual.transform));
                    lod0Triangles += CountLod0Triangles(visual);
                }

                var runtime = root.AddComponent<ForestCellRuntime>();
                var counters = new ForestCellRenderCounters(
                    bindings.Count,
                    visualRootObject.GetComponentsInChildren<Renderer>(true).Length,
                    visualRootObject.GetComponentsInChildren<LODGroup>(true).Length,
                    visualRootObject.GetComponentsInChildren<Collider>(true).Length,
                    lod0Triangles);
                runtime.EditorConfigure(
                    cell,
                    new[] { species },
                    visualRootObject.transform,
                    bindings.ToArray(),
                    counters,
                    true);

                if (!runtime.TryValidateConfiguration(out string reason))
                {
                    throw new InvalidOperationException("Runtime validation failed: " + reason);
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return saved != null
                    ? saved
                    : throw new InvalidOperationException("Could not save forest cell prefab.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildValidationScene(GameObject prefab)
        {
            Scene original = SceneManager.GetActiveScene();
            Scene validation = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            validation.name = "SCN_Validation_ForestCell";
            try
            {
                SceneManager.SetActiveScene(validation);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, validation) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate validation cell.");
                }

                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "ValidationGround";
                ground.transform.position = new Vector3(0f, -0.5f, 0f);
                ground.transform.localScale = new Vector3(90f, 1f, 90f);
                Material floor = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
                if (floor != null)
                {
                    ground.GetComponent<MeshRenderer>().sharedMaterial = floor;
                }

                GameObject lightObject = new("ValidationSun");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.shadows = LightShadows.Soft;
                lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

                GameObject cameraObject = new("ValidationCamera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.farClipPlane = 300f;
                cameraObject.transform.position = new Vector3(-42f, 10f, -42f);
                cameraObject.transform.LookAt(new Vector3(0f, 8f, 0f));
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.38f, 0.42f, 0.46f);

                EditorSceneManager.SaveScene(validation, ValidationScenePath, false);
            }
            finally
            {
                EditorSceneManager.CloseScene(validation, true);
                if (original.IsValid() && original.isLoaded)
                {
                    SceneManager.SetActiveScene(original);
                }
            }
        }

        private static long CountLod0Triangles(GameObject visual)
        {
            LODGroup group = visual.GetComponentInChildren<LODGroup>(true);
            Renderer[] renderers = group != null
                ? group.GetLODs()[0].renderers
                : visual.GetComponentsInChildren<Renderer>(true);
            long triangles = 0;
            foreach (Renderer renderer in renderers)
            {
                MeshFilter filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                for (int subMesh = 0; subMesh < filter.sharedMesh.subMeshCount; subMesh++)
                {
                    triangles += (long)filter.sharedMesh.GetIndexCount(subMesh) / 3L;
                }
            }

            return triangles;
        }

        private static void SetStaticRecursively(GameObject value)
        {
            GameObjectUtility.SetStaticEditorFlags(value, StaticEditorFlags.BatchingStatic);
            foreach (Transform child in value.transform)
            {
                SetStaticRecursively(child.gameObject);
            }
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new InvalidOperationException("Missing asset: " + path);
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
