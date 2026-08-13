using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Forest;
using SonsOfTheForest.Infrastructure.Validation.Forest;
using SonsOfTheForest.Presentation.ForestCamp;
using SonsOfTheForest.Presentation.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SonsOfTheForest.Infrastructure.Editor.ForestCell
{
    public static class ForestCellContentBuilder
    {
        public const string PineSpeciesPath =
            "Assets/_Game/Data/World/Forest/Species/SPC_Pine.asset";
        public const string FirSpeciesPath =
            "Assets/_Game/Data/World/Forest/Species/SPC_Fir.asset";
        public const string MapleSpeciesPath =
            "Assets/_Game/Data/World/Forest/Species/SPC_Maple.asset";
        public const string CellPath =
            "Assets/_Game/Data/World/Forest/Cells/CELL_ProductionForest_001.asset";
        public const string PrefabPath =
            "Assets/_Game/Prefabs/World/ForestCells/PRF_ForestCell_Production_001.prefab";
        public const string InteractiveTreePrefabPath =
            "Assets/_Game/Prefabs/World/ForestCells/PRF_InteractiveTreeLease.prefab";
        public const string ValidationScenePath =
            "Assets/_Game/Scenes/Validation/SCN_Validation_ForestCell.unity";
        public const string FoundationScenePath =
            "Assets/_Game/Scenes/SCN_Foundation.unity";
        private const string PlayerPrefabPath =
            "Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab";

        private const string FloorMaterialPath =
            "Assets/_Game/Art/Materials/World/ForestCamp/MAT_ForestFloor.mat";
        private const string StableCellId = "cell.production.forest.001";
        private const string PineSpeciesId = "species.pine";
        private const string FirSpeciesId = "species.fir";
        private const string MapleSpeciesId = "species.maple";
        private const string CampfireConflictTreeId = "tree.ba23c0bf6f61c9a1747f";
        private const int PineCount = 25;
        private const int FirCount = 5;

        private static readonly string[] PineVariantIds =
        {
            "variant.pine.large.1", "variant.pine.large.2", "variant.pine.large.3",
            "variant.pine.big.1", "variant.pine.big.2", "variant.pine.big.3"
        };

        private static readonly string[] FirVariantIds =
        {
            "variant.fir.tall", "variant.fir.compact"
        };

        private static readonly string[] MapleVariantIds =
        {
            "variant.maple.large.1", "variant.maple.large.2",
            "variant.maple.large.3", "variant.maple.medium.1"
        };

        private static readonly string[] PinePrefabPaths =
        {
            ForestTreeArtContentBuilder.PrefabRoot + "/Pine/PRF_Tree_PineLarge1.prefab",
            ForestTreeArtContentBuilder.PrefabRoot + "/Pine/PRF_Tree_PineLarge2.prefab",
            ForestTreeArtContentBuilder.PrefabRoot + "/Pine/PRF_Tree_PineLarge3.prefab",
            ForestTreeArtContentBuilder.PrefabRoot + "/Pine/PRF_Tree_PineBig1.prefab",
            ForestTreeArtContentBuilder.PrefabRoot + "/Pine/PRF_Tree_PineBig2.prefab",
            ForestTreeArtContentBuilder.PrefabRoot + "/Pine/PRF_Tree_PineBig3.prefab"
        };

        private static readonly string[] FirPrefabPaths =
        {
            ForestTreeArtContentBuilder.PrefabRoot + "/Fir/PRF_Tree_FirTall.prefab",
            ForestTreeArtContentBuilder.PrefabRoot + "/Fir/PRF_Tree_FirCompact.prefab"
        };

        private static readonly string[] MaplePrefabPaths =
        {
            ForestTreeArtContentBuilder.PrefabRoot + "/Maple/PRF_Tree_MapleLarge1.prefab",
            ForestTreeArtContentBuilder.PrefabRoot + "/Maple/PRF_Tree_MapleLarge2.prefab",
            ForestTreeArtContentBuilder.PrefabRoot + "/Maple/PRF_Tree_MapleLarge3.prefab",
            ForestTreeArtContentBuilder.PrefabRoot + "/Maple/PRF_Tree_MapleMedium1.prefab"
        };

        [MenuItem("Sons Of The Forest/Forest Cell/Build Production Forest Cell")]
        public static void BuildAll()
        {
            EnsureFolder(Path.GetDirectoryName(PineSpeciesPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(CellPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(ValidationScenePath)?.Replace('\\', '/'));

            ForestSpeciesDefinition[] species =
            {
                BuildSpecies(PineSpeciesPath, PineSpeciesId, PineVariantIds, PinePrefabPaths),
                BuildSpecies(FirSpeciesPath, FirSpeciesId, FirVariantIds, FirPrefabPaths),
                BuildSpecies(MapleSpeciesPath, MapleSpeciesId, MapleVariantIds, MaplePrefabPaths)
            };
            ForestCellDefinition cell = BuildCell(species);
            GameObject interactiveTreePrefab = BuildInteractiveTreePrefab();
            GameObject prefab = BuildPrefab(cell, species, interactiveTreePrefab);
            BuildValidationScene(prefab);
            IntegrateFoundation(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ForestCell] Built {cell.PlacementCount} deterministic static trees, " +
                      $"checksum {cell.PlacementChecksum}.");
        }

        private static ForestSpeciesDefinition BuildSpecies(
            string assetPath,
            string speciesId,
            IReadOnlyList<string> variantIds,
            IReadOnlyList<string> prefabPaths)
        {
            ForestSpeciesDefinition species =
                AssetDatabase.LoadAssetAtPath<ForestSpeciesDefinition>(assetPath);
            if (species == null)
            {
                species = ScriptableObject.CreateInstance<ForestSpeciesDefinition>();
                AssetDatabase.CreateAsset(species, assetPath);
            }

            if (variantIds.Count != prefabPaths.Count)
            {
                throw new InvalidOperationException("Species variant IDs and prefabs do not match.");
            }

            var variants = new ForestVisualVariant[variantIds.Count];
            string speciesLabel = speciesId.Replace("species.", string.Empty);
            ForestHarvestProfile harvestProfile = AssetDatabase.LoadAssetAtPath<ForestHarvestProfile>(
                "Assets/_Game/Data/World/Forest/Harvest/HRV_" + speciesLabel + ".asset");
            for (int index = 0; index < variants.Length; index++)
            {
                GameObject prefab = Load<GameObject>(prefabPaths[index]);
                ForestFellingKitDefinition kit =
                    AssetDatabase.LoadAssetAtPath<ForestFellingKitDefinition>(
                        "Assets/_Game/Data/World/Forest/Harvest/Kits/KIT_" +
                        variantIds[index] + ".asset");
                variants[index] = new ForestVisualVariant(variantIds[index], prefab, 1f, kit);
            }

            species.EditorConfigure(
                speciesId,
                variants,
                true,
                ForestStaticShadowPolicy.PrefabLodPolicy,
                harvestProfile);
            if (!species.TryValidate(out string reason))
            {
                throw new InvalidOperationException("Species validation failed: " + reason);
            }

            EditorUtility.SetDirty(species);
            return species;
        }

        private static ForestCellDefinition BuildCell(
            IReadOnlyList<ForestSpeciesDefinition> speciesDefinitions)
        {
            ForestCellDefinition cell =
                AssetDatabase.LoadAssetAtPath<ForestCellDefinition>(CellPath);
            if (cell == null)
            {
                cell = ScriptableObject.CreateInstance<ForestCellDefinition>();
                AssetDatabase.CreateAsset(cell, CellPath);
            }

            if (cell.ForestCellId.Value != StableCellId || cell.PlacementCount != 38)
            {
                throw new InvalidOperationException(
                    "Production art migration requires the approved 38-tree cell identity.");
            }

            ForestTreePlacementRecord[] placements = MigratePlacements(cell, speciesDefinitions);
            cell.EditorApplyBake(
                StableCellId,
                2,
                cell.GeneratorVersion,
                cell.Bounds,
                cell.Seed,
                cell.DensityPerSquareMeter,
                placements);
            if (!cell.TryValidate(out string reason))
            {
                throw new InvalidOperationException("Cell validation failed: " + reason);
            }

            EditorUtility.SetDirty(cell);
            return cell;
        }

        private static ForestTreePlacementRecord[] MigratePlacements(
            ForestCellDefinition cell,
            IReadOnlyList<ForestSpeciesDefinition> speciesDefinitions)
        {
            if (speciesDefinitions == null || speciesDefinitions.Count != 3 ||
                speciesDefinitions.Any(value => value == null))
            {
                throw new InvalidOperationException(
                    "Production art migration requires Pine, Fir and Maple species.");
            }

            ForestSpeciesDefinition pine = speciesDefinitions.Single(value =>
                value.SpeciesId.Value == PineSpeciesId);
            ForestSpeciesDefinition fir = speciesDefinitions.Single(value =>
                value.SpeciesId.Value == FirSpeciesId);
            ForestSpeciesDefinition maple = speciesDefinitions.Single(value =>
                value.SpeciesId.Value == MapleSpeciesId);

            string[] rankedIds = cell.Placements
                .Select(value => value.TreeInstanceId.Value)
                .OrderBy(StableHash)
                .ThenBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var pineIds = new HashSet<string>(
                rankedIds.Take(PineCount), StringComparer.Ordinal);
            var firIds = new HashSet<string>(
                rankedIds.Skip(PineCount).Take(FirCount), StringComparer.Ordinal);

            var migrated = new ForestTreePlacementRecord[cell.PlacementCount];
            for (int index = 0; index < cell.PlacementCount; index++)
            {
                ForestTreePlacementRecord previous = cell.Placements[index];
                string id = previous.TreeInstanceId.Value;
                ForestSpeciesDefinition species = pineIds.Contains(id)
                    ? pine
                    : firIds.Contains(id)
                        ? fir
                        : maple;
                IReadOnlyList<ForestVisualVariant> variants = species.VisualVariants;
                int variantIndex = (int)(StableHash(id + "|variant") % (uint)variants.Count);
                Vector3 position = id == CampfireConflictTreeId
                    ? new Vector3(-12f, previous.LocalPosition.y, 10f)
                    : previous.LocalPosition;
                migrated[index] = new ForestTreePlacementRecord(
                    id,
                    species.SpeciesId.Value,
                    variants[variantIndex].VariantId.Value,
                    position,
                    previous.YawDegrees,
                    previous.UniformScale);
            }

            return migrated;
        }

        private static uint StableHash(string value)
        {
            const uint offset = 2166136261u;
            const uint prime = 16777619u;
            uint hash = offset;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                hash ^= (byte)character;
                hash *= prime;
                hash ^= (byte)(character >> 8);
                hash *= prime;
            }

            return hash;
        }

        private static GameObject BuildPrefab(
            ForestCellDefinition cell,
            ForestSpeciesDefinition[] speciesDefinitions,
            GameObject interactiveTreePrefab)
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
                    ForestSpeciesDefinition species = speciesDefinitions.SingleOrDefault(value =>
                        value.SpeciesId == placement.SpeciesId);
                    if (species == null ||
                        !species.TryGetVariant(placement.VariantId.Value, out ForestVisualVariant variant))
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
                    speciesDefinitions,
                    visualRootObject.transform,
                    bindings.ToArray(),
                    counters,
                    true);

                var interactiveRootObject = new GameObject("InteractiveTrees");
                interactiveRootObject.transform.SetParent(root.transform, false);
                var harvestOutputRootObject = new GameObject("HarvestOutputs");
                harvestOutputRootObject.transform.SetParent(root.transform, false);
                var coordinator = root.AddComponent<ForestCellInteractionCoordinator>();
                coordinator.EditorConfigure(
                    runtime,
                    null,
                    interactiveTreePrefab,
                    interactiveRootObject.transform,
                    harvestOutputRootObject.transform,
                    18f,
                    24f,
                    100f,
                    8f,
                    0.6f,
                    8f);
                if (!coordinator.TryValidateConfiguration(out string coordinatorReason))
                {
                    throw new InvalidOperationException(
                        "Coordinator validation failed: " + coordinatorReason);
                }

                BuildHarvestPresentation(root, coordinator);

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

        private static GameObject BuildInteractiveTreePrefab()
        {
            var root = new GameObject("PRF_InteractiveTreeLease");
            try
            {
                var anchor = new GameObject("VisualAnchor");
                anchor.transform.SetParent(root.transform, false);
                Rigidbody body = root.AddComponent<Rigidbody>();
                body.mass = 200f;
                body.useGravity = false;
                body.isKinematic = true;
                body.constraints = RigidbodyConstraints.FreezeAll;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 7f, 0f);
                collider.height = 14f;
                collider.radius = 0.45f;
                var tree = root.AddComponent<ForestInteractiveTree>();
                tree.EditorConfigure(anchor.transform, body, collider);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, InteractiveTreePrefabPath);
                return saved != null
                    ? saved
                    : throw new InvalidOperationException("Could not save interactive-tree prefab.");
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

                ForestCellInteractionCoordinator coordinator =
                    instance.GetComponent<ForestCellInteractionCoordinator>();
                ForestCellRuntime runtime = instance.GetComponent<ForestCellRuntime>();
                Vector3 firstTree = runtime.Definition.Placements[0].LocalPosition;
                GameObject playerProxy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerProxy.name = "PlayerProxy_ContextMenuHitNearestTree";
                playerProxy.transform.position = firstTree + new Vector3(4f, 1f, 0f);
                playerProxy.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
                var driver = playerProxy.AddComponent<ForestTreeValidationDriver>();
                driver.EditorConfigure(coordinator, 55f);
                coordinator.SetObserver(playerProxy.transform);

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
                light.intensity = 0.9f;
                light.shadows = LightShadows.Soft;
                lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

                GameObject fillLightObject = new("ValidationFill");
                Light fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.intensity = 0.55f;
                fillLight.shadows = LightShadows.None;
                fillLightObject.transform.rotation = Quaternion.Euler(38f, 148f, 0f);

                GameObject cameraObject = new("ValidationCamera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.farClipPlane = 300f;
                cameraObject.transform.position = new Vector3(-52f, 14f, -52f);
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

        [MenuItem("Sons Of The Forest/Forest Harvest/Build Harvest Quality Validation Scene")]
        public static void BuildHarvestQualityValidationScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            ForestCellDefinition cell = AssetDatabase.LoadAssetAtPath<ForestCellDefinition>(CellPath);
            if (prefab == null || playerPrefab == null || cell == null)
            {
                throw new InvalidOperationException("Harvest validation requires production cell and player prefabs.");
            }

            ForestTreePlacementRecord target = cell.Placements.First(value =>
                value.VariantId.Value == "variant.pine.large.1");
            Scene original = SceneManager.GetActiveScene();
            Scene validation = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            validation.name = "SCN_Validation_ForestCell";
            try
            {
                SceneManager.SetActiveScene(validation);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, validation) as GameObject;
                GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab, validation) as GameObject;
                if (instance == null || player == null)
                {
                    throw new InvalidOperationException("Could not instantiate harvest validation composition.");
                }

                player.name = "Player_HarvestQuality_Controls_1_2_Mouse";
                Vector3 targetWorld = instance.transform.TransformPoint(target.LocalPosition);
                player.transform.position = targetWorld + new Vector3(0f, 0.05f, -2.15f);
                player.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                instance.GetComponent<ForestCellInteractionCoordinator>().SetObserver(player.transform);
                var validationDriver = player.AddComponent<ForestHarvestQualityValidationDriver>();
                validationDriver.EditorConfigure(player.GetComponent<PlayerAxeHarvestController>(), true,
                    player.GetComponent<TransformPlayerLookDriver>());

                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "ValidationGround";
                ground.transform.position = new Vector3(0f, -0.5f, 0f);
                ground.transform.localScale = new Vector3(90f, 1f, 90f);
                Material floor = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
                if (floor != null) ground.GetComponent<MeshRenderer>().sharedMaterial = floor;

                GameObject lightObject = new("ValidationSun");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 0.9f;
                light.shadows = LightShadows.Soft;
                lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

                GameObject overview = new("ValidationOverviewCamera");
                Camera camera = overview.AddComponent<Camera>();
                camera.enabled = false;
                camera.farClipPlane = 300f;
                overview.transform.position = targetWorld + new Vector3(-10f, 5f, -10f);
                overview.transform.LookAt(targetWorld + Vector3.up * 4f);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.38f, 0.42f, 0.46f);
                EditorSceneManager.SaveScene(validation, ValidationScenePath, false);
            }
            finally
            {
                EditorSceneManager.CloseScene(validation, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }

        private static void IntegrateFoundation(GameObject prefab)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != FoundationScenePath || scene.isDirty)
            {
                throw new InvalidOperationException(
                    "SCN_Foundation must be active and clean before production-cell integration.");
            }

            Transform sceneRoot = scene.GetRootGameObjects()
                .Select(value => value.transform)
                .SingleOrDefault(value => value.name == "SCN_Foundation");
            Transform terrain = sceneRoot != null
                ? sceneRoot.Find("_WORLD/Terrain")
                : null;
            Transform localPlayer = sceneRoot != null
                ? sceneRoot.Find("_GAMEPLAY/Player/LocalPlayer")
                : null;
            Transform forestGround = terrain != null
                ? terrain.Find("PRF_ForestCampPlayground/ForestGround")
                : null;
            Transform matureTrees = terrain != null
                ? terrain.Find(
                    "PRF_ForestCampPlayground/ForestModels/" +
                    "ForestFidelityVegetation/MatureConiferClusters")
                : null;
            if (terrain == null || localPlayer == null || forestGround == null ||
                matureTrees == null)
            {
                throw new InvalidOperationException(
                    "SCN_Foundation is missing Terrain, LocalPlayer, ForestGround or " +
                    "legacy mature-tree roots.");
            }

            ForestCellRuntime[] existingCells = scene.GetRootGameObjects()
                .SelectMany(value => value.GetComponentsInChildren<ForestCellRuntime>(true))
                .ToArray();
            foreach (ForestCellRuntime existing in existingCells)
            {
                if (existing.transform.IsChildOf(terrain))
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Could not instantiate production forest cell in SCN_Foundation.");
            }

            instance.name = "PRF_ForestCell_Production_001";
            instance.transform.SetParent(terrain, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            ForestCellInteractionCoordinator coordinator =
                instance.GetComponent<ForestCellInteractionCoordinator>();
            coordinator.SetObserver(localPlayer);
            EditorUtility.SetDirty(coordinator);
            ForestHarvestPresentationPool presentation =
                instance.GetComponent<ForestHarvestPresentationPool>();
            Camera playerCamera = localPlayer.GetComponentInChildren<Camera>(true);
            if (presentation != null && playerCamera != null)
            {
                presentation.SetObserver(playerCamera.transform);
                EditorUtility.SetDirty(presentation);
            }

            matureTrees.gameObject.SetActive(false);
            EditorUtility.SetDirty(matureTrees.gameObject);
            Vector3 groundScale = forestGround.localScale;
            forestGround.localScale = new Vector3(90f, groundScale.y, 90f);
            EditorUtility.SetDirty(forestGround);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, FoundationScenePath, false))
            {
                throw new InvalidOperationException("Could not save SCN_Foundation.");
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

        private static void BuildHarvestPresentation(
            GameObject root, ForestCellInteractionCoordinator coordinator)
        {
            const string chipSource =
                "Assets/_Game/Art/World/Forest/Harvest/Generated/Effects/CHIPS_Harvest.fbx";
            Mesh woodChip = LoadMesh(chipSource, "WoodChip");
            Mesh barkChip = LoadMesh(chipSource, "BarkChip");
            Material wood = Load<Material>(
                "Assets/_Game/Art/World/Forest/Harvest/Materials/MAT_Pine_EndGrain.mat");
            Material bark = Load<Material>(
                "Assets/_Game/Art/World/Forest/Trees/Curated/Materials/Pine/MAT_Pine_Bark.mat");
            var chips = new ParticleSystem[4];
            var impacts = new ParticleSystem[3];
            for (int index = 0; index < chips.Length; index++)
            {
                chips[index] = CreateParticlePool(
                    root.transform, "WoodChipPool_" + index,
                    index % 2 == 0 ? woodChip : barkChip,
                    index % 2 == 0 ? wood : bark, false);
            }

            for (int index = 0; index < impacts.Length; index++)
            {
                impacts[index] = CreateParticlePool(
                    root.transform, "ImpactDebrisPool_" + index,
                    barkChip, bark, true);
            }

            AudioSource audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Logarithmic;
            audio.maxDistance = 30f;
            ForestHarvestPresentationPool pool = root.AddComponent<ForestHarvestPresentationPool>();
            pool.EditorConfigure(coordinator, null, chips, impacts, audio);
        }

        private static ParticleSystem CreateParticlePool(
            Transform parent, string name, Mesh mesh, Material material, bool impact)
        {
            var value = new GameObject(name);
            value.transform.SetParent(parent, false);
            ParticleSystem particles = value.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = impact ? 0.55f : 0.32f;
            main.startLifetime = impact ? 1.2f : 0.65f;
            main.startSpeed = impact ? 3.0f : 2.2f;
            main.startSize = impact ? 1.5f : 1f;
            main.maxParticles = impact ? 28 : 18;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = impact ? 1.1f : 0.75f;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)(impact ? 22 : 12))
            });
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = impact ? 62f : 36f;
            shape.radius = impact ? 0.8f : 0.12f;
            ParticleSystemRenderer renderer = value.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        private static Mesh LoadMesh(string path, string name)
        {
            Mesh value = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Mesh>()
                .SingleOrDefault(candidate => candidate.name == name);
            return value != null
                ? value
                : throw new InvalidOperationException("Missing mesh " + name + " in " + path);
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
