using SonsOfTheForest.Core;

namespace SonsOfTheForest.Infrastructure.SceneBootstrap
{
    public interface ISceneReadinessValidator
    {
        void ValidateScene(in SceneBootstrapContext context, ValidationReport report);
    }
}
