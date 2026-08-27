using UnityEngine;

/// <summary>
/// Gắn lên Prefab vật phẩm (Máu, Tiền...). 
/// Yêu cầu: Có Collider2D (để IsTrigger = true) và Layer đúng với thiết lập trong PlayerMagnet.
/// </summary>
public class CollectibleItem : MonoBehaviour
{
    [Header("Item Data")]
    public ItemData data;

    [Header("Debug Tracking")]
    [Tooltip("Đã bị nhặt chưa (ngăn chặn việc nhặt đúp nhiều lần)")]
    [SerializeField, ReadOnly] private bool _isCollected;
    public bool IsCollected 
    { 
        get => _isCollected; 
        private set => _isCollected = value; 
    }
    
    [Tooltip("Vận tốc bay lơ lửng về phía người chơi hiện tại")]
    [SerializeField, ReadOnly] private float _currentSpeed;
    
    [Tooltip("Lần cuối cùng bị lực nam châm tác động (nếu mất lực sẽ rớt xuống đất)")]
    [SerializeField, ReadOnly] private float _lastPullTime;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Nếu đang lơ lửng (bị hút) nhưng người chơi chạy quá nhanh ra khỏi vùng từ trường
        if (!IsCollected && _rb != null && _rb.bodyType == RigidbodyType2D.Kinematic)
        {
            // PlayerMagnet ngừng gọi PullTowards quá 0.1s -> Rơi xuống lại
            if (Time.time - _lastPullTime > 0.1f)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _currentSpeed = 0f;
            }
        }
    }

    /// <summary>
    /// Được gọi mỗi frame từ PlayerMagnet khi vật phẩm nằm trong từ trường.
    /// Dùng MoveTowards thay vì DOTween để đạn bay theo dấu mục tiêu đang di chuyển liên tục.
    /// </summary>
    public void PullTowards(Transform target, float baseSpeed, float maxSpeed, float acceleration)
    {
        // Nếu chưa gán data thì chặn luôn
        if (IsCollected || data == null) return;

        _lastPullTime = Time.time;

        // Tắt vật lý (trọng lực) để đồng tiền bay lơ lửng mượt mà về phía người chơi
        if (_rb != null && _rb.bodyType != RigidbodyType2D.Kinematic)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
        }

        // Tăng tốc độ bay theo thời gian (gia tốc) nhưng không vượt quá Tốc độ Tối đa (maxSpeed)
        _currentSpeed += acceleration * Time.deltaTime;
        float actualSpeed = Mathf.Min(baseSpeed + _currentSpeed, maxSpeed);

        transform.position = Vector3.MoveTowards(transform.position, target.position, actualSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) <= data.collectDistance)
        {
            Collect(target.gameObject);
        }
    }

    private void Collect(GameObject player)
    {
        IsCollected = true;
        
        if (data == null) return;

        AudioEvents.TriggerSound3D("Player", "CollectItems", "n", transform.position);

        // TODO: Liên kết với hệ thống máu/tiền thực tế của người chơi.
        // Ví dụ:
        // player.GetComponent<PlayerStats>().AddCoin(data.value);
        Debug.Log($"[Item Magnet] Đã nhặt được {data.value} {data.itemName}!");

        // Ẩn object đi. (Khuyến nghị dùng Object Pool khi game thực tế sinh ra nhiều tiền).
        gameObject.SetActive(false);

    }

    private void OnEnable()
    {
        // Reset trạng thái khi được kích hoạt lại (phục vụ cho Object Pool)
        IsCollected = false;
        _currentSpeed = 0f;
        
        // Trả lại vật lý để nó có thể rơi xuống đất lần tiếp theo
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (data != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, data.collectDistance);
        }
    }
}
