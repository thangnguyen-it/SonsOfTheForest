using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SonsOfTheForest.Core.World;
using UnityEngine;

namespace SonsOfTheForest.Data.Forest
{
    public static class ForestPlacementChecksum
    {
        public static string Compute(
            ForestCellId cellId,
            int schemaVersion,
            int generatorVersion,
            Bounds bounds,
            int seed,
            float densityPerSquareMeter,
            IReadOnlyList<ForestTreePlacementRecord> placements)
        {
            if (placements == null)
            {
                throw new ArgumentNullException(nameof(placements));
            }

            var ordered = new ForestTreePlacementRecord[placements.Count];
            for (int index = 0; index < placements.Count; index++)
            {
                ordered[index] = placements[index];
            }

            Array.Sort(
                ordered,
                (left, right) => string.CompareOrdinal(
                    left.TreeInstanceId.Value,
                    right.TreeInstanceId.Value));

            var canonical = new StringBuilder(256 + ordered.Length * 128);
            canonical.Append(cellId.Value).Append('|')
                .Append(schemaVersion.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(generatorVersion.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(seed.ToString(CultureInfo.InvariantCulture)).Append('|');
            AppendVector(canonical, bounds.center);
            AppendVector(canonical, bounds.size);
            AppendFloat(canonical, densityPerSquareMeter);

            for (int index = 0; index < ordered.Length; index++)
            {
                ForestTreePlacementRecord placement = ordered[index];
                canonical.Append(placement.TreeInstanceId.Value).Append('|')
                    .Append(placement.SpeciesId.Value).Append('|')
                    .Append(placement.VariantId.Value).Append('|');
                AppendVector(canonical, placement.LocalPosition);
                AppendFloat(canonical, placement.YawDegrees);
                AppendFloat(canonical, placement.UniformScale);
            }

            using SHA256 sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
            var result = new StringBuilder(bytes.Length * 2);
            for (int index = 0; index < bytes.Length; index++)
            {
                result.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }

        public static bool IsSha256(string value)
        {
            if (value == null || value.Length != 64)
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AppendVector(StringBuilder builder, Vector3 value)
        {
            AppendFloat(builder, value.x);
            AppendFloat(builder, value.y);
            AppendFloat(builder, value.z);
        }

        private static void AppendFloat(StringBuilder builder, float value)
        {
            builder.Append(BitConverter.SingleToInt32Bits(value).ToString("x8", CultureInfo.InvariantCulture))
                .Append('|');
        }
    }
}
