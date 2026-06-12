using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    public int healAmount = 20;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. KIỂM TRA XEM PLAYER CÓ ĐANG BỊ DÍNH BÙA CỦA WRATH KHÔNG?
            AntiHeal bua = collision.GetComponent<AntiHeal>();

            if (bua != null && bua.thoiGianConLai > 0)
            {
                Debug.Log("Đang dính hiệu ứng Vết Thương Sâu! Không thể hồi máu trong " + Mathf.Round(bua.thoiGianConLai) + " giây!");
                // Bạn có thể phát âm thanh báo lỗi ở đây
                return; // Văng ra luôn, không cho ăn máu
            }

            // 2. NẾU KHÔNG DÍNH BÙA THÌ HỒI MÁU BÌNH THƯỜNG
            PlayerHealth pHealth = collision.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.Heal(healAmount);
                Debug.Log("Đã hồi " + healAmount + " máu!");
                Destroy(gameObject); // Ăn xong thì biến mất
            }
        }
    }
}