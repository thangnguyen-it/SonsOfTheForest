namespace SonsOfTheForest.Core.Services
{
    public enum GameServiceState
    {
        Uninitialized,
        Initializing,
        Ready,
        Failed,
        ShuttingDown,
        Shutdown
    }
}
