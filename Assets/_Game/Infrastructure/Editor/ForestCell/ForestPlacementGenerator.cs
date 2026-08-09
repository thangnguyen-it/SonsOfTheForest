using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SonsOfTheForest.Core.World;
using SonsOfTheForest.Data.Forest;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Editor.ForestCell
{
    public readonly struct ForestSpeciesBakeInput
    {
        public ForestSpeciesBakeInput(
            string speciesId,
            string[] variantIds,
            float[] variantWeights,
            float speciesWeight,
            float minimumScale,
            float maximumScale)
        {
            SpeciesId = speciesId;
            VariantIds = variantIds;
            VariantWeights = variantWeights;
            SpeciesWeight = speciesWeight;
            MinimumScale = minimumScale;
            MaximumScale = maximumScale;
        }

        public string SpeciesId { get; }

        public string[] VariantIds { get; }

        public float[] VariantWeights { get; }

        public float SpeciesWeight { get; }

        public float MinimumScale { get; }

        public float MaximumScale { get; }
    }

    public readonly struct ForestPlacementBakeRequest
    {
        public ForestPlacementBakeRequest(
            ForestCellId cellId,
            int generatorVersion,
            Bounds bounds,
            int seed,
            float densityPerSquareMeter,
            ForestSpeciesBakeInput[] species)
        {
            CellId = cellId;
            GeneratorVersion = generatorVersion;
            Bounds = bounds;
            Seed = seed;
            DensityPerSquareMeter = densityPerSquareMeter;
            Species = species;
        }

        public ForestCellId CellId { get; }

        public int GeneratorVersion { get; }

        public Bounds Bounds { get; }

        public int Seed { get; }

        public float DensityPerSquareMeter { get; }

        public ForestSpeciesBakeInput[] Species { get; }
    }

    public static class ForestPlacementGenerator
    {
        private const int MaximumPlacements = 4096;
        private const int AttemptsPerPlacement = 48;
        private const int ClusterCount = 4;

        public static ForestTreePlacementRecord[] Generate(in ForestPlacementBakeRequest request)
        {
            ValidateRequest(in request);
            float area = request.Bounds.size.x * request.Bounds.size.z;
            int count = Mathf.Clamp(
                Mathf.RoundToInt(area * request.DensityPerSquareMeter),
                0,
                MaximumPlacements);
            if (count == 0)
            {
                return Array.Empty<ForestTreePlacementRecord>();
            }

            var random = new DeterministicRandom(unchecked((uint)request.Seed));
            var clusterCenters = new Vector2[ClusterCount];
            for (int index = 0; index < clusterCenters.Length; index++)
            {
                clusterCenters[index] = RandomPointInBounds(ref random, request.Bounds, 0.12f);
            }

            float nominalSpacing = Mathf.Sqrt(area / Mathf.Max(1, count));
            float minimumSpacing = Mathf.Clamp(nominalSpacing * 0.58f, 1.5f, 8f);
            float minimumSpacingSquared = minimumSpacing * minimumSpacing;
            float clusterRadius = Mathf.Max(5f, nominalSpacing * 3.1f);
            var accepted = new List<Vector2>(count);
            var records = new ForestTreePlacementRecord[count];

            for (int index = 0; index < count; index++)
            {
                Vector2 best = default;
                float bestDistance = -1f;
                bool found = false;
                for (int attempt = 0; attempt < AttemptsPerPlacement; attempt++)
                {
                    Vector2 candidate = random.NextFloat01() < 0.62f
                        ? RandomClusterPoint(
                            ref random,
                            request.Bounds,
                            clusterCenters[random.NextInt(clusterCenters.Length)],
                            clusterRadius)
                        : RandomPointInBounds(ref random, request.Bounds, 0.03f);
                    float nearest = NearestDistanceSquared(candidate, accepted);
                    if (nearest > bestDistance)
                    {
                        best = candidate;
                        bestDistance = nearest;
                    }

                    if (nearest >= minimumSpacingSquared)
                    {
                        found = true;
                        best = candidate;
                        break;
                    }
                }

                if (!found && bestDistance < 0f)
                {
                    best = RandomPointInBounds(ref random, request.Bounds, 0.03f);
                }

                accepted.Add(best);
                ForestSpeciesBakeInput species = SelectSpecies(ref random, request.Species);
                string variantId = SelectVariant(ref random, in species);
                float scale = Mathf.Lerp(species.MinimumScale, species.MaximumScale, random.NextFloat01());
                string instanceId = CreateInstanceId(
                    request.CellId,
                    request.GeneratorVersion,
                    request.Seed,
                    index);
                records[index] = new ForestTreePlacementRecord(
                    instanceId,
                    species.SpeciesId,
                    variantId,
                    new Vector3(best.x, request.Bounds.min.y, best.y),
                    random.NextFloat01() * 360f,
                    scale);
            }

            return records;
        }

        private static void ValidateRequest(in ForestPlacementBakeRequest request)
        {
            if (!request.CellId.IsValid || request.GeneratorVersion <= 0)
            {
                throw new ArgumentException("A valid cell ID and generator version are required.");
            }

            if (request.Bounds.size.x <= 0f || request.Bounds.size.z <= 0f ||
                float.IsNaN(request.DensityPerSquareMeter) ||
                float.IsInfinity(request.DensityPerSquareMeter) ||
                request.DensityPerSquareMeter < 0f)
            {
                throw new ArgumentException("Bounds and density are invalid.");
            }

            if (request.Species == null || request.Species.Length == 0)
            {
                throw new ArgumentException("At least one species bake input is required.");
            }

            for (int index = 0; index < request.Species.Length; index++)
            {
                ForestSpeciesBakeInput species = request.Species[index];
                if (string.IsNullOrWhiteSpace(species.SpeciesId) ||
                    species.VariantIds == null || species.VariantIds.Length == 0 ||
                    species.VariantWeights == null ||
                    species.VariantWeights.Length != species.VariantIds.Length ||
                    species.SpeciesWeight <= 0f ||
                    species.MinimumScale <= 0f ||
                    species.MaximumScale < species.MinimumScale)
                {
                    throw new ArgumentException("Species bake input is invalid at index " + index + ".");
                }

                for (int variant = 0; variant < species.VariantIds.Length; variant++)
                {
                    if (string.IsNullOrWhiteSpace(species.VariantIds[variant]) ||
                        species.VariantWeights[variant] <= 0f)
                    {
                        throw new ArgumentException("Variant bake input is invalid at index " + variant + ".");
                    }
                }
            }
        }

        private static ForestSpeciesBakeInput SelectSpecies(
            ref DeterministicRandom random,
            ForestSpeciesBakeInput[] species)
        {
            float total = 0f;
            for (int index = 0; index < species.Length; index++)
            {
                total += species[index].SpeciesWeight;
            }

            float cursor = random.NextFloat01() * total;
            for (int index = 0; index < species.Length; index++)
            {
                cursor -= species[index].SpeciesWeight;
                if (cursor <= 0f)
                {
                    return species[index];
                }
            }

            return species[species.Length - 1];
        }

        private static string SelectVariant(
            ref DeterministicRandom random,
            in ForestSpeciesBakeInput species)
        {
            float total = 0f;
            for (int index = 0; index < species.VariantWeights.Length; index++)
            {
                total += species.VariantWeights[index];
            }

            float cursor = random.NextFloat01() * total;
            for (int index = 0; index < species.VariantIds.Length; index++)
            {
                cursor -= species.VariantWeights[index];
                if (cursor <= 0f)
                {
                    return species.VariantIds[index];
                }
            }

            return species.VariantIds[species.VariantIds.Length - 1];
        }

        private static Vector2 RandomClusterPoint(
            ref DeterministicRandom random,
            Bounds bounds,
            Vector2 center,
            float radius)
        {
            float angle = random.NextFloat01() * Mathf.PI * 2f;
            float distance = radius * random.NextFloat01() * random.NextFloat01();
            var value = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            float marginX = bounds.size.x * 0.03f;
            float marginZ = bounds.size.z * 0.03f;
            value.x = Mathf.Clamp(value.x, bounds.min.x + marginX, bounds.max.x - marginX);
            value.y = Mathf.Clamp(value.y, bounds.min.z + marginZ, bounds.max.z - marginZ);
            return value;
        }

        private static Vector2 RandomPointInBounds(
            ref DeterministicRandom random,
            Bounds bounds,
            float normalizedMargin)
        {
            float marginX = bounds.size.x * normalizedMargin;
            float marginZ = bounds.size.z * normalizedMargin;
            return new Vector2(
                Mathf.Lerp(bounds.min.x + marginX, bounds.max.x - marginX, random.NextFloat01()),
                Mathf.Lerp(bounds.min.z + marginZ, bounds.max.z - marginZ, random.NextFloat01()));
        }

        private static float NearestDistanceSquared(Vector2 candidate, List<Vector2> accepted)
        {
            if (accepted.Count == 0)
            {
                return float.MaxValue;
            }

            float nearest = float.MaxValue;
            for (int index = 0; index < accepted.Count; index++)
            {
                nearest = Mathf.Min(nearest, (candidate - accepted[index]).sqrMagnitude);
            }

            return nearest;
        }

        private static string CreateInstanceId(
            ForestCellId cellId,
            int generatorVersion,
            int seed,
            int generationSlot)
        {
            string source = cellId.Value + "|" +
                            generatorVersion.ToString(CultureInfo.InvariantCulture) + "|" +
                            seed.ToString(CultureInfo.InvariantCulture) + "|" +
                            generationSlot.ToString(CultureInfo.InvariantCulture);
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
            var suffix = new StringBuilder(24);
            for (int index = 0; index < 10; index++)
            {
                suffix.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
            }

            return "tree." + suffix;
        }

        private struct DeterministicRandom
        {
            private uint state;

            public DeterministicRandom(uint seed)
            {
                state = seed == 0u ? 0x6d2b79f5u : seed;
            }

            public float NextFloat01()
            {
                uint value = NextUInt();
                return (value >> 8) * (1f / 16777216f);
            }

            public int NextInt(int exclusiveMaximum)
            {
                if (exclusiveMaximum <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
                }

                return (int)(NextUInt() % (uint)exclusiveMaximum);
            }

            private uint NextUInt()
            {
                uint value = state;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                state = value;
                return value;
            }
        }
    }
}
