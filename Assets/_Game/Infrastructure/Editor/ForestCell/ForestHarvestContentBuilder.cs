using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Forest;
using SonsOfTheForest.Presentation.ForestCamp;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SonsOfTheForest.Infrastructure.Editor.ForestCell
{
    public static class ForestHarvestContentBuilder
    {
        public const string ManifestPath = "Tools/ForestPipeline/forest-harvest-manifest.json";
        public const string HarvestRoot = "Assets/_Game/Art/World/Forest/Harvest";
        public const string PrefabRoot = "Assets/_Game/Prefabs/World/Forest/Harvest";
        public const string DataRoot = "Assets/_Game/Data/World/Forest/Harvest";
        public const string AxePrefabPath = "Assets/_Game/Prefabs/Items/Tools/PRF_SurvivalAxe.prefab";
        public const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab";

        [MenuItem("Sons Of The Forest/Forest Harvest/Build Production Harvest Loop")]
        public static void BuildAll()
        {
            Manifest manifest = ReadManifest();
            EnsureFolder(HarvestRoot + "/Materials");
            EnsureFolder(PrefabRoot + "/FellingKits");
            EnsureFolder(PrefabRoot + "/Logs");
            EnsureFolder(DataRoot + "/Kits");
            EnsureFolder(Path.GetDirectoryName(AxePrefabPath)?.Replace('\\', '/'));

            var materials = new Dictionary<string, SpeciesMaterials>(StringComparer.Ordinal);
            foreach (string species in new[] { "Pine", "Fir", "Maple" })
            {
                materials.Add(species, BuildSpeciesMaterials(species));
            }

            var logs = new Dictionary<string, GameObject[]>(StringComparer.Ordinal);
            foreach (string species in materials.Keys)
            {
                logs.Add(species, BuildLogPrefabs(species, materials[species]));
            }

            foreach (IGrouping<string, VariantRecord> group in manifest.variants.GroupBy(value => value.speciesId))
            {
                BuildHarvestProfile(group.Key);
            }

            foreach (VariantRecord variant in manifest.variants)
            {
                GameObject visual = BuildFellingVisual(variant, materials[variant.species]);
                BuildFellingKit(variant, visual, logs[variant.species]);
            }

            GameObject axe = BuildAxePrefab();
            ConfigurePlayerPrefab(axe);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ForestCellContentBuilder.BuildAll();
            ValidateClosure(manifest);
            Debug.Log("[ForestHarvest] Built 12 felling kits, 6 logs and the player axe loop.");
        }

        private static Manifest ReadManifest()
        {
            string path = Path.GetFullPath(ManifestPath);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Harvest manifest is missing: " + ManifestPath);
            }

            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
            if (manifest == null || manifest.variants == null || manifest.variants.Length != 12 ||
                manifest.variants.Any(value => string.IsNullOrWhiteSpace(value.speciesId) ||
                                               string.IsNullOrWhiteSpace(value.variantId) ||
                                               string.IsNullOrWhiteSpace(value.runtimeVariantId)))
            {
                throw new InvalidOperationException("Harvest manifest must contain 12 stable variants.");
            }

            return manifest;
        }

        private static SpeciesMaterials BuildSpeciesMaterials(string species)
        {
            Material bark = Load<Material>(ForestTreeArtContentBuilder.CuratedRoot +
                "/Materials/" + species + "/MAT_" + species + "_Bark.mat");
            Material foliage = Load<Material>(ForestTreeArtContentBuilder.CuratedRoot +
                "/Materials/" + species + "/MAT_" + species + "_Foliage.mat");
            Material cut = CreateLitMaterial("MAT_" + species + "_EndGrain",
                species == "Fir" ? new Color(0.54f, 0.36f, 0.18f) :
                species == "Maple" ? new Color(0.62f, 0.43f, 0.24f) :
                new Color(0.68f, 0.48f, 0.25f), 0.22f, HarvestRoot + "/Materials");
            return new SpeciesMaterials(bark, foliage, cut);
        }

        private static ForestHarvestProfile BuildHarvestProfile(string speciesId)
        {
            string label = speciesId.Replace("species.", string.Empty);
            string path = DataRoot + "/HRV_" + label + ".asset";
            ForestHarvestProfile profile = AssetDatabase.LoadAssetAtPath<ForestHarvestProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ForestHarvestProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            float health = speciesId == "species.fir" ? 88f : speciesId == "species.maple" ? 112f : 100f;
            profile.EditorConfigure(health, 3, 0.62f, 19f, 10f, 1.1f, 11f, 30f, 80f);
            if (!profile.TryValidate(out string reason))
            {
                throw new InvalidOperationException("Harvest profile invalid: " + reason);
            }

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static GameObject BuildFellingVisual(VariantRecord variant, SpeciesMaterials materials)
        {
            string sourcePath = HarvestRoot + "/Generated/FellingKits/" + variant.species +
                                "/KIT_" + variant.variantId + ".fbx";
            GameObject source = Load<GameObject>(sourcePath);
            var root = new GameObject("PRF_Felling_" + variant.variantId);
            try
            {
                Transform upper = NewChild(root.transform, "UpperRoot");
                Transform stump = NewChild(root.transform, "StumpRoot");
                Renderer[] upperRenderers = new Renderer[3];
                for (int lod = 0; lod < 3; lod++)
                {
                    upperRenderers[lod] = CloneMeshChild(source.transform, "Upper_LOD" + lod,
                        upper, new[] { materials.Bark, materials.Foliage },
                        lod < 2 ? ShadowCastingMode.On : ShadowCastingMode.Off);
                }

                AddLodGroup(upper.gameObject, new[]
                {
                    new LOD(0.22f, new[] { upperRenderers[0] }),
                    new LOD(0.075f, new[] { upperRenderers[1] }),
                    new LOD(0.012f, new[] { upperRenderers[2] })
                });

                Renderer stump0 = CloneMeshChild(source.transform, "Stump_LOD0", stump,
                    new[] { materials.Bark, materials.Cut }, ShadowCastingMode.On);
                Renderer stump1 = CloneMeshChild(source.transform, "Stump_LOD1", stump,
                    new[] { materials.Bark, materials.Cut }, ShadowCastingMode.Off);
                AddLodGroup(stump.gameObject, new[]
                {
                    new LOD(0.08f, new[] { stump0 }),
                    new LOD(0.015f, new[] { stump1 })
                });

                GameObject upperCap = CloneMeshChild(source.transform, "Upper_CutCap", upper,
                    new[] { materials.Cut }, ShadowCastingMode.Off).gameObject;
                GameObject stumpCap = CloneMeshChild(source.transform, "Stump_CutCap", stump,
                    new[] { materials.Cut }, ShadowCastingMode.Off).gameObject;
                upperCap.SetActive(false);
                stumpCap.SetActive(false);

                GameObject seam = CloneMeshChild(source.transform, "Seam_Intact", root.transform,
                    new[] { materials.Bark, materials.Cut }, ShadowCastingMode.On).gameObject;
                var stages = new GameObject[3];
                for (int index = 0; index < stages.Length; index++)
                {
                    Transform direction = NewChild(root.transform, "NotchDirection_" + (index + 1));
                    CloneMeshChild(source.transform, "Notch_Stage" + (index + 1), direction,
                        new[] { materials.Bark, materials.Cut }, ShadowCastingMode.On);
                    stages[index] = direction.gameObject;
                    stages[index].SetActive(false);
                }

                ForestFellingVisual visual = root.AddComponent<ForestFellingVisual>();
                visual.EditorConfigure(upper, stump, seam, stages, upperCap, stumpCap);
                string prefabPath = PrefabRoot + "/FellingKits/PRF_Felling_" + variant.variantId + ".prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return saved != null ? saved : throw new InvalidOperationException("Could not save " + prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject[] BuildLogPrefabs(string species, SpeciesMaterials materials)
        {
            GameObject source = Load<GameObject>(HarvestRoot + "/Generated/Logs/" + species +
                                                 "/LOGS_" + species + ".fbx");
            var results = new GameObject[2];
            for (int variant = 0; variant < 2; variant++)
            {
                var root = new GameObject("PRF_Log_" + species + "_" + (variant + 1));
                try
                {
                    Renderer lod0 = CloneLogChild(source.transform, "Log" + (variant + 1) + "_LOD0",
                        root.transform, materials);
                    Renderer lod1 = CloneLogChild(source.transform, "Log" + (variant + 1) + "_LOD1",
                        root.transform, materials);
                    AddLodGroup(root, new[]
                    {
                        new LOD(0.10f, new[] { lod0 }),
                        new LOD(0.018f, new[] { lod1 })
                    });
                    Rigidbody body = root.AddComponent<Rigidbody>();
                    body.mass = 30f;
                    body.interpolation = RigidbodyInterpolation.Interpolate;
                    body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    var collider = root.AddComponent<CapsuleCollider>();
                    collider.direction = 2;
                    collider.radius = species == "Fir" ? 0.30f : 0.34f + variant * 0.035f;
                    collider.height = 2.55f + variant * 0.35f;
                    root.AddComponent<ForestHarvestLog>();
                    string path = PrefabRoot + "/Logs/PRF_Log_" + species + "_" + (variant + 1) + ".prefab";
                    results[variant] = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            return results;
        }

        private static Renderer CloneLogChild(
            Transform source, string name, Transform parent, SpeciesMaterials materials)
        {
            Transform sourceChild = FindRequired(source, name);
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localScale = Vector3.one * 100f;
            Mesh mesh = sourceChild.GetComponent<MeshFilter>().sharedMesh;
            child.AddComponent<MeshFilter>().sharedMesh = CopyGeneratedMesh(mesh);
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { materials.Bark, materials.Cut };
            renderer.shadowCastingMode = name.EndsWith("LOD0", StringComparison.Ordinal)
                ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return renderer;
        }

        private static ForestFellingKitDefinition BuildFellingKit(
            VariantRecord variant, GameObject visual, GameObject[] logs)
        {
            string path = DataRoot + "/Kits/KIT_" + variant.runtimeVariantId + ".asset";
            ForestFellingKitDefinition kit =
                AssetDatabase.LoadAssetAtPath<ForestFellingKitDefinition>(path);
            if (kit == null)
            {
                kit = ScriptableObject.CreateInstance<ForestFellingKitDefinition>();
                AssetDatabase.CreateAsset(kit, path);
            }

            kit.EditorConfigure(variant.speciesId, variant.runtimeVariantId, visual, logs,
                variant.cutHeight, variant.trunkRadius, variant.logYield,
                Mathf.Max(3f, variant.targetHeight - variant.cutHeight - 1f));
            if (!kit.TryValidate(out string reason))
            {
                throw new InvalidOperationException("Felling kit invalid: " + reason);
            }

            EditorUtility.SetDirty(kit);
            return kit;
        }

        private static GameObject BuildAxePrefab()
        {
            GameObject source = Load<GameObject>(HarvestRoot + "/Generated/Tools/AXE_Survival.fbx");
            Material handle = CreateLitMaterial("MAT_Axe_Handle", new Color(0.19f, 0.095f, 0.035f),
                0.24f, HarvestRoot + "/Materials");
            Material metal = CreateLitMaterial("MAT_Axe_Metal", new Color(0.16f, 0.18f, 0.19f),
                0.46f, HarvestRoot + "/Materials");
            var root = new GameObject("PRF_SurvivalAxe");
            try
            {
                CloneMeshChild(source.transform, "Axe_Handle", root.transform,
                    new[] { handle }, ShadowCastingMode.On);
                CloneMeshChild(source.transform, "Axe_Head", root.transform,
                    new[] { metal }, ShadowCastingMode.On);
                CloneMeshChild(source.transform, "Axe_Blade", root.transform,
                    new[] { metal }, ShadowCastingMode.On);
                Transform bladeBase = NewChild(root.transform, "BladeBase");
                bladeBase.localPosition = new Vector3(-0.045f, 0.03f, 0.235f);
                Transform bladeTip = NewChild(root.transform, "BladeTip");
                bladeTip.localPosition = new Vector3(0.045f, 0.17f, 0.235f);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, AxePrefabPath);
                return saved != null ? saved : throw new InvalidOperationException("Could not save axe prefab.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigurePlayerPrefab(GameObject axePrefab)
        {
            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform hands = player.transform.Find("ViewRoot/ViewCamera/Hands") ??
                                  player.GetComponentsInChildren<Transform>(true)
                                      .SingleOrDefault(value => value.name == "Hands");
                Camera camera = player.GetComponentInChildren<Camera>(true);
                if (hands == null || camera == null)
                {
                    throw new InvalidOperationException("Player prefab requires Hands and ViewCamera.");
                }

                PlayerAxeHarvestController controller =
                    player.GetComponent<PlayerAxeHarvestController>() ??
                    player.AddComponent<PlayerAxeHarvestController>();
                controller.EditorConfigure(hands, camera, axePrefab, 22f, 0.78f, 0.34f, 0.58f, 0.075f);
                ForestImpactDamageReceiver damageReceiver =
                    player.GetComponent<ForestImpactDamageReceiver>() ??
                    player.AddComponent<ForestImpactDamageReceiver>();
                damageReceiver.EditorConfigure(100f);
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        private static Renderer CloneMeshChild(
            Transform sourceRoot, string name, Transform parent, Material[] materials,
            ShadowCastingMode shadows)
        {
            Transform source = FindRequired(sourceRoot, name);
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null ||
                sourceFilter.sharedMesh.subMeshCount != materials.Length)
            {
                throw new InvalidOperationException("Mesh/material contract mismatch: " + name);
            }

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = source.localPosition;
            child.transform.localRotation = source.localRotation;
            child.transform.localScale = source.localScale;
            child.AddComponent<MeshFilter>().sharedMesh = CopyGeneratedMesh(sourceFilter.sharedMesh);
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = shadows;
            renderer.receiveShadows = true;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            return renderer;
        }

        private static void AddLodGroup(GameObject root, LOD[] lods)
        {
            LODGroup group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = true;
            group.SetLODs(lods);
            group.RecalculateBounds();
        }

        private static Mesh CopyGeneratedMesh(Mesh source)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string group = Path.GetFileNameWithoutExtension(sourcePath);
            string folder = HarvestRoot + "/Meshes/" + group;
            EnsureFolder(folder);
            string destination = folder + "/MSH_" + source.name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(destination);
            Mesh copy = UnityEngine.Object.Instantiate(source);
            copy.name = "MSH_" + source.name;
            if (existing == null)
            {
                AssetDatabase.CreateAsset(copy, destination);
                return copy;
            }

            EditorUtility.CopySerialized(copy, existing);
            UnityEngine.Object.DestroyImmediate(copy);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Material CreateLitMaterial(
            string name, Color color, float smoothness, string folder)
        {
            EnsureFolder(folder);
            string path = folder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("HDRP/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_SurfaceType", 0f);
            material.SetFloat("_AlphaCutoffEnable", 0f);
            material.enableInstancing = true;
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform FindRequired(Transform root, string name)
        {
            Transform value = root.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(candidate => candidate.name == name);
            return value != null ? value : throw new InvalidOperationException("Missing generated node: " + name);
        }

        private static Transform NewChild(Transform parent, string name)
        {
            var value = new GameObject(name);
            value.transform.SetParent(parent, false);
            return value.transform;
        }

        private static void ValidateClosure(Manifest manifest)
        {
            foreach (VariantRecord variant in manifest.variants)
            {
                ForestFellingKitDefinition kit = Load<ForestFellingKitDefinition>(
                    DataRoot + "/Kits/KIT_" + variant.runtimeVariantId + ".asset");
                if (!kit.TryValidate(out string reason) || kit.SpeciesId != variant.speciesId ||
                    kit.VariantId != variant.runtimeVariantId)
                {
                    throw new InvalidOperationException("Kit closure failed: " + reason);
                }
            }

            GameObject player = Load<GameObject>(PlayerPrefabPath);
            if (player.GetComponent<PlayerAxeHarvestController>() == null)
            {
                throw new InvalidOperationException("Player axe controller was not integrated.");
            }
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            return value != null ? value : throw new InvalidOperationException("Missing asset: " + path);
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

        [Serializable]
        private sealed class Manifest
        {
            public VariantRecord[] variants = Array.Empty<VariantRecord>();
        }

        [Serializable]
        private sealed class VariantRecord
        {
            public string speciesId = string.Empty;
            public string species = string.Empty;
            public string variantId = string.Empty;
            public string runtimeVariantId = string.Empty;
            public float cutHeight;
            public float trunkRadius;
            public float targetHeight;
            public int logYield;
        }

        private readonly struct SpeciesMaterials
        {
            public SpeciesMaterials(Material bark, Material foliage, Material cut)
            {
                Bark = bark;
                Foliage = foliage;
                Cut = cut;
            }

            public Material Bark { get; }
            public Material Foliage { get; }
            public Material Cut { get; }
        }
    }
}
