using UnityEngine;
using HeartOfTheNight.Common;
using HeartOfTheNight.Player;

namespace HeartOfTheNight.Environment
{
    /// <summary>
    /// Gắn script này vào các bẫy môi trường (Lava, Spikes...).
    /// Yêu cầu GameObject bẫy có Collider2D (nên set IsTrigger = true).
    /// Layer Trap phải va chạm với Player và Enemy trong Physics2D Collision Matrix.
    /// </summary>
    public class EnvironmentTrap : MonoBehaviour
    {
        private const int InstaKillDamage = 99999;

        [Header("Trap Settings")]
        [Tooltip("Nếu True, chạm vào là chết ngay lập tức (Lava). Nếu False, trừ máu theo lượng Damage bên dưới (Spikes).")]
        [SerializeField] private bool _instaKill;

        [Tooltip("Số máu trừ nếu không phải InstaKill.")]
        [SerializeField] private int _damage;

        [Tooltip("Lực nảy khi dính bẫy (Chỉ dùng nếu không phải InstaKill)")]
        [SerializeField] private float _knockbackForce;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            HandleCollision(collision.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.gameObject);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (_instaKill)
                HandleCollision(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_instaKill)
                HandleCollision(collision.gameObject);
        }

        private void HandleCollision(GameObject target)
        {
            if (target == null)
                return;

            PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                ApplyPlayerHit(playerHealth, target);
                return;
            }

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null)
                return;

            ApplyEnemyHit(damageable, target);
        }

        private void ApplyPlayerHit(PlayerHealth playerHealth, GameObject target)
        {
            if (_instaKill)
            {
                playerHealth.InstaKill();
                return;
            }

            if (_damage > 0)
                playerHealth.TakeDamage(_damage);

            ApplyKnockback(target);
        }

        private void ApplyEnemyHit(IDamageable damageable, GameObject target)
        {
            int amount = _instaKill ? InstaKillDamage : _damage;
            if (amount > 0)
                damageable.TakeDamage(amount);

            if (!_instaKill)
                ApplyKnockback(target);
        }

        private void ApplyKnockback(GameObject target)
        {
            if (_knockbackForce <= 0f)
                return;

            INhanKnockback knockbackObj = target.GetComponentInParent<INhanKnockback>();
            if (knockbackObj == null)
                return;

            Vector2 bounceDir = Vector2.up;
            PlayerMovement move = target.GetComponentInParent<PlayerMovement>();
            if (move != null)
                bounceDir.x = move.IsFacingRight ? -1f : 1f;

            knockbackObj.ApplyKnockback(bounceDir.normalized, _knockbackForce);
        }
    }
}
