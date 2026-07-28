using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class HeartOfTheNightBullet : MonoBehaviour
    {
        [Header("VFX")]
        [Tooltip("Prefab hiệu ứng nổ khi đạn trúng mục tiêu hoặc hết thời gian sống")]
        [SerializeField] private GameObject hitVfxPrefab;

        private Rigidbody2D rb;
        private int damage;
        private float lifetime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true; // Đảm bảo đạn là Trigger để xuyên qua nhau
        }

        public void Launch(Vector2 direction, float speed, int bulletDamage, float life)
        {
            damage = bulletDamage;
            lifetime = life;
            rb.linearVelocity = direction.normalized * speed;

            // Xoay đạn theo hướng bắn. 
            // Lưu ý: Nếu sprite viên đạn gốc vẽ hướng lên trên (thay vì mũi nhọn chỉ sang phải), 
            // hãy đổi thành: angle - 90f
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Bỏ qua nếu chạm phải Boss hoặc quái khác
            if (EnemyCombatRules.IsEnemyCollider(other)) return;

            // Nếu chạm Player, gây sát thương
            if (EnemyCombatRules.TryGetPlayerDamageable(other, out var target))
            {
                target.TakeDamage(damage);
            }

            // Chạm vào Player hoặc môi trường (tường, đất) thì sinh VFX và tự hủy
            SpawnVFX();
            Destroy(gameObject);
        }

        private void SpawnVFX()
        {
            if (hitVfxPrefab != null)
            {
                Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}