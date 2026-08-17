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

        private Rigidbody2D rb;
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

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void Launch(Transform playerTarget, Vector2 initialDirection, float bulletSpeed,
                           float turnRateDeg, float stopHomingDistance, float lockPlayerSpeed,
                           int dmg, float life, LayerMask ground)
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
            rb.linearVelocity = initialDirection.normalized * speed;

            float angle = Mathf.Atan2(initialDirection.y, initialDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
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

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                SpawnVFX();
                Destroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
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

            Vector2 vel = rb.linearVelocity;
            float currentAngle = vel.sqrMagnitude > 0.01f
                ? Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg
                : Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            float desiredAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
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

            // Lướt (~35) vượt ngưỡng → khóa thẳng để né được; đi bộ (~11) vẫn bị bám.
            if (targetRb != null && targetRb.linearVelocity.magnitude >= homingLockPlayerSpeed)
            {
                homingLocked = true;
                return;
            }

            if (groundLayer.value != 0 && Physics2D.Linecast(rb.position, AimPoint, groundLayer).collider != null)
                homingLocked = true;
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
            if (EnemyCombatRules.IsEnemyCollider(other)) return;

            if (EnemyCombatRules.TryGetPlayerDamageable(other, out var damageable))
                damageable.TakeDamage(damage);

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
