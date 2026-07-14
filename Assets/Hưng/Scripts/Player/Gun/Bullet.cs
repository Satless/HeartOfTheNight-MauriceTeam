using UnityEngine;

/// <summary>
/// Gắn script này vào Bullet Prefab.
/// Prefab cần có: SpriteRenderer, Rigidbody2D (Gravity Scale = 0), Collider2D (Is Trigger = true).
/// </summary>
public class Bullet : MonoBehaviour
{
    // Damage được set từ PlayerAttack.Fire()
    [HideInInspector] public int damage;

    [Header("Collision Layers & Tags")]
    [Tooltip("Layer va chạm các loại quái")]
    [SerializeField] private LayerMask _enemyLayer;
    [Tooltip("Layer va chạm đất, tường, vật cản, v.v")]
    [SerializeField] private LayerMask _groundLayer;

    [Tooltip("Danh sách tag được phép gây sát thương")]
    [TagSelector] 
    [SerializeField] private string[] _enemyTags;

    // Cache Rigidbody2D — tránh GetComponent mỗi lần bắn (Zero GC)
    public Rigidbody2D RB { get; private set; }

    // Tham chiếu pool để tự trả về — set 1 lần duy nhất qua Init()
    private BulletPool _pool;
    private float _lifetime;
    private float _spawnTime;

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>(); // Cache 1 lần duy nhất
    }

    /// <summary>
    /// Gọi bởi BulletPool.CreateBullet() — chỉ chạy 1 lần khi tạo đạn.
    /// </summary>
    public void Init(BulletPool pool)
    {
        _pool = pool;
    }

    /// <summary>
    /// Gọi bởi PlayerAttack.Fire() mỗi lần lấy đạn từ pool.
    /// Reset trạng thái đạn cho lần sử dụng mới.
    /// </summary>
    public void Activate(float lifetime, int bulletDamage)
    {
        _lifetime = lifetime;
        _spawnTime = Time.time;
        damage = bulletDamage;
        RB.linearVelocity = Vector2.zero; // Reset vận tốc từ lần bắn trước
    }

    private void Update()
    {
        // Tự trả về pool sau khi hết lifetime — thay thế Destroy(bullet, lifetime)
        if (Time.time >= _spawnTime + _lifetime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Va chạm Enemy: Dùng Layer để lọc va chạm vật lý trước (Zero GC, cực nhanh)
        if (((1 << other.gameObject.layer) & _enemyLayer) != 0)
        {
            // Kiểm tra xem quái này có mang Tag hợp lệ không (hỗ trợ nhiều Tag)
            if (HasEnemyTag(other))
            {
                // Tự implement theo hệ thống health của bạn, ví dụ:
                // other.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            }
            
            // Cứ chạm trúng Layer Enemy là đạn bị hủy (trả về pool)
            ReturnToPool();
            return;
        }

        // 2. Va chạm Ground: Tilemap thường gán sẵn Layer Ground, lọc theo Layer là chuẩn xác nhất
        if (((1 << other.gameObject.layer) & _groundLayer) != 0)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        RB.linearVelocity = Vector2.zero;
        _pool.Return(this);
    }

    /// <summary>
    /// Kiểm tra va chạm có nằm trong danh sách Tag hợp lệ hay không (Zero GC do dùng CompareTag).
    /// </summary>
    private bool HasEnemyTag(Collider2D other)
    {
        for (int i = 0; i < _enemyTags.Length; i++)
        {
            if (other.CompareTag(_enemyTags[i])) return true;
        }
        return false;
    }
}
