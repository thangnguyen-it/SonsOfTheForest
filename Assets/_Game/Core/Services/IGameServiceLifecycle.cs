using SonsOfTheForest.Core;

namespace SonsOfTheForest.Core.Services
{
    public interface IGameServiceLifecycle
    {
        GameResult Initialize(IServiceResolver services);

        GameResult Shutdown();
    }
}
