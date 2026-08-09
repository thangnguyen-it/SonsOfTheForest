using SonsOfTheForest.Infrastructure.Forest;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Validation.Forest
{
    [DisallowMultipleComponent]
    public sealed class ForestTreeValidationDriver : MonoBehaviour
    {
        [SerializeField]
        private ForestCellInteractionCoordinator coordinator;

        [SerializeField]
        [Min(0.01f)]
        private float damagePerHit = 55f;

        public bool HitNearestTree()
        {
            if (coordinator == null || coordinator.CellRuntime == null)
            {
                return false;
            }

            ForestStaticVisualBinding nearest = default;
            float nearestSquared = float.MaxValue;
            bool found = false;
            var bindings = coordinator.CellRuntime.StaticBindings;
            for (int index = 0; index < bindings.Count; index++)
            {
                ForestStaticVisualBinding binding = bindings[index];
                float distanceSquared = (binding.VisualRoot.position - transform.position).sqrMagnitude;
                if (distanceSquared < nearestSquared)
                {
                    nearest = binding;
                    nearestSquared = distanceSquared;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            Vector3 direction = nearest.VisualRoot.position - transform.position;
            direction.y = 0f;
            var damage = new TreeDamageEvent(
                damagePerHit,
                nearest.VisualRoot.position + Vector3.up,
                direction.normalized);
            return coordinator.TryDamage(nearest.TreeInstanceId.Value, in damage, out _);
        }

        [ContextMenu("Hit Nearest Tree")]
        private void HitNearestTreeFromContextMenu()
        {
            HitNearestTree();
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            ForestCellInteractionCoordinator interactionCoordinator,
            float configuredDamagePerHit)
        {
            coordinator = interactionCoordinator;
            damagePerHit = configuredDamagePerHit;
        }
#endif
    }
}
