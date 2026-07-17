using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class WeaponSlot
{
    public GunWeaponData variant1;
    public GunWeaponData variant2;
}

public class PlayerAttack : MonoBehaviour
{
    [Header("Weapons (Press 1, 2, 3 to switch, Q to toggle variant)")]
    [Tooltip("Vũ khí 1 (Phím 1)")]
    public WeaponSlot Weapon1;
    [Tooltip("Vũ khí 2 (Phím 2)")]
    public WeaponSlot Weapon2;
    [Tooltip("Vũ khí 3 (Phím 3)")]
    public WeaponSlot Weapon3;

    [HideInInspector]
    public GunWeaponData Data; // Vũ khí đang cầm hiện tại

    private int _currentSlotIndex = 1;
    private bool _useVariant2 = false;

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
        EquipSlot(1); // Mặc định cầm súng 1 (nếu có)
    }

    private void Update()
    {
        HandleWeaponSwitching();
        HandleFacing();
        HandleFire();
    }

    private void HandleWeaponSwitching()
    {
        // Đổi súng bằng phím số
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(3);

        // Đổi biến thể bằng phím Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _useVariant2 = !_useVariant2;
            EquipSlot(_currentSlotIndex); // Re-equip slot hiện tại với biến thể mới
        }
    }

    private void EquipSlot(int slotNumber)
    {
        _currentSlotIndex = slotNumber;
        WeaponSlot slot = slotNumber == 1 ? Weapon1 : (slotNumber == 2 ? Weapon2 : Weapon3);
        
        GunWeaponData weaponToEquip = _useVariant2 ? slot.variant2 : slot.variant1;
        
        // Nếu biến thể 2 không tồn tại thì tự động lấy biến thể 1 (để khỏi văng lỗi)
        if (weaponToEquip == null) 
            weaponToEquip = slot.variant1;

        if (weaponToEquip != null)
        {
            EquipWeapon(weaponToEquip, slotNumber);
        }
    }

    private void EquipWeapon(GunWeaponData newWeapon, int slotNumber)
    {
        if (Data == newWeapon) return;

        Data = newWeapon;
        Debug.Log($"Đã chuyển sang súng {slotNumber}: {Data.name}");

        // Cập nhật lại hình ảnh súng
        if (_upperBodyVisual != null && Data.weaponAnimator != null)
        {
            Animator anim = _upperBodyVisual.GetComponent<Animator>();
            if (anim != null)
            {
                anim.runtimeAnimatorController = Data.weaponAnimator;
            }
        }
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

        // Bỏ qua nếu chưa trang bị súng
        if (Data == null) return;

        // Hướng bắn độc lập với chân, tính theo hướng ngắm chuột
        float dirX = _isAimingRight ? 1f : -1f;

        // Số lượng đạn bắn ra (Pistol=1, Shotgun=5...)
        int bulletsToShoot = Data.bulletsPerShot > 0 ? Data.bulletsPerShot : 1;

        for (int i = 0; i < bulletsToShoot; i++)
        {
            // Lấy đạn từ pool thay vì Instantiate — Zero GC
            Bullet bullet = _bulletPool.Get(_firePoint.position);
            bullet.Activate(Data.bulletLifetime, Data.damage);

            // Tính toán góc lệch ngẫu nhiên (spread)
            float randomSpread = UnityEngine.Random.Range(-Data.spreadAngle, Data.spreadAngle);
            
            // Vector hướng bay gốc
            Vector2 baseDirection = new Vector2(dirX, 0f);
            
            // Xoay vector hướng bay theo spreadAngle
            Vector2 finalDirection = Quaternion.Euler(0, 0, randomSpread) * baseDirection;

            // Truyền gia tốc cho đạn
            bullet.RB.linearVelocity = finalDirection.normalized * Data.bulletSpeed;
        }

        // --- Hiệu ứng giật súng (Recoil) với DOTween ---
        if (_upperBodyVisual != null)
        {
            Animator anim = _upperBodyVisual.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Fire");
            }

            _upperBodyVisual.DOKill(); // Dừng tween cũ
            
            // Rút ngắn thời gian giật nếu fireRate quá nhanh (như Minigun) để hiệu ứng không đứt gãy
            float recoilDuration = Mathf.Min(0.1f, Data.fireRate * 0.8f);
            
            Vector3 recoilForce = new Vector3(-dirX * 0.15f, 0.03f, 0f);
            _upperBodyVisual.DOPunchPosition(recoilForce, recoilDuration, 1, 0.5f).SetRelative(true);
        }
    }
}
