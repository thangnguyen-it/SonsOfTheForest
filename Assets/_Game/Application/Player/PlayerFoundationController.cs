using System;
using System.Linq;
using SonsOfTheForest.Core;
using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Application.Player
{
    [DefaultExecutionOrder(50)]
    public sealed class PlayerFoundationController : MonoBehaviour,
        IPlayerMovementStateReader
    {
        [SerializeField]
        private PlayerFoundationSettings settings;

        [SerializeField]
        private MonoBehaviour intentSourceComponent;

        [SerializeField]
        private MonoBehaviour motionDriverComponent;

        [SerializeField]
        private MonoBehaviour lookDriverComponent;

        private readonly PlayerMovementModel movementModel = new();
        private readonly PlayerLookModel lookModel = new();
        private IPlayerIntentSource intentSource;
        private IPlayerMotionDriver motionDriver;
        private IPlayerLookDriver lookDriver;

        public PlayerMovementState CurrentMovementState { get; private set; }

        public PlayerLookState CurrentLookState { get; private set; } =
            PlayerLookState.Identity;

        public PlayerFoundationSettings Settings => settings;

        private void Awake()
        {
            intentSource = ResolveRequired<IPlayerIntentSource>(
                intentSourceComponent,
                nameof(intentSourceComponent));
            motionDriver = ResolveRequired<IPlayerMotionDriver>(
                motionDriverComponent,
                nameof(motionDriverComponent));
            lookDriver = ResolveRequired<IPlayerLookDriver>(
                lookDriverComponent,
                nameof(lookDriverComponent));

            if (settings == null)
            {
                throw new InvalidOperationException(
                    "PlayerFoundationController requires foundation settings.");
            }

            var report = new ValidationReport();
            settings.Validate(report);
            if (!report.IsValid)
            {
                string details = string.Join(
                    Environment.NewLine,
                    report.Issues.Select(issue => issue.ToString()));
                throw new InvalidOperationException(
                    "Player foundation settings are invalid:" +
                    Environment.NewLine +
                    details);
            }

            Vector3 forward = SanitizeDirection(lookDriver.PlanarForward);
            CurrentMovementState = new PlayerMovementState(
                PlayerLocomotionMode.Airborne,
                PlayerStance.Standing,
                false,
                false,
                false,
                Vector3.zero,
                0f,
                forward);
            lookDriver.ApplyLook(CurrentLookState);
        }

        private void Update()
        {
            CurrentLookState = lookModel.Step(
                CurrentLookState,
                intentSource.ConsumeLookIntent(),
                settings.LookConfig,
                Time.deltaTime);
            lookDriver.ApplyLook(CurrentLookState);
        }

        private void FixedUpdate()
        {
            PlayerMovementConfig config = settings.MovementConfig;
            PlayerGroundInfo groundInfo = motionDriver.SampleGround(
                config.SlopeLimitDegrees);
            bool canStand = motionDriver.CanOccupyStance(
                PlayerStance.Standing,
                config);
            var constraints = new MovementConstraints(
                canSprint: true,
                canJump: true,
                canStand: canStand,
                speedMultiplier: 1f);
            var reconciledState = new PlayerMovementState(
                CurrentMovementState.LocomotionMode,
                CurrentMovementState.Stance,
                groundInfo.IsGrounded && groundInfo.IsWalkable,
                CurrentMovementState.IsSprinting,
                CurrentMovementState.IsCrouching,
                motionDriver.CurrentPlanarVelocity,
                motionDriver.CurrentVerticalVelocity,
                CurrentMovementState.FacingForward);

            CurrentMovementState = movementModel.Step(
                reconciledState,
                intentSource.ConsumeMovementIntent(),
                constraints,
                config,
                groundInfo,
                lookDriver.PlanarForward,
                lookDriver.PlanarRight,
                Time.fixedDeltaTime);
            motionDriver.ApplyMotion(
                CurrentMovementState,
                groundInfo,
                config);
        }

        private static T ResolveRequired<T>(
            MonoBehaviour component,
            string fieldName)
            where T : class
        {
            if (component is T resolved)
            {
                return resolved;
            }

            throw new InvalidOperationException(
                $"{fieldName} must reference a component implementing {typeof(T).Name}.");
        }

        private static Vector3 SanitizeDirection(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > 0.000001f
                ? value.normalized
                : Vector3.forward;
        }
    }
}
