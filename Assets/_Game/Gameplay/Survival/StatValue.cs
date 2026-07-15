using UnityEngine;

namespace SonsOfTheForest.Gameplay.Survival
{
    public readonly struct StatValue
    {
        public StatValue(SurvivalStatKind kind, float current, float min, float max)
        {
            Kind = kind;
            Current = current;
            Min = min;
            Max = max;
            Normalized = max <= min
                ? 0f
                : Mathf.Clamp01((current - min) / (max - min));
        }

        public SurvivalStatKind Kind { get; }

        public float Current { get; }

        public float Min { get; }

        public float Max { get; }

        public float Normalized { get; }
    }
}
