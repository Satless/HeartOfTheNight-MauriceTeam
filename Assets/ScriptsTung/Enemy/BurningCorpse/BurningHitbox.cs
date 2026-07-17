using UnityEngine;

public class BurningHitbox : MonoBehaviour
{
    [Header("Kéo con quái mẹ (BurningCorpse) vào đây")]
    public BurningCorpse parentCorpse;

    private bool hasDealtDamage = false;

    private void OnEnable()
    {
        hasDealtDamage = false; // Reset lại mỗi khi quái vung tay
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        XuLySatThuong(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        XuLySatThuong(collision);
    }

    private void XuLySatThuong(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hasDealtDamage)
        {
            hasDealtDamage = true;

            PlayerHealth pHealth = collision.GetComponent<PlayerHealth>();
            if (pHealth != null && parentCorpse != null)
            {
                // Báo cho con quái mẹ biết là đã chém trúng, kêu mẹ nó trừ máu đi
                parentCorpse.DealDamageAndBurn(pHealth);
            }

            // Chém trúng rồi thì tắt Hitbox
            gameObject.SetActive(false);
        }
    }
}