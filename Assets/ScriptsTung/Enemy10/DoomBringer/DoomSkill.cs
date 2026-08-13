using UnityEngine;
using HeartOfTheNight.Common; // THÊM THƯ VIỆN CHỨA BỘ LUẬT

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
        if (collision.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // LỌC: Bỏ qua va chạm cứng, chỉ xét Hurtbox mềm (isTrigger = true)
            if (!collision.isTrigger) return;

            // TÌM IDamageable TỪ HURTBOX HOẶC TỪ OBJECT CHA
            IDamageable target = collision.GetComponent<IDamageable>();
            if (target == null) target = collision.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                target.TakeDamage(damage);
                Debug.Log("Đạn DoomBringer trúng HURTBOX Player qua IDamageable! Trừ " + damage + " máu.");
            }

            // Trúng người là nổ/biến mất luôn
            TuHuy();
        }
        // 2. NẾU RỚT XUỐNG ĐẤT / ĐẬP VÀO TƯỜNG (Check bằng Layer Ground)
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
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