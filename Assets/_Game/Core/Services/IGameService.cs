using SonsOfTheForest.Core;

namespace SonsOfTheForest.Core.Services
{
    public interface IGameService
    {
        StableStringId ServiceId { get; }

        GameServiceState State { get; }
    }
}
