using System;
using System.Collections.Generic;
using SonsOfTheForest.Data.Forest;

namespace SonsOfTheForest.Infrastructure.Forest
{
    public sealed class ForestTreeDeltaStore
    {
        private readonly Dictionary<ForestTreeDeltaKey, ForestTreeStateDelta> deltas = new();

        public int Count => deltas.Count;

        public bool TryGet(ForestTreeDeltaKey key, out ForestTreeStateDelta delta) =>
            deltas.TryGetValue(key, out delta);

        public void Set(ForestTreeDeltaKey key, in ForestTreeStateDelta delta)
        {
            if (!key.IsValid)
            {
                throw new ArgumentException("A valid cell/tree key is required.", nameof(key));
            }

            if (!delta.TryValidate(out string reason))
            {
                throw new ArgumentException(reason, nameof(delta));
            }

            if (!delta.IsChanged)
            {
                deltas.Remove(key);
                return;
            }

            deltas[key] = delta;
        }

        public bool Remove(ForestTreeDeltaKey key) => deltas.Remove(key);

        public void Clear() => deltas.Clear();
    }
}
