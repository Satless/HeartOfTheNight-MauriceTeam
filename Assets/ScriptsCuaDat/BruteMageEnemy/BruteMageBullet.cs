using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class BruteMageBullet : MonoBehaviour
    {
        private Rigidbody2D rb;
        private int damage;
        private float lifetime;
        private BruteMage owner;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void Launch(BruteMage shooter, Vector2 direction, float speed, int bulletDamage, float life)
        {
            owner = shooter;
            damage = bulletDamage;
            lifetime = life;
            rb.linearVelocity = direction * speed;

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
            if (owner != null && other.GetComponentInParent<BruteMage>() == owner) return;

            var target = other.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}
