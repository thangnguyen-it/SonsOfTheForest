using System;
using System.Collections.Generic;
using SonsOfTheForest.Data.Forest;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Forest
{
    [DisallowMultipleComponent]
    public sealed class ForestCellRuntime : MonoBehaviour
    {
        [SerializeField]
        private ForestCellDefinition definition;

        [SerializeField]
        private ForestSpeciesDefinition[] speciesDefinitions = Array.Empty<ForestSpeciesDefinition>();

        [SerializeField]
        private Transform staticVisualRoot;

        [SerializeField]
        private ForestStaticVisualBinding[] staticBindings = Array.Empty<ForestStaticVisualBinding>();

        [SerializeField]
        private ForestCellRenderCounters renderCounters;

        [SerializeField]
        private bool loadOnEnable = true;

        private bool isLoaded;

        public event Action Loaded;

        public event Action Unloaded;

        public ForestCellDefinition Definition => definition;

        public IReadOnlyList<ForestSpeciesDefinition> SpeciesDefinitions => speciesDefinitions;

        public IReadOnlyList<ForestStaticVisualBinding> StaticBindings => staticBindings;

        public ForestCellRenderCounters RenderCounters => renderCounters;

        public bool IsLoaded => isLoaded;

        private void OnEnable()
        {
            if (!Application.isPlaying || !loadOnEnable)
            {
                return;
            }

            if (!TryLoad(out string reason))
            {
                Debug.LogError("Forest cell failed closed: " + reason, this);
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                Unload();
            }
        }

        public bool TryLoad(out string reason)
        {
            if (isLoaded)
            {
                reason = string.Empty;
                return true;
            }

            if (!TryValidateConfiguration(out reason))
            {
                if (staticVisualRoot != null)
                {
                    staticVisualRoot.gameObject.SetActive(false);
                }

                return false;
            }

            staticVisualRoot.gameObject.SetActive(true);
            isLoaded = true;
            Loaded?.Invoke();
            reason = string.Empty;
            return true;
        }

        public void Unload()
        {
            bool wasLoaded = isLoaded;
            if (staticVisualRoot != null)
            {
                staticVisualRoot.gameObject.SetActive(false);
            }

            isLoaded = false;
            if (wasLoaded)
            {
                Unloaded?.Invoke();
            }
        }

        public bool TryValidateConfiguration(out string reason)
        {
            if (definition == null)
            {
                reason = "ForestCellDefinition is missing.";
                return false;
            }

            if (!definition.TryValidate(out reason))
            {
                return false;
            }

            if (staticVisualRoot == null || !staticVisualRoot.IsChildOf(transform))
            {
                reason = "Static visual root must be a child of the cell owner.";
                return false;
            }

            if (speciesDefinitions == null || speciesDefinitions.Length == 0)
            {
                reason = "At least one ForestSpeciesDefinition is required.";
                return false;
            }

            var speciesIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < speciesDefinitions.Length; index++)
            {
                ForestSpeciesDefinition species = speciesDefinitions[index];
                if (species == null || !species.TryValidate(out reason))
                {
                    reason = species == null ? "Species definition is missing." : reason;
                    return false;
                }

                if (!speciesIds.Add(species.SpeciesId.Value))
                {
                    reason = "Duplicate SpeciesId: " + species.SpeciesId.Value;
                    return false;
                }
            }

            if (staticBindings == null || staticBindings.Length != definition.PlacementCount)
            {
                reason = "Static binding count must equal baked placement count.";
                return false;
            }

            var bindingIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < staticBindings.Length; index++)
            {
                ForestStaticVisualBinding binding = staticBindings[index];
                if (!binding.TreeInstanceId.IsValid ||
                    !binding.SpeciesId.IsValid ||
                    !binding.VariantId.IsValid ||
                    binding.VisualRoot == null ||
                    !binding.VisualRoot.IsChildOf(staticVisualRoot))
                {
                    reason = "Static visual binding is invalid at index " + index + ".";
                    return false;
                }

                if (!bindingIds.Add(binding.TreeInstanceId.Value))
                {
                    reason = "Duplicate static binding ID: " + binding.TreeInstanceId.Value;
                    return false;
                }

                if (!TryFindPlacement(binding, out ForestTreePlacementRecord placement))
                {
                    reason = "Static binding has no matching placement: " +
                             binding.TreeInstanceId.Value;
                    return false;
                }

                ForestSpeciesDefinition species = FindSpecies(placement.SpeciesId.Value);
                if (species == null ||
                    !species.TryGetVariant(placement.VariantId.Value, out ForestVisualVariant variant) ||
                    variant.VisualPrefab == null)
                {
                    reason = "Placement references a missing species or variant: " +
                             placement.TreeInstanceId.Value;
                    return false;
                }

                if (binding.VisualRoot.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                {
                    reason = "Static tree visuals cannot contain per-tree MonoBehaviours.";
                    return false;
                }

                if (binding.VisualRoot.GetComponentsInChildren<Collider>(true).Length != 0)
                {
                    reason = "Static tree visuals cannot contain colliders.";
                    return false;
                }

                if (species.RequireLodGroup &&
                    binding.VisualRoot.GetComponentInChildren<LODGroup>(true) == null)
                {
                    reason = "Static visual is missing the required LODGroup.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private bool TryFindPlacement(
            ForestStaticVisualBinding binding,
            out ForestTreePlacementRecord placement)
        {
            IReadOnlyList<ForestTreePlacementRecord> placements = definition.Placements;
            for (int index = 0; index < placements.Count; index++)
            {
                ForestTreePlacementRecord candidate = placements[index];
                if (candidate.TreeInstanceId == binding.TreeInstanceId &&
                    candidate.SpeciesId == binding.SpeciesId &&
                    candidate.VariantId == binding.VariantId)
                {
                    placement = candidate;
                    return true;
                }
            }

            placement = default;
            return false;
        }

        private ForestSpeciesDefinition FindSpecies(string speciesId)
        {
            for (int index = 0; index < speciesDefinitions.Length; index++)
            {
                ForestSpeciesDefinition species = speciesDefinitions[index];
                if (species != null &&
                    string.Equals(species.SpeciesId.Value, speciesId, StringComparison.Ordinal))
                {
                    return species;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            ForestCellDefinition cellDefinition,
            ForestSpeciesDefinition[] species,
            Transform visualRoot,
            ForestStaticVisualBinding[] bindings,
            ForestCellRenderCounters counters,
            bool shouldLoadOnEnable)
        {
            definition = cellDefinition;
            speciesDefinitions = species ?? Array.Empty<ForestSpeciesDefinition>();
            staticVisualRoot = visualRoot;
            staticBindings = bindings ?? Array.Empty<ForestStaticVisualBinding>();
            renderCounters = counters;
            loadOnEnable = shouldLoadOnEnable;
        }
#endif
    }
}
