namespace SonsOfTheForest.Gameplay.Player
{
    public interface IPlayerIntentSource
    {
        MovementIntent ConsumeMovementIntent();

        LookIntent ConsumeLookIntent();
    }
}
