using UnityEngine;
using DG.Tweening;

public class PlayerAttack : MonoBehaviour
{
    [Tooltip("Kéo thẳng ScriptableObject của súng vào đây")]
    public GunWeaponData Data;

    [Header("Camera")]
    [Tooltip("Kéo thẳng camera trên hierarchy vào ô này. Dùng để tính tọa độ ngắm bắn ngang theo chuột.")]
    [SerializeField] private Camera _mainCamera;

    [Header("Visuals")]
    [Tooltip("Kéo child phần thân trên (súng) vào đây (Tren)")]
    [SerializeField] private Transform _upperBodyVisual;

    [Header("References")]
    [Tooltip("Kéo child phần sinh đạn của người chơi vào đây.")]
    [SerializeField] private Transform _firePoint;
    [Tooltip("Kéo cái kho đạn object pooling vào đây.")]
    [SerializeField] private BulletPool _bulletPool;

    // Tái dùng PlayerMovement để đọc IsWallJumpLocked
    private PlayerMovement _movement;

    private float _lastFireTime;
    private bool _isAimingRight = true;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        HandleFacing();
        HandleFire();
    }

    // ─── FACING ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Xoay nhân vật trái/phải dựa trên vị trí chuột.
    /// Súng chỉ lật theo nhân vật (không xoay góc), đạn chỉ bắn ngang.
    /// Bỏ qua khi đang bị tước quyền điều khiển wall jump.
    /// </summary>
    private void HandleFacing()
    {
        // Khóa ngắm chuột khi đang lướt (Ép phần trên quay theo hướng vật lý/hướng lướt)
        if (_movement.IsDashing && _movement.Data.lockFacingToDashDirection)
        {
            if (_upperBodyVisual != null)
            {
                Vector3 scale = _upperBodyVisual.localScale;
                scale.x = _movement.IsFacingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                _upperBodyVisual.localScale = scale;
            }
            return;
        }

        // Khóa ngắm chuột theo điều kiện:
        // Nếu ĐANG bị khóa di chuyển do bật tường VÀ cấu hình yêu cầu lật người theo hướng bật tường (doTurnOnWallJump = true)
        if (_movement.IsWallJumpLocked && _movement.Data.doTurnOnWallJump) return;

        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _isAimingRight = mouseWorld.x > transform.position.x;

        if (_upperBodyVisual != null)
        {
            Vector3 scale = _upperBodyVisual.localScale;
            // Ép scale X dương nếu chuột bên phải, âm nếu chuột bên trái
            scale.x = _isAimingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _upperBodyVisual.localScale = scale;
        }
    }

    // ─── FIRE ────────────────────────────────────────────────────────────────

    private void HandleFire()
    {
        // Khóa bắn khi đang bám tường (Nếu cấu hình không cho phép vừa bám tường vừa bắn)
        if (_movement.IsSliding && !_movement.Data.allowShootWhileSliding) return;

        // Khóa bắn khi lướt (Nếu cấu hình không cho phép vừa lướt vừa bắn)
        if (_movement.IsDashing && !_movement.Data.allowShootWhileDashing) return;

        // Giữ chuột trái → bắn liên tục theo fireRate
        // GetMouseButton(0) trả về true từ frame đầu giữ nút nên bao luôn cả click đơn
        if (Input.GetMouseButton(0) && Time.time >= _lastFireTime + Data.fireRate)
        {
            Fire();
        }
    }

    private void Fire()
    {
        _lastFireTime = Time.time;

        // Lấy đạn từ pool thay vì Instantiate — Zero GC
        Bullet bullet = _bulletPool.Get(_firePoint.position);
        bullet.Activate(Data.bulletLifetime, Data.damage);

        // Hướng bắn độc lập với chân, tính theo hướng ngắm chuột
        float dirX = _isAimingRight ? 1f : -1f;

        // Tốc độ gốc của đạn
        float bulletVelocityX = dirX * Data.bulletSpeed;

        bullet.RB.linearVelocity = new Vector2(bulletVelocityX, 0f);

        // --- Hiệu ứng giật súng (Recoil) với DOTween ---
        if (_upperBodyVisual != null)
        {
            _upperBodyVisual.DOKill(); // Dừng tween cũ
            // Đẩy lùi Transform ngược với hướng bắn
            Vector3 recoilForce = new Vector3(-dirX * 0.15f, 0.03f, 0f);
            _upperBodyVisual.DOPunchPosition(recoilForce, 0.1f, 1, 0.5f).SetRelative(true);
        }
    }
}
