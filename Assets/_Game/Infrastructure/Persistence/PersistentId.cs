using System;
using SonsOfTheForest.Core;

namespace SonsOfTheForest.Infrastructure.Persistence
{
    public readonly struct PersistentId : IEquatable<PersistentId>
    {
        private readonly StableStringId _id;

        public PersistentId(string value)
        {
            _id = new StableStringId(value);
        }

        public string Value => _id.Value;

        public bool IsValid => _id.IsValid;

        public static bool operator ==(PersistentId left, PersistentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PersistentId left, PersistentId right)
        {
            return !left.Equals(right);
        }

        public bool Equals(PersistentId other)
        {
            return _id.Equals(other._id);
        }

        public override bool Equals(object obj)
        {
            return obj is PersistentId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        public override string ToString()
        {
            return _id.ToString();
        }
    }
}
