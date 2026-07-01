using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Tooltip("Kéo thẳng ScriptableObject của súng vào đây")]
    public GunWeapon Data;

    [Header("Camera")]
    [Tooltip("Kéo thẳng camera trên hierarchy vào ô này. Dùng để tính tọa độ ngắm bắn ngang theo chuột.")]
    [SerializeField] private Camera _mainCamera;

    [Header("References")]
    [Tooltip("Kéo child phần sinh đạn của người chơi vào đây.")]
    [SerializeField] private Transform _firePoint;
    [Tooltip("Kéo cái kho đạn object pooling vào đây.")]
    [SerializeField] private BulletPool _bulletPool;

    // Tái dùng PlayerMovement để gọi CheckDirectionToFace và đọc IsWallJumpLocked
    private PlayerMovement _movement;

    private float _lastFireTime;

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
        // Khóa ngắm chuột theo điều kiện:
        // Nếu ĐANG bị khóa di chuyển do bật tường VÀ cấu hình yêu cầu lật người theo hướng bật tường (doTurnOnWallJump = true)
        if (_movement.IsWallJumpLocked && _movement.Data.doTurnOnWallJump) return;

        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        bool mouseIsRight = mouseWorld.x > transform.position.x;
        _movement.CheckDirectionToFace(mouseIsRight);
    }

    // ─── FIRE ────────────────────────────────────────────────────────────────

    private void HandleFire()
    {
        // Khóa bắn khi đang bám tường / leo tường
        if (_movement.IsSliding) return;

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

        // Hướng bắn chỉ theo trục ngang (trái hoặc phải)
        float dirX = _movement.IsFacingRight ? 1f : -1f;

        // Dùng Rigidbody2D đã cache sẵn trong Bullet — không GetComponent runtime
        bullet.RB.AddForce(new Vector2(dirX, 0f) * Data.bulletSpeed, ForceMode2D.Impulse);
    }
}
