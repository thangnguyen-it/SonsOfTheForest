using System;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Forest
{
    [DisallowMultipleComponent]
    public sealed class ForestImpactDamageReceiver : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;

        public event Action<float, Vector3, string> DamageReceived;

        public float MaximumHealth => maximumHealth;
        public float CurrentHealth => currentHealth;
        public bool IsAlive => currentHealth > 0f;

        public bool ApplyForestImpact(float amount, Vector3 sourcePosition, string sourceTreeId)
        {
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount) ||
                string.IsNullOrWhiteSpace(sourceTreeId) || !IsAlive)
            {
                return false;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            DamageReceived?.Invoke(amount, sourcePosition, sourceTreeId);
            return true;
        }

#if UNITY_EDITOR
        public void EditorConfigure(float health)
        {
            maximumHealth = Mathf.Max(1f, health);
            currentHealth = maximumHealth;
        }
#endif
    }
}
