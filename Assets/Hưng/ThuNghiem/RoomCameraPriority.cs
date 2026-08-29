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

        [Header("Secret")]
        [Tooltip("Bật = phòng bí mật, hiện trên màn Level Complete (SECRET found/total).")]
        [SerializeField] private bool isSecretRoom;
        [Tooltip("Để trống = Scene + tên object.")]
        [SerializeField] private string secretId;

        private bool _isPlayerInRoom = false;

        public bool IsSecretRoom
        {
            get
            {
                if (isSecretRoom)
                    return true;
                return NameLooksSecret(gameObject.name)
                    || (transform.parent != null && NameLooksSecret(transform.parent.name));
            }
        }

        public string SecretId
        {
            get
            {
                if (!string.IsNullOrEmpty(secretId))
                    return secretId;
                return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + gameObject.name;
            }
        }

        private static bool NameLooksSecret(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            return name.IndexOf("secret", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("hidden", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("bí mật", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("bi mat", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

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
            if (_playerTransform == null || _roomCollider == null) return;

            if (_roomCollider.OverlapPoint(_playerTransform.position))
            {
                if (_roomCamera != null)
                    _roomCamera.Priority = 20;

                if (!_isPlayerInRoom)
                {
                    _isPlayerInRoom = true;
                    if (IsSecretRoom)
                        LevelStatsTracker.DiscoverSecret(SecretId);

                    if (MinimapManager.Instance != null && roomIndex >= 0)
                        MinimapManager.Instance.SetCurrentRoom(roomIndex, _roomCollider.bounds.center);
                }

                if (MinimapManager.Instance != null && roomIndex >= 0)
                    MinimapManager.Instance.UpdatePlayerPosition(_playerTransform.position);
            }
            else
            {
                if (_roomCamera != null)
                    _roomCamera.Priority = 10;

                _isPlayerInRoom = false;
            }
        }
    }
}
