using UnityEngine;

namespace SonsOfTheForest.Gameplay.Player
{
    public sealed class PlayerLookModel
    {
        public PlayerLookState Step(
            PlayerLookState previousState,
            LookIntent intent,
            PlayerLookConfig config,
            float deltaTime)
        {
            float yaw = WrapYaw(SanitizeFinite(previousState.Yaw));
            float minimumPitch = SanitizeFinite(config.MinPitch);
            float maximumPitch = SanitizeFinite(config.MaxPitch);

            if (minimumPitch > maximumPitch)
            {
                minimumPitch = 0f;
                maximumPitch = 0f;
            }

            float pitch = Mathf.Clamp(
                SanitizeFinite(previousState.Pitch),
                minimumPitch,
                maximumPitch);
            Vector2 input = SanitizeInput(intent.Value);
            float scale = ResolveScale(intent.InputKind, config, deltaTime);
            float yawDelta = SafeProduct(input.x, scale);
            float pitchDirection = config.InvertY ? 1f : -1f;
            float pitchDelta = SafeProduct(
                input.y * pitchDirection,
                scale);

            yaw = WrapYaw(SafeSum(yaw, yawDelta));
            pitch = Mathf.Clamp(
                SafeSum(pitch, pitchDelta),
                minimumPitch,
                maximumPitch);

            return new PlayerLookState(yaw, pitch);
        }

        private static float ResolveScale(
            LookInputKind inputKind,
            PlayerLookConfig config,
            float deltaTime)
        {
            switch (inputKind)
            {
                case LookInputKind.Delta:
                    return SanitizeNonnegative(config.MouseSensitivity);

                case LookInputKind.Rate:
                    float safeDeltaTime =
                        IsFinite(deltaTime) && deltaTime > 0f
                            ? deltaTime
                            : 0f;
                    return SafeProduct(
                        SanitizeNonnegative(config.StickSensitivity),
                        safeDeltaTime);

                default:
                    return 0f;
            }
        }

        private static Vector2 SanitizeInput(Vector2 input)
        {
            return IsFinite(input.x) && IsFinite(input.y)
                ? input
                : Vector2.zero;
        }

        private static float WrapYaw(float yaw)
        {
            double wrapped = ((double)yaw + 180d) % 360d;

            if (wrapped < 0d)
            {
                wrapped += 360d;
            }

            return (float)(wrapped - 180d);
        }

        private static float SanitizeNonnegative(float value)
        {
            return IsFinite(value) && value >= 0f
                ? value
                : 0f;
        }

        private static float SanitizeFinite(float value)
        {
            return IsFinite(value) ? value : 0f;
        }

        private static float SafeProduct(float left, float right)
        {
            float product = left * right;

            return IsFinite(product) ? product : 0f;
        }

        private static float SafeSum(float left, float right)
        {
            float sum = left + right;

            return IsFinite(sum) ? sum : left;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
