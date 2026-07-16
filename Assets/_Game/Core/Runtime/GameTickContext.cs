namespace SonsOfTheForest.Core.Runtime
{
    public readonly struct GameTickContext
    {
        public float DeltaTime { get; }

        public float UnscaledDeltaTime { get; }

        public double TimeSeconds { get; }

        public long FrameIndex { get; }

        public GameTickContext(
            float deltaTime,
            float unscaledDeltaTime,
            double timeSeconds,
            long frameIndex)
        {
            DeltaTime = deltaTime;
            UnscaledDeltaTime = unscaledDeltaTime;
            TimeSeconds = timeSeconds;
            FrameIndex = frameIndex;
        }
    }
}
