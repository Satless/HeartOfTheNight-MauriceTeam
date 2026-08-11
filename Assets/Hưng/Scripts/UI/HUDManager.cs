using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HeartOfTheNight.Player;

namespace HeartOfTheNight.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Image switchCooldownFill;
        [SerializeField] private TextMeshProUGUI cooldownText;

        [Header("Heat Bar")]
        [SerializeField] private Image heatBarFill;

        [Header("Health Bar")]
        [Tooltip("Kéo GameObject 'Mau' vào đây")]
        [SerializeField] private GameObject healthContainer;

        private HeartOfTheNight.Player.PlayerHealth _playerHealth;
        private HeartOfTheNight.Player.PlayerAttack _playerAttack;

        private void Start()
        {
            // Tự động tìm Player trong Scene
            FindPlayerAndSubscribe();
        }

        private void FindPlayerAndSubscribe()
        {
            _playerHealth = Object.FindFirstObjectByType<HeartOfTheNight.Player.PlayerHealth>();
            _playerAttack = Object.FindFirstObjectByType<HeartOfTheNight.Player.PlayerAttack>();

            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += UpdateHealth;
                // Cập nhật UI lần đầu với lượng máu hiện tại
                UpdateHealth(_playerHealth.GetCurrentHealth(), _playerHealth.GetCurrentHealth()); 
            }
            else
            {
                Debug.LogWarning("[HUDManager] Không tìm thấy PlayerHealth trong Scene!");
            }

            if (_playerAttack != null)
            {
                _playerAttack.OnHeatChanged += UpdateHeat;
                _playerAttack.OnWeaponChanged += UpdateWeapon;
                // Force update ngay lập tức để hiển thị vũ khí mặc định
                // (phòng trường hợp PlayerAttack.Start() đã chạy trước và event đã fire rồi)
                if (_playerAttack.Data != null)
                    UpdateWeapon(_playerAttack.Data);

                // Cũng sync thanh nhiệt ban đầu
                UpdateHeat(_playerAttack.CurrentHeat, _playerAttack.MaxHeat);
            }
            else
            {
                Debug.LogWarning("[HUDManager] Không tìm thấy PlayerAttack trong Scene!");
            }
        }

        private void Update()
        {
            if (_playerAttack != null)
            {
                float remainingTime = _playerAttack.SwitchEndTime - Time.time;
                
                // Cập nhật Vòng tròn (Fill)
                if (switchCooldownFill != null)
                {
                    if (remainingTime > 0 && _playerAttack.SwitchDelay > 0)
                    {
                        switchCooldownFill.fillAmount = remainingTime / _playerAttack.SwitchDelay;
                    }
                    else
                    {
                        switchCooldownFill.fillAmount = 0;
                    }
                }

                // Cập nhật Số đếm ngược (Text)
                if (cooldownText != null)
                {
                    if (remainingTime > 0)
                    {
                        cooldownText.enabled = true;
                        
                        if (remainingTime >= 0.6f)
                        {
                            // 2.0 -> 1.01: Hiện 2
                            // 1.0 -> 0.6: Hiện 1
                            cooldownText.text = Mathf.CeilToInt(remainingTime).ToString();
                        }
                        else
                        {
                            // 0.5 -> 0.0: Hiện 0.5, 0.4... 
                            // Dùng Floor để cắt bỏ việc tự động làm tròn lên của string format
                            float decimalTime = Mathf.Floor(remainingTime * 10f) / 10f;
                            // Format "0.0" (hoặc "F1") bắt buộc hiển thị đúng 1 chữ số thập phân
                            cooldownText.text = decimalTime.ToString("0.0");
                        }
                    }
                    else
                    {
                        cooldownText.enabled = false;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged -= UpdateHealth;
            }

            if (_playerAttack != null)
            {
                _playerAttack.OnHeatChanged -= UpdateHeat;
                _playerAttack.OnWeaponChanged -= UpdateWeapon;
            }
        }

        private void UpdateWeapon(GunWeaponData data)
        {
            if (weaponIcon != null && data != null)
            {
                if (data.weaponIcon != null)
                {
                    weaponIcon.sprite = data.weaponIcon;
                    weaponIcon.enabled = true;
                }
                else
                {
                    weaponIcon.enabled = false;
                }
            }
        }

        private void UpdateHeat(float currentHeat, float maxHeat)
        {
            if (heatBarFill != null)
            {
                heatBarFill.fillAmount = maxHeat > 0 ? currentHeat / maxHeat : 0;
            }
        }

        private void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (healthContainer == null) return;

            int totalBlocks = healthContainer.transform.childCount;
            if (totalBlocks == 0) return;

            // Tính số ô máu CẦN BẬT dựa trên tỷ lệ % máu hiện tại
            // Dùng Mathf.CeilToInt để lẻ 1 xíu máu (VD: 1/100) thì vẫn còn sáng 1 ô, chỉ tắt hết khi máu thật sự = 0
            float healthPercent = (float)currentHealth / maxHealth;
            int activeBlocks = Mathf.CeilToInt(healthPercent * totalBlocks);

            // Dựa vào cấu trúc: Mau (healthContainer) -> CotmauDen -> CotmauDo
            for (int i = 0; i < totalBlocks; i++)
            {
                Transform cotMauDen = healthContainer.transform.GetChild(i);
                
                if (cotMauDen.childCount > 0)
                {
                    Transform cotMauDo = cotMauDen.GetChild(0);
                    // Bật các cục đỏ nếu index < activeBlocks
                    cotMauDo.gameObject.SetActive(i < activeBlocks);
                }
            }
        }
    }
}
