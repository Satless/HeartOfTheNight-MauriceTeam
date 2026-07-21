using UnityEngine;

/// <summary>
/// Gắn lên Prefab vật phẩm (Máu, Tiền...). 
/// Yêu cầu: Có Collider2D (để IsTrigger = true) và Layer đúng với thiết lập trong PlayerMagnet.
/// </summary>
public class CollectibleItem : MonoBehaviour
{
    [Header("Item Data")]
    public ItemData data;

    public bool IsCollected { get; private set; }
    private float _currentSpeed;
    private Rigidbody2D _rb;
    private float _lastPullTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Nếu đang lơ lửng (bị hút) nhưng người chơi chạy quá nhanh ra khỏi vùng từ trường
        if (!IsCollected && _rb != null && _rb.isKinematic)
        {
            // PlayerMagnet ngừng gọi PullTowards quá 0.1s -> Rơi xuống lại
            if (Time.time - _lastPullTime > 0.1f)
            {
                _rb.isKinematic = false;
                _currentSpeed = 0f;
            }
        }
    }

    /// <summary>
    /// Được gọi mỗi frame từ PlayerMagnet khi vật phẩm nằm trong từ trường.
    /// Dùng MoveTowards thay vì DOTween để đạn bay theo dấu mục tiêu đang di chuyển liên tục.
    /// </summary>
    public void PullTowards(Transform target, float baseSpeed, float acceleration)
    {
        // Nếu chưa gán data thì chặn luôn
        if (IsCollected || data == null) return;

        _lastPullTime = Time.time;

        // Tắt vật lý (trọng lực) để đồng tiền bay lơ lửng mượt mà về phía người chơi
        if (_rb != null && !_rb.isKinematic)
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector2.zero;
        }

        // Tăng tốc độ bay theo thời gian (gia tốc) để tạo cảm giác hút mạnh
        _currentSpeed += acceleration * Time.deltaTime;
        float actualSpeed = baseSpeed + _currentSpeed;

        transform.position = Vector3.MoveTowards(transform.position, target.position, actualSpeed * Time.deltaTime);

        // Nếu đã bay đủ gần người chơi -> Gọi hàm nhặt
        if (Vector3.Distance(transform.position, target.position) <= data.collectDistance)
        {
            Collect(target.gameObject);
        }
    }

    private void Collect(GameObject player)
    {
        IsCollected = true;
        
        if (data == null) return;
        
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
            _rb.isKinematic = false;
        }
    }
}
