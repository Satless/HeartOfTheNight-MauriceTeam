using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Script quản lý máu của Enemy, tuân thủ FSM/Data-Driven giống PlayerMovement.
/// Không hardcode máu vào đây, mà đọc từ MauTestEnemyData (ScriptableObject).
/// Dùng Event-Driven (UnityEvent) để dễ dàng gắn VFX/SFX trên Inspector mà không dính code.
/// </summary>
public class MauTestEnemy : MonoBehaviour, NhanSatThuong
{
    [Header("Data Configuration")]
    [Tooltip("Data chứa cấu hình tĩnh (Máu, Thời gian nháy đỏ...)")]
    [SerializeField] private MauTestEnemyData _data;

    [Header("Events (Observer Pattern)")]
    [Tooltip("Phát sự kiện khi máu thay đổi để cập nhật UI thanh máu")]
    public UnityEvent<int, int> OnHealthChanged; // (currentHealth, maxHealth)
    [Tooltip("Phát sự kiện khi bị bắn (Phát âm thanh đau...)")]
    public UnityEvent OnTakeDamage;
    [Tooltip("Phát sự kiện khi chết (Nổ Particle, Rơi đồ...)")]
    public UnityEvent OnDeath;

    // Cache components (Zero GC policy)
    private SpriteRenderer _spriteRenderer;
    private WaitForSeconds _flashWait; // Cache yield instruction
    private Coroutine _flashCoroutine;

    // State (FSM variables)
    private int _currentHealth;
    private bool _isDead;

    private void Awake()
    {
        // Cache reference (Zero GC, không dùng GetComponent trong lúc đang chơi)
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Cache WaitForSeconds từ đầu để không sinh rác bộ nhớ mỗi lần bị bắn
        if (_data != null)
        {
            _flashWait = new WaitForSeconds(_data.damageFlashDuration);
        }
        else
        {
            _flashWait = new WaitForSeconds(0.1f); // Fallback an toàn
        }
    }

    private void Start()
    {
        // Khởi tạo trạng thái từ Data
        if (_data != null)
        {
            _currentHealth = _data.maxHealth;
        }
        else
        {
            Debug.LogWarning($"[MauTestEnemy] Chưa gắn Data cho {gameObject.name}. Tự động dùng máu 100.");
            _currentHealth = 100;
        }

        // Cập nhật UI lúc mới spawn
        OnHealthChanged?.Invoke(_currentHealth, _data != null ? _data.maxHealth : 100);
    }

    /// <summary>
    /// Triển khai interface NhanSatThuong.
    /// Hàm này được gọi độc lập từ bên ngoài (ví dụ Bullet.cs).
    /// </summary>
    public void TakeDamage(int damage)
    {
        // Tránh bị đánh thêm khi đã chết (nếu chưa kịp biến mất)
        if (_isDead) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0); // Không để máu âm

        Debug.Log($"[NhanSatThuong - MauTestEnemy] Đã nhận <color=red>{damage}</color> sát thương. Máu còn: <color=green>{_currentHealth}</color>");

        // Báo cho toàn hệ thống biết đã mất máu (UI, Audio Manager...)
        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke(_currentHealth, _data != null ? _data.maxHealth : 100);

        // Chạy hiệu ứng nháy đỏ nội bộ
        if (_spriteRenderer != null)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRedRoutine());
        }

        // Xử lý chết
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"[NhanSatThuong - MauTestEnemy] Quái {gameObject.name} <color=red>ĐÃ CHẾT</color>!");
        _isDead = true;
        OnDeath?.Invoke();

        // Tạm thời Destroy, nếu sau này dùng Object Pool thì đổi thành ReturnToPool
        Destroy(gameObject);
    }

    /// <summary>
    /// Hiệu ứng nháy đỏ - Tách riêng ra Coroutine để logic nhận sát thương (TakeDamage) không bị kẹt.
    /// </summary>
    private IEnumerator FlashRedRoutine()
    {
        _spriteRenderer.color = _data != null ? _data.damageColor : Color.red;
        yield return _flashWait;
        _spriteRenderer.color = Color.white;
    }
}
