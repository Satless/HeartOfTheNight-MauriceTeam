using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class InquisitorBullet : MonoBehaviour
    {
        private Rigidbody2D rb;
        private Transform target;
        private float speed;
        private float homingTurnRate;
        private int   damage;
        private float lifetime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void Launch(Transform playerTarget, Vector2 initialDirection, float bulletSpeed,
                           float turnRate, int dmg, float life)
        {
            target         = playerTarget;
            speed          = bulletSpeed;
            homingTurnRate = turnRate;
            damage         = dmg;
            lifetime       = life;
            rb.linearVelocity = initialDirection.normalized * speed;

            float angle = Mathf.Atan2(initialDirection.y, initialDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f) Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (target == null) return;

            Vector2 toPlayer = (Vector2)target.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude < 0.0001f) return;

            Vector2 desired = toPlayer.normalized;
            Vector2 current = rb.linearVelocity.sqrMagnitude > 0.01f
                ? rb.linearVelocity.normalized
                : desired;

            Vector2 steered = Vector2.MoveTowards(current, desired, homingTurnRate * Time.fixedDeltaTime);
            rb.linearVelocity = steered.normalized * speed;

            float angle = Mathf.Atan2(steered.y, steered.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (EnemyCombatRules.IsEnemyCollider(other)) return;

            if (EnemyCombatRules.TryGetPlayerDamageable(other, out var damageable))
                damageable.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}
