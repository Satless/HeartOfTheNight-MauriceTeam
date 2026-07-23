using UnityEngine;

/// <summary>
/// Quản lý logic vùng sát thương của Súng phun lửa bằng Physics2D.OverlapBoxNonAlloc.
/// KHÔNG CẦN Collider trên Prefab. Zero-GC.
/// </summary>
public class FlamethrowerLogic : MonoBehaviour
{
    [Header("Hitbox Cấu hình")]
    [Tooltip("Kích thước vùng lửa (Rộng x Cao)")]
    public Vector2 hitboxSize = new Vector2(5f, 2f);
    [Tooltip("Khoảng cách từ nòng súng đến tâm vùng lửa")]
    public Vector2 hitboxOffset = new Vector2(2.5f, 0f);
    [Tooltip("Layer của quái (Nên chọn đúng layer quái để tối ưu)")]
    public LayerMask targetLayer = ~0;

    private StatusEffectData _statusEffect;
    private Collider2D[] _results = new Collider2D[20];

    public void Activate(StatusEffectData effectData)
    {
        _statusEffect = effectData;
    }

    private void Update()
    {
        if (_statusEffect == null) return;

        // Tính tâm của Hitbox (hỗ trợ cả xoay Y 180 độ)
        Vector2 centerPos = (Vector2)transform.position + (Vector2)(transform.right * hitboxOffset.x) + (Vector2)(transform.up * hitboxOffset.y);

        // Quét tất cả collider lọt vào vùng lửa (Zero GC)
        int count = Physics2D.OverlapBoxNonAlloc(centerPos, hitboxSize, transform.eulerAngles.z, _results, targetLayer);

        for (int i = 0; i < count; i++)
        {
            StatusEffectReceiver receiver = _results[i].GetComponent<StatusEffectReceiver>();
            if (receiver != null)
            {
                receiver.ApplyStatus(_statusEffect);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Vector2 centerPos = (Vector2)transform.position + (Vector2)(transform.right * hitboxOffset.x) + (Vector2)(transform.up * hitboxOffset.y);
        
        Gizmos.matrix = Matrix4x4.TRS(centerPos, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hitboxSize);
    }
}
