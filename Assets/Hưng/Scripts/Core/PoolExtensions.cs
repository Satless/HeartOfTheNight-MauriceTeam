using UnityEngine;

/// <summary>
/// Extension Methods cho GameObject và Component.
/// Thay thế Instantiate/Destroy bằng Spawn/Despawn để tự động dùng Object Pooling.
/// Ví dụ: prefab.Spawn(pos) thay cho Instantiate(prefab, pos, ...)
///         instance.Despawn() thay cho Destroy(instance)
/// </summary>
public static class PoolExtensions
{
    // ─── SPAWN (thay thế Instantiate) ─────────────────────────────────────

    /// <summary>
    /// Lấy clone từ Pool tại vị trí và góc xoay chỉ định.
    /// </summary>
    public static GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return Pooling.Instance.Get(prefab, position, rotation);
    }

    /// <summary>
    /// Lấy clone từ Pool tại vị trí chỉ định (góc xoay mặc định).
    /// </summary>
    public static GameObject Spawn(this GameObject prefab, Vector3 position)
    {
        return Pooling.Instance.Get(prefab, position, Quaternion.identity);
    }

    /// <summary>
    /// Lấy clone từ Pool tại gốc tọa độ.
    /// </summary>
    public static GameObject Spawn(this GameObject prefab)
    {
        return Pooling.Instance.Get(prefab, Vector3.zero, Quaternion.identity);
    }

    /// <summary>
    /// Lấy clone từ Pool và trả về Component T (VD: Bullet, HitVfx...).
    /// </summary>
    public static T Spawn<T>(this T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        GameObject obj = Pooling.Instance.Get(prefab.gameObject, position, rotation);
        return obj.GetComponent<T>();
    }

    /// <summary>
    /// Lấy clone từ Pool và trả về Component T tại vị trí chỉ định.
    /// </summary>
    public static T Spawn<T>(this T prefab, Vector3 position) where T : Component
    {
        GameObject obj = Pooling.Instance.Get(prefab.gameObject, position, Quaternion.identity);
        return obj.GetComponent<T>();
    }

    // ─── DESPAWN (thay thế Destroy) ───────────────────────────────────────

    /// <summary>
    /// Trả clone về Pool (tắt đi, không Destroy). Zero-GC.
    /// </summary>
    public static void Despawn(this GameObject instance)
    {
        if (instance == null) return;
        if (Pooling.Instance != null)
            Pooling.Instance.Return(instance);
        else
            Object.Destroy(instance);
    }

    /// <summary>
    /// Trả clone về Pool thông qua bất kỳ Component nào.
    /// </summary>
    public static void Despawn(this Component instance)
    {
        if (instance == null) return;
        if (Pooling.Instance != null)
            Pooling.Instance.Return(instance.gameObject);
        else
            Object.Destroy(instance.gameObject);
    }

    // ─── PREWARM (tạo sẵn) ───────────────────────────────────────────────

    /// <summary>
    /// Tạo sẵn N clone tắt sẵn trong Pool. Gọi lúc khởi động game.
    /// </summary>
    public static void Prewarm(this GameObject prefab, int count)
    {
        Pooling.Instance.Prewarm(prefab, count);
    }
}
