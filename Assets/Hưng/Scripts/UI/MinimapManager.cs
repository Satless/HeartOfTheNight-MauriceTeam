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
        if (PauseUI.IsPaused)
            return;

        if (mapUIContainer != null)
        {
            // Đảo ngược trạng thái: Nếu đang tắt thì bật, đang bật thì tắt
            mapUIContainer.SetActive(!mapUIContainer.activeSelf);
        }
    }

    [Header("Cài đặt Tracking Tọa Độ")]
    [Tooltip("Tỷ lệ thu nhỏ từ Thế giới vào Minimap. Ví dụ: 1 unit ngoài đời = 5 pixel trên UI thì điền 5")]
    public float mapScale = 5f;

    private int _currentRoomIndex;
    private Vector2 _currentRoomWorldCenter;

    /// <summary>
    /// Lưu lại thông tin phòng hiện tại
    /// </summary>
    public void SetCurrentRoom(int roomIndex, Vector2 roomWorldCenter)
    {
        if (roomIndex < 0 || roomIndex >= roomAnchors.Length) return;

        _currentRoomIndex = roomIndex;
        _currentRoomWorldCenter = roomWorldCenter;
    }

    /// <summary>
    /// Cập nhật vị trí dấu chấm liên tục theo thời gian thực (Zero-GC)
    /// </summary>
    public void UpdatePlayerPosition(Vector2 playerWorldPos)
    {
        if (_currentRoomIndex < 0 || _currentRoomIndex >= roomAnchors.Length) return;
        if (playerDot == null || roomAnchors[_currentRoomIndex] == null) return;

        // Tính khoảng cách từ tâm phòng tới player ở ngoài thế giới
        Vector2 offset = playerWorldPos - _currentRoomWorldCenter;

        // Bắt đầu từ tọa độ của điểm neo (giữa phòng)
        playerDot.position = roomAnchors[_currentRoomIndex].position;
        
        // Cộng thêm độ lệch đã nhân với tỷ lệ thu nhỏ
        // Dùng localPosition để tịnh tiến chấm đỏ trên mặt phẳng UI
        playerDot.localPosition += new Vector3(offset.x * mapScale, offset.y * mapScale, 0);
    }
}
