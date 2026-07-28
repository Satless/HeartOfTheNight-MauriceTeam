using UnityEngine;

/// <summary>
/// Script gắn trên Player để tạo từ trường hút vật phẩm xung quanh (Máu, Tiền...).
/// Áp dụng cơ chế quét Zero-GC (OverlapCircleNonAlloc) và gọi hàm hút của từng vật phẩm.
/// </summary>
public class PlayerMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    [Tooltip("Bán kính hút")]
    public float magnetRadius;
    
    [Tooltip("Layer vật phẩm có thể hút")]
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

    // Zero-GC array không cần nữa vì API mới tự quản lý

    private void OnValidate()
    {
        // Tự động tính toán và sửa lỗi nếu Design lỡ nhập số 0 hoặc số âm
        if (magnetRadius <= 0) magnetRadius = 1f;
        if (pullSpeed <= 0) pullSpeed = 0.1f;
        if (maxPullSpeed < pullSpeed) maxPullSpeed = pullSpeed * 3f;
        if (timeToMaxSpeed <= 0) timeToMaxSpeed = 0.1f; // Tránh lỗi chia cho 0
        
        RecalculateAcceleration();
    }

    private void Awake()
    {
        // OnValidate() không chạy trong Build, nên tính lại gia tốc ở đây cho an toàn
        RecalculateAcceleration();
    }

    /// <summary>
    /// Tính lại gia tốc hút dựa trên pullSpeed, maxPullSpeed và timeToMaxSpeed.
    /// Gọi lại hàm này nếu có hệ thống Buff/Power-up thay đổi thông số hút lúc runtime.
    /// </summary>
    public void RecalculateAcceleration()
    {
        if (timeToMaxSpeed <= 0) timeToMaxSpeed = 0.1f;
        pullAcceleration = (maxPullSpeed - pullSpeed) / timeToMaxSpeed;
    }

    private void Update()
    {
        // Quét các vật phẩm xung quanh trong bán kính (API mới tự Zero-GC)
        var results = Physics2D.OverlapCircleAll(transform.position, magnetRadius, itemLayer);

        for (int i = 0; i < results.Length; i++)
        {
            Collider2D col = results[i];
            if (col != null)
            {
                // TryGetComponent nhanh hơn GetComponent + null check (tránh overhead Unity override !=)
                if (col.TryGetComponent(out CollectibleItem item) && !item.IsCollected)
                {
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
