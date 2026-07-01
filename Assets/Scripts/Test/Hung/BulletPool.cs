using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object Pool cho đạn — tái sử dụng thay vì Instantiate/Destroy mỗi phát bắn.
/// Gắn lên một GameObject trống trong Scene, kéo vào PlayerAttack qua Inspector.
/// </summary>
public class BulletPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("Kéo Prefab đạn vào đây")]
    [SerializeField] private GameObject _bulletPrefab;
    [Tooltip("Số đạn tạo sẵn lúc đầu")]
    [SerializeField] private int _initialSize = 20;

    // Stack nhanh hơn Queue cho pool vì chỉ cần push/pop — O(1)
    private readonly Stack<Bullet> _pool = new Stack<Bullet>();

    private void Start()
    {
        // Tạo sẵn đạn lúc khởi động — tránh GC spike khi gameplay bắt đầu
        for (int i = 0; i < _initialSize; i++)
        {
            Bullet bullet = CreateBullet();
            bullet.gameObject.SetActive(false);
            _pool.Push(bullet);
        }
    }

    /// <summary>
    /// Lấy đạn từ pool. Tự tạo thêm nếu pool hết (mở rộng linh hoạt).
    /// </summary>
    public Bullet Get(Vector3 position)
    {
        Bullet bullet;

        if (_pool.Count > 0)
        {
            bullet = _pool.Pop();
        }
        else
        {
            // Pool hết → tạo thêm (hiếm khi xảy ra nếu _initialSize đủ lớn)
            bullet = CreateBullet();
        }

        bullet.transform.position = position;
        bullet.gameObject.SetActive(true);
        return bullet;
    }

    /// <summary>
    /// Trả đạn về pool thay vì Destroy — Zero GC.
    /// </summary>
    public void Return(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
        _pool.Push(bullet);
    }

    private Bullet CreateBullet()
    {
        GameObject go = Instantiate(_bulletPrefab, transform);
        Bullet bullet = go.GetComponent<Bullet>();
        bullet.Init(this); // Truyền tham chiếu pool để Bullet tự trả về
        return bullet;
    }
}
