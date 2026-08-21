using System.Collections;
using UnityEngine;
using HeartOfTheNight.Player; // Kéo thư viện Player của nhóm sếp vào

public class PlayerHitVFX : MonoBehaviour
{
    [Header("Cài đặt Hiệu ứng")]
    public SpriteRenderer spriteRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;

    private Color originalColor;
    private Coroutine flashCoroutine;

    // Biến để lưu máu cũ, dùng để so sánh
    private int previousHealth;

    private void Start()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        // Nội soi tìm file máu
        PlayerHealth healthScript = GetComponent<PlayerHealth>();

        if (healthScript != null)
        {
            // Lưu lại mức máu ban đầu
            previousHealth = healthScript.GetCurrentHealth();

            // 🔥 CHIÊU CUỐI: Đăng ký nghe lén cái loa báo máu của bạn sếp!
            healthScript.OnHealthChanged += HandleHealthChanged;
        }
        else
        {
            Debug.LogError("Ủa sếp ơi, không tìm thấy PlayerHealth trên cục này!");
        }
    }

    // Hàm này sẽ TỰ ĐỘNG CHẠY mỗi khi hàm TakeDamage hoặc Heal của bạn sếp kêu Invoke
    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        // Kiểm tra xem máu có bị TỤT XUỐNG không (Bị đánh)
        // (Tránh trường hợp bơm máu nó cũng nháy đỏ)
        if (currentHealth < previousHealth)
        {
            TriggerFlash();
        }

        // Cập nhật lại mức máu để lần sau tính tiếp
        previousHealth = currentHealth;
    }

    private void TriggerFlash()
    {
        if (spriteRenderer == null) return;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    // Nhớ dọn rác (hủy đăng ký) khi Player chết để chống lỗi RAM
    private void OnDestroy()
    {
        PlayerHealth healthScript = GetComponent<PlayerHealth>();
        if (healthScript != null)
        {
            healthScript.OnHealthChanged -= HandleHealthChanged;
        }
    }
}