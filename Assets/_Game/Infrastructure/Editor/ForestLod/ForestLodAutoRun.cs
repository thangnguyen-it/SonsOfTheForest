using SonsOfTheForest.Infrastructure.Benchmark;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SonsOfTheForest.Infrastructure.Editor.ForestLod
{
    [InitializeOnLoad]
    public static class ForestLodAutoRun
    {
        private const string FoundationScenePath = "Assets/_Game/Scenes/SCN_Foundation.unity";
        private const string PhaseKey = "SOTF.ForestBench.Phase";
        private const string IncludeRingsKey = "SOTF.ForestBench.IncludeRings";
        private const string LodPrefabRoot = "Assets/_Game/Prefabs/World/ForestLod/";

        static ForestLodAutoRun()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Sons Of The Forest/Forest LOD/Build Game-Ready LOD Forest Pass")]
        public static void BuildOnly()
        {
            ForestLodContentBuilder.BuildAll();
        }

        [MenuItem("Sons Of The Forest/Forest LOD/Run Benchmark - Current Content")]
        public static void BenchmarkCurrentContent()
        {
            StartBenchmark("current", false);
        }

        [MenuItem("Sons Of The Forest/Forest LOD/Run Benchmark - LOD Stress Rings")]
        public static void BenchmarkLodStressRings()
        {
            StartBenchmark("lod_stress", true);
        }

        private static void StartBenchmark(string phase, bool includeRings)
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[ForestLod] Cannot start benchmark while Unity is busy.");
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                Debug.LogError("[ForestLod] Cannot start benchmark while the active scene is dirty.");
                return;
            }

            EditorSceneManager.OpenScene(FoundationScenePath, OpenSceneMode.Single);

            SessionState.SetString(PhaseKey, phase);
            SessionState.SetBool(IncludeRingsKey, includeRings);
            Debug.Log($"[ForestLod] Starting benchmark phase '{phase}' in Play Mode.");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                string phase = SessionState.GetString(PhaseKey, string.Empty);
                if (string.IsNullOrEmpty(phase))
                {
                    return;
                }

                var host = new GameObject("ForestBenchmarkHost");
                var runner = host.AddComponent<ForestBenchmarkRunner>();
                runner.phase = phase;

                if (SessionState.GetBool(IncludeRingsKey, false))
                {
                    runner.midRingPrefabs = new[]
                    {
                        AssetDatabase.LoadAssetAtPath<GameObject>(
                            LodPrefabRoot + "PRF_ConiferLod_A.prefab"),
                        AssetDatabase.LoadAssetAtPath<GameObject>(
                            LodPrefabRoot + "PRF_ConiferLod_B.prefab"),
                        AssetDatabase.LoadAssetAtPath<GameObject>(
                            LodPrefabRoot + "PRF_ConiferLod_C.prefab"),
                    };
                    runner.farInstanceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                        "Assets/_Game/Art/Models/World/Generated/ForestLod/MSH_ConiferLod_A_LOD3_HorizonProxy.asset");
                    runner.farBarkMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                        "Assets/_Game/Art/Materials/World/ForestFidelity/MAT_ConiferBark.mat");
                    runner.farCanopyMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                        "Assets/_Game/Art/Materials/World/ForestFidelity/MAT_ConiferNeedlesDeep.mat");
                }
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseString(PhaseKey);
                SessionState.EraseBool(IncludeRingsKey);
            }
        }
    }
}
