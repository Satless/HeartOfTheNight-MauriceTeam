using UnityEngine;

public class BurnHB : MonoBehaviour
{
    public int attackDamage = 10;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. In ra để test xem Hitbox đã chạm được Player chưa
        Debug.Log("Hitbox chém trúng: " + collision.gameObject.name);

        if (collision.CompareTag("Player"))
        {
            PlayerHealth pHealth = collision.GetComponent<PlayerHealth>();
            ////////S
            if (pHealth != null)
            {
                // 2. Tên sếp bây giờ là BurningCorpseImg nhé!
                BurningCorpseImg burnScript = GetComponentInParent<BurningCorpseImg>();

                if (burnScript != null)
                {
                    burnScript.DealDamageAndBurn(pHealth);
                }
                else
                {
                    pHealth.TakeDamage(attackDamage);
                }

                // 3. Đánh xong thì tự tắt
                gameObject.SetActive(false);
            }
        }
    }
}