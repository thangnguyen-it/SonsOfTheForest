using System;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Forest
{
    [DisallowMultipleComponent]
    public sealed class ForestFellingVisual : MonoBehaviour
    {
        [SerializeField] private Transform upperRoot;
        [SerializeField] private Transform stumpRoot;
        [SerializeField] private GameObject intactSeam;
        [SerializeField] private GameObject[] notchStages = Array.Empty<GameObject>();
        [SerializeField] private GameObject readyToFall;
        [SerializeField] private GameObject upperCutCap;
        [SerializeField] private GameObject stumpCutCap;

        public Transform UpperRoot => upperRoot;
        public Transform StumpRoot => stumpRoot;
        public int NotchStageCount => notchStages.Length;
        public GameObject ReadyToFall => readyToFall;

        public bool TryValidate(out string reason)
        {
            if (upperRoot == null || stumpRoot == null || intactSeam == null ||
                notchStages == null || notchStages.Length == 0 || upperCutCap == null ||
                stumpCutCap == null)
            {
                reason = "Felling visual references are incomplete.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void SetNotch(int stage, Vector3 localDirection)
        {
            intactSeam.SetActive(stage <= 0);
            float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            for (int index = 0; index < notchStages.Length; index++)
            {
                bool active = index == Mathf.Clamp(stage - 1, 0, notchStages.Length - 1) && stage > 0;
                notchStages[index].SetActive(active);
                if (active)
                {
                    notchStages[index].transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                }
            }
            if (readyToFall != null)
            {
                readyToFall.SetActive(false);
            }
        }

        public void SetReadyToFall(Vector3 localDirection)
        {
            intactSeam.SetActive(false);
            for (int index = 0; index < notchStages.Length; index++)
            {
                notchStages[index].SetActive(false);
            }
            float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            if (readyToFall != null)
            {
                readyToFall.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                readyToFall.SetActive(true);
            }
            else if (notchStages.Length > 0)
            {
                GameObject fallback = notchStages[notchStages.Length - 1];
                fallback.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                fallback.SetActive(true);
            }
        }

        public Transform DetachStump(Transform outputRoot)
        {
            intactSeam.SetActive(false);
            for (int index = 0; index < notchStages.Length; index++)
            {
                notchStages[index].SetActive(false);
            }
            if (readyToFall != null)
            {
                readyToFall.SetActive(false);
            }

            upperCutCap.SetActive(true);
            stumpCutCap.SetActive(true);
            stumpRoot.SetParent(outputRoot, true);
            upperRoot.localPosition = Vector3.zero;
            return stumpRoot;
        }

        public void SetUpperVisible(bool visible) => upperRoot.gameObject.SetActive(visible);

#if UNITY_EDITOR
        public void EditorConfigure(
            Transform upper, Transform stump, GameObject seam, GameObject[] stages,
            GameObject configuredReadyToFall, GameObject upperCap, GameObject stumpCap)
        {
            upperRoot = upper;
            stumpRoot = stump;
            intactSeam = seam;
            notchStages = stages ?? Array.Empty<GameObject>();
            readyToFall = configuredReadyToFall;
            upperCutCap = upperCap;
            stumpCutCap = stumpCap;
        }
#endif
    }
}
