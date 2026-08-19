using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Parallax theo từng phòng: layer là con của phòng, chỉ trôi khi camera phòng đó đang live.
/// Cắt sang phòng khác thì nền ở lại — không bị kéo theo camera.
/// </summary>
[DisallowMultipleComponent]
public class RoomParallax : MonoBehaviour
{
    [SerializeField]
    [Tooltip("CinemachineCamera của phòng này. Để trống thì tự tìm trong parent.")]
    CinemachineCamera roomCamera;

    [SerializeField]
    [Tooltip("Chỉ parallax khi camera phòng này đang live. Tắt nếu scene không dùng Cinemachine.")]
    bool onlyWhenRoomLive = true;

    [SerializeField]
    [Tooltip("Giới hạn dịch (world) để layer không tràn sang phòng bên.")]
    Vector2 maxShift = new(10f, 4f);

    ParallaxLayer[] _layers;
    Transform _followCam;

    void Awake()
    {
        if (roomCamera == null && transform.parent != null)
            roomCamera = transform.parent.GetComponentInChildren<CinemachineCamera>(true);

        _layers = GetComponentsInChildren<ParallaxLayer>(true);
    }

    void LateUpdate()
    {
        if (_layers == null || _layers.Length == 0)
            return;

        if (onlyWhenRoomLive && roomCamera != null && !CinemachineCore.IsLive(roomCamera))
            return;

        if (_followCam == null)
        {
            Camera main = Camera.main;
            if (main == null)
                return;
            _followCam = main.transform;
        }

        Vector2 delta = (Vector2)_followCam.position - (Vector2)transform.position;
        delta.x = Mathf.Clamp(delta.x, -maxShift.x, maxShift.x);
        delta.y = Mathf.Clamp(delta.y, -maxShift.y, maxShift.y);

        for (int i = 0; i < _layers.Length; i++)
            _layers[i].Apply(delta);
    }
}
