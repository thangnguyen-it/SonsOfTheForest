namespace SonsOfTheForest.Core.Events
{
    public interface IEventPublisher
    {
        void Publish<TEvent>(TEvent gameEvent) where TEvent : struct, IGameEvent;
    }
}
