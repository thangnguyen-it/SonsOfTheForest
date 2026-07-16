using System.Collections.Generic;
using SonsOfTheForest.Core;

namespace SonsOfTheForest.Infrastructure.SceneBootstrap
{
    public interface ISceneCompositionRoot
    {
        StableStringId SceneId { get; }

        IEnumerable<ISceneBootstrapStep> GetBootstrapSteps();
    }
}
