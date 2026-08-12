using System;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Forest;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SonsOfTheForest.Presentation.ForestCamp
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerAxeHarvestController : MonoBehaviour
    {
        public enum AxeMotionState
        {
            Unequipped,
            Equipping,
            Idle,
            SwingLeft,
            SwingRight,
            HitRecoil,
            MissRecovery,
            HeavyFinal,
            Unequipping
        }

        private const string PlayerMapName = "Player";
        private const string AttackActionName = "Attack";
        private const string EquipActionName = "Previous";
        private const string UnequipActionName = "Next";

        [SerializeField] private Transform handsAnchor;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private GameObject axePrefab;
        [SerializeField, Min(0.1f)] private float damagePerContact = 22f;
        [SerializeField, Min(0.1f)] private float swingDuration = 0.78f;
        [SerializeField, Range(0f, 1f)] private float hitWindowOpen = 0.34f;
        [SerializeField, Range(0f, 1f)] private float hitWindowClose = 0.58f;
        [SerializeField, Min(0.01f)] private float bladeRadius = 0.07f;
        [SerializeField] private LayerMask hitMask = ~0;

        private readonly Collider[] contacts = new Collider[12];
        private PlayerInput playerInput;
        private InputAction attackAction;
        private InputAction equipAction;
        private InputAction unequipAction;
        private GameObject axeInstance;
        private Transform bladeBase;
        private Transform bladeTip;
        private AxeMotionState motionState = AxeMotionState.Unequipped;
        private float stateElapsed;
        private bool swingLeft;
        private bool damageCommitted;
        private int swingSequence;
        private bool subscribed;

        public AxeMotionState MotionState => motionState;
        public bool IsEquipped => motionState != AxeMotionState.Unequipped &&
                                  motionState != AxeMotionState.Unequipping;
        public bool IsHitWindowOpen
        {
            get
            {
                if (motionState != AxeMotionState.SwingLeft && motionState != AxeMotionState.SwingRight)
                {
                    return false;
                }

                float normalized = stateElapsed / Mathf.Max(0.01f, swingDuration);
                return normalized >= hitWindowOpen && normalized <= hitWindowClose;
            }
        }

        private void Awake()
        {
            if (handsAnchor == null || viewCamera == null || axePrefab == null ||
                hitWindowOpen >= hitWindowClose)
            {
                throw new InvalidOperationException("Axe harvest controller references or timing are invalid.");
            }

            playerInput = GetComponent<PlayerInput>();
            InputActionMap map = playerInput.actions?.FindActionMap(PlayerMapName, false);
            attackAction = map?.FindAction(AttackActionName, false);
            equipAction = map?.FindAction(EquipActionName, false);
            unequipAction = map?.FindAction(UnequipActionName, false);
            if (attackAction == null || equipAction == null || unequipAction == null)
            {
                throw new InvalidOperationException("Player input requires Attack, Previous and Next actions.");
            }

            axeInstance = Instantiate(axePrefab, handsAnchor);
            axeInstance.name = "EquippedSurvivalAxe";
            axeInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            bladeBase = axeInstance.transform.Find("BladeBase");
            bladeTip = axeInstance.transform.Find("BladeTip");
            if (bladeBase == null || bladeTip == null)
            {
                throw new InvalidOperationException("Axe prefab requires BladeBase and BladeTip anchors.");
            }

            axeInstance.SetActive(false);
        }

        private void OnEnable()
        {
            if (subscribed || attackAction == null)
            {
                return;
            }

            attackAction.performed += HandleAttack;
            equipAction.performed += HandleEquip;
            unequipAction.performed += HandleUnequip;
            subscribed = true;
        }

        private void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            attackAction.performed -= HandleAttack;
            equipAction.performed -= HandleEquip;
            unequipAction.performed -= HandleUnequip;
            subscribed = false;
        }

        private void Update()
        {
            stateElapsed += Time.deltaTime;
            TickMotion();
            if (IsHitWindowOpen && !damageCommitted)
            {
                EvaluateBladeContact();
            }
        }

        private void HandleAttack(InputAction.CallbackContext _) => TryBeginSwing();
        private void HandleEquip(InputAction.CallbackContext _) => BeginEquip();
        private void HandleUnequip(InputAction.CallbackContext _) => BeginUnequip();

        public bool TryBeginSwing()
        {
            if (motionState != AxeMotionState.Idle)
            {
                return false;
            }

            swingLeft = !swingLeft;
            motionState = swingLeft ? AxeMotionState.SwingLeft : AxeMotionState.SwingRight;
            stateElapsed = 0f;
            damageCommitted = false;
            swingSequence++;
            return true;
        }

        public void BeginEquip()
        {
            if (motionState != AxeMotionState.Unequipped)
            {
                return;
            }

            axeInstance.SetActive(true);
            motionState = AxeMotionState.Equipping;
            stateElapsed = 0f;
        }

        public void BeginUnequip()
        {
            if (motionState == AxeMotionState.Unequipped || motionState == AxeMotionState.Unequipping)
            {
                return;
            }

            motionState = AxeMotionState.Unequipping;
            stateElapsed = 0f;
        }

        private void TickMotion()
        {
            switch (motionState)
            {
                case AxeMotionState.Equipping:
                    PoseEquip(Mathf.Clamp01(stateElapsed / 0.32f));
                    if (stateElapsed >= 0.32f) Transition(AxeMotionState.Idle);
                    break;
                case AxeMotionState.Idle:
                    PoseIdle();
                    break;
                case AxeMotionState.SwingLeft:
                case AxeMotionState.SwingRight:
                    PoseSwing(Mathf.Clamp01(stateElapsed / swingDuration),
                        motionState == AxeMotionState.SwingLeft);
                    if (stateElapsed >= swingDuration)
                    {
                        Transition(damageCommitted ? AxeMotionState.HitRecoil : AxeMotionState.MissRecovery);
                    }
                    break;
                case AxeMotionState.HitRecoil:
                    PoseRecoil(Mathf.Clamp01(stateElapsed / 0.24f), false);
                    if (stateElapsed >= 0.24f) Transition(AxeMotionState.Idle);
                    break;
                case AxeMotionState.HeavyFinal:
                    PoseRecoil(Mathf.Clamp01(stateElapsed / 0.42f), true);
                    if (stateElapsed >= 0.42f) Transition(AxeMotionState.Idle);
                    break;
                case AxeMotionState.MissRecovery:
                    PoseMiss(Mathf.Clamp01(stateElapsed / 0.30f));
                    if (stateElapsed >= 0.30f) Transition(AxeMotionState.Idle);
                    break;
                case AxeMotionState.Unequipping:
                    PoseEquip(1f - Mathf.Clamp01(stateElapsed / 0.28f));
                    if (stateElapsed >= 0.28f)
                    {
                        axeInstance.SetActive(false);
                        Transition(AxeMotionState.Unequipped);
                    }
                    break;
            }
        }

        private void EvaluateBladeContact()
        {
            int count = Physics.OverlapCapsuleNonAlloc(
                bladeBase.position, bladeTip.position, bladeRadius, contacts,
                hitMask, QueryTriggerInteraction.Ignore);
            for (int index = 0; index < count; index++)
            {
                Collider collider = contacts[index];
                ForestInteractiveTree tree = collider != null
                    ? collider.GetComponentInParent<ForestInteractiveTree>()
                    : null;
                if (tree == null)
                {
                    continue;
                }

                Vector3 hitPosition = collider.ClosestPoint(bladeTip.position);
                Vector3 normal = (bladeTip.position - hitPosition).normalized;
                Vector3 direction = viewCamera.transform.forward;
                var damage = new TreeDamageEvent(
                    damagePerContact, hitPosition, normal, direction,
                    "tool.axe.survival", swingSequence);
                if (!tree.TryReceiveDamage(in damage, out _))
                {
                    continue;
                }

                damageCommitted = true;
                if (tree.State == ForestTreeLifecycleState.Falling)
                {
                    Transition(AxeMotionState.HeavyFinal);
                }

                return;
            }
        }

        private void PoseIdle()
        {
            float breathe = Mathf.Sin(Time.time * 1.7f) * 0.008f;
            SetPose(new Vector3(0.27f, -0.30f + breathe, 0.48f), new Vector3(20f, -18f, -8f));
        }

        private void PoseEquip(float value)
        {
            float eased = value * value * (3f - 2f * value);
            SetPose(Vector3.Lerp(new Vector3(0.36f, -0.72f, 0.30f), new Vector3(0.27f, -0.30f, 0.48f), eased),
                Vector3.Lerp(new Vector3(75f, -30f, -20f), new Vector3(20f, -18f, -8f), eased));
        }

        private void PoseSwing(float value, bool left)
        {
            float side = left ? -1f : 1f;
            float arc = Mathf.Sin(value * Mathf.PI);
            float strike = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((value - 0.22f) / 0.42f));
            Vector3 position = new(0.24f + side * (0.22f - strike * 0.44f),
                -0.22f + arc * 0.20f, 0.43f + strike * 0.42f);
            Vector3 rotation = new(-48f + strike * 138f, side * (55f - strike * 110f), side * 22f);
            SetPose(position, rotation);
        }

        private void PoseRecoil(float value, bool heavy)
        {
            float amplitude = heavy ? 0.18f : 0.10f;
            SetPose(new Vector3(0.24f, -0.27f, 0.72f - value * amplitude),
                new Vector3(82f - value * 55f, 0f, Mathf.Sin(value * Mathf.PI) * 12f));
        }

        private void PoseMiss(float value)
        {
            SetPose(Vector3.Lerp(new Vector3(0.05f, -0.42f, 0.78f), new Vector3(0.27f, -0.30f, 0.48f), value),
                Vector3.Lerp(new Vector3(105f, 0f, 0f), new Vector3(20f, -18f, -8f), value));
        }

        private void SetPose(Vector3 position, Vector3 euler)
        {
            axeInstance.transform.localPosition = position;
            axeInstance.transform.localRotation = Quaternion.Euler(euler);
        }

        private void Transition(AxeMotionState next)
        {
            motionState = next;
            stateElapsed = 0f;
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            Transform hands, Camera camera, GameObject prefab, float damage,
            float duration, float windowOpen, float windowClose, float radius)
        {
            handsAnchor = hands;
            viewCamera = camera;
            axePrefab = prefab;
            damagePerContact = damage;
            swingDuration = duration;
            hitWindowOpen = windowOpen;
            hitWindowClose = windowClose;
            bladeRadius = radius;
        }
#endif
    }
}
