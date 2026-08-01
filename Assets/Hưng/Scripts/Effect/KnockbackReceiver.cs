using UnityEngine;

/// <summary>
/// Component plug-and-play cho cơ chế đẩy lùi (Knockback).
/// Gắn lên bất kỳ GameObject nào có Rigidbody2D là xong — không cần code thêm gì.
///
/// Cách dùng:
/// 1. Kéo thả Component này lên Prefab quái/vật thể.
/// 2. Chỉnh các thông số trên Inspector.
/// 3. Đạn sẽ tự động gọi ApplyKnockback() khi trúng.
///
/// Nếu Enemy có AI phức tạp (Boss chống knockback theo phase, Quái có khiên...),
/// có thể tự implement INhanKnockback thay vì dùng Component này.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class KnockbackReceiver : MonoBehaviour, INhanKnockback
{
    [Header("Knockback Settings")]
    [Tooltip("Hệ số kháng knockback")]
    [Range(0f, 1f)]
    [SerializeField] private float _resistance;

    [Tooltip("Giảm tốc lực đẩy của knockback")]
    [Range(1f, 30f)]
    [SerializeField] private float _deceleration;

    [Header("Choáng")]
    [Tooltip("Thời gian choáng sau khi bị đẩy (giây)")]
    [SerializeField] private float _stunDuration;

    // Cache Rigidbody2D (Zero GC — không GetComponent trong gameplay)
    private Rigidbody2D _rb;

    // Trạng thái knockback nội bộ
    [Header("Debug Tracking")]
    [Tooltip("Vận tốc văng đi hiện tại")]
    [SerializeField, ReadOnly] private Vector2 _knockbackVelocity;
    [Tooltip("Cờ đánh dấu đang trong trạng thái văng đi")]
    [SerializeField, ReadOnly] private bool _isInKnockback;
    [Tooltip("Thời điểm kết thúc choáng (tính theo Time.time)")]
    [SerializeField, ReadOnly] private float _stunEndTime;

    /// <summary>
    /// Enemy script có thể đọc biến này để biết quái đang bị choáng hay không.
    /// True = đang bị đẩy lùi HOẶC đang choáng → Enemy nên tạm dừng AI.
    /// VD: if (knockbackReceiver.IsKnockedBack) return; // Tạm dừng AI
    /// </summary>
    public bool IsKnockedBack => _isInKnockback || Time.time < _stunEndTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Implement INhanKnockback. Được gọi bởi Bullet.cs khi đạn trúng mục tiêu.
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        // Miễn nhiễm hoàn toàn
        if (_resistance >= 1f) return;

        // Bỏ qua lực quá nhỏ (tránh micro-jitter)
        if (force <= 0.01f) return;

        // Giảm lực theo hệ số kháng
        float adjustedForce = force * (1f - _resistance);

        // Ghi nhận vận tốc đẩy lùi — FixedUpdate sẽ giảm tốc dần
        _knockbackVelocity = direction.normalized * adjustedForce;
        _isInKnockback = true;

        // Bắt đầu thời gian choáng (nếu có)
        if (_stunDuration > 0)
        {
            _stunEndTime = Time.time + _stunDuration;
        }
    }

    private void FixedUpdate()
    {
        if (!_isInKnockback) return;

        // Giảm tốc dọc trục X — để trượt rồi dừng tự nhiên
        // Giữ nguyên Y để trọng lực Unity xử lý (rơi, nhảy...)
        _knockbackVelocity.x = Mathf.MoveTowards(_knockbackVelocity.x, 0f, _deceleration * Time.fixedDeltaTime);

        // Áp dụng: CHỈ ghi đè X, giữ nguyên Y từ vật lý
        _rb.linearVelocity = new Vector2(_knockbackVelocity.x, _rb.linearVelocity.y);

        // Dừng khi đã hết lực đẩy
        if (Mathf.Abs(_knockbackVelocity.x) < 0.01f)
        {
            _knockbackVelocity = Vector2.zero;
            _isInKnockback = false;
        }
    }
}
