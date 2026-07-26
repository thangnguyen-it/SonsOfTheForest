using UnityEngine;
using UnityEngine.Rendering;

namespace SonsOfTheForest.Infrastructure.Benchmark
{
    /// <summary>
    /// Central policy for the current production-forest performance gate.
    /// Values are provisional engineering baselines derived from the current
    /// R2 forest evidence/specification, not claimed Sons of the Forest defaults.
    /// </summary>
    public static class ProductionForestPerformancePolicy
    {
        public const int MinimumPlayableFps = 60;
        public const int NearPlayableTreeCount = 10;
        public const int MidStaticTreeCount = 50;
        public const int FarInstancedTreeCount = 300;
        public const float BenchmarkShadowDistanceMeters = 65f;
        public const ShadowCastingMode FarShadowCastingMode = ShadowCastingMode.Off;
        public const bool FarReceiveShadows = false;

        public static bool IsMaterialRenderPolicyReady(Material material)
        {
            if (material == null)
            {
                return false;
            }

            bool opaque = !material.HasProperty("_SurfaceType") || material.GetFloat("_SurfaceType") < 0.5f;
            return opaque && material.enableInstancing;
        }
    }
}
