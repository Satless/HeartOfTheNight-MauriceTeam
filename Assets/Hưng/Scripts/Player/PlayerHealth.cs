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
        [SerializeField] private PlayerData _playerData;

        [Header("Debug Tracking")]
        [SerializeField, ReadOnly] private int _maxHealth;
        [SerializeField, ReadOnly] private int _currentHealth;

        public event Action<int, int> OnHealthChanged;

        public int MaxHealth => _maxHealth;
        public int GetCurrentHealth() => _currentHealth;

        private void Start()
        {
            InitHealth();
        }

        private void InitHealth()
        {
            if (_playerData != null)
            {
                _maxHealth = _playerData.baseMaxHealth;
                _currentHealth = _maxHealth;
            }
            else
            {
                Debug.LogWarning("[PlayerHealth] Chưa gán PlayerData! Tạm dùng máu mặc định = 100.");
                _maxHealth = 100;
                _currentHealth = 100;
            }

            // Đồng bộ vào DataManager để UI hoặc Save/Load khác đọc
            if (HeartOfTheNight.Hung.DataManager.Instance != null)
            {
                HeartOfTheNight.Hung.DataManager.Instance.Data.playerHealth = _currentHealth;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Khôi phục máu từ Save Data (dùng cho luồng "Continue / Tiếp tục chơi dở").
        /// Gọi bởi LevelEntrance hoặc TestSaveLoad khi người chơi chọn tiếp tục màn đang dở.
        /// </summary>
        public void SyncHealthFromSave()
        {
            if (HeartOfTheNight.Hung.DataManager.Instance != null && HeartOfTheNight.Hung.DataManager.Instance.Data.playerHealth > 0)
            {
                _currentHealth = HeartOfTheNight.Hung.DataManager.Instance.Data.playerHealth;
            }
            // Nếu save data = 0 (chưa từng lưu), giữ nguyên _currentHealth từ InitHealth()

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (_currentHealth <= 0) return;

            _currentHealth -= amount;
            _currentHealth = Mathf.Max(_currentHealth, 0);
            //SoundManager.Instance.PlaySound3D("Player", "Hurt", transform.position);


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
        // ─── HỒI MÁU ────────────────────────────────────────────────────────────

        /// <summary>
        /// Hồi một lượng máu cụ thể (VD: nhặt thuốc hồi 20 máu). Không vượt quá maxHealth.
        /// </summary>
        public void Heal(int amount)
        {
            if (_currentHealth <= 0 || amount <= 0) return; // Đã chết thì không hồi

            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            SyncDataAndNotify();
            Debug.Log($"[PlayerHealth] Player được hồi <color=green>{amount}</color> máu. Máu hiện tại: <color=green>{_currentHealth}/{_maxHealth}</color>");
        }

        /// <summary>
        /// Hồi đầy máu về maxHealth (VD: nghỉ tại trạm cứu hộ, hồi sinh).
        /// </summary>
        public void HealToFull()
        {
            _currentHealth = _maxHealth;
            SyncDataAndNotify();
            Debug.Log($"[PlayerHealth] Player được hồi <color=green>ĐẦY MÁU</color>. Máu hiện tại: <color=green>{_currentHealth}/{_maxHealth}</color>");
        }

        /// <summary>
        /// Tăng giới hạn máu tối đa (VD: nhặt Heart Container kiểu Zelda).
        /// Chỉ nâng trần, KHÔNG hồi máu. Máu hiện tại giữ nguyên.
        /// HUD sẽ tự sinh thêm ô máu nhờ Inline Pool.
        /// </summary>
        public void IncreaseMaxHealth(int amount)
        {
            if (amount <= 0) return;

            _maxHealth += amount;
            // Không cộng _currentHealth — chỉ nâng trần, để người chơi tự hồi máu
            SyncDataAndNotify();
            Debug.Log($"[PlayerHealth] Max máu tăng <color=cyan>+{amount}</color>. Máu hiện tại: <color=green>{_currentHealth}/{_maxHealth}</color>");
        }

        /// <summary>
        /// Giảm giới hạn máu tối đa (VD: dính lời nguyền).
        /// </summary>
        public void DecreaseMaxHealth(int amount)
        {
            if (amount <= 0) return;

            _maxHealth -= amount;
            _maxHealth = Mathf.Max(10, _maxHealth); // Đảm bảo max máu không bị tụt về 0 (giữ ít nhất 1 ô)
            
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth); // Kéo máu hiện tại xuống nếu bị lố
            SyncDataAndNotify();
            Debug.Log($"[PlayerHealth] Max máu giảm <color=red>-{amount}</color>. Máu hiện tại: <color=green>{_currentHealth}/{_maxHealth}</color>");
        }

#if UNITY_EDITOR
        // ─── DEBUG TOOLS DÀNH CHO DESIGNER TEST TRÊN INSPECTOR ───
        [ContextMenu("🛠️ Test: Tăng 10 Max Máu")]
        private void DebugIncreaseMax() => IncreaseMaxHealth(10);

        [ContextMenu("🛠️ Test: Giảm 10 Max Máu")]
        private void DebugDecreaseMax() => DecreaseMaxHealth(10);

        [ContextMenu("🛠️ Test: Bị đánh trừ 15 Máu")]
        private void DebugTakeDamage() => TakeDamage(15);

        [ContextMenu("🛠️ Test: Nhặt thuốc hồi 10 Máu")]
        private void DebugHeal10() => Heal(10);
#endif

        // ─── ĐỒNG BỘ NỘI BỘ ─────────────────────────────────────────────────────

        /// <summary>
        /// Đồng bộ máu vào DataManager (RAM) và thông báo UI cập nhật.
        /// </summary>
        private void SyncDataAndNotify()
        {
            if (HeartOfTheNight.Hung.DataManager.Instance != null)
                HeartOfTheNight.Hung.DataManager.Instance.Data.playerHealth = _currentHealth;

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
