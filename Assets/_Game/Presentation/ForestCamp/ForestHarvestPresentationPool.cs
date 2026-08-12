using System.Collections.Generic;
using SonsOfTheForest.Infrastructure.Forest;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SonsOfTheForest.Presentation.ForestCamp
{
    [DisallowMultipleComponent]
    public sealed class ForestHarvestPresentationPool : MonoBehaviour
    {
        [SerializeField] private ForestCellInteractionCoordinator coordinator;
        [SerializeField] private Transform observer;
        [SerializeField] private ParticleSystem[] chipPool = System.Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] impactPool = System.Array.Empty<ParticleSystem>();
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] chopClips = System.Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] impactClips = System.Array.Empty<AudioClip>();

        private readonly HashSet<ForestInteractiveTree> registered = new();
        private int nextChip;
        private int nextImpact;
        private float cameraImpulse;
        private float rumbleRemaining;
        private Quaternion appliedCameraOffset = Quaternion.identity;

        public bool HasFinalChopAudio => chopClips != null && chopClips.Length > 0;
        public bool HasFinalImpactAudio => impactClips != null && impactClips.Length > 0;

        public void SetObserver(Transform value) => observer = value;

        private void OnEnable()
        {
            if (coordinator != null)
            {
                coordinator.TreePromoted += RegisterTree;
            }
        }

        private void OnDisable()
        {
            if (coordinator != null)
            {
                coordinator.TreePromoted -= RegisterTree;
            }

            foreach (ForestInteractiveTree tree in registered)
            {
                if (tree == null) continue;
                tree.Chopped -= HandleChopped;
                tree.Impacted -= HandleImpact;
            }

            registered.Clear();
            StopRumble();
        }

        private void LateUpdate()
        {
            if (observer != null)
            {
                observer.localRotation = observer.localRotation * Quaternion.Inverse(appliedCameraOffset);
                cameraImpulse = Mathf.MoveTowards(cameraImpulse, 0f, Time.deltaTime * 5f);
                float angle = Mathf.Sin(Time.unscaledTime * 46f) * cameraImpulse;
                appliedCameraOffset = Quaternion.Euler(angle, -angle * 0.55f, 0f);
                observer.localRotation *= appliedCameraOffset;
            }

            if (rumbleRemaining > 0f)
            {
                rumbleRemaining -= Time.unscaledDeltaTime;
                if (rumbleRemaining <= 0f) StopRumble();
            }
        }

        private void RegisterTree(ForestInteractiveTree tree)
        {
            if (tree == null || !registered.Add(tree))
            {
                return;
            }

            tree.Chopped += HandleChopped;
            tree.Impacted += HandleImpact;
        }

        private void HandleChopped(ForestInteractiveTree tree, TreeDamageEvent damage, int stage)
        {
            PlayParticle(chipPool, ref nextChip, damage.HitPosition,
                damage.HitNormal.sqrMagnitude > 0.001f ? damage.HitNormal : -damage.HitDirection);
            PlayClip(chopClips, tree.TreeInstanceId.Value.GetHashCode() + stage, 0.8f);
            cameraImpulse = Mathf.Max(cameraImpulse, stage >= 3 ? 0.8f : 0.35f);
            StartRumble(stage >= 3 ? 0.34f : 0.18f, stage >= 3 ? 0.22f : 0.12f);
        }

        private void HandleImpact(ForestInteractiveTree tree, Vector3 position, float magnitude)
        {
            PlayParticle(impactPool, ref nextImpact, position, Vector3.up);
            PlayClip(impactClips, tree.TreeInstanceId.Value.GetHashCode(), Mathf.Clamp01(magnitude / 7f));
            float distance = observer != null ? Vector3.Distance(observer.position, position) : 100f;
            cameraImpulse = Mathf.Max(cameraImpulse,
                Mathf.Clamp01(1f - distance / 20f) * Mathf.Clamp(magnitude * 0.18f, 0.4f, 1.8f));
            StartRumble(Mathf.Clamp01(magnitude / 9f) * 0.45f, 0.30f);
        }

        private static void PlayParticle(
            ParticleSystem[] pool, ref int cursor, Vector3 position, Vector3 normal)
        {
            if (pool == null || pool.Length == 0) return;
            ParticleSystem particle = pool[cursor++ % pool.Length];
            if (particle == null) return;
            particle.transform.SetPositionAndRotation(position,
                Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up));
            particle.Play(true);
        }

        private void PlayClip(AudioClip[] clips, int seed, float volume)
        {
            if (audioSource == null || clips == null || clips.Length == 0) return;
            int index = Mathf.Abs(seed == int.MinValue ? 0 : seed) % clips.Length;
            if (clips[index] != null) audioSource.PlayOneShot(clips[index], volume);
        }

        private void StartRumble(float strength, float duration)
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null) return;
            gamepad.SetMotorSpeeds(strength * 0.55f, strength);
            rumbleRemaining = Mathf.Max(rumbleRemaining, duration);
        }

        private void StopRumble()
        {
            if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
            rumbleRemaining = 0f;
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            ForestCellInteractionCoordinator owner, Transform cameraTransform,
            ParticleSystem[] chips, ParticleSystem[] impacts, AudioSource source)
        {
            coordinator = owner;
            observer = cameraTransform;
            chipPool = chips ?? System.Array.Empty<ParticleSystem>();
            impactPool = impacts ?? System.Array.Empty<ParticleSystem>();
            audioSource = source;
            chopClips = System.Array.Empty<AudioClip>();
            impactClips = System.Array.Empty<AudioClip>();
        }
#endif
    }
}
