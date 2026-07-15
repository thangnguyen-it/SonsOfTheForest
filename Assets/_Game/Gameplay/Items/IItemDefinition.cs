namespace SonsOfTheForest.Gameplay.Items
{
    public interface IItemDefinition
    {
        ItemId Id { get; }

        string DisplayName { get; }

        ItemCategory Category { get; }

        int MaxStack { get; }
    }
}
