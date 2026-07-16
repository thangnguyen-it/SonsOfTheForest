using SonsOfTheForest.Core;

namespace SonsOfTheForest.Core.Services
{
    public interface IServiceResolver
    {
        bool TryResolve<TService>(out TService service) where TService : class;

        GameResult<TService> Resolve<TService>() where TService : class;
    }
}
