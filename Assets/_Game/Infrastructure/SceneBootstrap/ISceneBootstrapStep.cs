using SonsOfTheForest.Core;

namespace SonsOfTheForest.Infrastructure.SceneBootstrap
{
    public interface ISceneBootstrapStep
    {
        StableStringId StepId { get; }

        SceneBootstrapPhase Phase { get; }

        GameResult Execute(in SceneBootstrapContext context);
    }
}
