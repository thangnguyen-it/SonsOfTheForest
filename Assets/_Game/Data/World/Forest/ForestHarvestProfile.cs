using System;
using UnityEngine;

namespace SonsOfTheForest.Data.Forest
{
    [CreateAssetMenu(fileName = "HRV_ForestSpecies", menuName = "Sons Of The Forest/Forest/Harvest Profile")]
    public sealed class ForestHarvestProfile : ScriptableObject
    {
        [SerializeField, Min(1f)] private float treeHealth = 100f;
        [SerializeField, Range(1, 6)] private int notchStageCount = 3;
        [SerializeField, Min(0.02f)] private float hingeDuration = 0.55f;
        [SerializeField, Range(5f, 45f)] private float hingeAngle = 18f;
        [SerializeField, Min(0f)] private float fallTorque = 9f;
        [SerializeField, Min(0.02f)] private float settleDuration = 0.8f;
        [SerializeField, Min(0.1f)] private float maximumFallDuration = 10f;
        [SerializeField, Min(0.1f)] private float logMass = 28f;
        [SerializeField, Min(0.1f)] private float fallingTreeDamage = 80f;

        public float TreeHealth => treeHealth;
        public int NotchStageCount => notchStageCount;
        public float HingeDuration => hingeDuration;
        public float HingeAngle => hingeAngle;
        public float FallTorque => fallTorque;
        public float SettleDuration => settleDuration;
        public float MaximumFallDuration => maximumFallDuration;
        public float LogMass => logMass;
        public float FallingTreeDamage => fallingTreeDamage;

        public bool TryValidate(out string reason)
        {
            if (!FinitePositive(treeHealth) || notchStageCount < 1 || notchStageCount > 6 ||
                !FinitePositive(hingeDuration) || !FinitePositive(hingeAngle) ||
                !FiniteNonNegative(fallTorque) || !FinitePositive(settleDuration) ||
                !FinitePositive(maximumFallDuration) || maximumFallDuration < settleDuration ||
                !FinitePositive(logMass) || !FiniteNonNegative(fallingTreeDamage))
            {
                reason = "Forest harvest profile contains invalid provisional tuning.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool FinitePositive(float value) => FiniteNonNegative(value) && value > 0f;
        private static bool FiniteNonNegative(float value) =>
            value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);

#if UNITY_EDITOR
        public void EditorConfigure(
            float health, int stages, float hingeSeconds, float hingeDegrees,
            float torque, float settleSeconds, float fallTimeout, float mass,
            float impactDamage)
        {
            treeHealth = health;
            notchStageCount = stages;
            hingeDuration = hingeSeconds;
            hingeAngle = hingeDegrees;
            fallTorque = torque;
            settleDuration = settleSeconds;
            maximumFallDuration = fallTimeout;
            logMass = mass;
            fallingTreeDamage = impactDamage;
        }
#endif
    }
}
