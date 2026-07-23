using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SonsOfTheForest.Infrastructure.Editor.ForestLod
{
    /// <summary>
    /// Builds the forest LOD layer without reintroducing flat near-field foliage.
    /// LOD0 is now a game-ready opaque geometry tree under the 60 FPS intake
    /// gate; LOD1-LOD3 use closed proxy geometry for distance rendering.
    /// </summary>
    public static class ForestLodContentBuilder
    {
        private const string MeshRoot = "Assets/_Game/Art/Models/World/Generated/ForestLod/";
        private const string PrefabRoot = "Assets/_Game/Prefabs/World/ForestLod/";
        private const string SourcePrefabRoot = "Assets/_Game/Prefabs/World/ForestFidelity/";
        private const string DataRoot = "Assets/_Game/Data/World/ForestLod/";
        private const string PerformanceVolumeProfilePath =
            "Assets/_Game/Data/World/ForestLod/ForestCampPerformanceVolumeProfile.asset";
        private const string WorldPrefabPath =
            "Assets/_Game/Prefabs/World/ForestCamp/PRF_ForestCampPlayground.prefab";
        private const string BarkMaterialPath =
            "Assets/_Game/Art/Materials/World/ForestFidelity/MAT_ConiferBark.mat";
        private const string CanopyMaterialPath =
            "Assets/_Game/Art/Materials/World/ForestFidelity/MAT_ConiferNeedlesDeep.mat";
        private const string DefaultVolumeProfilePath =
            "Assets/Settings/HDRPDefaultResources/DefaultSettingsVolumeProfile.asset";

        private const int BarkSubMesh = 0;
        private const int CanopySubMesh = 1;

        private static readonly float[] LodThresholds = { 0.40f, 0.16f, 0.05f, 0.005f };
        private static readonly long[] LodMaxTriangles = { 120_000L, 20_000L, 3_000L, 500L };

        private sealed class VariantSpec
        {
            public string Suffix;
            public int Seed;
            public float Height;
        }

        private static readonly VariantSpec[] Variants =
        {
            new VariantSpec { Suffix = "A", Seed = 9101, Height = 16f },
            new VariantSpec { Suffix = "B", Seed = 9202, Height = 13f },
            new VariantSpec { Suffix = "C", Seed = 9303, Height = 18.5f },
        };

        public static void BuildAll()
        {
            var details = new List<string>();
            try
            {
                EnsureFolder(MeshRoot);
                EnsureFolder(PrefabRoot);
                EnsureFolder(DataRoot);

                Material bark = Load<Material>(BarkMaterialPath);
                Material canopy = Load<Material>(CanopyMaterialPath);
                VolumeProfile performanceVolumeProfile = CreateOrUpdatePerformanceVolumeProfile();
                bark.enableInstancing = true;
                canopy.enableInstancing = true;

                var prefabs = new GameObject[Variants.Length];
                for (int index = 0; index < Variants.Length; index++)
                {
                    VariantSpec spec = Variants[index];
                    prefabs[index] = BuildVariantPrefab(spec, bark, canopy, details);
                }

                UpdateWorldPrefab(prefabs, performanceVolumeProfile, details);
                UpdateUnderstoreyPrefabs(details);
                ApplyShadowDistance(100f, details);

                Validate(prefabs);
                AssetDatabase.SaveAssets();
                WriteBuildLog(true, "Game-ready forest LOD pass built successfully.", details);
                Debug.Log(
                    "[ForestLod] Built game-ready LOD forest pass: opaque 3D LOD0 under " +
                    "the 60 FPS intake gate, solid proxy LOD1-LOD3, shared instanced materials, " +
                    "and distance shadow policy.");
            }
            catch (Exception exception)
            {
                WriteBuildLog(false, exception.Message, details);
                throw;
            }
        }

        private static GameObject BuildVariantPrefab(
            VariantSpec spec,
            Material bark,
            Material canopy,
            List<string> details)
        {
            Mesh lod0Mesh = SaveMesh(BuildLod0(spec), MeshName(spec, "LOD0_GameReadyBranchCanopy"));
            Mesh lod1Mesh = SaveMesh(BuildLod1(spec), MeshName(spec, "LOD1_SolidProxy"));
            Mesh lod2Mesh = SaveMesh(BuildLod2(spec), MeshName(spec, "LOD2_SolidProxy"));
            Mesh lod3Mesh = SaveMesh(BuildLod3(spec), MeshName(spec, "LOD3_HorizonProxy"));

            var root = new GameObject("PRF_ConiferLod_" + spec.Suffix);
            try
            {
                Renderer lod0 = AddLodChild(
                    root,
                    "LOD0_GameReadyBranchCanopy",
                    lod0Mesh,
                    new[] { bark, canopy },
                    ShadowCastingMode.On,
                    true);
                Renderer lod1 = AddLodChild(
                    root,
                    "LOD1_SolidBranchCanopy",
                    lod1Mesh,
                    new[] { bark, canopy },
                    ShadowCastingMode.On,
                    true);
                Renderer lod2 = AddLodChild(
                    root,
                    "LOD2_SolidCanopyProxy",
                    lod2Mesh,
                    new[] { bark, canopy },
                    ShadowCastingMode.On,
                    true);
                Renderer lod3 = AddLodChild(
                    root,
                    "LOD3_HorizonProxy",
                    lod3Mesh,
                    new[] { bark, canopy },
                    ShadowCastingMode.Off,
                    false);

                var lodGroup = root.AddComponent<LODGroup>();
                lodGroup.SetLODs(new[]
                {
                    new LOD(LodThresholds[0], new[] { lod0 }),
                    new LOD(LodThresholds[1], new[] { lod1 }),
                    new LOD(LodThresholds[2], new[] { lod2 }),
                    new LOD(LodThresholds[3], new[] { lod3 }),
                });
                lodGroup.fadeMode = LODFadeMode.CrossFade;
                lodGroup.animateCrossFading = true;
                lodGroup.RecalculateBounds();

                Bounds bounds = CalculateBounds(root);
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.direction = 1;
                capsule.center = new Vector3(0f, bounds.size.y * 0.5f, 0f);
                capsule.height = Mathf.Max(2f, bounds.size.y);
                capsule.radius = Mathf.Clamp(bounds.size.y * 0.031f, 0.3f, 0.62f);

                string prefabPath = PrefabRoot + "PRF_ConiferLod_" + spec.Suffix + ".prefab";
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException($"Could not save prefab: {prefabPath}");
                }

                details.Add(
                    $"PRF_ConiferLod_{spec.Suffix}: LOD0 {TriangleCount(lod0Mesh)} tris, " +
                    $"LOD1 {TriangleCount(lod1Mesh)} tris, LOD2 {TriangleCount(lod2Mesh)} tris, " +
                    $"LOD3 {TriangleCount(lod3Mesh)} tris");
                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static string MeshName(VariantSpec spec, string lodLabel)
        {
            return MeshRoot + "MSH_ConiferLod_" + spec.Suffix + "_" + lodLabel + ".asset";
        }

        private static Mesh BuildLod0(VariantSpec spec)
        {
            var random = new System.Random(spec.Seed);
            var accumulator = new MeshAccumulator(2);
            float height = spec.Height;
            accumulator.AddVerticalTube(
                Vector3.zero,
                height * 0.034f,
                0.055f,
                height * 0.98f,
                12,
                8,
                BarkSubMesh);

            for (int whorl = 0; whorl < 14; whorl++)
            {
                float t = Mathf.Lerp(0.12f, 0.94f, whorl / 13f);
                int branches = Mathf.RoundToInt(Mathf.Lerp(11f, 4f, t));
                float branchLength = BranchLength(height, t);
                for (int branch = 0; branch < branches; branch++)
                {
                    float yaw = branch * (360f / branches) + NextSigned(random) * 19f;
                    Vector3 radial = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    Vector3 start = radial * (height * 0.018f) + Vector3.up * (height * t);
                    Vector3 end = start + radial * branchLength;
                    end.y += Mathf.Lerp(-0.42f, 0.16f, t);
                    accumulator.AddTube(
                        start,
                        end,
                        Mathf.Lerp(0.095f, 0.035f, t),
                        Mathf.Lerp(0.035f, 0.012f, t),
                        7,
                        BarkSubMesh);

                    Vector3 crownCenter = Vector3.Lerp(start, end, 0.74f);
                    accumulator.AddEllipsoid(
                        crownCenter,
                        new Vector3(branchLength * 0.30f, height * 0.030f, branchLength * 0.19f),
                        9,
                        5,
                        CanopySubMesh);

                    AddNeedleClusterTriplet(
                        accumulator,
                        random,
                        crownCenter,
                        branchLength,
                        height,
                        radial);
                }
            }

            AddCanopyBands(accumulator, height, 0.62f, 9, 5);
            return accumulator.Build("MSH_ConiferLod_" + spec.Suffix + "_LOD0_GameReadyBranchCanopy");
        }

        private static Mesh BuildLod1(VariantSpec spec)
        {
            var random = new System.Random(spec.Seed + 100);
            var accumulator = new MeshAccumulator(2);
            float height = spec.Height;
            accumulator.AddVerticalTube(
                Vector3.zero,
                height * 0.032f,
                0.065f,
                height * 0.96f,
                8,
                5,
                BarkSubMesh);

            for (int whorl = 0; whorl < 10; whorl++)
            {
                float t = Mathf.Lerp(0.16f, 0.92f, whorl / 9f);
                int branches = Mathf.RoundToInt(Mathf.Lerp(8f, 3f, t));
                float branchLength = BranchLength(height, t);
                for (int branch = 0; branch < branches; branch++)
                {
                    float yaw = branch * (360f / branches) + NextSigned(random) * 18f;
                    Vector3 radial = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    Vector3 start = radial * (height * 0.018f) + Vector3.up * (height * t);
                    Vector3 end = start + radial * branchLength;
                    end.y += Mathf.Lerp(-0.36f, 0.18f, t);
                    accumulator.AddTube(
                        start,
                        end,
                        Mathf.Lerp(0.07f, 0.035f, t),
                        Mathf.Lerp(0.026f, 0.014f, t),
                        5,
                        BarkSubMesh);

                    Vector3 crown = Vector3.Lerp(start, end, 0.76f);
                    accumulator.AddEllipsoid(
                        crown,
                        new Vector3(branchLength * 0.32f, height * 0.035f, branchLength * 0.22f),
                        8,
                        4,
                        CanopySubMesh);
                }
            }

            AddCanopyBands(accumulator, height, 0.72f, 7, 4);
            return accumulator.Build("MSH_ConiferLod_" + spec.Suffix + "_LOD1_SolidProxy");
        }

        private static Mesh BuildLod2(VariantSpec spec)
        {
            var accumulator = new MeshAccumulator(2);
            float height = spec.Height;
            accumulator.AddVerticalTube(
                Vector3.zero,
                height * 0.032f,
                0.075f,
                height * 0.92f,
                6,
                2,
                BarkSubMesh);
            AddCanopyBands(accumulator, height, 0.88f, 6, 3);
            return accumulator.Build("MSH_ConiferLod_" + spec.Suffix + "_LOD2_SolidProxy");
        }

        private static Mesh BuildLod3(VariantSpec spec)
        {
            var accumulator = new MeshAccumulator(2);
            float height = spec.Height;
            accumulator.AddVerticalTube(
                Vector3.zero,
                height * 0.034f,
                0.08f,
                height * 0.52f,
                4,
                1,
                BarkSubMesh);
            accumulator.AddCone(
                Vector3.up * (height * 0.18f),
                BranchLength(height, 0.34f) * 0.95f,
                height * 0.52f,
                5,
                CanopySubMesh);
            accumulator.AddCone(
                Vector3.up * (height * 0.55f),
                BranchLength(height, 0.66f) * 0.72f,
                height * 0.40f,
                5,
                CanopySubMesh);
            return accumulator.Build("MSH_ConiferLod_" + spec.Suffix + "_LOD3_HorizonProxy");
        }

        private static void AddCanopyBands(
            MeshAccumulator accumulator,
            float height,
            float radiusScale,
            int radialSegments,
            int verticalSegments)
        {
            float[] bands = { 0.30f, 0.50f, 0.70f, 0.86f };
            for (int index = 0; index < bands.Length; index++)
            {
                float t = bands[index];
                float width = BranchLength(height, t) * radiusScale;
                accumulator.AddEllipsoid(
                    Vector3.up * (height * t),
                    new Vector3(width, height * Mathf.Lerp(0.055f, 0.035f, t), width * 0.82f),
                    radialSegments,
                    verticalSegments,
                    CanopySubMesh);
            }
        }

        private static float BranchLength(float height, float t)
        {
            return Mathf.Lerp(0.26f, 0.05f, Mathf.Pow(t, 1.15f)) * height;
        }

        private static float NextSigned(System.Random random)
        {
            return (float)(random.NextDouble() * 2d - 1d);
        }

        private static void AddNeedleClusterTriplet(
            MeshAccumulator accumulator,
            System.Random random,
            Vector3 center,
            float branchLength,
            float height,
            Vector3 radial)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, radial).normalized;
            if (tangent.sqrMagnitude < 0.01f)
            {
                tangent = Vector3.right;
            }

            for (int index = 0; index < 3; index++)
            {
                float along = Mathf.Lerp(-0.18f, 0.18f, index / 2f);
                Vector3 offset =
                    radial * (NextSigned(random) * branchLength * 0.06f) +
                    tangent * (along * branchLength) +
                    Vector3.up * (NextSigned(random) * height * 0.012f);
                accumulator.AddEllipsoid(
                    center + offset,
                    new Vector3(
                        branchLength * Mathf.Lerp(0.12f, 0.18f, index / 2f),
                        height * 0.018f,
                        branchLength * 0.08f),
                    7,
                    4,
                    CanopySubMesh);
            }
        }

        private static Renderer AddLodChild(
            GameObject root,
            string name,
            Mesh mesh,
            Material[] materials,
            ShadowCastingMode shadowMode,
            bool receiveShadows)
        {
            if (mesh.subMeshCount != materials.Length)
            {
                throw new InvalidOperationException(
                    $"{name}: submesh count {mesh.subMeshCount} != material count {materials.Length}.");
            }

            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            var filter = child.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = shadowMode;
            renderer.receiveShadows = receiveShadows;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            return renderer;
        }

        private static void UpdateWorldPrefab(
            IReadOnlyList<GameObject> prefabs,
            VolumeProfile performanceVolumeProfile,
            List<string> details)
        {
            GameObject world = PrefabUtility.LoadPrefabContents(WorldPrefabPath);
            try
            {
                Transform canopy = world.transform.Find(
                    "ForestModels/ForestFidelityVegetation/MatureConiferClusters");
                if (canopy == null)
                {
                    throw new InvalidOperationException(
                        "World prefab is missing MatureConiferClusters.");
                }

                Transform[] children = canopy.Cast<Transform>().ToArray();
                var records = children
                    .Select(child => new
                    {
                        child.name,
                        position = child.localPosition,
                        rotation = child.localRotation,
                        scale = child.localScale,
                    })
                    .ToArray();

                foreach (Transform child in children)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }

                for (int index = 0; index < records.Length; index++)
                {
                    GameObject prefab = prefabs[index % prefabs.Count];
                    var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (instance == null)
                    {
                        throw new InvalidOperationException(
                            $"Could not instantiate {prefab.name} into world prefab.");
                    }

                    instance.transform.SetParent(canopy, false);
                    instance.name = records[index].name;
                    instance.transform.localPosition = records[index].position;
                    instance.transform.localRotation = records[index].rotation;
                    instance.transform.localScale = records[index].scale;
                }

                EnsurePerformanceVolume(world, performanceVolumeProfile, details);
                PrefabUtility.SaveAsPrefabAsset(world, WorldPrefabPath);
                details.Add($"World prefab: replaced {records.Length} conifers with LOD prefabs.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(world);
            }
        }

        private static VolumeProfile CreateOrUpdatePerformanceVolumeProfile()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                PerformanceVolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "ForestCampPerformanceVolumeProfile";
                AssetDatabase.CreateAsset(profile, PerformanceVolumeProfilePath);
            }

            profile.components.RemoveAll(component => component == null);
            if (!profile.TryGet(out HDShadowSettings shadows))
            {
                shadows = ScriptableObject.CreateInstance<HDShadowSettings>();
                shadows.name = "HDShadowSettings";
                shadows.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(shadows, profile);
                profile.components.Add(shadows);
            }

            shadows.active = true;
            shadows.maxShadowDistance.overrideState = true;
            shadows.maxShadowDistance.value = 100f;
            EditorUtility.SetDirty(shadows);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void EnsurePerformanceVolume(
            GameObject world,
            VolumeProfile profile,
            List<string> details)
        {
            Transform existing = world.transform.Find("ForestPerformanceVolume");
            GameObject volumeObject;
            if (existing == null)
            {
                volumeObject = new GameObject("ForestPerformanceVolume");
                volumeObject.transform.SetParent(world.transform, false);
            }
            else
            {
                volumeObject = existing.gameObject;
            }

            Volume volume = volumeObject.GetComponent<Volume>();
            if (volume == null)
            {
                volume = volumeObject.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volumeObject);
            details.Add("World prefab: global forest performance volume overrides shadow distance to 100 m.");
        }

        private static void UpdateUnderstoreyPrefabs(List<string> details)
        {
            string[] labels = { "GrassTuft", "Fern" };
            string[] suffixes = { "A", "B", "C" };
            int updated = 0;
            foreach (string label in labels)
            {
                foreach (string suffix in suffixes)
                {
                    string path = SourcePrefabRoot + "PRF_" + label + "_" + suffix + ".prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    {
                        continue;
                    }

                    GameObject contents = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        Renderer[] renderers = contents.GetComponentsInChildren<Renderer>(true);
                        foreach (Renderer renderer in renderers)
                        {
                            renderer.shadowCastingMode = ShadowCastingMode.Off;
                            foreach (Material material in renderer.sharedMaterials)
                            {
                                if (material != null)
                                {
                                    material.enableInstancing = true;
                                    EditorUtility.SetDirty(material);
                                }
                            }
                        }

                        LODGroup lodGroup = contents.GetComponent<LODGroup>();
                        if (lodGroup == null)
                        {
                            lodGroup = contents.AddComponent<LODGroup>();
                        }

                        lodGroup.SetLODs(new[] { new LOD(0.025f, renderers) });
                        lodGroup.fadeMode = LODFadeMode.CrossFade;
                        lodGroup.animateCrossFading = true;
                        lodGroup.RecalculateBounds();
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                        updated++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                }
            }

            details.Add($"Understorey: {updated} prefabs cast no realtime shadows and cull at 2.5% screen size.");
        }

        private static void ApplyShadowDistance(float distance, List<string> details)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DefaultVolumeProfilePath);
            if (profile == null)
            {
                details.Add("Shadow distance: default volume profile not found (skipped).");
                return;
            }

            if (profile.TryGet(out HDShadowSettings shadows))
            {
                shadows.maxShadowDistance.overrideState = true;
                shadows.maxShadowDistance.value = distance;
                EditorUtility.SetDirty(profile);
                details.Add($"Shadow distance set to {distance:F0} m on default volume profile.");
            }
            else
            {
                details.Add("Shadow distance: HDShadowSettings missing on default profile (skipped).");
            }
        }

        private static void Validate(IReadOnlyList<GameObject> prefabs)
        {
            foreach (GameObject prefab in prefabs)
            {
                LODGroup lodGroup = prefab.GetComponent<LODGroup>();
                if (lodGroup == null || lodGroup.lodCount != 4)
                {
                    throw new InvalidOperationException($"{prefab.name}: missing 4-level LODGroup.");
                }

                LOD[] lods = lodGroup.GetLODs();
                for (int index = 0; index < lods.Length; index++)
                {
                    if (Mathf.Abs(lods[index].screenRelativeTransitionHeight - LodThresholds[index]) > 0.011f)
                    {
                        throw new InvalidOperationException(
                            $"{prefab.name}: LOD{index} threshold mismatch.");
                    }

                    long triangles = TriangleCount(lods[index].renderers);
                    if (triangles <= 0 || triangles > LodMaxTriangles[index])
                    {
                        throw new InvalidOperationException(
                            $"{prefab.name}: LOD{index} triangle budget violated ({triangles}).");
                    }
                }

                if (TriangleCount(lods[0].renderers) > 120_000L)
                {
                    throw new InvalidOperationException(
                        $"{prefab.name}: LOD0 exceeds the preferred 60 FPS near-tree budget.");
                }

                if (lods[0].renderers.Any(renderer => renderer.name.Contains("Plane")) ||
                    lods[1].renderers.Any(renderer => renderer.name.Contains("Card")))
                {
                    throw new InvalidOperationException(
                        $"{prefab.name}: near and middle LODs must not use visible cards or planes.");
                }

                if (lods[3].renderers.Any(
                        renderer => renderer.shadowCastingMode != ShadowCastingMode.Off))
                {
                    throw new InvalidOperationException($"{prefab.name}: LOD3 must not cast shadows.");
                }

                if (prefab.GetComponentsInChildren<Collider>(true).Length != 1 ||
                    prefab.GetComponent<CapsuleCollider>() == null)
                {
                    throw new InvalidOperationException(
                        $"{prefab.name}: expected one root capsule collider.");
                }

                Material[] materials = prefab.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Distinct()
                    .ToArray();
                if (materials.Length > 3 ||
                    materials.Any(material => material == null || !material.enableInstancing))
                {
                    throw new InvalidOperationException(
                        $"{prefab.name}: materials must be shared and instanced.");
                }

                if (materials.Any(UsesAlphaCutoutOrDoubleSided))
                {
                    throw new InvalidOperationException(
                        $"{prefab.name}: LOD pass must not introduce alpha-cutout or double-sided foliage.");
                }
            }
        }

        private static bool UsesAlphaCutoutOrDoubleSided(Material material)
        {
            return HasFloatAbove(material, "_AlphaCutoffEnable", 0.5f) ||
                   HasFloatAbove(material, "_DoubleSidedEnable", 0.5f);
        }

        private static bool HasFloatAbove(Material material, string property, float threshold)
        {
            return material.HasProperty(property) && material.GetFloat(property) > threshold;
        }

        private static void EnableInstancingOnMaterials(GameObject prefab)
        {
            foreach (Material material in prefab.GetComponentsInChildren<Renderer>(true)
                         .SelectMany(renderer => renderer.sharedMaterials)
                         .Distinct())
            {
                if (material != null)
                {
                    material.enableInstancing = true;
                    EditorUtility.SetDirty(material);
                }
            }
        }

        private static void RemoveColliders(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static long TriangleCount(IEnumerable<Renderer> renderers)
        {
            return renderers
                .Select(renderer => renderer.GetComponent<MeshFilter>())
                .Where(filter => filter != null && filter.sharedMesh != null)
                .Sum(filter => TriangleCount(filter.sharedMesh));
        }

        private static long TriangleCount(Mesh mesh)
        {
            long indexCount = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                indexCount += (long)mesh.GetIndexCount(subMesh);
            }

            return indexCount / 3L;
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

        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            return existing;
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

        [Serializable]
        private sealed class BuildLog
        {
            public bool success;
            public string message;
            public string createdUtc;
            public List<string> details = new List<string>();
        }

        private static void WriteBuildLog(bool success, string message, List<string> details)
        {
            try
            {
                string directory = Path.GetFullPath(
                    Path.Combine(UnityEngine.Application.dataPath, "..", "Benchmarks"));
                Directory.CreateDirectory(directory);
                var log = new BuildLog
                {
                    success = success,
                    message = message,
                    createdUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    details = details,
                };
                File.WriteAllText(
                    Path.Combine(directory, "forest_lod_build_log.json"),
                    JsonUtility.ToJson(log, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[ForestLod] Could not write build log: " + exception.Message);
            }
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

        private sealed class MeshAccumulator
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int>[] _triangles;

            public MeshAccumulator(int subMeshCount)
            {
                _triangles = new List<int>[subMeshCount];
                for (int index = 0; index < subMeshCount; index++)
                {
                    _triangles[index] = new List<int>();
                }
            }

            public void AddVerticalTube(
                Vector3 basePosition,
                float baseRadius,
                float topRadius,
                float height,
                int radialSegments,
                int heightRings,
                int subMesh)
            {
                var centers = new List<Vector3>();
                var radii = new List<float>();
                for (int ring = 0; ring <= heightRings; ring++)
                {
                    float t = ring / (float)heightRings;
                    centers.Add(basePosition + Vector3.up * (height * t));
                    radii.Add(Mathf.Lerp(baseRadius, topRadius, t));
                }

                AddTube(centers, radii, radialSegments, subMesh);
            }

            public void AddTube(
                Vector3 start,
                Vector3 end,
                float startRadius,
                float endRadius,
                int radialSegments,
                int subMesh)
            {
                AddTube(
                    new List<Vector3> { start, Vector3.Lerp(start, end, 0.5f), end },
                    new List<float> { startRadius, Mathf.Lerp(startRadius, endRadius, 0.5f), endRadius },
                    radialSegments,
                    subMesh);
            }

            private void AddTube(
                IReadOnlyList<Vector3> centers,
                IReadOnlyList<float> radii,
                int radialSegments,
                int subMesh)
            {
                int ringCount = centers.Count;
                var starts = new int[ringCount];
                for (int ring = 0; ring < ringCount; ring++)
                {
                    Vector3 tangent = RingTangent(centers, ring);
                    BuildFrame(tangent, out Vector3 axisA, out Vector3 axisB);
                    starts[ring] = _vertices.Count;
                    for (int segment = 0; segment <= radialSegments; segment++)
                    {
                        float angle = segment / (float)radialSegments * Mathf.PI * 2f;
                        Vector3 offset =
                            (axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle)) * radii[ring];
                        _vertices.Add(centers[ring] + offset);
                        _uvs.Add(new Vector2(segment / (float)radialSegments, ring / (float)(ringCount - 1)));
                    }
                }

                for (int ring = 0; ring < ringCount - 1; ring++)
                {
                    for (int segment = 0; segment < radialSegments; segment++)
                    {
                        int bottomLeft = starts[ring] + segment;
                        int topLeft = starts[ring + 1] + segment;
                        AddTriangle(bottomLeft, topLeft, topLeft + 1, subMesh);
                        AddTriangle(bottomLeft, topLeft + 1, bottomLeft + 1, subMesh);
                    }
                }
            }

            public void AddEllipsoid(
                Vector3 center,
                Vector3 radius,
                int radialSegments,
                int verticalSegments,
                int subMesh)
            {
                int start = _vertices.Count;
                for (int y = 0; y <= verticalSegments; y++)
                {
                    float v = y / (float)verticalSegments;
                    float phi = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, v);
                    float ringScale = Mathf.Cos(phi);
                    float yOffset = Mathf.Sin(phi) * radius.y;
                    for (int x = 0; x <= radialSegments; x++)
                    {
                        float u = x / (float)radialSegments;
                        float theta = u * Mathf.PI * 2f;
                        var point = new Vector3(
                            Mathf.Cos(theta) * radius.x * ringScale,
                            yOffset,
                            Mathf.Sin(theta) * radius.z * ringScale);
                        _vertices.Add(center + point);
                        _uvs.Add(new Vector2(u, v));
                    }
                }

                int stride = radialSegments + 1;
                for (int y = 0; y < verticalSegments; y++)
                {
                    for (int x = 0; x < radialSegments; x++)
                    {
                        int a = start + y * stride + x;
                        int b = start + (y + 1) * stride + x;
                        AddTriangle(a, b, b + 1, subMesh);
                        AddTriangle(a, b + 1, a + 1, subMesh);
                    }
                }
            }

            public void AddCone(
                Vector3 basePosition,
                float radius,
                float height,
                int radialSegments,
                int subMesh)
            {
                int ring = _vertices.Count;
                for (int segment = 0; segment <= radialSegments; segment++)
                {
                    float angle = segment / (float)radialSegments * Mathf.PI * 2f;
                    _vertices.Add(basePosition + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius));
                    _uvs.Add(new Vector2(segment / (float)radialSegments, 0f));
                }

                int apex = _vertices.Count;
                _vertices.Add(basePosition + Vector3.up * height);
                _uvs.Add(new Vector2(0.5f, 1f));
                for (int segment = 0; segment < radialSegments; segment++)
                {
                    AddTriangle(ring + segment, apex, ring + segment + 1, subMesh);
                }
            }

            private static Vector3 RingTangent(IReadOnlyList<Vector3> centers, int ring)
            {
                if (ring == 0)
                {
                    return (centers[1] - centers[0]).normalized;
                }

                if (ring == centers.Count - 1)
                {
                    return (centers[ring] - centers[ring - 1]).normalized;
                }

                return (centers[ring + 1] - centers[ring - 1]).normalized;
            }

            private static void BuildFrame(Vector3 tangent, out Vector3 axisA, out Vector3 axisB)
            {
                Vector3 reference = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.92f
                    ? Vector3.right
                    : Vector3.up;
                axisA = Vector3.Cross(tangent, reference).normalized;
                axisB = Vector3.Cross(tangent, axisA).normalized;
            }

            private void AddTriangle(int a, int b, int c, int subMesh)
            {
                _triangles[subMesh].Add(a);
                _triangles[subMesh].Add(b);
                _triangles[subMesh].Add(c);
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(_vertices);
                mesh.SetUVs(0, _uvs);
                mesh.subMeshCount = _triangles.Length;
                for (int index = 0; index < _triangles.Length; index++)
                {
                    mesh.SetTriangles(_triangles[index], index, true);
                }

                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
