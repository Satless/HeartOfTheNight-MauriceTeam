using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn script này vào Bullet Prefab.
/// Prefab cần có: SpriteRenderer, Rigidbody2D (Gravity Scale = 0), Collider2D (Is Trigger = true).
/// Dùng Linecast thủ công trong FixedUpdate để chống xuyên tường (CCD không hoạt động với Trigger).
/// </summary>
public class Bullet : MonoBehaviour
{
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

    private GunWeaponData _data;
    private float _spawnTime;

    private int _pierceLeft;
    private VfxPool _vfxPool;
    private HashSet<Collider2D> _hitColliders = new HashSet<Collider2D>();
    private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[10];
    private static readonly Collider2D[] _aoeBuffer = new Collider2D[20];

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
    public void Activate(GunWeaponData data, VfxPool vfxPool)
    {
        _data = data;
        _vfxPool = vfxPool;
        
        _spawnTime = Time.time;
        _pierceLeft = data.pierceCount;
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
        if (_data != null && Time.time >= _spawnTime + _data.bulletLifetime)
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
        
        // Vẽ đường đạn để Debug
        Debug.DrawLine(_lastPosition, currentPosition, hitCount > 0 ? Color.green : Color.red, 0.5f);
        
        if (hitCount > 1)
        {
            // Bubble sort đơn giản để đảm bảo xử lý va chạm theo đúng thứ tự gần -> xa
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
                bool stopBullet = ProcessCollision(hit.collider, hit.point, hit.normal);
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
        // Khi trigger, normal lấy là ngược chiều vận tốc để dội lại
        Vector2 normal = RB.linearVelocity != Vector2.zero ? -RB.linearVelocity.normalized : Vector2.up;
        
        // Lấy điểm chạm chính xác trên bề mặt vật thể, thay vì lấy tâm viên đạn
        Vector2 hitPoint = other.ClosestPoint(transform.position);

        bool stopBullet = ProcessCollision(other, hitPoint, normal);
        if (stopBullet)
        {
            _hasHit = true;
            ReturnToPool();
        }
    }

    private bool ProcessCollision(Collider2D other, Vector2 hitPoint, Vector2 hitNormal)
    {
        // 1. Va chạm Ground
        if (((1 << other.gameObject.layer) & _groundLayer) != 0)
        {
            if (_data.isExplosive)
            {
                Explode(hitPoint, hitNormal, null);
            }
            else
            {
                SpawnHitVfx(hitPoint);
            }
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
                    nhanSatThuong.TakeDamage(_data.damage);
                }
            }
            
            _hitColliders.Add(other); // Đánh dấu đã đâm qua

            if (_data.isExplosive)
            {
                // Đạn nổ AOE: không xuyên, nổ ngay
                Explode(hitPoint, hitNormal, other);
                return true; 
            }
            else
            {
                SpawnHitVfx(hitPoint);
                
                _pierceLeft--;
                if (_pierceLeft < 0) 
                {
                    return true; // Hết lượt xuyên -> Báo hiệu nổ đạn
                }
                return false; // Còn lượt xuyên -> Cho bay xuyên qua
            }
        }

        return false;
    }

    private void Explode(Vector2 hitPoint, Vector2 hitNormal, Collider2D primaryTarget)
    {
        // 1. Dời tâm nổ ra một chút ngược hướng va chạm (chỉ dời 0.05f để không bị lệch quá nhiều)
        Vector2 explosionCenter = hitPoint + hitNormal * 0.05f;

        // Sinh hiệu ứng nổ (VFX)
        SpawnHitVfx(explosionCenter);

        if (_data.explosionRadius <= 0) return;

        // 2. Quét các mục tiêu trong bán kính nổ
        int count = Physics2D.OverlapCircleNonAlloc(explosionCenter, _data.explosionRadius, _aoeBuffer, _enemyLayer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _aoeBuffer[i];
            
            // Bỏ qua nếu là mục tiêu chính đã ăn sát thương gốc
            if (col == primaryTarget) continue;

            if (HasEnemyTag(col))
            {
                // 3. Line of Sight Check (chống nổ xuyên tường/Ground)
                // Bắn tia từ tâm nổ đến mục tiêu phụ
                Vector2 targetPos = col.bounds.center;
                RaycastHit2D blockCheck = Physics2D.Linecast(explosionCenter, targetPos, _groundLayer);
                
                // Bị tường/đất chặn ngang -> Không dính AOE
                if (blockCheck.collider != null)
                {
                    continue; 
                }

                // Gây sát thương nổ lan
                NhanSatThuong nhanSatThuong = col.GetComponent<NhanSatThuong>();
                if (nhanSatThuong != null)
                {
                    nhanSatThuong.TakeDamage(_data.explosionDamage);
                }
            }
        }
    }

    private void SpawnHitVfx(Vector2 position)
    {
        if (_vfxPool != null && _hitVfxPrefab != null)
        {
            // Lấy hướng bay hiện tại của đạn (1 là phải, -1 là trái)
            float dirX = RB.linearVelocity.x != 0 ? Mathf.Sign(RB.linearVelocity.x) : 1f;
            _vfxPool.SpawnVfx(_hitVfxPrefab, position, dirX);
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
