using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SonsOfTheForest.Infrastructure.Editor.ForestCell
{
    public static class ForestTreeArtContentBuilder
    {
        public const string CuratedRoot = "Assets/_Game/Art/World/Forest/Trees/Curated";
        public const string PrefabRoot = CuratedRoot + "/Prefabs";

        private const string PineSource =
            "Assets/pine-trees-pack-lowpoly-game-ready-lods/source/Pine_pack.fbx";
        private const string FirSource =
            "Assets/realistic-fir-trees-pack-lods-gameready/source/Christmass trees pack.fbx";
        private const string MapleSource =
            "Assets/maple-trees-pack-lowpoly-game-ready-lods/source/Acer tree pack.fbx";

        private static readonly VariantSpec[] Variants =
        {
            new("Pine", "PineLarge1", PineSource, "Pine_large_1", 0.80f,
                new Vector3(270.02f, 0f, 0f)),
            new("Pine", "PineLarge2", PineSource, "Pine_large_2", 0.80f,
                new Vector3(270.02f, 0f, 0f)),
            new("Pine", "PineLarge3", PineSource, "Pine_large_3", 0.80f,
                new Vector3(270.02f, 0f, 0f)),
            new("Pine", "PineBig1", PineSource, "Pine_big_1", 1.05f,
                new Vector3(270.02f, 0f, 0f)),
            new("Pine", "PineBig2", PineSource, "Pine_big_2", 1.05f,
                new Vector3(270.02f, 0f, 0f)),
            new("Pine", "PineBig3", PineSource, "Pine_big_3", 1.05f,
                new Vector3(270.02f, 0f, 0f)),
            new("Fir", "FirTall", FirSource, "Christmas tree", 1.60f,
                new Vector3(0f, 180f, 180f), false),
            new("Fir", "FirCompact", FirSource, "Christmas tree_2", 3.40f,
                new Vector3(0f, 180f, 180f), false),
            new("Maple", "MapleLarge1", MapleSource, "Acer_large_1", 0.85f,
                Vector3.zero),
            new("Maple", "MapleLarge2", MapleSource, "Acer_large_2", 0.85f,
                Vector3.zero),
            new("Maple", "MapleLarge3", MapleSource, "Acer_large_3", 0.90f,
                Vector3.zero),
            new("Maple", "MapleMedium1", MapleSource, "Acer_medium_1", 1.20f,
                Vector3.zero)
        };

        public static IReadOnlyList<string> PinePrefabPaths => PrefabPaths("Pine");
        public static IReadOnlyList<string> FirPrefabPaths => PrefabPaths("Fir");
        public static IReadOnlyList<string> MaplePrefabPaths => PrefabPaths("Maple");

        [MenuItem("Sons Of The Forest/Forest Cell/Build Curated Production Tree Art")]
        public static void BuildAll()
        {
            EnsureFolder(CuratedRoot);
            EnsureFolder(CuratedRoot + "/Meshes");
            EnsureFolder(CuratedRoot + "/Textures");
            EnsureFolder(CuratedRoot + "/Materials");
            EnsureFolder(PrefabRoot);

            SpeciesMaterials pine = BuildPineMaterials();
            SpeciesMaterials fir = BuildFirMaterials();
            SpeciesMaterials maple = BuildMapleMaterials();

            foreach (VariantSpec variant in Variants)
            {
                SpeciesMaterials materials = variant.Species == "Pine"
                    ? pine
                    : variant.Species == "Fir" ? fir : maple;
                BuildVariant(variant, materials);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateCuratedClosure();
            Debug.Log("[ForestTreeArt] Built 6 Pine, 2 Fir and 4 Maple project-owned LOD prefabs.");
        }

        private static SpeciesMaterials BuildPineMaterials()
        {
            string root = CuratedRoot + "/Textures/Pine";
            EnsureFolder(root);
            Texture2D barkBase = BakeTexture(
                "Assets/pine-trees-pack-lowpoly-game-ready-lods/textures/Bark_basecolor.png",
                root + "/T_Pine_Bark_Base.png", 2048, false, null);
            Texture2D barkNormal = BakeTexture(
                "Assets/pine-trees-pack-lowpoly-game-ready-lods/textures/Bark_normal.png",
                root + "/T_Pine_Bark_Normal.png", 2048, true, null);
            Texture2D leafBase = BakeTexture(
                "Assets/pine-trees-pack-lowpoly-game-ready-lods/textures/Cluster_full_basecolor.png",
                root + "/T_Pine_Foliage_Base.png", 2048, false,
                "Assets/pine-trees-pack-lowpoly-game-ready-lods/textures/Cluster_full_Opacity.png");
            Texture2D leafNormal = BakeTexture(
                "Assets/pine-trees-pack-lowpoly-game-ready-lods/textures/Cluster_full_normal.png",
                root + "/T_Pine_Foliage_Normal.png", 2048, true, null);
            return new SpeciesMaterials(
                CreateMaterial("Pine", "Bark", barkBase, barkNormal, false),
                CreateMaterial("Pine", "Foliage", leafBase, leafNormal, true));
        }

        private static SpeciesMaterials BuildFirMaterials()
        {
            string root = CuratedRoot + "/Textures/Fir";
            EnsureFolder(root);
            Texture2D barkBase = BakeTexture(
                "Assets/realistic-fir-trees-pack-lods-gameready/textures/BarkAlbedo.png",
                root + "/T_Fir_Bark_Base.png", 2048, false, null);
            Texture2D barkNormal = BakeTexture(
                "Assets/realistic-fir-trees-pack-lods-gameready/textures/Bark_normal.png",
                root + "/T_Fir_Bark_Normal.png", 2048, true, null);
            Texture2D leafBase = BakeTexture(
                "Assets/realistic-fir-trees-pack-lods-gameready/textures/BrunchesAlbedo.png",
                root + "/T_Fir_Foliage_Base.png", 2048, false, null);
            Texture2D leafNormal = BakeTexture(
                "Assets/realistic-fir-trees-pack-lods-gameready/textures/Brunches_normal.png",
                root + "/T_Fir_Foliage_Normal.png", 2048, true, null);
            return new SpeciesMaterials(
                CreateMaterial("Fir", "Bark", barkBase, barkNormal, false),
                CreateMaterial("Fir", "Foliage", leafBase, leafNormal, true));
        }

        private static SpeciesMaterials BuildMapleMaterials()
        {
            string root = CuratedRoot + "/Textures/Maple";
            EnsureFolder(root);
            Texture2D barkBase = BakeTexture(
                "Assets/maple-trees-pack-lowpoly-game-ready-lods/textures/bark_basecolor.png",
                root + "/T_Maple_Bark_Base.png", 2048, false, null);
            Texture2D barkNormal = BakeTexture(
                "Assets/maple-trees-pack-lowpoly-game-ready-lods/textures/bark_normal.png",
                root + "/T_Maple_Bark_Normal.png", 2048, true, null);
            Texture2D leafBase = BakeTexture(
                "Assets/maple-trees-pack-lowpoly-game-ready-lods/textures/Cluster_albedo.png",
                root + "/T_Maple_Foliage_Base.png", 2048, false,
                "Assets/maple-trees-pack-lowpoly-game-ready-lods/textures/Cluster_Opacity.png");
            Texture2D leafNormal = BakeTexture(
                "Assets/maple-trees-pack-lowpoly-game-ready-lods/textures/Cluster_Normal.png",
                root + "/T_Maple_Foliage_Normal.png", 2048, true, null);
            return new SpeciesMaterials(
                CreateMaterial("Maple", "Bark", barkBase, barkNormal, false),
                CreateMaterial("Maple", "Foliage", leafBase, leafNormal, true));
        }

        private static void BuildVariant(VariantSpec spec, SpeciesMaterials materials)
        {
            string meshRoot = CuratedRoot + "/Meshes/" + spec.Species + "/" + spec.Id;
            string prefabFolder = PrefabRoot + "/" + spec.Species;
            EnsureFolder(meshRoot);
            EnsureFolder(prefabFolder);

            Mesh[] meshes = new Mesh[4];
            for (int lod = 0; lod < meshes.Length; lod++)
            {
                string sourceName = spec.SourceNode + (lod == 3 && spec.NestedSource
                    ? "_Billboard_LOD3"
                    : "_LOD" + lod);
                meshes[lod] = CopyMesh(spec.SourcePath, sourceName,
                    meshRoot + "/MSH_" + spec.Id + "_LOD" + lod + ".asset");
            }

            Material billboard = BuildBillboardMaterial(spec);
            var root = new GameObject("PRF_Tree_" + spec.Id);
            try
            {
                var renderers = new Renderer[4];
                for (int lod = 0; lod < 4; lod++)
                {
                    var child = new GameObject("LOD" + lod);
                    child.transform.SetParent(root.transform, false);
                    child.transform.localRotation = Quaternion.Euler(spec.SourceRotation);
                    child.transform.localScale = Vector3.one * (100f * spec.ArtScale);
                    var filter = child.AddComponent<MeshFilter>();
                    filter.sharedMesh = meshes[lod];
                    var renderer = child.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = lod == 3
                        ? new[] { billboard }
                        : new[] { materials.Bark, materials.Foliage };
                    renderer.shadowCastingMode = lod == 3
                        ? ShadowCastingMode.Off
                        : ShadowCastingMode.On;
                    renderer.receiveShadows = lod != 3;
                    renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
                    renderers[lod] = renderer;
                }

                var group = root.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.SetLODs(new[]
                {
                    new LOD(0.25f, new[] { renderers[0] }),
                    new LOD(0.125f, new[] { renderers[1] }),
                    new LOD(0.0625f, new[] { renderers[2] }),
                    new LOD(0.01f, new[] { renderers[3] })
                });
                group.RecalculateBounds();
                string path = prefabFolder + "/PRF_Tree_" + spec.Id + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Material BuildBillboardMaterial(VariantSpec spec)
        {
            string textureFolder = CuratedRoot + "/Textures/" + spec.Species + "/Billboards";
            EnsureFolder(textureFolder);
            string source = BillboardTexturePath(spec);
            Texture2D texture = BakeTexture(source,
                textureFolder + "/T_" + spec.Id + "_Billboard_Base.png", 1024, false, null);
            return CreateMaterial(spec.Species, spec.Id + "_Billboard", texture, null, true);
        }

        private static string BillboardTexturePath(VariantSpec spec)
        {
            if (spec.Species == "Pine")
            {
                return "Assets/pine-trees-pack-lowpoly-game-ready-lods/textures/" +
                       spec.SourceNode + "_Billboard_Color.png";
            }

            if (spec.Species == "Fir")
            {
                string file = spec.SourceNode.Replace(' ', '_');
                return "Assets/realistic-fir-trees-pack-lods-gameready/textures/" +
                       file + "_BillboardAlbedo.png";
            }

            return "Assets/maple-trees-pack-lowpoly-game-ready-lods/textures/" +
                   spec.SourceNode + "_Billboard_Color.png";
        }

        private static Mesh CopyMesh(string sourcePath, string sourceName, string destination)
        {
            Mesh source = AssetDatabase.LoadAllAssetsAtPath(sourcePath)
                .OfType<Mesh>()
                .SingleOrDefault(value => string.Equals(value.name, sourceName, StringComparison.Ordinal));
            if (source == null)
            {
                throw new InvalidOperationException("Missing source mesh: " + sourceName);
            }

            Mesh copy = UnityEngine.Object.Instantiate(source);
            copy.name = Path.GetFileNameWithoutExtension(destination);
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(destination);
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

        private static Texture2D BakeTexture(
            string sourcePath,
            string destination,
            int maximumSize,
            bool normalMap,
            string alphaSourcePath)
        {
            Texture2D source = Load<Texture2D>(sourcePath);
            int width = Mathf.Min(maximumSize, source.width);
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * (width / (float)source.width)));
            Color32[] color = ReadPixels(source, width, height, normalMap);
            if (!string.IsNullOrEmpty(alphaSourcePath))
            {
                Color32[] alpha = ReadPixels(Load<Texture2D>(alphaSourcePath), width, height, true);
                for (int index = 0; index < color.Length; index++)
                {
                    color[index].a = alpha[index].r;
                }
            }

            var output = new Texture2D(width, height, TextureFormat.RGBA32, false, normalMap);
            output.SetPixels32(color);
            output.Apply(false, false);
            string fullPath = Path.GetFullPath(destination);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);
            File.WriteAllBytes(fullPath, output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normalMap;
            importer.alphaIsTransparency = !normalMap;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.maxTextureSize = maximumSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return Load<Texture2D>(destination);
        }

        private static Color32[] ReadPixels(Texture2D source, int width, int height, bool linear)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.ARGB32,
                linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                var readable = new Texture2D(width, height, TextureFormat.RGBA32, false, linear);
                readable.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                readable.Apply(false, false);
                Color32[] pixels = readable.GetPixels32();
                UnityEngine.Object.DestroyImmediate(readable);
                return pixels;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static Material CreateMaterial(
            string species,
            string label,
            Texture2D baseMap,
            Texture2D normalMap,
            bool alphaClipped)
        {
            string folder = CuratedRoot + "/Materials/" + species;
            EnsureFolder(folder);
            string path = folder + "/MAT_" + species + "_" + label + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("HDRP/Lit"));
                material.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseColorMap", baseMap);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_NormalMap", normalMap);
            material.SetFloat("_NormalScale", normalMap != null ? 1f : 0f);
            material.SetFloat("_SurfaceType", 0f);
            material.SetFloat("_AlphaCutoffEnable", alphaClipped ? 1f : 0f);
            material.SetFloat("_AlphaCutoff", alphaClipped ? 0.36f : 0.5f);
            material.SetFloat("_DoubleSidedEnable", alphaClipped ? 1f : 0f);
            material.SetFloat("_DoubleSidedNormalMode", alphaClipped ? 1f : 0f);
            material.SetFloat("_Smoothness", alphaClipped ? 0.18f : 0.28f);
            material.enableInstancing = true;
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ValidateCuratedClosure()
        {
            foreach (string path in Variants.Select(PrefabPath))
            {
                GameObject prefab = Load<GameObject>(path);
                LODGroup group = prefab.GetComponent<LODGroup>();
                if (group == null || group.lodCount != 4 ||
                    prefab.GetComponentsInChildren<Collider>(true).Length != 0 ||
                    prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                {
                    throw new InvalidOperationException("Curated prefab closure failed: " + path);
                }

                if (group.GetLODs()[3].renderers.Any(value =>
                        value.shadowCastingMode != ShadowCastingMode.Off))
                {
                    throw new InvalidOperationException("Final LOD must not cast shadows: " + path);
                }

                string[] dependencies = AssetDatabase.GetDependencies(path, true);
                if (dependencies.Any(value => value.StartsWith(
                        "Assets/pine-trees-pack", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("Assets/realistic-fir", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("Assets/maple-trees-pack", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Raw-pack dependency escaped curation: " + path);
                }
            }
        }

        private static IReadOnlyList<string> PrefabPaths(string species) => Variants
            .Where(value => value.Species == species)
            .Select(PrefabPath)
            .ToArray();

        private static string PrefabPath(VariantSpec value) =>
            PrefabRoot + "/" + value.Species + "/PRF_Tree_" + value.Id + ".prefab";

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            return value != null ? value : throw new InvalidOperationException("Missing asset: " + path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private readonly struct SpeciesMaterials
        {
            public SpeciesMaterials(Material bark, Material foliage)
            {
                Bark = bark;
                Foliage = foliage;
            }

            public Material Bark { get; }
            public Material Foliage { get; }
        }

        private sealed class VariantSpec
        {
            public VariantSpec(
                string species,
                string id,
                string sourcePath,
                string sourceNode,
                float artScale,
                Vector3 sourceRotation,
                bool nestedSource = true)
            {
                Species = species;
                Id = id;
                SourcePath = sourcePath;
                SourceNode = sourceNode;
                ArtScale = artScale;
                SourceRotation = sourceRotation;
                NestedSource = nestedSource;
            }

            public string Species { get; }
            public string Id { get; }
            public string SourcePath { get; }
            public string SourceNode { get; }
            public float ArtScale { get; }
            public Vector3 SourceRotation { get; }
            public bool NestedSource { get; }
        }
    }
}
