using System.Collections.Generic;

namespace SonsOfTheForest.Core
{
    public sealed class ValidationReport
    {
        private readonly List<ValidationIssue> _issues = new();

        public IReadOnlyList<ValidationIssue> Issues => _issues;

        public bool HasErrors { get; private set; }

        public bool IsValid => !HasErrors;

        public void Add(ValidationIssue issue)
        {
            _issues.Add(issue);
            if (issue.Severity == ValidationSeverity.Error)
            {
                HasErrors = true;
            }
        }

        public void AddInfo(string code, string message)
        {
            Add(new ValidationIssue(ValidationSeverity.Info, code, message));
        }

        public void AddWarning(string code, string message)
        {
            Add(new ValidationIssue(ValidationSeverity.Warning, code, message));
        }

        public void AddError(string code, string message)
        {
            Add(new ValidationIssue(ValidationSeverity.Error, code, message));
        }
    }
}
