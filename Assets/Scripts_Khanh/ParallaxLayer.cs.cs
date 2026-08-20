using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("0 = đứng yên trong phòng. Gần 1 = đi gần như camera (layer xa).")]
    public float parallaxFactor = 0.3f;

    [SerializeField]
    bool affectY = true;

    Vector3 _startLocal;
    bool _captured;

    void Awake()
    {
        CaptureStart();
    }

    void CaptureStart()
    {
        _startLocal = transform.localPosition;
        _captured = true;
    }

    public void Apply(Vector2 cameraDeltaFromRoom)
    {
        if (!_captured)
            CaptureStart();

        transform.localPosition = _startLocal + new Vector3(
            cameraDeltaFromRoom.x * parallaxFactor,
            affectY ? cameraDeltaFromRoom.y * parallaxFactor : 0f,
            0f);
    }

    // Giữ cho ParallaxBackground cũ compile; RoomParallax dùng Apply().
    public void Move(float delta)
    {
        Vector3 newPos = transform.localPosition;
        newPos.x -= delta * parallaxFactor;
        transform.localPosition = newPos;
    }
}
