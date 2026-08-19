using UnityEngine;
using UnityEngine.InputSystem;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance { get; private set; }

    [Header("Cài đặt Bật/Tắt Map")]
    [Tooltip("Kéo object chứa toàn bộ Map (ví dụ Image nền) vào đây")]
    public GameObject mapUIContainer;

    [Header("Minimap UI Components")]
    [Tooltip("Kéo các GameObject điểm neo (RoomAnchor) vào đây theo đúng thứ tự phòng")]
    public RectTransform[] roomAnchors;

    [Tooltip("Kéo UI Image chấm của Player vào đây")]
    public RectTransform playerDot;

    // --- BIẾN NEW INPUT SYSTEM ---
    private InputSystem_Actions _input;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Khởi tạo Input System
        _input = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        // Bật toàn bộ Input và lắng nghe sự kiện phím ToggleMap ở bên map UI
        _input.Enable();
        _input.UI.ToggleMap.performed += OnToggleMap;
    }

    private void OnDisable()
    {
        // Hủy lắng nghe sự kiện và tắt Input để dọn rác (Chống Memory Leak)
        _input.UI.ToggleMap.performed -= OnToggleMap;
        _input.Disable();
    }

    private void Start()
    {
        // Tắt map đi khi mới load vào scene
        if (mapUIContainer != null)
        {
            mapUIContainer.SetActive(false);
        }
    }

    // --- HÀM NÀY SẼ TỰ CHẠY MỘT LẦN KHI BẠN NHẤN PHÍM TAB ---
    private void OnToggleMap(InputAction.CallbackContext context)
    {
        if (mapUIContainer != null)
        {
            // Đảo ngược trạng thái: Nếu đang tắt thì bật, đang bật thì tắt
            mapUIContainer.SetActive(!mapUIContainer.activeSelf);
        }
    }

    /// <summary>
    /// Di chuyển chấm player đến phòng tương ứng (vẫn chạy ngầm ngay cả khi Map đang tắt)
    /// </summary>
    public void SetCurrentRoom(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= roomAnchors.Length) return;

        if (playerDot != null && roomAnchors[roomIndex] != null)
        {
            playerDot.position = roomAnchors[roomIndex].position;
        }
    }
}
