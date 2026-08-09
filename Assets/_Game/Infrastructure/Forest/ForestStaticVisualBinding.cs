using System;
using SonsOfTheForest.Core;
using SonsOfTheForest.Core.World;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Forest
{
    [Serializable]
    public struct ForestStaticVisualBinding
    {
        [SerializeField]
        private string treeInstanceId;

        [SerializeField]
        private string speciesId;

        [SerializeField]
        private string variantId;

        [SerializeField]
        private Transform visualRoot;

        public ForestStaticVisualBinding(
            string treeInstanceId,
            string speciesId,
            string variantId,
            Transform visualRoot)
        {
            this.treeInstanceId = treeInstanceId ?? string.Empty;
            this.speciesId = speciesId ?? string.Empty;
            this.variantId = variantId ?? string.Empty;
            this.visualRoot = visualRoot;
        }

        public ForestTreeInstanceId TreeInstanceId => new(treeInstanceId);

        public StableStringId SpeciesId => new(speciesId);

        public StableStringId VariantId => new(variantId);

        public Transform VisualRoot => visualRoot;
    }

    [Serializable]
    public struct ForestCellRenderCounters
    {
        [SerializeField]
        private int staticTreeCount;

        [SerializeField]
        private int rendererCount;

        [SerializeField]
        private int lodGroupCount;

        [SerializeField]
        private int colliderCount;

        [SerializeField]
        private long lod0TriangleCount;

        public ForestCellRenderCounters(
            int staticTreeCount,
            int rendererCount,
            int lodGroupCount,
            int colliderCount,
            long lod0TriangleCount)
        {
            this.staticTreeCount = staticTreeCount;
            this.rendererCount = rendererCount;
            this.lodGroupCount = lodGroupCount;
            this.colliderCount = colliderCount;
            this.lod0TriangleCount = lod0TriangleCount;
        }

        public int StaticTreeCount => staticTreeCount;

        public int RendererCount => rendererCount;

        public int LodGroupCount => lodGroupCount;

        public int ColliderCount => colliderCount;

        public long Lod0TriangleCount => lod0TriangleCount;
    }
}
