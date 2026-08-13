using System;
using UnityEngine;

namespace SonsOfTheForest.Presentation.ForestCamp
{
    [DisallowMultipleComponent]
    public sealed class AxeViewmodelAnimator : MonoBehaviour
    {
        public const string IdleClip = "Axe_Idle";
        public const string EquipClip = "Axe_Equip";
        public const string UnequipClip = "Axe_Unequip";
        public const string ChopLeftClip = "Axe_Chop_Left";
        public const string ChopRightClip = "Axe_Chop_Right";
        public const string ChopHeavyClip = "Axe_Chop_Heavy";
        public const string RecoveryClip = "Axe_Recovery";

        [SerializeField] private Animation animationPlayer;
        [SerializeField] private Transform bladeBase;
        [SerializeField] private Transform bladeTip;
        [SerializeField] private Transform leftGrip;
        [SerializeField] private Transform rightGrip;

        public Transform BladeBase => bladeBase;
        public Transform BladeTip => bladeTip;
        public Transform LeftGrip => leftGrip;
        public Transform RightGrip => rightGrip;

        private void Awake()
        {
            if (!TryValidate(out string reason))
            {
                throw new InvalidOperationException(reason);
            }
        }

        public bool TryValidate(out string reason)
        {
            if (animationPlayer == null || bladeBase == null || bladeTip == null ||
                leftGrip == null || rightGrip == null)
            {
                reason = "Axe viewmodel animation and grip/contact anchors are incomplete.";
                return false;
            }

            string[] required =
            {
                IdleClip, EquipClip, UnequipClip, ChopLeftClip, ChopRightClip,
                ChopHeavyClip, RecoveryClip
            };
            for (int index = 0; index < required.Length; index++)
            {
                if (animationPlayer.GetClip(required[index]) == null)
                {
                    reason = "Axe viewmodel is missing authored clip " + required[index] + ".";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public float Play(string clip, bool crossFade = true)
        {
            AnimationClip animationClip = animationPlayer.GetClip(clip);
            if (animationClip == null)
            {
                throw new ArgumentException("Unknown axe viewmodel clip: " + clip, nameof(clip));
            }

            if (crossFade && animationPlayer.isPlaying)
            {
                animationPlayer.CrossFade(clip, 0.06f, PlayMode.StopAll);
            }
            else
            {
                animationPlayer.Play(clip, PlayMode.StopAll);
            }

            return animationClip.length;
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            Animation player, Transform configuredBladeBase, Transform configuredBladeTip,
            Transform configuredLeftGrip, Transform configuredRightGrip)
        {
            animationPlayer = player;
            bladeBase = configuredBladeBase;
            bladeTip = configuredBladeTip;
            leftGrip = configuredLeftGrip;
            rightGrip = configuredRightGrip;
        }
#endif
    }
}
