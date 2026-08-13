using System;
using System.Collections.Generic;
using SonsOfTheForest.Data.Forest;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Forest
{
    [DisallowMultipleComponent]
    public sealed class ForestCellInteractionCoordinator : MonoBehaviour
    {
        [SerializeField]
        private ForestCellRuntime cellRuntime;

        [SerializeField]
        private Transform observer;

        [SerializeField]
        private GameObject interactiveTreePrefab;

        [SerializeField]
        private Transform interactiveRoot;

        [SerializeField]
        private Transform harvestOutputRoot;

        [SerializeField]
        [Min(0.1f)]
        private float promotionRadius = 18f;

        [SerializeField]
        [Min(0.1f)]
        private float demotionRadius = 24f;

        [SerializeField]
        [Min(0.02f)]
        private float evaluationInterval = 0.2f;

        [SerializeField]
        [Min(0.01f)]
        private float damageThreshold = 100f;

        [SerializeField]
        [Min(0f)]
        private float fallImpulse = 8f;

        [SerializeField]
        [Min(0.02f)]
        private float settleDuration = 0.6f;

        [SerializeField]
        [Min(0.1f)]
        private float maximumFallDuration = 8f;

        private readonly Dictionary<string, ForestInteractiveTree> leases = new(StringComparer.Ordinal);
        private readonly ForestTreeDeltaStore deltaStore = new();
        private float nextEvaluationTime;

        public event Action<ForestInteractiveTree> TreePromoted;

        public ForestTreeDeltaStore DeltaStore => deltaStore;

        public int LeaseCount => leases.Count;

        public Transform Observer => observer;

        public ForestCellRuntime CellRuntime => cellRuntime;

        private void OnEnable()
        {
            if (cellRuntime != null)
            {
                cellRuntime.Loaded += HandleCellLoaded;
                cellRuntime.Unloaded += HandleCellUnloaded;
            }
        }

        private void Start()
        {
            if (cellRuntime != null && cellRuntime.IsLoaded)
            {
                RestoreChangedTrees();
                EvaluateProximity();
            }
        }

        private void OnDisable()
        {
            if (cellRuntime != null)
            {
                cellRuntime.Loaded -= HandleCellLoaded;
                cellRuntime.Unloaded -= HandleCellUnloaded;
            }
        }

        private void Update()
        {
            if (observer == null || cellRuntime == null || !cellRuntime.IsLoaded ||
                Time.unscaledTime < nextEvaluationTime)
            {
                return;
            }

            nextEvaluationTime = Time.unscaledTime + evaluationInterval;
            EvaluateProximity();
        }

        public void SetObserver(Transform value)
        {
            observer = value;
        }

        public bool TryValidateConfiguration(out string reason)
        {
            if (cellRuntime == null || interactiveTreePrefab == null || interactiveRoot == null ||
                harvestOutputRoot == null || !interactiveRoot.IsChildOf(transform) ||
                !harvestOutputRoot.IsChildOf(transform))
            {
                reason = "Coordinator requires a cell runtime, interactive prefab and owned root.";
                return false;
            }

            if (interactiveTreePrefab.GetComponent<ForestInteractiveTree>() == null ||
                promotionRadius <= 0f || demotionRadius <= promotionRadius ||
                evaluationInterval <= 0f || damageThreshold <= 0f ||
                settleDuration <= 0f || maximumFallDuration < settleDuration)
            {
                reason = "Coordinator policy or interactive prefab is invalid.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void EvaluateProximity()
        {
            if (observer == null || cellRuntime == null || !cellRuntime.IsLoaded)
            {
                return;
            }

            Vector3 observerPosition = observer.position;
            float promotionSquared = promotionRadius * promotionRadius;
            float demotionSquared = demotionRadius * demotionRadius;
            IReadOnlyList<ForestStaticVisualBinding> bindings = cellRuntime.StaticBindings;
            for (int index = 0; index < bindings.Count; index++)
            {
                ForestStaticVisualBinding binding = bindings[index];
                var key = new ForestTreeDeltaKey(
                    cellRuntime.Definition.ForestCellId,
                    binding.TreeInstanceId);
                bool changed = deltaStore.TryGet(key, out ForestTreeStateDelta delta);
                float distanceSquared = (binding.VisualRoot.position - observerPosition).sqrMagnitude;
                if (changed || distanceSquared <= promotionSquared)
                {
                    EnsurePromoted(binding, changed, in delta);
                }
                else if (distanceSquared >= demotionSquared)
                {
                    TryDemote(binding.TreeInstanceId.Value);
                }
            }
        }

        public bool TryDamage(
            string treeInstanceId,
            in TreeDamageEvent damageEvent,
            out string reason)
        {
            if (!TryFindBinding(treeInstanceId, out ForestStaticVisualBinding binding))
            {
                reason = "TreeInstanceId is not part of this cell.";
                return false;
            }

            var key = new ForestTreeDeltaKey(
                cellRuntime.Definition.ForestCellId,
                binding.TreeInstanceId);
            bool changed = deltaStore.TryGet(key, out ForestTreeStateDelta delta);
            ForestInteractiveTree tree = EnsurePromoted(binding, changed, in delta);
            return tree.TryReceiveDamage(in damageEvent, out reason);
        }

        public bool TryGetLease(string treeInstanceId, out ForestInteractiveTree tree) =>
            leases.TryGetValue(treeInstanceId, out tree) && tree != null && tree.gameObject.activeSelf;

        public bool TryDemote(string treeInstanceId)
        {
            if (!leases.TryGetValue(treeInstanceId, out ForestInteractiveTree tree) ||
                tree == null || !tree.gameObject.activeSelf || !tree.CanDemote)
            {
                return false;
            }

            var key = new ForestTreeDeltaKey(tree.CellId, tree.TreeInstanceId);
            if (deltaStore.TryGet(key, out _))
            {
                return false;
            }

            if (TryFindBinding(treeInstanceId, out ForestStaticVisualBinding binding))
            {
                binding.VisualRoot.gameObject.SetActive(true);
                if (!IsStaticRepresentationReady(in binding))
                {
                    binding.VisualRoot.gameObject.SetActive(false);
                    return false;
                }
            }
            else
            {
                return false;
            }

            tree.gameObject.SetActive(false);
            return true;
        }

        private ForestInteractiveTree EnsurePromoted(
            ForestStaticVisualBinding binding,
            bool hasDelta,
            in ForestTreeStateDelta delta)
        {
            string id = binding.TreeInstanceId.Value;
            bool shouldRestoreDelta = false;
            bool createdLease = false;
            if (!leases.TryGetValue(id, out ForestInteractiveTree tree) || tree == null)
            {
                if (!TryFindPlacement(id, out ForestTreePlacementRecord placement) ||
                    !TryFindVariant(placement, out ForestVisualVariant variant, out ForestSpeciesDefinition species) ||
                    variant.FellingKit == null || species.HarvestProfile == null)
                {
                    throw new InvalidOperationException("Cannot promote unresolved tree: " + id);
                }

                createdLease = true;
                shouldRestoreDelta = hasDelta;
                GameObject instance = Instantiate(interactiveTreePrefab, interactiveRoot);
                instance.name = "Interactive_" + id;
                tree = instance.GetComponent<ForestInteractiveTree>();
                try
                {
                    // Configure applies baked placement in the coordinate space of
                    // the interactive owner. The lease prefab is instantiated under
                    // InteractiveRoot, so its local transform must be authoritative;
                    // keeping Instantiate's worldPositionStays default left promoted
                    // trees at world origin whenever the owner hierarchy moved.
                    instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    instance.transform.localScale = Vector3.one;
                    tree.Configure(
                        cellRuntime.Definition.ForestCellId,
                        in placement,
                        variant.VisualPrefab,
                        variant.FellingKit,
                        species.HarvestProfile,
                        harvestOutputRoot);
                    tree.StateChanged += HandleTreeStateChanged;
                }
                catch
                {
                    Destroy(instance);
                    throw;
                }
            }
            else
            {
                bool wasActive = tree.gameObject.activeSelf;
                tree.gameObject.SetActive(true);
                tree.SetOwnedOutputsActive(true);
                shouldRestoreDelta = hasDelta && !wasActive;
            }

            try
            {
                if (shouldRestoreDelta)
                {
                    tree.ApplyDelta(in delta);
                }

                if (!tree.ValidateRepresentationReady() || tree.TreeInstanceId != binding.TreeInstanceId ||
                    tree.SpeciesId != binding.SpeciesId || tree.VariantId != binding.VariantId)
                {
                    throw new InvalidOperationException(
                        "Interactive representation failed readiness validation: " + id);
                }

                if (createdLease)
                {
                    leases.Add(id, tree);
                }

                binding.VisualRoot.gameObject.SetActive(false);
                if (createdLease)
                {
                    TreePromoted?.Invoke(tree);
                }
            }
            catch
            {
                binding.VisualRoot.gameObject.SetActive(true);
                tree.SetOwnedOutputsActive(false);
                tree.gameObject.SetActive(false);
                if (createdLease)
                {
                    tree.StateChanged -= HandleTreeStateChanged;
                    Destroy(tree.gameObject);
                }

                throw;
            }

            return tree;
        }

        public bool TryGetRepresentationSnapshot(
            string treeInstanceId,
            out ForestRepresentationHandoffSnapshot snapshot)
        {
            if (!TryFindBinding(treeInstanceId, out ForestStaticVisualBinding binding))
            {
                snapshot = default;
                return false;
            }

            bool hasLease = leases.TryGetValue(treeInstanceId, out ForestInteractiveTree tree) &&
                            tree != null && tree.gameObject.activeSelf;
            snapshot = new ForestRepresentationHandoffSnapshot(
                cellRuntime.Definition.ForestCellId.Value,
                binding.TreeInstanceId.Value,
                binding.SpeciesId.Value,
                binding.VariantId.Value,
                binding.VisualRoot.gameObject.activeInHierarchy,
                hasLease,
                hasLease && tree.ValidateRepresentationReady(),
                hasLease ? tree.State : ForestTreeLifecycleState.Standing);
            return true;
        }

        private static bool IsStaticRepresentationReady(in ForestStaticVisualBinding binding) =>
            binding.VisualRoot != null && binding.VisualRoot.gameObject.activeSelf &&
            binding.VisualRoot.gameObject.activeInHierarchy;

        private void HandleTreeStateChanged(ForestInteractiveTree tree)
        {
            var key = new ForestTreeDeltaKey(tree.CellId, tree.TreeInstanceId);
            ForestTreeStateDelta delta = tree.CaptureDelta();
            deltaStore.Set(key, in delta);
            if (TryFindBinding(tree.TreeInstanceId.Value, out ForestStaticVisualBinding binding))
            {
                binding.VisualRoot.gameObject.SetActive(false);
            }
        }

        private void HandleCellLoaded()
        {
            RestoreChangedTrees();
            EvaluateProximity();
        }

        private void HandleCellUnloaded()
        {
            foreach (KeyValuePair<string, ForestInteractiveTree> pair in leases)
            {
                ForestInteractiveTree tree = pair.Value;
                if (tree == null || !tree.gameObject.activeSelf)
                {
                    continue;
                }

                ForestTreeStateDelta delta = tree.CaptureDelta();
                if (delta.IsChanged)
                {
                    var key = new ForestTreeDeltaKey(tree.CellId, tree.TreeInstanceId);
                    deltaStore.Set(key, in delta);
                }

                tree.gameObject.SetActive(false);
                tree.SetOwnedOutputsActive(false);
            }
        }

        private void RestoreChangedTrees()
        {
            IReadOnlyList<ForestStaticVisualBinding> bindings = cellRuntime.StaticBindings;
            for (int index = 0; index < bindings.Count; index++)
            {
                ForestStaticVisualBinding binding = bindings[index];
                var key = new ForestTreeDeltaKey(
                    cellRuntime.Definition.ForestCellId,
                    binding.TreeInstanceId);
                if (deltaStore.TryGet(key, out ForestTreeStateDelta delta))
                {
                    EnsurePromoted(binding, true, in delta);
                }
            }
        }

        private bool TryFindBinding(string id, out ForestStaticVisualBinding binding)
        {
            IReadOnlyList<ForestStaticVisualBinding> bindings = cellRuntime.StaticBindings;
            for (int index = 0; index < bindings.Count; index++)
            {
                if (string.Equals(
                        bindings[index].TreeInstanceId.Value,
                        id,
                        StringComparison.Ordinal))
                {
                    binding = bindings[index];
                    return true;
                }
            }

            binding = default;
            return false;
        }

        private bool TryFindPlacement(string id, out ForestTreePlacementRecord placement)
        {
            IReadOnlyList<ForestTreePlacementRecord> placements = cellRuntime.Definition.Placements;
            for (int index = 0; index < placements.Count; index++)
            {
                if (string.Equals(
                        placements[index].TreeInstanceId.Value,
                        id,
                        StringComparison.Ordinal))
                {
                    placement = placements[index];
                    return true;
                }
            }

            placement = default;
            return false;
        }

        private bool TryFindVariant(
            in ForestTreePlacementRecord placement,
            out ForestVisualVariant variant,
            out ForestSpeciesDefinition resolvedSpecies)
        {
            IReadOnlyList<ForestSpeciesDefinition> species = cellRuntime.SpeciesDefinitions;
            for (int index = 0; index < species.Count; index++)
            {
                ForestSpeciesDefinition candidate = species[index];
                if (candidate != null && candidate.SpeciesId == placement.SpeciesId &&
                    candidate.TryGetVariant(placement.VariantId.Value, out variant))
                {
                    resolvedSpecies = candidate;
                    return true;
                }
            }

            variant = null;
            resolvedSpecies = null;
            return false;
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            ForestCellRuntime runtime,
            Transform playerObserver,
            GameObject leasePrefab,
            Transform leaseRoot,
            Transform outputRoot,
            float promoteDistance,
            float demoteDistance,
            float health,
            float impulse,
            float settleSeconds,
            float maximumFallSeconds)
        {
            cellRuntime = runtime;
            observer = playerObserver;
            interactiveTreePrefab = leasePrefab;
            interactiveRoot = leaseRoot;
            harvestOutputRoot = outputRoot;
            promotionRadius = promoteDistance;
            demotionRadius = demoteDistance;
            damageThreshold = health;
            fallImpulse = impulse;
            settleDuration = settleSeconds;
            maximumFallDuration = maximumFallSeconds;
        }
#endif
    }
}
