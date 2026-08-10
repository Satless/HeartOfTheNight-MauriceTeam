#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using TMPro;
using HeartOfTheNight.Player;

/// <summary>
/// Bảng Debug trực quan trên Canvas. Bật/tắt bằng phím F1.
/// Hiển thị: Trạng thái FSM, Animation đang chạy, Súng hiện tại, Input người chơi.
/// Chỉ tồn tại trong Editor và Development Build — bản Release sạch sẽ.
/// </summary>
public class DebugPanelController : MonoBehaviour
{
    [Header("References (Auto-Find nếu bỏ trống)")]
    [Tooltip("Kéo GameObject Player vào đây, hoặc script tự tìm trong Scene")]
    [SerializeField] private GameObject _player;

    [Header("UI Elements")]
    [Tooltip("Root Canvas chứa panel debug (sẽ bật/tắt scale)")]
    [SerializeField] private RectTransform _debugCanvas;

    // Auto-resolved từ _player
    private PlayerMovement _movement;
    private PlayerAttack _attack;
    private PlayerAnimation _animation;

    // Text hiển thị giá trị (children)
    private TMP_Text _txtTenTrangthai;  // child "TenTrangthai" của "TrangThai"
    private TMP_Text _txtTenAnimation;  // child "TenAnimation" của "Animation"

    // Nháy sáng: Súng slot indicators
    private TMP_Text _txtSung1;
    private TMP_Text _txtSung2;
    private TMP_Text _txtSung3;

    // Nháy sáng: Input indicators
    private TMP_Text _txtDoivukhi;
    private TMP_Text _txtNhay;
    private TMP_Text _txtLuot;
    private TMP_Text _txtTancong;

    // Nháy sáng: WASD (con của Dichuyen)
    private TMP_Text _txtA;
    private TMP_Text _txtS;
    private TMP_Text _txtD;
    private TMP_Text _txtW;

    // Trạng thái hiện/ẩn
    private bool _isVisible = false;

    // Cache màu — tránh tạo Color mới mỗi frame
    private static readonly Color DIM_COLOR = new Color(1f, 1f, 1f, 0.19f);
    private static readonly Color BRIGHT_COLOR = new Color(1f, 1f, 1f, 1f);
    private static readonly Color ACTIVE_SLOT_COLOR = new Color(0f, 1f, 0.5f, 1f); // Xanh lá sáng

    private void Awake()
    {
        // Auto-find Player nếu chưa kéo thả
        if (_player == null)
        {
            var found = FindFirstObjectByType<PlayerMovement>();
            if (found != null) _player = found.gameObject;
        }

        // Resolve các component từ Player
        if (_player != null)
        {
            _movement = _player.GetComponent<PlayerMovement>();
            _attack = _player.GetComponent<PlayerAttack>();
            _animation = _player.GetComponent<PlayerAnimation>();
        }

        // Tìm các TMP_Text theo tên GameObject trong hierarchy
        if (_debugCanvas != null)
        {
            var khung = _debugCanvas.GetChild(0); // Khung
            CacheUIReferences(khung);
        }

        // Ẩn panel lúc đầu
        SetPanelVisible(false);
    }

