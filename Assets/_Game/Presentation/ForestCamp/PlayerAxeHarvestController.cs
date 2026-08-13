using System;
using SonsOfTheForest.Data.Forest;
using SonsOfTheForest.Infrastructure.Forest;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SonsOfTheForest.Presentation.ForestCamp
{
    public enum AxeActionState
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

    public enum AxeContactResult
    {
        None,
        NoTreeOverlap,
        OutOfRange,
        FacingRejected,
        DamageRejected,
        Accepted
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerAxeHarvestController : MonoBehaviour
    {
        private const string PlayerMapName = "Player";
        private const string AttackActionName = "Attack";
        private const string EquipActionName = "Previous";
        private const string UnequipActionName = "Next";

        [SerializeField] private Transform handsAnchor;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private GameObject axePrefab;
        [SerializeField, Min(0.1f)] private float damagePerContact = 22f;
        [SerializeField, Min(0.1f)] private float fallbackSwingDuration = 1.1f;
        [SerializeField, Range(0.1f, 0.9f)] private float contactMarkerNormalized = 0.5f;
        [SerializeField, Min(0.01f)] private float bladeRadius = 0.07f;
        [SerializeField, Min(0.25f)] private float maximumContactDistance = 3f;
        [SerializeField, Range(-1f, 1f)] private float minimumFacingDot = 0.1f;
        [SerializeField] private LayerMask hitMask = ~0;

        private readonly Collider[] contacts = new Collider[12];
        private PlayerInput playerInput;
        private InputAction attackAction;
        private InputAction equipAction;
        private InputAction unequipAction;
        private GameObject axeInstance;
        private AxeViewmodelAnimator viewmodel;
        private Transform bladeBase;
        private Transform bladeTip;
        private AxeActionState motionState = AxeActionState.Unequipped;
        private float stateElapsed;
        private float stateDuration;
        private float previousNormalizedTime;
        private bool swingLeft;
        private bool contactDispatched;
        private bool damageCommitted;
        private int contactDispatchCount;
        private AxeContactResult lastContactResult;
        private int swingSequence;
        private bool subscribed;

        public AxeActionState MotionState => motionState;
        public int ContactDispatchCount => contactDispatchCount;
        public AxeContactResult LastContactResult => lastContactResult;
        public bool LastContactAccepted => damageCommitted;
        public float ActionNormalizedTime => stateElapsed / Mathf.Max(0.01f, stateDuration);
        public Vector3 LastContactBladeBase { get; private set; }
        public Vector3 LastContactBladeTip { get; private set; }
        public int LastOverlapCount { get; private set; }
        public bool IsEquipped => motionState != AxeActionState.Unequipped &&
                                  motionState != AxeActionState.Unequipping;
        public bool IsHitWindowOpen
        {
            get
            {
                if (motionState != AxeActionState.SwingLeft && motionState != AxeActionState.SwingRight)
                {
                    return false;
                }

                float normalized = stateElapsed / Mathf.Max(0.01f, stateDuration);
                return !contactDispatched && previousNormalizedTime < contactMarkerNormalized &&
                       normalized >= contactMarkerNormalized;
            }
        }

        private void Awake()
        {
            if (handsAnchor == null || viewCamera == null || axePrefab == null ||
                contactMarkerNormalized <= 0f || contactMarkerNormalized >= 1f)
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
            viewmodel = axeInstance.GetComponent<AxeViewmodelAnimator>();
            string viewmodelReason = viewmodel == null
                ? "AxeViewmodelAnimator component is missing."
                : string.Empty;
            if (viewmodel == null || !viewmodel.TryValidate(out viewmodelReason))
            {
                throw new InvalidOperationException("Axe prefab requires a synchronized viewmodel: " +
                                                    viewmodelReason);
            }
            bladeBase = viewmodel.BladeBase;
            bladeTip = viewmodel.BladeTip;

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
            previousNormalizedTime = stateElapsed / Mathf.Max(0.01f, stateDuration);
            stateElapsed += Time.deltaTime;
            if (IsHitWindowOpen)
            {
                contactDispatched = true;
                contactDispatchCount++;
                EvaluateBladeContact();
            }
            TickMotion();
        }

        private void HandleAttack(InputAction.CallbackContext _) => TryBeginSwing();
        private void HandleEquip(InputAction.CallbackContext _) => BeginEquip();
        private void HandleUnequip(InputAction.CallbackContext _) => BeginUnequip();

        public bool TryBeginSwing()
        {
            if (motionState != AxeActionState.Idle)
            {
                return false;
            }

            swingLeft = !swingLeft;
            motionState = swingLeft ? AxeActionState.SwingLeft : AxeActionState.SwingRight;
            stateElapsed = 0f;
            previousNormalizedTime = 0f;
            contactDispatched = false;
            damageCommitted = false;
            lastContactResult = AxeContactResult.None;
            swingSequence++;
            stateDuration = Mathf.Max(fallbackSwingDuration, viewmodel.Play(
                swingLeft ? AxeViewmodelAnimator.ChopLeftClip : AxeViewmodelAnimator.ChopRightClip));
            return true;
        }

        public void BeginEquip()
        {
            if (motionState != AxeActionState.Unequipped)
            {
                return;
            }

            axeInstance.SetActive(true);
            motionState = AxeActionState.Equipping;
            stateElapsed = 0f;
            stateDuration = viewmodel.Play(AxeViewmodelAnimator.EquipClip, false);
        }

        public void BeginUnequip()
        {
            if (motionState == AxeActionState.Unequipped || motionState == AxeActionState.Unequipping)
            {
                return;
            }

            motionState = AxeActionState.Unequipping;
            stateElapsed = 0f;
            stateDuration = viewmodel.Play(AxeViewmodelAnimator.UnequipClip);
        }

        private void TickMotion()
        {
            switch (motionState)
            {
                case AxeActionState.Equipping:
                    if (stateElapsed >= stateDuration) Transition(AxeActionState.Idle);
                    break;
                case AxeActionState.Idle:
                    break;
                case AxeActionState.SwingLeft:
                case AxeActionState.SwingRight:
                    if (stateElapsed >= stateDuration)
                    {
                        Transition(damageCommitted ? AxeActionState.HitRecoil : AxeActionState.MissRecovery);
                    }
                    break;
                case AxeActionState.HitRecoil:
                    if (stateElapsed >= stateDuration) Transition(AxeActionState.Idle);
                    break;
                case AxeActionState.HeavyFinal:
                    if (stateElapsed >= stateDuration) Transition(AxeActionState.Idle);
                    break;
                case AxeActionState.MissRecovery:
                    if (stateElapsed >= stateDuration) Transition(AxeActionState.Idle);
                    break;
                case AxeActionState.Unequipping:
                    if (stateElapsed >= stateDuration)
                    {
                        axeInstance.SetActive(false);
                        Transition(AxeActionState.Unequipped);
                    }
                    break;
            }
        }

        private void EvaluateBladeContact()
        {
            lastContactResult = AxeContactResult.NoTreeOverlap;
            viewmodel.GetBladeContactSegment(viewCamera.transform, out Vector3 bladeBasePosition,
                out Vector3 bladeTipPosition);
            LastContactBladeBase = bladeBasePosition;
            LastContactBladeTip = bladeTipPosition;
            int count = Physics.OverlapCapsuleNonAlloc(
                bladeBasePosition, bladeTipPosition, bladeRadius, contacts,
                hitMask, QueryTriggerInteraction.Ignore);
            LastOverlapCount = count;
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

                Vector3 hitPosition = collider.ClosestPoint(bladeTipPosition);
                Vector3 viewToHit = hitPosition - viewCamera.transform.position;
                if (viewToHit.sqrMagnitude > maximumContactDistance * maximumContactDistance)
                {
                    lastContactResult = AxeContactResult.OutOfRange;
                    continue;
                }

                if (Vector3.Dot(viewCamera.transform.forward, viewToHit.normalized) < minimumFacingDot)
                {
                    lastContactResult = AxeContactResult.FacingRejected;
                    continue;
                }
                Vector3 normal = (bladeTipPosition - hitPosition).normalized;
                Vector3 direction = viewCamera.transform.forward;
                var damage = new TreeDamageEvent(
                    damagePerContact, hitPosition, normal, direction,
                    "tool.axe.survival", swingSequence);
                if (!tree.TryReceiveDamage(in damage, out _))
                {
                    lastContactResult = AxeContactResult.DamageRejected;
                    continue;
                }

                damageCommitted = true;
                lastContactResult = AxeContactResult.Accepted;
                if (tree.State == ForestTreeLifecycleState.Falling)
                {
                    Transition(AxeActionState.HeavyFinal);
                }

                return;
            }
        }

        private void Transition(AxeActionState next)
        {
            motionState = next;
            stateElapsed = 0f;
            if (viewmodel == null)
            {
                return;
            }

            if (next == AxeActionState.Idle)
            {
                stateDuration = viewmodel.Play(AxeViewmodelAnimator.IdleClip);
            }
            else if (next == AxeActionState.HitRecoil || next == AxeActionState.MissRecovery)
            {
                stateDuration = viewmodel.Play(AxeViewmodelAnimator.RecoveryClip);
            }
            else if (next == AxeActionState.HeavyFinal)
            {
                stateDuration = viewmodel.Play(AxeViewmodelAnimator.ChopHeavyClip);
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            Transform hands, Camera camera, GameObject prefab, float damage,
            float duration, float contactMarker, float radius)
        {
            handsAnchor = hands;
            viewCamera = camera;
            axePrefab = prefab;
            damagePerContact = damage;
            fallbackSwingDuration = duration;
            contactMarkerNormalized = contactMarker;
            bladeRadius = radius;
        }
#endif
    }
}
