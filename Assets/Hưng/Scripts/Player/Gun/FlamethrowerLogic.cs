using UnityEngine;

/// <summary>
/// Quản lý logic vùng sát thương của Súng phun lửa bằng Physics2D.OverlapBoxAll.
/// KHÔNG CẦN Collider trên Prefab.
/// </summary>
public class FlamethrowerLogic : MonoBehaviour
{
    [Header("Hitbox")]
    [Tooltip("Kích thước vùng lửa")]
    public Vector2 hitboxSize = new Vector2(5f, 2f);
    [Tooltip("Khoảng cách từ nòng súng đến tâm vùng lửa")]
    public Vector2 hitboxOffset = new Vector2(2.5f, 0f);
    [Tooltip("Layer mục tiêu")]
    public LayerMask targetLayer = ~0;

    [Header("Tag Filter")]
    [Tooltip("Danh sách tag được phép nhận sát thương lửa")]
    [TagSelector]
    [SerializeField] private string[] _targetTags;

    [Header("Debug Tracking")]
    [Tooltip("Hiệu ứng trạng thái (Cháy, Độc...) đang được gán cho luồng lửa này")]
    [SerializeField, ReadOnly] private StatusEffectData _statusEffect;

    public void Activate(StatusEffectData effectData)
    {
        _statusEffect = effectData;
    }

    private void Update()
    {
        if (_statusEffect == null) return;

        // Tính tâm của Hitbox (hỗ trợ cả xoay Y 180 độ)
        Vector2 centerPos = (Vector2)transform.position + (Vector2)(transform.right * hitboxOffset.x) + (Vector2)(transform.up * hitboxOffset.y);

        // Quét tất cả collider lọt vào vùng lửa
        var results = Physics2D.OverlapBoxAll(centerPos, hitboxSize, transform.eulerAngles.z, targetLayer);

        for (int i = 0; i < results.Length; i++)
        {
            // Lọc theo Tag trước khi xử lý
            if (!HasTargetTag(results[i])) continue;

            StatusEffectReceiver receiver = results[i].GetComponent<StatusEffectReceiver>();
            if (receiver != null)
            {
                receiver.ApplyStatus(_statusEffect);
            }
        }
    }

    /// <summary>
    /// Kiểm tra xem collider có nằm trong danh sách tag cho phép không.
    /// Nếu _targetTags rỗng (chưa setup) → cho phép tất cả.
    /// </summary>
    private bool HasTargetTag(Collider2D col)
    {
        if (_targetTags == null || _targetTags.Length == 0) return true;

        for (int i = 0; i < _targetTags.Length; i++)
        {
            if (col.CompareTag(_targetTags[i])) return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Vector2 centerPos = (Vector2)transform.position + (Vector2)(transform.right * hitboxOffset.x) + (Vector2)(transform.up * hitboxOffset.y);
        
        Gizmos.matrix = Matrix4x4.TRS(centerPos, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hitboxSize);
    }
}
