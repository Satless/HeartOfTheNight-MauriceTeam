using UnityEngine;

/// <summary>
/// Component tự động gắn lên mỗi clone khi được Pool sinh ra.
/// Lưu PrefabId (Tên của Prefab gốc) để khi Despawn, hệ thống biết phải trả về đúng Pool nào.
/// Developer KHÔNG CẦN quan tâm đến file này.
/// </summary>
public class PoolMember : MonoBehaviour
{
    /// <summary>
    /// Tên của Prefab gốc đã sinh ra clone này.
    /// Dùng tên thay vì InstanceID để đảm bảo Pool không bị mồ côi khi đổi Scene và UnloadAssets.
    /// </summary>
    [HideInInspector] public string PrefabId;
}
