using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100; // Thêm biến máu tối đa
    public int health = 100;

    [Header("Hiệu ứng Thiêu đốt")]
    public bool isBurning = false;
    public int burnDamagePerTick = 5; // Mất 5 máu mỗi giây khi cháy
    private float burnTimer = 0f;

    void Start()
    {
        health = maxHealth; // Đầu game set máu đầy
    }

    void Update()
    {
        // Nếu đang bị cháy -> Bắt đầu đếm thời gian để trừ máu
        if (isBurning)
        {
            burnTimer += Time.deltaTime;
            if (burnTimer >= 1f) // Cứ mỗi 1 giây
            {
                TakeDamage(burnDamagePerTick);
                burnTimer = 0f; // Reset đồng hồ đếm
            }
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Player bị đánh! Máu còn: " + health);

        if (health <= 0)
        {
            Debug.Log("Player chết!");
            Destroy(gameObject);
        }
    }

    // Quái lửa sẽ gọi hàm này khi chém trúng
    public void StartBurning()
    {
        if (!isBurning)
        {
            isBurning = true;
            burnTimer = 0f;
            Debug.Log("Player đang BỊ THIÊU ĐỐT!");
        }
    }

    // Nút Dash sẽ gọi hàm này để dập lửa
    public void CureBurn()
    {
        if (isBurning)
        {
            isBurning = false;
            Debug.Log("Đã DẬP TẮT lửa!");
        }
    }

    // ================= HÀM MỚI THÊM: DÙNG ĐỂ HỒI MÁU =================
    public void Heal(int amount)
    {
        health += amount;

        // Nếu hồi lố qua mức tối đa thì ghim lại ở mức tối đa
        if (health > maxHealth)
        {
            health = maxHealth;
        }

        Debug.Log("Player vừa hồi máu! Máu hiện tại: " + health);
    }
}