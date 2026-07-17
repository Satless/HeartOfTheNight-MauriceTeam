using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    public int attackDamage = 10;

    // Biến này giúp quái chém trúng 1 lần là thôi, không bị trừ máu liên tục
    private bool hasDealtDamage = false;

    // Hàm này tự động chạy mỗi khi Hitbox được bật lên (SetActive(true))
    private void OnEnable()
    {
        hasDealtDamage = false; // Reset lại trạng thái để chuẩn bị cho nhát chém mới
    }

    // Xử lý khi Player vừa chạm vào Hitbox
    private void OnTriggerEnter2D(Collider2D collision)
    {
        XuLySatThuong(collision);
    }

    // Xử lý khi Player đang đứng sẵn bên trong Hitbox
    private void OnTriggerStay2D(Collider2D collision)
    {
        XuLySatThuong(collision);
    }

    private void XuLySatThuong(Collider2D collision)
    {
        // Nếu chém trúng Player VÀ nhát chém này chưa gây sát thương bao giờ
        if (collision.CompareTag("Player") && !hasDealtDamage)
        {
            hasDealtDamage = true; // Đánh dấu là đã chém trúng rồi
            // Trừ HP
            collision.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
            // Tắt Hitbox đi ngay lập tức
            gameObject.SetActive(false);
        }
    }
}