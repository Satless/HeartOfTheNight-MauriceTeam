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
}
