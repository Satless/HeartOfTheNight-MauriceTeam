using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection; // 🔥 THÊM CÁI NÀY ĐỂ DÙNG TIA X-QUANG NỘI SOI
using HeartOfTheNight.Player;

public class HitEffectVFX : MonoBehaviour
{
    [Header("Cài đặt Hiệu ứng")]
    public SpriteRenderer spriteRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;

    [Header("Cảm biến sát thương (Tags)")]
    public List<string> damageTags = new List<string> { "Weapon", "EnemyBullet" };

    private Color originalColor;
    private Coroutine flashCoroutine;
    private int previousHealth;
    private PlayerHealth playerHealthScript;

    private void Start()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        playerHealthScript = GetComponent<PlayerHealth>();

        if (playerHealthScript != null)
        {
            previousHealth = playerHealthScript.GetCurrentHealth();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckHitAndFlash(collision.gameObject.tag);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckHitAndFlash(collision.gameObject.tag);
    }

    private void CheckHitAndFlash(string hitTag)
    {
        if (damageTags.Contains(hitTag))
        {
            if (playerHealthScript != null)
            {
                if (playerHealthScript.hasShield) return;
                StartCoroutine(WaitAndCheckHealthDrop_Player());
            }
            else
            {
                // 🔥 QUÁI BỊ CHÉM -> CHẠY HÀM ĐỢI VÀ SOI CODE DƯỚI NÀY
                StartCoroutine(WaitAndCheckDeath_Enemy());
            }
        }
    }

    // ==========================================
    // KHU VỰC DÀNH CHO QUÁI (TỰ ĐỘNG HÓA 100%)
    // ==========================================
    private IEnumerator WaitAndCheckDeath_Enemy()
    {
        // 1. Đợi 1 frame cho code trừ máu của con quái chạy xong
        yield return new WaitForEndOfFrame();

        // 2. Nội soi xem nó chết chưa
        if (KiemTraQuaiChetChua())
        {
            // NẾU ĐÃ CHẾT: Hủy chớp đỏ, đổ màu trắng trả lại màu gốc cho Animation Dead
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
        }
        else
        {
            // NẾU CÒN SỐNG: Chớp đỏ!
            TriggerFlash();
        }
    }

    private bool KiemTraQuaiChetChua()
    {
        // Dùng tia X-quang quét qua TẤT CẢ các script gắn trên cục này
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            System.Type type = script.GetType();

            // Tìm xem bạn coder có viết biến "isDead" (true/false) không
            FieldInfo isDeadField = type.GetField("isDead", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (isDeadField != null)
            {
                return (bool)isDeadField.GetValue(script);
            }

            // Nếu không có isDead, tìm xem có biến "currentHealth" không
            FieldInfo hpField = type.GetField("currentHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (hpField != null)
            {
                return (int)hpField.GetValue(script) <= 0; // Nếu máu <= 0 nghĩa là đã chết
            }
        }
        return false; // Nếu không tìm ra, mặc định là chưa chết
    }

    // ==========================================
    // KHU VỰC DÀNH CHO PLAYER (GIỮ NGUYÊN NHƯ CŨ)
    // ==========================================
    private IEnumerator WaitAndCheckHealthDrop_Player()
    {
        yield return new WaitForEndOfFrame();
        int currentHp = playerHealthScript.GetCurrentHealth();

        if (currentHp <= 0)
        {
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            yield break;
        }

        if (currentHp < previousHealth) TriggerFlash();
        previousHealth = currentHp;
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
}