namespace SonsOfTheForest.Gameplay.Interaction
{
    public readonly struct InteractionPrompt
    {
        public InteractionPrompt(
            string text,
            InteractionInputKind inputKind,
            float holdSeconds,
            InteractionAvailability availability)
        {
            Text = text ?? string.Empty;
            InputKind = inputKind;
            HoldSeconds = holdSeconds > 0f ? holdSeconds : 0f;
            Availability = availability;
        }

        public string Text { get; }

        public InteractionInputKind InputKind { get; }

        public float HoldSeconds { get; }

        public InteractionAvailability Availability { get; }

        public bool HasText => !string.IsNullOrWhiteSpace(Text);

        public static InteractionPrompt None => new InteractionPrompt(
            string.Empty,
            InteractionInputKind.Press,
            0f,
            InteractionAvailability.Hidden);

        public static InteractionPrompt Press(string text)
        {
            return new InteractionPrompt(
                text,
                InteractionInputKind.Press,
                0f,
                InteractionAvailability.Available);
        }

        public static InteractionPrompt Hold(string text, float holdSeconds)
        {
            return new InteractionPrompt(
                text,
                InteractionInputKind.Hold,
                holdSeconds,
                InteractionAvailability.Available);
        }
    }
}
