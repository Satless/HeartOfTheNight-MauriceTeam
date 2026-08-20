using UnityEngine;
using HeartOfTheNight.Common;

public class BurnHB : MonoBehaviour
{
    public int attackDamage = 10;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. In ra để test xem Hitbox đã chạm được Player chưa
        Debug.Log("Hitbox chém trúng: " + collision.gameObject.name);

        if (collision.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // LỌC: Đảm bảo chỉ đánh trúng Hurtbox, không đánh vào Collider đất
            if (!collision.isTrigger) return;

            // 2. TÌM INTERFACE IDamageable TỪ HURTBOX HOẶC TỪ OBJECT CHA
            IDamageable target = collision.GetComponent<IDamageable>();
            if (target == null) target = collision.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                BurningCorpseImg burnScript = GetComponentInParent<BurningCorpseImg>();

                if (burnScript != null)
                {
                    // 3. Truyền thẳng Hurtbox vào hàm để BurnScript xử lý
                    burnScript.DealDamageAndBurn(collision);
                }
                else
                {
                    target.TakeDamage(attackDamage);
                }

                // 4. Đánh xong thì tự tắt
                gameObject.SetActive(false);
            }
        }
    }
}