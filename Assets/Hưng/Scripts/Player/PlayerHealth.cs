using UnityEngine;
using System;
using HeartOfTheNight.Common;
using HeartOfTheNight.Hung;

namespace HeartOfTheNight.Player
{
    /// <summary>
    /// Quản lý máu cơ bản của Player, tương thích với hệ thống tấn công của Enemy (IDamageable).
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private int _maxHealth;

        [Header("Debug Tracking")]
        [SerializeField, ReadOnly] private int _currentHealth;

        public event Action<int, int> OnHealthChanged;

        public int GetCurrentHealth() => _currentHealth;

        private void Start()
        {
            SyncHealthFromSave();
        }

        public void SyncHealthFromSave()
        {
            // Lấy máu từ Save Data, nếu Data bằng 0 (lần đầu chơi) thì lấy maxHealth
            if (HeartOfTheNight.Hung.DataManager.Instance != null && HeartOfTheNight.Hung.DataManager.Instance.Data.playerHealth > 0)
            {
                _currentHealth = HeartOfTheNight.Hung.DataManager.Instance.Data.playerHealth;
            }
            else
            {
                _currentHealth = _maxHealth;
                
                // Đồng bộ ngược lại vào Data (trên RAM)
                if (HeartOfTheNight.Hung.DataManager.Instance != null)
                    HeartOfTheNight.Hung.DataManager.Instance.Data.playerHealth = _currentHealth;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (_currentHealth <= 0) return;

            _currentHealth -= amount;
            _currentHealth = Mathf.Max(_currentHealth, 0);
            SoundManager.Instance.PlaySound3D("Player", "Hurt", transform.position);


            // Đồng bộ máu mới vào DataManager (chỉ lưu trên RAM, chưa ghi ra file để tránh giật lag)
            if (HeartOfTheNight.Hung.DataManager.Instance != null)
            {
                HeartOfTheNight.Hung.DataManager.Instance.Data.playerHealth = _currentHealth;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            Debug.Log($"[PlayerHealth] Player bị trừ <color=red>{amount}</color> máu. Máu còn lại: <color=green>{_currentHealth}</color>");

            if (_currentHealth <= 0)
            {
                Die();
                SoundManager.Instance.PlaySound3D("Player", "Death", transform.position);
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
        //đạt vừa thêm 9h30 8/3/2026
        public void HealToFull()
        {
            _currentHealth = _maxHealth;
            if (HeartOfTheNight.Hung.DataManager.Instance != null)
                HeartOfTheNight.Hung.DataManager.Instance.Data.playerHealth = _currentHealth;
        }
    }
}