    private void CacheUIReferences(Transform khung)
    {
        // TrangThai → child TenTrangthai (hiện tên state)
        Transform trangThai = khung.Find("TrangThai");
        if (trangThai != null)
            _txtTenTrangthai = FindTMP(trangThai, "TenTrangthai");

        // Animation → child TenAnimation (hiện tên clip)
        Transform animation = khung.Find("Animation");
        if (animation != null)
            _txtTenAnimation = FindTMP(animation, "TenAnimation");

        // Sung → children 1, 2, 3 (nháy sáng số slot)
        Transform sung = khung.Find("Sung");
        if (sung != null)
        {
            _txtSung1 = FindTMP(sung, "1");
            _txtSung2 = FindTMP(sung, "2");
            _txtSung3 = FindTMP(sung, "3");
        }

        // Input indicators
        _txtDoivukhi = FindTMP(khung, "Doivukhi");
        _txtNhay = FindTMP(khung, "Nhay");
        _txtLuot = FindTMP(khung, "Luot");
        _txtTancong = FindTMP(khung, "Tancong");

        // WASD con của Dichuyen
        Transform dichuyen = khung.Find("Dichuyen");
        if (dichuyen != null)
        {
            _txtA = FindTMP(dichuyen, "A");
            _txtS = FindTMP(dichuyen, "S");
            _txtD = FindTMP(dichuyen, "D");
            _txtW = FindTMP(dichuyen, "W");
        }
    }

    /// <summary>Tìm TMP_Text trên child có tên nhất định.</summary>
    private TMP_Text FindTMP(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private void Update()
    {
        // Toggle bằng F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _isVisible = !_isVisible;
            SetPanelVisible(_isVisible);
        }

        if (!_isVisible) return;

        UpdateInfoTexts();
        UpdateInputIndicators();
    }

    private void SetPanelVisible(bool visible)
    {
        if (_debugCanvas != null)
            _debugCanvas.localScale = visible ? Vector3.one : Vector3.zero;
    }

    // ─── CẬP NHẬT THÔNG TIN ─────────────────────────────────────────────────

    private void UpdateInfoTexts()
    {
        // 1. Trạng thái FSM → ghi tên vào child TenTrangthai
        if (_txtTenTrangthai != null && _movement != null)
            _txtTenTrangthai.text = _movement.CurrentState.ToString();

        // 2. Animation đang chạy → ghi tên vào child TenAnimation
        if (_txtTenAnimation != null && _animation != null)
            _txtTenAnimation.text = _animation.CurrentAnimName ?? "";

        // 3. Súng — chỉ nháy sáng số slot, không hiện tên
        if (_attack != null)
        {
            int slot = _attack.CurrentSlotIndex;
            SetIndicator(_txtSung1, slot == 1, ACTIVE_SLOT_COLOR);
            SetIndicator(_txtSung2, slot == 2, ACTIVE_SLOT_COLOR);
            SetIndicator(_txtSung3, slot == 3, ACTIVE_SLOT_COLOR);
        }
    }

    // ─── CẬP NHẬT INPUT INDICATORS ──────────────────────────────────────────

    private void UpdateInputIndicators()
    {
        if (_movement == null || _attack == null) return;

        Vector2 moveInput = _movement.MoveInput;

        // Di chuyển: A S D W
        SetIndicator(_txtA, moveInput.x < -0.1f);
        SetIndicator(_txtD, moveInput.x > 0.1f);
        SetIndicator(_txtW, moveInput.y > 0.1f);
        SetIndicator(_txtS, moveInput.y < -0.1f);

        // Đổi chế độ súng (Q) — sáng khi đang ở variant 2
        SetIndicator(_txtDoivukhi, _attack.IsUsingVariant2);

        // Nhảy — sáng khi phím Space đang được nhấn (Input System IsPressed)
        SetIndicator(_txtNhay, _movement.IsPressingJump);

        // Lướt (Dash) — sáng khi phím Dash đang được nhấn hoặc đang Dashing
        SetIndicator(_txtLuot, _movement.IsPressingDash || _movement.IsDashing);

        // Tấn công — sáng khi đang nhấn giữ chuột trái
        SetIndicator(_txtTancong, _attack.IsPressingFire);
    }

    /// <summary>
    /// Chuyển alpha của text giữa sáng (active) và mờ (inactive).
    /// </summary>
    private void SetIndicator(TMP_Text txt, bool active, Color? activeColor = null)
    {
        if (txt == null) return;
        txt.color = active ? (activeColor ?? BRIGHT_COLOR) : DIM_COLOR;
    }
}
#endif
