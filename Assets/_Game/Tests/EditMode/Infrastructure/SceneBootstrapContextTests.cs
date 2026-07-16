using NUnit.Framework;
using SonsOfTheForest.Core;
using SonsOfTheForest.Core.Events;
using SonsOfTheForest.Core.Services;
using SonsOfTheForest.Infrastructure.SceneBootstrap;

namespace SonsOfTheForest.Tests.EditMode.Infrastructure
{
    public sealed class SceneBootstrapContextTests
    {
        [Test]
        public void Context_PreservesSceneIdServicesAndEvents()
        {
            var sceneId = new StableStringId("scene.foundation");
            var services = new ServiceResolverStub();
            var events = new EventPublisherStub();

            var context = new SceneBootstrapContext(sceneId, services, events);

            Assert.That(context.SceneId, Is.EqualTo(sceneId));
            Assert.That(context.Services, Is.SameAs(services));
            Assert.That(context.Events, Is.SameAs(events));
        }

        [Test]
        public void Context_PreservesNullServiceAndEventReferences()
        {
            var context = new SceneBootstrapContext(
                new StableStringId("scene.foundation"),
                null,
                null);

            Assert.That(context.Services, Is.Null);
            Assert.That(context.Events, Is.Null);
        }

        private sealed class ServiceResolverStub : IServiceResolver
        {
            public bool TryResolve<TService>(out TService service) where TService : class
            {
                service = null;
                return false;
            }

            public GameResult<TService> Resolve<TService>() where TService : class
            {
                return GameResult<TService>.Failure("Not registered.");
            }
        }

        private sealed class EventPublisherStub : IEventPublisher
        {
            public void Publish<TEvent>(TEvent gameEvent) where TEvent : struct, IGameEvent
            {
            }
        }
    }
}
