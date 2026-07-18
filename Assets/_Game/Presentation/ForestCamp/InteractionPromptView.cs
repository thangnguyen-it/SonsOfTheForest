using System.Collections;
using SonsOfTheForest.Application.ForestCamp;
using SonsOfTheForest.Gameplay.Interaction;
using SonsOfTheForest.Gameplay.Items;
using UnityEngine;
using UnityEngine.UI;

namespace SonsOfTheForest.Presentation.ForestCamp
{
    [DisallowMultipleComponent]
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField]
        private GameObject promptRoot;

        [SerializeField]
        private Text promptText;

        [SerializeField]
        private Text inventoryText;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private Color availableColor = Color.white;

        [SerializeField]
        private Color disabledColor = new(0.65f, 0.65f, 0.65f, 1f);

        [SerializeField, Min(0.1f)]
        private float statusDuration = 2.5f;

        private PlayerInventory inventory;
        private Coroutine clearStatusRoutine;

        private void OnDestroy()
        {
            BindInventory(null);
        }

        public void BindInventory(PlayerInventory value)
        {
            if (inventory != null)
            {
                inventory.CountChanged -= OnInventoryChanged;
            }

            inventory = value;
            if (inventory != null)
            {
                inventory.CountChanged += OnInventoryChanged;
            }

            RefreshInventory();
        }

        public void SetPrompt(InteractionPrompt prompt)
        {
            bool visible = prompt.Availability != InteractionAvailability.Hidden &&
                           prompt.HasText;
            if (promptRoot != null)
            {
                promptRoot.SetActive(visible);
            }

            if (promptText == null)
            {
                return;
            }

            promptText.text = visible
                ? $"{GetInputLabel(prompt.InputKind)}  {prompt.Text}"
                : string.Empty;
            promptText.color = prompt.Availability ==
                               InteractionAvailability.Available
                ? availableColor
                : disabledColor;
        }

        public void ShowResult(InteractionResult result)
        {
            if (!result.Handled || statusText == null)
            {
                return;
            }

            statusText.text = result.Result.Succeeded
                ? "Done"
                : result.Result.Error;
            if (clearStatusRoutine != null)
            {
                StopCoroutine(clearStatusRoutine);
            }

            clearStatusRoutine = StartCoroutine(ClearStatusAfterDelay());
        }

        private void OnInventoryChanged(ItemId itemId, int count)
        {
            RefreshInventory();
        }

        private void RefreshInventory()
        {
            if (inventoryText == null)
            {
                return;
            }

            int sticks = inventory?.GetCount(ForestCampItemIds.Stick) ?? 0;
            int stones = inventory?.GetCount(ForestCampItemIds.Stone) ?? 0;
            inventoryText.text = $"STICKS  {sticks:00}     STONES  {stones:00}";
        }

        private IEnumerator ClearStatusAfterDelay()
        {
            yield return new WaitForSeconds(statusDuration);
            if (statusText != null)
            {
                statusText.text = string.Empty;
            }

            clearStatusRoutine = null;
        }

        private static string GetInputLabel(InteractionInputKind kind)
        {
            return kind == InteractionInputKind.Hold ? "[HOLD E]" : "[E]";
        }
    }
}
