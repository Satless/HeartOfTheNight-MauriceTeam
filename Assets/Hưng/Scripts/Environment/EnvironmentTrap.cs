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
                    playerHealth.TakeDamage(_damage);
                }
            }
        }
    }
}
