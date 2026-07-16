namespace SonsOfTheForest.Core
{
    public readonly struct ValidationIssue
    {
        public ValidationSeverity Severity { get; }

        public string Code { get; }

        public string Message { get; }

        public ValidationIssue(ValidationSeverity severity, string code, string message)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            return $"[{Severity}] {Code}: {Message}";
        }
    }
}
