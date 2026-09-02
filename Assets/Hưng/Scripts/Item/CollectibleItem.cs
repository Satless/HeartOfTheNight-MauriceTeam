using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HeartOfTheNight.Player;

/// <summary>
/// Gắn lên Prefab vật phẩm (Máu, Tiền, Buff...).
/// Yêu cầu: Có Collider2D (IsTrigger = true) và Layer đúng với thiết lập trong PlayerMagnet.
/// 
/// Luồng hoạt động:
/// 1. PlayerMagnet quét vùng → gọi PullTowards() → item bay về player.
/// 2. Đủ gần → Collect() → áp dụng hiệu ứng theo ItemData.itemType.
/// 3. Buff có thời hạn (Shield/Speed/Jump): ẩn item, gắn vào player, chạy coroutine.
/// 4. Hết hạn → revert buff, cleanup, SetActive(false) → sẵn sàng Object Pool.
/// </summary>
public class CollectibleItem : MonoBehaviour
{
    // ─── INSPECTOR ──────────────────────────────────────────────────────────────
    [Header("Item Data")]
    public ItemData data;

    [Header("Debug Tracking")]
    [Tooltip("Đã bị nhặt chưa (ngăn chặn việc nhặt đúp nhiều lần)")]
    [SerializeField, ReadOnly] private bool _isCollected;
    public bool IsCollected
    {
        get => _isCollected;
        private set => _isCollected = value;
    }

    [Tooltip("Vận tốc bay lơ lửng về phía người chơi hiện tại")]
    [SerializeField, ReadOnly] private float _currentSpeed;

    [Tooltip("Lần cuối cùng bị lực nam châm tác động (nếu mất lực sẽ rớt xuống đất)")]
    [SerializeField, ReadOnly] private float _lastPullTime;

    private bool _isBeingPulled;

    private Rigidbody2D _rb;
    private SpriteRenderer[] _renderers;
    private Collider2D[] _colliders;
    private float _spawnTime;
    private float _lastWobbleOffset;
    private float _initialPullDistance;

    // Wobble không bao giờ được dịch item quá con số này mỗi frame,
    // bất kể Designer chỉnh amplitude/frequency cao đến mấy.
    // Ngăn item teleport xuyên Ground/Wall.
    private const float MAX_WOBBLE_DELTA_PER_FRAME = 0.5f;

    // Ngăn chặn MoveTowards nhảy một khoảng quá xa trong 1 frame nếu maxSpeed bị set quá lớn
    private const float MAX_PULL_DELTA_PER_FRAME = 1.0f;

    [Tooltip("Thời gian chờ tối thiểu sau khi spawn trước khi item có thể bị nam châm hút (0 = hút ngay)")]
    public float pullDelayAfterSpawn = 0.4f;

    [Tooltip("Nếu bật, item sẽ phải chờ hoàn thành animation rơi chạm đất (của ItemFloating) rồi mới bị hút.")]
    public bool waitForGroundHit = true;

    private ItemFloating _floatingScript;

    // Extract magic numbers thành constants (Data-Driven)
    private const float PULL_RELEASE_TIMEOUT = 0.1f;
    private const float GROUND_WAIT_TIMEOUT = 3f;

    /// <summary>
    /// true nếu item được đặt sẵn trong Scene (không phải spawn runtime bởi EnemyLootDrop).
    /// Item đặt sẵn bỏ qua waitForGroundHit vì ItemFloating.Start() sẽ bắn nó lên
    /// và có thể không rơi trúng Ground layer → vĩnh viễn không hút được.
    /// </summary>
    private bool _isPrePlaced;

    // ─── BUFF STATE ─────────────────────────────────────────────────────────────
    /// <summary>Thời gian buff còn lại (dùng cho coroutine, hỗ trợ ExtendDuration).</summary>
    private float _remainingBuffTime;
    private bool _isRunningBuff;
    private GameObject _spawnedBuffVisual;

    /// <summary>
    /// Bộ đếm static: theo dõi số stack đang active cho mỗi loại buff.
    /// Reset tự động khi chuyển scene (level-based).
    /// </summary>
    private static readonly Dictionary<ItemData.ItemType, int> _stackCounts = new();

