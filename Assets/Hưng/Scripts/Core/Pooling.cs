using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống Object Pooling toàn cục (Universal Auto-Pool).
/// Tự động khởi tạo khi game chạy — không cần kéo thả lên Scene.
/// Sử dụng PoolExtensions (.Spawn() / .Despawn()) để tương tác.
/// </summary>
public class Pooling : MonoBehaviour
{
    public static Pooling Instance { get; private set; }

    // Khóa = Tên của Prefab gốc, Giá trị = hàng đợi clone đang tắt chờ tái sử dụng
    private readonly Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
    private bool _isQuitting;

    [Header("Debug Tracking")]
    [Tooltip("Theo dõi số lượng object rảnh rỗi trong từng Pool (Chỉ chạy trên Editor)")]
    [ReadOnly] public List<string> poolTracking = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;

        var go = new GameObject("--- POOLING ---");
        Instance = go.AddComponent<Pooling>();
        DontDestroyOnLoad(go);
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Tự động cập nhật giao diện Inspector mỗi frame để dễ Debug
        poolTracking.Clear();
        foreach (var kvp in _pools)
        {
            poolTracking.Add($"[{kvp.Key}] : {kvp.Value.Count} rảnh");
        }
    }
#endif

    /// <summary>
    /// Lấy 1 clone từ Pool. Nếu Pool rỗng hoặc chưa tồn tại, tự Instantiate thêm.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;

        if (_pools.TryGetValue(key, out var queue) && queue.Count > 0)
        {
            GameObject obj = queue.Dequeue();
            obj.transform.SetParent(null);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        // Pool rỗng hoặc chưa tồn tại → Tạo clone mới
        return CreateInstance(prefab, key, position, rotation, setActive: true);
    }

    /// <summary>
    /// Trả clone về Pool (tắt đi, không Destroy). Zero-GC.
    /// </summary>
    public void Return(GameObject instance)
    {
        if (_isQuitting || instance == null) return;

        var member = instance.GetComponent<PoolMember>();
        if (member == null)
        {
            // Không thuộc Pool nào → Destroy bình thường (fallback an toàn)
            Debug.LogWarning($"[Pooling] '{instance.name}' không có PoolMember. Đã Destroy thay vì trả về Pool.");
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(transform);

        string key = member.PrefabId;
        if (!_pools.TryGetValue(key, out var queue))
        {
            queue = new Queue<GameObject>();
            _pools[key] = queue;
        }
        queue.Enqueue(instance);
    }

    /// <summary>
    /// Tạo sẵn N clone tắt sẵn trong Pool (gọi lúc khởi động để Zero-GC khi gameplay).
    /// Nếu pool đã tồn tại, chỉ bổ sung thêm clone mới.
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        string key = prefab.name;
        if (!_pools.ContainsKey(key))
        {
            _pools[key] = new Queue<GameObject>();
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = CreateInstance(prefab, key, Vector3.zero, Quaternion.identity, setActive: false);
            _pools[key].Enqueue(obj);
        }
    }

    private GameObject CreateInstance(GameObject prefab, string prefabId, Vector3 position, Quaternion rotation, bool setActive)
    {
        GameObject obj;

        if (setActive)
        {
            // Sinh ra để dùng ngay → không gắn parent (tự do trong World Space)
            obj = Instantiate(prefab, position, rotation);
        }
        else
        {
            // Sinh ra để cất vào kho → gắn dưới Pooling cho Hierarchy gọn gàng
            obj = Instantiate(prefab, position, rotation, transform);
            obj.SetActive(false);
        }

        // Gắn PoolMember để clone nhớ đường về đúng Pool
        var member = obj.GetComponent<PoolMember>();
        if (member == null)
        {
            member = obj.AddComponent<PoolMember>();
        }
        member.PrefabId = prefabId;

        return obj;
    }
}
