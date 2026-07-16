namespace SonsOfTheForest.Gameplay.Player
{
    public readonly struct PlayerLookState
    {
        public PlayerLookState(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = pitch;
        }

        public float Yaw { get; }

        public float Pitch { get; }

        public static PlayerLookState Identity => new(0f, 0f);
    }
}
