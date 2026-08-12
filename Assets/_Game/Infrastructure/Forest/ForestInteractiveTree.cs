using System;
using System.Collections.Generic;
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
        private enum ActiveFallPhase { None, Hinging, Falling, Settling, Harvested }

        [SerializeField] private Transform visualAnchor;
        [SerializeField] private Rigidbody body;
        [SerializeField] private CapsuleCollider trunkCollider;

        private ForestCellId cellId;
        private ForestTreeInstanceId treeInstanceId;
        private StableStringId speciesId;
        private StableStringId variantId;
        private ForestTreeLifecycleState state;
        private ActiveFallPhase fallPhase;
        private float accumulatedDamage;
        private ForestHarvestProfile harvestProfile;
        private ForestFellingKitDefinition fellingKit;
        private Transform outputRoot;
        private float fallElapsed;
        private float fallStartedFixedTime;
        private float settledElapsed;
        private float hingeElapsed;
        private Vector3 accumulatedCutDirection;
        private Vector3 lastHitDirection;
        private Vector3 baseWorldPosition;
        private Quaternion hingeStartRotation;
        private Vector3 hingeAxis;
        private GameObject standingVisual;
        private ForestFellingVisual fellingVisual;
        private Transform stumpOutput;
        private readonly List<ForestHarvestLog> logs = new(5);
        private bool outputsSpawned;
        private bool impactRaised;
        private bool playerDamageApplied;

        public event Action<ForestInteractiveTree> StateChanged;
        public event Action<ForestInteractiveTree, Vector3, float> Impacted;
        public event Action<ForestInteractiveTree, TreeDamageEvent, int> Chopped;

        public ForestCellId CellId => cellId;
        public ForestTreeInstanceId TreeInstanceId => treeInstanceId;
        public StableStringId SpeciesId => speciesId;
        public StableStringId VariantId => variantId;
        public ForestTreeLifecycleState State => state;
        public float AccumulatedDamage => accumulatedDamage;
        public float DamageThreshold => harvestProfile != null ? harvestProfile.TreeHealth : 100f;
        public int NotchStage => ResolveNotchStage();
        public bool OutputsSpawned => outputsSpawned;
        public bool PlayerDamageApplied => playerDamageApplied;
        public IReadOnlyList<ForestHarvestLog> Logs => logs;
        public bool CanDemote => state == ForestTreeLifecycleState.Standing && accumulatedDamage <= 0f;

        public void Configure(
            ForestCellId owningCellId,
            in ForestTreePlacementRecord placement,
            GameObject visualPrefab,
            ForestFellingKitDefinition configuredFellingKit,
            ForestHarvestProfile configuredHarvestProfile,
            Transform configuredOutputRoot)
        {
            string kitReason = string.Empty;
            string profileReason = string.Empty;
            bool kitValid = configuredFellingKit != null &&
                            configuredFellingKit.TryValidate(out kitReason);
            bool profileValid = configuredHarvestProfile != null &&
                                configuredHarvestProfile.TryValidate(out profileReason);
            if (!owningCellId.IsValid || visualPrefab == null || configuredOutputRoot == null ||
                !kitValid || !profileValid)
            {
                throw new ArgumentException(
                    "Interactive tree requires valid art/profile/output ownership. " + kitReason + profileReason);
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
            fellingKit = configuredFellingKit;
            harvestProfile = configuredHarvestProfile;
            outputRoot = configuredOutputRoot;
            transform.localPosition = placement.LocalPosition;
            transform.localRotation = Quaternion.Euler(0f, placement.YawDegrees, 0f);
            transform.localScale = Vector3.one * placement.UniformScale;

            if (standingVisual == null)
            {
                standingVisual = Instantiate(visualPrefab, visualAnchor);
                standingVisual.name = "InteractiveVisual";
                standingVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                standingVisual.transform.localScale = Vector3.one;
                StripChildPhysics(standingVisual);
                ConfigureTrunkColliderFromVisual(standingVisual, 0f);
            }

            state = ForestTreeLifecycleState.Standing;
            fallPhase = ActiveFallPhase.None;
            accumulatedDamage = 0f;
            accumulatedCutDirection = Vector3.zero;
            lastHitDirection = Vector3.zero;
            outputsSpawned = false;
            impactRaised = false;
            playerDamageApplied = false;
            ApplyStandingPhysics();
        }

        public bool TryReceiveDamage(in TreeDamageEvent damageEvent, out string reason)
        {
            if (!damageEvent.IsValid)
            {
                reason = "Tree damage event is invalid.";
                return false;
            }

            if (state == ForestTreeLifecycleState.Falling || state == ForestTreeLifecycleState.Felled)
            {
                reason = "A falling or harvested tree cannot receive standing-tree damage.";
                return false;
            }

            EnsureFellingVisual();
            accumulatedDamage = Mathf.Min(DamageThreshold, accumulatedDamage + damageEvent.Amount);
            Vector3 direction = ResolveFallDirection(damageEvent.HitDirection);
            accumulatedCutDirection += direction * damageEvent.Amount;
            lastHitDirection = ResolveFallDirection(accumulatedCutDirection);
            int stage = ResolveNotchStage();
            fellingVisual.SetNotch(stage, lastHitDirection);
            Chopped?.Invoke(this, damageEvent, stage);

            if (accumulatedDamage >= DamageThreshold)
            {
                BeginHinging();
            }
            else
            {
                state = ForestTreeLifecycleState.Damaged;
                StateChanged?.Invoke(this);
            }

            reason = string.Empty;
            return true;
        }

        public ForestTreeStateDelta CaptureDelta() => new(
            state, accumulatedDamage, lastHitDirection, transform.localPosition,
            transform.localRotation);

        public void ApplyDelta(in ForestTreeStateDelta delta)
        {
            if (!delta.TryValidate(out string reason))
            {
                throw new ArgumentException(reason, nameof(delta));
            }

            accumulatedDamage = delta.AccumulatedDamage;
            lastHitDirection = delta.LastHitDirection;
            accumulatedCutDirection = lastHitDirection * accumulatedDamage;
            state = delta.State;
            transform.localPosition = delta.LocalPosition;
            transform.localRotation = delta.LocalRotation;
            if (state != ForestTreeLifecycleState.Standing || accumulatedDamage > 0f)
            {
                EnsureFellingVisual();
                fellingVisual.SetNotch(ResolveNotchStage(), lastHitDirection);
            }

            if (state == ForestTreeLifecycleState.Falling)
            {
                fallPhase = ActiveFallPhase.Falling;
                fallStartedFixedTime = Time.fixedTime;
                fallElapsed = 0f;
                ApplyFallingPhysics();
            }
            else if (state == ForestTreeLifecycleState.Felled)
            {
                fallPhase = ActiveFallPhase.Harvested;
                ApplyStandingPhysics();
                trunkCollider.enabled = false;
            }
            else
            {
                ApplyStandingPhysics();
            }
        }

        public void SetOwnedOutputsActive(bool active)
        {
            if (stumpOutput != null)
            {
                stumpOutput.gameObject.SetActive(active);
            }

            for (int index = 0; index < logs.Count; index++)
            {
                if (logs[index] != null)
                {
                    logs[index].gameObject.SetActive(active);
                }
            }
        }

        private void FixedUpdate()
        {
            switch (fallPhase)
            {
                case ActiveFallPhase.Hinging:
                    TickHinge();
                    break;
                case ActiveFallPhase.Falling:
                    TickFalling();
                    break;
                case ActiveFallPhase.Settling:
                    TickSettling();
                    break;
            }
        }

        private void TickHinge()
        {
            hingeElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(hingeElapsed / harvestProfile.HingeDuration);
            float eased = t * t * (3f - 2f * t);
            body.MoveRotation(hingeStartRotation * Quaternion.AngleAxis(
                harvestProfile.HingeAngle * eased, hingeAxis));
            if (t < 1f)
            {
                return;
            }

            ApplyFallingPhysics();
            Vector3 worldDirection = ResolveWorldFallDirection();
            body.AddTorque(Vector3.Cross(Vector3.up, worldDirection) * harvestProfile.FallTorque,
                ForceMode.Impulse);
            fallPhase = ActiveFallPhase.Falling;
        }

        private void TickFalling()
        {
            fallElapsed = Mathf.Max(0f, Time.fixedTime - fallStartedFixedTime);
            bool slow = body.linearVelocity.sqrMagnitude <= 0.09f &&
                        body.angularVelocity.sqrMagnitude <= 0.09f;
            if (slow && fallElapsed > harvestProfile.HingeDuration + 0.25f)
            {
                fallPhase = ActiveFallPhase.Settling;
                settledElapsed = 0f;
            }
            else if (fallElapsed >= harvestProfile.MaximumFallDuration)
            {
                CompleteHarvest();
            }
        }

        private void TickSettling()
        {
            fallElapsed = Mathf.Max(0f, Time.fixedTime - fallStartedFixedTime);
            bool slow = body.linearVelocity.sqrMagnitude <= 0.09f &&
                        body.angularVelocity.sqrMagnitude <= 0.09f;
            settledElapsed = slow ? settledElapsed + Time.fixedDeltaTime : 0f;
            if (settledElapsed >= harvestProfile.SettleDuration ||
                fallElapsed >= harvestProfile.MaximumFallDuration)
            {
                CompleteHarvest();
            }
            else if (!slow)
            {
                fallPhase = ActiveFallPhase.Falling;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (fallPhase != ActiveFallPhase.Falling && fallPhase != ActiveFallPhase.Settling)
            {
                return;
            }

            float magnitude = collision.relativeVelocity.magnitude;
            if (!impactRaised && magnitude >= 1.25f)
            {
                impactRaised = true;
                Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
                Impacted?.Invoke(this, point, magnitude);
            }

            if (!playerDamageApplied && magnitude >= 1.25f)
            {
                ForestImpactDamageReceiver receiver =
                    collision.collider.GetComponentInParent<ForestImpactDamageReceiver>();
                if (receiver != null)
                {
                    float damage = harvestProfile.FallingTreeDamage *
                                   Mathf.Clamp(magnitude / 8f, 0.25f, 1f);
                    receiver.ApplyForestImpact(damage, transform.position, treeInstanceId.Value);
                    playerDamageApplied = true;
                }
            }

            body.linearDamping = Mathf.Max(body.linearDamping, 0.55f);
            body.angularDamping = Mathf.Max(body.angularDamping, 0.8f);
        }

        private void BeginHinging()
        {
            state = ForestTreeLifecycleState.Falling;
            fallPhase = ActiveFallPhase.Hinging;
            fallElapsed = 0f;
            fallStartedFixedTime = Time.fixedTime;
            settledElapsed = 0f;
            hingeElapsed = 0f;
            baseWorldPosition = transform.position;
            Vector3 cutPoint = baseWorldPosition + transform.up * fellingKit.CutHeight;
            stumpOutput = fellingVisual.DetachStump(outputRoot);
            transform.position = cutPoint;
            hingeStartRotation = transform.rotation;
            Vector3 worldDirection = ResolveWorldFallDirection();
            hingeAxis = transform.InverseTransformDirection(Vector3.Cross(Vector3.up, worldDirection)).normalized;
            if (hingeAxis.sqrMagnitude < 0.01f)
            {
                hingeAxis = Vector3.right;
            }

            ConfigureTrunkColliderFromVisual(fellingVisual.UpperRoot.gameObject, 0f);
            ApplyHingePhysics();
            StateChanged?.Invoke(this);
        }

        private void CompleteHarvest()
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            trunkCollider.enabled = false;
            fallPhase = ActiveFallPhase.Harvested;
            state = ForestTreeLifecycleState.Felled;
            SpawnLogsOnce();
            fellingVisual.SetUpperVisible(false);
            StateChanged?.Invoke(this);
        }

        private void SpawnLogsOnce()
        {
            if (outputsSpawned)
            {
                return;
            }

            outputsSpawned = true;
            Vector3 axis = transform.up.normalized;
            if (Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.9f)
            {
                axis = ResolveWorldFallDirection();
            }

            float spacing = fellingKit.UsableTrunkLength / fellingKit.LogYield;
            for (int index = 0; index < fellingKit.LogYield; index++)
            {
                GameObject prefab = fellingKit.WholeLogPrefabs[index % fellingKit.WholeLogPrefabs.Count];
                Vector3 proposed = transform.position + axis * (spacing * (index + 0.5f));
                if (Physics.Raycast(proposed + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 8f, ~0,
                        QueryTriggerInteraction.Ignore))
                {
                    proposed.y = hit.point.y + fellingKit.TrunkRadius;
                }

                GameObject instance = Instantiate(prefab, proposed,
                    Quaternion.LookRotation(axis, Vector3.up), outputRoot);
                instance.name = treeInstanceId.Value + "_Log_" + index.ToString("D2");
                ForestHarvestLog log = instance.GetComponent<ForestHarvestLog>();
                log.Configure(cellId, treeInstanceId, index, harvestProfile.LogMass);
                Rigidbody logBody = log.GetComponent<Rigidbody>();
                logBody.linearVelocity = body.linearVelocity * 0.15f;
                logs.Add(log);
            }
        }

        private void EnsureFellingVisual()
        {
            if (fellingVisual != null)
            {
                return;
            }

            GameObject instance = Instantiate(fellingKit.FellingVisualPrefab, visualAnchor);
            instance.name = "FellingVisual";
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            fellingVisual = instance.GetComponent<ForestFellingVisual>();
            string reason = fellingVisual == null
                ? "ForestFellingVisual component is missing."
                : string.Empty;
            if (fellingVisual == null || !fellingVisual.TryValidate(out reason))
            {
                Destroy(instance);
                throw new InvalidOperationException("Felling kit prefab is invalid: " + reason);
            }

            standingVisual.SetActive(false);
        }

        private int ResolveNotchStage()
        {
            if (accumulatedDamage <= 0f || harvestProfile == null)
            {
                return 0;
            }

            float ratio = Mathf.Clamp01(accumulatedDamage / DamageThreshold);
            return Mathf.Clamp(Mathf.CeilToInt(ratio * harvestProfile.NotchStageCount),
                1, harvestProfile.NotchStageCount);
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

        private Vector3 ResolveWorldFallDirection()
        {
            Vector3 value = transform.parent != null
                ? transform.parent.TransformDirection(lastHitDirection)
                : lastHitDirection;
            value.y = 0f;
            return value.sqrMagnitude > 0.001f ? value.normalized : transform.forward;
        }

        private void ApplyStandingPhysics()
        {
            EnsureComponentReferences();
            trunkCollider.enabled = true;
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        private void ApplyHingePhysics()
        {
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        private void ApplyFallingPhysics()
        {
            body.constraints = RigidbodyConstraints.None;
            body.useGravity = true;
            body.isKinematic = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = 0.12f;
            body.angularDamping = 0.18f;
            body.WakeUp();
        }

        private void ConfigureTrunkColliderFromVisual(GameObject visual, float bottomOffset)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
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
            trunkCollider.center = new Vector3(0f, localCenter.y + bottomOffset, 0f);
            trunkCollider.height = Mathf.Max(2f, height);
            trunkCollider.radius = Mathf.Clamp(fellingKit != null ? fellingKit.TrunkRadius : height * 0.032f,
                0.25f, 0.75f);
        }

        private static void StripChildPhysics(GameObject visual)
        {
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Destroy(collider);
            }
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
