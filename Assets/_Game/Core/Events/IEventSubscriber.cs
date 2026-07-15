using System;

namespace SonsOfTheForest.Core.Events
{
    public interface IEventSubscriber
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent;
    }
}
