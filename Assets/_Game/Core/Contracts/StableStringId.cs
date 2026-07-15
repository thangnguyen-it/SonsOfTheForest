using System;

namespace SonsOfTheForest.Core
{
    public readonly struct StableStringId : IEquatable<StableStringId>
    {
        private readonly string _value;

        public StableStringId(string value)
        {
            _value = value;
        }

        public string Value => _value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public static bool operator ==(StableStringId left, StableStringId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StableStringId left, StableStringId right)
        {
            return !left.Equals(right);
        }

        public bool Equals(StableStringId other)
        {
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is StableStringId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
