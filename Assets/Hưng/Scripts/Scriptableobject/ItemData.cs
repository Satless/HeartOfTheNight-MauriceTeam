using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Data/Item Data")]
public class ItemData : ScriptableObject
{
    public enum ItemType { HealHP, Shield, SpeedBuff, JumpBuff, WeaponUnlock }

    /// <summary>
    /// Independent: mỗi buff tự chạy timer riêng (nhặt 2 cái = 2 timer song song).
    /// ExtendDuration: nhặt thêm = cộng thêm thời gian vào buff đang chạy.
    /// </summary>
    public enum DurationBehavior { Independent, ExtendDuration }

    // ─── ITEM INFORMATION ───────────────────────────────────────────────────────
    [Header("Item Information")]
    public string itemName;
    public ItemType itemType;

    [Tooltip("Giá trị của vật phẩm (lượng máu hồi, lượng vàng cộng thêm...)")]
    public int value;

    // ─── BUFF SETTINGS ──────────────────────────────────────────────────────────
    [Header("Buff Settings (Shield / Speed / Jump)")]
    [Tooltip("Thời gian buff tồn tại (giây). Chỉ dùng cho buff có thời hạn.")]
    public float buffDuration;

    [Tooltip("Hệ số nhân (VD: 1.5 = tăng 50%). Dùng cho SpeedBuff / JumpBuff.")]
    public float multiplier = 1f;

    // ─── STACK SETTINGS ─────────────────────────────────────────────────────────
    [Header("Stack Settings")]
    [Tooltip("Số lần buff cùng loại có thể cộng dồn tối đa. 1 = không cộng dồn.")]
    [Min(1)]
    public int maxStacks = 1;

    [Tooltip("Giới hạn hệ số nhân tối đa khi cộng dồn (VD: 3.0 = tối đa gấp 3 lần).")]
    public float maxMultiplier = 3f;

    [Tooltip("Cách xử lý thời gian khi nhặt thêm buff cùng loại.")]
    public DurationBehavior durationBehavior = DurationBehavior.Independent;

    // ─── VISUAL EFFECTS ─────────────────────────────────────────────────────────
    [Header("Visual Effects")]
    [Tooltip("Prefab VFX phát một lần khi nhặt (nổ tung, lấp lánh...). Để trống nếu chưa có.")]
    public GameObject pickupVFX;

    [Tooltip("Prefab visual gắn lên player suốt thời gian buff (khiên, aura tốc độ...). Để trống nếu chưa có.")]
    public GameObject buffVisualPrefab;

    // ─── WEAPON UNLOCK SETTINGS ─────────────────────────────────────────────────
    [Header("Weapon Unlock Settings")]
    [Tooltip("Ô súng sẽ được mở khóa (1, 2, 3, 4). Chỉ dùng cho ItemType.WeaponUnlock.")]
    [Range(1, 4)]
    public int weaponSlotIndex = 1;

    [Tooltip("Phần trăm thanh nhiệt sẽ được giảm đi nếu nhặt trùng súng đã mở khóa (0.5 = 50%).")]
    [Range(0f, 1f)]
    public float heatReducePercentage = 0.5f;

    // ─── MAGNET SETTINGS ────────────────────────────────────────────────────────
    [Header("Magnet Settings")]
    [Tooltip("Khoảng cách đủ gần để người chơi thực sự nhặt được vật phẩm.")]
    public float collectDistance;

    // ─── SOUND ───────────────────────────────────────────────────────────────────
    [Header("Sound")]
    [Tooltip("Âm thanh khi nhặt — theo cấu trúc 3 tầng của SoundLibrary_New.")]
    public string sfxCategory = "Player";
    public string sfxSubCategory = "CollectItems";
    public string sfxAction = "n";
}
