using System;
using SonsOfTheForest.Core;
using SonsOfTheForest.Core.World;
using SonsOfTheForest.Data.Forest;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Forest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class ForestInteractiveTree : MonoBehaviour, ITreeDamageReceiver
    {
        [SerializeField]
        private Transform visualAnchor;

        [SerializeField]
        private Rigidbody body;

        [SerializeField]
        private CapsuleCollider trunkCollider;

        private ForestCellId cellId;
        private ForestTreeInstanceId treeInstanceId;
        private StableStringId speciesId;
        private StableStringId variantId;
        private ForestTreeLifecycleState state;
        private float accumulatedDamage;
        private float damageThreshold;
        private float fallImpulse;
        private float settleDuration;
        private float maximumFallDuration;
        private float fallElapsed;
        private float settledElapsed;
        private Vector3 lastHitDirection;
        private GameObject visualInstance;

        public event Action<ForestInteractiveTree> StateChanged;

        public ForestCellId CellId => cellId;

        public ForestTreeInstanceId TreeInstanceId => treeInstanceId;

        public StableStringId SpeciesId => speciesId;

        public StableStringId VariantId => variantId;

        public ForestTreeLifecycleState State => state;

        public float AccumulatedDamage => accumulatedDamage;

        public bool CanDemote => state == ForestTreeLifecycleState.Standing && accumulatedDamage <= 0f;

        public void Configure(
            ForestCellId owningCellId,
            in ForestTreePlacementRecord placement,
            GameObject visualPrefab,
            float configuredDamageThreshold,
            float configuredFallImpulse,
            float configuredSettleDuration,
            float configuredMaximumFallDuration)
        {
            if (!owningCellId.IsValid || visualPrefab == null)
            {
                throw new ArgumentException("Interactive tree requires a valid cell and visual prefab.");
            }

            if (!placement.TryValidate(out string reason))
            {
                throw new ArgumentException("Interactive tree configuration is invalid: " + reason);
            }

            EnsureComponentReferences();
            cellId = owningCellId;
            treeInstanceId = placement.TreeInstanceId;
            speciesId = placement.SpeciesId;
            variantId = placement.VariantId;
            damageThreshold = Mathf.Max(0.01f, configuredDamageThreshold);
            fallImpulse = Mathf.Max(0f, configuredFallImpulse);
            settleDuration = Mathf.Max(0.02f, configuredSettleDuration);
            maximumFallDuration = Mathf.Max(settleDuration, configuredMaximumFallDuration);

            transform.localPosition = placement.LocalPosition;
            transform.localRotation = Quaternion.Euler(0f, placement.YawDegrees, 0f);
            transform.localScale = Vector3.one * placement.UniformScale;
            if (visualInstance == null)
            {
                visualInstance = Instantiate(visualPrefab, visualAnchor);
                visualInstance.name = "InteractiveVisual";
                visualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                visualInstance.transform.localScale = Vector3.one;
                foreach (Collider childCollider in visualInstance.GetComponentsInChildren<Collider>(true))
                {
                    childCollider.enabled = false;
                    Destroy(childCollider);
                }

                ConfigureTrunkColliderFromVisual();
            }

            ApplyStandingPhysics();
            state = ForestTreeLifecycleState.Standing;
            accumulatedDamage = 0f;
            lastHitDirection = Vector3.zero;
        }

        public bool TryReceiveDamage(in TreeDamageEvent damageEvent, out string reason)
        {
            if (!damageEvent.IsValid)
            {
                reason = "Tree damage event is invalid.";
                return false;
            }

            if (state == ForestTreeLifecycleState.Falling ||
                state == ForestTreeLifecycleState.Felled)
            {
                reason = "A falling or felled tree cannot receive standing-tree damage.";
                return false;
            }

            accumulatedDamage += damageEvent.Amount;
            lastHitDirection = ResolveFallDirection(damageEvent.HitDirection);
            if (accumulatedDamage >= damageThreshold)
            {
                BeginFalling(damageEvent.HitPosition);
            }
            else
            {
                state = ForestTreeLifecycleState.Damaged;
                StateChanged?.Invoke(this);
            }

            reason = string.Empty;
            return true;
        }

        public ForestTreeStateDelta CaptureDelta()
        {
            return new ForestTreeStateDelta(
                state,
                accumulatedDamage,
                lastHitDirection,
                transform.localPosition,
                transform.localRotation);
        }

        public void ApplyDelta(in ForestTreeStateDelta delta)
        {
            if (!delta.TryValidate(out string reason))
            {
                throw new ArgumentException(reason, nameof(delta));
            }

            accumulatedDamage = delta.AccumulatedDamage;
            lastHitDirection = delta.LastHitDirection;
            state = delta.State;
            transform.localPosition = delta.LocalPosition;
            transform.localRotation = delta.LocalRotation;
            if (state == ForestTreeLifecycleState.Falling)
            {
                ApplyFallingPhysics();
            }
            else
            {
                ApplyStandingPhysics();
            }
        }

        private void FixedUpdate()
        {
            if (state != ForestTreeLifecycleState.Falling)
            {
                return;
            }

            fallElapsed += Time.fixedDeltaTime;
            bool slow = body.linearVelocity.sqrMagnitude <= 0.04f &&
                        body.angularVelocity.sqrMagnitude <= 0.04f;
            settledElapsed = slow ? settledElapsed + Time.fixedDeltaTime : 0f;
            if (settledElapsed >= settleDuration || fallElapsed >= maximumFallDuration)
            {
                state = ForestTreeLifecycleState.Felled;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                StateChanged?.Invoke(this);
            }
        }

        private void BeginFalling(Vector3 hitPosition)
        {
            state = ForestTreeLifecycleState.Falling;
            fallElapsed = 0f;
            settledElapsed = 0f;
            ApplyFallingPhysics();
            Vector3 worldDirection = transform.parent != null
                ? transform.parent.TransformDirection(lastHitDirection)
                : lastHitDirection;
            body.AddForceAtPosition(
                worldDirection * fallImpulse,
                hitPosition,
                ForceMode.Impulse);
            body.AddTorque(
                Vector3.Cross(Vector3.up, worldDirection) * fallImpulse,
                ForceMode.Impulse);
            StateChanged?.Invoke(this);
        }

        private Vector3 ResolveFallDirection(Vector3 requestedDirection)
        {
            Vector3 local = transform.parent != null
                ? transform.parent.InverseTransformDirection(requestedDirection)
                : requestedDirection;
            local.y = 0f;
            if (local.sqrMagnitude > 0.0001f)
            {
                return local.normalized;
            }

            uint hash = 2166136261u;
            string value = treeInstanceId.Value;
            for (int index = 0; index < value.Length; index++)
            {
                hash = (hash ^ value[index]) * 16777619u;
            }

            float angle = (hash % 3600u) * (Mathf.PI / 1800f);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        private void ApplyStandingPhysics()
        {
            EnsureComponentReferences();
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void ApplyFallingPhysics()
        {
            EnsureComponentReferences();
            body.constraints = RigidbodyConstraints.None;
            body.useGravity = true;
            body.isKinematic = false;
            body.WakeUp();
        }

        private void ConfigureTrunkColliderFromVisual()
        {
            Renderer[] renderers = visualInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                trunkCollider.center = new Vector3(0f, 7f, 0f);
                trunkCollider.height = 14f;
                trunkCollider.radius = 0.45f;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            float inverseScale = 1f / Mathf.Max(0.001f, transform.lossyScale.y);
            float height = bounds.size.y * inverseScale;
            trunkCollider.direction = 1;
            trunkCollider.center = new Vector3(0f, localCenter.y, 0f);
            trunkCollider.height = Mathf.Max(2f, height);
            trunkCollider.radius = Mathf.Clamp(height * 0.032f, 0.32f, 0.68f);
        }

        private void EnsureComponentReferences()
        {
            body ??= GetComponent<Rigidbody>();
            trunkCollider ??= GetComponent<CapsuleCollider>();
            visualAnchor ??= transform.Find("VisualAnchor");
            if (visualAnchor == null)
            {
                var anchor = new GameObject("VisualAnchor");
                anchor.transform.SetParent(transform, false);
                visualAnchor = anchor.transform;
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(Transform anchor, Rigidbody rigidbody, CapsuleCollider collider)
        {
            visualAnchor = anchor;
            body = rigidbody;
            trunkCollider = collider;
        }
#endif
    }
}
