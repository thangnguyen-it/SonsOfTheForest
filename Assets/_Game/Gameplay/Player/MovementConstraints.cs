namespace SonsOfTheForest.Gameplay.Player
{
    public readonly struct MovementConstraints
    {
        public MovementConstraints(
            bool canSprint,
            bool canJump,
            bool canStand,
            float speedMultiplier)
        {
            CanSprint = canSprint;
            CanJump = canJump;
            CanStand = canStand;
            SpeedMultiplier =
                float.IsNaN(speedMultiplier) || speedMultiplier < 0f
                    ? 0f
                    : speedMultiplier;
        }

        public bool CanSprint { get; }

        public bool CanJump { get; }

        public bool CanStand { get; }

        public float SpeedMultiplier { get; }

        public static MovementConstraints Permissive =>
            new(true, true, true, 1f);
    }
}
