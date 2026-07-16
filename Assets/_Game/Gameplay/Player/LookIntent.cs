using UnityEngine;

namespace SonsOfTheForest.Gameplay.Player
{
    public readonly struct LookIntent
    {
        public LookIntent(Vector2 value, LookInputKind inputKind)
        {
            Value = value;
            InputKind = inputKind;
        }

        public Vector2 Value { get; }

        public LookInputKind InputKind { get; }

        public bool IsZero => Value == Vector2.zero;

        public static LookIntent None =>
            new(Vector2.zero, LookInputKind.Delta);
    }
}
