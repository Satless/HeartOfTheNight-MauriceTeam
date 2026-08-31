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

    [Header("Wobble Effect (Quỹ đạo hình Sin)")]
    [Tooltip("Biên độ sóng (độ ngoằn ngoèo). 0 = bay thẳng.")]
    public float wobbleAmplitude = 0.15f;
    
    [Tooltip("Tần số sóng (tốc độ gợn sóng).")]
    public float wobbleFrequency = 10f;

    // Thay vì ẩn đi, lôi ra Inspector cho Design xem nhưng không cho sửa để dễ debug
    [Header("Debug Tracking")]
    [Tooltip("Gia tốc hút thực tế (tự động tính).\nCông thức: (maxPullSpeed - pullSpeed) / timeToMaxSpeed")]
    [ReadOnly]
    public float pullAcceleration;

    // Pre-allocated array cho OverlapCircleNonAlloc (Zero-GC)
    private const int MAX_ITEMS_IN_RANGE = 20;
    private readonly Collider2D[] _results = new Collider2D[MAX_ITEMS_IN_RANGE];

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
        // Quét các vật phẩm xung quanh trong bán kính (NonAlloc = Zero-GC)
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, magnetRadius, _results, itemLayer);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _results[i];
            if (col != null && col.TryGetComponent(out CollectibleItem item) && !item.IsCollected)
            {
                item.PullTowards(transform, pullSpeed, maxPullSpeed, pullAcceleration, wobbleAmplitude, wobbleFrequency);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
