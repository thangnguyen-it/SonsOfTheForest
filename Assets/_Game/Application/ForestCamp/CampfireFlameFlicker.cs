using UnityEngine;

namespace SonsOfTheForest.Application.ForestCamp
{
    [DisallowMultipleComponent]
    public sealed class CampfireFlameFlicker : MonoBehaviour
    {
        [SerializeField]
        private Light fireLight;

        [SerializeField]
        private Transform[] flameLayers = System.Array.Empty<Transform>();

        [SerializeField, Min(0f)]
        private float baseIntensity = 900f;

        [SerializeField, Range(0f, 1f)]
        private float variation = 0.22f;

        [SerializeField, Min(0.1f)]
        private float frequency = 7f;

        private Vector3[] baseScales;

        private void Awake()
        {
            baseScales = new Vector3[flameLayers.Length];
            for (int index = 0; index < flameLayers.Length; index++)
            {
                baseScales[index] = flameLayers[index] != null
                    ? flameLayers[index].localScale
                    : Vector3.one;
            }
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(Time.time * frequency, 0.37f);
            float factor = 1f + (noise - 0.5f) * 2f * variation;
            if (fireLight != null)
            {
                fireLight.intensity = baseIntensity * factor;
            }

            for (int index = 0; index < flameLayers.Length; index++)
            {
                Transform layer = flameLayers[index];
                if (layer == null)
                {
                    continue;
                }

                float offset = 1f + Mathf.Sin(
                    Time.time * (frequency + index * 0.7f) + index) *
                    variation * 0.35f;
                Vector3 scale = baseScales[index];
                layer.localScale = new Vector3(
                    scale.x / offset,
                    scale.y * offset * factor,
                    scale.z / offset);
            }
        }
    }
}
