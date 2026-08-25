using UnityEngine;
using System;
using System.Collections;
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
        [Tooltip("Chờ nhân vật nằm xuống xong mới hiện YOU DIED.")]
        [SerializeField] private float _deathScreenDelay = 2f;

        [Header("Debug Tracking")]
        [SerializeField, ReadOnly] private int _maxHealth;
        [SerializeField, ReadOnly] private int _currentHealth;

        public bool hasShield = false;/////
        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;

        public int MaxHealth => _maxHealth;
        public int GetCurrentHealth() => _currentHealth;

        private bool _isDead;
        public bool IsDead => _isDead;
       
        private void Start()
        {
            InitHealth();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                hasShield = false;
                TakeDamage(Mathf.Max(_currentHealth, 1) + 9999);
            }
        }
#endif

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

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            // Continue / respawn sẽ ghi máu từ save — đừng đè max lên RAM trước đó.
            if (HeartOfTheNight.Hung.DataManager.Instance != null
                && !HeartOfTheNight.Hung.DataManager.Instance.IsApplyingSpawnRestore)
            {
                HeartOfTheNight.Hung.DataManager.Instance.Data.playerHealth = _currentHealth;
            }
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

        public void InstaKill()
        {
            if (_currentHealth <= 0) return;
            
            Debug.Log("[PlayerHealth] Dính bẫy tử thần! Ép phá khiên và chết ngay lập tức.");
            hasShield = false; // Xuyên qua mọi loại khiên bảo vệ
            TakeDamage(_currentHealth); // Ép trừ đúng bằng lượng máu hiện tại để về 0
        }

        public void TakeDamage(int amount)
        {
            if (hasShield) return;

            if (_currentHealth <= 0) return;

            _currentHealth -= amount;
            _currentHealth = Mathf.Max(_currentHealth, 0);
            AudioEvents.TriggerSound3D("Player", "Hurt", "n", transform.position);


            // Hiển thị số sát thương nhảy lên đầu Player (Màu thường)
            HeartOfTheNight.UI.DamagePopup.Create(transform.position + Vector3.up * 0.5f, amount);
            
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
                //SoundManager.Instance.PlaySound3D("Player", "Death", transform.position);
                AudioEvents.TriggerSound3D("Player", "Die", "n", transform.position);
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            Debug.Log("[PlayerHealth] Player <color=red>ĐÃ CHẾT</color>!");
            OnDeath?.Invoke();
            StartCoroutine(DieRoutine());           
        }

        private IEnumerator DieRoutine()
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            var move = GetComponent<PlayerMovement>();
            if (move != null) move.enabled = false;

            var attack = GetComponent<PlayerAttack>();
            if (attack != null) attack.enabled = false;

            var anim = GetComponent<PlayerAnimation>();
            if (anim != null)
                anim.TriggerDeath();

            float delay = _deathScreenDelay > 0f ? _deathScreenDelay : 2f;
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(delay);

            var deadScreen = UnityEngine.Object.FindFirstObjectByType<DeadScreenUI>(FindObjectsInactive.Include);
            if (deadScreen != null)
                deadScreen.Show();
            else if (HeartOfTheNight.Hung.DataManager.Instance != null)
                HeartOfTheNight.Hung.DataManager.Instance.RespawnAtCheckpoint();

            Destroy(gameObject);
        }
        // ─── HỒI MÁU ────────────────────────────────────────────────────────────

        /// <summary>
        /// Hồi một lượng máu cụ thể (VD: nhặt thuốc hồi 20 máu). Không vượt quá maxHealth.
        /// </summary>
        public void Heal(int amount)
        {
            if (_currentHealth <= 0 || amount <= 0) return; // Đã chết thì không hồi

            // THÊM ĐOẠN CHẶN ANTI-HEAL VÀO ĐÂY
            AntiHeal anti = GetComponent<AntiHeal>();
            if (anti != null && anti.thoiGianConLai > 0)
            {
                Debug.Log("[PlayerHealth] Bơm máu thất bại! Đang dính hiệu ứng Anti-Heal của quái.");
                return; // Đá văng ra ngoài, không cho cộng máu
            }

            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            SyncDataAndNotify();
            Debug.Log($"[PlayerHealth] Player được hồi <color=green>{amount}</color> máu. Máu hiện tại: <color=green>{_currentHealth}/{_maxHealth}</color>");

            AudioEvents.TriggerSound3D("Effects", "Heal", "Normal", transform.position);
        }

        /// <summary>
        /// Hồi đầy máu về maxHealth (VD: nghỉ tại trạm cứu hộ, hồi sinh).
        /// </summary>
        public void HealToFull()
        {
            _currentHealth = _maxHealth;
            SyncDataAndNotify();
            Debug.Log($"[PlayerHealth] Player được hồi <color=green>ĐẦY MÁU</color>. Máu hiện tại: <color=green>{_currentHealth}/{_maxHealth}</color>");

            AudioEvents.TriggerSound3D("Effects", "Heal", "Full", transform.position);
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

            AudioEvents.TriggerSound3D("Effects", "Heal", "MaxHealthIncrease", transform.position);
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

            AudioEvents.TriggerSound3D("Effects", "Heal", "MaxHealthDecrease", transform.position);
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
