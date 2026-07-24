using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SonsOfTheForest.Infrastructure.Benchmark
{
    /// <summary>
    /// Benchmark-only far-forest renderer for the Phase 1 model-source trial.
    /// It keeps the distant 300-tree cluster out of the GameObject/LODGroup path
    /// and renders accepted distant LOD meshes through GPU instancing.
    /// </summary>
    public sealed class SotfForestPhase1InstanceCloud : MonoBehaviour
    {
        private const int MaxBatchSize = 1023;

        [Serializable]
        public sealed class InstanceSet
        {
            public string sourceName;
            public Mesh mesh;
            public Material[] materials;
            public Vector3[] positions;
            public Vector3[] eulerAngles;
            public float[] scales;
        }

        [SerializeField]
        private InstanceSet[] instanceSets = Array.Empty<InstanceSet>();

        private readonly List<CachedSet> _cachedSets = new List<CachedSet>();

        public InstanceSet[] InstanceSets
        {
            get => instanceSets;
            set
            {
                instanceSets = value ?? Array.Empty<InstanceSet>();
                RebuildCache();
            }
        }

        public int TotalInstanceCount
        {
            get
            {
                int count = 0;
                foreach (InstanceSet set in instanceSets)
                {
                    if (set != null && set.positions != null)
                    {
                        count += set.positions.Length;
                    }
                }

                return count;
            }
        }

        public long TotalInstancedTriangles
        {
            get
            {
                long total = 0L;
                foreach (InstanceSet set in instanceSets)
                {
                    if (set == null || set.mesh == null || set.positions == null)
                    {
                        continue;
                    }

                    total += TriangleCount(set.mesh) * set.positions.Length;
                }

                return total;
            }
        }

        private void OnEnable()
        {
            RebuildCache();
        }

        private void OnValidate()
        {
            RebuildCache();
        }

        private void Update()
        {
            foreach (CachedSet set in _cachedSets)
            {
                if (set.Mesh == null || set.Materials == null || set.Materials.Length == 0)
                {
                    continue;
                }

                int subMeshCount = Mathf.Max(1, set.Mesh.subMeshCount);
                for (int batchIndex = 0; batchIndex < set.Batches.Count; batchIndex++)
                {
                    Matrix4x4[] batch = set.Batches[batchIndex];
                    for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                    {
                        Material material = set.Materials[Mathf.Min(subMesh, set.Materials.Length - 1)];
                        if (material == null)
                        {
                            continue;
                        }

                        var renderParams = new RenderParams(material)
                        {
                            worldBounds = set.WorldBounds,
                            shadowCastingMode = ShadowCastingMode.Off,
                            receiveShadows = false,
                        };
                        Graphics.RenderMeshInstanced(
                            renderParams,
                            set.Mesh,
                            subMesh,
                            batch,
                            batch.Length,
                            0);
                    }
                }
            }
        }

        private void RebuildCache()
        {
            _cachedSets.Clear();
            foreach (InstanceSet set in instanceSets)
            {
                if (set == null ||
                    set.mesh == null ||
                    set.positions == null ||
                    set.positions.Length == 0)
                {
                    continue;
                }

                var cached = new CachedSet
                {
                    Mesh = set.mesh,
                    Materials = EnsureInstancedMaterials(set.materials),
                    WorldBounds = new Bounds(transform.position, Vector3.one),
                };

                var current = new List<Matrix4x4>(MaxBatchSize);
                for (int index = 0; index < set.positions.Length; index++)
                {
                    Vector3 position = transform.TransformPoint(set.positions[index]);
                    Vector3 euler = SafeArrayValue(set.eulerAngles, index, Vector3.zero);
                    float scale = SafeArrayValue(set.scales, index, 1f);
                    Matrix4x4 matrix = Matrix4x4.TRS(
                        position,
                        Quaternion.Euler(euler),
                        Vector3.one * scale);
                    current.Add(matrix);

                    Bounds instanceBounds = set.mesh.bounds;
                    instanceBounds.center = position;
                    instanceBounds.extents *= Mathf.Max(0.01f, scale);
                    if (cached.Batches.Count == 0 && current.Count == 1)
                    {
                        cached.WorldBounds = instanceBounds;
                    }
                    else
                    {
                        cached.WorldBounds.Encapsulate(instanceBounds);
                    }

                    if (current.Count == MaxBatchSize)
                    {
                        cached.Batches.Add(current.ToArray());
                        current.Clear();
                    }
                }

                if (current.Count > 0)
                {
                    cached.Batches.Add(current.ToArray());
                }

                _cachedSets.Add(cached);
            }
        }

        private static Vector3 SafeArrayValue(Vector3[] values, int index, Vector3 fallback)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
        }

        private static Material[] EnsureInstancedMaterials(Material[] materials)
        {
            if (materials == null)
            {
                return Array.Empty<Material>();
            }

            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];
                if (material != null && !material.enableInstancing)
                {
                    material.enableInstancing = true;
                }
            }

            return materials;
        }

        private static float SafeArrayValue(float[] values, int index, float fallback)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : fallback;
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

        private sealed class CachedSet
        {
            public Mesh Mesh;
            public Material[] Materials;
            public Bounds WorldBounds;
            public readonly List<Matrix4x4[]> Batches = new List<Matrix4x4[]>();
        }
    }
}
