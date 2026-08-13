using SonsOfTheForest.Data.Forest;

namespace SonsOfTheForest.Infrastructure.Forest
{
    public readonly struct ForestRepresentationHandoffSnapshot
    {
        public ForestRepresentationHandoffSnapshot(
            string forestCellId,
            string treeInstanceId,
            string speciesId,
            string variantId,
            bool staticVisible,
            bool hasInteractiveLease,
            bool interactiveReady,
            ForestTreeLifecycleState lifecycleState)
        {
            ForestCellId = forestCellId;
            TreeInstanceId = treeInstanceId;
            SpeciesId = speciesId;
            VariantId = variantId;
            StaticVisible = staticVisible;
            HasInteractiveLease = hasInteractiveLease;
            InteractiveReady = interactiveReady;
            LifecycleState = lifecycleState;
        }

        public string ForestCellId { get; }
        public string TreeInstanceId { get; }
        public string SpeciesId { get; }
        public string VariantId { get; }
        public bool StaticVisible { get; }
        public bool HasInteractiveLease { get; }
        public bool InteractiveReady { get; }
        public ForestTreeLifecycleState LifecycleState { get; }
        public bool ExactlyOneRepresentationVisible =>
            StaticVisible != (HasInteractiveLease && InteractiveReady);
    }
}
