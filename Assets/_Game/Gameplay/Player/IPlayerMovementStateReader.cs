namespace SonsOfTheForest.Gameplay.Player
{
    public interface IPlayerMovementStateReader
    {
        PlayerMovementState CurrentMovementState { get; }

        PlayerLookState CurrentLookState { get; }
    }
}
