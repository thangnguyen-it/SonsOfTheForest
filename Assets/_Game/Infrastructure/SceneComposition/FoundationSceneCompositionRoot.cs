using System;
using System.Collections.Generic;
using System.Linq;
using SonsOfTheForest.Application.Player;
using SonsOfTheForest.Core;
using SonsOfTheForest.Infrastructure.SceneBootstrap;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.SceneComposition
{
    [DefaultExecutionOrder(-1000)]
    public sealed class FoundationSceneCompositionRoot : MonoBehaviour,
        ISceneCompositionRoot,
        ISceneReadinessValidator,
        IValidatable
    {
        private static readonly StableStringId FoundationSceneId =
            new("scene.foundation");

        private static readonly ISceneBootstrapStep[] NoBootstrapSteps =
            Array.Empty<ISceneBootstrapStep>();

        [SerializeField]
        private Transform playerContainer;

        [SerializeField]
        private Transform worldContainer;

        [SerializeField]
        private PlayerFoundationController localPlayer;

        [SerializeField]
        private Camera playerCamera;

        [SerializeField]
        private Camera fallbackCamera;

        [SerializeField]
        private GameObject playground;

        [SerializeField]
        private Collider playableSurface;

        public StableStringId SceneId => FoundationSceneId;

        public Transform PlayerContainer => playerContainer;

        public Transform WorldContainer => worldContainer;

        public PlayerFoundationController LocalPlayer => localPlayer;

        public Camera PlayerCamera => playerCamera;

        public Camera FallbackCamera => fallbackCamera;

        public GameObject Playground => playground;

        public Collider PlayableSurface => playableSurface;

        public IEnumerable<ISceneBootstrapStep> GetBootstrapSteps()
        {
            return NoBootstrapSteps;
        }

        public void ValidateScene(
            in SceneBootstrapContext context,
            ValidationReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (context.SceneId != SceneId)
            {
                report.AddError(
                    "SCENE.ID_MISMATCH",
                    $"Expected scene id '{SceneId}' but received '{context.SceneId}'.");
            }

            Validate(report);
        }

        public void Validate(ValidationReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            RequireReference(playerContainer, "SCENE.PLAYER_CONTAINER_MISSING",
                "The foundation scene requires its Player container.", report);
            RequireReference(worldContainer, "SCENE.WORLD_CONTAINER_MISSING",
                "The foundation scene requires its World container.", report);
            RequireReference(localPlayer, "SCENE.LOCAL_PLAYER_MISSING",
                "The foundation scene requires one local player.", report);
            RequireReference(playerCamera, "SCENE.PLAYER_CAMERA_MISSING",
                "The local player requires an explicitly wired camera.", report);
            RequireReference(fallbackCamera, "SCENE.FALLBACK_CAMERA_MISSING",
                "The original scene camera must remain wired as a disabled fallback.",
                report);
            RequireReference(playground, "SCENE.PLAYGROUND_MISSING",
                "The foundation scene requires a replaceable playground fixture.",
                report);
            RequireReference(playableSurface, "SCENE.SURFACE_MISSING",
                "The playground requires an explicitly wired playable surface.",
                report);

            ValidateSceneMembership(report);
            ValidateHierarchy(report);
            ValidateRuntimeState(report);

            report.AddInfo(
                "SCENE.PLAYGROUND_PROVISIONAL",
                "The foundation playground and spawn are engineering fixtures, not measured Sons of the Forest world fidelity.");
        }

        private void Awake()
        {
            var report = new ValidationReport();
            Validate(report);
            if (report.IsValid)
            {
                return;
            }

            string details = string.Join(
                Environment.NewLine,
                report.Issues.Select(issue => issue.ToString()));
            throw new InvalidOperationException(
                "Foundation scene composition is invalid:" +
                Environment.NewLine +
                details);
        }

        private void ValidateSceneMembership(ValidationReport report)
        {
            if (playerContainer != null)
            {
                RequireSameScene(playerContainer.gameObject,
                    "SCENE.PLAYER_CONTAINER_WRONG_SCENE", report);
            }

            if (worldContainer != null)
            {
                RequireSameScene(worldContainer.gameObject,
                    "SCENE.WORLD_CONTAINER_WRONG_SCENE", report);
            }

            if (localPlayer != null)
            {
                RequireSameScene(localPlayer.gameObject,
                    "SCENE.LOCAL_PLAYER_WRONG_SCENE", report);
            }

            if (playerCamera != null)
            {
                RequireSameScene(playerCamera.gameObject,
                    "SCENE.PLAYER_CAMERA_WRONG_SCENE", report);
            }

            if (fallbackCamera != null)
            {
                RequireSameScene(fallbackCamera.gameObject,
                    "SCENE.FALLBACK_CAMERA_WRONG_SCENE", report);
            }

            if (playground != null)
            {
                RequireSameScene(playground,
                    "SCENE.PLAYGROUND_WRONG_SCENE", report);
            }

            if (playableSurface != null)
            {
                RequireSameScene(playableSurface.gameObject,
                    "SCENE.SURFACE_WRONG_SCENE", report);
            }
        }

        private void ValidateHierarchy(ValidationReport report)
        {
            if (localPlayer != null &&
                playerContainer != null &&
                localPlayer.transform.parent != playerContainer)
            {
                report.AddError(
                    "SCENE.LOCAL_PLAYER_PARENT",
                    "The local player must be a direct child of the Player container.");
            }

            if (playground != null &&
                worldContainer != null &&
                playground.transform.parent != worldContainer)
            {
                report.AddError(
                    "SCENE.PLAYGROUND_PARENT",
                    "The playground must be a direct child of the World container.");
            }

            if (playerCamera != null &&
                localPlayer != null &&
                !playerCamera.transform.IsChildOf(localPlayer.transform))
            {
                report.AddError(
                    "SCENE.PLAYER_CAMERA_PARENT",
                    "The active camera must belong to the local player prefab.");
            }

            if (playableSurface != null &&
                playground != null &&
                !playableSurface.transform.IsChildOf(playground.transform))
            {
                report.AddError(
                    "SCENE.SURFACE_PARENT",
                    "The playable surface must belong to the playground prefab.");
            }
        }

        private void ValidateRuntimeState(ValidationReport report)
        {
            if (localPlayer != null && !localPlayer.gameObject.activeInHierarchy)
            {
                report.AddError(
                    "SCENE.LOCAL_PLAYER_INACTIVE",
                    "The local player must be active.");
            }

            if (playerCamera != null && !playerCamera.isActiveAndEnabled)
            {
                report.AddError(
                    "SCENE.PLAYER_CAMERA_INACTIVE",
                    "The local player camera must be active and enabled.");
            }

            if (fallbackCamera != null && fallbackCamera.isActiveAndEnabled)
            {
                report.AddError(
                    "SCENE.FALLBACK_CAMERA_ACTIVE",
                    "The fallback scene camera must be disabled while the local player camera is active.");
            }

            if (playableSurface != null &&
                (!playableSurface.enabled ||
                 !playableSurface.gameObject.activeInHierarchy ||
                 playableSurface.isTrigger))
            {
                report.AddError(
                    "SCENE.SURFACE_NOT_SOLID",
                    "The playable surface must be active, enabled, and non-trigger.");
            }

            int activeCameraCount = 0;
            int activeListenerCount = 0;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                activeCameraCount += root
                    .GetComponentsInChildren<Camera>(true)
                    .Count(camera => camera.isActiveAndEnabled);
                activeListenerCount += root
                    .GetComponentsInChildren<AudioListener>(true)
                    .Count(listener => listener.isActiveAndEnabled);
            }

            if (activeCameraCount != 1)
            {
                report.AddError(
                    "SCENE.ACTIVE_CAMERA_COUNT",
                    $"Expected exactly one active camera but found {activeCameraCount}.");
            }

            if (activeListenerCount != 1)
            {
                report.AddError(
                    "SCENE.ACTIVE_LISTENER_COUNT",
                    $"Expected exactly one active audio listener but found {activeListenerCount}.");
            }

            if (playerCamera != null)
            {
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener == null || !listener.isActiveAndEnabled)
                {
                    report.AddError(
                        "SCENE.PLAYER_LISTENER_MISSING",
                        "The active local player camera requires its audio listener.");
                }
            }
        }

        private void RequireSameScene(
            GameObject target,
            string code,
            ValidationReport report)
        {
            if (target.scene != gameObject.scene)
            {
                report.AddError(
                    code,
                    $"'{target.name}' does not belong to '{gameObject.scene.name}'.");
            }
        }

        private static void RequireReference(
            UnityEngine.Object value,
            string code,
            string message,
            ValidationReport report)
        {
            if (value == null)
            {
                report.AddError(code, message);
            }
        }
    }
}
