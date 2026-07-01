/*using UnityEngine;

public class DoomSkill : MonoBehaviour
{
    [Header("Cài đặt Sát thương")]
    public int damage = 15;

    [Header("Dọn rác")]
    public float lifeTime = 5f; // Tự hủy sau 5 giây nếu bay ra ngoài map (để chống lag game)

    [Header("Hiệu ứng (Tùy chọn)")]
    public GameObject hitEffect; // Kéo prefab hiệu ứng nổ/tia lửa vào đây (nếu có)

    void Start()
    {
        // Vừa sinh ra là hẹn giờ 5 giây sau tự hủy luôn cho nhẹ máy
        Destroy(gameObject, lifeTime);
    }

    // Dùng OnTriggerEnter2D vì đạn thường là "Trigger" xuyên thấu chứ không phải cục gạch cản đường
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. NẾU BẮN TRÚNG PLAYER
        if (collision.CompareTag("Player"))
        {
            PlayerHealth pHealth = collision.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(damage);
                Debug.Log("Đạn trúng Player! Trừ " + damage + " máu.");
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
        // Nếu bạn có làm hiệu ứng nổ rùm beng thì nó sẽ hiện ra ở đây
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // Hủy viên đạn
        Destroy(gameObject);
    }
}*/