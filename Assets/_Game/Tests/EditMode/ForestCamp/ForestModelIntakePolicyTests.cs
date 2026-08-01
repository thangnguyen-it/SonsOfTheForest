using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SonsOfTheForest.Tests.ForestCamp.EditMode
{
    public sealed class ForestModelIntakePolicyTests
    {
        private const string CatalogPath =
            "Assets/_Docs/Research/R2_TREE_MODEL_SOURCE_CATALOG.md";
        private const string IntakeGatePath =
            "Assets/_Docs/Research/R2_FOREST_MODEL_INTAKE_GATE.md";
        private const string WorldPath =
            "Assets/_Game/Prefabs/World/ForestCamp/PRF_ForestCampPlayground.prefab";
        private const string MayoTrialBuilderPath =
            "Assets/_Game/Infrastructure/Editor/ForestModelIntake/MayoPineForestTrialBuilder.cs";
        private const string Phase1SpecPath =
            "Assets/_Docs/Research/R2_SOTF_FOREST_PHASE1_SPEC.md";
        private const string AttributionPath =
            "Assets/_Docs/ThirdParty/FOREST_MODEL_ATTRIBUTION.md";
        private const string Phase1TrialBuilderPath =
            "Assets/_Game/Infrastructure/Editor/ForestModelIntake/SotfForestPhase1TrialBuilder.cs";
        private const string ProductionForestRendererPath =
            "Assets/_Game/Infrastructure/Benchmark/ProductionForestRenderer.cs";
        private const string ProductionForestPolicyPath =
            "Assets/_Game/Infrastructure/Benchmark/ProductionForestPerformancePolicy.cs";
        private const string ForestBenchmarkRunnerPath =
            "Assets/_Game/Infrastructure/Benchmark/ForestBenchmarkRunner.cs";
        private const string ProductionForestDocPath =
            "Assets/_Docs/Research/R2_PRODUCTION_FOREST_RENDERER.md";
        private const string RuntimePerformanceSamplerPath =
            "Assets/_Game/Infrastructure/Benchmark/RuntimePerformanceSampler.cs";
        private const string PerformanceBaselineDocPath =
            "Assets/_Docs/Research/R2_PERF0_PRODUCTION_PERFORMANCE_BASELINE.md";

        private const long PreferredNearHeroLod0Budget = 120_000L;
        private const long HardNearExceptionalLimit = 200_000L;
        private const long RawMantissaMapleTrialTriangles = 2_417_961L;
        private const long RawPolyHavenAdultFirTrialTriangles = 6_982_937L;

        [Test]
        public void SourceCatalog_RecordsStableSixtyFpsAsModelAcceptanceGate()
        {
            string catalog = File.ReadAllText(CatalogPath);

            Assert.That(catalog, Does.Contain("Non-negotiable 60 FPS gate"));
            Assert.That(catalog, Does.Contain("stable minimum of 60 FPS"));
            Assert.That(catalog, Does.Contain("Near hero tree LOD0 | 30k-120k tris"));
            Assert.That(catalog, Does.Contain("Near exceptional tree | up to 200k tris"));
            Assert.That(catalog, Does.Contain("cluster benchmark: at least 10 near, 50 mid, and 300 far/impostor trees"));
        }

        [Test]
        public void SourceCatalog_RejectsRawMantissaMapleAsGameplayPrefab()
        {
            string catalog = File.ReadAllText(CatalogPath);

            Assert.That(catalog, Does.Contain("Unity raw import measurement for Maple 004"));
            Assert.That(catalog, Does.Contain("2,417,961 triangles"));
            Assert.That(catalog, Does.Contain("reject raw Maple 004 as direct gameplay forest content"));
            Assert.That(catalog, Does.Contain("raw extracted Maple files were removed from `Assets/`"));
        }

        [Test]
        public void SourceCatalog_RejectsRawPolyHavenAdultFirAsGameplayPrefab()
        {
            string catalog = File.ReadAllText(CatalogPath);

            Assert.That(catalog, Does.Contain("`fir_tree_01` adult conifer inspection"));
            Assert.That(catalog, Does.Contain("6,982,937 triangles"));
            Assert.That(catalog, Does.Contain("reject raw import as direct gameplay content"));
            Assert.That(catalog, Does.Contain("temporary raw import was removed from `Assets/`"));
        }

        [Test]
        public void SourceCatalog_RecordsMayoPineForestAsPrototypeCandidateOnly()
        {
            string catalog = File.ReadAllText(CatalogPath);
            string intakeGate = File.ReadAllText(IntakeGatePath);

            Assert.That(catalog, Does.Contain("Unity Asset Store local trial - Mayo Games Pine forest set Free sample"));
            Assert.That(catalog, Does.Contain("`PRF_TRIAL_furTree_SOTFWrapper` | yes | 1,334"));
            Assert.That(catalog, Does.Contain("static 60 FPS budget prototyping"));
            Assert.That(catalog, Does.Contain("not the final Sons-of-the-Forest-like forest art target"));
            Assert.That(intakeGate, Does.Contain("Mayo Games Pine forest free sample local trial"));
            Assert.That(intakeGate, Does.Contain("PASS as a static-geometry prototype candidate"));
            Assert.That(intakeGate, Does.Contain("FAIL as a runtime 60 FPS promotion candidate"));
            Assert.That(intakeGate, Does.Contain("HOLD as final forest art"));
        }

        [Test]
        public void MayoLocalTrialWorkflow_RemainsLocalAndBenchmarkFocused()
        {
            string builder = File.ReadAllText(MayoTrialBuilderPath);
            string catalog = File.ReadAllText(CatalogPath);
            string intakeGate = File.ReadAllText(IntakeGatePath);

            Assert.That(builder, Does.Contain("Pine forest set [Free sample]"));
            Assert.That(builder, Does.Contain("Assets/pineForset_MayoGames_free"));
            Assert.That(builder, Does.Contain("SOTF_LocalWrappers"));
            Assert.That(builder, Does.Contain("Build 60 FPS Benchmark Scene"));
            Assert.That(builder, Does.Contain("Run Runtime FPS Benchmark"));
            Assert.That(builder, Does.Contain("mayo_pine_local"));
            Assert.That(builder, Does.Contain("Write Intake Report"));
            Assert.That(builder, Does.Contain("ShadowCastingMode.Off"));
            Assert.That(
                catalog,
                Does.Contain("Run Runtime FPS Benchmark"),
                "The catalog must document the local benchmark workflow.");
            Assert.That(
                intakeGate,
                Does.Contain("None of those generated package-derived assets are public-repository deliverables"),
                "The intake gate must keep generated Asset Store wrappers out of public Git.");
            Assert.That(intakeGate, Does.Contain("MayoBenchmark_Far_300"));
            Assert.That(intakeGate, Does.Contain("Runtime FPS promotion gate"));
        }

        [Test]
        public void Phase1ForestSpec_ConvertsUserResearchIntoImplementationTargets()
        {
            string spec = File.ReadAllText(Phase1SpecPath);

            Assert.That(spec, Does.Contain("Mature conifers often have long bare boles"));
            Assert.That(spec, Does.Contain("Primary conifer skeleton"));
            Assert.That(spec, Does.Contain("pine-trees-pack-lowpoly-game-ready-lods"));
            Assert.That(spec, Does.Contain("realistic-fir-trees-pack-lods-gameready"));
            Assert.That(spec, Does.Contain("five-birch-trees-pack-lowpoly-lods"));
            Assert.That(spec, Does.Contain("maple-trees-pack-lowpoly-game-ready-lods"));
            Assert.That(spec, Does.Contain("24-35 m"));
            Assert.That(spec, Does.Contain("45-65%"));
            Assert.That(spec, Does.Contain("2-4 m"));
            Assert.That(spec, Does.Contain("10 near, 50 mid, and 300 far trees"));
            Assert.That(spec, Does.Contain("300-tree far benchmark cluster may use only candidates that pass the hard intake gate"));
            Assert.That(spec, Does.Contain("GPU-instanced distant/proxy cloud"));
        }

        [Test]
        public void ThirdPartyForestModelAttribution_RecordsLocalSourceLicenses()
        {
            string attribution = File.ReadAllText(AttributionPath);

            Assert.That(attribution, Does.Contain("LOLIPOP"));
            Assert.That(attribution, Does.Contain("CC Attribution"));
            Assert.That(attribution, Does.Contain("five-birch-trees-pack-lowpoly-lods-08fe5117138e4fdaa7ca440ef1201e07"));
            Assert.That(attribution, Does.Contain("maple-trees-pack-lowpoly-game-ready-lods-b5d2833c258f4054a01ee2b4ef85adf0"));
            Assert.That(attribution, Does.Contain("pine-trees-pack-lowpoly-game-ready-lods-e1e9c07b8e2e445c943fec660beefba2"));
            Assert.That(attribution, Does.Contain("realistic-fir-trees-pack-lods-gameready-f58e8b6d733e4b0586e5b7db847b89e7"));
            Assert.That(attribution, Does.Contain("Local/ignored"));
        }

        [Test]
        public void Phase1LocalTrialWorkflow_StaysIgnoredAndRequiresRuntimeBenchmark()
        {
            string gitIgnore = File.ReadAllText(".gitignore");
            string builder = File.ReadAllText(Phase1TrialBuilderPath);
            string spec = File.ReadAllText(Phase1SpecPath);

            Assert.That(gitIgnore, Does.Contain("/[Aa]ssets/five-birch-trees-pack-lowpoly-lods/"));
            Assert.That(gitIgnore, Does.Contain("/[Aa]ssets/maple-trees-pack-lowpoly-game-ready-lods/"));
            Assert.That(gitIgnore, Does.Contain("/[Aa]ssets/pine-trees-pack-lowpoly-game-ready-lods/"));
            Assert.That(gitIgnore, Does.Contain("/[Aa]ssets/realistic-fir-trees-pack-lods-gameready/"));
            Assert.That(gitIgnore, Does.Contain("/[Aa]ssets/_LocalTrials/"));
            Assert.That(builder, Does.Contain("SOTF Phase 1 Local Trial/Build Source Wrappers"));
            Assert.That(builder, Does.Contain("SOTF Phase 1 Local Trial/Build Visual Benchmark Scene"));
            Assert.That(builder, Does.Contain("SOTF Phase 1 Local Trial/Run Runtime FPS Benchmark"));
            Assert.That(builder, Does.Contain("Build Standalone Benchmark Player"));
            Assert.That(builder, Does.Contain("Build And Run Standalone Benchmark Player"));
            Assert.That(builder, Does.Contain("sotf_forest_phase1_local"));
            Assert.That(builder, Does.Contain("AcceptedWrapperPaths"));
            Assert.That(builder, Does.Contain("MatureConiferPhase1_Far_300"));
            Assert.That(builder, Does.Contain("PlaceInstancedFarCluster"));
            Assert.That(builder, Does.Contain("ProductionForestRenderer"));
            Assert.That(builder, Does.Contain("passesHardGate"));
            Assert.That(builder, Does.Contain("ShadowCastingMode.Off"));
            Assert.That(spec, Does.Contain("Runtime or standalone benchmark must be recorded"));
        }

        [Test]
        public void ProductionForestRenderer_UsesInstancedFarForestPolicy()
        {
            string renderer = File.ReadAllText(ProductionForestRendererPath);
            string policy = File.ReadAllText(ProductionForestPolicyPath);
            string runner = File.ReadAllText(ForestBenchmarkRunnerPath);
            string sampler = File.ReadAllText(RuntimePerformanceSamplerPath);
            string builder = File.ReadAllText(Phase1TrialBuilderPath);
            string doc = File.ReadAllText(ProductionForestDocPath);

            Assert.That(renderer, Does.Contain("Graphics.RenderMeshInstanced"));
            Assert.That(renderer, Does.Contain("MaxBatchSize = 1023"));
            Assert.That(renderer, Does.Contain("enableInstancing = true"));
            Assert.That(renderer, Does.Contain("ShadowCastingMode"));
            Assert.That(policy, Does.Contain("MinimumPlayableFps = 60"));
            Assert.That(policy, Does.Contain("P95FrameBudgetMilliseconds = 20f"));
            Assert.That(policy, Does.Contain("P99FrameBudgetMilliseconds = 25f"));
            Assert.That(policy, Does.Contain("MaximumSteadyStateGcAllocationBytesPerFrame = 0L"));
            Assert.That(policy, Does.Contain("FarInstancedTreeCount = 300"));
            Assert.That(policy, Does.Contain("BenchmarkShadowDistanceMeters = 65f"));
            Assert.That(policy, Does.Contain("FarShadowCastingMode = ShadowCastingMode.Off"));
            Assert.That(builder, Does.Contain("BuildPipeline.BuildPlayer"));
            Assert.That(builder, Does.Contain("BuildTarget.StandaloneWindows64"));
            Assert.That(builder, Does.Contain("BuildOptions.AutoRunPlayer"));
            Assert.That(builder, Does.Contain("ForestBenchmarkRunner"));
            Assert.That(builder, Does.Contain("PlayerSettings.enableFrameTimingStats = true"));
            Assert.That(
                File.ReadAllText("ProjectSettings/ProjectSettings.asset"),
                Does.Contain("enableFrameTimingStats: 1"));
            Assert.That(runner, Does.Contain("minimumWarmupFrames"));
            Assert.That(runner, Does.Contain("ignoredStartupStallFrames"));
            Assert.That(runner, Does.Contain("startupStallFrameThresholdMs"));
            Assert.That(runner, Does.Contain("empty_hdrp_camera"));
            Assert.That(runner, Does.Contain("lighting_volume_only"));
            Assert.That(runner, Does.Contain("ground_only"));
            Assert.That(runner, Does.Contain("near_10_only"));
            Assert.That(runner, Does.Contain("mid_50_only"));
            Assert.That(runner, Does.Contain("far_300_instanced_only"));
            Assert.That(runner, Does.Contain("broadleaf_24_only"));
            Assert.That(runner, Does.Contain("full_without_atmosphere"));
            Assert.That(runner, Does.Contain("empty_hdrp_camera_repeat"));
            Assert.That(runner, Does.Contain("baseline_orbit_repeat"));
            Assert.That(runner, Does.Contain("UnityEngine.Application.runInBackground = true"));
            Assert.That(runner, Does.Contain("cpuMainThreadPresentWait"));
            Assert.That(runner, Does.Contain("p99Ms"));
            // R2Perf1BenchmarkHarnessTests owns the executable schema-v2 budget
            // authority and serialization contract. Do not duplicate it here with
            // a source-token assertion that comments can satisfy.
            Assert.That(sampler, Does.Contain("FrameTimingManager.CaptureFrameTimings"));
            Assert.That(sampler, Does.Contain("CPU Main Thread Frame Time"));
            Assert.That(sampler, Does.Contain("GPU Frame Time"));
            Assert.That(sampler, Does.Contain("GC Allocated In Frame"));
            Assert.That(sampler, Does.Contain("Draw Calls Count"));
            Assert.That(sampler, Does.Contain("Total Used Memory"));
            Assert.That(doc, Does.Contain("not render the whole forest as normal tree prefabs"));
            Assert.That(doc, Does.Contain("near-only realtime tree shadows"));
            Assert.That(doc, Does.Contain("FAIL"));
            Assert.That(doc, Does.Contain("not approved for gameplay-scene promotion yet"));
        }

        [Test]
        public void Perf0Baseline_RecordsEvidenceBudgetsAndNextDecisionGate()
        {
            string baseline = File.ReadAllText(PerformanceBaselineDocPath);

            Assert.That(baseline, Does.Contain("R2-PERF0"));
            Assert.That(baseline, Does.Contain("VERIFIED"));
            Assert.That(baseline, Does.Contain("PROVISIONAL"));
            Assert.That(baseline, Does.Contain("UNKNOWN"));
            Assert.That(baseline, Does.Contain("16.67 ms"));
            Assert.That(baseline, Does.Contain("p95 <= 20 ms"));
            Assert.That(baseline, Does.Contain("p99 <= 25 ms"));
            Assert.That(baseline, Does.Contain("0 B/frame"));
            Assert.That(baseline, Does.Contain("empty_hdrp_camera"));
            Assert.That(baseline, Does.Contain("far_300_instanced_only"));
            Assert.That(baseline, Does.Contain("No new forest model is promoted"));
        }

        [Test]
        public void RuntimeBudgets_RejectMillionTriangleTreesBeforeGameplayPromotion()
        {
            Assert.That(RawMantissaMapleTrialTriangles, Is.GreaterThan(HardNearExceptionalLimit));
            Assert.That(RawPolyHavenAdultFirTrialTriangles, Is.GreaterThan(HardNearExceptionalLimit));
            Assert.That(HardNearExceptionalLimit, Is.GreaterThan(PreferredNearHeroLod0Budget));
            Assert.That(
                RawMantissaMapleTrialTriangles / HardNearExceptionalLimit,
                Is.GreaterThanOrEqualTo(12L),
                "The rejected raw Maple trial is more than an order of magnitude over the hard near-tree limit.");
            Assert.That(
                RawPolyHavenAdultFirTrialTriangles / HardNearExceptionalLimit,
                Is.GreaterThanOrEqualTo(34L),
                "The rejected raw adult Fir trial is far beyond the hard near-tree limit.");
        }

        [Test]
        public void Assets_DoNotContainRejectedRawAdultFirImport()
        {
            string[] rawFirAssets = AssetDatabase.FindAssets("fir_tree_01")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith("Assets/", System.StringComparison.Ordinal))
                .ToArray();

            Assert.That(
                rawFirAssets,
                Is.Empty,
                "Poly Haven adult fir raw source must stay outside Git/Assets until a curated LOD version exists.");
        }

        [Test]
        public void GitIgnore_KeepsRawManualMarketplaceTrialsOutOfRepository()
        {
            string gitIgnore = File.ReadAllText(".gitignore");

            Assert.That(gitIgnore, Does.Contain("/[Aa]ssets/pineForset_MayoGames_free/"));
            Assert.That(gitIgnore, Does.Contain("/[Aa]ssets/pineForset_MayoGames_free.meta"));
            Assert.That(gitIgnore, Does.Contain("/[Bb]enchmarks/"));
            Assert.That(gitIgnore, Does.Contain(".vsconfig"));
        }

        [Test]
        public void PlayableWorld_DoesNotReferenceExternalTrialOrRawMarketplacePrefabs()
        {
            GameObject world = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPath);
            Assert.That(world, Is.Not.Null);

            string[] transformNames = world
                .GetComponentsInChildren<Transform>(true)
                .Select(value => value.name)
                .ToArray();

            Assert.That(transformNames, Has.None.Contains("PRF_TRIAL_PolyHavenModelLineup"));
            Assert.That(transformNames.Any(value => value.Contains("ExternalTrials")), Is.False);
            Assert.That(transformNames.Any(value => value.Contains("Mantissa")), Is.False);
            Assert.That(transformNames.Any(value => value.Contains("Fab")), Is.False);
            Assert.That(transformNames.Any(value => value.Contains("Sketchfab")), Is.False);
            Assert.That(transformNames.Any(value => value.Contains("fir_tree_01")), Is.False);
        }
    }
}
