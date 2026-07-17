using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    /// <summary>
    /// Dan thuong cua boss (State 1 - Na dan). Bay thang theo huong duoc ban, chi gay sat thuong len Player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class HeartOfTheNightBullet : MonoBehaviour
    {
        private Rigidbody2D rb;
        private int damage;
        private float lifetime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void Launch(Vector2 direction, float speed, int bulletDamage, float life)
        {
            damage = bulletDamage;
            lifetime = life;
            rb.linearVelocity = direction.normalized * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (EnemyCombatRules.IsEnemyCollider(other)) return;

            if (EnemyCombatRules.TryGetPlayerDamageable(other, out var target))
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
