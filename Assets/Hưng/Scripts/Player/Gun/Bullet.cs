using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn script này vào Bullet Prefab.
/// Prefab cần có: SpriteRenderer, Rigidbody2D (Gravity Scale = 0), Collider2D (Is Trigger = true).
/// Dùng Linecast thủ công trong FixedUpdate để chống xuyên tường (CCD không hoạt động với Trigger).
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

    [Header("Visuals")]
    [Tooltip("Kéo Prefab hiệu ứng va chạm/nổ vào đây")]
    [SerializeField] private GameObject _hitVfxPrefab;
    public GameObject HitVfxPrefab => _hitVfxPrefab;

    [Tooltip("Danh sách tag được phép gây sát thương")]
    [TagSelector] 
    [SerializeField] private string[] _enemyTags;

    // Cache Rigidbody2D — tránh GetComponent mỗi lần bắn (Zero GC)
    public Rigidbody2D RB { get; private set; }

    // Tham chiếu pool để tự trả về — set 1 lần duy nhất qua Init()
    private BulletPool _pool;
    public string PoolKey { get; private set; }

    private float _lifetime;
    private float _spawnTime;

    private int _pierceLeft;
    private VfxPool _vfxPool;
    private HashSet<Collider2D> _hitColliders = new HashSet<Collider2D>();
    private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[10];

    // Linecast anti-tunneling: lưu vị trí frame trước để quét đường đi
    private Vector2 _lastPosition;
    private bool _hasHit; // Guard chống xử lý va chạm 2 lần trong cùng 1 frame

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>(); // Cache 1 lần duy nhất
    }

    /// <summary>
    /// Gọi bởi BulletPool.CreateBullet() — chỉ chạy 1 lần khi tạo đạn.
    /// </summary>
    public void Init(BulletPool pool, string poolKey)
    {
        _pool = pool;
        PoolKey = poolKey;
    }

    /// <summary>
    /// Gọi bởi PlayerAttack.Fire() mỗi lần lấy đạn từ pool.
    /// Reset trạng thái đạn cho lần sử dụng mới.
    /// </summary>
    public void Activate(float lifetime, int bulletDamage, int pierceCount, VfxPool vfxPool)
    {
        _lifetime = lifetime;
        _spawnTime = Time.time;
        damage = bulletDamage;
        
        _pierceLeft = pierceCount;
        _vfxPool = vfxPool;
        _hitColliders.Clear();

        RB.linearVelocity = Vector2.zero; // Reset vận tốc từ lần bắn trước
        _lastPosition = RB.position;      // Chụp vị trí khởi điểm để bắt đầu quét từ đây
        _hasHit = false;

        // Xóa Trail rác (nếu có) khi lôi đạn từ pool ra
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.Clear();
    }

    private void Update()
    {
        // Tự trả về pool sau khi hết lifetime — thay thế Destroy(bullet, lifetime)
        if (Time.time >= _spawnTime + _lifetime)
        {
            ReturnToPool();
        }
    }

    private void FixedUpdate()
    {
        if (_hasHit) return;

        Vector2 currentPosition = RB.position;

        // Quét xuyên nhiều vật thể bằng LinecastNonAlloc (Zero GC)
        int hitCount = Physics2D.LinecastNonAlloc(_lastPosition, currentPosition, _hitBuffer, _enemyLayer | _groundLayer);
        
        if (hitCount > 1)
        {
            // Bubble sort đơn giản để đảm bảo xử lý va chạm theo đúng thứ tự gần -> xa (để đạn nổ trúng tường trước khi trúng quái đứng sau tường)
            for (int i = 0; i < hitCount - 1; i++)
            {
                for (int j = 0; j < hitCount - i - 1; j++)
                {
                    if (_hitBuffer[j].fraction > _hitBuffer[j + 1].fraction)
                    {
                        var temp = _hitBuffer[j];
                        _hitBuffer[j] = _hitBuffer[j + 1];
                        _hitBuffer[j + 1] = temp;
                    }
                }
            }
        }

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _hitBuffer[i];
            if (hit.collider != null)
            {
                bool stopBullet = ProcessCollision(hit.collider, hit.point);
                if (stopBullet)
                {
                    _hasHit = true;
                    ReturnToPool();
                    break;
                }
            }
        }

        _lastPosition = currentPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return; 
        bool stopBullet = ProcessCollision(other, transform.position);
        if (stopBullet)
        {
            _hasHit = true;
            ReturnToPool();
        }
    }

    private bool ProcessCollision(Collider2D other, Vector2 hitPoint)
    {
        // 1. Va chạm Ground
        if (((1 << other.gameObject.layer) & _groundLayer) != 0)
        {
            SpawnHitVfx(hitPoint);
            return true; // Báo hiệu đạn phải nổ ngay lập tức
        }

        // 2. Va chạm Enemy
        if (((1 << other.gameObject.layer) & _enemyLayer) != 0)
        {
            if (_hitColliders.Contains(other)) return false; // Đã xuyên qua con này rồi, bỏ qua

            if (HasEnemyTag(other))
            {
                NhanSatThuong nhanSatThuong = other.GetComponent<NhanSatThuong>();
                if (nhanSatThuong != null)
                {
                    nhanSatThuong.TakeDamage(damage);
                }
            }
            
            SpawnHitVfx(hitPoint);
            _hitColliders.Add(other); // Đánh dấu đã đâm qua

            _pierceLeft--;
            if (_pierceLeft < 0) 
            {
                return true; // Hết lượt xuyên -> Báo hiệu nổ đạn
            }
            return false; // Còn lượt xuyên -> Cho bay xuyên qua
        }

        return false;
    }

    private void SpawnHitVfx(Vector2 position)
    {
        if (_vfxPool != null && _hitVfxPrefab != null)
        {
            _vfxPool.SpawnVfx(_hitVfxPrefab, position);
        }
    }

    private void ReturnToPool()
    {
        _hasHit = true; // Đảm bảo không xử lý trùng nếu ReturnToPool() bị gọi nhiều lần
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
