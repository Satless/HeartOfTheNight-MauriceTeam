using UnityEngine;

/// <summary>
/// Component plug-and-play cho cơ chế đẩy lùi (Knockback).
/// Gắn lên GameObject có Rigidbody2D. Đạn gọi ApplyKnockback() khi trúng.
///
/// Chạy sau AI / EnemySeparation (execution order 200) để không bị ghi đè vận tốc
/// trong cùng FixedUpdate. Enemy vẫn phải dừng chase/patrol khi IsKnockedBack,
/// vì nhiều quái ghi linearVelocity trong Update (chạy sau FixedUpdate).
///
/// Hyper armor: implement IKnockbackGate trên cùng GameObject
/// (vd. đang dash / charge / cast thì CanReceiveKnockback = false).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(200)]
public class KnockbackReceiver : MonoBehaviour, INhanKnockback
{
    const float FallbackDeceleration = 12f;

    [Header("Knockback Settings")]
    [Tooltip("Hệ số kháng knockback")]
    [Range(0f, 1f)]
    [SerializeField] private float _resistance;

    [Tooltip("Giảm tốc lực đẩy của knockback. 0 trong prefab sẽ dùng 12 để không trượt vô hạn.")]
    [Range(0f, 30f)]
    [SerializeField] private float _deceleration;

    [Header("Choáng")]
    [Tooltip("Thời gian choáng sau khi bị đẩy (giây)")]
    [SerializeField] private float _stunDuration;

    private Rigidbody2D _rb;
    private IKnockbackGate _gate;

    [Header("Debug Tracking")]
    [SerializeField, ReadOnly] private Vector2 _knockbackVelocity;
    [SerializeField, ReadOnly] private bool _isInKnockback;
    [SerializeField, ReadOnly] private float _stunEndTime;

    /// <summary>
    /// True = đang bị đẩy HOẶC đang choáng → Enemy nên tạm dừng AI.
    /// </summary>
    public bool IsKnockedBack => _isInKnockback || Time.time < _stunEndTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _gate = GetComponent<IKnockbackGate>();
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (!isActiveAndEnabled) return;
        if (_rb == null || !_rb.simulated) return;
        if (_rb.bodyType == RigidbodyType2D.Static) return;
        if ((_rb.constraints & RigidbodyConstraints2D.FreezePositionX) != 0) return;
        if (_gate != null && !_gate.CanReceiveKnockback) return;
        if (_resistance >= 1f) return;
        if (force <= 0.01f) return;
        if (direction.sqrMagnitude < 0.0001f) return;

        float adjustedForce = force * (1f - _resistance);
        _knockbackVelocity = direction.normalized * adjustedForce;
        _isInKnockback = true;

        _stunEndTime = _stunDuration > 0f ? Time.time + _stunDuration : 0f;
    }

    private void FixedUpdate()
    {
        if (_rb == null || !_rb.simulated) return;

        if (!_isInKnockback)
        {
            if (Time.time < _stunEndTime)
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        float decel = _deceleration > 0.01f ? _deceleration : FallbackDeceleration;
        _knockbackVelocity.x = Mathf.MoveTowards(_knockbackVelocity.x, 0f, decel * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector2(_knockbackVelocity.x, _rb.linearVelocity.y);

        if (Mathf.Abs(_knockbackVelocity.x) < 0.01f)
        {
            _knockbackVelocity = Vector2.zero;
            _isInKnockback = false;
        }
    }

    private void OnDisable()
    {
        _isInKnockback = false;
        _knockbackVelocity = Vector2.zero;
        _stunEndTime = 0f;
    }
}
