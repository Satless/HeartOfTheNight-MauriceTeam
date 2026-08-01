using UnityEngine;

[CreateAssetMenu(menuName = "Gun Weapon")]
public class GunWeapon : ScriptableObject
{
    [Header("Fire")]
    public float fireRate = 0.15f;      // Giây giữa các phát khi giữ chuột (liên tục)
    public float bulletSpeed = 20f;     // Tốc độ đạn khi spawn
    public int damage = 10;

    [Header("Bullet")]
    public GameObject bulletPrefab;     // Kéo Bullet Prefab vào đây trong Inspector
    public float bulletLifetime = 3f;   // Đạn tự destroy sau n giây nếu không trúng gì
}
