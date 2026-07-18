using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SonsOfTheForest.Presentation.Player
{
    [DisallowMultipleComponent]
    public sealed class LocalPlayerVisualController : MonoBehaviour
    {
        [SerializeField]
        private SkinnedMeshRenderer fullBodyRenderer;

        [SerializeField]
        private SkinnedMeshRenderer localBodyRenderer;

        [SerializeField]
        private SkinnedMeshRenderer fullBodyShadowRenderer;

        private void Awake()
        {
            if (fullBodyRenderer == null ||
                localBodyRenderer == null ||
                fullBodyShadowRenderer == null)
            {
                throw new InvalidOperationException(
                    "LocalPlayerVisualController requires all three renderer modes.");
            }
        }

        private void OnEnable()
        {
            fullBodyRenderer.enabled = false;
            localBodyRenderer.enabled = true;
            localBodyRenderer.shadowCastingMode = ShadowCastingMode.Off;
            fullBodyShadowRenderer.enabled = true;
            fullBodyShadowRenderer.shadowCastingMode =
                ShadowCastingMode.ShadowsOnly;
        }
    }
}
