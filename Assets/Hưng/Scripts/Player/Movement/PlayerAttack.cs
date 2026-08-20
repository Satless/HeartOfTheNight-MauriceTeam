using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeartOfTheNight.Player
{
    [System.Serializable]
public class WeaponSlot
{
    public GunWeaponData variant1;
    public GunWeaponData variant2;

    [Header("Overheat")]
    [Tooltip("Nhiệt lượng tối đa")]
    public float maxHeat;
    [Tooltip("Tốc độ nguội (nhiệt/giây) khi KHÔNG bắn.")]
    public float cooldownRate;
    [Tooltip("Mức % cần nguội để được bắn lại")]
    [Range(0f, 0.9f)]
    public float unlockThreshold;
    [Tooltip("Thời gian đóng băng (giây) khi thanh nhiệt đầy. Thanh nhiệt sẽ KHÔNG tản nhiệt trong khoảng này.")]
    public float overheatFreezeDuration;

    // Runtime variables (Debug Tracking)
    [Tooltip("Nhiệt lượng hiện tại của súng. Súng sẽ ngừng bắn nếu chạm mức maxHeat.")]
    [ReadOnly] public float currentHeat;
    [Tooltip("Cờ đánh dấu súng đang bị quá nhiệt. Phải chờ nguội dưới mức unlockThreshold mới được bắn tiếp.")]
    [ReadOnly] public bool isOverheated;
    
    [HideInInspector] public float overheatFreezeEndTime; // Lưu thời điểm kết thúc đóng băng tản nhiệt

    [Header("Designer Info (Auto-Calculated)")]
    [Tooltip("Băng đạn ảo (Số viên / thời gian) xả liên tục trước khi quá nhiệt của Biến thể 1")]
    [ReadOnly] public string variant1Capacity;
    [Tooltip("Băng đạn ảo (Số viên / thời gian) xả liên tục trước khi quá nhiệt của Biến thể 2")]
    [ReadOnly] public string variant2Capacity;
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

    [Header("Debug Tracking")]
    [Tooltip("Dữ liệu ScriptableObject của khẩu súng đang được cầm trên tay (thay đổi khi bấm phím số hoặc Q).")]
    [ReadOnly] public GunWeaponData Data; // Vũ khí đang cầm hiện tại

    [Tooltip("Vị trí ô súng đang dùng (1, 2 hoặc 3).")]
    [SerializeField, ReadOnly] private int _currentSlotIndex;
    [Tooltip("Đang sử dụng bản biến thể phụ (chuyển đổi bằng phím Q).")]
    [SerializeField, ReadOnly] private bool _useVariant2 = false;

    // Tự động tìm khi cần, tự phục hồi sau khi đổi scene (DontDestroyOnLoad safe)
    private Camera _mainCameraCache;
    private Camera MainCamera
    {
        get
        {
            if (_mainCameraCache == null)
                _mainCameraCache = Camera.main;
            return _mainCameraCache;
        }
    }

    [Header("Visuals")]
    [Tooltip("Kéo child phần thân trên (súng) vào đây (Tren)")]
    [SerializeField] private Transform _upperBodyVisual;

    [Header("References")]
    [Tooltip("Kéo child phần sinh đạn của người chơi vào đây.")]
    [SerializeField] private Transform _firePoint;

    [Header("Switching")]
    [Tooltip("Thời gian delay (giây) không thể bắn sau khi đổi súng/biến thể")]
    [SerializeField] private float _switchDelay;
    private float _switchEndTime;

    // Tái dùng PlayerMovement để đọc IsWallJumpLocked
    private PlayerMovement _movement;
    private Animator _weaponAnimator;
    private PlayerAnimation _animation;

    // Hiệu ứng phun lửa (lấy từ Pool khi cầm súng lửa, trả về Pool khi đổi súng)
    private GameObject _flamethrowerInstance;

    private float _lastFireTime;
    private float _currentFireSpeedMul; // Cache tốc độ múa để dùng lúc TryFire
    private bool _isAimingRight = true;

    private InputSystem_Actions _input;
    private bool _hasInitialized = false; // Cờ theo dõi xem game đã khởi tạo xong chưa

    // ─── OVERHEAT ────────────────────────────────────────────────────────────

    private bool _isFiringThisFrame; // Để biết frame này có bắn không → quyết định nguội

    private WeaponSlot CurrentSlot => GetSlot(_currentSlotIndex);

    /// <summary> UI đọc để vẽ thanh nhiệt. </summary>
    public float CurrentHeat => CurrentSlot != null ? CurrentSlot.currentHeat : 0f;
    public float MaxHeat => CurrentSlot != null ? CurrentSlot.maxHeat : 100f;
    public bool IsOverheated => CurrentSlot != null && CurrentSlot.isOverheated;

    /// <summary> Event cho UI: (currentHeat, maxHeat). </summary>
    public event Action<float, float> OnHeatChanged;
    /// <summary> Event cho UI/SFX: true = vừa quá nhiệt, false = vừa nguội xong. </summary>
    public event Action<bool> OnOverheatStateChanged;
    public event Action<GunWeaponData> OnWeaponChanged;

    private WeaponSlot GetSlot(int slotIndex)
    {
        if (slotIndex == 1) return Weapon1;
        if (slotIndex == 2) return Weapon2;
        return Weapon3;
    }

    // ─── PROPERTIES & EVENTS CHO ANIMATION ────────────────────────────────────
    
    public bool IsPressingFire => _input != null && _input.Player.Attack.IsPressed();
    public bool IsAimingRight => _isAimingRight;
    public event Action<float, float> OnRecoil; // (dirX, fireRate)

    /// <summary>Ô súng đang dùng (1, 2, 3) — cho Debug Panel.</summary>
    public int CurrentSlotIndex => _currentSlotIndex;
    /// <summary>Đang dùng biến thể phụ (Q) — cho Debug Panel.</summary>
    public bool IsUsingVariant2 => _useVariant2;

    /// <summary>Tên animation súng đang chạy — cho Debug Panel.</summary>
    public string CurrentWeaponAnimName 
    {
        get 
        {
            if (_weaponAnimator != null && _weaponAnimator.isActiveAndEnabled && _weaponAnimator.runtimeAnimatorController != null)
            {
                var clipInfo = _weaponAnimator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0 && clipInfo[0].clip != null)
                    return clipInfo[0].clip.name;
            }
            return "";
        }
    }
    
    public float LastPressedToggleTime { get; private set; }
    
    public float SwitchDelay => _switchDelay;
    public float SwitchEndTime => _switchEndTime;


    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _animation = GetComponent<PlayerAnimation>();
        if (_upperBodyVisual != null) _weaponAnimator = _upperBodyVisual.GetComponent<Animator>();
        
        _input = new InputSystem_Actions();

        _input.Player.Weapon1.started += (InputAction.CallbackContext context) => EquipSlot(1);
        _input.Player.Weapon2.started += (InputAction.CallbackContext context) => EquipSlot(2);
        _input.Player.Weapon3.started += (InputAction.CallbackContext context) => EquipSlot(3);
        
        _input.Player.ToggleVariant.started += (InputAction.CallbackContext context) => 
        {
            LastPressedToggleTime = Time.time;
            _useVariant2 = !_useVariant2;
            EquipSlot(_currentSlotIndex);
        };
    }

    private void OnValidate()
    {
        UpdateWeaponCapacity(Weapon1);
        UpdateWeaponCapacity(Weapon2);
        UpdateWeaponCapacity(Weapon3);
    }

    private void UpdateWeaponCapacity(WeaponSlot slot)
    {
        if (slot == null) return;
        slot.variant1Capacity = CalculateCapacity(slot, slot.variant1);
        slot.variant2Capacity = CalculateCapacity(slot, slot.variant2);
    }

    private string CalculateCapacity(WeaponSlot slot, GunWeaponData data)
    {
        if (data == null) return "Trống";
        if (!data.canOverheat) return "Vô hạn (Không sinh nhiệt)";
        if (data.heatPerShot <= 0) return "Vô hạn (Nhiệt = 0)";

        if (data.isContinuousFire)
        {
            // Súng lửa: bắn liên tục không có khoảng nghỉ nguội, nhiệt cộng dồn theo giây
            float timeToOverheat = slot.maxHeat / data.heatPerShot;
            return $"{timeToOverheat:F1} giây";
        }
        else
        {
            // Nếu viên đạn đầu tiên bắn ra đã đụng nóc nhiệt ngay lập tức
            if (data.heatPerShot >= slot.maxHeat) return "1 viên (Quá nhiệt ngay lập tức)";

            // Đạn thường có khoảng nghỉ fireRate để súng tản nhiệt
            float coolPerShot = data.fireRate * slot.cooldownRate;
            float netHeat = data.heatPerShot - coolPerShot;

            if (netHeat <= 0) return "Vô hạn (Tản nhiệt nhanh hơn Sinh nhiệt)";

            // Trừ đi viên đạn đầu tiên (vì nó bắn ra khi súng nguội, chưa có thời gian nghỉ tản nhiệt)
            int shots = Mathf.CeilToInt((slot.maxHeat - data.heatPerShot) / netHeat) + 1;
            return $"{shots} viên";
        }
    }

    private void OnEnable() => _input?.Enable();
    private void OnDisable() => _input?.Disable();

    private void Start()
    {
        if (_upperBodyVisual != null && _upperBodyVisual.GetComponent<WeaponEventRelay>() == null)
        {
            Debug.LogWarning($"<color=red>[LỖI NGHIÊM TRỌNG]</color> GameObject '{_upperBodyVisual.name}' đang thiếu component 'WeaponEventRelay'. Súng sẽ KHÔNG THỂ BẮN vì đứt chuỗi Animation Event!");
        }

        // Tạo sẵn đạn và hiệu ứng cho tất cả các súng (Zero-GC khi gameplay)
        PrewarmWeapon(Weapon1);
        PrewarmWeapon(Weapon2);
        PrewarmWeapon(Weapon3);

        EquipSlot(1); // Mặc định cầm súng 1 (nếu có)
        _hasInitialized = true; // Đánh dấu đã khởi tạo xong
    }

    private void PrewarmWeapon(WeaponSlot slot)
    {
        if (slot == null) return;
        PrewarmVariant(slot.variant1);
        PrewarmVariant(slot.variant2);
    }

    private void PrewarmVariant(GunWeaponData data)
    {
        if (data == null) return;

        // Tạo sẵn đạn
        if (data.bulletPrefab != null)
        {
            data.bulletPrefab.gameObject.Prewarm(data.bulletPrewarmCount);

            // Tạo sẵn hiệu ứng nổ của đạn
            if (data.bulletPrefab.HitVfxPrefab != null)
            {
                data.bulletPrefab.HitVfxPrefab.Prewarm(data.hitVfxPrewarmCount);
            }
        }

        // Tạo sẵn hiệu ứng phun lửa
        if (data.isContinuousFire && data.continuousVfxPrefab != null)
        {
            data.continuousVfxPrefab.Prewarm(1);
        }

        // Tạo sẵn VFX hiệu ứng trạng thái (Cháy, Độc...)
        if (data.statusEffect != null && data.statusEffect.effectVfxPrefab != null)
        {
            data.statusEffect.effectVfxPrefab.Prewarm(data.statusEffect.prewarmCount);
        }
    }

    private void Update()
    {
        HandleFacing();

        _isFiringThisFrame = false;
        HandleFire();
        HandleOverheatCooldown();
    }

    private void EquipSlot(int slotNumber)
    {
        // Chặn người chơi ấn chuyển súng liên tục (chỉ chặn khi chuyển sang ô súng khác, bấm Q thì được qua)
        if (slotNumber != _currentSlotIndex && Time.time < _switchEndTime) return;

        WeaponSlot slot = slotNumber == 1 ? Weapon1 : (slotNumber == 2 ? Weapon2 : Weapon3);
        
        GunWeaponData weaponToEquip = _useVariant2 ? slot.variant2 : slot.variant1;
        
        // Nếu biến thể 2 không tồn tại thì tự động lấy biến thể 1 (để khỏi văng lỗi)
        if (weaponToEquip == null) 
            weaponToEquip = slot.variant1;

        // Chỉ tính delay nếu thực sự chuyển sang ô súng khác 
        if (_hasInitialized && slotNumber != _currentSlotIndex)
        {
            _switchEndTime = Time.time + _switchDelay;
        }

        _currentSlotIndex = slotNumber;
        if (weaponToEquip != null)
        {
            EquipWeapon(weaponToEquip, slotNumber); // Lệnh này sẽ gán Data = weaponToEquip
        }

        // Kích hoạt animation rút súng (idle) để người chơi biết mình đang cầm súng gì
        // (Bỏ qua lần đầu tiên khởi tạo ở Start)
        if (_hasInitialized && _animation != null)
        {
            _animation.TriggerWeaponSwitchDisplay();
        }

        // Cập nhật giao diện thanh nhiệt khi đổi súng
        if (slot != null)
        {
            OnHeatChanged?.Invoke(slot.currentHeat, slot.maxHeat);
            OnOverheatStateChanged?.Invoke(slot.isOverheated);
        }
    }

    private void EquipWeapon(GunWeaponData newWeapon, int slotNumber)
    {
        if (Data == newWeapon) return;

        OnWeaponChanged?.Invoke(newWeapon);

        // Trả lại súng lửa cũ về Pool (nếu có)
        if (_flamethrowerInstance != null)
        {
            _flamethrowerInstance.Despawn();
            _flamethrowerInstance = null;
            if (_weaponAnimator != null && _weaponAnimator.isActiveAndEnabled)
            {
                _weaponAnimator.SetBool("Fire", false);
            }
        }

        Data = newWeapon;
        Debug.Log($"Đã chuyển sang súng {slotNumber}: {Data.name}");

        // Cập nhật lại hình ảnh súng
        if (_weaponAnimator != null && Data.weaponAnimator != null)
        {
            _weaponAnimator.runtimeAnimatorController = Data.weaponAnimator;
            
            // TỰ ĐỘNG HÓA TỐC ĐỘ ANIMATION (Auto-Sync)
            _currentFireSpeedMul = Data.animationSpeedMultiplier; // Mặc định dự phòng
            if (Data.fireRate > 0 && !Data.isContinuousFire && Data.fireAnimationClip != null)
            {
                _currentFireSpeedMul = Data.fireAnimationClip.length / Data.fireRate;
                Debug.Log($"<color=cyan>[Auto-Sync]</color> {Data.name}: Đã lưu tốc độ Animator (FireSpeedMul) = {_currentFireSpeedMul:F2}x (Độ dài Clip: {Data.fireAnimationClip.length:F2}s / FireRate: {Data.fireRate}s)");
            }
        }

        // Lấy súng lửa mới từ Pool (nếu súng hiện tại là loại bắn liên tục)
        if (Data.isContinuousFire && Data.continuousVfxPrefab != null)
        {
            _flamethrowerInstance = Data.continuousVfxPrefab.Spawn();
            _flamethrowerInstance.SetActive(false);
        }
    }

    // ─── FACING ─────────────────────────────────────────────────────────────

    private void HandleFacing()
    {
        if (_movement.IsDashing && _movement.Data.lockFacingToDashDirection)
        {
            // Hình ảnh (quay mặt) đã được tách sang PlayerAnimation
            return;
        }

        if (_movement.IsWallJumpLocked && _movement.Data.doTurnOnWallJump) return;

        Camera cam = MainCamera;
        if (cam == null) return; // Scene chưa có camera (VD: đang loading) → bỏ qua frame này

        // Ưu tiên Pointer.current (hỗ trợ cả Touch Mobile, Pen, Mouse) thay vì hardcode Mouse
        Vector2 pointerPos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(pointerPos);
        _isAimingRight = mouseWorld.x > transform.position.x;
    }

    // ─── FIRE ────────────────────────────────────────────────────────────────

    private void HandleFire()
    {
        // Khóa bắn khi đang bám tường, lướt, hoặc bật tường (tắt súng lửa nếu đang bắn)
        if ((_movement.IsSliding && !_movement.Data.allowShootWhileSliding) || 
            (_movement.IsDashing && !_movement.Data.allowShootWhileDashing) ||
            _movement.IsWallJumping)
        {
            if (_flamethrowerInstance != null && _flamethrowerInstance.activeSelf) 
            {
                _flamethrowerInstance.SetActive(false);
                if (_weaponAnimator != null && _weaponAnimator.isActiveAndEnabled)
                {
                    _weaponAnimator.SetBool("Fire", false);
                }
            }
            return;
        }

        // Khóa bắn khi quá nhiệt (tắt súng lửa nếu đang bắn)
        if (IsOverheated)
        {
            if (_flamethrowerInstance != null && _flamethrowerInstance.activeSelf)
            {
                _flamethrowerInstance.SetActive(false);
                if (_weaponAnimator != null && _weaponAnimator.isActiveAndEnabled)
                {
                    _weaponAnimator.SetBool("Fire", false);
                }
            }
            return;
        }

        // Logic cho Súng bắn liên tục (Súng lửa)

        if (Data != null && Data.isContinuousFire)
        {
            if (_input.Player.Attack.IsPressed())
            {
                // Đảm bảo thân trên được bật trước khi Animator chạy (tránh race condition thứ tự Update)
                _animation?.ShowUpperBodyImmediately();

                // Bật hiệu ứng phun lửa (đã được tạo sẵn trong EquipWeapon)
                if (_flamethrowerInstance != null && !_flamethrowerInstance.activeSelf)
                {
                    _flamethrowerInstance.SetActive(true);
                    
                    // Dùng Bool cho Súng lửa (Liên tục)
                    if (_weaponAnimator != null && _weaponAnimator.isActiveAndEnabled)
                    {
                        _weaponAnimator.SetFloat("FireSpeedMul", _currentFireSpeedMul);
                        _weaponAnimator.SetBool("Fire", true);
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

                    // Tích nhiệt liên tục (heatPerShot = nhiệt/giây cho súng lửa)
                    if (Data.canOverheat)
                    {
                        AddHeat(Data.heatPerShot * Time.deltaTime);
                    }

                    // Nếu fireRate > 0 thì giật súng
                    if (Data.fireRate > 0 && Time.time >= _lastFireTime + Data.fireRate)
                    {
                        _lastFireTime = Time.time;
                        float curDirX = _isAimingRight ? 1f : -1f;
                        OnRecoil?.Invoke(curDirX, Data.fireRate);
                    }
                }

            }
            else
            {
                // Nhả chuột → tắt hiệu ứng (giữ lại, không xóa)
                if (_flamethrowerInstance != null && _flamethrowerInstance.activeSelf)
                {
                    _flamethrowerInstance.SetActive(false);
                    if (_weaponAnimator != null && _weaponAnimator.isActiveAndEnabled)
                    {
                        _weaponAnimator.SetBool("Fire", false);
                    }
                }
            }
            return; // Đã xử lý xong súng lửa, bỏ qua đạn thường
        }

        // Giữ chuột trái → bắn đạn thường theo fireRate
        if (_input.Player.Attack.IsPressed() && Time.time >= _lastFireTime + Data.fireRate)
        {
            TryFire();
        }
    }

    private void OnDestroy()
    {
        // Trả lại súng lửa về Pool khi Player bị destroy
        if (_flamethrowerInstance != null && Pooling.Instance != null)
        {
            _flamethrowerInstance.Despawn();
            _flamethrowerInstance = null;
        }
    }

    private void TryFire()
    {
        if (Data == null) return;

        // ═══ CHẶN KÉP: Phòng trường hợp isOverheated bật giữa lúc HandleFire() và TryFire() ═══
        // Vì Animation Event bắn đạn (ExecuteShot) chạy BẤT ĐỒNG BỘ (sau khi Animator xử lý),
        // nên có thể xảy ra: HandleFire() thấy chưa quá nhiệt → gọi TryFire() → SetTrigger("Fire").
        // Nhưng cùng frame đó, Animator xử lý trigger CŨ và ExecuteShot gây quá nhiệt.
        // Trigger MỚI vừa set sẽ bị queue lại → bắn thêm 1 phát oan.
        // Guard này triệt tiêu hoàn toàn kịch bản đó.
        if (IsOverheated) return;

        _lastFireTime = Time.time;

        if (_weaponAnimator == null || _weaponAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"<color=red>[LỖI ANIMATION]</color> Không tìm thấy Animator hoặc Controller trên súng {Data.name}! Đạn sẽ không được bắn ra.");
            return;
        }

        // Đảm bảo thân trên được bật trước khi Animator chạy (tránh race condition thứ tự Update)
        _animation?.ShowUpperBodyImmediately();

        // Kích hoạt hoạt ảnh bắn. Khi hoạt ảnh tới đúng frame, nó sẽ gọi event ExecuteShot()
        if (_weaponAnimator.isActiveAndEnabled)
        {
            _weaponAnimator.SetFloat("FireSpeedMul", _currentFireSpeedMul);
            _weaponAnimator.SetTrigger("Fire");
        }
    }

    public void ExecuteShot()
    {
        // Bỏ qua nếu chưa trang bị súng
        if (Data == null) return;

        // Bỏ qua nếu súng đã quá nhiệt (tránh lỗi Animator đã queue sẵn lệnh Fire từ frame trước)
        if (IsOverheated) return;


        //sfx for weapons
        // if (!string.IsNullOrEmpty(Data.fireSoundName))
        // {
        //         SoundManager.Instance.PlaySound3D("Weapons", Data.fireSoundName, _firePoint.position);
        // }


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

            Vector3 spawnPos;
            Vector2 finalDirection;

            if (Data.isVerticalSpread)
            {
                // ═══ VERTICAL MULTI-SHOT (Contra Style) ═══
                // Dàn đều các viên đạn theo trục Y quanh nòng súng.
                // VD: 3 viên, spacing = 0.5 → offsets: -0.5, 0.0, +0.5
                float halfCount = (bulletsToShoot - 1) * 0.5f;
                float offsetY = (i - halfCount) * Data.verticalSpacing;

                spawnPos = _firePoint.position + new Vector3(0f, offsetY, 0f);

                // Tất cả đạn bay ngang song song (không lệch góc)
                finalDirection = new Vector2(dirX, 0f);
            }
            else
            {
                // ═══ SPREAD NGANG (Shotgun / Pistol) ═══
                spawnPos = _firePoint.position;

                // Tính toán góc lệch ngẫu nhiên (spread)
                float randomSpread = UnityEngine.Random.Range(-Data.spreadAngle, Data.spreadAngle);
                Vector2 baseDirection = new Vector2(dirX, 0f);
                finalDirection = Quaternion.Euler(0, 0, randomSpread) * baseDirection;
            }

            // Lấy đạn từ Pool theo đúng loại của súng đang cầm
            Bullet bullet = Data.bulletPrefab.Spawn(spawnPos);
            bullet.Activate(Data);

            // Truyền gia tốc cho đạn
            bullet.RB.linearVelocity = finalDirection.normalized * Data.bulletSpeed;
        }

        // Tích nhiệt khi bắn đạn thường (mỗi phát bóp cò = 1 lần cộng)
        if (Data.canOverheat)
        {
            AddHeat(Data.heatPerShot);
        }

        // --- Hiệu ứng giật súng (Recoil) ---
        OnRecoil?.Invoke(dirX, Data.fireRate);
    }

    // ─── OVERHEAT LOGIC ──────────────────────────────────────────────────────

    private void AddHeat(float amount)
    {
        WeaponSlot slot = CurrentSlot;
        if (slot == null) return;

        _isFiringThisFrame = true;
        float heatBefore = slot.currentHeat; // Bắt lấy mức nhiệt thực tế sau khi đã tản bớt
        slot.currentHeat += amount;
        slot.currentHeat = Mathf.Min(slot.currentHeat, slot.maxHeat);
        OnHeatChanged?.Invoke(slot.currentHeat, slot.maxHeat);

        Debug.Log($"<color=yellow>[Heat]</color> {Data.name}: Nhiệt hiện tại {heatBefore:F1} + {amount:F2} → {slot.currentHeat:F1}/{slot.maxHeat}");

        if (!slot.isOverheated && slot.currentHeat >= slot.maxHeat)
        {
            slot.isOverheated = true;
            slot.overheatFreezeEndTime = Time.time + slot.overheatFreezeDuration;
            OnOverheatStateChanged?.Invoke(true);
            Debug.Log($"<color=red>[Overheat] QUÁ NHIỆT!</color> Thanh nhiệt đầy ({slot.currentHeat}/{slot.maxHeat}). Đóng băng {slot.overheatFreezeDuration}s. Khóa bắn cho đến khi nguội xuống {slot.unlockThreshold * 100}%.");

            SoundManager.Instance.PlaySound3D("Weapons","OverheatOn", transform.position);

            // ═══ ÉP ANIMATOR HỦY DÁNG BẮN TỨC THÌ ═══
            // ResetTrigger xóa mọi trigger "Fire" đang chờ trong hàng đợi Animator.
            // Play("Idle", 0, 0f) ép nhảy thẳng về frame đầu tiên của Idle, bỏ qua mọi Transition.
            // Update(0) ép Animator xử lý ngay lập tức trong frame này (không chờ sang frame sau).
            if (_weaponAnimator != null && _weaponAnimator.isActiveAndEnabled)
            {
                _weaponAnimator.ResetTrigger("Fire");
                _weaponAnimator.SetBool("Fire", false);
                _weaponAnimator.Play("Idle", 0, 0f);
                _weaponAnimator.Update(0f); // Ép xử lý tức thì, triệt tiêu hoàn toàn trigger còn sót
            }

            // Yêu cầu bên Animation sinh khói tại nòng súng
            if (_animation != null)
            {
                _animation.PlayOverheatVfx(_firePoint.position);
            }
        }
    }

    private void HandleOverheatCooldown()
    {
        UpdateSlotCooldown(Weapon1, _currentSlotIndex == 1 && _isFiringThisFrame);
        UpdateSlotCooldown(Weapon2, _currentSlotIndex == 2 && _isFiringThisFrame);
        UpdateSlotCooldown(Weapon3, _currentSlotIndex == 3 && _isFiringThisFrame);
    }

    private void UpdateSlotCooldown(WeaponSlot slot, bool isFiringThisFrame)
    {
        if (slot == null) return;

        // Khóa tản nhiệt nếu đang trong thời gian đóng băng 0.2s
        if (slot.isOverheated && Time.time < slot.overheatFreezeEndTime)
        {
            return;
        }

        // Chỉ nguội khi frame này KHÔNG bắn súng này
        if (!isFiringThisFrame && slot.currentHeat > 0)
        {
            slot.currentHeat -= slot.cooldownRate * Time.deltaTime;
            slot.currentHeat = Mathf.Max(slot.currentHeat, 0f);
            
            if (slot == CurrentSlot)
            {
                OnHeatChanged?.Invoke(slot.currentHeat, slot.maxHeat);
            }
        }

        // Mở khóa khi đã nguội đủ (hysteresis chống flicker bật/tắt)
        if (slot.isOverheated && slot.currentHeat <= slot.maxHeat * slot.unlockThreshold)
        {
            slot.isOverheated = false;
            if (slot == CurrentSlot)
            {
                OnOverheatStateChanged?.Invoke(false);
                Debug.Log($"<color=green>[Overheat] Đã nguội!</color> Thanh nhiệt: {slot.currentHeat:F1}/{slot.maxHeat}. Mở khóa bắn.");

                SoundManager.Instance.PlaySound3D("Weapons", "OverheatOff", transform.position);
            }
        }
    }
}


}
