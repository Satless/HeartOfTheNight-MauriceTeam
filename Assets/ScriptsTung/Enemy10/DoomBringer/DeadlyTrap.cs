using UnityEngine;
using HeartOfTheNight.Common;

public class DeadlyTrap : MonoBehaviour
{
    [Header("Cài đặt Sát thương Bẫy")]
    [Tooltip("Lượng máu sẽ trừ")]
    public int damageToDeal = 20;

    [Tooltip("Bật Tích = Đứng lên là mất máu LIÊN TỤC. Bỏ Tích = Chỉ mất máu 1 lần duy nhất lúc chạm")]
    public bool dealDamageContinuously = true;

    [Tooltip("Thời gian chờ giữa 2 lần trừ máu (Tính bằng giây. Khuyên dùng: 0.5 - 1s)")]
    public float damageInterval = 1f;

    [Tooltip("Có phá hủy/xóa trap này sau khi chạm không? (Chỉ xài khi dealDamageContinuously = false)")]
    public bool destroyTrapOnHit = false;

    private float nextDamageTime = 0f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        GiaoSatThuong(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (dealDamageContinuously) GiaoSatThuong(collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GiaoSatThuong(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (dealDamageContinuously) GiaoSatThuong(collision.collider);
    }

    private void GiaoSatThuong(Collider2D col)
    {
        if (Time.time < nextDamageTime) return;

        // 🔥 FIX 1: Nhận diện Player đa năng (Kiểm tra tag/layer ở chính nó và ở Root)
        bool isPlayer = col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player") || col.transform.root.CompareTag("Player");

        if (isPlayer)
        {
            // 🔥 FIX 2: Quét IDamageable toàn diện (Từ chính nó, lên trên, và xuống dưới con)
            IDamageable target = col.GetComponent<IDamageable>();
            if (target == null) target = col.GetComponentInParent<IDamageable>();
            if (target == null) target = col.GetComponentInChildren<IDamageable>();

            if (target != null)
            {
                target.TakeDamage(damageToDeal);
                Debug.Log($"<color=red>Bẫy {gameObject.name} rút của Player {damageToDeal} HP!</color>");

                nextDamageTime = Time.time + damageInterval;

                if (destroyTrapOnHit)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}