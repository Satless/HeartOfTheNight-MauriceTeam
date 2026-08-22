using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; 
using TMPro; // Thêm thư viện TextMeshPro

namespace HeartOfTheNight.UI
{
    public class HUDKeyboardController : MonoBehaviour
    {
        [Header("Key Indicators (Đổi màu Image nền)")]
        [SerializeField] private Image _o1Shoot;
        [SerializeField] private Image _o2Jump;
        [SerializeField] private Image _o3Dash;
        [SerializeField] private Image _o4Map;
        [SerializeField] private Image _o5Toggle;
        [SerializeField] private Image _o6SwitchWeapon;

        [Header("Màu của O1..O5")]
        [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color _pressedColor = new Color(1f, 1f, 1f, 1f);

        [Header("Weapon Numbers (Đổi Alpha của TextMeshPro)")]
        [Tooltip("Kéo TMP_Text của Gun1..Gun4 vào đây")]
        [SerializeField] private TMP_Text _gun1;
        [SerializeField] private TMP_Text _gun2;
        [SerializeField] private TMP_Text _gun3;
        [SerializeField] private TMP_Text _gun4;

        [Header("Độ mờ của Gun1..Gun4")]
        [SerializeField] private float _normalAlpha = 0.5f;
        [SerializeField] private float _pressedAlpha = 1f;

        private InputSystem_Actions _inputActions;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            _inputActions.Enable();

            // Đăng ký đổi MÀU
            RegisterColorAction(_inputActions.Player.Attack, _o1Shoot);
            RegisterColorAction(_inputActions.Player.Jump, _o2Jump);
            RegisterColorAction(_inputActions.Player.Dash, _o3Dash);
            RegisterColorAction(_inputActions.UI.ToggleMap, _o4Map);
            RegisterColorAction(_inputActions.Player.ToggleVariant, _o5Toggle);
            
            // Đăng ký đổi MÀU cho O6 VÀ đổi ALPHA cho súng tương ứng
            RegisterWeaponAction(_inputActions.Player.Weapon1, _gun1);
            RegisterWeaponAction(_inputActions.Player.Weapon2, _gun2);
            RegisterWeaponAction(_inputActions.Player.Weapon3, _gun3);
            RegisterWeaponAction(_inputActions.Player.Weapon4, _gun4);
        }

        private void OnDisable()
        {
            if (_inputActions != null)
            {
                _inputActions.Disable();
            }
        }

        // --- HÀM XỬ LÝ ĐỔI MÀU (O1..O4) ---
        private void RegisterColorAction(InputAction action, Image img)
        {
            if (action == null || img == null) return;

            img.color = _normalColor; // Mặc định

            action.started += ctx => img.color = _pressedColor;
            action.canceled += ctx => img.color = _normalColor;
        }

        // --- HÀM XỬ LÝ ĐỔI ALPHA SÚNG + NHÁY O6 ---
        private void RegisterWeaponAction(InputAction action, TMP_Text gunText)
        {
            if (action == null) return;

            // Chỉnh alpha mặc định cho chữ
            if (gunText != null) SetTextAlpha(gunText, _normalAlpha);

            action.started += ctx => 
            {
                if (_o6SwitchWeapon != null) _o6SwitchWeapon.color = _pressedColor;
                if (gunText != null) SetTextAlpha(gunText, _pressedAlpha);
            };

            action.canceled += ctx => 
            {
                if (_o6SwitchWeapon != null) _o6SwitchWeapon.color = _normalColor;
                if (gunText != null) SetTextAlpha(gunText, _normalAlpha);
            };
        }

        private void SetTextAlpha(TMP_Text text, float alpha)
        {
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }

        private void Update()
        {
            // Bắt phím số 4 cứng vì chưa có trong Input Map
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit4Key.wasPressedThisFrame)
                {
                    if (_o6SwitchWeapon != null) _o6SwitchWeapon.color = _pressedColor;
                    if (_gun4 != null) SetTextAlpha(_gun4, _pressedAlpha);
                }
                else if (Keyboard.current.digit4Key.wasReleasedThisFrame)
                {
                    if (_o6SwitchWeapon != null) _o6SwitchWeapon.color = _normalColor;
                    if (_gun4 != null) SetTextAlpha(_gun4, _normalAlpha);
                }
            }
        }
    }
}
