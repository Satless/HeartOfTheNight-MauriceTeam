using UnityEngine;

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Data/Status Effect")]
public class StatusEffectData : ScriptableObject
{
    [Tooltip("Tên của hiệu ứng (VD: Cháy, Độc)")]
    public string effectName = "Cháy";

    [Tooltip("Tổng thời gian kéo dài hiệu ứng (giây)")]
    public float duration = 3f;

    [Tooltip("Sát thương gây ra mỗi nhịp (Tick)")]
    public int damagePerTick = 5;

    [Tooltip("Thời gian giữa mỗi nhịp giật sát thương (giây). Càng nhỏ giật càng nhanh.")]
    public float tickInterval = 0.5f;

    [Tooltip("Hình ảnh/VFX đính lên người mục tiêu khi dính hiệu ứng này (Tùy chọn)")]
    public GameObject effectVfxPrefab;
}
