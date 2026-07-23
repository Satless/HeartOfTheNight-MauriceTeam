using UnityEngine;

[CreateAssetMenu(menuName = "Gun Weapon Data")]
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

    [Header("Visuals")]
    [Tooltip("Tốc độ chạy clip hoạt ảnh bắn (1=Bình thường, 2=Nhanh gấp đôi)")]
    public float animationSpeedMultiplier = 1f;
    [Tooltip("Kéo Prefab đạn của súng này vào đây")]
    public Bullet bulletPrefab;

    [Tooltip("Kéo Animator Override Controller của súng này vào đây")]
    public RuntimeAnimatorController weaponAnimator;

    [Header("Special Logic")]
    [Tooltip("Số lượng quái vật tối đa đạn có thể bay xuyên qua (0 = không xuyên)")]
    public int pierceCount = 0;
    [Tooltip("Tích vào nếu đây là súng bắn liên tục (Súng lửa) - Sẽ không dùng Animation Event")]
    public bool isContinuousFire = false;
}
