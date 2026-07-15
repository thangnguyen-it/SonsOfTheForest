using SonsOfTheForest.Core;

namespace SonsOfTheForest.Gameplay.Interaction
{
    public readonly struct InteractionResult
    {
        public InteractionResult(bool handled, GameResult result)
        {
            Handled = handled;
            Result = result;
        }

        public bool Handled { get; }

        public GameResult Result { get; }

        public static InteractionResult NotHandled()
        {
            return new InteractionResult(false, GameResult.Success());
        }

        public static InteractionResult HandledSuccess()
        {
            return new InteractionResult(true, GameResult.Success());
        }

        public static InteractionResult HandledFailure(string error)
        {
            return new InteractionResult(true, GameResult.Failure(error));
        }
    }
}
