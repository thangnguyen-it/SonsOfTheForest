using System.Collections;
using NUnit.Framework;
using SonsOfTheForest.Infrastructure.Forest;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SonsOfTheForest.Tests.ForestCell.PlayMode
{
    public sealed class ForestCellRuntimeSmokeTests
    {
        [UnityTest]
        public IEnumerator ProductionCell_LoadsAndUnloadsWithoutChangingBakedPopulation()
        {
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/World/ForestCells/PRF_ForestCell_Production_001.prefab");
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);
            ForestCellRuntime runtime = instance.GetComponent<ForestCellRuntime>();
            int expected = runtime.Definition.PlacementCount;

            yield return null;
            Assert.That(runtime.IsLoaded, Is.True);
            Assert.That(runtime.StaticBindings.Count, Is.EqualTo(expected));
            runtime.Unload();
            Assert.That(runtime.IsLoaded, Is.False);
            Assert.That(runtime.TryLoad(out string reason), Is.True, reason);
            Assert.That(runtime.StaticBindings.Count, Is.EqualTo(expected));

            Object.Destroy(instance);
            yield return null;
#else
            yield break;
#endif
        }
    }
}
