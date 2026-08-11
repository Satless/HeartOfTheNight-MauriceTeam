using UnityEngine;
using HeartOfTheNight.Common; // 1. THÊM THƯ VIỆN CHỨA BỘ LUẬT

public class DoomSkill : MonoBehaviour
{
    [Header("Cài đặt Sát thương")]
    public int damage = 15;

    [Header("Dọn rác")]
    public float lifeTime = 5f;

    [Header("Hiệu ứng (Tùy chọn)")]
    public GameObject hitEffect;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. NẾU BẮN TRÚNG PLAYER
        if (collision.CompareTag("Player"))
        {
            // 2. TÌM IDamageable THAY VÌ PlayerHealth
            IDamageable target = collision.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
                Debug.Log("Đạn DoomBringer trúng Player qua IDamageable! Trừ " + damage + " máu.");
            }

            // Trúng người là nổ/biến mất luôn
            TuHuy();
        }
        // 2. NẾU RỚT XUỐNG ĐẤT / ĐẬP VÀO TƯỜNG (Dành cho Bom)
        else if (collision.CompareTag("Ground"))
        {
            TuHuy();
        }
    }

    void TuHuy()
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}