    /// <summary>
    /// Chủ sở hữu buff đang chạy (dùng cho ExtendDuration: tìm buff cùng loại đang active để cộng thêm thời gian).
    /// </summary>
    private static readonly Dictionary<ItemData.ItemType, CollectibleItem> _extendableBuffOwners = new();

    // ─── LIFECYCLE ──────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Domain Reload safety: clear static state khi Play Mode bắt đầu
        _stackCounts.Clear();
        _extendableBuffOwners.Clear();
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _colliders = GetComponents<Collider2D>();
        _spawnTime = Time.time;

        // Detect item đặt sẵn trong Scene vs spawn runtime:
        // Nếu item đã có trong Scene từ đầu, Awake() chạy trước bất kỳ Instantiate() nào.
        // Item spawn bởi EnemyLootDrop sẽ có _spawnTime > 0 (game đã chạy một lúc).
        // Item đặt sẵn có Awake() chạy ở frame đầu tiên khi scene load.
        _isPrePlaced = (gameObject.scene.isLoaded && Time.frameCount <= 1);
        _floatingScript = GetComponent<ItemFloating>();
    }

    private void Update()
    {
        // Đang chạy buff → không cần check pull nữa
        if (_isRunningBuff) return;

        // Chỉ xử lý rớt lại nếu vật phẩm ĐANG THỰC SỰ BỊ HÚT
        // (Tránh nhầm với trạng thái Kinematic tự nhiên của ItemFloating khi chạm đất)
        if (_isBeingPulled && !IsCollected && _rb != null)
        {
            // PlayerMagnet ngừng gọi PullTowards quá 0.1s -> Rơi xuống lại
            if (Time.time - _lastPullTime > PULL_RELEASE_TIMEOUT)
            {
                _isBeingPulled = false;
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _currentSpeed = 0f;
                
                // Bật lại ItemFloating và reset trạng thái để item rớt xuống đất nảy lại
                if (_floatingScript != null)
                {
                    _floatingScript.enabled = true;
                    _floatingScript.ResetLandedState();
                }

                // KHÔNG set isTrigger = false ở đây!
                // ItemFloating.OnCollisionEnter2D() sẽ tự set isTrigger = true khi chạm Ground.
                // Nếu ta ép false ở đây mà item đặt sẵn không qua OnCollisionEnter2D lần nữa
                // → collider vĩnh viễn false → player va đập thay vì đi xuyên qua để nhặt.
            }
        }
    }

    private void Start()
    {
        if (!IsWeaponUnlockItem())
            return;

        if (TryHideIfWeaponAlreadyUnlocked())
            return;

        Invoke(nameof(RetryHideIfWeaponAlreadyUnlocked), 0.5f);
        Invoke(nameof(RetryHideIfWeaponAlreadyUnlocked), 1.5f);
    }

    private void OnEnable()
    {
        // Reset trạng thái khi được kích hoạt lại (phục vụ cho Object Pool)
        IsCollected = false;
        _isBeingPulled = false;
        _currentSpeed = 0f;
        _isRunningBuff = false;
        _remainingBuffTime = 0f;
        _spawnTime = Time.time;
        _lastWobbleOffset = 0f;
        _initialPullDistance = 0f;

        // Trả lại vật lý + hiển thị
        if (_rb != null)
            _rb.bodyType = RigidbodyType2D.Dynamic;

        RestoreItemVisuals();
        TryHideIfWeaponAlreadyUnlocked();
    }

    private void OnDisable()
    {
        // Cleanup nếu bị disable giữa chừng (VD: chuyển scene khi buff đang chạy)
        if (_isRunningBuff)
        {
            CleanupBuffState();
        }
    }

    // ─── MAGNET PULL (giữ nguyên logic cũ) ──────────────────────────────────────

    /// <summary>
    /// Được gọi mỗi frame từ PlayerMagnet khi vật phẩm nằm trong từ trường.
    /// Dùng MoveTowards để item bay mượt theo người chơi đang di chuyển.
    /// </summary>
    public void PullTowards(Transform target, float baseSpeed, float maxSpeed, float acceleration, float wobbleAmplitude = 0f, float wobbleFrequency = 0f)
    {
        if (IsCollected || data == null) return;
        
        // 1. Chờ đủ thời gian delay tối thiểu
        if (Time.time - _spawnTime < pullDelayAfterSpawn) return;

        // 2. Chờ chạm đất (nếu có ItemFloating)
        //    Item đặt sẵn (_isPrePlaced) bỏ qua bước này vì ItemFloating.Start()
        //    sẽ bắn item lên với lực ngẫu nhiên, có thể không rơi trúng Ground layer
        //    → hasLandOnGround vĩnh viễn false → nam châm không bao giờ hút được.
        if (waitForGroundHit && !_isPrePlaced && _floatingScript != null && _floatingScript.enabled)
        {
            bool timedOut = (Time.time - _spawnTime) > GROUND_WAIT_TIMEOUT;
            if (!_floatingScript.HasLanded && !timedOut) return;
        }

        // Ghi nhận khoảng cách ban đầu khi bắt đầu hút (dùng cho wobble fade-out)
        if (!_isBeingPulled)
        {
            _initialPullDistance = Vector2.Distance(transform.position, target.position);
            if (_initialPullDistance < 0.1f) _initialPullDistance = 1f; // safety
            _lastWobbleOffset = 0f; // Đảm bảo reset pha dao động khi hút lại sau khi rớt
        }

        _isBeingPulled = true;
        _lastPullTime = Time.time;

        // Tắt script ItemFloating (nếu có) để nó không khóa trục X của item khi đang lơ lửng
        if (_floatingScript != null && _floatingScript.enabled)
        {
            _floatingScript.enabled = false;
        }

        // Tắt vật lý (trọng lực) để item bay lơ lửng mượt mà về phía người chơi
        if (_rb != null && _rb.bodyType != RigidbodyType2D.Kinematic)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
        }

        // Tăng tốc độ bay theo thời gian nhưng không vượt quá maxSpeed
        _currentSpeed += acceleration * Time.deltaTime;
        float actualSpeed = Mathf.Min(baseSpeed + _currentSpeed, maxSpeed);

        float distanceRemaining = Vector2.Distance(transform.position, target.position);
        
        // Rào tốc độ tối đa mỗi frame để không bị tunnel qua Ground nếu Designer set maxSpeed cực lớn
        float moveStep = actualSpeed * Time.deltaTime;
        moveStep = Mathf.Min(moveStep, MAX_PULL_DELTA_PER_FRAME);
        
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveStep);

        // Hiệu ứng quỹ đạo hình Sin (Wobble) sử dụng Delta để không bị dồn pos
        //
        // Fade-out dùng _initialPullDistance (khoảng cách lúc bắt đầu hút) thay vì
        // collectDistance (khoảng cách nhặt, rất nhỏ ~0.5). Trước đây dùng collectDistance
        // → ratio luôn = 1.0 → wobble không bao giờ giảm → amplitude quá lớn = teleport
        // xuyên Ground.
        if (wobbleAmplitude > 0f)
        {
            float fadeFactor = Mathf.Clamp01(distanceRemaining / _initialPullDistance);
            float currentWobble = Mathf.Sin(Time.time * wobbleFrequency) * fadeFactor * wobbleAmplitude;
            float wobbleDelta = currentWobble - _lastWobbleOffset;
            _lastWobbleOffset = currentWobble;

            // Clamp: không bao giờ dịch quá MAX_WOBBLE_DELTA_PER_FRAME mỗi frame
            // → dù Designer chỉnh amplitude/frequency cao mấy cũng không teleport xuyên collider
            wobbleDelta = Mathf.Clamp(wobbleDelta, -MAX_WOBBLE_DELTA_PER_FRAME, MAX_WOBBLE_DELTA_PER_FRAME);

            Vector3 toTarget = target.position - transform.position;
            Vector3 perpendicular = Vector3.Cross(toTarget, Vector3.forward).normalized;
            // Fallback: khi item gần trùng vị trí player, Cross trả về zero → dùng Vector3.up
            if (perpendicular.sqrMagnitude < 0.001f)
                perpendicular = Vector3.up;
            transform.position += perpendicular * wobbleDelta;
            
            // Cập nhật lại khoảng cách sau khi wobble để trigger thu thập chính xác
            distanceRemaining = Vector2.Distance(transform.position, target.position);
        }

        float collectRange = data.collectDistance > 0f ? data.collectDistance : 0.5f;
        if (distanceRemaining <= collectRange)
        {
            Collect(target.gameObject);
        }
    }

    // ─── COLLECT & APPLY ────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsCollected && collision.CompareTag("Player")) Collect(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsCollected && collision.gameObject.CompareTag("Player")) Collect(collision.gameObject);
    }

    private void Collect(GameObject player)
    {
        IsCollected = true;
        if (data == null) return;

        // SFX
        AudioEvents.TriggerSound3D(data.sfxCategory, data.sfxSubCategory, data.sfxAction, transform.position);

        // VFX nhặt đồ (optional — có thể chưa có prefab)
        // TODO: Thay Instantiate bằng Object Pool khi game có nhiều item cùng lúc.
        if (data.pickupVFX != null)
            Instantiate(data.pickupVFX, transform.position, Quaternion.identity);

        // Tìm components trên player (one-time, không ở Update)
        var health = player.GetComponentInParent<PlayerHealth>();
        var movement = player.GetComponentInParent<PlayerMovement>();
        var attack = player.GetComponentInParent<PlayerAttack>();

        switch (data.itemType)
        {
            case ItemData.ItemType.HealHP:
                ApplyHeal(health);
                break;

            case ItemData.ItemType.Shield:
                ApplyShield(player, health);
                break;

            case ItemData.ItemType.SpeedBuff:
                ApplyMultiplierBuff(player, movement,
                    () => movement.moveSpeedMultiplier,
                    val => movement.moveSpeedMultiplier = val);
                break;

            case ItemData.ItemType.JumpBuff:
                ApplyMultiplierBuff(player, movement,
                    () => movement.jumpForceMultiplier,
                    val => movement.jumpForceMultiplier = val);
                break;

            case ItemData.ItemType.WeaponUnlock:
                ApplyWeaponUnlock(attack);
                break;
        }
    }

    // ─── WEAPON UNLOCK ──────────────────────────────────────────────────────────

    private bool IsWeaponUnlockItem()
    {
        return data != null && data.itemType == ItemData.ItemType.WeaponUnlock;
    }

    private void RetryHideIfWeaponAlreadyUnlocked()
    {
        TryHideIfWeaponAlreadyUnlocked();
    }

    /// <summary>
    /// Súng unlock 1 lần / slot. Đã mở trong save thì không hiện lại khi chơi lại màn.
    /// </summary>
    private bool TryHideIfWeaponAlreadyUnlocked()
    {
        if (!IsWeaponUnlockItem() || IsCollected)
            return false;

        var save = HeartOfTheNight.Hung.DataManager.Instance != null
            ? HeartOfTheNight.Hung.DataManager.Instance.Data
            : null;
        if (save == null || !save.hasSave)
            return false;
        if (!save.IsWeaponUnlocked(data.weaponSlotIndex))
            return false;

        IsCollected = true;
        gameObject.SetActive(false);
        return true;
    }

    private void ApplyWeaponUnlock(PlayerAttack attack)
    {
        if (attack != null)
        {
            int slot = data.weaponSlotIndex;
            if (!attack.IsWeaponUnlocked(slot))
            {
                // Nhặt lần đầu -> Mở khóa và tự động đổi súng
                attack.UnlockWeapon(slot);
            }
            else
            {
                // Đã mở khóa -> Giảm nhiệt độ
                attack.ReduceHeatPercentage(slot, data.heatReducePercentage);
            }
        }
        
        // Tác dụng tức thì → deactivate ngay
        gameObject.SetActive(false);
    }

    // ─── HEAL ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Nhặt luôn (item biến mất), máu đầy thì Heal(0) = không hồi gì.
    /// Anti-Heal đã được check bên trong PlayerHealth.Heal() rồi.
    /// </summary>
    private void ApplyHeal(PlayerHealth health)
    {
        if (health != null)
            health.Heal(data.value);

        // Instant → deactivate ngay
        gameObject.SetActive(false);
    }

    // ─── SHIELD ─────────────────────────────────────────────────────────────────

    private void ApplyShield(GameObject player, PlayerHealth health)
    {
        if (health == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // Check stacking
        if (!CanApplyStack())
        {
            gameObject.SetActive(false);
            return;
        }

        // ExtendDuration: nếu đã có shield đang chạy → cộng thêm thời gian
        if (data.durationBehavior == ItemData.DurationBehavior.ExtendDuration
            && TryExtendExistingBuff())
        {
            gameObject.SetActive(false);
            return;
        }

        // Bắt đầu buff mới
        HideItemVisuals();
        transform.SetParent(player.transform);
        transform.localPosition = Vector3.zero;
        StartCoroutine(ShieldRoutine(health));
    }

    private IEnumerator ShieldRoutine(PlayerHealth health)
    {
        _isRunningBuff = true;
        _remainingBuffTime = data.buffDuration;
        AddStack(data.itemType);
        RegisterAsExtendableOwner();

        // Bật shield
        health.hasShield = true;

        // Spawn visual khiên gắn lên player
        if (data.buffVisualPrefab != null)
        {
            _spawnedBuffVisual = Instantiate(data.buffVisualPrefab,
                health.transform.position, Quaternion.identity, health.transform);
        }

        // Đếm ngược — dùng while loop để hỗ trợ ExtendDuration (cộng thêm _remainingBuffTime từ bên ngoài)
        while (_remainingBuffTime > 0f)
        {
            // Shield bị phá bởi damage → PlayerHealth set hasShield = false → thoát sớm
            if (!health.hasShield) break;

            _remainingBuffTime -= Time.deltaTime;
            yield return null;
        }

        // Hết hạn hoặc bị phá
        health.hasShield = false;
        if (_spawnedBuffVisual != null)
            Destroy(_spawnedBuffVisual);

        RemoveStack(data.itemType);
        UnregisterExtendableOwner();
        CleanupAfterBuff();
    }

    // ─── SPEED / JUMP BUFF ──────────────────────────────────────────────────────

    private void ApplyMultiplierBuff(GameObject player, PlayerMovement movement,
        System.Func<float> getter, System.Action<float> setter)
    {
        if (movement == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // Check stacking
        if (!CanApplyStack())
        {
            gameObject.SetActive(false);
            return;
        }

        // ExtendDuration: cộng thêm thời gian vào buff đang chạy
        if (data.durationBehavior == ItemData.DurationBehavior.ExtendDuration
            && TryExtendExistingBuff())
        {
            gameObject.SetActive(false);
            return;
        }

        // Bắt đầu buff mới
        HideItemVisuals();
        transform.SetParent(player.transform);
        transform.localPosition = Vector3.zero;
        StartCoroutine(MultiplierBuffRoutine(getter, setter));
    }

    private IEnumerator MultiplierBuffRoutine(System.Func<float> getter, System.Action<float> setter)
    {
        _isRunningBuff = true;
        _remainingBuffTime = data.buffDuration;
        AddStack(data.itemType);
        RegisterAsExtendableOwner();

        // Tính bonus: 1.5x → bonus = 0.5, cộng vào multiplier hiện tại, cap tại maxMultiplier
        float bonus = data.multiplier - 1f;
        float currentTotal = getter();
        float newTotal = Mathf.Min(currentTotal + bonus, data.maxMultiplier);
        float actualBonus = newTotal - currentTotal;
        setter(newTotal);

        // Spawn buff visual (aura tốc độ, hiệu ứng nhảy...)
        if (data.buffVisualPrefab != null && transform.parent != null)
        {
            _spawnedBuffVisual = Instantiate(data.buffVisualPrefab,
                transform.parent.position, Quaternion.identity, transform.parent);
        }

        // Đếm ngược
        while (_remainingBuffTime > 0f)
        {
            _remainingBuffTime -= Time.deltaTime;
            yield return null;
        }

        // Hết hạn → revert multiplier
        float current = getter();
        setter(Mathf.Max(1f, current - actualBonus)); // Không bao giờ dưới 1

        if (_spawnedBuffVisual != null)
            Destroy(_spawnedBuffVisual);

        RemoveStack(data.itemType);
        UnregisterExtendableOwner();
        CleanupAfterBuff();
    }

    // ─── STACK MANAGEMENT ───────────────────────────────────────────────────────

    private bool CanApplyStack()
    {
        int current = GetStackCount(data.itemType);
        return current < data.maxStacks;
    }

    private static int GetStackCount(ItemData.ItemType type)
    {
        return _stackCounts.TryGetValue(type, out int count) ? count : 0;
    }

    private static void AddStack(ItemData.ItemType type)
    {
        _stackCounts[type] = GetStackCount(type) + 1;
    }

    private static void RemoveStack(ItemData.ItemType type)
    {
        int count = GetStackCount(type);
        if (count <= 1)
            _stackCounts.Remove(type);
        else
            _stackCounts[type] = count - 1;
    }

    // ─── EXTEND DURATION ────────────────────────────────────────────────────────

    private void RegisterAsExtendableOwner()
    {
        if (data.durationBehavior == ItemData.DurationBehavior.ExtendDuration)
            _extendableBuffOwners[data.itemType] = this;
    }

    private void UnregisterExtendableOwner()
    {
        if (_extendableBuffOwners.TryGetValue(data.itemType, out var owner) && owner == this)
            _extendableBuffOwners.Remove(data.itemType);
    }

    /// <summary>
    /// Tìm buff cùng loại đang chạy → cộng thêm thời gian vào nó.
    /// Return true nếu extend thành công (item mới không cần tự chạy buff).
    /// </summary>
    private bool TryExtendExistingBuff()
    {
        if (_extendableBuffOwners.TryGetValue(data.itemType, out var owner)
            && owner != null && owner._isRunningBuff)
        {
            owner._remainingBuffTime += data.buffDuration;
            return true;
        }
        return false;
    }

    // ─── VISUAL HELPERS ─────────────────────────────────────────────────────────

    /// <summary>
    /// Ẩn item khỏi thế giới (tắt sprite, collider, physics) khi bắt đầu buff có thời hạn.
    /// Item vẫn active để coroutine chạy.
    /// </summary>
    private void HideItemVisuals()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].enabled = false;

        for (int i = 0; i < _colliders.Length; i++)
            _colliders[i].enabled = false;

        // Dừng hiệu ứng lơ lửng nếu có (tương thích với ItemFloating của Tùng)
        if (TryGetComponent(out ItemFloating floating))
            floating.StopFloating();
    }

    /// <summary>
    /// Khôi phục hiển thị (dùng khi Object Pool re-enable item).
    /// </summary>
    private void RestoreItemVisuals()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].enabled = true;
        }

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
                _colliders[i].enabled = true;
        }
    }

    // ─── CLEANUP ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi sau khi buff hết hạn: tách khỏi player, deactivate, sẵn sàng pool.
    /// </summary>
    private void CleanupAfterBuff()
    {
        _isRunningBuff = false;
        transform.SetParent(null);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Cleanup khẩn cấp khi bị disable giữa chừng (chuyển scene, player chết...).
    /// Không revert multiplier ở đây vì player có thể đã bị destroy.
    /// </summary>
    private void CleanupBuffState()
    {
        if (_spawnedBuffVisual != null)
            Destroy(_spawnedBuffVisual);

        RemoveStack(data.itemType);
        UnregisterExtendableOwner();
        _isRunningBuff = false;
    }

    // ─── GIZMOS ─────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (data != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, data.collectDistance);
        }
    }
}
