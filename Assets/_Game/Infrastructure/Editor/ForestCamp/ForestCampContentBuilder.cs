using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonsOfTheForest.Application.ForestCamp;
using SonsOfTheForest.Infrastructure.SceneComposition;
using SonsOfTheForest.Presentation.ForestCamp;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SonsOfTheForest.Infrastructure.Editor.ForestCamp
{
    public static class ForestCampContentBuilder
    {
        private const string MaterialRoot =
            "Assets/_Game/Art/Materials/World/ForestCamp/";
        private const string WorldPrefabRoot =
            "Assets/_Game/Prefabs/World/ForestCamp/";
        private const string ItemPrefabRoot =
            "Assets/_Game/Prefabs/Items/Resources/";
        private const string BuildingPrefabRoot =
            "Assets/_Game/Prefabs/Building/Campfire/";
        private const string PlayerPrefabPath =
            "Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab";
        private const string WorldPrefabPath =
            WorldPrefabRoot + "PRF_ForestCampPlayground.prefab";
        private const string RecipePath =
            "Assets/_Game/Data/Recipes/RCP_ProvisionalCampfire.asset";
        private const string ScenePath =
            "Assets/_Game/Scenes/SCN_Foundation.unity";

        private const string FirModel =
            "Assets/_Game/Art/Models/World/PolyHaven/FirSapling/fir_sapling_1k.fbx";
        private const string StumpModel =
            "Assets/_Game/Art/Models/World/PolyHaven/TreeStump01/tree_stump_01_1k.fbx";
        private const string RockModel =
            "Assets/_Game/Art/Models/World/PolyHaven/RockMossSet02/rock_moss_set_02_1k.fbx";

        private const string ForestFloorDiffuse =
            "Assets/_Game/Art/Textures/World/PolyHaven/ForestFloor/forest_floor_diff_2k.jpg";
        private const string ForestFloorNormal =
            "Assets/_Game/Art/Textures/World/PolyHaven/ForestFloor/forest_floor_nor_dx_2k.jpg";
        private const string FirBranchesDiffuse =
            "Assets/_Game/Art/Textures/World/PolyHaven/FirSapling/fir_sapling_branches_diff_1k.jpg";
        private const string FirBranchesNormal =
            "Assets/_Game/Art/Textures/World/PolyHaven/FirSapling/fir_sapling_branches_nor_dx_1k.jpg";
        private const string FirTwigsDiffuse =
            "Assets/_Game/Art/Textures/World/PolyHaven/FirSapling/fir_sapling_twigs_diff_1k.jpg";
        private const string FirTwigsAlpha =
            "Assets/_Game/Art/Textures/World/PolyHaven/FirSapling/fir_sapling_twigs_alpha_1k.jpg";
        private const string FirTwigsNormal =
            "Assets/_Game/Art/Textures/World/PolyHaven/FirSapling/fir_sapling_twigs_nor_dx_1k.jpg";
        private const string FirTwigsRgba =
            "Assets/_Game/Art/Textures/World/Generated/fir_sapling_twigs_rgba_1k.png";
        private const string StumpDiffuse =
            "Assets/_Game/Art/Textures/World/PolyHaven/TreeStump01/tree_stump_01_diff_1k.jpg";
        private const string StumpNormal =
            "Assets/_Game/Art/Textures/World/PolyHaven/TreeStump01/tree_stump_01_nor_dx_1k.jpg";
        private const string RockDiffuse =
            "Assets/_Game/Art/Textures/World/PolyHaven/RockMossSet02/rock_moss_set_02_diff_1k.jpg";
        private const string RockNormal =
            "Assets/_Game/Art/Textures/World/PolyHaven/RockMossSet02/rock_moss_set_02_nor_dx_1k.jpg";

        [MenuItem("Sons Of The Forest/Build Forest Camp Vertical Slice")]
        public static void BuildAll()
        {
            ConfigureSourceTextures();
            CreateCombinedTwigTexture();
            MaterialSet materials = CreateMaterials();
            CampfireRecipeAsset recipe = CreateRecipe();
            PrefabSet prefabs = CreateSourcePrefabs(materials);
            GameObject stick = CreateStickPickup(materials.Wood);
            GameObject stone = CreateStonePickup(prefabs.Rocks[0]);
            GameObject campfire = CreateCampfire(
                recipe,
                prefabs.Rocks,
                materials);
            CreateWorld(prefabs, stick, stone, campfire, materials.Ground);
            UpdatePlayerPrefab();
            IntegrateOfficialScene();
            ForestFidelityContentBuilder.BuildAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Forest Camp vertical slice content built successfully.");
        }

        private static void ConfigureSourceTextures()
        {
            string[] normalMaps =
            {
                ForestFloorNormal,
                FirBranchesNormal,
                FirTwigsNormal,
                StumpNormal,
                RockNormal,
            };
            foreach (string path in normalMaps)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException(
                        $"Missing normal texture importer: {path}");
                }

                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                importer.flipGreenChannel = true;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 8;
                importer.SaveAndReimport();
            }
        }

        private static void CreateCombinedTwigTexture()
        {
            Texture2D diffuse = LoadImage(FirTwigsDiffuse);
            Texture2D alpha = LoadImage(FirTwigsAlpha);
            if (diffuse.width != alpha.width || diffuse.height != alpha.height)
            {
                throw new InvalidOperationException(
                    "Fir twig diffuse and alpha textures have different dimensions.");
            }

            Color32[] colors = diffuse.GetPixels32();
            Color32[] alphaColors = alpha.GetPixels32();
            for (int index = 0; index < colors.Length; index++)
            {
                colors[index].a = alphaColors[index].r;
            }

            var combined = new Texture2D(
                diffuse.width,
                diffuse.height,
                TextureFormat.RGBA32,
                true,
                false);
            combined.SetPixels32(colors);
            combined.Apply(true, false);
            File.WriteAllBytes(ToAbsolutePath(FirTwigsRgba), combined.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(diffuse);
            UnityEngine.Object.DestroyImmediate(alpha);
            UnityEngine.Object.DestroyImmediate(combined);
            AssetDatabase.ImportAsset(FirTwigsRgba, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(FirTwigsRgba);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 8;
            importer.SaveAndReimport();
        }

        private static Texture2D LoadImage(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(ToAbsolutePath(assetPath))))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException(
                    $"Could not decode texture: {assetPath}");
            }

            return texture;
        }

        private static MaterialSet CreateMaterials()
        {
            return new MaterialSet
            {
                Ground = CreateLitMaterial(
                    MaterialRoot + "MAT_ForestFloor.mat",
                    ForestFloorDiffuse,
                    ForestFloorNormal,
                    new Color(0.38f, 0.4f, 0.32f, 1f),
                    0.14f,
                    new Vector2(20f, 20f)),
                FirBranches = CreateLitMaterial(
                    MaterialRoot + "MAT_FirBranches.mat",
                    FirBranchesDiffuse,
                    FirBranchesNormal,
                    Color.white,
                    0.2f,
                    Vector2.one),
                FirTwigs = CreateLitMaterial(
                    MaterialRoot + "MAT_FirTwigs.mat",
                    FirTwigsRgba,
                    FirTwigsNormal,
                    Color.white,
                    0.15f,
                    Vector2.one,
                    true),
                Stump = CreateLitMaterial(
                    MaterialRoot + "MAT_TreeStump.mat",
                    StumpDiffuse,
                    StumpNormal,
                    Color.white,
                    0.16f,
                    Vector2.one),
                Rock = CreateLitMaterial(
                    MaterialRoot + "MAT_MossRock.mat",
                    RockDiffuse,
                    RockNormal,
                    Color.white,
                    0.18f,
                    Vector2.one),
                Wood = CreateLitMaterial(
                    MaterialRoot + "MAT_CampWood.mat",
                    FirBranchesDiffuse,
                    FirBranchesNormal,
                    new Color(0.65f, 0.52f, 0.4f, 1f),
                    0.12f,
                    new Vector2(2f, 1f)),
                Blueprint = CreateLitMaterial(
                    MaterialRoot + "MAT_CampfireBlueprint.mat",
                    null,
                    null,
                    new Color(0.16f, 0.22f, 0.2f, 1f),
                    0.22f,
                    Vector2.one),
                FlameOuter = CreateEmissiveMaterial(
                    MaterialRoot + "MAT_FlameOuter.mat",
                    new Color(1f, 0.08f, 0.01f, 1f),
                    850f),
                FlameInner = CreateEmissiveMaterial(
                    MaterialRoot + "MAT_FlameInner.mat",
                    new Color(1f, 0.55f, 0.04f, 1f),
                    1400f),
            };
        }

        private static Material CreateLitMaterial(
            string path,
            string diffusePath,
            string normalPath,
            Color color,
            float smoothness,
            Vector2 tiling,
            bool alphaCutout = false)
        {
            AssetDatabase.DeleteAsset(path);
            var material = new Material(Shader.Find("HDRP/Lit"));
            material.name = Path.GetFileNameWithoutExtension(path);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (!string.IsNullOrEmpty(diffusePath))
            {
                material.SetTexture(
                    "_BaseColorMap",
                    AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath));
                material.SetTextureScale("_BaseColorMap", tiling);
            }

            if (!string.IsNullOrEmpty(normalPath))
            {
                material.SetTexture(
                    "_NormalMap",
                    AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                material.SetTextureScale("_NormalMap", tiling);
                material.SetFloat("_NormalScale", 1f);
            }

            if (alphaCutout)
            {
                material.SetFloat("_AlphaCutoffEnable", 1f);
                material.SetFloat("_AlphaCutoff", 0.42f);
                material.SetFloat("_DoubleSidedEnable", 1f);
                material.SetFloat("_CullMode", 0f);
            }

            HDMaterial.ValidateMaterial(material);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material CreateEmissiveMaterial(
            string path,
            Color color,
            float intensity)
        {
            Material material = CreateLitMaterial(
                path,
                null,
                null,
                color,
                0.08f,
                Vector2.one);
            material.SetColor("_EmissiveColor", color);
            material.SetColor("_EmissiveColorLDR", color);
            material.SetFloat("_UseEmissiveIntensity", 1f);
            material.SetFloat("_EmissiveIntensity", intensity);
            material.SetFloat("_EmissiveExposureWeight", 0.8f);
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static CampfireRecipeAsset CreateRecipe()
        {
            AssetDatabase.DeleteAsset(RecipePath);
            var recipe = ScriptableObject.CreateInstance<CampfireRecipeAsset>();
            AssetDatabase.CreateAsset(recipe, RecipePath);
            var serialized = new SerializedObject(recipe);
            SerializedProperty requirements = serialized.FindProperty("requirements");
            requirements.arraySize = 2;
            SetRequirement(requirements.GetArrayElementAtIndex(0), "resource.stick", 2);
            SetRequirement(requirements.GetArrayElementAtIndex(1), "resource.stone", 4);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssetIfDirty(recipe);
            return recipe;
        }

        private static void SetRequirement(
            SerializedProperty property,
            string itemId,
            int quantity)
        {
            property.FindPropertyRelative("itemId").stringValue = itemId;
            property.FindPropertyRelative("quantity").intValue = quantity;
        }

        private static PrefabSet CreateSourcePrefabs(MaterialSet materials)
        {
            var set = new PrefabSet
            {
                Firs = new GameObject[3],
                Rocks = new GameObject[7],
            };
            for (int index = 0; index < 3; index++)
            {
                string suffix = ((char)('A' + index)).ToString();
                set.Firs[index] = CreateModelVariant(
                    FirModel,
                    "fir_sapling_" + suffix.ToLowerInvariant(),
                    WorldPrefabRoot + "PRF_FirSapling_" + suffix + ".prefab",
                    new[] { materials.FirBranches, materials.FirTwigs },
                    ColliderKind.Tree);
            }

            set.Stump = CreateModelVariant(
                StumpModel,
                "tree_stump_01_1k",
                WorldPrefabRoot + "PRF_TreeStump_01.prefab",
                new[] { materials.Stump },
                ColliderKind.Box);

            for (int index = 0; index < 7; index++)
            {
                int number = index + 7;
                set.Rocks[index] = CreateModelVariant(
                    RockModel,
                    "rock_moss_set_02_rock" + number.ToString("00"),
                    WorldPrefabRoot + "PRF_MossRock_" + number.ToString("00") + ".prefab",
                    new[] { materials.Rock },
                    ColliderKind.Box);
            }

            return set;
        }

        private static GameObject CreateModelVariant(
            string modelPath,
            string retainedName,
            string prefabPath,
            Material[] materials,
            ColliderKind colliderKind)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject root = UnityEngine.Object.Instantiate(source);
            Transform retained = source.name == retainedName &&
                                 root.GetComponent<Renderer>() != null
                ? root.transform
                : root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(value => value.name == retainedName);
            if (retained == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    $"Model part '{retainedName}' was not found in '{modelPath}'.");
            }

            root.name = Path.GetFileNameWithoutExtension(prefabPath);
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
            {
                if (child != retained)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            retained.localPosition = Vector3.zero;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            Bounds bounds = CalculateBounds(root);
            if (colliderKind == ColliderKind.Tree)
            {
                var collider = root.AddComponent<CapsuleCollider>();
                collider.center = root.transform.InverseTransformPoint(bounds.center);
                collider.height = Mathf.Max(1f, bounds.size.y);
                collider.radius = Mathf.Clamp(
                    Mathf.Min(bounds.size.x, bounds.size.z) * 0.12f,
                    0.12f,
                    0.35f);
            }
            else
            {
                var collider = root.AddComponent<BoxCollider>();
                collider.center = root.transform.InverseTransformPoint(bounds.center);
                collider.size = bounds.size;
            }

            SavePrefab(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private static GameObject CreateStickPickup(Material wood)
        {
            var root = new GameObject("PRF_Resource_Stick");
            ResourcePickup pickup = root.AddComponent<ResourcePickup>();
            SetString(pickup, "itemId", "resource.stick");
            SetString(pickup, "displayName", "stick");
            SetInt(pickup, "quantity", 1);
            var collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 0.12f, 0f);
            collider.size = new Vector3(0.85f, 0.2f, 0.3f);
            CreateBranchVisual(root.transform, wood, 0.92f, 0.045f);
            SavePrefab(root, ItemPrefabRoot + "PRF_Resource_Stick.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                ItemPrefabRoot + "PRF_Resource_Stick.prefab");
        }

        private static GameObject CreateStonePickup(GameObject rockPrefab)
        {
            var root = new GameObject("PRF_Resource_Stone");
            ResourcePickup pickup = root.AddComponent<ResourcePickup>();
            SetString(pickup, "itemId", "resource.stone");
            SetString(pickup, "displayName", "stone");
            SetInt(pickup, "quantity", 1);
            var collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 0.16f, 0f);
            collider.radius = 0.25f;
            GameObject visual = UnityEngine.Object.Instantiate(rockPrefab, root.transform);
            visual.name = "StoneVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.24f;
            foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(childCollider);
            }

            SavePrefab(root, ItemPrefabRoot + "PRF_Resource_Stone.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                ItemPrefabRoot + "PRF_Resource_Stone.prefab");
        }

        private static GameObject CreateCampfire(
            CampfireRecipeAsset recipe,
            IReadOnlyList<GameObject> rocks,
            MaterialSet materials)
        {
            var root = new GameObject("PRF_CampfireConstructionSite");
            var collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 0.3f, 0f);
            collider.size = new Vector3(2.2f, 0.8f, 2.2f);
            var blueprint = new GameObject("Blueprint");
            blueprint.transform.SetParent(root.transform, false);
            var completed = new GameObject("CompletedCampfire");
            completed.transform.SetParent(root.transform, false);

            CreateRockRing(blueprint.transform, rocks, materials.Blueprint, 8, 0.75f);
            CreateRockRing(completed.transform, rocks, null, 10, 0.72f);
            CreateBranchVisual(completed.transform, materials.Wood, 1.05f, 0.075f,
                new Vector3(0f, 0.22f, 0f), Quaternion.Euler(0f, 45f, 90f));
            CreateBranchVisual(completed.transform, materials.Wood, 1.05f, 0.075f,
                new Vector3(0f, 0.22f, 0f), Quaternion.Euler(0f, -45f, 90f));

            Mesh flameMesh = CreateFlameMesh();
            Transform outer = CreateFlame(
                completed.transform,
                "FlameOuter",
                flameMesh,
                materials.FlameOuter,
                new Vector3(0f, 0.32f, 0f),
                new Vector3(0.62f, 1.25f, 0.62f));
            Transform inner = CreateFlame(
                completed.transform,
                "FlameInner",
                flameMesh,
                materials.FlameInner,
                new Vector3(0.12f, 0.3f, -0.08f),
                new Vector3(0.32f, 0.88f, 0.32f));

            var lightObject = new GameObject("FireLight");
            lightObject.transform.SetParent(completed.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            Light fireLight = lightObject.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.36f, 0.08f);
            fireLight.range = 9f;
            fireLight.intensity = 900f;
            fireLight.shadows = LightShadows.Soft;

            CampfireFlameFlicker flicker = completed.AddComponent<CampfireFlameFlicker>();
            var flickerData = new SerializedObject(flicker);
            flickerData.FindProperty("fireLight").objectReferenceValue = fireLight;
            SerializedProperty layers = flickerData.FindProperty("flameLayers");
            layers.arraySize = 2;
            layers.GetArrayElementAtIndex(0).objectReferenceValue = outer;
            layers.GetArrayElementAtIndex(1).objectReferenceValue = inner;
            flickerData.ApplyModifiedPropertiesWithoutUndo();

            CampfireConstructionSite site =
                root.AddComponent<CampfireConstructionSite>();
            SetReference(site, "recipeAsset", recipe);
            SetReference(site, "blueprintRoot", blueprint);
            SetReference(site, "completedRoot", completed);
            completed.SetActive(false);
            SavePrefab(root, BuildingPrefabRoot + "PRF_CampfireConstructionSite.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                BuildingPrefabRoot + "PRF_CampfireConstructionSite.prefab");
        }

        private static void CreateRockRing(
            Transform parent,
            IReadOnlyList<GameObject> rocks,
            Material overrideMaterial,
            int count,
            float radius)
        {
            for (int index = 0; index < count; index++)
            {
                float angle = index * Mathf.PI * 2f / count;
                GameObject rock = UnityEngine.Object.Instantiate(
                    rocks[index % rocks.Count],
                    parent);
                rock.name = "RingStone_" + index.ToString("00");
                rock.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.12f,
                    Mathf.Sin(angle) * radius);
                rock.transform.localRotation = Quaternion.Euler(
                    0f,
                    -angle * Mathf.Rad2Deg,
                    0f);
                rock.transform.localScale = Vector3.one * 0.28f;
                foreach (Collider childCollider in rock.GetComponentsInChildren<Collider>(true))
                {
                    UnityEngine.Object.DestroyImmediate(childCollider);
                }

                if (overrideMaterial != null)
                {
                    foreach (Renderer renderer in rock.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.sharedMaterial = overrideMaterial;
                    }
                }
            }
        }

        private static void CreateWorld(
            PrefabSet prefabs,
            GameObject stick,
            GameObject stone,
            GameObject campfire,
            Material groundMaterial)
        {
            var root = new GameObject("PRF_ForestCampPlayground");
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "ForestGround";
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            ground.transform.localScale = new Vector3(60f, 0.4f, 60f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            var forest = new GameObject("ForestModels");
            forest.transform.SetParent(root.transform, false);
            Vector3[] treePositions =
            {
                new(-13f, 0f, -10f), new(-7f, 0f, -14f),
                new(0f, 0f, -16f), new(7f, 0f, -14f),
                new(13f, 0f, -10f), new(16f, 0f, -4f),
                new(16f, 0f, 4f), new(13f, 0f, 11f),
                new(7f, 0f, 15f), new(0f, 0f, 17f),
                new(-7f, 0f, 15f), new(-13f, 0f, 11f),
                new(-16f, 0f, 5f), new(-16f, 0f, -3f),
                new(-20f, 0f, -16f), new(-10f, 0f, -23f),
                new(5f, 0f, -24f), new(19f, 0f, -18f),
                new(24f, 0f, -2f), new(22f, 0f, 16f),
                new(8f, 0f, 24f), new(-8f, 0f, 24f),
                new(-22f, 0f, 16f), new(-24f, 0f, -5f),
            };
            for (int index = 0; index < treePositions.Length; index++)
            {
                GameObject tree = UnityEngine.Object.Instantiate(
                    prefabs.Firs[index % prefabs.Firs.Length],
                    forest.transform);
                tree.name = "Fir_" + index.ToString("00");
                tree.transform.localPosition = treePositions[index];
                tree.transform.localRotation = Quaternion.Euler(
                    0f,
                    index * 47f % 360f,
                    0f);
                float scale = 7.5f + index % 5 * 0.85f;
                tree.transform.localScale = Vector3.one * scale;
            }

            PlaceModel(prefabs.Stump, forest.transform, "Stump_A",
                new Vector3(-7f, 0f, 8f), 1.1f, 32f);
            PlaceModel(prefabs.Stump, forest.transform, "Stump_B",
                new Vector3(8f, 0f, -8f), 0.85f, 143f);
            for (int index = 0; index < 10; index++)
            {
                float angle = index * Mathf.PI * 0.71f;
                float radius = 10f + index % 3 * 3.2f;
                PlaceModel(
                    prefabs.Rocks[index % prefabs.Rocks.Length],
                    forest.transform,
                    "ForestRock_" + index.ToString("00"),
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius),
                    0.45f + index % 4 * 0.12f,
                    index * 37f);
            }

            var resources = new GameObject("CollectibleResources");
            resources.transform.SetParent(root.transform, false);
            Vector3[] stickPositions =
            {
                new(-1.2f, 0.08f, -2.5f), new(1.4f, 0.08f, -1.2f),
                new(-2.2f, 0.08f, 1.3f), new(3.1f, 0.08f, 1.8f),
            };
            Vector3[] stonePositions =
            {
                new(-0.8f, 0.05f, -1.1f), new(1.9f, 0.05f, -2.2f),
                new(-2.5f, 0.05f, -0.4f), new(2.8f, 0.05f, 0.2f),
                new(-3.1f, 0.05f, 2.4f), new(3.4f, 0.05f, 2.9f),
            };
            PlaceCollectibles(stick, resources.transform, stickPositions, "Stick");
            PlaceCollectibles(stone, resources.transform, stonePositions, "Stone");

            GameObject site = UnityEngine.Object.Instantiate(campfire, root.transform);
            site.name = "CampfireConstructionSite";
            site.transform.localPosition = new Vector3(0f, 0f, 5f);
            SavePrefab(root, WorldPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void PlaceCollectibles(
            GameObject prefab,
            Transform parent,
            IReadOnlyList<Vector3> positions,
            string label)
        {
            for (int index = 0; index < positions.Count; index++)
            {
                GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);
                instance.name = label + "_" + index.ToString("00");
                instance.transform.localPosition = positions[index];
                instance.transform.localRotation = Quaternion.Euler(
                    index * 11f,
                    index * 61f,
                    index * 7f);
            }
        }

        private static void PlaceModel(
            GameObject prefab,
            Transform parent,
            string name,
            Vector3 position,
            float scale,
            float yaw)
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one * scale;
        }

        private static void UpdatePlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerInventory inventory = root.GetComponent<PlayerInventory>() ??
                                            root.AddComponent<PlayerInventory>();
                Transform oldHud = root.transform.Find("ForestCampHUD");
                if (oldHud != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldHud.gameObject);
                }

                InteractionPromptView view = CreateHud(root.transform);
                PlayerInteractionController controller =
                    root.GetComponent<PlayerInteractionController>() ??
                    root.AddComponent<PlayerInteractionController>();
                Transform interactionOrigin = root.GetComponentsInChildren<Transform>(true)
                    .First(value => value.name == "InteractionOrigin");
                Camera camera = root.GetComponentInChildren<Camera>(true);
                SetReference(controller, "actor", root);
                SetReference(controller, "interactionOrigin", interactionOrigin);
                SetReference(controller, "viewCamera", camera);
                SetReference(controller, "promptView", view);
                SetFloat(controller, "maxDistance", 3.2f);
                SetFloat(controller, "focusRadius", 0.1f);
                EditorUtility.SetDirty(inventory);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static InteractionPromptView CreateHud(Transform parent)
        {
            var hudObject = new GameObject(
                "ForestCampHUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(InteractionPromptView));
            hudObject.transform.SetParent(parent, false);
            Canvas canvas = hudObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = hudObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject promptRoot = new GameObject(
                "InteractionPrompt",
                typeof(RectTransform),
                typeof(Image));
            promptRoot.transform.SetParent(hudObject.transform, false);
            RectTransform promptRect = (RectTransform)promptRoot.transform;
            promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0f, 130f);
            promptRect.sizeDelta = new Vector2(760f, 54f);
            Image promptPanel = promptRoot.GetComponent<Image>();
            promptPanel.color = new Color(0.015f, 0.025f, 0.025f, 0.82f);
            promptPanel.raycastTarget = false;
            Text prompt = CreateText(
                promptRoot.transform,
                "PromptText",
                font,
                24,
                TextAnchor.MiddleCenter,
                Color.white);
            Stretch(prompt.rectTransform, new Vector2(18f, 4f));

            Text inventory = CreateText(
                hudObject.transform,
                "InventorySummary",
                font,
                20,
                TextAnchor.UpperLeft,
                new Color(0.92f, 0.94f, 0.9f, 1f));
            inventory.rectTransform.anchorMin = inventory.rectTransform.anchorMax =
                new Vector2(0f, 1f);
            inventory.rectTransform.pivot = new Vector2(0f, 1f);
            inventory.rectTransform.anchoredPosition = new Vector2(32f, -28f);
            inventory.rectTransform.sizeDelta = new Vector2(520f, 40f);

            Text status = CreateText(
                hudObject.transform,
                "StatusMessage",
                font,
                22,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.76f, 0.35f, 1f));
            status.rectTransform.anchorMin = status.rectTransform.anchorMax =
                new Vector2(0.5f, 0f);
            status.rectTransform.pivot = new Vector2(0.5f, 0f);
            status.rectTransform.anchoredPosition = new Vector2(0f, 195f);
            status.rectTransform.sizeDelta = new Vector2(900f, 42f);

            InteractionPromptView view =
                hudObject.GetComponent<InteractionPromptView>();
            SetReference(view, "promptRoot", promptRoot);
            SetReference(view, "promptText", prompt);
            SetReference(view, "inventoryText", inventory);
            SetReference(view, "statusText", status);
            promptRoot.SetActive(false);
            return view;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = padding;
            rect.offsetMax = -padding;
        }

        private static void IntegrateOfficialScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            FoundationSceneCompositionRoot composition = scene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<FoundationSceneCompositionRoot>(true))
                .Single();
            var serialized = new SerializedObject(composition);
            Transform worldContainer = (Transform)serialized
                .FindProperty("worldContainer").objectReferenceValue;
            GameObject oldWorld = (GameObject)serialized
                .FindProperty("playground").objectReferenceValue;
            if (oldWorld != null)
            {
                UnityEngine.Object.DestroyImmediate(oldWorld);
            }

            GameObject worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath);
            GameObject world = (GameObject)PrefabUtility.InstantiatePrefab(worldPrefab, scene);
            world.transform.SetParent(worldContainer, false);
            world.transform.localPosition = Vector3.zero;
            Collider surface = world.transform.Find("ForestGround").GetComponent<Collider>();
            serialized.Update();
            serialized.FindProperty("playground").objectReferenceValue = world;
            serialized.FindProperty("playableSurface").objectReferenceValue = surface;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateBranchVisual(
            Transform parent,
            Material material,
            float length,
            float radius,
            Vector3? position = null,
            Quaternion? rotation = null)
        {
            GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            branch.name = "NaturalBranch";
            branch.transform.SetParent(parent, false);
            branch.transform.localPosition = position ?? new Vector3(0f, 0.12f, 0f);
            branch.transform.localRotation = rotation ?? Quaternion.Euler(8f, 18f, 90f);
            branch.transform.localScale = new Vector3(radius, length * 0.5f, radius);
            branch.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(branch.GetComponent<Collider>());

            GameObject offshoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            offshoot.name = "BranchOffshoot";
            offshoot.transform.SetParent(branch.transform, false);
            offshoot.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            offshoot.transform.localRotation = Quaternion.Euler(0f, 0f, 42f);
            offshoot.transform.localScale = new Vector3(0.55f, 0.32f, 0.55f);
            offshoot.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(offshoot.GetComponent<Collider>());
        }

        private static Transform CreateFlame(
            Transform parent,
            string name,
            Mesh mesh,
            Material material,
            Vector3 position,
            Vector3 scale)
        {
            var flame = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            flame.transform.SetParent(parent, false);
            flame.transform.localPosition = position;
            flame.transform.localScale = scale;
            flame.GetComponent<MeshFilter>().sharedMesh = mesh;
            flame.GetComponent<MeshRenderer>().sharedMaterial = material;
            return flame.transform;
        }

        private static Mesh CreateFlameMesh()
        {
            const string path =
                "Assets/_Game/Art/Models/World/Generated/MSH_CampfireFlame.asset";
            AssetDatabase.DeleteAsset(path);
            const int sides = 12;
            var vertices = new Vector3[sides + 2];
            var triangles = new int[sides * 6];
            vertices[0] = Vector3.zero;
            vertices[sides + 1] = new Vector3(0f, 1f, 0f);
            for (int index = 0; index < sides; index++)
            {
                float angle = index * Mathf.PI * 2f / sides;
                vertices[index + 1] = new Vector3(
                    Mathf.Cos(angle) * 0.5f,
                    0f,
                    Mathf.Sin(angle) * 0.5f);
                int next = (index + 1) % sides;
                int triangle = index * 6;
                triangles[triangle] = 0;
                triangles[triangle + 1] = next + 1;
                triangles[triangle + 2] = index + 1;
                triangles[triangle + 3] = index + 1;
                triangles[triangle + 4] = next + 1;
                triangles[triangle + 5] = sides + 1;
            }

            var mesh = new Mesh { name = "MSH_CampfireFlame" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            AssetDatabase.DeleteAsset(path);
            if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
            {
                throw new InvalidOperationException($"Could not save prefab: {path}");
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(
                UnityEngine.Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void SetReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(
            UnityEngine.Object target,
            string propertyName,
            float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private enum ColliderKind
        {
            Tree,
            Box,
        }

        private sealed class MaterialSet
        {
            public Material Ground;
            public Material FirBranches;
            public Material FirTwigs;
            public Material Stump;
            public Material Rock;
            public Material Wood;
            public Material Blueprint;
            public Material FlameOuter;
            public Material FlameInner;
        }

        private sealed class PrefabSet
        {
            public GameObject[] Firs;
            public GameObject Stump;
            public GameObject[] Rocks;
        }
    }
}
