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
        [Tooltip("Dải màu của thanh nhiệt (ví dụ: Trắng/Xanh -> Vàng -> Đỏ)")]
        [SerializeField] private Gradient heatGradient;

        [Header("Health Bar")]
        [Tooltip("Kéo GameObject 'Mau' vào đây")]
        [SerializeField] private GameObject healthContainer;
        [Tooltip("Kéo Prefab 'MauTrong' vào đây (1 ô đại diện cho 10 máu)")]
        [SerializeField] private GameObject healthBlockPrefab;

        [Header("Damage Popup")]
        [Tooltip("Kéo Prefab 'HienThiSatThuong' vào đây để hệ thống hiển thị số khi bắn trúng")]
        [SerializeField] private GameObject damagePopupPrefab;

        [Header("Death Settings")]
        [Tooltip("Kéo các GameObject muốn ẩn khi chết vào đây (HUD_HeartAGun, HUD_Keyboard, KeyHUD, HUDKeyboardController...)")]
        [SerializeField] private GameObject[] elementsToHideOnDeath;

        [Header("Keyboard Controller")]
        [SerializeField] private HUDKeyboardController keyboardController;

        private HeartOfTheNight.Player.PlayerHealth _playerHealth;
        private HeartOfTheNight.Player.PlayerAttack _playerAttack;

        private void Start()
        {
            if (damagePopupPrefab != null)
            {
                HeartOfTheNight.UI.DamagePopup.SetupPrefab(damagePopupPrefab);
            }

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
                _playerHealth.OnDeath += HandlePlayerDeath;
                // Cập nhật UI lần đầu với lượng máu hiện tại
                UpdateHealth(_playerHealth.GetCurrentHealth(), _playerHealth.MaxHealth); 
            }
            else
            {
                Debug.LogWarning("[HUDManager] Không tìm thấy PlayerHealth trong Scene!");
            }

            if (_playerAttack != null)
            {
                _playerAttack.OnHeatChanged += UpdateHeat;
                _playerAttack.OnWeaponChanged += UpdateWeapon;
                _playerAttack.OnWeaponUnlocked += HandleWeaponUnlocked;

                // Force update ngay lập tức để hiển thị vũ khí mặc định
                // (phòng trường hợp PlayerAttack.Start() đã chạy trước và event đã fire rồi)
                if (_playerAttack.Data != null)
                    UpdateWeapon(_playerAttack.Data);

                // Cũng sync thanh nhiệt ban đầu
                UpdateHeat(_playerAttack.CurrentHeat, _playerAttack.MaxHeat);

                // Khởi tạo trạng thái hiển thị của các phím súng (ẩn súng chưa mở khóa)
                if (keyboardController == null)
                    keyboardController = Object.FindFirstObjectByType<HUDKeyboardController>();
                
                if (keyboardController != null)
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        keyboardController.SetWeaponKeyVisibility(i, _playerAttack.IsWeaponUnlocked(i));
                    }
                }
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
                _playerHealth.OnDeath -= HandlePlayerDeath;
            }

            if (_playerAttack != null)
            {
                _playerAttack.OnHeatChanged -= UpdateHeat;
                _playerAttack.OnWeaponChanged -= UpdateWeapon;
                _playerAttack.OnWeaponUnlocked -= HandleWeaponUnlocked;
            }
        }

        private void HandleWeaponUnlocked(int slotIndex)
        {
            if (keyboardController != null)
            {
                keyboardController.SetWeaponKeyVisibility(slotIndex, true);
            }
        }

        private void HandlePlayerDeath()
        {
            Debug.Log($"[HUDManager] Nhận sự kiện OnDeath! Bắt đầu tắt giao diện...");

            if (elementsToHideOnDeath == null || elementsToHideOnDeath.Length == 0)
            {
                Debug.LogWarning("[HUDManager] Chưa gán các UI cần tắt vào mảng 'elementsToHideOnDeath' trong Inspector!");
                return;
            }

            int disabledCount = 0;
            foreach (var element in elementsToHideOnDeath)
            {
                if (element != null)
                {
                    element.SetActive(false);
                    disabledCount++;
                    Debug.Log($"[HUDManager] Đã ẩn UI: {element.name}");
                }
            }
            
            Debug.Log($"[HUDManager] Quét xong. Đã ẩn {disabledCount}/{elementsToHideOnDeath.Length} đối tượng được gán.");
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
                float percent = maxHeat > 0 ? currentHeat / maxHeat : 0;
                heatBarFill.fillAmount = percent;
                
                // Đổi màu thanh nhiệt (Xanh -> Đỏ)
                if (heatGradient != null)
                {
                    heatBarFill.color = heatGradient.Evaluate(percent);
                }
            }
        }

        private void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (healthContainer == null) return;

            int expectedBlocks = Mathf.Max(1, maxHealth / 10); // Đảm bảo luôn có ít nhất 1 ô (nếu maxHealth < 10)

            // Tự động sinh/ẩn block nếu có Prefab (Inline Pool — Zero-GC)
            if (healthBlockPrefab != null)
            {
                // Đếm số ô đang active
                int activeCount = 0;
                for (int i = 0; i < healthContainer.transform.childCount; i++)
                {
                    if (healthContainer.transform.GetChild(i).gameObject.activeSelf)
                        activeCount++;
                }
                
                // Tạo thêm nếu thiếu: ưu tiên tái sử dụng child inactive trước khi Instantiate
                if (activeCount < expectedBlocks)
                {
                    // Bước 1: Bật lại các child đang inactive (tái sử dụng)
                    for (int i = 0; i < healthContainer.transform.childCount && activeCount < expectedBlocks; i++)
                    {
                        GameObject child = healthContainer.transform.GetChild(i).gameObject;
                        if (!child.activeSelf)
                        {
                            child.SetActive(true);
                            activeCount++;
                        }
                    }
                    
                    // Bước 2: Nếu vẫn thiếu (chưa từng tạo bao giờ) → Instantiate bổ sung
                    while (activeCount < expectedBlocks)
                    {
                        Instantiate(healthBlockPrefab, healthContainer.transform);
                        activeCount++;
                    }
                }
                
                // Ẩn đi nếu dư (trường hợp maxHealth bị giảm) — KHÔNG Destroy, giữ lại để tái sử dụng
                if (activeCount > expectedBlocks)
                {
                    int toHide = activeCount - expectedBlocks;
                    // Duyệt ngược để ẩn từ cuối lên
                    for (int i = healthContainer.transform.childCount - 1; i >= 0 && toHide > 0; i--)
                    {
                        GameObject child = healthContainer.transform.GetChild(i).gameObject;
                        if (child.activeSelf)
                        {
                            child.SetActive(false);
                            toHide--;
                        }
                    }
                }
            }

            // Tính số ô máu CẦN BẬT dựa trên 10 máu = 1 ô
            // Dùng Mathf.CeilToInt để 1-10 máu -> bật 1 ô, 11-20 máu -> bật 2 ô...
            int activeBlocks = Mathf.CeilToInt((float)currentHealth / 10f);

            // Dựa vào cấu trúc: Mau (healthContainer) -> MauTrong -> Mauday
            for (int i = 0; i < expectedBlocks; i++)
            {
                if (i >= healthContainer.transform.childCount) break; // An toàn nếu Instantiate chưa xong trong frame
                
                Transform mauTrong = healthContainer.transform.GetChild(i);
                
                if (mauTrong.childCount > 0)
                {
                    Transform mauDay = mauTrong.GetChild(0);
                    // Bật các cục đỏ nếu index < activeBlocks
                    mauDay.gameObject.SetActive(i < activeBlocks);
                }
            }
        }
    }
}
