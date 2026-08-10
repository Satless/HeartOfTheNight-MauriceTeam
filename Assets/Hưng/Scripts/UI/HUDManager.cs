using UnityEngine;
using UnityEngine.UI;
using HeartOfTheNight.Player;

namespace HeartOfTheNight.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private Image weaponIcon;

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
                    // Bật Preserve Aspect tự động qua code cho chắc
                    weaponIcon.preserveAspect = true;
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
