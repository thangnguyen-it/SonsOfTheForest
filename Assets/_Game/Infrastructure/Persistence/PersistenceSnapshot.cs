namespace SonsOfTheForest.Infrastructure.Persistence
{
    public readonly struct PersistenceSnapshot
    {
        public PersistenceSnapshot(
            PersistentId id,
            PersistenceScope scope,
            string contractVersion,
            string json)
        {
            Id = id;
            Scope = scope;
            ContractVersion = contractVersion ?? string.Empty;
            Json = json ?? string.Empty;
        }

        public PersistentId Id { get; }

        public PersistenceScope Scope { get; }

        public string ContractVersion { get; }

        public string Json { get; }

        public bool IsValid =>
            Id.IsValid &&
            !string.IsNullOrWhiteSpace(ContractVersion) &&
            !string.IsNullOrWhiteSpace(Json);
    }
}
