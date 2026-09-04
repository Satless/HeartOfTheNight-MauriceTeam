using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class InquisitorBullet : MonoBehaviour
    {
        [Header("VFX")]
        [SerializeField] private GameObject hitVfxPrefab;

        private static readonly Collider2D[] OverlapBuf = new Collider2D[12];

        private Rigidbody2D rb;
        private Collider2D col;
        private Transform target;
        private Collider2D targetCol;
        private Rigidbody2D targetRb;
        private float speed;
        private float homingTurnRateDeg;
        private float homingStopDistance;
        private float homingLockPlayerSpeed;
        private LayerMask groundLayer;
        private bool homingLocked;
        private int damage;
        private float lifetime;
        private bool consumed;
        private float groundImmunity;
        private float castRadius;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            col.isTrigger = true;
            castRadius = col is CircleCollider2D circle ? circle.radius : 0.28f;
        }

        public void Launch(Transform playerTarget, Vector2 initialDirection, float bulletSpeed,
                           float turnRateDeg, float stopHomingDistance, float lockPlayerSpeed,
                           int dmg, float life, LayerMask ground, Collider2D shooterCol = null)
        {
            target = playerTarget;
            CacheTarget();
            speed = bulletSpeed;
            homingTurnRateDeg = Mathf.Max(0f, turnRateDeg);
            homingStopDistance = Mathf.Max(0f, stopHomingDistance);
            homingLockPlayerSpeed = Mathf.Max(0f, lockPlayerSpeed);
            groundLayer = ground;
            homingLocked = homingTurnRateDeg <= 0f;
            damage = dmg;
            lifetime = life;
            groundImmunity = 0.1f;

            Vector2 dir = initialDirection.sqrMagnitude > 0.0001f
                ? initialDirection.normalized
                : Vector2.right;
            rb.linearVelocity = dir * speed;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            if (shooterCol != null)
                Physics2D.IgnoreCollision(col, shooterCol, true);

            IgnoreOverlappingTerrain();
        }

        private void CacheTarget()
        {
            targetCol = null;
            targetRb = null;
            if (target == null) return;

            targetCol = target.GetComponent<Collider2D>();
            if (targetCol == null)
                targetCol = target.GetComponentInChildren<Collider2D>();

            targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb == null)
                targetRb = target.GetComponentInParent<Rigidbody2D>();
        }

        private void IgnoreOverlappingTerrain()
        {
            if (groundLayer.value == 0) return;

            var filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayer);
            filter.useTriggers = true;

            int count = col.Overlap(filter, OverlapBuf);
            for (int i = 0; i < count; i++)
            {
                var hit = OverlapBuf[i];
                if (hit != null && hit != col)
                    Physics2D.IgnoreCollision(col, hit, true);
            }
        }

        private void Update()
        {
            if (consumed) return;

            if (groundImmunity > 0f)
                groundImmunity -= Time.deltaTime;

            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
                Consume();
        }

        private void FixedUpdate()
        {
            if (consumed) return;

            if (target == null)
            {
                MaintainStraightFlight();
                return;
            }

            if (!homingLocked)
                TryLockHoming();

            if (homingLocked)
            {
                MaintainStraightFlight();
                return;
            }

            Vector2 toPlayer = AimPoint - rb.position;
            if (toPlayer.sqrMagnitude < 0.0001f) return;

            Vector2 desiredDir = toPlayer.normalized;
            if (WouldHitTerrain(desiredDir, toPlayer.magnitude))
            {
                desiredDir = FlattenAwayFromFloor(desiredDir);
                if (WouldHitTerrain(desiredDir, Mathf.Min(toPlayer.magnitude, 1.2f)))
                {
                    homingLocked = true;
                    FlattenIfDiving();
                    MaintainStraightFlight();
                    return;
                }
            }

            Vector2 vel = rb.linearVelocity;
            float currentAngle = vel.sqrMagnitude > 0.01f
                ? Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg
                : Mathf.Atan2(desiredDir.y, desiredDir.x) * Mathf.Rad2Deg;
            float desiredAngle = Mathf.Atan2(desiredDir.y, desiredDir.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, desiredAngle,
                                                    homingTurnRateDeg * Time.fixedDeltaTime);
            float rad = newAngle * Mathf.Deg2Rad;
            rb.linearVelocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
            transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }

        private void TryLockHoming()
        {
            float dist = Vector2.Distance(rb.position, AimPoint);
            if (dist <= homingStopDistance)
            {
                homingLocked = true;
                return;
            }

            if (targetRb != null && targetRb.linearVelocity.magnitude >= homingLockPlayerSpeed)
            {
                homingLocked = true;
                return;
            }

            if (groundLayer.value != 0)
            {
                RaycastHit2D hit = Physics2D.CircleCast(
                    rb.position, castRadius * 0.85f, (AimPoint - rb.position).normalized,
                    dist, groundLayer);
                if (hit.collider != null)
                {
                    homingLocked = true;
                    FlattenIfDiving();
                }
            }
        }

        private bool WouldHitTerrain(Vector2 dir, float distance)
        {
            if (groundLayer.value == 0 || dir.sqrMagnitude < 0.0001f) return false;
            float dist = Mathf.Max(0.15f, distance);
            RaycastHit2D hit = Physics2D.CircleCast(
                rb.position, castRadius * 0.85f, dir.normalized, dist, groundLayer);
            return hit.collider != null;
        }

        private Vector2 FlattenAwayFromFloor(Vector2 desiredDir)
        {
            float dirX = Mathf.Sign(AimPoint.x - rb.position.x);
            if (Mathf.Abs(dirX) < 0.01f)
                dirX = Mathf.Sign(rb.linearVelocity.x);
            if (Mathf.Abs(dirX) < 0.01f)
                dirX = 1f;
            return new Vector2(dirX, Mathf.Max(0f, desiredDir.y)).normalized;
        }

        private void FlattenIfDiving()
        {
            Vector2 vel = rb.linearVelocity;
            if (vel.y >= 0f) return;

            vel.y = 0f;
            if (vel.sqrMagnitude < 0.01f)
            {
                float dirX = Mathf.Sign(AimPoint.x - rb.position.x);
                if (Mathf.Abs(dirX) < 0.01f) dirX = 1f;
                vel = new Vector2(dirX, 0f);
            }

            rb.linearVelocity = vel.normalized * speed;
        }

        private Vector2 AimPoint =>
            targetCol != null ? (Vector2)targetCol.bounds.center : (Vector2)target.position;

        private void MaintainStraightFlight()
        {
            Vector2 vel = rb.linearVelocity;
            if (vel.sqrMagnitude < 0.01f) return;

            rb.linearVelocity = vel.normalized * speed;
            float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other == null) return;
            if (EnemyCombatRules.IsEnemyCollider(other)) return;

            if (EnemyCombatRules.TryGetPlayerDamageable(other, out var damageable))
            {
                damageable.TakeDamage(damage);
                Consume();
                return;
            }

            if (!IsSolidTerrain(other)) return;
            if (groundImmunity > 0f) return;

            Consume();
        }

        private static bool IsSolidTerrain(Collider2D other)
        {
            if (other.isTrigger) return false;
            return other.gameObject.layer == LayerMask.NameToLayer("Ground")
                || other.gameObject.layer == LayerMask.NameToLayer("Wall")
                || other.CompareTag("Ground")
                || other.CompareTag("Wall");
        }

        private void Consume()
        {
            if (consumed) return;
            consumed = true;
            SpawnVFX();
            Destroy(gameObject);
        }

        private void SpawnVFX()
        {
            if (hitVfxPrefab != null)
                Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
        }
    }
}
