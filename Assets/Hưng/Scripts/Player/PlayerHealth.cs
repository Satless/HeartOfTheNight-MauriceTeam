using UnityEngine;
using HeartOfTheNight.Common;

namespace HeartOfTheNight.Player
{
    /// <summary>
    /// Quản lý máu cơ bản của Player, tương thích với hệ thống tấn công của Enemy (IDamageable).
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private int _maxHealth = 100;

        [Header("Debug Tracking")]
        [SerializeField, ReadOnly] private int _currentHealth;

        private void Start()
        {
            _currentHealth = _maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (_currentHealth <= 0) return;

            _currentHealth -= amount;
            _currentHealth = Mathf.Max(_currentHealth, 0);

            Debug.Log($"[PlayerHealth] Player bị trừ <color=red>{amount}</color> máu. Máu còn lại: <color=green>{_currentHealth}</color>");

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("[PlayerHealth] Player <color=red>ĐÃ CHẾT</color>!");
            
            // 1. Kích hoạt Animation chết và tách cái xác ra ngoài Scene
            var anim = GetComponent<PlayerAnimation>();
            // if (anim != null) anim.TriggerDeath();

            // // 2. Tắt vật lý (đứng hình) và toàn bộ va chạm (để đạn bay xuyên qua)
            // var rb = GetComponent<Rigidbody2D>();
            // if (rb != null)
            // {
            //     rb.linearVelocity = Vector2.zero;
            //     rb.simulated = false; 
            // }

            // var colliders = GetComponentsInChildren<Collider2D>();
            // foreach (var col in colliders)
            // {
            //     col.enabled = false;
            // }

            // // 3. Khóa điều khiển
            // var move = GetComponent<PlayerMovement>();
            // if (move != null) move.enabled = false;

            // var attack = GetComponent<PlayerAttack>();
            // if (attack != null) attack.enabled = false;

            if (anim != null) 
            {
                anim.TriggerDeath();
                anim.DetachVisualsForDeath();
            }

            // 2. Xóa sổ hoàn toàn nhân vật gốc (Xóa máu, xóa logic, xóa collider)
            // Lệnh này sẽ khiến biến `player` trong code của quái vật trở thành NULL.
            // Nhờ đó, quái vật sẽ lập tức ngừng bắn và đứng im.
            Destroy(gameObject);
            
            // TODO: Bắn Event Game Over ra UI (nếu có)
        }
    }
}
