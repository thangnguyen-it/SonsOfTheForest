using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SonsOfTheForest.Infrastructure.Editor.ForestModelIntake
{
    /// <summary>
    /// Validates imported forest model candidates before they can be promoted from
    /// a visual trial into the playable forest. This is intentionally stricter
    /// than "does it look good in the Scene view": the project now targets a
    /// stable 60 FPS playable forest, so raw photogrammetry-scale trees are
    /// treated as source/reference material until they have authored LODs,
    /// sensible shadow policy, shared materials, and benchmark approval.
    /// </summary>
    public static class ForestModelIntakeValidator
    {
        public const long NearHeroLod0TriangleBudget = 120_000L;
        public const long NearExceptionalHardLimit = 200_000L;
        public const long MidLod1TriangleBudget = 20_000L;
        public const long FarLod2TriangleBudget = 3_000L;
        public const long DistantFinalLodTriangleBudget = 500L;
        public const int PreferredMaterialLimit = 4;

        private const string ReportDirectory = "Benchmarks";
        private const string JsonReportName = "forest_model_intake_report.json";
        private const string MarkdownReportName = "forest_model_intake_report.md";

        [Serializable]
        public sealed class IntakeReport
        {
            public string createdUtc;
            public List<CandidateReport> candidates = new List<CandidateReport>();
        }

        [Serializable]
        public sealed class CandidateReport
        {
            public string name;
            public string assetPath;
            public int rendererCount;
            public int meshFilterCount;
            public int materialCount;
            public long totalTriangles;
            public bool hasLodGroup;
            public bool passesHardGate;
            public List<LodReport> lods = new List<LodReport>();
            public List<string> errors = new List<string>();
            public List<string> warnings = new List<string>();
        }

        [Serializable]
        public sealed class LodReport
        {
            public string name;
            public float threshold;
            public int rendererCount;
            public long triangles;
            public bool castsRealtimeShadows;
        }

        [MenuItem("Sons Of The Forest/Forest Models/Validate Selected Model Candidates", true)]
        private static bool CanValidateSelection()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        [MenuItem("Sons Of The Forest/Forest Models/Validate Selected Model Candidates")]
        public static void ValidateSelected()
        {
            IntakeReport report = BuildReport(Selection.gameObjects);
            foreach (CandidateReport candidate in report.candidates)
            {
                LogCandidate(candidate);
            }
        }

        [MenuItem("Sons Of The Forest/Forest Models/Write Selected Model Candidate Report", true)]
        private static bool CanWriteSelectionReport()
        {
            return CanValidateSelection();
        }

        [MenuItem("Sons Of The Forest/Forest Models/Write Selected Model Candidate Report")]
        public static void WriteSelectedReport()
        {
            IntakeReport report = BuildReport(Selection.gameObjects);
            string directory = Path.GetFullPath(
                Path.Combine(UnityEngine.Application.dataPath, "..", ReportDirectory));
            Directory.CreateDirectory(directory);

            string jsonPath = Path.Combine(directory, JsonReportName);
            string markdownPath = Path.Combine(directory, MarkdownReportName);
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(markdownPath, ToMarkdown(report));

            Debug.Log(
                "[ForestModelIntake] Wrote model intake reports: " +
                $"{jsonPath} and {markdownPath}");
            foreach (CandidateReport candidate in report.candidates)
            {
                LogCandidate(candidate);
            }
        }

        public static IntakeReport BuildReport(IEnumerable<GameObject> candidates)
        {
            var report = new IntakeReport
            {
                createdUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            };

            foreach (GameObject candidate in candidates.Where(value => value != null))
            {
                report.candidates.Add(Analyze(candidate));
            }

            return report;
        }

        private static CandidateReport Analyze(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(value => value != null)
                .ToArray();
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true)
                .Where(value => value != null && value.sharedMesh != null)
                .ToArray();
            Material[] materials = renderers
                .SelectMany(value => value.sharedMaterials)
                .Where(value => value != null)
                .Distinct()
                .ToArray();

            var candidate = new CandidateReport
            {
                name = root.name,
                assetPath = AssetDatabase.GetAssetPath(root),
                rendererCount = renderers.Length,
                meshFilterCount = filters.Length,
                materialCount = materials.Length,
                totalTriangles = filters.Sum(value => TriangleCount(value.sharedMesh)),
            };

            LODGroup lodGroup = root.GetComponentInChildren<LODGroup>(true);
            candidate.hasLodGroup = lodGroup != null;
            if (lodGroup == null)
            {
                candidate.errors.Add("Missing LODGroup. Game-ready forest trees must ship with authored LODs.");
                candidate.lods.Add(new LodReport
                {
                    name = "Whole asset",
                    threshold = 1f,
                    rendererCount = renderers.Length,
                    triangles = candidate.totalTriangles,
                    castsRealtimeShadows = renderers.Any(CastsRealtimeShadows),
                });
            }
            else
            {
                LOD[] lods = lodGroup.GetLODs();
                for (int index = 0; index < lods.Length; index++)
                {
                    Renderer[] lodRenderers = lods[index].renderers
                        .Where(value => value != null)
                        .ToArray();
                    candidate.lods.Add(new LodReport
                    {
                        name = "LOD" + index.ToString(CultureInfo.InvariantCulture),
                        threshold = lods[index].screenRelativeTransitionHeight,
                        rendererCount = lodRenderers.Length,
                        triangles = TriangleCount(lodRenderers),
                        castsRealtimeShadows = lodRenderers.Any(CastsRealtimeShadows),
                    });
                }

                ValidateLodChain(candidate);
            }

            ValidateMaterials(candidate, materials);
            ValidateRendererShape(candidate, renderers);
            candidate.passesHardGate = candidate.errors.Count == 0;
            return candidate;
        }

        private static void ValidateLodChain(CandidateReport candidate)
        {
            if (candidate.lods.Count < 3)
            {
                candidate.errors.Add("LODGroup has fewer than 3 LOD levels.");
                return;
            }

            long lod0Triangles = candidate.lods[0].triangles;
            if (lod0Triangles > NearExceptionalHardLimit)
            {
                candidate.errors.Add(
                    $"LOD0 has {lod0Triangles:N0} triangles; hard limit is " +
                    $"{NearExceptionalHardLimit:N0}. Treat it as source/reference until decimated.");
            }
            else if (lod0Triangles > NearHeroLod0TriangleBudget)
            {
                candidate.warnings.Add(
                    $"LOD0 has {lod0Triangles:N0} triangles; above the preferred " +
                    $"{NearHeroLod0TriangleBudget:N0} budget and must be benchmark-approved.");
            }

            if (candidate.lods.Count > 1 && candidate.lods[1].triangles > MidLod1TriangleBudget)
            {
                candidate.errors.Add(
                    $"LOD1 has {candidate.lods[1].triangles:N0} triangles; budget is " +
                    $"{MidLod1TriangleBudget:N0}.");
            }

            if (candidate.lods.Count > 2 && candidate.lods[2].triangles > FarLod2TriangleBudget)
            {
                candidate.errors.Add(
                    $"LOD2 has {candidate.lods[2].triangles:N0} triangles; budget is " +
                    $"{FarLod2TriangleBudget:N0}.");
            }

            LodReport last = candidate.lods[candidate.lods.Count - 1];
            if (last.triangles > DistantFinalLodTriangleBudget)
            {
                candidate.errors.Add(
                    $"Final distant LOD has {last.triangles:N0} triangles; budget is " +
                    $"{DistantFinalLodTriangleBudget:N0}.");
            }

            if (last.castsRealtimeShadows)
            {
                candidate.errors.Add("Final distant LOD must not cast realtime shadows.");
            }

            for (int index = 1; index < candidate.lods.Count; index++)
            {
                if (candidate.lods[index].threshold >= candidate.lods[index - 1].threshold)
                {
                    candidate.errors.Add("LOD thresholds must strictly decrease.");
                    break;
                }
            }
        }

        private static void ValidateMaterials(CandidateReport candidate, IReadOnlyCollection<Material> materials)
        {
            if (materials.Count == 0)
            {
                candidate.errors.Add("No materials found.");
                return;
            }

            if (materials.Count > PreferredMaterialLimit)
            {
                candidate.warnings.Add(
                    $"{materials.Count} materials found; prefer {PreferredMaterialLimit} or fewer shared materials.");
            }

            foreach (Material material in materials)
            {
                if (FloatAbove(material, "_SurfaceType", 0.5f))
                {
                    candidate.errors.Add(
                        $"{material.name} is Transparent. Forest foliage should use Opaque + alpha clip if needed.");
                }

                if (!material.enableInstancing)
                {
                    candidate.warnings.Add($"{material.name} does not have GPU instancing enabled.");
                }

                if (FloatAbove(material, "_AlphaCutoffEnable", 0.5f))
                {
                    candidate.warnings.Add(
                        $"{material.name} uses alpha clipping; inspect near-field cards for visible flat sheets.");
                }

                if (FloatAbove(material, "_DoubleSidedEnable", 0.5f))
                {
                    candidate.warnings.Add(
                        $"{material.name} is double-sided; verify normals/lighting do not show white or black card backs.");
                }
            }
        }

        private static void ValidateRendererShape(CandidateReport candidate, IEnumerable<Renderer> renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                string name = renderer.name;
                if (name.IndexOf("plane", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("card", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    candidate.warnings.Add(
                        $"{renderer.name} looks like card/plane foliage by name; inspect close-up readability.");
                }
            }
        }

        private static bool CastsRealtimeShadows(Renderer renderer)
        {
            return renderer.shadowCastingMode != ShadowCastingMode.Off;
        }

        private static bool FloatAbove(Material material, string property, float threshold)
        {
            return material.HasProperty(property) && material.GetFloat(property) > threshold;
        }

        private static long TriangleCount(IEnumerable<Renderer> renderers)
        {
            return renderers
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

        private static void LogCandidate(CandidateReport candidate)
        {
            string status = candidate.passesHardGate ? "PASS" : "REJECT";
            string message =
                $"[ForestModelIntake] {status}: {candidate.name} — " +
                $"{candidate.totalTriangles:N0} tris, {candidate.rendererCount} renderers, " +
                $"{candidate.materialCount} materials, {candidate.lods.Count} LOD entries.";

            if (candidate.errors.Count > 0)
            {
                Debug.LogError(message + "\n" + string.Join("\n", candidate.errors));
            }
            else if (candidate.warnings.Count > 0)
            {
                Debug.LogWarning(message + "\n" + string.Join("\n", candidate.warnings));
            }
            else
            {
                Debug.Log(message);
            }
        }

        private static string ToMarkdown(IntakeReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Forest model intake report");
            builder.AppendLine();
            builder.AppendLine("- Created UTC: " + report.createdUtc);
            builder.AppendLine(
                "- Hard gate: LOD0 <= 200k triangles, LOD1 <= 20k, LOD2 <= 3k, final LOD <= 500 and no realtime distant shadow.");
            builder.AppendLine();
            builder.AppendLine("| Candidate | Result | Tris | Renderers | Materials | LODs | Errors | Warnings |");
            builder.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");
            foreach (CandidateReport candidate in report.candidates)
            {
                builder.AppendLine(
                    $"| {candidate.name} | {(candidate.passesHardGate ? "PASS" : "REJECT")} | " +
                    $"{candidate.totalTriangles} | {candidate.rendererCount} | {candidate.materialCount} | " +
                    $"{candidate.lods.Count} | {candidate.errors.Count} | {candidate.warnings.Count} |");
            }

            return builder.ToString();
        }
    }
}
