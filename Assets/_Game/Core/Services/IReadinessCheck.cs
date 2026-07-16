using SonsOfTheForest.Core;

namespace SonsOfTheForest.Core.Services
{
    public interface IReadinessCheck
    {
        StableStringId CheckId { get; }

        GameResult CheckReadiness(IServiceResolver services);
    }
}
