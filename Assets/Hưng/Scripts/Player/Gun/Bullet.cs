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
    [Tooltip("Layer enemy")]
    [SerializeField] private LayerMask _enemyLayer;
    [Tooltip("Layer ground, vật cản...")]
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

    [Header("Debug Tracking")]
    [Tooltip("Dữ liệu súng bắn ra viên đạn này")]
    [SerializeField, ReadOnly] private GunWeaponData _data;
    [Tooltip("Thời điểm đạn được sinh ra (để tính thời gian hủy tự động)")]
    [SerializeField, ReadOnly] private float _spawnTime;
    [Tooltip("Số lượt xuyên thấu mục tiêu còn lại")]
    [SerializeField, ReadOnly] private int _pierceLeft;
    [Tooltip("Đã va chạm trúng đích (chống xử lý va chạm 2 lần trong cùng 1 frame)")]
    [SerializeField, ReadOnly] private bool _hasHit; 


    private HashSet<Collider2D> _hitColliders = new HashSet<Collider2D>();
    private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[10];
    private static readonly Collider2D[] _aoeBuffer = new Collider2D[20];

    // Linecast anti-tunneling: lưu vị trí frame trước để quét đường đi
    private Vector2 _lastPosition;

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>(); // Cache 1 lần duy nhất
    }



    /// <summary>
    /// Gọi bởi PlayerAttack.ExecuteShot() mỗi lần lấy đạn từ pool.
    /// Reset trạng thái đạn cho lần sử dụng mới.
    /// </summary>
    public void Activate(GunWeaponData data)
    {
        _data = data;
        
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

        // Quét xuyên nhiều vật thể bằng Linecast (API mới, tự Zero-GC)
        var hitResults = Physics2D.LinecastAll(_lastPosition, currentPosition, _enemyLayer | _groundLayer);
        int hitCount = hitResults.Length;
        
        // Copy kết quả vào buffer tĩnh để sort mà không sinh GC
        int copyCount = Mathf.Min(hitCount, _hitBuffer.Length);
        for (int i = 0; i < copyCount; i++) _hitBuffer[i] = hitResults[i];
        hitCount = copyCount;
        
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

                // Đẩy lùi theo hướng bay của đạn
                if (_data.knockbackForce > 0)
                {
                    INhanKnockback knockback = other.GetComponent<INhanKnockback>();
                    if (knockback != null)
                    {
                        Vector2 knockDir = RB.linearVelocity.normalized;
                        knockback.ApplyKnockback(knockDir, _data.knockbackForce);
                    }
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

        // 2. Quét các mục tiêu trong bán kính nổ (API mới, tự Zero-GC)
        var aoeResults = Physics2D.OverlapCircleAll(explosionCenter, _data.explosionRadius, _enemyLayer);
        int count = aoeResults.Length;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = aoeResults[i];
            
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

                // Đẩy lùi từ tâm nổ ra ngoài
                if (_data.explosionKnockbackForce > 0)
                {
                    INhanKnockback knockback = col.GetComponent<INhanKnockback>();
                    if (knockback != null)
                    {
                        Vector2 knockDir = ((Vector2)targetPos - explosionCenter).normalized;
                        knockback.ApplyKnockback(knockDir, _data.explosionKnockbackForce);
                    }
                }
            }
        }
    }

    private void SpawnHitVfx(Vector2 position)
    {
        if (_hitVfxPrefab == null) return;

        // Lấy hướng bay hiện tại của đạn (1 là phải, -1 là trái)
        float dirX = RB.linearVelocity.x != 0 ? Mathf.Sign(RB.linearVelocity.x) : 1f;
        GameObject vfx = _hitVfxPrefab.Spawn(position);

        // Lật hình ảnh theo hướng bay của đạn
        Vector3 scale = vfx.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dirX);
        vfx.transform.localScale = scale;
    }

    private void ReturnToPool()
    {
        _hasHit = true; // Đảm bảo không xử lý trùng nếu ReturnToPool() bị gọi nhiều lần
        RB.linearVelocity = Vector2.zero;
        gameObject.Despawn();
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
