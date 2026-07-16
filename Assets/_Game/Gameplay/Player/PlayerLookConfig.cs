using System;
using SonsOfTheForest.Core;

namespace SonsOfTheForest.Gameplay.Player
{
    public readonly struct PlayerLookConfig : IValidatable
    {
        public PlayerLookConfig(
            float mouseSensitivity,
            float stickSensitivity,
            float minPitch,
            float maxPitch,
            bool invertY)
        {
            MouseSensitivity = mouseSensitivity;
            StickSensitivity = stickSensitivity;
            MinPitch = minPitch;
            MaxPitch = maxPitch;
            InvertY = invertY;
        }

        public float MouseSensitivity { get; }

        public float StickSensitivity { get; }

        public float MinPitch { get; }

        public float MaxPitch { get; }

        public bool InvertY { get; }

        public static PlayerLookConfig Default =>
            new(
                mouseSensitivity: 0.1f,
                stickSensitivity: 120f,
                minPitch: -89f,
                maxPitch: 89f,
                invertY: false);

        public void Validate(ValidationReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            ValidateSensitivity(
                report,
                MouseSensitivity,
                "player.look.mouseSensitivity");
            ValidateSensitivity(
                report,
                StickSensitivity,
                "player.look.stickSensitivity");

            if (!IsFinite(MinPitch) ||
                !IsFinite(MaxPitch) ||
                MinPitch >= MaxPitch)
            {
                report.AddError(
                    "player.look.pitchOrder",
                    "Minimum pitch must be less than maximum pitch.");
            }

            if (!IsFinite(MinPitch) || MinPitch < -89f)
            {
                report.AddError(
                    "player.look.minPitch",
                    "Minimum pitch must be at least -89 degrees.");
            }

            if (!IsFinite(MaxPitch) || MaxPitch > 89f)
            {
                report.AddError(
                    "player.look.maxPitch",
                    "Maximum pitch must be at most 89 degrees.");
            }
        }

        private static void ValidateSensitivity(
            ValidationReport report,
            float value,
            string code)
        {
            if (!IsFinite(value) || value < 0f)
            {
                report.AddError(
                    code,
                    "Sensitivity must be finite and nonnegative.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
