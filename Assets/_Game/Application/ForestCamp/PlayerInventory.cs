using System;
using SonsOfTheForest.Core;
using SonsOfTheForest.Gameplay.Inventory;
using SonsOfTheForest.Gameplay.Items;
using UnityEngine;

namespace SonsOfTheForest.Application.ForestCamp
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour, IInventoryWriter
    {
        [SerializeField, Min(1)]
        private int maxPerItem = 50;

        private InventoryModel model;

        public event Action<ItemId, int> CountChanged;

        public int MaxPerItem => EnsureModel().MaxPerItem;

        private void Awake()
        {
            EnsureModel();
        }

        public int GetCount(ItemId itemId)
        {
            return EnsureModel().GetCount(itemId);
        }

        public bool Contains(ItemId itemId, int quantity)
        {
            return EnsureModel().Contains(itemId, quantity);
        }

        public GameResult<int> TryAdd(ItemStack stack)
        {
            GameResult<int> result = EnsureModel().TryAdd(stack);
            if (result.Succeeded)
            {
                CountChanged?.Invoke(stack.ItemId, GetCount(stack.ItemId));
            }

            return result;
        }

        public GameResult TryRemove(ItemStack stack)
        {
            GameResult result = EnsureModel().TryRemove(stack);
            if (result.Succeeded)
            {
                CountChanged?.Invoke(stack.ItemId, GetCount(stack.ItemId));
            }

            return result;
        }

        private InventoryModel EnsureModel()
        {
            model ??= new InventoryModel(Mathf.Max(1, maxPerItem));
            return model;
        }
    }
}
