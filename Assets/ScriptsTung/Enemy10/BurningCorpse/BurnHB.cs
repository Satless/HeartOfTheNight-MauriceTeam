using UnityEngine;
using HeartOfTheNight.Common; // 1. THÊM THƯ VIỆN BỘ LUẬT CHUNG

public class BurnHB : MonoBehaviour
{
    public int attackDamage = 10;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. In ra để test xem Hitbox đã chạm được Player chưa
        Debug.Log("Hitbox chém trúng: " + collision.gameObject.name);

        if (collision.CompareTag("Player"))
        {
            // 2. TÌM INTERFACE IDamageable THAY VÌ PlayerHealth
            IDamageable target = collision.GetComponent<IDamageable>();

            if (target != null)
            {
                // Tên sếp vẫn là BurningCorpseImg
                BurningCorpseImg burnScript = GetComponentInParent<BurningCorpseImg>();

                if (burnScript != null)
                {
                    // 3. Truyền thẳng Collider2D (collision) vào hàm như đã sửa ở file sếp
                    burnScript.DealDamageAndBurn(collision);
                }
                else
                {
                    // Nếu không có sếp thì tự hitbox trừ máu luôn
                    target.TakeDamage(attackDamage);
                }

                // 4. Đánh xong thì tự tắt
                gameObject.SetActive(false);
            }
        }
    }
}