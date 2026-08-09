using System;

namespace SonsOfTheForest.Core.World
{
    public readonly struct ForestCellId : IEquatable<ForestCellId>
    {
        private readonly StableStringId _value;

        public ForestCellId(string value)
        {
            _value = new StableStringId(value);
        }

        public string Value => _value.Value;

        public bool IsValid => ForestIdentityRules.IsValid(Value);

        public static bool operator ==(ForestCellId left, ForestCellId right) => left.Equals(right);

        public static bool operator !=(ForestCellId left, ForestCellId right) => !left.Equals(right);

        public bool Equals(ForestCellId other) => _value.Equals(other._value);

        public override bool Equals(object obj) => obj is ForestCellId other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => Value;
    }

    public readonly struct ForestTreeInstanceId : IEquatable<ForestTreeInstanceId>
    {
        private readonly StableStringId _value;

        public ForestTreeInstanceId(string value)
        {
            _value = new StableStringId(value);
        }

        public string Value => _value.Value;

        public bool IsValid => ForestIdentityRules.IsValid(Value);

        public static bool operator ==(ForestTreeInstanceId left, ForestTreeInstanceId right) => left.Equals(right);

        public static bool operator !=(ForestTreeInstanceId left, ForestTreeInstanceId right) => !left.Equals(right);

        public bool Equals(ForestTreeInstanceId other) => _value.Equals(other._value);

        public override bool Equals(object obj) => obj is ForestTreeInstanceId other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => Value;
    }

    internal static class ForestIdentityRules
    {
        private const int MaximumLength = 96;

        public static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumLength)
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if ((character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') ||
                    character == '.' ||
                    character == '-' ||
                    character == '_')
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
