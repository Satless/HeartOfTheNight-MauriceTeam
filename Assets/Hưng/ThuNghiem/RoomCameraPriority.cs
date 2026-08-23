using UnityEngine;
using Unity.Cinemachine;

namespace HeartOfTheNight.ThuNghiem
{
    public class RoomCameraPriority : MonoBehaviour
    {
        [Tooltip("Kéo Camera của phòng này vào đây")]
        [SerializeField] private CinemachineCamera _roomCamera;

        [Tooltip("Index của phòng này trên Minimap (để -1 nếu phòng không có trên map)")]
        [SerializeField] private int roomIndex;
        private bool _isPlayerInRoom = false;

        private Collider2D _roomCollider;
        private Transform _playerTransform;

        private void Awake()
        {
            _roomCollider = GetComponent<Collider2D>();

            // 1. Tự động tìm Camera nằm chung trong cùng 1 Phòng (cùng thư mục cha) nếu bạn lười kéo thả
            if (_roomCamera == null && transform.parent != null)
            {
                _roomCamera = transform.parent.GetComponentInChildren<CinemachineCamera>();
            }

            // 2. Tự động gán luôn cái viền (Bounding Shape) cho Camera đỡ phải kéo tay!
            if (_roomCamera != null)
            {
                var confiner = _roomCamera.GetComponent<CinemachineConfiner2D>();
                if (confiner != null && confiner.BoundingShape2D == null)
                {
                    confiner.BoundingShape2D = _roomCollider;
                    confiner.InvalidateBoundingShapeCache();
                }
            }
        }

        private void Start()
        {
            // Tắt nội suy (Blend) mặc định của Cinemachine để cắt cảnh tức thì
            var brain = Camera.main.GetComponent<CinemachineBrain>();
            if (brain != null) 
            {
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }
            
            // Tìm Player
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        // Trở lại cách dùng Update() kết hợp OverlapPoint.
        // Cách này KHÔNG sinh ra rác (Zero GC) và KHÔNG vi phạm quy tắc "No Expensive Calls" 
        // vì OverlapPoint chỉ là một phép toán kiểm tra tọa độ cực nhẹ, không phải là hàm tìm kiếm (Find).
        // Giải quyết triệt để lỗi "Camera lề mề" do Unity Physics thường bị khựng 1 nhịp khi Player dịch chuyển tức thời (Teleport).
        private void Update()
        {
            if (_playerTransform == null || _roomCamera == null) return;

            // Kiểm tra xem vị trí tâm của Player có đang nằm trong giới hạn của Phòng này không?
            if (_roomCollider.OverlapPoint(_playerTransform.position))
            {
                // Nếu có, đưa Camera này lên làm Vua (Priority 20)
                _roomCamera.Priority = 20;

                // --- LOGIC MỚI CHO MINIMAP ---
                if (MinimapManager.Instance != null && roomIndex >= 0)
                {
                    // Chỉ gọi SetCurrentRoom 1 lần khi player vừa bước vào hoặc respawn tại phòng này
                    if (!_isPlayerInRoom)
                    {
                        _isPlayerInRoom = true;
                        // Gửi thêm tọa độ tâm phòng ngoài thế giới (world space) cho Minimap
                        MinimapManager.Instance.SetCurrentRoom(roomIndex, _roomCollider.bounds.center);
                    }
                    
                    // Cập nhật vị trí liên tục theo thời gian thực (Zero-GC)
                    MinimapManager.Instance.UpdatePlayerPosition(_playerTransform.position);
                }
                // -----------------------------
            }
            else
            {
                // Nếu Player đi khỏi, giáng cấp Camera này xuống (Priority 10)
                _roomCamera.Priority = 10;
                
                // --- RESET TRẠNG THÁI MINIMAP ---
                _isPlayerInRoom = false;
            }
        }
    }
}
