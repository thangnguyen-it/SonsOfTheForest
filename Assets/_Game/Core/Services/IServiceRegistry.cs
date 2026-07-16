using SonsOfTheForest.Core;

namespace SonsOfTheForest.Core.Services
{
    public interface IServiceRegistry : IServiceResolver
    {
        GameResult Register<TService>(TService service) where TService : class;

        GameResult Unregister<TService>() where TService : class;
    }
}
