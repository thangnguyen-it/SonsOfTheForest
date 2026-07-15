namespace SonsOfTheForest.Core
{
    public readonly struct GameResult
    {
        private GameResult(bool succeeded, string error)
        {
            Succeeded = succeeded;
            Error = error ?? string.Empty;
        }

        public bool Succeeded { get; }

        public bool Failed => !Succeeded;

        public string Error { get; }

        public static GameResult Success()
        {
            return new GameResult(true, string.Empty);
        }

        public static GameResult Failure(string error)
        {
            return new GameResult(false, error);
        }

        public override string ToString()
        {
            return Succeeded ? "Success" : $"Failure: {Error}";
        }
    }

    public readonly struct GameResult<T>
    {
        private GameResult(bool succeeded, T value, string error)
        {
            Succeeded = succeeded;
            Value = value;
            Error = error ?? string.Empty;
        }

        public bool Succeeded { get; }

        public bool Failed => !Succeeded;

        public string Error { get; }

        public T Value { get; }

        public static GameResult<T> Success(T value)
        {
            return new GameResult<T>(true, value, string.Empty);
        }

        public static GameResult<T> Failure(string error)
        {
            return new GameResult<T>(false, default, error);
        }

        public override string ToString()
        {
            return Succeeded ? $"Success: {Value}" : $"Failure: {Error}";
        }
    }
}
