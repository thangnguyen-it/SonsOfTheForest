using SonsOfTheForest.Core;
using SonsOfTheForest.Core.Events;
using SonsOfTheForest.Core.Services;

namespace SonsOfTheForest.Infrastructure.SceneBootstrap
{
    public readonly struct SceneBootstrapContext
    {
        public StableStringId SceneId { get; }

        public IServiceResolver Services { get; }

        public IEventPublisher Events { get; }

        public SceneBootstrapContext(
            StableStringId sceneId,
            IServiceResolver services,
            IEventPublisher events)
        {
            SceneId = sceneId;
            Services = services;
            Events = events;
        }
    }
}
