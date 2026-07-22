using UnityEngine;

/// <summary>
/// Script gắn trên Player để tạo từ trường hút vật phẩm xung quanh (Máu, Tiền...).
/// Áp dụng cơ chế quét Zero-GC (OverlapCircleNonAlloc) và gọi hàm hút của từng vật phẩm.
/// </summary>
public class PlayerMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    [Tooltip("Bán kính từ trường hút vật phẩm (máu, tiền).")]
    public float magnetRadius;
    
    [Tooltip("Layer của các vật phẩm có thể hút được. Nhớ tạo Layer 'Item' và gán cho Máu/Tiền.")]
    public LayerMask itemLayer;
    
    [Tooltip("Tốc độ bay cơ bản")]
    public float pullSpeed;
    
    [Tooltip("Tốc độ bay tối đa")]
    public float maxPullSpeed;
    
    [Tooltip("Thời gian (giây) để tăng tốc từ 'Tốc độ cơ bản' lên đến 'Tốc độ tối đa'.")]
    public float timeToMaxSpeed;

    // Biến này bị ẩn đi (Design không cần quan tâm nữa), Code sẽ tự tính!
    [HideInInspector]
    public float pullAcceleration;

    // Zero-GC array để lưu kết quả quét va chạm (tối đa hút 20 vật phẩm cùng lúc)
    private Collider2D[] _results = new Collider2D[20];

    private void OnValidate()
    {
        // Tự động tính toán và sửa lỗi nếu Design lỡ nhập số 0 hoặc số âm
        if (pullSpeed <= 0) pullSpeed = 0.1f;
        if (maxPullSpeed < pullSpeed) maxPullSpeed = pullSpeed * 3f;
        if (timeToMaxSpeed <= 0) timeToMaxSpeed = 0.1f; // Tránh lỗi chia cho 0
        
        // Tự động tính gia tốc (Design sướng rơn vì không cần nhức đầu học Vật lý nữa)
        pullAcceleration = (maxPullSpeed - pullSpeed) / timeToMaxSpeed;
    }

    private void Update()
    {
        // Quét các vật phẩm xung quanh trong bán kính mà không sinh rác bộ nhớ (Zero-GC)
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, magnetRadius, _results, itemLayer);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _results[i];
            if (col != null)
            {
                // Kiểm tra xem vật thể có chứa script CollectibleItem không
                CollectibleItem item = col.GetComponent<CollectibleItem>();
                if (item != null && !item.IsCollected)
                {
                    // Truyền transform của người chơi để vật phẩm tự bay về
                    item.PullTowards(transform, pullSpeed, maxPullSpeed, pullAcceleration);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
