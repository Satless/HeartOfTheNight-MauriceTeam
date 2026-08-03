using UnityEngine;

[CreateAssetMenu(fileName = "NewGunWeaponData", menuName = "Data/Gun Weapon Data")]
public class GunWeaponData : ScriptableObject
{
    [Header("Fire")]
    [Tooltip("Giây giữa các phát khi giữ chuột (liên tục)")]
    public float fireRate;
    [Tooltip("Tốc độ đạn khi xuất hiện")]
    public float bulletSpeed;
    [Tooltip("Sát thương cơ bản của mỗi viên đạn")]
    public int damage;
    [Tooltip("Đạn tự tắt khi không trúng gì cả")]
    public float bulletLifetime;

    [Header("Shotgun / Multi-shot")]
    [Tooltip("Số viên đạn bắn ra trong 1 lần bóp cò (Pistol/Minigun = 1)")]
    public int bulletsPerShot;
    [Tooltip("Góc tỏa ngẫu nhiên của đạn (Shotgun = 15-30, Minigun = 2-3, Pistol = 0)")]
    public float spreadAngle;

    [Header("Vertical Multi-shot (Contra Style)")]
    [Tooltip("Tích vào nếu đạn dàn song song theo trục DỌC thay vì tỏa góc ngang (Kiểu Contra). " +
             "Khi bật, spreadAngle sẽ bị bỏ qua — dùng verticalSpacing để chỉnh khoảng cách.")]
    public bool isVerticalSpread;
    [Tooltip("Khoảng cách giữa mỗi đường đạn dọc (đơn vị Unity). " +
             "VD: 0.5 = nửa ô, 1.0 = 1 ô. Các đường đạn dàn đều quanh nòng súng.")]
    [Range(0.1f, 3f)]
    public float verticalSpacing;

    [Header("Visuals")]
    [Tooltip("Ảnh hiển thị vũ khí trên HUD")]
    public Sprite weaponIcon;
    [Tooltip("Tốc độ chạy clip hoạt ảnh bắn (Dự phòng cho súng lửa liên tục)")]
    public float animationSpeedMultiplier;
    [Tooltip("Kéo Prefab đạn của súng này vào đây")]
    public Bullet bulletPrefab;

    [Tooltip("Kéo Animator Override Controller của súng này vào đây")]
    public RuntimeAnimatorController weaponAnimator;
    [Tooltip("Kéo ĐÚNG Clip Fire/Shoot của súng này vào đây để hệ thống tự động đồng bộ tốc độ bắn (Auto-Sync)")]
    public AnimationClip fireAnimationClip;

    [Header("Explosive / AOE")]
    [Tooltip("Đạn có phát nổ AOE khi chạm mục tiêu/tường không?")]
    public bool isExplosive;
    [Tooltip("Bán kính vụ nổ (0 nếu không nổ)")]
    public float explosionRadius;
    [Tooltip("Sát thương nổ lan (cộng dồn với sát thương chính nếu trúng trực tiếp)")]
    public int explosionDamage;

    [Header("Special Logic")]
    [Tooltip("Hiệu ứng trạng thái áp dụng lên quái (Ví dụ: Thiêu Đốt)")]
    public StatusEffectData statusEffect;

    [Tooltip("Số lượng quái vật tối đa đạn có thể bay xuyên qua (0 = không xuyên)")]
    public int pierceCount;
    [Tooltip("Tích vào nếu đây là súng bắn liên tục (Súng lửa) - Sẽ không dùng Animation Event")]
    public bool isContinuousFire = false;
    [Tooltip("Prefab dùng cho súng bắn liên tục (Súng lửa) - Sẽ thay thế Bullet Prefab")]
    public GameObject continuousVfxPrefab;

    [Header("Knockback")]
    [Tooltip("Lực đẩy lùi khi đạn trúng mục tiêu trực tiếp (0 = không đẩy). " +
             "VD: Lục thường = 5, Minigun = 2, Lục điện/Lửa = 0")]
    public float knockbackForce;
    [Tooltip("Lực đẩy lùi từ vụ nổ AOE (0 = không đẩy). " +
             "Chỉ áp dụng cho súng có isExplosive = true. Hướng đẩy = tâm nổ → mục tiêu.")]
    public float explosionKnockbackForce;

    [Header("Pooling")]
    [Tooltip("Số lượng đạn tạo sẵn trong Pool khi game khởi động")]
    public int bulletPrewarmCount;
    [Tooltip("Số lượng hiệu ứng nổ (HitVfx) tạo sẵn trong Pool khi game khởi động")]
    public int hitVfxPrewarmCount;

    [Header("Overheat (Quá nhiệt)")]
    [Tooltip("Tích vào nếu súng này sinh nhiệt khi bắn. " +
             "Nếu tắt, súng này KHÔNG cộng nhiệt vào thanh chung.")]
    public bool canOverheat;
    [Tooltip("Nhiệt cộng vào thanh chung mỗi lần bắn (đạn thường) hoặc mỗi giây (súng lửa). " +
             "VD: Minigun = 3 (nóng nhanh), Pistol = 1 (nóng chậm), Shotgun = 5 (nóng vừa do bắn chậm)")]
    public float heatPerShot;

    private void OnValidate()
    {
        // Rào trước tránh lỗi
        if (animationSpeedMultiplier <= 0) animationSpeedMultiplier = 1f;
        if (bulletsPerShot <= 0) bulletsPerShot = 1;
    }
}
