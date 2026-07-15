using SonsOfTheForest.Core;

namespace SonsOfTheForest.Infrastructure.Persistence
{
    public interface IPersistentObject
    {
        PersistentId PersistenceId { get; }

        PersistenceScope PersistenceScope { get; }

        string ContractVersion { get; }

        PersistenceSnapshot CaptureState();

        GameResult RestoreState(PersistenceSnapshot snapshot);
    }
}
