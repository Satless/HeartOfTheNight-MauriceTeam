using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class HeartOfTheNightBullet : MonoBehaviour
    {
        [Header("VFX")]
        [Tooltip("Prefab hiệu ứng nổ khi đạn trúng Player hoặc hết thời gian sống")]
        [SerializeField] private GameObject hitVfxPrefab;

        [Header("Chống lọt hurtbox (sweep)")]
        [SerializeField] private float sweepRadius = 0.45f;

        private Rigidbody2D rb;
        private int damage;
        private float lifetime;
        private Vector2 lastPos;
        private bool consumed;
        private int playerMask;
        private static readonly RaycastHit2D[] SweepHits = new RaycastHit2D[12];

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            if (col is CircleCollider2D circle && circle.radius > sweepRadius)
                sweepRadius = circle.radius;

            playerMask = LayerMask.GetMask("Player");
        }

        public void Launch(Vector2 direction, float speed, int bulletDamage, float life)
        {
            damage = bulletDamage;
            lifetime = life;
            rb.linearVelocity = direction.normalized * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Start()
        {
            lastPos = transform.position;
        }

        private void Update()
        {
            if (consumed) return;

            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
                Consume(playVfx: true);
        }

        private void FixedUpdate()
        {
            if (consumed) return;

            Vector2 now = transform.position;
            Vector2 delta = now - lastPos;
            float dist = delta.magnitude;
            if (dist > 0.001f)
            {
                int count = Physics2D.CircleCastNonAlloc(
                    lastPos, sweepRadius, delta / dist, SweepHits, dist, playerMask);

                for (int i = 0; i < count; i++)
                {
                    if (TryHitPlayer(SweepHits[i].collider))
                        return;
                }
            }

            lastPos = now;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHitPlayer(other);
        }

        private bool TryHitPlayer(Collider2D other)
        {
            if (consumed || other == null) return false;
            if (EnemyCombatRules.IsEnemyCollider(other)) return false;
            if (!EnemyCombatRules.TryGetPlayerDamageable(other, out var target))
                return false;

            target.TakeDamage(damage);
            Consume(playVfx: true);
            return true;
        }

        private void Consume(bool playVfx)
        {
            if (consumed) return;
            consumed = true;

            if (playVfx)
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
