using UnityEngine;

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Data/Status Effect")]
public class StatusEffectData : ScriptableObject
{
    [Tooltip("Tên của hiệu ứng (VD: Cháy, Độc)")]
    public string effectName;

    [Tooltip("Tổng thời gian kéo dài hiệu ứng (giây)")]
    public float duration;

    [Tooltip("Sát thương gây ra mỗi nhịp (Tick)")]
    public int damagePerTick;

    [Tooltip("Thời gian giữa mỗi nhịp giật sát thương (giây). Càng nhỏ giật càng nhanh.")]
    public float tickInterval;

    [Tooltip("Hình ảnh/VFX đính lên người mục tiêu khi dính hiệu ứng này")]
    public GameObject effectVfxPrefab;

    [Header("Pooling")]
    [Tooltip("Số lượng VFX tạo sẵn trong Pool khi game khởi động (mỗi hiệu ứng tự quyết số lượng riêng)")]
    public int prewarmCount;

    [Header("Audio Settings")]
    [Tooltip("Category (Tầng 1) - Ví dụ: StatusEffect")]
    public string soundCategory = "Effects";
    [Tooltip("SubCategory (Tầng 2) - Ví dụ: Burn, Poison")]
    public string soundSubCategory;
    [Tooltip("ActionName (Tầng 3) phát khi dính hiệu ứng - Ví dụ: Apply")]
    public string applySoundName = "ApplyEffect";
    [Tooltip("ActionName (Tầng 3) phát mỗi nhịp giật - Ví dụ: Tick")]
    public string tickSoundName = "TickDmg";
}
