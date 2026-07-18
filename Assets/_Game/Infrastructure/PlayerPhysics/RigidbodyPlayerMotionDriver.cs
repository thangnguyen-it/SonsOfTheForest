using SonsOfTheForest.Gameplay.Player;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.PlayerPhysics
{
    [DefaultExecutionOrder(25)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class RigidbodyPlayerMotionDriver : MonoBehaviour,
        IPlayerMotionDriver
    {
        private const float QueryRadiusInset = 0.02f;
        private const float GeometryEpsilon = 0.0001f;
        private const int QueryCapacity = 16;

        [Header("Provisional physics tuning")]
        [SerializeField]
        private float bodyMass = 80f;

        [SerializeField]
        private float groundProbeStart = 0.05f;

        [SerializeField]
        private float groundProbeDistance = 0.12f;

        [SerializeField]
        private LayerMask collisionMask = ~0;

        private readonly RaycastHit[] groundHits =
            new RaycastHit[QueryCapacity];
        private readonly Collider[] overlapHits =
            new Collider[QueryCapacity];
        private Rigidbody body;
        private CapsuleCollider capsule;

        public Vector3 CurrentPlanarVelocity
        {
            get
            {
                Vector3 velocity = body == null
                    ? Vector3.zero
                    : body.linearVelocity;
                return new Vector3(velocity.x, 0f, velocity.z);
            }
        }

        public float CurrentVerticalVelocity =>
            body == null ? 0f : body.linearVelocity.y;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            ConfigureBody();
        }

        private void OnValidate()
        {
            bodyMass = Mathf.Max(0.01f, bodyMass);
            groundProbeStart = Mathf.Max(0.001f, groundProbeStart);
            groundProbeDistance = Mathf.Max(0.001f, groundProbeDistance);
        }

        public PlayerGroundInfo SampleGround(float slopeLimitDegrees)
        {
            if (body == null || capsule == null)
            {
                return PlayerGroundInfo.Airborne;
            }

            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            float radius = Mathf.Max(
                0.01f,
                capsule.radius - QueryRadiusInset);
            Vector3 origin = body.position +
                Vector3.up * (capsule.radius + groundProbeStart);
            float distance = groundProbeStart + groundProbeDistance;
            int hitCount = physicsScene.SphereCast(
                origin,
                radius,
                Vector3.down,
                groundHits,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            bool found = false;
            RaycastHit nearest = default;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit candidate = groundHits[index];
                if (IsSelf(candidate.collider))
                {
                    continue;
                }

                if (!found || candidate.distance < nearest.distance)
                {
                    found = true;
                    nearest = candidate;
                }
            }

            if (!found)
            {
                return PlayerGroundInfo.Airborne;
            }

            float slopeAngle = Vector3.Angle(nearest.normal, Vector3.up);
            bool walkable = slopeAngle <= Mathf.Clamp(
                slopeLimitDegrees,
                0f,
                89f);
            return new PlayerGroundInfo(
                true,
                nearest.normal,
                slopeAngle,
                walkable);
        }

        public bool CanOccupyStance(
            PlayerStance stance,
            PlayerMovementConfig config)
        {
            if (stance == PlayerStance.Crouching)
            {
                return true;
            }

            float radius = Mathf.Max(
                0.01f,
                config.CapsuleRadius - QueryRadiusInset);
            float height = Mathf.Max(config.StandingHeight, radius * 2f);
            Vector3 bottom = body.position + Vector3.up *
                (radius + QueryRadiusInset);
            Vector3 top = body.position + Vector3.up *
                (height - radius - QueryRadiusInset);
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            int overlapCount = physicsScene.OverlapCapsule(
                bottom,
                top,
                radius,
                overlapHits,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            for (int index = 0; index < overlapCount; index++)
            {
                if (!IsSelf(overlapHits[index]))
                {
                    return false;
                }
            }

            return overlapCount < overlapHits.Length;
        }

        public void ApplyMotion(
            PlayerMovementState state,
            PlayerGroundInfo groundInfo,
            PlayerMovementConfig config)
        {
            ApplyCapsule(state.Stance, config);

            Vector3 planarVelocity = state.PlanarVelocity;
            if (groundInfo.IsGrounded && groundInfo.IsWalkable)
            {
                Vector3 projected = Vector3.ProjectOnPlane(
                    planarVelocity,
                    groundInfo.Normal);
                if (projected.sqrMagnitude > GeometryEpsilon &&
                    planarVelocity.sqrMagnitude > GeometryEpsilon)
                {
                    planarVelocity = projected.normalized *
                        planarVelocity.magnitude;
                }
            }
            else if (groundInfo.IsGrounded && !groundInfo.IsWalkable)
            {
                Vector3 downhill = Vector3.ProjectOnPlane(
                    Vector3.down,
                    groundInfo.Normal);
                if (downhill.sqrMagnitude > GeometryEpsilon)
                {
                    downhill.Normalize();
                    Vector3 uphill = -downhill;
                    float uphillSpeed = Vector3.Dot(
                        planarVelocity,
                        uphill);
                    if (uphillSpeed > 0f)
                    {
                        planarVelocity -= uphill * uphillSpeed;
                    }
                }
            }

            Vector3 targetVelocity = planarVelocity +
                Vector3.up * state.VerticalVelocity;
            Vector3 correction = targetVelocity - body.linearVelocity;
            body.AddForce(correction, ForceMode.VelocityChange);
        }

        private void ConfigureBody()
        {
            body.mass = Mathf.Max(0.01f, bodyMass);
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;
            capsule.direction = 1;
        }

        private void ApplyCapsule(
            PlayerStance stance,
            PlayerMovementConfig config)
        {
            float targetHeight = stance == PlayerStance.Crouching
                ? config.CrouchingHeight
                : config.StandingHeight;
            float targetRadius = config.CapsuleRadius;
            Vector3 targetCenter = new(
                0f,
                targetHeight * 0.5f,
                0f);

            if (Mathf.Abs(capsule.height - targetHeight) > GeometryEpsilon)
            {
                capsule.height = targetHeight;
            }

            if (Mathf.Abs(capsule.radius - targetRadius) > GeometryEpsilon)
            {
                capsule.radius = targetRadius;
            }

            if ((capsule.center - targetCenter).sqrMagnitude > GeometryEpsilon)
            {
                capsule.center = targetCenter;
            }
        }

        private bool IsSelf(Collider candidate)
        {
            return candidate == null ||
                candidate == capsule ||
                candidate.transform.IsChildOf(transform);
        }
    }
}
