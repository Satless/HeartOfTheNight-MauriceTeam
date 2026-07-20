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

    [Tooltip("Danh sách tag được phép gây sát thương")]
    [TagSelector] 
    [SerializeField] private string[] _enemyTags;

    // Cache Rigidbody2D — tránh GetComponent mỗi lần bắn (Zero GC)
    public Rigidbody2D RB { get; private set; }

    // Tham chiếu pool để tự trả về — set 1 lần duy nhất qua Init()
    private BulletPool _pool;
    private float _lifetime;
    private float _spawnTime;

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
        _lastPosition = RB.position;      // Chụp vị trí khởi điểm để bắt đầu quét từ đây
        _hasHit = false;
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

        // Quét đường thẳng giữa vị trí frame trước và frame này — bắt được va chạm
        // dù đạn bay nhanh cỡ nào, không phụ thuộc CCD của Unity/Box2D.
        // Physics2D.Linecast (bản trả về 1 kết quả) là Zero-GC.
        RaycastHit2D hit = Physics2D.Linecast(_lastPosition, currentPosition, _enemyLayer | _groundLayer);
        
        // Vẽ tia Linecast ra Scene View để test trực quan (Màu xanh nếu trúng, màu đỏ nếu trượt, hiển thị 0.5s)
        Debug.DrawLine(_lastPosition, currentPosition, hit.collider != null ? Color.green : Color.red, 0.5f);

        if (hit.collider != null)
        {
            _hasHit = true; // Chặn OnTriggerEnter2D xử lý trùng lặp cùng va chạm này
            HandleCollision(hit.collider);
        }

        _lastPosition = currentPosition;
    }

    /// <summary>
    /// OnTriggerEnter2D giữ lại làm lớp dự phòng (bắt overlap tĩnh, ví dụ quái đi vào đúng lúc đạn đứng yên).
    /// Cờ _hasHit đảm bảo không xử lý trùng 2 lần cùng 1 va chạm trong 1 frame.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return; // Đã xử lý bởi Linecast rồi thì bỏ qua
        HandleCollision(other);
    }

    /// <summary>
    /// Logic va chạm gộp chung — dùng cho cả Linecast và OnTriggerEnter2D.
    /// Sửa logic damage/enemy chỉ cần sửa 1 chỗ duy nhất.
    /// </summary>
    private void HandleCollision(Collider2D other)
    {
        // 1. Va chạm Enemy: Dùng Layer để lọc va chạm vật lý trước (Zero GC, cực nhanh)
        if (((1 << other.gameObject.layer) & _enemyLayer) != 0)
        {
            // Kiểm tra xem quái này có mang Tag hợp lệ không (hỗ trợ nhiều Tag)
            if (HasEnemyTag(other))
            {
                // Gọi interface NhanSatThuong thay vì GetComponent cứng
                NhanSatThuong nhanSatThuong = other.GetComponent<NhanSatThuong>();
                if (nhanSatThuong != null)
                {
                    // Truyền sát thương động đã nhận từ GunWeaponData qua hàm Activate
                    nhanSatThuong.TakeDamage(damage); 
                }
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
