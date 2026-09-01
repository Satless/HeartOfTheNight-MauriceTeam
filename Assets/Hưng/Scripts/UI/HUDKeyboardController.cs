using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; 
using TMPro; // Thêm thư viện TextMeshPro

namespace HeartOfTheNight.UI
{
    public class HUDKeyboardController : MonoBehaviour
    {
        [Header("Key Indicators (Đổi màu Image nền)")]
        [SerializeField] private Image _o0Pause; // Nút ESC
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
            
            // [QUAN TRỌNG] Đăng ký Event một lần duy nhất trong Awake để tránh Memory Leak khi Disable/Enable liên tục (ví dụ lúc Pause)
            RegisterColorAction(_inputActions.Player.Attack, _o1Shoot);
            RegisterColorAction(_inputActions.Player.Jump, _o2Jump);
            RegisterColorAction(_inputActions.Player.Dash, _o3Dash);
            RegisterColorAction(_inputActions.UI.ToggleMap, _o4Map);
            RegisterColorAction(_inputActions.Player.ToggleVariant, _o5Toggle);
            
            RegisterWeaponAction(_inputActions.Player.Weapon1, _gun1);
            RegisterWeaponAction(_inputActions.Player.Weapon2, _gun2);
            RegisterWeaponAction(_inputActions.Player.Weapon3, _gun3);
            RegisterWeaponAction(_inputActions.Player.Weapon4, _gun4);
        }

        private void OnEnable()
        {
            GameplayEvents.OnGameplayInputEnabled += HandleGameplayInputEnabled;
            HandleGameplayInputEnabled(GameplayEvents.InputEnabled);
        }

        private void OnDisable()
        {
            GameplayEvents.OnGameplayInputEnabled -= HandleGameplayInputEnabled;
            if (_inputActions != null)
                _inputActions.Disable();
        }

        private void HandleGameplayInputEnabled(bool inputEnabled)
        {
            if (_inputActions == null)
                return;

            if (inputEnabled)
            {
                _inputActions.Enable();
                if (_o0Pause != null)
                    _o0Pause.color = _normalColor;
            }
            else
            {
                _inputActions.Disable();
                if (_o0Pause != null)
                    _o0Pause.color = _pressedColor;
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

        public void SetWeaponKeyVisibility(int slotIndex, bool isVisible)
        {
            TMP_Text targetText = slotIndex == 1 ? _gun1 : (slotIndex == 2 ? _gun2 : (slotIndex == 3 ? _gun3 : _gun4));
            if (targetText != null)
            {
                // Thường Text sẽ nằm trong một Image (khung viền nút bấm), nên ta tắt parent luôn cho sạch
                if (targetText.transform.parent != null && targetText.transform.parent.GetComponent<Image>() != null)
                {
                    targetText.transform.parent.gameObject.SetActive(isVisible);
                }
                else
                {
                    targetText.gameObject.SetActive(isVisible);
                }
            }
        }

        private void Update()
        {
            if (!GameplayEvents.InputEnabled)
                return;

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
