using UnityEngine;

[CreateAssetMenu(menuName = "Gun Weapon")]
public class GunWeapon : ScriptableObject
{
    [Header("Fire")]
    [Tooltip("Giây giữa các phát khi giữ chuột (liên tục)")]
    public float fireRate = 0.15f;
    [Tooltip("Tốc độ đạn khi xuất hiện")]
    public float bulletSpeed = 20f;
    [Tooltip("Sát thương cơ bản của mỗi viên đạn")]
    public int damage = 10;

    [Header("Bullet")]
    [Tooltip("Kéo Prefab đạn vào đây")]
    public GameObject bulletPrefab;
    [Tooltip("Đạn tự tắt khi không trúng gì cả")]
    public float bulletLifetime = 3f;
}
