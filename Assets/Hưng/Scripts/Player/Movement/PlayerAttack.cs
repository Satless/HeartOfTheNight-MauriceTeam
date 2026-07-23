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
    [Tooltip("Kéo kho chứa hiệu ứng nổ VfxPool vào đây.")]
    [SerializeField] private VfxPool _vfxPool;

    // Tái dùng PlayerMovement để đọc IsWallJumpLocked
    private PlayerMovement _movement;

    // Hiệu ứng phun lửa (tạo sẵn 1 lần, chỉ bật/tắt, không bao giờ Destroy)
    private GameObject _flamethrowerInstance;

    private float _lastFireTime;
    private bool _isAimingRight = true;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        EquipSlot(1); // Mặc định cầm súng 1 (nếu có)
    }

    private void Start()
    {
        // Khởi tạo pool với loại đạn hiện tại
        if (Data != null && Data.bulletPrefab != null)
        {
            if (_bulletPool != null)
            {
                _bulletPool.Prewarm(Data.bulletPrefab);
            }
            if (_vfxPool != null && Data.bulletPrefab.HitVfxPrefab != null)
            {
                _vfxPool.Prewarm(Data.bulletPrefab.HitVfxPrefab);
            }
        }
        
        PrewarmWeapon(Weapon1);
        PrewarmWeapon(Weapon2);
        PrewarmWeapon(Weapon3);
    }

    private void PrewarmWeapon(WeaponSlot slot)
    {
        if (slot == null) return;
        if (slot.variant1 != null && slot.variant1.bulletPrefab != null) 
        {
            _bulletPool.Prewarm(slot.variant1.bulletPrefab);
            if (_vfxPool != null && slot.variant1.bulletPrefab.HitVfxPrefab != null)
                _vfxPool.Prewarm(slot.variant1.bulletPrefab.HitVfxPrefab);
        }
        if (slot.variant2 != null && slot.variant2.bulletPrefab != null) 
        {
            _bulletPool.Prewarm(slot.variant2.bulletPrefab);
            if (_vfxPool != null && slot.variant2.bulletPrefab.HitVfxPrefab != null)
                _vfxPool.Prewarm(slot.variant2.bulletPrefab.HitVfxPrefab);
        }
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

        // Tắt súng lửa cũ nếu đang bắn mà đổi súng (chỉ tắt, KHÔNG xóa)
        if (_flamethrowerInstance != null && _flamethrowerInstance.activeSelf)
        {
            _flamethrowerInstance.SetActive(false);
            if (_upperBodyVisual != null)
            {
                Animator anim = _upperBodyVisual.GetComponent<Animator>();
                if (anim != null) anim.SetBool("Fire", false);
            }
        }

        Data = newWeapon;
        Debug.Log($"Đã chuyển sang súng {slotNumber}: {Data.name}");

        // Cập nhật lại hình ảnh súng
        if (_upperBodyVisual != null && Data.weaponAnimator != null)
        {
            Animator anim = _upperBodyVisual.GetComponent<Animator>();
            if (anim != null)
            {
                anim.runtimeAnimatorController = Data.weaponAnimator;
                anim.speed = Data.animationSpeedMultiplier;
            }
        }

        // Tạo sẵn hiệu ứng phun lửa 1 lần duy nhất (nếu chưa có)
        if (Data.isContinuousFire && Data.continuousVfxPrefab != null && _flamethrowerInstance == null)
        {
            _flamethrowerInstance = Instantiate(Data.continuousVfxPrefab);
            _flamethrowerInstance.SetActive(false);
        }
    }

    // ─── FACING ─────────────────────────────────────────────────────────────

    private void HandleFacing()
    {
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

        if (_movement.IsWallJumpLocked && _movement.Data.doTurnOnWallJump) return;

        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _isAimingRight = mouseWorld.x > transform.position.x;

        if (_upperBodyVisual != null)
        {
            Vector3 scale = _upperBodyVisual.localScale;
            scale.x = _isAimingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _upperBodyVisual.localScale = scale;
        }
    }

    // ─── FIRE ────────────────────────────────────────────────────────────────

    private void HandleFire()
    {
        // Khóa bắn khi đang bám tường hoặc lướt (tắt súng lửa nếu đang bắn)
        if ((_movement.IsSliding && !_movement.Data.allowShootWhileSliding) || 
            (_movement.IsDashing && !_movement.Data.allowShootWhileDashing))
        {
            if (_flamethrowerInstance != null && _flamethrowerInstance.activeSelf) 
            {
                _flamethrowerInstance.SetActive(false);
                if (_upperBodyVisual != null)
                {
                    Animator anim = _upperBodyVisual.GetComponent<Animator>();
                    if (anim != null) anim.SetBool("Fire", false);
                }
            }
            return;
        }

        // Logic cho Súng bắn liên tục (Súng lửa)
        if (Data != null && Data.isContinuousFire)
        {
            if (Input.GetMouseButton(0))
            {
                // Bật hiệu ứng phun lửa (đã được tạo sẵn trong EquipWeapon)
                if (_flamethrowerInstance != null && !_flamethrowerInstance.activeSelf)
                {
                    _flamethrowerInstance.SetActive(true);
                    
                    // Dùng Bool cho Súng lửa (Liên tục)
                    if (_upperBodyVisual != null)
                    {
                        Animator anim = _upperBodyVisual.GetComponent<Animator>();
                        if (anim != null) anim.SetBool("Fire", true);
                    }

                    FlamethrowerLogic logic = _flamethrowerInstance.GetComponent<FlamethrowerLogic>();
                    if (logic != null) logic.Activate(Data.statusEffect);
                }
                
                if (_flamethrowerInstance != null && _flamethrowerInstance.activeSelf)
                {
                    // Cập nhật vị trí bám theo nòng súng liên tục
                    _flamethrowerInstance.transform.position = _firePoint.position;
                    // LẬT BẰNG ROTATION Y
                    _flamethrowerInstance.transform.rotation = _isAimingRight ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);

                    // Nếu fireRate > 0 thì giật súng (DOTween)
                    if (Data.fireRate > 0 && Time.time >= _lastFireTime + Data.fireRate)
                    {
                        _lastFireTime = Time.time;
                        if (_upperBodyVisual != null)
                        {
                            _upperBodyVisual.DOKill();
                            float curDirX = _isAimingRight ? 1f : -1f;
                            Vector3 recoilForce = new Vector3(-curDirX * 0.15f, 0.03f, 0f);
                            _upperBodyVisual.DOPunchPosition(recoilForce, Mathf.Min(0.1f, Data.fireRate * 0.8f), 1, 0.5f).SetRelative(true);
                        }
                    }
                }
            }
            else
            {
                // Nhả chuột → tắt hiệu ứng (giữ lại, không xóa)
                if (_flamethrowerInstance != null && _flamethrowerInstance.activeSelf)
                {
                    _flamethrowerInstance.SetActive(false);
                    if (_upperBodyVisual != null)
                    {
                        Animator anim = _upperBodyVisual.GetComponent<Animator>();
                        if (anim != null) anim.SetBool("Fire", false);
                    }
                }
            }
            return; // Đã xử lý xong súng lửa, bỏ qua đạn thường
        }

        // Giữ chuột trái → bắn đạn thường theo fireRate
        if (Input.GetMouseButton(0) && Time.time >= _lastFireTime + Data.fireRate)
        {
            TryFire();
        }
    }

    private void OnDestroy()
    {
        // Chống leak bộ nhớ khi Player bị destroy
        if (_flamethrowerInstance != null)
        {
            Destroy(_flamethrowerInstance);
        }
    }

    private void TryFire()
    {
        _lastFireTime = Time.time;

        if (Data == null) return;

        // Kích hoạt hoạt ảnh bắn. Khi hoạt ảnh tới đúng frame, nó sẽ gọi event ExecuteShot()
        if (_upperBodyVisual != null)
        {
            Animator anim = _upperBodyVisual.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Fire");
            }
        }
    }

    public void ExecuteShot()
    {
        // Bỏ qua nếu chưa trang bị súng
        if (Data == null) return;

        // Hướng bắn độc lập với chân, tính theo hướng ngắm chuột
        float dirX = _isAimingRight ? 1f : -1f;

        // Số lượng đạn bắn ra (Pistol=1, Shotgun=5...)
        int bulletsToShoot = Data.bulletsPerShot > 0 ? Data.bulletsPerShot : 1;

        for (int i = 0; i < bulletsToShoot; i++)
        {
            if (Data.bulletPrefab == null)
            {
                Debug.LogWarning($"Súng {Data.name} chưa được gắn Bullet Prefab trong ScriptableObject!");
                return;
            }

            // Lấy đạn từ pool theo đúng loại của súng đang cầm
            Bullet bullet = _bulletPool.Get(Data.bulletPrefab, _firePoint.position);
            bullet.Activate(Data, _vfxPool);

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
            _upperBodyVisual.DOKill(); // Dừng tween cũ
            
            // Rút ngắn thời gian giật nếu fireRate quá nhanh (như Minigun) để hiệu ứng không đứt gãy
            float recoilDuration = Mathf.Min(0.1f, Data.fireRate * 0.8f);
            
            Vector3 recoilForce = new Vector3(-dirX * 0.15f, 0.03f, 0f);
            _upperBodyVisual.DOPunchPosition(recoilForce, recoilDuration, 1, 0.5f).SetRelative(true);
        }
    }
}
