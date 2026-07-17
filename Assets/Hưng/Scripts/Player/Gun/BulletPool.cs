using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object Pool đa hình (Multi-Pool) — hỗ trợ nhiều loại đạn khác nhau trong cùng 1 Pool Manager.
/// Gắn lên một GameObject trống trong Scene, kéo vào PlayerAttack qua Inspector.
/// </summary>
public class BulletPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("Số đạn tạo sẵn lúc đầu cho MỖI LOẠI súng")]
    [SerializeField] private int _initialSizePerType = 20;

    // Dictionary lưu trữ nhiều Stack đạn. Khóa (key) là tên của Prefab đạn.
    private readonly Dictionary<string, Stack<Bullet>> _pools = new Dictionary<string, Stack<Bullet>>();

    /// <summary>
    /// Hàm này để PlayerAttack gọi lúc khởi động, giúp đẻ sẵn đạn ra tránh lag lúc mới bắn.
    /// </summary>
    public void Prewarm(Bullet prefab)
    {
        if (prefab == null) return;
        string key = prefab.name;

        if (!_pools.ContainsKey(key))
        {
            _pools[key] = new Stack<Bullet>();
            for (int i = 0; i < _initialSizePerType; i++)
            {
                Bullet bullet = CreateBullet(prefab, key);
                bullet.gameObject.SetActive(false);
                _pools[key].Push(bullet);
            }
        }
    }

    /// <summary>
    /// Lấy đạn từ pool. Yêu cầu truyền vào đúng Prefab của loại súng đang cầm.
    /// </summary>
    public Bullet Get(Bullet prefab, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogError("BulletPool: Prefab đạn bị null! Hãy kiểm tra lại ScriptableObject súng.");
            return null;
        }

        string key = prefab.name;

        if (!_pools.ContainsKey(key))
        {
            _pools[key] = new Stack<Bullet>();
        }

        Stack<Bullet> pool = _pools[key];
        Bullet bullet;

        if (pool.Count > 0)
        {
            bullet = pool.Pop();
        }
        else
        {
            bullet = CreateBullet(prefab, key);
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
        if (_pools.ContainsKey(bullet.PoolKey))
        {
            _pools[bullet.PoolKey].Push(bullet);
        }
        else
        {
            Debug.LogWarning($"BulletPool: Trả đạn về nhưng không tìm thấy pool '{bullet.PoolKey}'. Đã hủy.");
            Destroy(bullet.gameObject);
        }
    }

    private Bullet CreateBullet(Bullet prefab, string key)
    {
        Bullet bullet = Instantiate(prefab, transform);
        bullet.Init(this, key); // Truyền key để viên đạn nhớ nhà của nó
        return bullet;
    }
}
