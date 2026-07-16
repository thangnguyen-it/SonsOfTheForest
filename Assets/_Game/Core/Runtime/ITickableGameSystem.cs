namespace SonsOfTheForest.Core.Runtime
{
    public interface ITickableGameSystem
    {
        GameLoopStage Stage { get; }

        void Tick(in GameTickContext context);
    }
}
