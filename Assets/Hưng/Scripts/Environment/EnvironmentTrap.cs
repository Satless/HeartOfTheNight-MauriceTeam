using UnityEngine;
using HeartOfTheNight.Player;

namespace HeartOfTheNight.Environment
{
    /// <summary>
    /// Gắn script này vào các bẫy môi trường (Lava, Spikes...).
    /// Yêu cầu GameObject bẫy có Collider2D (Nên set IsTrigger = true).
    /// Để tối ưu, nên tạo một Layer riêng (vd: Trap) và thiết lập Collision Matrix để bẫy chỉ va chạm với Player.
    /// </summary>
    public class EnvironmentTrap : MonoBehaviour
    {
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

        private void HandleCollision(GameObject target)
        {
            // Tìm script PlayerHealth trên đối tượng va chạm (hoặc cha của nó)
            PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                if (_instaKill)
                {
                    playerHealth.InstaKill();
                }
                else
                {
                    // 1. Trừ máu (nếu có sát thương)
                    if (_damage > 0)
                    {
                        playerHealth.TakeDamage(_damage);
                    }

                    // 2. Knockback (áp dụng kể cả khi damage = 0, vd: Nấm nảy, bẫy đẩy)
                    INhanKnockback knockbackObj = target.GetComponentInParent<INhanKnockback>();
                    if (knockbackObj != null)
                    {
                        Vector2 bounceDir = Vector2.up; // Hướng nảy mặc định văng lên trên
                        
                        PlayerMovement move = target.GetComponentInParent<PlayerMovement>();
                        if (move != null)
                        {
                            // Văng dội ngược lại hướng nhân vật đang đứng
                            bounceDir.x = move.IsFacingRight ? -1f : 1f;
                        }
                        
                        knockbackObj.ApplyKnockback(bounceDir.normalized, _knockbackForce);
                    }
                }
            }
        }
    }
}
